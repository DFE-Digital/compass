using Compass.Models;
using Compass.Models.Fips;
using Compass.Models.ServiceSchema;
using Compass.Services.ServiceSchema;
using Compass.ViewModels.Modern;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Compass.Controllers.Modern;

public partial class ModernManageController
{
    [HttpGet("service-schema")]
    public async Task<IActionResult> ServiceSchema(CancellationToken ct = default)
    {
        var disabled = await RequireFipsDatabaseAsync();
        if (disabled != null)
            return disabled;
        if (!await SchemaFeatureEnabledAsync())
            return NotFound();

        // Census is recorded on each product's Additional information tab.
        return RedirectToAction(nameof(Fips), new { tab = "active" });
    }

    [HttpGet("service-schema/pick-products")]
    public async Task<IActionResult> SchemaPickProducts(string? q, Guid? exclude, CancellationToken ct)
    {
        if (!await SchemaFeatureEnabledAsync())
            return NotFound();

        var term = (q ?? "").Trim().TrimStart('/').Trim();
        if (term.Length < 2)
            return Json(new { results = Array.Empty<object>() });

        var rows = await _context.CMDBProducts.AsNoTracking()
            .Where(p =>
                p.Status != CMDBProductStatus.Rejected &&
                p.Id != exclude &&
                p.Title != null &&
                p.Title.Contains(term))
            .OrderBy(p => p.Title)
            .Take(20)
            .Select(p => new { p.Id, p.Title, p.UniqueID, p.IsEnterpriseService })
            .ToListAsync(ct);
        var results = rows.Select(p => new
        {
            id = p.Id,
            name = p.Title + " (" + p.UniqueID + ")" + (p.IsEnterpriseService ? " — Enterprise" : "")
        });
        return Json(new { results });
    }

    [HttpGet("service-schema/{id:guid}", Name = "ServiceSchemaProduct")]
    [HttpGet("/ModernManage/ServiceSchemaProduct/{id:guid}")]
    public async Task<IActionResult> ServiceSchemaProduct(Guid id, string? section, string? topic, CancellationToken ct)
    {
        var disabled = await RequireFipsDatabaseAsync();
        if (disabled != null)
            return disabled;
        if (!await SchemaFeatureEnabledAsync())
            return NotFound();

        var areaKey = section;
        if (string.IsNullOrWhiteSpace(areaKey))
        {
            var legacyArea = Request.Query["area"].ToString();
            if (!string.IsNullOrWhiteSpace(legacyArea))
                areaKey = legacyArea;
        }

        return RedirectToAction(nameof(FipsProduct), new { id, tab = "additional", section = areaKey, topic });
    }

    [HttpPost("service-schema/{id:guid}/entries", Name = "SaveSchemaEntry")]
    [HttpPost("/ModernManage/ServiceSchemaProduct/{id:guid}/entries")]
    [HttpPost("fips/{id:guid}/schema/entries")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveSchemaEntry(Guid id, SchemaEntryInput input, CancellationToken ct)
    {
        var gate = await RequireSchemaEditorAsync(id, input.SectionKey, ct);
        if (gate != null)
            return gate;

        var layout = await ServiceSchemaLayout.LoadAsync(_context, ct);
        var topic = layout.FindTopic(input.SectionKey);
        if (topic != null)
            return await SaveTopicEntryAsync(id, topic, input, ct);

        var section = ServiceSchemaCatalog.FindSection(input.SectionKey);
        if (section == null)
            return await SchemaBack(id, null, "Choose a schema section.");

        var binding = ServiceSchemaAdminLookups.BindingFor(section.Key);
        var lookups = await ServiceSchemaAdminLookups.LoadAsync(
            _context, new[] { binding.PrimaryKey, binding.SecondaryKey }, ct);
        var primary = Allowed(lookups, binding.PrimaryKey);
        var secondary = Allowed(lookups, binding.SecondaryKey);
        var rows = ResponseRows(input);
        if (rows.Count == 0)
            return await SchemaBack(id, section.Key, "Add at least one response.");

        var email = CurrentUserEmail;
        var now = DateTime.UtcNow;
        var added = 0;
        foreach (var row in rows)
        {
            var narrative = TrimTo(row.Narrative, 10000);
            var url = TrimTo(row.Url, 500);
            var lookup = TrimTo(row.Lookup, 80);
            var lookupSecondary = TrimTo(row.SecondaryLookup, 80);
            if (!string.IsNullOrWhiteSpace(lookup) && !primary.Contains(lookup))
                return await SchemaBack(id, section.Key, "Choose a value from the list.");
            if (!string.IsNullOrWhiteSpace(lookupSecondary) && !secondary.Contains(lookupSecondary))
                return await SchemaBack(id, section.Key, "Choose a value from the list.");

            int? staffRoleId = null;
            if (row.StaffRoleId is int roleId)
            {
                var roleOk = await _context.StaffRoles.AnyAsync(r => r.Id == roleId && r.IsActive, ct);
                if (!roleOk)
                    return await SchemaBack(id, section.Key, "Choose an active staff role.");
                staffRoleId = roleId;
            }

            var title = TrimTo(row.Title, 300)
                ?? ServiceSchemaAdminLookups.LabelFor(lookups, lookup)
                ?? ServiceSchemaAdminLookups.LabelFor(lookups, lookupSecondary);
            var person = TrimTo(row.PersonName, 200);
            if (string.IsNullOrWhiteSpace(title) &&
                string.IsNullOrWhiteSpace(narrative) &&
                string.IsNullOrWhiteSpace(person) &&
                string.IsNullOrWhiteSpace(url))
                continue;

            _context.CensusEntries.Add(new CensusEntry
            {
                ProductId = id,
                DomainKey = section.DomainKey,
                Title = title,
                Narrative = narrative,
                LookupCode = lookup,
                SecondaryLookupCode = lookupSecondary,
                StaffRoleId = staffRoleId,
                PersonName = person,
                PersonEmail = TrimTo(row.PersonEmail, 255),
                ExternalUrl = url,
                VerificationStatusCode = "UNVERIFIED",
                CreatedAt = now,
                CreatedBy = email,
                UpdatedAt = now,
                UpdatedBy = email
            });
            added++;
        }

        if (added == 0)
            return await SchemaBack(id, section.Key, "Add a title, a lookup, or a description for each response.");

        await _context.SaveChangesAsync(ct);
        TempData["SuccessMessage"] = added == 1 ? "Response saved." : added + " responses saved.";
        return await SchemaBack(id, section.Key, null);
    }

    [HttpPost("service-schema/{id:guid}/entries/{entryId:guid}/remove", Name = "RemoveSchemaEntry")]
    [HttpPost("/ModernManage/ServiceSchemaProduct/{id:guid}/entries/{entryId:guid}/remove")]
    [HttpPost("fips/{id:guid}/schema/entries/{entryId:guid}/remove")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveSchemaEntry(Guid id, Guid entryId, string? sectionKey, string? reason, CancellationToken ct)
    {
        var gate = await RequireSchemaEditorAsync(id, sectionKey, ct);
        if (gate != null)
            return gate;

        var entry = await _context.CensusEntries.FirstOrDefaultAsync(
            e => e.Id == entryId && e.ProductId == id && e.RemovedAt == null, ct);
        if (entry == null)
            return await SchemaBack(id, sectionKey, "That entry is no longer on this service.");

        entry.RemovedAt = DateTime.UtcNow;
        entry.RemovedBy = CurrentUserEmail;
        entry.RemovalReason = TrimTo(reason, 500);
        entry.UpdatedAt = entry.RemovedAt.Value;
        entry.UpdatedBy = entry.RemovedBy;
        entry.RowVersion++;
        await _context.SaveChangesAsync(ct);
        TempData["SuccessMessage"] = "Schema entry removed from this service.";
        return await SchemaBack(id, sectionKey, null);
    }

    [HttpPost("service-schema/{id:guid}/sections", Name = "SaveSchemaSection")]
    [HttpPost("/ModernManage/ServiceSchemaProduct/{id:guid}/sections")]
    [HttpPost("fips/{id:guid}/schema/sections")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveSchemaSection(Guid id, string? sectionKey, string? statusCode, string? explanation, CancellationToken ct)
    {
        var gate = await RequireSchemaEditorAsync(id, sectionKey, ct);
        if (gate != null)
            return gate;

        var layout = await ServiceSchemaLayout.LoadAsync(_context, ct);
        var status = statusCode?.Trim();
        if (ServiceSchemaAreas.IsProductDetails(sectionKey) &&
            (ServiceSchemaWorkspace.IsSectionCompleteStatus(status) ||
             string.Equals(status, "SECTION_OPEN", StringComparison.OrdinalIgnoreCase)))
        {
            var detailsEmail = CurrentUserEmail;
            var detailsRow = await _context.CensusSectionDeclarations
                .FirstOrDefaultAsync(d => d.ProductId == id && d.SectionKey == ServiceSchemaAreas.ProductDetailsKey, ct);
            if (string.Equals(status, "SECTION_OPEN", StringComparison.OrdinalIgnoreCase))
            {
                if (detailsRow != null && ServiceSchemaWorkspace.IsSectionCompleteStatus(detailsRow.StatusCode))
                    _context.CensusSectionDeclarations.Remove(detailsRow);
                await _context.SaveChangesAsync(ct);
                TempData["SuccessMessage"] = "Product details set back to in progress.";
                return await SchemaBack(id, ServiceSchemaAreas.ProductDetailsKey, null);
            }

            if (!await ProductDetailsHasInformationAsync(id, ct))
                return await SchemaBack(id, ServiceSchemaAreas.ProductDetailsKey, "Add or update product details before marking this section complete.");

            if (detailsRow == null)
            {
                detailsRow = new CensusSectionDeclaration { ProductId = id, SectionKey = ServiceSchemaAreas.ProductDetailsKey };
                _context.CensusSectionDeclarations.Add(detailsRow);
            }

            detailsRow.StatusCode = ServiceSchemaWorkspace.SectionCompleteStatus;
            detailsRow.Explanation = null;
            detailsRow.UpdatedAt = DateTime.UtcNow;
            detailsRow.UpdatedBy = detailsEmail;
            await _context.SaveChangesAsync(ct);
            TempData["SuccessMessage"] = "Product details marked complete.";
            return await SchemaBack(id, ServiceSchemaAreas.ProductDetailsKey, null);
        }

        if (ServiceSchemaWorkspace.IsSectionCompleteStatus(status) ||
            string.Equals(status, "SECTION_OPEN", StringComparison.OrdinalIgnoreCase))
        {
            var topicMatch = layout.FindTopic(sectionKey);
            if (topicMatch == null)
                return await SchemaBack(id, sectionKey, "Choose a schema question.");

            var completeEmail = CurrentUserEmail;
            var completeRow = await _context.CensusSectionDeclarations
                .FirstOrDefaultAsync(d => d.ProductId == id && d.SectionKey == topicMatch.Key, ct);
            var topicName = ServiceSchemaAreas.ShortTopicName(topicMatch);
            if (string.Equals(status, "SECTION_OPEN", StringComparison.OrdinalIgnoreCase))
            {
                if (completeRow != null && ServiceSchemaWorkspace.IsSectionCompleteStatus(completeRow.StatusCode))
                    _context.CensusSectionDeclarations.Remove(completeRow);
                await _context.SaveChangesAsync(ct);
                TempData["SuccessMessage"] = topicName + " set back to in progress.";
                return await SchemaBack(id, topicMatch.Key, null);
            }

            if (!await TopicHasRecordedInformationAsync(id, topicMatch, ct))
                return await SchemaBack(id, topicMatch.Key, "Add a response, or mark nothing to record, before marking this question complete.");

            if (completeRow == null)
            {
                completeRow = new CensusSectionDeclaration { ProductId = id, SectionKey = topicMatch.Key };
                _context.CensusSectionDeclarations.Add(completeRow);
            }

            completeRow.StatusCode = ServiceSchemaWorkspace.SectionCompleteStatus;
            completeRow.Explanation = null;
            completeRow.UpdatedAt = DateTime.UtcNow;
            completeRow.UpdatedBy = completeEmail;
            await _context.SaveChangesAsync(ct);
            TempData["SuccessMessage"] = topicName + " marked complete.";
            return await SchemaBack(id, topicMatch.Key, null);
        }

        var topic = layout.FindTopic(sectionKey);
        var section = ServiceSchemaCatalog.FindSection(sectionKey);
        var area = layout.Areas.FirstOrDefault(a => string.Equals(a.Key, sectionKey, StringComparison.OrdinalIgnoreCase));
        var key = topic?.Key ?? section?.Key ?? area?.Key;
        if (key == null)
            return await SchemaBack(id, sectionKey, "Choose a schema section.");

        var email = CurrentUserEmail;
        var row = await _context.CensusSectionDeclarations
            .FirstOrDefaultAsync(d => d.ProductId == id && d.SectionKey == key, ct);
        if (string.IsNullOrWhiteSpace(status) || string.Equals(status, "NOT_RECORDED", StringComparison.OrdinalIgnoreCase))
        {
            if (row != null)
                _context.CensusSectionDeclarations.Remove(row);
            await _context.SaveChangesAsync(ct);
            TempData["SuccessMessage"] = "You can add a response for this entry.";
            return await SchemaBack(id, key, null);
        }

        if (!ServiceSchemaWorkspace.IsClosingStatus(status))
            return await SchemaBack(id, key, "Choose confirmed none or not applicable.");

        if (topic != null && await TopicHasRecordedInformationAsync(id, topic, ct))
            return await SchemaBack(id, key, "Information added for this question. Check your submission and if correct, mark the question complete.");

        if (row == null)
        {
            row = new CensusSectionDeclaration { ProductId = id, SectionKey = key };
            _context.CensusSectionDeclarations.Add(row);
        }

        row.StatusCode = status;
        row.Explanation = TrimTo(explanation, 2000);
        row.UpdatedAt = DateTime.UtcNow;
        row.UpdatedBy = email;
        await _context.SaveChangesAsync(ct);
        TempData["SuccessMessage"] = "Nothing to record for this entry.";
        return await SchemaBack(id, key, null);
    }

    private async Task<IActionResult> SaveTopicEntryAsync(Guid id, ServiceSchemaTopic topic, SchemaEntryInput input, CancellationToken ct)
    {
        if (ServiceSchemaAreas.IsServiceResponsibility(topic.Key))
            return await SchemaBack(id, topic.Key, "Service offering contacts are managed on the product record.");

        var narrative = TrimTo(input.Narrative, 10000);
        var url = TrimTo(input.ExternalUrl, 500);
        string? title = TrimTo(input.Title, 300);
        string? lookup = null;
        string? secondary = null;
        Guid? catalogueId = input.CatalogueItemId is Guid existing && existing != Guid.Empty ? existing : null;

        if (string.Equals(topic.Key, "dependency", StringComparison.OrdinalIgnoreCase)
            && string.Equals(input.DependencyScope, "internal", StringComparison.OrdinalIgnoreCase))
        {
            if (input.ProductId is not Guid productId || productId == Guid.Empty || productId == id)
                return await SchemaBack(id, topic.Key, "Choose a service register product.");
            var linked = await _context.CMDBProducts.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == productId && p.Status != CMDBProductStatus.Rejected, ct);
            if (linked == null)
                return await SchemaBack(id, topic.Key, "Choose a product from the register, including Enterprise services.");
            var productKey = productId.ToString("D");
            var duplicate = await _context.CensusEntries.AnyAsync(e =>
                e.ProductId == id && e.RemovedAt == null && e.DomainKey == topic.Key && e.LookupCode == productKey, ct);
            if (duplicate)
                return await SchemaBack(id, topic.Key, "That product is already an internal dependency.");
            title = linked.IsEnterpriseService ? linked.Title + " (Enterprise)" : linked.Title;
            lookup = productKey;
        }
        else if (topic.Mode == "lookup")
        {
            lookup = TrimTo(input.LookupCode, 80);
            var lookups = await ServiceSchemaAdminLookups.LoadAsync(_context, new[] { topic.LookupSource }, ct);
            var allowed = lookups.TryGetValue(topic.LookupSource ?? "", out var choices)
                ? choices.Select(c => c.Value).ToHashSet(StringComparer.OrdinalIgnoreCase)
                : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(lookup) || !allowed.Contains(lookup))
                return await SchemaBack(id, topic.Key, "Choose a value from the list.");
            title ??= ServiceSchemaAdminLookups.LabelFor(lookups, lookup);
        }
        else if (topic.Mode == "choice")
        {
            lookup = TrimTo(input.LookupCode, 80);
            var chosen = topic.ChoiceOptions.FirstOrDefault(c => string.Equals(c.Value, lookup, StringComparison.OrdinalIgnoreCase));
            if (chosen == null)
                return await SchemaBack(id, topic.Key, "Choose one of the options.");
            title = chosen.Label;
        }
        else if (topic.Mode == "yes-choice")
        {
            lookup = TrimTo(input.LookupCode, 80);
            if (string.Equals(lookup, "no", StringComparison.OrdinalIgnoreCase))
            {
                title = "No";
                lookup = "no";
                narrative = null;
            }
            else if (string.Equals(lookup, "yes", StringComparison.OrdinalIgnoreCase))
            {
                var chosen = (input.SecondaryLookupCodes ?? [])
                    .Select(code => topic.ChoiceOptions.FirstOrDefault(c => string.Equals(c.Value, code?.Trim(), StringComparison.OrdinalIgnoreCase)))
                    .Where(c => c != null)
                    .DistinctBy(c => c!.Value, StringComparer.OrdinalIgnoreCase)
                    .Cast<ServiceSchemaChoice>()
                    .ToList();
                if (chosen.Count == 0)
                    return await SchemaBack(id, topic.Key, "Choose the kind of enabling functionality.");
                if (chosen.Any(c => string.Equals(c.Value, "other", StringComparison.OrdinalIgnoreCase)) && string.IsNullOrWhiteSpace(narrative))
                    return await SchemaBack(id, topic.Key, "Describe the other enabling functionality.");
                title = "Yes — " + string.Join(", ", chosen.Select(c => c.Label));
                lookup = "yes";
                secondary = string.Join(",", chosen.Select(c => c.Value));
                if (secondary.Length > 80)
                    return await SchemaBack(id, topic.Key, "Choose fewer kinds of enabling functionality.");
            }
            else
            {
                return await SchemaBack(id, topic.Key, "Choose yes or no.");
            }
        }
        else if (topic.Mode == "products")
        {
            if (input.ProductId is not Guid productId || productId == Guid.Empty || productId == id)
                return await SchemaBack(id, topic.Key, "Choose another service register product.");
            var linked = await _context.CMDBProducts.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == productId && p.Status != CMDBProductStatus.Rejected, ct);
            if (linked == null)
                return await SchemaBack(id, topic.Key, "Choose a product from the register.");
            var productKey = productId.ToString("D");
            var duplicate = await _context.CensusEntries.AnyAsync(e =>
                e.ProductId == id && e.RemovedAt == null && e.DomainKey == topic.Key && e.LookupCode == productKey, ct);
            if (duplicate)
                return await SchemaBack(id, topic.Key, "That product is already on this list.");
            title = linked.Title;
            lookup = productKey;
        }
        else if (topic.Mode == "catalogue")
        {
            var createName = TrimTo(input.CreateCatalogueName, 300) ?? title;
            if (catalogueId == null && string.IsNullOrWhiteSpace(createName))
                return await SchemaBack(id, topic.Key, "Choose an existing item or add a new one.");

            if (catalogueId != null)
            {
                var item = await _context.CensusCatalogueItems.FirstOrDefaultAsync(c =>
                    c.Id == catalogueId && c.RemovedAt == null && c.Kind == topic.CatalogueKind &&
                    (c.StatusCode == "ACTIVE" || c.StatusCode == "NEW"), ct);
                if (item == null)
                    return await SchemaBack(id, topic.Key, "Choose an item from the list.");
                title = item.Name;
            }
            else
            {
                var match = await _context.CensusCatalogueItems.FirstOrDefaultAsync(c =>
                    c.Kind == topic.CatalogueKind && c.RemovedAt == null &&
                    (c.StatusCode == "ACTIVE" || c.StatusCode == "NEW") && c.Name == createName, ct);
                if (match == null)
                {
                    match = new CensusCatalogueItem
                    {
                        Kind = topic.CatalogueKind!,
                        Reference = Guid.NewGuid().ToString("N")[..32].ToUpperInvariant(),
                        Name = createName!,
                        StatusCode = "NEW",
                        AddedAgainstProductId = id,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = CurrentUserEmail,
                        UpdatedAt = DateTime.UtcNow,
                        UpdatedBy = CurrentUserEmail
                    };
                    _context.CensusCatalogueItems.Add(match);
                }
                catalogueId = match.Id;
                title = match.Name;
            }

            if (catalogueId != null && string.Equals(topic.Key, ServiceSchemaAreas.ApiUseKey, StringComparison.OrdinalIgnoreCase))
            {
                var providesIt = await _context.CensusEntries.AnyAsync(e =>
                    e.ProductId == id && e.RemovedAt == null && e.CatalogueItemId == catalogueId &&
                    e.DomainKey == ServiceSchemaAreas.ApiProvideKey, ct);
                if (providesIt)
                    return await SchemaBack(id, topic.Key, "This product provides that API. Choose an API another product provides, or add one from outside the register.");
            }

            if (catalogueId != null &&
                (string.Equals(topic.Key, ServiceSchemaAreas.ApiUseKey, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(topic.Key, ServiceSchemaAreas.ApiProvideKey, StringComparison.OrdinalIgnoreCase)))
            {
                var duplicate = await _context.CensusEntries.AnyAsync(e =>
                    e.ProductId == id && e.RemovedAt == null && e.DomainKey == topic.Key && e.CatalogueItemId == catalogueId, ct);
                if (duplicate)
                    return await SchemaBack(id, topic.Key, "That API is already on this list.");
            }
        }
        else if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(narrative))
        {
            return await SchemaBack(id, topic.Key, "Enter a title or a description.");
        }

        var now = DateTime.UtcNow;
        var email = CurrentUserEmail;
        var entry = topic.Mode == "yes-choice"
            ? await _context.CensusEntries.FirstOrDefaultAsync(e =>
                e.ProductId == id && e.RemovedAt == null && e.DomainKey == topic.Key, ct)
            : null;
        var created = entry == null;
        if (entry == null)
        {
            entry = new CensusEntry
            {
                ProductId = id,
                DomainKey = topic.Key,
                CreatedAt = now,
                CreatedBy = email,
                VerificationStatusCode = "UNVERIFIED"
            };
            _context.CensusEntries.Add(entry);
        }

        entry.Title = title ?? "";
        entry.Narrative = narrative;
        entry.LookupCode = lookup;
        entry.SecondaryLookupCode = secondary;
        entry.CatalogueItemId = catalogueId;
        entry.ExternalUrl = url;
        entry.PersonName = TrimTo(input.PersonName, 200);
        entry.PersonEmail = TrimTo(input.PersonEmail, 255);
        entry.UpdatedAt = now;
        entry.UpdatedBy = email;
        await _context.SaveChangesAsync(ct);
        TempData["SuccessMessage"] = created ? "Saved." : "Answer updated.";
        return await SchemaBack(id, topic.Key, null);
    }

    [HttpPost("fips/{id:guid}/schema/services")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LinkSchemaService(Guid id, Guid? serviceId, string? relationshipCode, string? narrative, CancellationToken ct)
    {
        var gate = await RequireSchemaEditorAsync(id, "service-estate", ct);
        if (gate != null)
            return gate;

        if (serviceId == null || !await _context.CensusServices.AnyAsync(s => s.Id == serviceId && s.RemovedAt == null, ct))
            return await SchemaBack(id, "service-estate", "Choose a service.");
        if (!await LookupCodeIsActiveAsync("service_product_relationship", relationshipCode, ct))
            return await SchemaBack(id, "service-estate", "Choose how this product relates to the service.");

        var duplicate = await _context.CensusServiceProducts.AnyAsync(x =>
            x.ProductId == id &&
            x.ServiceId == serviceId &&
            x.RelationshipCode == relationshipCode &&
            x.RemovedAt == null, ct);
        if (duplicate)
            return await SchemaBack(id, "service-estate", "That service is already linked in this way.");

        _context.CensusServiceProducts.Add(new CensusServiceProduct
        {
            ProductId = id,
            ServiceId = serviceId.Value,
            RelationshipCode = relationshipCode!.Trim(),
            Narrative = TrimTo(narrative, 4000),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = CurrentUserEmail
        });
        await _context.SaveChangesAsync(ct);
        TempData["SuccessMessage"] = "Service linked.";
        return await SchemaBack(id, "service-estate", null);
    }

    [HttpPost("fips/{id:guid}/schema/services/{linkId:guid}/remove")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnlinkSchemaService(Guid id, Guid linkId, CancellationToken ct)
    {
        var gate = await RequireSchemaEditorAsync(id, "service-estate", ct);
        if (gate != null)
            return gate;

        var link = await _context.CensusServiceProducts.FirstOrDefaultAsync(
            x => x.Id == linkId && x.ProductId == id && x.RemovedAt == null, ct);
        if (link != null)
        {
            link.RemovedAt = DateTime.UtcNow;
            link.RemovedBy = CurrentUserEmail;
            await _context.SaveChangesAsync(ct);
            TempData["SuccessMessage"] = "Service unlinked. The service itself is unchanged.";
        }

        return await SchemaBack(id, "service-estate", null);
    }

    private Task<bool> SchemaFeatureEnabledAsync() =>
        _globalFeatureToggle.IsFeatureEnabledForPrincipalAsync(FeatureCodes.ServiceRegisterSchema, User);

    private async Task<IActionResult?> RequireSchemaEditorAsync(Guid id, string? sectionKey, CancellationToken ct)
    {
        if (!await SchemaFeatureEnabledAsync())
            return NotFound();

        var exists = await _context.CMDBProducts.AnyAsync(p => p.Id == id, ct);
        if (!exists)
            return NotFound();

        var canEdit = await UserIsNamedProductContactAsync(id, CurrentUserEmail, ct)
            || await UserIsNamedInAdditionalResponsibilitiesAsync(id, CurrentUserEmail, ct)
            || await CanEditFipsProductInformationAsync(ct);
        if (!canEdit)
            return await SchemaBack(id, sectionKey, "Only a named contact, someone named in additional responsibilities, or an operations console user can change this schema.");

        return null;
    }

    private async Task<bool> LookupCodeIsActiveAsync(string? setKey, string? code, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(setKey))
            return string.IsNullOrWhiteSpace(code);
        if (string.IsNullOrWhiteSpace(code))
            return true;
        return await _context.ServiceSchemaLookupValues.AnyAsync(v =>
            v.IsActive &&
            v.Code == code &&
            v.LookupSet.Key == setKey, ct);
    }

    private async Task<bool> TopicHasRecordedInformationAsync(Guid productId, ServiceSchemaTopic topic, CancellationToken ct)
    {
        var domains = await _context.CensusEntries.AsNoTracking()
            .Where(e => e.ProductId == productId && e.RemovedAt == null)
            .Select(e => e.DomainKey)
            .ToListAsync(ct);
        if (domains.Any(domain => ServiceSchemaAreas.DomainMatches(topic, domain)))
            return true;
        return ServiceSchemaAreas.IsServiceResponsibility(topic.Key) &&
            await _context.CMDBProductContacts.AnyAsync(c => c.CMDBProductId == productId, ct);
    }

    private async Task<bool> ProductDetailsHasInformationAsync(Guid productId, CancellationToken ct)
    {
        var product = await _context.CMDBProducts.AsNoTracking()
            .Where(p => p.Id == productId)
            .Select(p => new
            {
                HasDescription = p.UserDescription != null && p.UserDescription != "",
                HasPhase = p.PhaseId != null,
                HasDirectorate = p.Directorates.Any(),
                HasBusinessArea = p.BusinessAreas.Any(),
                HasChannel = p.Channels.Any(),
                HasType = p.Types.Any(),
                HasUrl = p.ProductURL != null && p.ProductURL != "",
                HasCategorisation = p.CategorisationItems.Any()
            })
            .FirstOrDefaultAsync(ct);
        if (product == null)
            return false;
        return product.HasDescription
            || product.HasPhase
            || product.HasDirectorate
            || product.HasBusinessArea
            || product.HasChannel
            || product.HasType
            || product.HasUrl
            || product.HasCategorisation;
    }

    private async Task<IActionResult> SchemaBack(Guid id, string? sectionKey, string? error, CancellationToken ct = default, bool areaOnly = false)
    {
        if (!string.IsNullOrWhiteSpace(error))
            TempData["ErrorMessage"] = error;

        string? section = sectionKey;
        string? topic = null;
        if (!areaOnly)
        {
            var layout = await ServiceSchemaLayout.LoadAsync(_context, ct);
            var found = layout.FindTopic(sectionKey);
            if (found != null)
            {
                topic = found.Key;
                section = layout.AreaFor(found.Key)?.Key ?? sectionKey;
            }
        }

        var url = Url.Action(nameof(FipsProduct), new { id, tab = "additional", section, topic })
            ?? $"/modern/manage/fips/{id}?tab=additional";
        return LocalRedirect(url);
    }

    private static HashSet<string> Allowed(
        IReadOnlyDictionary<string, List<ServiceSchemaChoice>> lookups,
        string? key)
    {
        if (key == null || !lookups.TryGetValue(key, out var list))
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        return list.Select(c => c.Value).ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static List<SchemaResponseRow> ResponseRows(SchemaEntryInput input)
    {
        var count = new[]
        {
            input.ResponseTitle?.Length ?? 0,
            input.ResponseNarrative?.Length ?? 0,
            input.ResponseLookup?.Length ?? 0,
            input.ResponseLookupSecondary?.Length ?? 0,
            input.ResponseStaffRole?.Length ?? 0,
            input.ResponsePerson?.Length ?? 0,
            input.ResponseEmail?.Length ?? 0,
            input.ResponseUrl?.Length ?? 0
        }.Max();

        if (count == 0)
        {
            return
            [
                new SchemaResponseRow(
                    input.Title,
                    input.Narrative,
                    input.LookupCode,
                    input.SecondaryLookupCode,
                    input.StaffRoleId,
                    input.PersonName,
                    input.PersonEmail,
                    input.ExternalUrl)
            ];
        }

        var rows = new List<SchemaResponseRow>(count);
        for (var i = 0; i < count; i++)
        {
            int? role = null;
            if (int.TryParse(At(input.ResponseStaffRole, i), out var parsed) && parsed > 0)
                role = parsed;
            rows.Add(new SchemaResponseRow(
                At(input.ResponseTitle, i),
                At(input.ResponseNarrative, i),
                At(input.ResponseLookup, i),
                At(input.ResponseLookupSecondary, i),
                role,
                At(input.ResponsePerson, i),
                At(input.ResponseEmail, i),
                At(input.ResponseUrl, i)));
        }

        return rows;
    }

    private static string? At(string[]? values, int index) =>
        values != null && index < values.Length ? values[index] : null;

    private static string? TrimTo(string? value, int max)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return null;
        return trimmed.Length <= max ? trimmed : trimmed[..max];
    }
}

public class SchemaEntryInput
{
    public string? SectionKey { get; set; }
    public string? Title { get; set; }
    public string? Narrative { get; set; }
    public string? LookupCode { get; set; }
    public string? SecondaryLookupCode { get; set; }
    public List<string>? SecondaryLookupCodes { get; set; }
    public Guid? CatalogueItemId { get; set; }
    public int? StaffRoleId { get; set; }
    public string? PersonName { get; set; }
    public string? PersonEmail { get; set; }
    public string? ExternalUrl { get; set; }
    public string? VerificationStatusCode { get; set; }
    public string? CreateCatalogueName { get; set; }
    public Guid? ProductId { get; set; }
    public string? DependencyScope { get; set; }
    public string[]? ResponseTitle { get; set; }
    public string[]? ResponseNarrative { get; set; }
    public string[]? ResponseLookup { get; set; }
    public string[]? ResponseLookupSecondary { get; set; }
    public string[]? ResponseStaffRole { get; set; }
    public string[]? ResponsePerson { get; set; }
    public string[]? ResponseEmail { get; set; }
    public string[]? ResponseUrl { get; set; }
}

public sealed record SchemaResponseRow(
    string? Title,
    string? Narrative,
    string? Lookup,
    string? SecondaryLookup,
    int? StaffRoleId,
    string? PersonName,
    string? PersonEmail,
    string? Url);
