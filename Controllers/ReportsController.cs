using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Compass.Data;
using Compass.Models;
using Compass.Services;
using System.Security.Claims;

namespace Compass.Controllers;

[Authorize]
public class ReportsController : Controller
{
    private readonly CompassDbContext _context;
    private readonly ILogger<ReportsController> _logger;
    private readonly IPermissionService _permissionService;
    private readonly IProductsApiService _productsApiService;
    private readonly IServiceAssessmentApiService _serviceAssessmentApiService;

    public ReportsController(
        CompassDbContext context, 
        ILogger<ReportsController> logger, 
        IPermissionService permissionService,
        IProductsApiService productsApiService,
        IServiceAssessmentApiService serviceAssessmentApiService)
    {
        _context = context;
        _logger = logger;
        _permissionService = permissionService;
        _productsApiService = productsApiService;
        _serviceAssessmentApiService = serviceAssessmentApiService;
    }

    private string GetUserEmail()
    {
        return User.Identity?.Name 
            ?? User.FindFirst(ClaimTypes.Email)?.Value 
            ?? User.FindFirst("preferred_username")?.Value
            ?? User.FindFirst("email")?.Value
            ?? string.Empty;
    }

    private async Task<User?> GetCurrentUserAsync()
    {
        var userEmail = GetUserEmail();
        if (string.IsNullOrEmpty(userEmail))
        {
            return null;
        }

        return await _context.Users
            .Include(u => u.DivisionUsers)
                .ThenInclude(du => du.Division)
            .Include(u => u.BusinessAreaUsers)
                .ThenInclude(bau => bau.BusinessAreaLookup)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == userEmail.ToLower());
    }

    private async Task<bool> IsAdminUserAsync()
    {
        var userEmail = GetUserEmail();
        if (string.IsNullOrEmpty(userEmail))
        {
            return false;
        }

        try
        {
            return await _permissionService.IsSuperAdminAsync(userEmail) ||
                   await _permissionService.IsInGroupAsync(userEmail, "Central Operations Admin");
        }
        catch
        {
            // Non-blocking: default to false
            return false;
        }
    }

    /// <summary>
    /// Default dashboard that routes users to their appropriate reports based on their assignments
    /// </summary>
    public async Task<IActionResult> Index()
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null)
        {
            TempData["ErrorMessage"] = "Unable to identify the current user.";
            return RedirectToAction("Index", "Home");
        }

        var isAdmin = await IsAdminUserAsync();

        // Admin users can see all reports - show dashboard with all options
        if (isAdmin)
        {
            ViewData["Title"] = "Reports";
            ViewBag.IsAdmin = true;
            ViewBag.Message = "As an administrator, you can view all reports and see all projects.";
            return View();
        }

        var userDivisions = currentUser.DivisionUsers
            .Where(du => du.Division != null && du.Division.IsActive)
            .Select(du => du.Division!)
            .ToList();

        var userBusinessAreas = currentUser.BusinessAreaUsers
            .Where(bau => bau.BusinessAreaLookup != null && bau.BusinessAreaLookup.IsActive)
            .Select(bau => bau.BusinessAreaLookup!)
            .ToList();

        // If user has divisions, show division report
        if (userDivisions.Any())
        {
            return RedirectToAction(nameof(DivisionReport));
        }

        // If user has business areas, show business area report
        if (userBusinessAreas.Any())
        {
            return RedirectToAction(nameof(BusinessAreaReport));
        }

        // If user has neither, show a message
        ViewData["Title"] = "Reports";
        ViewBag.Message = "You are not assigned to any divisions or business areas. Please contact an administrator to be assigned.";
        return View();
    }

    /// <summary>
    /// Division report showing projects assigned to the user's divisions/directorates
    /// </summary>
    public async Task<IActionResult> DivisionReport()
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null)
        {
            TempData["ErrorMessage"] = "Unable to identify the current user.";
            return RedirectToAction("Index", "Home");
        }

        var isAdmin = await IsAdminUserAsync();
        List<Division> userDivisions;
        List<Project> projects;

        if (isAdmin)
        {
            // Admin users can see all divisions and all projects
            userDivisions = await _context.Divisions
                .Where(d => d.IsActive)
                .OrderBy(d => d.Name)
                .ToListAsync();

            projects = await _context.Projects
                .Where(p => !p.IsDeleted)
                .Include(p => p.Directorates)
                    .ThenInclude(d => d.Division)
                .Include(p => p.BusinessAreaLookup)
                .Include(p => p.RagStatusLookup)
                .Include(p => p.PhaseLookup)
                .Include(p => p.PrimaryContactUser)
                .Include(p => p.SeniorResponsibleOfficers)
                    .ThenInclude(sro => sro.User)
                .Include(p => p.ServiceOwners)
                    .ThenInclude(so => so.User)
                .OrderBy(p => p.Title)
                .ToListAsync();
        }
        else
        {
            // Regular users see only their assigned divisions
            userDivisions = currentUser.DivisionUsers
                .Where(du => du.Division != null && du.Division.IsActive)
                .Select(du => du.Division!)
                .ToList();

            if (!userDivisions.Any())
            {
                TempData["ErrorMessage"] = "You are not assigned to any divisions.";
                return RedirectToAction(nameof(Index));
            }

            var divisionIds = userDivisions.Select(d => d.Id).ToList();

            // Get projects assigned to these divisions via ProjectDirectorate
            projects = await _context.Projects
                .Where(p => !p.IsDeleted && p.Directorates.Any(d => divisionIds.Contains(d.DivisionId)))
                .Include(p => p.Directorates)
                    .ThenInclude(d => d.Division)
                .Include(p => p.BusinessAreaLookup)
                .Include(p => p.RagStatusLookup)
                .Include(p => p.PhaseLookup)
                .Include(p => p.PrimaryContactUser)
                .Include(p => p.SeniorResponsibleOfficers)
                    .ThenInclude(sro => sro.User)
                .Include(p => p.ServiceOwners)
                    .ThenInclude(so => so.User)
                .OrderBy(p => p.Title)
                .ToListAsync();
        }

        ViewData["Title"] = "Division report";
        ViewBag.UserDivisions = userDivisions;
        ViewBag.Projects = projects;
        ViewBag.ProjectCount = projects.Count;
        ViewBag.IsAdmin = isAdmin;

        return View();
    }

    /// <summary>
    /// Business area report showing projects in the user's business areas
    /// </summary>
    public async Task<IActionResult> BusinessAreaReport()
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null)
        {
            TempData["ErrorMessage"] = "Unable to identify the current user.";
            return RedirectToAction("Index", "Home");
        }

        var isAdmin = await IsAdminUserAsync();
        List<BusinessAreaLookup> userBusinessAreas;
        List<Project> projects;

        if (isAdmin)
        {
            // Admin users can see all business areas and all projects
            userBusinessAreas = await _context.BusinessAreaLookups
                .Where(ba => ba.IsActive)
                .OrderBy(ba => ba.Name)
                .ToListAsync();

            projects = await _context.Projects
                .Where(p => !p.IsDeleted)
                .Include(p => p.BusinessAreaLookup)
                .Include(p => p.Directorates)
                    .ThenInclude(d => d.Division)
                .Include(p => p.RagStatusLookup)
                .Include(p => p.PhaseLookup)
                .Include(p => p.PrimaryContactUser)
                .Include(p => p.SeniorResponsibleOfficers)
                    .ThenInclude(sro => sro.User)
                .Include(p => p.ServiceOwners)
                    .ThenInclude(so => so.User)
                .OrderBy(p => p.Title)
                .ToListAsync();
        }
        else
        {
            // Regular users see only their assigned business areas
            userBusinessAreas = currentUser.BusinessAreaUsers
                .Where(bau => bau.BusinessAreaLookup != null && bau.BusinessAreaLookup.IsActive)
                .Select(bau => bau.BusinessAreaLookup!)
                .ToList();

            if (!userBusinessAreas.Any())
            {
                TempData["ErrorMessage"] = "You are not assigned to any business areas.";
                return RedirectToAction(nameof(Index));
            }

            var businessAreaIds = userBusinessAreas.Select(ba => ba.Id).ToList();

            // Get projects assigned to these business areas
            projects = await _context.Projects
                .Where(p => !p.IsDeleted && p.BusinessAreaId.HasValue && businessAreaIds.Contains(p.BusinessAreaId.Value))
                .Include(p => p.BusinessAreaLookup)
                .Include(p => p.Directorates)
                    .ThenInclude(d => d.Division)
                .Include(p => p.RagStatusLookup)
                .Include(p => p.PhaseLookup)
                .Include(p => p.PrimaryContactUser)
                .Include(p => p.SeniorResponsibleOfficers)
                    .ThenInclude(sro => sro.User)
                .Include(p => p.ServiceOwners)
                    .ThenInclude(so => so.User)
                .OrderBy(p => p.Title)
                .ToListAsync();
        }

        ViewData["Title"] = "Business area report";
        ViewBag.UserBusinessAreas = userBusinessAreas;
        ViewBag.Projects = projects;
        ViewBag.ProjectCount = projects.Count;
        ViewBag.IsAdmin = isAdmin;

        return View();
    }

    /// <summary>
    /// Service Owner and SRO report showing all products and projects the user is responsible for
    /// </summary>
    [HttpGet]
    [Route("api/Reports")]
    [Route("Reports/ServiceOwnerSroReport")]
    public async Task<IActionResult> ServiceOwnerSroReport(int? userId = null)
    {
        try
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null)
            {
                TempData["ErrorMessage"] = "Unable to identify the current user.";
                return RedirectToAction("Index", "Home");
            }

            var isAdmin = await IsAdminUserAsync();
            
            // Determine which user to show report for
            User? selectedUser = currentUser;
            if (userId.HasValue && isAdmin)
            {
                selectedUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == userId.Value);
                if (selectedUser == null)
                {
                    selectedUser = currentUser;
                }
            }

            var userEmail = selectedUser.Email.ToLower();
            var viewModel = new ServiceOwnerSroReportViewModel
            {
                CurrentUser = currentUser,
                SelectedUser = selectedUser,
                IsAdmin = isAdmin
            };

            // Get all products from CMS
            var allProducts = await _productsApiService.GetProductsAsync();
            
            // Filter products where user is service owner or SRO
            var userProducts = allProducts.Where(p =>
                (p.ServiceOwners?.Any(so => so.EmailAddress?.Equals(userEmail, StringComparison.OrdinalIgnoreCase) == true) == true) ||
                (p.SeniorResponsibleOfficers?.Any(sro => sro.EmailAddress?.Equals(userEmail, StringComparison.OrdinalIgnoreCase) == true) == true)
            ).ToList();

            // Build product summaries
            foreach (var product in userProducts)
            {
                var roles = new List<string>();
                if (product.ServiceOwners?.Any(so => so.EmailAddress?.Equals(userEmail, StringComparison.OrdinalIgnoreCase) == true) == true)
                {
                    roles.Add("Service Owner");
                    viewModel.ProductsAsServiceOwner++;
                }
                if (product.SeniorResponsibleOfficers?.Any(sro => sro.EmailAddress?.Equals(userEmail, StringComparison.OrdinalIgnoreCase) == true) == true)
                {
                    roles.Add("SRO");
                    viewModel.ProductsAsSro++;
                }

                // Check for performance reporting (this would need to be implemented based on your reporting service)
                // For now, we'll set defaults
                var productSummary = new ProductSummary
                {
                    DocumentId = product.DocumentId ?? string.Empty,
                    FipsId = product.FipsId ?? string.Empty,
                    Title = product.Title,
                    Phase = product.Phase,
                    State = product.State,
                    ProductUrl = product.ProductUrl,
                    Roles = roles,
                    HasPerformanceReporting = false, // TODO: Check if product has performance reporting
                    LastPerformanceSubmission = null, // TODO: Get from reporting service
                    IsPerformanceReportingOverdue = false // TODO: Check if overdue
                };

                viewModel.Products.Add(productSummary);
            }

            viewModel.TotalProducts = viewModel.Products.Count;

            // Get projects where user is service owner or SRO
            var userProjects = await _context.Projects
                .Where(p => !p.IsDeleted &&
                    (p.ServiceOwners.Any(so => so.UserId == selectedUser.Id) ||
                     p.SeniorResponsibleOfficers.Any(sro => sro.UserId == selectedUser.Id)))
                .Include(p => p.ServiceOwners)
                    .ThenInclude(so => so.User)
                .Include(p => p.SeniorResponsibleOfficers)
                    .ThenInclude(sro => sro.User)
                .Include(p => p.RagStatusLookup)
                .Include(p => p.PhaseLookup)
                .Include(p => p.Milestones.Where(m => !m.IsDeleted))
                .Include(p => p.Issues.Where(i => !i.IsDeleted))
                .Include(p => p.Risks.Where(r => !r.IsDeleted))
                .Include(p => p.Actions.Where(a => !a.IsDeleted))
                .OrderBy(p => p.Title)
                .ToListAsync();

            // Build project summaries
            foreach (var project in userProjects)
            {
                var roles = new List<string>();
                if (project.ServiceOwners.Any(so => so.UserId == selectedUser.Id))
                {
                    roles.Add("Service Owner");
                    viewModel.ProjectsAsServiceOwner++;
                }
                if (project.SeniorResponsibleOfficers.Any(sro => sro.UserId == selectedUser.Id))
                {
                    roles.Add("SRO");
                    viewModel.ProjectsAsSro++;
                }

                var milestones = project.Milestones.Where(m => !m.IsDeleted).ToList();
                var overdueMilestones = milestones.Where(m => 
                    m.DueDate < DateTime.Today && 
                    m.Status != "complete" && 
                    m.Status != "cancelled").ToList();

                var issues = project.Issues.Where(i => !i.IsDeleted).ToList();
                var highPriorityIssues = issues.Where(i => 
                    i.Severity == "high" || i.Severity == "critical").ToList();
                var openIssues = issues.Where(i => 
                    i.Status != "resolved" && i.Status != "closed").ToList();

                var risks = project.Risks.Where(r => !r.IsDeleted).ToList();
                var highRisks = risks.Where(r => 
                    (r.LikelihoodRating >= 4 && r.ImpactRating >= 4) || 
                    r.RiskScore >= 16).ToList();

                var actions = project.Actions.Where(a => !a.IsDeleted).ToList();
                var openActions = actions.Where(a => 
                    a.Status != "completed" && a.Status != "closed").ToList();

                var projectSummary = new ProjectSummary
                {
                    Id = project.Id,
                    ProjectCode = project.ProjectCode,
                    Title = project.Title,
                    RagStatus = project.RagStatusLookup?.Name ?? project.RagStatus,
                    Phase = project.PhaseLookup?.Name,
                    Status = project.Status,
                    Roles = roles,
                    MilestoneCount = milestones.Count,
                    OverdueMilestoneCount = overdueMilestones.Count,
                    IssueCount = issues.Count,
                    HighPriorityIssueCount = highPriorityIssues.Count,
                    RiskCount = risks.Count,
                    HighRiskCount = highRisks.Count,
                    ActionCount = actions.Count,
                    OpenActionCount = openActions.Count
                };

                viewModel.Projects.Add(projectSummary);
            }

            viewModel.TotalProjects = viewModel.Projects.Count;

            // Calculate performance reporting summary
            viewModel.PerformanceReporting.TotalProductsRequiringReporting = viewModel.Products.Count;
            viewModel.PerformanceReporting.ProductsWithSubmissions = viewModel.Products.Count(p => p.HasPerformanceReporting);
            viewModel.PerformanceReporting.ProductsOverdue = viewModel.Products.Count(p => p.IsPerformanceReportingOverdue);
            viewModel.PerformanceReporting.ProductsUpToDate = viewModel.Products.Count(p => 
                p.HasPerformanceReporting && !p.IsPerformanceReportingOverdue);
            viewModel.PerformanceReporting.CompletionPercentage = viewModel.PerformanceReporting.TotalProductsRequiringReporting > 0
                ? (double)viewModel.PerformanceReporting.ProductsWithSubmissions / viewModel.PerformanceReporting.TotalProductsRequiringReporting * 100
                : 0;
            viewModel.PerformanceReporting.LastSubmissionDate = viewModel.Products
                .Where(p => p.LastPerformanceSubmission.HasValue)
                .Select(p => p.LastPerformanceSubmission!.Value)
                .OrderByDescending(d => d)
                .Cast<DateTime?>()
                .FirstOrDefault();

            // Calculate project reporting summary
            viewModel.ProjectReporting.TotalProjects = viewModel.Projects.Count;
            viewModel.ProjectReporting.TotalMilestones = viewModel.Projects.Sum(p => p.MilestoneCount);
            viewModel.ProjectReporting.OverdueMilestones = viewModel.Projects.Sum(p => p.OverdueMilestoneCount);
            viewModel.ProjectReporting.TotalIssues = viewModel.Projects.Sum(p => p.IssueCount);
            viewModel.ProjectReporting.OpenIssues = viewModel.Projects.Sum(p => p.IssueCount); // Simplified
            viewModel.ProjectReporting.HighPriorityIssues = viewModel.Projects.Sum(p => p.HighPriorityIssueCount);
            viewModel.ProjectReporting.TotalRisks = viewModel.Projects.Sum(p => p.RiskCount);
            viewModel.ProjectReporting.HighRisks = viewModel.Projects.Sum(p => p.HighRiskCount);
            viewModel.ProjectReporting.TotalActions = viewModel.Projects.Sum(p => p.ActionCount);
            viewModel.ProjectReporting.OpenActions = viewModel.Projects.Sum(p => p.OpenActionCount);
            
            // Projects by RAG status
            viewModel.ProjectReporting.ProjectsByRagStatus = viewModel.Projects
                .GroupBy(p => p.RagStatus ?? "Unknown")
                .ToDictionary(g => g.Key, g => g.Count());

            // Get service assessment data
            try
            {
                var assessmentResponse = await _serviceAssessmentApiService.GetActionsByStandardAsync();
                if (assessmentResponse?.Assessments != null)
                {
                    // Filter assessments for products/projects the user is responsible for
                    var productFipsIds = viewModel.Products.Select(p => p.FipsId).Where(f => !string.IsNullOrEmpty(f)).ToList();
                    var projectCodes = viewModel.Projects.Select(p => p.ProjectCode).Where(c => !string.IsNullOrEmpty(c)).ToList();

                    var relevantAssessments = assessmentResponse.Assessments
                        .Where(a => 
                            (!string.IsNullOrEmpty(a.AssessmentName) && 
                             (productFipsIds.Any(fid => a.AssessmentName.Contains(fid, StringComparison.OrdinalIgnoreCase)) ||
                              projectCodes.Any(pc => a.AssessmentName.Contains(pc, StringComparison.OrdinalIgnoreCase)))))
                        .ToList();

                    viewModel.ServiceAssessment.TotalAssessments = relevantAssessments.Count;
                    viewModel.ServiceAssessment.AssessmentsPassed = relevantAssessments.Count(a => 
                        a.AssessmentOutcome?.Equals("pass", StringComparison.OrdinalIgnoreCase) == true ||
                        a.AssessmentOutcome?.Equals("approved", StringComparison.OrdinalIgnoreCase) == true);
                    viewModel.ServiceAssessment.AssessmentsFailed = relevantAssessments.Count(a => 
                        a.AssessmentOutcome?.Equals("fail", StringComparison.OrdinalIgnoreCase) == true ||
                        a.AssessmentOutcome?.Equals("rejected", StringComparison.OrdinalIgnoreCase) == true);
                    viewModel.ServiceAssessment.AssessmentsInProgress = relevantAssessments.Count(a => 
                        a.AssessmentStatus?.Equals("in progress", StringComparison.OrdinalIgnoreCase) == true ||
                        a.AssessmentStatus?.Equals("draft", StringComparison.OrdinalIgnoreCase) == true);

                    // Count actions
                    var allActions = relevantAssessments
                        .SelectMany(a => a.ActionsByStandard ?? new List<ActionsByStandard>())
                        .SelectMany(ab => ab.Actions ?? new List<ActionItem>())
                        .ToList();
                    
                    viewModel.ServiceAssessment.TotalActions = allActions.Count;
                    viewModel.ServiceAssessment.OpenActions = allActions.Count(a => 
                        a.Status?.Equals("open", StringComparison.OrdinalIgnoreCase) == true ||
                        a.Status?.Equals("in progress", StringComparison.OrdinalIgnoreCase) == true);
                    viewModel.ServiceAssessment.OverdueActions = allActions.Count(a => 
                        a.EstimatedResolutionDate.HasValue && 
                        a.EstimatedResolutionDate.Value < DateTime.Today &&
                        (a.Status?.Equals("open", StringComparison.OrdinalIgnoreCase) == true ||
                         a.Status?.Equals("in progress", StringComparison.OrdinalIgnoreCase) == true));

                    // Assessments by type and phase
                    viewModel.ServiceAssessment.AssessmentsByType = relevantAssessments
                        .GroupBy(a => a.AssessmentType ?? "Unknown")
                        .ToDictionary(g => g.Key, g => g.Count());
                    
                    viewModel.ServiceAssessment.AssessmentsByPhase = relevantAssessments
                        .GroupBy(a => a.AssessmentPhase ?? "Unknown")
                        .ToDictionary(g => g.Key, g => g.Count());
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch service assessment data");
            }

            // Get all users for switcher (if admin)
            if (isAdmin)
            {
                viewModel.AllUsers = await _context.Users
                    .OrderBy(u => u.Name)
                    .ToListAsync();
            }

            ViewData["Title"] = $"Service Owner & SRO Report - {selectedUser.Name}";
            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading Service Owner/SRO report");
            TempData["ErrorMessage"] = "An error occurred while loading the report.";
            return RedirectToAction(nameof(Index));
        }
    }
}
