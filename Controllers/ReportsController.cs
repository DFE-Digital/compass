using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
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
    private readonly IMemoryCache _cache;

    // Cache durations
    private static readonly TimeSpan ProjectsCacheDuration      = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan ExternalApiCacheDuration   = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan LookupCacheDuration        = TimeSpan.FromMinutes(15);

    private const string AllProjectsCacheKey         = "reports_all_projects";
    private const string ServiceAssessmentCacheKey   = "reports_service_assessments";
    private const string ProductsCacheKey            = "reports_products";
    private const string DivisionsCacheKey           = "reports_divisions";
    private const string BusinessAreasCacheKey       = "reports_business_areas";
    private const string DeliveryPrioritiesCacheKey  = "reports_delivery_priorities";

    public ReportsController(
        CompassDbContext context,
        ILogger<ReportsController> logger,
        IPermissionService permissionService,
        IProductsApiService productsApiService,
        IServiceAssessmentApiService serviceAssessmentApiService,
        IMemoryCache cache)
    {
        _context = context;
        _logger = logger;
        _permissionService = permissionService;
        _productsApiService = productsApiService;
        _serviceAssessmentApiService = serviceAssessmentApiService;
        _cache = cache;
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
        if (string.IsNullOrEmpty(userEmail)) return null;

        return await _context.Users
            .Include(u => u.DivisionUsers).ThenInclude(du => du.Division)
            .Include(u => u.BusinessAreaUsers).ThenInclude(bau => bau.BusinessAreaLookup)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == userEmail.ToLower());
    }

    private async Task<bool> IsAdminUserAsync()
    {
        var userEmail = GetUserEmail();
        if (string.IsNullOrEmpty(userEmail)) return false;
        try
        {
            return await _permissionService.IsSuperAdminAsync(userEmail) ||
                   await _permissionService.IsInGroupAsync(userEmail, "Central Operations Admin");
        }
        catch { return false; }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helper: load all projects with full includes
    // ─────────────────────────────────────────────────────────────────────────
    /// <summary>Returns the effective RAG name for a project, using the lookup first then falling back to the legacy string field.</summary>
    private static string GetProjectRagName(Project p)
    {
        var name = p.RagStatusLookup?.Name?.Trim();
        if (!string.IsNullOrEmpty(name)) return name;
        // Fall back to deprecated string field, normalising separators
        var legacy = p.RagStatus?.Trim()
            .Replace(" / ", "-").Replace("/", "-").Replace(" /", "-").Replace("/ ", "-");
        return string.IsNullOrEmpty(legacy) ? "Not set" : legacy;
    }

    private IQueryable<Project> AllProjectsQuery() =>
        _context.Projects
            .Where(p => !p.IsDeleted && (p.Status == "Active" || p.Status == "Paused"))
            .Include(p => p.Directorates).ThenInclude(d => d.Division)
            .Include(p => p.BusinessAreaLookup)
            .Include(p => p.RagStatusLookup)
            .Include(p => p.PhaseLookup)
            .Include(p => p.PrimaryContactUser)
            .Include(p => p.SeniorResponsibleOfficers).ThenInclude(sro => sro.User)
            .Include(p => p.ServiceOwners).ThenInclude(so => so.User)
            .Include(p => p.Outcomes)
            .Include(p => p.Risks.Where(r => !r.IsDeleted))
            .Include(p => p.Issues.Where(i => !i.IsDeleted))
            .Include(p => p.Milestones.Where(m => !m.IsDeleted))
            .Include(p => p.MonthlyUpdates)
            .Include(p => p.DeliveryPriority)
            .Include(p => p.ActivityTypeLookup);

    // ─────────────────────────────────────────────────────────────────────────
    // Cached data helpers – avoid repeated heavy queries across page loads
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Returns all non-deleted projects with full includes, cached for 5 minutes.</summary>
    private async Task<List<Project>> GetAllProjectsCachedAsync()
    {
        if (_cache.TryGetValue(AllProjectsCacheKey, out List<Project>? cached) && cached != null)
            return cached;

        var projects = await AllProjectsQuery().OrderBy(p => p.Title).ToListAsync();
        _cache.Set(AllProjectsCacheKey, projects, ProjectsCacheDuration);
        return projects;
    }

    /// <summary>Returns service assessment data, cached for 10 minutes.</summary>
    private async Task<(int total, int passed, int failed, int inProgress)> GetServiceAssessmentSummaryCachedAsync()
    {
        if (_cache.TryGetValue(ServiceAssessmentCacheKey, out (int, int, int, int) cachedResult))
            return cachedResult;

        int total = 0, passed = 0, failed = 0, inProgress = 0;
        try
        {
            var response = await _serviceAssessmentApiService.GetActionsByStandardAsync();
            if (response?.Assessments != null)
            {
                total      = response.Assessments.Count;
                passed     = response.Assessments.Count(a =>
                    a.AssessmentOutcome?.Equals("pass",     StringComparison.OrdinalIgnoreCase) == true ||
                    a.AssessmentOutcome?.Equals("approved", StringComparison.OrdinalIgnoreCase) == true);
                failed     = response.Assessments.Count(a =>
                    a.AssessmentOutcome?.Equals("fail",     StringComparison.OrdinalIgnoreCase) == true ||
                    a.AssessmentOutcome?.Equals("rejected", StringComparison.OrdinalIgnoreCase) == true);
                inProgress = response.Assessments.Count(a =>
                    a.AssessmentStatus?.Equals("in progress", StringComparison.OrdinalIgnoreCase) == true ||
                    a.AssessmentStatus?.Equals("draft",       StringComparison.OrdinalIgnoreCase) == true);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch service assessment data");
        }

        var result = (total, passed, failed, inProgress);
        _cache.Set(ServiceAssessmentCacheKey, result, ExternalApiCacheDuration);
        return result;
    }

    /// <summary>Returns CMS products list, cached for 10 minutes.</summary>
    private async Task<List<dynamic>> GetProductsCachedAsync()
    {
        if (_cache.TryGetValue(ProductsCacheKey, out List<dynamic>? cachedProducts) && cachedProducts != null)
            return cachedProducts;

        var products = new List<dynamic>();
        try
        {
            products = (await _productsApiService.GetProductsAsync()).Cast<dynamic>().ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch products from CMS");
        }

        _cache.Set(ProductsCacheKey, products, ExternalApiCacheDuration);
        return products;
    }

    /// <summary>Returns active divisions, cached for 15 minutes.</summary>
    private async Task<List<Division>> GetDivisionsCachedAsync()
    {
        if (_cache.TryGetValue(DivisionsCacheKey, out List<Division>? cached) && cached != null)
            return cached;

        var divisions = await _context.Divisions.Where(d => d.IsActive).OrderBy(d => d.Name).ToListAsync();
        _cache.Set(DivisionsCacheKey, divisions, LookupCacheDuration);
        return divisions;
    }

    /// <summary>Returns active business areas, cached for 15 minutes.</summary>
    private async Task<List<BusinessAreaLookup>> GetBusinessAreasCachedAsync()
    {
        if (_cache.TryGetValue(BusinessAreasCacheKey, out List<BusinessAreaLookup>? cached) && cached != null)
            return cached;

        var bas = await _context.BusinessAreaLookups.Where(ba => ba.IsActive).OrderBy(ba => ba.Name).ToListAsync();
        _cache.Set(BusinessAreasCacheKey, bas, LookupCacheDuration);
        return bas;
    }

    /// <summary>Returns delivery priorities, cached for 15 minutes.</summary>
    private async Task<List<DeliveryPriority>> GetDeliveryPrioritiesCachedAsync()
    {
        if (_cache.TryGetValue(DeliveryPrioritiesCacheKey, out List<DeliveryPriority>? cached) && cached != null)
            return cached;

        var priorities = await _context.Set<DeliveryPriority>().OrderBy(dp => dp.Name).ToListAsync();
        _cache.Set(DeliveryPrioritiesCacheKey, priorities, LookupCacheDuration);
        return priorities;
    }

    /// <summary>
    /// Fetches all data needed for the Index action.
    /// DB queries run sequentially (DbContext is not thread-safe).
    /// The external API call is kicked off first so it runs concurrently with the DB work.
    /// </summary>
    private async Task<(
        List<Project> allProjects,
        List<Division> divisions,
        List<BusinessAreaLookup> businessAreas,
        (int total, int passed, int failed, int inProgress) assessmentSummary,
        List<ProjectMonthlyUpdate> monthlyUpdates,
        List<AccessibilityIssue> accessibilityIssues)>
        FetchIndexDataAsync(DateTime now)
    {
        // Start the external API call in the background – it doesn't touch DbContext
        var assessmentsTask = GetServiceAssessmentSummaryCachedAsync();

        // DB queries must be sequential on the same DbContext instance
        var allProjects      = await GetAllProjectsCachedAsync();
        var divisions        = await GetDivisionsCachedAsync();
        var businessAreas    = await GetBusinessAreasCachedAsync();
        var monthlyUpdates   = await _context.Set<ProjectMonthlyUpdate>()
                                    .Where(u => u.Year == now.Year && u.Month == now.Month)
                                    .ToListAsync();
        var accessibilityIssues = await _context.Set<AccessibilityIssue>()
                                    .Where(i => !i.IsDeleted)
                                    .ToListAsync();

        // Now await the API result (likely already done by the time we get here)
        var assessmentSummary = await assessmentsTask;

        return (allProjects, divisions, businessAreas, assessmentSummary, monthlyUpdates, accessibilityIssues);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // INDEX – high-level overview for everyone
    // ─────────────────────────────────────────────────────────────────────────
    [HttpGet]
    [Route("Reports")]
    [Route("Reports/Index")]
    public async Task<IActionResult> Index()
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null)
        {
            TempData["ErrorMessage"] = "Unable to identify the current user.";
            return RedirectToAction("Index", "Home");
        }

        var isAdmin = await IsAdminUserAsync();

        // Parallelise independent data fetches
        var now = DateTime.UtcNow;
        var (allProjects, divisions, businessAreas, assessmentSummary, monthlyUpdates, accessibilityIssues) =
            await FetchIndexDataAsync(now);

        // RAG breakdown (with legacy string fallback)
        var ragBreakdown = allProjects
            .GroupBy(p => GetProjectRagName(p))
            .ToDictionary(g => g.Key, g => g.Count());

        // Phase breakdown
        var phaseBreakdown = allProjects
            .GroupBy(p => p.PhaseLookup?.Name ?? "Not set")
            .ToDictionary(g => g.Key, g => g.Count());

        // Division project counts
        var divisionProjectCounts = divisions.Select(d => new
        {
            Division = d,
            Count = allProjects.Count(p => p.Directorates.Any(dir => dir.DivisionId == d.Id)),
            RedCount = allProjects.Count(p => p.Directorates.Any(dir => dir.DivisionId == d.Id) &&
                (GetProjectRagName(p) == "Red" || GetProjectRagName(p) == "Amber-Red")),
            FlagshipCount = allProjects.Count(p => p.Directorates.Any(dir => dir.DivisionId == d.Id) && p.IsFlagship)
        }).ToList();

        // Business area project counts
        var baProjectCounts = businessAreas.Select(ba => new
        {
            BusinessArea = ba,
            Count = allProjects.Count(p => p.BusinessAreaId == ba.Id),
            RedCount = allProjects.Count(p => p.BusinessAreaId == ba.Id &&
                (GetProjectRagName(p) == "Red" || GetProjectRagName(p) == "Amber-Red")),
            FlagshipCount = allProjects.Count(p => p.BusinessAreaId == ba.Id && p.IsFlagship)
        }).ToList();

        var totalActiveProjects = allProjects.Count(p => p.Status == "Active");
        var submittedThisMonth  = monthlyUpdates.Count;

        // Accessibility summary from AISS
        var totalOpenAccessibilityIssues = accessibilityIssues.Count(i => i.Status != "resolved" && i.Status != "wont_fix");
        var criticalAccessibilityIssues  = 0; // AccessibilityIssue doesn't have a Severity field

        var (totalAssessments, passedAssessments, failedAssessments, inProgressAssessments) = assessmentSummary;

        // Flagship / multi-dept counts
        var flagshipCount = allProjects.Count(p => p.IsFlagship);
        var multiDeptCount = allProjects.Count(p => p.IsMultiDepartmentProject);
        var aiInitiativeCount = allProjects.Count(p => p.IsAiInitiative);

        // High risk/issue counts
        var highRiskProjects = allProjects.Count(p =>
            p.Risks.Any(r => !r.IsDeleted && (r.RiskScore >= 16 || (r.LikelihoodRating >= 4 && r.ImpactRating >= 4))));
        var highIssueProjects = allProjects.Count(p =>
            p.Issues.Any(i => !i.IsDeleted && (i.Severity == "critical" || i.Severity == "high") &&
                i.Status != "resolved" && i.Status != "closed"));

        ViewData["Title"] = "Reports overview";
        ViewBag.IsAdmin = isAdmin;
        ViewBag.AllProjects = allProjects;
        ViewBag.TotalProjects = allProjects.Count;
        ViewBag.ActiveProjects = totalActiveProjects;
        ViewBag.Divisions = divisions;
        ViewBag.BusinessAreas = businessAreas;
        ViewBag.RagBreakdown = ragBreakdown;
        ViewBag.PhaseBreakdown = phaseBreakdown;
        ViewBag.DivisionProjectCounts = divisionProjectCounts;
        ViewBag.BaProjectCounts = baProjectCounts;
        ViewBag.TotalActiveProjects = totalActiveProjects;
        ViewBag.SubmittedThisMonth = submittedThisMonth;
        ViewBag.MonthName = now.ToString("MMMM yyyy");
        ViewBag.TotalOpenAccessibilityIssues = totalOpenAccessibilityIssues;
        ViewBag.CriticalAccessibilityIssues = criticalAccessibilityIssues;
        ViewBag.TotalAssessments = totalAssessments;
        ViewBag.PassedAssessments = passedAssessments;
        ViewBag.FailedAssessments = failedAssessments;
        ViewBag.InProgressAssessments = inProgressAssessments;
        ViewBag.FlagshipCount = flagshipCount;
        ViewBag.MultiDeptCount = multiDeptCount;
        ViewBag.AiInitiativeCount = aiInitiativeCount;
        ViewBag.HighRiskProjects = highRiskProjects;
        ViewBag.HighIssueProjects = highIssueProjects;

        return View();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // DIVISION REPORT
    // ─────────────────────────────────────────────────────────────────────────
    [HttpGet]
    [Route("Reports/DivisionReport")]
    public async Task<IActionResult> DivisionReport(int? divisionId = null)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null)
        {
            TempData["ErrorMessage"] = "Unable to identify the current user.";
            return RedirectToAction("Index", "Home");
        }

        var isAdmin = await IsAdminUserAsync();
        List<Division> availableDivisions;

        if (isAdmin)
        {
            availableDivisions = await GetDivisionsCachedAsync();
        }
        else
        {
            availableDivisions = currentUser.DivisionUsers
                .Where(du => du.Division != null && du.Division.IsActive)
                .Select(du => du.Division!).ToList();

            if (!availableDivisions.Any())
            {
                TempData["ErrorMessage"] = "You are not assigned to any divisions.";
                return RedirectToAction(nameof(Index));
            }
        }

        // Select division to show
        Division? selectedDivision = null;
        if (divisionId.HasValue)
        {
            selectedDivision = availableDivisions.FirstOrDefault(d => d.Id == divisionId.Value);
        }
        if (selectedDivision == null)
        {
            selectedDivision = availableDivisions.FirstOrDefault();
        }

        List<Project> projects;
        if (selectedDivision != null)
        {
            projects = await AllProjectsQuery()
                .Where(p => p.Directorates.Any(d => d.DivisionId == selectedDivision.Id))
                .OrderBy(p => p.Title)
                .ToListAsync();
        }
        else
        {
            projects = new List<Project>();
        }

        // RAG breakdown for selected division (with legacy string fallback)
        var ragBreakdown = projects
            .GroupBy(p => GetProjectRagName(p))
            .ToDictionary(g => g.Key, g => g.Count());

        // Phase breakdown
        var phaseBreakdown = projects
            .GroupBy(p => p.PhaseLookup?.Name ?? "Not set")
            .ToDictionary(g => g.Key, g => g.Count());

        // Monthly reporting for this division's projects
        var now = DateTime.UtcNow;
        var projectIds = projects.Select(p => p.Id).ToList();
        var monthlyUpdates = await _context.Set<ProjectMonthlyUpdate>()
            .Where(u => u.Year == now.Year && u.Month == now.Month
                && projectIds.Contains(u.ProjectId))
            .ToListAsync();

        // Business areas within this division
        var divisionBusinessAreas = selectedDivision != null
            ? await _context.DivisionBusinessAreas
                .Where(dba => dba.DivisionId == selectedDivision.Id)
                .Include(dba => dba.BusinessAreaLookup)
                .ToListAsync()
            : new List<DivisionBusinessArea>();

        // Start external API calls in background (no DbContext involvement)
        var assessmentsTask = GetServiceAssessmentSummaryCachedAsync();
        var productsTask    = GetProductsCachedAsync();

        // DB queries sequential
        var accessibilityIssues = await _context.Set<AccessibilityIssue>().Where(i => !i.IsDeleted).ToListAsync();

        // Await external results (likely already done)
        var (totalAssessments, passedAssessments, failedAssessments, _) = await assessmentsTask;
        var allProducts = await productsTask;

        var allDivisionsForNav = await GetDivisionsCachedAsync();
        var allBusinessAreasForNav = await GetBusinessAreasCachedAsync();

        ViewData["Title"] = selectedDivision != null ? $"{selectedDivision.Name} – division report" : "Division report";
        ViewBag.IsAdmin = isAdmin;
        ViewBag.AvailableDivisions = availableDivisions;
        ViewBag.AllDivisions = allDivisionsForNav;
        ViewBag.AllBusinessAreas = allBusinessAreasForNav;
        ViewBag.SelectedDivision = selectedDivision;
        ViewBag.Projects = projects;
        ViewBag.ProjectCount = projects.Count;
        ViewBag.RagBreakdown = ragBreakdown;
        ViewBag.PhaseBreakdown = phaseBreakdown;
        ViewBag.SubmittedThisMonth = monthlyUpdates.Count;
        ViewBag.MonthName = now.ToString("MMMM yyyy");
        ViewBag.DivisionBusinessAreas = divisionBusinessAreas;
        ViewBag.AccessibilityIssues = accessibilityIssues;
        ViewBag.TotalAssessments = totalAssessments;
        ViewBag.PassedAssessments = passedAssessments;
        ViewBag.FailedAssessments = failedAssessments;
        ViewBag.FlagshipCount = projects.Count(p => p.IsFlagship);
        ViewBag.MultiDeptCount = projects.Count(p => p.IsMultiDepartmentProject);
        ViewBag.HighRiskCount = projects.Count(p =>
            p.Risks.Any(r => !r.IsDeleted && (r.RiskScore >= 16 || (r.LikelihoodRating >= 4 && r.ImpactRating >= 4))));
        ViewBag.HighIssueCount = projects.Count(p =>
            p.Issues.Any(i => !i.IsDeleted && (i.Severity == "critical" || i.Severity == "high") &&
                i.Status != "resolved" && i.Status != "closed"));
        ViewBag.Products = allProducts;

        return View();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // BUSINESS AREA REPORT
    // ─────────────────────────────────────────────────────────────────────────
    [HttpGet]
    [Route("Reports/BusinessAreaReport")]
    public async Task<IActionResult> BusinessAreaReport(
        int? businessAreaId = null,
        int projectPage = 1,
        int projectPageSize = 15,
        int productPage = 1,
        int productPageSize = 15,
        string? projectStatus = null,
        string? projectPhase = null,
        string? projectRag = null,
        string? projectDivision = null,
        string? projectBusinessArea = null,
        string? projectSearch = null,
        string? productPhase = null,
        string? productBusinessArea = null,
        string? productSearch = null)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null)
        {
            TempData["ErrorMessage"] = "Unable to identify the current user.";
            return RedirectToAction("Index", "Home");
        }

        var isAdmin = await IsAdminUserAsync();
        List<BusinessAreaLookup> availableBusinessAreas;

        if (isAdmin)
        {
            availableBusinessAreas = await GetBusinessAreasCachedAsync();
        }
        else
        {
            availableBusinessAreas = currentUser.BusinessAreaUsers
                .Where(bau => bau.BusinessAreaLookup != null && bau.BusinessAreaLookup.IsActive)
                .Select(bau => bau.BusinessAreaLookup!).ToList();

            if (!availableBusinessAreas.Any())
            {
                TempData["ErrorMessage"] = "You are not assigned to any business areas.";
                return RedirectToAction(nameof(Index));
            }
        }

        // Select business area to show (null = "All business areas" view)
        BusinessAreaLookup? selectedBa = null;
        if (businessAreaId.HasValue)
        {
            selectedBa = availableBusinessAreas.FirstOrDefault(ba => ba.Id == businessAreaId.Value);
        }

        var availableBaIds = availableBusinessAreas.Select(ba => ba.Id).ToList();
        List<Project> projects;
        if (selectedBa != null)
        {
            projects = await AllProjectsQuery()
                .Where(p => p.BusinessAreaId == selectedBa.Id)
                .OrderBy(p => p.Title)
                .ToListAsync();
        }
        else
        {
            // All business areas: include projects from every available business area
            projects = await AllProjectsQuery()
                .Where(p => p.BusinessAreaId != null && availableBaIds.Contains(p.BusinessAreaId.Value))
                .OrderBy(p => p.Title)
                .ToListAsync();
        }

        // RAG breakdown (with legacy string fallback)
        var ragBreakdown = projects
            .GroupBy(p => GetProjectRagName(p))
            .ToDictionary(g => g.Key, g => g.Count());

        // Phase breakdown
        var phaseBreakdown = projects
            .GroupBy(p => p.PhaseLookup?.Name ?? "Not set")
            .ToDictionary(g => g.Key, g => g.Count());

        // Monthly reporting
        var now = DateTime.UtcNow;
        var projectIds = projects.Select(p => p.Id).ToList();
        var monthlyUpdates = await _context.Set<ProjectMonthlyUpdate>()
            .Where(u => u.Year == now.Year && u.Month == now.Month
                && projectIds.Contains(u.ProjectId))
            .ToListAsync();

        // Leadership for this business area (only when a single BA is selected)
        var leadershipAssignments = selectedBa != null
            ? await _context.Set<UserBusinessAreaRoleAssignment>()
                .Where(a => a.BusinessAreaName == selectedBa.Name)
                .Include(a => a.User)
                .OrderBy(a => a.Role)
                .ToListAsync()
            : new List<UserBusinessAreaRoleAssignment>();

        // Start external API calls in background (no DbContext involvement)
        var assessmentsTask2 = GetServiceAssessmentSummaryCachedAsync();
        var productsTask2    = GetProductsCachedAsync();

        // DB queries sequential
        var accessibilityIssues = await _context.Set<AccessibilityIssue>().Where(i => !i.IsDeleted).ToListAsync();

        // Await external results (likely already done)
        var (totalAssessments, passedAssessments, failedAssessments, _) = await assessmentsTask2;
        var allProducts = await productsTask2;

        // Products for this business area (or all BAs when viewing "All business areas")
        var productDtos = allProducts.OfType<ProductDto>().ToList();
        var baNamesForFilter = selectedBa != null
            ? new HashSet<string>(StringComparer.OrdinalIgnoreCase) { selectedBa.Name }
            : availableBusinessAreas.Select(ba => ba.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var productsForBusinessAreaFull = productDtos
            .Where(p => p.CategoryValues?.Any(cv =>
                cv.CategoryType?.Name?.Equals("Business area", StringComparison.OrdinalIgnoreCase) == true &&
                !string.IsNullOrEmpty(cv.Name) &&
                baNamesForFilter.Contains(cv.Name)) == true)
            .OrderBy(p => p.Title)
            .ToList();

        // Filter projects
        var filteredProjects = projects.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(projectStatus))
            filteredProjects = filteredProjects.Where(p => string.Equals(p.Status, projectStatus?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(projectPhase))
            filteredProjects = filteredProjects.Where(p => string.Equals(p.PhaseLookup?.Name, projectPhase?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(projectRag))
        {
            var ragNorm = projectRag.Trim().Replace(" / ", "-").Replace("/", "-");
            filteredProjects = filteredProjects.Where(p =>
                string.Equals(GetProjectRagName(p), ragNorm, StringComparison.OrdinalIgnoreCase));
        }
        if (!string.IsNullOrWhiteSpace(projectDivision))
            filteredProjects = filteredProjects.Where(p => p.Directorates.Any(d =>
                string.Equals(d.Division?.Name, projectDivision?.Trim(), StringComparison.OrdinalIgnoreCase)));
        if (!string.IsNullOrWhiteSpace(projectBusinessArea))
            filteredProjects = filteredProjects.Where(p => string.Equals(p.BusinessAreaLookup?.Name, projectBusinessArea?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(projectSearch))
        {
            var q = projectSearch.Trim().ToLowerInvariant();
            filteredProjects = filteredProjects.Where(p =>
                (p.Title?.ToLowerInvariant().Contains(q) == true) ||
                (p.ProjectCode?.ToLowerInvariant().Contains(q) == true));
        }
        var filteredProjectList = filteredProjects.ToList();
        var projectTotalFiltered = filteredProjectList.Count;
        projectPageSize = Math.Clamp(projectPageSize, 5, 100);
        var projectTotalPages = projectTotalFiltered == 0 ? 1 : (int)Math.Ceiling(projectTotalFiltered / (double)projectPageSize);
        projectPage = Math.Clamp(projectPage, 1, projectTotalPages);
        var pagedProjects = filteredProjectList
            .Skip((projectPage - 1) * projectPageSize)
            .Take(projectPageSize)
            .ToList();

        // Filter products
        var filteredProducts = productsForBusinessAreaFull.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(productPhase))
            filteredProducts = filteredProducts.Where(p => string.Equals(p.Phase, productPhase?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(productBusinessArea))
        {
            filteredProducts = filteredProducts.Where(p =>
                p.CategoryValues?.Any(cv =>
                    cv.CategoryType?.Name?.Equals("Business area", StringComparison.OrdinalIgnoreCase) == true &&
                    string.Equals(cv.Name, productBusinessArea?.Trim(), StringComparison.OrdinalIgnoreCase)) == true);
        }
        if (!string.IsNullOrWhiteSpace(productSearch))
        {
            var q = productSearch.Trim().ToLowerInvariant();
            filteredProducts = filteredProducts.Where(p =>
                (p.Title?.ToLowerInvariant().Contains(q) == true) ||
                (p.FipsId?.ToLowerInvariant().Contains(q) == true));
        }
        var filteredProductList = filteredProducts.ToList();
        var productTotalFiltered = filteredProductList.Count;
        productPageSize = Math.Clamp(productPageSize, 5, 100);
        var productTotalPages = productTotalFiltered == 0 ? 1 : (int)Math.Ceiling(productTotalFiltered / (double)productPageSize);
        productPage = Math.Clamp(productPage, 1, productTotalPages);
        var pagedProducts = filteredProductList
            .Skip((productPage - 1) * productPageSize)
            .Take(productPageSize)
            .ToList();

        // Distinct values for filter dropdowns (from full lists)
        var projectDivisionNames = projects
            .SelectMany(p => p.Directorates.Select(d => d.Division?.Name).Where(n => !string.IsNullOrEmpty(n)))
            .Distinct()
            .OrderBy(n => n)
            .ToList();
        var projectPhaseNames = projects
            .Select(p => p.PhaseLookup?.Name)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Distinct()
            .OrderBy(n => n)
            .Cast<string>()
            .ToList();
        var projectRagNames = ragBreakdown.Keys.OrderBy(k => k).ToList();
        var projectStatusNames = projects
            .Select(p => p.Status)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct()
            .OrderBy(s => s)
            .Cast<string>()
            .ToList();
        var productPhaseNames = productsForBusinessAreaFull
            .Select(p => p.Phase)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct()
            .OrderBy(s => s)
            .ToList();
        var productBusinessAreaNames = productsForBusinessAreaFull
            .SelectMany(p => p.CategoryValues ?? new List<CategoryValueDto>())
            .Where(cv => cv.CategoryType?.Name?.Equals("Business area", StringComparison.OrdinalIgnoreCase) == true && !string.IsNullOrWhiteSpace(cv.Name))
            .Select(cv => cv.Name!)
            .Distinct()
            .OrderBy(n => n)
            .ToList();

        var allDivisionsForNav2 = await GetDivisionsCachedAsync();
        var allBusinessAreasForNav2 = await GetBusinessAreasCachedAsync();

        ViewData["Title"] = selectedBa != null ? $"{selectedBa.Name} – business area report" : "Business area report";
        ViewBag.IsAdmin = isAdmin;
        ViewBag.AvailableBusinessAreas = availableBusinessAreas;
        ViewBag.AllDivisions = allDivisionsForNav2;
        ViewBag.AllBusinessAreas = allBusinessAreasForNav2;
        ViewBag.SelectedBusinessArea = selectedBa;
        ViewBag.Projects = pagedProjects;
        ViewBag.ProjectCount = projects.Count;
        ViewBag.FilteredProjectCount = projectTotalFiltered;
        ViewBag.ProjectPage = projectPage;
        ViewBag.ProjectPageSize = projectPageSize;
        ViewBag.ProjectTotalPages = projectTotalPages;
        ViewBag.ProjectStatus = projectStatus;
        ViewBag.ProjectPhase = projectPhase;
        ViewBag.ProjectRag = projectRag;
        ViewBag.ProjectDivision = projectDivision;
        ViewBag.ProjectBusinessArea = projectBusinessArea;
        ViewBag.ProjectSearch = projectSearch;
        ViewBag.ProjectDivisionNames = projectDivisionNames;
        ViewBag.ProjectPhaseNames = projectPhaseNames;
        ViewBag.ProjectRagNames = projectRagNames;
        ViewBag.ProjectStatusNames = projectStatusNames;
        ViewBag.AllFilteredProjectsForModal = filteredProjectList;
        ViewBag.RagBreakdown = ragBreakdown;
        ViewBag.PhaseBreakdown = phaseBreakdown;
        ViewBag.SubmittedThisMonth = monthlyUpdates.Count;
        ViewBag.MonthName = now.ToString("MMMM yyyy");
        ViewBag.LeadershipAssignments = leadershipAssignments;
        ViewBag.AccessibilityIssues = accessibilityIssues;
        ViewBag.TotalAssessments = totalAssessments;
        ViewBag.PassedAssessments = passedAssessments;
        ViewBag.FailedAssessments = failedAssessments;
        ViewBag.FlagshipCount = projects.Count(p => p.IsFlagship);
        ViewBag.MultiDeptCount = projects.Count(p => p.IsMultiDepartmentProject);
        ViewBag.HighRiskCount = projects.Count(p =>
            p.Risks.Any(r => !r.IsDeleted && (r.RiskScore >= 16 || (r.LikelihoodRating >= 4 && r.ImpactRating >= 4))));
        ViewBag.HighIssueCount = projects.Count(p =>
            p.Issues.Any(i => !i.IsDeleted && (i.Severity == "critical" || i.Severity == "high") &&
                i.Status != "resolved" && i.Status != "closed"));
        ViewBag.Products = allProducts;
        ViewBag.ProductsForBusinessArea = pagedProducts;
        ViewBag.FilteredProductCount = productTotalFiltered;
        ViewBag.ProductPage = productPage;
        ViewBag.ProductPageSize = productPageSize;
        ViewBag.ProductTotalPages = productTotalPages;
        ViewBag.ProductPhase = productPhase;
        ViewBag.ProductBusinessArea = productBusinessArea;
        ViewBag.ProductSearch = productSearch;
        ViewBag.ProductPhaseNames = productPhaseNames;
        ViewBag.ProductBusinessAreaNames = productBusinessAreaNames;

        return View();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PRIORITY OUTCOMES REPORT – flagship, priority, multi-dept
    // ─────────────────────────────────────────────────────────────────────────
    [HttpGet]
    [Route("Reports/PriorityOutcomes")]
    public async Task<IActionResult> PriorityOutcomes()
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null)
        {
            TempData["ErrorMessage"] = "Unable to identify the current user.";
            return RedirectToAction("Index", "Home");
        }

        var isAdmin = await IsAdminUserAsync();

        // Use cached projects – no second DB round-trip
        var allProjects = await GetAllProjectsCachedAsync();

        var flagshipProjects  = allProjects.Where(p => p.IsFlagship).ToList();
        var multiDeptProjects = allProjects.Where(p => p.IsMultiDepartmentProject).ToList();
        var aiProjects        = allProjects.Where(p => p.IsAiInitiative).ToList();
        var priorityProjects  = allProjects
            .Where(p => p.DeliveryPriorityId != null)
            .OrderBy(p => p.DeliveryPriority?.Name).ThenBy(p => p.Title)
            .ToList();

        // Delivery priorities lookup (cached)
        var deliveryPriorities = await GetDeliveryPrioritiesCachedAsync();

        // Projects grouped by delivery priority
        var projectsByPriority = deliveryPriorities
            .Select(dp => new
            {
                Priority = dp,
                Projects = priorityProjects.Where(p => p.DeliveryPriorityId == dp.Id).ToList()
            })
            .Where(x => x.Projects.Any())
            .ToList();

        var allDivisionsForNav3 = await GetDivisionsCachedAsync();
        var allBusinessAreasForNav3 = await GetBusinessAreasCachedAsync();

        ViewData["Title"] = "Priority outcomes and flagship projects";
        ViewBag.IsAdmin          = isAdmin;
        ViewBag.AllDivisions = allDivisionsForNav3;
        ViewBag.AllBusinessAreas = allBusinessAreasForNav3;
        ViewBag.FlagshipProjects  = flagshipProjects;
        ViewBag.MultiDeptProjects = multiDeptProjects;
        ViewBag.AiProjects        = aiProjects;
        ViewBag.PriorityProjects  = priorityProjects;
        ViewBag.ProjectsByPriority = projectsByPriority;
        ViewBag.DeliveryPriorities = deliveryPriorities;
        ViewBag.TotalFlagship  = flagshipProjects.Count;
        ViewBag.TotalMultiDept = multiDeptProjects.Count;
        ViewBag.TotalAi        = aiProjects.Count;
        ViewBag.TotalPriority  = priorityProjects.Count;
        // Pass all projects so the view can show a fallback "all" tab
        ViewBag.AllProjects    = allProjects;

        return View();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PERFORMANCE REPORTING – commission reporting dashboard (portfolio completion, 9 metrics)
    // ─────────────────────────────────────────────────────────────────────────
    [HttpGet]
    [Route("Reports/PerformanceReporting")]
    public async Task<IActionResult> PerformanceReporting(int? commissionId = null)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null)
        {
            TempData["ErrorMessage"] = "Unable to identify the current user.";
            return RedirectToAction("Index", "Home");
        }

        var viewModel = new PerformanceReportingViewModel();

        var activeCommissions = await _context.Commissions
            .Where(c => c.IsActive)
            .OrderByDescending(c => c.DueDate)
            .ToListAsync();

        viewModel.ActiveCommissions = activeCommissions;

        if (!activeCommissions.Any())
        {
            ViewData["Title"] = "Performance reporting";
            ViewBag.AllDivisions = await GetDivisionsCachedAsync();
            ViewBag.AllBusinessAreas = await GetBusinessAreasCachedAsync();
            ViewBag.IsAdmin = await IsAdminUserAsync();
            return View(viewModel);
        }

        var selectedCommission = commissionId.HasValue
            ? activeCommissions.FirstOrDefault(c => c.Id == commissionId.Value)
            : activeCommissions.FirstOrDefault();
        viewModel.SelectedCommission = selectedCommission;

        if (selectedCommission == null)
        {
            ViewData["Title"] = "Performance reporting";
            ViewBag.AllDivisions = await GetDivisionsCachedAsync();
            ViewBag.AllBusinessAreas = await GetBusinessAreasCachedAsync();
            ViewBag.IsAdmin = await IsAdminUserAsync();
            return View(viewModel);
        }

        List<string> productDocumentIdsInScope;
        List<(string DocumentId, string BusinessAreaName, string ProductTitle)> productBaList;
        try
        {
            var allProducts = await _productsApiService.GetProductsAsync();
            var inScopeProducts = allProducts
                .Where(p => !string.IsNullOrEmpty(p.DocumentId))
                .Where(p => string.IsNullOrEmpty(p.Phase) ||
                    (!p.Phase.Equals("Decommissioned", StringComparison.OrdinalIgnoreCase) &&
                     !p.Phase.Equals("Decommissioning", StringComparison.OrdinalIgnoreCase)))
                .Where(p =>
                {
                    var types = p.CategoryValues?
                        .Where(cv => cv.CategoryType?.Name?.Trim().Equals("Type", StringComparison.OrdinalIgnoreCase) == true)
                        .Select(cv => cv.Name?.Trim() ?? string.Empty)
                        .Where(name => !string.IsNullOrWhiteSpace(name))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList() ?? new List<string>();
                    if (!types.Any()) return true;
                    if (types.Count == 1 && types[0].Trim().Equals("Data", StringComparison.OrdinalIgnoreCase)) return false;
                    if (types.All(t => t.Trim().Equals("Data", StringComparison.OrdinalIgnoreCase))) return false;
                    return true;
                })
                .ToList();

            productDocumentIdsInScope = inScopeProducts.Select(p => p.DocumentId!).Distinct().ToList();
            viewModel.TotalProductsInScope = productDocumentIdsInScope.Count;

            // Map document ID -> business area name and product title (for expandable product list)
            productBaList = inScopeProducts.Select(p =>
            {
                var baName = p.CategoryValues?
                    .Where(cv => cv.CategoryType?.Name?.Trim().Equals("Business area", StringComparison.OrdinalIgnoreCase) == true)
                    .Select(cv => cv.Name?.Trim() ?? string.Empty)
                    .FirstOrDefault(name => !string.IsNullOrWhiteSpace(name)) ?? "Unassigned";
                return (p.DocumentId!, baName, p.Title ?? "Untitled");
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Performance reporting: failed to load products in scope");
            productDocumentIdsInScope = new List<string>();
            productBaList = new List<(string, string, string)>();
            viewModel.TotalProductsInScope = 0;
        }

        var submissions = await _context.CommissionSubmissions
            .Include(cs => cs.MetricValues)
            .Where(cs => cs.CommissionId == selectedCommission.Id)
            .ToListAsync();

        var submittedCount = submissions.Count(s => s.Status == CommissionSubmissionStatus.Submitted);
        var inProgressCount = submissions.Count(s => s.Status == CommissionSubmissionStatus.InProgress);
        var notStartedCount = submissions.Count(s => s.Status == CommissionSubmissionStatus.NotStarted);

        var submissionDocumentIds = submissions.Select(s => s.ProductDocumentId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var notStartedFromScope = productDocumentIdsInScope.Count(id => !submissionDocumentIds.Contains(id));
        notStartedCount += notStartedFromScope;

        // Late = services with no submission (total in scope minus submitted)
        var lateCount = viewModel.TotalProductsInScope - submittedCount;

        viewModel.SubmittedCount = submittedCount;
        viewModel.InProgressCount = inProgressCount;
        viewModel.NotStartedCount = notStartedCount;
        viewModel.LateCount = lateCount;

        var totalForCompletion = viewModel.TotalProductsInScope;
        viewModel.SubmissionCompletionPercentage = totalForCompletion > 0
            ? Math.Round((decimal)submittedCount * 100 / totalForCompletion, 1)
            : 0;

        // Business area completion: group products by BA, then count submission status per BA
        var submissionByDocId = submissions.ToDictionary(s => s.ProductDocumentId, s => s, StringComparer.OrdinalIgnoreCase);
        var performanceMetrics = await _context.PerformanceMetrics
            .Where(pm => !pm.IsDisabled)
            .OrderBy(pm => pm.Identifier)
            .ToListAsync();
        var baGroups = productBaList.GroupBy(x => x.BusinessAreaName, StringComparer.OrdinalIgnoreCase);
        foreach (var baGroup in baGroups.OrderByDescending(g =>
        {
            var docIdsInBa = g.Select(x => x.DocumentId).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var totalInBa = docIdsInBa.Count;
            var submittedInBa = docIdsInBa.Count(id => submissionByDocId.TryGetValue(id, out var sub) && sub.Status == CommissionSubmissionStatus.Submitted);
            return totalInBa > 0 ? (decimal)submittedInBa * 100 / totalInBa : 0;
        }).ThenBy(g => g.Key))
        {
            var baName = baGroup.Key;
            var docIdsInBa = baGroup.Select(x => x.DocumentId).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var totalInBa = docIdsInBa.Count;
            var submittedInBa = docIdsInBa.Count(id => submissionByDocId.TryGetValue(id, out var sub) && sub.Status == CommissionSubmissionStatus.Submitted);
            var inProgressInBa = docIdsInBa.Count(id => submissionByDocId.TryGetValue(id, out var sub) && sub.Status == CommissionSubmissionStatus.InProgress);
            var notStartedInBa = docIdsInBa.Count(id => !submissionByDocId.ContainsKey(id));
            // Late = services in this BA with no submission (total in BA minus submitted)
            var lateInBa = totalInBa - submittedInBa;
            var productTitles = baGroup.Select(x => x.ProductTitle).OrderBy(t => t, StringComparer.OrdinalIgnoreCase).ToList();
            var productsWithMetrics = new List<ProductInBusinessAreaMetrics>();
            foreach (var (docId, _, productTitle) in baGroup.OrderBy(x => x.ProductTitle, StringComparer.OrdinalIgnoreCase))
            {
                var submission = submissionByDocId.GetValueOrDefault(docId);
                var statusStr = submission == null ? "Not started" : submission.Status switch
                {
                    CommissionSubmissionStatus.Submitted => "Submitted",
                    CommissionSubmissionStatus.InProgress => "In progress",
                    CommissionSubmissionStatus.Late => "Late",
                    _ => "Not started"
                };
                var metricRows = new List<MetricValueForProduct>();
                foreach (var metric in performanceMetrics)
                {
                    var mv = submission?.MetricValues?.FirstOrDefault(m => m.PerformanceMetricId == metric.Id);
                    var displayValue = mv == null ? "–" : (mv.IsNotCaptured ? "Not captured" : (string.IsNullOrWhiteSpace(mv.Value) ? "–" : mv.Value.Trim()));
                    var isComplete = mv != null && (mv.IsComplete || (!mv.IsNotCaptured && !string.IsNullOrWhiteSpace(mv.Value)));
                    metricRows.Add(new MetricValueForProduct
                    {
                        PerformanceMetricId = metric.Id,
                        MetricTitle = metric.Title ?? metric.Identifier,
                        DisplayValue = displayValue,
                        IsComplete = isComplete,
                        ApplicablePhases = metric.ApplicablePhases ?? string.Empty,
                        ApplicableTypes = metric.ApplicableTypes ?? string.Empty
                    });
                }
                productsWithMetrics.Add(new ProductInBusinessAreaMetrics
                {
                    ProductTitle = productTitle,
                    ProductDocumentId = docId,
                    SubmissionStatus = statusStr,
                    MetricRows = metricRows
                });
            }
            viewModel.BusinessAreaCompletions.Add(new BusinessAreaCompletionItem
            {
                BusinessAreaName = baName,
                TotalProducts = totalInBa,
                SubmittedCount = submittedInBa,
                InProgressCount = inProgressInBa,
                NotStartedCount = notStartedInBa,
                LateCount = lateInBa,
                CompletionPercentage = totalInBa > 0 ? Math.Round((decimal)submittedInBa * 100 / totalInBa, 1) : 0,
                ProductTitles = productTitles,
                Products = productsWithMetrics
            });
        }

        // Submission comments (overall comments from submissions)
        foreach (var sub in submissions.Where(s => !string.IsNullOrWhiteSpace(s.Comments)))
        {
            viewModel.SubmissionComments.Add(new SubmissionCommentItem
            {
                ProductTitle = sub.ProductTitle,
                Comments = sub.Comments?.Trim(),
                SubmittedDate = sub.SubmittedDate,
                SubmittedBy = sub.SubmittedBy
            });
        }
        viewModel.SubmissionComments = viewModel.SubmissionComments
            .OrderByDescending(c => c.SubmittedDate)
            .Take(50)
            .ToList();

        var totalSubmissions = submissions.Count;
        foreach (var metric in performanceMetrics)
        {
            var metricValues = submissions
                .SelectMany(s => s.MetricValues?.Where(mv => mv.PerformanceMetricId == metric.Id) ?? Array.Empty<CommissionMetricValue>())
                .ToList();
            var completedCount = submissions.Count(s =>
                s.MetricValues?.Any(mv => mv.PerformanceMetricId == metric.Id && (mv.IsComplete || (!string.IsNullOrWhiteSpace(mv.Value) && !mv.IsNotCaptured))) == true);
            var pct = totalSubmissions > 0 ? Math.Round((decimal)completedCount * 100 / totalSubmissions, 1) : 0;

            string? dataSummary = null;
            var valueCount = 0;
            var numericValues = new List<decimal>();
            foreach (var mv in metricValues.Where(mv => !mv.IsNotCaptured && !string.IsNullOrWhiteSpace(mv.Value)))
            {
                valueCount++;
                if (decimal.TryParse(mv.Value!.Trim(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var num))
                    numericValues.Add(num);
            }
            decimal? numericMin = null, numericMax = null, numericAvg = null;
            if (numericValues.Count > 0)
            {
                numericMin = numericValues.Min();
                numericMax = numericValues.Max();
                numericAvg = Math.Round(numericValues.Average(), 1);
                dataSummary = $"{numericValues.Count} value(s)";
            }
            else if (valueCount > 0)
                dataSummary = $"{valueCount} value(s) submitted";

            var comments = new List<MetricCommentItem>();
            foreach (var sub in submissions)
            {
                var subMetricValues = sub.MetricValues?.Where(mv => mv.PerformanceMetricId == metric.Id && (!string.IsNullOrWhiteSpace(mv.NotCapturedReason) || !string.IsNullOrWhiteSpace(mv.ReasonForDifference))) ?? Array.Empty<CommissionMetricValue>();
                foreach (var mv in subMetricValues)
                {
                    comments.Add(new MetricCommentItem
                    {
                        ProductTitle = sub.ProductTitle,
                        Value = mv.Value,
                        NotCapturedReason = mv.NotCapturedReason?.Trim(),
                        ReasonForDifference = mv.ReasonForDifference?.Trim()
                    });
                }
            }
            comments = comments.Take(20).ToList();

            viewModel.MetricCompletions.Add(new MetricCompletionSummary
            {
                PerformanceMetricId = metric.Id,
                Identifier = metric.Identifier,
                Title = metric.Title,
                ApplicablePhases = metric.ApplicablePhases ?? string.Empty,
                ApplicableTypes = metric.ApplicableTypes ?? string.Empty,
                CompletedCount = completedCount,
                TotalSubmissions = totalSubmissions,
                CompletionPercentage = pct,
                DataSummary = dataSummary,
                ValueCount = valueCount,
                NumericMin = numericMin,
                NumericMax = numericMax,
                NumericAvg = numericAvg,
                Comments = comments
            });
        }
        viewModel.MetricCompletions = viewModel.MetricCompletions.OrderByDescending(m => m.CompletionPercentage).ToList();

        ViewData["Title"] = "Performance reporting";
        ViewBag.AllDivisions = await GetDivisionsCachedAsync();
        ViewBag.AllBusinessAreas = await GetBusinessAreasCachedAsync();
        ViewBag.IsAdmin = await IsAdminUserAsync();
        return View(viewModel);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CACHE REFRESH – clears all reports cache entries and redirects back
    // ─────────────────────────────────────────────────────────────────────────
    [HttpPost]
    [Route("Reports/RefreshCache")]
    [ValidateAntiForgeryToken]
    public IActionResult RefreshCache(string? returnUrl = null)
    {
        _cache.Remove(AllProjectsCacheKey);
        _cache.Remove(ServiceAssessmentCacheKey);
        _cache.Remove(ProductsCacheKey);
        _cache.Remove(DivisionsCacheKey);
        _cache.Remove(BusinessAreasCacheKey);
        _cache.Remove(DeliveryPrioritiesCacheKey);
        TempData["SuccessMessage"] = "Report data refreshed.";
        return Redirect(returnUrl ?? Url.Action("Index", "Reports")!);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SERVICE OWNER / SRO REPORT (existing, kept for compatibility)
    // ─────────────────────────────────────────────────────────────────────────
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

            User? selectedUser = currentUser;
            if (userId.HasValue && isAdmin)
            {
                selectedUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId.Value)
                    ?? currentUser;
            }

            var userEmail = selectedUser.Email.ToLower();
            var viewModel = new ServiceOwnerSroReportViewModel
            {
                CurrentUser = currentUser,
                SelectedUser = selectedUser,
                IsAdmin = isAdmin
            };

            var allProducts = await _productsApiService.GetProductsAsync();
            var userProducts = allProducts.Where(p =>
                (p.ServiceOwners?.Any(so => so.EmailAddress?.Equals(userEmail, StringComparison.OrdinalIgnoreCase) == true) == true) ||
                (p.SeniorResponsibleOfficers?.Any(sro => sro.EmailAddress?.Equals(userEmail, StringComparison.OrdinalIgnoreCase) == true) == true)
            ).ToList();

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

                viewModel.Products.Add(new ProductSummary
                {
                    DocumentId = product.DocumentId ?? string.Empty,
                    FipsId = product.FipsId ?? string.Empty,
                    Title = product.Title,
                    Phase = product.Phase,
                    State = product.State,
                    ProductUrl = product.ProductUrl,
                    Roles = roles,
                    HasPerformanceReporting = false,
                    LastPerformanceSubmission = null,
                    IsPerformanceReportingOverdue = false
                });
            }

            viewModel.TotalProducts = viewModel.Products.Count;

            var userProjects = await _context.Projects
                .Where(p => !p.IsDeleted &&
                    (p.ServiceOwners.Any(so => so.UserId == selectedUser.Id) ||
                     p.SeniorResponsibleOfficers.Any(sro => sro.UserId == selectedUser.Id)))
                .Include(p => p.ServiceOwners).ThenInclude(so => so.User)
                .Include(p => p.SeniorResponsibleOfficers).ThenInclude(sro => sro.User)
                .Include(p => p.RagStatusLookup)
                .Include(p => p.PhaseLookup)
                .Include(p => p.Milestones.Where(m => !m.IsDeleted))
                .Include(p => p.Issues.Where(i => !i.IsDeleted))
                .Include(p => p.Risks.Where(r => !r.IsDeleted))
                .Include(p => p.Actions.Where(a => !a.IsDeleted))
                .OrderBy(p => p.Title)
                .ToListAsync();

            foreach (var project in userProjects)
            {
                var roles = new List<string>();
                if (project.ServiceOwners.Any(so => so.UserId == selectedUser.Id)) { roles.Add("Service Owner"); viewModel.ProjectsAsServiceOwner++; }
                if (project.SeniorResponsibleOfficers.Any(sro => sro.UserId == selectedUser.Id)) { roles.Add("SRO"); viewModel.ProjectsAsSro++; }

                var milestones = project.Milestones.Where(m => !m.IsDeleted).ToList();
                var overdueMilestones = milestones.Where(m => m.DueDate < DateTime.Today && m.Status != "complete" && m.Status != "cancelled").ToList();
                var issues = project.Issues.Where(i => !i.IsDeleted).ToList();
                var highPriorityIssues = issues.Where(i => i.Severity == "high" || i.Severity == "critical").ToList();
                var risks = project.Risks.Where(r => !r.IsDeleted).ToList();
                var highRisks = risks.Where(r => (r.LikelihoodRating >= 4 && r.ImpactRating >= 4) || r.RiskScore >= 16).ToList();
                var actions = project.Actions.Where(a => !a.IsDeleted).ToList();
                var openActions = actions.Where(a => a.Status != "completed" && a.Status != "closed").ToList();

                viewModel.Projects.Add(new ProjectSummary
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
                });
            }

            viewModel.TotalProjects = viewModel.Projects.Count;

            viewModel.PerformanceReporting.TotalProductsRequiringReporting = viewModel.Products.Count;
            viewModel.PerformanceReporting.ProductsWithSubmissions = viewModel.Products.Count(p => p.HasPerformanceReporting);
            viewModel.PerformanceReporting.ProductsOverdue = viewModel.Products.Count(p => p.IsPerformanceReportingOverdue);
            viewModel.PerformanceReporting.ProductsUpToDate = viewModel.Products.Count(p => p.HasPerformanceReporting && !p.IsPerformanceReportingOverdue);
            viewModel.PerformanceReporting.CompletionPercentage = viewModel.PerformanceReporting.TotalProductsRequiringReporting > 0
                ? (double)viewModel.PerformanceReporting.ProductsWithSubmissions / viewModel.PerformanceReporting.TotalProductsRequiringReporting * 100 : 0;

            viewModel.ProjectReporting.TotalProjects = viewModel.Projects.Count;
            viewModel.ProjectReporting.TotalMilestones = viewModel.Projects.Sum(p => p.MilestoneCount);
            viewModel.ProjectReporting.OverdueMilestones = viewModel.Projects.Sum(p => p.OverdueMilestoneCount);
            viewModel.ProjectReporting.TotalIssues = viewModel.Projects.Sum(p => p.IssueCount);
            viewModel.ProjectReporting.OpenIssues = viewModel.Projects.Sum(p => p.IssueCount);
            viewModel.ProjectReporting.HighPriorityIssues = viewModel.Projects.Sum(p => p.HighPriorityIssueCount);
            viewModel.ProjectReporting.TotalRisks = viewModel.Projects.Sum(p => p.RiskCount);
            viewModel.ProjectReporting.HighRisks = viewModel.Projects.Sum(p => p.HighRiskCount);
            viewModel.ProjectReporting.TotalActions = viewModel.Projects.Sum(p => p.ActionCount);
            viewModel.ProjectReporting.OpenActions = viewModel.Projects.Sum(p => p.OpenActionCount);
            viewModel.ProjectReporting.ProjectsByRagStatus = viewModel.Projects
                .GroupBy(p => p.RagStatus ?? "Unknown")
                .ToDictionary(g => g.Key, g => g.Count());

            try
            {
                var assessmentResponse = await _serviceAssessmentApiService.GetActionsByStandardAsync();
                if (assessmentResponse?.Assessments != null)
                {
                    var productFipsIds = viewModel.Products.Select(p => p.FipsId).Where(f => !string.IsNullOrEmpty(f)).ToList();
                    var projectCodes = viewModel.Projects.Select(p => p.ProjectCode).Where(c => !string.IsNullOrEmpty(c)).ToList();
                    var relevantAssessments = assessmentResponse.Assessments
                        .Where(a => !string.IsNullOrEmpty(a.AssessmentName) &&
                            (productFipsIds.Any(fid => a.AssessmentName.Contains(fid, StringComparison.OrdinalIgnoreCase)) ||
                             projectCodes.Any(pc => a.AssessmentName.Contains(pc, StringComparison.OrdinalIgnoreCase))))
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

            if (isAdmin)
            {
                viewModel.AllUsers = await _context.Users.OrderBy(u => u.Name).ToListAsync();
            }

            ViewData["Title"] = $"Service owner & SRO report – {selectedUser.Name}";
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
