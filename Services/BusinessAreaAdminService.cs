using Compass.Data;
using Microsoft.EntityFrameworkCore;

namespace Compass.Services;

public class BusinessAreaAdminService : IBusinessAreaAdminService
{
    private readonly CompassDbContext _db;

    public BusinessAreaAdminService(CompassDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task<bool> IsUserAdminForAnyBusinessAreaAsync(
        int userId,
        IReadOnlyCollection<int> businessAreaLookupIds,
        CancellationToken cancellationToken = default)
    {
        if (businessAreaLookupIds == null || businessAreaLookupIds.Count == 0)
            return false;

        var activeBaIds = await _db.BusinessAreaLookups.AsNoTracking()
            .Where(b => b.IsActive && businessAreaLookupIds.Contains(b.Id))
            .Select(b => b.Id)
            .ToListAsync(cancellationToken);
        if (activeBaIds.Count == 0)
            return false;

        return await _db.BusinessAreaAdminMembers.AsNoTracking()
            .AnyAsync(
                m => m.UserId == userId && activeBaIds.Contains(m.BusinessAreaLookupId),
                cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int?> GetLookupIdForBusinessAreaDisplayNameAsync(
        string? businessAreaDisplayName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(businessAreaDisplayName))
            return null;

        var t = businessAreaDisplayName.Trim();
        return await _db.BusinessAreaLookups.AsNoTracking()
            .Where(b => b.IsActive && b.Name.ToLower() == t.ToLower())
            .Select(b => (int?)b.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<int>> GetAdministeredBusinessAreaLookupIdsAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        return await _db.BusinessAreaAdminMembers.AsNoTracking()
            .Where(m => m.UserId == userId)
            .Join(
                _db.BusinessAreaLookups.AsNoTracking().Where(b => b.IsActive),
                m => m.BusinessAreaLookupId,
                b => b.Id,
                (_, b) => b.Id)
            .Distinct()
            .ToListAsync(cancellationToken);
    }
}
