using Compass.Data;
using Compass.Models.Fips;
using Compass.ViewModels.Modern;
using Microsoft.EntityFrameworkCore;

namespace Compass.Services.ServiceDataModels;

/// <summary>
/// Resolves admin lookup panel keys (same keys as /modern/admin lookups) into
/// census choice options. Does not invent a second lookup system.
/// </summary>
public static class CensusAdminLookupOptions
{
    public const string FipsUserGroups = "fips-user-groups";
    public const string FipsChannels = "fips-channels";
    public const string FipsTypes = "fips-types";
    public const string FipsBusinessAreas = "fips-business-areas";
    public const string FipsContactRoles = "fips-contact-roles";
    public const string Capabilities = "capabilities";

    public static IReadOnlyList<(string Key, string DisplayName)> BindableLookups { get; } =
    [
        (FipsUserGroups, "FIPS user groups"),
        (Capabilities, "Capabilities"),
        (FipsChannels, "FIPS channels"),
        (FipsTypes, "FIPS types"),
        (FipsBusinessAreas, "FIPS business areas"),
        (FipsContactRoles, "FIPS contact roles"),
        ("business-areas", "Business areas"),
        ("phases", "Delivery phases"),
        ("directorates", "Directorates"),
        ("activity-types", "Activity types"),
        ("work-tagging", "Work tags"),
        ("resource-bands", "Resource bands"),
        ("rag-defns", "RAG statuses"),
        ("risk-appetites", "Risk appetites"),
        ("universal-barriers", "Universal barriers"),
        ("demand-request-statuses", "Demand request statuses"),
        ("triage-outcome-stages", "Triage outcome stages")
    ];

    public static bool IsKnown(string? key) =>
        !string.IsNullOrWhiteSpace(key) &&
        BindableLookups.Any(l => string.Equals(l.Key, key.Trim(), StringComparison.OrdinalIgnoreCase));

    public static async Task<IReadOnlyList<CensusLookupOption>> ResolveAsync(
        CompassDbContext db,
        string? lookupKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(lookupKey))
            return Array.Empty<CensusLookupOption>();

        var key = lookupKey.Trim().ToLowerInvariant();
        List<(string ValueKey, string Label)> rows = key switch
        {
            FipsUserGroups => await ResolveFipsUserGroupsAsync(db, cancellationToken),
            Capabilities => (await db.CapabilityLookups.AsNoTracking().Where(x => x.IsActive)
                    .OrderBy(x => x.SortOrder).ThenBy(x => x.Title)
                    .Select(x => new { x.Id, x.Title, x.Reference }).ToListAsync(cancellationToken))
                .Select(x => (x.Id.ToString(), $"{x.Title} ({x.Reference})")).ToList(),
            FipsChannels => (await db.FipsChannels.AsNoTracking().Where(x => x.Active)
                    .OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name)
                    .Select(x => new { x.Id, x.Name }).ToListAsync(cancellationToken))
                .Select(x => (x.Id.ToString(), x.Name)).ToList(),
            FipsTypes => (await db.FipsTypes.AsNoTracking().Where(x => x.Active)
                    .OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name)
                    .Select(x => new { x.Id, x.Name }).ToListAsync(cancellationToken))
                .Select(x => (x.Id.ToString(), x.Name)).ToList(),
            FipsBusinessAreas => (await db.FipsBusinessAreas.AsNoTracking().Where(x => x.Active)
                    .OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name)
                    .Select(x => new { x.Id, x.Name }).ToListAsync(cancellationToken))
                .Select(x => (x.Id.ToString(), x.Name)).ToList(),
            FipsContactRoles => (await db.FipsContactRoles.AsNoTracking().Where(x => x.Active)
                    .OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name)
                    .Select(x => new { x.Id, x.Name }).ToListAsync(cancellationToken))
                .Select(x => (x.Id.ToString(), x.Name)).ToList(),
            "business-areas" => (await db.BusinessAreaLookups.AsNoTracking().Where(x => x.IsActive)
                    .OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
                    .Select(x => new { x.Id, x.Name }).ToListAsync(cancellationToken))
                .Select(x => (x.Id.ToString(), x.Name)).ToList(),
            "phases" => (await db.PhaseLookups.AsNoTracking().Where(x => x.IsActive)
                    .OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
                    .Select(x => new { x.Id, x.Name }).ToListAsync(cancellationToken))
                .Select(x => (x.Id.ToString(), x.Name)).ToList(),
            "directorates" => (await db.DirectorateLookups.AsNoTracking().Where(x => x.IsActive)
                    .OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
                    .Select(x => new { x.Id, x.Name }).ToListAsync(cancellationToken))
                .Select(x => (x.Id.ToString(), x.Name)).ToList(),
            "activity-types" => (await db.ActivityTypeLookups.AsNoTracking().Where(x => x.IsActive)
                    .OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
                    .Select(x => new { x.Id, x.Name }).ToListAsync(cancellationToken))
                .Select(x => (x.Id.ToString(), x.Name)).ToList(),
            "work-tagging" => (await db.WorkItemTagLookups.AsNoTracking().Where(x => x.IsActive)
                    .OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
                    .Select(x => new { x.Id, x.Name }).ToListAsync(cancellationToken))
                .Select(x => (x.Id.ToString(), x.Name)).ToList(),
            "resource-bands" => (await db.ResourceBandLookups.AsNoTracking().Where(x => x.IsActive)
                    .OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
                    .Select(x => new { x.Id, x.Name }).ToListAsync(cancellationToken))
                .Select(x => (x.Id.ToString(), x.Name)).ToList(),
            "rag-defns" => (await db.RagStatusLookups.AsNoTracking().Where(x => x.IsActive)
                    .OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
                    .Select(x => new { x.Id, x.Name }).ToListAsync(cancellationToken))
                .Select(x => (x.Id.ToString(), x.Name)).ToList(),
            "risk-appetites" => (await db.RiskAppetiteLookups.AsNoTracking().Where(x => x.IsActive)
                    .OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
                    .Select(x => new { x.Id, x.Name }).ToListAsync(cancellationToken))
                .Select(x => (x.Id.ToString(), x.Name)).ToList(),
            "universal-barriers" => (await db.UniversalBarrierLookups.AsNoTracking().Where(x => x.IsActive)
                    .OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
                    .Select(x => new { x.Id, x.Name }).ToListAsync(cancellationToken))
                .Select(x => (x.Id.ToString(), x.Name)).ToList(),
            "demand-request-statuses" => (await db.DemandRequestStatuses.AsNoTracking().Where(x => x.IsActive)
                    .OrderBy(x => x.SortOrder).ThenBy(x => x.Label)
                    .Select(x => new { x.Id, x.Code, x.Label }).ToListAsync(cancellationToken))
                .Select(x => (
                    string.IsNullOrWhiteSpace(x.Code) ? x.Id.ToString() : x.Code,
                    string.IsNullOrWhiteSpace(x.Label) ? x.Code : x.Label)).ToList(),
            "triage-outcome-stages" => (await db.DemandTriageOutcomeStages.AsNoTracking().Where(x => x.IsActive)
                    .OrderBy(x => x.SortOrder).ThenBy(x => x.Label)
                    .Select(x => new { x.Id, x.Code, x.Label }).ToListAsync(cancellationToken))
                .Select(x => (
                    string.IsNullOrWhiteSpace(x.Code) ? x.Id.ToString() : x.Code,
                    string.IsNullOrWhiteSpace(x.Label) ? x.Code : x.Label)).ToList(),
            _ => []
        };

        return rows
            .Select((r, i) => new CensusLookupOption(r.ValueKey, r.Label, i + 1))
            .ToList();
    }

    private static async Task<List<(string ValueKey, string Label)>> ResolveFipsUserGroupsAsync(
        CompassDbContext db,
        CancellationToken cancellationToken)
    {
        // Match the admin FIPS user groups panel: every group in the hierarchy
        // (including inactive), not active-only roots. Filtering Active first drops
        // inactive parents and flattens / omits nodes the admin still sees.
        var all = await db.FipsUserGroups.AsNoTracking()
            .Include(x => x.Synonyms)
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return AdminFipsUserGroupTreeHelper.BuildFlatTree(all)
            .Select(row => (
                row.Id.ToString(),
                AdminFipsUserGroupTreeHelper.FormatIndentedLabel(row.Name, row.Depth)))
            .ToList();
    }
}

public sealed record CensusLookupOption(string ValueKey, string Label, int SortOrder);
