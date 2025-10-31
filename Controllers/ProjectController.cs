using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Compass.Data;
using Compass.Models;
using Compass.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Compass.Controllers
{
    [Authorize]
    public class ProjectController : Controller
    {
        private readonly CompassDbContext _context;
        private readonly ILogger<ProjectController> _logger;
        private readonly IProductsApiService _productsApiService;

        public ProjectController(CompassDbContext context, ILogger<ProjectController> logger, IProductsApiService productsApiService)
        {
            _context = context;
            _logger = logger;
            _productsApiService = productsApiService;
        }

        // GET: Project
        public async Task<IActionResult> Index(string search, string ragStatus, string businessArea, string phase, string flagship)
        {
            // Get current user's email
            var userEmail = User.Identity?.Name;
            
            // Get user's projects (where they are a named contact)
            var userProjects = new List<Project>();
            if (!string.IsNullOrEmpty(userEmail))
            {
                userProjects = await _context.Projects
                    .Where(p => !p.IsDeleted && p.ProjectContacts.Any(pc => pc.Email.ToLower() == userEmail.ToLower()))
                    .Include(p => p.ProjectMissions)
                        .ThenInclude(pm => pm.Mission)
                    .Include(p => p.ProjectObjectives)
                        .ThenInclude(po => po.Objective)
                    .Include(p => p.FundingAllocations)
                        .ThenInclude(fa => fa.FundingSource)
                    .Include(p => p.Outcomes)
                    .Include(p => p.ProjectContacts)
                    .OrderBy(p => p.Title)
                    .ToListAsync();
            }

            var query = _context.Projects
                .Where(p => !p.IsDeleted)
                .Include(p => p.ProjectMissions)
                    .ThenInclude(pm => pm.Mission)
                .Include(p => p.ProjectObjectives)
                    .ThenInclude(po => po.Objective)
                .Include(p => p.FundingAllocations)
                    .ThenInclude(fa => fa.FundingSource)
                .Include(p => p.Outcomes)
                .Include(p => p.ProjectContacts)
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

            var projects = await query
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            // Populate ViewBag for filter dropdowns and current filter values
            ViewBag.CurrentSearch = search;
            ViewBag.CurrentRagStatus = ragStatus;
            ViewBag.CurrentBusinessArea = businessArea;
            ViewBag.CurrentPhase = phase;
            ViewBag.CurrentFlagship = flagship;

            // Get business areas and phases from lookup tables
            ViewBag.BusinessAreas = await _context.BusinessAreaLookups
                .Where(ba => ba.IsActive)
                .OrderBy(ba => ba.SortOrder)
                .ThenBy(ba => ba.Name)
                .Select(ba => ba.Name)
                .ToListAsync();

            ViewBag.Phases = await _context.PhaseLookups
                .Where(p => p.IsActive)
                .OrderBy(p => p.SortOrder)
                .ThenBy(p => p.Name)
                .Select(p => p.Name)
                .ToListAsync();

            // Pass user projects to the view
            ViewBag.UserProjects = userProjects;

            return View(projects);
        }

        // GET: Project/Details/5
        public async Task<IActionResult> Details(int? id, string tab = "overview")
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
                .Include(p => p.FundingAllocations)
                    .ThenInclude(fa => fa.FundingSource)
                .Include(p => p.Outcomes)
                .Include(p => p.ProjectContacts)
                .Include(p => p.Successes)
                .Include(p => p.ProjectProducts)
                .Include(p => p.Milestones)
                .Include(p => p.Risks)
                .Include(p => p.Issues)
                .Include(p => p.Actions)
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

            // Get business areas and phases from lookup tables
            ViewBag.BusinessAreas = await _context.BusinessAreaLookups
                .Where(ba => ba.IsActive)
                .OrderBy(ba => ba.SortOrder)
                .ThenBy(ba => ba.Name)
                .Select(ba => ba.Name)
                .ToListAsync();

            ViewBag.Phases = await _context.PhaseLookups
                .Where(p => p.IsActive)
                .OrderBy(p => p.SortOrder)
                .ThenBy(p => p.Name)
                .Select(p => p.Name)
                .ToListAsync();

            // Get government departments for multi-department cooperation
            ViewBag.GovernmentDepartments = await _context.GovernmentDepartments
                .Where(d => !d.IsDeleted && d.ClosedAt == null)
                .OrderBy(d => d.Title)
                .ToListAsync();

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

            return View(project);
        }

        // POST: Project/AddDependency
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDependency(int projectId, int dependsOnProjectId, string? dependencyType, string? description)
        {
            try
            {
                var dependency = new Dependency
                {
                    SourceEntityType = "Project",
                    SourceEntityId = projectId,
                    TargetEntityType = "Project", 
                    TargetEntityId = dependsOnProjectId,
                    DependencyType = dependencyType ?? "General",
                    Description = description ?? "",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Dependencies.Add(dependency);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Dependency added successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding dependency");
                TempData["ErrorMessage"] = "An error occurred while adding the dependency.";
            }

            return RedirectToAction(nameof(Details), new { id = projectId });
        }

        // POST: Project/AddSuccess
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSuccess(int projectId, string successDescription, bool isReportedToSlt = false)
        {
            try
            {
                var success = new ProjectSuccess
                {
                    ProjectId = projectId,
                    SuccessDescription = successDescription,
                    RecordedByEmail = User.FindFirstValue(ClaimTypes.Email) ?? "unknown@example.com",
                    RecordedByName = User.FindFirstValue(ClaimTypes.Name) ?? "Unknown User",
                    RecordedAt = DateTime.UtcNow,
                    IsReportedToSlt = isReportedToSlt
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

            return RedirectToAction(nameof(Details), new { id = projectId, tab = "successes" });
        }

        // POST: Project/UpdateSuccess
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSuccess(int projectId, int successId, string successDescription, bool isReportedToSlt = false)
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

                success.SuccessDescription = successDescription;
                success.IsReportedToSlt = isReportedToSlt;
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Success updated successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating project success");
                TempData["ErrorMessage"] = "An error occurred while updating the success.";
            }

            return RedirectToAction(nameof(Details), new { id = projectId, tab = "successes" });
        }

        // POST: Project/DeleteSuccess
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSuccess(int successId, int projectId)
        {
            try
            {
                var success = await _context.ProjectSuccesses.FindAsync(successId);
                if (success != null)
                {
                    _context.ProjectSuccesses.Remove(success);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Success deleted successfully!";
                }
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

        // POST: Project/AddContact
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddContact(int projectId, string name, string? role, string email, string? description)
        {
            try
            {
                // Check if contact with this email already exists for this project
                var existingContact = await _context.ProjectContacts
                    .FirstOrDefaultAsync(pc => pc.ProjectId == projectId && pc.Email.ToLower() == email.ToLower());

                if (existingContact != null)
                {
                    TempData["ErrorMessage"] = $"A contact with email '{email}' already exists for this project.";
                    return RedirectToAction(nameof(Details), new { id = projectId, tab = "contacts" });
                }

                var contact = new ProjectContact
                {
                    ProjectId = projectId,
                    Name = name,
                    Role = string.IsNullOrWhiteSpace(role) ? "Not specified" : role,
                    Email = email,
                    RoleDescription = description,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.ProjectContacts.Add(contact);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Contact added successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding project contact");
                TempData["ErrorMessage"] = "An error occurred while adding the contact.";
            }

            return RedirectToAction(nameof(Details), new { id = projectId, tab = "contacts" });
        }

        // POST: Project/UpdateContact
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateContact(int projectId, int contactId, string name, string? role, string email, string? description)
        {
            try
            {
                var contact = await _context.ProjectContacts
                    .FirstOrDefaultAsync(c => c.Id == contactId && c.ProjectId == projectId);

                if (contact == null)
                {
                    TempData["ErrorMessage"] = "Contact not found.";
                    return RedirectToAction(nameof(Details), new { id = projectId, tab = "contacts" });
                }

                contact.Name = name;
                contact.Role = string.IsNullOrWhiteSpace(role) ? "Not specified" : role;
                contact.Email = email;
                contact.RoleDescription = description;
                contact.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Contact updated successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating project contact");
                TempData["ErrorMessage"] = "An error occurred while updating the contact.";
            }

            return RedirectToAction(nameof(Details), new { id = projectId, tab = "contacts" });
        }

        // POST: Project/DeleteContact
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteContact(int projectId, int contactId)
        {
            try
            {
                var contact = await _context.ProjectContacts
                    .FirstOrDefaultAsync(c => c.Id == contactId && c.ProjectId == projectId);

                if (contact == null)
                {
                    TempData["ErrorMessage"] = "Contact not found.";
                    return RedirectToAction(nameof(Details), new { id = projectId, tab = "contacts" });
                }

                _context.ProjectContacts.Remove(contact);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Contact deleted successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting project contact");
                TempData["ErrorMessage"] = "An error occurred while deleting the contact.";
            }

            return RedirectToAction(nameof(Details), new { id = projectId, tab = "contacts" });
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

            // Use Compass-specific lookups instead of CMS
            ViewBag.BusinessAreas = await _context.BusinessAreaLookups
                .Where(ba => ba.IsActive)
                .OrderBy(ba => ba.SortOrder)
                .ThenBy(ba => ba.Name)
                .Select(ba => ba.Name)
                .ToListAsync();

            ViewBag.Phases = await _context.PhaseLookups
                .Where(p => p.IsActive)
                .OrderBy(p => p.SortOrder)
                .ThenBy(p => p.Name)
                .Select(p => p.Name)
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

            // Get phases from lookup table for dropdown
            ViewBag.Phases = await _context.PhaseLookups
                .Where(p => p.IsActive)
                .OrderBy(p => p.SortOrder)
                .ThenBy(p => p.Name)
                .Select(p => p.Name)
                .ToListAsync();

            return View(project);
        }

        // POST: Project/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,BusinessArea,Aim,Phase,IsMultiDepartmentProject,OtherDepartments")] Project project)
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
            ViewBag.Phases = await _context.PhaseLookups
                .Where(p => p.IsActive)
                .OrderBy(p => p.SortOrder)
                .ThenBy(p => p.Name)
                .Select(p => p.Name)
                .ToListAsync();

            return View(project);
        }

        // POST: Project/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Aim,StartDate,TargetDeliveryDate,IsFlagship,IsAiInitiative,RagStatus,RagJustification,Phase,BusinessArea,TotalPermFte,TotalMspFte,Status,IsMultiDepartmentProject,OtherDepartments")] Project project)
        {
            _logger.LogInformation("Create POST called - Title: {Title}, Aim: {Aim}", project.Title, project.Aim);
            _logger.LogInformation("ModelState.IsValid: {IsValid}", ModelState.IsValid);
            
            // Log all form data
            _logger.LogInformation("Form data received:");
            foreach (var key in Request.Form.Keys)
            {
                _logger.LogInformation("  {Key}: {Value}", key, Request.Form[key]);
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
                        if (parts.Length == 2 && int.TryParse(parts[1], out int lastNumber))
                        {
                            nextNumber = lastNumber + 1;
                        }
                    }
                    
                    project.ProjectCode = $"DEL-{nextNumber:D4}";
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
                                SortOrder = i + 1
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

            // Use Compass-specific lookups instead of CMS
            ViewBag.BusinessAreas = await _context.BusinessAreaLookups
                .Where(ba => ba.IsActive)
                .OrderBy(ba => ba.SortOrder)
                .ThenBy(ba => ba.Name)
                .Select(ba => ba.Name)
                .ToListAsync();

            ViewBag.Phases = await _context.PhaseLookups
                .Where(p => p.IsActive)
                .OrderBy(p => p.SortOrder)
                .ThenBy(p => p.Name)
                .Select(p => p.Name)
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

            return View(project);
        }

        // POST: Project/AddOutcome
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddOutcome(int projectId, string outcome, string measureOfSuccess, string confidenceLevel, string? confidenceExplanation)
        {
            try
            {
                var project = await _context.Projects
                    .Include(p => p.Outcomes)
                    .FirstOrDefaultAsync(p => p.Id == projectId && !p.IsDeleted);

                if (project == null)
                {
                    return NotFound();
                }

                var maxSortOrder = project.Outcomes.Any() ? project.Outcomes.Max(o => o.SortOrder) : 0;

                var projectOutcome = new ProjectOutcome
                {
                    ProjectId = projectId,
                    Outcome = outcome,
                    MeasureOfSuccess = measureOfSuccess,
                    ConfidenceLevel = confidenceLevel,
                    ConfidenceExplanation = confidenceExplanation,
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
                _logger.LogError(ex, "Error adding outcome to project {ProjectId}", projectId);
                TempData["ErrorMessage"] = "Error adding outcome. Please try again.";
            }

            return RedirectToAction(nameof(Details), new { id = projectId, tab = "outcomes" });
        }

        // POST: Project/UpdateOutcome
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOutcome(int projectId, int outcomeId, string outcome, string measureOfSuccess, string confidenceLevel, string? confidenceExplanation, int sortOrder)
        {
            try
            {
                var projectOutcome = await _context.ProjectOutcomes
                    .FirstOrDefaultAsync(o => o.Id == outcomeId && o.ProjectId == projectId);

                if (projectOutcome == null)
                {
                    return NotFound();
                }

                projectOutcome.Outcome = outcome;
                projectOutcome.MeasureOfSuccess = measureOfSuccess;
                projectOutcome.ConfidenceLevel = confidenceLevel;
                projectOutcome.ConfidenceExplanation = confidenceExplanation;
                projectOutcome.SortOrder = sortOrder;
                projectOutcome.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Outcome updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating outcome {OutcomeId}", outcomeId);
                TempData["ErrorMessage"] = "Error updating outcome. Please try again.";
            }

            return RedirectToAction(nameof(Details), new { id = projectId, tab = "outcomes" });
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
                    return RedirectToAction(nameof(Details), new { id = projectId, tab = "funding" });
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

            return RedirectToAction(nameof(Details), new { id = projectId, tab = "funding" });
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
                .FirstOrDefaultAsync(m => m.Id == id);

            if (milestone == null)
            {
                return NotFound();
            }

            return View(milestone);
        }

        // POST: Project/AddMilestone
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMilestone(int projectId, string name, string? description, DateTime dueDate, string status)
        {
            try
            {
                var project = await _context.Projects
                    .FirstOrDefaultAsync(p => p.Id == projectId && !p.IsDeleted);

                if (project == null)
                {
                    return NotFound();
                }

                var milestone = new Milestone
                {
                    ProjectId = projectId,
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
        public async Task<IActionResult> UpdateMilestone(int projectId, int milestoneId, string name, string? description, DateTime dueDate, string status, DateTime? actualDate, int? progressPercent)
        {
            try
            {
                var milestone = await _context.Milestones
                    .FirstOrDefaultAsync(m => m.Id == milestoneId && m.ProjectId == projectId && !m.IsDeleted);

                if (milestone == null)
                {
                    return NotFound();
                }

                milestone.Name = name;
                milestone.Description = description;
                milestone.DueDate = dueDate;
                milestone.Status = status;
                milestone.ActualDate = actualDate;
                milestone.ProgressPercent = progressPercent;
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
        public async Task<IActionResult> AddIssue(int projectId, string title, string? description, string severity, string status, DateTime detectedDate, DateTime? targetResolutionDate, string? category, string? workaround)
        {
            try
            {
                // Check if an active issue with this title already exists for this project
                var existingIssue = await _context.Issues
                    .FirstOrDefaultAsync(i => i.ProjectId == projectId && 
                                             i.Title.ToLower() == title.ToLower() && 
                                             !i.IsDeleted);

                if (existingIssue != null)
                {
                    TempData["ErrorMessage"] = $"An issue with the title '{title}' already exists for this project.";
                    return RedirectToAction(nameof(Details), new { id = projectId, tab = "issues" });
                }

                var issue = new Issue
                {
                    ProjectId = projectId,
                    Title = title,
                    Description = description,
                    Severity = severity,
                    Status = status,
                    DetectedDate = detectedDate,
                    TargetResolutionDate = targetResolutionDate,
                    Category = category,
                    Workaround = workaround,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Issues.Add(issue);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Issue added successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding issue to project {ProjectId}", projectId);
                TempData["ErrorMessage"] = "Error adding issue. Please try again.";
            }

            return RedirectToAction(nameof(Details), new { id = projectId, tab = "issues" });
        }

        // POST: Project/UpdateIssue
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateIssue(int projectId, int issueId, string title, string? description, string severity, string status, DateTime detectedDate, DateTime? targetResolutionDate, string? category, string? workaround, string? resolutionSummary)
        {
            try
            {
                var issue = await _context.Issues
                    .FirstOrDefaultAsync(i => i.Id == issueId && i.ProjectId == projectId && !i.IsDeleted);

                if (issue == null)
                {
                    return NotFound();
                }

                issue.Title = title;
                issue.Description = description;
                issue.Severity = severity;
                issue.Status = status;
                issue.DetectedDate = detectedDate;
                issue.TargetResolutionDate = targetResolutionDate;
                issue.Category = category;
                issue.Workaround = workaround;
                issue.ResolutionSummary = resolutionSummary;
                issue.UpdatedAt = DateTime.UtcNow;

                if (status == "closed" || status == "resolved")
                {
                    issue.ClosedDate = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Issue updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating issue {IssueId}", issueId);
                TempData["ErrorMessage"] = "Error updating issue. Please try again.";
            }

            return RedirectToAction(nameof(Details), new { id = projectId, tab = "issues" });
        }

        // POST: Project/DeleteIssue
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteIssue(int projectId, int issueId)
        {
            try
            {
                var issue = await _context.Issues
                    .FirstOrDefaultAsync(i => i.Id == issueId && i.ProjectId == projectId);

                if (issue == null)
                {
                    TempData["ErrorMessage"] = "Issue not found.";
                    return RedirectToAction(nameof(Details), new { id = projectId, tab = "issues" });
                }

                issue.IsDeleted = true;
                issue.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Issue deleted successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting issue {IssueId}", issueId);
                TempData["ErrorMessage"] = "Error deleting issue. Please try again.";
            }

            return RedirectToAction(nameof(Details), new { id = projectId, tab = "issues" });
        }

        // POST: Project/AddDependency
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDependency(string sourceEntityType, int sourceEntityId, string targetEntityType, int targetEntityId, string? dependencyType, string? description)
        {
            try
            {
                // Check if this exact dependency relationship already exists
                var existingDependency = await _context.Dependencies
                    .FirstOrDefaultAsync(d => d.SourceEntityType == sourceEntityType && 
                                             d.SourceEntityId == sourceEntityId &&
                                             d.TargetEntityType == targetEntityType && 
                                             d.TargetEntityId == targetEntityId);

                if (existingDependency != null)
                {
                    TempData["ErrorMessage"] = "This dependency relationship already exists.";
                    return RedirectToAction(nameof(Details), new { id = sourceEntityId, tab = "dependencies" });
                }

                var dependency = new Dependency
                {
                    SourceEntityType = sourceEntityType,
                    SourceEntityId = sourceEntityId,
                    TargetEntityType = targetEntityType,
                    TargetEntityId = targetEntityId,
                    DependencyType = dependencyType ?? "Related",
                    Description = description,
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Dependencies.Add(dependency);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Dependency added successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding dependency");
                TempData["ErrorMessage"] = "Error adding dependency. Please try again.";
            }

            return RedirectToAction(nameof(Details), new { id = sourceEntityId, tab = "dependencies" });
        }

        // POST: Project/UpdateDependency
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateDependency(int dependencyId, string? dependencyType, string? status, string? description)
        {
            try
            {
                var dependency = await _context.Dependencies
                    .FirstOrDefaultAsync(d => d.Id == dependencyId);

                if (dependency == null)
                {
                    TempData["ErrorMessage"] = "Dependency not found.";
                    return RedirectToAction(nameof(Index));
                }

                dependency.DependencyType = dependencyType;
                dependency.Status = status ?? "Active";
                dependency.Description = description;
                dependency.UpdatedAt = DateTime.UtcNow;

                if (status == "Resolved")
                {
                    dependency.ResolvedDate = DateTime.UtcNow;
                    dependency.ResolvedByEmail = User.FindFirstValue(ClaimTypes.Email) ?? "unknown@example.com";
                    dependency.ResolvedByName = User.FindFirstValue(ClaimTypes.Name) ?? "Unknown User";
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Dependency updated successfully.";
                
                // Redirect back to source entity if it's a project
                if (dependency.SourceEntityType == "Project")
                {
                    return RedirectToAction(nameof(Details), new { id = dependency.SourceEntityId, tab = "dependencies" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating dependency {DependencyId}", dependencyId);
                TempData["ErrorMessage"] = "Error updating dependency. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Project/DeleteDependency
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDependency(int projectId, int dependencyId)
        {
            try
            {
                var dependency = await _context.Dependencies
                    .FirstOrDefaultAsync(d => d.Id == dependencyId);

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

        private bool ProjectExists(int id)
        {
            return _context.Projects.Any(e => e.Id == id && !e.IsDeleted);
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
    }
}
