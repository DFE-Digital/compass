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
    public Task<IActionResult> Index(CancellationToken cancellationToken) =>
        RenderAsync("~/Views/Modern/Profile/Index.cshtml", cancellationToken);

    [HttpGet("permissions")]
    public Task<IActionResult> Permissions(CancellationToken cancellationToken) =>
        RenderAsync("~/Views/Modern/Profile/Permissions.cshtml", cancellationToken);

    private async Task<IActionResult> RenderAsync(string viewName, CancellationToken cancellationToken)
    {
        var vm = await BuildViewModelAsync(cancellationToken);
        return View(viewName, vm);
    }

    private async Task<ModernProfileViewModel> BuildViewModelAsync(CancellationToken cancellationToken)
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

        return new ModernProfileViewModel
        {
            Email = string.IsNullOrEmpty(email) ? null : email,
            DisplayName = BuildDisplayName(dbUser),
            DirectoryGroups = groups
        };
    }

    private static string? BuildDisplayName(User? dbUser)
    {
        if (dbUser == null) return null;
        if (!string.IsNullOrWhiteSpace(dbUser.Name))
            return dbUser.Name.Trim();

        var parts = new[] { dbUser.FirstName, dbUser.LastName }
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s!.Trim());
        var joined = string.Join(" ", parts);
        return string.IsNullOrWhiteSpace(joined) ? null : joined;
    }
}
