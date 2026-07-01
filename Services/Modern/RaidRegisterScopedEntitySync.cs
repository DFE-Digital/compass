using Compass.Data;
using Compass.Models;
using Microsoft.EntityFrameworkCore;

namespace Compass.Services.Modern;

/// <summary>
/// Links risks, issues, assumptions, dependencies and near misses to a RAID register
/// when they belong to assigned work items or service register products (or register org scope).
/// </summary>
public static class RaidRegisterScopedEntitySync
{
    public static async Task SyncAsync(
        CompassDbContext db,
        int registerId,
        int? addedByUserId,
        CancellationToken cancellationToken = default)
    {
        var register = await db.RaidRegisters
            .Include(r => r.WorkItems)
            .Include(r => r.Services)
            .Include(r => r.Directorates)
            .Include(r => r.BusinessAreas)
            .FirstOrDefaultAsync(r => r.Id == registerId && !r.IsDeleted, cancellationToken);

        if (register == null)
            return;

        var projectIds = register.WorkItems.Select(w => w.ProjectId).Distinct().ToList();
        var serviceIds = register.Services.Select(s => s.FipsServiceId).Distinct().ToList();
        var directorateIds = RaidRegisterScopeHelper.GetDirectorateIds(register);
        var businessAreaIds = RaidRegisterScopeHelper.GetBusinessAreaIds(register);

        if (projectIds.Count > 0)
        {
            var projectBusinessAreaIds = await db.Projects.AsNoTracking()
                .Where(p => projectIds.Contains(p.Id) && p.BusinessAreaId != null)
                .Select(p => p.BusinessAreaId!.Value)
                .Distinct()
                .ToListAsync(cancellationToken);
            businessAreaIds = businessAreaIds.Union(projectBusinessAreaIds).Distinct().ToList();
        }

        if (projectIds.Count == 0 && serviceIds.Count == 0 &&
            directorateIds.Count == 0 && businessAreaIds.Count == 0)
        {
            return;
        }

        var now = DateTime.UtcNow;

        var riskIds = await QueryRiskIdsAsync(db, projectIds, serviceIds, cancellationToken);
        var issueIds = await QueryIssueIdsAsync(db, projectIds, serviceIds, cancellationToken);
        var assumptionIds = await QueryAssumptionIdsAsync(db, projectIds, serviceIds, cancellationToken);
        var dependencyIds = await QueryDependencyIdsAsync(db, projectIds, riskIds, issueIds, cancellationToken);
        var nearMissIds = await QueryNearMissIdsAsync(db, directorateIds, businessAreaIds, cancellationToken);

        await EnsureRiskLinksAsync(db, registerId, riskIds, addedByUserId, now, cancellationToken);
        await EnsureIssueLinksAsync(db, registerId, issueIds, addedByUserId, now, cancellationToken);
        await EnsureAssumptionLinksAsync(db, registerId, assumptionIds, addedByUserId, now, cancellationToken);
        await EnsureDependencyLinksAsync(db, registerId, dependencyIds, addedByUserId, now, cancellationToken);
        await EnsureNearMissLinksAsync(db, registerId, nearMissIds, addedByUserId, now, cancellationToken);

        if (db.ChangeTracker.HasChanges())
            await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task<List<int>> QueryRiskIdsAsync(
        CompassDbContext db,
        IReadOnlyList<int> projectIds,
        IReadOnlyList<int> serviceIds,
        CancellationToken ct)
    {
        var ids = new HashSet<int>();
        if (projectIds.Count > 0)
        {
            var fromProjects = await db.Risks.AsNoTracking()
                .Where(r => !r.IsDeleted && r.ProjectId != null && projectIds.Contains(r.ProjectId.Value))
                .Select(r => r.Id)
                .ToListAsync(ct);
            ids.UnionWith(fromProjects);
        }

        if (serviceIds.Count > 0)
        {
            var fromServices = await db.Risks.AsNoTracking()
                .Where(r => !r.IsDeleted && r.PrimaryProductId != null && serviceIds.Contains(r.PrimaryProductId.Value))
                .Select(r => r.Id)
                .ToListAsync(ct);
            ids.UnionWith(fromServices);
        }

        return ids.ToList();
    }

    private static async Task<List<int>> QueryIssueIdsAsync(
        CompassDbContext db,
        IReadOnlyList<int> projectIds,
        IReadOnlyList<int> serviceIds,
        CancellationToken ct)
    {
        var ids = new HashSet<int>();
        if (projectIds.Count > 0)
        {
            var fromProjects = await db.Issues.AsNoTracking()
                .Where(i => !i.IsDeleted && i.ProjectId != null && projectIds.Contains(i.ProjectId.Value))
                .Select(i => i.Id)
                .ToListAsync(ct);
            ids.UnionWith(fromProjects);
        }

        if (serviceIds.Count > 0)
        {
            var fromServices = await db.Issues.AsNoTracking()
                .Where(i => !i.IsDeleted && i.PrimaryProductId != null && serviceIds.Contains(i.PrimaryProductId.Value))
                .Select(i => i.Id)
                .ToListAsync(ct);
            ids.UnionWith(fromServices);
        }

        return ids.ToList();
    }

    private static async Task<List<int>> QueryAssumptionIdsAsync(
        CompassDbContext db,
        IReadOnlyList<int> projectIds,
        IReadOnlyList<int> serviceIds,
        CancellationToken ct)
    {
        var ids = new HashSet<int>();
        if (projectIds.Count > 0)
        {
            var fromProjects = await db.Assumptions.AsNoTracking()
                .Where(a => !a.IsDeleted && a.ProjectId != null && projectIds.Contains(a.ProjectId.Value))
                .Select(a => a.Id)
                .ToListAsync(ct);
            ids.UnionWith(fromProjects);
        }

        if (serviceIds.Count > 0)
        {
            var fromServices = await db.Assumptions.AsNoTracking()
                .Where(a => !a.IsDeleted && a.PrimaryProductId != null && serviceIds.Contains(a.PrimaryProductId.Value))
                .Select(a => a.Id)
                .ToListAsync(ct);
            ids.UnionWith(fromServices);
        }

        return ids.ToList();
    }

    private static async Task<List<int>> QueryDependencyIdsAsync(
        CompassDbContext db,
        IReadOnlyList<int> projectIds,
        IReadOnlyList<int> riskIds,
        IReadOnlyList<int> issueIds,
        CancellationToken ct)
    {
        if (projectIds.Count == 0 && riskIds.Count == 0 && issueIds.Count == 0)
            return new List<int>();

        var q = db.Dependencies.AsNoTracking().AsQueryable();
        var ids = new HashSet<int>();

        if (riskIds.Count > 0)
        {
            var fromRisks = await q.Where(d =>
                    (d.SourceEntityType == "Risk" && riskIds.Contains(d.SourceEntityId)) ||
                    (d.TargetEntityType == "Risk" && riskIds.Contains(d.TargetEntityId)))
                .Select(d => d.Id)
                .ToListAsync(ct);
            ids.UnionWith(fromRisks);
        }

        if (issueIds.Count > 0)
        {
            var fromIssues = await q.Where(d =>
                    (d.SourceEntityType == "Issue" && issueIds.Contains(d.SourceEntityId)) ||
                    (d.TargetEntityType == "Issue" && issueIds.Contains(d.TargetEntityId)))
                .Select(d => d.Id)
                .ToListAsync(ct);
            ids.UnionWith(fromIssues);
        }

        if (projectIds.Count > 0)
        {
            var fromProjects = await q.Where(d =>
                    (d.SourceEntityType == "Project" && projectIds.Contains(d.SourceEntityId)) ||
                    (d.TargetEntityType == "Project" && projectIds.Contains(d.TargetEntityId)))
                .Select(d => d.Id)
                .ToListAsync(ct);
            ids.UnionWith(fromProjects);
        }

        return ids.ToList();
    }

    private static async Task<List<int>> QueryNearMissIdsAsync(
        CompassDbContext db,
        IReadOnlyList<int> directorateIds,
        IReadOnlyList<int> businessAreaIds,
        CancellationToken ct)
    {
        if (directorateIds.Count == 0 && businessAreaIds.Count == 0)
            return new List<int>();

        var q = db.NearMisses.AsNoTracking().Where(n => !n.IsDeleted);
        var ids = new HashSet<int>();

        if (directorateIds.Count > 0)
        {
            var fromDir = await q.Where(n =>
                    n.DirectorateLookupId != null && directorateIds.Contains(n.DirectorateLookupId.Value))
                .Select(n => n.Id)
                .ToListAsync(ct);
            ids.UnionWith(fromDir);
        }

        if (businessAreaIds.Count > 0)
        {
            var fromBa = await q.Where(n =>
                    n.BusinessAreaLookupId != null && businessAreaIds.Contains(n.BusinessAreaLookupId.Value))
                .Select(n => n.Id)
                .ToListAsync(ct);
            ids.UnionWith(fromBa);
        }

        return ids.ToList();
    }

    private static async Task EnsureRiskLinksAsync(
        CompassDbContext db,
        int registerId,
        IReadOnlyList<int> riskIds,
        int? addedByUserId,
        DateTime addedAt,
        CancellationToken ct)
    {
        if (riskIds.Count == 0) return;

        var existing = await db.RaidRegisterRisks
            .Where(x => x.RaidRegisterId == registerId)
            .Select(x => x.RiskId)
            .ToListAsync(ct);
        var existingSet = existing.ToHashSet();

        foreach (var riskId in riskIds.Where(id => !existingSet.Contains(id)))
        {
            db.RaidRegisterRisks.Add(new RaidRegisterRisk
            {
                RaidRegisterId = registerId,
                RiskId = riskId,
                AddedAt = addedAt,
                AddedByUserId = addedByUserId
            });
        }
    }

    private static async Task EnsureIssueLinksAsync(
        CompassDbContext db,
        int registerId,
        IReadOnlyList<int> issueIds,
        int? addedByUserId,
        DateTime addedAt,
        CancellationToken ct)
    {
        if (issueIds.Count == 0) return;

        var existing = await db.RaidRegisterIssues
            .Where(x => x.RaidRegisterId == registerId)
            .Select(x => x.IssueId)
            .ToListAsync(ct);
        var existingSet = existing.ToHashSet();

        foreach (var issueId in issueIds.Where(id => !existingSet.Contains(id)))
        {
            db.RaidRegisterIssues.Add(new RaidRegisterIssue
            {
                RaidRegisterId = registerId,
                IssueId = issueId,
                AddedAt = addedAt,
                AddedByUserId = addedByUserId
            });
        }
    }

    private static async Task EnsureAssumptionLinksAsync(
        CompassDbContext db,
        int registerId,
        IReadOnlyList<int> assumptionIds,
        int? addedByUserId,
        DateTime addedAt,
        CancellationToken ct)
    {
        if (assumptionIds.Count == 0) return;

        var existing = await db.RaidRegisterAssumptions
            .Where(x => x.RaidRegisterId == registerId)
            .Select(x => x.AssumptionId)
            .ToListAsync(ct);
        var existingSet = existing.ToHashSet();

        foreach (var assumptionId in assumptionIds.Where(id => !existingSet.Contains(id)))
        {
            db.RaidRegisterAssumptions.Add(new RaidRegisterAssumption
            {
                RaidRegisterId = registerId,
                AssumptionId = assumptionId,
                AddedAt = addedAt,
                AddedByUserId = addedByUserId
            });
        }
    }

    private static async Task EnsureDependencyLinksAsync(
        CompassDbContext db,
        int registerId,
        IReadOnlyList<int> dependencyIds,
        int? addedByUserId,
        DateTime addedAt,
        CancellationToken ct)
    {
        if (dependencyIds.Count == 0) return;

        var existing = await db.RaidRegisterDependencies
            .Where(x => x.RaidRegisterId == registerId)
            .Select(x => x.DependencyId)
            .ToListAsync(ct);
        var existingSet = existing.ToHashSet();

        foreach (var dependencyId in dependencyIds.Where(id => !existingSet.Contains(id)))
        {
            db.RaidRegisterDependencies.Add(new RaidRegisterDependency
            {
                RaidRegisterId = registerId,
                DependencyId = dependencyId,
                AddedAt = addedAt,
                AddedByUserId = addedByUserId
            });
        }
    }

    private static async Task EnsureNearMissLinksAsync(
        CompassDbContext db,
        int registerId,
        IReadOnlyList<int> nearMissIds,
        int? addedByUserId,
        DateTime addedAt,
        CancellationToken ct)
    {
        if (nearMissIds.Count == 0) return;

        var existing = await db.RaidRegisterNearMisses
            .Where(x => x.RaidRegisterId == registerId)
            .Select(x => x.NearMissId)
            .ToListAsync(ct);
        var existingSet = existing.ToHashSet();

        foreach (var nearMissId in nearMissIds.Where(id => !existingSet.Contains(id)))
        {
            db.RaidRegisterNearMisses.Add(new RaidRegisterNearMiss
            {
                RaidRegisterId = registerId,
                NearMissId = nearMissId,
                AddedAt = addedAt,
                AddedByUserId = addedByUserId
            });
        }
    }
}
