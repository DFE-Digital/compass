using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Compass.Data;
using Compass.Models;
using Compass.Services;
using Microsoft.AspNetCore.Authorization;

namespace Compass.Controllers;

[Authorize]
public class ActionController : Controller
{
    private readonly CompassDbContext _context;
    private readonly IProductsApiService _productsApiService;
    private readonly ILogger<ActionController> _logger;

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
        if (id == null)
        {
            return NotFound();
        }

        var action = await _context.Actions
            .Include(a => a.Objective)
            .Include(a => a.ParentAction)
            .Include(a => a.ActionSource)
            .Include(a => a.SubActions.Where(sa => !sa.IsDeleted))
            .Include(a => a.RiskActions)
                .ThenInclude(ra => ra.Risk)
            .Include(a => a.IssueActions)
                .ThenInclude(ia => ia.Issue)
            .Include(a => a.MilestoneActions)
                .ThenInclude(ma => ma.Milestone)
            .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);

        if (action == null)
        {
            return NotFound();
        }

        // Fetch products for lookup
        var products = await _productsApiService.GetProductsAsync();
        ViewBag.Products = products.OrderBy(p => p.Title).ToList();

        return View(action);
    }

    // GET: Action/Create
    public async Task<IActionResult> Create(int? objectiveId, int? parentActionId, int? riskId, int? issueId, int? milestoneId)
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
    public async Task<IActionResult> Create(int? riskId, int? issueId, int? milestoneId)
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
}

