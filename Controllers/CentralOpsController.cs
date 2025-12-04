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
using ClosedXML.Excel;

namespace Compass.Controllers;

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
            // Get all projects with related data
            var allProjects = await _context.Projects
                .Include(p => p.MonthlyUpdates)
                .Include(p => p.PrimaryContactUser)
                .Include(p => p.DeliveryPriority)
                .Include(p => p.BusinessAreaLookup)
                .Include(p => p.ServiceOwners)
                    .ThenInclude(so => so.User)
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


            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading monthly reporting");
            TempData["ErrorMessage"] = "An error occurred while loading monthly reporting. Please try again.";
            return View();
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
            // Get all projects with monthly updates
            var allProjects = await _context.Projects
                .Include(p => p.PrimaryContactUser)
                .Include(p => p.DeliveryPriority)
                .Include(p => p.BusinessAreaLookup)
                .Include(p => p.MonthlyUpdates)
                .Where(p => !p.IsDeleted)
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
            // Get all projects with monthly updates
            var allProjects = await _context.Projects
                .Include(p => p.PrimaryContactUser)
                .Include(p => p.DeliveryPriority)
                .Include(p => p.BusinessAreaLookup)
                .Include(p => p.ServiceOwners)
                    .ThenInclude(so => so.User)
                .Include(p => p.MonthlyUpdates)
                .Where(p => !p.IsDeleted)
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

            var query = _context.Projects
                .Include(p => p.PrimaryContactUser)
                .Include(p => p.DeliveryPriority)
                .Include(p => p.BusinessAreaLookup)
                .Include(p => p.ActivityTypeLookup)
                .Include(p => p.Directorates)
                    .ThenInclude(d => d.DirectorateLookup)
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

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(p => p.Status == status);
            }

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

            var directorates = await _context.DirectorateLookups
                .Where(d => d.IsActive)
                .OrderBy(d => d.Name)
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.BusinessArea = businessArea;
            ViewBag.Priority = priority;
            ViewBag.Rag = rag;
            ViewBag.Status = status;
            ViewBag.TotalCount = totalCount;
            ViewBag.BusinessAreas = businessAreas;
            ViewBag.Priorities = priorities;
            ViewBag.Directorates = directorates;
            ViewBag.Rags = new[] { "Red", "Amber-Red", "Amber", "Amber-Green", "Green" };
            ViewBag.Statuses = new[] { "Active", "Paused", "Completed", "Cancelled" };

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
                    .ThenInclude(d => d.DirectorateLookup)
                .Include(p => p.ProjectProducts)
                .Include(p => p.ProjectContacts)
                    .ThenInclude(pc => pc.User)
                .Include(p => p.PmoContacts)
                    .ThenInclude(pc => pc.User)
                .Include(p => p.Milestones)
                .Include(p => p.ProjectMissions)
                    .ThenInclude(pm => pm.Mission)
                .Where(p => !p.IsDeleted);

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
            var worksheet = workbook.Worksheets.Add("Work");

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
                    .Select(d => d.DirectorateLookup?.Name ?? string.Empty)
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

            // Build query with all necessary includes (same as ExportManageWork)
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
                    .ThenInclude(d => d.DirectorateLookup)
                .Include(p => p.ProjectProducts)
                .Include(p => p.ProjectContacts)
                    .ThenInclude(pc => pc.User)
                .Include(p => p.PmoContacts)
                    .ThenInclude(pc => pc.User)
                .Include(p => p.Milestones)
                .Include(p => p.ProjectMissions)
                    .ThenInclude(pm => pm.Mission)
                .Where(p => !p.IsDeleted);

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
                    .Select(d => d.DirectorateLookup?.Name ?? string.Empty)
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
    public async Task<IActionResult> WorkItemDetails(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var project = await _context.Projects
            .Include(p => p.DeliveryPriority)
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
                .ThenInclude(d => d.DirectorateLookup)
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
        
        ViewBag.RagStatuses = new[] { "Red", "Amber-Red", "Amber", "Amber-Green", "Green" };
        ViewBag.Statuses = new[] { "Active", "Paused", "Completed", "Cancelled" };

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
    public async Task<IActionResult> UpdateField(int id, string field, string? value, string? statusChangeReason)
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
                    project.RagStatus = value;
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
            // Determine the week to display
            DateTime weekStart;
            if (year.HasValue && week.HasValue)
            {
                // Calculate the date from year and week number using ISO 8601 week calculation
                var jan1 = new DateTime(year.Value, 1, 1);
                var daysOffset = DayOfWeek.Monday - jan1.DayOfWeek;
                if (daysOffset > 0) daysOffset -= 7; // Adjust if Jan 1 is after Monday
                var firstMonday = jan1.AddDays(daysOffset);
                weekStart = firstMonday.AddDays((week.Value - 1) * 7);
            }
            else
            {
                // Default to current week - calculate Monday of current week
                var today = DateTime.UtcNow.Date;
                var daysSinceMonday = ((int)today.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
                weekStart = today.AddDays(-daysSinceMonday);
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

            // Check for previous week
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
                                  m.DueDate <= previousWeekEnd);

            var prevWeekNumber = calendar.GetWeekOfYear(previousWeekStart, culture.DateTimeFormat.CalendarWeekRule, culture.DateTimeFormat.FirstDayOfWeek);
            var prevYear = previousWeekStart.Year;

            // Check for next week
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
                                  m.DueDate <= nextWeekEnd);

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
                    .ThenInclude(d => d.DirectorateLookup)
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
                            .Select(d => d.DirectorateLookupId)
                            .ToList();

                        // Add new directorates
                        foreach (var directorateId in request.DirectorateIds)
                        {
                            if (!existingDirectorateIds.Contains(directorateId))
                            {
                                var newDirectorate = new ProjectDirectorate
                                {
                                    ProjectId = project.Id,
                                    DirectorateLookupId = directorateId,
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
            
            // Get all projects
            var allProjects = await _context.Projects
                .Include(p => p.PrimaryContactUser)
                .Include(p => p.DeliveryPriority)
                .Include(p => p.BusinessAreaLookup)
                .Include(p => p.Milestones)
                .Include(p => p.Risks)
                .Include(p => p.Issues)
                .Include(p => p.MonthlyUpdates)
                .Where(p => !p.IsDeleted)
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
            
            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading monthly summary for {Year}-{Month}", year, month);
            TempData["ErrorMessage"] = "An error occurred while loading the monthly summary. Please try again.";
            return RedirectToAction(nameof(Dashboard));
        }
    }

    // GET: CentralOps/AccessDenied
    // This action should NOT require authorization - it's for showing the access denied message
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }
}

