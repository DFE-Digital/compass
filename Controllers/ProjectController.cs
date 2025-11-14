using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Compass.Data;
using Compass.Models;
using Compass.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using Compass.ViewModels;

namespace Compass.Controllers
{
    [Authorize]
    public class ProjectController : Controller
    {
        private readonly CompassDbContext _context;
        private readonly ILogger<ProjectController> _logger;
        private readonly IProductsApiService _productsApiService;
        private const string OtherOptionValue = "__other__";

        private static readonly (string Value, string Label)[] MilestoneStatuses = new[]
        {
            ("not_started", "Not started"),
            ("in_progress", "In progress"),
            ("on_track", "On track"),
            ("at_risk", "At risk"),
            ("delayed", "Delayed"),
            ("complete", "Complete"),
            ("cancelled", "Cancelled")
        };

        private static readonly (string Value, string Label)[] KpiStatuses = new[]
        {
            ("not_started", "Not started"),
            ("monitoring", "Monitoring"),
            ("on_track", "On track"),
            ("at_risk", "At risk"),
            ("blocked", "Blocked"),
            ("achieved", "Achieved"),
            ("retired", "Retired")
        };

        private static readonly string[] ActionStatusValues =
        {
            "not_started",
            "in_progress",
            "blocked",
            "done",
            "cancelled"
        };

        private static readonly string[] ActionPriorityValues =
        {
            "low",
            "medium",
            "high"
        };

        private static readonly string[] IssueSeverityValues =
        {
            "low",
            "medium",
            "high",
            "critical"
        };

        private static readonly string[] IssueStatusValues =
        {
            "open",
            "in_progress",
            "blocked",
            "resolved",
            "closed"
        };

        private static readonly string[] OutcomeConfidenceValues =
        {
            "Low",
            "Medium",
            "High"
        };

        public ProjectController(CompassDbContext context, ILogger<ProjectController> logger, IProductsApiService productsApiService)
        {
            _context = context;
            _logger = logger;
            _productsApiService = productsApiService;
        }

        // GET: Project
        public async Task<IActionResult> Index(string search, string ragStatus, string businessArea, string phase, string flagship, int? priority, int page = 1)
        {
            const int pageSize = 15;
            var pageNumber = page < 1 ? 1 : page;

            // Get current user's email
            var userEmail = User.Identity?.Name;

            // Get user's projects (where they are a named contact)
            var userProjects = new List<Project>();
            if (!string.IsNullOrEmpty(userEmail))
            {
                userProjects = await _context.Projects
                    .Where(p => !p.IsDeleted && p.ProjectContacts.Any(pc => pc.Email.ToLower() == userEmail.ToLower()))
                    .AsNoTracking()
                    .Include(p => p.DeliveryPriority)
                    .Include(p => p.ProjectMissions)
                        .ThenInclude(pm => pm.Mission)
                    .Include(p => p.ProjectObjectives)
                        .ThenInclude(po => po.Objective)
                    .Include(p => p.FundingAllocations)
                        .ThenInclude(fa => fa.FundingSource)
                    .Include(p => p.Outcomes)
                    .Include(p => p.ProjectContacts)
                        .ThenInclude(pc => pc.User)
                    .OrderBy(p => p.Title)
                    .ToListAsync();
            }

            var query = _context.Projects
                .Where(p => !p.IsDeleted)
                .Include(p => p.DeliveryPriority)
                .Include(p => p.ProjectMissions)
                    .ThenInclude(pm => pm.Mission)
                .Include(p => p.ProjectObjectives)
                    .ThenInclude(po => po.Objective)
                .Include(p => p.FundingAllocations)
                    .ThenInclude(fa => fa.FundingSource)
                .Include(p => p.Outcomes)
                .Include(p => p.ProjectContacts)
                    .ThenInclude(pc => pc.User)
                .AsQueryable();

            // Apply search filter
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p => p.Title.Contains(search) || p.ProjectCode.Contains(search));
            }

            // Apply RAG Status filter
            if (!string.IsNullOrEmpty(ragStatus))
            {
                query = query.Where(p => p.RagStatus == ragStatus);
            }

            // Apply Business Area filter
            if (!string.IsNullOrEmpty(businessArea))
            {
                query = query.Where(p => p.BusinessArea == businessArea);
            }

            // Apply Phase filter
            if (!string.IsNullOrEmpty(phase))
            {
                query = query.Where(p => p.Phase == phase);
            }

            // Apply Flagship filter
            if (!string.IsNullOrEmpty(flagship))
            {
                var isFlagship = flagship == "true";
                query = query.Where(p => p.IsFlagship == isFlagship);
            }

            if (priority.HasValue)
            {
                query = query.Where(p => p.DeliveryPriorityId == priority.Value);
            }

            var orderedQuery = query
                .OrderByDescending(p => p.CreatedAt)
                .AsNoTracking();

            var totalCount = await orderedQuery.CountAsync();

            var projects = await orderedQuery
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Populate ViewBag for filter dropdowns and current filter values
            ViewBag.CurrentSearch = search;
            ViewBag.CurrentRagStatus = ragStatus;
            ViewBag.CurrentBusinessArea = businessArea;
            ViewBag.CurrentPhase = phase;
            ViewBag.CurrentFlagship = flagship;
            ViewBag.CurrentPriority = priority;

            // Get business areas and phases from CMS
            ViewBag.BusinessAreas = await _productsApiService.GetBusinessAreasAsync();

            ViewBag.Phases = await _productsApiService.GetPhasesAsync();
            ViewBag.DeliveryPriorities = await _context.DeliveryPriorities
                .Where(dp => dp.IsActive)
                .OrderBy(dp => dp.SortOrder)
                .ThenBy(dp => dp.Name)
                .AsNoTracking()
                .ToListAsync();

            var viewModel = new ProjectIndexViewModel
            {
                Projects = projects.AsReadOnly(),
                UserProjects = userProjects.AsReadOnly(),
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };

            return View(viewModel);
        }

        // GET: Project/Details/5
        public async Task<IActionResult> Details(int? id, string tab = "overview", string issuesView = "table")
        {
            if (id == null)
            {
                return NotFound();
            }

        var project = await _context.Projects
            .Include(p => p.DeliveryPriority)
            .Include(p => p.PrimaryContactUser)
                .Include(p => p.ProjectMissions)
                    .ThenInclude(pm => pm.Mission)
                .Include(p => p.ProjectObjectives)
                    .ThenInclude(po => po.Objective)
                .Include(p => p.FundingAllocations)
                    .ThenInclude(fa => fa.FundingSource)
                .Include(p => p.Outcomes)
                .Include(p => p.ProjectContacts)
                .Include(p => p.Successes)
                .Include(p => p.ProjectProducts)
                .Include(p => p.Milestones)
                    .ThenInclude(m => m.Objective)
                .Include(p => p.Kpis)
                    .ThenInclude(k => k.PerformanceData)
                        .ThenInclude(dp => dp.SubmittedByUser)
                .Include(p => p.Kpis)
                    .ThenInclude(k => k.Objective)
                .Include(p => p.Kpis)
                    .ThenInclude(k => k.Milestone)
                .Include(p => p.Kpis)
                    .ThenInclude(k => k.OwnerUser)
                .Include(p => p.Risks)
                    .ThenInclude(r => r.RiskActions)
                        .ThenInclude(ra => ra.Action)
                .Include(p => p.Risks)
                    .ThenInclude(r => r.RiskDecisions)
                        .ThenInclude(rd => rd.Decision)
                .Include(p => p.Issues)
                    .ThenInclude(i => i.IssueActions)
                        .ThenInclude(ia => ia.Action)
                .Include(p => p.Issues)
                    .ThenInclude(i => i.IssueDecisions)
                        .ThenInclude(id => id.Decision)
                .Include(p => p.Actions)
                    .ThenInclude(a => a.RiskActions)
                        .ThenInclude(ra => ra.Risk)
                .Include(p => p.Actions)
                    .ThenInclude(a => a.IssueActions)
                        .ThenInclude(ia => ia.Issue)
                .Include(p => p.Actions)
                    .ThenInclude(a => a.Decision)
                .Include(p => p.Actions)
                    .ThenInclude(a => a.ActionSource)
                .Include(p => p.Actions)
                    .ThenInclude(a => a.ParentAction)
                .Include(p => p.Decisions)
                    .ThenInclude(d => d.RiskDecisions)
                        .ThenInclude(rd => rd.Risk)
                .Include(p => p.Decisions)
                    .ThenInclude(d => d.IssueDecisions)
                        .ThenInclude(id => id.Issue)
                .Include(p => p.Decisions)
                    .ThenInclude(d => d.Actions)
                .Include(p => p.Decisions)
                    .ThenInclude(d => d.OwnerUser)
                .Include(p => p.RagHistory)
                .Include(p => p.ResourceFunding)
                .Include(p => p.FundingHistory)
                .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);

            if (project == null)
            {
                return NotFound();
            }

            // Manually load dependencies since the relationship is polymorphic
            project.DependenciesAsSource = await _context.Dependencies
                .Where(d => d.SourceEntityType == "Project" && d.SourceEntityId == project.Id)
                .ToListAsync();

            project.DependenciesAsTarget = await _context.Dependencies
                .Where(d => d.TargetEntityType == "Project" && d.TargetEntityId == project.Id)
                .ToListAsync();

            // Populate dependency titles
            foreach (var dep in project.DependenciesAsSource)
            {
                dep.SourceEntityTitle = project.Title;
                dep.TargetEntityTitle = await GetEntityTitle(dep.TargetEntityType, dep.TargetEntityId);
            }
            foreach (var dep in project.DependenciesAsTarget)
            {
                dep.SourceEntityTitle = await GetEntityTitle(dep.SourceEntityType, dep.SourceEntityId);
                dep.TargetEntityTitle = project.Title;
            }

            // Get all projects for dependency dropdown
            ViewBag.AllProjects = await _context.Projects
                .Where(p => !p.IsDeleted && p.Id != id)
                .OrderBy(p => p.Title)
                .Select(p => new { p.Id, p.Title })
                .ToListAsync();

            // Get CMS products for linking
            ViewBag.CmsProducts = await _productsApiService.GetProductsAsync();

            // Get objectives and missions for strategic alignment
            ViewBag.Objectives = await _context.Objectives
                .Where(o => !o.IsDeleted && o.Status == "active")
                .OrderBy(o => o.Theme)
                .ThenBy(o => o.Title)
                .ToListAsync();

            ViewBag.Missions = await _context.Missions
                .Where(m => !m.IsDeleted && m.Status == "Active")
                .OrderBy(m => m.Theme)
                .ThenBy(m => m.Title)
                .ToListAsync();

            // Get business areas and phases from CMS
            ViewBag.BusinessAreas = await _productsApiService.GetBusinessAreasAsync();

            ViewBag.Phases = await _productsApiService.GetPhasesAsync();

            ViewBag.DeliveryPriorities = await _context.DeliveryPriorities
                .Where(dp => dp.IsActive)
                .OrderBy(dp => dp.SortOrder)
                .ThenBy(dp => dp.Name)
                .ToListAsync();

            // Get government departments for multi-department cooperation
            ViewBag.GovernmentDepartments = await _context.GovernmentDepartments
                .Where(d => !d.IsDeleted && d.ClosedAt == null)
                .OrderBy(d => d.Title)
                .ToListAsync();

            ViewBag.ActionSources = await _context.ActionSources
                .Where(a => a.IsActive)
                .OrderBy(a => a.SortOrder)
                .ToListAsync();

            ViewBag.DecisionStatuses = new[] { "pending", "approved", "rejected", "superseded" };
            ViewBag.ActionStatuses = ActionStatusValues;
            ViewBag.ActionPriorities = ActionPriorityValues;

            // Get deliverable projects if this is a flagship project
            if (project.IsFlagship)
            {
                _logger.LogInformation("Loading deliverables for flagship project {ProjectId}. Total dependencies: {Count}", 
                    project.Id, project.DependenciesAsSource?.Count ?? 0);
                
                if (project.DependenciesAsSource != null && project.DependenciesAsSource.Any())
                {
                    foreach (var dep in project.DependenciesAsSource)
                    {
                        _logger.LogInformation("Dependency found: TargetType={TargetType}, TargetId={TargetId}, DependencyType={DependencyType}",
                            dep.TargetEntityType, dep.TargetEntityId, dep.DependencyType);
                    }
                }
                else
                {
                    _logger.LogWarning("DependenciesAsSource is null or empty for project {ProjectId}", project.Id);
                }
                
                var deliverableIds = project.DependenciesAsSource
                    .Where(d => d.TargetEntityType == "Project" && d.DependencyType == "Deliverable")
                    .Select(d => d.TargetEntityId)
                    .ToList();

                _logger.LogInformation("Found {Count} deliverable relationships for project {ProjectId}", 
                    deliverableIds.Count, project.Id);

                ViewBag.DeliverableProjects = await _context.Projects
                    .Include(p => p.Milestones)
                    .Where(p => deliverableIds.Contains(p.Id) && !p.IsDeleted)
                    .OrderBy(p => p.Title)
                    .ToListAsync();
                
                _logger.LogInformation("Loaded {Count} deliverable projects for project {ProjectId}", 
                    ((List<Project>)ViewBag.DeliverableProjects).Count, project.Id);
            }
            else
            {
                ViewBag.DeliverableProjects = new List<Project>();
            }

            // Set current tab
            ViewBag.CurrentTab = tab;
            ViewBag.IssuesView = string.Equals(issuesView, "priority", StringComparison.OrdinalIgnoreCase) ? "priority" : "table";

            return View(project);
        }

        // POST: Project/AddSuccess
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSuccess([Bind(Prefix = "Input")] ProjectSuccessInputModel input)
        {
            if (!ModelState.IsValid)
            {
                var projectSummary = await GetProjectSummaryAsync(input.ProjectId);

                if (projectSummary == null)
                {
                    return NotFound();
                }

                var invalidViewModel = new ProjectSuccessFormViewModel
                {
                    ProjectTitle = projectSummary.Title,
                    ProjectSummary = projectSummary,
                    Input = input
                };

                return View("CreateSuccess", invalidViewModel);
            }

            try
            {
                var projectExists = await _context.Projects
                    .AnyAsync(p => p.Id == input.ProjectId && !p.IsDeleted);

                if (!projectExists)
                {
                    return NotFound();
                }

                var success = new ProjectSuccess
                {
                    ProjectId = input.ProjectId,
                    SuccessDescription = input.SuccessDescription,
                    RecordedByEmail = User.FindFirstValue(ClaimTypes.Email) ?? "unknown@example.com",
                    RecordedByName = User.FindFirstValue(ClaimTypes.Name) ?? "Unknown User",
                    RecordedAt = DateTime.UtcNow,
                    IsReportedToSlt = input.IsReportedToSlt
                };

                _context.ProjectSuccesses.Add(success);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Success added successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding project success");
                TempData["ErrorMessage"] = "An error occurred while adding the success.";
            }

            return RedirectToAction(nameof(Details), new { id = input.ProjectId, tab = "successes" });
        }

        // POST: Project/UpdateSuccess
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSuccess([Bind(Prefix = "Input")] ProjectSuccessInputModel input)
        {
            if (!input.SuccessId.HasValue)
            {
                return NotFound();
            }

            var success = await _context.ProjectSuccesses
                .Include(s => s.Project)
                .FirstOrDefaultAsync(s => s.Id == input.SuccessId.Value && s.ProjectId == input.ProjectId);

            if (success == null || success.Project.IsDeleted)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                var invalidViewModel = new ProjectSuccessFormViewModel
                {
                    ProjectTitle = success.Project.Title,
                    ProjectSummary = CreateProjectSummary(success.Project),
                    Input = input,
                    RecordedAt = success.RecordedAt,
                    RecordedByName = success.RecordedByName,
                    RecordedByEmail = success.RecordedByEmail
                };

                return View("EditSuccess", invalidViewModel);
            }

            try
            {
                success.SuccessDescription = input.SuccessDescription;
                success.IsReportedToSlt = input.IsReportedToSlt;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Success updated successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating project success");
                TempData["ErrorMessage"] = "An error occurred while updating the success.";
            }

            return RedirectToAction(nameof(Details), new { id = input.ProjectId, tab = "successes" });
        }

        // POST: Project/DeleteSuccess
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSuccess(int projectId, int successId)
        {
            try
            {
                var success = await _context.ProjectSuccesses
                    .FirstOrDefaultAsync(s => s.Id == successId && s.ProjectId == projectId);

                if (success == null)
                {
                    TempData["ErrorMessage"] = "Success not found.";
                    return RedirectToAction(nameof(Details), new { id = projectId, tab = "successes" });
                }

                _context.ProjectSuccesses.Remove(success);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Success deleted successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting project success");
                TempData["ErrorMessage"] = "An error occurred while deleting the success.";
            }

            return RedirectToAction(nameof(Details), new { id = projectId, tab = "successes" });
        }

        // POST: Project/AddProduct
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddProduct(int projectId, string productFipsId)
        {
            try
            {
                // Get product details from CMS
                var cmsProduct = await _productsApiService.GetProductByFipsIdAsync(productFipsId);
                
                if (cmsProduct == null)
                {
                    TempData["ErrorMessage"] = "Product not found in CMS.";
                    return RedirectToAction(nameof(Details), new { id = projectId, tab = "products" });
                }

                // Check if product is already linked
                var existingLink = await _context.ProjectProducts
                    .FirstOrDefaultAsync(pp => pp.ProjectId == projectId && pp.ProductFipsId == productFipsId);
                
                if (existingLink != null)
                {
                    TempData["ErrorMessage"] = "This product is already linked to the project.";
                    return RedirectToAction(nameof(Details), new { id = projectId, tab = "products" });
                }

                var projectProduct = new ProjectProduct
                {
                    ProjectId = projectId,
                    ProductFipsId = cmsProduct.FipsId ?? productFipsId,
                    ProductTitle = cmsProduct.Title,
                    ProductDescription = null, // CMS doesn't provide description
                    ProductUrl = null, // CMS doesn't provide URL
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.ProjectProducts.Add(projectProduct);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Product linked successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error linking product to project");
                TempData["ErrorMessage"] = "An error occurred while linking the product.";
            }

            return RedirectToAction(nameof(Details), new { id = projectId, tab = "products" });
        }

        // POST: Project/RemoveProduct
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveProduct(int projectProductId, int projectId)
        {
            try
            {
                var projectProduct = await _context.ProjectProducts.FindAsync(projectProductId);
                if (projectProduct != null)
                {
                    _context.ProjectProducts.Remove(projectProduct);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Product unlinked successfully!";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unlinking product from project");
                TempData["ErrorMessage"] = "An error occurred while unlinking the product.";
            }

            return RedirectToAction(nameof(Details), new { id = projectId, tab = "products" });
        }

        // GET: Project/CreateProduct
        public async Task<IActionResult> CreateProduct(int projectId)
        {
            var project = await _context.Projects.FindAsync(projectId);
            if (project == null)
            {
                return NotFound();
            }

            // Get business areas and phases for dropdowns
            var businessAreas = await _productsApiService.GetBusinessAreaCategoryValuesAsync();
            var phases = await _productsApiService.GetPhaseCategoryValuesAsync();

            ViewBag.BusinessAreas = businessAreas;
            ViewBag.Phases = phases;
            ViewBag.ProjectId = projectId;
            ViewBag.ProjectTitle = project.Title;
            ViewBag.ProjectAim = project.Aim;
            ViewBag.ProjectBusinessArea = project.BusinessArea;
            ViewBag.ProjectPhase = project.Phase;

            return View(project);
        }

        // POST: Project/CreateProduct
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProduct(
            int projectId, 
            string productTitle, 
            string? shortDescription, 
            string? longDescription,
            int? businessAreaCategoryValueId,
            int? phaseCategoryValueId,
            bool confirmNotDuplicate = false)
        {
            var project = await _context.Projects.FindAsync(projectId);
            if (project == null)
            {
                return NotFound();
            }

            // Check for similar products if not confirmed
            if (!confirmNotDuplicate)
            {
                var similarProducts = await _productsApiService.SearchProductsByTitleAsync(productTitle);
                if (similarProducts.Any())
                {
                    // Get business areas and phases for dropdowns
                    var businessAreas = await _productsApiService.GetBusinessAreaCategoryValuesAsync();
                    var phases = await _productsApiService.GetPhaseCategoryValuesAsync();

                    ViewBag.BusinessAreas = businessAreas;
                    ViewBag.Phases = phases;
                    ViewBag.ProjectId = projectId;
                    ViewBag.ProjectTitle = project.Title;
                    ViewBag.ProjectAim = project.Aim;
                    ViewBag.ProjectBusinessArea = project.BusinessArea;
                    ViewBag.ProjectPhase = project.Phase;
                    ViewBag.SimilarProducts = similarProducts;
                    ViewBag.ProductTitle = productTitle;
                    ViewBag.ShortDescription = shortDescription;
                    ViewBag.LongDescription = longDescription;
                    ViewBag.BusinessAreaCategoryValueId = businessAreaCategoryValueId;
                    ViewBag.PhaseCategoryValueId = phaseCategoryValueId;

                    TempData["WarningMessage"] = "Similar products found in FIPS CMS. Please confirm this is not a duplicate.";
                    return View(project);
                }
            }

            try
            {
                // Build category values list
                var categoryValueIds = new List<int>();
                if (businessAreaCategoryValueId.HasValue)
                {
                    categoryValueIds.Add(businessAreaCategoryValueId.Value);
                }
                if (phaseCategoryValueId.HasValue)
                {
                    categoryValueIds.Add(phaseCategoryValueId.Value);
                }

                // Create product in CMS
                var createdProduct = await _productsApiService.CreateProductAsync(
                    productTitle,
                    shortDescription,
                    longDescription,
                    categoryValueIds,
                    "Active"
                );

                if (createdProduct == null || string.IsNullOrEmpty(createdProduct.FipsId))
                {
                    TempData["ErrorMessage"] = "Failed to create product in FIPS CMS.";
                    return RedirectToAction(nameof(CreateProduct), new { projectId });
                }

                // Link the newly created product to the project
                var projectProduct = new ProjectProduct
                {
                    ProjectId = projectId,
                    ProductFipsId = createdProduct.FipsId,
                    ProductTitle = createdProduct.Title,
                    ProductDescription = shortDescription,
                    ProductUrl = null,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.ProjectProducts.Add(projectProduct);
                await _context.SaveChangesAsync();

                // Redirect to confirmation page
                return RedirectToAction(nameof(CreateProductConfirmation), new { 
                    projectId, 
                    fipsId = createdProduct.FipsId,
                    productTitle = createdProduct.Title
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product in FIPS CMS");
                TempData["ErrorMessage"] = "An error occurred while creating the product.";
                return RedirectToAction(nameof(CreateProduct), new { projectId });
            }
        }

        // GET: Project/CreateProductConfirmation
        public async Task<IActionResult> CreateProductConfirmation(int projectId, string fipsId, string productTitle)
        {
            var project = await _context.Projects.FindAsync(projectId);
            if (project == null)
            {
                return NotFound();
            }

            ViewBag.ProjectId = projectId;
            ViewBag.ProjectTitle = project.Title;
            ViewBag.FipsId = fipsId;
            ViewBag.ProductTitle = productTitle;

            return View();
        }

        // ==================== TEAM MANAGEMENT ====================

        [HttpGet]
        public async Task<IActionResult> CreateTeamMember(int projectId)
        {
            var summary = await GetProjectSummaryAsync(projectId);
            if (summary == null)
            {
                return NotFound();
            }

            var placeholder = new ProjectContact
            {
                ProjectId = summary.Id,
                FundingArrangement = "Not specified",
                EmploymentType = "Permanent",
                TeamStatus = "current"
            };

            var viewModel = BuildTeamMemberFormViewModel(summary, placeholder);
            return View("CreateTeamMember", viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTeamMember(ProjectTeamMemberInputModel input)
        {
            input.EmploymentType = NormalizeEmploymentType(input.EmploymentType);
            input.TeamStatus = NormalizeTeamStatus(input.TeamStatus);

            if (string.Equals(input.TeamStatus, "previous", StringComparison.OrdinalIgnoreCase) &&
                string.IsNullOrWhiteSpace(input.LeaveReason))
            {
                ModelState.AddModelError(nameof(ProjectTeamMemberInputModel.LeaveReason), "Provide a reason for leaving the team.");
            }

            if (!input.UserId.HasValue)
            {
                ModelState.AddModelError(nameof(ProjectTeamMemberInputModel.UserId), "Select a team member.");
            }

            if (!ModelState.IsValid)
            {
                var invalidViewModel = await BuildTeamMemberFormViewModelAsync(input);
                if (invalidViewModel == null)
                {
                    return NotFound();
                }
                return View("CreateTeamMember", invalidViewModel);
            }

            try
            {
                var project = await _context.Projects
                    .Include(p => p.ProjectContacts)
                    .FirstOrDefaultAsync(p => p.Id == input.ProjectId && !p.IsDeleted);

                if (project == null)
                {
                    return NotFound();
                }

                var user = await _context.Users.FindAsync(input.UserId!.Value);
                if (user == null)
                {
                    ModelState.AddModelError(nameof(ProjectTeamMemberInputModel.UserId), "Selected person could not be found.");
                    var invalidViewModel = await BuildTeamMemberFormViewModelAsync(input);
                    if (invalidViewModel == null)
                    {
                        return NotFound();
                    }
                    return View("CreateTeamMember", invalidViewModel);
                }

                var teamMember = new ProjectContact
                {
                    ProjectId = input.ProjectId,
                    UserId = user.Id,
                    Name = user.Name,
                    Email = user.Email,
                    Role = input.Role.Trim(),
                    FundingArrangement = input.FundingArrangement.Trim(),
                    EmploymentType = input.EmploymentType,
                    TeamStatus = input.TeamStatus,
                    LeaveReason = string.Equals(input.TeamStatus, "previous", StringComparison.OrdinalIgnoreCase) ? input.LeaveReason?.Trim() : null,
                    LeftAt = string.Equals(input.TeamStatus, "previous", StringComparison.OrdinalIgnoreCase) ? DateTime.UtcNow : null,
                    SortOrder = project.ProjectContacts.Count + 1,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.ProjectContacts.Add(teamMember);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Team member added successfully.";
                return RedirectToAction(nameof(Details), new { id = input.ProjectId, tab = "team" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding project team member");
                TempData["ErrorMessage"] = "An error occurred while adding the team member.";
                return RedirectToAction(nameof(Details), new { id = input.ProjectId, tab = "team" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditTeamMember(int projectId, int teamMemberId)
        {
            var contact = await _context.ProjectContacts
                .Include(pc => pc.Project)
                .Include(pc => pc.User)
                .FirstOrDefaultAsync(pc => pc.Id == teamMemberId && pc.ProjectId == projectId);

            if (contact == null || contact.Project.IsDeleted)
            {
                return NotFound();
            }

            await EnsureContactUserAsync(contact);
            var summary = CreateProjectSummary(contact.Project);
            var viewModel = BuildTeamMemberFormViewModel(summary, contact);
            viewModel.Input.TeamMemberId = contact.Id;
            return View("EditTeamMember", viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateTeamMember(ProjectTeamMemberInputModel input)
        {
            if (!input.TeamMemberId.HasValue)
            {
                return NotFound();
            }

            input.EmploymentType = NormalizeEmploymentType(input.EmploymentType);
            input.TeamStatus = NormalizeTeamStatus(input.TeamStatus);

            if (string.Equals(input.TeamStatus, "previous", StringComparison.OrdinalIgnoreCase) &&
                string.IsNullOrWhiteSpace(input.LeaveReason))
            {
                ModelState.AddModelError(nameof(ProjectTeamMemberInputModel.LeaveReason), "Provide a reason for leaving the team.");
            }

            if (!input.UserId.HasValue)
            {
                ModelState.AddModelError(nameof(ProjectTeamMemberInputModel.UserId), "Select a team member.");
            }

            if (!ModelState.IsValid)
            {
                var invalidViewModel = await BuildTeamMemberFormViewModelAsync(input);
                if (invalidViewModel == null)
                {
                    return NotFound();
                }
                return View("EditTeamMember", invalidViewModel);
            }

            var contact = await _context.ProjectContacts
                .Include(pc => pc.Project)
                .FirstOrDefaultAsync(pc => pc.Id == input.TeamMemberId.Value && pc.ProjectId == input.ProjectId);

            if (contact == null || contact.Project.IsDeleted)
            {
                return NotFound();
            }

            var user = await _context.Users.FindAsync(input.UserId!.Value);
            if (user == null)
            {
                ModelState.AddModelError(nameof(ProjectTeamMemberInputModel.UserId), "Selected person could not be found.");
                var invalidViewModel = await BuildTeamMemberFormViewModelAsync(input);
                if (invalidViewModel == null)
                {
                    return NotFound();
                }
                return View("EditTeamMember", invalidViewModel);
            }

            contact.UserId = user.Id;
            contact.Name = user.Name;
            contact.Email = user.Email;
            contact.Role = input.Role.Trim();
            contact.FundingArrangement = input.FundingArrangement.Trim();
            contact.EmploymentType = input.EmploymentType;
            contact.TeamStatus = input.TeamStatus;
            contact.LeaveReason = string.Equals(input.TeamStatus, "previous", StringComparison.OrdinalIgnoreCase) ? input.LeaveReason?.Trim() : null;
            contact.LeftAt = string.Equals(input.TeamStatus, "previous", StringComparison.OrdinalIgnoreCase) ? contact.LeftAt ?? DateTime.UtcNow : null;
            if (!string.Equals(input.TeamStatus, "previous", StringComparison.OrdinalIgnoreCase))
            {
                contact.LeftAt = null;
            }
            contact.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Team member updated successfully.";
            return RedirectToAction(nameof(TeamMemberDetails), new { projectId = input.ProjectId, teamMemberId = contact.Id });
        }

        [HttpGet]
        public async Task<IActionResult> TeamMemberDetails(int projectId, int teamMemberId)
        {
            var contact = await _context.ProjectContacts
                .Include(pc => pc.Project)
                .Include(pc => pc.User)
                .FirstOrDefaultAsync(pc => pc.Id == teamMemberId && pc.ProjectId == projectId);

            if (contact == null || contact.Project.IsDeleted)
            {
                return NotFound();
            }

            var viewModel = new ProjectTeamMemberDetailsViewModel
            {
                ProjectId = contact.ProjectId,
                TeamMemberId = contact.Id,
                ProjectTitle = contact.Project.Title,
                Name = contact.Name,
                Email = contact.Email,
                Role = contact.Role,
                FundingArrangement = contact.FundingArrangement,
                EmploymentType = contact.EmploymentType,
                TeamStatus = string.IsNullOrWhiteSpace(contact.TeamStatus) ? "current" : contact.TeamStatus,
                LeaveReason = contact.LeaveReason,
                AddedAt = contact.CreatedAt,
                LeftAt = contact.LeftAt,
                JobTitle = contact.User?.JobTitle
            };

            return View("TeamMemberDetails", viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveTeamMember(ProjectTeamMemberRemovalInputModel input)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please provide a reason for removing this person from the team.";
                return RedirectToAction(nameof(TeamMemberDetails), new { projectId = input.ProjectId, teamMemberId = input.TeamMemberId });
            }

            var contact = await _context.ProjectContacts
                .FirstOrDefaultAsync(pc => pc.Id == input.TeamMemberId && pc.ProjectId == input.ProjectId);

            if (contact == null)
            {
                return NotFound();
            }

            contact.TeamStatus = "previous";
            contact.LeaveReason = input.Reason.Trim();
            contact.LeftAt = DateTime.UtcNow;
            contact.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Team member marked as previous.";
            return RedirectToAction(nameof(TeamMemberDetails), new { projectId = input.ProjectId, teamMemberId = input.TeamMemberId });
        }

        private async Task EnsureContactUserAsync(ProjectContact contact)
        {
            if (contact.UserId.HasValue || string.IsNullOrWhiteSpace(contact.Email))
            {
                return;
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == contact.Email);
            if (user != null)
            {
                contact.UserId = user.Id;
                contact.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        private IReadOnlyList<SelectListItem> BuildEmploymentTypeOptions(string? selected)
        {
            return new List<SelectListItem>
            {
                new("Permanent", "Permanent", string.Equals(selected, "Permanent", StringComparison.OrdinalIgnoreCase)),
                new("MSP", "MSP", string.Equals(selected, "MSP", StringComparison.OrdinalIgnoreCase))
            };
        }

        private IReadOnlyList<SelectListItem> BuildTeamStatusOptions(string? selected)
        {
            return new List<SelectListItem>
            {
                new("Current", "current", string.Equals(selected, "current", StringComparison.OrdinalIgnoreCase)),
                new("Previous", "previous", string.Equals(selected, "previous", StringComparison.OrdinalIgnoreCase))
            };
        }

        private static string NormalizeEmploymentType(string? value)
            => string.Equals(value, "MSP", StringComparison.OrdinalIgnoreCase) ? "MSP" : "Permanent";

        private static string NormalizeTeamStatus(string? value)
            => string.Equals(value, "previous", StringComparison.OrdinalIgnoreCase) ? "previous" : "current";

        private ProjectTeamMemberFormViewModel BuildTeamMemberFormViewModel(ProjectSummaryViewModel summary, ProjectContact? member)
        {
            var input = new ProjectTeamMemberInputModel
            {
                ProjectId = summary.Id,
                TeamMemberId = member?.Id,
                UserId = member?.UserId,
                Role = member?.Role ?? string.Empty,
                FundingArrangement = member?.FundingArrangement ?? "Not specified",
                EmploymentType = member?.EmploymentType ?? "Permanent",
                TeamStatus = member?.TeamStatus ?? "current",
                LeaveReason = member?.LeaveReason
            };

            return new ProjectTeamMemberFormViewModel
            {
                Input = input,
                ProjectSummary = summary,
                ProjectTitle = summary.Title,
                EmploymentTypeOptions = BuildEmploymentTypeOptions(input.EmploymentType),
                TeamStatusOptions = BuildTeamStatusOptions(input.TeamStatus),
                SelectedUserName = member?.User?.Name ?? member?.Name,
                SelectedUserEmail = member?.User?.Email ?? member?.Email
            };
        }

        private async Task<ProjectTeamMemberFormViewModel?> BuildTeamMemberFormViewModelAsync(ProjectTeamMemberInputModel input)
        {
            var summary = await GetProjectSummaryAsync(input.ProjectId);
            if (summary == null)
            {
                return null;
            }

            string? selectedName = null;
            string? selectedEmail = null;
            if (input.UserId.HasValue)
            {
                var user = await _context.Users.FindAsync(input.UserId.Value);
                if (user != null)
                {
                    selectedName = user.Name;
                    selectedEmail = user.Email;
                }
            }

            return new ProjectTeamMemberFormViewModel
            {
                Input = input,
                ProjectSummary = summary,
                ProjectTitle = summary.Title,
                EmploymentTypeOptions = BuildEmploymentTypeOptions(input.EmploymentType),
                TeamStatusOptions = BuildTeamStatusOptions(input.TeamStatus),
                SelectedUserName = selectedName,
                SelectedUserEmail = selectedEmail
            };
        }

        // GET: Project/Create
    public async Task<IActionResult> Create()
    {
        ViewBag.Missions = await _context.Missions
            .Where(m => !m.IsDeleted)
            .OrderBy(m => m.Title)
            .Select(m => new { m.Id, m.Title })
            .ToListAsync();

        ViewBag.FundingSources = await _context.FundingSources
            .Where(fs => fs.IsActive)
            .OrderBy(fs => fs.SortOrder)
            .ThenBy(fs => fs.Name)
            .Select(fs => new { fs.Id, fs.Name })
            .ToListAsync();

        // Get business areas and phases from CMS
        ViewBag.BusinessAreas = await _productsApiService.GetBusinessAreasAsync();

        ViewBag.Phases = await _productsApiService.GetPhasesAsync();

            ViewBag.DeliveryPriorities = await _context.DeliveryPriorities
                .Where(dp => dp.IsActive)
                .OrderBy(dp => dp.SortOrder)
                .ThenBy(dp => dp.Name)
                .ToListAsync();

        // Get objectives grouped by theme
        var objectives = await _context.Objectives
            .Where(o => !o.IsDeleted && o.Status == "active")
            .OrderBy(o => o.Theme)
            .ThenBy(o => o.Title)
            .Select(o => new ObjectiveDto { Id = o.Id, Title = o.Title, Description = o.Description, Theme = o.Theme })
            .ToListAsync();

        var objectivesByTheme = objectives
            .GroupBy(o => o.Theme ?? "Other")
            .ToDictionary(g => g.Key, g => g.ToList());

        ViewBag.Objectives = objectivesByTheme;

        // Get organizational groups
        ViewBag.OrganizationalGroups = await _context.OrganizationalGroups
            .Where(g => g.IsActive)
            .OrderBy(g => g.SortOrder)
            .ThenBy(g => g.Name)
            .ToListAsync();

        ViewBag.PrimaryContactUser = null;

        return View(new Project());
    }

        // GET: Project/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var project = await _context.Projects.FindAsync(id);
            if (project == null || project.IsDeleted)
            {
                return NotFound();
            }

            // Get phases from CMS for dropdown
            ViewBag.Phases = await _productsApiService.GetPhasesAsync();

            return View(project);
        }

        // POST: Project/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,BusinessArea,HistoricBuRTId,Aim,Phase,IsMultiDepartmentProject,OtherDepartments")] Project project)
        {
            if (id != project.Id)
            {
                return NotFound();
            }

            // Log the Phase value received
            _logger.LogInformation("Edit POST - Project ID: {ProjectId}, Phase received: {Phase}, Form Phase value: {FormPhase}", 
                id, project?.Phase, Request.Form["Phase"].ToString());

            // Get Phase directly from form if model binding didn't work
            var phaseFromForm = Request.Form["Phase"].ToString();
            if (string.IsNullOrEmpty(project.Phase) && !string.IsNullOrEmpty(phaseFromForm))
            {
                project.Phase = phaseFromForm;
                _logger.LogInformation("Phase value taken from form directly: {Phase}", phaseFromForm);
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingProject = await _context.Projects.FindAsync(id);
                    if (existingProject == null || existingProject.IsDeleted)
                    {
                        return NotFound();
                    }

                    _logger.LogInformation("Updating project {ProjectId} - Old Phase: {OldPhase}, New Phase: {NewPhase}", 
                        id, existingProject.Phase, project.Phase);

                    existingProject.Title = project.Title;
                    existingProject.BusinessArea = project.BusinessArea;
                    existingProject.HistoricBuRTId = project.HistoricBuRTId;
                    existingProject.Aim = project.Aim;
                    existingProject.Phase = project.Phase;
                    existingProject.IsMultiDepartmentProject = project.IsMultiDepartmentProject;
                    existingProject.OtherDepartments = project.OtherDepartments;
                    existingProject.UpdatedAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Project {ProjectId} updated successfully. Phase is now: {Phase}", id, existingProject.Phase);

                    TempData["SuccessMessage"] = "Project updated successfully.";
                    return RedirectToAction(nameof(Details), new { id = id });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating project {ProjectId}", id);
                    TempData["ErrorMessage"] = "An error occurred while updating the project.";
                }
            }
            else
            {
                // Log model state errors
                foreach (var error in ModelState)
                {
                    _logger.LogWarning("ModelState error for {Key}: {Errors}", 
                        error.Key, string.Join(", ", error.Value.Errors.Select(e => e.ErrorMessage)));
                }
            }

            // Reload phases if there's an error
            ViewBag.Phases = await _productsApiService.GetPhasesAsync();

            return View(project);
        }

        // POST: Project/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Aim,StartDate,TargetDeliveryDate,IsFlagship,IsAiInitiative,RagStatus,RagJustification,Phase,BusinessArea,HistoricBuRTId,TotalPermFte,TotalMspFte,Status,IsMultiDepartmentProject,OtherDepartments,DeliveryPriorityId,PrimaryContactUserId")] Project project)
        {
            _logger.LogInformation("Create POST called - Title: {Title}, Aim: {Aim}", project.Title, project.Aim);
            _logger.LogInformation("ModelState.IsValid: {IsValid}", ModelState.IsValid);
            
            // Log all form data
            _logger.LogInformation("Form data received:");
            foreach (var key in Request.Form.Keys)
            {
                _logger.LogInformation("  {Key}: {Value}", key, Request.Form[key]);
            }
            
            // Additional server-side validation for required fields
            if (string.IsNullOrWhiteSpace(project.Aim))
            {
                ModelState.AddModelError(nameof(project.Aim), "The Project Aim field is required.");
            }
            
            if (!project.StartDate.HasValue)
            {
                ModelState.AddModelError(nameof(project.StartDate), "The Start Date field is required.");
            }
            
            if (string.IsNullOrWhiteSpace(project.RagStatus))
            {
                ModelState.AddModelError(nameof(project.RagStatus), "The RAG Status field is required.");
            }
            
            if (string.IsNullOrWhiteSpace(project.Status))
            {
                ModelState.AddModelError(nameof(project.Status), "The Project Status field is required.");
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState is invalid. Errors: {Errors}", 
                    string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Generate project code
                    var lastProject = await _context.Projects
                        .OrderByDescending(p => p.ProjectCode)
                        .FirstOrDefaultAsync();
                    
                    int nextNumber = 1;
                    if (lastProject != null && !string.IsNullOrEmpty(lastProject.ProjectCode))
                    {
                        var parts = lastProject.ProjectCode.Split('-');
                        if (parts.Length >= 2 && int.TryParse(parts.Last(), out int lastNumber))
                        {
                            nextNumber = lastNumber + 1;
                        }
                    }
                    
                    project.ProjectCode = $"DDTDEL-{nextNumber:D4}";
                    project.CreatedAt = DateTime.UtcNow;
                    project.UpdatedAt = DateTime.UtcNow;

                    _logger.LogInformation("Adding project to context with code: {ProjectCode}", project.ProjectCode);
                    _context.Projects.Add(project);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Project saved successfully with ID: {ProjectId}", project.Id);

                    // Handle mission relationships
                    var selectedMissionIds = Request.Form["SelectedMissionIds"].Where(x => !string.IsNullOrEmpty(x)).Select(int.Parse).ToList();
                    foreach (var missionId in selectedMissionIds)
                    {
                        var mission = await _context.Missions.FindAsync(missionId);
                        if (mission != null)
                        {
                            project.ProjectMissions.Add(new ProjectMission
                            {
                                MissionId = missionId,
                                Mission = mission
                            });
                        }
                    }

                    // Handle objective relationships
                    var selectedObjectiveIds = Request.Form["SelectedObjectiveIds"].Where(x => !string.IsNullOrEmpty(x)).Select(int.Parse).ToList();
                    foreach (var objectiveId in selectedObjectiveIds)
                    {
                        var objective = await _context.Objectives.FindAsync(objectiveId);
                        if (objective != null)
                        {
                            project.ProjectObjectives.Add(new ProjectObjective
                            {
                                ObjectiveId = objectiveId,
                                Objective = objective
                            });
                        }
                    }

                    // Handle funding allocations
                    var fundingSourceIds = Request.Form["FundingAllocations[].FundingSourceId"].Where(x => !string.IsNullOrEmpty(x)).ToList();
                    var allocationPercentages = Request.Form["FundingAllocations[].AllocationPercentage"].Where(x => !string.IsNullOrEmpty(x)).ToList();
                    var fundingNotes = Request.Form["FundingAllocations[].Notes"].ToList();

                    for (int i = 0; i < fundingSourceIds.Count; i++)
                    {
                        if (int.TryParse(fundingSourceIds[i], out int sourceId) && 
                            decimal.TryParse(allocationPercentages[i], out decimal percentage))
                        {
                            var fundingSource = await _context.FundingSources.FindAsync(sourceId);
                            if (fundingSource != null)
                            {
                                project.FundingAllocations.Add(new ProjectFundingAllocation
                                {
                                    FundingSourceId = sourceId,
                                    FundingSource = fundingSource,
                                    AllocationPercentage = percentage,
                                    Notes = i < fundingNotes.Count ? fundingNotes[i] : ""
                                });
                            }
                        }
                    }

                    // Handle outcomes
                    var outcomeTexts = Request.Form["Outcomes[].Outcome"].Where(x => !string.IsNullOrEmpty(x)).ToList();
                    var measuresOfSuccess = Request.Form["Outcomes[].MeasureOfSuccess"].ToList();
                    var confidenceLevels = Request.Form["Outcomes[].ConfidenceLevel"].ToList();
                    var confidenceExplanations = Request.Form["Outcomes[].ConfidenceExplanation"].ToList();

                    for (int i = 0; i < outcomeTexts.Count; i++)
                    {
                        project.Outcomes.Add(new ProjectOutcome
                        {
                            Outcome = outcomeTexts[i],
                            MeasureOfSuccess = i < measuresOfSuccess.Count ? measuresOfSuccess[i] : "",
                            ConfidenceLevel = i < confidenceLevels.Count ? confidenceLevels[i] : "Medium",
                            ConfidenceExplanation = i < confidenceExplanations.Count ? confidenceExplanations[i] : "",
                            SortOrder = i + 1
                        });
                    }

                    // Handle contacts
                    var roles = Request.Form["Contacts[].Role"].ToList();
                    var names = Request.Form["Contacts[].Name"].ToList();
                    var emails = Request.Form["Contacts[].Email"].ToList();
                    var roleDescriptions = Request.Form["Contacts[].RoleDescription"].ToList();

                    for (int i = 0; i < names.Count; i++)
                    {
                        if (!string.IsNullOrWhiteSpace(names[i]))
                        {
                            var role = i < roles.Count ? roles[i] : "";
                            project.ProjectContacts.Add(new ProjectContact
                            {
                                Role = string.IsNullOrWhiteSpace(role) ? "Not specified" : role,
                                Name = names[i],
                                Email = i < emails.Count ? emails[i] : "",
                                RoleDescription = i < roleDescriptions.Count ? roleDescriptions[i] : "",
                                SortOrder = i + 1,
                                FundingArrangement = "Not specified",
                                EmploymentType = "Permanent",
                                TeamStatus = "current"
                            });
                        }
                    }

                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Project created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating project: {Message}", ex.Message);
                    TempData["ErrorMessage"] = $"An error occurred while creating the project: {ex.Message}";
                }
            }

            // If we get here, there was an error - reload the form data
            ViewBag.Missions = await _context.Missions
                .Where(m => !m.IsDeleted)
                .OrderBy(m => m.Title)
                .Select(m => new { m.Id, m.Title })
                .ToListAsync();

            ViewBag.FundingSources = await _context.FundingSources
                .Where(fs => fs.IsActive)
                .OrderBy(fs => fs.SortOrder)
                .ThenBy(fs => fs.Name)
                .Select(fs => new { fs.Id, fs.Name })
                .ToListAsync();

            // Get business areas and phases from CMS
            ViewBag.BusinessAreas = await _productsApiService.GetBusinessAreasAsync();

            ViewBag.Phases = await _productsApiService.GetPhasesAsync();

            ViewBag.DeliveryPriorities = await _context.DeliveryPriorities
                .Where(dp => dp.IsActive)
                .OrderBy(dp => dp.SortOrder)
                .ThenBy(dp => dp.Name)
                .ToListAsync();

            // Get objectives grouped by theme
            var objectives = await _context.Objectives
                .Where(o => !o.IsDeleted && o.Status == "active")
                .OrderBy(o => o.Theme)
                .ThenBy(o => o.Title)
                .Select(o => new ObjectiveDto { Id = o.Id, Title = o.Title, Description = o.Description, Theme = o.Theme })
                .ToListAsync();

            var objectivesByTheme = objectives
                .GroupBy(o => o.Theme ?? "Other")
                .ToDictionary(g => g.Key, g => g.ToList());

            ViewBag.Objectives = objectivesByTheme;

            ViewBag.PrimaryContactUser = project.PrimaryContactUserId.HasValue
                ? await _context.Users.FindAsync(project.PrimaryContactUserId.Value)
                : null;

            return View(project);
        }

        // POST: Project/AddOutcome
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddOutcome([Bind(Prefix = "Input")] ProjectOutcomeInputModel input)
        {
            if (!ModelState.IsValid)
            {
                var projectSummary = await GetProjectSummaryAsync(input.ProjectId);

                if (projectSummary == null)
                {
                    return NotFound();
                }

                var invalidViewModel = BuildOutcomeFormViewModel(projectSummary, input, showDeleteButton: false);
                return View("CreateOutcome", invalidViewModel);
            }

            try
            {
                var project = await _context.Projects
                    .Include(p => p.Outcomes)
                    .FirstOrDefaultAsync(p => p.Id == input.ProjectId && !p.IsDeleted);

                if (project == null)
                {
                    return NotFound();
                }

                var maxSortOrder = project.Outcomes.Any() ? project.Outcomes.Max(o => o.SortOrder) : 0;

                var projectOutcome = new ProjectOutcome
                {
                    ProjectId = input.ProjectId,
                    Outcome = input.Outcome,
                    MeasureOfSuccess = input.MeasureOfSuccess,
                    ConfidenceLevel = input.ConfidenceLevel,
                    ConfidenceExplanation = input.ConfidenceExplanation,
                    SortOrder = maxSortOrder + 1,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.ProjectOutcomes.Add(projectOutcome);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Outcome added successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding outcome to project {ProjectId}", input.ProjectId);
                TempData["ErrorMessage"] = "Error adding outcome. Please try again.";
            }

            return RedirectToAction(nameof(Details), new { id = input.ProjectId, tab = "outcomes" });
        }

        // POST: Project/UpdateOutcome
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOutcome([Bind(Prefix = "Input")] ProjectOutcomeInputModel input)
        {
            if (!input.OutcomeId.HasValue)
            {
                return NotFound();
            }

            var projectOutcome = await _context.ProjectOutcomes
                .Include(o => o.Project)
                .FirstOrDefaultAsync(o => o.Id == input.OutcomeId.Value && o.ProjectId == input.ProjectId);

            if (projectOutcome == null || projectOutcome.Project.IsDeleted)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                var invalidViewModel = BuildOutcomeFormViewModel(CreateProjectSummary(projectOutcome.Project), input, showDeleteButton: true);
                return View("EditOutcome", invalidViewModel);
            }

            try
            {
                projectOutcome.Outcome = input.Outcome;
                projectOutcome.MeasureOfSuccess = input.MeasureOfSuccess;
                projectOutcome.ConfidenceLevel = input.ConfidenceLevel;
                projectOutcome.ConfidenceExplanation = input.ConfidenceExplanation;
                projectOutcome.SortOrder = input.SortOrder;
                projectOutcome.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Outcome updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating outcome {OutcomeId}", input.OutcomeId);
                TempData["ErrorMessage"] = "Error updating outcome. Please try again.";
            }

            return RedirectToAction(nameof(Details), new { id = input.ProjectId, tab = "outcomes" });
        }

        // POST: Project/UpdateRagStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRagStatus(int projectId, string ragStatus, string ragJustification)
        {
            try
            {
                var project = await _context.Projects
                    .Include(p => p.RagHistory)
                    .FirstOrDefaultAsync(p => p.Id == projectId && !p.IsDeleted);

                if (project == null)
                {
                    return NotFound();
                }

                // Only create history entry if status has changed
                if (project.RagStatus != ragStatus)
                {
                    var ragHistory = new ProjectRagHistory
                    {
                        ProjectId = projectId,
                        RagStatus = ragStatus,
                        Justification = ragJustification,
                        PathToGreen = project.PathToGreen, // Capture current path to green
                        ChangedAt = DateTime.UtcNow,
                        ChangedByEmail = User.Identity?.Name ?? "Unknown",
                        ChangedByName = User.Identity?.Name ?? "Unknown"
                    };
                    _context.ProjectRagHistories.Add(ragHistory);
                }

                // Update current project status
                project.RagStatus = ragStatus;
                project.RagJustification = ragJustification;
                project.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "RAG status updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating RAG status for project {ProjectId}", projectId);
                TempData["ErrorMessage"] = "Error updating RAG status. Please try again.";
            }

            return RedirectToAction(nameof(Details), new { id = projectId, tab = "rag" });
        }

        // POST: Project/UpdatePathToGreen
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePathToGreen(int projectId, string pathToGreen)
        {
            try
            {
                var project = await _context.Projects
                    .FirstOrDefaultAsync(p => p.Id == projectId && !p.IsDeleted);

                if (project == null)
                {
                    return NotFound();
                }

                // Create history entry to track path to green change
                if (project.PathToGreen != pathToGreen)
                {
                    var ragHistory = new ProjectRagHistory
                    {
                        ProjectId = projectId,
                        RagStatus = project.RagStatus,
                        Justification = project.RagJustification,
                        PathToGreen = pathToGreen, // Record new path to green
                        ChangedAt = DateTime.UtcNow,
                        ChangedByEmail = User.Identity?.Name ?? "Unknown",
                        ChangedByName = User.Identity?.Name ?? "Unknown"
                    };
                    _context.ProjectRagHistories.Add(ragHistory);
                }

                project.PathToGreen = pathToGreen;
                project.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Path to green updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating path to green for project {ProjectId}", projectId);
                TempData["ErrorMessage"] = "Error updating path to green. Please try again.";
            }

            return RedirectToAction(nameof(Details), new { id = projectId, tab = "rag" });
        }

        // POST: Project/DeleteRagHistory
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRagHistory(int projectId, int ragHistoryId)
        {
            try
            {
                var ragHistory = await _context.ProjectRagHistories
                    .FirstOrDefaultAsync(rh => rh.Id == ragHistoryId && rh.ProjectId == projectId);

                if (ragHistory == null)
                {
                    TempData["ErrorMessage"] = "RAG history entry not found.";
                    return RedirectToAction(nameof(Details), new { id = projectId, tab = "rag" });
                }

                _context.ProjectRagHistories.Remove(ragHistory);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "RAG history entry deleted successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting RAG history {RagHistoryId}", ragHistoryId);
                TempData["ErrorMessage"] = "Error deleting RAG history entry. Please try again.";
            }

            return RedirectToAction(nameof(Details), new { id = projectId, tab = "rag" });
        }

        // POST: Project/UpdateFunding
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateFunding(int projectId, string resourceType, decimal count, decimal programmePercentage, decimal adminPercentage, string? notes)
        {
            try
            {
                // Validate percentages
                if (programmePercentage + adminPercentage > 100)
                {
                    TempData["ErrorMessage"] = "Programme and Admin percentages cannot exceed 100% in total.";
                    return RedirectToAction(nameof(Details), new { id = projectId, tab = "overview" });
                }

                var project = await _context.Projects
                    .Include(p => p.ResourceFunding)
                    .FirstOrDefaultAsync(p => p.Id == projectId && !p.IsDeleted);

                if (project == null)
                {
                    return NotFound();
                }

                // Update the project's FTE count
                if (resourceType == "Permanent")
                {
                    project.TotalPermFte = count;
                }
                else if (resourceType == "MSP")
                {
                    project.TotalMspFte = count;
                }

                project.UpdatedAt = DateTime.UtcNow;

                // Find or create the resource funding record
                var resourceFunding = project.ResourceFunding
                    .FirstOrDefault(rf => rf.ResourceType == resourceType);

                if (resourceFunding == null)
                {
                    // Create new funding record
                    resourceFunding = new ProjectResourceFunding
                    {
                        ProjectId = projectId,
                        ResourceType = resourceType,
                        ProgrammeFundedPercentage = programmePercentage,
                        AdminFundedPercentage = adminPercentage,
                        Notes = notes,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.ProjectResourceFundings.Add(resourceFunding);
                }
                else
                {
                    // Update existing funding record
                    resourceFunding.ProgrammeFundedPercentage = programmePercentage;
                    resourceFunding.AdminFundedPercentage = adminPercentage;
                    resourceFunding.Notes = notes;
                    resourceFunding.UpdatedAt = DateTime.UtcNow;
                }

                // Create funding history record
                var fundingHistory = new ProjectResourceFundingHistory
                {
                    ProjectId = projectId,
                    ResourceType = resourceType,
                    Count = count,
                    ProgrammeFundedPercentage = programmePercentage,
                    AdminFundedPercentage = adminPercentage,
                    Notes = notes,
                    ChangedAt = DateTime.UtcNow,
                    ChangedByEmail = User.Identity?.Name ?? "system",
                    ChangedByName = User.Identity?.Name ?? "System"
                };
                _context.ProjectResourceFundingHistories.Add(fundingHistory);

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"{resourceType} funding allocation updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating funding for project {ProjectId}", projectId);
                TempData["ErrorMessage"] = "Error updating funding allocation. Please try again.";
            }

            return RedirectToAction(nameof(Details), new { id = projectId, tab = "overview" });
        }

        // POST: Project/DeleteOutcome
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteOutcome(int projectId, int outcomeId)
        {
            try
            {
                var projectOutcome = await _context.ProjectOutcomes
                    .FirstOrDefaultAsync(o => o.Id == outcomeId && o.ProjectId == projectId);

                if (projectOutcome == null)
                {
                    return NotFound();
                }

                _context.ProjectOutcomes.Remove(projectOutcome);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Outcome deleted successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting outcome {OutcomeId}", outcomeId);
                TempData["ErrorMessage"] = "Error deleting outcome. Please try again.";
            }

            return RedirectToAction(nameof(Details), new { id = projectId, tab = "outcomes" });
        }

        // GET: Project/OutcomeDetails/5
        public async Task<IActionResult> OutcomeDetails(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var outcome = await _context.ProjectOutcomes
                .Include(o => o.Project)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (outcome == null)
            {
                return NotFound();
            }

            return View(outcome);
        }

        // GET: Project/MilestoneDetails/5
        public async Task<IActionResult> MilestoneDetails(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var milestone = await _context.Milestones
                .Include(m => m.Project)
            .Include(m => m.MilestoneUpdates)
            .Include(m => m.MilestoneActions)
                .ThenInclude(ma => ma.Action)
            .Include(m => m.MilestoneRisks)
                .ThenInclude(mr => mr.Risk)
            .Include(m => m.MilestoneIssues)
                .ThenInclude(mi => mi.Issue)
            .FirstOrDefaultAsync(m => m.Id == id);

            if (milestone == null)
            {
                return NotFound();
            }

        var linkedDecisions = new List<Decision>();
        if (milestone.ProjectId.HasValue)
        {
            linkedDecisions = await _context.Decisions
                .Include(d => d.OwnerUser)
                .Include(d => d.Actions)
                    .ThenInclude(a => a.MilestoneActions)
                .Where(d => d.ProjectId == milestone.ProjectId.Value && !d.IsDeleted)
                .Where(d => d.Actions.Any(a => !a.IsDeleted && a.MilestoneActions.Any(ma => ma.MilestoneId == milestone.Id)))
                .OrderByDescending(d => d.DecisionDate ?? d.CreatedAt)
                .ToListAsync();
        }

        ViewBag.LinkedDecisions = linkedDecisions;

            return View(milestone);
        }

        // POST: Project/AddMilestone
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMilestone(int projectId, string name, string? description, DateTime dueDate, string status, int? objectiveId)
        {
            try
            {
                var project = await _context.Projects
                    .FirstOrDefaultAsync(p => p.Id == projectId && !p.IsDeleted);

                if (project == null)
                {
                    return NotFound();
                }

                int? resolvedObjectiveId = null;
                if (objectiveId.HasValue)
                {
                    var objectiveLinked = await _context.ProjectObjectives
                        .AnyAsync(po => po.ProjectId == projectId && po.ObjectiveId == objectiveId.Value);

                    if (!objectiveLinked)
                    {
                        TempData["ErrorMessage"] = "The selected objective is not linked to this project.";
                        return RedirectToAction(nameof(Details), new { id = projectId, tab = "milestones" });
                    }

                    resolvedObjectiveId = objectiveId.Value;
                }

                var milestone = new Milestone
                {
                    ProjectId = projectId,
                    ObjectiveId = resolvedObjectiveId,
                    Name = name,
                    Description = description,
                    DueDate = dueDate,
                    Status = status,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Milestones.Add(milestone);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Milestone added successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding milestone to project {ProjectId}", projectId);
                TempData["ErrorMessage"] = "Error adding milestone. Please try again.";
            }

            return RedirectToAction(nameof(Details), new { id = projectId, tab = "milestones" });
        }

        // POST: Project/UpdateMilestone
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateMilestone(int projectId, int milestoneId, string name, string? description, DateTime dueDate, string status, DateTime? actualDate, int? progressPercent, int? objectiveId)
        {
            try
            {
                var milestone = await _context.Milestones
                    .FirstOrDefaultAsync(m => m.Id == milestoneId && m.ProjectId == projectId);

                if (milestone == null)
                {
                    return NotFound();
                }

                int? resolvedObjectiveId = null;
                if (objectiveId.HasValue)
                {
                    var objectiveLinked = await _context.ProjectObjectives
                        .AnyAsync(po => po.ProjectId == projectId && po.ObjectiveId == objectiveId.Value);

                    if (!objectiveLinked)
                    {
                        TempData["ErrorMessage"] = "The selected objective is not linked to this project.";
                        return RedirectToAction(nameof(Details), new { id = projectId, tab = "milestones" });
                    }

                    resolvedObjectiveId = objectiveId.Value;
                }

                milestone.Name = name;
                milestone.Description = description;
                milestone.DueDate = dueDate;
                milestone.Status = status;
                milestone.ActualDate = actualDate;
                milestone.ProgressPercent = progressPercent;
                milestone.ObjectiveId = resolvedObjectiveId;
                milestone.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Milestone updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating milestone {MilestoneId}", milestoneId);
                TempData["ErrorMessage"] = "Error updating milestone. Please try again.";
            }

            return RedirectToAction(nameof(Details), new { id = projectId, tab = "milestones" });
        }

        // POST: Project/DeleteMilestone
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMilestone(int projectId, int milestoneId)
        {
            try
            {
                var milestone = await _context.Milestones
                    .FirstOrDefaultAsync(m => m.Id == milestoneId && m.ProjectId == projectId);

                if (milestone == null)
                {
                    return NotFound();
                }

                milestone.IsDeleted = true;
                milestone.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Milestone deleted successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting milestone {MilestoneId}", milestoneId);
                TempData["ErrorMessage"] = "Error deleting milestone. Please try again.";
            }

            return RedirectToAction(nameof(Details), new { id = projectId, tab = "milestones" });
        }

        // POST: Project/UpdateTitle
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateTitle(int id, string title)
        {
            try
            {
                var project = await _context.Projects.FindAsync(id);
                if (project == null || project.IsDeleted)
                {
                    TempData["ErrorMessage"] = "Project not found.";
                    return RedirectToAction(nameof(Index));
                }

                project.Title = title;
                project.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Project title updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating project title");
                TempData["ErrorMessage"] = "An error occurred while updating the project title.";
            }

            return RedirectToAction(nameof(Details), new { id = id, tab = "settings" });
        }

    // POST: Project/UpdateDeliveryPriority
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateDeliveryPriority(int id, int? deliveryPriorityId)
    {
        try
        {
            var project = await _context.Projects.FindAsync(id);
            if (project == null || project.IsDeleted)
            {
                TempData["ErrorMessage"] = "Project not found.";
                return RedirectToAction(nameof(Index));
            }

            if (deliveryPriorityId.HasValue)
            {
                var exists = await _context.DeliveryPriorities
                    .AnyAsync(dp => dp.Id == deliveryPriorityId.Value && dp.IsActive);
                if (!exists)
                {
                    TempData["ErrorMessage"] = "Selected delivery priority is not available.";
                    return RedirectToAction(nameof(Details), new { id, tab = "overview" });
                }
            }

            project.DeliveryPriorityId = deliveryPriorityId;
            project.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Delivery priority updated successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating delivery priority for project {ProjectId}", id);
            TempData["ErrorMessage"] = "An error occurred while updating the delivery priority.";
        }

        return RedirectToAction(nameof(Details), new { id = id, tab = "overview" });
    }

        // POST: Project/UpdateStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status, string? statusChangeReason)
        {
            try
            {
                var project = await _context.Projects.FindAsync(id);
                if (project == null || project.IsDeleted)
                {
                    TempData["ErrorMessage"] = "Project not found.";
                    return RedirectToAction(nameof(Index));
                }

                project.Status = status;
                project.StatusChangeReason = statusChangeReason;
                project.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Project status updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating project status");
                TempData["ErrorMessage"] = "An error occurred while updating the project status.";
            }

            return RedirectToAction(nameof(Details), new { id = id, tab = "settings" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePrimaryContact(int id, int? primaryContactUserId)
        {
            try
            {
                var project = await _context.Projects
                    .Include(p => p.PrimaryContactUser)
                    .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

                if (project == null)
                {
                    TempData["ErrorMessage"] = "Project not found.";
                    return RedirectToAction(nameof(Index));
                }

                if (primaryContactUserId.HasValue)
                {
                    var contact = await _context.Users.FindAsync(primaryContactUserId.Value);
                    if (contact == null)
                    {
                        TempData["ErrorMessage"] = "Selected contact could not be found.";
                        return RedirectToAction(nameof(Details), new { id, tab = "overview" });
                    }
                }

                project.PrimaryContactUserId = primaryContactUserId;
                project.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = primaryContactUserId.HasValue
                    ? "Primary contact updated successfully."
                    : "Primary contact removed.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating primary contact for project {ProjectId}", id);
                TempData["ErrorMessage"] = "An error occurred while updating the primary contact.";
            }

            return RedirectToAction(nameof(Details), new { id, tab = "overview" });
        }

        // POST: Project/UpdateAim
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAim(int id, string? aim)
        {
            try
            {
                var project = await _context.Projects.FindAsync(id);
                if (project == null || project.IsDeleted)
                {
                    TempData["ErrorMessage"] = "Project not found.";
                    return RedirectToAction(nameof(Index));
                }

                project.Aim = aim;
                project.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Project aim updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating project aim");
                TempData["ErrorMessage"] = "An error occurred while updating the project aim.";
            }

            return RedirectToAction(nameof(Details), new { id = id, tab = "overview" });
        }

        // GET: Project/StrategicAlignment/5
        public async Task<IActionResult> StrategicAlignment(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var project = await _context.Projects
                .Include(p => p.ProjectMissions)
                    .ThenInclude(pm => pm.Mission)
                .Include(p => p.ProjectObjectives)
                    .ThenInclude(po => po.Objective)
                .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);

            if (project == null)
            {
                return NotFound();
            }

            return View(project);
        }

        // POST: Project/UpdateStrategicObjectives
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStrategicObjectives(int id, string? strategicObjectives)
        {
            try
            {
                var project = await _context.Projects.FindAsync(id);
                if (project == null || project.IsDeleted)
                {
                    TempData["ErrorMessage"] = "Project not found.";
                    return RedirectToAction(nameof(Index));
                }

                project.StrategicObjectives = strategicObjectives;
                project.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Strategic objectives updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating strategic objectives");
                TempData["ErrorMessage"] = "An error occurred while updating strategic objectives.";
            }

            return RedirectToAction(nameof(Details), new { id = id, tab = "strategicalignment" });
        }

        // POST: Project/UpdateStrategicObjectivesAndLinks
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStrategicObjectivesAndLinks(int id, string? strategicObjectivesText, List<int> selectedObjectiveIds)
        {
            try
            {
                var project = await _context.Projects
                    .Include(p => p.ProjectObjectives)
                    .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

                if (project == null)
                {
                    TempData["ErrorMessage"] = "Project not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Update narrative text
                project.StrategicObjectives = strategicObjectivesText;

                // Update linked objectives
                // Remove existing links
                _context.ProjectObjectives.RemoveRange(project.ProjectObjectives);

                // Add new links
                if (selectedObjectiveIds != null && selectedObjectiveIds.Any())
                {
                    foreach (var objectiveId in selectedObjectiveIds)
                    {
                        project.ProjectObjectives.Add(new ProjectObjective
                        {
                            ProjectId = id,
                            ObjectiveId = objectiveId
                        });
                    }
                }

                project.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Strategic objectives updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating strategic objectives and links");
                TempData["ErrorMessage"] = "An error occurred while updating strategic objectives.";
            }

            return RedirectToAction(nameof(Details), new { id = id, tab = "strategicalignment" });
        }

        // POST: Project/UpdateMissionPillars
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateMissionPillars(int id, string? missionPillars)
        {
            try
            {
                var project = await _context.Projects.FindAsync(id);
                if (project == null || project.IsDeleted)
                {
                    TempData["ErrorMessage"] = "Project not found.";
                    return RedirectToAction(nameof(Index));
                }

                project.MissionPillars = missionPillars;
                project.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Mission pillars updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating mission pillars");
                TempData["ErrorMessage"] = "An error occurred while updating mission pillars.";
            }

            return RedirectToAction(nameof(Details), new { id = id, tab = "strategicalignment" });
        }

        // POST: Project/UpdateMissionPillarsAndLinks
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateMissionPillarsAndLinks(int id, string? missionPillarsText, List<int> selectedMissionIds)
        {
            try
            {
                var project = await _context.Projects
                    .Include(p => p.ProjectMissions)
                    .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

                if (project == null)
                {
                    TempData["ErrorMessage"] = "Project not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Update narrative text
                project.MissionPillars = missionPillarsText;

                // Update linked missions
                // Remove existing links
                _context.ProjectMissions.RemoveRange(project.ProjectMissions);

                // Add new links
                if (selectedMissionIds != null && selectedMissionIds.Any())
                {
                    foreach (var missionId in selectedMissionIds)
                    {
                        project.ProjectMissions.Add(new ProjectMission
                        {
                            ProjectId = id,
                            MissionId = missionId
                        });
                    }
                }

                project.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Mission pillars updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating mission pillars and links");
                TempData["ErrorMessage"] = "An error occurred while updating mission pillars.";
            }

            return RedirectToAction(nameof(Details), new { id = id, tab = "strategicalignment" });
        }

        // POST: Project/UpdateMultiDepartmentCooperation
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateMultiDepartmentCooperation(int id, bool isMultiDepartmentProject, string? otherDepartments)
        {
            try
            {
                var project = await _context.Projects
                    .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

                if (project == null)
                {
                    TempData["ErrorMessage"] = "Project not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Update multi-department status
                project.IsMultiDepartmentProject = isMultiDepartmentProject;

                // Update other departments list
                if (isMultiDepartmentProject && !string.IsNullOrWhiteSpace(otherDepartments))
                {
                    project.OtherDepartments = otherDepartments;
                }
                else
                {
                    project.OtherDepartments = null;
                }

                project.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Multi-department cooperation updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating multi-department cooperation");
                TempData["ErrorMessage"] = "An error occurred while updating multi-department cooperation.";
            }

            return RedirectToAction(nameof(Details), new { id = id, tab = "strategicalignment" });
        }

        // GET: Project/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var project = await _context.Projects
                .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);

            if (project == null)
            {
                return NotFound();
            }

            return View(project);
        }

        // POST: Project/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var project = await _context.Projects.FindAsync(id);
            if (project != null)
            {
                project.IsDeleted = true;
                project.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Project deleted successfully.";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Project/AddIssue
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddIssue([Bind(Prefix = "Input")] ProjectIssueInputModel input)
        {
            input.LinkedActionIds = input.LinkedActionIds?.Where(id => id > 0).Distinct().ToList() ?? new List<int>();

            input.Title = SanitiseText(input.Title) ?? string.Empty;
            input.Description = SanitiseText(input.Description);
            input.Severity = string.IsNullOrWhiteSpace(input.Severity) ? IssueSeverityValues[1] : input.Severity.Trim();
            input.Status = string.IsNullOrWhiteSpace(input.Status) ? IssueStatusValues[0] : input.Status.Trim();
            input.Category = SanitiseText(input.Category);
            input.Workaround = SanitiseText(input.Workaround);
            input.ResolutionSummary = SanitiseText(input.ResolutionSummary);
            input.FipsId = SanitiseText(input.FipsId);
            input.BusinessArea = SanitiseText(input.BusinessArea);
            input.SourceType = SanitiseText(input.SourceType);
            input.SourceReference = SanitiseText(input.SourceReference);
            input.SourceRecordUrl = SanitiseText(input.SourceRecordUrl);
            input.OwnerEmail = SanitiseText(input.OwnerEmail);

            if (string.IsNullOrWhiteSpace(input.Title))
            {
                ModelState.AddModelError(nameof(ProjectIssueInputModel.Title), "Enter an issue title.");
            }

            if (!IssueSeverityValues.Contains(input.Severity, StringComparer.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(ProjectIssueInputModel.Severity), "Select a valid severity.");
            }

            if (!IssueStatusValues.Contains(input.Status, StringComparer.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(ProjectIssueInputModel.Status), "Select a valid status.");
            }

            if (input.TargetResolutionDate.HasValue && input.TargetResolutionDate.Value < input.DetectedDate)
            {
                ModelState.AddModelError(nameof(ProjectIssueInputModel.TargetResolutionDate), "Target resolution date cannot be earlier than the detected date.");
            }

            var project = await GetProjectForIssueAsync(input.ProjectId);
            if (project == null)
            {
                return NotFound();
            }

            var duplicate = await _context.Issues
                .AnyAsync(i => i.ProjectId == input.ProjectId && !i.IsDeleted && i.Title.ToLower() == input.Title.ToLower());
            if (duplicate)
            {
                ModelState.AddModelError(nameof(ProjectIssueInputModel.Title), "An issue with this title already exists for this project.");
            }

            if (input.SourceRiskId.HasValue && !project.Risks.Any(r => r.Id == input.SourceRiskId.Value))
            {
                ModelState.AddModelError(nameof(ProjectIssueInputModel.SourceRiskId), "Select a risk from this project.");
            }

            var validActionIds = project.Actions.Select(a => a.Id).ToHashSet();
            if (input.LinkedActionIds.Any(id => !validActionIds.Contains(id)))
            {
                ModelState.AddModelError(nameof(ProjectIssueInputModel.LinkedActionIds), "Select actions from this project.");
            }

            if (!ModelState.IsValid)
            {
                var viewModel = await BuildIssueFormViewModelAsync(project, input, showDeleteButton: false);
                return View("CreateIssue", viewModel);
            }

            var issue = new Issue
            {
                ProjectId = input.ProjectId,
                Title = input.Title,
                Description = input.Description,
                Severity = input.Severity,
                Status = input.Status,
                DetectedDate = input.DetectedDate,
                TargetResolutionDate = input.TargetResolutionDate,
                Category = input.Category,
                Workaround = input.Workaround,
                ResolutionSummary = input.ResolutionSummary,
                FipsId = input.FipsId,
                BusinessArea = input.BusinessArea,
                SourceType = input.SourceType,
                SourceReference = input.SourceReference,
                SourceRecordUrl = input.SourceRecordUrl,
                SourceRiskId = input.SourceRiskId,
                BlockedFlag = string.Equals(input.Status, "blocked", StringComparison.OrdinalIgnoreCase),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            if (!string.IsNullOrWhiteSpace(input.OwnerEmail))
            {
                var owner = await FindUserByEmailAsync(input.OwnerEmail);
                issue.OwnerUserId = owner?.Id;
            }

            if (string.Equals(issue.Status, "resolved", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(issue.Status, "closed", StringComparison.OrdinalIgnoreCase))
            {
                issue.ClosedDate = DateTime.UtcNow;
            }

            _context.Issues.Add(issue);
            await _context.SaveChangesAsync();

            if (input.LinkedActionIds.Any())
            {
                foreach (var actionId in input.LinkedActionIds)
                {
                    _context.IssueActions.Add(new IssueAction
                    {
                        IssueId = issue.Id,
                        ActionId = actionId
                    });
                }
                await _context.SaveChangesAsync();
            }

            TempData["SuccessMessage"] = "Issue added successfully.";
            return RedirectToAction(nameof(Details), new { id = input.ProjectId, tab = "issues" });
        }

        // POST: Project/UpdateIssue
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateIssue([Bind(Prefix = "Input")] ProjectIssueInputModel input)
        {
            if (!input.IssueId.HasValue)
            {
                return BadRequest();
            }

            input.LinkedActionIds = input.LinkedActionIds?.Where(id => id > 0).Distinct().ToList() ?? new List<int>();

            input.Title = SanitiseText(input.Title) ?? string.Empty;
            input.Description = SanitiseText(input.Description);
            input.Severity = string.IsNullOrWhiteSpace(input.Severity) ? IssueSeverityValues[1] : input.Severity.Trim();
            input.Status = string.IsNullOrWhiteSpace(input.Status) ? IssueStatusValues[0] : input.Status.Trim();
            input.Category = SanitiseText(input.Category);
            input.Workaround = SanitiseText(input.Workaround);
            input.ResolutionSummary = SanitiseText(input.ResolutionSummary);
            input.FipsId = SanitiseText(input.FipsId);
            input.BusinessArea = SanitiseText(input.BusinessArea);
            input.SourceType = SanitiseText(input.SourceType);
            input.SourceReference = SanitiseText(input.SourceReference);
            input.SourceRecordUrl = SanitiseText(input.SourceRecordUrl);
            input.OwnerEmail = SanitiseText(input.OwnerEmail);

            if (string.IsNullOrWhiteSpace(input.Title))
            {
                ModelState.AddModelError(nameof(ProjectIssueInputModel.Title), "Enter an issue title.");
            }

            if (!IssueSeverityValues.Contains(input.Severity, StringComparer.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(ProjectIssueInputModel.Severity), "Select a valid severity.");
            }

            if (!IssueStatusValues.Contains(input.Status, StringComparer.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(ProjectIssueInputModel.Status), "Select a valid status.");
            }

            if (input.TargetResolutionDate.HasValue && input.TargetResolutionDate.Value < input.DetectedDate)
            {
                ModelState.AddModelError(nameof(ProjectIssueInputModel.TargetResolutionDate), "Target resolution date cannot be earlier than the detected date.");
            }

            var issue = await _context.Issues
                .Include(i => i.IssueActions)
                .FirstOrDefaultAsync(i => i.Id == input.IssueId.Value && i.ProjectId == input.ProjectId && !i.IsDeleted);

            if (issue == null)
            {
                return NotFound();
            }

            var project = await GetProjectForIssueAsync(input.ProjectId);
            if (project == null)
            {
                return NotFound();
            }

            if (input.SourceRiskId.HasValue && !project.Risks.Any(r => r.Id == input.SourceRiskId.Value))
            {
                ModelState.AddModelError(nameof(ProjectIssueInputModel.SourceRiskId), "Select a risk from this project.");
            }

            var validActionIds = project.Actions.Select(a => a.Id).ToHashSet();
            if (input.LinkedActionIds.Any(id => !validActionIds.Contains(id)))
            {
                ModelState.AddModelError(nameof(ProjectIssueInputModel.LinkedActionIds), "Select actions from this project.");
            }

            if (!ModelState.IsValid)
            {
                var viewModel = await BuildIssueFormViewModelAsync(project, input, showDeleteButton: true);
                return View("EditIssue", viewModel);
            }

            issue.Title = input.Title;
            issue.Description = input.Description;
            issue.Severity = input.Severity;
            issue.Status = input.Status;
            issue.DetectedDate = input.DetectedDate;
            issue.TargetResolutionDate = input.TargetResolutionDate;
            issue.Category = input.Category;
            issue.Workaround = input.Workaround;
            issue.ResolutionSummary = input.ResolutionSummary;
            issue.FipsId = input.FipsId;
            issue.BusinessArea = input.BusinessArea;
            issue.SourceType = input.SourceType;
            issue.SourceReference = input.SourceReference;
            issue.SourceRecordUrl = input.SourceRecordUrl;
            issue.SourceRiskId = input.SourceRiskId;
            issue.BlockedFlag = string.Equals(input.Status, "blocked", StringComparison.OrdinalIgnoreCase);
            issue.UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(input.OwnerEmail))
            {
                var owner = await FindUserByEmailAsync(input.OwnerEmail);
                issue.OwnerUserId = owner?.Id;
            }
            else
            {
                issue.OwnerUserId = null;
            }

            if (string.Equals(issue.Status, "resolved", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(issue.Status, "closed", StringComparison.OrdinalIgnoreCase))
            {
                issue.ClosedDate = DateTime.UtcNow;
            }
            else
            {
                issue.ClosedDate = null;
            }

            var existingActionIds = issue.IssueActions.Select(ia => ia.ActionId).ToList();
            foreach (var link in issue.IssueActions.Where(ia => !input.LinkedActionIds.Contains(ia.ActionId)).ToList())
            {
                _context.IssueActions.Remove(link);
            }

            foreach (var actionId in input.LinkedActionIds.Except(existingActionIds))
            {
                _context.IssueActions.Add(new IssueAction
                {
                    IssueId = issue.Id,
                    ActionId = actionId
                });
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Issue updated successfully.";
            return RedirectToAction(nameof(Details), new { id = input.ProjectId, tab = "issues" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateIssueTargetDate(int projectId, int issueId, DateTime? targetResolutionDate, string tab = "issues", string issuesView = "table")
        {
            var normalizedIssuesView = string.Equals(issuesView, "priority", StringComparison.OrdinalIgnoreCase) ? "priority" : "table";

            var issue = await _context.Issues
                .FirstOrDefaultAsync(i => i.Id == issueId && i.ProjectId == projectId && !i.IsDeleted);

            if (issue == null)
            {
                return NotFound();
            }

            if (targetResolutionDate.HasValue && targetResolutionDate.Value.Date < issue.DetectedDate.Date)
            {
                TempData["ErrorMessage"] = "Target resolution date cannot be earlier than the detected date.";
                return RedirectToAction(nameof(Details), new { id = projectId, tab, issuesView = normalizedIssuesView });
            }

            issue.TargetResolutionDate = targetResolutionDate;
            issue.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = targetResolutionDate.HasValue
                ? "Target resolution date updated."
                : "Target resolution date cleared.";

            return RedirectToAction(nameof(Details), new { id = projectId, tab, issuesView = normalizedIssuesView });
        }


        private async Task<User?> FindUserByEmailAsync(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return null;
            }

            var normalisedEmail = email.Trim().ToLowerInvariant();
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == normalisedEmail);
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
                return $"Error loading {entityType}";
            }
        }

        // POST: Project/UpdateFlagshipStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateFlagshipStatus(int id, bool isFlagship)
        {
            try
            {
                var project = await _context.Projects.FindAsync(id);
                if (project == null || project.IsDeleted)
                {
                    TempData["ErrorMessage"] = "Project not found.";
                    return RedirectToAction(nameof(Index));
                }

                project.IsFlagship = isFlagship;
                project.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = isFlagship 
                    ? "Project marked as flagship successfully." 
                    : "Flagship status removed successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating flagship status for project {ProjectId}", id);
                TempData["ErrorMessage"] = "Error updating flagship status. Please try again.";
            }

            return RedirectToAction(nameof(Details), new { id, tab = "strategicalignment" });
        }

        // POST: Project/UpdateBusinessArea
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateBusinessArea(int id, string? businessArea)
        {
            try
            {
                var project = await _context.Projects.FindAsync(id);
                if (project == null || project.IsDeleted)
                {
                    TempData["ErrorMessage"] = "Project not found.";
                    return RedirectToAction(nameof(Index));
                }

                project.BusinessArea = businessArea;
                project.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Business area updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating business area");
                TempData["ErrorMessage"] = "An error occurred while updating the business area.";
            }

            return RedirectToAction(nameof(Details), new { id = id, tab = "overview" });
        }

        // POST: Project/UpdatePhase
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePhase(int id, string? phase)
        {
            try
            {
                var project = await _context.Projects.FindAsync(id);
                if (project == null || project.IsDeleted)
                {
                    TempData["ErrorMessage"] = "Project not found.";
                    return RedirectToAction(nameof(Index));
                }

                project.Phase = phase;
                project.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Phase updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating phase");
                TempData["ErrorMessage"] = "An error occurred while updating the phase.";
            }

            return RedirectToAction(nameof(Details), new { id = id, tab = "overview" });
        }

        // POST: Project/UpdateHistoricBuRTId
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateHistoricBuRTId(int id, string? historicBuRTId)
        {
            try
            {
                var project = await _context.Projects.FindAsync(id);
                if (project == null || project.IsDeleted)
                {
                    TempData["ErrorMessage"] = "Project not found.";
                    return RedirectToAction(nameof(Index));
                }

                project.HistoricBuRTId = historicBuRTId;
                project.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Historic BuRT ID updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating historic BuRT ID");
                TempData["ErrorMessage"] = "An error occurred while updating the historic BuRT ID.";
            }

            return RedirectToAction(nameof(Details), new { id = id, tab = "overview" });
        }

        // POST: Project/UpdateStartDate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStartDate(int id, DateTime? startDate)
        {
            try
            {
                var project = await _context.Projects.FindAsync(id);
                if (project == null || project.IsDeleted)
                {
                    TempData["ErrorMessage"] = "Project not found.";
                    return RedirectToAction(nameof(Index));
                }

                project.StartDate = startDate;
                project.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Start date updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating start date");
                TempData["ErrorMessage"] = "An error occurred while updating the start date.";
            }

            return RedirectToAction(nameof(Details), new { id = id, tab = "overview" });
        }

        // POST: Project/UpdateTargetDeliveryDate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateTargetDeliveryDate(int id, DateTime? targetDeliveryDate)
        {
            try
            {
                var project = await _context.Projects.FindAsync(id);
                if (project == null || project.IsDeleted)
                {
                    TempData["ErrorMessage"] = "Project not found.";
                    return RedirectToAction(nameof(Index));
                }

                project.TargetDeliveryDate = targetDeliveryDate;
                project.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Target end date updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating target delivery date");
                TempData["ErrorMessage"] = "An error occurred while updating the target end date.";
            }

            return RedirectToAction(nameof(Details), new { id = id, tab = "overview" });
        }

        // POST: Project/AddDeliverable
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDeliverable(int flagshipProjectId, int deliverableProjectId, string? description)
        {
            try
            {
                _logger.LogInformation("AddDeliverable called: FlagshipId={FlagshipId}, DeliverableId={DeliverableId}", 
                    flagshipProjectId, deliverableProjectId);

                var flagshipProject = await _context.Projects.FindAsync(flagshipProjectId);
                if (flagshipProject == null || flagshipProject.IsDeleted || !flagshipProject.IsFlagship)
                {
                    _logger.LogWarning("Flagship project {FlagshipId} not found or invalid", flagshipProjectId);
                    TempData["ErrorMessage"] = "Flagship project not found or invalid.";
                    return RedirectToAction(nameof(Index));
                }

                var deliverableProject = await _context.Projects.FindAsync(deliverableProjectId);
                if (deliverableProject == null || deliverableProject.IsDeleted)
                {
                    _logger.LogWarning("Deliverable project {DeliverableId} not found", deliverableProjectId);
                    TempData["ErrorMessage"] = "Deliverable project not found.";
                    return RedirectToAction(nameof(Details), new { id = flagshipProjectId, tab = "deliverables" });
                }

                // Check if relationship already exists
                var allDependencies = await _context.Dependencies
                    .Where(d => d.SourceEntityType == "Project" && d.SourceEntityId == flagshipProjectId)
                    .ToListAsync();
                    
                _logger.LogInformation("Found {Count} existing dependencies for flagship {FlagshipId}", 
                    allDependencies.Count, flagshipProjectId);
                
                foreach (var dep in allDependencies)
                {
                    _logger.LogInformation("Existing dependency: TargetType={TargetType}, TargetId={TargetId}, DependencyType={DependencyType}",
                        dep.TargetEntityType, dep.TargetEntityId, dep.DependencyType);
                }

                var existingDependency = allDependencies.FirstOrDefault(d => 
                    d.TargetEntityType == "Project" && 
                    d.TargetEntityId == deliverableProjectId &&
                    d.DependencyType == "Deliverable");

                if (existingDependency != null)
                {
                    _logger.LogWarning("Deliverable relationship already exists between {FlagshipId} and {DeliverableId}", 
                        flagshipProjectId, deliverableProjectId);
                    TempData["ErrorMessage"] = "This project is already added as a deliverable.";
                    return RedirectToAction(nameof(Details), new { id = flagshipProjectId, tab = "deliverables" });
                }

                // Create the deliverable relationship
                var dependency = new Dependency
                {
                    SourceEntityType = "Project",
                    SourceEntityId = flagshipProjectId,
                    TargetEntityType = "Project",
                    TargetEntityId = deliverableProjectId,
                    DependencyType = "Deliverable",
                    Description = description ?? "",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Dependencies.Add(dependency);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Successfully added deliverable {DeliverableId} ({DeliverableTitle}) to flagship project {FlagshipId} with DependencyType={DependencyType}", 
                    deliverableProjectId, deliverableProject.Title, flagshipProjectId, dependency.DependencyType);

                TempData["SuccessMessage"] = $"'{deliverableProject.Title}' added as a deliverable successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding deliverable {DeliverableId} to flagship project {FlagshipId}", 
                    deliverableProjectId, flagshipProjectId);
                TempData["ErrorMessage"] = "Error adding deliverable. Please try again.";
            }

            return RedirectToAction(nameof(Details), new { id = flagshipProjectId, tab = "deliverables" });
        }

        // POST: Project/RemoveDeliverable
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveDeliverable(int flagshipProjectId, int deliverableProjectId)
        {
            try
            {
                var dependency = await _context.Dependencies
                    .FirstOrDefaultAsync(d => 
                        d.SourceEntityType == "Project" && 
                        d.SourceEntityId == flagshipProjectId &&
                        d.TargetEntityType == "Project" && 
                        d.TargetEntityId == deliverableProjectId &&
                        d.DependencyType == "Deliverable");

                if (dependency == null)
                {
                    TempData["ErrorMessage"] = "Deliverable relationship not found.";
                    return RedirectToAction(nameof(Details), new { id = flagshipProjectId, tab = "deliverables" });
                }

                _context.Dependencies.Remove(dependency);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Deliverable removed successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing deliverable {DeliverableId} from flagship project {FlagshipId}", 
                    deliverableProjectId, flagshipProjectId);
                TempData["ErrorMessage"] = "Error removing deliverable. Please try again.";
            }

            return RedirectToAction(nameof(Details), new { id = flagshipProjectId, tab = "deliverables" });
        }

        [HttpGet]
        public async Task<IActionResult> CreateAction(int projectId, int? linkedRiskId, int? linkedIssueId, int? parentActionId, int? decisionId)
        {
            var project = await GetProjectForActionAsync(projectId);
            if (project == null)
            {
                return NotFound();
            }

            var input = new ProjectActionInputModel
            {
                ProjectId = projectId,
                Status = ActionStatusValues[0]
            };

            if (linkedRiskId.HasValue && project.Risks.Any(r => r.Id == linkedRiskId.Value))
            {
                input.LinkedRiskIds.Add(linkedRiskId.Value);
                input.InitiatingEntityType = "risk";
                input.InitiatingEntityId = linkedRiskId.Value;
            }

            if (linkedIssueId.HasValue && project.Issues.Any(i => i.Id == linkedIssueId.Value))
            {
                if (!input.LinkedIssueIds.Contains(linkedIssueId.Value))
                {
                    input.LinkedIssueIds.Add(linkedIssueId.Value);
                }

                if (string.IsNullOrWhiteSpace(input.InitiatingEntityType))
                {
                    input.InitiatingEntityType = "issue";
                    input.InitiatingEntityId = linkedIssueId.Value;
                }
            }

            if (parentActionId.HasValue && project.Actions.Any(a => a.Id == parentActionId.Value))
            {
                input.ParentActionId = parentActionId;
            }

            if (decisionId.HasValue && project.Decisions.Any(d => d.Id == decisionId.Value))
            {
                input.DecisionId = decisionId;

                if (string.IsNullOrWhiteSpace(input.InitiatingEntityType))
                {
                    input.InitiatingEntityType = "decision";
                    input.InitiatingEntityId = decisionId.Value;
                }
            }

            var viewModel = await BuildActionFormViewModelAsync(project, input, showDeleteButton: false);
            return View("CreateAction", viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> EditAction(int projectId, int actionId)
        {
            var project = await GetProjectForActionAsync(projectId);
            if (project == null)
            {
                return NotFound();
            }

            var action = project.Actions.FirstOrDefault(a => a.Id == actionId);
            if (action == null)
            {
                return NotFound();
            }

            var input = new ProjectActionInputModel
            {
                ProjectId = projectId,
                ActionId = actionId,
                Title = action.Title,
                Description = action.Description,
                Status = action.Status,
                AssignedToEmail = action.AssignedToEmail,
                Priority = action.Priority,
                StartDate = action.StartDate,
                DueDate = action.DueDate,
                CompletedDate = action.CompletedDate,
                FipsId = action.FipsId,
                BusinessArea = action.BusinessArea,
                ActionSourceId = action.ActionSourceId,
                SourceType = action.SourceType,
                SourceReference = action.SourceReference,
                SourceRecordUrl = action.SourceRecordUrl,
                Notes = action.Notes,
                EvidenceUrl = action.EvidenceUrl,
                DecisionId = action.DecisionId,
                ParentActionId = action.ParentActionId,
                LinkedRiskIds = action.RiskActions.Select(ra => ra.RiskId).ToList(),
                LinkedIssueIds = action.IssueActions.Select(ia => ia.IssueId).ToList()
            };

            var viewModel = await BuildActionFormViewModelAsync(project, input, showDeleteButton: true, currentActionId: actionId);
            return View("EditAction", viewModel);
        }

        // POST: Project/AddAction
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAction([Bind(Prefix = "Input")] ProjectActionInputModel input)
        {
            input.LinkedRiskIds = input.LinkedRiskIds?.Where(id => id > 0).Distinct().ToList() ?? new List<int>();
            input.LinkedIssueIds = input.LinkedIssueIds?.Where(id => id > 0).Distinct().ToList() ?? new List<int>();

            input.Title = SanitiseText(input.Title) ?? string.Empty;
            input.Description = SanitiseText(input.Description);
            input.Status = string.IsNullOrWhiteSpace(input.Status) ? ActionStatusValues[0] : input.Status.Trim();
            input.AssignedToEmail = SanitiseText(input.AssignedToEmail);
            input.Priority = SanitiseText(input.Priority);
            input.FipsId = SanitiseText(input.FipsId);
            input.BusinessArea = SanitiseText(input.BusinessArea);
            input.SourceType = SanitiseText(input.SourceType);
            input.SourceReference = SanitiseText(input.SourceReference);
            input.SourceRecordUrl = SanitiseText(input.SourceRecordUrl);
            input.Notes = SanitiseText(input.Notes);
            input.EvidenceUrl = SanitiseText(input.EvidenceUrl);

            if (string.IsNullOrWhiteSpace(input.Title))
            {
                ModelState.AddModelError(nameof(ProjectActionInputModel.Title), "Enter an action title.");
            }

            if (!ActionStatusValues.Contains(input.Status, StringComparer.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(ProjectActionInputModel.Status), "Select a valid status.");
            }

            if (!string.IsNullOrWhiteSpace(input.Priority) && !ActionPriorityValues.Contains(input.Priority, StringComparer.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(ProjectActionInputModel.Priority), "Select a valid priority.");
            }

            var project = await GetProjectForActionAsync(input.ProjectId);
            if (project == null)
            {
                return NotFound();
            }

            var duplicate = await _context.Actions
                .AnyAsync(a => a.ProjectId == input.ProjectId && !a.IsDeleted && a.Title.ToLower() == input.Title.ToLower());
            if (duplicate)
            {
                ModelState.AddModelError(nameof(ProjectActionInputModel.Title), "An action with this title already exists for this project.");
            }

            int? decisionId = null;
            if (input.DecisionId.HasValue)
            {
                if (project.Decisions.Any(d => d.Id == input.DecisionId.Value))
                {
                    decisionId = input.DecisionId.Value;
                }
                else
                {
                    ModelState.AddModelError(nameof(ProjectActionInputModel.DecisionId), "Select a decision from this project.");
                }
            }

            int? parentActionId = null;
            if (input.ParentActionId.HasValue)
            {
                if (project.Actions.Any(a => a.Id == input.ParentActionId.Value))
                {
                    parentActionId = input.ParentActionId.Value;
                }
                else
                {
                    ModelState.AddModelError(nameof(ProjectActionInputModel.ParentActionId), "Select a parent action from this project.");
                }
            }

            var validRiskIds = project.Risks.Select(r => r.Id).ToHashSet();
            if (input.LinkedRiskIds.Any(id => !validRiskIds.Contains(id)))
            {
                ModelState.AddModelError(nameof(ProjectActionInputModel.LinkedRiskIds), "Select risks from this project.");
            }

            var validIssueIds = project.Issues.Select(i => i.Id).ToHashSet();
            if (input.LinkedIssueIds.Any(id => !validIssueIds.Contains(id)))
            {
                ModelState.AddModelError(nameof(ProjectActionInputModel.LinkedIssueIds), "Select issues from this project.");
            }

            if (!ModelState.IsValid)
            {
                var viewModel = await BuildActionFormViewModelAsync(project, input, showDeleteButton: false);
                return View("CreateAction", viewModel);
            }

            var action = new Models.Action
            {
                ProjectId = input.ProjectId,
                Title = input.Title,
                Description = input.Description,
                Status = input.Status,
                AssignedToEmail = input.AssignedToEmail,
                Priority = input.Priority,
                StartDate = input.StartDate,
                DueDate = input.DueDate,
                CompletedDate = input.CompletedDate,
                FipsId = input.FipsId,
                BusinessArea = input.BusinessArea,
                ActionSourceId = input.ActionSourceId > 0 ? input.ActionSourceId : null,
                SourceType = input.SourceType,
                SourceReference = input.SourceReference,
                SourceRecordUrl = input.SourceRecordUrl,
                Notes = input.Notes,
                EvidenceUrl = input.EvidenceUrl,
                DecisionId = decisionId,
                ParentActionId = parentActionId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Actions.Add(action);
            await _context.SaveChangesAsync();

            if (input.LinkedRiskIds.Any())
            {
                foreach (var riskId in input.LinkedRiskIds.Distinct())
                {
                    _context.RiskActions.Add(new RiskAction
                    {
                        ActionId = action.Id,
                        RiskId = riskId
                    });
                }
            }

            if (input.LinkedIssueIds.Any())
            {
                foreach (var issueId in input.LinkedIssueIds.Distinct())
                {
                    _context.IssueActions.Add(new IssueAction
                    {
                        ActionId = action.Id,
                        IssueId = issueId
                    });
                }
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Action added successfully.";
            return RedirectToAction(nameof(Details), new { id = input.ProjectId, tab = "actions" });
        }

        // POST: Project/UpdateAction
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAction([Bind(Prefix = "Input")] ProjectActionInputModel input)
        {
            if (!input.ActionId.HasValue)
            {
                return BadRequest();
            }

            input.LinkedRiskIds = input.LinkedRiskIds?.Where(id => id > 0).Distinct().ToList() ?? new List<int>();
            input.LinkedIssueIds = input.LinkedIssueIds?.Where(id => id > 0).Distinct().ToList() ?? new List<int>();

            input.Title = SanitiseText(input.Title) ?? string.Empty;
            input.Description = SanitiseText(input.Description);
            input.Status = string.IsNullOrWhiteSpace(input.Status) ? ActionStatusValues[0] : input.Status.Trim();
            input.AssignedToEmail = SanitiseText(input.AssignedToEmail);
            input.Priority = SanitiseText(input.Priority);
            input.FipsId = SanitiseText(input.FipsId);
            input.BusinessArea = SanitiseText(input.BusinessArea);
            input.SourceType = SanitiseText(input.SourceType);
            input.SourceReference = SanitiseText(input.SourceReference);
            input.SourceRecordUrl = SanitiseText(input.SourceRecordUrl);
            input.Notes = SanitiseText(input.Notes);
            input.EvidenceUrl = SanitiseText(input.EvidenceUrl);

            if (string.IsNullOrWhiteSpace(input.Title))
            {
                ModelState.AddModelError(nameof(ProjectActionInputModel.Title), "Enter an action title.");
            }

            if (!ActionStatusValues.Contains(input.Status, StringComparer.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(ProjectActionInputModel.Status), "Select a valid status.");
            }

            if (!string.IsNullOrWhiteSpace(input.Priority) && !ActionPriorityValues.Contains(input.Priority, StringComparer.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(ProjectActionInputModel.Priority), "Select a valid priority.");
            }

            var action = await _context.Actions
                .Include(a => a.RiskActions)
                .Include(a => a.IssueActions)
                .FirstOrDefaultAsync(a => a.Id == input.ActionId.Value && a.ProjectId == input.ProjectId && !a.IsDeleted);

            if (action == null)
            {
                return NotFound();
            }

            var project = await GetProjectForActionAsync(input.ProjectId);
            if (project == null)
            {
                return NotFound();
            }

            int? decisionId = null;
            if (input.DecisionId.HasValue)
            {
                if (project.Decisions.Any(d => d.Id == input.DecisionId.Value))
                {
                    decisionId = input.DecisionId.Value;
                }
                else
                {
                    ModelState.AddModelError(nameof(ProjectActionInputModel.DecisionId), "Select a decision from this project.");
                }
            }

            int? parentActionId = null;
            if (input.ParentActionId.HasValue)
            {
                if (input.ParentActionId.Value == action.Id)
                {
                    ModelState.AddModelError(nameof(ProjectActionInputModel.ParentActionId), "An action cannot be its own parent.");
                }
                else if (project.Actions.Any(a => a.Id == input.ParentActionId.Value))
                {
                    parentActionId = input.ParentActionId.Value;
                }
                else
                {
                    ModelState.AddModelError(nameof(ProjectActionInputModel.ParentActionId), "Select a parent action from this project.");
                }
            }

            var validRiskIds = project.Risks.Select(r => r.Id).ToHashSet();
            if (input.LinkedRiskIds.Any(id => !validRiskIds.Contains(id)))
            {
                ModelState.AddModelError(nameof(ProjectActionInputModel.LinkedRiskIds), "Select risks from this project.");
            }

            var validIssueIds = project.Issues.Select(i => i.Id).ToHashSet();
            if (input.LinkedIssueIds.Any(id => !validIssueIds.Contains(id)))
            {
                ModelState.AddModelError(nameof(ProjectActionInputModel.LinkedIssueIds), "Select issues from this project.");
            }

            if (!ModelState.IsValid)
            {
                var viewModel = await BuildActionFormViewModelAsync(project, input, showDeleteButton: true, currentActionId: action.Id);
                return View("EditAction", viewModel);
            }

            action.Title = input.Title;
            action.Description = input.Description;
            action.Status = input.Status;
            action.AssignedToEmail = input.AssignedToEmail;
            action.Priority = input.Priority;
            action.StartDate = input.StartDate;
            action.DueDate = input.DueDate;
            action.CompletedDate = input.CompletedDate;
            action.FipsId = input.FipsId;
            action.BusinessArea = input.BusinessArea;
            action.ActionSourceId = input.ActionSourceId > 0 ? input.ActionSourceId : null;
            action.SourceType = input.SourceType;
            action.SourceReference = input.SourceReference;
            action.SourceRecordUrl = input.SourceRecordUrl;
            action.Notes = input.Notes;
            action.EvidenceUrl = input.EvidenceUrl;
            action.DecisionId = decisionId;
            action.ParentActionId = parentActionId;
            action.UpdatedAt = DateTime.UtcNow;

            var existingRiskIds = action.RiskActions.Select(ra => ra.RiskId).ToList();
            foreach (var link in action.RiskActions.Where(ra => !input.LinkedRiskIds.Contains(ra.RiskId)).ToList())
            {
                _context.RiskActions.Remove(link);
            }

            foreach (var riskId in input.LinkedRiskIds.Except(existingRiskIds))
            {
                _context.RiskActions.Add(new RiskAction
                {
                    ActionId = action.Id,
                    RiskId = riskId
                });
            }

            var existingIssueIds = action.IssueActions.Select(ia => ia.IssueId).ToList();
            foreach (var link in action.IssueActions.Where(ia => !input.LinkedIssueIds.Contains(ia.IssueId)).ToList())
            {
                _context.IssueActions.Remove(link);
            }

            foreach (var issueId in input.LinkedIssueIds.Except(existingIssueIds))
            {
                _context.IssueActions.Add(new IssueAction
                {
                    ActionId = action.Id,
                    IssueId = issueId
                });
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Action updated successfully.";
            return RedirectToAction(nameof(Details), new { id = input.ProjectId, tab = "actions" });
        }

        // POST: Project/DeleteAction
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAction(int projectId, int actionId)
        {
            try
            {
                var action = await _context.Actions
                    .FirstOrDefaultAsync(a => a.Id == actionId && a.ProjectId == projectId && !a.IsDeleted);

                if (action == null)
                {
                    TempData["ErrorMessage"] = "Action not found.";
                    return RedirectToAction(nameof(Details), new { id = projectId, tab = "actions" });
                }

                action.IsDeleted = true;
                action.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Action deleted successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting action {ActionId} for project {ProjectId}", actionId, projectId);
                TempData["ErrorMessage"] = "Error deleting action. Please try again.";
            }

            return RedirectToAction(nameof(Details), new { id = projectId, tab = "actions" });
        }

        // POST: Project/UpdateDecision
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateDecision(
            int projectId,
            int decisionId,
            string title,
            string status,
            string? summary,
            string? decisionType,
            DateTime? decisionDate,
            string? businessArea,
            string? outcome,
            string? notes,
            string? ownerEmail,
            string? fipsId,
            string? sourceType,
            string? sourceReference,
            string? sourceRecordUrl,
            int[]? linkedRiskIds,
            int[]? linkedIssueIds,
            int[]? linkedActionIds)
        {
            try
            {
                var decision = await _context.Decisions
                    .Include(d => d.RiskDecisions)
                    .Include(d => d.IssueDecisions)
                    .FirstOrDefaultAsync(d => d.Id == decisionId && d.ProjectId == projectId && !d.IsDeleted);

                if (decision == null)
                {
                    TempData["ErrorMessage"] = "Decision not found.";
                    return RedirectToAction(nameof(Details), new { id = projectId, tab = "decisions" });
                }

                if (string.IsNullOrWhiteSpace(title))
                {
                    TempData["ErrorMessage"] = "Decision title is required.";
                    return RedirectToAction(nameof(Details), new { id = projectId, tab = "decisions" });
                }

                int? ownerUserId = null;
                if (!string.IsNullOrWhiteSpace(ownerEmail))
                {
                    var owner = await _context.Users
                        .FirstOrDefaultAsync(u => u.Email.ToLower() == ownerEmail.Trim().ToLower());
                    ownerUserId = owner?.Id;
                }

                decision.Title = title.Trim();
                decision.Status = string.IsNullOrWhiteSpace(status) ? decision.Status : status.Trim();
                decision.Summary = string.IsNullOrWhiteSpace(summary) ? null : summary.Trim();
                decision.DecisionType = string.IsNullOrWhiteSpace(decisionType) ? null : decisionType.Trim();
                decision.DecisionDate = decisionDate;
                decision.BusinessArea = string.IsNullOrWhiteSpace(businessArea) ? null : businessArea.Trim();
                decision.Outcome = string.IsNullOrWhiteSpace(outcome) ? null : outcome.Trim();
                decision.Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
                decision.OwnerUserId = ownerUserId;
                decision.FipsId = string.IsNullOrWhiteSpace(fipsId) ? null : fipsId.Trim();
                decision.SourceType = string.IsNullOrWhiteSpace(sourceType) ? null : sourceType.Trim();
                decision.SourceReference = string.IsNullOrWhiteSpace(sourceReference) ? null : sourceReference.Trim();
                decision.SourceRecordUrl = string.IsNullOrWhiteSpace(sourceRecordUrl) ? null : sourceRecordUrl.Trim();
                decision.UpdatedAt = DateTime.UtcNow;

                var selectedRiskIds = new HashSet<int>((linkedRiskIds ?? Array.Empty<int>()).Where(id => id > 0));
                foreach (var link in decision.RiskDecisions.Where(rd => !selectedRiskIds.Contains(rd.RiskId)).ToList())
                {
                    _context.RiskDecisions.Remove(link);
                }

                var existingRiskIds = decision.RiskDecisions.Select(rd => rd.RiskId).ToList();
                var newRiskIds = selectedRiskIds.Except(existingRiskIds).ToList();
                if (newRiskIds.Any())
                {
                    var validRiskIds = await _context.Risks
                        .Where(r => newRiskIds.Contains(r.Id) && r.ProjectId == projectId && !r.IsDeleted)
                        .Select(r => r.Id)
                        .ToListAsync();

                    foreach (var riskId in validRiskIds)
                    {
                        _context.RiskDecisions.Add(new RiskDecision { RiskId = riskId, DecisionId = decision.Id });
                    }
                }

                var selectedIssueIds = new HashSet<int>((linkedIssueIds ?? Array.Empty<int>()).Where(id => id > 0));
                foreach (var link in decision.IssueDecisions.Where(idl => !selectedIssueIds.Contains(idl.IssueId)).ToList())
                {
                    _context.IssueDecisions.Remove(link);
                }

                var existingIssueIds = decision.IssueDecisions.Select(idl => idl.IssueId).ToList();
                var newIssueIds = selectedIssueIds.Except(existingIssueIds).ToList();
                if (newIssueIds.Any())
                {
                    var validIssueIds = await _context.Issues
                        .Where(i => newIssueIds.Contains(i.Id) && i.ProjectId == projectId && !i.IsDeleted)
                        .Select(i => i.Id)
                        .ToListAsync();

                    foreach (var issueId in validIssueIds)
                    {
                        _context.IssueDecisions.Add(new IssueDecision { IssueId = issueId, DecisionId = decision.Id });
                    }
                }

                var selectedActionIds = new HashSet<int>((linkedActionIds ?? Array.Empty<int>()).Where(id => id > 0));
                var actionsToClear = await _context.Actions
                    .Where(a => a.DecisionId == decision.Id && !selectedActionIds.Contains(a.Id))
                    .ToListAsync();

                foreach (var action in actionsToClear)
                {
                    action.DecisionId = null;
                    action.UpdatedAt = DateTime.UtcNow;
                }

                if (selectedActionIds.Any())
                {
                    var actionsToLink = await _context.Actions
                        .Where(a => selectedActionIds.Contains(a.Id) && a.ProjectId == projectId && !a.IsDeleted)
                        .ToListAsync();

                    foreach (var action in actionsToLink)
                    {
                        action.DecisionId = decision.Id;
                        action.UpdatedAt = DateTime.UtcNow;
                    }
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Decision updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating decision {DecisionId} for project {ProjectId}", decisionId, projectId);
                TempData["ErrorMessage"] = "Error updating decision. Please try again.";
            }

            return RedirectToAction(nameof(Details), new { id = projectId, tab = "decisions" });
        }

        // POST: Project/CreateKpi
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateKpi([Bind(Prefix = "Input")] ProjectKpiInputModel input)
        {
            async Task<IActionResult> ReturnCreateViewAsync()
            {
                var projectForView = await GetProjectForKpiAsync(input.ProjectId);
                if (projectForView == null)
                {
                    return NotFound();
                }

                var viewModel = await BuildKpiFormViewModelAsync(projectForView, input, showActiveToggle: false);
                return View("CreateKpi", viewModel);
            }

            ValidateKpiOtherFields(input);
            input.ReportingStages = SanitiseReportingStageSelections(input.ReportingStages);
            var statusValue = ResolveKpiStatus(input);

            var trimmedName = input.Name?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(trimmedName))
            {
                ModelState.AddModelError(nameof(ProjectKpiInputModel.Name), "Enter a KPI name.");
            }

            var (categoryCode, categoryName) = await ResolveKpiCategoryAsync(input);
            if (string.IsNullOrWhiteSpace(categoryCode))
            {
                ModelState.AddModelError(nameof(ProjectKpiInputModel.CategoryCode), "Select a category.");
            }

            if (!ModelState.IsValid)
            {
                return await ReturnCreateViewAsync();
            }

            try
            {
                var projectExists = await _context.Projects.AnyAsync(p => p.Id == input.ProjectId && !p.IsDeleted);
                if (!projectExists)
                {
                    return NotFound();
                }

                var resolvedObjectiveId = await ResolveObjectiveIdAsync(input);
                var resolvedMilestoneId = await ResolveMilestoneIdAsync(input);

                if (!ModelState.IsValid)
                {
                    return await ReturnCreateViewAsync();
                }

                var unitOfMeasureValue = ResolveSelectedValue(input.UnitOfMeasure, input.UnitOfMeasureOther);
                var reportingStagesValue = SerialiseReportingStages(input.ReportingStages);
                var dataSourceValue = ResolveSelectedValue(input.DataSource, input.DataSourceOther);
                var frequencyValue = SanitiseText(input.Frequency);

                var kpi = new Kpi
                {
                    Name = trimmedName,
                    Code = string.Empty,
                    Category = categoryName,
                    Description = SanitiseText(input.Description),
                    UnitOfMeasure = unitOfMeasureValue,
                    CalculationMethod = SanitiseText(input.CalculationMethod),
                    Frequency = frequencyValue,
                    TargetValue = input.TargetValue,
                    Thresholds = SanitiseText(input.Thresholds),
                    DataSource = dataSourceValue,
                    ReportingStage = reportingStagesValue,
                    Status = statusValue,
                    AssignedToEntityId = input.ProjectId.ToString(CultureInfo.InvariantCulture),
                    EntityType = "project",
                    ProjectId = input.ProjectId,
                    ObjectiveId = resolvedObjectiveId,
                    MilestoneId = resolvedMilestoneId,
                    OwnerUserId = null,
                    ValidationRule = null,
                    Active = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Kpis.Add(kpi);
                await _context.SaveChangesAsync();

                if (!string.IsNullOrWhiteSpace(categoryCode))
                {
                    var generatedCode = GenerateKpiCode(categoryCode!, kpi.Id);

                    if (await _context.Kpis.AnyAsync(existing => existing.Id != kpi.Id && existing.Code == generatedCode))
                    {
                        ModelState.AddModelError(nameof(ProjectKpiInputModel.CategoryCode), "Unable to generate a unique KPI code. Please check the category configuration.");
                        return await ReturnCreateViewAsync();
                    }

                    kpi.Code = generatedCode;
                    kpi.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    input.Code = generatedCode;
                }

                TempData["SuccessMessage"] = "KPI created successfully.";
                return RedirectToAction(nameof(Details), new { id = input.ProjectId, tab = "kpis" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating KPI for project {ProjectId}", input.ProjectId);
                TempData["ErrorMessage"] = "An error occurred while creating the KPI.";
            }

            return await ReturnCreateViewAsync();
        }

        // POST: Project/UpdateKpi
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateKpi([Bind(Prefix = "Input")] ProjectKpiInputModel input)
        {
            if (!input.KpiId.HasValue)
            {
                TempData["ErrorMessage"] = "No KPI was specified for update.";
                return RedirectToAction(nameof(Details), new { id = input.ProjectId, tab = "kpis" });
            }

            async Task<IActionResult> ReturnEditViewAsync()
            {
                var projectForView = await GetProjectForKpiAsync(input.ProjectId);
                if (projectForView == null)
                {
                    return NotFound();
                }

                var viewModel = await BuildKpiFormViewModelAsync(projectForView, input, showActiveToggle: true);
                return View("EditKpi", viewModel);
            }

            ValidateKpiOtherFields(input);
            input.ReportingStages = SanitiseReportingStageSelections(input.ReportingStages);
            var statusValue = ResolveKpiStatus(input);

            var trimmedName = input.Name?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(trimmedName))
            {
                ModelState.AddModelError(nameof(ProjectKpiInputModel.Name), "Enter a KPI name.");
            }

            var (categoryCode, categoryName) = await ResolveKpiCategoryAsync(input);
            if (string.IsNullOrWhiteSpace(categoryCode))
            {
                ModelState.AddModelError(nameof(ProjectKpiInputModel.CategoryCode), "Select a category.");
            }

            if (!ModelState.IsValid)
            {
                return await ReturnEditViewAsync();
            }

            try
            {
                var kpi = await _context.Kpis
                    .FirstOrDefaultAsync(k => k.Id == input.KpiId.Value && k.ProjectId == input.ProjectId);

                if (kpi == null)
                {
                    TempData["ErrorMessage"] = "KPI not found.";
                    return RedirectToAction(nameof(Details), new { id = input.ProjectId, tab = "kpis" });
                }

                var resolvedObjectiveId = await ResolveObjectiveIdAsync(input);
                var resolvedMilestoneId = await ResolveMilestoneIdAsync(input);

                if (!ModelState.IsValid)
                {
                    return await ReturnEditViewAsync();
                }

                var unitOfMeasureValue = ResolveSelectedValue(input.UnitOfMeasure, input.UnitOfMeasureOther);
                var reportingStagesValue = SerialiseReportingStages(input.ReportingStages);
                var dataSourceValue = ResolveSelectedValue(input.DataSource, input.DataSourceOther);
                var frequencyValue = SanitiseText(input.Frequency);

                kpi.Name = trimmedName;
                kpi.Category = categoryName;
                kpi.Description = SanitiseText(input.Description);
                kpi.UnitOfMeasure = unitOfMeasureValue;
                kpi.CalculationMethod = SanitiseText(input.CalculationMethod);
                kpi.Frequency = frequencyValue;
                kpi.TargetValue = input.TargetValue;
                kpi.Thresholds = SanitiseText(input.Thresholds);
                kpi.DataSource = dataSourceValue;
                kpi.ReportingStage = reportingStagesValue;
                kpi.Status = statusValue;
                kpi.ObjectiveId = resolvedObjectiveId;
                kpi.MilestoneId = resolvedMilestoneId;
                kpi.OwnerUserId = null;
                kpi.ValidationRule = null;
                kpi.Active = input.Active;
                kpi.UpdatedAt = DateTime.UtcNow;

                if (!string.IsNullOrWhiteSpace(categoryCode))
                {
                    var generatedCode = GenerateKpiCode(categoryCode!, kpi.Id);
                    kpi.Code = generatedCode;
                    input.Code = generatedCode;
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "KPI updated successfully.";
                return RedirectToAction(nameof(Details), new { id = input.ProjectId, tab = "kpis" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating KPI {KpiId} for project {ProjectId}", input.KpiId, input.ProjectId);
                TempData["ErrorMessage"] = "An error occurred while updating the KPI.";
            }

            return await ReturnEditViewAsync();
        }

        // POST: Project/SetKpiActive
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetKpiActive(int projectId, int kpiId, bool active)
        {
            try
            {
                var kpi = await _context.Kpis.FirstOrDefaultAsync(k => k.Id == kpiId && k.ProjectId == projectId);
                if (kpi == null)
                {
                    TempData["ErrorMessage"] = "KPI not found.";
                    return RedirectToAction(nameof(Details), new { id = projectId, tab = "kpis" });
                }

                kpi.Active = active;
                kpi.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = active ? "KPI restored successfully." : "KPI archived successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting KPI {KpiId} active flag", kpiId);
                TempData["ErrorMessage"] = "An error occurred while updating the KPI.";
            }

            return RedirectToAction(nameof(Details), new { id = projectId, tab = "kpis" });
        }

        // POST: Project/AddKpiDataPoint
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddKpiDataPoint(KpiDataPointInputModel input)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Unable to record KPI data. Please check the details and try again.";
                return RedirectToAction(nameof(Details), new { id = input.ProjectId, tab = "kpis" });
            }

            try
            {
                var kpi = await _context.Kpis
                    .FirstOrDefaultAsync(k => k.Id == input.KpiId && k.ProjectId == input.ProjectId);

                if (kpi == null)
                {
                    TempData["ErrorMessage"] = "KPI not found.";
                    return RedirectToAction(nameof(Details), new { id = input.ProjectId, tab = "kpis" });
                }

                var duplicatePeriod = await _context.KpiDataPoints
                    .AnyAsync(dp => dp.KpiId == input.KpiId && dp.ReportingPeriodStart == input.ReportingPeriodStart && dp.ReportingPeriodEnd == input.ReportingPeriodEnd);

                if (duplicatePeriod)
                {
                    TempData["ErrorMessage"] = "A data submission already exists for the selected reporting period.";
                    return RedirectToAction(nameof(Details), new { id = input.ProjectId, tab = "kpis" });
                }

                int? submittedByUserId = null;
                if (!string.IsNullOrWhiteSpace(input.SubmittedByEmail))
                {
                    var submittedByUser = await FindUserByEmailAsync(input.SubmittedByEmail);
                    if (submittedByUser == null)
                    {
                        TempData["ErrorMessage"] = "We could not find a user with that email address.";
                        return RedirectToAction(nameof(Details), new { id = input.ProjectId, tab = "kpis" });
                    }

                    submittedByUserId = submittedByUser.Id;
                }

                var dataPoint = new KpiDataPoint
                {
                    KpiId = input.KpiId,
                    ReportingPeriodStart = input.ReportingPeriodStart,
                    ReportingPeriodEnd = input.ReportingPeriodEnd,
                    Value = input.Value,
                    ValueNarrative = input.ValueNarrative,
                    Notes = input.Notes,
                    IsValidated = input.IsValidated,
                    SubmissionStatus = input.IsValidated ? "validated" : "submitted",
                    SubmittedByUserId = submittedByUserId,
                    SubmittedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.KpiDataPoints.Add(dataPoint);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "KPI data recorded successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding KPI data point for KPI {KpiId}", input.KpiId);
                TempData["ErrorMessage"] = "An error occurred while recording KPI data.";
            }

            return RedirectToAction(nameof(Details), new { id = input.ProjectId, tab = "kpis" });
        }

        // POST: Project/DeleteKpiDataPoint
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteKpiDataPoint(int projectId, int dataPointId)
        {
            try
            {
                var dataPoint = await _context.KpiDataPoints
                    .Include(dp => dp.Kpi)
                    .FirstOrDefaultAsync(dp => dp.Id == dataPointId && dp.Kpi.ProjectId == projectId);

                if (dataPoint == null)
                {
                    TempData["ErrorMessage"] = "KPI data submission not found.";
                    return RedirectToAction(nameof(Details), new { id = projectId, tab = "kpis" });
                }

                _context.KpiDataPoints.Remove(dataPoint);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "KPI data submission deleted.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting KPI data point {DataPointId}", dataPointId);
                TempData["ErrorMessage"] = "An error occurred while deleting the KPI data submission.";
            }

            return RedirectToAction(nameof(Details), new { id = projectId, tab = "kpis" });
        }

        [HttpGet]
        public async Task<IActionResult> CreateKpi(int projectId)
        {
            var project = await GetProjectForKpiAsync(projectId);
            if (project == null)
            {
                return NotFound();
            }

            var input = new ProjectKpiInputModel
            {
                ProjectId = projectId,
                Active = true
            };

            var viewModel = await BuildKpiFormViewModelAsync(project, input, showActiveToggle: false);

            return View("CreateKpi", viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> EditKpi(int projectId, int kpiId)
        {
            var project = await GetProjectForKpiAsync(projectId);
            if (project == null)
            {
                return NotFound();
            }

            var kpi = await _context.Kpis
                .AsNoTracking()
                .FirstOrDefaultAsync(k => k.Id == kpiId && k.ProjectId == projectId);

            if (kpi == null)
            {
                return NotFound();
            }

            var input = new ProjectKpiInputModel
            {
                ProjectId = projectId,
                KpiId = kpiId,
                Name = kpi.Name,
                Code = kpi.Code,
                Category = kpi.Category,
                CategoryCode = ExtractCategoryCode(kpi),
                Description = kpi.Description,
                UnitOfMeasure = kpi.UnitOfMeasure,
                CalculationMethod = kpi.CalculationMethod,
                Frequency = kpi.Frequency,
                TargetValue = kpi.TargetValue,
                Thresholds = kpi.Thresholds,
                DataSource = kpi.DataSource,
                ReportingStages = SplitReportingStages(kpi.ReportingStage),
                ObjectiveId = kpi.ObjectiveId,
                MilestoneId = kpi.MilestoneId,
                Active = kpi.Active
            };

            var viewModel = await BuildKpiFormViewModelAsync(project, input, showActiveToggle: true);

            return View("EditKpi", viewModel);
        }

        private async Task<Project?> GetProjectForKpiAsync(int projectId)
        {
            return await _context.Projects
                .Where(p => p.Id == projectId && !p.IsDeleted)
                .Include(p => p.ProjectObjectives)
                    .ThenInclude(po => po.Objective)
                .Include(p => p.Milestones)
                .AsSplitQuery()
                .FirstOrDefaultAsync();
        }

        private async Task<ProjectKpiFormViewModel> BuildKpiFormViewModelAsync(Project project, ProjectKpiInputModel input, bool showActiveToggle)
        {
            var categoryOptions = (await BuildKpiCategoryOptionsAsync(input)).ToList();
            var reportingStages = await _productsApiService.GetPhasesAsync();
            var reportingStageOptions = BuildReportingStageOptions(reportingStages, input.ReportingStages);
            var statusOptions = BuildKpiStatusOptions(input.Status);
            var unitOptions = BuildUnitOfMeasureOptions(string.Equals(input.UnitOfMeasure, OtherOptionValue, StringComparison.Ordinal) ? input.UnitOfMeasureOther : input.UnitOfMeasure);
            var frequencyOptions = BuildFrequencyOptions(input.Frequency);
            var dataSourceOptions = BuildDataSourceOptions(string.Equals(input.DataSource, OtherOptionValue, StringComparison.Ordinal) ? input.DataSourceOther : input.DataSource);
            var objectiveOptions = await BuildObjectiveOptionsAsync(project, input.ObjectiveId);
            var milestoneOptions = await BuildMilestoneOptionsAsync(project, input.MilestoneId);

            return new ProjectKpiFormViewModel
            {
                Input = input,
                ProjectTitle = project.Title,
                ProjectSummary = CreateProjectSummary(project),
                CategoryOptions = categoryOptions,
                ReportingStageOptions = reportingStageOptions,
                StatusOptions = statusOptions,
                UnitOfMeasureOptions = unitOptions,
                FrequencyOptions = frequencyOptions,
                DataSourceOptions = dataSourceOptions,
                ObjectiveOptions = objectiveOptions,
                MilestoneOptions = milestoneOptions,
                ShowActiveToggle = showActiveToggle
            };
        }

        private async Task<IEnumerable<SelectListItem>> BuildKpiCategoryOptionsAsync(ProjectKpiInputModel input)
        {
            var categories = await _context.KpiCategories
                .AsNoTracking()
                .OrderBy(c => c.SortOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();

            if (!categories.Any())
            {
                return BuildLegacyCategoryOptions(input.Category);
            }

            var selectedCode = string.IsNullOrWhiteSpace(input.CategoryCode) ? null : SanitiseIdentifier(input.CategoryCode);
            if (string.IsNullOrWhiteSpace(selectedCode) && !string.IsNullOrWhiteSpace(input.Category))
            {
                var matchByName = categories.FirstOrDefault(cv => string.Equals(cv.Name, input.Category.Trim(), StringComparison.OrdinalIgnoreCase));
                if (matchByName != null)
                {
                    selectedCode = matchByName.Code;
                    input.CategoryCode = selectedCode;
                }
            }
            else if (!string.IsNullOrWhiteSpace(selectedCode))
            {
                input.CategoryCode = selectedCode;
            }

            var options = categories
                .Where(c => c.IsActive)
                .Select(c => new SelectListItem(c.Name, c.Code, !string.IsNullOrWhiteSpace(selectedCode) && string.Equals(c.Code, selectedCode, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            if (!string.IsNullOrWhiteSpace(selectedCode) && options.All(o => !string.Equals(o.Value, selectedCode, StringComparison.OrdinalIgnoreCase)))
            {
                var selectedCategory = categories.FirstOrDefault(c => string.Equals(c.Code, selectedCode, StringComparison.OrdinalIgnoreCase));
                var label = selectedCategory?.Name ?? input.Category ?? selectedCode;
                if (selectedCategory != null && !selectedCategory.IsActive)
                {
                    label += " (inactive)";
                }
                options.Insert(0, new SelectListItem(label, selectedCode, true));
            }

            if (options.Count == 0)
            {
                // all categories are inactive, fall back to showing them for selection
                options = categories
                    .Select(c =>
                    {
                        var label = c.IsActive ? c.Name : $"{c.Name} (inactive)";
                        var selected = !string.IsNullOrWhiteSpace(selectedCode) && string.Equals(c.Code, selectedCode, StringComparison.OrdinalIgnoreCase);
                        return new SelectListItem(label, c.Code, selected, !c.IsActive);
                    })
                    .ToList();
            }

            return options;
        }

        private static IEnumerable<SelectListItem> BuildCategoryOptions(string? currentValue)
        {
            var categories = new[]
            {
                "Accessibility",
                "Compliance",
                "Engagement",
                "Financial",
                "Operational",
                "Outcome",
                "Performance",
                "Quality",
                "User satisfaction"
            };

            return BuildSelectList(categories, currentValue, "Select category");
        }

        private static IEnumerable<SelectListItem> BuildReportingStageOptions(IEnumerable<string> stages, IEnumerable<string?>? selectedValues)
        {
            var selectedList = SanitiseReportingStageSelections(selectedValues);
            var selectedSet = new HashSet<string>(selectedList, StringComparer.OrdinalIgnoreCase);

            var options = new List<SelectListItem>();

            foreach (var stage in stages)
            {
                var value = SanitiseText(stage);
                if (string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                var isSelected = selectedSet.Contains(value);
                options.Add(new SelectListItem(value, value, isSelected));
                if (isSelected)
                {
                    selectedSet.Remove(value);
                }
            }

            if (selectedSet.Count > 0)
            {
                foreach (var remaining in selectedList.Where(value => selectedSet.Contains(value)))
                {
                    options.Add(new SelectListItem(remaining, remaining, true));
                }
            }

            return options;
        }

        private static IEnumerable<SelectListItem> BuildUnitOfMeasureOptions(string? currentValue)
        {
            var units = new[]
            {
                "Percentage",
                "Number",
                "Ratio",
                "Minutes",
                "Hours",
                "Days",
                "Weeks",
                "Months",
                "Currency (£)",
                "Currency (£m)",
                "Score (0-10)"
            };

            return BuildSelectList(units, currentValue, "Select unit of measure");
        }

        private static IEnumerable<SelectListItem> BuildFrequencyOptions(string? currentValue)
        {
            var frequencies = new[]
            {
                "One-off",
                "Weekly",
                "Monthly",
                "Quarterly",
                "Bi-annually",
                "Annually"
            };

            return BuildSelectList(frequencies, currentValue, "Select reporting frequency", includeOther: false);
        }

        private static IEnumerable<SelectListItem> BuildDataSourceOptions(string? currentValue)
        {
            var sources = new[]
            {
                "Analytics platform",
                "Data warehouse",
                "Finance system",
                "Manual submission",
                "Service desk",
                "Survey",
                "System integration",
                "Third-party data"
            };

            return BuildSelectList(sources, currentValue, "Select data source");
        }

        private async Task<IEnumerable<SelectListItem>> BuildObjectiveOptionsAsync(Project project, int? currentObjectiveId)
        {
            var items = new List<SelectListItem>
            {
                new SelectListItem("Not linked", string.Empty)
            };

            var objectives = project.ProjectObjectives?
                .Where(po => po.Objective != null)
                .Select(po => po.Objective!)
                .Where(o => !o.IsDeleted)
                .GroupBy(o => o.Id)
                .Select(g => g.First())
                .OrderBy(o => o.Title)
                .ToList() ?? new List<Objective>();

            items.AddRange(objectives.Select(o => new SelectListItem(o.Title, o.Id.ToString(CultureInfo.InvariantCulture))));

            if (currentObjectiveId.HasValue && items.All(i => i.Value != currentObjectiveId.Value.ToString(CultureInfo.InvariantCulture)))
            {
                var objective = await _context.Objectives
                    .AsNoTracking()
                    .FirstOrDefaultAsync(o => o.Id == currentObjectiveId.Value);

                if (objective != null)
                {
                    var text = $"{objective.Title} (not currently linked)";
                    items.Add(new SelectListItem(text, currentObjectiveId.Value.ToString(CultureInfo.InvariantCulture)));
                }
            }

            return items;
        }

        private async Task<IEnumerable<SelectListItem>> BuildMilestoneOptionsAsync(Project project, int? currentMilestoneId)
        {
            var items = new List<SelectListItem>
            {
                new SelectListItem("Not linked", string.Empty)
            };

            var milestones = project.Milestones?
                .Where(m => !m.IsDeleted)
                .OrderBy(m => m.DueDate)
                .ThenBy(m => m.Name)
                .ToList() ?? new List<Milestone>();

            items.AddRange(milestones.Select(m =>
            {
                var label = $"{m.Name} ({m.DueDate:dd MMM yyyy})";
                return new SelectListItem(label, m.Id.ToString(CultureInfo.InvariantCulture));
            }));

            if (currentMilestoneId.HasValue && items.All(i => i.Value != currentMilestoneId.Value.ToString(CultureInfo.InvariantCulture)))
            {
                var milestone = await _context.Milestones
                    .AsNoTracking()
                    .FirstOrDefaultAsync(m => m.Id == currentMilestoneId.Value);

                if (milestone != null)
                {
                    var text = $"{milestone.Name} (not currently linked)";
                    items.Add(new SelectListItem(text, currentMilestoneId.Value.ToString(CultureInfo.InvariantCulture)));
                }
            }

            return items;
        }

        private static List<SelectListItem> BuildSelectList(IEnumerable<string> sourceItems, string? currentValue, string placeholder, bool includeOther = true)
        {
            var normalisedItems = sourceItems
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                .Select(s => new SelectListItem(s, s))
                .ToList();

            if (!string.IsNullOrWhiteSpace(currentValue) &&
                normalisedItems.All(i => !string.Equals(i.Value, currentValue, StringComparison.OrdinalIgnoreCase)))
            {
                normalisedItems.Insert(0, new SelectListItem(currentValue, currentValue));
            }

            var result = new List<SelectListItem>
            {
                new SelectListItem(placeholder, string.Empty)
            };

            result.AddRange(normalisedItems);

            if (includeOther)
            {
                result.Add(new SelectListItem("Other (please specify)", OtherOptionValue));
            }

            return result;
        }

        private void ValidateKpiOtherFields(ProjectKpiInputModel input)
        {
            input.UnitOfMeasureOther = SanitiseText(input.UnitOfMeasureOther);
            input.DataSourceOther = SanitiseText(input.DataSourceOther);

            if (string.Equals(input.UnitOfMeasure, OtherOptionValue, StringComparison.Ordinal) && string.IsNullOrWhiteSpace(input.UnitOfMeasureOther))
            {
                ModelState.AddModelError(nameof(ProjectKpiInputModel.UnitOfMeasureOther), "Enter a unit of measure.");
            }

            if (string.Equals(input.DataSource, OtherOptionValue, StringComparison.Ordinal) && string.IsNullOrWhiteSpace(input.DataSourceOther))
            {
                ModelState.AddModelError(nameof(ProjectKpiInputModel.DataSourceOther), "Enter a data source.");
            }
        }

        private async Task<int?> ResolveObjectiveIdAsync(ProjectKpiInputModel input)
        {
            if (!input.ObjectiveId.HasValue)
            {
                return null;
            }

            var linked = await _context.ProjectObjectives
                .AnyAsync(po => po.ProjectId == input.ProjectId && po.ObjectiveId == input.ObjectiveId.Value);

            if (!linked)
            {
                ModelState.AddModelError(nameof(ProjectKpiInputModel.ObjectiveId), "Select an objective that is linked to this project.");
                return null;
            }

            return input.ObjectiveId.Value;
        }

        private async Task<int?> ResolveMilestoneIdAsync(ProjectKpiInputModel input)
        {
            if (!input.MilestoneId.HasValue)
            {
                return null;
            }

            var milestone = await _context.Milestones
                .FirstOrDefaultAsync(m => m.Id == input.MilestoneId.Value && m.ProjectId == input.ProjectId && !m.IsDeleted);

            if (milestone == null)
            {
                ModelState.AddModelError(nameof(ProjectKpiInputModel.MilestoneId), "Select a milestone that belongs to this project.");
                return null;
            }

            return milestone.Id;
        }

        private static string? ResolveSelectedValue(string? selectedValue, string? otherValue)
        {
            if (string.Equals(selectedValue, OtherOptionValue, StringComparison.Ordinal))
            {
                return string.IsNullOrWhiteSpace(otherValue) ? null : otherValue;
            }

            return SanitiseText(selectedValue);
        }

        private static string? SanitiseText(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        // GET: Project/CreateMilestone
        [HttpGet]
        public async Task<IActionResult> CreateMilestone(int projectId)
        {
            var project = await GetProjectForKpiAsync(projectId);
            if (project == null)
            {
                return NotFound();
            }

            var input = new ProjectMilestoneInputModel
            {
                ProjectId = projectId,
                Status = MilestoneStatuses.First().Value,
                DueDate = DateTime.UtcNow.Date
            };

            var viewModel = await BuildMilestoneFormViewModelAsync(project, input, isEdit: false);
            return View("CreateMilestone", viewModel);
        }

        // POST: Project/CreateMilestone
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateMilestone(ProjectMilestoneInputModel input)
        {
            var project = await GetProjectForKpiAsync(input.ProjectId);
            if (project == null)
            {
                return NotFound();
            }

            NormaliseMilestoneInput(input);
            ValidateMilestoneInput(input);
            await ValidateMilestoneObjectiveAsync(input);

            if (!ModelState.IsValid)
            {
                var viewModel = await BuildMilestoneFormViewModelAsync(project, input, isEdit: false);
                return View("CreateMilestone", viewModel);
            }

            var milestone = new Milestone
            {
                ProjectId = input.ProjectId,
                ObjectiveId = input.ObjectiveId,
                Name = input.Name!,
                Description = input.Description,
                DueDate = input.DueDate!.Value,
                ActualDate = input.ActualDate,
                Status = input.Status!,
                ProgressPercent = input.ProgressPercent,
                Notes = input.Notes,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Milestones.Add(milestone);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Milestone created successfully.";
            return RedirectToAction(nameof(Details), new { id = input.ProjectId, tab = "milestones" });
        }

        // GET: Project/EditMilestone
        [HttpGet]
        public async Task<IActionResult> EditMilestone(int projectId, int milestoneId)
        {
            var project = await GetProjectForKpiAsync(projectId);
            if (project == null)
            {
                return NotFound();
            }

            var milestone = project.Milestones.FirstOrDefault(m => m.Id == milestoneId && !m.IsDeleted);
            if (milestone == null)
            {
                return NotFound();
            }

            var input = new ProjectMilestoneInputModel
            {
                ProjectId = projectId,
                MilestoneId = milestoneId,
                Name = milestone.Name,
                Description = milestone.Description,
                DueDate = milestone.DueDate,
                ActualDate = milestone.ActualDate,
                Status = milestone.Status,
                ProgressPercent = milestone.ProgressPercent,
                ObjectiveId = milestone.ObjectiveId,
                Notes = milestone.Notes
            };

            var viewModel = await BuildMilestoneFormViewModelAsync(project, input, isEdit: true);
            return View("EditMilestone", viewModel);
        }

        // POST: Project/UpdateMilestone
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateMilestone(ProjectMilestoneInputModel input)
        {
            if (!input.MilestoneId.HasValue)
            {
                return BadRequest();
            }

            var project = await GetProjectForKpiAsync(input.ProjectId);
            if (project == null)
            {
                return NotFound();
            }

            NormaliseMilestoneInput(input);
            ValidateMilestoneInput(input);
            await ValidateMilestoneObjectiveAsync(input);

            var milestone = await _context.Milestones
                .FirstOrDefaultAsync(m => m.Id == input.MilestoneId.Value && m.ProjectId == input.ProjectId && !m.IsDeleted);

            if (milestone == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                var viewModel = await BuildMilestoneFormViewModelAsync(project, input, isEdit: true);
                return View("EditMilestone", viewModel);
            }

            milestone.Name = input.Name!;
            milestone.Description = input.Description;
            milestone.DueDate = input.DueDate!.Value;
            milestone.ActualDate = input.ActualDate;
            milestone.Status = input.Status!;
            milestone.ProgressPercent = input.ProgressPercent;
            milestone.ObjectiveId = input.ObjectiveId;
            milestone.Notes = input.Notes;
            milestone.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Milestone updated successfully.";
            return RedirectToAction(nameof(Details), new { id = input.ProjectId, tab = "milestones" });
        }

        private async Task<ProjectMilestoneFormViewModel> BuildMilestoneFormViewModelAsync(Project project, ProjectMilestoneInputModel input, bool isEdit)
        {
            var statusOptions = BuildMilestoneStatusOptions(input.Status);
            var objectiveOptions = await BuildObjectiveOptionsAsync(project, input.ObjectiveId);

            return new ProjectMilestoneFormViewModel
            {
                Input = input,
                ProjectTitle = project.Title,
                ProjectSummary = CreateProjectSummary(project),
                StatusOptions = statusOptions,
                ObjectiveOptions = objectiveOptions,
                ShowDeleteButton = isEdit
            };
        }

        private static IEnumerable<SelectListItem> BuildMilestoneStatusOptions(string? currentValue)
        {
            return MilestoneStatuses
                .Select(status => new SelectListItem(status.Label, status.Value, string.Equals(status.Value, currentValue, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        private static void NormaliseMilestoneInput(ProjectMilestoneInputModel input)
        {
            input.Name = SanitiseText(input.Name) ?? string.Empty;
            input.Description = SanitiseText(input.Description);
            input.Status = SanitiseText(input.Status)?.ToLowerInvariant();
            input.Notes = SanitiseText(input.Notes);

            if (input.DueDate.HasValue)
            {
                input.DueDate = input.DueDate.Value.Date;
            }

            if (input.ActualDate.HasValue)
            {
                input.ActualDate = input.ActualDate.Value.Date;
            }
        }

        private void ValidateMilestoneInput(ProjectMilestoneInputModel input)
        {
            if (string.IsNullOrWhiteSpace(input.Name))
            {
                ModelState.AddModelError(nameof(ProjectMilestoneInputModel.Name), "Enter a milestone name.");
            }

            if (!input.DueDate.HasValue)
            {
                ModelState.AddModelError(nameof(ProjectMilestoneInputModel.DueDate), "Enter a due date.");
            }

            if (string.IsNullOrWhiteSpace(input.Status) || !MilestoneStatuses.Any(status => string.Equals(status.Value, input.Status, StringComparison.OrdinalIgnoreCase)))
            {
                ModelState.AddModelError(nameof(ProjectMilestoneInputModel.Status), "Select a valid status.");
            }

            if (input.ProgressPercent.HasValue && (input.ProgressPercent < 0 || input.ProgressPercent > 100))
            {
                ModelState.AddModelError(nameof(ProjectMilestoneInputModel.ProgressPercent), "Enter progress as a percentage between 0 and 100.");
            }
        }

        private async Task ValidateMilestoneObjectiveAsync(ProjectMilestoneInputModel input)
        {
            if (!input.ObjectiveId.HasValue)
            {
                return;
            }

            var linked = await _context.ProjectObjectives
                .AnyAsync(po => po.ProjectId == input.ProjectId && po.ObjectiveId == input.ObjectiveId.Value);

            if (!linked)
            {
                ModelState.AddModelError(nameof(ProjectMilestoneInputModel.ObjectiveId), "Select an objective that is linked to this project.");
            }
        }

        private async Task<Project?> GetProjectForActionAsync(int projectId)
        {
            var project = await _context.Projects
                .Where(p => p.Id == projectId && !p.IsDeleted)
                .Include(p => p.Actions.Where(a => !a.IsDeleted))
                    .ThenInclude(a => a.RiskActions)
                .Include(p => p.Actions.Where(a => !a.IsDeleted))
                    .ThenInclude(a => a.IssueActions)
                .Include(p => p.Risks)
                .Include(p => p.Issues)
                .Include(p => p.Decisions)
                .AsSplitQuery()
                .FirstOrDefaultAsync();

            if (project == null)
            {
                return null;
            }

            project.Risks = project.Risks.Where(r => !r.IsDeleted).OrderBy(r => r.Title).ToList();
            project.Issues = project.Issues.Where(i => !i.IsDeleted).OrderBy(i => i.Title).ToList();
            project.Actions = project.Actions.Where(a => !a.IsDeleted).OrderBy(a => a.Title).ToList();
            project.Decisions = project.Decisions.Where(d => !d.IsDeleted).OrderByDescending(d => d.DecisionDate ?? d.CreatedAt).ToList();

            return project;
        }

        private async Task<ProjectActionFormViewModel> BuildActionFormViewModelAsync(Project project, ProjectActionInputModel input, bool showDeleteButton, int? currentActionId = null)
        {
            var textInfo = CultureInfo.InvariantCulture.TextInfo;
            var statusOptions = ActionStatusValues
                .Select(value => new SelectListItem(textInfo.ToTitleCase(value.Replace("_", " ")), value, string.Equals(input.Status, value, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            var priorityOptions = ActionPriorityValues
                .Select(value => new SelectListItem(textInfo.ToTitleCase(value), value, string.Equals(input.Priority, value, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            var actionSources = await _context.ActionSources
                .Where(a => a.IsActive)
                .OrderBy(a => a.SortOrder)
                .ToListAsync();

            var actionSourceOptions = actionSources
                .Select(source => new SelectListItem(source.Name, source.Id.ToString(CultureInfo.InvariantCulture), input.ActionSourceId == source.Id))
                .ToList();

            var businessAreas = await _productsApiService.GetBusinessAreasAsync();
            var businessAreaOptions = businessAreas
                .Select(area => new SelectListItem(area, area, string.Equals(input.BusinessArea, area, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            var riskOptions = project.Risks
                .Select(risk => new SelectListItem(risk.Title, risk.Id.ToString(CultureInfo.InvariantCulture), input.LinkedRiskIds.Contains(risk.Id)))
                .ToList();

            var issueOptions = project.Issues
                .Select(issue => new SelectListItem(issue.Title, issue.Id.ToString(CultureInfo.InvariantCulture), input.LinkedIssueIds.Contains(issue.Id)))
                .ToList();

            var decisionOptions = project.Decisions
                .Select(decision => new SelectListItem(decision.Title, decision.Id.ToString(CultureInfo.InvariantCulture), input.DecisionId == decision.Id))
                .ToList();

            var parentActionOptions = project.Actions
                .Where(a => !currentActionId.HasValue || a.Id != currentActionId.Value)
                .Select(action => new SelectListItem(action.Title, action.Id.ToString(CultureInfo.InvariantCulture), input.ParentActionId == action.Id))
                .ToList();

            string? contextDescription = null;
            if (!string.IsNullOrWhiteSpace(input.InitiatingEntityType) && input.InitiatingEntityId.HasValue)
            {
                switch (input.InitiatingEntityType.ToLowerInvariant())
                {
                    case "risk":
                        var contextRisk = project.Risks.FirstOrDefault(r => r.Id == input.InitiatingEntityId.Value);
                        if (contextRisk != null)
                        {
                            contextDescription = $"This action will be linked to risk \"{contextRisk.Title}\".";
                        }
                        break;
                    case "issue":
                        var contextIssue = project.Issues.FirstOrDefault(i => i.Id == input.InitiatingEntityId.Value);
                        if (contextIssue != null)
                        {
                            contextDescription = $"This action will be linked to issue \"{contextIssue.Title}\".";
                        }
                        break;
                    case "decision":
                        var contextDecision = project.Decisions.FirstOrDefault(d => d.Id == input.InitiatingEntityId.Value);
                        if (contextDecision != null)
                        {
                            contextDescription = $"This action will be linked to decision \"{contextDecision.Title}\".";
                        }
                        break;
                }
            }

            return new ProjectActionFormViewModel
            {
                Input = input,
                ProjectTitle = project.Title,
                ProjectSummary = CreateProjectSummary(project),
                LinkedContextDescription = contextDescription,
                StatusOptions = statusOptions,
                PriorityOptions = priorityOptions,
                ActionSourceOptions = actionSourceOptions,
                BusinessAreaOptions = businessAreaOptions,
                RiskOptions = riskOptions,
                IssueOptions = issueOptions,
                DecisionOptions = decisionOptions,
                ParentActionOptions = parentActionOptions,
                ShowDeleteButton = showDeleteButton
            };
        }

        private async Task<Project?> GetProjectForIssueAsync(int projectId)
        {
            var project = await _context.Projects
                .Where(p => p.Id == projectId && !p.IsDeleted)
                .Include(p => p.Risks)
                .Include(p => p.Actions.Where(a => !a.IsDeleted))
                .Include(p => p.Issues.Where(i => !i.IsDeleted))
                    .ThenInclude(i => i.IssueActions)
                .AsSplitQuery()
                .FirstOrDefaultAsync();

            if (project == null)
            {
                return null;
            }

            project.Risks = project.Risks.Where(r => !r.IsDeleted).OrderBy(r => r.Title).ToList();
            project.Actions = project.Actions.Where(a => !a.IsDeleted).OrderBy(a => a.Title).ToList();
            project.Issues = project.Issues.Where(i => !i.IsDeleted).OrderByDescending(i => i.DetectedDate).ToList();

            return project;
        }

        private async Task<ProjectIssueFormViewModel> BuildIssueFormViewModelAsync(Project project, ProjectIssueInputModel input, bool showDeleteButton)
        {
            var textInfo = CultureInfo.InvariantCulture.TextInfo;
            var severityOptions = IssueSeverityValues
                .Select(value => new SelectListItem(textInfo.ToTitleCase(value), value, string.Equals(input.Severity, value, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            var statusOptions = IssueStatusValues
                .Select(value => new SelectListItem(textInfo.ToTitleCase(value.Replace("_", " ")), value, string.Equals(input.Status, value, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            var riskOptions = new List<SelectListItem>
            {
                new SelectListItem("Not linked", string.Empty, !input.SourceRiskId.HasValue)
            };
            riskOptions.AddRange(project.Risks
                .Select(risk => new SelectListItem(risk.Title, risk.Id.ToString(CultureInfo.InvariantCulture), input.SourceRiskId == risk.Id))
                .ToList());

            var actionOptions = project.Actions
                .Select(action => new SelectListItem(action.Title, action.Id.ToString(CultureInfo.InvariantCulture), input.LinkedActionIds.Contains(action.Id)))
                .ToList();

            var businessAreas = await _productsApiService.GetBusinessAreasAsync();
            var businessAreaOptions = businessAreas
                .Select(area => new SelectListItem(area, area, string.Equals(input.BusinessArea, area, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            string? contextDescription = null;
            if (input.SourceRiskId.HasValue)
            {
                var contextRisk = project.Risks.FirstOrDefault(r => r.Id == input.SourceRiskId.Value);
                if (contextRisk != null)
                {
                    contextDescription = $"This issue is linked to risk \"{contextRisk.Title}\".";
                }
            }

            return new ProjectIssueFormViewModel
            {
                Input = input,
                ProjectTitle = project.Title,
                ProjectSummary = CreateProjectSummary(project),
                ContextDescription = contextDescription,
                SeverityOptions = severityOptions,
                StatusOptions = statusOptions,
                RiskOptions = riskOptions,
                ActionOptions = actionOptions,
                BusinessAreaOptions = businessAreaOptions,
                ShowDeleteButton = showDeleteButton
            };
        }

        [HttpGet]
        public async Task<IActionResult> CreateIssue(int projectId, int? sourceRiskId)
        {
            var project = await GetProjectForIssueAsync(projectId);
            if (project == null)
            {
                return NotFound();
            }

            var input = new ProjectIssueInputModel
            {
                ProjectId = projectId,
                DetectedDate = DateTime.Today
            };

            if (sourceRiskId.HasValue && project.Risks.Any(r => r.Id == sourceRiskId.Value))
            {
                input.SourceRiskId = sourceRiskId;
            }

            var viewModel = await BuildIssueFormViewModelAsync(project, input, showDeleteButton: false);
            return View("CreateIssue", viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> EditIssue(int projectId, int issueId)
        {
            var project = await GetProjectForIssueAsync(projectId);
            if (project == null)
            {
                return NotFound();
            }

            var issue = await _context.Issues
                .Include(i => i.IssueActions)
                .FirstOrDefaultAsync(i => i.Id == issueId && i.ProjectId == projectId && !i.IsDeleted);

            if (issue == null)
            {
                return NotFound();
            }

            var input = new ProjectIssueInputModel
            {
                ProjectId = projectId,
                IssueId = issueId,
                Title = issue.Title,
                Description = issue.Description,
                Severity = issue.Severity,
                Status = issue.Status,
                DetectedDate = issue.DetectedDate,
                TargetResolutionDate = issue.TargetResolutionDate,
                Category = issue.Category,
                Workaround = issue.Workaround,
                ResolutionSummary = issue.ResolutionSummary,
                FipsId = issue.FipsId,
                BusinessArea = issue.BusinessArea,
                SourceType = issue.SourceType,
                SourceReference = issue.SourceReference,
                SourceRecordUrl = issue.SourceRecordUrl,
                SourceRiskId = issue.SourceRiskId,
                LinkedActionIds = issue.IssueActions.Select(ia => ia.ActionId).ToList()
            };

            var viewModel = await BuildIssueFormViewModelAsync(project, input, showDeleteButton: true);
            return View("EditIssue", viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> CreateSuccess(int projectId)
        {
            var projectSummary = await GetProjectSummaryAsync(projectId);

            if (projectSummary == null)
            {
                return NotFound();
            }

            var viewModel = new ProjectSuccessFormViewModel
            {
                ProjectTitle = projectSummary.Title,
                ProjectSummary = projectSummary,
                Input = new ProjectSuccessInputModel
                {
                    ProjectId = projectSummary.Id
                }
            };

            return View("CreateSuccess", viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> EditSuccess(int projectId, int successId)
        {
            var success = await _context.ProjectSuccesses
                .Include(s => s.Project)
                .FirstOrDefaultAsync(s => s.Id == successId && s.ProjectId == projectId);

            if (success == null || success.Project.IsDeleted)
            {
                return NotFound();
            }

            var viewModel = new ProjectSuccessFormViewModel
            {
                ProjectTitle = success.Project.Title,
                ProjectSummary = CreateProjectSummary(success.Project),
                Input = new ProjectSuccessInputModel
                {
                    ProjectId = projectId,
                    SuccessId = successId,
                    SuccessDescription = success.SuccessDescription,
                    IsReportedToSlt = success.IsReportedToSlt
                },
                RecordedAt = success.RecordedAt,
                RecordedByName = success.RecordedByName,
                RecordedByEmail = success.RecordedByEmail
            };

            return View("EditSuccess", viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> SuccessDetails(int projectId, int successId)
        {
            var success = await _context.ProjectSuccesses
                .Include(s => s.Project)
                .FirstOrDefaultAsync(s => s.Id == successId && s.ProjectId == projectId);

            if (success == null || success.Project.IsDeleted)
            {
                return NotFound();
            }

            var viewModel = new ProjectSuccessDetailsViewModel
            {
                ProjectId = success.ProjectId,
                SuccessId = success.Id,
                ProjectTitle = success.Project.Title,
                SuccessDescription = success.SuccessDescription,
                RecordedAt = success.RecordedAt,
                RecordedByEmail = success.RecordedByEmail,
                RecordedByName = success.RecordedByName,
                IsReportedToSlt = success.IsReportedToSlt
            };

            return View("SuccessDetails", viewModel);
        }

        private static List<SelectListItem> BuildOutcomeConfidenceOptions(string? selectedValue)
        {
            return OutcomeConfidenceValues
                .Select(value => new SelectListItem(value, value, string.Equals(value, selectedValue, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        private static ProjectOutcomeFormViewModel BuildOutcomeFormViewModel(ProjectSummaryViewModel summary, ProjectOutcomeInputModel input, bool showDeleteButton)
        {
            return new ProjectOutcomeFormViewModel
            {
                Input = input,
                ProjectTitle = summary.Title,
                ProjectSummary = summary,
                ConfidenceOptions = BuildOutcomeConfidenceOptions(input.ConfidenceLevel),
                ShowDeleteButton = showDeleteButton
            };
        }

        [HttpGet]
        public async Task<IActionResult> CreateOutcome(int projectId)
        {
            var projectSummary = await GetProjectSummaryAsync(projectId);

            if (projectSummary == null)
            {
                return NotFound();
            }

            var input = new ProjectOutcomeInputModel
            {
                ProjectId = projectSummary.Id,
                ConfidenceLevel = "Medium"
            };

            var viewModel = BuildOutcomeFormViewModel(projectSummary, input, showDeleteButton: false);
            return View("CreateOutcome", viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> EditOutcome(int projectId, int outcomeId)
        {
            var outcome = await _context.ProjectOutcomes
                .Include(o => o.Project)
                .FirstOrDefaultAsync(o => o.Id == outcomeId && o.ProjectId == projectId);

            if (outcome == null || outcome.Project.IsDeleted)
            {
                return NotFound();
            }

            var input = new ProjectOutcomeInputModel
            {
                ProjectId = projectId,
                OutcomeId = outcomeId,
                Outcome = outcome.Outcome,
                MeasureOfSuccess = outcome.MeasureOfSuccess,
                ConfidenceLevel = outcome.ConfidenceLevel,
                ConfidenceExplanation = outcome.ConfidenceExplanation,
                SortOrder = outcome.SortOrder
            };

            var viewModel = BuildOutcomeFormViewModel(CreateProjectSummary(outcome.Project), input, showDeleteButton: true);
            return View("EditOutcome", viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> OutcomeDetails(int projectId, int outcomeId)
        {
            var outcome = await _context.ProjectOutcomes
                .Include(o => o.Project)
                .FirstOrDefaultAsync(o => o.Id == outcomeId && o.ProjectId == projectId);

            if (outcome == null || outcome.Project.IsDeleted)
            {
                return NotFound();
            }

            var viewModel = new ProjectOutcomeDetailsViewModel
            {
                ProjectId = outcome.ProjectId,
                OutcomeId = outcome.Id,
                ProjectTitle = outcome.Project.Title,
                Outcome = outcome.Outcome,
                MeasureOfSuccess = outcome.MeasureOfSuccess,
                ConfidenceLevel = outcome.ConfidenceLevel,
                ConfidenceExplanation = outcome.ConfidenceExplanation,
                SortOrder = outcome.SortOrder,
                CreatedAt = outcome.CreatedAt,
                UpdatedAt = outcome.UpdatedAt,
                ProjectSummary = CreateProjectSummary(outcome.Project),
                RaidSummary = new RaidLinkSummaryViewModel()
            };

            return View("OutcomeDetails", viewModel);
        }

        private async Task<Project?> GetProjectForDependencyAsync(int projectId)
        {
            return await _context.Projects
                .Where(p => p.Id == projectId && !p.IsDeleted)
                .Include(p => p.DependenciesAsSource)
                .Include(p => p.DependenciesAsTarget)
                .AsSplitQuery()
                .FirstOrDefaultAsync();
        }

        private async Task<ProjectDependencyFormViewModel> BuildDependencyFormViewModelAsync(Project project, ProjectDependencyInputModel input, bool showDeleteButton)
        {
            var targetEntityTypeOptions = new List<SelectListItem>
            {
                new("Select entity type", string.Empty),
                new("Project", "Project"),
                new("Milestone", "Milestone"),
                new("Issue", "Issue")
            };

            foreach (var option in targetEntityTypeOptions)
            {
                option.Selected = string.Equals(option.Value, input.TargetEntityType, StringComparison.OrdinalIgnoreCase);
            }

            var dependencyTypeOptions = new List<SelectListItem>
            {
                new("Blocking", "Blocking"),
                new("Related", "Related"),
                new("Prerequisite", "Prerequisite"),
                new("Successor", "Successor")
            };

            foreach (var option in dependencyTypeOptions)
            {
                option.Selected = string.Equals(option.Value, input.DependencyType, StringComparison.OrdinalIgnoreCase);
            }

            var statusOptions = new List<SelectListItem>
            {
                new("Active", "Active"),
                new("Resolved", "Resolved"),
                new("Cancelled", "Cancelled")
            };

            foreach (var option in statusOptions)
            {
                option.Selected = string.Equals(option.Value, input.Status, StringComparison.OrdinalIgnoreCase);
            }

            var selectedIdValue = input.TargetEntityId?.ToString();

            var projectOptions = await _context.Projects
                .AsNoTracking()
                .Where(p => !p.IsDeleted && p.Id != project.Id)
                .OrderBy(p => p.Title)
                .Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = string.IsNullOrWhiteSpace(p.Title) ? $"Project {p.Id}" : p.Title
                })
                .ToListAsync();

            if (string.Equals(input.TargetEntityType, "Project", StringComparison.OrdinalIgnoreCase)
                && input.TargetEntityId.HasValue
                && projectOptions.All(o => !string.Equals(o.Value, selectedIdValue, StringComparison.Ordinal)))
            {
                var selectedProject = await _context.Projects
                    .AsNoTracking()
                    .Where(p => p.Id == input.TargetEntityId.Value)
                    .Select(p => new SelectListItem
                    {
                        Value = p.Id.ToString(),
                        Text = string.IsNullOrWhiteSpace(p.Title) ? $"Project {p.Id}" : p.Title
                    })
                    .FirstOrDefaultAsync();

                if (selectedProject != null)
                {
                    projectOptions.Insert(0, selectedProject);
                }
            }

            var milestoneOptions = await _context.Milestones
                .AsNoTracking()
                .Where(m => !m.IsDeleted && m.ProjectId == project.Id)
                .OrderBy(m => m.DueDate)
                .ThenBy(m => m.Name)
                .Select(m => new SelectListItem
                {
                    Value = m.Id.ToString(),
                    Text = string.IsNullOrWhiteSpace(m.Name)
                        ? $"Milestone {m.Id}"
                        : $"{m.Name} ({m.DueDate: d MMMM yyyy})"
                })
                .ToListAsync();

            if (string.Equals(input.TargetEntityType, "Milestone", StringComparison.OrdinalIgnoreCase)
                && input.TargetEntityId.HasValue
                && milestoneOptions.All(o => !string.Equals(o.Value, selectedIdValue, StringComparison.Ordinal)))
            {
                var selectedMilestone = await _context.Milestones
                    .AsNoTracking()
                    .Where(m => m.Id == input.TargetEntityId.Value)
                    .Select(m => new SelectListItem
                    {
                        Value = m.Id.ToString(),
                        Text = string.IsNullOrWhiteSpace(m.Name)
                            ? $"Milestone {m.Id}"
                            : $"{m.Name} ({m.DueDate: d MMMM yyyy})"
                    })
                    .FirstOrDefaultAsync();

                if (selectedMilestone != null)
                {
                    milestoneOptions.Insert(0, selectedMilestone);
                }
            }

            var issueOptions = await _context.Issues
                .AsNoTracking()
                .Where(i => !i.IsDeleted && i.ProjectId == project.Id)
                .OrderBy(i => i.Title)
                .Select(i => new SelectListItem
                {
                    Value = i.Id.ToString(),
                    Text = string.IsNullOrWhiteSpace(i.Title)
                        ? $"Issue {i.Id}"
                        : string.IsNullOrWhiteSpace(i.Severity)
                            ? i.Title
                            : $"{i.Title} ({i.Severity})"
                })
                .ToListAsync();

            if (string.Equals(input.TargetEntityType, "Issue", StringComparison.OrdinalIgnoreCase)
                && input.TargetEntityId.HasValue
                && issueOptions.All(o => !string.Equals(o.Value, selectedIdValue, StringComparison.Ordinal)))
            {
                var selectedIssue = await _context.Issues
                    .AsNoTracking()
                    .Where(i => i.Id == input.TargetEntityId.Value)
                    .Select(i => new SelectListItem
                    {
                        Value = i.Id.ToString(),
                        Text = string.IsNullOrWhiteSpace(i.Title)
                            ? $"Issue {i.Id}"
                            : string.IsNullOrWhiteSpace(i.Severity)
                                ? i.Title
                                : $"{i.Title} ({i.Severity})"
                    })
                    .FirstOrDefaultAsync();

                if (selectedIssue != null)
                {
                    issueOptions.Insert(0, selectedIssue);
                }
            }

            var targetOptionsByType = new Dictionary<string, IReadOnlyList<SelectListItem>>(StringComparer.OrdinalIgnoreCase)
            {
                ["Project"] = projectOptions,
                ["Milestone"] = milestoneOptions,
                ["Issue"] = issueOptions
            };

            if (input.TargetEntityId.HasValue)
            {
                var (title, summary) = await GetDependencyEntityDisplayAsync(input.TargetEntityType, input.TargetEntityId.Value);
                input.TargetEntityTitle = title;
                if (!string.IsNullOrWhiteSpace(summary))
                {
                    input.TargetEntityTitle = string.IsNullOrWhiteSpace(input.TargetEntityTitle)
                        ? summary
                        : $"{input.TargetEntityTitle} ({summary})";
                }
            }

            return new ProjectDependencyFormViewModel
            {
                Input = input,
                ProjectTitle = project.Title,
                ProjectSummary = CreateProjectSummary(project),
                TargetEntityTypeOptions = targetEntityTypeOptions,
                DependencyTypeOptions = dependencyTypeOptions,
                StatusOptions = statusOptions,
                TargetOptionsByType = targetOptionsByType,
                ShowDeleteButton = showDeleteButton,
                SelectedEntitySummary = input.TargetEntityTitle
            };
        }

        private async Task ValidateDependencyInputAsync(ProjectDependencyInputModel input)
        {
            var allowedTypes = new[] { "Project", "Milestone", "Issue" };

            if (string.IsNullOrWhiteSpace(input.TargetEntityType))
            {
                ModelState.AddModelError(nameof(ProjectDependencyInputModel.TargetEntityType), "Select what this project depends on.");
            }
            else if (!allowedTypes.Any(t => string.Equals(t, input.TargetEntityType, StringComparison.OrdinalIgnoreCase)))
            {
                ModelState.AddModelError(nameof(ProjectDependencyInputModel.TargetEntityType), "Select a valid entity type.");
            }

            if (!input.TargetEntityId.HasValue)
            {
                ModelState.AddModelError(nameof(ProjectDependencyInputModel.TargetEntityId), "Select an entity to link.");
            }
            else
            {
                var (title, _) = await GetDependencyEntityDisplayAsync(input.TargetEntityType, input.TargetEntityId.Value);
                if (title == null)
                {
                    ModelState.AddModelError(nameof(ProjectDependencyInputModel.TargetEntityId), "Select a valid entity to link.");
                }
            }

            if (!IsValidDependencyStatus(input.Status))
            {
                ModelState.AddModelError(nameof(ProjectDependencyInputModel.Status), "Select a valid status.");
            }

            input.DependencyType = SanitiseText(string.IsNullOrWhiteSpace(input.DependencyType) ? "Related" : input.DependencyType);
            input.Status = SanitiseText(string.IsNullOrWhiteSpace(input.Status) ? "Active" : input.Status);
            input.Description = SanitiseText(input.Description);
        }

        private async Task<string?> GetDependencyEntitySummaryAsync(string? entityType, int entityId)
        {
            var (title, summary) = await GetDependencyEntityDisplayAsync(entityType, entityId);
            if (title == null && summary == null)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(title))
            {
                return summary;
            }

            return string.IsNullOrWhiteSpace(summary) ? title : $"{title} ({summary})";
        }

        private async Task<(string? Title, string? Summary)> GetDependencyEntityDisplayAsync(string? entityType, int entityId)
        {
            if (string.IsNullOrWhiteSpace(entityType))
            {
                return (null, null);
            }

            switch (entityType)
            {
                case "Project":
                    var project = await _context.Projects
                        .AsNoTracking()
                        .Where(p => p.Id == entityId && !p.IsDeleted)
                        .Select(p => new { p.Id, p.Title })
                        .FirstOrDefaultAsync();
                    return project == null
                        ? (null, null)
                        : (project.Title, $"Project #{project.Id}");

                case "Milestone":
                    var milestone = await _context.Milestones
                        .AsNoTracking()
                        .Where(m => m.Id == entityId && !m.IsDeleted)
                        .Select(m => new { m.Name, m.DueDate, ProjectTitle = m.Project != null ? m.Project.Title : null })
                        .FirstOrDefaultAsync();
                    if (milestone == null)
                    {
                        return (null, null);
                    }

                    var milestoneParts = new List<string>();
                    if (milestone.DueDate != default)
                    {
                        milestoneParts.Add($"Due {milestone.DueDate:dd MMM yyyy}");
                    }
                    if (!string.IsNullOrWhiteSpace(milestone.ProjectTitle))
                    {
                        milestoneParts.Add(milestone.ProjectTitle);
                    }

                    return (milestone.Name, milestoneParts.Count > 0 ? string.Join(", ", milestoneParts) : null);

                case "Issue":
                    var issue = await _context.Issues
                        .AsNoTracking()
                        .Where(i => i.Id == entityId && !i.IsDeleted)
                        .Select(i => new { i.Title, i.Severity, ProjectTitle = i.Project != null ? i.Project.Title : null })
                        .FirstOrDefaultAsync();
                    if (issue == null)
                    {
                        return (null, null);
                    }

                    var issueParts = new List<string>();
                    if (!string.IsNullOrWhiteSpace(issue.Severity))
                    {
                        issueParts.Add(issue.Severity);
                    }
                    if (!string.IsNullOrWhiteSpace(issue.ProjectTitle))
                    {
                        issueParts.Add(issue.ProjectTitle);
                    }

                    return (issue.Title, issueParts.Count > 0 ? string.Join(", ", issueParts) : null);

                default:
                    return (null, null);
            }
        }

        private static bool IsValidDependencyStatus(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return true;
            }

            return status.Equals("Active", StringComparison.OrdinalIgnoreCase)
                || status.Equals("Resolved", StringComparison.OrdinalIgnoreCase)
                || status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase);
        }

        private async Task<Project?> GetProjectForDecisionAsync(int projectId)
        {
            return await _context.Projects
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == projectId && !p.IsDeleted);
        }

    private async Task<Milestone?> LoadDecisionSourceMilestoneAsync(int? milestoneId, bool includeActions = false)
    {
        if (!milestoneId.HasValue)
        {
            ViewData.Remove("DecisionSourceMilestone");
            return null;
        }

        IQueryable<Milestone> query = _context.Milestones
            .Include(m => m.Project);

        if (includeActions)
        {
            query = query
                .Include(m => m.MilestoneActions)
                    .ThenInclude(ma => ma.Action);
        }

        var milestone = await query
            .FirstOrDefaultAsync(m => m.Id == milestoneId.Value && !m.IsDeleted);

        if (milestone != null)
        {
            ViewData["DecisionSourceMilestone"] = milestone;
        }
        else
        {
            ViewData.Remove("DecisionSourceMilestone");
        }

        return milestone;
    }

        private async Task<ProjectDecisionFormViewModel> BuildDecisionFormViewModelAsync(Project project, ProjectDecisionInputModel input, bool showDeleteButton)
        {
            input.LinkedRiskIds = input.LinkedRiskIds.Distinct().ToList();
            input.LinkedIssueIds = input.LinkedIssueIds.Distinct().ToList();
            input.LinkedActionIds = input.LinkedActionIds.Distinct().ToList();

            var statusOptions = DecisionStatusValues
                .Select(status => new SelectListItem(char.ToUpper(status[0]) + status[1..], status)
                {
                    Selected = string.Equals(status, input.Status, StringComparison.OrdinalIgnoreCase)
                })
                .ToList();

            if (string.IsNullOrWhiteSpace(input.Status))
            {
                input.Status = "pending";
                foreach (var option in statusOptions)
                {
                    option.Selected = option.Value == "pending";
                }
            }

            var businessAreas = await _productsApiService.GetBusinessAreasAsync();
            var businessAreaOptions = new List<SelectListItem>
            {
                new("Select business area", string.Empty)
            };

            businessAreaOptions.AddRange(businessAreas
                .OrderBy(area => area, StringComparer.OrdinalIgnoreCase)
                .Select(area => new SelectListItem(area, area)
                {
                    Selected = string.Equals(area, input.BusinessArea, StringComparison.OrdinalIgnoreCase)
                }));

            var riskOptions = await _context.Risks
                .AsNoTracking()
                .Where(r => r.ProjectId == project.Id && !r.IsDeleted)
                .OrderBy(r => r.Title)
                .Select(r => new SelectListItem(r.Title, r.Id.ToString(), input.LinkedRiskIds.Contains(r.Id)))
                .ToListAsync();

            var issueOptions = await _context.Issues
                .AsNoTracking()
                .Where(i => i.ProjectId == project.Id && !i.IsDeleted)
                .OrderBy(i => i.Title)
                .Select(i => new SelectListItem(i.Title, i.Id.ToString(), input.LinkedIssueIds.Contains(i.Id)))
                .ToListAsync();

            var actionOptions = await _context.Actions
                .AsNoTracking()
                .Where(a => a.ProjectId == project.Id && !a.IsDeleted)
                .OrderBy(a => a.Title)
                .Select(a => new SelectListItem(a.Title, a.Id.ToString(), input.LinkedActionIds.Contains(a.Id)))
                .ToListAsync();

            return new ProjectDecisionFormViewModel
            {
                Input = input,
                ProjectTitle = project.Title,
                ProjectSummary = CreateProjectSummary(project),
                StatusOptions = statusOptions,
                BusinessAreaOptions = businessAreaOptions,
                RiskOptions = riskOptions,
                IssueOptions = issueOptions,
                ActionOptions = actionOptions,
                ShowDeleteButton = showDeleteButton
            };
        }

        private void NormaliseDecisionInput(ProjectDecisionInputModel input)
        {
            input.Title = input.Title?.Trim() ?? string.Empty;
            input.Status = string.IsNullOrWhiteSpace(input.Status) ? "pending" : input.Status.Trim().ToLowerInvariant();
            input.Summary = SanitiseText(input.Summary);
            input.DecisionType = SanitiseText(input.DecisionType);
            input.BusinessArea = SanitiseText(input.BusinessArea);
            input.Outcome = SanitiseText(input.Outcome);
            input.Notes = SanitiseText(input.Notes);
            input.OwnerEmail = SanitiseText(input.OwnerEmail);
            input.FipsId = SanitiseText(input.FipsId);
            input.SourceType = SanitiseText(input.SourceType);
            input.SourceReference = SanitiseText(input.SourceReference);
            input.SourceRecordUrl = SanitiseText(input.SourceRecordUrl);

            input.LinkedRiskIds = input.LinkedRiskIds.Where(id => id > 0).Distinct().ToList();
            input.LinkedIssueIds = input.LinkedIssueIds.Where(id => id > 0).Distinct().ToList();
            input.LinkedActionIds = input.LinkedActionIds.Where(id => id > 0).Distinct().ToList();
        }

        private static readonly string[] DecisionStatusValues = { "pending", "approved", "rejected", "superseded" };

        private bool IsValidDecisionStatus(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return false;
            }

            return DecisionStatusValues.Any(value => string.Equals(value, status, StringComparison.OrdinalIgnoreCase));
        }

        private async Task ApplyDecisionRelationshipsAsync(Decision decision, IEnumerable<int> riskIds, IEnumerable<int> issueIds, IEnumerable<int> actionIds)
        {
            var projectId = decision.ProjectId ?? 0;

            var desiredRiskIds = new HashSet<int>(riskIds.Where(id => id > 0));
            await _context.Entry(decision).Collection(d => d.RiskDecisions).LoadAsync();

            foreach (var link in decision.RiskDecisions.Where(rd => !desiredRiskIds.Contains(rd.RiskId)).ToList())
            {
                _context.RiskDecisions.Remove(link);
            }

            var existingRiskIds = decision.RiskDecisions.Select(rd => rd.RiskId).ToHashSet();
            var missingRiskIds = desiredRiskIds.Except(existingRiskIds).ToList();
            if (missingRiskIds.Any())
            {
                var validRiskIds = await _context.Risks
                    .Where(r => missingRiskIds.Contains(r.Id) && r.ProjectId == projectId && !r.IsDeleted)
                    .Select(r => r.Id)
                    .ToListAsync();

                foreach (var riskId in validRiskIds)
                {
                    _context.RiskDecisions.Add(new RiskDecision { DecisionId = decision.Id, RiskId = riskId });
                }
            }

            var desiredIssueIds = new HashSet<int>(issueIds.Where(id => id > 0));
            await _context.Entry(decision).Collection(d => d.IssueDecisions).LoadAsync();

            foreach (var link in decision.IssueDecisions.Where(idl => !desiredIssueIds.Contains(idl.IssueId)).ToList())
            {
                _context.IssueDecisions.Remove(link);
            }

            var existingIssueIds = decision.IssueDecisions.Select(idl => idl.IssueId).ToHashSet();
            var missingIssueIds = desiredIssueIds.Except(existingIssueIds).ToList();
            if (missingIssueIds.Any())
            {
                var validIssueIds = await _context.Issues
                    .Where(i => missingIssueIds.Contains(i.Id) && i.ProjectId == projectId && !i.IsDeleted)
                    .Select(i => i.Id)
                    .ToListAsync();

                foreach (var issueId in validIssueIds)
                {
                    _context.IssueDecisions.Add(new IssueDecision { DecisionId = decision.Id, IssueId = issueId });
                }
            }

            var desiredActionIds = new HashSet<int>(actionIds.Where(id => id > 0));

            var actionsToClear = await _context.Actions
                .Where(a => a.DecisionId == decision.Id && !desiredActionIds.Contains(a.Id))
                .ToListAsync();

            foreach (var action in actionsToClear)
            {
                action.DecisionId = null;
                action.UpdatedAt = DateTime.UtcNow;
            }

            if (desiredActionIds.Any())
            {
                var actionsToLink = await _context.Actions
                    .Where(a => desiredActionIds.Contains(a.Id) && a.ProjectId == projectId && !a.IsDeleted)
                    .ToListAsync();

                foreach (var action in actionsToLink)
                {
                    action.DecisionId = decision.Id;
                    action.UpdatedAt = DateTime.UtcNow;
                }
            }
        }

        [HttpGet]
        public async Task<IActionResult> CreateDependency(int projectId)
        {
            var project = await GetProjectForDependencyAsync(projectId);
            if (project == null)
            {
                return NotFound();
            }

            var input = new ProjectDependencyInputModel
            {
                ProjectId = projectId,
                SourceEntityType = "Project",
                SourceEntityId = projectId,
                DependencyType = "Related",
                Status = "Active"
            };

            var viewModel = await BuildDependencyFormViewModelAsync(project, input, showDeleteButton: false);
            return View("CreateDependency", viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> DependencyDetails(int projectId, int dependencyId)
        {
            var project = await GetProjectForDependencyAsync(projectId);
            if (project == null)
            {
                return NotFound();
            }

            var dependency = await _context.Dependencies
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == dependencyId && d.SourceEntityType == "Project" && d.SourceEntityId == projectId);

            if (dependency == null)
            {
                return NotFound();
            }

            var sourceDisplay = await GetDependencyEntitySummaryAsync(dependency.SourceEntityType, dependency.SourceEntityId)
                                ?? $"{dependency.SourceEntityType} {dependency.SourceEntityId}";
            var targetDisplay = await GetDependencyEntitySummaryAsync(dependency.TargetEntityType, dependency.TargetEntityId)
                                ?? $"{dependency.TargetEntityType} {dependency.TargetEntityId}";

            var viewModel = new ProjectDependencyDetailsViewModel
            {
                ProjectId = projectId,
                ProjectTitle = project.Title,
                Dependency = dependency,
                SourceDisplay = sourceDisplay,
                TargetDisplay = targetDisplay,
                ProjectSummary = CreateProjectSummary(project)
            };

            return View("DependencyDetails", viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> EditDependency(int projectId, int dependencyId)
        {
            var project = await GetProjectForDependencyAsync(projectId);
            if (project == null)
            {
                return NotFound();
            }

            var dependency = await _context.Dependencies
                .FirstOrDefaultAsync(d => d.Id == dependencyId && d.SourceEntityType == "Project" && d.SourceEntityId == projectId);

            if (dependency == null)
            {
                return NotFound();
            }

            var input = new ProjectDependencyInputModel
            {
                ProjectId = projectId,
                DependencyId = dependency.Id,
                SourceEntityType = dependency.SourceEntityType,
                SourceEntityId = dependency.SourceEntityId,
                TargetEntityType = dependency.TargetEntityType,
                TargetEntityId = dependency.TargetEntityId,
                DependencyType = dependency.DependencyType,
                Status = dependency.Status,
                Description = dependency.Description
            };

            var (title, summary) = await GetDependencyEntityDisplayAsync(dependency.TargetEntityType, dependency.TargetEntityId);
            input.TargetEntityTitle = title;

            var viewModel = await BuildDependencyFormViewModelAsync(project, input, showDeleteButton: true);
            viewModel.SelectedEntitySummary = string.IsNullOrWhiteSpace(summary)
                ? input.TargetEntityTitle
                : string.IsNullOrWhiteSpace(input.TargetEntityTitle)
                    ? summary
                    : $"{input.TargetEntityTitle} ({summary})";

            return View("EditDependency", viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDependency([Bind(Prefix = "Input")] ProjectDependencyInputModel input)
        {
            input.SourceEntityType = "Project";
            input.SourceEntityId = input.ProjectId;

            var project = await GetProjectForDependencyAsync(input.ProjectId);
            if (project == null)
            {
                return NotFound();
            }

            await ValidateDependencyInputAsync(input);

            if (!ModelState.IsValid)
            {
                var invalidViewModel = await BuildDependencyFormViewModelAsync(project, input, showDeleteButton: false);
                return View("CreateDependency", invalidViewModel);
            }

            try
            {
                var exists = await _context.Dependencies.AnyAsync(d =>
                    d.SourceEntityType == input.SourceEntityType &&
                    d.SourceEntityId == input.SourceEntityId &&
                    d.TargetEntityType == input.TargetEntityType &&
                    d.TargetEntityId == input.TargetEntityId);

                if (exists)
                {
                    ModelState.AddModelError(string.Empty, "This dependency relationship already exists.");
                    var duplicateViewModel = await BuildDependencyFormViewModelAsync(project, input, showDeleteButton: false);
                    return View("CreateDependency", duplicateViewModel);
                }

                var dependency = new Dependency
                {
                    SourceEntityType = input.SourceEntityType,
                    SourceEntityId = input.SourceEntityId,
                    TargetEntityType = input.TargetEntityType!,
                    TargetEntityId = input.TargetEntityId!.Value,
                    DependencyType = SanitiseText(input.DependencyType) ?? "Related",
                    Description = SanitiseText(input.Description),
                    Status = SanitiseText(input.Status) ?? "Active",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Dependencies.Add(dependency);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Dependency added successfully.";
                return RedirectToAction(nameof(Details), new { id = input.ProjectId, tab = "dependencies" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding dependency for project {ProjectId}", input.ProjectId);
                TempData["ErrorMessage"] = "Error adding dependency. Please try again.";

                var viewModel = await BuildDependencyFormViewModelAsync(project, input, showDeleteButton: false);
                return View("CreateDependency", viewModel);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateDependency([Bind(Prefix = "Input")] ProjectDependencyInputModel input)
        {
            if (!input.DependencyId.HasValue)
            {
                return NotFound();
            }

            var dependency = await _context.Dependencies
                .FirstOrDefaultAsync(d => d.Id == input.DependencyId.Value && d.SourceEntityType == "Project");

            if (dependency == null)
            {
                TempData["ErrorMessage"] = "Dependency not found.";
                return RedirectToAction(nameof(Details), new { id = input.ProjectId, tab = "dependencies" });
            }

            var project = await GetProjectForDependencyAsync(dependency.SourceEntityId);
            if (project == null)
            {
                return NotFound();
            }

            input.ProjectId = dependency.SourceEntityId;
            input.SourceEntityType = dependency.SourceEntityType;
            input.SourceEntityId = dependency.SourceEntityId;
            input.TargetEntityType = dependency.TargetEntityType;
            input.TargetEntityId = dependency.TargetEntityId;

            if (!IsValidDependencyStatus(input.Status))
            {
                ModelState.AddModelError(nameof(ProjectDependencyInputModel.Status), "Select a valid status.");
            }

            if (!ModelState.IsValid)
            {
                var invalidViewModel = await BuildDependencyFormViewModelAsync(project, input, showDeleteButton: true);
                return View("EditDependency", invalidViewModel);
            }

            try
            {
                dependency.DependencyType = SanitiseText(input.DependencyType) ?? "Related";
                dependency.Status = SanitiseText(input.Status) ?? "Active";
                dependency.Description = SanitiseText(input.Description);
                dependency.UpdatedAt = DateTime.UtcNow;

                if (string.Equals(dependency.Status, "Resolved", StringComparison.OrdinalIgnoreCase))
                {
                    dependency.ResolvedDate = DateTime.UtcNow;
                    dependency.ResolvedByEmail = User.FindFirstValue(ClaimTypes.Email) ?? "unknown@example.com";
                    dependency.ResolvedByName = User.FindFirstValue(ClaimTypes.Name) ?? "Unknown user";
                }
                else
                {
                    dependency.ResolvedDate = null;
                    dependency.ResolvedByEmail = null;
                    dependency.ResolvedByName = null;
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Dependency updated successfully.";
                return RedirectToAction(nameof(Details), new { id = dependency.SourceEntityId, tab = "dependencies" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating dependency {DependencyId}", dependency.Id);
                TempData["ErrorMessage"] = "Error updating dependency. Please try again.";

                var viewModel = await BuildDependencyFormViewModelAsync(project, input, showDeleteButton: true);
                return View("EditDependency", viewModel);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDependency(int projectId, int dependencyId)
        {
            try
            {
                var dependency = await _context.Dependencies
                    .FirstOrDefaultAsync(d => d.Id == dependencyId && d.SourceEntityType == "Project");

                if (dependency == null)
                {
                    TempData["ErrorMessage"] = "Dependency not found.";
                    return RedirectToAction(nameof(Details), new { id = projectId, tab = "dependencies" });
                }

                _context.Dependencies.Remove(dependency);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Dependency deleted successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting dependency {DependencyId}", dependencyId);
                TempData["ErrorMessage"] = "Error deleting dependency. Please try again.";
            }

            return RedirectToAction(nameof(Details), new { id = projectId, tab = "dependencies" });
        }

        [HttpGet]
        public async Task<IActionResult> CreateDecision(int projectId, int? riskId, int? issueId, int? actionId, int? milestoneId)
        {
            var project = await GetProjectForDecisionAsync(projectId);
            if (project == null)
            {
                return NotFound();
            }

            var input = new ProjectDecisionInputModel
            {
                ProjectId = projectId
            };

            if (riskId.HasValue)
            {
                input.LinkedRiskIds.Add(riskId.Value);
            }

            if (issueId.HasValue)
            {
                input.LinkedIssueIds.Add(issueId.Value);
            }

            if (actionId.HasValue)
            {
                input.LinkedActionIds.Add(actionId.Value);
            }

            if (milestoneId.HasValue)
            {
                input.SourceMilestoneId = milestoneId;
                var milestoneContext = await LoadDecisionSourceMilestoneAsync(milestoneId, includeActions: true);
                if (milestoneContext != null && milestoneContext.MilestoneActions.Any())
                {
                    var milestoneActionIds = milestoneContext.MilestoneActions
                        .Select(ma => ma.ActionId)
                        .Distinct();

                    foreach (var milestoneActionId in milestoneActionIds)
                    {
                        if (!input.LinkedActionIds.Contains(milestoneActionId))
                        {
                            input.LinkedActionIds.Add(milestoneActionId);
                        }
                    }
                }
            }
            else
            {
                ViewData.Remove("DecisionSourceMilestone");
            }

            var viewModel = await BuildDecisionFormViewModelAsync(project, input, showDeleteButton: false);
            return View("CreateDecision", viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> DecisionDetails(int projectId, int decisionId)
        {
            var project = await GetProjectForDecisionAsync(projectId);
            if (project == null)
            {
                return NotFound();
            }

            var decision = await _context.Decisions
                .AsNoTracking()
                .Include(d => d.RiskDecisions).ThenInclude(rd => rd.Risk)
                .Include(d => d.IssueDecisions).ThenInclude(idl => idl.Issue)
                .Include(d => d.Actions.Where(a => !a.IsDeleted))
                .Include(d => d.OwnerUser)
                .FirstOrDefaultAsync(d => d.Id == decisionId && d.ProjectId == projectId && !d.IsDeleted);

            if (decision == null)
            {
                return NotFound();
            }

            var viewModel = new ProjectDecisionDetailsViewModel
            {
                ProjectId = projectId,
                ProjectTitle = project.Title,
                Decision = decision,
                LinkedRisks = decision.RiskDecisions
                    .Where(rd => rd.Risk != null && !rd.Risk.IsDeleted)
                    .Select(rd => rd.Risk!)
                    .OrderBy(r => r.Title)
                    .ToList(),
                LinkedIssues = decision.IssueDecisions
                    .Where(idl => idl.Issue != null && !idl.Issue.IsDeleted)
                    .Select(idl => idl.Issue!)
                    .OrderBy(i => i.Title)
                    .ToList(),
                LinkedActions = decision.Actions
                    .Where(a => !a.IsDeleted)
                    .OrderBy(a => a.Title)
                    .ToList()
            };

            return View("DecisionDetails", viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> EditDecision(int projectId, int decisionId)
        {
            var project = await GetProjectForDecisionAsync(projectId);
            if (project == null)
            {
                return NotFound();
            }

            var decision = await _context.Decisions
                .Include(d => d.RiskDecisions)
                .Include(d => d.IssueDecisions)
                .Include(d => d.Actions)
                .Include(d => d.OwnerUser)
                .FirstOrDefaultAsync(d => d.Id == decisionId && d.ProjectId == projectId && !d.IsDeleted);

            if (decision == null)
            {
                return NotFound();
            }

            var input = new ProjectDecisionInputModel
            {
                ProjectId = projectId,
                DecisionId = decision.Id,
                Title = decision.Title,
                Status = decision.Status,
                Summary = decision.Summary,
                DecisionType = decision.DecisionType,
                DecisionDate = decision.DecisionDate,
                BusinessArea = decision.BusinessArea,
                Outcome = decision.Outcome,
                Notes = decision.Notes,
                OwnerEmail = decision.OwnerUser?.Email,
                FipsId = decision.FipsId,
                SourceType = decision.SourceType,
                SourceReference = decision.SourceReference,
                SourceRecordUrl = decision.SourceRecordUrl,
                LinkedRiskIds = decision.RiskDecisions.Select(rd => rd.RiskId).ToList(),
                LinkedIssueIds = decision.IssueDecisions.Select(idl => idl.IssueId).ToList(),
                LinkedActionIds = decision.Actions.Where(a => !a.IsDeleted).Select(a => a.Id).ToList()
            };

            var viewModel = await BuildDecisionFormViewModelAsync(project, input, showDeleteButton: true);
            return View("EditDecision", viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDecision([Bind(Prefix = "Input")] ProjectDecisionInputModel input)
        {
            var project = await GetProjectForDecisionAsync(input.ProjectId);
            if (project == null)
            {
                return NotFound();
            }

            NormaliseDecisionInput(input);

            if (!IsValidDecisionStatus(input.Status))
            {
                ModelState.AddModelError(nameof(ProjectDecisionInputModel.Status), "Select a valid status.");
            }

            if (!ModelState.IsValid)
            {
                await LoadDecisionSourceMilestoneAsync(input.SourceMilestoneId);
                var invalidViewModel = await BuildDecisionFormViewModelAsync(project, input, showDeleteButton: false);
                return View("CreateDecision", invalidViewModel);
            }

            try
            {
                var ownerUser = await FindUserByEmailAsync(input.OwnerEmail);

                var decision = new Decision
                {
                    ProjectId = input.ProjectId,
                    Title = input.Title,
                    Status = input.Status ?? "pending",
                    Summary = input.Summary,
                    DecisionType = input.DecisionType,
                    DecisionDate = input.DecisionDate,
                    BusinessArea = input.BusinessArea,
                    Outcome = input.Outcome,
                    Notes = input.Notes,
                    OwnerUserId = ownerUser?.Id,
                    FipsId = input.FipsId,
                    SourceType = input.SourceType,
                    SourceReference = input.SourceReference,
                    SourceRecordUrl = input.SourceRecordUrl,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false
                };

                _context.Decisions.Add(decision);
                await _context.SaveChangesAsync();

                await ApplyDecisionRelationshipsAsync(decision, input.LinkedRiskIds, input.LinkedIssueIds, input.LinkedActionIds);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Decision added successfully.";
                if (input.SourceMilestoneId.HasValue)
                {
                    return RedirectToAction(nameof(MilestoneDetails), new { id = input.SourceMilestoneId.Value });
                }

                return RedirectToAction(nameof(Details), new { id = input.ProjectId, tab = "decisions" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding decision to project {ProjectId}", input.ProjectId);
                TempData["ErrorMessage"] = "Error adding decision. Please try again.";

                await LoadDecisionSourceMilestoneAsync(input.SourceMilestoneId);
                var viewModel = await BuildDecisionFormViewModelAsync(project, input, showDeleteButton: false);
                return View("CreateDecision", viewModel);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateDecision([Bind(Prefix = "Input")] ProjectDecisionInputModel input)
        {
            if (!input.DecisionId.HasValue)
            {
                return NotFound();
            }

            var decision = await _context.Decisions
                .FirstOrDefaultAsync(d => d.Id == input.DecisionId.Value && d.ProjectId == input.ProjectId && !d.IsDeleted);

            if (decision == null)
            {
                TempData["ErrorMessage"] = "Decision not found.";
                return RedirectToAction(nameof(Details), new { id = input.ProjectId, tab = "decisions" });
            }

            var project = await GetProjectForDecisionAsync(input.ProjectId);
            if (project == null)
            {
                return NotFound();
            }

            NormaliseDecisionInput(input);

            if (!IsValidDecisionStatus(input.Status))
            {
                ModelState.AddModelError(nameof(ProjectDecisionInputModel.Status), "Select a valid status.");
            }

            if (!ModelState.IsValid)
            {
                await LoadDecisionSourceMilestoneAsync(input.SourceMilestoneId);
                var invalidViewModel = await BuildDecisionFormViewModelAsync(project, input, showDeleteButton: true);
                return View("EditDecision", invalidViewModel);
            }

            try
            {
                var ownerUser = await FindUserByEmailAsync(input.OwnerEmail);

                decision.Title = input.Title;
                decision.Status = input.Status ?? decision.Status;
                decision.Summary = input.Summary;
                decision.DecisionType = input.DecisionType;
                decision.DecisionDate = input.DecisionDate;
                decision.BusinessArea = input.BusinessArea;
                decision.Outcome = input.Outcome;
                decision.Notes = input.Notes;
                decision.OwnerUserId = ownerUser?.Id;
                decision.FipsId = input.FipsId;
                decision.SourceType = input.SourceType;
                decision.SourceReference = input.SourceReference;
                decision.SourceRecordUrl = input.SourceRecordUrl;
                decision.UpdatedAt = DateTime.UtcNow;

                await ApplyDecisionRelationshipsAsync(decision, input.LinkedRiskIds, input.LinkedIssueIds, input.LinkedActionIds);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Decision updated successfully.";
                if (input.SourceMilestoneId.HasValue)
                {
                    return RedirectToAction(nameof(MilestoneDetails), new { id = input.SourceMilestoneId.Value });
                }

                return RedirectToAction(nameof(Details), new { id = input.ProjectId, tab = "decisions" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating decision {DecisionId} for project {ProjectId}", input.DecisionId, input.ProjectId);
                TempData["ErrorMessage"] = "Error updating decision. Please try again.";

                await LoadDecisionSourceMilestoneAsync(input.SourceMilestoneId);
                var viewModel = await BuildDecisionFormViewModelAsync(project, input, showDeleteButton: true);
                return View("EditDecision", viewModel);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDecision(int projectId, int decisionId)
        {
            try
            {
                var decision = await _context.Decisions
                    .Include(d => d.RiskDecisions)
                    .Include(d => d.IssueDecisions)
                    .FirstOrDefaultAsync(d => d.Id == decisionId && d.ProjectId == projectId && !d.IsDeleted);

                if (decision == null)
                {
                    TempData["ErrorMessage"] = "Decision not found.";
                    return RedirectToAction(nameof(Details), new { id = projectId, tab = "decisions" });
                }

                decision.IsDeleted = true;
                decision.UpdatedAt = DateTime.UtcNow;

                _context.RiskDecisions.RemoveRange(decision.RiskDecisions);
                _context.IssueDecisions.RemoveRange(decision.IssueDecisions);

                var linkedActions = await _context.Actions
                    .Where(a => a.DecisionId == decision.Id && a.ProjectId == projectId)
                    .ToListAsync();

                foreach (var action in linkedActions)
                {
                    action.DecisionId = null;
                    action.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Decision deleted successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting decision {DecisionId} for project {ProjectId}", decisionId, projectId);
                TempData["ErrorMessage"] = "Error deleting decision. Please try again.";
            }

            return RedirectToAction(nameof(Details), new { id = projectId, tab = "decisions" });
        }

        private static ProjectSummaryViewModel CreateProjectSummary(Project project)
        {
        return new ProjectSummaryViewModel
        {
            Id = project.Id,
            Title = project.Title,
            ProjectCode = project.ProjectCode,
            Status = project.Status ?? string.Empty,
            RagStatus = project.RagStatus,
            Phase = project.Phase,
            BusinessArea = project.BusinessArea,
            StartDate = project.StartDate,
            TargetDeliveryDate = project.TargetDeliveryDate,
            PrimaryContactName = project.PrimaryContactUser?.Name,
            PrimaryContactEmail = project.PrimaryContactUser?.Email
        };
        }

        private async Task<ProjectSummaryViewModel?> GetProjectSummaryAsync(int projectId)
        {
            return await _context.Projects
                .Where(p => p.Id == projectId && !p.IsDeleted)
                .Select(p => new ProjectSummaryViewModel
                {
                    Id = p.Id,
                    Title = p.Title,
                    ProjectCode = p.ProjectCode,
                    Status = p.Status ?? string.Empty,
                    RagStatus = p.RagStatus,
                    Phase = p.Phase,
                    BusinessArea = p.BusinessArea,
                    StartDate = p.StartDate,
                TargetDeliveryDate = p.TargetDeliveryDate,
                PrimaryContactName = p.PrimaryContactUser != null ? p.PrimaryContactUser.Name : null,
                PrimaryContactEmail = p.PrimaryContactUser != null ? p.PrimaryContactUser.Email : null
                })
                .FirstOrDefaultAsync();
        }

        private static IEnumerable<SelectListItem> BuildLegacyCategoryOptions(string? currentValue)
        {
            var categories = new[]
            {
                "Accessibility",
                "Compliance",
                "Engagement",
                "Financial",
                "Operational",
                "Outcome",
                "Performance",
                "Quality",
                "User satisfaction"
            };

            return BuildSelectList(categories, currentValue, "Select category");
        }

        private async Task<(string? Code, string? Name)> ResolveKpiCategoryAsync(ProjectKpiInputModel input)
        {
            string? normalisedCode = string.IsNullOrWhiteSpace(input.CategoryCode) ? null : SanitiseIdentifier(input.CategoryCode);

            IQueryable<KpiCategory> query = _context.KpiCategories.AsNoTracking();

            KpiCategory? match = null;

            if (!string.IsNullOrWhiteSpace(normalisedCode))
            {
                match = await query.FirstOrDefaultAsync(cv => cv.Code == normalisedCode);
            }

            if (match == null && !string.IsNullOrWhiteSpace(input.Category))
            {
                var trimmedName = input.Category.Trim();
                var lowered = trimmedName.ToLower();
                match = await query.FirstOrDefaultAsync(cv => cv.Name.ToLower() == lowered);
                if (match != null)
                {
                    normalisedCode = match.Code;
                }
            }

            if (match != null)
            {
                input.CategoryCode = match.Code;
                input.Category = match.Name;
                return (match.Code, match.Name);
            }

            if (!string.IsNullOrWhiteSpace(normalisedCode))
            {
                input.CategoryCode = normalisedCode;
                if (string.IsNullOrWhiteSpace(input.Category))
                {
                    input.Category = normalisedCode;
                }
                return (normalisedCode, input.Category);
            }

            return (null, null);
        }

        private static string GenerateKpiCode(string categoryCode, int entryId)
        {
            var segment = SanitiseIdentifier(categoryCode);
            return $"{segment}-{entryId}";
        }

        private static string SanitiseIdentifier(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "KPI";
            }

            var trimmed = value.Trim().ToUpperInvariant();
            var filtered = new string(trimmed.Where(char.IsLetterOrDigit).ToArray());
            return string.IsNullOrWhiteSpace(filtered) ? "KPI" : filtered;
        }

        private static string? ExtractCategoryCode(Kpi kpi)
        {
            if (string.IsNullOrWhiteSpace(kpi.Code))
            {
                return null;
            }

            var parts = kpi.Code.Split('-', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 0)
            {
                return SanitiseIdentifier(parts[0]);
            }

            return null;
        }

        private static List<string> SanitiseReportingStageSelections(IEnumerable<string?>? selections)
        {
            var result = new List<string>();
            if (selections == null)
            {
                return result;
            }

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var selection in selections)
            {
                var cleaned = SanitiseText(selection);
                if (string.IsNullOrWhiteSpace(cleaned))
                {
                    continue;
                }

                if (seen.Add(cleaned))
                {
                    result.Add(cleaned);
                }
            }

            return result;
        }

        private static string? SerialiseReportingStages(IEnumerable<string?>? selections)
        {
            var cleaned = SanitiseReportingStageSelections(selections);
            return cleaned.Count == 0 ? null : string.Join(", ", cleaned);
        }

        private static List<string> SplitReportingStages(string? stored)
        {
            if (string.IsNullOrWhiteSpace(stored))
            {
                return new List<string>();
            }

            var parts = stored.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(part => (string?)part)
                .ToList();

            return SanitiseReportingStageSelections(parts);
        }

        private static IEnumerable<SelectListItem> BuildKpiStatusOptions(string? currentValue)
        {
            var sanitisedValue = SanitiseText(currentValue);

            var options = KpiStatuses
                .Select(status => new SelectListItem(status.Label, status.Value, string.Equals(status.Value, sanitisedValue, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            if (!string.IsNullOrWhiteSpace(sanitisedValue) && options.All(option => !string.Equals(option.Value, sanitisedValue, StringComparison.OrdinalIgnoreCase)))
            {
                options.Insert(0, new SelectListItem(sanitisedValue, sanitisedValue, true));
            }

            return options;
        }

        private string? ResolveKpiStatus(ProjectKpiInputModel input)
        {
            var value = SanitiseText(input.Status);

            if (string.IsNullOrWhiteSpace(value))
            {
                input.Status = null;
                return null;
            }

            if (KpiStatuses.Any(status => string.Equals(status.Value, value, StringComparison.OrdinalIgnoreCase)))
            {
                input.Status = value;
                return value;
            }

            input.Status = value;
            ModelState.AddModelError(nameof(ProjectKpiInputModel.Status), "Select a status from the list.");
            return null;
        }
    }
}
