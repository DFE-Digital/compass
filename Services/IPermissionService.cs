using Compass.Models;

namespace Compass.Services;

public interface IPermissionService
{
    Task<bool> HasPermissionAsync(string userEmail, string featureCode, PermissionType permission);
    Task<bool> IsSuperAdminAsync(string userEmail);
    /// <summary>Super admin or Central Operations Admin — same gate as <c>RequireCentralOpsAdmin</c>.</summary>
    Task<bool> IsCentralOperationsAdminOrSuperAdminAsync(string userEmail);

    /// <summary>Super admin, Central Operations Admin, or Admin group — same gate as <c>RequireOperationConsoleUser</c>.</summary>
    Task<bool> IsOperationConsoleUserAsync(string userEmail);
    Task<bool> IsInGroupAsync(string userEmail, string groupName);
    Task<List<string>> GetUserGroupsAsync(string userEmail);
    Task<List<PermissionType>> GetUserPermissionsForFeatureAsync(string userEmail, string featureCode);
    Task<User?> GetOrCreateUserAsync(string email, string? name = null);
}

