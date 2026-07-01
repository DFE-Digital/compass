using Compass.Data;
using Compass.Models;
using Compass.Models.Fips;
using Microsoft.EntityFrameworkCore;

namespace Compass.Services.Fips;

/// <summary>
/// Loads admin <see cref="DirectorateLookup"/> rows for FIPS product edit checkboxes.
/// </summary>
public static class FipsDirectorateLookupUiHelper
{
    public static async Task<List<DirectorateLookup>> LoadDirectorateLookupOptionsForEditAsync(
        CompassDbContext db,
        CMDBProduct product,
        CancellationToken cancellationToken = default)
    {
        var allLookups = await db.DirectorateLookups.AsNoTracking()
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        var lookupIdsOnProduct = new HashSet<int>();
        foreach (var link in product.Directorates)
        {
            var fd = link.FipsDirectorate;
            if (fd == null)
                continue;
            if (fd.DirectorateLookupId.HasValue)
                lookupIdsOnProduct.Add(fd.DirectorateLookupId.Value);
            else
            {
                var match = allLookups.FirstOrDefault(l =>
                    string.Equals(l.Name.Trim(), fd.Name.Trim(), StringComparison.OrdinalIgnoreCase));
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

    public static HashSet<int> GetSelectedDirectorateLookupIds(CMDBProduct product, List<DirectorateLookup> options)
    {
        var selected = new HashSet<int>();
        foreach (var link in product.Directorates)
        {
            var fd = link.FipsDirectorate;
            if (fd == null)
                continue;
            if (fd.DirectorateLookupId.HasValue)
            {
                if (options.Any(o => o.Id == fd.DirectorateLookupId.Value))
                    selected.Add(fd.DirectorateLookupId.Value);
            }
            else
            {
                var match = options.FirstOrDefault(o =>
                    string.Equals(o.Name.Trim(), fd.Name.Trim(), StringComparison.OrdinalIgnoreCase));
                if (match != null)
                    selected.Add(match.Id);
            }
        }

        return selected;
    }
}
