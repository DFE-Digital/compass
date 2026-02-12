using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Compass.Attributes;
using Compass.Data;
using Compass.Models;
using Compass.Services;
using Compass.ViewModels;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using System.Net;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Compass.Controllers;

public class CommissionProductMetricViewModel
{
    public ProductDto Product { get; set; } = new();
    public PerformanceMetric Metric { get; set; } = new();
    public CommissionMetricValue? MetricValue { get; set; }
    public CommissionSubmission? Submission { get; set; }
}

public class BusinessAreaCommissionCompletion
{
    public string BusinessArea { get; set; } = string.Empty;
    public int TotalProducts { get; set; }
    public int CompletedProducts { get; set; }
    public int InProgressProducts { get; set; }
    public int NotStartedProducts { get; set; }
    public int LateProducts { get; set; }
    public decimal CompletionPercentage { get; set; }
}

public class PrioritySummaryItem
{
    public string Priority { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class RagSummaryItem
{
    public string Rag { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class BusinessAreaSummaryItem
{
    public string BusinessArea { get; set; } = string.Empty;
    public int Count { get; set; }
    public int RedCount { get; set; }
    public int AmberRedCount { get; set; }
    public int AmberCount { get; set; }
    public int AmberGreenCount { get; set; }
    public int GreenCount { get; set; }
    public int RagNotSetCount { get; set; }
    public int CriticalPriorityCount { get; set; }
    public int HighPriorityCount { get; set; }
    public int MediumPriorityCount { get; set; }
    public int LowPriorityCount { get; set; }
    public int PriorityNotSetCount { get; set; }
    public int BlockedCount { get; set; }
    public double PreviousMonthSubmittedPercent { get; set; }
    public double CurrentMonthSubmittedPercent { get; set; }
    public int PreviousMonthSubmitted { get; set; }
    public int CurrentMonthSubmitted { get; set; }
}

public class MonthlyUpdateStats
{
    public int Year { get; set; }
    public int Month { get; set; }
    public int TotalProjects { get; set; }
    public int Submitted { get; set; }
    public int NotStarted { get; set; }
    public int InProgress { get; set; }
    public int Late { get; set; }
    public DateTime DueDate { get; set; }
}

public class BusinessAreaMonthlyUpdateStats
{
    public string BusinessArea { get; set; } = string.Empty;
    public int TotalProjects { get; set; }
    public int PreviousMonthSubmitted { get; set; }
    public double PreviousMonthSubmittedPercent { get; set; }
    public int CurrentMonthSubmitted { get; set; }
    public double CurrentMonthSubmittedPercent { get; set; }
}

public class BusinessAreaMonthlyData
{
    public string BusinessAreaName { get; set; } = string.Empty;
    public int TotalProjects { get; set; }
    public int SubmittedCount { get; set; }
    public List<Project> Projects { get; set; } = new List<Project>();
}

public class BusinessAreaRiskSummary
{
    public string BusinessAreaName { get; set; } = string.Empty;
    public int TotalProjects { get; set; }
    public int RedCount { get; set; }
    public int AmberRedCount { get; set; }
    public int CriticalPriorityCount { get; set; }
    public int HighPriorityCount { get; set; }
    public int DelayedMilestonesCount { get; set; }
    
    // Computed properties for backward compatibility
    public int RedAmberRedCount => RedCount + AmberRedCount;
    public int CriticalHighPriorityCount => CriticalPriorityCount + HighPriorityCount;
}

public class RagExceptionReport
{
    public int ProjectId { get; set; }
    public string ProjectCode { get; set; } = string.Empty;
    public string ProjectTitle { get; set; } = string.Empty;
    public string? BusinessArea { get; set; }
    public string? PrimaryContact { get; set; }
    public string RagStatus { get; set; } = string.Empty;
    public string ExceptionType { get; set; } = string.Empty;
    public string ExceptionDescription { get; set; } = string.Empty;
    public int OverdueMilestonesCount { get; set; }
    public int DelayedMilestonesCount { get; set; }
    public int AtRiskMilestonesCount { get; set; }
    public int TotalMilestonesCount { get; set; }
    public int OnTrackMilestonesCount { get; set; }
    public int CompleteMilestonesCount { get; set; }
}

public class MilestoneWithProject
{
    public Compass.Models.Milestone Milestone { get; set; } = null!;
    public Compass.Models.Project Project { get; set; } = null!;
}

public class RiskWithProject
{
    public Compass.Models.Risk Risk { get; set; } = null!;
    public Compass.Models.Project Project { get; set; } = null!;
}

public class IssueWithProject
{
    public Compass.Models.Issue Issue { get; set; } = null!;
    public Compass.Models.Project Project { get; set; } = null!;
}

public class PortfolioSummaryViewModel
{
    public int TotalProjects { get; set; }
    public int ActiveProjects { get; set; }
    public int PausedProjects { get; set; }
    public int CompletedProjects { get; set; }
    public int CancelledProjects { get; set; }
    public int RedProjects { get; set; }
    public int AmberRedProjects { get; set; }
    public int AmberProjects { get; set; }
    public int AmberGreenProjects { get; set; }
    public int GreenProjects { get; set; }
    public int CriticalPriorityProjects { get; set; }
    public int HighPriorityProjects { get; set; }
    public int MediumPriorityProjects { get; set; }
    public int LowPriorityProjects { get; set; }
    public int PriorityNotSetProjects { get; set; }
    public int TotalMilestones { get; set; }
    public int CompletedMilestones { get; set; }
    public int TotalRisks { get; set; }
    public int OpenRisks { get; set; }
    public int TotalIssues { get; set; }
    public int OpenIssues { get; set; }
}

public class BusinessAreaSummaryViewModel
{
    public string BusinessArea { get; set; } = string.Empty;
    public int? BusinessAreaId { get; set; }
    public int TotalProjects { get; set; }
    public int NewThisMonth { get; set; }
    public int MilestonesAchieved { get; set; }
    public int UpcomingMilestones { get; set; }
    public int LateMilestones { get; set; }
    public int RedCount { get; set; }
    public int AmberRedCount { get; set; }
    public int AmberCount { get; set; }
    public int AmberGreenCount { get; set; }
    public int GreenCount { get; set; }
    public int CriticalCount { get; set; }
    public int HighCount { get; set; }
    public int MediumCount { get; set; }
    public int LowCount { get; set; }
    public MonthlyUpdateStats? MonthlyUpdateStats { get; set; }
}

public class BulkUpdateRequest
{
    [Required]
    public List<int> ProjectIds { get; set; } = new();
    public string? PrimaryContactObjectId { get; set; }
    public string? PrimaryContactEmail { get; set; }
    public string? PrimaryContactName { get; set; }
    public bool ClearPrimaryContact { get; set; }
    public string? SroObjectId { get; set; }
    public string? SroEmail { get; set; }
    public string? SroName { get; set; }
    public bool ClearSro { get; set; }
    public string? BusinessArea { get; set; }
    public bool ClearBusinessArea { get; set; }
    public string? Priority { get; set; }
    public bool ClearPriority { get; set; }
    public List<int>? DirectorateIds { get; set; }
    public bool ClearDirectorates { get; set; }
}


[Authorize]
[RequireCentralOpsAdmin]
public class CentralOpsController : Controller
{
    private readonly CompassDbContext _context;
    private readonly ILogger<CentralOpsController> _logger;
    private readonly IUserDirectoryService _userDirectoryService;
    private readonly IMonthlyUpdateService _monthlyUpdateService;
    private readonly IProductsApiService _productsApiService;

    public CentralOpsController(
        CompassDbContext context,
        ILogger<CentralOpsController> logger,
        IUserDirectoryService userDirectoryService,
        IMonthlyUpdateService monthlyUpdateService,
        IProductsApiService productsApiService)
    {
        _context = context;
        _logger = logger;
        _userDirectoryService = userDirectoryService;
        _monthlyUpdateService = monthlyUpdateService;
        _productsApiService = productsApiService;
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
            
            // If update exists and has been submitted
            if (update != null && update.SubmittedAt.HasValue)
            {
                submitted++;
            }
            // Check if the due date has passed (regardless of whether update exists)
            else if (currentDate > dueDate)
            {
                late++;
            }
            // If update exists but not submitted and due date hasn't passed
            else if (update != null && !update.SubmittedAt.HasValue)
            {
                inProgress++;
            }
            // No update exists and due date hasn't passed
            else
            {
                notStarted++;
            }
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

    private static string NormalizeRagStatus(string? ragStatus)
    {
        if (string.IsNullOrWhiteSpace(ragStatus))
        {
            return string.Empty;
        }

        // Normalize the status - handle both "Amber-Green" and "Amber/Green" formats
        // Convert to lowercase first for case-insensitive comparison, then capitalize
        var normalized = ragStatus.Trim()
            .Replace(" / ", "-")
            .Replace("/", "-")
            .Replace(" /", "-")
            .Replace("/ ", "-");
        
        // Capitalize first letter of each word for consistency (e.g., "amber-red" -> "Amber-Red")
        var parts = normalized.Split('-');
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i].Length > 0)
            {
                parts[i] = char.ToUpperInvariant(parts[i][0]) + (parts[i].Length > 1 ? parts[i].Substring(1).ToLowerInvariant() : "");
            }
        }
        
        return string.Join("-", parts);
    }


    // GET: CentralOps/Dashboard - Redirect to Summary
    public IActionResult Dashboard()
    {
        return RedirectToAction("Summary");
    }

    // GET: CentralOps/Summary
    public async Task<IActionResult> Summary()
    {
        try
        {
            // Get all projects
            var allProjects = await _context.Projects
                .Include(p => p.PrimaryContactUser)
                .Include(p => p.DeliveryPriority)
                .Include(p => p.BusinessAreaLookup)
                .Include(p => p.MonthlyUpdates)
                .Where(p => !p.IsDeleted)
                .ToListAsync();

            // Determine current reporting period using 10-day rule
            var currentDate = DateTime.UtcNow;
            var currentYear = currentDate.Year;
            var currentMonth = currentDate.Month;
            
            var currentPeriodDueDate = _monthlyUpdateService.GetMonthlyUpdateDueDate(currentYear, currentMonth);
            var daysUntilCurrentPeriodDueDate = (currentPeriodDueDate - currentDate).Days;
            
            // Apply 10-day rule: if within 10 days of current period due date, use current period
            var reportYear = daysUntilCurrentPeriodDueDate <= 10 ? currentYear : (currentMonth == 1 ? currentYear - 1 : currentYear);
            var reportMonth = daysUntilCurrentPeriodDueDate <= 10 ? currentMonth : (currentMonth == 1 ? 12 : currentMonth - 1);
            
            // Calculate stats for the current reporting period
            var currentReportingPeriodStats = CalculateMonthlyUpdateStats(allProjects, reportYear, reportMonth, _monthlyUpdateService);

            // Count active work items
            var activeWorkItems = allProjects.Count(p => p.Status == "Active");

            // Count Red and Amber-Red items separately
            var redCount = allProjects.Count(p => {
                if (string.IsNullOrEmpty(p.RagStatus)) return false;
                var normalized = NormalizeRagStatus(p.RagStatus);
                return normalized == "Red";
            });
            var amberRedCount = allProjects.Count(p => {
                if (string.IsNullOrEmpty(p.RagStatus)) return false;
                var normalized = NormalizeRagStatus(p.RagStatus);
                return normalized == "Amber-Red";
            });
            var redAndAmberRedItems = redCount + amberRedCount;

            // Count Critical and High priority items
            var criticalPriorityCount = allProjects.Count(p => p.DeliveryPriority != null && p.DeliveryPriority.Name.ToLower().Contains("critical"));
            var highPriorityCount = allProjects.Count(p => p.DeliveryPriority != null && p.DeliveryPriority.Name.ToLower().Contains("high") && !p.DeliveryPriority.Name.ToLower().Contains("critical"));

            // Count open milestones (not deleted, not complete, not cancelled)
            var openMilestones = await _context.Milestones
                .Where(m => !m.IsDeleted && m.Status != "complete" && m.Status != "cancelled")
                .CountAsync();

            // Get all milestones for delayed calculation
            var allMilestones = await _context.Milestones
                .Include(m => m.Project)
                    .ThenInclude(p => p.BusinessAreaLookup)
                .Where(m => !m.IsDeleted && m.Status != "complete" && m.Status != "cancelled")
                .ToListAsync();

            var delayedMilestones = allMilestones
                .Where(m => m.DueDate < currentDate)
                .ToList();

            // Calculate business area risk data
            var businessAreaRiskData = new List<BusinessAreaRiskSummary>();
            var businessAreas = allProjects
                .Where(p => p.BusinessAreaLookup != null)
                .Select(p => p.BusinessAreaLookup!)
                .GroupBy(ba => ba.Id)
                .Select(g => g.First())
                .ToList();

            // Add "Not assigned" if there are projects without business area
            var notAssignedProjects = allProjects.Where(p => p.BusinessAreaLookup == null).ToList();
            if (notAssignedProjects.Any())
            {
                businessAreaRiskData.Add(new BusinessAreaRiskSummary
                {
                    BusinessAreaName = "Not assigned",
                    TotalProjects = notAssignedProjects.Count,
                    RedCount = notAssignedProjects.Count(p => {
                        if (string.IsNullOrEmpty(p.RagStatus)) return false;
                        var normalized = NormalizeRagStatus(p.RagStatus);
                        return normalized == "Red";
                    }),
                    AmberRedCount = notAssignedProjects.Count(p => {
                        if (string.IsNullOrEmpty(p.RagStatus)) return false;
                        var normalized = NormalizeRagStatus(p.RagStatus);
                        return normalized == "Amber-Red";
                    }),
                    CriticalPriorityCount = notAssignedProjects.Count(p => 
                        p.DeliveryPriority != null && p.DeliveryPriority.Name.ToLower().Contains("critical")),
                    HighPriorityCount = notAssignedProjects.Count(p => 
                        p.DeliveryPriority != null && 
                        p.DeliveryPriority.Name.ToLower().Contains("high") && 
                        !p.DeliveryPriority.Name.ToLower().Contains("critical")),
                    DelayedMilestonesCount = delayedMilestones.Count(m => m.Project?.BusinessAreaLookup == null)
                });
            }

            foreach (var businessArea in businessAreas)
            {
                var areaProjects = allProjects.Where(p => p.BusinessAreaLookup?.Id == businessArea.Id).ToList();
                var areaProjectIds = areaProjects.Select(p => p.Id).ToList();
                
                businessAreaRiskData.Add(new BusinessAreaRiskSummary
                {
                    BusinessAreaName = businessArea.Name,
                    TotalProjects = areaProjects.Count,
                    RedCount = areaProjects.Count(p => {
                        if (string.IsNullOrEmpty(p.RagStatus)) return false;
                        var normalized = NormalizeRagStatus(p.RagStatus);
                        return normalized == "Red";
                    }),
                    AmberRedCount = areaProjects.Count(p => {
                        if (string.IsNullOrEmpty(p.RagStatus)) return false;
                        var normalized = NormalizeRagStatus(p.RagStatus);
                        return normalized == "Amber-Red";
                    }),
                    CriticalPriorityCount = areaProjects.Count(p => 
                        p.DeliveryPriority != null && p.DeliveryPriority.Name.ToLower().Contains("critical")),
                    HighPriorityCount = areaProjects.Count(p => 
                        p.DeliveryPriority != null && 
                        p.DeliveryPriority.Name.ToLower().Contains("high") && 
                        !p.DeliveryPriority.Name.ToLower().Contains("critical")),
                    DelayedMilestonesCount = delayedMilestones.Count(m => m.ProjectId.HasValue && areaProjectIds.Contains(m.ProjectId.Value))
                });
            }

            // Order by risk score (Red/Amber-Red count + Critical/High Priority count + Delayed Milestones count)
            businessAreaRiskData = businessAreaRiskData
                .OrderByDescending(ba => ba.RedAmberRedCount + ba.CriticalHighPriorityCount + ba.DelayedMilestonesCount)
                .ThenByDescending(ba => ba.RedCount)
                .ThenByDescending(ba => ba.AmberRedCount)
                .ThenByDescending(ba => ba.CriticalPriorityCount)
                .ThenByDescending(ba => ba.HighPriorityCount)
                .ThenByDescending(ba => ba.DelayedMilestonesCount)
                .ToList();

            ViewBag.ActiveWorkItems = activeWorkItems;
            ViewBag.TotalWorkItems = allProjects.Count;
            ViewBag.RedAndAmberRedItems = redAndAmberRedItems;
            ViewBag.RedCount = redCount;
            ViewBag.AmberRedCount = amberRedCount;
            ViewBag.CriticalPriorityCount = criticalPriorityCount;
            ViewBag.HighPriorityCount = highPriorityCount;
            ViewBag.OpenMilestones = openMilestones;
            ViewBag.BusinessAreaRiskData = businessAreaRiskData;
            ViewBag.CurrentReportingPeriodStats = currentReportingPeriodStats;
            ViewBag.ReportYear = reportYear;
            ViewBag.ReportMonth = reportMonth;
            ViewBag.MonthName = new DateTime(reportYear, reportMonth, 1).ToString("MMMM yyyy");

            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading Central Operations summary");
            TempData["ErrorMessage"] = "An error occurred while loading the summary. Please try again.";
            return View();
        }
    }

    // GET: CentralOps/MonthlyReporting
    public async Task<IActionResult> MonthlyReporting(int? year, int? month, string? status)
    {
        try
        {
            // Get all active projects with related data (exclude Cancelled and Completed)
            var allProjects = await _context.Projects
                .Include(p => p.MonthlyUpdates)
                .Include(p => p.PrimaryContactUser)
                .Include(p => p.DeliveryPriority)
                .Include(p => p.BusinessAreaLookup)
                .Include(p => p.ServiceOwners)
                    .ThenInclude(so => so.User)
                .Where(p => !p.IsDeleted && p.Status != "Cancelled" && p.Status != "Completed")
                .ToListAsync();

            // Determine current reporting period using 10-day rule
            var currentDate = DateTime.UtcNow;
            var currentYear = currentDate.Year;
            var currentMonth = currentDate.Month;
            
            var currentPeriodDueDate = _monthlyUpdateService.GetMonthlyUpdateDueDate(currentYear, currentMonth);
            var daysUntilCurrentPeriodDueDate = (currentPeriodDueDate - currentDate).Days;
            
            // Apply 10-day rule: if within 10 days of current period due date, use current period
            var reportYear = daysUntilCurrentPeriodDueDate <= 10 ? currentYear : (currentMonth == 1 ? currentYear - 1 : currentYear);
            var reportMonth = daysUntilCurrentPeriodDueDate <= 10 ? currentMonth : (currentMonth == 1 ? 12 : currentMonth - 1);

            // Use provided year/month if specified, otherwise use determined period
            var filterYear = year ?? reportYear;
            var filterMonth = month ?? reportMonth;
            
            var dueDate = _monthlyUpdateService.GetMonthlyUpdateDueDate(filterYear, filterMonth);

            // Calculate stats for the reporting period
            var periodStats = CalculateMonthlyUpdateStats(allProjects, filterYear, filterMonth, _monthlyUpdateService);

            // Group projects by status for the current reporting period
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

                // If update exists and has been submitted
                if (update != null && update.SubmittedAt.HasValue)
                {
                    projectStatus = "Submitted";
                }
                // Check if the due date has passed (regardless of whether update exists)
                else if (currentDate > dueDate)
                {
                    projectStatus = "Late";
                }
                // If update exists but not submitted and due date hasn't passed
                else if (update != null && !update.SubmittedAt.HasValue)
                {
                    projectStatus = "In Progress";
                }
                // No update exists and due date hasn't passed
                else
                {
                    projectStatus = "Not Started";
                }

                if (statusGroups.ContainsKey(projectStatus))
                {
                    statusGroups[projectStatus].Add(project);
                }
            }

            // Sort projects within each group
            foreach (var group in statusGroups.Values)
            {
                group.Sort((a, b) => string.Compare(a.Title, b.Title, StringComparison.OrdinalIgnoreCase));
            }

            // Group projects by business area for the current reporting period
            var businessAreaGroups = new Dictionary<string, BusinessAreaMonthlyData>();
            
            // Get all business areas
            var businessAreas = allProjects
                .Where(p => p.BusinessAreaLookup != null)
                .Select(p => p.BusinessAreaLookup!)
                .GroupBy(ba => ba.Id)
                .Select(g => g.First())
                .ToList();

            // Add "Not assigned" if there are projects without business area
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

            // Calculate previous and next months for navigation
            var previousMonthDate = new DateTime(filterYear, filterMonth, 1).AddMonths(-1);
            ViewBag.PreviousYear = previousMonthDate.Year;
            ViewBag.PreviousMonth = previousMonthDate.Month;
            ViewBag.HasPreviousMonth = true; // Always allow going back

            // Calculate next month, but only allow if it's not beyond the current reporting period
            var nextMonthDate = new DateTime(filterYear, filterMonth, 1).AddMonths(1);
            var nextMonthYear = nextMonthDate.Year;
            var nextMonthMonth = nextMonthDate.Month;
            
            // Allow next month if it's <= current reporting period
            var nextMonthAllowed = nextMonthYear < reportYear || 
                                  (nextMonthYear == reportYear && nextMonthMonth <= reportMonth);
            
            ViewBag.NextYear = nextMonthYear;
            ViewBag.NextMonth = nextMonthMonth;
            ViewBag.HasNextMonth = nextMonthAllowed;

            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading monthly reporting");
            TempData["ErrorMessage"] = "An error occurred while loading monthly reporting. Please try again.";
            return View();
        }
    }

    // GET: CentralOps/GenerateMonthlyReport
    public async Task<IActionResult> GenerateMonthlyReport(int? year, int? month, string? format)
    {
        try
        {
            // Determine reporting period
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
            var monthName = new DateTime(filterYear, filterMonth, 1).ToString("MMMM yyyy");

            // Get all active projects with submitted monthly updates for the period (exclude Cancelled and Completed)
            var projectsWithUpdates = await _context.Projects
                .Include(p => p.MonthlyUpdates)
                    .ThenInclude(mu => mu.MonthlyUpdateNarratives)
                .Include(p => p.BusinessAreaLookup)
                .Include(p => p.PrimaryContactUser)
                .Include(p => p.DeliveryPriority)
                .Include(p => p.ServiceOwners)
                    .ThenInclude(so => so.User)
                .Where(p => !p.IsDeleted && p.Status != "Cancelled" && p.Status != "Completed")
                .Select(p => new
                {
                    Project = p,
                    MonthlyUpdate = p.MonthlyUpdates.FirstOrDefault(u => u.Year == filterYear && u.Month == filterMonth && u.SubmittedAt.HasValue)
                })
                .Where(x => x.MonthlyUpdate != null)
                .OrderBy(x => x.Project.Title)
                .ToListAsync();

            var reportItems = projectsWithUpdates.Select(x => new ViewModels.MonthlyReportItem
            {
                ProjectId = x.Project.Id,
                ProjectCode = $"DFE-DDT-{x.Project.Id}",
                ProjectTitle = x.Project.Title,
                BusinessArea = x.Project.BusinessAreaLookup?.Name ?? "Not assigned",
                PrimaryContact = x.Project.PrimaryContactUser?.Name ?? "-",
                ServiceOwner = x.Project.ServiceOwners?.FirstOrDefault()?.User?.Name ?? "-",
                RagStatus = NormalizeRagStatus(x.Project.RagStatus) ?? "Not set",
                UpdateNarratives = x.MonthlyUpdate!.MonthlyUpdateNarratives
                    .OrderBy(n => n.CreatedAt)
                    .Select(n => new ViewModels.MonthlyReportNarrative
                    {
                        Narrative = n.Narrative,
                        SubmittedBy = !string.IsNullOrEmpty(n.CreatedByName) ? n.CreatedByName : n.CreatedByUser?.Name ?? "Unknown",
                        SubmittedByEmail = n.CreatedByEmail ?? n.CreatedByUser?.Email ?? "",
                        SubmittedAt = n.CreatedAt
                    })
                    .ToList(),
                SubmittedAt = x.MonthlyUpdate.SubmittedAt!.Value,
                SubmittedBy = !string.IsNullOrEmpty(x.MonthlyUpdate.CreatedByName) 
                    ? x.MonthlyUpdate.CreatedByName 
                    : x.MonthlyUpdate.CreatedByUser?.Name ?? "Unknown",
                SubmittedByEmail = x.MonthlyUpdate.CreatedByEmail ?? x.MonthlyUpdate.CreatedByUser?.Email ?? ""
            }).ToList();

            ViewBag.ReportYear = filterYear;
            ViewBag.ReportMonth = filterMonth;
            ViewBag.MonthName = monthName;
            ViewBag.DueDate = dueDate;
            ViewBag.TotalSubmissions = reportItems.Count;

            // Calculate previous and next months for navigation
            var previousMonthDate = new DateTime(filterYear, filterMonth, 1).AddMonths(-1);
            ViewBag.PreviousYear = previousMonthDate.Year;
            ViewBag.PreviousMonth = previousMonthDate.Month;
            ViewBag.HasPreviousMonth = true; // Always allow going back

            // Calculate next month, but only allow if it's not beyond the current reporting period
            var nextMonthDate = new DateTime(filterYear, filterMonth, 1).AddMonths(1);
            var nextMonthYear = nextMonthDate.Year;
            var nextMonthMonth = nextMonthDate.Month;
            
            // Allow next month if it's <= current reporting period
            var nextMonthAllowed = nextMonthYear < reportYear || 
                                  (nextMonthYear == reportYear && nextMonthMonth <= reportMonth);
            
            ViewBag.NextYear = nextMonthYear;
            ViewBag.NextMonth = nextMonthMonth;
            ViewBag.HasNextMonth = nextMonthAllowed;

            // If Excel format requested, generate Excel document
            if (format?.ToLower() == "excel")
            {
                return await GenerateMonthlyReportExcel(reportItems, monthName, filterYear, filterMonth);
            }

            return View(reportItems);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating monthly report");
            TempData["ErrorMessage"] = "An error occurred while generating the monthly report. Please try again.";
            return RedirectToAction("MonthlyReporting");
        }
    }

    private async Task<IActionResult> GenerateMonthlyReportExcel(List<ViewModels.MonthlyReportItem> reportItems, string monthName, int year, int month)
    {
        try
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Monthly Report");

            // Add header information
            worksheet.Cell(1, 1).Value = "Monthly Update Report";
            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(1, 1).Style.Font.FontSize = 16;
            worksheet.Cell(1, 1).Style.Font.FontColor = XLColor.FromHtml("#005ea5");
            
            worksheet.Cell(2, 1).Value = $"Reporting Period: {monthName}";
            worksheet.Cell(3, 1).Value = $"Report Generated: {DateTime.UtcNow:dd MMMM yyyy HH:mm} UTC";
            worksheet.Cell(4, 1).Value = $"Total Submissions: {reportItems.Count}";

            // Add table headers - all data on one row
            var headerRow = 6;
            var headers = new[]
            {
                "Project Code",
                "Project Title",
                "Business Area",
                "RAG Status",
                "Primary Contact",
                "Service Owner",
                "Submitted By",
                "Submitted By Email",
                "Submitted At",
                "Update Narratives"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = worksheet.Cell(headerRow, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#f8f9fa");
                cell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            }

            // Add data - all information on one row per project
            var currentRow = headerRow + 1;
            foreach (var item in reportItems)
            {
                worksheet.Cell(currentRow, 1).Value = item.ProjectCode;
                worksheet.Cell(currentRow, 2).Value = item.ProjectTitle;
                worksheet.Cell(currentRow, 3).Value = item.BusinessArea;
                worksheet.Cell(currentRow, 4).Value = item.RagStatus;
                worksheet.Cell(currentRow, 5).Value = item.PrimaryContact;
                worksheet.Cell(currentRow, 6).Value = item.ServiceOwner;
                worksheet.Cell(currentRow, 7).Value = item.SubmittedBy;
                worksheet.Cell(currentRow, 8).Value = item.SubmittedByEmail;
                worksheet.Cell(currentRow, 9).Value = item.SubmittedAt;
                worksheet.Cell(currentRow, 9).Style.DateFormat.Format = "dd MMM yyyy HH:mm";
                
                // Combine all narratives into one cell
                if (item.UpdateNarratives.Any())
                {
                    var narrativesText = new System.Text.StringBuilder();
                    foreach (var narrative in item.UpdateNarratives)
                    {
                        if (narrativesText.Length > 0)
                        {
                            narrativesText.AppendLine();
                            narrativesText.AppendLine("---");
                            narrativesText.AppendLine();
                        }
                        
                        narrativesText.AppendLine(narrative.Narrative);
                        
                        var narrativeMeta = !string.IsNullOrEmpty(narrative.SubmittedByEmail)
                            ? $"Submitted by {narrative.SubmittedBy} ({narrative.SubmittedByEmail}) on {narrative.SubmittedAt:dd MMM yyyy HH:mm} UTC"
                            : $"Submitted by {narrative.SubmittedBy} on {narrative.SubmittedAt:dd MMM yyyy HH:mm} UTC";
                        narrativesText.AppendLine();
                        narrativesText.Append($"({narrativeMeta})");
                    }
                    worksheet.Cell(currentRow, 10).Value = narrativesText.ToString();
                }
                else
                {
                    worksheet.Cell(currentRow, 10).Value = "No narratives provided.";
                }
                
                // Enable text wrapping for narratives column
                worksheet.Cell(currentRow, 10).Style.Alignment.WrapText = true;
                
                currentRow++;
            }

            // Auto-fit columns
            worksheet.Columns().AdjustToContents();
            worksheet.Column(2).Style.Alignment.WrapText = true; // Project Title
            worksheet.Column(10).Style.Alignment.WrapText = true; // Update Narratives
            worksheet.SheetView.FreezeRows(headerRow);

            // Save to memory stream
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var fileName = $"Monthly_Report_{year}_{month:00}_{DateTime.UtcNow:yyyyMMdd-HHmmss}.xlsx";
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating Excel document");
            TempData["ErrorMessage"] = "An error occurred while generating the Excel document. Please try again.";
            return RedirectToAction("GenerateMonthlyReport", new { year, month });
        }
    }

    // GET: CentralOps/RagAndRisk
    public async Task<IActionResult> RagAndRisk()
    {
        try
        {
            // Get all projects with milestones and related data
            var allProjects = await _context.Projects
                .Include(p => p.Milestones)
                .Include(p => p.BusinessAreaLookup)
                .Include(p => p.PrimaryContactUser)
                .Include(p => p.DeliveryPriority)
                .Include(p => p.ServiceOwners)
                    .ThenInclude(so => so.User)
                .Where(p => !p.IsDeleted)
                .ToListAsync();

            // Count projects by RAG status and group projects
            var ragCounts = new Dictionary<string, int>
            {
                { "Red", 0 },
                { "Amber-Red", 0 },
                { "Amber", 0 },
                { "Amber-Green", 0 },
                { "Green", 0 },
                { "Not set", 0 }
            };

            var ragGroups = new Dictionary<string, List<Project>>
            {
                { "Red", new List<Project>() },
                { "Amber-Red", new List<Project>() },
                { "Amber", new List<Project>() },
                { "Amber-Green", new List<Project>() },
                { "Green", new List<Project>() },
                { "Not set", new List<Project>() }
            };

            foreach (var project in allProjects)
            {
                var normalized = NormalizeRagStatus(project.RagStatus);
                if (string.IsNullOrEmpty(normalized))
                {
                    ragCounts["Not set"]++;
                    ragGroups["Not set"].Add(project);
                }
                else if (ragCounts.ContainsKey(normalized))
                {
                    ragCounts[normalized]++;
                    ragGroups[normalized].Add(project);
                }
            }

            // Sort projects within each group
            foreach (var group in ragGroups.Values)
            {
                group.Sort((a, b) => string.Compare(a.Title, b.Title, StringComparison.OrdinalIgnoreCase));
            }

            // Calculate exception reporting
            var exceptions = CalculateRagExceptions(allProjects);

            ViewBag.RagCounts = ragCounts;
            ViewBag.RagGroups = ragGroups;
            ViewBag.TotalProjects = allProjects.Count;
            ViewBag.Exceptions = exceptions;

            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading RAG and Risk");
            TempData["ErrorMessage"] = "An error occurred while loading RAG and Risk. Please try again.";
            return View();
        }
    }

    private List<RagExceptionReport> CalculateRagExceptions(List<Project> projects)
    {
        var exceptions = new List<RagExceptionReport>();
        var currentDate = DateTime.UtcNow.Date;

        foreach (var project in projects)
        {
            var activeMilestones = project.Milestones?
                .Where(m => !m.IsDeleted && m.Status != "complete" && m.Status != "cancelled")
                .ToList() ?? new List<Milestone>();

            if (!activeMilestones.Any())
            {
                // No active milestones, skip exception checking
                continue;
            }

            var overdueMilestones = activeMilestones
                .Where(m => m.DueDate.Date < currentDate)
                .ToList();

            var delayedMilestones = activeMilestones
                .Where(m => m.Status == "delayed")
                .ToList();

            var atRiskMilestones = activeMilestones
                .Where(m => m.Status == "at_risk")
                .ToList();

            var onTrackMilestones = activeMilestones
                .Where(m => m.Status == "on_track")
                .ToList();

            var completeMilestones = project.Milestones?
                .Where(m => !m.IsDeleted && m.Status == "complete")
                .ToList() ?? new List<Milestone>();

            var ragStatus = NormalizeRagStatus(project.RagStatus) ?? "Not set";
            var hasProblemMilestones = overdueMilestones.Any() || delayedMilestones.Any() || atRiskMilestones.Any();
            
            // Check if all milestones are healthy (on_track or complete, with no problems)
            var totalMilestones = project.Milestones?.Count(m => !m.IsDeleted) ?? 0;
            var healthyMilestonesCount = onTrackMilestones.Count + completeMilestones.Count;
            var allMilestonesHealthy = !hasProblemMilestones && 
                                       totalMilestones > 0 && 
                                       healthyMilestonesCount == totalMilestones;

            // Exception 1: Green/Amber-Green RAG but has overdue/delayed/at_risk milestones
            if ((ragStatus == "Green" || ragStatus == "Amber-Green") && hasProblemMilestones)
            {
                var exceptionType = ragStatus == "Green" ? "Green RAG with Problem Milestones" : "Amber-Green RAG with Problem Milestones";
                var description = BuildExceptionDescription(overdueMilestones, delayedMilestones, atRiskMilestones);

                exceptions.Add(new RagExceptionReport
                {
                    ProjectId = project.Id,
                    ProjectCode = project.ProjectCode ?? $"DFE-DDT-{project.Id}",
                    ProjectTitle = project.Title,
                    BusinessArea = project.BusinessAreaLookup?.Name,
                    PrimaryContact = project.PrimaryContactUser != null 
                        ? $"{project.PrimaryContactUser.Name} ({project.PrimaryContactUser.Email})" 
                        : null,
                    RagStatus = ragStatus,
                    ExceptionType = exceptionType,
                    ExceptionDescription = description,
                    OverdueMilestonesCount = overdueMilestones.Count,
                    DelayedMilestonesCount = delayedMilestones.Count,
                    AtRiskMilestonesCount = atRiskMilestones.Count,
                    TotalMilestonesCount = project.Milestones?.Count(m => !m.IsDeleted) ?? 0,
                    OnTrackMilestonesCount = onTrackMilestones.Count,
                    CompleteMilestonesCount = completeMilestones.Count
                });
            }

            // Exception 2: Red RAG but all milestones are healthy (on_track or complete, no overdue/delayed/at_risk)
            if (ragStatus == "Red" && allMilestonesHealthy)
            {
                exceptions.Add(new RagExceptionReport
                {
                    ProjectId = project.Id,
                    ProjectCode = project.ProjectCode ?? $"DFE-DDT-{project.Id}",
                    ProjectTitle = project.Title,
                    BusinessArea = project.BusinessAreaLookup?.Name,
                    PrimaryContact = project.PrimaryContactUser != null 
                        ? $"{project.PrimaryContactUser.Name} ({project.PrimaryContactUser.Email})" 
                        : null,
                    RagStatus = ragStatus,
                    ExceptionType = "Red RAG with Healthy Milestones",
                    ExceptionDescription = $"Project has Red RAG status but all milestones are on track or complete with no overdue, delayed, or at-risk milestones.",
                    OverdueMilestonesCount = 0,
                    DelayedMilestonesCount = 0,
                    AtRiskMilestonesCount = 0,
                    TotalMilestonesCount = project.Milestones?.Count(m => !m.IsDeleted) ?? 0,
                    OnTrackMilestonesCount = onTrackMilestones.Count,
                    CompleteMilestonesCount = completeMilestones.Count
                });
            }

            // Exception 3: Amber-Green RAG but has delayed milestones
            if (ragStatus == "Amber-Green" && delayedMilestones.Any())
            {
                // Only add if not already added in Exception 1
                if (!exceptions.Any(e => e.ProjectId == project.Id))
                {
                    exceptions.Add(new RagExceptionReport
                    {
                        ProjectId = project.Id,
                        ProjectCode = project.ProjectCode ?? $"DFE-DDT-{project.Id}",
                        ProjectTitle = project.Title,
                        BusinessArea = project.BusinessAreaLookup?.Name,
                        PrimaryContact = project.PrimaryContactUser != null 
                            ? $"{project.PrimaryContactUser.Name} ({project.PrimaryContactUser.Email})" 
                            : null,
                        RagStatus = ragStatus,
                        ExceptionType = "Amber-Green RAG with Delayed Milestones",
                        ExceptionDescription = $"Project has Amber-Green RAG status but has {delayedMilestones.Count} delayed milestone(s).",
                        OverdueMilestonesCount = overdueMilestones.Count,
                        DelayedMilestonesCount = delayedMilestones.Count,
                        AtRiskMilestonesCount = atRiskMilestones.Count,
                        TotalMilestonesCount = project.Milestones?.Count(m => !m.IsDeleted) ?? 0,
                        OnTrackMilestonesCount = onTrackMilestones.Count,
                        CompleteMilestonesCount = completeMilestones.Count
                    });
                }
            }
        }

        return exceptions.OrderByDescending(e => e.RagStatus == "Red" ? 1 : 0)
                        .ThenByDescending(e => e.OverdueMilestonesCount + e.DelayedMilestonesCount + e.AtRiskMilestonesCount)
                        .ToList();
    }

    private string BuildExceptionDescription(List<Milestone> overdue, List<Milestone> delayed, List<Milestone> atRisk)
    {
        var parts = new List<string>();

        if (overdue.Any())
        {
            parts.Add($"{overdue.Count} overdue milestone(s)");
        }

        if (delayed.Any())
        {
            parts.Add($"{delayed.Count} delayed milestone(s)");
        }

        if (atRisk.Any())
        {
            parts.Add($"{atRisk.Count} at-risk milestone(s)");
        }

        return $"Project has {string.Join(", ", parts)}.";
    }

    // GET: CentralOps/RagStatus
    public async Task<IActionResult> RagStatus(string ragStatus)
    {
        try
        {
            // Get all projects
            var allProjects = await _context.Projects
                .Include(p => p.PrimaryContactUser)
                .Include(p => p.DeliveryPriority)
                .Include(p => p.BusinessAreaLookup)
                .Include(p => p.ServiceOwners)
                    .ThenInclude(so => so.User)
                .Where(p => !p.IsDeleted)
                .ToListAsync();

            // Filter projects by RAG status
            List<Project> filteredProjects;
            if (string.IsNullOrEmpty(ragStatus))
            {
                // "Not set" - projects with no RAG status
                filteredProjects = allProjects
                    .Where(p => string.IsNullOrWhiteSpace(p.RagStatus))
                    .OrderBy(p => p.Title)
                    .ToList();
            }
            else
            {
                filteredProjects = allProjects
                    .Where(p => NormalizeRagStatus(p.RagStatus) == ragStatus)
                    .OrderBy(p => p.Title)
                    .ToList();
            }

            ViewBag.RagStatus = ragStatus;
            ViewBag.FilteredProjects = filteredProjects;
            ViewBag.TotalCount = filteredProjects.Count;

            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading RAG status for {RagStatus}", ragStatus);
            TempData["ErrorMessage"] = "An error occurred while loading RAG status. Please try again.";
            return RedirectToAction("RagAndRisk");
        }
    }

    // GET: CentralOps/RagStatusPartial - Returns partial view for AJAX loading
    public async Task<IActionResult> RagStatusPartial(string? ragStatus = null)
    {
        try
        {
            // Get all projects
            var allProjects = await _context.Projects
                .Include(p => p.PrimaryContactUser)
                .Include(p => p.DeliveryPriority)
                .Include(p => p.BusinessAreaLookup)
                .Include(p => p.ServiceOwners)
                    .ThenInclude(so => so.User)
                .Where(p => !p.IsDeleted)
                .ToListAsync();

            // Filter projects by RAG status
            List<Project> filteredProjects;
            string displayStatus;
            
            if (string.IsNullOrEmpty(ragStatus) || ragStatus == "Not set")
            {
                // "Not set" - projects with no RAG status
                filteredProjects = allProjects
                    .Where(p => string.IsNullOrWhiteSpace(p.RagStatus))
                    .OrderBy(p => p.Title)
                    .ToList();
                displayStatus = "Not set";
            }
            else
            {
                filteredProjects = allProjects
                    .Where(p => NormalizeRagStatus(p.RagStatus) == ragStatus)
                    .OrderBy(p => p.Title)
                    .ToList();
                displayStatus = ragStatus;
            }

            ViewBag.RagStatus = ragStatus;
            ViewBag.DisplayStatus = displayStatus;
            ViewBag.FilteredProjects = filteredProjects;
            ViewBag.TotalCount = filteredProjects.Count;

            return PartialView("_RagStatusProjects", filteredProjects);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading RAG status partial for {RagStatus}", ragStatus);
            return PartialView("_RagStatusProjects", new List<Project>());
        }
    }

    // GET: CentralOps/AllWork
    public async Task<IActionResult> AllWork(string search, string ragStatus, string businessArea, string phase, string flagship, int? priority, bool clearFilters = false)
    {
        try
        {
            // Session keys for filters
            const string sessionKeySearch = "AllWork_Search";
            const string sessionKeyRagStatus = "AllWork_RagStatus";
            const string sessionKeyBusinessArea = "AllWork_BusinessArea";
            const string sessionKeyPhase = "AllWork_Phase";
            const string sessionKeyFlagship = "AllWork_Flagship";
            const string sessionKeyPriority = "AllWork_Priority";

            // Clear filters from session if requested
            if (clearFilters)
            {
                HttpContext.Session.Remove(sessionKeySearch);
                HttpContext.Session.Remove(sessionKeyRagStatus);
                HttpContext.Session.Remove(sessionKeyBusinessArea);
                HttpContext.Session.Remove(sessionKeyPhase);
                HttpContext.Session.Remove(sessionKeyFlagship);
                HttpContext.Session.Remove(sessionKeyPriority);
                search = null;
                ragStatus = null;
                businessArea = null;
                phase = null;
                flagship = null;
                priority = null;
            }
            else
            {
                var requestQuery = Request.Query;
                var hasSearchParam = requestQuery.ContainsKey("search");
                var hasRagStatusParam = requestQuery.ContainsKey("ragStatus");
                var hasBusinessAreaParam = requestQuery.ContainsKey("businessArea");
                var hasPhaseParam = requestQuery.ContainsKey("phase");
                var hasFlagshipParam = requestQuery.ContainsKey("flagship");
                var hasPriorityParam = requestQuery.ContainsKey("priority");

                // Handle search filter
                if (hasSearchParam)
                {
                    if (string.IsNullOrEmpty(search))
                        HttpContext.Session.Remove(sessionKeySearch);
                    else
                        HttpContext.Session.SetString(sessionKeySearch, search);
                }
                else
                {
                    search = HttpContext.Session.GetString(sessionKeySearch);
                }

                // Handle RAG Status filter
                if (hasRagStatusParam)
                {
                    if (string.IsNullOrEmpty(ragStatus))
                        HttpContext.Session.Remove(sessionKeyRagStatus);
                    else
                        HttpContext.Session.SetString(sessionKeyRagStatus, ragStatus);
                }
                else
                {
                    ragStatus = HttpContext.Session.GetString(sessionKeyRagStatus);
                }

                // Handle Business Area filter
                if (hasBusinessAreaParam)
                {
                    if (string.IsNullOrEmpty(businessArea))
                        HttpContext.Session.Remove(sessionKeyBusinessArea);
                    else
                        HttpContext.Session.SetString(sessionKeyBusinessArea, businessArea);
                }
                else
                {
                    businessArea = HttpContext.Session.GetString(sessionKeyBusinessArea);
                }

                // Handle Phase filter
                if (hasPhaseParam)
                {
                    if (string.IsNullOrEmpty(phase))
                        HttpContext.Session.Remove(sessionKeyPhase);
                    else
                        HttpContext.Session.SetString(sessionKeyPhase, phase);
                }
                else
                {
                    phase = HttpContext.Session.GetString(sessionKeyPhase);
                }

                // Handle Flagship filter
                if (hasFlagshipParam)
                {
                    if (string.IsNullOrEmpty(flagship))
                        HttpContext.Session.Remove(sessionKeyFlagship);
                    else
                        HttpContext.Session.SetString(sessionKeyFlagship, flagship);
                }
                else
                {
                    flagship = HttpContext.Session.GetString(sessionKeyFlagship);
                }

                // Handle Priority filter
                if (hasPriorityParam)
                {
                    if (!priority.HasValue)
                        HttpContext.Session.Remove(sessionKeyPriority);
                    else
                        HttpContext.Session.SetInt32(sessionKeyPriority, priority.Value);
                }
                else
                {
                    priority = HttpContext.Session.GetInt32(sessionKeyPriority);
                }
            }

            // Build query
            var query = _context.Projects
                .Where(p => !p.IsDeleted)
                .Include(p => p.PrimaryContactUser)
                .Include(p => p.DeliveryPriority)
                .Include(p => p.BusinessAreaLookup)
                .Include(p => p.PhaseLookup)
                .Include(p => p.ServiceOwners)
                    .ThenInclude(so => so.User)
                .AsQueryable();

            // Apply search filter
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p => p.Title.Contains(search) || p.ProjectCode.Contains(search));
            }

            // Apply RAG Status filter
            if (!string.IsNullOrEmpty(ragStatus))
            {
                query = query.Where(p => NormalizeRagStatus(p.RagStatus) == ragStatus);
            }

            // Apply Business Area filter
            if (!string.IsNullOrEmpty(businessArea))
            {
                query = query.Where(p => p.BusinessAreaLookup != null && p.BusinessAreaLookup.Name == businessArea);
            }

            // Apply Phase filter
            if (!string.IsNullOrEmpty(phase))
            {
                query = query.Where(p => p.PhaseLookup != null && p.PhaseLookup.Name == phase);
            }

            // Apply Flagship filter
            if (!string.IsNullOrEmpty(flagship))
            {
                var isFlagship = flagship == "true";
                query = query.Where(p => p.IsFlagship == isFlagship);
            }

            // Apply Priority filter
            if (priority.HasValue)
            {
                query = query.Where(p => p.DeliveryPriorityId == priority.Value);
            }

            // Get filtered projects
            var allProjects = await query
                .OrderBy(p => p.Title)
                .AsNoTracking()
                .ToListAsync();

            // Populate ViewBag for filter dropdowns and current filter values
            ViewBag.CurrentSearch = search;
            ViewBag.CurrentRagStatus = ragStatus;
            ViewBag.CurrentBusinessArea = businessArea;
            ViewBag.CurrentPhase = phase;
            ViewBag.CurrentFlagship = flagship;
            ViewBag.CurrentPriority = priority;

            // Get filter options
            ViewBag.BusinessAreas = await _context.BusinessAreaLookups
                .Where(ba => ba.IsActive)
                .OrderBy(ba => ba.SortOrder)
                .ThenBy(ba => ba.Name)
                .Select(ba => ba.Name)
                .Distinct()
                .ToListAsync();

            ViewBag.Phases = await _productsApiService.GetPhasesAsync();
            
            ViewBag.DeliveryPriorities = await _context.DeliveryPriorities
                .Where(dp => dp.IsActive)
                .OrderBy(dp => dp.SortOrder)
                .ThenBy(dp => dp.Name)
                .AsNoTracking()
                .ToListAsync();

            ViewBag.AllProjects = allProjects;

            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading all work");
            TempData["ErrorMessage"] = "An error occurred while loading all work. Please try again.";
            return View();
        }
    }

    // GET: CentralOps/MonthlyUpdateStatus
    public async Task<IActionResult> MonthlyUpdateStatus(int year, int month, string status)
    {
        try
        {
            // Get all active projects with monthly updates (exclude Cancelled and Completed)
            var allProjects = await _context.Projects
                .Include(p => p.PrimaryContactUser)
                .Include(p => p.DeliveryPriority)
                .Include(p => p.BusinessAreaLookup)
                .Include(p => p.MonthlyUpdates)
                .Where(p => !p.IsDeleted && p.Status != "Cancelled" && p.Status != "Completed")
                .ToListAsync();

            var dueDate = _monthlyUpdateService.GetMonthlyUpdateDueDate(year, month);
            var currentDate = DateTime.UtcNow;

            // Filter projects by status
            var filteredProjects = new List<Project>();
            
            foreach (var project in allProjects)
            {
                var update = project.MonthlyUpdates?.FirstOrDefault(u => u.Year == year && u.Month == month);
                string projectStatus;

                // If update exists and has been submitted
                if (update != null && update.SubmittedAt.HasValue)
                {
                    projectStatus = "Submitted";
                }
                // Check if the due date has passed (regardless of whether update exists)
                else if (currentDate > dueDate)
                {
                    projectStatus = "Late";
                }
                // If update exists but not submitted and due date hasn't passed
                else if (update != null && !update.SubmittedAt.HasValue)
                {
                    projectStatus = "In Progress";
                }
                // No update exists and due date hasn't passed
                else
                {
                    projectStatus = "Not Started";
                }

                if (projectStatus.Equals(status, StringComparison.OrdinalIgnoreCase))
                {
                    filteredProjects.Add(project);
                }
            }

            // Calculate previous month for display
            var prevMonth = month == 1 ? 12 : month - 1;
            var prevYear = month == 1 ? year - 1 : year;

            // Calculate business area completion percentages
            var businessAreaStats = new List<BusinessAreaMonthlyUpdateStats>();
            
            // Get all business areas
            var businessAreas = allProjects
                .Where(p => p.BusinessAreaLookup != null)
                .Select(p => p.BusinessAreaLookup!.Name)
                .Distinct()
                .OrderBy(ba => ba)
                .ToList();

            // Add "Not assigned" if there are projects without business area
            if (allProjects.Any(p => p.BusinessAreaLookup == null))
            {
                businessAreas.Add("Not assigned");
            }

            foreach (var businessArea in businessAreas)
            {
                var areaProjects = businessArea == "Not assigned"
                    ? allProjects.Where(p => p.BusinessAreaLookup == null).ToList()
                    : allProjects.Where(p => p.BusinessAreaLookup != null && p.BusinessAreaLookup.Name == businessArea).ToList();

                if (!areaProjects.Any()) continue;

                // Calculate for previous month
                var prevMonthStats = CalculateMonthlyUpdateStats(areaProjects, prevYear, prevMonth, _monthlyUpdateService);
                
                // Calculate for current month
                var currentMonthStats = CalculateMonthlyUpdateStats(areaProjects, year, month, _monthlyUpdateService);

                businessAreaStats.Add(new BusinessAreaMonthlyUpdateStats
                {
                    BusinessArea = businessArea,
                    TotalProjects = areaProjects.Count,
                    PreviousMonthSubmitted = prevMonthStats.Submitted,
                    PreviousMonthSubmittedPercent = areaProjects.Count > 0 ? (prevMonthStats.Submitted * 100.0) / areaProjects.Count : 0,
                    CurrentMonthSubmitted = currentMonthStats.Submitted,
                    CurrentMonthSubmittedPercent = areaProjects.Count > 0 ? (currentMonthStats.Submitted * 100.0) / areaProjects.Count : 0
                });
            }

            ViewBag.Year = year;
            ViewBag.Month = month;
            ViewBag.Status = status;
            ViewBag.MonthName = new DateTime(year, month, 1).ToString("MMMM yyyy");
            ViewBag.DueDate = dueDate;
            ViewBag.FilteredProjects = filteredProjects.OrderBy(p => p.Title).ToList();
            ViewBag.BusinessAreaStats = businessAreaStats;
            ViewBag.PreviousMonthName = new DateTime(prevYear, prevMonth, 1).ToString("MMMM yyyy");
            ViewBag.CurrentMonthName = new DateTime(year, month, 1).ToString("MMMM yyyy");

            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading monthly update status for {Year}/{Month}/{Status}", year, month, status);
            TempData["ErrorMessage"] = "An error occurred while loading monthly update status. Please try again.";
            return RedirectToAction("MonthlyReporting");
        }
    }

    // GET: CentralOps/MonthlyUpdateStatusPartial - Returns partial view for AJAX loading
    public async Task<IActionResult> MonthlyUpdateStatusPartial(int year, int month, string status)
    {
        try
        {
            // Get all active projects with monthly updates (exclude Cancelled and Completed)
            var allProjects = await _context.Projects
                .Include(p => p.PrimaryContactUser)
                .Include(p => p.DeliveryPriority)
                .Include(p => p.BusinessAreaLookup)
                .Include(p => p.ServiceOwners)
                    .ThenInclude(so => so.User)
                .Include(p => p.MonthlyUpdates)
                .Where(p => !p.IsDeleted && p.Status != "Cancelled" && p.Status != "Completed")
                .ToListAsync();

            var dueDate = _monthlyUpdateService.GetMonthlyUpdateDueDate(year, month);
            var currentDate = DateTime.UtcNow;

            // Filter projects by status
            var filteredProjects = new List<Project>();
            
            foreach (var project in allProjects)
            {
                var update = project.MonthlyUpdates?.FirstOrDefault(u => u.Year == year && u.Month == month);
                string projectStatus;

                // If update exists and has been submitted
                if (update != null && update.SubmittedAt.HasValue)
                {
                    projectStatus = "Submitted";
                }
                // Check if the due date has passed (regardless of whether update exists)
                else if (currentDate > dueDate)
                {
                    projectStatus = "Late";
                }
                // If update exists but not submitted and due date hasn't passed
                else if (update != null && !update.SubmittedAt.HasValue)
                {
                    projectStatus = "In Progress";
                }
                // No update exists and due date hasn't passed
                else
                {
                    projectStatus = "Not Started";
                }

                if (projectStatus.Equals(status, StringComparison.OrdinalIgnoreCase))
                {
                    filteredProjects.Add(project);
                }
            }

            ViewBag.Year = year;
            ViewBag.Month = month;
            ViewBag.Status = status;
            ViewBag.MonthName = new DateTime(year, month, 1).ToString("MMMM yyyy");
            ViewBag.DueDate = dueDate;
            ViewBag.FilteredProjects = filteredProjects.OrderBy(p => p.Title).ToList();

            return PartialView("_MonthlyUpdateStatusProjects", filteredProjects.OrderBy(p => p.Title).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading monthly update status partial for {Year}/{Month}/{Status}", year, month, status);
            return PartialView("_MonthlyUpdateStatusProjects", new List<Project>());
        }
    }

    // GET: CentralOps/ManageWork
    public async Task<IActionResult> ManageWork(
        string? search,
        string? businessArea,
        string? priority,
        string? rag,
        string? status)
    {
        try
        {
            // Debug: Check total projects in database
            var totalProjectsInDb = await _context.Projects.CountAsync();
            var nonDeletedProjects = await _context.Projects.CountAsync(p => !p.IsDeleted);
            _logger.LogInformation("ManageWork: Database has {Total} total projects, {NonDeleted} non-deleted projects", 
                totalProjectsInDb, nonDeletedProjects);

            // Default to Active status if no status is specified
            var effectiveStatus = string.IsNullOrEmpty(status) ? "Active" : status;

            // Build base query for status counts (before other filters)
            var baseQuery = _context.Projects.Where(p => !p.IsDeleted);
            
            // Apply search, business area, priority, and RAG filters to base query for counts
            if (!string.IsNullOrEmpty(search))
            {
                baseQuery = baseQuery.Where(p => 
                    (p.Title != null && p.Title.Contains(search)) || 
                    (p.ProjectCode != null && p.ProjectCode.Contains(search)) ||
                    (p.Aim != null && p.Aim.Contains(search)));
            }

            if (!string.IsNullOrEmpty(businessArea))
            {
                baseQuery = baseQuery.Where(p => p.BusinessAreaLookup != null && p.BusinessAreaLookup.Name == businessArea);
            }

            if (!string.IsNullOrEmpty(priority))
            {
                baseQuery = baseQuery.Where(p => p.DeliveryPriority != null && p.DeliveryPriority.Name == priority);
            }

            if (!string.IsNullOrEmpty(rag))
            {
                baseQuery = baseQuery.Where(p => p.RagStatus == rag);
            }

            // Calculate status counts
            var statusActiveCount = await baseQuery.CountAsync(p => p.Status == "Active");
            var statusPausedCount = await baseQuery.CountAsync(p => p.Status == "Paused");
            var statusCompletedCount = await baseQuery.CountAsync(p => p.Status == "Completed");
            var statusCancelledCount = await baseQuery.CountAsync(p => p.Status == "Cancelled");

            // Build main query with includes
            var query = _context.Projects
                .Include(p => p.PrimaryContactUser)
                .Include(p => p.DeliveryPriority)
                .Include(p => p.BusinessAreaLookup)
                .Include(p => p.ActivityTypeLookup)
                .Include(p => p.Directorates)
                    .ThenInclude(d => d.Division)
                .Include(p => p.SeniorResponsibleOfficers)
                    .ThenInclude(sro => sro.User)
                .Include(p => p.Milestones)
                .Where(p => !p.IsDeleted);

            // Apply filters
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p => 
                    (p.Title != null && p.Title.Contains(search)) || 
                    (p.ProjectCode != null && p.ProjectCode.Contains(search)) ||
                    (p.Aim != null && p.Aim.Contains(search)));
            }

            if (!string.IsNullOrEmpty(businessArea))
            {
                query = query.Where(p => p.BusinessAreaLookup != null && p.BusinessAreaLookup.Name == businessArea);
            }

            if (!string.IsNullOrEmpty(priority))
            {
                query = query.Where(p => p.DeliveryPriority != null && p.DeliveryPriority.Name == priority);
            }

            if (!string.IsNullOrEmpty(rag))
            {
                query = query.Where(p => p.RagStatus == rag);
            }

            // Apply status filter (defaults to Active)
            query = query.Where(p => p.Status == effectiveStatus);

            // Get all filtered results - sorted alphabetically by title by default
            var workItems = await query
                .OrderBy(p => p.Title)
                .ToListAsync();

            var totalCount = workItems.Count;
            _logger.LogInformation("ManageWork: Query returned {Count} projects (filters: search={Search}, businessArea={BusinessArea}, priority={Priority}, rag={Rag}, status={Status})", 
                totalCount, search, businessArea, priority, rag, status);

            // Get filter options
            var businessAreas = await _context.BusinessAreaLookups
                .Where(ba => ba.IsActive)
                .Select(ba => ba.Name)
                .Distinct()
                .OrderBy(ba => ba)
                .ToListAsync();

            var priorities = await _context.DeliveryPriorities
                .Where(dp => dp.IsActive)
                .Select(dp => dp.Name)
                .OrderBy(dp => dp)
                .ToListAsync();

            var directorates = await _context.Divisions
                .Where(d => d.IsActive)
                .OrderBy(d => d.Name)
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.BusinessArea = businessArea;
            ViewBag.Priority = priority;
            ViewBag.Rag = rag;
            ViewBag.Status = effectiveStatus;
            ViewBag.CurrentStatus = effectiveStatus;
            ViewBag.TotalCount = totalCount;
            ViewBag.BusinessAreas = businessAreas;
            ViewBag.Priorities = priorities;
            ViewBag.Directorates = directorates;
            ViewBag.Rags = new[] { "Red", "Amber-Red", "Amber", "Amber-Green", "Green" };
            ViewBag.Statuses = new[] { "Active", "Paused", "Completed", "Cancelled" };
            ViewBag.StatusActiveCount = statusActiveCount;
            ViewBag.StatusPausedCount = statusPausedCount;
            ViewBag.StatusCompletedCount = statusCompletedCount;
            ViewBag.StatusCancelledCount = statusCancelledCount;

            _logger.LogInformation("ManageWork: Passing {Count} projects to view. First item ID: {FirstId}", 
                workItems.Count, workItems.FirstOrDefault()?.Id);

            return View(workItems);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading projects for management");
            TempData["ErrorMessage"] = "An error occurred while loading projects. Please try again.";
            return View(new List<Project>());
        }
    }

    // GET: CentralOps/ExportManageWork
    public async Task<IActionResult> ExportManageWork(
        string? search,
        string? businessArea,
        string? priority,
        string? rag,
        string? status)
    {
        try
        {
            // Use the same query logic as ManageWork
            var query = _context.Projects
                .Include(p => p.DeliveryPriority)
                .Include(p => p.BusinessAreaLookup)
                .Include(p => p.PhaseLookup)
                .Include(p => p.PrimaryContactUser)
                .Include(p => p.ActivityTypeLookup)
                .Include(p => p.RiskAppetiteLookup)
                .Include(p => p.SeniorResponsibleOfficers)
                    .ThenInclude(sro => sro.User)
                .Include(p => p.ServiceOwners)
                    .ThenInclude(so => so.User)
                .Include(p => p.Directorates)
                    .ThenInclude(d => d.Division)
                .Include(p => p.ProjectProducts)
                .Include(p => p.ProjectContacts)
                    .ThenInclude(pc => pc.User)
                .Include(p => p.PmoContacts)
                    .ThenInclude(pc => pc.User)
                .Include(p => p.Milestones)
                .Include(p => p.ProjectMissions)
                    .ThenInclude(pm => pm.Mission)
                .Include(p => p.ProblemStatements)
                .Where(p => !p.IsDeleted && p.Status != "Cancelled" && p.Status != "Completed");

            // Apply filters (same as ManageWork)
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p => 
                    p.Title.Contains(search) ||
                    (p.ProjectCode != null && p.ProjectCode.Contains(search)) ||
                    (p.Aim != null && p.Aim.Contains(search)));
            }

            if (!string.IsNullOrEmpty(businessArea))
            {
                query = query.Where(p => p.BusinessAreaLookup != null && p.BusinessAreaLookup.Name == businessArea);
            }

            if (!string.IsNullOrEmpty(priority))
            {
                query = query.Where(p => p.DeliveryPriority != null && p.DeliveryPriority.Name == priority);
            }

            if (!string.IsNullOrEmpty(rag))
            {
                query = query.Where(p => p.RagStatus == rag);
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(p => p.Status == status);
            }

            var projects = await query
                .OrderBy(p => p.Title)
                .AsNoTracking()
                .ToListAsync();

            if (!projects.Any())
            {
                TempData["ErrorMessage"] = "No projects found to export.";
                return RedirectToAction("ManageWork", new { search, businessArea, priority, rag, status });
            }

            // Load ProblemStatements for all projects in batch
            var projectIds = projects.Select(p => p.Id).ToList();
            var problemStatements = await _context.ProjectProblemStatements
                .Where(ps => projectIds.Contains(ps.ProjectId))
                .AsNoTracking()
                .ToListAsync();
            
            // Group problem statements by project ID
            var problemStatementsByProject = problemStatements
                .GroupBy(ps => ps.ProjectId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(ps => ps.UpdatedAt).ToList());

            // Load dependencies for each project
            foreach (var project in projects)
            {
                // Load ProblemStatements if not already loaded
                if (problemStatementsByProject.TryGetValue(project.Id, out var projectProblemStatements))
                {
                    project.ProblemStatements = projectProblemStatements;
                }
                else
                {
                    project.ProblemStatements = new List<ProjectProblemStatement>();
                }

                project.DependenciesAsSource = await _context.Dependencies
                    .Where(d => d.SourceEntityType == "Project" && d.SourceEntityId == project.Id)
                    .AsNoTracking()
                    .ToListAsync();

                project.DependenciesAsTarget = await _context.Dependencies
                    .Where(d => d.TargetEntityType == "Project" && d.TargetEntityId == project.Id)
                    .AsNoTracking()
                    .ToListAsync();

                // Populate dependency titles
                foreach (var dep in project.DependenciesAsSource)
                {
                    dep.SourceEntityTitle = project.Title;
                    dep.TargetEntityTitle = await GetEntityTitle(dep.TargetEntityType, dep.TargetEntityId);
                }
                foreach (var dep in project.DependenciesAsTarget)
                {
                    dep.TargetEntityTitle = project.Title;
                    dep.SourceEntityTitle = await GetEntityTitle(dep.SourceEntityType, dep.SourceEntityId);
                }
            }

            // Find maximum number of SROs to determine column count
            var maxSroCount = projects.Max(p => p.SeniorResponsibleOfficers?.Count ?? 0);
            maxSroCount = Math.Max(maxSroCount, 1); // At least 1 column

            // Create workbook
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Work");

            // Build headers
            var headers = new List<string>
            {
                "Project Code",
                "Title",
                "Problem Statement",
                "Aim",
                "Business Area",
                "Phase",
                "Status",
                "Primary Contact",
                "Start Date",
                "End Date",
                "Activity Type",
                "Risk Appetite",
                "Current Priority",
                "Current RAG",
                "Subject to Spend Control"
            };

            // Add SRO columns (multiple if needed)
            for (int i = 1; i <= maxSroCount; i++)
            {
                headers.Add($"SRO {i}");
            }

            headers.AddRange(new[]
            {
                "Service Owner",
                "Directorates",
                "Linked Products",
                "Dependencies In",
                "Dependencies Out",
                "Strategic Alignment",
                "Governance",
                "Team",
                "Milestones"
            });

            // Add headers to worksheet
            for (int col = 0; col < headers.Count; col++)
            {
                var cell = worksheet.Cell(1, col + 1);
                cell.Value = headers[col];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#f1f3f5");
                cell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            }

            // Add data rows
            int currentRow = 2;
            foreach (var project in projects)
            {
                int col = 1;

                worksheet.Cell(currentRow, col++).Value = $"DFE-DDT-{project.Id}";
                worksheet.Cell(currentRow, col++).Value = project.Title ?? string.Empty;
                
                // Problem Statement (get the most recent one)
                var problemStatement = project.ProblemStatements?
                    .OrderByDescending(ps => ps.UpdatedAt)
                    .FirstOrDefault();
                worksheet.Cell(currentRow, col++).Value = problemStatement?.ProblemStatement ?? string.Empty;
                
                // Aim
                worksheet.Cell(currentRow, col++).Value = project.Aim ?? string.Empty;
                
                worksheet.Cell(currentRow, col++).Value = project.BusinessAreaLookup?.Name ?? string.Empty;
                worksheet.Cell(currentRow, col++).Value = project.PhaseLookup?.Name ?? string.Empty;
                worksheet.Cell(currentRow, col++).Value = project.Status ?? string.Empty;
                worksheet.Cell(currentRow, col++).Value = project.PrimaryContactUser != null 
                    ? $"{project.PrimaryContactUser.Name} ({project.PrimaryContactUser.Email})" 
                    : string.Empty;
                
                worksheet.Cell(currentRow, col++).Value = project.StartDate;
                if (project.StartDate.HasValue)
                {
                    worksheet.Cell(currentRow, col - 1).Style.NumberFormat.Format = "dd/mm/yyyy";
                }
                
                worksheet.Cell(currentRow, col++).Value = project.TargetDeliveryDate;
                if (project.TargetDeliveryDate.HasValue)
                {
                    worksheet.Cell(currentRow, col - 1).Style.NumberFormat.Format = "dd/mm/yyyy";
                }
                
                worksheet.Cell(currentRow, col++).Value = project.ActivityTypeLookup?.Name ?? string.Empty;
                worksheet.Cell(currentRow, col++).Value = project.RiskAppetiteLookup?.Name ?? string.Empty;
                worksheet.Cell(currentRow, col++).Value = project.DeliveryPriority?.Name ?? string.Empty;
                worksheet.Cell(currentRow, col++).Value = project.RagStatus ?? string.Empty;
                worksheet.Cell(currentRow, col++).Value = project.IsSubjectToSpendControl.HasValue 
                    ? (project.IsSubjectToSpendControl.Value ? "Yes" : "No") 
                    : string.Empty;

                // Add SROs (multiple columns)
                var sros = project.SeniorResponsibleOfficers?.OrderBy(sro => sro.CreatedAt).ToList() ?? new List<ProjectSeniorResponsibleOfficer>();
                for (int i = 0; i < maxSroCount; i++)
                {
                    if (i < sros.Count)
                    {
                        var sro = sros[i];
                        worksheet.Cell(currentRow, col++).Value = sro.User != null 
                            ? $"{sro.User.Name} ({sro.User.Email})" 
                            : string.Empty;
                    }
                    else
                    {
                        worksheet.Cell(currentRow, col++).Value = string.Empty;
                    }
                }

                // Service Owner
                var serviceOwners = project.ServiceOwners?
                    .Select(so => so.User != null ? $"{so.User.Name} ({so.User.Email})" : string.Empty)
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToList() ?? new List<string>();
                worksheet.Cell(currentRow, col++).Value = string.Join("; ", serviceOwners);

                // Directorates
                var directorates = project.Directorates?
                    .Select(d => d.Division?.Name ?? string.Empty)
                    .Where(d => !string.IsNullOrEmpty(d))
                    .ToList() ?? new List<string>();
                worksheet.Cell(currentRow, col++).Value = string.Join("; ", directorates);

                // Linked Products
                var products = project.ProjectProducts?
                    .Select(pp => pp.ProductTitle ?? string.Empty)
                    .Where(p => !string.IsNullOrEmpty(p))
                    .ToList() ?? new List<string>();
                worksheet.Cell(currentRow, col++).Value = string.Join("; ", products);

                // Dependencies In
                var dependenciesIn = project.DependenciesAsTarget?
                    .Select(d => $"{d.SourceEntityTitle} ({d.DependencyType ?? "N/A"})")
                    .ToList() ?? new List<string>();
                worksheet.Cell(currentRow, col++).Value = string.Join("; ", dependenciesIn);

                // Dependencies Out
                var dependenciesOut = project.DependenciesAsSource?
                    .Select(d => $"{d.TargetEntityTitle} ({d.DependencyType ?? "N/A"})")
                    .ToList() ?? new List<string>();
                worksheet.Cell(currentRow, col++).Value = string.Join("; ", dependenciesOut);

                // Strategic Alignment
                var strategicItems = new List<string>();
                if (!string.IsNullOrEmpty(project.StrategicObjectives))
                {
                    strategicItems.Add($"Objectives: {project.StrategicObjectives}");
                }
                if (project.ProjectMissions?.Any() == true)
                {
                    var missions = project.ProjectMissions
                        .Select(pm => pm.Mission?.Title ?? string.Empty)
                        .Where(m => !string.IsNullOrEmpty(m));
                    strategicItems.Add($"Missions: {string.Join(", ", missions)}");
                }
                worksheet.Cell(currentRow, col++).Value = string.Join(" | ", strategicItems);

                // Governance (PMO Contacts)
                var pmoContacts = project.PmoContacts?
                    .Select(pc => pc.User != null ? $"{pc.User.Name} ({pc.User.Email})" : string.Empty)
                    .Where(pc => !string.IsNullOrEmpty(pc))
                    .ToList() ?? new List<string>();
                worksheet.Cell(currentRow, col++).Value = string.Join("; ", pmoContacts);

                // Team
                var teamMembers = project.ProjectContacts?
                    .Where(pc => string.IsNullOrEmpty(pc.TeamStatus) || pc.TeamStatus == "current")
                    .Select(pc => pc.User != null 
                        ? $"{pc.User.Name} ({pc.Role})" 
                        : $"{pc.Name} ({pc.Role})")
                    .ToList() ?? new List<string>();
                worksheet.Cell(currentRow, col++).Value = string.Join("; ", teamMembers);

                // Milestones
                var milestones = project.Milestones?
                    .Where(m => !m.IsDeleted)
                    .Select(m => $"{m.Name} ({m.Status}) - Due: {m.DueDate:dd/MM/yyyy}")
                    .ToList() ?? new List<string>();
                worksheet.Cell(currentRow, col++).Value = string.Join(" | ", milestones);

                currentRow++;
            }

            // Auto-fit columns
            worksheet.Columns().AdjustToContents();
            
            // Wrap text for longer columns
            var wrapColumns = new[] { "Problem Statement", "Aim", "Strategic Alignment", "Team", "Milestones", "Dependencies In", "Dependencies Out", "Linked Products" };
            for (int i = 0; i < headers.Count; i++)
            {
                if (wrapColumns.Contains(headers[i]))
                {
                    worksheet.Column(i + 1).Style.Alignment.WrapText = true;
                }
            }

            worksheet.SheetView.FreezeRows(1);

            // Save to memory stream
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var fileName = $"manage-work-export-{DateTime.UtcNow:yyyyMMdd-HHmmss}.xlsx";
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting manage work to Excel");
            TempData["ErrorMessage"] = "An error occurred while exporting the data. Please try again.";
            return RedirectToAction("ManageWork", new { search, businessArea, priority, rag, status });
        }
    }

    // GET: CentralOps/ExportAllWork
    public async Task<IActionResult> ExportAllWork(string search, string ragStatus, string businessArea, string phase, string flagship, int? priority)
    {
        try
        {
            // Session keys for filters (same as AllWork)
            const string sessionKeySearch = "AllWork_Search";
            const string sessionKeyRagStatus = "AllWork_RagStatus";
            const string sessionKeyBusinessArea = "AllWork_BusinessArea";
            const string sessionKeyPhase = "AllWork_Phase";
            const string sessionKeyFlagship = "AllWork_Flagship";
            const string sessionKeyPriority = "AllWork_Priority";

            // Get filter values from session if not provided in query string
            if (string.IsNullOrEmpty(search))
                search = HttpContext.Session.GetString(sessionKeySearch);
            if (string.IsNullOrEmpty(ragStatus))
                ragStatus = HttpContext.Session.GetString(sessionKeyRagStatus);
            if (string.IsNullOrEmpty(businessArea))
                businessArea = HttpContext.Session.GetString(sessionKeyBusinessArea);
            if (string.IsNullOrEmpty(phase))
                phase = HttpContext.Session.GetString(sessionKeyPhase);
            if (string.IsNullOrEmpty(flagship))
                flagship = HttpContext.Session.GetString(sessionKeyFlagship);
            if (!priority.HasValue)
                priority = HttpContext.Session.GetInt32(sessionKeyPriority);

            // Build query with all necessary includes (same as ExportManageWork) - exclude Cancelled and Completed
            var query = _context.Projects
                .Include(p => p.DeliveryPriority)
                .Include(p => p.BusinessAreaLookup)
                .Include(p => p.PhaseLookup)
                .Include(p => p.PrimaryContactUser)
                .Include(p => p.ActivityTypeLookup)
                .Include(p => p.RiskAppetiteLookup)
                .Include(p => p.SeniorResponsibleOfficers)
                    .ThenInclude(sro => sro.User)
                .Include(p => p.ServiceOwners)
                    .ThenInclude(so => so.User)
                .Include(p => p.Directorates)
                    .ThenInclude(d => d.Division)
                .Include(p => p.ProjectProducts)
                .Include(p => p.ProjectContacts)
                    .ThenInclude(pc => pc.User)
                .Include(p => p.PmoContacts)
                    .ThenInclude(pc => pc.User)
                .Include(p => p.Milestones)
                .Include(p => p.ProjectMissions)
                    .ThenInclude(pm => pm.Mission)
                .Where(p => !p.IsDeleted && p.Status != "Cancelled" && p.Status != "Completed");

            // Apply filters (same as AllWork)
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p => p.Title.Contains(search) || (p.ProjectCode != null && p.ProjectCode.Contains(search)));
            }

            if (!string.IsNullOrEmpty(ragStatus))
            {
                query = query.Where(p => NormalizeRagStatus(p.RagStatus) == ragStatus);
            }

            if (!string.IsNullOrEmpty(businessArea))
            {
                query = query.Where(p => p.BusinessAreaLookup != null && p.BusinessAreaLookup.Name == businessArea);
            }

            if (!string.IsNullOrEmpty(phase))
            {
                query = query.Where(p => p.PhaseLookup != null && p.PhaseLookup.Name == phase);
            }

            if (!string.IsNullOrEmpty(flagship))
            {
                var isFlagship = flagship == "true";
                query = query.Where(p => p.IsFlagship == isFlagship);
            }

            if (priority.HasValue)
            {
                query = query.Where(p => p.DeliveryPriorityId == priority.Value);
            }

            var projects = await query
                .OrderBy(p => p.Title)
                .AsNoTracking()
                .ToListAsync();

            if (!projects.Any())
            {
                TempData["ErrorMessage"] = "No projects found to export.";
                return RedirectToAction("AllWork", new { search, ragStatus, businessArea, phase, flagship, priority });
            }

            // Load dependencies for each project
            foreach (var project in projects)
            {
                project.DependenciesAsSource = await _context.Dependencies
                    .Where(d => d.SourceEntityType == "Project" && d.SourceEntityId == project.Id)
                    .AsNoTracking()
                    .ToListAsync();

                project.DependenciesAsTarget = await _context.Dependencies
                    .Where(d => d.TargetEntityType == "Project" && d.TargetEntityId == project.Id)
                    .AsNoTracking()
                    .ToListAsync();

                // Populate dependency titles
                foreach (var dep in project.DependenciesAsSource)
                {
                    dep.SourceEntityTitle = project.Title;
                    dep.TargetEntityTitle = await GetEntityTitle(dep.TargetEntityType, dep.TargetEntityId);
                }
                foreach (var dep in project.DependenciesAsTarget)
                {
                    dep.TargetEntityTitle = project.Title;
                    dep.SourceEntityTitle = await GetEntityTitle(dep.SourceEntityType, dep.SourceEntityId);
                }
            }

            // Find maximum number of SROs to determine column count
            var maxSroCount = projects.Max(p => p.SeniorResponsibleOfficers?.Count ?? 0);
            maxSroCount = Math.Max(maxSroCount, 1); // At least 1 column

            // Create workbook
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("All Work");

            // Build headers
            var headers = new List<string>
            {
                "Project Code",
                "Title",
                "Business Area",
                "Phase",
                "Status",
                "Primary Contact",
                "Start Date",
                "End Date",
                "Activity Type",
                "Risk Appetite",
                "Current Priority",
                "Current RAG",
                "Subject to Spend Control"
            };

            // Add SRO columns (multiple if needed)
            for (int i = 1; i <= maxSroCount; i++)
            {
                headers.Add($"SRO {i}");
            }

            headers.AddRange(new[]
            {
                "Service Owner",
                "Directorates",
                "Linked Products",
                "Dependencies In",
                "Dependencies Out",
                "Strategic Alignment",
                "Governance",
                "Team",
                "Milestones"
            });

            // Add headers to worksheet
            for (int col = 0; col < headers.Count; col++)
            {
                var cell = worksheet.Cell(1, col + 1);
                cell.Value = headers[col];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#f1f3f5");
                cell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            }

            // Add data rows
            int currentRow = 2;
            foreach (var project in projects)
            {
                int col = 1;

                worksheet.Cell(currentRow, col++).Value = $"DFE-DDT-{project.Id}";
                worksheet.Cell(currentRow, col++).Value = project.Title ?? string.Empty;
                worksheet.Cell(currentRow, col++).Value = project.BusinessAreaLookup?.Name ?? string.Empty;
                worksheet.Cell(currentRow, col++).Value = project.PhaseLookup?.Name ?? string.Empty;
                worksheet.Cell(currentRow, col++).Value = project.Status ?? string.Empty;
                worksheet.Cell(currentRow, col++).Value = project.PrimaryContactUser != null 
                    ? $"{project.PrimaryContactUser.Name} ({project.PrimaryContactUser.Email})" 
                    : string.Empty;
                
                worksheet.Cell(currentRow, col++).Value = project.StartDate;
                if (project.StartDate.HasValue)
                {
                    worksheet.Cell(currentRow, col - 1).Style.NumberFormat.Format = "dd/mm/yyyy";
                }
                
                worksheet.Cell(currentRow, col++).Value = project.TargetDeliveryDate;
                if (project.TargetDeliveryDate.HasValue)
                {
                    worksheet.Cell(currentRow, col - 1).Style.NumberFormat.Format = "dd/mm/yyyy";
                }
                
                worksheet.Cell(currentRow, col++).Value = project.ActivityTypeLookup?.Name ?? string.Empty;
                worksheet.Cell(currentRow, col++).Value = project.RiskAppetiteLookup?.Name ?? string.Empty;
                worksheet.Cell(currentRow, col++).Value = project.DeliveryPriority?.Name ?? string.Empty;
                worksheet.Cell(currentRow, col++).Value = project.RagStatus ?? string.Empty;
                worksheet.Cell(currentRow, col++).Value = project.IsSubjectToSpendControl.HasValue 
                    ? (project.IsSubjectToSpendControl.Value ? "Yes" : "No") 
                    : string.Empty;

                // Add SROs (multiple columns)
                var sros = project.SeniorResponsibleOfficers?.OrderBy(sro => sro.CreatedAt).ToList() ?? new List<ProjectSeniorResponsibleOfficer>();
                for (int i = 0; i < maxSroCount; i++)
                {
                    if (i < sros.Count)
                    {
                        var sro = sros[i];
                        worksheet.Cell(currentRow, col++).Value = sro.User != null 
                            ? $"{sro.User.Name} ({sro.User.Email})" 
                            : string.Empty;
                    }
                    else
                    {
                        worksheet.Cell(currentRow, col++).Value = string.Empty;
                    }
                }

                // Service Owner
                var serviceOwners = project.ServiceOwners?
                    .Select(so => so.User != null ? $"{so.User.Name} ({so.User.Email})" : string.Empty)
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToList() ?? new List<string>();
                worksheet.Cell(currentRow, col++).Value = string.Join("; ", serviceOwners);

                // Directorates
                var directorates = project.Directorates?
                    .Select(d => d.Division?.Name ?? string.Empty)
                    .Where(d => !string.IsNullOrEmpty(d))
                    .ToList() ?? new List<string>();
                worksheet.Cell(currentRow, col++).Value = string.Join("; ", directorates);

                // Linked Products
                var products = project.ProjectProducts?
                    .Select(pp => pp.ProductTitle ?? string.Empty)
                    .Where(p => !string.IsNullOrEmpty(p))
                    .ToList() ?? new List<string>();
                worksheet.Cell(currentRow, col++).Value = string.Join("; ", products);

                // Dependencies In
                var dependenciesIn = project.DependenciesAsTarget?
                    .Select(d => $"{d.SourceEntityTitle} ({d.DependencyType ?? "N/A"})")
                    .ToList() ?? new List<string>();
                worksheet.Cell(currentRow, col++).Value = string.Join("; ", dependenciesIn);

                // Dependencies Out
                var dependenciesOut = project.DependenciesAsSource?
                    .Select(d => $"{d.TargetEntityTitle} ({d.DependencyType ?? "N/A"})")
                    .ToList() ?? new List<string>();
                worksheet.Cell(currentRow, col++).Value = string.Join("; ", dependenciesOut);

                // Strategic Alignment
                var strategicItems = new List<string>();
                if (!string.IsNullOrEmpty(project.StrategicObjectives))
                {
                    strategicItems.Add($"Objectives: {project.StrategicObjectives}");
                }
                if (project.ProjectMissions?.Any() == true)
                {
                    var missions = project.ProjectMissions
                        .Select(pm => pm.Mission?.Title ?? string.Empty)
                        .Where(m => !string.IsNullOrEmpty(m));
                    strategicItems.Add($"Missions: {string.Join(", ", missions)}");
                }
                worksheet.Cell(currentRow, col++).Value = string.Join(" | ", strategicItems);

                // Governance (PMO Contacts)
                var pmoContacts = project.PmoContacts?
                    .Select(pc => pc.User != null ? $"{pc.User.Name} ({pc.User.Email})" : string.Empty)
                    .Where(pc => !string.IsNullOrEmpty(pc))
                    .ToList() ?? new List<string>();
                worksheet.Cell(currentRow, col++).Value = string.Join("; ", pmoContacts);

                // Team
                var teamMembers = project.ProjectContacts?
                    .Where(pc => string.IsNullOrEmpty(pc.TeamStatus) || pc.TeamStatus == "current")
                    .Select(pc => pc.User != null 
                        ? $"{pc.User.Name} ({pc.Role})" 
                        : $"{pc.Name} ({pc.Role})")
                    .ToList() ?? new List<string>();
                worksheet.Cell(currentRow, col++).Value = string.Join("; ", teamMembers);

                // Milestones
                var milestones = project.Milestones?
                    .Where(m => !m.IsDeleted)
                    .Select(m => $"{m.Name} ({m.Status}) - Due: {m.DueDate:dd/MM/yyyy}")
                    .ToList() ?? new List<string>();
                worksheet.Cell(currentRow, col++).Value = string.Join(" | ", milestones);

                currentRow++;
            }

            // Auto-fit columns
            worksheet.Columns().AdjustToContents();
            
            // Wrap text for longer columns
            var wrapColumns = new[] { "Strategic Alignment", "Team", "Milestones", "Dependencies In", "Dependencies Out", "Linked Products" };
            for (int i = 0; i < headers.Count; i++)
            {
                if (wrapColumns.Contains(headers[i]))
                {
                    worksheet.Column(i + 1).Style.Alignment.WrapText = true;
                }
            }

            worksheet.SheetView.FreezeRows(1);

            // Save to memory stream
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var fileName = $"all-work-export-{DateTime.UtcNow:yyyyMMdd-HHmmss}.xlsx";
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting all work to Excel");
            TempData["ErrorMessage"] = "An error occurred while exporting the data. Please try again.";
            return RedirectToAction("AllWork", new { search, ragStatus, businessArea, phase, flagship, priority });
        }
    }

    private async Task<string> GetEntityTitle(string entityType, int entityId)
    {
        try
        {
            return entityType switch
            {
                "Project" => await _context.Projects
                    .Where(p => p.Id == entityId)
                    .Select(p => p.Title)
                    .FirstOrDefaultAsync() ?? "Unknown Project",
                "Milestone" => await _context.Milestones
                    .Where(m => m.Id == entityId)
                    .Select(m => m.Name)
                    .FirstOrDefaultAsync() ?? "Unknown Milestone",
                "Issue" => await _context.Issues
                    .Where(i => i.Id == entityId)
                    .Select(i => i.Title)
                    .FirstOrDefaultAsync() ?? "Unknown Issue",
                "Risk" => await _context.Risks
                    .Where(r => r.Id == entityId)
                    .Select(r => r.Title)
                    .FirstOrDefaultAsync() ?? "Unknown Risk",
                "Action" => await _context.Actions
                    .Where(a => a.Id == entityId)
                    .Select(a => a.Title)
                    .FirstOrDefaultAsync() ?? "Unknown Action",
                _ => $"Unknown {entityType}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting entity title for {EntityType} {EntityId}", entityType, entityId);
            return $"Unknown {entityType}";
        }
    }

    // GET: CentralOps/WorkItemDetails/5
    public async Task<IActionResult> WorkItemDetails(int? id, string? tab)
    {
        if (id == null)
        {
            return NotFound();
        }
        
        ViewBag.CurrentTab = tab ?? "overview";

        var project = await _context.Projects
            .Include(p => p.DeliveryPriority)
            .Include(p => p.RagStatusLookup)
            .Include(p => p.PrimaryContactUser)
            .Include(p => p.BusinessAreaLookup)
            .Include(p => p.PhaseLookup)
            .Include(p => p.ActivityTypeLookup)
            .Include(p => p.RiskAppetiteLookup)
            .Include(p => p.SeniorResponsibleOfficers)
                .ThenInclude(sro => sro.User)
            .Include(p => p.ServiceOwners)
                .ThenInclude(so => so.User)
            .Include(p => p.Directorates)
                .ThenInclude(d => d.Division)
            .Include(p => p.PmoContacts)
                .ThenInclude(pc => pc.User)
            .Include(p => p.ProjectObjectives)
                .ThenInclude(po => po.Objective)
            .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);

        if (project == null)
        {
            return NotFound();
        }

        // Get all lookup data for dropdowns
        ViewBag.Priorities = await _context.DeliveryPriorities
            .Where(dp => dp.IsActive)
            .OrderBy(dp => dp.SortOrder)
            .ThenBy(dp => dp.Name)
            .ToListAsync();
        
        ViewBag.BusinessAreas = await _context.BusinessAreaLookups
            .Where(ba => ba.IsActive)
            .OrderBy(ba => ba.SortOrder)
            .ThenBy(ba => ba.Name)
            .ToListAsync();
        
        ViewBag.Phases = await _context.PhaseLookups
            .Where(p => p.IsActive)
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Name)
            .ToListAsync();
        
        ViewBag.ActivityTypes = await _context.ActivityTypeLookups
            .Where(at => at.IsActive)
            .OrderBy(at => at.SortOrder)
            .ThenBy(at => at.Name)
            .ToListAsync();
        
        ViewBag.Directorates = await _context.DirectorateLookups
            .Where(d => d.IsActive)
            .OrderBy(d => d.SortOrder)
            .ThenBy(d => d.Name)
            .ToListAsync();
        
        ViewBag.RagStatuses = await _context.RagStatusLookups
            .Where(r => r.IsActive)
            .OrderBy(r => r.SortOrder)
            .ThenBy(r => r.Name)
            .ToListAsync();
        ViewBag.Statuses = new[] { "Active", "Paused", "Completed", "Cancelled" };

        // Get audit logs for the project and related entities
        var projectIdString = id.Value.ToString();
        
        // Get related entity IDs first for better performance
        var milestoneIds = await _context.Milestones
            .Where(m => m.ProjectId == id.Value && !m.IsDeleted)
            .Select(m => m.Id.ToString())
            .ToListAsync();
            
        var monthlyUpdateIds = await _context.ProjectMonthlyUpdates
            .Where(mu => mu.ProjectId == id.Value)
            .Select(mu => mu.Id.ToString())
            .ToListAsync();
            
        var narrativeIds = await _context.MonthlyUpdateNarratives
            .Include(n => n.ProjectMonthlyUpdate)
            .Where(n => n.ProjectMonthlyUpdate.ProjectId == id.Value)
            .Select(n => n.Id.ToString())
            .ToListAsync();
            
        var statusUpdateIds = await _context.ProjectStatusUpdates
            .Where(psu => psu.ProjectId == id.Value)
            .Select(psu => psu.Id.ToString())
            .ToListAsync();
        
        // Only get audit logs for THIS specific project - ensure EntityId matches exactly
        // Filter Project entities strictly by EntityId to avoid showing other projects
        var auditLogs = await _context.AuditLogs
            .Where(a => 
                (a.Entity == "Project" && a.EntityId == projectIdString) ||
                (a.Entity == "Milestone" && milestoneIds.Contains(a.EntityId)) ||
                (a.Entity == "ProjectMonthlyUpdate" && monthlyUpdateIds.Contains(a.EntityId)) ||
                (a.Entity == "MonthlyUpdateNarrative" && narrativeIds.Contains(a.EntityId)) ||
                (a.Entity == "ProjectStatusUpdate" && statusUpdateIds.Contains(a.EntityId))
            )
            .OrderByDescending(a => a.ChangedUtc)
            .Take(500) // Limit to most recent 500 entries
            .ToListAsync();
        
        // Additional safety check: filter out any Project audit logs that don't match this project
        // This ensures we never show audit logs from other projects
        auditLogs = auditLogs
            .Where(a => a.Entity != "Project" || a.EntityId == projectIdString)
            .ToList();

        // Get direct creation/update info from entities that track it (excluding project creation to avoid duplicates)
        var entityActions = new List<dynamic>();

        // Milestones
        var milestones = await _context.Milestones
            .Where(m => m.ProjectId == id.Value && !m.IsDeleted)
            .ToListAsync();
        foreach (var milestone in milestones)
        {
            if (milestone.CreatedAt != default)
            {
                entityActions.Add(new
                {
                    Date = milestone.CreatedAt,
                    Action = "Create",
                    Entity = "Milestone",
                    EntityId = milestone.Id.ToString(),
                    EntityReference = milestone.Name,
                    ChangedBy = "System",
                    ChangedByEmail = "",
                    ChangedByUserId = "",
                    BeforeJson = (string?)null,
                    AfterJson = (string?)null
                });
            }
        }

        // Monthly Updates
        var monthlyUpdates = await _context.ProjectMonthlyUpdates
            .Where(mu => mu.ProjectId == id.Value)
            .ToListAsync();
        foreach (var update in monthlyUpdates)
        {
            if (update.CreatedAt != default)
            {
                var createdBy = update.CreatedByName ?? update.CreatedByUser?.Name ?? update.CreatedByEmail ?? "Unknown";
                var createdByEmail = update.CreatedByEmail ?? update.CreatedByUser?.Email ?? "";
                entityActions.Add(new
                {
                    Date = update.CreatedAt,
                    Action = "Create",
                    Entity = "ProjectMonthlyUpdate",
                    EntityId = update.Id.ToString(),
                    EntityReference = $"Monthly Update - {new DateTime(update.Year, update.Month, 1):MMMM yyyy}",
                    ChangedBy = createdBy,
                    ChangedByEmail = createdByEmail,
                    ChangedByUserId = update.CreatedByUserId?.ToString() ?? "",
                    BeforeJson = (string?)null,
                    AfterJson = (string?)null
                });
            }
        }

        // Monthly Update Narratives
        var narratives = await _context.MonthlyUpdateNarratives
            .Include(n => n.ProjectMonthlyUpdate)
            .Where(n => n.ProjectMonthlyUpdate.ProjectId == id.Value)
            .ToListAsync();
        foreach (var narrative in narratives)
        {
            if (narrative.CreatedAt != default)
            {
                var createdBy = narrative.CreatedByName ?? narrative.CreatedByUser?.Name ?? narrative.CreatedByEmail ?? "Unknown";
                var createdByEmail = narrative.CreatedByEmail ?? narrative.CreatedByUser?.Email ?? "";
                var update = narrative.ProjectMonthlyUpdate;
                entityActions.Add(new
                {
                    Date = narrative.CreatedAt,
                    Action = "Create",
                    Entity = "MonthlyUpdateNarrative",
                    EntityId = narrative.Id.ToString(),
                    EntityReference = $"Update Narrative - {new DateTime(update.Year, update.Month, 1):MMMM yyyy}",
                    ChangedBy = createdBy,
                    ChangedByEmail = createdByEmail,
                    ChangedByUserId = narrative.CreatedByUserId?.ToString() ?? "",
                    BeforeJson = (string?)null,
                    AfterJson = (string?)null
                });
            }
        }

        // Status Updates
        var statusUpdates = await _context.ProjectStatusUpdates
            .Where(psu => psu.ProjectId == id.Value)
            .ToListAsync();
        foreach (var statusUpdate in statusUpdates)
        {
            if (statusUpdate.CreatedAt != default)
            {
                var createdBy = statusUpdate.CreatedByName ?? statusUpdate.CreatedByUser?.Name ?? statusUpdate.CreatedByEmail ?? "Unknown";
                var createdByEmail = statusUpdate.CreatedByEmail ?? statusUpdate.CreatedByUser?.Email ?? "";
                entityActions.Add(new
                {
                    Date = statusUpdate.CreatedAt,
                    Action = "Create",
                    Entity = "ProjectStatusUpdate",
                    EntityId = statusUpdate.Id.ToString(),
                    EntityReference = "Status Update",
                    ChangedBy = createdBy,
                    ChangedByEmail = createdByEmail,
                    ChangedByUserId = statusUpdate.CreatedByUserId?.ToString() ?? "",
                    BeforeJson = (string?)null,
                    AfterJson = (string?)null
                });
            }
        }

        // Combine audit logs with entity actions and sort by date
        var auditLogActions = auditLogs.Select(a => new
        {
            AuditLogId = a.AuditLogId.ToString(), // Include unique ID for modal
            Date = a.ChangedUtc,
            Action = a.Action,
            Entity = a.Entity,
            EntityId = a.EntityId,
            EntityReference = a.EntityReference,
            ChangedBy = a.ChangedBy,
            ChangedByEmail = a.ChangedByEmail,
            ChangedByUserId = a.ChangedByUserId,
            BeforeJson = a.BeforeJson,
            AfterJson = a.AfterJson
        }).ToList();
        
        var entityActionList = entityActions.Select(ea => new
        {
            AuditLogId = Guid.NewGuid().ToString(), // Generate unique ID for entity actions
            Date = (DateTime)ea.Date,
            Action = (string)ea.Action,
            Entity = (string)ea.Entity,
            EntityId = (string)ea.EntityId,
            EntityReference = (string?)ea.EntityReference,
            ChangedBy = (string?)ea.ChangedBy,
            ChangedByEmail = (string?)ea.ChangedByEmail,
            ChangedByUserId = (string?)ea.ChangedByUserId,
            BeforeJson = (string?)ea.BeforeJson,
            AfterJson = (string?)ea.AfterJson
        }).ToList();
        
        var allAuditActions = auditLogActions
            .Concat(entityActionList)
            .OrderByDescending(a => a.Date)
            .Take(500)
            .ToList();

        ViewBag.AuditLogs = allAuditActions;

        return View(project);
    }

    // POST: CentralOps/UpdatePriority
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdatePriority(int id, int? priorityId)
    {
        var project = await _context.Projects.FindAsync(id);
        if (project == null || project.IsDeleted)
        {
            return NotFound();
        }

        try
        {
            project.DeliveryPriorityId = priorityId;
            project.UpdatedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = "Priority updated successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating priority for project {ProjectId}", id);
            TempData["ErrorMessage"] = "An error occurred while updating the priority.";
        }

        return RedirectToAction(nameof(WorkItemDetails), new { id });
    }

    // POST: CentralOps/UpdateField
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateField(int id, string field, string? value, string? statusChangeReason, int? ragStatusLookupId)
    {
        var project = await _context.Projects.FindAsync(id);
        if (project == null || project.IsDeleted)
        {
            return NotFound();
        }

        try
        {
            switch (field.ToLower())
            {
                case "title":
                    project.Title = value ?? string.Empty;
                    break;
                case "aim":
                    project.Aim = value;
                    break;
                case "businessarea":
                    if (int.TryParse(value, out var businessAreaId) && businessAreaId > 0)
                    {
                        project.BusinessAreaId = businessAreaId;
                    }
                    else
                    {
                        project.BusinessAreaId = null;
                    }
                    break;
                case "phase":
                    if (int.TryParse(value, out var phaseId) && phaseId > 0)
                    {
                        project.PhaseId = phaseId;
                    }
                    else
                    {
                        project.PhaseId = null;
                    }
                    break;
                case "activitytype":
                    if (int.TryParse(value, out var activityTypeId) && activityTypeId > 0)
                    {
                        project.ActivityTypeLookupId = activityTypeId;
                    }
                    else
                    {
                        project.ActivityTypeLookupId = null;
                    }
                    break;
                case "status":
                    project.Status = value ?? "Active";
                    if (!string.IsNullOrEmpty(statusChangeReason))
                    {
                        // Add status update with reason
                        // This would require a StatusUpdate model - for now just update the status
                    }
                    break;
                case "ragstatus":
                    if (ragStatusLookupId.HasValue && ragStatusLookupId.Value > 0)
                    {
                        project.RagStatusLookupId = ragStatusLookupId.Value;
                        // Also update the legacy RagStatus field for backward compatibility
                        var ragStatusLookup = await _context.RagStatusLookups.FindAsync(ragStatusLookupId.Value);
                        if (ragStatusLookup != null)
                        {
                            project.RagStatus = ragStatusLookup.Name;
                        }
                    }
                    else
                    {
                        project.RagStatusLookupId = null;
                        project.RagStatus = value;
                    }
                    break;
                case "primarycontact":
                    if (int.TryParse(value, out var userId) && userId > 0)
                    {
                        project.PrimaryContactUserId = userId;
                    }
                    else
                    {
                        project.PrimaryContactUserId = null;
                    }
                    break;
                case "flagship":
                    project.IsFlagship = value == "true";
                    break;
                case "startdate":
                    if (DateTime.TryParse(value, out var startDate))
                    {
                        project.StartDate = startDate;
                    }
                    else
                    {
                        project.StartDate = null;
                    }
                    break;
                case "targetdate":
                    if (DateTime.TryParse(value, out var targetDate))
                    {
                        project.TargetDeliveryDate = targetDate;
                    }
                    else
                    {
                        project.TargetDeliveryDate = null;
                    }
                    break;
            }

            project.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = $"{field} updated successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating {Field} for project {ProjectId}", field, id);
            TempData["ErrorMessage"] = $"An error occurred while updating {field}.";
        }

        return RedirectToAction(nameof(WorkItemDetails), new { id });
    }

    // POST: CentralOps/UpdateBusinessCaseApproval
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateBusinessCaseApproval(int id, string? businessCaseApproval)
    {
        var project = await _context.Projects.FindAsync(id);
        if (project == null || project.IsDeleted)
        {
            return NotFound();
        }

        try
        {
            project.BusinessCaseApproval = string.IsNullOrWhiteSpace(businessCaseApproval) ? null : businessCaseApproval.Trim();
            project.UpdatedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = "Business case approval updated successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating business case approval for project {ProjectId}", id);
            TempData["ErrorMessage"] = "An error occurred while updating the business case approval.";
        }

        return RedirectToAction(nameof(WorkItemDetails), new { id });
    }

    // GET: CentralOps/BusinessAreaDetails/{businessArea}
    public async Task<IActionResult> BusinessAreaDetails(string? businessArea)
    {
        if (string.IsNullOrEmpty(businessArea))
        {
            return NotFound();
        }

        try
        {
            // Get all projects for this business area (or unassigned if "Not assigned")
            var projects = businessArea == "Not assigned"
                ? await _context.Projects
                    .Include(p => p.PrimaryContactUser)
                    .Include(p => p.DeliveryPriority)
                    .Include(p => p.BusinessAreaLookup)
                    .Where(p => !p.IsDeleted && p.BusinessAreaLookup == null)
                    .OrderBy(p => p.Title)
                    .ToListAsync()
                : await _context.Projects
                    .Include(p => p.PrimaryContactUser)
                    .Include(p => p.DeliveryPriority)
                    .Include(p => p.BusinessAreaLookup)
                    .Where(p => !p.IsDeleted && p.BusinessAreaLookup != null && p.BusinessAreaLookup.Name == businessArea)
                    .OrderBy(p => p.Title)
                    .ToListAsync();

            // Calculate RAG counts
            var ragCounts = new Dictionary<string, int>
            {
                { "Red", projects.Count(p => NormalizeRagStatus(p.RagStatus) == "Red") },
                { "Amber-Red", projects.Count(p => NormalizeRagStatus(p.RagStatus) == "Amber-Red") },
                { "Amber", projects.Count(p => NormalizeRagStatus(p.RagStatus) == "Amber") },
                { "Amber-Green", projects.Count(p => NormalizeRagStatus(p.RagStatus) == "Amber-Green") },
                { "Green", projects.Count(p => NormalizeRagStatus(p.RagStatus) == "Green") }
            };

            // Calculate Priority counts
            var priorityCounts = new Dictionary<string, int>
            {
                { "Critical", projects.Count(p => p.DeliveryPriority != null && p.DeliveryPriority.Name.ToLower().Contains("critical")) },
                { "High", projects.Count(p => p.DeliveryPriority != null && p.DeliveryPriority.Name.ToLower().Contains("high") && !p.DeliveryPriority.Name.ToLower().Contains("critical")) },
                { "Medium", projects.Count(p => p.DeliveryPriority != null && p.DeliveryPriority.Name.ToLower().Contains("medium")) },
                { "Low", projects.Count(p => p.DeliveryPriority != null && p.DeliveryPriority.Name.ToLower().Contains("low")) },
                { "Not set", projects.Count(p => p.DeliveryPriority == null) }
            };

            ViewBag.BusinessArea = businessArea;
            ViewBag.Projects = projects;
            ViewBag.RagCounts = ragCounts;
            ViewBag.PriorityCounts = priorityCounts;

            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading business area details for {BusinessArea}", businessArea);
            TempData["ErrorMessage"] = "An error occurred while loading the business area details. Please try again.";
            return RedirectToAction(nameof(Dashboard));
        }
    }

    // GET: CentralOps/SltWeeklyReport
    public async Task<IActionResult> SltWeeklyReport(int? year, int? week)
    {
        try
        {
            // Determine the week to display - Thursday to Wednesday (report is read on Thursday)
            DateTime weekStart;
            if (year.HasValue && week.HasValue)
            {
                // Calculate the date from year and week number - find first Thursday of the year
                var jan1 = new DateTime(year.Value, 1, 1);
                var daysOffset = DayOfWeek.Thursday - jan1.DayOfWeek;
                if (daysOffset < 0) daysOffset += 7; // Adjust if Jan 1 is after Thursday
                var firstThursday = jan1.AddDays(daysOffset);
                weekStart = firstThursday.AddDays((week.Value - 1) * 7);
            }
            else
            {
                // Default to current week - calculate Thursday of current week
                var today = DateTime.UtcNow.Date;
                var daysSinceThursday = ((int)today.DayOfWeek - (int)DayOfWeek.Thursday + 7) % 7;
                weekStart = today.AddDays(-daysSinceThursday);
            }

            weekStart = weekStart.Date;
            var weekEnd = weekStart.AddDays(6).Date.AddHours(23).AddMinutes(59).AddSeconds(59);

            var culture = System.Globalization.CultureInfo.CurrentCulture;
            var calendar = culture.Calendar;
            var weekNumber = calendar.GetWeekOfYear(weekStart, culture.DateTimeFormat.CalendarWeekRule, culture.DateTimeFormat.FirstDayOfWeek);
            var reportYear = weekStart.Year;

            // Get legacy successes marked for SLT report within this week
            var legacySuccesses = await _context.ProjectSuccesses
                .Include(s => s.Project)
                    .ThenInclude(p => p.BusinessAreaLookup)
                .Where(s => s.IsReportedToSlt && 
                           s.RecordedAt >= weekStart && 
                           s.RecordedAt <= weekEnd)
                .OrderByDescending(s => s.RecordedAt)
                .ToListAsync();

            // Get weekly success updates marked for SLT report within this week
            var weeklySuccesses = await _context.ProjectWeeklySuccessUpdates
                .Include(s => s.Project)
                    .ThenInclude(p => p.BusinessAreaLookup)
                .Where(s => s.IsReportedToSlt && 
                           s.WeekStartDate <= weekEnd && 
                           s.WeekEndDate >= weekStart)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            // Create unified list of all successes
            var allSuccesses = new List<UnifiedSuccessItem>();
            
            foreach (var success in legacySuccesses)
            {
                allSuccesses.Add(new UnifiedSuccessItem
                {
                    Type = "Legacy",
                    Id = success.Id,
                    ProjectId = success.ProjectId,
                    SuccessDescription = success.SuccessDescription,
                    CreatedByName = success.RecordedByName,
                    CreatedByEmail = success.RecordedByEmail,
                    CreatedAt = success.RecordedAt,
                    SltResponse = success.SltResponse,
                    SltRespondedByName = success.SltRespondedByName,
                    SltRespondedByEmail = success.SltRespondedByEmail,
                    SltRespondedAt = success.SltRespondedAt
                });
            }
            
            foreach (var success in weeklySuccesses)
            {
                allSuccesses.Add(new UnifiedSuccessItem
                {
                    Type = "Weekly",
                    Id = success.Id,
                    ProjectId = success.ProjectId,
                    SuccessDescription = success.SuccessDescription,
                    CreatedByName = success.CreatedByName,
                    CreatedByEmail = success.CreatedByEmail,
                    CreatedAt = success.CreatedAt,
                    SltResponse = success.SltResponse,
                    SltRespondedByName = success.SltRespondedByName,
                    SltRespondedByEmail = success.SltRespondedByEmail,
                    SltRespondedAt = success.SltRespondedAt
                });
            }

            // Get all active business areas from admin settings
            var allActiveBusinessAreas = await _context.BusinessAreaLookups
                .Where(ba => ba.IsActive)
                .OrderBy(ba => ba.SortOrder)
                .ThenBy(ba => ba.Name)
                .Select(ba => ba.Name)
                .ToListAsync();

            // Group successes by Business Area, then by Project
            var successGroupsByArea = allSuccesses
                .GroupBy(s => 
                {
                    var project = s.Type == "Legacy" 
                        ? legacySuccesses.First(ls => ls.Id == s.Id).Project
                        : weeklySuccesses.First(ws => ws.Id == s.Id).Project;
                    return project.BusinessAreaLookup?.Name ?? "Not assigned";
                })
                .ToDictionary(
                    baGroup => baGroup.Key,
                    baGroup => baGroup
                        .GroupBy(s => s.ProjectId)
                        .Select(pGroup =>
                        {
                            var firstSuccess = pGroup.First();
                            var project = firstSuccess.Type == "Legacy"
                                ? legacySuccesses.First(ls => ls.Id == firstSuccess.Id).Project
                                : weeklySuccesses.First(ws => ws.Id == firstSuccess.Id).Project;
                            
                            return new ProjectSuccessGroup
                            {
                                ProjectId = pGroup.Key,
                                ProjectTitle = project.Title,
                                ProjectCode = $"DFE-DDT-{project.Id}",
                                Successes = pGroup.OrderByDescending(s => s.CreatedAt).ToList()
                            };
                        })
                        .OrderBy(pg => pg.ProjectTitle)
                        .ToList()
                );

            // Create business area groups for ALL active business areas
            var businessAreaGroups = allActiveBusinessAreas
                .Select(areaName => 
                {
                    var hasSuccesses = successGroupsByArea.TryGetValue(areaName, out var projectGroups);
                    return new BusinessAreaSuccessGroup
                    {
                        BusinessAreaName = areaName,
                        UpdateCount = hasSuccesses ? projectGroups!.SelectMany(pg => pg.Successes).Count() : 0,
                        ProjectGroups = hasSuccesses ? projectGroups! : new List<ProjectSuccessGroup>()
                    };
                })
                .ToList();

            // Add "Not assigned" if there are successes without a business area
            if (successGroupsByArea.TryGetValue("Not assigned", out var notAssignedProjects))
            {
                businessAreaGroups.Add(new BusinessAreaSuccessGroup
                {
                    BusinessAreaName = "Not assigned",
                    UpdateCount = notAssignedProjects.SelectMany(pg => pg.Successes).Count(),
                    ProjectGroups = notAssignedProjects
                });
            }

            // Keep legacy successes list for backward compatibility
            var successes = legacySuccesses;

            // Get milestones due this week
            var milestones = await _context.Milestones
                .Include(m => m.Project)
                .Where(m => !m.IsDeleted && 
                           m.DueDate >= weekStart && 
                           m.DueDate <= weekEnd)
                .OrderBy(m => m.DueDate)
                .ToListAsync();

            // Get risks created or updated within this week (Thursday to Wednesday)
            var risks = await _context.Risks
                .Include(r => r.Project)
                    .ThenInclude(p => p.BusinessAreaLookup)
                .Where(r => !r.IsDeleted && 
                           r.ProjectId != null && 
                           (r.CreatedAt >= weekStart && r.CreatedAt <= weekEnd ||
                            r.UpdatedAt >= weekStart && r.UpdatedAt <= weekEnd))
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            // Get issues created or updated within this week (Thursday to Wednesday)
            var issues = await _context.Issues
                .Include(i => i.Project)
                    .ThenInclude(p => p.BusinessAreaLookup)
                .Where(i => !i.IsDeleted && 
                           i.ProjectId != null && 
                           (i.CreatedAt >= weekStart && i.CreatedAt <= weekEnd ||
                            i.UpdatedAt >= weekStart && i.UpdatedAt <= weekEnd))
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();

            // Get project status updates created within this week (Thursday to Wednesday)
            var updates = await _context.ProjectStatusUpdates
                .Include(u => u.Project)
                    .ThenInclude(p => p.BusinessAreaLookup)
                .Where(u => u.CreatedAt >= weekStart && u.CreatedAt <= weekEnd)
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            // Check for previous week (Thursday to Wednesday)
            var previousWeekStart = weekStart.AddDays(-7);
            var previousWeekEnd = previousWeekStart.AddDays(6).Date.AddHours(23).AddMinutes(59).AddSeconds(59);
            var hasPreviousWeek = await _context.ProjectSuccesses
                .AnyAsync(s => s.IsReportedToSlt && 
                              s.RecordedAt >= previousWeekStart && 
                              s.RecordedAt <= previousWeekEnd) ||
                await _context.ProjectWeeklySuccessUpdates
                    .AnyAsync(s => s.IsReportedToSlt && 
                                  s.WeekStartDate <= previousWeekEnd && 
                                  s.WeekEndDate >= previousWeekStart) ||
                await _context.Milestones
                    .AnyAsync(m => !m.IsDeleted && 
                                  m.DueDate >= previousWeekStart && 
                                  m.DueDate <= previousWeekEnd) ||
                await _context.Risks
                    .AnyAsync(r => !r.IsDeleted && r.ProjectId != null &&
                                  (r.CreatedAt >= previousWeekStart && r.CreatedAt <= previousWeekEnd ||
                                   r.UpdatedAt >= previousWeekStart && r.UpdatedAt <= previousWeekEnd)) ||
                await _context.Issues
                    .AnyAsync(i => !i.IsDeleted && i.ProjectId != null &&
                                  (i.CreatedAt >= previousWeekStart && i.CreatedAt <= previousWeekEnd ||
                                   i.UpdatedAt >= previousWeekStart && i.UpdatedAt <= previousWeekEnd)) ||
                await _context.ProjectStatusUpdates
                    .AnyAsync(u => u.CreatedAt >= previousWeekStart && u.CreatedAt <= previousWeekEnd);

            var prevWeekNumber = calendar.GetWeekOfYear(previousWeekStart, culture.DateTimeFormat.CalendarWeekRule, culture.DateTimeFormat.FirstDayOfWeek);
            var prevYear = previousWeekStart.Year;

            // Check for next week (Thursday to Wednesday)
            var nextWeekStart = weekStart.AddDays(7);
            var nextWeekEnd = nextWeekStart.AddDays(6).Date.AddHours(23).AddMinutes(59).AddSeconds(59);
            var hasNextWeek = await _context.ProjectSuccesses
                .AnyAsync(s => s.IsReportedToSlt && 
                              s.RecordedAt >= nextWeekStart && 
                              s.RecordedAt <= nextWeekEnd) ||
                await _context.ProjectWeeklySuccessUpdates
                    .AnyAsync(s => s.IsReportedToSlt && 
                                  s.WeekStartDate <= nextWeekEnd && 
                                  s.WeekEndDate >= nextWeekStart) ||
                await _context.Milestones
                    .AnyAsync(m => !m.IsDeleted && 
                                  m.DueDate >= nextWeekStart && 
                                  m.DueDate <= nextWeekEnd) ||
                await _context.Risks
                    .AnyAsync(r => !r.IsDeleted && r.ProjectId != null &&
                                  (r.CreatedAt >= nextWeekStart && r.CreatedAt <= nextWeekEnd ||
                                   r.UpdatedAt >= nextWeekStart && r.UpdatedAt <= nextWeekEnd)) ||
                await _context.Issues
                    .AnyAsync(i => !i.IsDeleted && i.ProjectId != null &&
                                  (i.CreatedAt >= nextWeekStart && i.CreatedAt <= nextWeekEnd ||
                                   i.UpdatedAt >= nextWeekStart && i.UpdatedAt <= nextWeekEnd)) ||
                await _context.ProjectStatusUpdates
                    .AnyAsync(u => u.CreatedAt >= nextWeekStart && u.CreatedAt <= nextWeekEnd);

            var nextWeekNumber = calendar.GetWeekOfYear(nextWeekStart, culture.DateTimeFormat.CalendarWeekRule, culture.DateTimeFormat.FirstDayOfWeek);
            var nextYear = nextWeekStart.Year;

            var viewModel = new SltWeeklyReportViewModel
            {
                Year = reportYear,
                WeekNumber = weekNumber,
                WeekStartDate = weekStart,
                WeekEndDate = weekEnd,
                Successes = successes,
                Milestones = milestones,
                Risks = risks,
                Issues = issues,
                Updates = updates,
                BusinessAreaGroups = businessAreaGroups,
                HasPreviousWeek = hasPreviousWeek,
                HasNextWeek = hasNextWeek,
                PreviousWeekYear = hasPreviousWeek ? prevYear : null,
                PreviousWeekNumber = hasPreviousWeek ? prevWeekNumber : null,
                NextWeekYear = hasNextWeek ? nextYear : null,
                NextWeekNumber = hasNextWeek ? nextWeekNumber : null
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading SLT weekly report");
            TempData["ErrorMessage"] = "An error occurred while loading the SLT weekly report. Please try again.";
            return View(new SltWeeklyReportViewModel());
        }
    }

    // POST: CentralOps/SaveSltResponse
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveSltResponse(int successId, string successType, string responseText)
    {
        try
        {
            // Get current user info
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? User.FindFirst("preferred_username")?.Value ?? "Unknown";
            var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? User.FindFirst("name")?.Value ?? "Unknown";

            if (string.IsNullOrWhiteSpace(responseText))
            {
                return Json(new { success = false, message = "Response text cannot be empty." });
            }

            if (successType == "Legacy")
            {
                var success = await _context.ProjectSuccesses.FindAsync(successId);
                if (success == null)
                {
                    return Json(new { success = false, message = "Success record not found." });
                }

                success.SltResponse = responseText;
                success.SltRespondedByEmail = userEmail;
                success.SltRespondedByName = userName;
                success.SltRespondedAt = DateTime.UtcNow;
            }
            else if (successType == "Weekly")
            {
                var success = await _context.ProjectWeeklySuccessUpdates.FindAsync(successId);
                if (success == null)
                {
                    return Json(new { success = false, message = "Success record not found." });
                }

                success.SltResponse = responseText;
                success.SltRespondedByEmail = userEmail;
                success.SltRespondedByName = userName;
                success.SltRespondedAt = DateTime.UtcNow;
            }
            else
            {
                return Json(new { success = false, message = "Invalid success type." });
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving SLT response for {SuccessType} {SuccessId}", successType, successId);
            return Json(new { success = false, message = "An error occurred while saving the response." });
        }
    }

    // POST: CentralOps/DeleteSltResponse
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSltResponse(int successId, string successType)
    {
        try
        {
            if (successType == "Legacy")
            {
                var success = await _context.ProjectSuccesses.FindAsync(successId);
                if (success == null)
                {
                    return Json(new { success = false, message = "Success record not found." });
                }

                success.SltResponse = null;
                success.SltRespondedByEmail = null;
                success.SltRespondedByName = null;
                success.SltRespondedAt = null;
            }
            else if (successType == "Weekly")
            {
                var success = await _context.ProjectWeeklySuccessUpdates.FindAsync(successId);
                if (success == null)
                {
                    return Json(new { success = false, message = "Success record not found." });
                }

                success.SltResponse = null;
                success.SltRespondedByEmail = null;
                success.SltRespondedByName = null;
                success.SltRespondedAt = null;
            }
            else
            {
                return Json(new { success = false, message = "Invalid success type." });
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting SLT response for {SuccessType} {SuccessId}", successType, successId);
            return Json(new { success = false, message = "An error occurred while deleting the response." });
        }
    }

    // POST: CentralOps/BulkUpdateProjects
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkUpdateProjects([FromBody] BulkUpdateRequest request)
    {
        try
        {
            if (request.ProjectIds == null || request.ProjectIds.Count == 0)
            {
                return Json(new { success = false, message = "No projects selected." });
            }

            var projects = await _context.Projects
                .Include(p => p.PrimaryContactUser)
                .Include(p => p.SeniorResponsibleOfficers)
                    .ThenInclude(sro => sro.User)
                .Include(p => p.Directorates)
                    .ThenInclude(d => d.Division)
                .Where(p => request.ProjectIds.Contains(p.Id) && !p.IsDeleted)
                .ToListAsync();

            if (!projects.Any())
            {
                return Json(new { success = false, message = "No valid projects found." });
            }

            int updatedCount = 0;

            foreach (var project in projects)
            {
                bool projectUpdated = false;

                // Update Primary Contact
                if (request.ClearPrimaryContact || (!string.IsNullOrEmpty(request.PrimaryContactObjectId) || !string.IsNullOrEmpty(request.PrimaryContactEmail)))
                {
                    User? primaryContactUser = null;
                    if (!request.ClearPrimaryContact)
                    {
                        if (Guid.TryParse(request.PrimaryContactObjectId, out var objectIdGuid))
                        {
                            try
                            {
                                primaryContactUser = await _userDirectoryService.EnsureUserAsync(objectIdGuid);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Failed to ensure user with ObjectId {ObjectId}", request.PrimaryContactObjectId);
                            }
                        }
                        if (primaryContactUser == null && !string.IsNullOrEmpty(request.PrimaryContactEmail))
                        {
                            primaryContactUser = await _context.Users
                                .FirstOrDefaultAsync(u => u.Email.ToLower() == request.PrimaryContactEmail.ToLower());
                        }
                    }
                    project.PrimaryContactUserId = primaryContactUser?.Id;
                    project.PrimaryContactUser = primaryContactUser;
                    projectUpdated = true;
                }

                // Update SRO
                if (request.ClearSro || (!string.IsNullOrEmpty(request.SroObjectId) || !string.IsNullOrEmpty(request.SroEmail)))
                {
                    if (request.ClearSro)
                    {
                        var existingSros = project.SeniorResponsibleOfficers.ToList();
                        foreach (var sro in existingSros)
                        {
                            _context.ProjectSeniorResponsibleOfficers.Remove(sro);
                        }
                        projectUpdated = true;
                    }
                    else
                    {
                        User? sroUser = null;
                        if (Guid.TryParse(request.SroObjectId, out var sroObjectIdGuid))
                        {
                            try
                            {
                                sroUser = await _userDirectoryService.EnsureUserAsync(sroObjectIdGuid);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Failed to ensure SRO user with ObjectId {ObjectId}", request.SroObjectId);
                            }
                        }
                        if (sroUser == null && !string.IsNullOrEmpty(request.SroEmail))
                        {
                            sroUser = await _context.Users
                                .FirstOrDefaultAsync(u => u.Email.ToLower() == request.SroEmail.ToLower());
                        }

                        if (sroUser != null)
                        {
                            // Check if SRO already exists
                            var existingSro = project.SeniorResponsibleOfficers
                                .FirstOrDefault(sro => sro.UserId == sroUser.Id);
                            if (existingSro == null)
                            {
                                // Remove existing SROs if we're replacing
                                var existingSros = project.SeniorResponsibleOfficers.ToList();
                                foreach (var sro in existingSros)
                                {
                                    _context.ProjectSeniorResponsibleOfficers.Remove(sro);
                                }
                                // Add new SRO
                                var newSro = new ProjectSeniorResponsibleOfficer
                                {
                                    ProjectId = project.Id,
                                    UserId = sroUser.Id,
                                    CreatedAt = DateTime.UtcNow
                                };
                                _context.ProjectSeniorResponsibleOfficers.Add(newSro);
                                projectUpdated = true;
                            }
                        }
                    }
                }

                // Update Business Area
                if (request.ClearBusinessArea || !string.IsNullOrEmpty(request.BusinessArea))
                {
                    if (request.ClearBusinessArea)
                    {
                        project.BusinessAreaId = null;
                        projectUpdated = true;
                    }
                    else if (!string.IsNullOrEmpty(request.BusinessArea))
                    {
                        var businessArea = await _context.BusinessAreaLookups
                            .FirstOrDefaultAsync(ba => ba.Name == request.BusinessArea && ba.IsActive);
                        if (businessArea != null)
                        {
                            project.BusinessAreaId = businessArea.Id;
                            projectUpdated = true;
                        }
                    }
                }

                // Update Priority
                if (request.ClearPriority || !string.IsNullOrEmpty(request.Priority))
                {
                    if (request.ClearPriority)
                    {
                        project.DeliveryPriorityId = null;
                        projectUpdated = true;
                    }
                    else if (!string.IsNullOrEmpty(request.Priority))
                    {
                        var priority = await _context.DeliveryPriorities
                            .FirstOrDefaultAsync(dp => dp.Name == request.Priority && dp.IsActive);
                        if (priority != null)
                        {
                            project.DeliveryPriorityId = priority.Id;
                            projectUpdated = true;
                        }
                    }
                }

                // Update Directorates
                if (request.ClearDirectorates || (request.DirectorateIds != null && request.DirectorateIds.Count > 0))
                {
                    if (request.ClearDirectorates)
                    {
                        var existingDirectorates = project.Directorates.ToList();
                        foreach (var dir in existingDirectorates)
                        {
                            _context.ProjectDirectorates.Remove(dir);
                        }
                        projectUpdated = true;
                    }
                    else if (request.DirectorateIds != null && request.DirectorateIds.Count > 0)
                    {
                        // Get existing directorate IDs
                        var existingDirectorateIds = project.Directorates
                            .Select(d => d.DivisionId)
                            .ToList();

                        // Add new directorates
                        foreach (var directorateId in request.DirectorateIds)
                        {
                            if (!existingDirectorateIds.Contains(directorateId))
                            {
                                var newDirectorate = new ProjectDirectorate
                                {
                                    ProjectId = project.Id,
                                    DivisionId = directorateId,
                                    CreatedAt = DateTime.UtcNow
                                };
                                _context.ProjectDirectorates.Add(newDirectorate);
                                projectUpdated = true;
                            }
                        }
                    }
                }

                if (projectUpdated)
                {
                    project.UpdatedAt = DateTime.UtcNow;
                    updatedCount++;
                }
            }

            await _context.SaveChangesAsync();

            return Json(new { success = true, updatedCount = updatedCount });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing bulk update");
            return Json(new { success = false, message = "An error occurred while updating projects: " + ex.Message });
        }
    }

    // GET: CentralOps/MonthlySummary
    public async Task<IActionResult> MonthlySummary(int? year, int? month)
    {
        try
        {
            // Default to current month if not specified
            var currentDate = DateTime.UtcNow;
            var reportYear = year ?? currentDate.Year;
            var reportMonth = month ?? currentDate.Month;
            
            // Calculate month boundaries
            var monthStart = new DateTime(reportYear, reportMonth, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59);
            
            // Calculate previous and next months for navigation
            var prevMonth = reportMonth == 1 ? 12 : reportMonth - 1;
            var prevYear = reportMonth == 1 ? reportYear - 1 : reportYear;
            var nextMonth = reportMonth == 12 ? 1 : reportMonth + 1;
            var nextYear = reportMonth == 12 ? reportYear + 1 : reportYear;
            
            // Get all active projects (exclude Cancelled and Completed)
            var allProjects = await _context.Projects
                .Include(p => p.PrimaryContactUser)
                .Include(p => p.DeliveryPriority)
                .Include(p => p.BusinessAreaLookup)
                .Include(p => p.Milestones)
                .Include(p => p.Risks)
                .Include(p => p.Issues)
                .Include(p => p.MonthlyUpdates)
                .Where(p => !p.IsDeleted && p.Status != "Cancelled" && p.Status != "Completed")
                .ToListAsync();
            
            // 1. New projects added this month
            var newProjects = allProjects
                .Where(p => p.CreatedAt >= monthStart && p.CreatedAt <= monthEnd)
                .OrderByDescending(p => p.CreatedAt)
                .ToList();
            
            // 2. Key milestones from priority projects (High/Critical priority)
            var priorityProjects = allProjects
                .Where(p => p.DeliveryPriority != null && 
                           (p.DeliveryPriority.Name.ToLower().Contains("high") || 
                            p.DeliveryPriority.Name.ToLower().Contains("critical")))
                .ToList();
            
            var keyMilestones = priorityProjects
                .SelectMany(p => p.Milestones
                    .Where(m => !m.IsDeleted && 
                               m.DueDate >= monthStart && 
                               m.DueDate <= monthEnd.AddMonths(3)) // Show milestones due in next 3 months
                    .Select(m => new MilestoneWithProject { Project = p, Milestone = m }))
                .OrderBy(x => x.Milestone.DueDate)
                .ToList();
            
            // 3. Milestones achieved this month
            var achievedMilestones = allProjects
                .SelectMany(p => p.Milestones
                    .Where(m => !m.IsDeleted && 
                               m.Status == "complete" && 
                               m.ActualDate.HasValue &&
                               m.ActualDate.Value >= monthStart && 
                               m.ActualDate.Value <= monthEnd)
                    .Select(m => new MilestoneWithProject { Project = p, Milestone = m }))
                .OrderByDescending(x => x.Milestone.ActualDate)
                .ToList();
            
            // 4. Open and new high risk items
            var highRiskItems = allProjects
                .SelectMany(p => p.Risks
                    .Where(r => !r.IsDeleted && 
                               (r.Status != "closed" || r.ClosedDate == null) &&
                               (r.RiskScore >= 15 || // High risk score threshold
                                r.ImpactRating >= 4 || 
                                r.LikelihoodRating >= 4 ||
                                (r.CreatedAt >= monthStart && r.CreatedAt <= monthEnd))) // New this month
                    .Select(r => new RiskWithProject { Project = p, Risk = r }))
                .OrderByDescending(x => x.Risk.RiskScore)
                .ThenByDescending(x => x.Risk.CreatedAt)
                .ToList();
            
            // 5. Open and high priority issues
            var highPriorityIssues = allProjects
                .SelectMany(p => p.Issues
                    .Where(i => !i.IsDeleted && 
                               (i.Status != "resolved" && i.Status != "closed") &&
                               (i.Severity.ToLower() == "high" || 
                                i.Severity.ToLower() == "critical" ||
                                i.Priority != null && (i.Priority.ToLower() == "high" || i.Priority.ToLower() == "critical") ||
                                (i.CreatedAt >= monthStart && i.CreatedAt <= monthEnd))) // New this month
                    .Select(i => new IssueWithProject { Project = p, Issue = i }))
                .OrderByDescending(x => x.Issue.Severity == "critical" ? 2 : x.Issue.Severity == "high" ? 1 : 0)
                .ThenByDescending(x => x.Issue.CreatedAt)
                .ToList();
            
            // 6. Portfolio Summary
            var portfolioSummary = new PortfolioSummaryViewModel
            {
                TotalProjects = allProjects.Count,
                ActiveProjects = allProjects.Count(p => p.Status == "Active"),
                PausedProjects = allProjects.Count(p => p.Status == "Paused"),
                CompletedProjects = allProjects.Count(p => p.Status == "Completed"),
                CancelledProjects = allProjects.Count(p => p.Status == "Cancelled"),
                RedProjects = allProjects.Count(p => NormalizeRagStatus(p.RagStatus) == "Red"),
                AmberRedProjects = allProjects.Count(p => NormalizeRagStatus(p.RagStatus) == "Amber-Red"),
                AmberProjects = allProjects.Count(p => NormalizeRagStatus(p.RagStatus) == "Amber"),
                AmberGreenProjects = allProjects.Count(p => NormalizeRagStatus(p.RagStatus) == "Amber-Green"),
                GreenProjects = allProjects.Count(p => NormalizeRagStatus(p.RagStatus) == "Green"),
                CriticalPriorityProjects = allProjects.Count(p => p.DeliveryPriority != null && p.DeliveryPriority.Name.ToLower().Contains("critical")),
                HighPriorityProjects = allProjects.Count(p => p.DeliveryPriority != null && p.DeliveryPriority.Name.ToLower().Contains("high") && !p.DeliveryPriority.Name.ToLower().Contains("critical")),
                MediumPriorityProjects = allProjects.Count(p => p.DeliveryPriority != null && p.DeliveryPriority.Name.ToLower().Contains("medium")),
                LowPriorityProjects = allProjects.Count(p => p.DeliveryPriority != null && p.DeliveryPriority.Name.ToLower().Contains("low")),
                PriorityNotSetProjects = allProjects.Count(p => p.DeliveryPriority == null),
                TotalMilestones = allProjects.SelectMany(p => p.Milestones).Count(m => !m.IsDeleted),
                CompletedMilestones = allProjects.SelectMany(p => p.Milestones).Count(m => !m.IsDeleted && m.Status == "complete"),
                TotalRisks = allProjects.SelectMany(p => p.Risks).Count(r => !r.IsDeleted),
                OpenRisks = allProjects.SelectMany(p => p.Risks).Count(r => !r.IsDeleted && r.Status != "closed"),
                TotalIssues = allProjects.SelectMany(p => p.Issues).Count(i => !i.IsDeleted),
                OpenIssues = allProjects.SelectMany(p => p.Issues).Count(i => !i.IsDeleted && i.Status != "resolved" && i.Status != "closed")
            };
            
            // Monthly update stats for the selected month
            var monthlyUpdateStats = CalculateMonthlyUpdateStats(allProjects, reportYear, reportMonth, _monthlyUpdateService);
            
            ViewBag.Year = reportYear;
            ViewBag.Month = reportMonth;
            ViewBag.MonthName = monthStart.ToString("MMMM yyyy");
            ViewBag.MonthStart = monthStart;
            ViewBag.MonthEnd = monthEnd;
            ViewBag.PreviousMonth = prevMonth;
            ViewBag.PreviousYear = prevYear;
            ViewBag.NextMonth = nextMonth;
            ViewBag.NextYear = nextYear;
            ViewBag.NewProjects = newProjects;
            ViewBag.KeyMilestones = keyMilestones;
            ViewBag.AchievedMilestones = achievedMilestones;
            ViewBag.HighRiskItems = highRiskItems;
            ViewBag.HighPriorityIssues = highPriorityIssues;
            ViewBag.PortfolioSummary = portfolioSummary;
            ViewBag.MonthlyUpdateStats = monthlyUpdateStats;
            ViewBag.AllProjects = allProjects;
            
            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading monthly summary for {Year}-{Month}", year, month);
            TempData["ErrorMessage"] = "An error occurred while loading the monthly summary. Please try again.";
            return RedirectToAction(nameof(Dashboard));
        }
    }

    // GET: CentralOps/MonthlySummaryV2
    public async Task<IActionResult> MonthlySummaryV2(int? year, int? month, int? businessAreaId)
    {
        try
        {
            // Determine current reporting period using 10-day rule
            var currentDate = DateTime.UtcNow;
            var currentYear = currentDate.Year;
            var currentMonth = currentDate.Month;
            
            var currentPeriodDueDate = _monthlyUpdateService.GetMonthlyUpdateDueDate(currentYear, currentMonth);
            var daysUntilCurrentPeriodDueDate = (currentPeriodDueDate - currentDate).Days;
            
            // Apply 10-day rule: if within 10 days of current period due date, use current period
            var defaultReportYear = daysUntilCurrentPeriodDueDate <= 10 ? currentYear : (currentMonth == 1 ? currentYear - 1 : currentYear);
            var defaultReportMonth = daysUntilCurrentPeriodDueDate <= 10 ? currentMonth : (currentMonth == 1 ? 12 : currentMonth - 1);
            
            // Use provided year/month if specified, otherwise use determined period
            var reportYear = year ?? defaultReportYear;
            var reportMonth = month ?? defaultReportMonth;
            
            // Calculate month boundaries
            var monthStart = new DateTime(reportYear, reportMonth, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59);
            
            // Get all active projects (exclude Cancelled and Completed)
            var query = _context.Projects
                .Include(p => p.PrimaryContactUser)
                .Include(p => p.DeliveryPriority)
                .Include(p => p.BusinessAreaLookup)
                .Include(p => p.Milestones)
                .Include(p => p.Risks)
                .Include(p => p.Issues)
                .Include(p => p.MonthlyUpdates)
                .Include(p => p.RagStatusLookup)
                .Include(p => p.ProblemStatements)
                .Where(p => !p.IsDeleted && p.Status != "Cancelled" && p.Status != "Completed");
            
            // Filter by business area if specified
            if (businessAreaId.HasValue)
            {
                query = query.Where(p => p.BusinessAreaId == businessAreaId.Value);
            }
            
            var allProjects = await query.ToListAsync();
            
            // Get all business areas for the filter dropdown
            var businessAreas = await _context.BusinessAreaLookups
                .Where(ba => ba.IsActive)
                .OrderBy(ba => ba.SortOrder)
                .ThenBy(ba => ba.Name)
                .ToListAsync();
            
            // 1. Summary data
            var totalActiveProjects = allProjects.Count;
            var newProjectsThisMonth = allProjects
                .Where(p => p.CreatedAt >= monthStart && p.CreatedAt <= monthEnd)
                .ToList();
            
            var milestonesAchieved = allProjects
                .SelectMany(p => p.Milestones
                    .Where(m => !m.IsDeleted && 
                               m.Status == "complete" && 
                               m.ActualDate.HasValue &&
                               m.ActualDate.Value >= monthStart && 
                               m.ActualDate.Value <= monthEnd)
                    .Select(m => new MilestoneWithProject { Project = p, Milestone = m }))
                .ToList();
            
            // 2. Monthly update stats
            var monthlyUpdateStats = CalculateMonthlyUpdateStats(allProjects, reportYear, reportMonth, _monthlyUpdateService);
            
            // 3. RAG Status distribution
            var ragDistribution = new Dictionary<string, int>
            {
                { "Red", 0 },
                { "Amber-Red", 0 },
                { "Amber", 0 },
                { "Amber-Green", 0 },
                { "Green", 0 },
                { "Not Set", 0 }
            };
            
            foreach (var project in allProjects)
            {
                var ragStatus = NormalizeRagStatus(project.RagStatusLookup?.Name ?? project.RagStatus);
                if (string.IsNullOrWhiteSpace(ragStatus))
                {
                    ragDistribution["Not Set"]++;
                }
                else if (ragDistribution.ContainsKey(ragStatus))
                {
                    ragDistribution[ragStatus]++;
                }
            }
            
            // 4. Priority distribution
            var priorityDistribution = new Dictionary<string, int>
            {
                { "Critical", 0 },
                { "High", 0 },
                { "Medium", 0 },
                { "Low", 0 },
                { "Not Set", 0 }
            };
            
            foreach (var project in allProjects)
            {
                if (project.DeliveryPriority == null)
                {
                    priorityDistribution["Not Set"]++;
                }
                else
                {
                    var priorityName = project.DeliveryPriority.Name.ToLower();
                    if (priorityName.Contains("critical"))
                    {
                        priorityDistribution["Critical"]++;
                    }
                    else if (priorityName.Contains("high"))
                    {
                        priorityDistribution["High"]++;
                    }
                    else if (priorityName.Contains("medium"))
                    {
                        priorityDistribution["Medium"]++;
                    }
                    else if (priorityName.Contains("low"))
                    {
                        priorityDistribution["Low"]++;
                    }
                    else
                    {
                        priorityDistribution["Not Set"]++;
                    }
                }
            }
            
            // 5. New projects this month
            var newProjectsList = newProjectsThisMonth
                .OrderByDescending(p => p.CreatedAt)
                .ToList();
            
            // 6. Upcoming milestones (due in the selected month)
            var upcomingMilestones = allProjects
                .SelectMany(p => p.Milestones
                    .Where(m => !m.IsDeleted && 
                               m.Status != "complete" && 
                               m.Status != "cancelled" &&
                               m.DueDate >= monthStart && 
                               m.DueDate <= monthEnd)
                    .Select(m => new MilestoneWithProject { Project = p, Milestone = m }))
                .OrderBy(x => x.Milestone.DueDate)
                .ToList();
            
            // 7. Late milestones (only count as late if due date is before today)
            var todayUtcDate = DateTime.UtcNow.Date;
            var lateMilestones = allProjects
                .SelectMany(p => p.Milestones
                    .Where(m => !m.IsDeleted && 
                               m.Status != "complete" && 
                               m.Status != "cancelled" &&
                               m.DueDate < monthEnd &&
                               m.DueDate.Date < todayUtcDate)
                    .Select(m => new MilestoneWithProject { Project = p, Milestone = m }))
                .OrderBy(x => x.Milestone.DueDate)
                .ToList();
            
            // 8. Business area summary
            var businessAreaSummaries = allProjects
                .GroupBy(p => p.BusinessAreaLookup)
                .Select(g => new BusinessAreaSummaryViewModel
                {
                    BusinessArea = g.Key?.Name ?? "Not Set",
                    BusinessAreaId = g.Key?.Id,
                    TotalProjects = g.Count(),
                    NewThisMonth = g.Count(p => p.CreatedAt >= monthStart && p.CreatedAt <= monthEnd),
                    MilestonesAchieved = g.SelectMany(p => p.Milestones
                        .Where(m => !m.IsDeleted && 
                                   m.Status == "complete" && 
                                   m.ActualDate.HasValue &&
                                   m.ActualDate.Value >= monthStart && 
                                   m.ActualDate.Value <= monthEnd)).Count(),
                    UpcomingMilestones = g.SelectMany(p => p.Milestones
                        .Where(m => !m.IsDeleted && 
                                   m.Status != "complete" && 
                                   m.Status != "cancelled" &&
                                   m.DueDate >= monthStart && 
                                   m.DueDate <= monthEnd)).Count(),
                    LateMilestones = g.SelectMany(p => p.Milestones
                        .Where(m => !m.IsDeleted && 
                                   m.Status != "complete" && 
                                   m.Status != "cancelled" &&
                                   m.DueDate < monthEnd)).Count(),
                    RedCount = g.Count(p => NormalizeRagStatus(p.RagStatusLookup?.Name ?? p.RagStatus) == "Red"),
                    AmberRedCount = g.Count(p => NormalizeRagStatus(p.RagStatusLookup?.Name ?? p.RagStatus) == "Amber-Red"),
                    AmberCount = g.Count(p => NormalizeRagStatus(p.RagStatusLookup?.Name ?? p.RagStatus) == "Amber"),
                    AmberGreenCount = g.Count(p => NormalizeRagStatus(p.RagStatusLookup?.Name ?? p.RagStatus) == "Amber-Green"),
                    GreenCount = g.Count(p => NormalizeRagStatus(p.RagStatusLookup?.Name ?? p.RagStatus) == "Green"),
                    CriticalCount = g.Count(p => p.DeliveryPriority != null && p.DeliveryPriority.Name.ToLower().Contains("critical")),
                    HighCount = g.Count(p => p.DeliveryPriority != null && p.DeliveryPriority.Name.ToLower().Contains("high") && !p.DeliveryPriority.Name.ToLower().Contains("critical")),
                    MediumCount = g.Count(p => p.DeliveryPriority != null && p.DeliveryPriority.Name.ToLower().Contains("medium") && !p.DeliveryPriority.Name.ToLower().Contains("high") && !p.DeliveryPriority.Name.ToLower().Contains("critical")),
                    LowCount = g.Count(p => p.DeliveryPriority != null && p.DeliveryPriority.Name.ToLower().Contains("low") && !p.DeliveryPriority.Name.ToLower().Contains("medium") && !p.DeliveryPriority.Name.ToLower().Contains("high") && !p.DeliveryPriority.Name.ToLower().Contains("critical")),
                    MonthlyUpdateStats = CalculateMonthlyUpdateStats(g.ToList(), reportYear, reportMonth, _monthlyUpdateService)
                })
                .OrderBy(ba => ba.BusinessArea)
                .ToList();
            
            // 9. Projects with path to green (non-green RAG status with path to green defined)
            // Order by RAG status from worst to best: Red, Amber-Red, Amber, Amber-Green
            var projectsWithPathToGreen = allProjects
                .Where(p => !string.IsNullOrWhiteSpace(p.PathToGreen) && 
                           NormalizeRagStatus(p.RagStatusLookup?.Name ?? p.RagStatus) != "Green" &&
                           !string.IsNullOrWhiteSpace(NormalizeRagStatus(p.RagStatusLookup?.Name ?? p.RagStatus)))
                .OrderBy(p => 
                {
                    var ragStatus = NormalizeRagStatus(p.RagStatusLookup?.Name ?? p.RagStatus);
                    return ragStatus switch
                    {
                        "Red" => 1,
                        "Amber-Red" => 2,
                        "Amber" => 3,
                        "Amber-Green" => 4,
                        "Green" => 5,
                        _ => 99
                    };
                })
                .ThenBy(p => p.BusinessAreaLookup?.Name ?? "ZZZ")
                .ThenBy(p => p.Title)
                .ToList();
            
            // 10. Work items and active projects for selected business area
            var businessAreaRisks = new List<RiskWithProject>();
            var businessAreaIssues = new List<IssueWithProject>();
            var businessAreaActiveProjects = new List<Compass.Models.Project>();
            Compass.Models.BusinessAreaLookup? selectedBusinessArea = null;
            
            if (businessAreaId.HasValue)
            {
                selectedBusinessArea = await _context.BusinessAreaLookups.FindAsync(businessAreaId.Value);
                if (selectedBusinessArea != null)
                {
                    // Get active projects for this business area
                    businessAreaActiveProjects = allProjects
                        .Where(p => p.BusinessAreaId == businessAreaId.Value)
                        .OrderBy(p => p.Title)
                        .ToList();
                    
                    // Get risks for projects in this business area
                    businessAreaRisks = allProjects
                        .Where(p => p.BusinessAreaId == businessAreaId.Value)
                        .SelectMany(p => p.Risks
                            .Where(r => !r.IsDeleted && (r.Status != "closed" || r.ClosedDate == null))
                            .Select(r => new RiskWithProject { Project = p, Risk = r }))
                        .OrderByDescending(x => x.Risk.RiskScore)
                        .ThenByDescending(x => x.Risk.CreatedAt)
                        .ToList();
                    
                    // Get issues for projects in this business area
                    businessAreaIssues = allProjects
                        .Where(p => p.BusinessAreaId == businessAreaId.Value)
                        .SelectMany(p => p.Issues
                            .Where(i => !i.IsDeleted && i.Status != "resolved" && i.Status != "closed")
                            .Select(i => new IssueWithProject { Project = p, Issue = i }))
                        .OrderByDescending(x => x.Issue.Severity == "critical" ? 2 : x.Issue.Severity == "high" ? 1 : 0)
                        .ThenByDescending(x => x.Issue.CreatedAt)
                        .ToList();
                }
            }
            
            ViewBag.Year = reportYear;
            ViewBag.Month = reportMonth;
            ViewBag.MonthName = monthStart.ToString("MMMM yyyy");
            ViewBag.MonthStart = monthStart;
            ViewBag.MonthEnd = monthEnd;
            ViewBag.BusinessAreaId = businessAreaId;
            ViewBag.BusinessAreas = businessAreas;
            ViewBag.TotalActiveProjects = totalActiveProjects;
            ViewBag.NewProjectsThisMonth = newProjectsList;
            ViewBag.MilestonesAchieved = milestonesAchieved;
            ViewBag.MonthlyUpdateStats = monthlyUpdateStats;
            ViewBag.RagDistribution = ragDistribution;
            ViewBag.PriorityDistribution = priorityDistribution;
            ViewBag.UpcomingMilestones = upcomingMilestones;
            ViewBag.LateMilestones = lateMilestones;
            ViewBag.BusinessAreaSummaries = businessAreaSummaries;
            ViewBag.ProjectsWithPathToGreen = projectsWithPathToGreen;
            ViewBag.BusinessAreaRisks = businessAreaRisks;
            ViewBag.BusinessAreaIssues = businessAreaIssues;
            ViewBag.BusinessAreaActiveProjects = businessAreaActiveProjects;
            
            // Month-over-month trends - calculate previous month data
            var prevMonth = reportMonth == 1 ? 12 : reportMonth - 1;
            var prevYear = reportMonth == 1 ? reportYear - 1 : reportYear;
            var prevMonthStart = new DateTime(prevYear, prevMonth, 1);
            var prevMonthEnd = prevMonthStart.AddMonths(1).AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59);
            
            // Get projects for previous month (same filter logic)
            var prevMonthQuery = _context.Projects
                .Include(p => p.Milestones)
                .Include(p => p.MonthlyUpdates)
                .Include(p => p.RagStatusLookup)
                .Include(p => p.DeliveryPriority)
                .Where(p => !p.IsDeleted && p.Status != "Cancelled" && p.Status != "Completed");
            
            if (businessAreaId.HasValue)
            {
                prevMonthQuery = prevMonthQuery.Where(p => p.BusinessAreaId == businessAreaId.Value);
            }
            
            var prevMonthProjects = await prevMonthQuery.ToListAsync();
            
            // Get RAG history to track changes during the current month
            var ragHistoryDuringMonth = await _context.ProjectRagHistories
                .Include(rh => rh.Project)
                .Include(rh => rh.RagStatusLookup)
                .Where(rh => rh.ChangedAt >= monthStart && rh.ChangedAt <= monthEnd && !rh.Project.IsDeleted)
                .ToListAsync();
            
            if (businessAreaId.HasValue)
            {
                ragHistoryDuringMonth = ragHistoryDuringMonth
                    .Where(rh => rh.Project.BusinessAreaId == businessAreaId.Value)
                    .ToList();
            }
            
            // Projects that changed RAG status during the month
            var projectsWithRagChange = ragHistoryDuringMonth
                .Select(rh => rh.ProjectId)
                .Distinct()
                .Count();
            
            // Projects that changed Status during the month (projects updated during month with status change)
            // Note: We can't track historical status changes easily, so we'll use UpdatedAt as a proxy
            // This is an approximation - projects updated during the month may have had status changes
            var projectsWithStatusChange = allProjects
                .Where(p => p.UpdatedAt >= monthStart && 
                           p.UpdatedAt <= monthEnd &&
                           prevMonthProjects.Any(pp => pp.Id == p.Id && pp.Status != p.Status))
                .Count();
            
            // Projects that changed Priority during the month
            var projectsWithPriorityChange = allProjects
                .Where(p => p.UpdatedAt >= monthStart && 
                           p.UpdatedAt <= monthEnd &&
                           prevMonthProjects.Any(pp => pp.Id == p.Id && 
                           ((pp.DeliveryPriorityId == null && p.DeliveryPriorityId != null) ||
                            (pp.DeliveryPriorityId != null && p.DeliveryPriorityId == null) ||
                            (pp.DeliveryPriorityId != p.DeliveryPriorityId))))
                .Count();
            
            // Calculate previous month stats
            var prevMonthTotalProjects = prevMonthProjects.Count;
            var prevMonthNewProjects = prevMonthProjects.Count(p => p.CreatedAt >= prevMonthStart && p.CreatedAt <= prevMonthEnd);
            var prevMonthMilestonesAchieved = prevMonthProjects
                .SelectMany(p => p.Milestones
                    .Where(m => !m.IsDeleted && 
                               m.Status == "complete" && 
                               m.ActualDate.HasValue &&
                               m.ActualDate.Value >= prevMonthStart && 
                               m.ActualDate.Value <= prevMonthEnd))
                .Count();
            var prevMonthUpdateStats = CalculateMonthlyUpdateStats(prevMonthProjects, prevYear, prevMonth, _monthlyUpdateService);
            
            // Calculate previous month RAG distribution
            // Get RAG history up to the end of the previous month to find what RAG status each project had
            var prevMonthProjectIds = prevMonthProjects.Select(p => p.Id).ToList();
            var ragHistoryUpToPrevMonth = await _context.ProjectRagHistories
                .Include(rh => rh.RagStatusLookup)
                .Where(rh => rh.ChangedAt < monthStart && 
                            prevMonthProjectIds.Contains(rh.ProjectId) && 
                            !rh.Project.IsDeleted)
                .OrderByDescending(rh => rh.ChangedAt)
                .ToListAsync();
            
            // Create a dictionary of project ID to their last RAG status before the current month
            // Group by project and take the most recent entry for each project
            var projectRagStatusAtPrevMonthEnd = ragHistoryUpToPrevMonth
                .GroupBy(rh => rh.ProjectId)
                .ToDictionary(g => g.Key, g => NormalizeRagStatus(g.First().RagStatusLookup?.Name ?? g.First().RagStatus));
            
            var prevMonthRagDistribution = new Dictionary<string, int>
            {
                { "Red", 0 },
                { "Amber-Red", 0 },
                { "Amber", 0 },
                { "Amber-Green", 0 },
                { "Green", 0 },
                { "Not Set", 0 }
            };
            
            foreach (var project in prevMonthProjects)
            {
                string ragStatus;
                // If we have history for this project, use the last RAG status before current month
                if (projectRagStatusAtPrevMonthEnd.ContainsKey(project.Id))
                {
                    ragStatus = projectRagStatusAtPrevMonthEnd[project.Id];
                }
                else
                {
                    // No history, use current RAG status (assumes it hasn't changed since project creation)
                    ragStatus = NormalizeRagStatus(project.RagStatusLookup?.Name ?? project.RagStatus);
                }
                
                if (string.IsNullOrWhiteSpace(ragStatus))
                {
                    prevMonthRagDistribution["Not Set"]++;
                }
                else if (prevMonthRagDistribution.ContainsKey(ragStatus))
                {
                    prevMonthRagDistribution[ragStatus]++;
                }
            }
            
            // Calculate previous month Priority distribution
            var prevMonthPriorityDistribution = new Dictionary<string, int>
            {
                { "Critical", 0 },
                { "High", 0 },
                { "Medium", 0 },
                { "Low", 0 },
                { "Not Set", 0 }
            };
            
            foreach (var project in prevMonthProjects)
            {
                if (project.DeliveryPriority == null)
                {
                    prevMonthPriorityDistribution["Not Set"]++;
                }
                else
                {
                    var priorityName = project.DeliveryPriority.Name.ToLower();
                    if (priorityName.Contains("critical"))
                    {
                        prevMonthPriorityDistribution["Critical"]++;
                    }
                    else if (priorityName.Contains("high"))
                    {
                        prevMonthPriorityDistribution["High"]++;
                    }
                    else if (priorityName.Contains("medium"))
                    {
                        prevMonthPriorityDistribution["Medium"]++;
                    }
                    else if (priorityName.Contains("low"))
                    {
                        prevMonthPriorityDistribution["Low"]++;
                    }
                    else
                    {
                        prevMonthPriorityDistribution["Not Set"]++;
                    }
                }
            }
            
            // Flagship projects
            var flagshipProjects = allProjects
                .Where(p => p.IsFlagship)
                .OrderBy(p => p.BusinessAreaLookup?.Name ?? "ZZZ")
                .ThenBy(p => p.Title)
                .ToList();
            
            // Resource summary (FTE)
            var totalPermFte = allProjects
                .Where(p => p.TotalPermFte.HasValue)
                .Sum(p => p.TotalPermFte.Value);
            var totalMspFte = allProjects
                .Where(p => p.TotalMspFte.HasValue)
                .Sum(p => p.TotalMspFte.Value);
            var totalFte = totalPermFte + totalMspFte;
            
            // Resource summary by business area (if not in business area view)
            var resourceByBusinessArea = new List<object>();
            if (!businessAreaId.HasValue)
            {
                resourceByBusinessArea = allProjects
                    .Where(p => p.BusinessAreaLookup != null)
                    .GroupBy(p => new { p.BusinessAreaId, p.BusinessAreaLookup!.Name })
                    .Select(g => new
                    {
                        BusinessAreaId = g.Key.BusinessAreaId,
                        BusinessAreaName = g.Key.Name,
                        TotalPermFte = g.Where(p => p.TotalPermFte.HasValue).Sum(p => p.TotalPermFte!.Value),
                        TotalMspFte = g.Where(p => p.TotalMspFte.HasValue).Sum(p => p.TotalMspFte!.Value),
                        TotalFte = g.Where(p => p.TotalPermFte.HasValue || p.TotalMspFte.HasValue)
                            .Sum(p => (p.TotalPermFte ?? 0) + (p.TotalMspFte ?? 0)),
                        ProjectCount = g.Count(),
                        Projects = g.ToList()
                    })
                    .OrderBy(x => x.BusinessAreaName)
                    .ToList<object>();
            }
            
            ViewBag.PrevMonthTotalProjects = prevMonthTotalProjects;
            ViewBag.PrevMonthNewProjects = prevMonthNewProjects;
            ViewBag.PrevMonthMilestonesAchieved = prevMonthMilestonesAchieved;
            ViewBag.PrevMonthUpdateStats = prevMonthUpdateStats;
            ViewBag.PrevMonthRagDistribution = prevMonthRagDistribution;
            ViewBag.PrevMonthPriorityDistribution = prevMonthPriorityDistribution;
            ViewBag.PrevMonthName = prevMonthStart.ToString("MMMM yyyy");
            ViewBag.ProjectsWithRagChange = projectsWithRagChange;
            ViewBag.ProjectsWithStatusChange = projectsWithStatusChange;
            ViewBag.ProjectsWithPriorityChange = projectsWithPriorityChange;
            ViewBag.FlagshipProjects = flagshipProjects;
            ViewBag.TotalPermFte = totalPermFte;
            ViewBag.TotalMspFte = totalMspFte;
            ViewBag.TotalFte = totalFte;
            ViewBag.ResourceByBusinessArea = resourceByBusinessArea;
            ViewBag.AllProjects = allProjects;
            
            // Generate narrative summary
            var selectedBusinessAreaName = businessAreaId.HasValue 
                ? businessAreas.FirstOrDefault(ba => ba.Id == businessAreaId.Value)?.Name 
                : null;
            var narrativeSummary = GenerateNarrativeSummary(
                allProjects,
                monthlyUpdateStats,
                ragDistribution,
                priorityDistribution,
                newProjectsList,
                milestonesAchieved,
                upcomingMilestones,
                lateMilestones,
                projectsWithPathToGreen,
                businessAreaRisks,
                businessAreaIssues,
                businessAreaId,
                selectedBusinessAreaName,
                monthStart.ToString("MMMM yyyy"));
            ViewBag.NarrativeSummary = narrativeSummary;
            
            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading monthly summary v2 for {Year}-{Month}", year, month);
            TempData["ErrorMessage"] = "An error occurred while loading the monthly summary. Please try again.";
            return RedirectToAction(nameof(Dashboard));
        }
    }

    // GET: CentralOps/ExportMonthlySummaryV2
    public async Task<IActionResult> ExportMonthlySummaryV2(int? year, int? month, int? businessAreaId)
    {
        try
        {
            // Default to current month if not specified
            var currentDate = DateTime.UtcNow;
            var reportYear = year ?? currentDate.Year;
            var reportMonth = month ?? currentDate.Month;
            
            // Calculate month boundaries
            var monthStart = new DateTime(reportYear, reportMonth, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59);
            
            // Get all active projects (exclude Cancelled and Completed) - same query as MonthlySummaryV2
            var query = _context.Projects
                .Include(p => p.PrimaryContactUser)
                .Include(p => p.DeliveryPriority)
                .Include(p => p.BusinessAreaLookup)
                .Include(p => p.Milestones)
                .Include(p => p.Risks)
                .Include(p => p.Issues)
                .Include(p => p.MonthlyUpdates)
                    .ThenInclude(mu => mu.MonthlyUpdateNarratives)
                .Include(p => p.RagStatusLookup)
                .Include(p => p.ProblemStatements)
                .Where(p => !p.IsDeleted && p.Status != "Cancelled" && p.Status != "Completed");
            
            // Filter by business area if specified
            if (businessAreaId.HasValue)
            {
                query = query.Where(p => p.BusinessAreaId == businessAreaId.Value);
            }
            
            var allProjects = await query
                .AsNoTracking()
                .ToListAsync();
            
            // Order in memory to avoid null propagating operator in expression tree
            allProjects = allProjects
                .OrderBy(p => p.BusinessAreaLookup?.Name ?? "ZZZ")
                .ThenBy(p => p.Title)
                .ToList();

            if (!allProjects.Any())
            {
                TempData["ErrorMessage"] = "No projects found to export.";
                return RedirectToAction("MonthlySummaryV2", new { year = reportYear, month = reportMonth, businessAreaId });
            }

            // Load ProblemStatements for all projects in batch (AsNoTracking may not populate navigation properties)
            var projectIds = allProjects.Select(p => p.Id).ToList();
            var problemStatements = await _context.ProjectProblemStatements
                .Where(ps => projectIds.Contains(ps.ProjectId))
                .AsNoTracking()
                .ToListAsync();
            
            // Group problem statements by project ID
            var problemStatementsByProject = problemStatements
                .GroupBy(ps => ps.ProjectId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(ps => ps.UpdatedAt).ToList());

            // Assign ProblemStatements to each project
            foreach (var project in allProjects)
            {
                if (problemStatementsByProject.TryGetValue(project.Id, out var projectProblemStatements))
                {
                    project.ProblemStatements = projectProblemStatements;
                }
                else
                {
                    project.ProblemStatements = new List<ProjectProblemStatement>();
                }
            }

            // Load RAG history for the month to get submission data
            var ragHistoryForMonth = await _context.ProjectRagHistories
                .Where(rh => projectIds.Contains(rh.ProjectId) && 
                             rh.ChangedAt >= monthStart && 
                             rh.ChangedAt <= monthEnd)
                .OrderByDescending(rh => rh.ChangedAt)
                .AsNoTracking()
                .ToListAsync();

            // Group RAG history by project ID (get most recent for each project in the month)
            var ragHistoryByProject = ragHistoryForMonth
                .GroupBy(rh => rh.ProjectId)
                .ToDictionary(g => g.Key, g => g.First());

            // Create workbook
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add($"Monthly Summary {monthStart:MMMM yyyy}");

            // Build headers
            var headers = new List<string>
            {
                "Project Code",
                "Title",
                "Problem Statement",
                "Aim",
                "Business Area",
                "Current RAG Status",
                "RAG Status (Monthly Submission)",
                "RAG Justification (Monthly Submission)",
                "Path to Green (Monthly Submission)",
                "Current Priority",
                "Priority (Monthly Submission)",
                "Monthly Update Status",
                "Submitted Date",
                "Narrative"
            };

            // Add headers to worksheet
            for (int col = 0; col < headers.Count; col++)
            {
                var cell = worksheet.Cell(1, col + 1);
                cell.Value = headers[col];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#f1f3f5");
                cell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            }

            // Get monthly update due date for status calculation
            var dueDate = _monthlyUpdateService.GetMonthlyUpdateDueDate(reportYear, reportMonth);

            // Add data rows
            int currentRow = 2;
            foreach (var project in allProjects)
            {
                int col = 1;

                worksheet.Cell(currentRow, col++).Value = $"DFE-DDT-{project.Id}";
                worksheet.Cell(currentRow, col++).Value = project.Title ?? string.Empty;
                
                // Problem Statement (get the most recent one)
                var problemStatement = project.ProblemStatements?
                    .OrderByDescending(ps => ps.UpdatedAt)
                    .FirstOrDefault();
                worksheet.Cell(currentRow, col++).Value = problemStatement?.ProblemStatement ?? string.Empty;
                
                // Aim
                worksheet.Cell(currentRow, col++).Value = project.Aim ?? string.Empty;
                
                worksheet.Cell(currentRow, col++).Value = project.BusinessAreaLookup?.Name ?? string.Empty;
                
                // Current RAG Status
                var currentRagStatus = NormalizeRagStatus(project.RagStatusLookup?.Name ?? project.RagStatus);
                worksheet.Cell(currentRow, col++).Value = currentRagStatus ?? "Not Set";
                
                // Get monthly update for this project and month
                var monthlyUpdate = project.MonthlyUpdates?.FirstOrDefault(u => u.Year == reportYear && u.Month == reportMonth);
                
                // Get RAG history entry for this month (if submitted via monthly update)
                ProjectRagHistory? ragHistoryEntry = null;
                if (ragHistoryByProject.TryGetValue(project.Id, out var ragHistory))
                {
                    ragHistoryEntry = ragHistory;
                }

                // RAG Status from monthly submission (from RAG history if available)
                var submissionRagStatus = ragHistoryEntry?.RagStatus ?? currentRagStatus;
                worksheet.Cell(currentRow, col++).Value = submissionRagStatus ?? "Not Set";

                // RAG Justification from monthly submission
                worksheet.Cell(currentRow, col++).Value = ragHistoryEntry?.Justification ?? project.RagJustification ?? string.Empty;

                // Path to Green from monthly submission
                worksheet.Cell(currentRow, col++).Value = ragHistoryEntry?.PathToGreen ?? project.PathToGreen ?? string.Empty;

                // Current Priority
                worksheet.Cell(currentRow, col++).Value = project.DeliveryPriority?.Name ?? "Not Set";

                // Priority from monthly submission (same as current, as priority changes aren't tracked in monthly updates separately)
                worksheet.Cell(currentRow, col++).Value = project.DeliveryPriority?.Name ?? "Not Set";

                // Monthly Update Status
                string updateStatus;
                if (monthlyUpdate != null && monthlyUpdate.SubmittedAt.HasValue)
                {
                    updateStatus = "Submitted";
                }
                else if (currentDate > dueDate)
                {
                    updateStatus = "Late";
                }
                else if (monthlyUpdate != null && !monthlyUpdate.SubmittedAt.HasValue)
                {
                    updateStatus = "In Progress";
                }
                else
                {
                    updateStatus = "Not Started";
                }
                worksheet.Cell(currentRow, col++).Value = updateStatus;

                // Submitted Date
                if (monthlyUpdate?.SubmittedAt.HasValue == true)
                {
                    worksheet.Cell(currentRow, col++).Value = monthlyUpdate.SubmittedAt.Value;
                    worksheet.Cell(currentRow, col - 1).Style.NumberFormat.Format = "dd/mm/yyyy hh:mm";
                }
                else
                {
                    worksheet.Cell(currentRow, col++).Value = string.Empty;
                }

                // Narrative (combine all narratives from monthly update)
                var narrative = string.Empty;
                if (monthlyUpdate != null)
                {
                    if (monthlyUpdate.MonthlyUpdateNarratives?.Any() == true)
                    {
                        narrative = string.Join(" | ", monthlyUpdate.MonthlyUpdateNarratives.Select(n => n.Narrative));
                    }
                    else if (!string.IsNullOrEmpty(monthlyUpdate.Narrative))
                    {
                        narrative = monthlyUpdate.Narrative;
                    }
                }
                worksheet.Cell(currentRow, col++).Value = narrative;

                currentRow++;
            }

            // Auto-fit columns
            worksheet.Columns().AdjustToContents();
            
            // Wrap text for longer columns
            var wrapColumns = new[] { "Problem Statement", "Aim", "RAG Justification (Monthly Submission)", "Path to Green (Monthly Submission)", "Narrative" };
            for (int i = 0; i < headers.Count; i++)
            {
                if (wrapColumns.Contains(headers[i]))
                {
                    worksheet.Column(i + 1).Style.Alignment.WrapText = true;
                }
            }

            worksheet.SheetView.FreezeRows(1);

            // Save to memory stream
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var fileName = $"monthly-summary-{reportYear}-{reportMonth:D2}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.xlsx";
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting monthly summary v2 to Excel for {Year}-{Month}", year, month);
            TempData["ErrorMessage"] = "An error occurred while exporting the data. Please try again.";
            return RedirectToAction("MonthlySummaryV2", new { year, month, businessAreaId });
        }
    }

    // GET: CentralOps/MonthlySummaryV2Pdf
    public async Task<IActionResult> MonthlySummaryV2Pdf(int? year, int? month, int? businessAreaId)
    {
        try
        {
            // Default to current month if not specified
            var currentDate = DateTime.UtcNow;
            var reportYear = year ?? currentDate.Year;
            var reportMonth = month ?? currentDate.Month;
            
            // Calculate month boundaries
            var monthStart = new DateTime(reportYear, reportMonth, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59);
            
            // Get all active projects (exclude Cancelled and Completed)
            var query = _context.Projects
                .Include(p => p.PrimaryContactUser)
                .Include(p => p.DeliveryPriority)
                .Include(p => p.BusinessAreaLookup)
                .Include(p => p.Milestones)
                .Include(p => p.Risks)
                .Include(p => p.Issues)
                .Include(p => p.MonthlyUpdates)
                .Include(p => p.RagStatusLookup)
                .Include(p => p.ProblemStatements)
                .Where(p => !p.IsDeleted && p.Status != "Cancelled" && p.Status != "Completed");
            
            // Filter by business area if specified
            if (businessAreaId.HasValue)
            {
                query = query.Where(p => p.BusinessAreaId == businessAreaId.Value);
            }
            
            var allProjects = await query.ToListAsync();
            
            // Get all business areas for the filter dropdown
            var businessAreas = await _context.BusinessAreaLookups
                .Where(ba => ba.IsActive)
                .OrderBy(ba => ba.SortOrder)
                .ThenBy(ba => ba.Name)
                .ToListAsync();
            
            // Gather all the same data as MonthlySummaryV2
            var totalActiveProjects = allProjects.Count;
            var newProjectsThisMonth = allProjects
                .Where(p => p.CreatedAt >= monthStart && p.CreatedAt <= monthEnd)
                .ToList();
            
            var milestonesAchieved = allProjects
                .SelectMany(p => p.Milestones
                    .Where(m => !m.IsDeleted && 
                               m.Status == "complete" && 
                               m.ActualDate.HasValue &&
                               m.ActualDate.Value >= monthStart && 
                               m.ActualDate.Value <= monthEnd)
                    .Select(m => new MilestoneWithProject { Project = p, Milestone = m }))
                .ToList();
            
            var monthlyUpdateStats = CalculateMonthlyUpdateStats(allProjects, reportYear, reportMonth, _monthlyUpdateService);
            
            // RAG Status distribution
            var ragDistribution = new Dictionary<string, int>
            {
                { "Red", 0 },
                { "Amber-Red", 0 },
                { "Amber", 0 },
                { "Amber-Green", 0 },
                { "Green", 0 },
                { "Not Set", 0 }
            };
            
            foreach (var project in allProjects)
            {
                var ragStatus = NormalizeRagStatus(project.RagStatusLookup?.Name ?? project.RagStatus);
                if (string.IsNullOrWhiteSpace(ragStatus))
                {
                    ragDistribution["Not Set"]++;
                }
                else if (ragDistribution.ContainsKey(ragStatus))
                {
                    ragDistribution[ragStatus]++;
                }
            }
            
            // Priority distribution
            var priorityDistribution = new Dictionary<string, int>
            {
                { "Critical", 0 },
                { "High", 0 },
                { "Medium", 0 },
                { "Low", 0 },
                { "Not Set", 0 }
            };
            
            foreach (var project in allProjects)
            {
                if (project.DeliveryPriority == null)
                {
                    priorityDistribution["Not Set"]++;
                }
                else
                {
                    var priorityName = project.DeliveryPriority.Name.ToLower();
                    if (priorityName.Contains("critical"))
                    {
                        priorityDistribution["Critical"]++;
                    }
                    else if (priorityName.Contains("high"))
                    {
                        priorityDistribution["High"]++;
                    }
                    else if (priorityName.Contains("medium"))
                    {
                        priorityDistribution["Medium"]++;
                    }
                    else if (priorityName.Contains("low"))
                    {
                        priorityDistribution["Low"]++;
                    }
                    else
                    {
                        priorityDistribution["Not Set"]++;
                    }
                }
            }
            
            var newProjectsList = newProjectsThisMonth
                .OrderByDescending(p => p.CreatedAt)
                .ToList();
            
            var upcomingMilestones = allProjects
                .SelectMany(p => p.Milestones
                    .Where(m => !m.IsDeleted && 
                               m.Status != "complete" && 
                               m.Status != "cancelled" &&
                               m.DueDate >= monthStart && 
                               m.DueDate <= monthEnd)
                    .Select(m => new MilestoneWithProject { Project = p, Milestone = m }))
                .OrderBy(x => x.Milestone.DueDate)
                .ToList();
            
            var todayUtcDate = DateTime.UtcNow.Date;
            var lateMilestones = allProjects
                .SelectMany(p => p.Milestones
                    .Where(m => !m.IsDeleted && 
                               m.Status != "complete" && 
                               m.Status != "cancelled" &&
                               m.DueDate < monthEnd &&
                               m.DueDate.Date < todayUtcDate)
                    .Select(m => new MilestoneWithProject { Project = p, Milestone = m }))
                .OrderBy(x => x.Milestone.DueDate)
                .ToList();
            
            var businessAreaSummaries = allProjects
                .GroupBy(p => p.BusinessAreaLookup)
                .Select(g => new BusinessAreaSummaryViewModel
                {
                    BusinessArea = g.Key?.Name ?? "Not Set",
                    BusinessAreaId = g.Key?.Id,
                    TotalProjects = g.Count(),
                    NewThisMonth = g.Count(p => p.CreatedAt >= monthStart && p.CreatedAt <= monthEnd),
                    MilestonesAchieved = g.SelectMany(p => p.Milestones
                        .Where(m => !m.IsDeleted && 
                                   m.Status == "complete" && 
                                   m.ActualDate.HasValue &&
                                   m.ActualDate.Value >= monthStart && 
                                   m.ActualDate.Value <= monthEnd)).Count(),
                    UpcomingMilestones = g.SelectMany(p => p.Milestones
                        .Where(m => !m.IsDeleted && 
                                   m.Status != "complete" && 
                                   m.Status != "cancelled" &&
                                   m.DueDate >= monthStart && 
                                   m.DueDate <= monthEnd)).Count(),
                    LateMilestones = g.SelectMany(p => p.Milestones
                        .Where(m => !m.IsDeleted && 
                                   m.Status != "complete" && 
                                   m.Status != "cancelled" &&
                                   m.DueDate < monthEnd)).Count(),
                    RedCount = g.Count(p => NormalizeRagStatus(p.RagStatusLookup?.Name ?? p.RagStatus) == "Red"),
                    AmberRedCount = g.Count(p => NormalizeRagStatus(p.RagStatusLookup?.Name ?? p.RagStatus) == "Amber-Red"),
                    AmberCount = g.Count(p => NormalizeRagStatus(p.RagStatusLookup?.Name ?? p.RagStatus) == "Amber"),
                    AmberGreenCount = g.Count(p => NormalizeRagStatus(p.RagStatusLookup?.Name ?? p.RagStatus) == "Amber-Green"),
                    GreenCount = g.Count(p => NormalizeRagStatus(p.RagStatusLookup?.Name ?? p.RagStatus) == "Green"),
                    CriticalCount = g.Count(p => p.DeliveryPriority != null && p.DeliveryPriority.Name.ToLower().Contains("critical")),
                    HighCount = g.Count(p => p.DeliveryPriority != null && p.DeliveryPriority.Name.ToLower().Contains("high") && !p.DeliveryPriority.Name.ToLower().Contains("critical")),
                    MediumCount = g.Count(p => p.DeliveryPriority != null && p.DeliveryPriority.Name.ToLower().Contains("medium") && !p.DeliveryPriority.Name.ToLower().Contains("high") && !p.DeliveryPriority.Name.ToLower().Contains("critical")),
                    LowCount = g.Count(p => p.DeliveryPriority != null && p.DeliveryPriority.Name.ToLower().Contains("low") && !p.DeliveryPriority.Name.ToLower().Contains("medium") && !p.DeliveryPriority.Name.ToLower().Contains("high") && !p.DeliveryPriority.Name.ToLower().Contains("critical")),
                    MonthlyUpdateStats = CalculateMonthlyUpdateStats(g.ToList(), reportYear, reportMonth, _monthlyUpdateService)
                })
                .OrderBy(ba => ba.BusinessArea)
                .ToList();
            
            var projectsWithPathToGreen = allProjects
                .Where(p => !string.IsNullOrWhiteSpace(p.PathToGreen) && 
                           NormalizeRagStatus(p.RagStatusLookup?.Name ?? p.RagStatus) != "Green" &&
                           !string.IsNullOrWhiteSpace(NormalizeRagStatus(p.RagStatusLookup?.Name ?? p.RagStatus)))
                .OrderBy(p => 
                {
                    var ragStatus = NormalizeRagStatus(p.RagStatusLookup?.Name ?? p.RagStatus);
                    return ragStatus switch
                    {
                        "Red" => 1,
                        "Amber-Red" => 2,
                        "Amber" => 3,
                        "Amber-Green" => 4,
                        "Green" => 5,
                        _ => 99
                    };
                })
                .ThenBy(p => p.BusinessAreaLookup?.Name ?? "ZZZ")
                .ThenBy(p => p.Title)
                .ToList();
            
            var businessAreaRisks = new List<RiskWithProject>();
            var businessAreaIssues = new List<IssueWithProject>();
            var businessAreaActiveProjects = new List<Compass.Models.Project>();
            Compass.Models.BusinessAreaLookup? selectedBusinessArea = null;
            
            if (businessAreaId.HasValue)
            {
                selectedBusinessArea = await _context.BusinessAreaLookups.FindAsync(businessAreaId.Value);
                if (selectedBusinessArea != null)
                {
                    businessAreaActiveProjects = allProjects
                        .Where(p => p.BusinessAreaId == businessAreaId.Value)
                        .OrderBy(p => p.Title)
                        .ToList();
                    
                    businessAreaRisks = allProjects
                        .Where(p => p.BusinessAreaId == businessAreaId.Value)
                        .SelectMany(p => p.Risks
                            .Where(r => !r.IsDeleted && (r.Status != "closed" || r.ClosedDate == null))
                            .Select(r => new RiskWithProject { Project = p, Risk = r }))
                        .OrderByDescending(x => x.Risk.RiskScore)
                        .ThenByDescending(x => x.Risk.CreatedAt)
                        .ToList();
                    
                    businessAreaIssues = allProjects
                        .Where(p => p.BusinessAreaId == businessAreaId.Value)
                        .SelectMany(p => p.Issues
                            .Where(i => !i.IsDeleted && i.Status != "resolved" && i.Status != "closed")
                            .Select(i => new IssueWithProject { Project = p, Issue = i }))
                        .OrderByDescending(x => x.Issue.Severity == "critical" ? 2 : x.Issue.Severity == "high" ? 1 : 0)
                        .ThenByDescending(x => x.Issue.CreatedAt)
                        .ToList();
                }
            }
            
            // Resource summary (FTE)
            var totalPermFte = allProjects
                .Where(p => p.TotalPermFte.HasValue)
                .Sum(p => p.TotalPermFte.Value);
            var totalMspFte = allProjects
                .Where(p => p.TotalMspFte.HasValue)
                .Sum(p => p.TotalMspFte.Value);
            var totalFte = totalPermFte + totalMspFte;
            
            // Generate PDF
            var monthName = monthStart.ToString("MMMM yyyy");
            var selectedBusinessAreaName = businessAreaId.HasValue 
                ? businessAreas.FirstOrDefault(ba => ba.Id == businessAreaId.Value)?.Name 
                : null;
            
            var pdfBytes = GenerateMonthlySummaryPdf(
                monthName,
                selectedBusinessAreaName,
                totalActiveProjects,
                monthlyUpdateStats,
                milestonesAchieved.Count,
                newProjectsList.Count,
                ragDistribution,
                priorityDistribution,
                newProjectsList,
                milestonesAchieved,
                upcomingMilestones,
                lateMilestones,
                businessAreaSummaries,
                projectsWithPathToGreen,
                businessAreaRisks,
                businessAreaIssues,
                businessAreaActiveProjects,
                totalFte);
            
            var fileName = $"Monthly-Summary-{monthStart:yyyy-MM}";
            if (selectedBusinessAreaName != null)
            {
                fileName += $"-{selectedBusinessAreaName.Replace(" ", "-")}";
            }
            fileName += ".pdf";
            
            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating PDF for monthly summary v2 for {Year}-{Month}", year, month);
            TempData["ErrorMessage"] = "An error occurred while generating the PDF report. Please try again.";
            return RedirectToAction("MonthlySummaryV2", new { year, month, businessAreaId });
        }
    }

    private byte[] GenerateMonthlySummaryPdf(
        string monthName,
        string? businessAreaName,
        int totalActiveProjects,
        MonthlyUpdateStats? monthlyUpdateStats,
        int milestonesAchievedCount,
        int newProjectsCount,
        Dictionary<string, int> ragDistribution,
        Dictionary<string, int> priorityDistribution,
        List<Compass.Models.Project> newProjects,
        List<MilestoneWithProject> milestonesAchieved,
        List<MilestoneWithProject> upcomingMilestones,
        List<MilestoneWithProject> lateMilestones,
        List<BusinessAreaSummaryViewModel> businessAreaSummaries,
        List<Compass.Models.Project> projectsWithPathToGreen,
        List<RiskWithProject> businessAreaRisks,
        List<IssueWithProject> businessAreaIssues,
        List<Compass.Models.Project> businessAreaActiveProjects,
        decimal totalFte)
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Inter"));
                
                page.Header()
                    .Row(row =>
                    {
                        row.RelativeItem().Column(column =>
                        {
                            column.Item().Text(businessAreaName ?? "Monthly report dashboard")
                                .FontSize(18)
                                .Bold()
                                .FontColor(Colors.Blue.Darken3);
                            column.Item().Text(monthName)
                                .FontSize(12)
                                .FontColor(Colors.Grey.Darken1);
                        });
                        row.ConstantItem(60).AlignRight().Text("Page")
                            .FontSize(10)
                            .FontColor(Colors.Grey.Medium);
                    });
                
                page.Content()
                    .PaddingVertical(10)
                    .Column(column =>
                    {
                        // Summary Section
                        column.Item().PaddingBottom(15).Row(row =>
                        {
                            row.RelativeItem().PaddingRight(5).Column(col =>
                            {
                                col.Item().Background(Colors.Blue.Lighten5).Padding(10).Border(1).BorderColor(Colors.Blue.Darken1).Column(c =>
                                {
                                    c.Item().Text("Total Projects").FontSize(9).FontColor(Colors.Grey.Darken2);
                                    c.Item().Text(totalActiveProjects.ToString()).FontSize(16).Bold();
                                });
                            });
                            row.RelativeItem().PaddingRight(5).Column(col =>
                            {
                                col.Item().Background(Colors.Green.Lighten5).Padding(10).Border(1).BorderColor(Colors.Green.Darken1).Column(c =>
                                {
                                    c.Item().Text("Submitted Updates").FontSize(9).FontColor(Colors.Grey.Darken2);
                                    c.Item().Text((monthlyUpdateStats?.Submitted ?? 0).ToString()).FontSize(16).Bold();
                                });
                            });
                            row.RelativeItem().PaddingRight(5).Column(col =>
                            {
                                col.Item().Background(Colors.Orange.Lighten5).Padding(10).Border(1).BorderColor(Colors.Orange.Darken1).Column(c =>
                                {
                                    c.Item().Text("Milestones Achieved").FontSize(9).FontColor(Colors.Grey.Darken2);
                                    c.Item().Text(milestonesAchievedCount.ToString()).FontSize(16).Bold();
                                });
                            });
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Background(Colors.Teal.Lighten5).Padding(10).Border(1).BorderColor(Colors.Teal.Darken1).Column(c =>
                                {
                                    c.Item().Text("New Projects").FontSize(9).FontColor(Colors.Grey.Darken2);
                                    c.Item().Text(newProjectsCount.ToString()).FontSize(16).Bold();
                                });
                            });
                        });
                        
                        // RAG Distribution
                        column.Item().PaddingBottom(10).Column(col =>
                        {
                            col.Item().PaddingBottom(5).Text("RAG Status Distribution").FontSize(14).Bold();
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                });
                                
                                table.Header(header =>
                                {
                                    header.Cell().Element(CellStyle).Text("Red").Bold();
                                    header.Cell().Element(CellStyle).Text("Amber-Red").Bold();
                                    header.Cell().Element(CellStyle).Text("Amber").Bold();
                                    header.Cell().Element(CellStyle).Text("Amber-Green").Bold();
                                    header.Cell().Element(CellStyle).Text("Green").Bold();
                                    header.Cell().Element(CellStyle).Text("Not Set").Bold();
                                });
                                
                                table.Cell().Element(CellStyle).Text(ragDistribution.GetValueOrDefault("Red", 0).ToString());
                                table.Cell().Element(CellStyle).Text(ragDistribution.GetValueOrDefault("Amber-Red", 0).ToString());
                                table.Cell().Element(CellStyle).Text(ragDistribution.GetValueOrDefault("Amber", 0).ToString());
                                table.Cell().Element(CellStyle).Text(ragDistribution.GetValueOrDefault("Amber-Green", 0).ToString());
                                table.Cell().Element(CellStyle).Text(ragDistribution.GetValueOrDefault("Green", 0).ToString());
                                table.Cell().Element(CellStyle).Text(ragDistribution.GetValueOrDefault("Not Set", 0).ToString());
                            });
                        });
                        
                        // Priority Distribution
                        column.Item().PaddingBottom(10).Column(col =>
                        {
                            col.Item().PaddingBottom(5).Text("Priority Distribution").FontSize(14).Bold();
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                });
                                
                                table.Header(header =>
                                {
                                    header.Cell().Element(CellStyle).Text("Critical").Bold();
                                    header.Cell().Element(CellStyle).Text("High").Bold();
                                    header.Cell().Element(CellStyle).Text("Medium").Bold();
                                    header.Cell().Element(CellStyle).Text("Low").Bold();
                                    header.Cell().Element(CellStyle).Text("Not Set").Bold();
                                });
                                
                                table.Cell().Element(CellStyle).Text(priorityDistribution.GetValueOrDefault("Critical", 0).ToString());
                                table.Cell().Element(CellStyle).Text(priorityDistribution.GetValueOrDefault("High", 0).ToString());
                                table.Cell().Element(CellStyle).Text(priorityDistribution.GetValueOrDefault("Medium", 0).ToString());
                                table.Cell().Element(CellStyle).Text(priorityDistribution.GetValueOrDefault("Low", 0).ToString());
                                table.Cell().Element(CellStyle).Text(priorityDistribution.GetValueOrDefault("Not Set", 0).ToString());
                            });
                        });
                        
                        // New Projects
                        if (newProjects.Any())
                        {
                            column.Item().PaddingBottom(10).Column(col =>
                            {
                                col.Item().PaddingBottom(5).Text($"New Projects ({newProjects.Count})").FontSize(14).Bold();
                                foreach (var project in newProjects.Take(10))
                                {
                                    col.Item().PaddingBottom(3).Text($"{project.Title}").FontSize(10);
                                }
                                if (newProjects.Count > 10)
                                {
                                    col.Item().Text($"... and {newProjects.Count - 10} more").FontSize(9).Italic().FontColor(Colors.Grey.Medium);
                                }
                            });
                        }
                        
                        // Milestones Achieved
                        if (milestonesAchieved.Any())
                        {
                            column.Item().PaddingBottom(10).Column(col =>
                            {
                                col.Item().PaddingBottom(5).Text($"Milestones Achieved ({milestonesAchieved.Count})").FontSize(14).Bold();
                                foreach (var item in milestonesAchieved.Take(10))
                                {
                                    col.Item().PaddingBottom(3).Text($"{item.Project.Title}: {item.Milestone.Name}")
                                        .FontSize(10);
                                }
                                if (milestonesAchieved.Count > 10)
                                {
                                    col.Item().Text($"... and {milestonesAchieved.Count - 10} more").FontSize(9).Italic().FontColor(Colors.Grey.Medium);
                                }
                            });
                        }
                        
                        // Business Area Summary (if not filtered)
                        if (businessAreaName == null && businessAreaSummaries.Any())
                        {
                            column.Item().PaddingBottom(10).Column(col =>
                            {
                                col.Item().PaddingBottom(5).Text("Business Area Summary").FontSize(14).Bold();
                                col.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(2);
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                    });
                                    
                                    table.Header(header =>
                                    {
                                        header.Cell().Element(CellStyle).Text("Business Area").Bold();
                                        header.Cell().Element(CellStyle).Text("Total").Bold();
                                        header.Cell().Element(CellStyle).Text("Red").Bold();
                                        header.Cell().Element(CellStyle).Text("Amber").Bold();
                                        header.Cell().Element(CellStyle).Text("Green").Bold();
                                    });
                                    
                                    foreach (var summary in businessAreaSummaries.Take(15))
                                    {
                                        table.Cell().Element(CellStyle).Text(summary.BusinessArea);
                                        table.Cell().Element(CellStyle).Text(summary.TotalProjects.ToString());
                                        table.Cell().Element(CellStyle).Text((summary.RedCount + summary.AmberRedCount).ToString());
                                        table.Cell().Element(CellStyle).Text((summary.AmberCount + summary.AmberGreenCount).ToString());
                                        table.Cell().Element(CellStyle).Text(summary.GreenCount.ToString());
                                    }
                                });
                            });
                        }
                        
                        // Business Area Active Projects (if filtered)
                        if (businessAreaName != null && businessAreaActiveProjects.Any())
                        {
                            column.Item().PaddingBottom(10).Column(col =>
                            {
                                col.Item().PaddingBottom(5).Text($"Active Projects ({businessAreaActiveProjects.Count})").FontSize(14).Bold();
                                foreach (var project in businessAreaActiveProjects.Take(20))
                                {
                                    var ragStatus = NormalizeRagStatus(project.RagStatusLookup?.Name ?? project.RagStatus);
                                    var ragColor = GetRagColor(ragStatus);
                                    col.Item().PaddingBottom(3).Row(row =>
                                    {
                                        row.RelativeItem().Text(project.Title).FontSize(10);
                                        row.ConstantItem(60).Text(ragStatus).FontSize(9).FontColor(ragColor);
                                    });
                                }
                                if (businessAreaActiveProjects.Count > 20)
                                {
                                    col.Item().Text($"... and {businessAreaActiveProjects.Count - 20} more").FontSize(9).Italic().FontColor(Colors.Grey.Medium);
                                }
                            });
                        }
                        
                        // Risks (if filtered)
                        if (businessAreaRisks.Any())
                        {
                            column.Item().PaddingBottom(10).Column(col =>
                            {
                                col.Item().PaddingBottom(5).Text($"Open Risks ({businessAreaRisks.Count})").FontSize(14).Bold();
                                foreach (var item in businessAreaRisks.Take(10))
                                {
                                    col.Item().PaddingBottom(3).Text($"{item.Project.Title}: {item.Risk.Title} (Score: {item.Risk.RiskScore})")
                                        .FontSize(10);
                                }
                                if (businessAreaRisks.Count > 10)
                                {
                                    col.Item().Text($"... and {businessAreaRisks.Count - 10} more").FontSize(9).Italic().FontColor(Colors.Grey.Medium);
                                }
                            });
                        }
                        
                        // Issues (if filtered)
                        if (businessAreaIssues.Any())
                        {
                            column.Item().PaddingBottom(10).Column(col =>
                            {
                                col.Item().PaddingBottom(5).Text($"Open Issues ({businessAreaIssues.Count})").FontSize(14).Bold();
                                foreach (var item in businessAreaIssues.Take(10))
                                {
                                    col.Item().PaddingBottom(3).Text($"{item.Project.Title}: {item.Issue.Title} ({item.Issue.Severity})")
                                        .FontSize(10);
                                }
                                if (businessAreaIssues.Count > 10)
                                {
                                    col.Item().Text($"... and {businessAreaIssues.Count - 10} more").FontSize(9).Italic().FontColor(Colors.Grey.Medium);
                                }
                            });
                        }
                        
                        // Resource Summary
                        if (totalFte > 0)
                        {
                            column.Item().PaddingBottom(10).Text($"Total FTE: {totalFte:F1}").FontSize(12).Bold();
                        }
                    });
                
                page.Footer()
                    .AlignCenter()
                    .Text($"Generated on {DateTime.UtcNow.ToString("dd MMMM yyyy HH:mm UTC")}")
                    .FontSize(8)
                    .FontColor(Colors.Grey.Medium);
            });
        });
        
        return document.GeneratePdf();
    }
    
    private static IContainer CellStyle(IContainer container)
    {
        return container
            .Border(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Padding(5)
            .AlignCenter();
    }
    
    private static string GetRagColor(string ragStatus)
    {
        return ragStatus switch
        {
            "Red" => Colors.Red.Darken2,
            "Amber-Red" => Colors.Orange.Darken2,
            "Amber" => Colors.Orange.Medium,
            "Amber-Green" => Colors.LightGreen.Medium,
            "Green" => Colors.Green.Darken2,
            _ => Colors.Grey.Medium
        };
    }

    // GET: CentralOps/MonthlySummaryV2ByRagStatus
    public async Task<IActionResult> MonthlySummaryV2ByRagStatus(int year, int month, string ragStatus, int? businessAreaId)
    {
        try
        {
            // Calculate month boundaries
            var monthStart = new DateTime(year, month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59);
            
            // Get all active projects (exclude Cancelled and Completed)
            var query = _context.Projects
                .Include(p => p.PrimaryContactUser)
                .Include(p => p.DeliveryPriority)
                .Include(p => p.BusinessAreaLookup)
                .Include(p => p.RagStatusLookup)
                .Include(p => p.ServiceOwners)
                    .ThenInclude(so => so.User)
                .Where(p => !p.IsDeleted && p.Status != "Cancelled" && p.Status != "Completed");
            
            if (businessAreaId.HasValue)
            {
                query = query.Where(p => p.BusinessAreaId == businessAreaId.Value);
            }
            
            var allProjects = await query.ToListAsync();
            
            // Filter by RAG status
            List<Project> filteredProjects;
            if (ragStatus == "Not Set" || string.IsNullOrEmpty(ragStatus))
            {
                filteredProjects = allProjects
                    .Where(p => string.IsNullOrWhiteSpace(NormalizeRagStatus(p.RagStatusLookup?.Name ?? p.RagStatus)))
                    .OrderBy(p => p.BusinessAreaLookup?.Name ?? "ZZZ")
                    .ThenBy(p => p.Title)
                    .ToList();
            }
            else
            {
                filteredProjects = allProjects
                    .Where(p => NormalizeRagStatus(p.RagStatusLookup?.Name ?? p.RagStatus) == ragStatus)
                    .OrderBy(p => p.BusinessAreaLookup?.Name ?? "ZZZ")
                    .ThenBy(p => p.Title)
                    .ToList();
            }
            
            ViewBag.Year = year;
            ViewBag.Month = month;
            ViewBag.RagStatus = ragStatus;
            ViewBag.BusinessAreaId = businessAreaId;
            ViewBag.MonthName = monthStart.ToString("MMMM yyyy");
            ViewBag.FilteredProjects = filteredProjects;
            ViewBag.TotalCount = filteredProjects.Count;
            
            var businessAreas = await _context.BusinessAreaLookups.OrderBy(ba => ba.Name).ToListAsync();
            ViewBag.BusinessAreas = businessAreas;
            
            if (businessAreaId.HasValue)
            {
                var selectedBusinessArea = businessAreas.FirstOrDefault(ba => ba.Id == businessAreaId.Value);
                ViewBag.SelectedBusinessAreaName = selectedBusinessArea?.Name;
            }
            
            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading monthly summary v2 by RAG status for {Year}-{Month}-{RagStatus}", year, month, ragStatus);
            TempData["ErrorMessage"] = "An error occurred while loading the filtered projects. Please try again.";
            return RedirectToAction("MonthlySummaryV2", new { year, month, businessAreaId });
        }
    }

    // GET: CentralOps/MonthlySummaryV2ByPriority
    public async Task<IActionResult> MonthlySummaryV2ByPriority(int year, int month, string priority, int? businessAreaId)
    {
        try
        {
            // Calculate month boundaries
            var monthStart = new DateTime(year, month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59);
            
            // Get all active projects (exclude Cancelled and Completed)
            var query = _context.Projects
                .Include(p => p.PrimaryContactUser)
                .Include(p => p.DeliveryPriority)
                .Include(p => p.BusinessAreaLookup)
                .Include(p => p.RagStatusLookup)
                .Include(p => p.ServiceOwners)
                    .ThenInclude(so => so.User)
                .Where(p => !p.IsDeleted && p.Status != "Cancelled" && p.Status != "Completed");
            
            if (businessAreaId.HasValue)
            {
                query = query.Where(p => p.BusinessAreaId == businessAreaId.Value);
            }
            
            var allProjects = await query.ToListAsync();
            
            // Filter by Priority
            List<Project> filteredProjects;
            if (priority == "Not Set" || string.IsNullOrEmpty(priority))
            {
                filteredProjects = allProjects
                    .Where(p => p.DeliveryPriority == null)
                    .OrderBy(p => p.BusinessAreaLookup?.Name ?? "ZZZ")
                    .ThenBy(p => p.Title)
                    .ToList();
            }
            else
            {
                filteredProjects = allProjects
                    .Where(p => p.DeliveryPriority != null && 
                               (priority == "Critical" && p.DeliveryPriority.Name.ToLower().Contains("critical")) ||
                               (priority == "High" && p.DeliveryPriority.Name.ToLower().Contains("high") && !p.DeliveryPriority.Name.ToLower().Contains("critical")) ||
                               (priority == "Medium" && p.DeliveryPriority.Name.ToLower().Contains("medium")) ||
                               (priority == "Low" && p.DeliveryPriority.Name.ToLower().Contains("low")))
                    .OrderBy(p => p.BusinessAreaLookup?.Name ?? "ZZZ")
                    .ThenBy(p => p.Title)
                    .ToList();
            }
            
            ViewBag.Year = year;
            ViewBag.Month = month;
            ViewBag.Priority = priority;
            ViewBag.BusinessAreaId = businessAreaId;
            ViewBag.MonthName = monthStart.ToString("MMMM yyyy");
            ViewBag.FilteredProjects = filteredProjects;
            ViewBag.TotalCount = filteredProjects.Count;
            
            var businessAreas = await _context.BusinessAreaLookups.OrderBy(ba => ba.Name).ToListAsync();
            ViewBag.BusinessAreas = businessAreas;
            
            if (businessAreaId.HasValue)
            {
                var selectedBusinessArea = businessAreas.FirstOrDefault(ba => ba.Id == businessAreaId.Value);
                ViewBag.SelectedBusinessAreaName = selectedBusinessArea?.Name;
            }
            
            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading monthly summary v2 by Priority for {Year}-{Month}-{Priority}", year, month, priority);
            TempData["ErrorMessage"] = "An error occurred while loading the filtered projects. Please try again.";
            return RedirectToAction("MonthlySummaryV2", new { year, month, businessAreaId });
        }
    }

    private string GenerateNarrativeSummary(
        List<Compass.Models.Project> projects,
        MonthlyUpdateStats? monthlyUpdateStats,
        Dictionary<string, int> ragDistribution,
        Dictionary<string, int> priorityDistribution,
        List<Compass.Models.Project> newProjects,
        List<MilestoneWithProject> milestonesAchieved,
        List<MilestoneWithProject> upcomingMilestones,
        List<MilestoneWithProject> lateMilestones,
        List<Compass.Models.Project> projectsWithPathToGreen,
        List<RiskWithProject> businessAreaRisks,
        List<IssueWithProject> businessAreaIssues,
        int? businessAreaId,
        string? businessAreaName,
        string monthName)
    {
        var narrative = new System.Text.StringBuilder();
        var isBusinessAreaView = businessAreaId.HasValue && !string.IsNullOrEmpty(businessAreaName);
        
        var totalProjects = projects.Count;
        var ragTotal = ragDistribution.Values.Sum();
        var priorityTotal = priorityDistribution.Values.Sum();
        
        // Introduction
        if (isBusinessAreaView)
        {
            narrative.Append($"<p class='mb-3'><strong>{businessAreaName}</strong> has <strong>{totalProjects}</strong> active project{(totalProjects != 1 ? "s" : "")} in {monthName}. ");
        }
        else
        {
            narrative.Append($"<p class='mb-3'>The portfolio contains <strong>{totalProjects}</strong> active project{(totalProjects != 1 ? "s" : "")} in {monthName}. ");
        }
        
        // New projects
        if (newProjects.Count > 0)
        {
            narrative.Append($"<strong>{newProjects.Count}</strong> new project{(newProjects.Count != 1 ? "s were" : " was")} added this month. ");
        }
        
        // Milestones achieved
        if (milestonesAchieved.Count > 0)
        {
            narrative.Append($"<strong>{milestonesAchieved.Count}</strong> milestone{(milestonesAchieved.Count != 1 ? "s were" : " was")} achieved. ");
        }
        
        narrative.Append("</p>");
        
        // RAG Status Analysis
        if (ragTotal > 0)
        {
            narrative.Append("<p class='mb-3'>");
            var redCount = ragDistribution.ContainsKey("Red") ? ragDistribution["Red"] : 0;
            var amberRedCount = ragDistribution.ContainsKey("Amber-Red") ? ragDistribution["Amber-Red"] : 0;
            var amberGreenCount = ragDistribution.ContainsKey("Amber-Green") ? ragDistribution["Amber-Green"] : 0;
            var greenCount = ragDistribution.ContainsKey("Green") ? ragDistribution["Green"] : 0;
            var redPct = (redCount * 100.0) / ragTotal;
            var greenPct = (greenCount * 100.0) / ragTotal;
            
            if (redCount > 0 || amberRedCount > 0)
            {
                narrative.Append("Attention is required as ");
                if (redCount > 0)
                {
                    narrative.Append($"{redCount} project{(redCount != 1 ? "s are" : " is")} showing Red status ({redPct:F1}%)");
                    if (amberRedCount > 0)
                    {
                        narrative.Append($" and {amberRedCount} project{(amberRedCount != 1 ? "s are" : " is")} at Amber-Red");
                    }
                }
                else if (amberRedCount > 0)
                {
                    narrative.Append($"{amberRedCount} project{(amberRedCount != 1 ? "s are" : " is")} at Amber-Red");
                }
                narrative.Append(". These require immediate attention. ");
            }
            
            if (greenCount > 0 && greenPct >= 50)
            {
                narrative.Append($"{greenPct:F0}% of projects are Green, indicating good overall health. ");
            }
            else if (greenCount > 0)
            {
                narrative.Append($"{greenCount} project{(greenCount != 1 ? "s are" : " is")} Green. ");
            }
            
            if (projectsWithPathToGreen.Count > 0)
            {
                narrative.Append($"{projectsWithPathToGreen.Count} project{(projectsWithPathToGreen.Count != 1 ? "s have" : " has")} a defined path to green, showing proactive management. ");
            }
            
            narrative.Append("</p>");
        }
        
        // Priority Analysis
        if (priorityTotal > 0)
        {
            narrative.Append("<p class='mb-3'>");
            var criticalCount = priorityDistribution.ContainsKey("Critical") ? priorityDistribution["Critical"] : 0;
            var highCount = priorityDistribution.ContainsKey("High") ? priorityDistribution["High"] : 0;
            
            if (criticalCount > 0 || highCount > 0)
            {
                narrative.Append("Priority focus areas include ");
                if (criticalCount > 0)
                {
                    narrative.Append($"{criticalCount} Critical priority project{(criticalCount != 1 ? "s" : "")}");
                    if (highCount > 0)
                    {
                        narrative.Append($" and {highCount} High priority project{(highCount != 1 ? "s" : "")}");
                    }
                }
                else if (highCount > 0)
                {
                    narrative.Append($"{highCount} High priority project{(highCount != 1 ? "s" : "")}");
                }
                narrative.Append(". These should be prioritised for resources and support. ");
            }
            
            narrative.Append("</p>");
        }
        
        // Milestones Analysis
        if (lateMilestones.Count > 0 || upcomingMilestones.Count > 0)
        {
            narrative.Append("<p class='mb-3'>");
            if (lateMilestones.Count > 0)
            {
                narrative.Append($"{lateMilestones.Count} milestone{(lateMilestones.Count != 1 ? "s are" : " is")} overdue");
                if (upcomingMilestones.Count > 0)
                {
                    narrative.Append($" and {upcomingMilestones.Count} milestone{(upcomingMilestones.Count != 1 ? "s are" : " is")} due this month, requiring close monitoring");
                }
                narrative.Append(". ");
            }
            else if (upcomingMilestones.Count > 0)
            {
                narrative.Append($"{upcomingMilestones.Count} milestone{(upcomingMilestones.Count != 1 ? "s are" : " is")} due this month, requiring close monitoring. ");
            }
            narrative.Append("</p>");
        }
        
        // Monthly Update Status
        if (monthlyUpdateStats != null && monthlyUpdateStats.TotalProjects > 0)
        {
            narrative.Append("<p class='mb-3'>");
            var submittedPct = (monthlyUpdateStats.Submitted * 100.0) / monthlyUpdateStats.TotalProjects;
            var latePct = (monthlyUpdateStats.Late * 100.0) / monthlyUpdateStats.TotalProjects;
            
            if (monthlyUpdateStats.Late > 0)
            {
                narrative.Append($"Reporting status shows {monthlyUpdateStats.Late} project{(monthlyUpdateStats.Late != 1 ? "s have" : " has")} late monthly updates ({latePct:F0}%). ");
            }
            else if (submittedPct >= 90)
            {
                narrative.Append($"Excellent reporting compliance with {submittedPct:F0}% of projects submitted. ");
            }
            else if (monthlyUpdateStats.Submitted > 0)
            {
                narrative.Append($"{monthlyUpdateStats.Submitted} of {monthlyUpdateStats.TotalProjects} projects ({submittedPct:F0}%) have submitted updates. ");
            }
            
            if (monthlyUpdateStats.InProgress > 0)
            {
                narrative.Append($"{monthlyUpdateStats.InProgress} update{(monthlyUpdateStats.InProgress != 1 ? "s are" : " is")} in progress. ");
            }
            
            narrative.Append("</p>");
        }
        
        // Business Area specific risks and issues
        if (isBusinessAreaView && (businessAreaRisks.Count > 0 || businessAreaIssues.Count > 0))
        {
            narrative.Append("<p class='mb-3'>");
            if (businessAreaRisks.Count > 0)
            {
                var highRiskCount = businessAreaRisks.Count(r => r.Risk.RiskScore >= 20);
                var risksWithoutPriority = businessAreaRisks.Count(r => r.Risk.RiskPriorityId == null);
                narrative.Append($"Risk management shows {businessAreaRisks.Count} open risk{(businessAreaRisks.Count != 1 ? "s" : "")}");
                if (highRiskCount > 0)
                {
                    narrative.Append($", including {highRiskCount} high-risk item{(highRiskCount != 1 ? "s" : "")} with a risk score of 20 or higher");
                }
                if (risksWithoutPriority > 0)
                {
                    narrative.Append($". <strong>Risk:</strong> {risksWithoutPriority} risk{(risksWithoutPriority != 1 ? "s do" : " does")} not have a priority set, which makes it difficult to assess their relative importance and allocate resources appropriately");
                }
                narrative.Append(". ");
            }
            
            if (businessAreaIssues.Count > 0)
            {
                var criticalIssues = businessAreaIssues.Count(i => i.Issue.Severity.ToLower() == "critical");
                var highIssues = businessAreaIssues.Count(i => i.Issue.Severity.ToLower() == "high");
                var issuesWithoutPriority = businessAreaIssues.Count(i => i.Issue.PriorityId == null && string.IsNullOrWhiteSpace(i.Issue.Priority));
                narrative.Append($"There are {businessAreaIssues.Count} open issue{(businessAreaIssues.Count != 1 ? "s" : "")}");
                if (criticalIssues > 0 || highIssues > 0)
                {
                    narrative.Append($" ({criticalIssues} critical, {highIssues} high severity)");
                }
                if (issuesWithoutPriority > 0)
                {
                    narrative.Append($". <strong>Risk:</strong> {issuesWithoutPriority} issue{(issuesWithoutPriority != 1 ? "s do" : " does")} not have a priority set, which makes it difficult to assess their relative importance and allocate resources appropriately");
                }
                narrative.Append(". ");
            }
            
            narrative.Append("</p>");
        }
        
        // Flag projects with "Not Set" RAG status as a risk
        var notSetRagCount = ragDistribution.ContainsKey("Not Set") ? ragDistribution["Not Set"] : 0;
        if (notSetRagCount > 0)
        {
            narrative.Append("<p class='mb-3'><strong>Risk:</strong> ");
            if (isBusinessAreaView)
            {
                narrative.Append($"{notSetRagCount} project{(notSetRagCount != 1 ? "s in" : " in")} {businessAreaName} do{(notSetRagCount != 1 ? "" : "es")} not have a RAG status set");
            }
            else
            {
                narrative.Append($"{notSetRagCount} project{(notSetRagCount != 1 ? "s" : "")} do{(notSetRagCount != 1 ? "" : "es")} not have a RAG status set");
            }
            narrative.Append(", which makes it difficult to assess their current health and identify projects that may require attention. ");
            narrative.Append("</p>");
        }
        
        // Overall assessment with actionable insights
        narrative.Append("<p class='mb-0'><strong>Overall Assessment:</strong> ");
        if (isBusinessAreaView)
        {
            narrative.Append($"{businessAreaName} demonstrates ");
        }
        else
        {
            narrative.Append("The portfolio demonstrates ");
        }
        
        var overallHealth = "mixed performance";
        var recommendations = new List<string>();
        
        if (ragTotal > 0)
        {
            var greenPct = (ragDistribution.ContainsKey("Green") ? ragDistribution["Green"] : 0) * 100.0 / ragTotal;
            var redPct = (ragDistribution.ContainsKey("Red") ? ragDistribution["Red"] : 0) * 100.0 / ragTotal;
            var amberRedPct = (ragDistribution.ContainsKey("Amber-Red") ? ragDistribution["Amber-Red"] : 0) * 100.0 / ragTotal;
            var amberGreenPct = (ragDistribution.ContainsKey("Amber-Green") ? ragDistribution["Amber-Green"] : 0) * 100.0 / ragTotal;
            
            if (greenPct >= 60 && redPct < 10)
            {
                overallHealth = "strong overall health with the majority of projects on track";
            }
            else if (greenPct >= 40 && redPct < 20)
            {
                overallHealth = "moderate health with some areas requiring attention";
                if (redPct > 0)
                {
                    recommendations.Add("focus on Red status projects to prevent escalation");
                }
            }
            else if (redPct >= 20)
            {
                overallHealth = "significant challenges requiring immediate focus";
                recommendations.Add("urgent intervention needed for Red status projects");
            }
            else if (amberGreenPct >= 30)
            {
                overallHealth = "positive trajectory with many projects approaching Green status";
            }
            
            // Add specific recommendations based on data
            if (redPct > 15 && projectsWithPathToGreen.Count == 0)
            {
                recommendations.Add("consider defining paths to green for Red/Amber-Red projects");
            }
        }
        
        narrative.Append(overallHealth);
        
        // Add recommendations if any
        if (recommendations.Any())
        {
            narrative.Append(" <strong>Key actions:</strong> " + string.Join("; ", recommendations) + ".");
        }
        
        // Add positive highlights
        var positiveHighlights = new List<string>();
        if (milestonesAchieved.Count > 0 && lateMilestones.Count == 0)
        {
            positiveHighlights.Add("all milestones are on track");
        }
        if (monthlyUpdateStats != null && monthlyUpdateStats.Late == 0 && monthlyUpdateStats.Submitted >= monthlyUpdateStats.TotalProjects * 0.9)
        {
            positiveHighlights.Add("excellent reporting compliance");
        }
        if (newProjects.Count > 0 && newProjects.Count <= 3)
        {
            positiveHighlights.Add("controlled growth with new project intake");
        }
        
        if (positiveHighlights.Any())
        {
            narrative.Append(" Highlights include " + string.Join("; ", positiveHighlights) + ".");
        }
        
        narrative.Append("</p>");
        
        return narrative.ToString();
    }

    // GET: CentralOps/AccessDenied
    // This action should NOT require authorization - it's for showing the access denied message
    // GET: CentralOps/PerformanceReporting
    public async Task<IActionResult> PerformanceReporting(int? commissionId = null, string businessArea = null)
    {
        // Get all active commissions
        var commissions = await _context.Commissions
            .Where(c => c.IsActive)
            .OrderByDescending(c => c.DueDate)
            .ToListAsync();

        if (!commissionId.HasValue)
        {
            // Show commission selection
            return View("~/Views/CentralOps/PerformanceReporting/Index.cshtml", commissions);
        }

        // Get selected commission
        var commission = await _context.Commissions
            .FirstOrDefaultAsync(c => c.Id == commissionId.Value);

        if (commission == null)
        {
            TempData["ErrorMessage"] = "Commission not found.";
            return View("~/Views/CentralOps/PerformanceReporting/Index.cshtml", commissions);
        }

        // Get all products that should be in this commission
        var allProducts = await _productsApiService.GetAllProductsAsync();
        var eligibleProducts = allProducts
            .Where(p => p.State != null &&
                        p.State.Equals("Active", StringComparison.OrdinalIgnoreCase) &&
                        p.PublishedAt.HasValue &&
                        (string.IsNullOrEmpty(p.Phase) ||
                         (!p.Phase.Equals("Decommissioned", StringComparison.OrdinalIgnoreCase) &&
                          !p.Phase.Equals("Decommissioning", StringComparison.OrdinalIgnoreCase))))
            // Exclude products where the only Type is "Data" from performance reporting,
            // but keep products that have "Data" alongside another Type.
            .Where(p =>
            {
                var types = p.CategoryValues?
                    .Where(cv => cv.CategoryType?.Name?.Equals("Type", StringComparison.OrdinalIgnoreCase) == true)
                    .Select(cv => cv.Name)
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .ToList() ?? new List<string>();

                if (!types.Any())
                {
                    // No Type categories – include in reporting
                    return true;
                }

                var nonDataTypes = types
                    .Where(t => !t.Equals("Data", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                // Include if there is any non-"Data" type; exclude if all types are "Data"
                return nonDataTypes.Any();
            })
            .ToList();

        // Get all submissions for this commission
        var submissions = await _context.CommissionSubmissions
            .Include(cs => cs.MetricValues)
                .ThenInclude(mv => mv.PerformanceMetric)
            .Where(cs => cs.CommissionId == commissionId.Value)
            .ToDictionaryAsync(cs => cs.ProductDocumentId, cs => cs);

        // Get all performance metrics
        var allMetrics = await _context.PerformanceMetrics
            .Where(pm => !pm.IsDisabled)
            .OrderBy(pm => pm.Identifier)
            .ToListAsync();

        // Calculate business area completion from ALL eligible products (before filtering)
        // This ensures all business areas are always visible in the completion table
        var businessAreaCompletions = new List<BusinessAreaCommissionCompletion>();
        
        var productsByBusinessArea = eligibleProducts
            .GroupBy(p =>
            {
                var businessArea = p.CategoryValues?
                    .FirstOrDefault(cv => cv.CategoryType?.Name?.Equals("Business area", StringComparison.OrdinalIgnoreCase) == true)
                    ?.Name ?? "Unassigned";
                return businessArea;
            })
            .ToList();

        // Get unique business areas for filter dropdown (from eligible products before filtering)
        var allBusinessAreas = eligibleProducts
            .Select(p => p.CategoryValues?
                .FirstOrDefault(cv => cv.CategoryType?.Name?.Equals("Business area", StringComparison.OrdinalIgnoreCase) == true)
                ?.Name ?? "Unassigned")
            .Distinct()
            .OrderBy(ba => ba)
            .ToList();

        // Filter products by business area if selected (for the products table only)
        var filteredProducts = eligibleProducts;
        if (!string.IsNullOrEmpty(businessArea) && businessArea != "all")
        {
            filteredProducts = eligibleProducts
                .Where(p =>
                {
                    var productBusinessArea = p.CategoryValues?
                        .FirstOrDefault(cv => cv.CategoryType?.Name?.Equals("Business area", StringComparison.OrdinalIgnoreCase) == true)
                        ?.Name ?? "Unassigned";
                    return productBusinessArea.Equals(businessArea, StringComparison.OrdinalIgnoreCase);
                })
                .ToList();
        }

        // Build product-metric data from filtered products
        var productMetricData = new List<CommissionProductMetricViewModel>();
        
        foreach (var product in filteredProducts.OrderBy(p => p.Title))
        {
            var documentId = product.DocumentId ?? "";
            var submission = submissions.GetValueOrDefault(documentId);
            
            foreach (var metric in allMetrics)
            {
                var metricValue = submission?.MetricValues?.FirstOrDefault(mv => mv.PerformanceMetricId == metric.Id);
                
                productMetricData.Add(new CommissionProductMetricViewModel
                {
                    Product = product,
                    Metric = metric,
                    MetricValue = metricValue,
                    Submission = submission
                });
            }
        }

        foreach (var group in productsByBusinessArea)
        {
            var businessAreaName = group.Key;
            var products = group.ToList();
            var totalProducts = products.Count;
            
            var completedProducts = products.Count(p =>
            {
                var docId = p.DocumentId ?? "";
                var sub = submissions.GetValueOrDefault(docId);
                return sub?.Status == CommissionSubmissionStatus.Submitted || sub?.Status == CommissionSubmissionStatus.Late;
            });
            
            var inProgressProducts = products.Count(p =>
            {
                var docId = p.DocumentId ?? "";
                var sub = submissions.GetValueOrDefault(docId);
                return sub?.Status == CommissionSubmissionStatus.InProgress;
            });
            
            var notStartedProducts = products.Count(p =>
            {
                var docId = p.DocumentId ?? "";
                var sub = submissions.GetValueOrDefault(docId);
                return sub?.Status == CommissionSubmissionStatus.NotStarted || sub == null;
            });
            
            var lateProducts = products.Count(p =>
            {
                var docId = p.DocumentId ?? "";
                var sub = submissions.GetValueOrDefault(docId);
                return sub?.Status == CommissionSubmissionStatus.Late;
            });

            var completionPercentage = totalProducts > 0 ? (decimal)completedProducts / totalProducts * 100 : 0;

            businessAreaCompletions.Add(new BusinessAreaCommissionCompletion
            {
                BusinessArea = businessAreaName,
                TotalProducts = totalProducts,
                CompletedProducts = completedProducts,
                InProgressProducts = inProgressProducts,
                NotStartedProducts = notStartedProducts,
                LateProducts = lateProducts,
                CompletionPercentage = Math.Round(completionPercentage, 1)
            });
        }

        businessAreaCompletions = businessAreaCompletions.OrderByDescending(ba => ba.CompletionPercentage).ToList();

        ViewBag.Commission = commission;
        ViewBag.ProductMetricData = productMetricData;
        ViewBag.BusinessAreaCompletions = businessAreaCompletions;
        ViewBag.AllCommissions = commissions;
        ViewBag.BusinessAreas = allBusinessAreas;
        ViewBag.SelectedBusinessArea = businessArea;

        return View("~/Views/CentralOps/PerformanceReporting/Details.cshtml", commission);
    }

    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }
}

