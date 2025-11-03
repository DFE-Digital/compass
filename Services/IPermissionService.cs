using Compass.Models;

namespace Compass.Services;

public interface IPermissionService
{
    Task<bool> HasPermissionAsync(string userEmail, string featureCode, PermissionType permission);
    Task<bool> IsSuperAdminAsync(string userEmail);
    Task<bool> IsInGroupAsync(string userEmail, string groupName);
    Task<List<string>> GetUserGroupsAsync(string userEmail);
    Task<List<PermissionType>> GetUserPermissionsForFeatureAsync(string userEmail, string featureCode);
    Task<User?> GetOrCreateUserAsync(string email, string? name = null);
}

