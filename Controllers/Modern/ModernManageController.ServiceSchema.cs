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
    public async Task<IActionResult> ServiceSchema(string? q, string? tab, int page = 1, CancellationToken ct = default)
    {
        var disabled = await RequireFipsDatabaseAsync();
        if (disabled != null)
            return disabled;
        if (!await SchemaFeatureEnabledAsync())
            return NotFound();

        SetNav("manage-service-schema");
        var model = await ServiceSchemaWorkspace.LoadDirectoryAsync(_context, q, tab, page, ct);
        var report = await ServiceSchemaReport.LoadAsync(_context, ct);
        var byId = report.Products.ToDictionary(p => p.Id);
        foreach (var item in model.Items)
        {
            if (!byId.TryGetValue(item.Id, out var covered))
                continue;
            item.SchemaPercent = covered.Percent;
            item.SectionsAddressed = covered.TopicsRecorded;
            item.SectionCount = covered.TopicCount;
            item.CensusState = CensusState(covered);
        }

        var email = CurrentUserEmail.Trim();
        var myIds = string.IsNullOrWhiteSpace(email)
            ? new HashSet<Guid>()
            : (await _context.CMDBProductContacts.AsNoTracking()
                .Where(c => c.UserEmail != null && c.UserEmail.ToLower() == email.ToLower())
                .Select(c => c.CMDBProductId)
                .Distinct()
                .ToListAsync(ct)).ToHashSet();

        model.NotStarted = report.NotStarted;
        model.InProgress = report.InProgress;
        model.Complete = report.Complete;
        model.MyProducts = report.Products
            .Where(p => myIds.Contains(p.Id))
            .OrderBy(p => p.TopicCount > 0 && p.TopicsRecorded >= p.TopicCount)
            .ThenBy(p => p.Percent)
            .ThenBy(p => p.Title, StringComparer.OrdinalIgnoreCase)
            .Select(ToDirectoryRow)
            .ToList();
        model.Queue = report.Products
            .Where(p => p.TopicCount == 0 || p.TopicsRecorded < p.TopicCount)
            .Take(25)
            .Select(ToDirectoryRow)
            .ToList();
        model.QueueTotal = report.NotStarted + report.InProgress;
        return View("~/Views/Modern/Manage/ServiceSchema.cshtml", model);
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

        SetNav("manage-service-schema");
        var canEdit = await UserIsNamedProductContactAsync(id, CurrentUserEmail, ct)
            || await CanEditFipsProductInformationAsync(ct);
        var areaKey = section;
        if (string.IsNullOrWhiteSpace(areaKey))
        {
            var legacyArea = Request.Query["area"].ToString();
            if (!string.IsNullOrWhiteSpace(legacyArea))
                areaKey = legacyArea;
        }
        var pageModel = await ServiceSchemaWorkspace.LoadRecordAsync(_context, id, areaKey, topic, canEdit, ct);
        if (pageModel == null)
            return NotFound();
        return View("~/Views/Modern/Manage/ServiceSchemaProduct.cshtml", pageModel);
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
        var topic = layout.FindTopic(sectionKey);
        var section = ServiceSchemaCatalog.FindSection(sectionKey);
        var area = layout.Areas.FirstOrDefault(a => string.Equals(a.Key, sectionKey, StringComparison.OrdinalIgnoreCase));
        var key = topic?.Key ?? section?.Key ?? area?.Key;
        if (key == null)
            return await SchemaBack(id, sectionKey, "Choose a schema section.");

        var status = statusCode?.Trim();
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

    private static ServiceSchemaDirectoryRow ToDirectoryRow(ServiceSchemaDqProductRow row) =>
        new()
        {
            Id = row.Id,
            UniqueId = row.UniqueId,
            Title = row.Title,
            BusinessAreas = row.BusinessAreas,
            Phase = row.Phase,
            Status = "Active",
            CensusState = CensusState(row),
            SchemaPercent = row.Percent,
            SectionsAddressed = row.TopicsRecorded,
            SectionCount = row.TopicCount
        };

    private static string CensusState(ServiceSchemaDqProductRow row) =>
        row.TopicCount > 0 && row.TopicsRecorded >= row.TopicCount ? "Complete"
        : row.TopicsRecorded == 0 ? "Not started"
        : "In progress";

    private async Task<IActionResult> SaveTopicEntryAsync(Guid id, ServiceSchemaTopic topic, SchemaEntryInput input, CancellationToken ct)
    {
        var narrative = TrimTo(input.Narrative, 10000);
        var url = TrimTo(input.ExternalUrl, 500);
        string? title = TrimTo(input.Title, 300);
        string? lookup = null;
        Guid? catalogueId = input.CatalogueItemId is Guid existing && existing != Guid.Empty ? existing : null;

        if (topic.Mode == "lookup")
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
        else if (topic.Mode == "catalogue")
        {
            var createName = TrimTo(input.CreateCatalogueName, 300) ?? title;
            if (catalogueId == null && string.IsNullOrWhiteSpace(createName))
                return await SchemaBack(id, topic.Key, "Choose an existing item or add a new one.");

            if (catalogueId != null)
            {
                var item = await _context.CensusCatalogueItems.FirstOrDefaultAsync(c =>
                    c.Id == catalogueId && c.RemovedAt == null && c.Kind == topic.CatalogueKind && c.StatusCode == "ACTIVE", ct);
                if (item == null)
                    return await SchemaBack(id, topic.Key, "Choose an item from the list.");
                title = item.Name;
            }
            else
            {
                var match = await _context.CensusCatalogueItems.FirstOrDefaultAsync(c =>
                    c.Kind == topic.CatalogueKind && c.RemovedAt == null && c.StatusCode == "ACTIVE" && c.Name == createName, ct);
                if (match == null)
                {
                    match = new CensusCatalogueItem
                    {
                        Kind = topic.CatalogueKind!,
                        Reference = Guid.NewGuid().ToString("N")[..32].ToUpperInvariant(),
                        Name = createName!,
                        StatusCode = "ACTIVE",
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
        }
        else if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(narrative))
        {
            return await SchemaBack(id, topic.Key, "Enter a title or a description.");
        }

        var now = DateTime.UtcNow;
        var email = CurrentUserEmail;
        _context.CensusEntries.Add(new CensusEntry
        {
            ProductId = id,
            DomainKey = topic.Key,
            Title = title,
            Narrative = narrative,
            LookupCode = lookup,
            CatalogueItemId = catalogueId,
            PersonName = TrimTo(input.PersonName, 200),
            PersonEmail = TrimTo(input.PersonEmail, 255),
            ExternalUrl = url,
            VerificationStatusCode = "UNVERIFIED",
            CreatedAt = now,
            CreatedBy = email,
            UpdatedAt = now,
            UpdatedBy = email
        });
        await _context.SaveChangesAsync(ct);
        TempData["SuccessMessage"] = "Saved.";
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
            || await CanEditFipsProductInformationAsync(ct);
        if (!canEdit)
            return await SchemaBack(id, sectionKey, "Only a named contact or an operations console user can change this schema.");

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

    private async Task<IActionResult> SchemaBack(Guid id, string? sectionKey, string? error, CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(error))
            TempData["ErrorMessage"] = error;

        string? section = sectionKey;
        string? topic = null;
        var layout = await ServiceSchemaLayout.LoadAsync(_context, ct);
        var found = layout.FindTopic(sectionKey);
        if (found != null)
        {
            topic = found.Key;
            section = layout.AreaFor(found.Key)?.Key ?? sectionKey;
        }

        var url = Url.RouteUrl("ServiceSchemaProduct", new { id, section, topic })
            ?? $"/modern/manage/service-schema/{id}";
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
    public Guid? CatalogueItemId { get; set; }
    public int? StaffRoleId { get; set; }
    public string? PersonName { get; set; }
    public string? PersonEmail { get; set; }
    public string? ExternalUrl { get; set; }
    public string? VerificationStatusCode { get; set; }
    public string? CreateCatalogueName { get; set; }
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
