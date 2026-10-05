using Compass.Data;
using Compass.Models.ServiceSchema;
using Compass.ViewModels.Modern;
using Microsoft.EntityFrameworkCore;

namespace Compass.Services.ServiceSchema;

public static class ServiceSchemaProductLoader
{
    public static async Task<ServiceSchemaProductPage> LoadAsync(
        CompassDbContext db,
        Guid productId,
        bool canEdit,
        CancellationToken cancellationToken)
    {
        var lookups = await db.ServiceSchemaLookupValues.AsNoTracking()
            .Include(v => v.LookupSet)
            .OrderBy(v => v.SortOrder).ThenBy(v => v.Label)
            .ToListAsync(cancellationToken);

        var lookupLabels = new Dictionary<(string Set, string Code), string>();
        var lookupChoices = new Dictionary<string, List<ServiceSchemaChoice>>(StringComparer.OrdinalIgnoreCase);
        foreach (var value in lookups)
        {
            var setKey = value.LookupSet.Key;
            lookupLabels[(setKey, value.Code)] = value.Label;
            if (!value.IsActive)
                continue;
            if (!lookupChoices.TryGetValue(setKey, out var list))
            {
                list = new List<ServiceSchemaChoice>();
                lookupChoices[setKey] = list;
            }
            list.Add(new ServiceSchemaChoice { Value = value.Code, Label = value.Label });
        }

        var catalogues = await db.CensusCatalogueItems.AsNoTracking()
            .Where(c => c.RemovedAt == null && c.StatusCode != "RETIRED")
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);
        var catalogueChoices = new Dictionary<string, List<ServiceSchemaChoice>>(StringComparer.OrdinalIgnoreCase);
        var catalogueNames = catalogues.ToDictionary(c => c.Id, c => c.Name);
        foreach (var item in catalogues)
        {
            if (!catalogueChoices.TryGetValue(item.Kind, out var list))
            {
                list = new List<ServiceSchemaChoice>();
                catalogueChoices[item.Kind] = list;
            }
            list.Add(new ServiceSchemaChoice { Value = item.Id.ToString(), Label = item.Name });
        }

        var staffRoles = await db.StaffRoles.AsNoTracking()
            .Where(r => r.IsActive)
            .OrderBy(r => r.Family).ThenBy(r => r.SortOrder).ThenBy(r => r.Name)
            .Select(r => new ServiceSchemaChoice { Value = r.Id.ToString(), Label = r.Name, Group = r.Family })
            .ToListAsync(cancellationToken);
        var staffById = await db.StaffRoles.AsNoTracking()
            .ToDictionaryAsync(r => r.Id, r => r.Family + " — " + r.Name, cancellationToken);

        var services = await db.CensusServices.AsNoTracking()
            .Where(s => s.RemovedAt == null)
            .OrderBy(s => s.Name)
            .Select(s => new ServiceSchemaChoice { Value = s.Id.ToString(), Label = s.Reference + " — " + s.Name })
            .ToListAsync(cancellationToken);

        var lineLinks = await db.CensusServiceLineServices.AsNoTracking()
            .Where(x => x.RemovedAt == null)
            .Select(x => new { x.ServiceId, x.ServiceLine.Name })
            .ToListAsync(cancellationToken);
        var linesByService = lineLinks
            .GroupBy(x => x.ServiceId)
            .ToDictionary(g => g.Key, g => string.Join(", ", g.Select(x => x.Name).Distinct().OrderBy(n => n)));

        var productLinks = await db.CensusServiceProducts.AsNoTracking()
            .Include(x => x.Service)
            .Where(x => x.ProductId == productId && x.RemovedAt == null)
            .OrderBy(x => x.Service.Name)
            .ToListAsync(cancellationToken);

        var entries = await db.CensusEntries.AsNoTracking()
            .Where(e => e.ProductId == productId && e.RemovedAt == null)
            .OrderBy(e => e.SortOrder).ThenBy(e => e.CreatedAt)
            .ToListAsync(cancellationToken);

        var declarations = await db.CensusSectionDeclarations.AsNoTracking()
            .Where(d => d.ProductId == productId)
            .ToListAsync(cancellationToken);
        var declarationByKey = declarations.ToDictionary(d => d.SectionKey, StringComparer.OrdinalIgnoreCase);

        string Label(string? setKey, string? code)
        {
            if (string.IsNullOrWhiteSpace(setKey) || string.IsNullOrWhiteSpace(code))
                return "";
            return lookupLabels.TryGetValue((setKey, code), out var label) ? label : code;
        }

        var page = new ServiceSchemaProductPage
        {
            CanEdit = canEdit,
            Services = services,
            StaffRoles = staffRoles,
            Lookups = lookupChoices,
            Catalogues = catalogueChoices,
            RelationshipTypes = lookupChoices.GetValueOrDefault("service_product_relationship") ?? new(),
            DeclarationStatuses = lookupChoices.GetValueOrDefault("missing_information_status") ?? new(),
            VerificationStatuses = lookupChoices.GetValueOrDefault("verification_status") ?? new(),
            LinkedServices = productLinks.Select(link => new ServiceSchemaLinkedService
            {
                LinkId = link.Id,
                ServiceName = link.Service.Reference + " — " + link.Service.Name,
                RelationshipLabel = Label("service_product_relationship", link.RelationshipCode),
                Narrative = link.Narrative,
                Lines = linesByService.TryGetValue(link.ServiceId, out var names) && !string.IsNullOrWhiteSpace(names) ? names : "—"
            }).ToList()
        };

        foreach (var section in ServiceSchemaCatalog.Sections)
        {
            declarationByKey.TryGetValue(section.Key, out var declaration);
            var rows = entries.Where(e => string.Equals(e.DomainKey, section.DomainKey, StringComparison.OrdinalIgnoreCase));
            page.Sections.Add(new ServiceSchemaSectionPage
            {
                Key = section.Key,
                Title = section.Title,
                Help = section.Help,
                DomainKey = section.DomainKey,
                LookupSetKey = section.LookupSetKey,
                LookupLabel = section.LookupLabel,
                SecondaryLookupSetKey = section.SecondaryLookupSetKey,
                SecondaryLookupLabel = section.SecondaryLookupLabel,
                CatalogueKind = section.CatalogueKind,
                CapturePerson = section.CapturePerson,
                CaptureStaffRole = section.CaptureStaffRole,
                CaptureUrl = section.CaptureUrl,
                TitleLabel = section.TitleLabel,
                NarrativeLabel = section.NarrativeLabel,
                DeclarationStatus = declaration?.StatusCode ?? "",
                DeclarationLabel = declaration == null ? "Not recorded" : Label("missing_information_status", declaration.StatusCode),
                DeclarationExplanation = declaration?.Explanation,
                Entries = rows.Select(e =>
                {
                    var title = e.Title;
                    if (e.CatalogueItemId is Guid catalogueId && catalogueNames.TryGetValue(catalogueId, out var shared))
                        title = string.IsNullOrWhiteSpace(title) ? shared : shared + (string.IsNullOrWhiteSpace(title) ? "" : " — " + title);
                    var meta = new List<string>();
                    var primary = Label(section.LookupSetKey, e.LookupCode);
                    var secondary = Label(section.SecondaryLookupSetKey, e.SecondaryLookupCode);
                    if (!string.IsNullOrWhiteSpace(primary)) meta.Add(primary);
                    if (!string.IsNullOrWhiteSpace(secondary)) meta.Add(secondary);
                    if (e.StaffRoleId is int roleId && staffById.TryGetValue(roleId, out var roleName))
                        meta.Add(roleName);
                    if (!string.IsNullOrWhiteSpace(e.PersonName))
                        meta.Add(e.PersonName);
                    return new ServiceSchemaEntryRow
                    {
                        Id = e.Id,
                        Title = string.IsNullOrWhiteSpace(title) ? "Untitled" : title,
                        Narrative = e.Narrative,
                        Meta = meta.Count == 0 ? null : string.Join(" · ", meta),
                        Url = e.ExternalUrl,
                        VerificationLabel = Label("verification_status", e.VerificationStatusCode)
                    };
                }).ToList()
            });
        }

        return page;
    }
}
