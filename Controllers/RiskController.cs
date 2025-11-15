using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Compass.Data;
using Compass.Models;
using Compass.Services;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;

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
    public async Task<IActionResult> Create(int? objectiveId, int? milestoneId, string? returnTo)
    {
        var risk = new Risk
        {
            ObjectiveId = objectiveId,
            IdentifiedDate = DateTime.UtcNow.Date
        };

        ViewBag.Objectives = new SelectList(await _context.Objectives.Where(o => !o.IsDeleted).OrderBy(o => o.Title).ToListAsync(), "Id", "Title", objectiveId);
        ViewBag.RiskTypes = await _context.RiskTypes.Where(rt => rt.IsActive).OrderBy(rt => rt.Name).ToListAsync();
        ViewBag.RiskTiers = new SelectList(await _context.RiskTiers.Where(rt => rt.IsActive).OrderBy(rt => rt.SortOrder).ToListAsync(), "Id", "Name");
        ViewBag.SelectedRiskTypeIds = Array.Empty<int>();

        var products = await _productsApiService.GetProductsAsync(null);
        ViewBag.Products = products.OrderBy(p => p.Title).ToList();

        var businessAreas = await _productsApiService.GetBusinessAreasAsync();
        ViewBag.BusinessAreas = businessAreas;

        ViewBag.MilestoneId = milestoneId;
        ViewBag.ReturnTo = returnTo;

        if (milestoneId.HasValue)
        {
            var milestone = await _context.Milestones
                .Include(m => m.Project)
                .FirstOrDefaultAsync(m => m.Id == milestoneId.Value && !m.IsDeleted);

            if (milestone != null)
            {
                ViewBag.SourceMilestone = milestone;
                risk.ProjectId = milestone.ProjectId;
                risk.ObjectiveId ??= milestone.ObjectiveId;
            }
        }

        await PopulateRiskLookupsAsync();
        ViewBag.TagList = string.Empty;

        return View(risk);
    }

    // POST: Risk/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("ObjectiveId,FipsId,Title,Description,BusinessArea,RiskTierId,OwnerEmail,ImpactRating,LikelihoodRating,ProximityDate,Response,ResidualImpact,ResidualLikelihood,TargetDate,Status,Notes,RiskStatusId,RiskPriorityId,RiskLikelihoodId,RiskImpactLevelId,RiskProximityId,RiskCategoryId,IdentifiedDate,NextReviewDate,LastReviewDate,GovernanceBoardId,ResponseStrategy,Source,SourceId,PrimaryProductId")] Risk risk, int[] selectedRiskTypes, int? milestoneId, string? returnTo, string? tagList)
    {
        var tagValues = ParseTags(tagList);
        Milestone? milestoneContext = null;
        if (milestoneId.HasValue)
        {
            milestoneContext = await _context.Milestones.FirstOrDefaultAsync(m => m.Id == milestoneId.Value && !m.IsDeleted);
            if (milestoneContext != null)
            {
                risk.ProjectId ??= milestoneContext.ProjectId;
                risk.ObjectiveId ??= milestoneContext.ObjectiveId;
            }
        }

        if (risk.ObjectiveId == 0) risk.ObjectiveId = null;
        if (risk.RiskTierId == 0) risk.RiskTierId = null;
        if (risk.RiskStatusId == 0) risk.RiskStatusId = null;
        if (risk.RiskPriorityId == 0) risk.RiskPriorityId = null;
        if (risk.RiskLikelihoodId == 0) risk.RiskLikelihoodId = null;
        if (risk.RiskImpactLevelId == 0) risk.RiskImpactLevelId = null;
        if (risk.RiskProximityId == 0) risk.RiskProximityId = null;
        if (risk.RiskCategoryId == 0) risk.RiskCategoryId = null;
        if (string.IsNullOrWhiteSpace(risk.OwnerEmail)) risk.OwnerEmail = null;

        if (ModelState.IsValid)
        {
            try
            {
                risk.IdentifiedDate ??= DateTime.UtcNow.Date;
                var currentUserEmail = User.Identity?.Name;
                if (!string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == currentUserEmail.ToLower());
                    if (currentUser != null)
                    {
                        risk.CreatedByUserId = currentUser.Id;
                        risk.UpdatedByUserId = currentUser.Id;
                    }
                }

                if (!string.IsNullOrWhiteSpace(risk.OwnerEmail))
                {
                    var ownerUser = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == risk.OwnerEmail.ToLower());
                    risk.OwnerUserId = ownerUser?.Id;
                }
                else
                {
                    risk.OwnerUserId = null;
                }

                if (risk.RiskStatusId.HasValue)
                {
                    var statusLookup = await _context.RiskStatuses.FirstOrDefaultAsync(s => s.Id == risk.RiskStatusId.Value);
                    if (statusLookup != null)
                    {
                        risk.Status = statusLookup.Label;
                    }
                }

                risk.RiskScore = risk.ImpactRating * risk.LikelihoodRating;
                risk.InherentScore = risk.RiskScore;
                risk.ResidualScore = (risk.ResidualImpact ?? 0) * (risk.ResidualLikelihood ?? 0);
                risk.CreatedAt = DateTime.UtcNow;
                risk.UpdatedAt = DateTime.UtcNow;
                risk.IsDeleted = false;

                _context.Add(risk);
                await _context.SaveChangesAsync();

                if (selectedRiskTypes != null && selectedRiskTypes.Length > 0)
                {
                    foreach (var riskTypeId in selectedRiskTypes.Distinct())
                    {
                        _context.RiskRiskTypes.Add(new RiskRiskType
                        {
                            RiskId = risk.Id,
                            RiskTypeId = riskTypeId,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }

                if (milestoneContext != null)
                {
                    _context.MilestoneRisks.Add(new MilestoneRisk
                    {
                        MilestoneId = milestoneContext.Id,
                        RiskId = risk.Id
                    });
                }

                await ReplaceRiskTagsAsync(risk.Id, tagValues);

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Risk '{risk.Title}' has been created successfully.";

                if (milestoneContext != null)
                {
                    if (!string.IsNullOrWhiteSpace(returnTo) && returnTo.Equals("project-milestone", StringComparison.OrdinalIgnoreCase))
                    {
                        return RedirectToAction("MilestoneDetails", "Project", new { id = milestoneContext.Id });
                    }

                    return RedirectToAction("Details", "Milestone", new { id = milestoneContext.Id });
                }

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
        ViewBag.RiskTiers = new SelectList(await _context.RiskTiers.Where(rt => rt.IsActive).OrderBy(rt => rt.SortOrder).ToListAsync(), "Id", "Name", risk.RiskTierId);
        ViewBag.SelectedRiskTypeIds = selectedRiskTypes?.ToList() ?? new List<int>();

        var products = await _productsApiService.GetProductsAsync(null);
        ViewBag.Products = products.OrderBy(p => p.Title).ToList();

        var businessAreas = await _productsApiService.GetBusinessAreasAsync();
        ViewBag.BusinessAreas = businessAreas;

        ViewBag.MilestoneId = milestoneId;
        ViewBag.ReturnTo = returnTo;
        if (milestoneContext != null)
        {
            ViewBag.SourceMilestone = milestoneContext;
        }

        await PopulateRiskLookupsAsync();
        ViewBag.TagList = tagList ?? string.Empty;

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
            .Include(r => r.Tags)
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

        await PopulateRiskLookupsAsync();
        ViewBag.TagList = string.Join(", ", risk.Tags.Select(t => t.Value));

        return View(risk);
    }

    // POST: Risk/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,ObjectiveId,FipsId,Title,Description,BusinessArea,RiskTierId,OwnerEmail,ImpactRating,LikelihoodRating,ProximityDate,Response,ResidualImpact,ResidualLikelihood,TargetDate,Status,ClosedDate,Notes,RiskStatusId,RiskPriorityId,RiskLikelihoodId,RiskImpactLevelId,RiskProximityId,RiskCategoryId,IdentifiedDate,NextReviewDate,LastReviewDate,GovernanceBoardId,ResponseStrategy,Source,SourceId,PrimaryProductId")] Risk risk, int[] selectedRiskTypes, string? tagList)
    {
        if (id != risk.Id)
        {
            return NotFound();
        }

        var tagValues = ParseTags(tagList);

        var existingRisk = await _context.Risks
            .Include(r => r.RiskRiskTypes)
            .Include(r => r.Tags)
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);

        if (existingRisk == null)
        {
            return NotFound();
        }

        risk.ObjectiveId = risk.ObjectiveId == 0 ? null : risk.ObjectiveId;
        risk.RiskTierId = risk.RiskTierId == 0 ? null : risk.RiskTierId;
        risk.RiskStatusId = risk.RiskStatusId == 0 ? null : risk.RiskStatusId;
        risk.RiskPriorityId = risk.RiskPriorityId == 0 ? null : risk.RiskPriorityId;
        risk.RiskLikelihoodId = risk.RiskLikelihoodId == 0 ? null : risk.RiskLikelihoodId;
        risk.RiskImpactLevelId = risk.RiskImpactLevelId == 0 ? null : risk.RiskImpactLevelId;
        risk.RiskProximityId = risk.RiskProximityId == 0 ? null : risk.RiskProximityId;
        risk.RiskCategoryId = risk.RiskCategoryId == 0 ? null : risk.RiskCategoryId;
        if (string.IsNullOrWhiteSpace(risk.OwnerEmail)) risk.OwnerEmail = null;

        if (ModelState.IsValid)
        {
            try
            {
                existingRisk.ObjectiveId = risk.ObjectiveId;
                existingRisk.FipsId = risk.FipsId;
                existingRisk.Title = risk.Title;
                existingRisk.Description = risk.Description;
                existingRisk.Category = risk.Category;
                existingRisk.BusinessArea = risk.BusinessArea;
                existingRisk.RiskTierId = risk.RiskTierId;
                existingRisk.OwnerEmail = risk.OwnerEmail;
                existingRisk.RiskStatusId = risk.RiskStatusId;
                existingRisk.RiskPriorityId = risk.RiskPriorityId;
                existingRisk.RiskLikelihoodId = risk.RiskLikelihoodId;
                existingRisk.RiskImpactLevelId = risk.RiskImpactLevelId;
                existingRisk.RiskProximityId = risk.RiskProximityId;
                existingRisk.RiskCategoryId = risk.RiskCategoryId;
                existingRisk.IdentifiedDate = risk.IdentifiedDate ?? existingRisk.IdentifiedDate;
                existingRisk.NextReviewDate = risk.NextReviewDate;
                existingRisk.LastReviewDate = risk.LastReviewDate;
                existingRisk.GovernanceBoardId = risk.GovernanceBoardId;
                existingRisk.ResponseStrategy = string.IsNullOrWhiteSpace(risk.ResponseStrategy) ? null : risk.ResponseStrategy.Trim();
                existingRisk.Source = string.IsNullOrWhiteSpace(risk.Source) ? null : risk.Source.Trim();
                existingRisk.SourceId = string.IsNullOrWhiteSpace(risk.SourceId) ? null : risk.SourceId.Trim();
                existingRisk.PrimaryProductId = risk.PrimaryProductId;

                if (!string.IsNullOrWhiteSpace(risk.OwnerEmail))
                {
                    var ownerUser = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == risk.OwnerEmail.ToLower());
                    existingRisk.OwnerUserId = ownerUser?.Id;
                }
                else
                {
                    existingRisk.OwnerUserId = null;
                }

                if (risk.RiskStatusId.HasValue)
                {
                    var statusLookup = await _context.RiskStatuses.FirstOrDefaultAsync(s => s.Id == risk.RiskStatusId.Value);
                    if (statusLookup != null)
                    {
                        existingRisk.Status = statusLookup.Label;
                    }
                }
                else if (!string.IsNullOrWhiteSpace(risk.Status))
                {
                    existingRisk.Status = risk.Status;
                }

                existingRisk.ImpactRating = risk.ImpactRating;
                existingRisk.LikelihoodRating = risk.LikelihoodRating;
                existingRisk.RiskScore = risk.ImpactRating * risk.LikelihoodRating;
                existingRisk.InherentScore = existingRisk.RiskScore;
                existingRisk.ResidualImpact = risk.ResidualImpact;
                existingRisk.ResidualLikelihood = risk.ResidualLikelihood;
                existingRisk.ResidualScore = (risk.ResidualImpact ?? 0) * (risk.ResidualLikelihood ?? 0);
                existingRisk.ProximityDate = risk.ProximityDate;
                existingRisk.Response = risk.Response;
                existingRisk.TargetDate = risk.TargetDate;
                existingRisk.ClosedDate = risk.ClosedDate;
                existingRisk.Notes = risk.Notes;

                var currentUserEmail = User.Identity?.Name;
                if (!string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == currentUserEmail.ToLower());
                    if (currentUser != null)
                    {
                        existingRisk.UpdatedByUserId = currentUser.Id;
                    }
                }

                existingRisk.UpdatedAt = DateTime.UtcNow;

                var existingRiskTypes = _context.RiskRiskTypes.Where(rrt => rrt.RiskId == existingRisk.Id);
                _context.RiskRiskTypes.RemoveRange(existingRiskTypes);

                if (selectedRiskTypes != null && selectedRiskTypes.Length > 0)
                {
                    foreach (var riskTypeId in selectedRiskTypes.Distinct())
                    {
                        _context.RiskRiskTypes.Add(new RiskRiskType
                        {
                            RiskId = existingRisk.Id,
                            RiskTypeId = riskTypeId,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }

                await ReplaceRiskTagsAsync(existingRisk.Id, tagValues);

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Risk '{existingRisk.Title}' has been updated successfully.";

                if (existingRisk.ObjectiveId.HasValue)
                {
                    return RedirectToAction(nameof(Index), new { objectiveId = existingRisk.ObjectiveId });
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
        ViewBag.SelectedRiskTypeIds = selectedRiskTypes?.ToList() ?? new List<int>();
        ViewBag.RiskTiers = new SelectList(await _context.RiskTiers.Where(rt => rt.IsActive).OrderBy(rt => rt.SortOrder).ToListAsync(), "Id", "Name", risk.RiskTierId);

        var products = await _productsApiService.GetProductsAsync(null);
        ViewBag.Products = products.OrderBy(p => p.Title).ToList();

        var businessAreas = await _productsApiService.GetBusinessAreasAsync();
        ViewBag.BusinessAreas = businessAreas;

        await PopulateRiskLookupsAsync();
        ViewBag.TagList = tagList ?? string.Join(", ", existingRisk?.Tags.Select(t => t.Value) ?? Array.Empty<string>());

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

    private async Task PopulateRiskLookupsAsync()
    {
        ViewBag.RiskStatuses = new SelectList(
            await _context.RiskStatuses.Where(s => s.IsActive).OrderBy(s => s.SortOrder).ToListAsync(),
            "Id",
            "Label");

        ViewBag.RiskPriorities = new SelectList(
            await _context.RiskPriorities.Where(p => p.IsActive).OrderBy(p => p.SortOrder).ToListAsync(),
            "Id",
            "Label");

        ViewBag.RiskLikelihoods = new SelectList(
            await _context.RiskLikelihoods.Where(l => l.IsActive).OrderBy(l => l.SortOrder).ToListAsync(),
            "Id",
            "Label");

        ViewBag.RiskImpactLevels = new SelectList(
            await _context.RiskImpactLevels.Where(i => i.IsActive).OrderBy(i => i.SortOrder).ToListAsync(),
            "Id",
            "Label");

        ViewBag.RiskProximities = new SelectList(
            await _context.RiskProximities.Where(p => p.IsActive).OrderBy(p => p.SortOrder).ToListAsync(),
            "Id",
            "Label");

        ViewBag.RiskCategories = new SelectList(
            await _context.RiskCategories.Where(c => c.IsActive).OrderBy(c => c.SortOrder).ToListAsync(),
            "Id",
            "Label");

        ViewBag.GovernanceBoards = new SelectList(
            await _context.GovernanceBoards.Where(g => g.IsActive).OrderBy(g => g.SortOrder).ToListAsync(),
            "Id",
            "Label");
    }

    private static IReadOnlyList<string> ParseTags(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return Array.Empty<string>();
        }

        return raw.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(tag => tag.Trim())
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private async Task ReplaceRiskTagsAsync(int riskId, IReadOnlyCollection<string> tagValues)
    {
        var existing = await _context.RiskTags
            .Where(t => t.RiskId == riskId)
            .ToListAsync();

        if (existing.Any())
        {
            _context.RiskTags.RemoveRange(existing);
        }

        if (tagValues.Count == 0)
        {
            return;
        }

        foreach (var value in tagValues)
        {
            _context.RiskTags.Add(new RiskTag
            {
                RiskId = riskId,
                Value = value
            });
        }
    }

}
