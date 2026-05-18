using Compass.Data;
using Microsoft.EntityFrameworkCore;

namespace Compass.Services;

public class DirectorateLeadershipService : IDirectorateLeadershipService
{
    private readonly CompassDbContext _db;

    public DirectorateLeadershipService(CompassDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task<bool> IsUserDirectorateLeaderForProjectContextAsync(
        int userId,
        IReadOnlyCollection<int> projectDivisionIds,
        IReadOnlyCollection<int> businessAreaLookupIds,
        CancellationToken cancellationToken = default)
    {
        var userDivisionIds = await _db.DivisionUsers.AsNoTracking()
            .Where(m => m.UserId == userId)
            .Join(
                _db.Divisions.AsNoTracking().Where(d => d.IsActive),
                m => m.DivisionId,
                d => d.Id,
                (_, d) => d.Id)
            .Distinct()
            .ToListAsync(cancellationToken);
        if (userDivisionIds.Count == 0)
            return false;

        var userSet = userDivisionIds.ToHashSet();

        if (projectDivisionIds is { Count: > 0 } && projectDivisionIds.Any(id => id > 0 && userSet.Contains(id)))
            return true;

        if (businessAreaLookupIds is not { Count: > 0 })
            return false;

        var bas = businessAreaLookupIds.Where(id => id > 0).Distinct().ToList();
        if (bas.Count == 0)
            return false;

        return await _db.DivisionBusinessAreas.AsNoTracking()
            .AnyAsync(
                dba => userSet.Contains(dba.DivisionId) && bas.Contains(dba.BusinessAreaLookupId),
                cancellationToken);
    }
}
