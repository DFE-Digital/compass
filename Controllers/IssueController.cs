using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Compass.Data;
using Compass.Models;
using Compass.Services;
using Microsoft.AspNetCore.Authorization;

namespace Compass.Controllers;

[Authorize]
public class IssueController : Controller
{
    private readonly CompassDbContext _context;
    private readonly IProductsApiService _productsApiService;
    private readonly ILogger<IssueController> _logger;

    public IssueController(CompassDbContext context, IProductsApiService productsApiService, ILogger<IssueController> logger)
    {
        _context = context;
        _productsApiService = productsApiService;
        _logger = logger;
    }

    // GET: Issue
    public async Task<IActionResult> Index(
        int? objectiveId,
        string? dimension,
        string? businessArea,
        string? status,
        string[]? products,
        string? severity,
        string? priority,
        string tab = "active",
        string viewScope = "assigned_to_me")
    {
        var userEmail = User.Identity?.Name;
        var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == userEmail!.ToLower());
        
        // Get user's preferred business areas for "My business area" dimension
        var userBusinessAreas = new List<string>();
        if (currentUser != null)
        {
            var preferences = await _context.UserPreferences.FindAsync(currentUser.Id);
            if (preferences != null && !string.IsNullOrEmpty(preferences.PreferredBusinessAreas))
            {
                userBusinessAreas = preferences.PreferredBusinessAreas
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(ba => ba.Trim())
                    .ToList();
            }
        }

        var query = _context.Issues
            .Include(i => i.OwnerUser)
            .Include(i => i.Objective)
            .Where(i => !i.IsDeleted);

        if (objectiveId.HasValue)
        {
            query = query.Where(i => i.ObjectiveId == objectiveId);
            ViewBag.ObjectiveId = objectiveId;
            var objective = await _context.Objectives.FindAsync(objectiveId);
            ViewBag.ObjectiveTitle = objective?.Title;
        }

        // Apply tab filter
        if (tab == "active")
        {
            query = query.Where(i => i.Status != "resolved" && i.Status != "closed");
        }
        else if (tab == "closed")
        {
            query = query.Where(i => i.Status == "resolved" || i.Status == "closed");
        }

        // Apply view scope filter
        if (viewScope == "assigned_to_me" && currentUser != null)
        {
            query = query.Where(i => i.OwnerUserId == currentUser.Id);
        }
        else if (viewScope == "my_products")
        {
            query = query.Where(i => !string.IsNullOrEmpty(i.FipsId));
        }
        // organisation_wide shows all items (no additional filter)

        // Apply dimension filters (for backwards compatibility and additional filtering)
        if (dimension == "assigned_to_me" && currentUser != null)
        {
            query = query.Where(i => i.OwnerUserId == currentUser.Id);
        }
        else if (dimension == "my_business_area" && userBusinessAreas.Any())
        {
            query = query.Where(i => i.BusinessArea != null && userBusinessAreas.Contains(i.BusinessArea));
        }

        // Apply standard filters
        if (!string.IsNullOrEmpty(businessArea))
        {
            query = query.Where(i => i.BusinessArea == businessArea);
        }

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(i => i.Status == status);
        }

        if (products != null && products.Any())
        {
            query = query.Where(i => i.FipsId != null && products.Contains(i.FipsId));
        }

        // Apply issue-specific filters
        if (!string.IsNullOrEmpty(severity))
        {
            query = query.Where(i => i.Severity == severity);
        }

        if (!string.IsNullOrEmpty(priority))
        {
            query = query.Where(i => i.Priority == priority);
        }

        var issues = await query
            .OrderByDescending(i => i.Severity)
            .ThenBy(i => i.TargetResolutionDate)
            .ToListAsync();

        // Get items assigned to current user (separate from filtered results)
        var myIssues = new List<Issue>();
        if (currentUser != null)
        {
            myIssues = await _context.Issues
                .Include(i => i.OwnerUser)
                .Include(i => i.Objective)
                .Where(i => !i.IsDeleted && i.OwnerUserId == currentUser.Id && i.Status != "resolved" && i.Status != "closed")
                .OrderByDescending(i => i.Severity)
                .ThenBy(i => i.TargetResolutionDate)
                .Take(10) // Limit to 10 most important
                .ToListAsync();
        }
        ViewBag.MyIssues = myIssues;

        // Calculate summary counts
        var allIssuesQuery = _context.Issues.Where(i => !i.IsDeleted);
        if (objectiveId.HasValue)
        {
            allIssuesQuery = allIssuesQuery.Where(i => i.ObjectiveId == objectiveId);
        }
        
        ViewBag.OpenCount = await allIssuesQuery.CountAsync(i => i.Status != "resolved" && i.Status != "closed");
        ViewBag.ClosedCount = await allIssuesQuery.CountAsync(i => i.Status == "resolved" || i.Status == "closed");
        ViewBag.OverdueCount = await allIssuesQuery.CountAsync(i => 
            i.Status != "resolved" && 
            i.Status != "closed" && 
            i.TargetResolutionDate.HasValue && 
            i.TargetResolutionDate.Value < DateTime.Now);
        
        // Calculate view dimension counts (active items only)
        var activeIssuesQuery = allIssuesQuery.Where(i => i.Status != "resolved" && i.Status != "closed");
        ViewBag.AssignedToMeCount = currentUser != null 
            ? await activeIssuesQuery.CountAsync(i => i.OwnerUserId == currentUser.Id)
            : 0;
        ViewBag.MyProductsCount = await activeIssuesQuery.CountAsync(i => !string.IsNullOrEmpty(i.FipsId));
        ViewBag.OrganisationWideCount = await activeIssuesQuery.CountAsync();

        // Pass filter values to view
        ViewBag.CurrentTab = tab;
        ViewBag.CurrentViewScope = viewScope;
        ViewBag.CurrentDimension = dimension;
        ViewBag.CurrentBusinessArea = businessArea;
        ViewBag.CurrentStatus = status;
        ViewBag.CurrentProducts = products ?? Array.Empty<string>();
        ViewBag.CurrentSeverity = severity;
        ViewBag.CurrentPriority = priority;
        
        // Get data for filter dropdowns
        var businessAreas = await _productsApiService.GetBusinessAreasAsync();
        ViewBag.BusinessAreas = businessAreas;
        
        // Get distinct products (FipsId) that are actually used in issues
        var fipsIdsInUse = await _context.Issues
            .Where(i => !i.IsDeleted && !string.IsNullOrEmpty(i.FipsId))
            .Select(i => i.FipsId)
            .Distinct()
            .ToListAsync();
        
        // Get product details from API service
        var allProducts = await _productsApiService.GetProductsAsync();
        var productsInUse = allProducts
            .Where(p => fipsIdsInUse.Contains(p.FipsId))
            .OrderBy(p => p.Title)
            .ToList();
        
        ViewBag.Products = productsInUse;
        
        // Create a mapping dictionary for FipsId to Product Title
        var productMapping = allProducts.ToDictionary(p => p.FipsId ?? "", p => p.Title);
        ViewBag.ProductMapping = productMapping;
        
        ViewBag.UserBusinessAreas = userBusinessAreas;
        
        return View(issues);
    }

    // GET: Issue/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var issue = await _context.Issues
            .Include(i => i.OwnerUser)
            .Include(i => i.Objective)
            .Include(i => i.IssueActions)
                .ThenInclude(ia => ia.Action)
            .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);

        if (issue == null)
        {
            return NotFound();
        }

        // Fetch products for lookup
        var products = await _productsApiService.GetProductsAsync();
        ViewBag.Products = products.OrderBy(p => p.Title).ToList();

        return View(issue);
    }

    // GET: Issue/Create
    public async Task<IActionResult> Create(int? objectiveId)
    {
        ViewBag.Users = new SelectList(await _context.Users.OrderBy(u => u.Name).ToListAsync(), "Id", "Name");
        ViewBag.Objectives = new SelectList(await _context.Objectives.Where(o => !o.IsDeleted).OrderBy(o => o.Title).ToListAsync(), "Id", "Title", objectiveId);
        
        var products = await _productsApiService.GetProductsAsync(null);
        ViewBag.Products = products.OrderBy(p => p.Title).ToList();
        
        var businessAreas = await _productsApiService.GetBusinessAreasAsync();
        ViewBag.BusinessAreas = businessAreas;
        
        return View();
    }

    // POST: Issue/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("ObjectiveId,FipsId,Title,Description,Category,BusinessArea,OwnerUserId,Severity,Priority,DetectedDate,TargetResolutionDate,Status,ResolutionSummary,Workaround,BlockedFlag")] Issue issue)
    {
        if (ModelState.IsValid)
        {
            try
            {
                // Ensure nullable foreign keys are set to null if they're 0 or invalid
                if (issue.ObjectiveId == 0) issue.ObjectiveId = null;
                if (issue.OwnerUserId == 0) issue.OwnerUserId = null;
                
                issue.CreatedAt = DateTime.UtcNow;
                issue.UpdatedAt = DateTime.UtcNow;
                issue.IsDeleted = false;
                
                _context.Add(issue);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = $"Issue '{issue.Title}' has been created successfully.";
                
                if (issue.ObjectiveId.HasValue)
                {
                    return RedirectToAction(nameof(Index), new { objectiveId = issue.ObjectiveId });
                }
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating issue");
                ModelState.AddModelError("", "An error occurred while creating the issue. Please try again.");
            }
        }
        
        ViewBag.Users = new SelectList(await _context.Users.OrderBy(u => u.Name).ToListAsync(), "Id", "Name", issue.OwnerUserId);
        ViewBag.Objectives = new SelectList(await _context.Objectives.Where(o => !o.IsDeleted).OrderBy(o => o.Title).ToListAsync(), "Id", "Title", issue.ObjectiveId);
        
        var products = await _productsApiService.GetProductsAsync(null);
        ViewBag.Products = products.OrderBy(p => p.Title).ToList();
        
        var businessAreas = await _productsApiService.GetBusinessAreasAsync();
        ViewBag.BusinessAreas = businessAreas;
        
        return View(issue);
    }

    // GET: Issue/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var issue = await _context.Issues.FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted);
        if (issue == null)
        {
            return NotFound();
        }

        ViewBag.Users = new SelectList(await _context.Users.OrderBy(u => u.Name).ToListAsync(), "Id", "Name", issue.OwnerUserId);
        ViewBag.Objectives = new SelectList(await _context.Objectives.Where(o => !o.IsDeleted).OrderBy(o => o.Title).ToListAsync(), "Id", "Title", issue.ObjectiveId);
        
        var products = await _productsApiService.GetProductsAsync(null);
        ViewBag.Products = products.OrderBy(p => p.Title).ToList();
        
        var businessAreas = await _productsApiService.GetBusinessAreasAsync();
        ViewBag.BusinessAreas = businessAreas;
        
        return View(issue);
    }

    // POST: Issue/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,ObjectiveId,FipsId,Title,Description,Category,BusinessArea,OwnerUserId,Severity,Priority,DetectedDate,TargetResolutionDate,Status,ResolutionSummary,Workaround,BlockedFlag,ClosedDate")] Issue issue)
    {
        if (id != issue.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                var existingIssue = await _context.Issues.FindAsync(id);
                if (existingIssue == null || existingIssue.IsDeleted)
                {
                    return NotFound();
                }

                // Ensure nullable foreign keys are set to null if they're 0 or invalid
                if (issue.ObjectiveId == 0) issue.ObjectiveId = null;
                if (issue.OwnerUserId == 0) issue.OwnerUserId = null;

                existingIssue.ObjectiveId = issue.ObjectiveId;
                existingIssue.FipsId = issue.FipsId;
                existingIssue.Title = issue.Title;
                existingIssue.Description = issue.Description;
                existingIssue.Category = issue.Category;
                existingIssue.BusinessArea = issue.BusinessArea;
                existingIssue.OwnerUserId = issue.OwnerUserId;
                existingIssue.Severity = issue.Severity;
                existingIssue.Priority = issue.Priority;
                existingIssue.DetectedDate = issue.DetectedDate;
                existingIssue.TargetResolutionDate = issue.TargetResolutionDate;
                existingIssue.Status = issue.Status;
                existingIssue.ResolutionSummary = issue.ResolutionSummary;
                existingIssue.Workaround = issue.Workaround;
                existingIssue.BlockedFlag = issue.BlockedFlag;
                existingIssue.ClosedDate = issue.ClosedDate;
                existingIssue.UpdatedAt = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = $"Issue '{issue.Title}' has been updated successfully.";
                
                if (issue.ObjectiveId.HasValue)
                {
                    return RedirectToAction(nameof(Index), new { objectiveId = issue.ObjectiveId });
                }
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating issue");
                ModelState.AddModelError("", "An error occurred while updating the issue. Please try again.");
            }
        }
        
        ViewBag.Users = new SelectList(await _context.Users.OrderBy(u => u.Name).ToListAsync(), "Id", "Name", issue.OwnerUserId);
        ViewBag.Objectives = new SelectList(await _context.Objectives.Where(o => !o.IsDeleted).OrderBy(o => o.Title).ToListAsync(), "Id", "Title", issue.ObjectiveId);
        
        var products = await _productsApiService.GetProductsAsync(null);
        ViewBag.Products = products.OrderBy(p => p.Title).ToList();
        
        var businessAreas = await _productsApiService.GetBusinessAreasAsync();
        ViewBag.BusinessAreas = businessAreas;
        
        return View(issue);
    }

    // GET: Issue/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var issue = await _context.Issues
            .Include(i => i.OwnerUser)
            .Include(i => i.Objective)
            .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);
            
        if (issue == null)
        {
            return NotFound();
        }

        return View(issue);
    }

    // POST: Issue/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        try
        {
            var issue = await _context.Issues.FindAsync(id);
            if (issue != null && !issue.IsDeleted)
            {
                issue.IsDeleted = true;
                issue.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = $"Issue '{issue.Title}' has been deleted successfully.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting issue");
            TempData["ErrorMessage"] = "An error occurred while deleting the issue. Please try again.";
        }
        
        return RedirectToAction(nameof(Index));
    }
}

