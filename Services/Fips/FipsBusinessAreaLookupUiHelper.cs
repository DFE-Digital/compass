using Compass.Data;
using Compass.Models;
using Compass.Models.Fips;
using Microsoft.EntityFrameworkCore;

namespace Compass.Services.Fips;

/// <summary>
/// Loads admin <see cref="BusinessAreaLookup"/> rows for FIPS product edit checkboxes,
/// including inactive lookups still linked to the product (legacy/unmigrated matches by name).
/// </summary>
public static class FipsBusinessAreaLookupUiHelper
{
    public static async Task<List<BusinessAreaLookup>> LoadBusinessAreaLookupOptionsForEditAsync(
        CompassDbContext db,
        CMDBProduct product,
        CancellationToken cancellationToken = default)
    {
        var allLookups = await db.BusinessAreaLookups.AsNoTracking()
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        var lookupIdsOnProduct = new HashSet<int>();
        foreach (var link in product.BusinessAreas)
        {
            var fba = link.FipsBusinessArea;
            if (fba == null)
                continue;
            if (fba.BusinessAreaLookupId.HasValue)
                lookupIdsOnProduct.Add(fba.BusinessAreaLookupId.Value);
            else
            {
                var match = allLookups.FirstOrDefault(l =>
                    string.Equals(l.Name.Trim(), fba.Name.Trim(), StringComparison.OrdinalIgnoreCase));
                if (match != null)
                    lookupIdsOnProduct.Add(match.Id);
            }
        }

        var activeIds = allLookups.Where(l => l.IsActive).Select(l => l.Id).ToHashSet();
        var selectedInactiveIds = lookupIdsOnProduct.Where(id => !activeIds.Contains(id)).ToHashSet();

        return allLookups
            .Where(l => l.IsActive || selectedInactiveIds.Contains(l.Id))
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
            .ToList();
    }

    public static HashSet<int> GetSelectedBusinessAreaLookupIds(CMDBProduct product, List<BusinessAreaLookup> options)
    {
        var selected = new HashSet<int>();
        foreach (var link in product.BusinessAreas)
        {
            var fba = link.FipsBusinessArea;
            if (fba == null)
                continue;
            if (fba.BusinessAreaLookupId.HasValue)
            {
                if (options.Any(o => o.Id == fba.BusinessAreaLookupId.Value))
                    selected.Add(fba.BusinessAreaLookupId.Value);
            }
            else
            {
                var match = options.FirstOrDefault(o =>
                    string.Equals(o.Name.Trim(), fba.Name.Trim(), StringComparison.OrdinalIgnoreCase));
                if (match != null)
                    selected.Add(match.Id);
            }
        }

        return selected;
    }
}
