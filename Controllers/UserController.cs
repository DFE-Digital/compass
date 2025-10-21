using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Compass.Data;
using Compass.Models;
using Compass.Helpers;
using Compass.Services;

namespace Compass.Controllers;

[Authorize]
public class UserController : Controller
{
    private readonly CompassDbContext _context;
    private readonly ILogger<UserController> _logger;
    private readonly IProductsApiService _productsApiService;

    public UserController(CompassDbContext context, ILogger<UserController> logger, IProductsApiService productsApiService)
    {
        _context = context;
        _logger = logger;
        _productsApiService = productsApiService;
    }

    // GET: User/MySettings
    public async Task<IActionResult> MySettings()
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

        var preferences = await _context.UserPreferences.FindAsync(user.Id);
        var selectedBusinessAreas = preferences != null && !string.IsNullOrEmpty(preferences.PreferredBusinessAreas)
            ? preferences.PreferredBusinessAreas.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(ba => ba.Trim()).ToList()
            : new List<string>();

        var businessAreas = await _productsApiService.GetBusinessAreasAsync();
        
        ViewBag.User = user;
        ViewBag.SelectedBusinessAreas = selectedBusinessAreas;
        ViewBag.BusinessAreas = businessAreas;

        return View();
    }

    // POST: User/MySettings
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MySettings(string[] selectedBusinessAreas)
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

        return RedirectToAction(nameof(MySettings));
    }
}

