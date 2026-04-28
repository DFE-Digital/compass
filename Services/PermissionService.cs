using Compass.Data;
using Compass.Models;
using Microsoft.EntityFrameworkCore;

namespace Compass.Services;

public class PermissionService : IPermissionService
{
    private readonly CompassDbContext _context;
    private readonly IGlobalFeatureToggleService _globalFeatures;
    private readonly ILogger<PermissionService> _logger;

    // Super Admin group name - users in this group have all permissions
    private const string SuperAdminGroupName = "Super admin";

    // Central Operations Admin group name - default group with all permissions
    private const string CentralOpsAdminGroupName = "Central Operations Admin";

    public PermissionService(
        CompassDbContext context,
        IGlobalFeatureToggleService globalFeatures,
        ILogger<PermissionService> logger)
    {
        _context = context;
        _globalFeatures = globalFeatures;
        _logger = logger;
    }

    public async Task<bool> IsSuperAdminAsync(string userEmail)
    {
        if (string.IsNullOrWhiteSpace(userEmail))
            return false;

        // Check if user is in the Super admin group
        return await IsInGroupAsync(userEmail, SuperAdminGroupName);
    }

    /// <inheritdoc />
    public async Task<bool> IsCentralOperationsAdminOrSuperAdminAsync(string userEmail)
    {
        if (string.IsNullOrWhiteSpace(userEmail))
            return false;
        if (await IsSuperAdminAsync(userEmail))
            return true;
        return await IsInGroupAsync(userEmail, CentralOpsAdminGroupName);
    }

    private const string AppAdminGroupName = "Admin";

    /// <inheritdoc />
    public async Task<bool> IsOperationConsoleUserAsync(string userEmail)
    {
        if (string.IsNullOrWhiteSpace(userEmail))
            return false;
        if (await IsSuperAdminAsync(userEmail))
            return true;
        if (await IsInGroupAsync(userEmail, CentralOpsAdminGroupName))
            return true;
        return await IsInGroupAsync(userEmail, AppAdminGroupName);
    }

    public async Task<bool> HasPermissionAsync(string userEmail, string featureCode, PermissionType permission)
    {
        if (string.IsNullOrWhiteSpace(userEmail) || string.IsNullOrWhiteSpace(featureCode))
            return false;

        var userForFeature = await GetOrCreateUserAsync(userEmail);
        if (userForFeature == null)
            return false;
        if (!await _globalFeatures.IsFeatureEnabledForUserAsync(featureCode, userForFeature.Id))
            return false;

        // Super admin has all permissions
        if (await IsSuperAdminAsync(userEmail))
            return true;

        var user = userForFeature;

        // Get user's groups
        var userGroups = await _context.UserGroups
            .Include(ug => ug.Group)
            .ThenInclude(g => g.GroupFeaturePermissions)
            .ThenInclude(gfp => gfp.Feature)
            .Where(ug => ug.UserId == user.Id && ug.Group.IsActive)
            .Select(ug => ug.Group)
            .ToListAsync();

        // Check if user is in Central Operations Admin group (has all permissions)
        if (userGroups.Any(g => g.Name.Equals(CentralOpsAdminGroupName, StringComparison.OrdinalIgnoreCase)))
            return true;

        // Check if any group has the required permission for the feature
        var hasPermission = userGroups
            .SelectMany(g => g.GroupFeaturePermissions)
            .Any(gfp => gfp.Feature.Code.Equals(featureCode, StringComparison.OrdinalIgnoreCase) &&
                       gfp.Feature.AccessMode != FeatureAccessMode.Off &&
                       gfp.Permission == permission);

        return hasPermission;
    }

    public async Task<bool> IsInGroupAsync(string userEmail, string groupName)
    {
        if (string.IsNullOrWhiteSpace(userEmail) || string.IsNullOrWhiteSpace(groupName))
            return false;

        var user = await GetOrCreateUserAsync(userEmail);
        if (user == null)
            return false;

        // Materialize the groups first, then do case-insensitive comparison in memory
        // This avoids EF Core translation issues with StringComparison.OrdinalIgnoreCase
        var userGroups = await _context.UserGroups
            .Include(ug => ug.Group)
            .Where(ug => ug.UserId == user.Id && ug.Group.IsActive)
            .Select(ug => ug.Group.Name)
            .ToListAsync();

        return userGroups.Any(g => string.Equals(g, groupName, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<List<string>> GetUserGroupsAsync(string userEmail)
    {
        if (string.IsNullOrWhiteSpace(userEmail))
            return new List<string>();

        var user = await GetOrCreateUserAsync(userEmail);
        if (user == null)
            return new List<string>();

        return await _context.UserGroups
            .Include(ug => ug.Group)
            .Where(ug => ug.UserId == user.Id && ug.Group.IsActive)
            .Select(ug => ug.Group.Name)
            .ToListAsync();
    }

    public async Task<List<PermissionType>> GetUserPermissionsForFeatureAsync(string userEmail, string featureCode)
    {
        if (string.IsNullOrWhiteSpace(userEmail) || string.IsNullOrWhiteSpace(featureCode))
            return new List<PermissionType>();

        var user = await GetOrCreateUserAsync(userEmail);
        if (user == null)
            return new List<PermissionType>();
        if (!await _globalFeatures.IsFeatureEnabledForUserAsync(featureCode, user.Id))
            return new List<PermissionType>();

        // Super admin has all permissions
        if (await IsSuperAdminAsync(userEmail))
            return Enum.GetValues<PermissionType>().ToList();

        // Get user's groups
        var userGroups = await _context.UserGroups
            .Include(ug => ug.Group)
            .ThenInclude(g => g.GroupFeaturePermissions)
            .ThenInclude(gfp => gfp.Feature)
            .Where(ug => ug.UserId == user.Id && ug.Group.IsActive)
            .Select(ug => ug.Group)
            .ToListAsync();

        // Check if user is in Central Operations Admin group (has all permissions)
        if (userGroups.Any(g => g.Name.Equals(CentralOpsAdminGroupName, StringComparison.OrdinalIgnoreCase)))
            return Enum.GetValues<PermissionType>().ToList();

        // Get all permissions for this feature across all user's groups
        var permissions = userGroups
            .SelectMany(g => g.GroupFeaturePermissions)
            .Where(gfp => gfp.Feature.Code.Equals(featureCode, StringComparison.OrdinalIgnoreCase) &&
                         gfp.Feature.AccessMode != FeatureAccessMode.Off)
            .Select(gfp => gfp.Permission)
            .Distinct()
            .ToList();

        return permissions;
    }

    public async Task<User?> GetOrCreateUserAsync(string email, string? name = null)
    {
        if (string.IsNullOrWhiteSpace(email))
            return null;

        var normalizedEmail = email.ToLowerInvariant().Trim();

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

        if (user == null)
        {
            // Create user if they don't exist
            user = new User
            {
                Email = normalizedEmail,
                Name = name ?? email.Split('@')[0].Replace(".", " "),
                Role = UserRole.Visitor,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created new user: {Email}", normalizedEmail);
        }

        return user;
    }
}

