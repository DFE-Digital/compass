using System.Linq;
using System.Security.Claims;
using Compass.Data;
using Compass.Models;
using Microsoft.EntityFrameworkCore;

namespace Compass.Services;

public sealed class GlobalFeatureToggleService : IGlobalFeatureToggleService
{
    private readonly CompassDbContext _db;

    public GlobalFeatureToggleService(CompassDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task<bool> IsFeatureEnabledForUserAsync(string featureCode, int? userId)
    {
        if (string.IsNullOrWhiteSpace(featureCode))
            return false;

        var normalized = featureCode.Trim();
        var row = await _db.Features.AsNoTracking()
            .FirstOrDefaultAsync(f => f.Code == normalized);

        if (row == null)
        {
            var def = ApplicationFeatureToggleDefinition.All
                .FirstOrDefault(d => string.Equals(d.Code, normalized, StringComparison.OrdinalIgnoreCase));
            if (def != null)
                return def.DefaultEnabled;
            return true;
        }

        return row.AccessMode switch
        {
            FeatureAccessMode.Off => false,
            FeatureAccessMode.OnForAll => true,
            FeatureAccessMode.OnForSome => userId is > 0
                && await IsOnForSomeAllowedAsync(row.Id, userId.Value),
            _ => false
        };
    }

    private async Task<bool> IsOnForSomeAllowedAsync(int featureId, int userId)
    {
        if (await _db.FeatureUserAllows.AsNoTracking()
                .AnyAsync(a => a.FeatureId == featureId && a.UserId == userId))
            return true;

        var userGroupIds = await _db.UserGroups.AsNoTracking()
            .Where(ug => ug.UserId == userId && ug.Group.IsActive)
            .Select(ug => ug.GroupId)
            .ToListAsync();
        if (userGroupIds.Count == 0)
            return false;

        return await _db.FeatureGroupAllows.AsNoTracking()
            .AnyAsync(f => f.FeatureId == featureId && userGroupIds.Contains(f.GroupId));
    }

    /// <inheritdoc />
    public async Task<bool> IsFeatureEnabledForPrincipalAsync(string featureCode, ClaimsPrincipal? user)
    {
        var id = await ResolveUserIdFromPrincipalAsync(user);
        return await IsFeatureEnabledForUserAsync(featureCode, id);
    }

    private async Task<int?> ResolveUserIdFromPrincipalAsync(ClaimsPrincipal? user)
    {
        if (user?.Identity?.IsAuthenticated != true)
            return null;

        var email = user.Identity?.Name
            ?? user.FindFirst(ClaimTypes.Email)?.Value
            ?? user.FindFirst("preferred_username")?.Value
            ?? user.FindFirst("email")?.Value;
        if (string.IsNullOrWhiteSpace(email))
            return null;
        email = email.Trim();
        return await _db.Users.AsNoTracking()
            .Where(u => u.Email.ToLower() == email.ToLower())
            .Select(u => (int?)u.Id)
            .FirstOrDefaultAsync();
    }
}
