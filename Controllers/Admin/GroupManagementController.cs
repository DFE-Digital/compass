using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Compass.Data;
using Compass.Models;
using Compass.Services;
using System.Security.Claims;

namespace Compass.Controllers.Admin
{
    [Route("Admin/UserManagement")]
    [Authorize]
    public class GroupManagementController : Controller
    {
        private readonly CompassDbContext _context;
        private readonly ILogger<GroupManagementController> _logger;
        private readonly IPermissionService _permissionService;
        private readonly IUserDirectoryService _userDirectoryService;

        public GroupManagementController(
            CompassDbContext context,
            ILogger<GroupManagementController> logger,
            IPermissionService permissionService,
            IUserDirectoryService userDirectoryService)
        {
            _context = context;
            _logger = logger;
            _permissionService = permissionService;
            _userDirectoryService = userDirectoryService;
        }

        private string GetUserEmail()
        {
            return User.Identity?.Name 
                ?? User.FindFirst(ClaimTypes.Email)?.Value 
                ?? User.FindFirst("preferred_username")?.Value
                ?? User.FindFirst("email")?.Value
                ?? string.Empty;
        }

        private async Task<bool> IsSuperAdminOrAdminAsync()
        {
            var userEmail = GetUserEmail();
            if (string.IsNullOrEmpty(userEmail))
                return false;

            return await _permissionService.IsSuperAdminAsync(userEmail) ||
                   await _permissionService.IsInGroupAsync(userEmail, "Central Operations Admin");
        }

        // GET: Admin/GroupManagement
        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index(string? tab = "users")
        {
            if (!await IsSuperAdminOrAdminAsync())
            {
                return Forbid();
            }

            var groups = await _context.Groups
                .Include(g => g.UserGroups)
                .ThenInclude(ug => ug.User)
                .Include(g => g.GroupFeaturePermissions)
                .ThenInclude(gfp => gfp.Feature)
                .OrderBy(g => g.Name)
                .ToListAsync();

            var users = await _context.Users
                .Include(u => u.UserGroups)
                .ThenInclude(ug => ug.Group)
                .OrderBy(u => u.Name)
                .ToListAsync();

            var features = await _context.Features
                .OrderBy(f => f.Name)
                .ToListAsync();

            ViewBag.Users = users;
            ViewBag.Groups = groups;
            ViewBag.Features = features;
            ViewBag.ActiveTab = tab ?? "users";

            return View("~/Views/Admin/GroupManagement/Index.cshtml", groups);
        }

        // GET: Admin/GroupManagement/Groups
        [HttpGet("Groups")]
        public async Task<IActionResult> Groups()
        {
            if (!await IsSuperAdminOrAdminAsync())
            {
                return Forbid();
            }

            var groups = await _context.Groups
                .Include(g => g.UserGroups)
                .ThenInclude(ug => ug.User)
                .Include(g => g.GroupFeaturePermissions)
                .ThenInclude(gfp => gfp.Feature)
                .OrderBy(g => g.Name)
                .ToListAsync();

            return View("~/Views/Admin/GroupManagement/Groups.cshtml", groups);
        }

        // GET: Admin/GroupManagement/CreateGroup
        [HttpGet("CreateGroup")]
        public async Task<IActionResult> CreateGroup()
        {
            if (!await IsSuperAdminOrAdminAsync())
            {
                return Forbid();
            }

            ViewBag.Features = await _context.Features
                .Where(f => f.IsActive)
                .OrderBy(f => f.Name)
                .ToListAsync();

            return View("~/Views/Admin/GroupManagement/CreateGroup.cshtml");
        }

        // POST: Admin/GroupManagement/CreateGroup
        [HttpPost("CreateGroup")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateGroup(Group group, int[] featureIds, PermissionType[] permissions)
        {
            if (!await IsSuperAdminOrAdminAsync())
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var userEmail = GetUserEmail();
                    group.CreatedBy = userEmail;
                    group.CreatedAt = DateTime.UtcNow;
                    group.UpdatedBy = userEmail;
                    group.UpdatedAt = DateTime.UtcNow;

                    _context.Groups.Add(group);
                    await _context.SaveChangesAsync();

                    // Add feature permissions
                    if (featureIds != null && permissions != null && featureIds.Length == permissions.Length)
                    {
                        for (int i = 0; i < featureIds.Length; i++)
                        {
                            var featureId = featureIds[i];
                            var permission = permissions[i];

                            // Check if this permission already exists
                            var exists = await _context.GroupFeaturePermissions
                                .AnyAsync(gfp => gfp.GroupId == group.Id && 
                                               gfp.FeatureId == featureId && 
                                               gfp.Permission == permission);

                            if (!exists)
                            {
                                var groupFeaturePermission = new GroupFeaturePermission
                                {
                                    GroupId = group.Id,
                                    FeatureId = featureId,
                                    Permission = permission,
                                    CreatedAt = DateTime.UtcNow,
                                    CreatedBy = userEmail
                                };

                                _context.GroupFeaturePermissions.Add(groupFeaturePermission);
                            }
                        }

                        await _context.SaveChangesAsync();
                    }

                    _logger.LogInformation("Group created: {GroupName} by {User}", group.Name, userEmail);
                    return RedirectToAction(nameof(Groups));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating group");
                    ModelState.AddModelError("", "An error occurred while creating the group. Please try again.");
                }
            }

            ViewBag.Features = await _context.Features
                .Where(f => f.IsActive)
                .OrderBy(f => f.Name)
                .ToListAsync();

            return View("~/Views/Admin/GroupManagement/CreateGroup.cshtml", group);
        }

        // GET: Admin/GroupManagement/EditGroup/5
        [HttpGet("EditGroup/{id:int}")]
        public async Task<IActionResult> EditGroup(int? id)
        {
            if (!await IsSuperAdminOrAdminAsync())
            {
                return Forbid();
            }

            if (id == null)
            {
                return NotFound();
            }

            var group = await _context.Groups
                .Include(g => g.GroupFeaturePermissions)
                .ThenInclude(gfp => gfp.Feature)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (group == null)
            {
                return NotFound();
            }

            ViewBag.Features = await _context.Features
                .Where(f => f.IsActive)
                .OrderBy(f => f.Name)
                .ToListAsync();

            return View("~/Views/Admin/GroupManagement/EditGroup.cshtml", group);
        }

        // POST: Admin/GroupManagement/EditGroup/5
        [HttpPost("EditGroup/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditGroup(int id, Group group, int[] featureIds, PermissionType[] permissions)
        {
            if (!await IsSuperAdminOrAdminAsync())
            {
                return Forbid();
            }

            if (id != group.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingGroup = await _context.Groups.FindAsync(id);
                    if (existingGroup == null)
                    {
                        return NotFound();
                    }

                    // Don't allow editing system groups (except name/description)
                    if (existingGroup.IsSystemGroup && group.IsSystemGroup == false)
                    {
                        ModelState.AddModelError("", "Cannot change system group status.");
                        ViewBag.Features = await _context.Features
                            .Where(f => f.IsActive)
                            .OrderBy(f => f.Name)
                            .ToListAsync();
                        return View("~/Views/Admin/GroupManagement/EditGroup.cshtml", group);
                    }

                    var userEmail = GetUserEmail();
                    existingGroup.Name = group.Name;
                    existingGroup.Description = group.Description;
                    existingGroup.IsActive = group.IsActive;
                    existingGroup.UpdatedBy = userEmail;
                    existingGroup.UpdatedAt = DateTime.UtcNow;

                    // Remove existing permissions
                    var existingPermissions = await _context.GroupFeaturePermissions
                        .Where(gfp => gfp.GroupId == id)
                        .ToListAsync();
                    _context.GroupFeaturePermissions.RemoveRange(existingPermissions);

                    // Add new permissions
                    if (featureIds != null && permissions != null && featureIds.Length == permissions.Length)
                    {
                        for (int i = 0; i < featureIds.Length; i++)
                        {
                            var featureId = featureIds[i];
                            var permission = permissions[i];

                            var groupFeaturePermission = new GroupFeaturePermission
                            {
                                GroupId = id,
                                FeatureId = featureId,
                                Permission = permission,
                                CreatedAt = DateTime.UtcNow,
                                CreatedBy = userEmail
                            };

                            _context.GroupFeaturePermissions.Add(groupFeaturePermission);
                        }
                    }

                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Group updated: {GroupName} by {User}", existingGroup.Name, userEmail);
                    return RedirectToAction(nameof(Groups));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating group");
                    ModelState.AddModelError("", "An error occurred while updating the group. Please try again.");
                }
            }

            ViewBag.Features = await _context.Features
                .Where(f => f.IsActive)
                .OrderBy(f => f.Name)
                .ToListAsync();

            return View("~/Views/Admin/GroupManagement/EditGroup.cshtml", group);
        }

        // GET: Admin/GroupManagement/DeleteGroup/5
        [HttpGet("DeleteGroup/{id:int}")]
        public async Task<IActionResult> DeleteGroup(int? id)
        {
            if (!await IsSuperAdminOrAdminAsync())
            {
                return Forbid();
            }

            if (id == null)
            {
                return NotFound();
            }

            var group = await _context.Groups
                .Include(g => g.UserGroups)
                .ThenInclude(ug => ug.User)
                .Include(g => g.GroupFeaturePermissions)
                .ThenInclude(gfp => gfp.Feature)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (group == null)
            {
                return NotFound();
            }

            // Don't allow deleting system groups
            if (group.IsSystemGroup)
            {
                TempData["Error"] = "Cannot delete system groups.";
                return RedirectToAction(nameof(Groups));
            }

            return View("~/Views/Admin/GroupManagement/DeleteGroup.cshtml", group);
        }

        // POST: Admin/GroupManagement/DeleteGroup/5
        [HttpPost("DeleteGroup/{id:int}"), ActionName("DeleteGroup")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteGroupConfirmed(int id)
        {
            if (!await IsSuperAdminOrAdminAsync())
            {
                return Forbid();
            }

            var group = await _context.Groups
                .Include(g => g.UserGroups)
                .Include(g => g.GroupFeaturePermissions)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (group == null)
            {
                return NotFound();
            }

            // Don't allow deleting system groups
            if (group.IsSystemGroup)
            {
                TempData["Error"] = "Cannot delete system groups.";
                return RedirectToAction(nameof(Groups));
            }

            try
            {
                // Remove user groups
                _context.UserGroups.RemoveRange(group.UserGroups);

                // Remove permissions
                _context.GroupFeaturePermissions.RemoveRange(group.GroupFeaturePermissions);

                // Remove group
                _context.Groups.Remove(group);

                await _context.SaveChangesAsync();

                var userEmail = GetUserEmail();
                _logger.LogInformation("Group deleted: {GroupName} by {User}", group.Name, userEmail);
                return RedirectToAction(nameof(Groups));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting group");
                ModelState.AddModelError("", "An error occurred while deleting the group. Please try again.");
                return View("~/Views/Admin/GroupManagement/DeleteGroup.cshtml", group);
            }
        }

        // GET: Admin/GroupManagement/ManageUsers/5
        [HttpGet("ManageUsers/{id:int}")]
        public async Task<IActionResult> ManageUsers(int? id)
        {
            if (!await IsSuperAdminOrAdminAsync())
            {
                return Forbid();
            }

            if (id == null)
            {
                return NotFound();
            }

            var group = await _context.Groups
                .Include(g => g.UserGroups)
                .ThenInclude(ug => ug.User)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (group == null)
            {
                return NotFound();
            }

            return View("~/Views/Admin/GroupManagement/ManageUsers.cshtml", group);
        }

        // POST: Admin/GroupManagement/AddUserToGroup
        [HttpPost("AddUserToGroup")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddUserToGroup(int groupId, string? entraUserObjectId, string? entraUserEmail, string? entraUserName)
        {
            if (!await IsSuperAdminOrAdminAsync())
            {
                return Forbid();
            }

            var group = await _context.Groups
                .Include(g => g.UserGroups)
                .ThenInclude(ug => ug.User)
                .FirstOrDefaultAsync(g => g.Id == groupId);

            if (group == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(entraUserObjectId) || string.IsNullOrWhiteSpace(entraUserEmail))
            {
                TempData["ErrorMessage"] = "Please select a user from Entra ID.";
                return RedirectToAction(nameof(ManageUsers), new { id = groupId });
            }

            try
            {
                var currentUserEmail = GetUserEmail();
                User? user = null;

                // Try to get or create user using Entra Object ID
                if (Guid.TryParse(entraUserObjectId, out var objectIdGuid))
                {
                    try
                    {
                        // Use UserDirectoryService to ensure user exists (fetches from Graph and creates/updates in DB)
                        user = await _userDirectoryService.EnsureUserAsync(objectIdGuid);
                        _logger.LogInformation("Ensured user exists via UserDirectoryService: ObjectId={ObjectId}, Email={Email}, Name={Name}", 
                            entraUserObjectId, user.Email, user.Name);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to ensure user with ObjectId {ObjectId}, trying email lookup", entraUserObjectId);
                    }
                }

                // Fallback: try to find user by email if EnsureUserAsync failed
                if (user == null && !string.IsNullOrWhiteSpace(entraUserEmail))
                {
                    var userEmailLower = entraUserEmail.ToLowerInvariant().Trim();
                    user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == userEmailLower);

                    if (user != null)
                    {
                        // Update AzureObjectId if it's missing
                        if (string.IsNullOrWhiteSpace(user.AzureObjectId) && Guid.TryParse(entraUserObjectId, out var objectIdGuid2))
                        {
                            user.AzureObjectId = objectIdGuid2.ToString();
                            user.UpdatedAt = DateTime.UtcNow;
                            await _context.SaveChangesAsync();
                        }

                        // Update name if provided and different
                        if (!string.IsNullOrWhiteSpace(entraUserName) && user.Name != entraUserName.Trim())
                        {
                            user.Name = entraUserName.Trim();
                            user.UpdatedAt = DateTime.UtcNow;
                            await _context.SaveChangesAsync();
                        }
                    }
                }

                // If still no user, create one with the provided data
                if (user == null)
                {
                    var userEmailLower = entraUserEmail.ToLowerInvariant().Trim();
                    user = new User
                    {
                        Email = userEmailLower,
                        Name = !string.IsNullOrWhiteSpace(entraUserName) ? entraUserName.Trim() : userEmailLower,
                        AzureObjectId = Guid.TryParse(entraUserObjectId, out _) ? entraUserObjectId : null,
                        Role = UserRole.Visitor,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Created new user: {Email}", userEmailLower);
                }

                // Check if user is already in the group
                var existingUserGroup = await _context.UserGroups
                    .FirstOrDefaultAsync(ug => ug.UserId == user.Id && ug.GroupId == groupId);

                if (existingUserGroup != null)
                {
                    TempData["ErrorMessage"] = $"User {user.Email} is already in this group.";
                    return RedirectToAction(nameof(ManageUsers), new { id = groupId });
                }

                // Add user to group
                var userGroup = new UserGroup
                {
                    UserId = user.Id,
                    GroupId = groupId,
                    AssignedAt = DateTime.UtcNow,
                    AssignedBy = currentUserEmail
                };

                _context.UserGroups.Add(userGroup);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"User {user.Name} ({user.Email}) has been added to the group.";
                _logger.LogInformation("User {UserEmail} added to group {GroupName} by {User}", user.Email, group.Name, currentUserEmail);
                return RedirectToAction(nameof(ManageUsers), new { id = groupId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding user to group");
                TempData["ErrorMessage"] = "An error occurred while adding the user. Please try again.";
                return RedirectToAction(nameof(ManageUsers), new { id = groupId });
            }
        }

        // POST: Admin/GroupManagement/RemoveUserFromGroup
        [HttpPost("RemoveUserFromGroup")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveUserFromGroup(int groupId, int userId)
        {
            if (!await IsSuperAdminOrAdminAsync())
            {
                return Forbid();
            }

            var group = await _context.Groups
                .Include(g => g.UserGroups)
                .ThenInclude(ug => ug.User)
                .FirstOrDefaultAsync(g => g.Id == groupId);

            if (group == null)
            {
                return NotFound();
            }

            try
            {
                var userGroup = await _context.UserGroups
                    .Include(ug => ug.User)
                    .FirstOrDefaultAsync(ug => ug.UserId == userId && ug.GroupId == groupId);

                if (userGroup != null)
                {
                    var userName = userGroup.User.Name;
                    var userEmail = userGroup.User.Email;
                    
                    _context.UserGroups.Remove(userGroup);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"User {userName} ({userEmail}) has been removed from the group.";
                    var currentUserEmail = GetUserEmail();
                    _logger.LogInformation("User {UserEmail} removed from group {GroupName} by {User}", userEmail, group.Name, currentUserEmail);
                }

                return RedirectToAction(nameof(ManageUsers), new { id = groupId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing user from group");
                TempData["ErrorMessage"] = "An error occurred while removing the user. Please try again.";
                return RedirectToAction(nameof(ManageUsers), new { id = groupId });
            }
        }

        // POST: Admin/GroupManagement/RemoveUserFromGroupFromPermissions
        [HttpPost("RemoveUserFromGroupFromPermissions")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveUserFromGroupFromPermissions(int groupId, int userId)
        {
            if (!await IsSuperAdminOrAdminAsync())
            {
                return Forbid();
            }

            var group = await _context.Groups
                .FirstOrDefaultAsync(g => g.Id == groupId);

            if (group == null)
            {
                return NotFound();
            }

            // Allow removing from system groups but log it
            if (group.IsSystemGroup)
            {
                var currentUserEmail = GetUserEmail();
                _logger.LogWarning("User {User} removed from system group {GroupName} from UserPermissions view", currentUserEmail, group.Name);
            }

            try
            {
                var userGroup = await _context.UserGroups
                    .Include(ug => ug.User)
                    .Include(ug => ug.Group)
                    .FirstOrDefaultAsync(ug => ug.UserId == userId && ug.GroupId == groupId);

                if (userGroup != null)
                {
                    var userName = userGroup.User.Name;
                    var userEmail = userGroup.User.Email;
                    var groupName = userGroup.Group.Name;
                    
                    _context.UserGroups.Remove(userGroup);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"User {userName} ({userEmail}) has been removed from the {groupName} group.";
                    var currentUserEmail = GetUserEmail();
                    _logger.LogInformation("User {UserEmail} removed from group {GroupName} by {User} (from UserPermissions)", userEmail, groupName, currentUserEmail);
                }

                return RedirectToAction(nameof(UserPermissions));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing user from group from UserPermissions");
                TempData["ErrorMessage"] = "An error occurred while removing the user. Please try again.";
                return RedirectToAction(nameof(UserPermissions));
            }
        }

        // GET: Admin/GroupManagement/Features
        [HttpGet("Features")]
        public async Task<IActionResult> Features()
        {
            if (!await IsSuperAdminOrAdminAsync())
            {
                return Forbid();
            }

            var features = await _context.Features
                .OrderBy(f => f.Name)
                .ToListAsync();

            return View("~/Views/Admin/GroupManagement/Features.cshtml", features);
        }

        // GET: Admin/UserManagement/UserPermissions
        [HttpGet("UserPermissions")]
        public async Task<IActionResult> UserPermissions()
        {
            if (!await IsSuperAdminOrAdminAsync())
            {
                return Forbid();
            }

            var users = await _context.Users
                .Include(u => u.UserGroups)
                .ThenInclude(ug => ug.Group)
                .ThenInclude(g => g.GroupFeaturePermissions)
                .ThenInclude(gfp => gfp.Feature)
                .OrderBy(u => u.Name)
                .ToListAsync();

            return View("~/Views/Admin/GroupManagement/UserPermissions.cshtml", users);
        }

        // GET: Admin/GroupManagement/CreateFeature
        [HttpGet("CreateFeature")]
        public IActionResult CreateFeature()
        {
            return View("~/Views/Admin/GroupManagement/CreateFeature.cshtml");
        }

        // POST: Admin/GroupManagement/CreateFeature
        [HttpPost("CreateFeature")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateFeature(Feature feature)
        {
            if (!await IsSuperAdminOrAdminAsync())
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    feature.CreatedAt = DateTime.UtcNow;
                    feature.UpdatedAt = DateTime.UtcNow;
                    feature.Code = feature.Code.ToLower().Replace(" ", "_");

                    _context.Features.Add(feature);
                    await _context.SaveChangesAsync();

                    var userEmail = GetUserEmail();
                    _logger.LogInformation("Feature created: {FeatureName} by {User}", feature.Name, userEmail);
                    return RedirectToAction(nameof(Features));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating feature");
                    ModelState.AddModelError("", "An error occurred while creating the feature. Please try again.");
                }
            }

            return View("~/Views/Admin/GroupManagement/CreateFeature.cshtml", feature);
        }
    }
}

