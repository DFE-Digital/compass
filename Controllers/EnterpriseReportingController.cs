using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Compass.Data;
using Compass.Models;
using Compass.Services;
using System.Text;

namespace Compass.Controllers;

[Authorize]
public class EnterpriseReportingController : Controller
{
    private readonly CompassDbContext _context;
    private readonly ILogger<EnterpriseReportingController> _logger;
    private readonly IProductsApiService _productsApiService;
    private readonly IReturnStatusService _returnStatusService;

    public EnterpriseReportingController(
        CompassDbContext context, 
        ILogger<EnterpriseReportingController> logger,
        IProductsApiService productsApiService,
        IReturnStatusService returnStatusService)
    {
        _context = context;
        _logger = logger;
        _productsApiService = productsApiService;
        _returnStatusService = returnStatusService;
    }

    // GET: EnterpriseReporting/Objectives
    public async Task<IActionResult> Objectives(
        string[]? themes,
        string[]? riskLevels,
        string[]? issueLevels,
        string[]? milestoneStatuses,
        string sortColumn = "Title",
        string sortDirection = "asc")
    {
        try
        {
            var objectives = await _context.Objectives
                .Where(o => !o.IsDeleted)
                .ToListAsync();
            
            var objectiveViewModels = new List<dynamic>();
            
            foreach (var objective in objectives)
            {
                var risks = await _context.Risks
                    .Where(r => r.ObjectiveId == objective.Id && !r.IsDeleted)
                    .ToListAsync();
                
                var issues = await _context.Issues
                    .Where(i => i.ObjectiveId == objective.Id && !i.IsDeleted)
                    .ToListAsync();
                
                var actions = await _context.Actions
                    .Where(a => a.ObjectiveId == objective.Id && !a.IsDeleted)
                    .ToListAsync();
                
                var milestones = await _context.Milestones
                    .Where(m => m.ObjectiveId == objective.Id && !m.IsDeleted)
                    .ToListAsync();
                
                // Calculate statistics
                var highRisks = risks.Count(r => r.RiskScore >= 15);
                var mediumRisks = risks.Count(r => r.RiskScore >= 10 && r.RiskScore < 15);
                var lowRisks = risks.Count(r => r.RiskScore < 10);
                var openRisks = risks.Count(r => r.Status == "open" || r.Status == "treating");
                
                var criticalIssues = issues.Count(i => i.Severity == "critical");
                var openIssues = issues.Count(i => i.Status == "open" || i.Status == "in_progress");
                var blockedIssues = issues.Count(i => i.BlockedFlag);
                
                var overdueActions = actions.Count(a => 
                    a.DueDate.HasValue && 
                    a.DueDate < DateTime.Today && 
                    a.Status != "done" && 
                    a.Status != "cancelled");
                var doneActions = actions.Count(a => a.Status == "done");
                
                var delayedMilestones = milestones.Count(m => 
                    m.Status == "delayed" || 
                    (m.DueDate < DateTime.Today && m.Status != "complete" && m.Status != "cancelled"));
                var atRiskMilestones = milestones.Count(m => m.Status == "at_risk");
                var onTrackMilestones = milestones.Count(m => m.Status == "on_track");
                var completeMilestones = milestones.Count(m => m.Status == "complete");
                
                objectiveViewModels.Add(new
                {
                    Objective = objective,
                    TotalRisks = risks.Count,
                    HighRisks = highRisks,
                    MediumRisks = mediumRisks,
                    LowRisks = lowRisks,
                    OpenRisks = openRisks,
                    TotalIssues = issues.Count,
                    CriticalIssues = criticalIssues,
                    OpenIssues = openIssues,
                    BlockedIssues = blockedIssues,
                    TotalActions = actions.Count,
                    OverdueActions = overdueActions,
                    DoneActions = doneActions,
                    TotalMilestones = milestones.Count,
                    DelayedMilestones = delayedMilestones,
                    AtRiskMilestones = atRiskMilestones,
                    OnTrackMilestones = onTrackMilestones,
                    CompleteMilestones = completeMilestones
                });
            }
            
            // Apply filters
            if (themes != null && themes.Any())
            {
                objectiveViewModels = objectiveViewModels
                    .Where(o => themes.Contains(((Objective)o.Objective).Theme))
                    .ToList();
            }
            
            if (riskLevels != null && riskLevels.Any())
            {
                objectiveViewModels = objectiveViewModels
                    .Where(o => 
                        (riskLevels.Contains("high") && (int)o.HighRisks > 0) ||
                        (riskLevels.Contains("medium") && (int)o.MediumRisks > 0) ||
                        (riskLevels.Contains("low") && (int)o.LowRisks > 0) ||
                        (riskLevels.Contains("open") && (int)o.OpenRisks > 0))
                    .ToList();
            }
            
            if (issueLevels != null && issueLevels.Any())
            {
                objectiveViewModels = objectiveViewModels
                    .Where(o => 
                        (issueLevels.Contains("critical") && (int)o.CriticalIssues > 0) ||
                        (issueLevels.Contains("blocked") && (int)o.BlockedIssues > 0) ||
                        (issueLevels.Contains("open") && (int)o.OpenIssues > 0))
                    .ToList();
            }
            
            if (milestoneStatuses != null && milestoneStatuses.Any())
            {
                objectiveViewModels = objectiveViewModels
                    .Where(o => 
                        (milestoneStatuses.Contains("delayed") && (int)o.DelayedMilestones > 0) ||
                        (milestoneStatuses.Contains("at_risk") && (int)o.AtRiskMilestones > 0) ||
                        (milestoneStatuses.Contains("on_track") && (int)o.OnTrackMilestones > 0) ||
                        (milestoneStatuses.Contains("complete") && (int)o.CompleteMilestones > 0))
                    .ToList();
            }
            
            // Apply sorting
            objectiveViewModels = sortColumn switch
            {
                "Title" => sortDirection == "asc" 
                    ? objectiveViewModels.OrderBy(o => ((Objective)o.Objective).Title).ToList()
                    : objectiveViewModels.OrderByDescending(o => ((Objective)o.Objective).Title).ToList(),
                "Theme" => sortDirection == "asc"
                    ? objectiveViewModels.OrderBy(o => ((Objective)o.Objective).Theme ?? "").ToList()
                    : objectiveViewModels.OrderByDescending(o => ((Objective)o.Objective).Theme ?? "").ToList(),
                "TotalRisks" => sortDirection == "asc"
                    ? objectiveViewModels.OrderBy(o => (int)o.TotalRisks).ToList()
                    : objectiveViewModels.OrderByDescending(o => (int)o.TotalRisks).ToList(),
                "TotalIssues" => sortDirection == "asc"
                    ? objectiveViewModels.OrderBy(o => (int)o.TotalIssues).ToList()
                    : objectiveViewModels.OrderByDescending(o => (int)o.TotalIssues).ToList(),
                "TotalActions" => sortDirection == "asc"
                    ? objectiveViewModels.OrderBy(o => (int)o.TotalActions).ToList()
                    : objectiveViewModels.OrderByDescending(o => (int)o.TotalActions).ToList(),
                "TotalMilestones" => sortDirection == "asc"
                    ? objectiveViewModels.OrderBy(o => (int)o.TotalMilestones).ToList()
                    : objectiveViewModels.OrderByDescending(o => (int)o.TotalMilestones).ToList(),
                _ => objectiveViewModels.OrderBy(o => ((Objective)o.Objective).Title).ToList()
            };
            
            // Get all available themes (before filtering) for filter options
            var allAvailableThemes = objectives
                .Select(o => o.Theme)
                .Where(t => !string.IsNullOrEmpty(t))
                .Distinct()
                .OrderBy(t => t)
                .ToArray();
            
            // Pass filter and sort state to view
            ViewBag.AllThemes = allAvailableThemes;
            ViewBag.CurrentThemes = themes ?? Array.Empty<string>();
            ViewBag.CurrentRiskLevels = riskLevels ?? Array.Empty<string>();
            ViewBag.CurrentIssueLevels = issueLevels ?? Array.Empty<string>();
            ViewBag.CurrentMilestoneStatuses = milestoneStatuses ?? Array.Empty<string>();
            ViewBag.SortColumn = sortColumn;
            ViewBag.SortDirection = sortDirection;
            
            return View(objectiveViewModels);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading objectives summary");
            TempData["ErrorMessage"] = "An error occurred while loading objectives. Please try again.";
            return View(new List<dynamic>());
        }
    }

    // GET: EnterpriseReporting/ObjectiveDetails/5
    public async Task<IActionResult> ObjectiveDetails(int id, string view = "overview")
    {
        try
        {
            var objective = await _context.Objectives
                .Include(o => o.OwnerUser)
                .FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted);
            
            if (objective == null)
            {
                TempData["ErrorMessage"] = "Objective not found.";
                return RedirectToAction(nameof(Objectives));
            }
            
            // Get all products associated with this objective through RAID items
            var products = await _productsApiService.GetProductsAsync(null);
            
            var associatedFipsIds = new HashSet<string>();
            
            var risks = await _context.Risks
                .Include(r => r.RiskRiskTypes).ThenInclude(rrt => rrt.RiskType)
                .Include(r => r.RiskTier)
                .Where(r => r.ObjectiveId == objective.Id && !r.IsDeleted)
                .OrderByDescending(r => r.RiskScore)
                .ToListAsync();
            
            foreach (var r in risks.Where(r => !string.IsNullOrEmpty(r.FipsId)))
                associatedFipsIds.Add(r.FipsId!);
            
            var issues = await _context.Issues
                .Include(i => i.OwnerUser)
                .Where(i => i.ObjectiveId == objective.Id && !i.IsDeleted)
                .OrderByDescending(i => i.Severity == "critical" ? 4 : i.Severity == "high" ? 3 : i.Severity == "medium" ? 2 : 1)
                .ToListAsync();
            
            foreach (var i in issues.Where(i => !string.IsNullOrEmpty(i.FipsId)))
                associatedFipsIds.Add(i.FipsId!);
            
            // Get actions - note: related entities (AssignedToUser, ActionSource) will be loaded on-demand
            var actionsQuery = from act in _context.Set<Models.Action>()
                              where act.ObjectiveId == objective.Id && !act.IsDeleted
                              orderby act.Status == "blocked" ? 0 : act.Status == "in_progress" ? 1 : act.Status == "not_started" ? 2 : 3, act.DueDate
                              select act;
            var actions = await actionsQuery.ToListAsync();
            
            foreach (var actionItem in actions.Where(actionItem => !string.IsNullOrEmpty(actionItem.FipsId)))
                associatedFipsIds.Add(actionItem.FipsId!);
            
            var milestones = await _context.Milestones
                .Include(m => m.OwnerUser)
                .Where(m => m.ObjectiveId == objective.Id && !m.IsDeleted)
                .OrderBy(m => m.Status == "delayed" ? 0 : m.Status == "at_risk" ? 1 : m.Status == "on_track" ? 2 : 3)
                .ThenBy(m => m.DueDate)
                .ToListAsync();
            
            foreach (var m in milestones.Where(m => !string.IsNullOrEmpty(m.FipsId)))
                associatedFipsIds.Add(m.FipsId!);
            
            // Get projects associated with this objective
            var projects = await _context.ProjectObjectives
                .Include(po => po.Project)
                .Where(po => po.ObjectiveId == objective.Id)
                .Select(po => po.Project)
                .Where(p => !p.IsDeleted)
                .OrderBy(p => p.Title)
                .ToListAsync();
            
            var associatedProducts = products.Where(p => associatedFipsIds.Contains(p.FipsId)).ToList();
            
            // Get other objectives in the same theme
            var relatedObjectives = new List<Objective>();
            if (!string.IsNullOrEmpty(objective.Theme))
            {
                relatedObjectives = await _context.Objectives
                    .Where(o => !o.IsDeleted && o.Theme == objective.Theme && o.Id != objective.Id)
                    .OrderBy(o => o.Title)
                    .ToListAsync();
            }
            
            ViewBag.Objective = objective;
            ViewBag.Risks = risks;
            ViewBag.Issues = issues;
            ViewBag.Actions = actions;
            ViewBag.Milestones = milestones;
            ViewBag.Projects = projects;
            ViewBag.AssociatedProducts = associatedProducts;
            ViewBag.Products = products;
            ViewBag.RelatedObjectives = relatedObjectives;
            ViewBag.CurrentView = view;
            
            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading objective details for ID {ObjectiveId}", id);
            TempData["ErrorMessage"] = "An error occurred while loading objective details. Please try again.";
            return RedirectToAction(nameof(Objectives));
        }
    }

    // GET: EnterpriseReporting/FunctionalStandards
    public async Task<IActionResult> FunctionalStandards()
    {
        // Get all functional standards with their assessment counts and criteria counts
        var standards = await _context.FunctionalStandards
            .Include(fs => fs.Themes)
                .ThenInclude(t => t.PracticeAreas)
                    .ThenInclude(pa => pa.Criteria)
            .OrderBy(fs => fs.Id)
            .ToListAsync();
        
        // Get assessment counts for each standard
        var standardViewModels = new List<dynamic>();
        foreach (var standard in standards)
        {
            var inProgressAssessments = await _context.FunctionalStandardAssessments
                .CountAsync(fsa => fsa.FunctionalStandardId == standard.Id && !fsa.SubmittedAt.HasValue);
            
            var completedAssessments = await _context.FunctionalStandardAssessments
                .CountAsync(fsa => fsa.FunctionalStandardId == standard.Id && fsa.SubmittedAt.HasValue);
            
            // Calculate total criteria count
            var criteriaCount = standard.Themes?
                .Sum(t => t.PracticeAreas?.Sum(pa => pa.Criteria?.Count ?? 0) ?? 0) ?? 0;
            
            standardViewModels.Add(new 
            { 
                Standard = standard,
                CriteriaCount = criteriaCount,
                InProgressAssessments = inProgressAssessments,
                CompletedAssessments = completedAssessments
            });
        }
        
        ViewBag.Standards = standardViewModels;
        
        return View("~/Views/EnterpriseReporting/FunctionalStandards/Index.cshtml");
    }

    // GET: EnterpriseReporting/SelectStandard/1
    public async Task<IActionResult> SelectStandard(int standardId)
    {
        if (standardId <= 0)
        {
            return RedirectToAction(nameof(FunctionalStandards));
        }

        var standard = await _context.FunctionalStandards
            .Include(fs => fs.Themes)
                .ThenInclude(t => t.PracticeAreas)
                    .ThenInclude(pa => pa.Criteria)
            .FirstOrDefaultAsync(fs => fs.Id == standardId);

        if (standard == null)
        {
            TempData["ErrorMessage"] = "Functional standard not found";
            return RedirectToAction(nameof(FunctionalStandards));
        }

        // Get all assessments with their criteria responses to calculate completion
        var allAssessments = await _context.FunctionalStandardAssessments
            .Include(fsa => fsa.CriteriaResponses)
            .Where(fsa => fsa.FunctionalStandardId == standardId)
            .OrderByDescending(fsa => fsa.AssessmentDate)
            .ToListAsync();

        // Get all users to map AssessedBy email to Name
        var allUsers = await _context.Users.ToListAsync();
        var userMap = allUsers.ToDictionary(u => u.Email.ToLowerInvariant(), u => u.Name);

        // Calculate total criteria count for this standard
        var totalCriteria = standard.Themes?
            .Sum(t => t.PracticeAreas?.Sum(pa => pa.Criteria?.Count ?? 0) ?? 0) ?? 0;

        // Separate in-progress and submitted assessments
        var inProgressAssessments = new List<dynamic>();
        var submittedAssessments = new List<dynamic>();

        foreach (var assessment in allAssessments)
        {
            var completedCount = assessment.CriteriaResponses.Count(r => r.Attainment.HasValue);
            var isComplete = completedCount == totalCriteria && totalCriteria > 0;
            
            // Get user name from AssessedBy (which is email) or fall back to AssessedBy value
            var assessedByEmail = assessment.AssessedBy.ToLowerInvariant();
            var userName = userMap.ContainsKey(assessedByEmail) ? userMap[assessedByEmail] : assessment.AssessedBy;
            
            var assessmentInfo = new
            {
                Assessment = assessment,
                CompletedCount = completedCount,
                TotalCount = totalCriteria,
                IsComplete = isComplete,
                UserName = userName
            };

            if (assessment.SubmittedAt.HasValue)
            {
                submittedAssessments.Add(assessmentInfo);
            }
            else
            {
                inProgressAssessments.Add(assessmentInfo);
            }
        }

        ViewBag.Standard = standard;
        ViewBag.InProgressAssessments = inProgressAssessments;
        ViewBag.SubmittedAssessments = submittedAssessments;
        ViewBag.HasInProgressAssessment = inProgressAssessments.Any();

        return View("~/Views/EnterpriseReporting/FunctionalStandards/SelectStandard.cshtml");
    }

    // GET: EnterpriseReporting/FunctionalStandardsGuidance
    public IActionResult FunctionalStandardsGuidance()
    {
        return View("~/Views/EnterpriseReporting/FunctionalStandards/Guidance.cshtml");
    }

    // POST: EnterpriseReporting/StartAssessment
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StartAssessment(int standardId, string assessmentName)
    {
        if (standardId <= 0 || string.IsNullOrWhiteSpace(assessmentName))
        {
            TempData["ErrorMessage"] = "Please provide an assessment name";
            return RedirectToAction(nameof(SelectStandard), new { standardId });
        }

        var standard = await _context.FunctionalStandards
            .Include(fs => fs.Themes)
                .ThenInclude(t => t.PracticeAreas)
                    .ThenInclude(pa => pa.Criteria)
            .FirstOrDefaultAsync(fs => fs.Id == standardId);

        if (standard == null)
        {
            TempData["ErrorMessage"] = "Functional standard not found";
            return RedirectToAction(nameof(FunctionalStandards));
        }

        // Check if there's already an in-progress assessment
        var existingInProgress = await _context.FunctionalStandardAssessments
            .Where(fsa => fsa.FunctionalStandardId == standardId && !fsa.SubmittedAt.HasValue)
            .AnyAsync();

        if (existingInProgress)
        {
            TempData["ErrorMessage"] = "You cannot start a new assessment as one is already in progress";
            return RedirectToAction(nameof(SelectStandard), new { standardId });
        }

        try
        {
            // Get current user email
            var userEmail = User.Identity?.Name 
                ?? User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value 
                ?? User.FindFirst("preferred_username")?.Value
                ?? User.FindFirst("email")?.Value
                ?? string.Empty;

            // Get user name if available
            var userName = userEmail;
            if (!string.IsNullOrEmpty(userEmail))
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == userEmail.ToLower());
                if (user != null)
                {
                    userName = user.Name;
                }
            }

            // Create new assessment
            var assessment = new FunctionalStandardAssessment
            {
                FunctionalStandardId = standardId,
                AssessmentName = assessmentName,
                AssessedBy = userEmail, // Store email for lookup
                AssessmentDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.FunctionalStandardAssessments.Add(assessment);
            await _context.SaveChangesAsync();

            // Initialize all criteria responses
            foreach (var theme in standard.Themes ?? new List<FunctionalStandardTheme>())
            {
                foreach (var practiceArea in theme.PracticeAreas ?? new List<PracticeArea>())
                {
                    foreach (var criterion in practiceArea.Criteria ?? new List<Criterion>())
                    {
                        var response = new AssessmentCriteriaResponse
                        {
                            AssessmentId = assessment.Id,
                            FunctionalStandardId = standardId,
                            ThemeId = theme.ThemeId,
                            PracticeAreaId = practiceArea.PracticeAreaId,
                            CriteriaCode = criterion.CriteriaCode,
                            Attainment = null, // Not set initially
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };
                        _context.AssessmentCriteriaResponses.Add(response);
                    }
                }
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Assessment '{assessmentName}' has been created";
            return RedirectToAction(nameof(ConductAssessment), new { assessmentId = assessment.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating assessment");
            TempData["ErrorMessage"] = "An error occurred while creating the assessment";
            return RedirectToAction(nameof(SelectStandard), new { standardId });
        }
    }

    // GET: EnterpriseReporting/ConductAssessment/5
    public async Task<IActionResult> ConductAssessment(int assessmentId)
    {
        var assessment = await _context.FunctionalStandardAssessments
            .Include(fsa => fsa.FunctionalStandard)
                .ThenInclude(fs => fs.Themes)
                    .ThenInclude(t => t.PracticeAreas)
                        .ThenInclude(pa => pa.Criteria)
            .Include(fsa => fsa.CriteriaResponses)
            .FirstOrDefaultAsync(fsa => fsa.Id == assessmentId);

        if (assessment == null)
        {
            TempData["ErrorMessage"] = "Assessment not found";
            return RedirectToAction(nameof(FunctionalStandards));
        }

        return View("~/Views/EnterpriseReporting/FunctionalStandards/Conduct.cshtml", assessment);
    }

    // POST: EnterpriseReporting/SaveCriteriaResponse
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveCriteriaResponse(int responseId, int attainment, string? notes)
    {
        try
        {
            var response = await _context.AssessmentCriteriaResponses
                .FirstOrDefaultAsync(acr => acr.Id == responseId);

            if (response == null)
            {
                return Json(new { success = false, message = "Response not found" });
            }

            response.Attainment = (AttainmentLevel)attainment;
            response.Notes = notes;
            response.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving criteria response");
            return Json(new { success = false, message = "An error occurred while saving" });
        }
    }

    // GET: EnterpriseReporting/ViewAssessment/5
    public async Task<IActionResult> ViewAssessment(int assessmentId)
    {
        var assessment = await _context.FunctionalStandardAssessments
            .Include(fsa => fsa.FunctionalStandard)
                .ThenInclude(fs => fs.Themes)
                    .ThenInclude(t => t.PracticeAreas)
                        .ThenInclude(pa => pa.Criteria)
            .Include(fsa => fsa.CriteriaResponses)
            .FirstOrDefaultAsync(fsa => fsa.Id == assessmentId);

        if (assessment == null)
        {
            TempData["ErrorMessage"] = "Assessment not found";
            return RedirectToAction(nameof(FunctionalStandards));
        }

        ViewBag.IsReadOnly = true;

        return View("~/Views/EnterpriseReporting/FunctionalStandards/Conduct.cshtml", assessment);
    }

    // POST: EnterpriseReporting/DeleteAssessment
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAssessment(int assessmentId, int standardId)
    {
        var assessment = await _context.FunctionalStandardAssessments
            .Include(fsa => fsa.CriteriaResponses)
            .FirstOrDefaultAsync(fsa => fsa.Id == assessmentId);

        if (assessment == null)
        {
            TempData["ErrorMessage"] = "Assessment not found";
            return RedirectToAction(nameof(SelectStandard), new { standardId });
        }

        try
        {
            // Remove all criteria responses first
            _context.AssessmentCriteriaResponses.RemoveRange(assessment.CriteriaResponses);
            
            // Remove the assessment
            _context.FunctionalStandardAssessments.Remove(assessment);
            
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = $"Assessment '{assessment.AssessmentName}' has been deleted successfully";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting assessment {AssessmentId}", assessmentId);
            TempData["ErrorMessage"] = "An error occurred while deleting the assessment";
        }

        return RedirectToAction(nameof(SelectStandard), new { standardId });
    }

    // POST: EnterpriseReporting/SubmitAssessment
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitAssessment(int assessmentId)
    {
        var assessment = await _context.FunctionalStandardAssessments
            .Include(fsa => fsa.FunctionalStandard)
                .ThenInclude(fs => fs.Themes)
                    .ThenInclude(t => t.PracticeAreas)
                        .ThenInclude(pa => pa.Criteria)
            .Include(fsa => fsa.CriteriaResponses)
            .FirstOrDefaultAsync(fsa => fsa.Id == assessmentId);

        if (assessment == null)
        {
            TempData["ErrorMessage"] = "Assessment not found";
            return RedirectToAction(nameof(FunctionalStandards));
        }

        // Calculate total criteria
        var totalCriteria = assessment.FunctionalStandard?.Themes?
            .Sum(t => t.PracticeAreas?.Sum(pa => pa.Criteria?.Count ?? 0) ?? 0) ?? 0;
        
        var completedCount = assessment.CriteriaResponses.Count(r => r.Attainment.HasValue);

        if (completedCount < totalCriteria)
        {
            TempData["ErrorMessage"] = $"Cannot submit assessment. Only {completedCount} of {totalCriteria} criteria have been assessed.";
            return RedirectToAction(nameof(ConductAssessment), new { assessmentId });
        }

        assessment.SubmittedAt = DateTime.UtcNow;
        assessment.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Assessment submitted successfully";
        
        return RedirectToAction(nameof(ViewAssessmentSummary), new { assessmentId });
    }

    // GET: EnterpriseReporting/ViewAssessmentSummary/5
    public async Task<IActionResult> ViewAssessmentSummary(int assessmentId)
    {
        var assessment = await _context.FunctionalStandardAssessments
            .Include(fsa => fsa.FunctionalStandard)
                .ThenInclude(fs => fs.Themes)
                    .ThenInclude(t => t.PracticeAreas)
                        .ThenInclude(pa => pa.Criteria)
            .Include(fsa => fsa.CriteriaResponses)
            .FirstOrDefaultAsync(fsa => fsa.Id == assessmentId);

        if (assessment == null)
        {
            TempData["ErrorMessage"] = "Assessment not found";
            return RedirectToAction(nameof(FunctionalStandards));
        }

        return View("~/Views/EnterpriseReporting/FunctionalStandards/Summary.cshtml", assessment);
    }

    // GET: EnterpriseReporting/ExportAssessmentToExcel/5
    public async Task<IActionResult> ExportAssessmentToExcel(int assessmentId)
    {
        var assessment = await _context.FunctionalStandardAssessments
            .Include(fsa => fsa.FunctionalStandard)
                .ThenInclude(fs => fs.Themes)
                    .ThenInclude(t => t.PracticeAreas)
                        .ThenInclude(pa => pa.Criteria)
            .Include(fsa => fsa.CriteriaResponses)
            .FirstOrDefaultAsync(fsa => fsa.Id == assessmentId);

        if (assessment == null)
        {
            TempData["ErrorMessage"] = "Assessment not found";
            return RedirectToAction(nameof(FunctionalStandards));
        }

        // Create CSV content
        var csv = new StringBuilder();
        
        // Add header rows
        csv.AppendLine("Functional Standard Assessment Export");
        csv.AppendLine($"Assessment Name,{assessment.AssessmentName}");
        csv.AppendLine($"Standard,{assessment.FunctionalStandard?.Title ?? "N/A"}");
        csv.AppendLine($"Assessed By,{assessment.AssessedBy}");
        csv.AppendLine($"Assessment Date,{assessment.AssessmentDate:yyyy-MM-dd HH:mm}");
        if (assessment.SubmittedAt.HasValue)
        {
            csv.AppendLine($"Submitted Date,{assessment.SubmittedAt.Value:yyyy-MM-dd HH:mm}");
        }
        csv.AppendLine();
        
        // Add column headers
        csv.AppendLine("Theme,Theme Title,Practice Area,Practice Area Title,Rating,Criteria Code,Criteria,Attainment,Notes");
        
        // Add data rows
        if (assessment.FunctionalStandard?.Themes != null)
        {
            foreach (var theme in assessment.FunctionalStandard.Themes.OrderBy(t => t.ThemeId))
            {
                if (theme.PracticeAreas != null)
                {
                    foreach (var practiceArea in theme.PracticeAreas.OrderBy(pa => pa.PracticeAreaId))
                    {
                        if (practiceArea.Criteria != null)
                        {
                            foreach (var criterion in practiceArea.Criteria.OrderBy(c => c.CriteriaCode))
                            {
                                var response = assessment.CriteriaResponses
                                    .FirstOrDefault(r => r.ThemeId == theme.ThemeId && 
                                                        r.PracticeAreaId == practiceArea.PracticeAreaId &&
                                                        r.CriteriaCode == criterion.CriteriaCode);
                                
                                var attainment = response?.Attainment switch
                                {
                                    AttainmentLevel.NotOrSeldomMet => "Not, or seldom met",
                                    AttainmentLevel.PartiallyMet => "Partially met",
                                    AttainmentLevel.FullyMet => "Fully met",
                                    _ => ""
                                };
                                
                                var notes = response?.Notes ?? "";
                                
                                // Escape quotes and commas in CSV
                                var escapeCsv = new Func<string, string>(s => 
                                {
                                    if (string.IsNullOrEmpty(s)) return "";
                                    if (s.Contains(",") || s.Contains("\"") || s.Contains("\n"))
                                    {
                                        return "\"" + s.Replace("\"", "\"\"") + "\"";
                                    }
                                    return s;
                                });
                                
                                csv.AppendLine($"{theme.ThemeId},{escapeCsv(theme.Title)},{practiceArea.PracticeAreaId},{escapeCsv(practiceArea.Title)},{criterion.Rating},{criterion.CriteriaCode},{escapeCsv(criterion.Criteria)},{escapeCsv(attainment)},{escapeCsv(notes)}");
                            }
                        }
                    }
                }
            }
        }
        
        // Return as Excel-compatible CSV
        var fileName = $"Assessment_{assessment.AssessmentName.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
        var bytes = Encoding.UTF8.GetBytes(csv.ToString());
        
        // Add UTF-8 BOM for Excel compatibility
        var bom = Encoding.UTF8.GetPreamble();
        var fileBytes = new byte[bom.Length + bytes.Length];
        Buffer.BlockCopy(bom, 0, fileBytes, 0, bom.Length);
        Buffer.BlockCopy(bytes, 0, fileBytes, bom.Length, bytes.Length);
        
        return File(fileBytes, "text/csv", fileName);
    }

    #region Enterprise Metrics

    // GET: EnterpriseReporting/EnterpriseMetrics
    public async Task<IActionResult> EnterpriseMetrics()
    {
        // Get monthly returns starting from October 2025
        var returns = await GetOrCreateEnterpriseReturns(1); // 1 upcoming month

        return View("~/Views/EnterpriseReporting/EnterpriseMetrics/Index.cshtml", returns);
    }

    // GET: EnterpriseReporting/SubmitEnterpriseMetrics/2025/10
    public async Task<IActionResult> SubmitEnterpriseMetrics(int year, int month)
    {
        // Get or create the return for this period
        var enterpriseReturn = await _context.EnterpriseReturns
            .Include(er => er.MetricValues)
                .ThenInclude(emv => emv.EnterpriseMetric)
            .FirstOrDefaultAsync(er => er.Year == year && er.Month == month);

        var isReadOnly = false;

        if (enterpriseReturn == null)
        {
            // Create new return
            enterpriseReturn = new EnterpriseReturn
            {
                Year = year,
                Month = month,
                Status = _returnStatusService.CalculateReturnStatus(year, month, null),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.EnterpriseReturns.Add(enterpriseReturn);
            await _context.SaveChangesAsync();
        }
        else
        {
            // Update status
            enterpriseReturn.Status = _returnStatusService.CalculateReturnStatus(year, month, enterpriseReturn.SubmittedDate);
            if (enterpriseReturn.Status == ReturnStatus.Submitted)
            {
                isReadOnly = true;
            }
        }

        // Get all enterprise metrics valid for this reporting period
        var allMetrics = await _context.EnterpriseMetrics
            .Where(m => m.ValidFromYear < year || 
                       (m.ValidFromYear == year && m.ValidFromMonth <= month))
            .OrderBy(m => m.Identifier)
            .ToListAsync();

        // Get or create metric values
        var metricValues = new List<EnterpriseMetricValue>();
        foreach (var metric in allMetrics)
        {
            var existingValue = enterpriseReturn.MetricValues?
                .FirstOrDefault(mv => mv.EnterpriseMetricId == metric.Id);

            if (existingValue != null)
            {
                metricValues.Add(existingValue);
            }
            else
            {
                var newValue = new EnterpriseMetricValue
                {
                    EnterpriseReturnId = enterpriseReturn.Id,
                    EnterpriseMetricId = metric.Id,
                    EnterpriseMetric = metric,
                    IsComplete = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.EnterpriseMetricValues.Add(newValue);
                metricValues.Add(newValue);
            }
        }

        await _context.SaveChangesAsync();

        ViewBag.EnterpriseReturn = enterpriseReturn;
        ViewBag.IsReadOnly = isReadOnly;

        return View("~/Views/EnterpriseReporting/EnterpriseMetrics/Submit.cshtml", metricValues);
    }

    // POST: EnterpriseReporting/SaveEnterpriseMetricValue
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveEnterpriseMetricValue(int id, string? value)
    {
        try
        {
            var metricValue = await _context.EnterpriseMetricValues
                .Include(mv => mv.EnterpriseMetric)
                .FirstOrDefaultAsync(mv => mv.Id == id);
                
            if (metricValue == null)
            {
                return Json(new { success = false, message = "Metric value not found" });
            }

            metricValue.Value = value;
            
            // Mark as complete if value is provided OR if empty but allowNull is true (not captured)
            var isNotCaptured = string.IsNullOrWhiteSpace(value);
            if (isNotCaptured && metricValue.EnterpriseMetric != null)
            {
                try
                {
                    var rules = System.Text.Json.JsonSerializer.Deserialize<ValidationRules>(metricValue.EnterpriseMetric.ValidationRules);
                    metricValue.IsComplete = rules?.AllowNull == true;
                }
                catch
                {
                    metricValue.IsComplete = !string.IsNullOrWhiteSpace(value);
                }
            }
            else
            {
                metricValue.IsComplete = !string.IsNullOrWhiteSpace(value);
            }
            
            metricValue.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving enterprise metric value");
            return Json(new { success = false, message = "An error occurred while saving" });
        }
    }

    // POST: EnterpriseReporting/SubmitEnterpriseReturn
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitEnterpriseReturn(int returnId)
    {
        try
        {
            var enterpriseReturn = await _context.EnterpriseReturns
                .Include(er => er.MetricValues)
                .FirstOrDefaultAsync(er => er.Id == returnId);

            if (enterpriseReturn == null)
            {
                TempData["ErrorMessage"] = "Return not found";
                return RedirectToAction(nameof(EnterpriseMetrics));
            }

            // Check if all metrics are complete
            if (enterpriseReturn.MetricValues == null || !enterpriseReturn.MetricValues.All(mv => mv.IsComplete))
            {
                TempData["ErrorMessage"] = "Please complete all metrics before submitting";
                return RedirectToAction(nameof(SubmitEnterpriseMetrics), new { year = enterpriseReturn.Year, month = enterpriseReturn.Month });
            }

            enterpriseReturn.Status = ReturnStatus.Submitted;
            enterpriseReturn.SubmittedDate = DateTime.UtcNow;
            enterpriseReturn.SubmittedBy = User.Identity?.Name ?? "Unknown";
            enterpriseReturn.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Return for {new DateTime(enterpriseReturn.Year, enterpriseReturn.Month, 1).ToString("MMMM yyyy")} has been submitted successfully";
            return RedirectToAction(nameof(EnterpriseMetrics));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting enterprise return");
            TempData["ErrorMessage"] = "An error occurred while submitting the return";
            return RedirectToAction(nameof(EnterpriseMetrics));
        }
    }

    // POST: EnterpriseReporting/UnsubmitEnterpriseReturn
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnsubmitEnterpriseReturn(int returnId)
    {
        var enterpriseReturn = await _context.EnterpriseReturns
            .FirstOrDefaultAsync(er => er.Id == returnId);

        if (enterpriseReturn == null)
        {
            TempData["ErrorMessage"] = "Return not found";
            return RedirectToAction(nameof(EnterpriseMetrics));
        }

        if (enterpriseReturn.Status != ReturnStatus.Submitted)
        {
            TempData["ErrorMessage"] = "This return has not been submitted yet";
            return RedirectToAction(nameof(EnterpriseMetrics));
        }

        try
        {
            enterpriseReturn.Status = _returnStatusService.CalculateReturnStatus(enterpriseReturn.Year, enterpriseReturn.Month, null);
            enterpriseReturn.SubmittedDate = null;
            enterpriseReturn.SubmittedBy = null;
            enterpriseReturn.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Return for {new DateTime(enterpriseReturn.Year, enterpriseReturn.Month, 1).ToString("MMMM yyyy")} has been unsubmitted";
            return RedirectToAction(nameof(EnterpriseMetrics));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unsubmitting enterprise return");
            TempData["ErrorMessage"] = "An error occurred while unsubmitting the return";
            return RedirectToAction(nameof(EnterpriseMetrics));
        }
    }

    private async Task<List<EnterpriseReturn>> GetOrCreateEnterpriseReturns(int upcomingMonths)
    {
        var returns = new List<EnterpriseReturn>();
        var now = DateTime.UtcNow;

        // Start from October 2025
        var startDate = new DateTime(2025, 10, 1);
        
        // End date is current month + upcoming months
        var endDate = now.AddMonths(upcomingMonths);
        if (endDate < startDate)
        {
            endDate = startDate.AddMonths(upcomingMonths);
        }

        for (var date = startDate; 
             date <= endDate; 
             date = date.AddMonths(1))
        {
            var existingReturn = await _context.EnterpriseReturns
                .Include(er => er.MetricValues)
                    .ThenInclude(mv => mv.EnterpriseMetric)
                .FirstOrDefaultAsync(er => er.Year == date.Year && er.Month == date.Month);

            if (existingReturn != null)
            {
                existingReturn.Status = _returnStatusService.CalculateReturnStatus(existingReturn.Year, existingReturn.Month, existingReturn.SubmittedDate);
                returns.Add(existingReturn);
            }
            else
            {
                var newReturn = new EnterpriseReturn
                {
                    Year = date.Year,
                    Month = date.Month,
                    Status = _returnStatusService.CalculateReturnStatus(date.Year, date.Month, null)
                };
                returns.Add(newReturn);
            }
        }

        return returns.OrderByDescending(r => r.Year).ThenByDescending(r => r.Month).ToList();
    }

    #endregion
}

