using System.Linq;
using Compass.Data;
using Compass.Models;
using Compass.Models.Fips;
using Compass.Models.Modern.Work;
using Compass.ViewModels.Modern;
using Microsoft.EntityFrameworkCore;

namespace Compass.Services.Fips;

/// <summary>
/// Loads RAID risks/issues linked to a service register <see cref="FipsService"/> row (and CMDB product resolution for product detail pages).
/// </summary>
public static class FipsProductRaidQuery
{
    /// <summary>
    /// Options for associating a risk/issue with a product: active Service Register (<see cref="CMDBProduct"/> status Active),
    /// each mapped to a <see cref="FipsService"/> row for <c>PrimaryProductId</c>. Values are <see cref="FipsService.ServiceId"/>.
    /// </summary>
    public static async Task<List<RiskIssueNamedIntOption>> BuildActiveServiceRegisterSelectOptionsForRaidAsync(
        CompassDbContext db,
        CancellationToken cancellationToken = default)
    {
        var serviceIdsByFipsId = await EnsureActiveServiceRegisterServicesAsync(db, cancellationToken);
        if (serviceIdsByFipsId.Count == 0)
            return new List<RiskIssueNamedIntOption>();

        var products = await db.CMDBProducts.AsNoTracking()
            .Where(p => p.Status == CMDBProductStatus.Active)
            .OrderBy(p => p.Title)
            .ToListAsync(cancellationToken);

        var options = new List<RiskIssueNamedIntOption>(products.Count);
        var usedServiceIds = new HashSet<int>();
        foreach (var product in products)
        {
            var fipsId = ResolveServiceRegisterFipsId(product);
            if (string.IsNullOrEmpty(fipsId) ||
                !serviceIdsByFipsId.TryGetValue(fipsId, out var serviceId) ||
                !usedServiceIds.Add(serviceId))
            {
                continue;
            }

            var label = string.IsNullOrWhiteSpace(product.CMDBID)
                ? product.Title
                : $"{product.Title} ({product.CMDBID.Trim()})";
            options.Add(new RiskIssueNamedIntOption { Id = serviceId, Name = label });
        }

        return options;
    }

    /// <summary>Alias for <see cref="BuildActiveServiceRegisterSelectOptionsForRaidAsync"/>.</summary>
    public static Task<List<RiskIssueNamedIntOption>> BuildActiveCmdbProductServiceSelectOptionsForRaidAsync(
        CompassDbContext db,
        CancellationToken cancellationToken = default) =>
        BuildActiveServiceRegisterSelectOptionsForRaidAsync(db, cancellationToken);

    /// <summary>
    /// Ensures each active CMDB product has a matching <see cref="FipsService"/> row (by FIPS / CMDB id).
    /// Returns FipsId → ServiceId for active register products.
    /// </summary>
    private static async Task<Dictionary<string, int>> EnsureActiveServiceRegisterServicesAsync(
        CompassDbContext db,
        CancellationToken cancellationToken)
    {
        var products = await db.CMDBProducts
            .Where(p => p.Status == CMDBProductStatus.Active)
            .ToListAsync(cancellationToken);
        if (products.Count == 0)
            return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        var fipsIds = products
            .Select(ResolveServiceRegisterFipsId)
            .Where(id => !string.IsNullOrEmpty(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var existing = await db.Services
            .Where(s => fipsIds.Contains(s.FipsId))
            .ToListAsync(cancellationToken);
        var byFipsId = existing.ToDictionary(s => s.FipsId, StringComparer.OrdinalIgnoreCase);

        var changed = false;
        foreach (var product in products)
        {
            var fipsId = ResolveServiceRegisterFipsId(product);
            if (string.IsNullOrEmpty(fipsId))
                continue;

            if (byFipsId.TryGetValue(fipsId, out var service))
            {
                if (!service.IsActive)
                {
                    service.IsActive = true;
                    changed = true;
                }

                var title = product.Title?.Trim();
                if (!string.IsNullOrEmpty(title) &&
                    !string.Equals(service.DisplayName, title, StringComparison.Ordinal))
                {
                    service.DisplayName = title;
                    service.UpdatedUtc = DateTime.UtcNow;
                    changed = true;
                }
            }
            else
            {
                service = new FipsService
                {
                    FipsId = fipsId,
                    DisplayName = product.Title,
                    IsActive = true
                };
                db.Services.Add(service);
                byFipsId[fipsId] = service;
                changed = true;
            }
        }

        if (changed)
            await db.SaveChangesAsync(cancellationToken);

        var activeFipsIds = products
            .Select(ResolveServiceRegisterFipsId)
            .Where(id => !string.IsNullOrEmpty(id))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return byFipsId
            .Where(kv => activeFipsIds.Contains(kv.Key) && kv.Value.IsActive)
            .ToDictionary(kv => kv.Key, kv => kv.Value.ServiceId, StringComparer.OrdinalIgnoreCase);
    }

    private static string ResolveServiceRegisterFipsId(CMDBProduct product)
    {
        if (!string.IsNullOrWhiteSpace(product.CMDBID))
            return product.CMDBID.Trim();
        return product.UniqueID > 0 ? $"SR-{product.UniqueID}" : string.Empty;
    }

    /// <summary>
    /// Resolves FIPS <c>Service.ServiceId</c> for a CMDB product row (identity / CMDB code).
    /// </summary>
    public static async Task<int?> ResolvePrimaryProductServiceIdAsync(CompassDbContext db, CMDBProduct product, CancellationToken ct)
    {
        if (product.Status == CMDBProductStatus.Active)
        {
            var map = await EnsureActiveServiceRegisterServicesAsync(db, ct);
            var fipsId = ResolveServiceRegisterFipsId(product);
            if (!string.IsNullOrEmpty(fipsId) && map.TryGetValue(fipsId, out var ensuredId))
                return ensuredId;
        }

        if (product.UniqueID != 0)
        {
            var sid = await db.Services.AsNoTracking()
                .Where(s => s.ServiceId == product.UniqueID)
                .Select(s => (int?)s.ServiceId)
                .FirstOrDefaultAsync(ct);
            if (sid.HasValue)
                return sid.Value;
        }

        if (!string.IsNullOrWhiteSpace(product.CMDBID))
        {
            var cmdb = product.CMDBID.Trim();
            var byFips = await db.Services.AsNoTracking()
                .Where(s => s.FipsId == cmdb)
                .Select(s => (int?)s.ServiceId)
                .FirstOrDefaultAsync(ct);
            if (byFips.HasValue)
                return byFips.Value;
        }

        return null;
    }

    public static async Task PopulateRaidListsAsync(CompassDbContext db, FipsProductDetailViewModel vm, CMDBProduct product, CancellationToken ct)
    {
        var serviceId = await ResolvePrimaryProductServiceIdAsync(db, product, ct);
        vm.ResolvedFipsServiceId = serviceId;

        var pid = product.Id.ToString();
        var cmdb = product.CMDBID?.Trim();

        var riskRows = await db.Risks.AsNoTracking()
            .Where(r => !r.IsDeleted &&
                        (
                            (serviceId.HasValue && r.PrimaryProductId == serviceId.Value) ||
                            (!string.IsNullOrEmpty(r.ProductDocumentId) && r.ProductDocumentId == pid) ||
                            (!string.IsNullOrEmpty(cmdb) && r.FipsId == cmdb)
                        ))
            .Include(r => r.RiskBusinessAreas).ThenInclude(x => x.BusinessAreaLookup)
            .Include(r => r.RiskTier)
            .Include(r => r.RiskStatus)
            .Include(r => r.RiskPriority)
            .Include(r => r.Likelihood)
            .Include(r => r.ImpactLevel)
            .Include(r => r.Project).ThenInclude(p => p!.BusinessAreaLookup)
            .Include(r => r.PrimaryProduct)
            .Include(r => r.OwnerUser)
            .OrderByDescending(r => r.UpdatedAt)
            .ToListAsync(ct);

        vm.ProductRisks = riskRows.Select(r =>
        {
            var statusName = r.RiskStatus?.Label ?? r.Status ?? "";
            var closed = r.ClosedDate.HasValue ||
                         statusName.Contains("closed", StringComparison.OrdinalIgnoreCase);
            var rel = RaidRegisterTableFormatting.BuildRiskRelation(r);
            return new FipsProductRaidListItem
            {
                Id = r.Id,
                Title = r.Title,
                Status = statusName,
                Score = r.RiskScore,
                UpdatedAt = r.UpdatedAt,
                IsClosed = closed,
                BusinessAreaLabel = RaidRegisterTableFormatting.FormatRiskBusinessAreaLabels(r),
                RelationKind = rel.Kind,
                RelationProjectId = rel.ProjectId,
                RelationTarget = rel.Target,
                OwnerLabel = r.OwnerUser != null
                    ? (string.IsNullOrWhiteSpace(r.OwnerUser.Name) ? r.OwnerUser.Email : r.OwnerUser.Name)
                    : r.OwnerEmail,
                LikelihoodLabel = r.Likelihood?.Label,
                ImpactLabel = r.ImpactLevel?.Label,
                TierName = r.RiskTier?.Name
            };
        }).ToList();

        var issueRows = await db.Issues.AsNoTracking()
            .Where(i => !i.IsDeleted &&
                        (
                            (serviceId.HasValue && i.PrimaryProductId == serviceId.Value) ||
                            (!string.IsNullOrEmpty(i.ProductDocumentId) && i.ProductDocumentId == pid) ||
                            (!string.IsNullOrEmpty(cmdb) && i.FipsId == cmdb)
                        ))
            .Include(i => i.IssueBusinessAreas).ThenInclude(x => x.BusinessAreaLookup)
            .Include(i => i.StatusLookup)
            .Include(i => i.SeverityLookup)
            .Include(i => i.PriorityLookup)
            .Include(i => i.Project).ThenInclude(p => p!.BusinessAreaLookup)
            .Include(i => i.PrimaryProduct)
            .Include(i => i.OwnerUser)
            .OrderByDescending(i => i.UpdatedAt)
            .ToListAsync(ct);

        vm.ProductIssues = issueRows.Select(i =>
        {
            var statusName = i.StatusLookup?.Label ?? i.Status ?? "";
            var closed = i.ClosedDate.HasValue ||
                         statusName.Contains("closed", StringComparison.OrdinalIgnoreCase);
            var rel = RaidRegisterTableFormatting.BuildIssueRelation(i);
            return new FipsProductRaidListItem
            {
                Id = i.Id,
                Title = i.Title,
                Status = statusName,
                Score = null,
                UpdatedAt = i.UpdatedAt,
                IsClosed = closed,
                BusinessAreaLabel = RaidRegisterTableFormatting.FormatIssueBusinessAreaLabels(i),
                RelationKind = rel.Kind,
                RelationProjectId = rel.ProjectId,
                RelationTarget = rel.Target,
                OwnerLabel = i.OwnerUser != null
                    ? (string.IsNullOrWhiteSpace(i.OwnerUser.Name) ? i.OwnerUser.Email : i.OwnerUser.Name)
                    : null,
                PriorityLabel = i.PriorityLookup?.Label ?? i.Priority,
                SeverityName = i.SeverityLookup?.Label ?? i.Severity
            };
        }).ToList();

        if (serviceId.HasValue)
        {
            var assumptionRows = await db.Assumptions.AsNoTracking()
                .Where(a => !a.IsDeleted && a.PrimaryProductId == serviceId.Value)
                .Include(a => a.StatusLookup)
                .Include(a => a.CriticalityLookup)
                .Include(a => a.OwnerUser)
                .OrderByDescending(a => a.UpdatedAt)
                .ToListAsync(ct);

            vm.ProductAssumptions = assumptionRows.Select(a => new FipsProductAssumptionListItem
            {
                Id = a.Id,
                Description = a.Description,
                StatusLabel = a.StatusLookup?.Label,
                CriticalityLabel = a.CriticalityLookup?.Label,
                ReviewDate = a.ReviewDate,
                OwnerLabel = a.OwnerUser != null
                    ? (string.IsNullOrWhiteSpace(a.OwnerUser.Name) ? a.OwnerUser.Email : a.OwnerUser.Name)
                    : null
            }).ToList();
        }

        var riskIds = vm.ProductRisks.Select(r => r.Id).ToList();
        var issueIds = vm.ProductIssues.Select(i => i.Id).ToList();
        if (riskIds.Count > 0 || issueIds.Count > 0)
        {
            var deps = await db.Dependencies.AsNoTracking()
                .Include(d => d.LinkTypeLookup)
                .Where(d =>
                    (riskIds.Count > 0 &&
                     ((d.SourceEntityType == "Risk" && riskIds.Contains(d.SourceEntityId)) ||
                      (d.TargetEntityType == "Risk" && riskIds.Contains(d.TargetEntityId)))) ||
                    (issueIds.Count > 0 &&
                     ((d.SourceEntityType == "Issue" && issueIds.Contains(d.SourceEntityId)) ||
                      (d.TargetEntityType == "Issue" && issueIds.Contains(d.TargetEntityId)))))
                .OrderByDescending(d => d.UpdatedAt)
                .Take(100)
                .ToListAsync(ct);

            var riskTitles = riskIds.Count == 0
                ? new Dictionary<int, string>()
                : await db.Risks.AsNoTracking()
                    .Where(r => riskIds.Contains(r.Id))
                    .ToDictionaryAsync(r => r.Id, r => r.Title, ct);
            var issueTitles = issueIds.Count == 0
                ? new Dictionary<int, string>()
                : await db.Issues.AsNoTracking()
                    .Where(i => issueIds.Contains(i.Id))
                    .ToDictionaryAsync(i => i.Id, i => i.Title, ct);

            string EndpointLabel(string entityType, int id)
            {
                if (string.Equals(entityType, "Risk", StringComparison.OrdinalIgnoreCase) &&
                    riskTitles.TryGetValue(id, out var rt))
                    return rt;
                if (string.Equals(entityType, "Issue", StringComparison.OrdinalIgnoreCase) &&
                    issueTitles.TryGetValue(id, out var it))
                    return it;
                return $"{entityType} #{id}";
            }

            vm.ProductDependencies = deps.Select(d => new FipsProductDependencyListItem
            {
                Id = d.Id,
                SourceLabel = EndpointLabel(d.SourceEntityType, d.SourceEntityId),
                TargetLabel = EndpointLabel(d.TargetEntityType, d.TargetEntityId),
                LinkTypeLabel = d.LinkTypeLookup?.Label ?? d.DependencyType,
                Status = d.Status,
                UpdatedAt = d.UpdatedAt
            }).ToList();
        }
    }
}
