using Compass.Attributes;
using Compass.Controllers;
using Compass.Data;
using Compass.Models;
using Compass.Services;
using Compass.Services.Aiss;
using Compass.ViewModels;
using Compass.ViewModels.Modern;
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
    private readonly ModernMonthlyReportService _monthlyReportService;
    private readonly ModernRaidReportingService _raidReportingService;
    private readonly CommissionReportingAnalyticsService _commissionReportingAnalytics;
    private readonly IServiceAssessmentApiService _serviceAssessmentApi;
    private readonly IAissSummaryService _aissSummary;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ModernReportingController> _logger;

    public ModernReportingController(
        CompassDbContext context,
        IMonthlyUpdateService monthlyUpdateService,
        ModernMonthlyReportService monthlyReportService,
        ModernRaidReportingService raidReportingService,
        CommissionReportingAnalyticsService commissionReportingAnalytics,
        IServiceAssessmentApiService serviceAssessmentApi,
        IAissSummaryService aissSummary,
        IConfiguration configuration,
        ILogger<ModernReportingController> logger)
    {
        _context = context;
        _monthlyUpdateService = monthlyUpdateService;
        _monthlyReportService = monthlyReportService;
        _raidReportingService = raidReportingService;
        _commissionReportingAnalytics = commissionReportingAnalytics;
        _serviceAssessmentApi = serviceAssessmentApi;
        _aissSummary = aissSummary;
        _configuration = configuration;
        _logger = logger;
    }

    private void SetNav(string subNavItem)
    {
        ViewBag.MainNavSection = "reporting";
        ViewBag.SubNavItem = subNavItem;
    }

    /// <summary>Landing URL — redirects to Dashboard.</summary>
    [HttpGet("")]
    public IActionResult Index() => RedirectToAction(nameof(Dashboard));

    /// <summary>Reporting dashboard — entry points to report areas.</summary>
    [HttpGet("dashboard")]
    public IActionResult Dashboard()
    {
        SetNav("reporting-dashboard");
        return View("~/Views/Modern/Reporting/Dashboard.cshtml", new ModernReportingDashboardViewModel());
    }

    /// <summary>Monthly reporting dashboard — portfolio health, milestones, RAG/priority, and business area summary.</summary>
    [HttpGet("monthly-update")]
    public async Task<IActionResult> MonthlyUpdate(int? year, int? month, int? businessAreaId, int? directorateId)
    {
        try
        {
            var model = await _monthlyReportService.BuildDashboardAsync(year, month, businessAreaId, directorateId);
            SetNav("reporting-monthly");
            return View("~/Views/Modern/Reporting/MonthlyUpdate.cshtml", model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading modern monthly report dashboard");
            TempData["ErrorMessage"] = "An error occurred while loading the monthly report. Please try again.";
            SetNav("reporting-monthly");
            return View("~/Views/Modern/Reporting/MonthlyUpdate.cshtml", new ModernMonthlyReportDashboardViewModel
            {
                MinReportYear = 2026,
                MaxReportYear = Math.Max(2026, DateTime.UtcNow.Year)
            });
        }
    }

    /// <summary>Full monthly update overview with stats (existing logic).</summary>
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

            SetNav("reporting-monthly");

            return View("~/Views/Modern/Reporting/MonthlyUpdateOverview.cshtml");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading modern monthly update overview");
            TempData["ErrorMessage"] = "An error occurred while loading reporting compliance. Please try again.";
            SetNav("reporting-monthly");
            return View("~/Views/Modern/Reporting/MonthlyUpdateOverview.cshtml");
        }
    }

    /// <summary>Performance commissions — completed rounds, return rates, metric completion, and business area breakdown (catalogue scope).</summary>
    [HttpGet("performance")]
    public async Task<IActionResult> Performance(int? commissionId, int? businessAreaId, int? directorateId, CancellationToken cancellationToken = default)
    {
        try
        {
            var model = await _commissionReportingAnalytics.BuildPerformancePageAsync(commissionId, businessAreaId, directorateId, cancellationToken);
            SetNav("reporting-performance");
            return View("~/Views/Modern/Reporting/Performance.cshtml", model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading performance commission reporting");
            TempData["ErrorMessage"] = "Could not load performance commission reporting. Please try again.";
            SetNav("reporting-performance");
            return View("~/Views/Modern/Reporting/Performance.cshtml", new ModernReportingPerformancePageViewModel());
        }
    }

    /// <summary>Published service assessments from SAS: portfolio summary, published list, and actions by standard.</summary>
    [HttpGet("assessments")]
    public async Task<IActionResult> Assessments(CancellationToken cancellationToken = default)
    {
        try
        {
            var summaryTask = _serviceAssessmentApi.GetPublishedSummaryAsync(cancellationToken);
            var byStandardTask = _serviceAssessmentApi.GetPublishedActionsByStandardAsync(cancellationToken);
            var allWithActionsTask = _serviceAssessmentApi.GetActionsByStandardAsync();
            await Task.WhenAll(summaryTask, byStandardTask, allWithActionsTask);

            var summary = await summaryTask;
            var byStandard = await byStandardTask;
            var allWithActions = await allWithActionsTask;

            var reportBase = (_configuration["FipsSync:Sas:ReportBaseUrl"] ?? "https://service-assessments.education.gov.uk/reports/report")
                .TrimEnd('/');

            var vm = new ModernServiceAssessmentsReportViewModel
            {
                SummaryLoadFailed = summary is null,
                SasReportBaseUrl = reportBase
            };

            if (summary?.Summaries != null)
            {
                var s = summary.Summaries;
                vm.TotalAssessments = s.TotalAssessments;
                vm.ByType = SortCountDictionary(s.ByType);
                vm.ByPhase = OrderPhases(s.ByPhase);
                vm.ByYear = SortCountDictionary(s.ByYear, yearSort: true, yearAscending: true);
            }

            if (summary?.Assessments is { Count: > 0 } list)
            {
                vm.ByOutcome = CountOutcomesForServiceAssessmentsOnly(list);

                var rows = list
                    .Select(a => new SasAssessmentListItem
                    {
                        AssessmentId = a.AssessmentID,
                        Name = a.Name,
                        Type = a.Type,
                        Phase = a.Phase,
                        Outcome = a.Outcome,
                        Portfolio = a.Portfolio,
                        AssessmentDate = a.AssessmentDateTime
                    })
                    .OrderByDescending(x => x.AssessmentDate)
                    .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                vm.AssessmentRows = rows;
                vm.AssessmentsGroupedByType = rows
                    .GroupBy(x => string.IsNullOrWhiteSpace(x.Type) ? "Other" : x.Type!.Trim(), StringComparer.OrdinalIgnoreCase)
                    .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
                    .Select(g => new SasAssessmentsTypeGroup
                    {
                        TypeLabel = g.Key,
                        Items = g
                            .OrderByDescending(x => x.AssessmentDate)
                            .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                            .ToList()
                    })
                    .ToList();
            }

            {
                var (stdRows, outcomeDetail) = ServiceAssessmentStandardActionOutcomeBuilder.Build(
                    byStandard,
                    allWithActions,
                    summary?.Assessments);
                vm.StandardActionRows = stdRows;
                vm.ServiceStandardOutcomeBreakdownAvailable = outcomeDetail;
            }

            vm.ActionsByStandardLoadFailed = byStandard is null && allWithActions is null;

            if (vm.SummaryLoadFailed || (byStandard is null && allWithActions is null))
            {
                TempData["ErrorMessage"] =
                    "Part of the service assessment data could not be loaded. Check the SAS connection or try again.";
            }

            SetNav("reporting-assessments");
            return View("~/Views/Modern/Reporting/Assessments.cshtml", vm);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading service assessments report");
            TempData["ErrorMessage"] = "Could not load service assessments. Please try again.";
            SetNav("reporting-assessments");
            return View("~/Views/Modern/Reporting/Assessments.cshtml", new ModernServiceAssessmentsReportViewModel
            {
                SummaryLoadFailed = true,
                ActionsByStandardLoadFailed = true
            });
        }
    }

    /// <summary>AISS portfolio summary and issue trend charts (accessibility report).</summary>
    [HttpGet("accessibility")]
    [RequireCentralOpsAdmin]
    public async Task<IActionResult> Accessibility(CancellationToken cancellationToken = default)
    {
        SetNav("reporting-accessibility");
        var summaryTask = _aissSummary.GetSummaryAsync(cancellationToken);
        var trendsTask = _aissSummary.GetCriterionTrendsAsync(12, cancellationToken);
        await Task.WhenAll(summaryTask, trendsTask);
        var (summary, error) = await summaryTask;
        var (trends, trendsError) = await trendsTask;
        var vm = new ModernOperationsAccessibilityViewModel
        {
            Summary = summary,
            Trends = trends,
            ErrorMessage = error,
            TrendsError = trendsError
        };
        return View("~/Views/Modern/Reporting/Accessibility.cshtml", vm);
    }

    private static readonly string[] DeliveryPhaseOrder =
    [
        "Discovery", "Alpha", "Private Beta", "Public Beta", "Live"
    ];

    private static List<KeyValuePair<string, int>> CountOutcomesForServiceAssessmentsOnly(
        IEnumerable<SasPublishedAssessmentRow> list)
    {
        var d = list
            .Where(r => string.Equals(r.Type?.Trim(), "Service assessment", StringComparison.OrdinalIgnoreCase))
            .GroupBy(
                r => string.IsNullOrWhiteSpace(r.Outcome) ? "—" : r.Outcome!.Trim(),
                StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);
        return SortCountDictionary(d, yearSort: false);
    }

    /// <summary>Phases: Discovery, Alpha, Private Beta, Public Beta, Live, then any other known labels.</summary>
    private static List<KeyValuePair<string, int>> OrderPhases(Dictionary<string, int>? d)
    {
        if (d is null or { Count: 0 })
        {
            return new List<KeyValuePair<string, int>>();
        }

        var result = new List<KeyValuePair<string, int>>();
        var matchedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var canonical in DeliveryPhaseOrder)
        {
            var p = d.FirstOrDefault(
                x => !matchedKeys.Contains(x.Key) &&
                     string.Equals(x.Key.Trim(), canonical, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(p.Key))
            {
                result.Add(new KeyValuePair<string, int>(canonical, p.Value));
                matchedKeys.Add(p.Key);
            }
        }

        foreach (var p in d.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
        {
            if (matchedKeys.Contains(p.Key)) continue;
            result.Add(p);
            matchedKeys.Add(p.Key);
        }

        return result;
    }

    private static List<KeyValuePair<string, int>> SortCountDictionary(
        Dictionary<string, int>? d,
        bool yearSort = false,
        bool yearAscending = true)
    {
        if (d is null or { Count: 0 })
        {
            return new List<KeyValuePair<string, int>>();
        }

        if (yearSort)
        {
            return yearAscending
                ? d.OrderBy(p => int.TryParse(p.Key, out var y) ? y : 9999).ToList()
                : d.OrderByDescending(p => int.TryParse(p.Key, out var y) ? y : 0).ToList();
        }

        return d
            .OrderByDescending(p => p.Value)
            .ThenBy(p => p.Key, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>Canonical detail URL redirects to the unified performance report with <c>commissionId</c>.</summary>
    [HttpGet("performance/{commissionId:int}")]
    public IActionResult PerformanceCommission(int commissionId)
    {
        return RedirectToAction(nameof(Performance), new { commissionId });
    }

    /// <summary>Legacy URL — consolidated into <see cref="Raid"/>.</summary>
    [HttpGet("risk")]
    public IActionResult Risk() => RedirectToAction(nameof(Raid), new { tab = "risks" });

    /// <summary>RAID analytics — risk/issue heatmaps, trends, and portfolio signals.</summary>
    [HttpGet("raid")]
    public async Task<IActionResult> Raid(
        string? tab,
        string? riskIntel,
        int? businessAreaId,
        int? directorateId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var model = await _raidReportingService.BuildAsync(
                tab,
                riskIntel,
                businessAreaId,
                directorateId,
                _context,
                cancellationToken);
            SetNav("reporting-raid");
            return View("~/Views/Modern/Reporting/Raid.cshtml", model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading RAID reporting dashboard");
            TempData["ErrorMessage"] = "Could not load RAID reporting. Please try again.";
            SetNav("reporting-raid");
            return View("~/Views/Modern/Reporting/Raid.cshtml", new ModernRaidReportingViewModel());
        }
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
}
