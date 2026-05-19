using Compass.Attributes;
using Compass.Data;
using Compass.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Compass.Controllers.Api.V1.Admin;

/// <summary>
/// Read-only API for COMPASS admin configuration lookups (the value lists managed under
/// <c>/modern/admin</c> — business areas, RAID statuses, priorities, etc.).
///
/// <para>
/// Two endpoints are exposed:
/// <list type="bullet">
///   <item><c>GET /api/v1/admin/lookups</c> — high level catalog of every supported lookup type.</item>
///   <item><c>GET /api/v1/admin/lookups/{key}</c> — all items for a specific lookup type.</item>
/// </list>
/// </para>
/// </summary>
[ApiController]
[Route("api/v1/admin/lookups")]
public class LookupsController : ControllerBase
{
    private readonly CompassDbContext _db;

    public LookupsController(CompassDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    [RequireApiPermission("AdminLookups", "read")]
    public async Task<IActionResult> List()
    {
        var counts = await GetAllCountsAsync();

        var data = LookupCatalog.All
            .OrderBy(d => d.Category)
            .ThenBy(d => d.Label)
            .Select(d => new
            {
                key = d.Key,
                label = d.Label,
                category = d.Category,
                description = d.Description,
                itemCount = counts.TryGetValue(d.Key, out var c) ? c : 0,
                url = $"/api/v1/admin/lookups/{d.Key}"
            })
            .ToList();

        return Ok(new
        {
            totalRecords = data.Count,
            data
        });
    }

    [HttpGet("{key}")]
    [RequireApiPermission("AdminLookups", "read")]
    public async Task<IActionResult> Items(
        [FromRoute] string key,
        [FromQuery] bool includeInactive = false)
    {
        var def = LookupCatalog.Find(key);
        if (def == null)
        {
            return NotFound(new
            {
                error = new
                {
                    code = "NOT_FOUND",
                    message = $"Unknown lookup key '{key}'. Call GET /api/v1/admin/lookups to discover supported keys."
                }
            });
        }

        var items = await def.Fetch(_db, includeInactive);

        return Ok(new
        {
            key = def.Key,
            label = def.Label,
            category = def.Category,
            description = def.Description,
            includeInactive,
            totalRecords = items.Count,
            data = items
        });
    }

    private async Task<Dictionary<string, int>> GetAllCountsAsync()
    {
        var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var def in LookupCatalog.All)
        {
            result[def.Key] = await def.Count(_db);
        }

        return result;
    }
}

/// <summary>
/// Normalised lookup item returned to API callers. Type-specific extras live on <see cref="Extra"/>
/// so the core shape is consistent across every lookup.
/// </summary>
public class AdminLookupItemDto
{
    public int Id { get; set; }
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public Dictionary<string, object?>? Extra { get; set; }
}

/// <summary>
/// Registry of every admin lookup that can be surfaced via the API. Each entry pairs a stable
/// kebab-case <see cref="Key"/> with a fetcher that returns items in the normalised
/// <see cref="AdminLookupItemDto"/> shape.
/// </summary>
internal static class LookupCatalog
{
    internal sealed record Def(
        string Key,
        string Label,
        string Category,
        string Description,
        Func<CompassDbContext, Task<int>> Count,
        Func<CompassDbContext, bool, Task<List<AdminLookupItemDto>>> Fetch);

    public static IReadOnlyList<Def> All => _all;
    public static Def? Find(string? key) =>
        _all.FirstOrDefault(d => string.Equals(d.Key, key, StringComparison.OrdinalIgnoreCase));

    private static readonly IReadOnlyList<Def> _all = BuildCatalog();

    private static IReadOnlyList<Def> BuildCatalog()
    {
        var list = new List<Def>();

        // Core delivery taxonomy
        list.Add(Simple("business-areas", "Business areas", "core",
            "Business areas used to classify work items and map to divisions.",
            ctx => ctx.BusinessAreaLookups));
        list.Add(Simple("phases", "Delivery phases", "core",
            "Delivery phases assigned to work items (e.g. Discovery, Alpha, Beta, Live).",
            ctx => ctx.PhaseLookups));
        list.Add(Simple("directorates", "Directorates", "core",
            "Directorates that group portfolios and leadership reporting lines.",
            ctx => ctx.DirectorateLookups));
        list.Add(Simple("activity-types", "Activity types", "core",
            "Activity types used to classify work and projects.",
            ctx => ctx.ActivityTypeLookups));
        list.Add(Simple("work-tagging", "Work tags", "core",
            "Custom tags that can be applied to work items.",
            ctx => ctx.WorkItemTagLookups));

        // Priority and RAG
        list.Add(new Def(
            "priorities", "Priority levels", "core",
            "Delivery priority levels assigned to work items for triage and reporting.",
            ctx => ctx.DeliveryPriorities.CountAsync(),
            async (ctx, includeInactive) =>
            {
                var q = ctx.DeliveryPriorities.AsNoTracking().AsQueryable();
                if (!includeInactive) q = q.Where(x => x.IsActive);
                var rows = await q.OrderBy(x => x.SortOrder).ThenBy(x => x.Name).ToListAsync();
                return rows.Select(x => new AdminLookupItemDto
                {
                    Id = x.Id, Name = x.Name, Description = x.Description,
                    SortOrder = x.SortOrder, IsActive = x.IsActive,
                    Extra = new Dictionary<string, object?>
                    {
                        ["summary"] = x.Summary,
                        ["cssClass"] = x.CssClass
                    }
                }).ToList();
            }));

        list.Add(new Def(
            "rag-defns", "RAG definitions", "core",
            "Configurable Red, Amber and Green status labels used in monthly updates and dashboards.",
            ctx => ctx.RagStatusLookups.CountAsync(),
            async (ctx, includeInactive) =>
            {
                var q = ctx.RagStatusLookups.AsNoTracking().AsQueryable();
                if (!includeInactive) q = q.Where(x => x.IsActive);
                var rows = await q.OrderBy(x => x.SortOrder).ThenBy(x => x.Name).ToListAsync();
                return rows.Select(x => new AdminLookupItemDto
                {
                    Id = x.Id, Name = x.Name, Description = x.Description,
                    SortOrder = x.SortOrder, IsActive = x.IsActive,
                    Extra = new Dictionary<string, object?>
                    {
                        ["cssClass"] = x.CssClass
                    }
                }).ToList();
            }));

        // Universal barriers
        list.Add(new Def(
            "universal-barriers", "Universal barriers", "demand",
            "GOV.UK universal barriers used during demand exploration.",
            ctx => ctx.UniversalBarrierLookups.CountAsync(),
            async (ctx, includeInactive) =>
            {
                var q = ctx.UniversalBarrierLookups.AsNoTracking().AsQueryable();
                if (!includeInactive) q = q.Where(x => x.IsActive);
                var rows = await q.OrderBy(x => x.SortOrder).ThenBy(x => x.Name).ToListAsync();
                return rows.Select(x => new AdminLookupItemDto
                {
                    Id = x.Id, Name = x.Name, Description = x.Description,
                    SortOrder = x.SortOrder, IsActive = x.IsActive,
                    Extra = new Dictionary<string, object?>
                    {
                        ["guidanceUrl"] = x.GuidanceUrl
                    }
                }).ToList();
            }));

        // Missions & objectives
        list.Add(new Def(
            "mission-pillars", "Mission pillars", "strategy",
            "Strategic mission pillars used when aligning work and demand to missions.",
            ctx => ctx.Missions.CountAsync(m => !m.IsDeleted),
            async (ctx, includeInactive) =>
            {
                var q = ctx.Missions.AsNoTracking().AsQueryable();
                if (!includeInactive) q = q.Where(m => !m.IsDeleted);
                var rows = await q.OrderBy(m => m.Title).ToListAsync();
                return rows.Select(m => new AdminLookupItemDto
                {
                    Id = m.Id, Name = m.Title, Description = m.Description,
                    SortOrder = 0, IsActive = !m.IsDeleted,
                    Extra = new Dictionary<string, object?>
                    {
                        ["status"] = m.Status
                    }
                }).ToList();
            }));

        list.Add(new Def(
            "priority-outcomes", "Priority outcomes", "strategy",
            "Priority outcomes (objectives) used across work and demand.",
            ctx => ctx.Objectives.CountAsync(o => !o.IsDeleted),
            async (ctx, includeInactive) =>
            {
                var q = ctx.Objectives.AsNoTracking().AsQueryable();
                if (!includeInactive) q = q.Where(o => !o.IsDeleted);
                var rows = await q.OrderBy(o => o.Title).ToListAsync();
                return rows.Select(o => new AdminLookupItemDto
                {
                    Id = o.Id, Name = o.Title, Description = o.Description,
                    SortOrder = 0, IsActive = !o.IsDeleted,
                    Extra = new Dictionary<string, object?>
                    {
                        ["theme"] = o.Theme,
                        ["status"] = o.Status,
                        ["missionId"] = o.MissionId
                    }
                }).ToList();
            }));

        // Portfolios (organizational groups)
        list.Add(new Def(
            "portfolios", "Portfolios", "core",
            "Organizational groups / portfolios that nest work areas.",
            ctx => ctx.OrganizationalGroups.CountAsync(),
            async (ctx, includeInactive) =>
            {
                var q = ctx.OrganizationalGroups.AsNoTracking().AsQueryable();
                if (!includeInactive) q = q.Where(x => x.IsActive);
                var rows = await q.OrderBy(x => x.SortOrder).ThenBy(x => x.Name).ToListAsync();
                return rows.Select(x => new AdminLookupItemDto
                {
                    Id = x.Id, Name = x.Name, Description = x.Description,
                    SortOrder = x.SortOrder, IsActive = x.IsActive,
                    Extra = new Dictionary<string, object?>
                    {
                        ["parentGroupId"] = x.ParentGroupId
                    }
                }).ToList();
            }));

        // Government departments
        list.Add(new Def(
            "departments", "Government departments", "core",
            "Government departments used across reporting and FIPS linkage.",
            ctx => ctx.GovernmentDepartments.CountAsync(d => !d.IsDeleted),
            async (ctx, includeInactive) =>
            {
                var q = ctx.GovernmentDepartments.AsNoTracking().AsQueryable();
                if (!includeInactive) q = q.Where(d => !d.IsDeleted && d.ClosedAt == null);
                var rows = await q.OrderBy(d => d.Title).ToListAsync();
                return rows.Select(d => new AdminLookupItemDto
                {
                    Id = d.Id, Name = d.Title, Description = d.Description,
                    SortOrder = 0, IsActive = !d.IsDeleted && d.ClosedAt == null,
                    Extra = new Dictionary<string, object?>
                    {
                        ["abbreviation"] = d.Abbreviation,
                        ["format"] = d.Format,
                        ["webUrl"] = d.WebUrl,
                        ["govukStatus"] = d.GovukStatus,
                        ["parentDepartmentId"] = d.ParentDepartmentId
                    }
                }).ToList();
            }));

        // Risk tiers (Code, Name, Summary, GovernanceLevel, IsProposedTier)
        list.Add(new Def(
            "risk-tiers", "Risk tiers", "risk",
            "Governance tier lookup applied to risks (project, programme, portfolio, etc.).",
            ctx => ctx.RiskTiers.CountAsync(),
            async (ctx, includeInactive) =>
            {
                var q = ctx.RiskTiers.AsNoTracking().AsQueryable();
                if (!includeInactive) q = q.Where(x => x.IsActive);
                var rows = await q.OrderBy(x => x.SortOrder).ThenBy(x => x.Name).ToListAsync();
                return rows.Select(x => new AdminLookupItemDto
                {
                    Id = x.Id, Code = x.Code, Name = x.Name, Description = x.Description,
                    SortOrder = x.SortOrder, IsActive = x.IsActive,
                    Extra = new Dictionary<string, object?>
                    {
                        ["summary"] = x.Summary,
                        ["governanceLevel"] = x.GovernanceLevel,
                        ["isProposedTier"] = x.IsProposedTier
                    }
                }).ToList();
            }));

        // Risk appetites (Simple Name/Description shape)
        list.Add(Simple("risk-appetites", "Risk appetites", "risk",
            "Work-level risk appetite scale used across dashboards and detail views.",
            ctx => ctx.RiskAppetiteLookups));

        // Standards
        list.Add(Simple("std-categories", "Standard categories", "standards",
            "Top-level categories used to group standards.",
            ctx => ctx.StandardCategories));

        list.Add(new Def(
            "std-subcategories", "Standard sub-categories", "standards",
            "Sub-categories nested under each standard category.",
            ctx => ctx.StandardSubCategories.CountAsync(),
            async (ctx, includeInactive) =>
            {
                var q = ctx.StandardSubCategories.AsNoTracking().AsQueryable();
                if (!includeInactive) q = q.Where(x => x.IsActive);
                var rows = await q.OrderBy(x => x.SortOrder).ThenBy(x => x.Name).ToListAsync();
                return rows.Select(x => new AdminLookupItemDto
                {
                    Id = x.Id, Name = x.Name, Description = x.Description,
                    SortOrder = x.SortOrder, IsActive = x.IsActive,
                    Extra = new Dictionary<string, object?>
                    {
                        ["categoryId"] = x.CategoryId
                    }
                }).ToList();
            }));

        // RAID lookups — every entity that inherits RaidLookupBase.
        list.AddRange(new[]
        {
            Raid<RiskStatus>("risk-statuses", "Risk statuses", "risk", "Core risk workflow states."),
            Raid<RiskPriority>("risk-priorities", "Risk priorities", "risk", "Priority scale applied to risks."),
            Raid<RiskLikelihood>("risk-likelihoods", "Risk likelihoods", "risk", "Likelihood scale used to calculate scores.",
                e => e is RiskLikelihood rl ? new() { ["matrixScore"] = rl.MatrixScore } : null),
            Raid<RiskImpactLevel>("risk-impact-levels", "Risk impact levels", "risk", "Impact scale for risks.",
                e => e is RiskImpactLevel im ? new() { ["matrixScore"] = im.MatrixScore } : null),
            Raid<RiskProximity>("risk-proximities", "Risk proximities", "risk", "Timeline bands for when a risk may materialise."),
            Raid<RiskTreatment>("risk-treatments", "Risk treatments", "risk", "Primary treatment strategies for managing risks."),
            Raid<RiskCategory>("risk-categories", "Risk categories", "risk", "Categorisation for risk libraries."),

            Raid<IssueStatus>("issue-statuses", "Issue statuses", "issue", "Issue workflow states."),
            Raid<IssuePriority>("issue-priorities", "Issue priorities", "issue", "Priority options for issues."),
            Raid<IssueSeverity>("issue-severities", "Issue severities", "issue", "Severity scale mapped to RAID reporting."),
            Raid<IssueCategory>("issue-categories", "Issue categories", "issue", "Issue categorisation used in dashboards."),

            Raid<ActionStatus>("action-statuses", "Action statuses", "action", "Workflow states shown on every action."),
            Raid<ActionPriority>("action-priorities", "Action priorities", "action", "Priority options shared across action listings."),
            Raid<ActionType>("action-types", "Action types", "action", "Helps teams categorise actions for reporting."),
            Raid<ActionCategory>("action-categories", "Action categories", "action", "Used to slice actions by category."),
            Raid<ActionImpactLevel>("action-impact-levels", "Action impact levels", "action", "Impact level choices aligned with RAID reporting."),
            Raid<ActionReminderFrequency>("action-reminder-frequencies", "Action reminder frequencies", "action", "Determines how often reminders fire for actions."),
            Raid<ActionEscalationThreshold>("action-escalation-thresholds", "Action escalation thresholds", "action", "Number of days before escalation is triggered."),

            Raid<DecisionStatus>("decision-statuses", "Decision statuses", "decision", "Status values for decisions."),
            Raid<DecisionPriority>("decision-priorities", "Decision priorities", "decision", "Decision priority labels."),
            Raid<DecisionOutcome>("decision-outcomes", "Decision outcomes", "decision", "Possible outcomes recorded when a decision is made."),
            Raid<DecisionImplementationStatus>("decision-implementation-statuses", "Decision implementation statuses", "decision", "Tracks implementation progress."),

            Raid<RaidEvidenceType>("raid-evidence-types", "Evidence types", "raid", "Shared evidence/documentation types."),
            Raid<GovernanceBoard>("governance-boards", "Governance boards", "raid", "Committees and boards used for RAID escalation."),

            Raid<DemandRequestStatus>("demand-request-statuses", "Demand request statuses", "demand", "Workflow states for demand requests."),
            Raid<DemandTriageOutcomeStage>("triage-outcome-stages", "Triage outcome stages", "demand", "Stages recorded with demand triage outcomes."),

            Raid<AssumptionStatus>("assumption-statuses", "Assumption statuses", "assumption", "Lifecycle states for delivery assumptions."),
            Raid<AssumptionCriticality>("assumption-criticalities", "Assumption criticalities", "assumption", "How critical the assumption is if it fails."),

            Raid<DependencyCriticality>("dependency-criticalities", "Dependency criticalities", "dependency", "Criticality of dependency relationships."),
            Raid<DependencyLinkType>("dependency-link-types", "Dependency link types", "dependency", "Standard dependency classifications."),

            Raid<NearMissType>("near-miss-types", "Near miss types", "nearmiss", "Near miss or unexpected issue classification."),
            Raid<NearMissSeriousness>("near-miss-seriousness", "Near miss seriousness", "nearmiss", "Seriousness scale (1–4) for near misses."),
            Raid<NearMissStatus>("near-miss-statuses", "Near miss statuses", "nearmiss", "Open or closed workflow states for near misses.")
        });

        return list;
    }

    /// <summary>
    /// Helper for "simple" lookups exposing Id/Name/Description/SortOrder/IsActive only. Works
    /// for any entity that implements those properties via duck typing (Func selector).
    /// </summary>
    private static Def Simple<TEntity>(
        string key, string label, string category, string description,
        Func<CompassDbContext, DbSet<TEntity>> set)
        where TEntity : class
    {
        return new Def(
            key, label, category, description,
            ctx => set(ctx).CountAsync(),
            async (ctx, includeInactive) =>
            {
                var query = set(ctx).AsNoTracking().AsQueryable();
                // Use dynamic projection so this helper stays generic across many entity types
                // that share the standard shape but don't have a common interface.
                var rows = await query.ToListAsync();
                var items = rows
                    .Select(r => ProjectSimple(r))
                    .Where(item => item != null)
                    .Cast<AdminLookupItemDto>()
                    .Where(item => includeInactive || item.IsActive)
                    .OrderBy(item => item.SortOrder).ThenBy(item => item.Name)
                    .ToList();
                return items;
            });
    }

    private static AdminLookupItemDto? ProjectSimple(object entity)
    {
        var t = entity.GetType();
        int id = (int)(t.GetProperty("Id")?.GetValue(entity) ?? 0);
        string name = (string?)t.GetProperty("Name")?.GetValue(entity) ?? string.Empty;
        string? description = t.GetProperty("Description")?.GetValue(entity) as string;
        int sortOrder = (int)(t.GetProperty("SortOrder")?.GetValue(entity) ?? 0);
        bool isActive = (bool)(t.GetProperty("IsActive")?.GetValue(entity) ?? true);
        return new AdminLookupItemDto
        {
            Id = id, Name = name, Description = description,
            SortOrder = sortOrder, IsActive = isActive
        };
    }

    /// <summary>
    /// Builder for entries backed by <see cref="RaidLookupBase"/> (Code/Label/Description/SortOrder/IsActive).
    /// </summary>
    private static Def Raid<TEntity>(
        string key, string label, string category, string description,
        Func<RaidLookupBase, Dictionary<string, object?>?>? extra = null)
        where TEntity : RaidLookupBase
    {
        return new Def(
            key, label, category, description,
            ctx => ctx.Set<TEntity>().CountAsync(),
            async (ctx, includeInactive) =>
            {
                var q = ctx.Set<TEntity>().AsNoTracking().AsQueryable();
                if (!includeInactive) q = q.Where(x => x.IsActive);
                var rows = await q.OrderBy(x => x.SortOrder).ThenBy(x => x.Label).ToListAsync();
                return rows.Select(r => new AdminLookupItemDto
                {
                    Id = r.Id, Code = r.Code, Name = r.Label, Description = r.Description,
                    SortOrder = r.SortOrder, IsActive = r.IsActive,
                    Extra = extra?.Invoke(r)
                }).ToList();
            });
    }
}
