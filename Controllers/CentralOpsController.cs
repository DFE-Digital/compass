using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Compass.Data;
using Compass.Models;
using Compass.Services;
using System.Security.Claims;

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
    public int CriticalPriorityCount { get; set; }
    public int HighPriorityCount { get; set; }
    public int MediumPriorityCount { get; set; }
    public int LowPriorityCount { get; set; }
    public int BlockedCount { get; set; }
}

[Authorize]
public class CentralOpsController : Controller
{
    private readonly CompassDbContext _context;
    private readonly IPermissionService _permissionService;
    private readonly ILogger<CentralOpsController> _logger;

    public CentralOpsController(
        CompassDbContext context,
        IPermissionService permissionService,
        ILogger<CentralOpsController> logger)
    {
        _context = context;
        _permissionService = permissionService;
        _logger = logger;
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

    private string GetUserEmail()
    {
        return User.Identity?.Name 
            ?? User.FindFirst(ClaimTypes.Email)?.Value 
            ?? User.FindFirst("preferred_username")?.Value
            ?? User.FindFirst("email")?.Value
            ?? string.Empty;
    }

    private async Task<bool> IsCentralOpsAdminAsync()
    {
        var userEmail = GetUserEmail();
        if (string.IsNullOrEmpty(userEmail))
            return false;

        return await _permissionService.IsSuperAdminAsync(userEmail) ||
               await _permissionService.IsInGroupAsync(userEmail, "Central Operations Admin");
    }

    // GET: CentralOps/Dashboard
    public async Task<IActionResult> Dashboard()
    {
        if (!await IsCentralOpsAdminAsync())
        {
            return Forbid();
        }

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
                    CriticalPriorityCount = g.Count(p => p.DeliveryPriority != null && p.DeliveryPriority.Name.ToLower().Contains("critical")),
                    HighPriorityCount = g.Count(p => p.DeliveryPriority != null && p.DeliveryPriority.Name.ToLower().Contains("high") && !p.DeliveryPriority.Name.ToLower().Contains("critical")),
                    MediumPriorityCount = g.Count(p => p.DeliveryPriority != null && p.DeliveryPriority.Name.ToLower().Contains("medium")),
                    LowPriorityCount = g.Count(p => p.DeliveryPriority != null && p.DeliveryPriority.Name.ToLower().Contains("low")),
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
                    CriticalPriorityCount = unassignedProjects.Count(p => p.DeliveryPriority != null && p.DeliveryPriority.Name.ToLower().Contains("critical")),
                    HighPriorityCount = unassignedProjects.Count(p => p.DeliveryPriority != null && p.DeliveryPriority.Name.ToLower().Contains("high") && !p.DeliveryPriority.Name.ToLower().Contains("critical")),
                    MediumPriorityCount = unassignedProjects.Count(p => p.DeliveryPriority != null && p.DeliveryPriority.Name.ToLower().Contains("medium")),
                    LowPriorityCount = unassignedProjects.Count(p => p.DeliveryPriority != null && p.DeliveryPriority.Name.ToLower().Contains("low")),
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
        if (!await IsCentralOpsAdminAsync())
        {
            return Forbid();
        }

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

            ViewBag.Search = search;
            ViewBag.BusinessArea = businessArea;
            ViewBag.Priority = priority;
            ViewBag.Rag = rag;
            ViewBag.Status = status;
            ViewBag.TotalCount = totalCount;
            ViewBag.BusinessAreas = businessAreas;
            ViewBag.Priorities = priorities;
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
        if (!await IsCentralOpsAdminAsync())
        {
            return Forbid();
        }

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
        if (!await IsCentralOpsAdminAsync())
        {
            return Forbid();
        }

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
        if (!await IsCentralOpsAdminAsync())
        {
            return Forbid();
        }

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
        if (!await IsCentralOpsAdminAsync())
        {
            return Forbid();
        }

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
}

