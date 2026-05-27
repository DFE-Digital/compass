using Compass.Data;
using Compass.Models;
using Compass.Models.Fips;
using Compass.Services.Fips;
using Compass.Services.Modern;
using Compass.ViewModels.Modern;
using Microsoft.EntityFrameworkCore;

namespace Compass.Services;

/// <summary>RAID register coverage — work items and services not in any register scope.</summary>
public class ModernRaidRegisterCoverageReportService
{
    private readonly CompassDbContext _db;

    public ModernRaidRegisterCoverageReportService(CompassDbContext db) => _db = db;

    public async Task<ModernRaidRegisterCoverageReportViewModel> BuildAsync(
        CancellationToken cancellationToken = default)
    {
        var registerCount = await _db.RaidRegisters.AsNoTracking()
            .CountAsync(r => !r.IsDeleted, cancellationToken);

        var trackedProjectIds = await _db.RaidRegisterWorkItems.AsNoTracking()
            .Where(w => !w.RaidRegister.IsDeleted)
            .Select(w => w.ProjectId)
            .Distinct()
            .ToListAsync(cancellationToken);
        var trackedProjectIdSet = trackedProjectIds.ToHashSet();

        var trackedServiceIds = await _db.RaidRegisterServices.AsNoTracking()
            .Where(s => !s.RaidRegister.IsDeleted)
            .Select(s => s.FipsServiceId)
            .Distinct()
            .ToListAsync(cancellationToken);
        var trackedServiceIdSet = trackedServiceIds.ToHashSet();

        var workItems = await _db.Projects.AsNoTracking()
            .Where(p => !p.IsDeleted && p.Status != "Cancelled" && p.Status != "Completed")
            .Include(p => p.BusinessAreaLookup)
            .Include(p => p.PrimaryOrganizationalGroup)
            .OrderBy(p => p.Title)
            .ToListAsync(cancellationToken);

        var untrackedWork = workItems
            .Where(p => !trackedProjectIdSet.Contains(p.Id))
            .Select(p => new RaidRegisterCoverageWorkItemRow
            {
                Id = p.Id,
                Title = p.Title ?? $"Work item #{p.Id}",
                Status = p.Status ?? "—",
                BusinessAreaName = ModernWorkService.ResolveProjectBusinessAreaDisplayName(p),
                DetailUrl = $"/modern/work/detail/{p.Id}"
            })
            .ToList();

        var serviceOptions = await FipsProductRaidQuery.BuildActiveServiceRegisterSelectOptionsForRaidAsync(
            _db, cancellationToken);

        var productsByServiceId = await BuildActiveProductUrlsByServiceIdAsync(cancellationToken);

        var untrackedServices = serviceOptions
            .Where(o => !trackedServiceIdSet.Contains(o.Id))
            .Select(o =>
            {
                productsByServiceId.TryGetValue(o.Id, out var productUrl);
                return new RaidRegisterCoverageServiceRow
                {
                    ServiceId = o.Id,
                    Name = o.Name,
                    CmdbId = ExtractCmdbIdFromLabel(o.Name),
                    DetailUrl = productUrl
                };
            })
            .OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new ModernRaidRegisterCoverageReportViewModel
        {
            RegisterCount = registerCount,
            InScopeWorkItemCount = workItems.Count,
            UntrackedWorkItemCount = untrackedWork.Count,
            UntrackedWorkItems = untrackedWork,
            InScopeServiceCount = serviceOptions.Count,
            UntrackedServiceCount = untrackedServices.Count,
            UntrackedServices = untrackedServices
        };
    }

    private async Task<Dictionary<int, string>> BuildActiveProductUrlsByServiceIdAsync(
        CancellationToken cancellationToken)
    {
        var products = await _db.CMDBProducts.AsNoTracking()
            .Where(p => p.Status == CMDBProductStatus.Active)
            .ToListAsync(cancellationToken);
        if (products.Count == 0)
            return new Dictionary<int, string>();

        var fipsIds = products
            .Select(ResolveServiceRegisterFipsId)
            .Where(id => !string.IsNullOrEmpty(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var services = await _db.Services.AsNoTracking()
            .Where(s => s.IsActive && fipsIds.Contains(s.FipsId))
            .ToListAsync(cancellationToken);

        var serviceByFipsId = services.ToDictionary(s => s.FipsId, StringComparer.OrdinalIgnoreCase);
        var urls = new Dictionary<int, string>();

        foreach (var product in products)
        {
            var fipsId = ResolveServiceRegisterFipsId(product);
            if (string.IsNullOrEmpty(fipsId) ||
                !serviceByFipsId.TryGetValue(fipsId, out var service) ||
                urls.ContainsKey(service.ServiceId))
            {
                continue;
            }

            urls[service.ServiceId] = $"/modern/operations/service-register/product/{product.Id}";
        }

        return urls;
    }

    private static string ResolveServiceRegisterFipsId(CMDBProduct product)
    {
        if (!string.IsNullOrWhiteSpace(product.CMDBID))
            return product.CMDBID.Trim();
        return product.UniqueID > 0 ? $"SR-{product.UniqueID}" : string.Empty;
    }

    private static string? ExtractCmdbIdFromLabel(string name)
    {
        var open = name.LastIndexOf('(');
        var close = name.LastIndexOf(')');
        if (open < 0 || close <= open)
            return null;
        return name[(open + 1)..close].Trim();
    }
}
