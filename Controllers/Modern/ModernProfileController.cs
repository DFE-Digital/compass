using System.Security.Claims;
using Compass.Data;
using Compass.Models;
using Compass.Services;
using Compass.ViewModels.Modern;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Compass.Controllers.Modern;

/// <summary>Current user profile at <c>/modern/profile</c>.</summary>
[Authorize]
[Route("modern/profile")]
public class ModernProfileController : Controller
{
    private readonly CompassDbContext _context;
    private readonly IPermissionService _permissionService;

    public ModernProfileController(CompassDbContext context, IPermissionService permissionService)
    {
        _context = context;
        _permissionService = permissionService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var email = User.Identity?.Name
            ?? User.FindFirstValue(ClaimTypes.Email)
            ?? User.FindFirstValue("preferred_username")
            ?? User.FindFirstValue("email")
            ?? string.Empty;

        User? dbUser = null;
        if (!string.IsNullOrEmpty(email))
        {
            dbUser = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower(), cancellationToken);
        }

        IReadOnlyList<string> groups = Array.Empty<string>();
        if (!string.IsNullOrEmpty(email))
        {
            try
            {
                groups = await _permissionService.GetUserGroupsAsync(email);
            }
            catch
            {
                groups = Array.Empty<string>();
            }
        }

        var vm = new ModernProfileViewModel
        {
            SignInName = User.Identity?.Name,
            Email = email,
            HasCompassUserRecord = dbUser != null,
            DatabaseName = dbUser?.Name,
            FirstName = dbUser?.FirstName,
            LastName = dbUser?.LastName,
            JobTitle = dbUser?.JobTitle,
            UserPrincipalName = dbUser?.UserPrincipalName,
            AzureObjectId = dbUser?.AzureObjectId,
            ApplicationRole = dbUser?.Role.ToString(),
            DirectoryGroups = groups
        };

        return View("~/Views/Modern/Profile/Index.cshtml", vm);
    }
}
