using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Compass.Data;
using Compass.Models;
using Compass.Services;
using System.Security.Claims;

namespace Compass.Controllers;

[Authorize]
public class ReportsController : Controller
{
    private readonly CompassDbContext _context;
    private readonly ILogger<ReportsController> _logger;
    private readonly IPermissionService _permissionService;

    public ReportsController(CompassDbContext context, ILogger<ReportsController> logger, IPermissionService permissionService)
    {
        _context = context;
        _logger = logger;
        _permissionService = permissionService;
    }

    private string GetUserEmail()
    {
        return User.Identity?.Name 
            ?? User.FindFirst(ClaimTypes.Email)?.Value 
            ?? User.FindFirst("preferred_username")?.Value
            ?? User.FindFirst("email")?.Value
            ?? string.Empty;
    }

    private async Task<User?> GetCurrentUserAsync()
    {
        var userEmail = GetUserEmail();
        if (string.IsNullOrEmpty(userEmail))
        {
            return null;
        }

        return await _context.Users
            .Include(u => u.DivisionUsers)
                .ThenInclude(du => du.Division)
            .Include(u => u.BusinessAreaUsers)
                .ThenInclude(bau => bau.BusinessAreaLookup)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == userEmail.ToLower());
    }

    private async Task<bool> IsAdminUserAsync()
    {
        var userEmail = GetUserEmail();
        if (string.IsNullOrEmpty(userEmail))
        {
            return false;
        }

        try
        {
            return await _permissionService.IsSuperAdminAsync(userEmail) ||
                   await _permissionService.IsInGroupAsync(userEmail, "Central Operations Admin");
        }
        catch
        {
            // Non-blocking: default to false
            return false;
        }
    }

    /// <summary>
    /// Default dashboard that routes users to their appropriate reports based on their assignments
    /// </summary>
    public async Task<IActionResult> Index()
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null)
        {
            TempData["ErrorMessage"] = "Unable to identify the current user.";
            return RedirectToAction("Index", "Home");
        }

        var isAdmin = await IsAdminUserAsync();

        // Admin users can see all reports - show dashboard with all options
        if (isAdmin)
        {
            ViewData["Title"] = "Reports";
            ViewBag.IsAdmin = true;
            ViewBag.Message = "As an administrator, you can view all reports and see all projects.";
            return View();
        }

        var userDivisions = currentUser.DivisionUsers
            .Where(du => du.Division != null && du.Division.IsActive)
            .Select(du => du.Division!)
            .ToList();

        var userBusinessAreas = currentUser.BusinessAreaUsers
            .Where(bau => bau.BusinessAreaLookup != null && bau.BusinessAreaLookup.IsActive)
            .Select(bau => bau.BusinessAreaLookup!)
            .ToList();

        // If user has divisions, show division report
        if (userDivisions.Any())
        {
            return RedirectToAction(nameof(DivisionReport));
        }

        // If user has business areas, show business area report
        if (userBusinessAreas.Any())
        {
            return RedirectToAction(nameof(BusinessAreaReport));
        }

        // If user has neither, show a message
        ViewData["Title"] = "Reports";
        ViewBag.Message = "You are not assigned to any divisions or business areas. Please contact an administrator to be assigned.";
        return View();
    }

    /// <summary>
    /// Division report showing projects assigned to the user's divisions/directorates
    /// </summary>
    public async Task<IActionResult> DivisionReport()
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null)
        {
            TempData["ErrorMessage"] = "Unable to identify the current user.";
            return RedirectToAction("Index", "Home");
        }

        var isAdmin = await IsAdminUserAsync();
        List<Division> userDivisions;
        List<Project> projects;

        if (isAdmin)
        {
            // Admin users can see all divisions and all projects
            userDivisions = await _context.Divisions
                .Where(d => d.IsActive)
                .OrderBy(d => d.Name)
                .ToListAsync();

            projects = await _context.Projects
                .Where(p => !p.IsDeleted)
                .Include(p => p.Directorates)
                    .ThenInclude(d => d.Division)
                .Include(p => p.BusinessAreaLookup)
                .Include(p => p.RagStatusLookup)
                .Include(p => p.PhaseLookup)
                .Include(p => p.PrimaryContactUser)
                .Include(p => p.SeniorResponsibleOfficers)
                    .ThenInclude(sro => sro.User)
                .Include(p => p.ServiceOwners)
                    .ThenInclude(so => so.User)
                .OrderBy(p => p.Title)
                .ToListAsync();
        }
        else
        {
            // Regular users see only their assigned divisions
            userDivisions = currentUser.DivisionUsers
                .Where(du => du.Division != null && du.Division.IsActive)
                .Select(du => du.Division!)
                .ToList();

            if (!userDivisions.Any())
            {
                TempData["ErrorMessage"] = "You are not assigned to any divisions.";
                return RedirectToAction(nameof(Index));
            }

            var divisionIds = userDivisions.Select(d => d.Id).ToList();

            // Get projects assigned to these divisions via ProjectDirectorate
            projects = await _context.Projects
                .Where(p => !p.IsDeleted && p.Directorates.Any(d => divisionIds.Contains(d.DivisionId)))
                .Include(p => p.Directorates)
                    .ThenInclude(d => d.Division)
                .Include(p => p.BusinessAreaLookup)
                .Include(p => p.RagStatusLookup)
                .Include(p => p.PhaseLookup)
                .Include(p => p.PrimaryContactUser)
                .Include(p => p.SeniorResponsibleOfficers)
                    .ThenInclude(sro => sro.User)
                .Include(p => p.ServiceOwners)
                    .ThenInclude(so => so.User)
                .OrderBy(p => p.Title)
                .ToListAsync();
        }

        ViewData["Title"] = "Division report";
        ViewBag.UserDivisions = userDivisions;
        ViewBag.Projects = projects;
        ViewBag.ProjectCount = projects.Count;
        ViewBag.IsAdmin = isAdmin;

        return View();
    }

    /// <summary>
    /// Business area report showing projects in the user's business areas
    /// </summary>
    public async Task<IActionResult> BusinessAreaReport()
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null)
        {
            TempData["ErrorMessage"] = "Unable to identify the current user.";
            return RedirectToAction("Index", "Home");
        }

        var isAdmin = await IsAdminUserAsync();
        List<BusinessAreaLookup> userBusinessAreas;
        List<Project> projects;

        if (isAdmin)
        {
            // Admin users can see all business areas and all projects
            userBusinessAreas = await _context.BusinessAreaLookups
                .Where(ba => ba.IsActive)
                .OrderBy(ba => ba.Name)
                .ToListAsync();

            projects = await _context.Projects
                .Where(p => !p.IsDeleted)
                .Include(p => p.BusinessAreaLookup)
                .Include(p => p.Directorates)
                    .ThenInclude(d => d.Division)
                .Include(p => p.RagStatusLookup)
                .Include(p => p.PhaseLookup)
                .Include(p => p.PrimaryContactUser)
                .Include(p => p.SeniorResponsibleOfficers)
                    .ThenInclude(sro => sro.User)
                .Include(p => p.ServiceOwners)
                    .ThenInclude(so => so.User)
                .OrderBy(p => p.Title)
                .ToListAsync();
        }
        else
        {
            // Regular users see only their assigned business areas
            userBusinessAreas = currentUser.BusinessAreaUsers
                .Where(bau => bau.BusinessAreaLookup != null && bau.BusinessAreaLookup.IsActive)
                .Select(bau => bau.BusinessAreaLookup!)
                .ToList();

            if (!userBusinessAreas.Any())
            {
                TempData["ErrorMessage"] = "You are not assigned to any business areas.";
                return RedirectToAction(nameof(Index));
            }

            var businessAreaIds = userBusinessAreas.Select(ba => ba.Id).ToList();

            // Get projects assigned to these business areas
            projects = await _context.Projects
                .Where(p => !p.IsDeleted && p.BusinessAreaId.HasValue && businessAreaIds.Contains(p.BusinessAreaId.Value))
                .Include(p => p.BusinessAreaLookup)
                .Include(p => p.Directorates)
                    .ThenInclude(d => d.Division)
                .Include(p => p.RagStatusLookup)
                .Include(p => p.PhaseLookup)
                .Include(p => p.PrimaryContactUser)
                .Include(p => p.SeniorResponsibleOfficers)
                    .ThenInclude(sro => sro.User)
                .Include(p => p.ServiceOwners)
                    .ThenInclude(so => so.User)
                .OrderBy(p => p.Title)
                .ToListAsync();
        }

        ViewData["Title"] = "Business area report";
        ViewBag.UserBusinessAreas = userBusinessAreas;
        ViewBag.Projects = projects;
        ViewBag.ProjectCount = projects.Count;
        ViewBag.IsAdmin = isAdmin;

        return View();
    }
}
