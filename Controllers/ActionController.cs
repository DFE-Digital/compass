using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Compass.Data;
using Compass.Models;
using Compass.Services;
using Microsoft.AspNetCore.Authorization;
using Compass.ViewModels;
using System;
using System.Globalization;
using System.Linq;

namespace Compass.Controllers;

[Authorize]
public class ActionController : Controller
{
    private readonly CompassDbContext _context;
    private readonly IProductsApiService _productsApiService;
    private readonly ILogger<ActionController> _logger;

    private static readonly (string Value, string Label)[] ActionStatusOptions = new[]
    {
        ("not_started", "Not started"),
        ("in_progress", "In progress"),
        ("blocked", "Blocked"),
        ("done", "Done"),
        ("cancelled", "Cancelled")
    };

    private static readonly (string Value, string Label)[] ActionPriorityOptions = new[]
    {
        ("high", "High"),
        ("medium", "Medium"),
        ("low", "Low")
    };

    private static readonly string[] ActionSourceTypes =
    {
        "Risk",
        "Issue",
        "Milestone",
        "Decision",
        "Product",
        "Other"
    };

    public ActionController(CompassDbContext context, IProductsApiService productsApiService, ILogger<ActionController> logger)
    {
        _context = context;
        _productsApiService = productsApiService;
        _logger = logger;
    }

    // GET: Action
    public async Task<IActionResult> Index(
        int? objectiveId, 
        int? riskId, 
        int? issueId, 
        int? milestoneId,
        string? dimension,
        string? businessArea,
        string? status,
        string[]? products,
        int? actionSourceId,
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

        var query = _context.Actions
            .Include(a => a.Objective)
            .Include(a => a.ParentAction)
            .Include(a => a.ActionSource)
            .Where(a => !a.IsDeleted);

        if (objectiveId.HasValue)
        {
            query = query.Where(a => a.ObjectiveId == objectiveId);
            ViewBag.ObjectiveId = objectiveId;
            var objective = await _context.Objectives.FindAsync(objectiveId);
            ViewBag.ObjectiveTitle = objective?.Title;
        }

        if (riskId.HasValue)
        {
            query = query.Where(a => a.RiskActions.Any(ra => ra.RiskId == riskId));
            ViewBag.RiskId = riskId;
            var risk = await _context.Risks.FindAsync(riskId);
            ViewBag.RiskTitle = risk?.Title;
        }

        if (issueId.HasValue)
        {
            query = query.Where(a => a.IssueActions.Any(ia => ia.IssueId == issueId));
            ViewBag.IssueId = issueId;
            var issue = await _context.Issues.FindAsync(issueId);
            ViewBag.IssueTitle = issue?.Title;
        }

        if (milestoneId.HasValue)
        {
            query = query.Where(a => a.MilestoneActions.Any(ma => ma.MilestoneId == milestoneId));
            ViewBag.MilestoneId = milestoneId;
            var milestone = await _context.Milestones.FindAsync(milestoneId);
            ViewBag.MilestoneName = milestone?.Name;
        }

        // Apply tab filter
        if (tab == "active")
        {
            query = query.Where(a => a.Status != "done" && a.Status != "cancelled");
        }
        else if (tab == "closed")
        {
            query = query.Where(a => a.Status == "done" || a.Status == "cancelled");
        }

        // Apply view scope filter
        if (viewScope == "assigned_to_me" && currentUser != null)
        {
            query = query.Where(a => a.AssignedToEmail != null && a.AssignedToEmail.ToLower() == currentUser.Email.ToLower());
        }
        else if (viewScope == "my_products")
        {
            query = query.Where(a => !string.IsNullOrEmpty(a.FipsId));
        }
        // organisation_wide shows all items (no additional filter)

        // Apply dimension filters (for backwards compatibility and additional filtering)
        if (dimension == "assigned_to_me" && currentUser != null)
        {
            query = query.Where(a => a.AssignedToEmail != null && a.AssignedToEmail.ToLower() == currentUser.Email.ToLower());
        }
        else if (dimension == "my_business_area" && userBusinessAreas.Any())
        {
            query = query.Where(a => a.BusinessArea != null && userBusinessAreas.Contains(a.BusinessArea));
        }

        // Apply standard filters
        if (!string.IsNullOrEmpty(businessArea))
        {
            query = query.Where(a => a.BusinessArea == businessArea);
        }

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(a => a.Status == status);
        }

        if (products != null && products.Any())
        {
            query = query.Where(a => a.FipsId != null && products.Contains(a.FipsId));
        }

        // Apply action-specific filters
        if (actionSourceId.HasValue)
        {
            query = query.Where(a => a.ActionSourceId == actionSourceId.Value);
        }

        var actions = await query
            .OrderByDescending(a => a.Priority)
            .ThenBy(a => a.DueDate)
            .ToListAsync();

        // Get items assigned to current user (separate from filtered results)
        var myActions = new List<Models.Action>();
        if (currentUser != null)
        {
            myActions = await _context.Actions
                .Include(a => a.Objective)
                .Include(a => a.ActionSource)
                .Where(a => !a.IsDeleted && a.AssignedToEmail != null && a.AssignedToEmail.ToLower() == currentUser.Email.ToLower() && a.Status != "done" && a.Status != "cancelled")
                .OrderByDescending(a => a.Priority)
                .ThenBy(a => a.DueDate)
                .Take(10) // Limit to 10 most important
                .ToListAsync();
        }
        ViewBag.MyActions = myActions;

        // Calculate summary counts
        var allActionsQuery = _context.Actions.Where(a => !a.IsDeleted);
        if (objectiveId.HasValue)
        {
            allActionsQuery = allActionsQuery.Where(a => a.ObjectiveId == objectiveId);
        }
        if (riskId.HasValue)
        {
            allActionsQuery = allActionsQuery.Where(a => a.RiskActions.Any(ra => ra.RiskId == riskId));
        }
        if (issueId.HasValue)
        {
            allActionsQuery = allActionsQuery.Where(a => a.IssueActions.Any(ia => ia.IssueId == issueId));
        }
        if (milestoneId.HasValue)
        {
            allActionsQuery = allActionsQuery.Where(a => a.MilestoneActions.Any(ma => ma.MilestoneId == milestoneId));
        }
        
        ViewBag.OpenCount = await allActionsQuery.CountAsync(a => a.Status != "done" && a.Status != "cancelled");
        ViewBag.ClosedCount = await allActionsQuery.CountAsync(a => a.Status == "done" || a.Status == "cancelled");
        ViewBag.OverdueCount = await allActionsQuery.CountAsync(a => 
            a.Status != "done" && 
            a.Status != "cancelled" && 
            a.DueDate.HasValue && 
            a.DueDate.Value < DateTime.Now);
        
        // Calculate view dimension counts (active items only)
        var activeActionsQuery = allActionsQuery.Where(a => a.Status != "done" && a.Status != "cancelled");
        ViewBag.AssignedToMeCount = currentUser != null 
            ? await activeActionsQuery.CountAsync(a => a.AssignedToEmail != null && a.AssignedToEmail.ToLower() == currentUser.Email.ToLower())
            : 0;
        ViewBag.MyProductsCount = await activeActionsQuery.CountAsync(a => !string.IsNullOrEmpty(a.FipsId));
        ViewBag.OrganisationWideCount = await activeActionsQuery.CountAsync();

        // Pass filter values to view
        ViewBag.CurrentTab = tab;
        ViewBag.CurrentViewScope = viewScope;
        ViewBag.CurrentDimension = dimension;
        ViewBag.CurrentBusinessArea = businessArea;
        ViewBag.CurrentStatus = status;
        ViewBag.CurrentProducts = products ?? Array.Empty<string>();
        ViewBag.CurrentActionSourceId = actionSourceId;
        
        // Get data for filter dropdowns
        var businessAreas = await _productsApiService.GetBusinessAreasAsync();
        ViewBag.BusinessAreas = businessAreas;
        
        // Get distinct products (FipsId) that are actually used in actions
        var fipsIdsInUse = await _context.Actions
            .Where(a => !a.IsDeleted && !string.IsNullOrEmpty(a.FipsId))
            .Select(a => a.FipsId)
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
        
        var actionSources = await _context.ActionSources.Where(a_s => a_s.IsActive).OrderBy(a_s => a_s.SortOrder).ToListAsync();
        ViewBag.ActionSources = actionSources;
        
        ViewBag.UserBusinessAreas = userBusinessAreas;
        
        return View(actions);
    }

    // GET: Action/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var action = await LoadActionForDetailsAsync(id.Value);
        if (action == null)
        {
            return NotFound();
        }

        var viewModel = await BuildActionDetailsViewModelAsync(action);
        return View(viewModel);
    }

    // GET: Action/Create
    public async Task<IActionResult> Create(int? objectiveId, int? parentActionId, int? riskId, int? issueId, int? milestoneId, string? returnTo)
    {
        ViewBag.Objectives = new SelectList(await _context.Objectives.Where(o => !o.IsDeleted).OrderBy(o => o.Title).ToListAsync(), "Id", "Title", objectiveId);
        ViewBag.ParentActions = new SelectList(await _context.Actions.Where(a => !a.IsDeleted).OrderBy(a => a.Title).ToListAsync(), "Id", "Title", parentActionId);
        ViewBag.ActionSources = new SelectList(await _context.ActionSources.Where(a_s => a_s.IsActive).OrderBy(a_s => a_s.SortOrder).ToListAsync(), "Id", "Name");
        
        var products = await _productsApiService.GetProductsAsync(null);
        ViewBag.Products = products.OrderBy(p => p.Title).ToList();
        
        var businessAreas = await _productsApiService.GetBusinessAreasAsync();
        ViewBag.BusinessAreas = businessAreas;
        
        // Handle creating action from risk, issue, or milestone
        ViewBag.RiskId = riskId;
        ViewBag.IssueId = issueId;
        ViewBag.MilestoneId = milestoneId;
        ViewBag.ReturnTo = returnTo;
        
        // Pre-populate action fields from source risk or issue
        var action = new Models.Action();
        
        if (riskId.HasValue)
        {
            var risk = await _context.Risks.FindAsync(riskId.Value);
            if (risk != null)
            {
                ViewBag.SourceRisk = risk;
                ViewBag.SourceType = "Risk";
                ViewBag.SourceTitle = risk.Title;
                
                // Pre-populate fields from risk
                action.FipsId = risk.FipsId;
                action.BusinessArea = risk.BusinessArea;
                action.ObjectiveId = risk.ObjectiveId;
                
                // Set action source to "Risk" if it exists
                var riskActionSource = await _context.ActionSources
                    .FirstOrDefaultAsync(a => a.Name.ToLower() == "risk");
                if (riskActionSource != null)
                {
                    action.ActionSourceId = riskActionSource.Id;
                }
            }
        }
        
        if (issueId.HasValue)
        {
            var issue = await _context.Issues.FindAsync(issueId.Value);
            if (issue != null)
            {
                ViewBag.SourceIssue = issue;
                ViewBag.SourceType = "Issue";
                ViewBag.SourceTitle = issue.Title;
                
                // Pre-populate fields from issue
                action.FipsId = issue.FipsId;
                action.BusinessArea = issue.BusinessArea;
                action.ObjectiveId = issue.ObjectiveId;
                
                // Set action source to "Issue" if it exists
                var issueActionSource = await _context.ActionSources
                    .FirstOrDefaultAsync(a => a.Name.ToLower() == "issue");
                if (issueActionSource != null)
                {
                    action.ActionSourceId = issueActionSource.Id;
                }
            }
        }
        
        if (milestoneId.HasValue)
        {
            var milestone = await _context.Milestones.FindAsync(milestoneId.Value);
            if (milestone != null)
            {
                ViewBag.SourceMilestone = milestone;
                ViewBag.SourceType = "Milestone";
                ViewBag.SourceTitle = milestone.Name;
                
                // Pre-populate fields from milestone
                action.FipsId = milestone.FipsId;
                action.BusinessArea = milestone.BusinessArea;
                action.ObjectiveId = milestone.ObjectiveId;
                
                // Set action source to "Milestone" if it exists
                var milestoneActionSource = await _context.ActionSources
                    .FirstOrDefaultAsync(a => a.Name.ToLower() == "milestone");
                if (milestoneActionSource != null)
                {
                    action.ActionSourceId = milestoneActionSource.Id;
                }
            }
        }
        
        return View(action);
    }

    // POST: Action/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int? riskId, int? issueId, int? milestoneId, string? returnTo)
    {
        // Manually create and populate the action from form data due to model binding conflict with "Action" class name
        var action = new Models.Action
        {
            Title = Request.Form["Title"].ToString(),
            Description = Request.Form["Description"].ToString(),
            Status = Request.Form["Status"].ToString(),
            FipsId = string.IsNullOrWhiteSpace(Request.Form["FipsId"]) ? null : Request.Form["FipsId"].ToString(),
            BusinessArea = string.IsNullOrWhiteSpace(Request.Form["BusinessArea"]) ? null : Request.Form["BusinessArea"].ToString(),
            Priority = string.IsNullOrWhiteSpace(Request.Form["Priority"]) ? null : Request.Form["Priority"].ToString(),
            Notes = string.IsNullOrWhiteSpace(Request.Form["Notes"]) ? null : Request.Form["Notes"].ToString(),
            EvidenceUrl = string.IsNullOrWhiteSpace(Request.Form["EvidenceUrl"]) ? null : Request.Form["EvidenceUrl"].ToString()
        };
        
        // Parse nullable int fields
        if (int.TryParse(Request.Form["ObjectiveId"], out int objId) && objId > 0)
            action.ObjectiveId = objId;
        if (int.TryParse(Request.Form["ActionSourceId"], out int sourceId) && sourceId > 0)
            action.ActionSourceId = sourceId;
        
        // Handle assigned email
        var assignedToEmailValue = Request.Form["AssignedToEmail"].FirstOrDefault() 
                                    ?? Request.Form["action.AssignedToEmail"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(assignedToEmailValue))
            action.AssignedToEmail = assignedToEmailValue;
            
        if (int.TryParse(Request.Form["ParentActionId"], out int parentId) && parentId > 0)
            action.ParentActionId = parentId;
        
        // Parse dates
        if (DateTime.TryParse(Request.Form["StartDate"], out DateTime startDate))
            action.StartDate = startDate;
        if (DateTime.TryParse(Request.Form["DueDate"], out DateTime dueDate))
            action.DueDate = dueDate;
        if (DateTime.TryParse(Request.Form["CompletedDate"], out DateTime completedDate))
            action.CompletedDate = completedDate;
        
        // Manually validate required fields
        if (string.IsNullOrWhiteSpace(action.Title))
        {
            ModelState.AddModelError("Title", "The Title field is required.");
        }
        if (string.IsNullOrWhiteSpace(action.Status))
        {
            action.Status = "not_started";
        }
        
        if (ModelState.IsValid)
        {
            try
            {
                action.CreatedAt = DateTime.UtcNow;
                action.UpdatedAt = DateTime.UtcNow;
                action.IsDeleted = false;
                
                _context.Add(action);
                await _context.SaveChangesAsync();
                
                // Create relationships if action was created from risk, issue, or milestone
                if (riskId.HasValue)
                {
                    var riskAction = new RiskAction
                    {
                        RiskId = riskId.Value,
                        ActionId = action.Id
                    };
                    _context.Add(riskAction);
                }
                
                if (issueId.HasValue)
                {
                    var issueAction = new IssueAction
                    {
                        IssueId = issueId.Value,
                        ActionId = action.Id
                    };
                    _context.Add(issueAction);
                }
                
                if (milestoneId.HasValue)
                {
                    var milestoneAction = new MilestoneAction
                    {
                        MilestoneId = milestoneId.Value,
                        ActionId = action.Id
                    };
                    _context.Add(milestoneAction);
                }
                
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = $"Action '{action.Title}' has been created successfully.";
                
                // Redirect back to the source page
                if (riskId.HasValue)
                {
                    return RedirectToAction("Details", "Risk", new { id = riskId.Value });
                }
                if (issueId.HasValue)
                {
                    return RedirectToAction("Details", "Issue", new { id = issueId.Value });
                }
                if (milestoneId.HasValue)
                {
                    if (!string.IsNullOrWhiteSpace(returnTo) && returnTo.Equals("project-milestone", StringComparison.OrdinalIgnoreCase))
                    {
                        return RedirectToAction("MilestoneDetails", "Project", new { id = milestoneId.Value });
                    }

                    return RedirectToAction("Details", "Milestone", new { id = milestoneId.Value });
                }
                if (action.ObjectiveId.HasValue)
                {
                    return RedirectToAction(nameof(Index), new { objectiveId = action.ObjectiveId });
                }
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating action");
                ModelState.AddModelError("", "An error occurred while creating the action. Please try again.");
            }
        }
        
        ViewBag.Objectives = new SelectList(await _context.Objectives.Where(o => !o.IsDeleted).OrderBy(o => o.Title).ToListAsync(), "Id", "Title", action.ObjectiveId);
        ViewBag.ParentActions = new SelectList(await _context.Actions.Where(a => !a.IsDeleted).OrderBy(a => a.Title).ToListAsync(), "Id", "Title", action.ParentActionId);
        ViewBag.ActionSources = new SelectList(await _context.ActionSources.Where(a_s => a_s.IsActive).OrderBy(a_s => a_s.SortOrder).ToListAsync(), "Id", "Name");
        
        var products = await _productsApiService.GetProductsAsync(null);
        ViewBag.Products = products.OrderBy(p => p.Title).ToList();
        
        var businessAreas = await _productsApiService.GetBusinessAreasAsync();
        ViewBag.BusinessAreas = businessAreas;
        
        // Restore riskId, issueId, and milestoneId to ViewBag so they're passed back to the form
        ViewBag.RiskId = riskId;
        ViewBag.IssueId = issueId;
        ViewBag.MilestoneId = milestoneId;
        
        // Re-populate source information if applicable
        if (riskId.HasValue)
        {
            var risk = await _context.Risks.FindAsync(riskId.Value);
            if (risk != null)
            {
                ViewBag.SourceRisk = risk;
                ViewBag.SourceType = "Risk";
                ViewBag.SourceTitle = risk.Title;
            }
        }
        
        if (issueId.HasValue)
        {
            var issue = await _context.Issues.FindAsync(issueId.Value);
            if (issue != null)
            {
                ViewBag.SourceIssue = issue;
                ViewBag.SourceType = "Issue";
                ViewBag.SourceTitle = issue.Title;
            }
        }
        
        if (milestoneId.HasValue)
        {
            var milestone = await _context.Milestones.FindAsync(milestoneId.Value);
            if (milestone != null)
            {
                ViewBag.SourceMilestone = milestone;
                ViewBag.SourceType = "Milestone";
                ViewBag.SourceTitle = milestone.Name;
            }
        }
        
        return View(action);
    }

    // GET: Action/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var action = await _context.Actions
            .Include(a => a.RiskActions)
                .ThenInclude(ra => ra.Risk)
            .Include(a => a.IssueActions)
                .ThenInclude(ia => ia.Issue)
            .Include(a => a.MilestoneActions)
                .ThenInclude(ma => ma.Milestone)
            .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);
            
        if (action == null)
        {
            return NotFound();
        }

        ViewBag.Objectives = new SelectList(await _context.Objectives.Where(o => !o.IsDeleted).OrderBy(o => o.Title).ToListAsync(), "Id", "Title", action.ObjectiveId);
        ViewBag.ParentActions = new SelectList(await _context.Actions.Where(a => !a.IsDeleted && a.Id != id).OrderBy(a => a.Title).ToListAsync(), "Id", "Title", action.ParentActionId);
        ViewBag.ActionSources = new SelectList(await _context.ActionSources.Where(a_s => a_s.IsActive).OrderBy(a_s => a_s.SortOrder).ToListAsync(), "Id", "Name", action.ActionSourceId);
        
        var products = await _productsApiService.GetProductsAsync(null);
        ViewBag.Products = products.OrderBy(p => p.Title).ToList();
        
        var businessAreas = await _productsApiService.GetBusinessAreasAsync();
        ViewBag.BusinessAreas = businessAreas;
        
        // Get related items information
        var relatedRisks = action.RiskActions.Select(ra => ra.Risk).Where(r => r != null).ToList();
        var relatedIssues = action.IssueActions.Select(ia => ia.Issue).Where(i => i != null).ToList();
        var relatedMilestones = action.MilestoneActions.Select(ma => ma.Milestone).Where(m => m != null).ToList();
        
        ViewBag.RelatedRisks = relatedRisks;
        ViewBag.RelatedIssues = relatedIssues;
        ViewBag.RelatedMilestones = relatedMilestones;
        
        return View(action);
    }

    // POST: Action/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id)
    {
        var existingAction = await _context.Actions.FindAsync(id);
        if (existingAction == null || existingAction.IsDeleted)
        {
            return NotFound();
        }

        // Manually populate from form data due to model binding conflict with "Action" class name
        existingAction.Title = Request.Form["Title"].ToString();
        existingAction.Description = Request.Form["Description"].ToString();
        existingAction.Status = Request.Form["Status"].ToString();
        existingAction.FipsId = string.IsNullOrWhiteSpace(Request.Form["FipsId"]) ? null : Request.Form["FipsId"].ToString();
        existingAction.BusinessArea = string.IsNullOrWhiteSpace(Request.Form["BusinessArea"]) ? null : Request.Form["BusinessArea"].ToString();
        existingAction.Priority = string.IsNullOrWhiteSpace(Request.Form["Priority"]) ? null : Request.Form["Priority"].ToString();
        existingAction.Notes = string.IsNullOrWhiteSpace(Request.Form["Notes"]) ? null : Request.Form["Notes"].ToString();
        existingAction.EvidenceUrl = string.IsNullOrWhiteSpace(Request.Form["EvidenceUrl"]) ? null : Request.Form["EvidenceUrl"].ToString();
        
        // Parse nullable int fields
        if (int.TryParse(Request.Form["ObjectiveId"], out int objId) && objId > 0)
            existingAction.ObjectiveId = objId;
        else
            existingAction.ObjectiveId = null;
            
        if (int.TryParse(Request.Form["ActionSourceId"], out int sourceId) && sourceId > 0)
            existingAction.ActionSourceId = sourceId;
        else
            existingAction.ActionSourceId = null;
        
        // Handle assigned email
        var assignedToEmailValue = Request.Form["AssignedToEmail"].FirstOrDefault() 
                                    ?? Request.Form["action.AssignedToEmail"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(assignedToEmailValue))
            existingAction.AssignedToEmail = assignedToEmailValue;
        else
            existingAction.AssignedToEmail = null;
            
        if (int.TryParse(Request.Form["ParentActionId"], out int parentId) && parentId > 0)
            existingAction.ParentActionId = parentId;
        else
            existingAction.ParentActionId = null;
        
        // Parse dates
        if (DateTime.TryParse(Request.Form["StartDate"], out DateTime startDate))
            existingAction.StartDate = startDate;
        else
            existingAction.StartDate = null;
            
        if (DateTime.TryParse(Request.Form["DueDate"], out DateTime dueDate))
            existingAction.DueDate = dueDate;
        else
            existingAction.DueDate = null;
            
        if (DateTime.TryParse(Request.Form["CompletedDate"], out DateTime completedDate))
            existingAction.CompletedDate = completedDate;
        else
            existingAction.CompletedDate = null;
        
        // Validate required fields
        if (string.IsNullOrWhiteSpace(existingAction.Title))
        {
            ModelState.AddModelError("Title", "The Title field is required.");
        }
        if (string.IsNullOrWhiteSpace(existingAction.Status))
        {
            existingAction.Status = "not_started";
        }

        if (ModelState.IsValid)
        {
            try
            {
                existingAction.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = $"Action '{existingAction.Title}' has been updated successfully.";
                return RedirectToAction(nameof(Details), new { id = existingAction.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating action");
                ModelState.AddModelError("", "An error occurred while updating the action. Please try again.");
            }
        }
        
        // Reload data for form if validation failed
        ViewBag.Objectives = new SelectList(await _context.Objectives.Where(o => !o.IsDeleted).OrderBy(o => o.Title).ToListAsync(), "Id", "Title", existingAction.ObjectiveId);
        ViewBag.ParentActions = new SelectList(await _context.Actions.Where(a => !a.IsDeleted && a.Id != id).OrderBy(a => a.Title).ToListAsync(), "Id", "Title", existingAction.ParentActionId);
        ViewBag.ActionSources = new SelectList(await _context.ActionSources.Where(a_s => a_s.IsActive).OrderBy(a_s => a_s.SortOrder).ToListAsync(), "Id", "Name", existingAction.ActionSourceId);
        
        var products = await _productsApiService.GetProductsAsync(null);
        ViewBag.Products = products.OrderBy(p => p.Title).ToList();
        
        var businessAreas = await _productsApiService.GetBusinessAreasAsync();
        ViewBag.BusinessAreas = businessAreas;
        
        return View(existingAction);
    }

    // GET: Action/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var action = await _context.Actions
            .Include(a => a.Objective)
            .Include(a => a.ParentAction)
            .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);
            
        if (action == null)
        {
            return NotFound();
        }

        return View(action);
    }

    // POST: Action/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        try
        {
            var action = await _context.Actions.FindAsync(id);
            if (action != null && !action.IsDeleted)
            {
                action.IsDeleted = true;
                action.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = $"Action '{action.Title}' has been deleted successfully.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting action");
            TempData["ErrorMessage"] = "An error occurred while deleting the action. Please try again.";
        }
        
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateDetails([Bind(Prefix = "Input")] ActionDetailsUpdateInputModel input)
    {
        input.SelectedRiskIds ??= new List<int>();
        input.SelectedIssueIds ??= new List<int>();
        input.SelectedMilestoneIds ??= new List<int>();

        var action = await _context.Actions
            .Include(a => a.RiskActions)
            .Include(a => a.IssueActions)
            .Include(a => a.MilestoneActions)
            .FirstOrDefaultAsync(a => a.Id == input.Id && !a.IsDeleted);

        if (action == null)
        {
            return NotFound();
        }

        // Normalise selections
        input.SelectedRiskIds = input.SelectedRiskIds.Distinct().ToList();
        input.SelectedIssueIds = input.SelectedIssueIds.Distinct().ToList();
        input.SelectedMilestoneIds = input.SelectedMilestoneIds.Distinct().ToList();

        // Validate status
        if (string.IsNullOrWhiteSpace(input.Status) || !ActionStatusOptions.Any(option => string.Equals(option.Value, input.Status, StringComparison.OrdinalIgnoreCase)))
        {
            ModelState.AddModelError("Input.Status", "Select a status.");
        }

        // Validate priority
        if (!string.IsNullOrWhiteSpace(input.Priority) && !ActionPriorityOptions.Any(option => string.Equals(option.Value, input.Priority, StringComparison.OrdinalIgnoreCase)))
        {
            ModelState.AddModelError("Input.Priority", "Select a valid priority.");
        }

        // Validate source type
        if (!string.IsNullOrWhiteSpace(input.SourceType) && !ActionSourceTypes.Any(type => string.Equals(type, input.SourceType, StringComparison.OrdinalIgnoreCase)))
        {
            ModelState.AddModelError("Input.SourceType", "Select a valid source type.");
        }

        var sanitisedTitle = SanitiseText(input.Title);
        if (string.IsNullOrWhiteSpace(sanitisedTitle))
        {
            ModelState.AddModelError("Input.Title", "Enter an action title.");
        }
        else
        {
            input.Title = sanitisedTitle;
        }

        // Validate objective and source references
        if (input.ObjectiveId.HasValue && !await _context.Objectives.AnyAsync(o => !o.IsDeleted && o.Id == input.ObjectiveId.Value))
        {
            ModelState.AddModelError("Input.ObjectiveId", "Select a valid objective.");
        }

        if (input.ActionSourceId.HasValue && !await _context.ActionSources.AnyAsync(s => s.IsActive && s.Id == input.ActionSourceId.Value))
        {
            ModelState.AddModelError("Input.ActionSourceId", "Select a valid action source.");
        }

        if (input.ParentActionId.HasValue)
        {
            if (input.ParentActionId.Value == action.Id)
            {
                ModelState.AddModelError("Input.ParentActionId", "An action cannot be its own parent.");
            }
            else if (!await _context.Actions.AnyAsync(a => !a.IsDeleted && a.Id == input.ParentActionId.Value))
            {
                ModelState.AddModelError("Input.ParentActionId", "Select a valid parent action.");
            }
        }

        // Validate business area
        var businessAreas = await _productsApiService.GetBusinessAreasAsync();
        if (!string.IsNullOrWhiteSpace(input.BusinessArea))
        {
        var businessAreaSet = new HashSet<string>(businessAreas ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
            if (!businessAreaSet.Contains(input.BusinessArea))
            {
                ModelState.AddModelError("Input.BusinessArea", "Select a valid business area.");
            }
        }

        // Validate product selection
        if (!string.IsNullOrWhiteSpace(input.FipsId))
        {
            var products = await _productsApiService.GetProductsAsync(null);
            if (!products.Any(p => string.Equals(p.FipsId, input.FipsId, StringComparison.OrdinalIgnoreCase)))
            {
                ModelState.AddModelError("Input.FipsId", "Select a valid product.");
            }
        }

        // Validate decision selection
        int? decisionId = null;
        if (input.DecisionId.HasValue)
        {
            var decisionExists = await _context.Decisions.AnyAsync(d => !d.IsDeleted && d.Id == input.DecisionId.Value && (!action.ProjectId.HasValue || d.ProjectId == action.ProjectId));
            if (!decisionExists)
            {
                ModelState.AddModelError("Input.DecisionId", "Select a decision from this project.");
            }
            else
            {
                decisionId = input.DecisionId.Value;
            }
        }

        // Validate linked risks/issues/milestones belong to the same project (if applicable)
        var riskIds = await _context.Risks
            .Where(r => !r.IsDeleted && (!action.ProjectId.HasValue || r.ProjectId == action.ProjectId))
            .Select(r => r.Id)
            .ToListAsync();
        if (input.SelectedRiskIds.Except(riskIds).Any())
        {
            ModelState.AddModelError("Input.SelectedRiskIds", "Select risks from the same project.");
        }

        var issueIds = await _context.Issues
            .Where(i => !i.IsDeleted && (!action.ProjectId.HasValue || i.ProjectId == action.ProjectId))
            .Select(i => i.Id)
            .ToListAsync();
        if (input.SelectedIssueIds.Except(issueIds).Any())
        {
            ModelState.AddModelError("Input.SelectedIssueIds", "Select issues from the same project.");
        }

        var milestoneIds = await _context.Milestones
            .Where(m => !m.IsDeleted && (!action.ProjectId.HasValue || m.ProjectId == action.ProjectId))
            .Select(m => m.Id)
            .ToListAsync();
        if (input.SelectedMilestoneIds.Except(milestoneIds).Any())
        {
            ModelState.AddModelError("Input.SelectedMilestoneIds", "Select milestones from the same project.");
        }

        if (!ModelState.IsValid)
        {
            var actionForView = await LoadActionForDetailsAsync(action.Id);
            if (actionForView == null)
            {
                return NotFound();
            }

            var viewModel = await BuildActionDetailsViewModelAsync(actionForView, input);
            return View("Details", viewModel);
        }

        // Apply updates
        action.Title = input.Title!;
        action.Description = SanitiseMultiline(input.Description);
        action.Notes = SanitiseMultiline(input.Notes);
        action.EvidenceUrl = SanitiseText(input.EvidenceUrl);
        action.Status = input.Status!.Trim().ToLowerInvariant();
        action.Priority = string.IsNullOrWhiteSpace(input.Priority) ? null : input.Priority.Trim().ToLowerInvariant();
        action.AssignedToEmail = SanitiseEmail(input.AssignedToEmail);
        action.BusinessArea = SanitiseText(input.BusinessArea);
        action.ObjectiveId = input.ObjectiveId;
        action.ActionSourceId = input.ActionSourceId;
        action.ParentActionId = input.ParentActionId;
        action.FipsId = SanitiseText(input.FipsId);
        action.StartDate = input.StartDate;
        action.DueDate = input.DueDate;
        action.CompletedDate = input.CompletedDate;
        action.SourceType = SanitiseText(input.SourceType);
        action.SourceReference = SanitiseText(input.SourceReference);
        action.SourceRecordUrl = SanitiseText(input.SourceRecordUrl);
        action.DecisionId = decisionId;

        UpdateActionLinks(action, input);

        action.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Action updated successfully.";
            return RedirectToAction(nameof(Details), new { id = action.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating action {ActionId}", action.Id);
            ModelState.AddModelError(string.Empty, "An error occurred while updating the action. Please try again.");

            var actionForView = await LoadActionForDetailsAsync(action.Id);
            if (actionForView == null)
            {
                return NotFound();
            }

            var viewModel = await BuildActionDetailsViewModelAsync(actionForView, input);
            return View("Details", viewModel);
        }
    }

    private async Task<Models.Action?> LoadActionForDetailsAsync(int id)
    {
        return await _context.Actions
            .Include(a => a.Objective)
            .Include(a => a.ParentAction)
            .Include(a => a.ActionSource)
            .Include(a => a.Decision)
            .Include(a => a.SubActions.Where(sa => !sa.IsDeleted))
            .Include(a => a.RiskActions)
                .ThenInclude(ra => ra.Risk)
            .Include(a => a.IssueActions)
                .ThenInclude(ia => ia.Issue)
            .Include(a => a.MilestoneActions)
                .ThenInclude(ma => ma.Milestone)
            .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);
    }

    private async Task<ActionDetailsViewModel> BuildActionDetailsViewModelAsync(Models.Action action, ActionDetailsUpdateInputModel? overrideInput = null)
    {
        var input = overrideInput ?? new ActionDetailsUpdateInputModel
        {
            Id = action.Id,
            Title = action.Title,
            Status = action.Status,
            Priority = action.Priority,
            AssignedToEmail = action.AssignedToEmail,
            BusinessArea = action.BusinessArea,
            ObjectiveId = action.ObjectiveId,
            ActionSourceId = action.ActionSourceId,
            ParentActionId = action.ParentActionId,
            FipsId = action.FipsId,
            StartDate = action.StartDate,
            DueDate = action.DueDate,
            CompletedDate = action.CompletedDate,
            Description = action.Description,
            Notes = action.Notes,
            EvidenceUrl = action.EvidenceUrl,
            SourceType = action.SourceType,
            SourceReference = action.SourceReference,
            SourceRecordUrl = action.SourceRecordUrl,
            DecisionId = action.DecisionId,
            SelectedRiskIds = action.RiskActions.Select(ra => ra.RiskId).ToList(),
            SelectedIssueIds = action.IssueActions.Select(ia => ia.IssueId).ToList(),
            SelectedMilestoneIds = action.MilestoneActions.Select(ma => ma.MilestoneId).ToList()
        };

        input.Id = action.Id;
        input.Title = string.IsNullOrWhiteSpace(input.Title) ? action.Title : input.Title;
        input.SelectedRiskIds ??= new List<int>();
        input.SelectedIssueIds ??= new List<int>();
        input.SelectedMilestoneIds ??= new List<int>();

        var businessAreas = await _productsApiService.GetBusinessAreasAsync();
        var products = await _productsApiService.GetProductsAsync(null);

        var productLookup = products
            .Where(p => !string.IsNullOrWhiteSpace(p.FipsId))
            .GroupBy(p => p.FipsId!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var statusOptions = ActionStatusOptions
            .Select(option => new SelectListItem(option.Label, option.Value, string.Equals(option.Value, input.Status, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        var priorityOptions = ActionPriorityOptions
            .Select(option => new SelectListItem(option.Label, option.Value, string.Equals(option.Value, input.Priority, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        var businessAreaOptions = businessAreas
            .Select(area => new SelectListItem(area, area, string.Equals(area, input.BusinessArea, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        var objectivesQuery = _context.Objectives
            .Where(o => !o.IsDeleted);
        if (action.ProjectId.HasValue)
        {
            objectivesQuery = objectivesQuery.Where(o =>
                _context.ProjectObjectives.Any(po => po.ProjectId == action.ProjectId.Value && po.ObjectiveId == o.Id));
        }

        var objectiveOptions = await objectivesQuery
            .OrderBy(o => o.Title)
            .Select(o => new SelectListItem(o.Title, o.Id.ToString(CultureInfo.InvariantCulture), input.ObjectiveId == o.Id))
            .ToListAsync();

        var actionSourceOptions = await _context.ActionSources
            .Where(source => source.IsActive)
            .OrderBy(source => source.SortOrder)
            .Select(source => new SelectListItem(source.Name, source.Id.ToString(CultureInfo.InvariantCulture), input.ActionSourceId == source.Id))
            .ToListAsync();

        var parentActionOptions = await _context.Actions
            .Where(a => !a.IsDeleted && a.Id != action.Id)
            .OrderBy(a => a.Title)
            .Select(a => new SelectListItem(a.Title, a.Id.ToString(CultureInfo.InvariantCulture), input.ParentActionId == a.Id))
            .ToListAsync();

        var productOptions = products
            .Where(p => !string.IsNullOrWhiteSpace(p.FipsId))
            .OrderBy(p => p.Title)
            .Select(p => new SelectListItem($"{p.Title} ({p.FipsId})", p.FipsId, string.Equals(p.FipsId, input.FipsId, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        var riskQuery = _context.Risks.Where(r => !r.IsDeleted);
        if (action.ProjectId.HasValue)
        {
            riskQuery = riskQuery.Where(r => r.ProjectId == action.ProjectId);
        }

        var riskSet = new HashSet<int>(input.SelectedRiskIds);
        var riskOptions = await riskQuery
            .OrderBy(r => r.Title)
            .Select(r => new SelectListItem(r.Title, r.Id.ToString(CultureInfo.InvariantCulture), riskSet.Contains(r.Id)))
            .ToListAsync();

        var issueQuery = _context.Issues.Where(i => !i.IsDeleted);
        if (action.ProjectId.HasValue)
        {
            issueQuery = issueQuery.Where(i => i.ProjectId == action.ProjectId);
        }

        var issueSet = new HashSet<int>(input.SelectedIssueIds);
        var issueOptions = await issueQuery
            .OrderBy(i => i.Title)
            .Select(i => new SelectListItem(i.Title, i.Id.ToString(CultureInfo.InvariantCulture), issueSet.Contains(i.Id)))
            .ToListAsync();

        var milestoneQuery = _context.Milestones.Where(m => !m.IsDeleted);
        if (action.ProjectId.HasValue)
        {
            milestoneQuery = milestoneQuery.Where(m => m.ProjectId == action.ProjectId);
        }

        var milestoneSet = new HashSet<int>(input.SelectedMilestoneIds);
        var milestoneOptions = await milestoneQuery
            .OrderBy(m => m.Name)
            .Select(m => new SelectListItem(m.Name, m.Id.ToString(CultureInfo.InvariantCulture), milestoneSet.Contains(m.Id)))
            .ToListAsync();

        var decisionQuery = _context.Decisions.Where(d => !d.IsDeleted);
        if (action.ProjectId.HasValue)
        {
            decisionQuery = decisionQuery.Where(d => d.ProjectId == action.ProjectId);
        }

        var decisionOptions = await decisionQuery
            .OrderBy(d => d.Title)
            .Select(d => new SelectListItem(d.Title, d.Id.ToString(CultureInfo.InvariantCulture), input.DecisionId == d.Id))
            .ToListAsync();

        var sourceTypeOptions = ActionSourceTypes
            .Select(type => new SelectListItem(type, type, string.Equals(type, input.SourceType, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        var decisionAssociations = await _context.Decisions
            .Where(d => d.Actions.Any(a => a.Id == action.Id))
            .OrderByDescending(d => d.DecisionDate ?? d.CreatedAt)
            .ToListAsync();

        if (action.Decision != null && decisionAssociations.All(d => d.Id != action.Decision.Id))
        {
            decisionAssociations.Insert(0, action.Decision);
        }

        return new ActionDetailsViewModel
        {
            Action = action,
            Input = input,
            StatusOptions = statusOptions,
            PriorityOptions = priorityOptions,
            BusinessAreaOptions = businessAreaOptions,
            ObjectiveOptions = objectiveOptions,
            ActionSourceOptions = actionSourceOptions,
            ParentActionOptions = parentActionOptions,
            ProductOptions = productOptions,
            RiskOptions = riskOptions,
            IssueOptions = issueOptions,
            MilestoneOptions = milestoneOptions,
            DecisionOptions = decisionOptions,
            SourceTypeOptions = sourceTypeOptions,
            ProductLookup = productLookup,
            DecisionAssociations = decisionAssociations
        };
    }

    private void UpdateActionLinks(Models.Action action, ActionDetailsUpdateInputModel input)
    {
        SynchroniseRiskLinks(action, input.SelectedRiskIds);
        SynchroniseIssueLinks(action, input.SelectedIssueIds);
        SynchroniseMilestoneLinks(action, input.SelectedMilestoneIds);
    }

    private void SynchroniseRiskLinks(Models.Action action, IEnumerable<int> desiredIds)
    {
        var desired = new HashSet<int>(desiredIds);

        foreach (var link in action.RiskActions.Where(l => !desired.Contains(l.RiskId)).ToList())
        {
            _context.RiskActions.Remove(link);
        }

        foreach (var riskId in desired.Where(id => action.RiskActions.All(l => l.RiskId != id)))
        {
            _context.RiskActions.Add(new RiskAction
            {
                ActionId = action.Id,
                RiskId = riskId
            });
        }
    }

    private void SynchroniseIssueLinks(Models.Action action, IEnumerable<int> desiredIds)
    {
        var desired = new HashSet<int>(desiredIds);

        foreach (var link in action.IssueActions.Where(l => !desired.Contains(l.IssueId)).ToList())
        {
            _context.IssueActions.Remove(link);
        }

        foreach (var issueId in desired.Where(id => action.IssueActions.All(l => l.IssueId != id)))
        {
            _context.IssueActions.Add(new IssueAction
            {
                ActionId = action.Id,
                IssueId = issueId
            });
        }
    }

    private void SynchroniseMilestoneLinks(Models.Action action, IEnumerable<int> desiredIds)
    {
        var desired = new HashSet<int>(desiredIds);

        foreach (var link in action.MilestoneActions.Where(l => !desired.Contains(l.MilestoneId)).ToList())
        {
            _context.MilestoneActions.Remove(link);
        }

        foreach (var milestoneId in desired.Where(id => action.MilestoneActions.All(l => l.MilestoneId != id)))
        {
            _context.MilestoneActions.Add(new MilestoneAction
            {
                ActionId = action.Id,
                MilestoneId = milestoneId
            });
        }
    }

    private static string? SanitiseText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string? SanitiseMultiline(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string? SanitiseEmail(string? value)
    {
        var sanitised = SanitiseText(value);
        return sanitised?.ToLowerInvariant();
    }
}

