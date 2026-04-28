using System.Linq;
using Compass.Data;
using Compass.Models;
using Compass.Models.Fips;
using Compass.Models.Modern.Work;
using Compass.ViewModels.Modern;
using Microsoft.EntityFrameworkCore;

namespace Compass.Services.Fips;

/// <summary>
/// Loads RAID risks/issues linked to a FIPS service register (CMDB) product via primary product id or legacy identifiers.
/// </summary>
public static class FipsProductRaidQuery
{
    /// <summary>
    /// Options for associating a risk with a product: same <see cref="CMDBProductStatus.Active"/> set as
    /// Manage → FIPS &quot;All&quot; tab, mapped to <see cref="FipsService.ServiceId"/> for
    /// <c>Risk.PrimaryProductId</c> (where a Service row can be resolved).
    /// </summary>
    public static async Task<List<RiskIssueNamedIntOption>> BuildActiveCmdbProductServiceSelectOptionsForRaidAsync(
        CompassDbContext db, CancellationToken cancellationToken = default)
    {
        var products = await db.CMDBProducts
            .AsNoTracking()
            .Where(p => p.Status == CMDBProductStatus.Active)
            .OrderBy(p => p.Title)
            .ToListAsync(cancellationToken);
        if (products.Count == 0)
            return new List<RiskIssueNamedIntOption>();

        var uids = products.Select(p => p.UniqueID).Where(x => x > 0).Distinct().ToList();
        var fipsKeys = products
            .Where(p => !string.IsNullOrWhiteSpace(p.CMDBID))
            .Select(p => p.CMDBID!.Trim())
            .Distinct()
            .ToList();
        var svcByServiceId = await db.Services.AsNoTracking()
            .Where(s => uids.Contains(s.ServiceId))
            .ToDictionaryAsync(s => s.ServiceId, s => s.ServiceId, cancellationToken);
        var svcByFips = fipsKeys.Count == 0
            ? new Dictionary<string, int>()
            : await db.Services.AsNoTracking()
                .Where(s => fipsKeys.Contains(s.FipsId))
                .ToDictionaryAsync(s => s.FipsId, s => s.ServiceId, cancellationToken);

        var list = new List<RiskIssueNamedIntOption>(products.Count);
        var usedIds = new HashSet<int>();
        foreach (var p in products)
        {
            int? sid = null;
            if (p.UniqueID > 0 && svcByServiceId.TryGetValue(p.UniqueID, out var a))
                sid = a;
            else if (!string.IsNullOrWhiteSpace(p.CMDBID) && svcByFips.TryGetValue(p.CMDBID.Trim(), out var b))
                sid = b;
            if (sid is { } id && id > 0 && usedIds.Add(id))
                list.Add(new RiskIssueNamedIntOption { Id = id, Name = p.Title });
        }
        return list;
    }

    /// <summary>
    /// Resolves FIPS <c>Service.ServiceId</c> for a CMDB product row (identity / CMDB code).
    /// </summary>
    public static async Task<int?> ResolvePrimaryProductServiceIdAsync(CompassDbContext db, CMDBProduct product, CancellationToken ct)
    {
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
    }
}
