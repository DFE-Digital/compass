using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Compass.Data;
using Compass.Models;

namespace Compass.Controllers;

[Authorize]
public class DdtReportsController : Controller
{
    private readonly CompassDbContext _context;
    private readonly ILogger<DdtReportsController> _logger;

    public DdtReportsController(CompassDbContext context, ILogger<DdtReportsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: DdtReports/Index - Landing page
    public IActionResult Index()
    {
        return View();
    }

    // GET: DdtReports/ViewReport
    public async Task<IActionResult> ViewReport(DateTime? weekStart, string section = "summary")
    {
        try
        {
            // Calculate week start if not provided
            if (!weekStart.HasValue)
            {
                var today = DateTime.Today;
                var dayOfWeek = (int)today.DayOfWeek;
                var diff = dayOfWeek == 0 ? -6 : 1 - dayOfWeek; // Monday = start of week
                weekStart = today.AddDays(diff);
            }

            // Get all active projects with their related data
            var projects = await _context.Projects
                .Include(p => p.Successes)
                .Include(p => p.Milestones)
                .Include(p => p.RagHistory)
                .Where(p => !p.IsDeleted && p.Status == "Active")
                .OrderBy(p => p.Title)
                .ToListAsync();

            ViewBag.CurrentSection = section;
            ViewBag.WeekStart = weekStart.Value;
            ViewBag.WeekEnd = weekStart.Value.AddDays(6);
            
            return View(projects);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading DDT Reports");
            TempData["ErrorMessage"] = "An error occurred while loading the DDT reports.";
            return RedirectToAction("Index");
        }
    }

    // GET: DdtReports/BusinessArea
    public async Task<IActionResult> BusinessArea(string area, DateTime? weekStart, string section = "summary")
    {
        try
        {
            // Calculate week start if not provided
            if (!weekStart.HasValue)
            {
                var today = DateTime.Today;
                var dayOfWeek = (int)today.DayOfWeek;
                var diff = dayOfWeek == 0 ? -6 : 1 - dayOfWeek; // Monday = start of week
                weekStart = today.AddDays(diff);
            }

            // Get all active projects for the specified business area
            var projects = await _context.Projects
                .Include(p => p.Successes)
                .Include(p => p.Milestones)
                .Include(p => p.RagHistory)
                .Where(p => !p.IsDeleted && p.Status == "Active" && p.BusinessArea == area)
                .OrderBy(p => p.Title)
                .ToListAsync();

            ViewBag.CurrentSection = section;
            ViewBag.WeekStart = weekStart.Value;
            ViewBag.WeekEnd = weekStart.Value.AddDays(6);
            ViewBag.BusinessArea = area;
            
            return View(projects);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading DDT Reports for business area: {Area}", area);
            TempData["ErrorMessage"] = $"An error occurred while loading the DDT reports for {area}.";
            return RedirectToAction("Index");
        }
    }

    // GET: DdtReports/FlagshipProjects
    public async Task<IActionResult> FlagshipProjects()
    {
        try
        {
            // Get all active flagship projects with their deliverables
            var flagshipProjects = await _context.Projects
                .Include(p => p.Milestones)
                .Include(p => p.DependenciesAsSource)
                .Where(p => !p.IsDeleted && p.Status == "Active" && p.IsFlagship)
                .OrderBy(p => p.Title)
                .ToListAsync();

            // For each flagship project, get its deliverable projects
            foreach (var flagship in flagshipProjects)
            {
                var deliverableIds = flagship.DependenciesAsSource
                    .Where(d => d.TargetEntityType == "Project" && d.DependencyType == "Deliverable")
                    .Select(d => d.TargetEntityId)
                    .ToList();

                var deliverables = await _context.Projects
                    .Include(p => p.Milestones)
                    .Where(p => deliverableIds.Contains(p.Id) && !p.IsDeleted)
                    .ToListAsync();

                // Store deliverables in a ViewBag dictionary keyed by flagship project ID
                ViewBag.Deliverables = ViewBag.Deliverables ?? new Dictionary<int, List<Project>>();
                ((Dictionary<int, List<Project>>)ViewBag.Deliverables)[flagship.Id] = deliverables;
            }

            return View(flagshipProjects);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading Flagship Projects for DDT Reports");
            TempData["ErrorMessage"] = "An error occurred while loading the flagship projects report.";
            return RedirectToAction("Index");
        }
    }
}

