using Compass.Data;
using Compass.Services;
using Microsoft.EntityFrameworkCore;

namespace Compass.Services.ServiceDataModels;

public sealed class ServiceDataModelAccessService : IServiceDataModelAccessService
{
    private readonly IPermissionService _permissions;

    public ServiceDataModelAccessService(IPermissionService permissions)
    {
        _permissions = permissions;
    }

    public Task<bool> CanManageModelsAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Task.FromResult(false);

        return _permissions.IsCentralOperationsAdminOrSuperAdminAsync(email.Trim());
    }

    public async Task<bool> CanAccessProductAsync(CompassDbContext db, string email, Guid productId)
    {
        if (string.IsNullOrWhiteSpace(email) || productId == Guid.Empty)
            return false;

        var normalized = email.Trim();
        if (await _permissions.IsOperationConsoleUserAsync(normalized))
            return true;

        var emailLower = normalized.ToLowerInvariant();
        return await db.CMDBProductContacts.AsNoTracking()
            .AnyAsync(c =>
                c.CMDBProductId == productId &&
                c.UserEmail != null &&
                c.UserEmail.Trim().ToLower() == emailLower);
    }

    public Task<bool> CanEditCensusAsync(CompassDbContext db, string email, Guid productId) =>
        CanAccessProductAsync(db, email, productId);

    public async Task<bool> CanReviewCensusAsync(CompassDbContext db, string email, Guid productId)
    {
        if (string.IsNullOrWhiteSpace(email) || productId == Guid.Empty)
            return false;

        var normalized = email.Trim();
        if (await _permissions.IsOperationConsoleUserAsync(normalized))
            return true;

        var emailLower = normalized.ToLowerInvariant();
        return await db.CMDBProductContacts.AsNoTracking()
            .AnyAsync(c =>
                c.CMDBProductId == productId &&
                c.CanManage &&
                c.UserEmail != null &&
                c.UserEmail.Trim().ToLower() == emailLower);
    }

    public async Task<IReadOnlyList<Guid>> FilterAccessibleProductIdsAsync(
        CompassDbContext db,
        string email,
        IEnumerable<Guid> productIds)
    {
        var ids = productIds.Where(id => id != Guid.Empty).Distinct().ToList();
        if (ids.Count == 0 || string.IsNullOrWhiteSpace(email))
            return Array.Empty<Guid>();

        var normalized = email.Trim();
        if (await _permissions.IsOperationConsoleUserAsync(normalized))
            return ids;

        var emailLower = normalized.ToLowerInvariant();
        var accessible = await db.CMDBProductContacts.AsNoTracking()
            .Where(c =>
                ids.Contains(c.CMDBProductId) &&
                c.UserEmail != null &&
                c.UserEmail.Trim().ToLower() == emailLower)
            .Select(c => c.CMDBProductId)
            .Distinct()
            .ToListAsync();

        return accessible;
    }
}
