using Compass.Data;
using Compass.Models;
using Compass.Models.Fips;
using Microsoft.EntityFrameworkCore;

namespace Compass.Services.Fips;

public sealed class FipsDirectorateLookupSyncService : IFipsDirectorateLookupSyncService
{
    private readonly CompassDbContext _db;

    public FipsDirectorateLookupSyncService(CompassDbContext db)
    {
        _db = db;
    }

    public async Task SyncFromDirectorateLookupsAsync(CancellationToken cancellationToken = default)
    {
        var lookups = await _db.DirectorateLookups.AsNoTracking()
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        var tracked = await _db.FipsDirectorates.ToListAsync(cancellationToken);
        var changed = false;

        foreach (var l in lookups)
        {
            var row = tracked.FirstOrDefault(f => f.DirectorateLookupId == l.Id)
                      ?? tracked.FirstOrDefault(f =>
                          f.DirectorateLookupId == null &&
                          string.Equals(f.Name.Trim(), l.Name.Trim(), StringComparison.OrdinalIgnoreCase));

            if (row == null)
            {
                _db.FipsDirectorates.Add(new FipsDirectorate
                {
                    DirectorateLookupId = l.Id,
                    Name = l.Name.Trim(),
                    Description = l.Description?.Trim(),
                    DisplayOrder = l.SortOrder,
                    Active = l.IsActive
                });
                changed = true;
            }
            else if (row.DirectorateLookupId != l.Id
                     || row.Name != l.Name.Trim()
                     || row.Description != l.Description?.Trim()
                     || row.DisplayOrder != l.SortOrder
                     || row.Active != l.IsActive)
            {
                row.DirectorateLookupId = l.Id;
                row.Name = l.Name.Trim();
                row.Description = l.Description?.Trim();
                row.DisplayOrder = l.SortOrder;
                row.Active = l.IsActive;
                changed = true;
            }
        }

        if (changed)
            await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<int[]> ResolveToFipsDirectorateIdsAsync(int[] directorateLookupIds,
        CancellationToken cancellationToken = default)
    {
        if (directorateLookupIds == null || directorateLookupIds.Length == 0)
            return Array.Empty<int>();

        var requested = directorateLookupIds.Distinct().ToArray();
        var idByLookup = await LookupFipsDirectorateIdsAsync(requested, cancellationToken);

        if (!requested.All(idByLookup.ContainsKey))
        {
            await SyncFromDirectorateLookupsAsync(cancellationToken);
            idByLookup = await LookupFipsDirectorateIdsAsync(requested, cancellationToken);
        }

        return directorateLookupIds
            .Where(lid => idByLookup.ContainsKey(lid))
            .Select(lid => idByLookup[lid])
            .Distinct()
            .ToArray();
    }

    private Task<Dictionary<int, int>> LookupFipsDirectorateIdsAsync(
        int[] requested,
        CancellationToken cancellationToken) =>
        _db.FipsDirectorates.AsNoTracking()
            .Where(f => f.DirectorateLookupId != null && requested.Contains(f.DirectorateLookupId.Value))
            .ToDictionaryAsync(f => f.DirectorateLookupId!.Value, f => f.Id, cancellationToken);
}
