using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Compass.Data;
using Compass.Models;
using Compass.Services;
using Microsoft.AspNetCore.Authorization;

namespace Compass.Controllers;

[Authorize]
public class MilestoneController : Controller
{
    private readonly CompassDbContext _context;
    private readonly IProductsApiService _productsApiService;
    private readonly ILogger<MilestoneController> _logger;

    public MilestoneController(CompassDbContext context, IProductsApiService productsApiService, ILogger<MilestoneController> logger)
    {
        _context = context;
        _productsApiService = productsApiService;
        _logger = logger;
    }

    // GET: Milestone
    public async Task<IActionResult> Index(
        int? objectiveId,
        string? dimension,
        string? businessArea,
        string? status,
        string[]? products,
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

        var query = _context.Milestones
            .Include(m => m.OwnerUser)
            .Include(m => m.Objective)
            .Where(m => !m.IsDeleted);

        if (objectiveId.HasValue)
        {
            query = query.Where(m => m.ObjectiveId == objectiveId);
            ViewBag.ObjectiveId = objectiveId;
            var objective = await _context.Objectives.FindAsync(objectiveId);
            ViewBag.ObjectiveTitle = objective?.Title;
        }

        // Apply tab filter
        if (tab == "active")
        {
            query = query.Where(m => m.Status != "complete" && m.Status != "cancelled");
        }
        else if (tab == "closed")
        {
            query = query.Where(m => m.Status == "complete" || m.Status == "cancelled");
        }

        // Apply view scope filter
        if (viewScope == "assigned_to_me" && currentUser != null)
        {
            query = query.Where(m => m.OwnerEmail != null && m.OwnerEmail.ToLower() == currentUser.Email.ToLower());
        }
        else if (viewScope == "my_products")
        {
            query = query.Where(m => !string.IsNullOrEmpty(m.FipsId));
        }
        // organisation_wide shows all items (no additional filter)

        // Apply dimension filters (for backwards compatibility and additional filtering)
        if (dimension == "assigned_to_me" && currentUser != null)
        {
            query = query.Where(m => m.OwnerEmail != null && m.OwnerEmail.ToLower() == currentUser.Email.ToLower());
        }
        else if (dimension == "my_business_area" && userBusinessAreas.Any())
        {
            query = query.Where(m => m.BusinessArea != null && userBusinessAreas.Contains(m.BusinessArea));
        }

        // Apply standard filters
        if (!string.IsNullOrEmpty(businessArea))
        {
            query = query.Where(m => m.BusinessArea == businessArea);
        }

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(m => m.Status == status);
        }

        if (products != null && products.Any())
        {
            query = query.Where(m => m.FipsId != null && products.Contains(m.FipsId));
        }

        var milestones = await query
            .OrderBy(m => m.DueDate)
            .ToListAsync();

        // Get items assigned to current user (separate from filtered results)
        var myMilestones = new List<Milestone>();
        if (currentUser != null)
        {
            myMilestones = await _context.Milestones
                .Include(m => m.OwnerUser)
                .Include(m => m.Objective)
                .Where(m => !m.IsDeleted && m.OwnerEmail != null && m.OwnerEmail.ToLower() == currentUser.Email.ToLower() && m.Status != "complete" && m.Status != "cancelled")
                .OrderBy(m => m.DueDate)
                .Take(10) // Limit to 10 most important
                .ToListAsync();
        }
        ViewBag.MyMilestones = myMilestones;

        // Calculate summary counts
        var allMilestonesQuery = _context.Milestones.Where(m => !m.IsDeleted);
        if (objectiveId.HasValue)
        {
            allMilestonesQuery = allMilestonesQuery.Where(m => m.ObjectiveId == objectiveId);
        }
        
        ViewBag.OpenCount = await allMilestonesQuery.CountAsync(m => m.Status != "complete" && m.Status != "cancelled");
        ViewBag.ClosedCount = await allMilestonesQuery.CountAsync(m => m.Status == "complete" || m.Status == "cancelled");
        ViewBag.OverdueCount = await allMilestonesQuery.CountAsync(m => 
            m.Status != "complete" && 
            m.Status != "cancelled" && 
            m.DueDate < DateTime.Now);
        
        // Calculate view dimension counts (active items only)
        var activeMilestonesQuery = allMilestonesQuery.Where(m => m.Status != "complete" && m.Status != "cancelled");
        ViewBag.AssignedToMeCount = currentUser != null 
            ? await activeMilestonesQuery.CountAsync(m => m.OwnerEmail != null && m.OwnerEmail.ToLower() == currentUser.Email.ToLower())
            : 0;
        ViewBag.MyProductsCount = await activeMilestonesQuery.CountAsync(m => !string.IsNullOrEmpty(m.FipsId));
        ViewBag.OrganisationWideCount = await activeMilestonesQuery.CountAsync();

        // Pass filter values to view
        ViewBag.CurrentTab = tab;
        ViewBag.CurrentViewScope = viewScope;
        ViewBag.CurrentDimension = dimension;
        ViewBag.CurrentBusinessArea = businessArea;
        ViewBag.CurrentStatus = status;
        ViewBag.CurrentProducts = products ?? Array.Empty<string>();
        
        // Get data for filter dropdowns
        var businessAreas = await _productsApiService.GetBusinessAreasAsync();
        ViewBag.BusinessAreas = businessAreas;
        
        // Get distinct products (FipsId) that are actually used in milestones
        var fipsIdsInUse = await _context.Milestones
            .Where(m => !m.IsDeleted && !string.IsNullOrEmpty(m.FipsId))
            .Select(m => m.FipsId)
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
        
        return View(milestones);
    }

    // GET: Milestone/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var milestone = await _context.Milestones
            .Include(m => m.OwnerUser)
            .Include(m => m.Objective)
            .Include(m => m.MilestoneActions)
                .ThenInclude(ma => ma.Action)
            .Include(m => m.MilestoneRisks)
                .ThenInclude(mr => mr.Risk)
            .Include(m => m.MilestoneIssues)
                .ThenInclude(mi => mi.Issue)
            .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);

        if (milestone == null)
        {
            return NotFound();
        }

        // Fetch products for lookup
        var products = await _productsApiService.GetProductsAsync();
        ViewBag.Products = products.OrderBy(p => p.Title).ToList();

        return View(milestone);
    }

    // GET: Milestone/Create
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

    // POST: Milestone/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("ObjectiveId,FipsId,Name,Description,BusinessArea,OwnerUserId,OwnerEmail,BaselineDueDate,DueDate,ActualDate,Status,ProgressPercent,ExternalRef,Notes")] Milestone milestone)
    {
        if (ModelState.IsValid)
        {
            try
            {
                // Ensure nullable foreign keys are set to null if they're 0 or invalid
                if (milestone.ObjectiveId == 0) milestone.ObjectiveId = null;
                if (milestone.OwnerUserId == 0) milestone.OwnerUserId = null;
                if (string.IsNullOrWhiteSpace(milestone.OwnerEmail)) milestone.OwnerEmail = null;
                
                milestone.CreatedAt = DateTime.UtcNow;
                milestone.UpdatedAt = DateTime.UtcNow;
                milestone.IsDeleted = false;
                
                _context.Add(milestone);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = $"Milestone '{milestone.Name}' has been created successfully.";
                
                if (milestone.ObjectiveId.HasValue)
                {
                    return RedirectToAction(nameof(Index), new { objectiveId = milestone.ObjectiveId });
                }
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating milestone");
                ModelState.AddModelError("", "An error occurred while creating the milestone. Please try again.");
            }
        }
        
        ViewBag.Users = new SelectList(await _context.Users.OrderBy(u => u.Name).ToListAsync(), "Id", "Name", milestone.OwnerUserId);
        ViewBag.Objectives = new SelectList(await _context.Objectives.Where(o => !o.IsDeleted).OrderBy(o => o.Title).ToListAsync(), "Id", "Title", milestone.ObjectiveId);
        
        var products = await _productsApiService.GetProductsAsync(null);
        ViewBag.Products = products.OrderBy(p => p.Title).ToList();
        
        var businessAreas = await _productsApiService.GetBusinessAreasAsync();
        ViewBag.BusinessAreas = businessAreas;
        
        return View(milestone);
    }

    // GET: Milestone/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var milestone = await _context.Milestones.FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);
        if (milestone == null)
        {
            return NotFound();
        }

        ViewBag.Users = new SelectList(await _context.Users.OrderBy(u => u.Name).ToListAsync(), "Id", "Name", milestone.OwnerUserId);
        ViewBag.Objectives = new SelectList(await _context.Objectives.Where(o => !o.IsDeleted).OrderBy(o => o.Title).ToListAsync(), "Id", "Title", milestone.ObjectiveId);
        
        var products = await _productsApiService.GetProductsAsync(null);
        ViewBag.Products = products.OrderBy(p => p.Title).ToList();
        
        var businessAreas = await _productsApiService.GetBusinessAreasAsync();
        ViewBag.BusinessAreas = businessAreas;
        
        return View(milestone);
    }

    // POST: Milestone/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,ObjectiveId,FipsId,Name,Description,BusinessArea,OwnerUserId,OwnerEmail,BaselineDueDate,DueDate,ActualDate,Status,ProgressPercent,ExternalRef,Notes")] Milestone milestone)
    {
        if (id != milestone.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                var existingMilestone = await _context.Milestones.FindAsync(id);
                if (existingMilestone == null || existingMilestone.IsDeleted)
                {
                    return NotFound();
                }

                // Ensure nullable foreign keys are set to null if they're 0 or invalid
                if (milestone.ObjectiveId == 0) milestone.ObjectiveId = null;
                if (milestone.OwnerUserId == 0) milestone.OwnerUserId = null;
                if (string.IsNullOrWhiteSpace(milestone.OwnerEmail)) milestone.OwnerEmail = null;

                existingMilestone.ObjectiveId = milestone.ObjectiveId;
                existingMilestone.FipsId = milestone.FipsId;
                existingMilestone.Name = milestone.Name;
                existingMilestone.Description = milestone.Description;
                existingMilestone.BusinessArea = milestone.BusinessArea;
                existingMilestone.OwnerUserId = milestone.OwnerUserId;
                existingMilestone.OwnerEmail = milestone.OwnerEmail;
                existingMilestone.BaselineDueDate = milestone.BaselineDueDate;
                existingMilestone.DueDate = milestone.DueDate;
                existingMilestone.ActualDate = milestone.ActualDate;
                existingMilestone.Status = milestone.Status;
                existingMilestone.ProgressPercent = milestone.ProgressPercent;
                existingMilestone.ExternalRef = milestone.ExternalRef;
                existingMilestone.Notes = milestone.Notes;
                existingMilestone.UpdatedAt = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = $"Milestone '{milestone.Name}' has been updated successfully.";
                
                if (milestone.ObjectiveId.HasValue)
                {
                    return RedirectToAction(nameof(Index), new { objectiveId = milestone.ObjectiveId });
                }
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating milestone");
                ModelState.AddModelError("", "An error occurred while updating the milestone. Please try again.");
            }
        }
        
        ViewBag.Users = new SelectList(await _context.Users.OrderBy(u => u.Name).ToListAsync(), "Id", "Name", milestone.OwnerUserId);
        ViewBag.Objectives = new SelectList(await _context.Objectives.Where(o => !o.IsDeleted).OrderBy(o => o.Title).ToListAsync(), "Id", "Title", milestone.ObjectiveId);
        
        var products = await _productsApiService.GetProductsAsync(null);
        ViewBag.Products = products.OrderBy(p => p.Title).ToList();
        
        var businessAreas = await _productsApiService.GetBusinessAreasAsync();
        ViewBag.BusinessAreas = businessAreas;
        
        return View(milestone);
    }

    // GET: Milestone/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var milestone = await _context.Milestones
            .Include(m => m.OwnerUser)
            .Include(m => m.Objective)
            .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);
            
        if (milestone == null)
        {
            return NotFound();
        }

        return View(milestone);
    }

    // POST: Milestone/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        try
        {
            var milestone = await _context.Milestones.FindAsync(id);
            if (milestone != null && !milestone.IsDeleted)
            {
                milestone.IsDeleted = true;
                milestone.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = $"Milestone '{milestone.Name}' has been deleted successfully.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting milestone");
            TempData["ErrorMessage"] = "An error occurred while deleting the milestone. Please try again.";
        }
        
        return RedirectToAction(nameof(Index));
    }
}

