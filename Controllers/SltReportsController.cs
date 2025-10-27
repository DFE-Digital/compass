using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Compass.Data;
using Compass.Models;

namespace Compass.Controllers;

[Authorize]
public class SltReportsController : Controller
{
    private readonly CompassDbContext _context;
    private readonly ILogger<SltReportsController> _logger;

    public SltReportsController(CompassDbContext context, ILogger<SltReportsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: SltReports/Index - Landing page
    public IActionResult Index()
    {
        return View();
    }

    // GET: SltReports/ViewReport
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
            _logger.LogError(ex, "Error loading SLT Reports");
            TempData["ErrorMessage"] = "An error occurred while loading the SLT reports.";
            return RedirectToAction("Index");
        }
    }
}

