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
        public async Task<IActionResult> Index()
        {
            var projects = await _context.Projects
                .Where(p => !p.IsDeleted)
                .Include(p => p.ProjectMissions)
                    .ThenInclude(pm => pm.Mission)
                .Include(p => p.ProjectObjectives)
                    .ThenInclude(po => po.Objective)
                .Include(p => p.FundingAllocations)
                    .ThenInclude(fa => fa.FundingSource)
                .Include(p => p.Outcomes)
                .Include(p => p.ProjectContacts)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(projects);
        }

        // GET: Project/Details/5
        public async Task<IActionResult> Details(int? id)
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
                .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);

            if (project == null)
            {
                return NotFound();
            }

            return View(project);
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

            ViewBag.BusinessAreas = await _productsApiService.GetBusinessAreasAsync();

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

            return View(new Project());
        }

        // POST: Project/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Aim,StartDate,TargetDeliveryDate,IsFlagship,IsAiInitiative,RagStatus,RagJustification,Phase,BusinessArea,TotalPermFte,TotalMspFte,Status")] Project project)
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
                    
                    project.ProjectCode = $"PRJ-{nextNumber:D4}";
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
                    var roles = Request.Form["Contacts[].Role"].Where(x => !string.IsNullOrEmpty(x)).ToList();
                    var names = Request.Form["Contacts[].Name"].ToList();
                    var emails = Request.Form["Contacts[].Email"].ToList();
                    var roleDescriptions = Request.Form["Contacts[].RoleDescription"].ToList();

                    for (int i = 0; i < roles.Count; i++)
                    {
                        project.ProjectContacts.Add(new ProjectContact
                        {
                            Role = roles[i],
                            Name = i < names.Count ? names[i] : "",
                            Email = i < emails.Count ? emails[i] : "",
                            RoleDescription = i < roleDescriptions.Count ? roleDescriptions[i] : "",
                            SortOrder = i + 1
                        });
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

            ViewBag.BusinessAreas = await _productsApiService.GetBusinessAreasAsync();

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

        private bool ProjectExists(int id)
        {
            return _context.Projects.Any(e => e.Id == id && !e.IsDeleted);
        }
    }
}
