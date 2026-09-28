using Compass.Data;
using Compass.Models;
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

    /// <summary>
    /// Admin-maintained option lists selectable as census Lookup / choice sources.
    /// Sorted alphabetically by display name for the theme editor dropdown.
    /// </summary>
    public static IReadOnlyList<(string Key, string DisplayName)> BindableLookups { get; } =
        BuildBindableLookups();

    private static IReadOnlyList<(string Key, string DisplayName)> BuildBindableLookups() =>
        new (string Key, string DisplayName)[]
        {
            // FIPS + capabilities (admin panels outside the generic lookup API catalog)
            (FipsUserGroups, "FIPS user groups"),
            (Capabilities, "Capabilities"),
            (FipsChannels, "FIPS channels"),
            (FipsTypes, "FIPS types"),
            (FipsBusinessAreas, "FIPS business areas"),
            (FipsContactRoles, "FIPS contact roles"),

            // Lookups & taxonomy / Work (admin hub)
            ("activity-types", "Activity types"),
            ("business-areas", "Business areas"),
            ("departments", "Government departments"),
            ("phases", "Delivery phases"),
            ("directorates", "Directorates"),
            ("issue-categories", "Issue categories"),
            ("mission-pillars", "Mission pillars"),
            ("priorities", "Priority levels"),
            ("priority-outcomes", "Priority outcomes"),
            ("rag-defns", "RAG definitions"),
            ("resource-bands", "Resource bands"),
            ("universal-barriers", "Universal barriers"),
            ("work-tagging", "Thematic tags"),

            // Portfolios + standards
            ("portfolios", "Portfolios"),
            ("std-categories", "Standard categories"),
            ("std-subcategories", "Standard sub-categories"),

            // RAID and related option lists (same class as admin lookup panels)
            ("risk-tiers", "Risk tiers"),
            ("risk-appetites", "Risk appetites"),
            ("risk-statuses", "Risk statuses"),
            ("risk-priorities", "Risk priorities"),
            ("risk-likelihoods", "Risk likelihoods"),
            ("risk-impact-levels", "Risk impact levels"),
            ("risk-proximities", "Risk proximities"),
            ("risk-treatments", "Risk treatments"),
            ("risk-categories", "Risk categories"),
            ("issue-statuses", "Issue statuses"),
            ("issue-priorities", "Issue priorities"),
            ("issue-severities", "Issue severities"),
            ("action-statuses", "Action statuses"),
            ("action-priorities", "Action priorities"),
            ("action-types", "Action types"),
            ("action-categories", "Action categories"),
            ("action-impact-levels", "Action impact levels"),
            ("action-reminder-frequencies", "Action reminder frequencies"),
            ("action-escalation-thresholds", "Action escalation thresholds"),
            ("decision-statuses", "Decision statuses"),
            ("decision-priorities", "Decision priorities"),
            ("decision-outcomes", "Decision outcomes"),
            ("decision-implementation-statuses", "Decision implementation statuses"),
            ("raid-evidence-types", "Evidence types"),
            ("governance-boards", "Governance boards"),
            ("demand-request-statuses", "Demand request statuses"),
            ("triage-outcome-stages", "Triage outcome stages"),
            ("assumption-statuses", "Assumption statuses"),
            ("assumption-criticalities", "Assumption criticalities"),
            ("dependency-criticalities", "Dependency criticalities"),
            ("dependency-link-types", "Dependency link types"),
            ("near-miss-types", "Near miss types"),
            ("near-miss-seriousness", "Near miss seriousness"),
            ("near-miss-statuses", "Near miss statuses"),
        }
        .OrderBy(l => l.DisplayName, StringComparer.OrdinalIgnoreCase)
        .ToArray();

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
            "priorities" => (await db.DeliveryPriorities.AsNoTracking().Where(x => x.IsActive)
                    .OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
                    .Select(x => new { x.Id, x.Name }).ToListAsync(cancellationToken))
                .Select(x => (x.Id.ToString(), x.Name)).ToList(),
            "portfolios" => (await db.OrganizationalGroups.AsNoTracking().Where(x => x.IsActive)
                    .OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
                    .Select(x => new { x.Id, x.Name }).ToListAsync(cancellationToken))
                .Select(x => (x.Id.ToString(), x.Name)).ToList(),
            "std-categories" => (await db.StandardCategories.AsNoTracking().Where(x => x.IsActive)
                    .OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
                    .Select(x => new { x.Id, x.Name }).ToListAsync(cancellationToken))
                .Select(x => (x.Id.ToString(), x.Name)).ToList(),
            "std-subcategories" => (await db.StandardSubCategories.AsNoTracking().Where(x => x.IsActive)
                    .OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
                    .Select(x => new { x.Id, x.Name }).ToListAsync(cancellationToken))
                .Select(x => (x.Id.ToString(), x.Name)).ToList(),
            "risk-tiers" => (await db.RiskTiers.AsNoTracking().Where(x => x.IsActive)
                    .OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
                    .Select(x => new { x.Id, x.Name }).ToListAsync(cancellationToken))
                .Select(x => (x.Id.ToString(), x.Name)).ToList(),
            "mission-pillars" => (await db.Missions.AsNoTracking()
                    .Where(m => !m.IsDeleted)
                    .OrderBy(m => m.Title)
                    .Select(m => new { m.Id, m.Title }).ToListAsync(cancellationToken))
                .Select(m => (m.Id.ToString(), m.Title)).ToList(),
            "priority-outcomes" => (await db.Objectives.AsNoTracking()
                    .Where(o => !o.IsDeleted)
                    .OrderBy(o => o.Title)
                    .Select(o => new { o.Id, o.Title }).ToListAsync(cancellationToken))
                .Select(o => (o.Id.ToString(), o.Title)).ToList(),
            "departments" => (await db.GovernmentDepartments.AsNoTracking()
                    .Where(d => !d.IsDeleted && d.ClosedAt == null)
                    .OrderBy(d => d.Title)
                    .Select(d => new { d.Id, d.Title }).ToListAsync(cancellationToken))
                .Select(d => (d.Id.ToString(), d.Title)).ToList(),
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
            "risk-statuses" => await ResolveRaidAsync<RiskStatus>(db, cancellationToken),
            "risk-priorities" => await ResolveRaidAsync<RiskPriority>(db, cancellationToken),
            "risk-likelihoods" => await ResolveRaidAsync<RiskLikelihood>(db, cancellationToken),
            "risk-impact-levels" => await ResolveRaidAsync<RiskImpactLevel>(db, cancellationToken),
            "risk-proximities" => await ResolveRaidAsync<RiskProximity>(db, cancellationToken),
            "risk-treatments" => await ResolveRaidAsync<RiskTreatment>(db, cancellationToken),
            "risk-categories" => await ResolveRaidAsync<RiskCategory>(db, cancellationToken),
            "issue-statuses" => await ResolveRaidAsync<IssueStatus>(db, cancellationToken),
            "issue-priorities" => await ResolveRaidAsync<IssuePriority>(db, cancellationToken),
            "issue-severities" => await ResolveRaidAsync<IssueSeverity>(db, cancellationToken),
            "issue-categories" => await ResolveRaidAsync<IssueCategory>(db, cancellationToken),
            "action-statuses" => await ResolveRaidAsync<ActionStatus>(db, cancellationToken),
            "action-priorities" => await ResolveRaidAsync<ActionPriority>(db, cancellationToken),
            "action-types" => await ResolveRaidAsync<ActionType>(db, cancellationToken),
            "action-categories" => await ResolveRaidAsync<ActionCategory>(db, cancellationToken),
            "action-impact-levels" => await ResolveRaidAsync<ActionImpactLevel>(db, cancellationToken),
            "action-reminder-frequencies" => await ResolveRaidAsync<ActionReminderFrequency>(db, cancellationToken),
            "action-escalation-thresholds" => await ResolveRaidAsync<ActionEscalationThreshold>(db, cancellationToken),
            "decision-statuses" => await ResolveRaidAsync<DecisionStatus>(db, cancellationToken),
            "decision-priorities" => await ResolveRaidAsync<DecisionPriority>(db, cancellationToken),
            "decision-outcomes" => await ResolveRaidAsync<DecisionOutcome>(db, cancellationToken),
            "decision-implementation-statuses" => await ResolveRaidAsync<DecisionImplementationStatus>(db, cancellationToken),
            "raid-evidence-types" => await ResolveRaidAsync<RaidEvidenceType>(db, cancellationToken),
            "governance-boards" => await ResolveRaidAsync<GovernanceBoard>(db, cancellationToken),
            "assumption-statuses" => await ResolveRaidAsync<AssumptionStatus>(db, cancellationToken),
            "assumption-criticalities" => await ResolveRaidAsync<AssumptionCriticality>(db, cancellationToken),
            "dependency-criticalities" => await ResolveRaidAsync<DependencyCriticality>(db, cancellationToken),
            "dependency-link-types" => await ResolveRaidAsync<DependencyLinkType>(db, cancellationToken),
            "near-miss-types" => await ResolveRaidAsync<NearMissType>(db, cancellationToken),
            "near-miss-seriousness" => await ResolveRaidAsync<NearMissSeriousness>(db, cancellationToken),
            "near-miss-statuses" => await ResolveRaidAsync<NearMissStatus>(db, cancellationToken),
            _ => []
        };

        return rows
            .Select((r, i) => new CensusLookupOption(r.ValueKey, r.Label, i + 1))
            .ToList();
    }

    private static async Task<List<(string ValueKey, string Label)>> ResolveRaidAsync<T>(
        CompassDbContext db,
        CancellationToken cancellationToken)
        where T : RaidLookupBase
    {
        return (await db.Set<T>().AsNoTracking().Where(x => x.IsActive)
                .OrderBy(x => x.SortOrder).ThenBy(x => x.Label)
                .Select(x => new { x.Id, x.Label }).ToListAsync(cancellationToken))
            .Select(x => (x.Id.ToString(), x.Label)).ToList();
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
