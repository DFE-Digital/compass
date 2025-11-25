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
    public int HighPriorityCount { get; set; }
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
                .GroupBy(p => p.RagStatus ?? "Not set")
                .Select(g => new RagSummaryItem
                {
                    Rag = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Rag == "Red" ? 4 : x.Rag == "Amber-Red" ? 3 : x.Rag == "Amber" ? 2 : x.Rag == "Amber-Green" ? 1 : x.Rag == "Green" ? 0 : -1)
                .ToList();

            // Most at risk projects (Red or Amber-Red RAG, or overdue target dates)
            var mostAtRisk = allProjects
                .Where(p => 
                    (p.RagStatus == "Red" || p.RagStatus == "Amber-Red") ||
                    (p.TargetDeliveryDate.HasValue && p.TargetDeliveryDate.Value < DateTime.UtcNow && p.Status == "Active"))
                .OrderByDescending(p => p.RagStatus == "Red" ? 4 : p.RagStatus == "Amber-Red" ? 3 : p.RagStatus == "Amber" ? 2 : 1)
                .ThenBy(p => p.TargetDeliveryDate)
                .Take(20)
                .ToList();

            // Summary by Business Area
            var businessAreaSummary = allProjects
                .Where(p => p.BusinessAreaLookup != null)
                .GroupBy(p => p.BusinessAreaLookup!.Name)
                .Select(g => new BusinessAreaSummaryItem
                {
                    BusinessArea = g.Key,
                    Count = g.Count(),
                    RedCount = g.Count(p => p.RagStatus == "Red"),
                    AmberRedCount = g.Count(p => p.RagStatus == "Amber-Red"),
                    AmberCount = g.Count(p => p.RagStatus == "Amber"),
                    AmberGreenCount = g.Count(p => p.RagStatus == "Amber-Green"),
                    GreenCount = g.Count(p => p.RagStatus == "Green"),
                    HighPriorityCount = g.Count(p => p.DeliveryPriority != null && (p.DeliveryPriority.Name.ToLower().Contains("high") || p.DeliveryPriority.Name.ToLower().Contains("critical"))),
                    BlockedCount = 0 // Projects don't have a blocked status
                })
                .OrderByDescending(x => x.RedCount + x.AmberRedCount)
                .ThenByDescending(x => x.HighPriorityCount)
                .ThenByDescending(x => x.Count)
                .ToList();

            ViewBag.PrioritySummary = prioritySummary;
            ViewBag.RagSummary = ragSummary;
            ViewBag.MostAtRisk = mostAtRisk;
            ViewBag.BusinessAreaSummary = businessAreaSummary;
            ViewBag.ActiveWorkItems = allProjects.Count(p => p.Status == "Active");

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

            // Get all filtered results - DataTables will handle sorting and pagination client-side
            var workItems = await query
                .OrderByDescending(p => p.RagStatus == "Red" ? 4 : p.RagStatus == "Amber-Red" ? 3 : p.RagStatus == "Amber" ? 2 : p.RagStatus == "Amber-Green" ? 1 : p.RagStatus == "Green" ? 0 : -1)
                .ThenByDescending(p => p.TargetDeliveryDate.HasValue ? 1 : 0)
                .ThenBy(p => p.TargetDeliveryDate)
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
}

