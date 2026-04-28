using Compass.Data;
using Compass.Models;
using Compass.Models.Fips;
using Microsoft.EntityFrameworkCore;

namespace Compass.Services.Fips;

public sealed class FipsBusinessAreaLookupSyncService : IFipsBusinessAreaLookupSyncService
{
    private readonly CompassDbContext _db;

    public FipsBusinessAreaLookupSyncService(CompassDbContext db)
    {
        _db = db;
    }

    public async Task SyncFromBusinessAreaLookupsAsync(CancellationToken cancellationToken = default)
    {
        var lookups = await _db.BusinessAreaLookups.AsNoTracking()
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        var tracked = await _db.FipsBusinessAreas.ToListAsync(cancellationToken);

        foreach (var l in lookups)
        {
            var row = tracked.FirstOrDefault(f => f.BusinessAreaLookupId == l.Id)
                      ?? tracked.FirstOrDefault(f =>
                          f.BusinessAreaLookupId == null &&
                          string.Equals(f.Name.Trim(), l.Name.Trim(), StringComparison.OrdinalIgnoreCase));

            if (row == null)
            {
                _db.FipsBusinessAreas.Add(new FipsBusinessArea
                {
                    BusinessAreaLookupId = l.Id,
                    Name = l.Name.Trim(),
                    Description = l.Description?.Trim(),
                    DisplayOrder = l.SortOrder,
                    Active = l.IsActive
                });
            }
            else
            {
                row.BusinessAreaLookupId = l.Id;
                row.Name = l.Name.Trim();
                row.Description = l.Description?.Trim();
                row.DisplayOrder = l.SortOrder;
                row.Active = l.IsActive;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<int[]> ResolveToFipsBusinessAreaIdsAsync(int[] businessAreaLookupIds,
        CancellationToken cancellationToken = default)
    {
        if (businessAreaLookupIds == null || businessAreaLookupIds.Length == 0)
            return Array.Empty<int>();

        var requested = businessAreaLookupIds.Distinct().ToArray();
        await SyncFromBusinessAreaLookupsAsync(cancellationToken);

        var idByLookup = await _db.FipsBusinessAreas.AsNoTracking()
            .Where(f => f.BusinessAreaLookupId != null && requested.Contains(f.BusinessAreaLookupId.Value))
            .ToDictionaryAsync(f => f.BusinessAreaLookupId!.Value, f => f.Id, cancellationToken);

        return businessAreaLookupIds
            .Where(lid => idByLookup.ContainsKey(lid))
            .Select(lid => idByLookup[lid])
            .Distinct()
            .ToArray();
    }
}
