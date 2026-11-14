using Compass.Data;
using Compass.ViewModels.Modern;
using Microsoft.EntityFrameworkCore;

namespace Compass.Services.ServiceSchema;

/// <summary>Form choices taken from Admin lookups. RAID lists are not included.</summary>
public static class ServiceSchemaAdminLookups
{
    public sealed record Source(string Key, string Label, string AdminPanel);

    public sealed record Binding(string? PrimaryKey, string? PrimaryLabel, string? SecondaryKey, string? SecondaryLabel);

    public static readonly IReadOnlyList<Source> Sources =
    [
        new("activity-types", "Activity types", "activity-types"),
        new("business-areas", "Business areas", "business-areas"),
        new("universal-barriers", "Universal barriers", "universal-barriers"),
        new("phases", "Phases", "phases"),
        new("directorates", "Directorates", "directorates"),
        new("departments", "Government departments", "departments"),
        new("mission-pillars", "Mission pillars", "mission-pillars"),
        new("priorities", "Delivery priorities", "priorities"),
        new("resource-bands", "Resource bands", "resource-bands"),
        new("priority-outcomes", "Priority outcomes", "priority-outcomes"),
        new("rag-defns", "RAG definitions", "rag-defns"),
        new("work-tagging", "Thematic tags", "work-tagging"),
        new("fips-channels", "Channels", "fips-channels"),
        new("fips-types", "Types", "fips-types"),
        new("fips-user-groups", "User groups", "fips-user-groups"),
        new("fips-contact-roles", "Contact roles", "fips-contact-roles"),
        new("fips-categorisation", "Categorisation", "fips-categorisation")
    ];

    public static readonly IReadOnlyDictionary<string, Binding> BySection =
        new Dictionary<string, Binding>(StringComparer.OrdinalIgnoreCase)
        {
            ["outcomes"] = new("priority-outcomes", "Priority outcome", "mission-pillars", "Mission pillar"),
            ["benefits"] = new("mission-pillars", "Mission pillar", "priorities", "Delivery priority"),
            ["user-groups"] = new("fips-user-groups", "User group", "fips-channels", "Channel"),
            ["user-needs"] = new("fips-user-groups", "Who this need is for", "universal-barriers", "Universal barrier"),
            ["journeys"] = new("fips-channels", "Channel", "phases", "Phase"),
            ["fulfilment"] = new("rag-defns", "How well it is met", "universal-barriers", "Universal barrier"),
            ["responsibilities"] = new("fips-contact-roles", "Register role, if any", "directorates", "Directorate"),
            ["capabilities"] = new("activity-types", "Activity type", "fips-types", "Type"),
            ["features"] = new("fips-types", "Type", "work-tagging", "Thematic tag"),
            ["technology"] = new("resource-bands", "Resource band", "activity-types", "Activity type"),
            ["dependencies"] = new("departments", "Department", "business-areas", "Business area"),
            ["data"] = new("fips-types", "Type", "fips-categorisation", "Categorisation"),
            ["measures"] = new("priorities", "Delivery priority", "rag-defns", "RAG"),
            ["assurance"] = new("phases", "Phase", "rag-defns", "RAG"),
            ["risks"] = new("business-areas", "Business area", "rag-defns", "RAG"),
            ["findings"] = new("universal-barriers", "Universal barrier", "phases", "Phase"),
            ["opportunities"] = new("priority-outcomes", "Priority outcome", "activity-types", "Activity type"),
            ["resources"] = new("activity-types", "Activity type", "directorates", "Directorate"),
            ["delivery"] = new("business-areas", "Business area", "phases", "Phase"),
            ["support"] = new("fips-contact-roles", "Contact role", "directorates", "Directorate")
        };

    public static Binding BindingFor(string? sectionKey) =>
        sectionKey != null && BySection.TryGetValue(sectionKey, out var binding)
            ? binding
            : new Binding(null, null, null, null);

    public static async Task<Dictionary<string, List<ServiceSchemaChoice>>> LoadAsync(
        CompassDbContext db,
        IEnumerable<string?> keys,
        CancellationToken ct)
    {
        var result = new Dictionary<string, List<ServiceSchemaChoice>>(StringComparer.OrdinalIgnoreCase);
        foreach (var key in keys.Where(k => !string.IsNullOrWhiteSpace(k)).Select(k => k!).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var choices = await LoadOneAsync(db, key, ct);
            if (choices != null)
                result[key] = choices;
        }

        return result;
    }

    public static string? LabelFor(IReadOnlyDictionary<string, List<ServiceSchemaChoice>> lookups, string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return null;
        foreach (var list in lookups.Values)
        {
            var match = list.FirstOrDefault(c => string.Equals(c.Value, code, StringComparison.OrdinalIgnoreCase));
            if (match != null)
                return match.Label;
        }

        return null;
    }

    private static async Task<List<ServiceSchemaChoice>?> LoadOneAsync(CompassDbContext db, string key, CancellationToken ct)
    {
        switch (key)
        {
            case "activity-types":
                return await Named(db.ActivityTypeLookups.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.SortOrder).ThenBy(x => x.Name), key, ct);
            case "business-areas":
                return await Named(db.BusinessAreaLookups.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.SortOrder).ThenBy(x => x.Name), key, ct);
            case "universal-barriers":
                return await Named(db.UniversalBarrierLookups.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.SortOrder).ThenBy(x => x.Name), key, ct);
            case "phases":
                return await Named(db.PhaseLookups.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.SortOrder).ThenBy(x => x.Name), key, ct);
            case "directorates":
                return await Named(db.DirectorateLookups.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.SortOrder).ThenBy(x => x.Name), key, ct);
            case "priorities":
                return await Named(db.DeliveryPriorities.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.SortOrder).ThenBy(x => x.Name), key, ct);
            case "resource-bands":
                return await Named(db.ResourceBandLookups.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.SortOrder).ThenBy(x => x.Name), key, ct);
            case "rag-defns":
                return await Named(db.RagStatusLookups.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.SortOrder).ThenBy(x => x.Name), key, ct);
            case "work-tagging":
                return await Named(db.WorkItemTagLookups.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.SortOrder).ThenBy(x => x.Name), key, ct);
            case "departments":
                return await db.GovernmentDepartments.AsNoTracking()
                    .Where(d => !d.IsDeleted && d.ClosedAt == null)
                    .OrderBy(d => d.Title)
                    .Select(d => new ServiceSchemaChoice { Value = key + ":" + d.Id, Label = d.Title })
                    .ToListAsync(ct);
            case "mission-pillars":
                return await db.Missions.AsNoTracking()
                    .Where(m => !m.IsDeleted)
                    .OrderBy(m => m.Title)
                    .Select(m => new ServiceSchemaChoice { Value = key + ":" + m.Id, Label = m.Title })
                    .ToListAsync(ct);
            case "priority-outcomes":
                return await db.Objectives.AsNoTracking()
                    .Where(o => !o.IsDeleted)
                    .OrderBy(o => o.Title)
                    .Select(o => new ServiceSchemaChoice { Value = key + ":" + o.Id, Label = o.Title })
                    .ToListAsync(ct);
            case "fips-channels":
                return await FipsNamed(db.FipsChannels.AsNoTracking().Where(x => x.Active).OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name), key, ct);
            case "fips-types":
                return await FipsNamed(db.FipsTypes.AsNoTracking().Where(x => x.Active).OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name), key, ct);
            case "fips-user-groups":
                return await FipsNamed(
                    db.FipsUserGroups.AsNoTracking()
                        .Where(x => x.Active && x.ParentId == null)
                        .OrderBy(x => x.Name),
                    key, ct);
            case "fips-contact-roles":
                return await FipsNamed(db.FipsContactRoles.AsNoTracking().Where(x => x.Active).OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name), key, ct);
            case "fips-categorisation":
                return await db.FipsCategorisationItems.AsNoTracking()
                    .Where(i => i.Active && i.Group.Active)
                    .OrderBy(i => i.Group.Name).ThenBy(i => i.DisplayOrder).ThenBy(i => i.Name)
                    .Select(i => new ServiceSchemaChoice
                    {
                        Value = key + ":" + i.Id,
                        Label = i.Name,
                        Group = i.Group.Name
                    })
                    .ToListAsync(ct);
            default:
                return null;
        }
    }

    private static async Task<List<ServiceSchemaChoice>> Named<T>(IQueryable<T> query, string key, CancellationToken ct)
        where T : class
    {
        var rows = await query.Select(x => new { Id = EF.Property<int>(x, "Id"), Name = EF.Property<string>(x, "Name") }).ToListAsync(ct);
        return rows.Select(x => new ServiceSchemaChoice { Value = key + ":" + x.Id, Label = x.Name }).ToList();
    }

    private static async Task<List<ServiceSchemaChoice>> FipsNamed<T>(IQueryable<T> query, string key, CancellationToken ct)
        where T : class =>
        await Named(query, key, ct);
}
