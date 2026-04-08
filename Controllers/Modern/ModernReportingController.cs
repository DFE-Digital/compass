using Compass.Controllers;
using Compass.Data;
using Compass.Models;
using Compass.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Compass.Controllers.Modern;

/// <summary>Modern reporting UI at <c>/modern/reporting/*</c> (same period logic as Central Ops monthly reporting).</summary>
[Authorize]
[Route("modern/reporting")]
public class ModernReportingController : Controller
{
    private readonly CompassDbContext _context;
    private readonly IMonthlyUpdateService _monthlyUpdateService;
    private readonly ILogger<ModernReportingController> _logger;

    public ModernReportingController(
        CompassDbContext context,
        IMonthlyUpdateService monthlyUpdateService,
        ILogger<ModernReportingController> logger)
    {
        _context = context;
        _monthlyUpdateService = monthlyUpdateService;
        _logger = logger;
    }

    private static MonthlyUpdateStats CalculateMonthlyUpdateStats(
        List<Project> projects,
        int year,
        int month,
        IMonthlyUpdateService monthlyUpdateService)
    {
        var totalProjects = projects.Count;
        var dueDate = monthlyUpdateService.GetMonthlyUpdateDueDate(year, month);
        var currentDate = DateTime.UtcNow;

        var submitted = 0;
        var notStarted = 0;
        var inProgress = 0;
        var late = 0;

        foreach (var project in projects)
        {
            var update = project.MonthlyUpdates?.FirstOrDefault(u => u.Year == year && u.Month == month);

            if (update != null && update.SubmittedAt.HasValue)
                submitted++;
            else if (currentDate > dueDate)
                late++;
            else if (update != null && !update.SubmittedAt.HasValue)
                inProgress++;
            else
                notStarted++;
        }

        return new MonthlyUpdateStats
        {
            Year = year,
            Month = month,
            TotalProjects = totalProjects,
            Submitted = submitted,
            NotStarted = notStarted,
            InProgress = inProgress,
            Late = late,
            DueDate = dueDate
        };
    }

    [HttpGet("monthly-update-overview")]
    public async Task<IActionResult> MonthlyUpdateOverview(int? year, int? month, string? status)
    {
        try
        {
            var allProjects = await _context.Projects
                .AsNoTracking()
                .Include(p => p.MonthlyUpdates)
                .Include(p => p.PrimaryContactUser)
                .Include(p => p.DeliveryPriority)
                .Include(p => p.BusinessAreaLookup)
                .Include(p => p.ServiceOwners)
                    .ThenInclude(so => so.User)
                .Where(p => !p.IsDeleted && p.Status != "Cancelled" && p.Status != "Completed")
                .ToListAsync();

            var currentDate = DateTime.UtcNow;
            var currentYear = currentDate.Year;
            var currentMonth = currentDate.Month;

            var currentPeriodDueDate = _monthlyUpdateService.GetMonthlyUpdateDueDate(currentYear, currentMonth);
            var daysUntilCurrentPeriodDueDate = (currentPeriodDueDate - currentDate).Days;

            var reportYear = daysUntilCurrentPeriodDueDate <= 10 ? currentYear : (currentMonth == 1 ? currentYear - 1 : currentYear);
            var reportMonth = daysUntilCurrentPeriodDueDate <= 10 ? currentMonth : (currentMonth == 1 ? 12 : currentMonth - 1);

            var filterYear = year ?? reportYear;
            var filterMonth = month ?? reportMonth;

            var dueDate = _monthlyUpdateService.GetMonthlyUpdateDueDate(filterYear, filterMonth);

            var periodStats = CalculateMonthlyUpdateStats(allProjects, filterYear, filterMonth, _monthlyUpdateService);

            var statusGroups = new Dictionary<string, List<Project>>
            {
                { "Submitted", new List<Project>() },
                { "Late", new List<Project>() },
                { "In Progress", new List<Project>() },
                { "Not Started", new List<Project>() }
            };

            foreach (var project in allProjects)
            {
                var update = project.MonthlyUpdates?.FirstOrDefault(u => u.Year == filterYear && u.Month == filterMonth);
                string projectStatus;

                if (update != null && update.SubmittedAt.HasValue)
                    projectStatus = "Submitted";
                else if (currentDate > dueDate)
                    projectStatus = "Late";
                else if (update != null && !update.SubmittedAt.HasValue)
                    projectStatus = "In Progress";
                else
                    projectStatus = "Not Started";

                if (statusGroups.ContainsKey(projectStatus))
                    statusGroups[projectStatus].Add(project);
            }

            foreach (var group in statusGroups.Values)
                group.Sort((a, b) => string.Compare(a.Title, b.Title, StringComparison.OrdinalIgnoreCase));

            var businessAreaGroups = new Dictionary<string, BusinessAreaMonthlyData>();

            var businessAreas = allProjects
                .Where(p => p.BusinessAreaLookup != null)
                .Select(p => p.BusinessAreaLookup!)
                .GroupBy(ba => ba.Id)
                .Select(g => g.First())
                .ToList();

            var notAssignedProjects = allProjects.Where(p => p.BusinessAreaLookup == null).ToList();
            if (notAssignedProjects.Any())
            {
                businessAreaGroups.Add("Not assigned", new BusinessAreaMonthlyData
                {
                    BusinessAreaName = "Not assigned",
                    TotalProjects = notAssignedProjects.Count,
                    SubmittedCount = notAssignedProjects.Count(p =>
                    {
                        var update = p.MonthlyUpdates?.FirstOrDefault(u => u.Year == filterYear && u.Month == filterMonth);
                        return update != null && update.SubmittedAt.HasValue;
                    }),
                    Projects = notAssignedProjects.OrderBy(p => p.Title).ToList()
                });
            }

            foreach (var businessArea in businessAreas)
            {
                var areaProjects = allProjects.Where(p => p.BusinessAreaLookup?.Id == businessArea.Id).ToList();
                var submittedProjects = areaProjects.Where(p =>
                {
                    var update = p.MonthlyUpdates?.FirstOrDefault(u => u.Year == filterYear && u.Month == filterMonth);
                    return update != null && update.SubmittedAt.HasValue;
                }).ToList();

                businessAreaGroups.Add(businessArea.Name, new BusinessAreaMonthlyData
                {
                    BusinessAreaName = businessArea.Name,
                    TotalProjects = areaProjects.Count,
                    SubmittedCount = submittedProjects.Count,
                    Projects = areaProjects.OrderBy(p => p.Title).ToList()
                });
            }

            ViewBag.PeriodStats = periodStats;
            ViewBag.StatusGroups = statusGroups;
            ViewBag.BusinessAreaGroups = businessAreaGroups;
            ViewBag.ReportYear = filterYear;
            ViewBag.ReportMonth = filterMonth;
            ViewBag.MonthName = new DateTime(filterYear, filterMonth, 1).ToString("MMMM yyyy");
            ViewBag.DueDate = dueDate;

            var previousMonthDate = new DateTime(filterYear, filterMonth, 1).AddMonths(-1);
            ViewBag.PreviousYear = previousMonthDate.Year;
            ViewBag.PreviousMonth = previousMonthDate.Month;
            ViewBag.HasPreviousMonth = true;

            var nextMonthDate = new DateTime(filterYear, filterMonth, 1).AddMonths(1);
            var nextMonthYear = nextMonthDate.Year;
            var nextMonthMonth = nextMonthDate.Month;
            var nextMonthAllowed = nextMonthYear < reportYear ||
                                  (nextMonthYear == reportYear && nextMonthMonth <= reportMonth);
            ViewBag.NextYear = nextMonthYear;
            ViewBag.NextMonth = nextMonthMonth;
            ViewBag.HasNextMonth = nextMonthAllowed;

            ViewBag.MainNavSection = "work";
            ViewBag.SubNavItem = "work-dashboard";

            return View("~/Views/Modern/Reporting/MonthlyUpdateOverview.cshtml");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading modern monthly update overview");
            TempData["ErrorMessage"] = "An error occurred while loading reporting compliance. Please try again.";
            ViewBag.MainNavSection = "work";
            ViewBag.SubNavItem = "work-dashboard";
            return View("~/Views/Modern/Reporting/MonthlyUpdateOverview.cshtml");
        }
    }
}
