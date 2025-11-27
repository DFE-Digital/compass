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

    public CentralOpsController(
        CompassDbContext context,
        ILogger<CentralOpsController> logger,
        IUserDirectoryService userDirectoryService)
    {
        _context = context;
        _logger = logger;
        _userDirectoryService = userDirectoryService;
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


    // GET: CentralOps/Dashboard
    public async Task<IActionResult> Dashboard()
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

            _logger.LogInformation("Dashboard: Loaded {Count} projects from database", allProjects.Count);
            
            if (allProjects.Count == 0)
            {
                _logger.LogWarning("Dashboard: No projects found in database. This might indicate a data issue.");
            }

            // Summary by Priority
            var prioritySummary = allProjects
                .GroupBy(p => p.DeliveryPriority != null ? p.DeliveryPriority.Name : "Not set")
                .Select(g => new PrioritySummaryItem
                {
                    Priority = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Priority)
                .ToList();

            // Summary by RAG
            var ragSummary = allProjects
                .Where(p => !string.IsNullOrEmpty(p.RagStatus))
                .GroupBy(p => NormalizeRagStatus(p.RagStatus) ?? "Not set")
                .Select(g => new RagSummaryItem
                {
                    Rag = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Rag == "Red" ? 4 : x.Rag == "Amber-Red" ? 3 : x.Rag == "Amber" ? 2 : x.Rag == "Amber-Green" ? 1 : x.Rag == "Green" ? 0 : -1)
                .ToList();

            // Most at risk projects (Red, Amber-Red RAG, or High/Critical priority, or overdue target dates)
            var mostAtRisk = allProjects
                .Where(p => 
                    {
                        var normalizedRag = NormalizeRagStatus(p.RagStatus);
                        var isHighPriority = p.DeliveryPriority != null && 
                            (p.DeliveryPriority.Name.ToLower().Contains("high") || 
                             p.DeliveryPriority.Name.ToLower().Contains("critical"));
                        return (normalizedRag == "Red" || normalizedRag == "Amber-Red") ||
                               isHighPriority ||
                               (p.TargetDeliveryDate.HasValue && p.TargetDeliveryDate.Value < DateTime.UtcNow && p.Status == "Active");
                    })
                .OrderByDescending(p => 
                    {
                        var normalizedRag = NormalizeRagStatus(p.RagStatus);
                        var isHighPriority = p.DeliveryPriority != null && 
                            (p.DeliveryPriority.Name.ToLower().Contains("high") || 
                             p.DeliveryPriority.Name.ToLower().Contains("critical"));
                        if (normalizedRag == "Red") return 5;
                        if (normalizedRag == "Amber-Red") return 4;
                        if (isHighPriority) return 3;
                        if (normalizedRag == "Amber") return 2;
                        return 1;
                    })
                .ThenBy(p => p.TargetDeliveryDate)
                .ToList();

            // Summary by Business Area - sorted alphabetically
            var businessAreaSummary = allProjects
                .Where(p => p.BusinessAreaLookup != null)
                .GroupBy(p => p.BusinessAreaLookup!.Name)
                .Select(g => new BusinessAreaSummaryItem
                {
                    BusinessArea = g.Key,
                    Count = g.Count(),
                    RedCount = g.Count(p => NormalizeRagStatus(p.RagStatus) == "Red"),
                    AmberRedCount = g.Count(p => NormalizeRagStatus(p.RagStatus) == "Amber-Red"),
                    AmberCount = g.Count(p => NormalizeRagStatus(p.RagStatus) == "Amber"),
                    AmberGreenCount = g.Count(p => NormalizeRagStatus(p.RagStatus) == "Amber-Green"),
                    GreenCount = g.Count(p => NormalizeRagStatus(p.RagStatus) == "Green"),
                    RagNotSetCount = g.Count(p => string.IsNullOrWhiteSpace(p.RagStatus)),
                    CriticalPriorityCount = g.Count(p => p.DeliveryPriority != null && p.DeliveryPriority.Name.ToLower().Contains("critical")),
                    HighPriorityCount = g.Count(p => p.DeliveryPriority != null && p.DeliveryPriority.Name.ToLower().Contains("high") && !p.DeliveryPriority.Name.ToLower().Contains("critical")),
                    MediumPriorityCount = g.Count(p => p.DeliveryPriority != null && p.DeliveryPriority.Name.ToLower().Contains("medium")),
                    LowPriorityCount = g.Count(p => p.DeliveryPriority != null && p.DeliveryPriority.Name.ToLower().Contains("low")),
                    PriorityNotSetCount = g.Count(p => p.DeliveryPriority == null),
                    BlockedCount = 0 // Projects don't have a blocked status
                })
                .OrderBy(x => x.BusinessArea)
                .ToList();

            // Add "Not assigned" row for projects without a business area
            var unassignedProjects = allProjects.Where(p => p.BusinessAreaLookup == null).ToList();
            if (unassignedProjects.Any())
            {
                var unassignedSummary = new BusinessAreaSummaryItem
                {
                    BusinessArea = "Not assigned",
                    Count = unassignedProjects.Count,
                    RedCount = unassignedProjects.Count(p => NormalizeRagStatus(p.RagStatus) == "Red"),
                    AmberRedCount = unassignedProjects.Count(p => NormalizeRagStatus(p.RagStatus) == "Amber-Red"),
                    AmberCount = unassignedProjects.Count(p => NormalizeRagStatus(p.RagStatus) == "Amber"),
                    AmberGreenCount = unassignedProjects.Count(p => NormalizeRagStatus(p.RagStatus) == "Amber-Green"),
                    GreenCount = unassignedProjects.Count(p => NormalizeRagStatus(p.RagStatus) == "Green"),
                    RagNotSetCount = unassignedProjects.Count(p => string.IsNullOrWhiteSpace(p.RagStatus)),
                    CriticalPriorityCount = unassignedProjects.Count(p => p.DeliveryPriority != null && p.DeliveryPriority.Name.ToLower().Contains("critical")),
                    HighPriorityCount = unassignedProjects.Count(p => p.DeliveryPriority != null && p.DeliveryPriority.Name.ToLower().Contains("high") && !p.DeliveryPriority.Name.ToLower().Contains("critical")),
                    MediumPriorityCount = unassignedProjects.Count(p => p.DeliveryPriority != null && p.DeliveryPriority.Name.ToLower().Contains("medium")),
                    LowPriorityCount = unassignedProjects.Count(p => p.DeliveryPriority != null && p.DeliveryPriority.Name.ToLower().Contains("low")),
                    PriorityNotSetCount = unassignedProjects.Count(p => p.DeliveryPriority == null),
                    BlockedCount = 0
                };
                businessAreaSummary.Add(unassignedSummary);
            }

            ViewBag.PrioritySummary = prioritySummary;
            ViewBag.RagSummary = ragSummary;
            ViewBag.MostAtRisk = mostAtRisk;
            ViewBag.BusinessAreaSummary = businessAreaSummary;
            ViewBag.ActiveWorkItems = allProjects.Count(p => p.Status == "Active");
            ViewBag.AllProjects = allProjects.OrderBy(p => p.Title).ToList();

            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading Central Operations dashboard");
            TempData["ErrorMessage"] = "An error occurred while loading the dashboard. Please try again.";
            return View();
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

    // GET: CentralOps/AccessDenied
    // This action should NOT require authorization - it's for showing the access denied message
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }
}

