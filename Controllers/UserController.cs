using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Compass.Data;
using Compass.Models;
using Compass.Helpers;
using Compass.Services;
using Compass.ViewModels;
using Compass.Security;
using System.Security.Claims;

namespace Compass.Controllers;

[Authorize]
public class UserController : Controller
{
    private readonly CompassDbContext _context;
    private readonly ILogger<UserController> _logger;
    private readonly IProductsApiService _productsApiService;
    private readonly IUserDirectoryService _userDirectoryService;

    public UserController(
        CompassDbContext context,
        ILogger<UserController> logger,
        IProductsApiService productsApiService,
        IUserDirectoryService userDirectoryService)
    {
        _context = context;
        _logger = logger;
        _productsApiService = productsApiService;
        _userDirectoryService = userDirectoryService;
    }

    // GET: User/MySettings
    public async Task<IActionResult> MySettings(string section = "profile")
    {
        var userEmail = User.Identity?.Name;
        _logger.LogInformation($"MySettings accessed by user: {userEmail}");
        
        if (string.IsNullOrEmpty(userEmail))
        {
            _logger.LogWarning("MySettings: No user email found");
            return Redirect("~/");
        }

        // Case-insensitive email lookup
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == userEmail.ToLower());
        if (user == null)
        {
            _logger.LogWarning($"MySettings: User not found in database for email: {userEmail}. Creating new user.");
            
            // Create user if they don't exist
            user = new User
            {
                Email = userEmail.ToLower(),
                Name = userEmail.Split('@')[0].Replace(".", " "),
                Role = UserRole.Visitor,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation($"MySettings: Created new user for email: {userEmail}");
        }

        user = await RefreshDirectoryProfileAsync(user, HttpContext.RequestAborted);

        var userWithAccess = await _context.Users
            .Include(u => u.UserGroups)
                .ThenInclude(ug => ug.Group)
                    .ThenInclude(g => g.GroupFeaturePermissions)
                        .ThenInclude(gfp => gfp.Feature)
            .FirstOrDefaultAsync(u => u.Id == user.Id) ?? user;

        var groupPermissions = userWithAccess.UserGroups
            .OrderBy(ug => ug.Group.Name)
            .Select(ug =>
            {
                var featurePermissions = ug.Group.GroupFeaturePermissions
                    .GroupBy(gfp => gfp.Feature)
                    .Select(group =>
                    {
                        var topPermission = group
                            .OrderByDescending(gfp => gfp.Permission)
                            .First();
                        return new FeaturePermissionSummaryViewModel
                        {
                            FeatureName = topPermission.Feature.Name,
                            FeatureCode = topPermission.Feature.Code,
                            Permission = topPermission.Permission,
                            PermissionLabel = DescribePermission(topPermission.Permission)
                        };
                    })
                    .OrderBy(fp => fp.FeatureName)
                    .ToList();

                return new UserGroupPermissionSummaryViewModel
                {
                    GroupName = ug.Group.Name,
                    GroupDescription = ug.Group.Description,
                    AssignedAt = ug.AssignedAt,
                    FeaturePermissions = featurePermissions
                };
            })
            .ToList();

        var preferences = await _context.UserPreferences.FindAsync(user.Id);
        var selectedBusinessAreas = preferences != null && !string.IsNullOrEmpty(preferences.PreferredBusinessAreas)
            ? preferences.PreferredBusinessAreas.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(ba => ba.Trim()).ToList()
            : new List<string>();

        var businessAreas = await _productsApiService.GetBusinessAreasAsync();
        
        ViewBag.User = userWithAccess;
        ViewBag.GroupPermissions = groupPermissions;
        ViewBag.SelectedBusinessAreas = selectedBusinessAreas;
        ViewBag.BusinessAreas = businessAreas;
        ViewBag.CurrentSection = section;

        return View();
    }

    // POST: User/MySettings
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MySettings(string[] selectedBusinessAreas, string section = "preferences")
    {
        try
        {
            var userEmail = User.Identity?.Name;
            if (!string.IsNullOrEmpty(userEmail))
            {
                await UserPreferencesHelper.SavePreferredBusinessAreasAsync(
                    _context, 
                    userEmail, 
                    selectedBusinessAreas?.ToList() ?? new List<string>()
                );

                TempData["SuccessMessage"] = "Your preferences have been saved successfully.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving user preferences");
            TempData["ErrorMessage"] = "An error occurred while saving your preferences. Please try again.";
        }

        return RedirectToAction(nameof(MySettings), new { section = section });
    }

    private async Task<User> RefreshDirectoryProfileAsync(User currentUser, CancellationToken cancellationToken)
    {
        var objectIdClaim = User.FindFirstValue(CompassClaimTypes.ObjectIdentifier);
        if (!Guid.TryParse(objectIdClaim, out var objectId) && !Guid.TryParse(currentUser.AzureObjectId, out objectId))
        {
            return currentUser;
        }

        try
        {
            var refreshed = await _userDirectoryService.EnsureUserAsync(objectId, cancellationToken);
            return refreshed;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to refresh user profile for {Email}", currentUser.Email);
            return currentUser;
        }
    }

    private static string DescribePermission(PermissionType permission)
        => permission switch
        {
            PermissionType.View => "View only",
            PermissionType.Create => "Create",
            PermissionType.Update => "Update",
            PermissionType.Delete => "Full control",
            _ => permission.ToString()
        };
}

