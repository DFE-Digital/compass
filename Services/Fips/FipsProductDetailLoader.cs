using Compass.Data;
using Compass.Models.Fips;
using Microsoft.EntityFrameworkCore;

namespace Compass.Services.Fips;

/// <summary>Tab-aware product loading for the FIPS product detail page — avoids loading RAID, CMS, SAS, etc. on every tab.</summary>
public static class FipsProductDetailLoader
{
    public static bool NeedsRaid(string tab) =>
        tab is "risks" or "issues" or "assumptions" or "dependencies";

    public static bool NeedsStrategicAlignment(string tab) =>
        string.Equals(tab, "strategic-alignment", StringComparison.OrdinalIgnoreCase);

    public static bool NeedsExtendedContext(string tab) =>
        tab is "performance" or "assurance";

    public static bool NeedsWorkItems(string tab) =>
        string.Equals(tab, "work", StringComparison.OrdinalIgnoreCase);

    public static bool NeedsFullAiss(string tab) =>
        string.Equals(tab, "accessibility", StringComparison.OrdinalIgnoreCase);

    public static bool NeedsAissSummary(string tab) =>
        tab is "information" or "accessibility";

    public static async Task<CMDBProduct?> LoadProductAsync(
        CompassDbContext db,
        Guid id,
        string detailTab,
        CancellationToken ct)
    {
        var query = db.CMDBProducts.AsQueryable();

        query = query
            .Include(p => p.Phase)
            .Include(p => p.BusinessAreas).ThenInclude(ba => ba.FipsBusinessArea)
            .Include(p => p.Directorates).ThenInclude(d => d.FipsDirectorate).ThenInclude(fd => fd.DirectorateLookup)
            .Include(p => p.Channels).ThenInclude(c => c.FipsChannel)
            .Include(p => p.UserGroups).ThenInclude(ug => ug.FipsUserGroup)
            .Include(p => p.Types).ThenInclude(t => t.FipsType)
            .Include(p => p.CategorisationItems).ThenInclude(ci => ci.FipsCategorisationItem).ThenInclude(i => i.Group)
            .Include(p => p.Contacts).ThenInclude(c => c.FipsContactRole);

        if (NeedsStrategicAlignment(detailTab))
        {
            query = query
                .Include(p => p.Objectives).ThenInclude(o => o.Objective)
                .Include(p => p.Missions).ThenInclude(m => m.Mission)
                .Include(p => p.WorkItemTags).ThenInclude(t => t.WorkItemTagLookup)
                .Include(p => p.RiskAppetiteLookup);
        }

        return await query.FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    /// <summary>Count linked work items without calling the CMS for document id resolution.</summary>
    public static Task<int> CountWorkLinksAsync(CompassDbContext db, CMDBProduct product, CancellationToken ct)
    {
        var docId = product.Id.ToString("D");
        var fipsId = product.CMDBID?.Trim();

        return db.ProjectProducts.AsNoTracking()
            .Where(pp =>
                pp.ProductDocumentId == docId ||
                (fipsId != null && pp.ProductFipsId == fipsId))
            .CountAsync(ct);
    }
}
