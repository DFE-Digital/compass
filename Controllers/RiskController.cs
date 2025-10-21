using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Compass.Data;
using Compass.Models;
using Compass.Services;
using Microsoft.AspNetCore.Authorization;

namespace Compass.Controllers;

[Authorize]
public class RiskController : Controller
{
    private readonly CompassDbContext _context;
    private readonly IProductsApiService _productsApiService;
    private readonly ILogger<RiskController> _logger;

    public RiskController(CompassDbContext context, IProductsApiService productsApiService, ILogger<RiskController> logger)
    {
        _context = context;
        _productsApiService = productsApiService;
        _logger = logger;
    }

    // GET: Risk
    public async Task<IActionResult> Index(
        int? objectiveId,
        string? dimension,
        string[]? businessAreas,
        string[]? statuses,
        string[]? products,
        int[]? riskTypeIds,
        int[]? riskTierIds,
        int[]? impacts,
        int[]? likelihoods,
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

        var query = _context.Risks
            .Include(r => r.Objective)
            .Include(r => r.RiskTier)
            .Include(r => r.RiskRiskTypes)
                .ThenInclude(rrt => rrt.RiskType)
            .Where(r => !r.IsDeleted);

        if (objectiveId.HasValue)
        {
            query = query.Where(r => r.ObjectiveId == objectiveId);
            ViewBag.ObjectiveId = objectiveId;
            var objective = await _context.Objectives.FindAsync(objectiveId);
            ViewBag.ObjectiveTitle = objective?.Title;
        }

        // Apply tab filter
        if (tab == "active")
        {
            query = query.Where(r => r.Status != "closed");
        }
        else if (tab == "closed")
        {
            query = query.Where(r => r.Status == "closed");
        }

        // Apply view scope filter
        if (viewScope == "assigned_to_me" && currentUser != null)
        {
            query = query.Where(r => r.OwnerEmail != null && r.OwnerEmail.ToLower() == currentUser.Email.ToLower());
        }
        else if (viewScope == "my_products")
        {
            query = query.Where(r => !string.IsNullOrEmpty(r.FipsId));
        }
        // organisation_wide shows all items (no additional filter)

        // Apply dimension filters (for backwards compatibility and additional filtering)
        if (dimension == "assigned_to_me" && currentUser != null)
        {
            query = query.Where(r => r.OwnerEmail != null && r.OwnerEmail.ToLower() == currentUser.Email.ToLower());
        }
        else if (dimension == "my_business_area" && userBusinessAreas.Any())
        {
            query = query.Where(r => r.BusinessArea != null && userBusinessAreas.Contains(r.BusinessArea));
        }

        // Apply standard filters
        if (businessAreas != null && businessAreas.Any())
        {
            query = query.Where(r => r.BusinessArea != null && businessAreas.Contains(r.BusinessArea));
        }

        if (statuses != null && statuses.Any())
        {
            query = query.Where(r => statuses.Contains(r.Status));
        }

        if (products != null && products.Any())
        {
            query = query.Where(r => r.FipsId != null && products.Contains(r.FipsId));
        }

        // Apply risk-specific filters
        if (riskTypeIds != null && riskTypeIds.Any())
        {
            query = query.Where(r => r.RiskRiskTypes.Any(rrt => riskTypeIds.Contains(rrt.RiskTypeId)));
        }

        if (riskTierIds != null && riskTierIds.Any())
        {
            query = query.Where(r => r.RiskTierId.HasValue && riskTierIds.Contains(r.RiskTierId.Value));
        }

        if (impacts != null && impacts.Any())
        {
            query = query.Where(r => impacts.Contains(r.ImpactRating));
        }

        if (likelihoods != null && likelihoods.Any())
        {
            query = query.Where(r => likelihoods.Contains(r.LikelihoodRating));
        }

        var risks = await query
            .OrderByDescending(r => r.RiskScore)
            .ThenBy(r => r.ProximityDate)
            .ToListAsync();

        // Get items assigned to current user (separate from filtered results)
        var myRisks = new List<Risk>();
        if (currentUser != null)
        {
            myRisks = await _context.Risks
                .Include(r => r.Objective)
                .Where(r => !r.IsDeleted && r.OwnerEmail != null && r.OwnerEmail.ToLower() == currentUser.Email.ToLower() && r.Status != "closed")
                .OrderByDescending(r => r.RiskScore)
                .ThenBy(r => r.ProximityDate)
                .Take(10) // Limit to 10 most important
                .ToListAsync();
        }
        ViewBag.MyRisks = myRisks;

        // Calculate summary counts
        var allRisksQuery = _context.Risks.Where(r => !r.IsDeleted);
        if (objectiveId.HasValue)
        {
            allRisksQuery = allRisksQuery.Where(r => r.ObjectiveId == objectiveId);
        }
        
        ViewBag.NewCount = await allRisksQuery.CountAsync(r => r.Status == "new");
        ViewBag.OpenCount = await allRisksQuery.CountAsync(r => r.Status != "closed" && r.Status != "new");
        ViewBag.ClosedCount = await allRisksQuery.CountAsync(r => r.Status == "closed");
        ViewBag.OverdueCount = await allRisksQuery.CountAsync(r => 
            r.Status != "closed" && 
            r.ProximityDate.HasValue && 
            r.ProximityDate.Value < DateTime.Now);
        
        // Calculate view dimension counts (active items only)
        var activeRisksQuery = allRisksQuery.Where(r => r.Status != "closed");
        ViewBag.AssignedToMeCount = currentUser != null 
            ? await activeRisksQuery.CountAsync(r => r.OwnerEmail != null && r.OwnerEmail.ToLower() == currentUser.Email.ToLower())
            : 0;
        ViewBag.MyProductsCount = await activeRisksQuery.CountAsync(r => !string.IsNullOrEmpty(r.FipsId));
        ViewBag.OrganisationWideCount = await activeRisksQuery.CountAsync();

        // Pass filter values to view
        ViewBag.CurrentTab = tab;
        ViewBag.CurrentViewScope = viewScope;
        ViewBag.CurrentDimension = dimension;
        ViewBag.CurrentBusinessAreas = businessAreas ?? Array.Empty<string>();
        ViewBag.CurrentStatuses = statuses ?? Array.Empty<string>();
        ViewBag.CurrentProducts = products ?? Array.Empty<string>();
        ViewBag.CurrentRiskTypeIds = riskTypeIds ?? Array.Empty<int>();
        ViewBag.CurrentRiskTierIds = riskTierIds ?? Array.Empty<int>();
        ViewBag.CurrentImpacts = impacts ?? Array.Empty<int>();
        ViewBag.CurrentLikelihoods = likelihoods ?? Array.Empty<int>();
        
        // Get data for filter options
        var allBusinessAreas = await _productsApiService.GetBusinessAreasAsync();
        ViewBag.BusinessAreas = allBusinessAreas;
        
        // Get all risk types and tiers
        var allRiskTypes = await _context.RiskTypes.Where(rt => rt.IsActive).OrderBy(rt => rt.Name).ToListAsync();
        ViewBag.RiskTypes = allRiskTypes;
        
        var allRiskTiers = await _context.RiskTiers.Where(rt => rt.IsActive).OrderBy(rt => rt.SortOrder).ToListAsync();
        ViewBag.RiskTiers = allRiskTiers;
        
        // Get distinct products (FipsId) that are actually used in risks
        var fipsIdsInUse = await _context.Risks
            .Where(r => !r.IsDeleted && !string.IsNullOrEmpty(r.FipsId))
            .Select(r => r.FipsId)
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
        
        return View(risks);
    }

    // GET: Risk/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var risk = await _context.Risks
            .Include(r => r.Objective)
            .Include(r => r.RiskTier)
            .Include(r => r.RiskRiskTypes)
                .ThenInclude(rrt => rrt.RiskType)
            .Include(r => r.RiskActions)
                .ThenInclude(ra => ra.Action)
            .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);

        if (risk == null)
        {
            return NotFound();
        }

        // Fetch products for lookup
        var products = await _productsApiService.GetProductsAsync();
        ViewBag.Products = products.OrderBy(p => p.Title).ToList();

        return View(risk);
    }

    // GET: Risk/Create
    public async Task<IActionResult> Create(int? objectiveId)
    {
        ViewBag.Users = new SelectList(await _context.Users.OrderBy(u => u.Name).ToListAsync(), "Id", "Name");
        ViewBag.Objectives = new SelectList(await _context.Objectives.Where(o => !o.IsDeleted).OrderBy(o => o.Title).ToListAsync(), "Id", "Title", objectiveId);
        ViewBag.RiskTypes = await _context.RiskTypes.Where(rt => rt.IsActive).OrderBy(rt => rt.Name).ToListAsync();
        ViewBag.RiskTiers = new SelectList(await _context.RiskTiers.Where(rt => rt.IsActive).OrderBy(rt => rt.SortOrder).ToListAsync(), "Id", "Name");
        
        var products = await _productsApiService.GetProductsAsync(null);
        ViewBag.Products = products.OrderBy(p => p.Title).ToList();
        
        var businessAreas = await _productsApiService.GetBusinessAreasAsync();
        ViewBag.BusinessAreas = businessAreas;
        
        return View();
    }

    // POST: Risk/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("ObjectiveId,FipsId,Title,Description,BusinessArea,RiskTierId,OwnerEmail,ImpactRating,LikelihoodRating,ProximityDate,Response,ResidualImpact,ResidualLikelihood,TargetDate,Status,Notes")] Risk risk, int[] selectedRiskTypes)
    {
        _logger.LogInformation("=== CREATE RISK POST ===");
        _logger.LogInformation("OwnerEmail received: {OwnerEmail}", risk.OwnerEmail);
        _logger.LogInformation("OwnerEmail from Request.Form: {FormValue}", Request.Form["OwnerEmail"].ToString());
        _logger.LogInformation("Risk types parameter is null: {IsNull}", selectedRiskTypes == null);
        _logger.LogInformation("Risk types count: {Count}", selectedRiskTypes?.Length ?? 0);
        _logger.LogInformation("Risk types values: {RiskTypes}", selectedRiskTypes != null ? string.Join(", ", selectedRiskTypes) : "none");
        _logger.LogInformation("ModelState.IsValid: {IsValid}", ModelState.IsValid);
        
        if (!ModelState.IsValid)
        {
            foreach (var modelState in ModelState.Values)
            {
                foreach (var error in modelState.Errors)
                {
                    _logger.LogWarning("ModelState Error: {Error}", error.ErrorMessage);
                }
            }
        }
        
        if (ModelState.IsValid)
        {
            try
            {
                // Ensure nullable foreign keys are set to null if they're 0 or invalid
                if (risk.ObjectiveId == 0) risk.ObjectiveId = null;
                if (risk.RiskTierId == 0) risk.RiskTierId = null;
                if (string.IsNullOrWhiteSpace(risk.OwnerEmail)) risk.OwnerEmail = null;
                
                risk.RiskScore = risk.ImpactRating * risk.LikelihoodRating;
                risk.CreatedAt = DateTime.UtcNow;
                risk.UpdatedAt = DateTime.UtcNow;
                risk.IsDeleted = false;
                
                _context.Add(risk);
                await _context.SaveChangesAsync();

                // Add risk types
                if (selectedRiskTypes != null && selectedRiskTypes.Length > 0)
                {
                    _logger.LogInformation("Adding {Count} risk types to risk {RiskId}", selectedRiskTypes.Length, risk.Id);
                    foreach (var riskTypeId in selectedRiskTypes)
                    {
                        _logger.LogInformation("Adding risk type {RiskTypeId} to risk {RiskId}", riskTypeId, risk.Id);
                        _context.RiskRiskTypes.Add(new RiskRiskType
                        {
                            RiskId = risk.Id,
                            RiskTypeId = riskTypeId,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Risk types saved successfully");
                }
                else
                {
                    _logger.LogWarning("No risk types to add");
                }
                
                TempData["SuccessMessage"] = $"Risk '{risk.Title}' has been created successfully.";
                
                if (risk.ObjectiveId.HasValue)
                {
                    return RedirectToAction(nameof(Index), new { objectiveId = risk.ObjectiveId });
                }
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating risk");
                ModelState.AddModelError("", "An error occurred while creating the risk. Please try again.");
            }
        }
        
        ViewBag.Objectives = new SelectList(await _context.Objectives.Where(o => !o.IsDeleted).OrderBy(o => o.Title).ToListAsync(), "Id", "Title", risk.ObjectiveId);
        ViewBag.RiskTypes = await _context.RiskTypes.Where(rt => rt.IsActive).OrderBy(rt => rt.Name).ToListAsync();
        ViewBag.RiskTiers = new SelectList(await _context.RiskTiers.Where(rt => rt.IsActive).OrderBy(rt => rt.SortOrder).ToListAsync(), "Id", "Name");
        
        var products = await _productsApiService.GetProductsAsync(null);
        ViewBag.Products = products.OrderBy(p => p.Title).ToList();
        
        var businessAreas = await _productsApiService.GetBusinessAreasAsync();
        ViewBag.BusinessAreas = businessAreas;
        
        return View(risk);
    }

    // GET: Risk/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var risk = await _context.Risks
            .Include(r => r.RiskRiskTypes)
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);
        if (risk == null)
        {
            return NotFound();
        }
        ViewBag.Objectives = new SelectList(await _context.Objectives.Where(o => !o.IsDeleted).OrderBy(o => o.Title).ToListAsync(), "Id", "Title", risk.ObjectiveId);
        ViewBag.RiskTypes = await _context.RiskTypes.Where(rt => rt.IsActive).OrderBy(rt => rt.Name).ToListAsync();
        ViewBag.SelectedRiskTypeIds = risk.RiskRiskTypes.Select(rrt => rrt.RiskTypeId).ToList();
        ViewBag.RiskTiers = new SelectList(await _context.RiskTiers.Where(rt => rt.IsActive).OrderBy(rt => rt.SortOrder).ToListAsync(), "Id", "Name", risk.RiskTierId);
        
        var products = await _productsApiService.GetProductsAsync(null);
        ViewBag.Products = products.OrderBy(p => p.Title).ToList();
        
        var businessAreas = await _productsApiService.GetBusinessAreasAsync();
        ViewBag.BusinessAreas = businessAreas;
        
        return View(risk);
    }

    // POST: Risk/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,ObjectiveId,FipsId,Title,Description,BusinessArea,RiskTierId,OwnerEmail,ImpactRating,LikelihoodRating,ProximityDate,Response,ResidualImpact,ResidualLikelihood,TargetDate,Status,ClosedDate,Notes")] Risk risk, int[] selectedRiskTypes)
    {
        _logger.LogInformation("=== EDIT RISK POST ===");
        _logger.LogInformation("Risk ID: {RiskId}", id);
        _logger.LogInformation("OwnerEmail received: {OwnerEmail}", risk.OwnerEmail);
        _logger.LogInformation("OwnerEmail from Request.Form: {FormValue}", Request.Form["OwnerEmail"].ToString());
        _logger.LogInformation("Risk types parameter is null: {IsNull}", selectedRiskTypes == null);
        _logger.LogInformation("Risk types count: {Count}", selectedRiskTypes?.Length ?? 0);
        _logger.LogInformation("Risk types values: {RiskTypes}", selectedRiskTypes != null ? string.Join(", ", selectedRiskTypes) : "none");
        _logger.LogInformation("ModelState.IsValid: {IsValid}", ModelState.IsValid);
        
        if (id != risk.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            foreach (var modelState in ModelState.Values)
            {
                foreach (var error in modelState.Errors)
                {
                    _logger.LogWarning("ModelState Error: {Error}", error.ErrorMessage);
                }
            }
        }

        if (ModelState.IsValid)
        {
            try
            {
                var existingRisk = await _context.Risks.FindAsync(id);
                if (existingRisk == null || existingRisk.IsDeleted)
                {
                    return NotFound();
                }

                // Ensure nullable foreign keys are set to null if they're 0 or invalid
                if (risk.ObjectiveId == 0) risk.ObjectiveId = null;
                if (risk.RiskTierId == 0) risk.RiskTierId = null;
                if (string.IsNullOrWhiteSpace(risk.OwnerEmail)) risk.OwnerEmail = null;

                existingRisk.ObjectiveId = risk.ObjectiveId;
                existingRisk.FipsId = risk.FipsId;
                existingRisk.Title = risk.Title;
                existingRisk.Description = risk.Description;
                existingRisk.Category = risk.Category;
                existingRisk.BusinessArea = risk.BusinessArea;
                existingRisk.RiskTierId = risk.RiskTierId;
                existingRisk.OwnerEmail = risk.OwnerEmail;
                existingRisk.ImpactRating = risk.ImpactRating;
                existingRisk.LikelihoodRating = risk.LikelihoodRating;
                existingRisk.RiskScore = risk.ImpactRating * risk.LikelihoodRating;
                existingRisk.ProximityDate = risk.ProximityDate;
                existingRisk.Response = risk.Response;
                existingRisk.ResidualImpact = risk.ResidualImpact;
                existingRisk.ResidualLikelihood = risk.ResidualLikelihood;
                existingRisk.TargetDate = risk.TargetDate;
                existingRisk.Status = risk.Status;
                existingRisk.ClosedDate = risk.ClosedDate;
                existingRisk.Notes = risk.Notes;
                existingRisk.UpdatedAt = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();
                
                // Update risk types - remove old ones and add new ones
                var existingRiskTypes = _context.RiskRiskTypes.Where(rrt => rrt.RiskId == risk.Id);
                _context.RiskRiskTypes.RemoveRange(existingRiskTypes);
                
                if (selectedRiskTypes != null && selectedRiskTypes.Length > 0)
                {
                    _logger.LogInformation("Adding {Count} risk types to risk {RiskId}", selectedRiskTypes.Length, risk.Id);
                    foreach (var riskTypeId in selectedRiskTypes)
                    {
                        _logger.LogInformation("Adding risk type {RiskTypeId} to risk {RiskId}", riskTypeId, risk.Id);
                        _context.RiskRiskTypes.Add(new RiskRiskType
                        {
                            RiskId = risk.Id,
                            RiskTypeId = riskTypeId,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }
                else
                {
                    _logger.LogInformation("No risk types to add (clearing all risk types)");
                }
                
                await _context.SaveChangesAsync();
                _logger.LogInformation("Risk types updated successfully");
                
                TempData["SuccessMessage"] = $"Risk '{risk.Title}' has been updated successfully.";
                
                if (risk.ObjectiveId.HasValue)
                {
                    return RedirectToAction(nameof(Index), new { objectiveId = risk.ObjectiveId });
                }
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating risk");
                ModelState.AddModelError("", "An error occurred while updating the risk. Please try again.");
            }
        }
        
        ViewBag.Objectives = new SelectList(await _context.Objectives.Where(o => !o.IsDeleted).OrderBy(o => o.Title).ToListAsync(), "Id", "Title", risk.ObjectiveId);
        ViewBag.RiskTypes = await _context.RiskTypes.Where(rt => rt.IsActive).OrderBy(rt => rt.Name).ToListAsync();
        ViewBag.SelectedRiskTypeIds = risk.RiskRiskTypes?.Select(rrt => rrt.RiskTypeId).ToList() ?? new List<int>();
        ViewBag.RiskTiers = new SelectList(await _context.RiskTiers.Where(rt => rt.IsActive).OrderBy(rt => rt.SortOrder).ToListAsync(), "Id", "Name", risk.RiskTierId);
        
        var products = await _productsApiService.GetProductsAsync(null);
        ViewBag.Products = products.OrderBy(p => p.Title).ToList();
        
        var businessAreas = await _productsApiService.GetBusinessAreasAsync();
        ViewBag.BusinessAreas = businessAreas;
        
        return View(risk);
    }

    // GET: Risk/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var risk = await _context.Risks
            .Include(r => r.Objective)
            .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);
            
        if (risk == null)
        {
            return NotFound();
        }

        return View(risk);
    }

    // POST: Risk/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        try
        {
            var risk = await _context.Risks.FindAsync(id);
            if (risk != null && !risk.IsDeleted)
            {
                risk.IsDeleted = true;
                risk.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = $"Risk '{risk.Title}' has been deleted successfully.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting risk");
            TempData["ErrorMessage"] = "An error occurred while deleting the risk. Please try again.";
        }
        
        return RedirectToAction(nameof(Index));
    }
}

