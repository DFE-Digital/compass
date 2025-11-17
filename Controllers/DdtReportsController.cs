using ClosedXML.Excel;
using System.IO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Compass.Data;
using Compass.Models;
using Compass.Services;

namespace Compass.Controllers;

[Authorize]
public class DdtReportsController : Controller
{
    private readonly CompassDbContext _context;
    private readonly ILogger<DdtReportsController> _logger;
    private readonly IProductsApiService _productsApiService;
    private readonly IReturnStatusService _returnStatusService;
    private readonly IUserDirectoryService _userDirectoryService;
    private static readonly string[] UserGroupCategoryTypeNames =
    {
        "User group", "User Group", "User groups", "User Groups",
        "User Type", "User Types", "Audience", "Target Audience"
    };
    private static readonly string[] SeniorResponsibleOfficerRoleKeywords =
        { "senior responsible", "senior_responsible", "senior_responsible_officer", "senior-responsible-officer", "sro", "senior responsible officer" };
    private static readonly string[] InformationAssetOwnerRoleKeywords =
        { "information asset owner", "information_asset_owner", "information-asset-owner", "information asset", "iao", "informationassetowner" };
    private static readonly string[] DeliveryManagerRoleKeywords =
        { "delivery manager", "delivery_manager", "delivery-manager", "delivery lead", "delivery_lead", "delivery owner", "delivery_owner", "dm" };

    public DdtReportsController(CompassDbContext context, ILogger<DdtReportsController> logger, IProductsApiService productsApiService, IReturnStatusService returnStatusService, IUserDirectoryService userDirectoryService)
    {
        _context = context;
        _logger = logger;
        _productsApiService = productsApiService;
        _returnStatusService = returnStatusService;
        _userDirectoryService = userDirectoryService;
    }

    // GET: DdtReports/Index - Landing page
    public IActionResult Index()
    {
        return View();
    }

    // GET: DdtReports/MyReports
    public async Task<IActionResult> MyReports()
    {
        try
        {
            var userEmail = User.Identity?.Name;
            
            if (string.IsNullOrEmpty(userEmail))
            {
                _logger.LogWarning("MyReports: No user email found");
                TempData["ErrorMessage"] = "Unable to identify the current user.";
                return RedirectToAction("Index");
            }

            // Get or create the current user
            var currentUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == userEmail.ToLower());

            if (currentUser == null)
            {
                _logger.LogWarning("MyReports: User not found in database for email: {Email}", userEmail);
                TempData["ErrorMessage"] = "User account not found. Please contact an administrator.";
                return RedirectToAction("Index");
            }

            // Get projects where user is a project contact
            var myProjects = await _context.Projects
                .Where(p => !p.IsDeleted && p.ProjectContacts.Any(pc => pc.Email.ToLower() == userEmail.ToLower()))
                .Include(p => p.ProjectContacts)
                .Include(p => p.Milestones)
                .Include(p => p.Issues)
                .Include(p => p.Risks)
                .Include(p => p.ProjectProducts)
                .OrderBy(p => p.Title)
                .ToListAsync();

            // Get products where user is a product contact
            var allProducts = await _productsApiService.GetProductsAsync();
            var myProducts = allProducts
                .Where(p => p.ProductContacts?.Any(pc => 
                    pc.UsersPermissionsUser?.Email?.Equals(userEmail, StringComparison.OrdinalIgnoreCase) == true) == true)
                .OrderBy(p => p.Title)
                .ToList();

            // Calculate summary statistics
            var allActiveMilestones = myProjects.SelectMany(p => p.Milestones.Where(m => !m.IsDeleted)).ToList();
            var milestonesDueThisWeek = allActiveMilestones.Where(m => m.DueDate >= DateTime.Today && m.DueDate <= DateTime.Today.AddDays(7)).ToList();
            var overdueMilestones = allActiveMilestones.Where(m => m.DueDate < DateTime.Today && m.Status != "complete").ToList();

            var allActiveIssues = myProjects.SelectMany(p => p.Issues.Where(i => !i.IsDeleted)).ToList();
            var highPriorityIssues = allActiveIssues.Where(i => i.Severity == "high" || i.Severity == "critical").ToList();
            var openIssues = allActiveIssues.Where(i => i.Status != "resolved" && i.Status != "closed").ToList();

            // Get RAG status breakdown
            var redProjects = myProjects.Where(p => p.RagStatus == "Red").ToList();
            var amberRedProjects = myProjects.Where(p => p.RagStatus == "Amber-Red").ToList();
            var amberProjects = myProjects.Where(p => p.RagStatus == "Amber" || p.RagStatus == "Amber-Green").ToList();
            var greenProjects = myProjects.Where(p => p.RagStatus == "Green").ToList();

            // Get recent successes (last 30 days)
            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
            var recentSuccesses = myProjects.SelectMany(p => p.Successes.Where(s => s.RecordedAt >= thirtyDaysAgo))
                .OrderByDescending(s => s.RecordedAt)
                .Take(10)
                .ToList();

            // Calculate Your Tasks
            // First, combine all at-risk projects (Red and Amber-Red)
            var atRiskProjects = redProjects.Concat(amberRedProjects).ToList();
            
            // Task 1: Projects needing Path to Green documented (at risk projects without path to green)
            var projectsNeedingPathToGreen = atRiskProjects.Where(p => string.IsNullOrWhiteSpace(p.PathToGreen)).ToList();
            
            // Task 2: Expired open milestones (milestones due in the past that are not complete or cancelled)
            var expiredOpenMilestones = allActiveMilestones
                .Where(m => m.DueDate < DateTime.Today && m.Status != "complete" && m.Status != "cancelled")
                .ToList();
            
            // Task 3: Products with operational returns that are Due or Late
            var now = DateTime.UtcNow;
            var currentYear = now.Month == 1 ? now.Year - 1 : now.Year;
            var currentMonth = now.Month == 1 ? 12 : now.Month - 1;
            
            var productsNeedingReturns = new List<(Compass.Models.ProductDto Product, ReturnStatus Status, DateTime DueDate)>();
            foreach (var product in myProducts.Where(p => !string.IsNullOrEmpty(p.FipsId)))
            {
                var productReturn = await _context.ProductReturns
                    .Where(pr => pr.FipsId == product.FipsId && pr.Year == currentYear && pr.Month == currentMonth)
                    .FirstOrDefaultAsync();
                
                var status = _returnStatusService.CalculateReturnStatus(
                    currentYear, 
                    currentMonth, 
                    productReturn?.SubmittedDate);
                
                if (status == ReturnStatus.Due || status == ReturnStatus.Late)
                {
                    var dueDate = _returnStatusService.GetReturnDueDate(currentYear, currentMonth);
                    productsNeedingReturns.Add((product, status, dueDate));
                }
            }
            
            // Task 4: High priority issues (already calculated above)
            
            // Calculate dashboard metrics
            var tasksDue = projectsNeedingPathToGreen.Count + expiredOpenMilestones.Count + productsNeedingReturns.Count + highPriorityIssues.Count;
            var serviceHealthIssues = productsNeedingReturns.Count;
            var projectHealthIssues = atRiskProjects.Count;
            
            // Pass all data to ViewBag
            ViewBag.CurrentUser = currentUser;
            ViewBag.MyProjects = myProjects;
            ViewBag.MyProducts = myProducts;
            ViewBag.AllActiveMilestones = allActiveMilestones;
            ViewBag.MilestonesDueThisWeek = milestonesDueThisWeek;
            ViewBag.OverdueMilestones = overdueMilestones;
            ViewBag.AllActiveIssues = allActiveIssues;
            ViewBag.HighPriorityIssues = highPriorityIssues;
            ViewBag.OpenIssues = openIssues;
            ViewBag.RedProjects = redProjects;
            ViewBag.AmberRedProjects = amberRedProjects;
            ViewBag.AmberProjects = amberProjects;
            ViewBag.GreenProjects = greenProjects;
            ViewBag.RecentSuccesses = recentSuccesses;
            
            // Task data
            ViewBag.AtRiskProjects = atRiskProjects;
            ViewBag.ProjectsNeedingPathToGreen = projectsNeedingPathToGreen;
            ViewBag.ExpiredOpenMilestones = expiredOpenMilestones;
            ViewBag.ProductsNeedingReturns = productsNeedingReturns;
            
            // Dashboard metrics
            ViewBag.TasksDue = tasksDue;
            ViewBag.ServiceHealthIssues = serviceHealthIssues;
            ViewBag.ProjectHealthIssues = projectHealthIssues;

            _logger.LogInformation(
                "MyReports loaded for {Email}: {Projects} projects, {Products} products, {Milestones} milestones, {Issues} issues",
                userEmail, myProjects.Count, myProducts.Count, allActiveMilestones.Count, allActiveIssues.Count);

            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading My Reports");
            TempData["ErrorMessage"] = "An error occurred while loading your reports.";
            return RedirectToAction("Index");
        }
    }

    // GET: DdtReports/ViewReport
    public async Task<IActionResult> ViewReport(DateTime? weekStart, string section = "summary")
    {
        try
        {
            // Get all active projects with their related data
            var projects = await _context.Projects
                .Include(p => p.Successes)
                .Include(p => p.Milestones)
                .Include(p => p.Issues)
                .Include(p => p.RagHistory)
                .Where(p => !p.IsDeleted && p.Status == "Active")
                .OrderBy(p => p.Title)
                .ToListAsync();

            // Get all products
            var allProducts = await _productsApiService.GetProductsAsync();
            
            // Calculate organization-wide metrics
            var allActiveMilestones = projects.SelectMany(p => p.Milestones.Where(m => !m.IsDeleted)).ToList();
            var overdueMilestones = allActiveMilestones.Where(m => m.DueDate < DateTime.Today && m.Status != "complete" && m.Status != "cancelled").ToList();
            var allActiveIssues = projects.SelectMany(p => p.Issues.Where(i => !i.IsDeleted)).ToList();
            var highPriorityIssues = allActiveIssues.Where(i => i.Severity == "high" || i.Severity == "critical").ToList();
            var openIssues = allActiveIssues.Where(i => i.Status != "resolved" && i.Status != "closed").ToList();
            
            // RAG status breakdown
            var redProjects = projects.Where(p => p.RagStatus == "Red").ToList();
            var amberRedProjects = projects.Where(p => p.RagStatus == "Amber-Red").ToList();
            var amberProjects = projects.Where(p => p.RagStatus == "Amber" || p.RagStatus == "Amber-Green").ToList();
            var greenProjects = projects.Where(p => p.RagStatus == "Green").ToList();
            var atRiskProjects = redProjects.Concat(amberRedProjects).ToList();
            
            // Projects needing Path to Green
            var projectsNeedingPathToGreen = atRiskProjects.Where(p => string.IsNullOrWhiteSpace(p.PathToGreen)).ToList();
            
            // Calculate organization health metrics
            var tasksDue = projectsNeedingPathToGreen.Count + overdueMilestones.Count + highPriorityIssues.Count;
            
            // Operational returns health check - OPTIMIZED: Single query instead of N+1
            var now = DateTime.UtcNow;
            var currentYear = now.Month == 1 ? now.Year - 1 : now.Year;
            var currentMonth = now.Month == 1 ? 12 : now.Month - 1;
            
            var productFipsIds = allProducts
                .Where(p => !string.IsNullOrEmpty(p.FipsId))
                .Select(p => p.FipsId)
                .ToList();
            
            var currentMonthReturns = await _context.ProductReturns
                .Where(pr => productFipsIds.Contains(pr.FipsId) && pr.Year == currentYear && pr.Month == currentMonth)
                .ToDictionaryAsync(pr => pr.FipsId);
            
            var serviceHealthIssues = 0;
            foreach (var product in allProducts.Where(p => !string.IsNullOrEmpty(p.FipsId)))
            {
                var productReturn = currentMonthReturns.ContainsKey(product.FipsId) ? currentMonthReturns[product.FipsId] : null;
                var status = _returnStatusService.CalculateReturnStatus(currentYear, currentMonth, productReturn?.SubmittedDate);
                
                if (status == ReturnStatus.Due || status == ReturnStatus.Late)
                {
                    serviceHealthIssues++;
                }
            }
            
            var projectHealthIssues = atRiskProjects.Count;
            
            // Pass all data to ViewBag
            ViewBag.CurrentSection = section;
            ViewBag.AllProjects = projects;
            ViewBag.AllProducts = allProducts;
            ViewBag.AllActiveMilestones = allActiveMilestones;
            ViewBag.OverdueMilestones = overdueMilestones;
            ViewBag.AllActiveIssues = allActiveIssues;
            ViewBag.HighPriorityIssues = highPriorityIssues;
            ViewBag.OpenIssues = openIssues;
            ViewBag.RedProjects = redProjects;
            ViewBag.AmberRedProjects = amberRedProjects;
            ViewBag.AmberProjects = amberProjects;
            ViewBag.GreenProjects = greenProjects;
            ViewBag.AtRiskProjects = atRiskProjects;
            ViewBag.ProjectsNeedingPathToGreen = projectsNeedingPathToGreen;
            
            // Dashboard metrics
            ViewBag.TasksDue = tasksDue;
            ViewBag.ServiceHealthIssues = serviceHealthIssues;
            ViewBag.ProjectHealthIssues = projectHealthIssues;
            ViewBag.TotalProjects = projects.Count;
            ViewBag.TotalProducts = allProducts.Count;
            
            _logger.LogInformation(
                "DDT ViewReport loaded: {Projects} projects, {Products} products, {Milestones} milestones, {Issues} issues",
                projects.Count, allProducts.Count, allActiveMilestones.Count, allActiveIssues.Count);
            
            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading DDT Reports");
            TempData["ErrorMessage"] = "An error occurred while loading the DDT reports.";
            return RedirectToAction("Index");
        }
    }

    // GET: DdtReports/BusinessArea
    public async Task<IActionResult> BusinessArea(string? area, DateTime? weekStart, string section = "summary")
    {
        try
        {
            // Get all business areas for selection
            var businessAreas = await _productsApiService.GetBusinessAreasAsync();
            ViewBag.BusinessAreas = businessAreas;

            // Determine the reporting week range
            DateTime resolvedWeekStart;
            if (weekStart.HasValue)
            {
                resolvedWeekStart = weekStart.Value;
            }
            else
            {
                var today = DateTime.Today;
                var dayOfWeek = (int)today.DayOfWeek;
                var diff = dayOfWeek == 0 ? -6 : 1 - dayOfWeek; // Monday = start of week
                resolvedWeekStart = today.AddDays(diff);
            }

            // Defaults when no area selected
            var projects = new List<Project>();
            var businessAreaProducts = new List<ProductDto>();
            var accessibilityEnrollments = new Dictionary<string, ProductAccessibility>();
            var totalProducts = 0;
            var enrolledProducts = 0;
            var totalAccessibilityIssues = 0;
            PerformanceMetric? perfUx1Metric = null;
            PerformanceMetric? perfAcc3Metric = null;
            var latestReturns = new Dictionary<string, ProductReturn>();

            if (!string.IsNullOrWhiteSpace(area))
            {
                // Get all active projects for the specified business area
                projects = await _context.Projects
                    .Include(p => p.Successes)
                    .Include(p => p.Milestones)
                    .Include(p => p.RagHistory)
                    .Where(p => !p.IsDeleted && p.Status == "Active" && p.BusinessArea == area)
                    .OrderBy(p => p.Title)
                    .ToListAsync();

                // Get all products from CMS
                var allProducts = await _productsApiService.GetProductsAsync(null);

                // Filter products by business area
                businessAreaProducts = allProducts
                    .Where(p => p.CategoryValues?.Any(cv =>
                        cv.CategoryType?.Name?.Equals("Business area", StringComparison.OrdinalIgnoreCase) == true &&
                        cv.Name?.Equals(area, StringComparison.OrdinalIgnoreCase) == true) == true)
                    .ToList();

                // Get accessibility data for products
                accessibilityEnrollments = await _context.ProductAccessibilities
                    .Where(pa => !pa.IsDeleted)
                    .Include(pa => pa.Issues)
                    .ToDictionaryAsync(pa => pa.FipsId);

                // Calculate product metrics
                totalProducts = businessAreaProducts.Count;
                enrolledProducts = businessAreaProducts.Count(p =>
                    !string.IsNullOrEmpty(p.FipsId) && accessibilityEnrollments.ContainsKey(p.FipsId));
                totalAccessibilityIssues = businessAreaProducts
                    .Where(p => !string.IsNullOrEmpty(p.FipsId) && accessibilityEnrollments.ContainsKey(p.FipsId))
                    .Sum(p => accessibilityEnrollments[p.FipsId].Issues?.Count(i => !i.IsDeleted && (i.Status == "open" || i.Status == "in_progress")) ?? 0);

                // Get performance metrics
                perfUx1Metric = await _context.PerformanceMetrics
                    .FirstOrDefaultAsync(pm => pm.Identifier == "perf-ux-1");
                perfAcc3Metric = await _context.PerformanceMetrics
                    .FirstOrDefaultAsync(pm => pm.Identifier == "perf-acc-3");

                // Get latest product returns for products in this business area
                var productFipsIds = businessAreaProducts
                    .Where(p => !string.IsNullOrEmpty(p.FipsId))
                    .Select(p => p.FipsId)
                    .ToList();

                latestReturns = await _context.ProductReturns
                    .Where(pr => productFipsIds.Contains(pr.FipsId) && pr.Status == ReturnStatus.Submitted)
                    .Include(pr => pr.MetricValues)
                    .GroupBy(pr => pr.FipsId)
                    .Select(g => g.OrderByDescending(pr => pr.Year)
                                   .ThenByDescending(pr => pr.Month)
                                   .First())
                    .ToDictionaryAsync(pr => pr.FipsId);
            }

            ViewBag.CurrentSection = section;
            ViewBag.WeekStart = resolvedWeekStart;
            ViewBag.WeekEnd = resolvedWeekStart.AddDays(6);
            ViewBag.BusinessArea = area;
            ViewBag.SelectedBusinessArea = area;
            ViewBag.BusinessAreaProducts = businessAreaProducts;
            ViewBag.TotalProducts = totalProducts;
            ViewBag.EnrolledProducts = enrolledProducts;
            ViewBag.TotalAccessibilityIssues = totalAccessibilityIssues;
            ViewBag.AccessibilityEnrollments = accessibilityEnrollments;
            ViewBag.PerfUx1Metric = perfUx1Metric;
            ViewBag.PerfAcc3Metric = perfAcc3Metric;
            ViewBag.LatestReturns = latestReturns;

            return View(projects);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading DDT Reports for business area: {Area}", area);
            TempData["ErrorMessage"] = $"An error occurred while loading the DDT reports for {area}.";
            return RedirectToAction("Index");
        }
    }

    // GET: DdtReports/FlagshipProjects
    public async Task<IActionResult> FlagshipProjects()
    {
        try
        {
            // Get all active flagship projects with their deliverables
            var flagshipProjects = await _context.Projects
                .Include(p => p.Milestones)
                .Include(p => p.Issues)
                .Include(p => p.DependenciesAsSource)
                .Where(p => !p.IsDeleted && p.Status == "Active" && p.IsFlagship)
                .OrderBy(p => p.Title)
                .ToListAsync();

            // For each flagship project, get its deliverable projects
            foreach (var flagship in flagshipProjects)
            {
                var deliverableIds = flagship.DependenciesAsSource
                    .Where(d => d.TargetEntityType == "Project" && d.DependencyType == "Deliverable")
                    .Select(d => d.TargetEntityId)
                    .ToList();

                var deliverables = await _context.Projects
                    .Include(p => p.Milestones)
                    .Include(p => p.Issues)
                    .Where(p => deliverableIds.Contains(p.Id) && !p.IsDeleted)
                    .ToListAsync();

                // Store deliverables in a ViewBag dictionary keyed by flagship project ID
                ViewBag.Deliverables = ViewBag.Deliverables ?? new Dictionary<int, List<Project>>();
                ((Dictionary<int, List<Project>>)ViewBag.Deliverables)[flagship.Id] = deliverables;
            }

            return View(flagshipProjects);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading Flagship Projects for DDT Reports");
            TempData["ErrorMessage"] = "An error occurred while loading the flagship projects report.";
            return RedirectToAction("Index");
        }
    }
    
    // GET: DdtReports/AccessibilityReport
    public async Task<IActionResult> AccessibilityReport()
    {
        try
        {
            // Get all enrolled products
            var enrolledProducts = await _context.ProductAccessibilities
                .Where(pa => !pa.IsDeleted && pa.IsActive)
                .Include(pa => pa.Issues)
                    .ThenInclude(i => i.WcagCriteriaLinks)
                    .ThenInclude(w => w.WcagCriterion)
                .ToListAsync();
            
            // Get total products from CMS
            var allProducts = await _productsApiService.GetProductsAsync();
            
            // Calculate metrics
            var totalProducts = allProducts.Count;
            var enrolledProductsCount = enrolledProducts.Count;
            
            // Count issues by WCAG level (using the new WcagCriteriaLinks)
            var issuesByLevel = new Dictionary<string, int>
            {
                { "A", 0 },
                { "AA", 0 },
                { "AAA", 0 },
                { "Best Practice", 0 }
            };
            
            var allIssues = enrolledProducts.SelectMany(pa => pa.Issues)
                .Where(i => !i.IsDeleted && (i.Status == "open" || i.Status == "in_progress"))
                .ToList();
            
            foreach (var issue in allIssues)
            {
                if (issue.IssueType == "WCAG" && issue.WcagCriteriaLinks != null && issue.WcagCriteriaLinks.Any())
                {
                    // Use the most stringent level from the linked criteria
                    var levels = issue.WcagCriteriaLinks.Select(w => w.WcagCriterion.Level).Distinct().ToList();
                    if (levels.Contains("AAA"))
                        issuesByLevel["AAA"]++;
                    else if (levels.Contains("AA"))
                        issuesByLevel["AA"]++;
                    else if (levels.Contains("A"))
                        issuesByLevel["A"]++;
                }
                else if (issue.IssueType == "Best Practice")
                {
                    issuesByLevel["Best Practice"]++;
                }
            }
            
            // Top 10 products with most issues (just the products, sorted by issue count)
            var topProductsWithIssues = enrolledProducts
                .Where(pa => pa.Issues.Any(i => !i.IsDeleted && (i.Status == "open" || i.Status == "in_progress")))
                .OrderByDescending(pa => pa.Issues.Count(i => !i.IsDeleted && (i.Status == "open" || i.Status == "in_progress")))
                .Take(10)
                .ToList();
            
            // Issues open but where planned resolution date is in the past
            var overdueIssues = allIssues
                .Where(i => i.IsResolving && 
                           i.PlannedResolutionDate.HasValue && 
                           i.PlannedResolutionDate.Value < DateTime.Today &&
                           (i.Status == "open" || i.Status == "in_progress"))
                .ToList();
            
            // Count issues not intended to be closed
            var wontFixIssues = allIssues
                .Where(i => !i.IsResolving && (i.Status == "open" || i.Status == "in_progress"))
                .Count();
            
            // Pass data to ViewBag
            ViewBag.TotalProducts = totalProducts;
            ViewBag.EnrolledProductsCount = enrolledProductsCount;
            ViewBag.IssuesByLevel = issuesByLevel;
            ViewBag.TopProductsWithIssues = topProductsWithIssues;
            ViewBag.OverdueIssues = overdueIssues;
            ViewBag.WontFixIssues = wontFixIssues;
            ViewBag.TotalOpenIssues = allIssues.Count;
            
            _logger.LogInformation(
                "Accessibility Report loaded: {Enrolled}/{Total} products enrolled, {Issues} open issues",
                enrolledProductsCount, totalProducts, allIssues.Count);
            
            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading Accessibility Report");
            TempData["ErrorMessage"] = "An error occurred while loading the accessibility report.";
            return RedirectToAction("Index");
        }
    }

    // GET: DdtReports/FipsCompletion
    public async Task<IActionResult> FipsCompletion()
    {
        try
        {
            var products = await _productsApiService.GetProductsAsync(null);
            var completionItems = products
                .OrderBy(p => p.Title)
                .Where(p => !string.IsNullOrEmpty(p.FipsId))
                .Select(CreateProductCompletionItem)
                .ToList();

            var averageCompletion = completionItems.Any()
                ? completionItems.Average(p => p.CompletionPercentage)
                : 0;

            var businessAreaCompletions = completionItems
                .GroupBy(p => string.IsNullOrWhiteSpace(p.BusinessArea) ? "Unassigned" : p.BusinessArea)
                .Select(g => new BusinessAreaCompletion
                {
                    BusinessArea = g.Key,
                    ProductCount = g.Count(),
                    AverageCompletionPercentage = g.Average(p => p.CompletionPercentage)
                })
                .OrderByDescending(ba => ba.AverageCompletionPercentage)
                .ToList();

            var zeroCompletionCount = completionItems.Count(p => Math.Abs(p.CompletionPercentage) < 0.0001);
            var fullCompletionCount = completionItems.Count(p => Math.Abs(p.CompletionPercentage - 100) < 0.0001);

            var completedPhaseCount = completionItems.Count(p => p.HasPhase);
            var completedBusinessAreaCount = completionItems.Count(p => p.HasBusinessArea);
            var completedProductUrlCount = completionItems.Count(p => p.HasProductUrl);

            var phaseCategoryValues = await _productsApiService.GetPhaseCategoryValuesAsync();
            var businessAreaCategoryValues = await _productsApiService.GetBusinessAreaCategoryValuesAsync();
            var userGroupCategoryValues = await _productsApiService.GetUserGroupCategoryValuesAsync();

            var viewModel = new FipsCompletionViewModel
            {
                Products = completionItems,
                AverageCompletionPercentage = averageCompletion,
                BusinessAreaCompletions = businessAreaCompletions,
                ZeroCompletionCount = zeroCompletionCount,
                FullCompletionCount = fullCompletionCount,
                CompletedPhaseCount = completedPhaseCount,
                CompletedBusinessAreaCount = completedBusinessAreaCount,
                CompletedUrlCount = completedProductUrlCount
            };
            
            ViewBag.PhaseCategoryValues = phaseCategoryValues;
            ViewBag.BusinessAreaCategoryValues = businessAreaCategoryValues;
            ViewBag.UserGroupCategoryValues = userGroupCategoryValues;
            
            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating FIPS completion report");
            TempData["ErrorMessage"] = "An error occurred while generating the report. Please try again.";
            return View(new FipsCompletionViewModel());
        }
    }

    [HttpGet]
    public async Task<IActionResult> ExportFipsCompletion(bool includeAll = false)
    {
        try
        {
            var products = includeAll
                ? await _productsApiService.GetAllProductsAsync(null)
                : await _productsApiService.GetProductsAsync(null);

            var completionItems = products
                .OrderBy(p => p.Title)
                .Where(p => includeAll || !string.IsNullOrEmpty(p.FipsId))
                .Select(CreateProductCompletionItem)
                .ToList();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("FIPS completion");

            var headers = new[]
            {
                "Product title",
                "FIPS ID",
                "State",
                "Phase",
                "Has phase",
                "Business area",
                "Has business area",
                "Contacts count",
                "Contacts",
                "Senior responsible officer",
                "Information asset owner",
                "Delivery manager",
                "Product URL",
                "Has product URL",
                "User groups",
                "User groups count",
                "Completion %"
            };

            for (var column = 0; column < headers.Length; column++)
            {
                var cell = worksheet.Cell(1, column + 1);
                cell.Value = headers[column];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#f1f3f5");
                cell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            }

            var currentRow = 2;

            foreach (var item in completionItems)
            {
                worksheet.Cell(currentRow, 1).Value = item.ProductTitle;
                worksheet.Cell(currentRow, 2).Value = item.FipsId;
                worksheet.Cell(currentRow, 3).Value = item.State;
                worksheet.Cell(currentRow, 4).Value = item.PhaseName ?? string.Empty;
                worksheet.Cell(currentRow, 5).Value = item.HasPhase ? "Yes" : "No";
                worksheet.Cell(currentRow, 6).Value = item.BusinessArea;
                worksheet.Cell(currentRow, 7).Value = item.HasBusinessArea ? "Yes" : "No";
                worksheet.Cell(currentRow, 8).Value = item.ContactsCount;
                worksheet.Cell(currentRow, 9).Value = item.ContactDetails.Any()
                    ? string.Join(Environment.NewLine, item.ContactDetails)
                    : string.Empty;
                worksheet.Cell(currentRow, 10).Value = item.SeniorResponsibleOfficer ?? string.Empty;
                worksheet.Cell(currentRow, 11).Value = item.InformationAssetOwner ?? string.Empty;
                worksheet.Cell(currentRow, 12).Value = item.DeliveryManager ?? string.Empty;
                worksheet.Cell(currentRow, 13).Value = item.ProductUrl ?? string.Empty;
                worksheet.Cell(currentRow, 14).Value = item.HasProductUrl ? "Yes" : "No";
                worksheet.Cell(currentRow, 15).Value = item.UserGroupNames.Any()
                    ? string.Join(", ", item.UserGroupNames)
                    : string.Empty;
                worksheet.Cell(currentRow, 16).Value = item.UserGroupsCount;
                worksheet.Cell(currentRow, 17).Value = item.CompletionPercentage / 100.0;
                worksheet.Cell(currentRow, 17).Style.NumberFormat.Format = "0.0%";

                currentRow++;
            }

            worksheet.Columns().AdjustToContents();
            worksheet.Column(9).Style.Alignment.WrapText = true;
            worksheet.SheetView.FreezeRows(1);

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var fileName = $"fips-completion{(includeAll ? "-all" : string.Empty)}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.xlsx";
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting FIPS completion report");
            TempData["ErrorMessage"] = "An error occurred while exporting the report. Please try again.";
            return RedirectToAction("FipsCompletion");
        }
    }

    public async Task<IActionResult> ExportFipsCompletionFiltered(
        string? searchTerm = null,
        string? completionFilter = null,
        string? statusFilter = null,
        string? businessAreaFilter = null,
        string? sroFilter = null,
        string? iaoFilter = null,
        string? deliveryManagerFilter = null)
    {
        try
        {
            var products = await _productsApiService.GetProductsAsync(null);

            var completionItems = products
                .OrderBy(p => p.Title)
                .Where(p => !string.IsNullOrEmpty(p.FipsId))
                .Select(CreateProductCompletionItem)
                .ToList();

            // Apply filters (same logic as JavaScript)
            var filteredItems = completionItems.Where(item =>
            {
                // Search filter
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    var searchLower = searchTerm.ToLowerInvariant();
                    if (!item.ProductTitle.ToLowerInvariant().Contains(searchLower) &&
                        !item.FipsId.ToLowerInvariant().Contains(searchLower))
                    {
                        return false;
                    }
                }

                // Completion filter
                if (!string.IsNullOrWhiteSpace(completionFilter))
                {
                    var completion = item.CompletionPercentage;
                    if (completionFilter == "100")
                    {
                        if (Math.Abs(completion - 100) > 0.0001) return false;
                    }
                    else if (completionFilter == "80-99")
                    {
                        if (completion < 80 || completion >= 100) return false;
                    }
                    else if (completionFilter == "60-79")
                    {
                        if (completion < 60 || completion >= 80) return false;
                    }
                    else if (completionFilter == "40-59")
                    {
                        if (completion < 40 || completion >= 60) return false;
                    }
                    else if (completionFilter == "0-39")
                    {
                        if (completion < 0 || completion >= 40) return false;
                    }
                }

                // Status filter
                if (!string.IsNullOrWhiteSpace(statusFilter))
                {
                    var completion = item.CompletionPercentage;
                    if (statusFilter == "complete")
                    {
                        if (Math.Abs(completion - 100) > 0.0001) return false;
                    }
                    else if (statusFilter == "incomplete")
                    {
                        if (completion >= 100) return false;
                    }
                }

                // Business area filter
                if (!string.IsNullOrWhiteSpace(businessAreaFilter))
                {
                    if (!item.BusinessArea.Equals(businessAreaFilter, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }

                // SRO filter (exact case-insensitive match, matching JavaScript indexOf behavior)
                if (!string.IsNullOrWhiteSpace(sroFilter))
                {
                    var sroLower = sroFilter.ToLowerInvariant();
                    var matchesSro = item.SeniorResponsibleOfficerContacts?.Any(contact =>
                        contact.ToLowerInvariant() == sroLower) ?? false;
                    if (!matchesSro) return false;
                }

                // IAO filter (exact case-insensitive match, matching JavaScript indexOf behavior)
                if (!string.IsNullOrWhiteSpace(iaoFilter))
                {
                    var iaoLower = iaoFilter.ToLowerInvariant();
                    var matchesIao = item.InformationAssetOwnerContacts?.Any(contact =>
                        contact.ToLowerInvariant() == iaoLower) ?? false;
                    if (!matchesIao) return false;
                }

                // Delivery manager filter (exact case-insensitive match, matching JavaScript indexOf behavior)
                if (!string.IsNullOrWhiteSpace(deliveryManagerFilter))
                {
                    var deliveryLower = deliveryManagerFilter.ToLowerInvariant();
                    var matchesDelivery = item.DeliveryManagerContacts?.Any(contact =>
                        contact.ToLowerInvariant() == deliveryLower) ?? false;
                    if (!matchesDelivery) return false;
                }

                return true;
            }).ToList();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("FIPS completion");

            var headers = new[]
            {
                "Product title",
                "FIPS ID",
                "State",
                "Phase",
                "Has phase",
                "Business area",
                "Has business area",
                "Contacts count",
                "Contacts",
                "Senior responsible officer",
                "Information asset owner",
                "Delivery manager",
                "Product URL",
                "Has product URL",
                "User groups",
                "User groups count",
                "Completion %"
            };

            for (var column = 0; column < headers.Length; column++)
            {
                var cell = worksheet.Cell(1, column + 1);
                cell.Value = headers[column];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#f1f3f5");
                cell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            }

            var currentRow = 2;

            foreach (var item in filteredItems)
            {
                worksheet.Cell(currentRow, 1).Value = item.ProductTitle;
                worksheet.Cell(currentRow, 2).Value = item.FipsId;
                worksheet.Cell(currentRow, 3).Value = item.State;
                worksheet.Cell(currentRow, 4).Value = item.PhaseName ?? string.Empty;
                worksheet.Cell(currentRow, 5).Value = item.HasPhase ? "Yes" : "No";
                worksheet.Cell(currentRow, 6).Value = item.BusinessArea;
                worksheet.Cell(currentRow, 7).Value = item.HasBusinessArea ? "Yes" : "No";
                worksheet.Cell(currentRow, 8).Value = item.ContactsCount;
                worksheet.Cell(currentRow, 9).Value = item.ContactDetails.Any()
                    ? string.Join(Environment.NewLine, item.ContactDetails)
                    : string.Empty;
                worksheet.Cell(currentRow, 10).Value = item.SeniorResponsibleOfficer ?? string.Empty;
                worksheet.Cell(currentRow, 11).Value = item.InformationAssetOwner ?? string.Empty;
                worksheet.Cell(currentRow, 12).Value = item.DeliveryManager ?? string.Empty;
                worksheet.Cell(currentRow, 13).Value = item.ProductUrl ?? string.Empty;
                worksheet.Cell(currentRow, 14).Value = item.HasProductUrl ? "Yes" : "No";
                worksheet.Cell(currentRow, 15).Value = item.UserGroupNames.Any()
                    ? string.Join(", ", item.UserGroupNames)
                    : string.Empty;
                worksheet.Cell(currentRow, 16).Value = item.UserGroupsCount;
                worksheet.Cell(currentRow, 17).Value = item.CompletionPercentage / 100.0;
                worksheet.Cell(currentRow, 17).Style.NumberFormat.Format = "0.0%";

                currentRow++;
            }

            worksheet.Columns().AdjustToContents();
            worksheet.Column(9).Style.Alignment.WrapText = true;
            worksheet.SheetView.FreezeRows(1);

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var fileName = $"fips-completion-filtered-{DateTime.UtcNow:yyyyMMdd-HHmmss}.xlsx";
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting filtered FIPS completion report");
            TempData["ErrorMessage"] = "An error occurred while exporting the filtered report. Please try again.";
            return RedirectToAction("FipsCompletion");
        }
    }

    // GET: DdtReports/MultipleBusinessAreas
    public async Task<IActionResult> MultipleBusinessAreas()
    {
        try
        {
            var products = await _productsApiService.GetAllProductsAsync(null);
            var productsWithMultipleBusinessAreas = new List<ProductWithMultipleBusinessAreas>();
            
            foreach (var product in products.OrderBy(p => p.Title))
            {
                // Get all business area category values for this product
                var businessAreas = product.CategoryValues?
                    .Where(cv => cv.CategoryType?.Name?.Equals("Business area", StringComparison.OrdinalIgnoreCase) == true)
                    .Select(cv => cv.Name)
                    .ToList() ?? new List<string>();
                
                // Only include products with multiple business areas
                if (businessAreas.Count > 1)
                {
                    productsWithMultipleBusinessAreas.Add(new ProductWithMultipleBusinessAreas
                    {
                        FipsId = product.FipsId ?? string.Empty,
                        ProductTitle = product.Title,
                        State = product.State,
                        BusinessAreas = businessAreas,
                        BusinessAreaCount = businessAreas.Count,
                        ContactsCount = product.ProductContacts?.Count ?? 0,
                        ProductUrl = product.ProductUrl
                    });
                }
            }
            
            var viewModel = new MultipleBusinessAreasViewModel
            {
                Products = productsWithMultipleBusinessAreas,
                TotalProductsWithMultipleBusinessAreas = productsWithMultipleBusinessAreas.Count,
                TotalProducts = products.Count
            };
            
            // Get business area category values for the modal dropdown
            var businessAreaCategoryValues = await _productsApiService.GetBusinessAreaCategoryValuesAsync();
            ViewBag.BusinessAreaCategoryValues = businessAreaCategoryValues;
            
            _logger.LogInformation("Found {Count} products with multiple business areas out of {Total} total products", 
                productsWithMultipleBusinessAreas.Count, products.Count);
            
            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating Multiple Business Areas report");
            TempData["ErrorMessage"] = "An error occurred while generating the report. Please try again.";
            return View(new MultipleBusinessAreasViewModel());
        }
    }

    // GET: DdtReports/AllCmsData
    public async Task<IActionResult> AllCmsData()
    {
        try
        {
            var products = await _productsApiService.GetAllProductsAsync(null);
            var completionItems = products
                .OrderBy(p => p.Title)
                .Select(CreateProductCompletionItem)
                .ToList();

            var averageCompletion = completionItems.Any()
                ? completionItems.Average(p => p.CompletionPercentage)
                : 0;

            var businessAreaCompletions = completionItems
                .GroupBy(p => string.IsNullOrWhiteSpace(p.BusinessArea) ? "Unassigned" : p.BusinessArea)
                .Select(g => new BusinessAreaCompletion
                {
                    BusinessArea = g.Key,
                    ProductCount = g.Count(),
                    AverageCompletionPercentage = g.Average(p => p.CompletionPercentage)
                })
                .OrderByDescending(ba => ba.AverageCompletionPercentage)
                .ToList();

            var zeroCompletionCount = completionItems.Count(p => Math.Abs(p.CompletionPercentage) < 0.0001);
            var fullCompletionCount = completionItems.Count(p => Math.Abs(p.CompletionPercentage - 100) < 0.0001);

            var completedPhaseCount = completionItems.Count(p => p.HasPhase);
            var completedBusinessAreaCount = completionItems.Count(p => p.HasBusinessArea);
            var completedProductUrlCount = completionItems.Count(p => p.HasProductUrl);

            var phaseCategoryValues = await _productsApiService.GetPhaseCategoryValuesAsync();
            var businessAreaCategoryValues = await _productsApiService.GetBusinessAreaCategoryValuesAsync();
            var userGroupCategoryValues = await _productsApiService.GetUserGroupCategoryValuesAsync();

            var viewModel = new FipsCompletionViewModel
            {
                Products = completionItems,
                AverageCompletionPercentage = averageCompletion,
                BusinessAreaCompletions = businessAreaCompletions,
                ZeroCompletionCount = zeroCompletionCount,
                FullCompletionCount = fullCompletionCount,
                CompletedPhaseCount = completedPhaseCount,
                CompletedBusinessAreaCount = completedBusinessAreaCount,
                CompletedUrlCount = completedProductUrlCount
            };
            
            ViewBag.PhaseCategoryValues = phaseCategoryValues;
            ViewBag.BusinessAreaCategoryValues = businessAreaCategoryValues;
            ViewBag.UserGroupCategoryValues = userGroupCategoryValues;
            
            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating All CMS Data report");
            TempData["ErrorMessage"] = "An error occurred while generating the report. Please try again.";
            return View(new FipsCompletionViewModel());
        }
    }

    // POST: DdtReports/UpdateProductPhase
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProductPhase(string fipsId, int phaseCategoryValueId)
    {
        try
        {
            // Get product title before update
            var product = await _productsApiService.GetProductByFipsIdAsync(fipsId);
            var productTitle = product?.Title ?? fipsId;
            
            // Get phase name
            var phases = await _productsApiService.GetPhaseCategoryValuesAsync();
            var phaseName = phases.FirstOrDefault(p => p.Id == phaseCategoryValueId)?.Name ?? "Phase";
            
            var success = await _productsApiService.UpdateProductPhaseAsync(fipsId, phaseCategoryValueId);
            
            if (success)
            {
                var updatedProduct = await _productsApiService.GetProductByFipsIdAsync(fipsId);
                var completionItem = updatedProduct != null ? CreateProductCompletionItem(updatedProduct) : null;
                var successMessage = $"<strong>{productTitle}</strong> - Phase updated to <strong>{phaseName}</strong>.";

                if (IsAjaxRequest())
                {
                    return Json(new { success = true, message = successMessage, product = completionItem });
                }

                TempData["SuccessMessage"] = successMessage;
            }
            else
            {
                const string errorMessage = "Failed to update product Phase. Please try again.";
                if (IsAjaxRequest())
                {
                    return Json(new { success = false, message = errorMessage });
                }

                TempData["ErrorMessage"] = errorMessage;
            }
            
            return RedirectToAction("FipsCompletion");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product Phase for {FipsId}", fipsId);
            const string errorMessage = "An error occurred while updating the product Phase.";
            if (IsAjaxRequest())
            {
                return Json(new { success = false, message = errorMessage });
            }

            TempData["ErrorMessage"] = errorMessage;
            return RedirectToAction("FipsCompletion");
        }
    }

    // POST: DdtReports/UpdateProductBusinessArea
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProductBusinessArea(string fipsId, int businessAreaCategoryValueId, string? returnUrl = null)
    {
        try
        {
            // Get product before update to check for multiple business areas
            var product = await _productsApiService.GetProductByFipsIdAsync(fipsId);
            var productTitle = product?.Title ?? fipsId;
            
            // Count existing business areas
            var existingBusinessAreas = product?.CategoryValues?
                .Where(cv => cv.CategoryType?.Name?.Equals("Business area", StringComparison.OrdinalIgnoreCase) == true)
                .Select(cv => cv.Name)
                .ToList() ?? new List<string>();
            
            // Get new business area name
            var businessAreas = await _productsApiService.GetBusinessAreaCategoryValuesAsync();
            var businessAreaName = businessAreas.FirstOrDefault(ba => ba.Id == businessAreaCategoryValueId)?.Name ?? "Business Area";
            
            var success = await _productsApiService.UpdateProductBusinessAreaAsync(fipsId, businessAreaCategoryValueId);
            
            if (success)
            {
                var updatedProduct = await _productsApiService.GetProductByFipsIdAsync(fipsId);
                var completionItem = updatedProduct != null ? CreateProductCompletionItem(updatedProduct) : null;
                string successMessage;

                // Provide different messages based on whether multiple business areas were removed
                if (existingBusinessAreas.Count > 1)
                {
                    successMessage = $"<strong>{productTitle}</strong> - Removed {existingBusinessAreas.Count} existing business area(s) ({string.Join(", ", existingBusinessAreas)}) and assigned <strong>{businessAreaName}</strong>.";
                }
                else if (existingBusinessAreas.Count == 1)
                {
                    successMessage = $"<strong>{productTitle}</strong> - Business area updated from <strong>{existingBusinessAreas[0]}</strong> to <strong>{businessAreaName}</strong>.";
                }
                else
                {
                    successMessage = $"<strong>{productTitle}</strong> - Business area assigned to <strong>{businessAreaName}</strong>.";
                }

                if (IsAjaxRequest())
                {
                    return Json(new { success = true, message = successMessage, product = completionItem });
                }

                TempData["SuccessMessage"] = successMessage;
            }
            else
            {
                const string errorMessage = "Failed to update product Business Area. Please try again.";
                if (IsAjaxRequest())
                {
                    return Json(new { success = false, message = errorMessage });
                }

                TempData["ErrorMessage"] = errorMessage;
            }
            
            // Redirect back to the referring page or default to FipsCompletion
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            
            return RedirectToAction("FipsCompletion");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product Business Area for {FipsId}", fipsId);
            const string errorMessage = "An error occurred while updating the product Business Area.";
            if (IsAjaxRequest())
            {
                return Json(new { success = false, message = errorMessage });
            }

            TempData["ErrorMessage"] = errorMessage;
            return RedirectToAction("FipsCompletion");
        }
    }

    // POST: DdtReports/UpdateProductUrl
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProductUrl(string fipsId, string productUrl)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(productUrl))
            {
                const string message = "Product URL cannot be empty.";
                if (IsAjaxRequest())
                {
                    return Json(new { success = false, message });
                }

                TempData["ErrorMessage"] = message;
                return RedirectToAction("FipsCompletion");
            }

            // Validate URL format
            if (!Uri.TryCreate(productUrl, UriKind.Absolute, out var uri) || 
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                const string message = "Please provide a valid HTTP or HTTPS URL.";
                if (IsAjaxRequest())
                {
                    return Json(new { success = false, message });
                }

                TempData["ErrorMessage"] = message;
                return RedirectToAction("FipsCompletion");
            }

            // Get product title before update
            var product = await _productsApiService.GetProductByFipsIdAsync(fipsId);
            var productTitle = product?.Title ?? fipsId;

            var success = await _productsApiService.UpdateProductUrlAsync(fipsId, productUrl);
            
            if (success)
            {
                var updatedProduct = await _productsApiService.GetProductByFipsIdAsync(fipsId);
                var completionItem = updatedProduct != null ? CreateProductCompletionItem(updatedProduct) : null;
                var successMessage = $"<strong>{productTitle}</strong> - Product URL updated to <strong>{productUrl}</strong>.";

                if (IsAjaxRequest())
                {
                    return Json(new { success = true, message = successMessage, product = completionItem });
                }

                TempData["SuccessMessage"] = successMessage;
            }
            else
            {
                const string errorMessage = "Failed to update product URL. Please try again.";
                if (IsAjaxRequest())
                {
                    return Json(new { success = false, message = errorMessage });
                }

                TempData["ErrorMessage"] = errorMessage;
            }
            
            return RedirectToAction("FipsCompletion");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product URL for {FipsId}", fipsId);
            const string errorMessage = "An error occurred while updating the product URL.";
            if (IsAjaxRequest())
            {
                return Json(new { success = false, message = errorMessage });
            }

            TempData["ErrorMessage"] = errorMessage;
            return RedirectToAction("FipsCompletion");
        }
    }

    // POST: DdtReports/UpdateProductUserGroups
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProductUserGroups(string fipsId, List<int> userGroupCategoryValueIds)
    {
        try
        {
            var product = await _productsApiService.GetProductByFipsIdAsync(fipsId);
            var productTitle = product?.Title ?? fipsId;

            var success = await _productsApiService.UpdateProductUserGroupsAsync(fipsId, userGroupCategoryValueIds);

            if (success)
            {
                var updatedProduct = await _productsApiService.GetProductByFipsIdAsync(fipsId);
                var completionItem = updatedProduct != null ? CreateProductCompletionItem(updatedProduct) : null;
                var userGroupNames = completionItem?.UserGroupNames ?? new List<string>();
                var successMessage = userGroupNames.Any()
                    ? $"<strong>{productTitle}</strong> - User groups updated ({string.Join(", ", userGroupNames)})"
                    : $"<strong>{productTitle}</strong> - User groups cleared.";

                if (IsAjaxRequest())
                {
                    return Json(new { success = true, message = successMessage, product = completionItem });
                }

                TempData["SuccessMessage"] = successMessage;
            }
            else
            {
                const string errorMessage = "Failed to update product user groups. Please try again.";
                if (IsAjaxRequest())
                {
                    return Json(new { success = false, message = errorMessage });
                }

                TempData["ErrorMessage"] = errorMessage;
            }

            return RedirectToAction("FipsCompletion");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product user groups for {FipsId}", fipsId);
            const string errorMessage = "An error occurred while updating the product user groups.";
            if (IsAjaxRequest())
            {
                return Json(new { success = false, message = errorMessage });
            }

            TempData["ErrorMessage"] = errorMessage;
            return RedirectToAction("FipsCompletion");
        }
    }

    // POST: DdtReports/UpdateProductState
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProductState(string fipsId, string state)
    {
        try
        {
            // Get product title and current state before update
            var product = await _productsApiService.GetProductByFipsIdAsync(fipsId);
            var productTitle = product?.Title ?? fipsId;
            var currentState = product?.State ?? "Unknown";

            var success = await _productsApiService.UpdateProductStateAsync(fipsId, state);
            
            if (success)
            {
                TempData["SuccessMessage"] = $"<strong>{productTitle}</strong> - State updated from <strong>{currentState}</strong> to <strong>{state}</strong>.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to update product state. Please try again.";
            }
            
            return RedirectToAction("FipsCompletion");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product state for {FipsId}", fipsId);
            TempData["ErrorMessage"] = "An error occurred while updating the product state.";
            return RedirectToAction("FipsCompletion");
        }
    }

    // POST: DdtReports/UpdateProductServiceOwner
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProductServiceOwner(string fipsId, string? entraUserObjectId, string? entraUserEmail, string? entraUserName)
    {
        try
        {
            // Get product title before update
            var product = await _productsApiService.GetProductByFipsIdAsync(fipsId);
            var productTitle = product?.Title ?? fipsId;

            if (string.IsNullOrWhiteSpace(entraUserObjectId) || string.IsNullOrWhiteSpace(entraUserEmail))
            {
                const string errorMessage = "Please select a user from the search results.";
                if (IsAjaxRequest())
                {
                    return Json(new { success = false, message = errorMessage });
                }
                TempData["ErrorMessage"] = errorMessage;
                return RedirectToAction("FipsCompletion");
            }

            // Fetch full user details from Microsoft Graph to get firstName and lastName
            string? firstName = null;
            string? lastName = null;
            string? displayName = entraUserName;
            string? actualObjectId = entraUserObjectId;
            
            try
            {
                if (Guid.TryParse(entraUserObjectId, out var objectIdGuid))
                {
                    var directoryUser = await _userDirectoryService.EnsureUserAsync(objectIdGuid);
                    firstName = directoryUser.FirstName;
                    lastName = directoryUser.LastName;
                    actualObjectId = directoryUser.AzureObjectId;
                    // Use the directory name if display name wasn't provided
                    if (string.IsNullOrWhiteSpace(displayName) && !string.IsNullOrWhiteSpace(directoryUser.Name))
                    {
                        displayName = directoryUser.Name;
                    }
                    
                    // Log the full user object for debugging
                    _logger.LogInformation("=== FULL ENTRA USER DATA FROM GRAPH ===");
                    _logger.LogInformation("Id (Database): {Id}", directoryUser.Id);
                    _logger.LogInformation("AzureObjectId: {AzureObjectId}", directoryUser.AzureObjectId);
                    _logger.LogInformation("Name: {Name}", directoryUser.Name);
                    _logger.LogInformation("Email: {Email}", directoryUser.Email);
                    _logger.LogInformation("FirstName: {FirstName}", directoryUser.FirstName);
                    _logger.LogInformation("LastName: {LastName}", directoryUser.LastName);
                    _logger.LogInformation("UserPrincipalName: {UserPrincipalName}", directoryUser.UserPrincipalName);
                    _logger.LogInformation("JobTitle: {JobTitle}", directoryUser.JobTitle);
                    _logger.LogInformation("Role: {Role}", directoryUser.Role);
                    _logger.LogInformation("=======================================");
                    
                    _logger.LogInformation("Fetched user details from Graph: ObjectId={ObjectId}, Email={Email}, FirstName={FirstName}, LastName={LastName}, DisplayName={DisplayName}",
                        actualObjectId, entraUserEmail, firstName, lastName, displayName);
                }
                else
                {
                    _logger.LogWarning("Invalid ObjectId format: {ObjectId}. This might be a database ID instead of Azure Object ID.", entraUserObjectId);
                    _logger.LogWarning("Received entraUserObjectId={EntraUserObjectId}, entraUserEmail={EntraUserEmail}, entraUserName={EntraUserName}",
                        entraUserObjectId, entraUserEmail, entraUserName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch user details from Graph for {ObjectId}, continuing with provided data", entraUserObjectId);
            }

            // Get or create the Entra user in CMS using the email and object ID from Entra ID
            // Use actualObjectId if we successfully fetched it from Graph, otherwise fall back to entraUserObjectId
            var entraIdToUse = !string.IsNullOrWhiteSpace(actualObjectId) ? actualObjectId : entraUserObjectId;
            
            _logger.LogInformation("Calling GetOrCreateEntraUserAsync with: Email={Email}, EntraId={EntraId}, DisplayName={DisplayName}, FirstName={FirstName}, LastName={LastName}",
                entraUserEmail, entraIdToUse, displayName, firstName, lastName);
            
            var entraUser = await _productsApiService.GetOrCreateEntraUserAsync(
                entraUserEmail,
                entraIdToUse, // Use the Entra Object ID as entraId
                displayName, // Display name
                firstName, // First name from Graph
                lastName  // Last name from Graph
            );

            if (entraUser == null)
            {
                const string errorMessage = "Failed to get or create Entra User. Please try again.";
                if (IsAjaxRequest())
                {
                    return Json(new { success = false, message = errorMessage });
                }
                TempData["ErrorMessage"] = errorMessage;
                return RedirectToAction("FipsCompletion");
            }

            var success = await _productsApiService.UpdateProductServiceOwnerAsync(fipsId, entraUser.Id);
            
            if (success)
            {
                var updatedProduct = await _productsApiService.GetProductByFipsIdAsync(fipsId);
                var completionItem = updatedProduct != null ? CreateProductCompletionItem(updatedProduct) : null;
                var userDisplayName = entraUser.DisplayName ?? entraUser.EmailAddress ?? "Unknown";
                var successMessage = $"<strong>{productTitle}</strong> - Service owner set to <strong>{userDisplayName}</strong>.";

                if (IsAjaxRequest())
                {
                    return Json(new { success = true, message = successMessage, product = completionItem });
                }

                TempData["SuccessMessage"] = successMessage;
            }
            else
            {
                const string errorMessage = "Failed to update product service owner. Please try again.";
                if (IsAjaxRequest())
                {
                    return Json(new { success = false, message = errorMessage });
                }

                TempData["ErrorMessage"] = errorMessage;
            }
            
            return RedirectToAction("FipsCompletion");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product service owner for {FipsId}", fipsId);
            const string errorMessage = "An error occurred while updating the product service owner.";
            if (IsAjaxRequest())
            {
                return Json(new { success = false, message = errorMessage });
            }

            TempData["ErrorMessage"] = errorMessage;
            return RedirectToAction("FipsCompletion");
        }
    }

    // POST: DdtReports/UpdateProductRole
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProductRole(string fipsId, string roleFieldName, string roleDisplayName, string? entraUserObjectId, string? entraUserEmail, string? entraUserName)
    {
        try
        {
            // Get product title before update
            var product = await _productsApiService.GetProductByFipsIdAsync(fipsId);
            var productTitle = product?.Title ?? fipsId;

            if (string.IsNullOrWhiteSpace(entraUserObjectId) || string.IsNullOrWhiteSpace(entraUserEmail))
            {
                var errorMessage = $"Please select a user from the search results.";
                if (IsAjaxRequest())
                {
                    return Json(new { success = false, message = errorMessage });
                }
                TempData["ErrorMessage"] = errorMessage;
                return RedirectToAction("FipsCompletion");
            }

            // Validate roleFieldName to prevent injection
            var validRoles = new[] { "product_manager", "delivery_manager", "Information_asset_owner", "reporting_user", "senior_responsible_officer", "service_designs", "user_researchers" };
            if (!validRoles.Contains(roleFieldName))
            {
                var errorMessage = "Invalid role specified.";
                if (IsAjaxRequest())
                {
                    return Json(new { success = false, message = errorMessage });
                }
                TempData["ErrorMessage"] = errorMessage;
                return RedirectToAction("FipsCompletion");
            }

            // Fetch full user details from Microsoft Graph to get firstName and lastName
            string? firstName = null;
            string? lastName = null;
            string? displayName = entraUserName;
            string? actualObjectId = entraUserObjectId;
            
            try
            {
                if (Guid.TryParse(entraUserObjectId, out var objectIdGuid))
                {
                    var directoryUser = await _userDirectoryService.EnsureUserAsync(objectIdGuid);
                    firstName = directoryUser.FirstName;
                    lastName = directoryUser.LastName;
                    actualObjectId = directoryUser.AzureObjectId;
                    if (string.IsNullOrWhiteSpace(displayName) && !string.IsNullOrWhiteSpace(directoryUser.Name))
                    {
                        displayName = directoryUser.Name;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch user details from Graph for {ObjectId}, continuing with provided data", entraUserObjectId);
            }

            // Get or create the Entra user in CMS (all roles use entra-user entities)
            var entraIdToUse = !string.IsNullOrWhiteSpace(actualObjectId) ? actualObjectId : entraUserObjectId;
            
            var entraUser = await _productsApiService.GetOrCreateEntraUserAsync(
                entraUserEmail,
                entraIdToUse,
                displayName,
                firstName,
                lastName
            );

            if (entraUser == null)
            {
                var errorMessage = $"Failed to get or create Entra User. Please try again.";
                if (IsAjaxRequest())
                {
                    return Json(new { success = false, message = errorMessage });
                }
                TempData["ErrorMessage"] = errorMessage;
                return RedirectToAction("FipsCompletion");
            }

            var userDisplayName = entraUser.DisplayName ?? entraUser.EmailAddress ?? "Unknown";

            var success = await _productsApiService.UpdateProductRoleAsync(fipsId, roleFieldName, entraUser.Id);
            
            if (success)
            {
                var updatedProduct = await _productsApiService.GetProductByFipsIdAsync(fipsId);
                var completionItem = updatedProduct != null ? CreateProductCompletionItem(updatedProduct) : null;
                var successMessage = $"<strong>{productTitle}</strong> - {roleDisplayName} set to <strong>{userDisplayName}</strong>.";

                if (IsAjaxRequest())
                {
                    return Json(new { success = true, message = successMessage, product = completionItem });
                }

                TempData["SuccessMessage"] = successMessage;
            }
            else
            {
                var errorMessage = $"Failed to update {roleDisplayName}. Please try again.";
                if (IsAjaxRequest())
                {
                    return Json(new { success = false, message = errorMessage });
                }

                TempData["ErrorMessage"] = errorMessage;
            }
            
            return RedirectToAction("FipsCompletion");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product role {RoleFieldName} for {FipsId}", roleFieldName, fipsId);
            var errorMessage = $"An error occurred while updating the {roleDisplayName}.";
            if (IsAjaxRequest())
            {
                return Json(new { success = false, message = errorMessage });
            }

            TempData["ErrorMessage"] = errorMessage;
            return RedirectToAction("FipsCompletion");
        }
    }
    
    // GET: DdtReports/DesignAndRunBoard
    public async Task<IActionResult> DesignAndRunBoard()
    {
        try
        {
            // Fetch all products from CMS
            var products = await _productsApiService.GetProductsAsync(null);
            
            // Get all accessibility enrollments
            var accessibilityEnrollments = await _context.ProductAccessibilities
                .Where(pa => !pa.IsDeleted)
                .Include(pa => pa.Issues)
                .ToDictionaryAsync(pa => pa.FipsId);
            
            // Get performance metrics for perf-ux-1 and perf-acc-3
            var perfUx1Metric = await _context.PerformanceMetrics
                .FirstOrDefaultAsync(pm => pm.Identifier == "perf-ux-1");
            var perfAcc3Metric = await _context.PerformanceMetrics
                .FirstOrDefaultAsync(pm => pm.Identifier == "perf-acc-3");
            
            // Get latest product returns for all products
            var latestReturns = await _context.ProductReturns
                .Where(pr => pr.Status == ReturnStatus.Submitted)
                .Include(pr => pr.MetricValues)
                .GroupBy(pr => pr.FipsId)
                .Select(g => g.OrderByDescending(pr => pr.Year)
                               .ThenByDescending(pr => pr.Month)
                               .First())
                .ToDictionaryAsync(pr => pr.FipsId);
            
            var designAndRunBoardItems = new List<DesignAndRunBoardItem>();
            
            // User group category type name variations
            var userGroupVariations = new[] { "User group", "User Group", "User groups", "User Groups", "User Type", "User Types", "Audience", "Target Audience" };
            
            foreach (var product in products.OrderBy(p => p.Title))
            {
                if (string.IsNullOrEmpty(product.FipsId)) continue;
                
                // Get business area from category values
                var businessArea = product.CategoryValues?
                    .FirstOrDefault(cv => cv.CategoryType?.Name?.Equals("Business area", StringComparison.OrdinalIgnoreCase) == true)?.Name 
                    ?? "Not assigned";
                
                // Get user groups count
                var userGroupsCount = product.CategoryValues?
                    .Count(cv => cv.CategoryType != null && 
                                userGroupVariations.Any(v => 
                                    cv.CategoryType.Name.Equals(v, StringComparison.OrdinalIgnoreCase))) ?? 0;
                
                // Calculate completion data
                var hasPhase = !string.IsNullOrEmpty(product.Phase) ||
                              (product.CategoryValues?.Any(cv => 
                                  cv.CategoryType?.Name?.Equals("Phase", StringComparison.OrdinalIgnoreCase) == true) == true);
                var hasBusinessArea = businessArea != "Not assigned";
                var contactsCount = product.ProductContacts?.Count ?? 0;
                var hasProductUrl = !string.IsNullOrEmpty(product.ProductUrl);
                
                // Calculate completion percentage (5 criteria, each worth 20%)
                var completionPercentage = 0.0;
                if (hasPhase) completionPercentage += 20;
                if (hasBusinessArea) completionPercentage += 20;
                if (contactsCount > 0) completionPercentage += 20;
                if (hasProductUrl) completionPercentage += 20;
                if (userGroupsCount > 0) completionPercentage += 20;
                
                // Get accessibility data
                var isEnrolled = accessibilityEnrollments.ContainsKey(product.FipsId);
                var accessibilityEnrollment = isEnrolled ? accessibilityEnrollments[product.FipsId] : null;
                var openIssues = accessibilityEnrollment?.Issues?.Count(i => !i.IsDeleted && (i.Status == "open" || i.Status == "in_progress")) ?? 0;
                var resolvedIssues = accessibilityEnrollment?.Issues?.Count(i => !i.IsDeleted && i.Status == "resolved") ?? 0;
                var totalIssues = accessibilityEnrollment?.Issues?.Count(i => !i.IsDeleted) ?? 0;
                
                // Determine compliance status
                var complianceStatus = "Not enrolled";
                if (isEnrolled)
                {
                    if (openIssues == 0)
                        complianceStatus = "Compliant";
                    else if (openIssues > 0 && openIssues <= 5)
                        complianceStatus = "Partially compliant";
                    else if (openIssues > 5)
                        complianceStatus = "Non-compliant";
                }
                
                // Get performance metrics
                string? perfUx1Value = null;
                string? perfAcc3Value = null;
                DateTime? lastSubmission = null;
                string? lastSubmittedBy = null;
                
                if (latestReturns.ContainsKey(product.FipsId))
                {
                    var latestReturn = latestReturns[product.FipsId];
                    lastSubmission = latestReturn.SubmittedDate;
                    lastSubmittedBy = latestReturn.SubmittedBy;
                    
                    if (perfUx1Metric != null)
                    {
                        perfUx1Value = latestReturn.MetricValues?
                            .FirstOrDefault(mv => mv.PerformanceMetricId == perfUx1Metric.Id)?.Value;
                    }
                    
                    if (perfAcc3Metric != null)
                    {
                        perfAcc3Value = latestReturn.MetricValues?
                            .FirstOrDefault(mv => mv.PerformanceMetricId == perfAcc3Metric.Id)?.Value;
                    }
                }
                
                // Calculate risk score
                var riskScore = 0.0;
                var riskFactors = new List<string>();
                
                // Accessibility risk factors
                if (!isEnrolled)
                {
                    riskScore += 20;
                    riskFactors.Add("Not enrolled in accessibility");
                }
                else if (openIssues > 0)
                {
                    var issueScore = Math.Min(openIssues * 5, 30);
                    riskScore += issueScore;
                    riskFactors.Add($"{openIssues} open accessibility issue{(openIssues > 1 ? "s" : "")}");
                }
                
                // User satisfaction risk factors
                if (!string.IsNullOrEmpty(perfUx1Value) && decimal.TryParse(perfUx1Value, out var uxScore))
                {
                    if (uxScore < 80)
                    {
                        var uxRisk = Math.Min(80 - uxScore, 30);
                        riskScore += (double)uxRisk;
                        riskFactors.Add($"Low user satisfaction ({uxScore})");
                    }
                }
                else
                {
                    riskScore += 15;
                    riskFactors.Add("No user satisfaction data");
                }
                
                // FIPS completion risk factors
                if (completionPercentage < 80)
                {
                    var completionRisk = Math.Min(80 - completionPercentage, 30);
                    riskScore += completionRisk;
                    riskFactors.Add($"Low FIPS completion ({completionPercentage:F0}%)");
                }
                
                // Missing critical data
                if (!hasPhase)
                {
                    riskScore += 10;
                    riskFactors.Add("No phase assigned");
                }
                
                if (!hasBusinessArea)
                {
                    riskScore += 10;
                    riskFactors.Add("No business area assigned");
                }
                
                if (contactsCount == 0)
                {
                    riskScore += 10;
                    riskFactors.Add("No contacts assigned");
                }
                
                designAndRunBoardItems.Add(new DesignAndRunBoardItem
                {
                    FipsId = product.FipsId,
                    ProductTitle = product.Title,
                    Phase = product.Phase ?? "Not set",
                    BusinessArea = businessArea,
                    HasPhase = hasPhase,
                    HasBusinessArea = hasBusinessArea,
                    ContactsCount = contactsCount,
                    HasProductUrl = hasProductUrl,
                    UserGroupsCount = userGroupsCount,
                    CompletionPercentage = completionPercentage,
                    IsEnrolledInAccessibility = isEnrolled,
                    AccessibilityEnrolledAt = accessibilityEnrollment?.EnrolledAt,
                    OpenAccessibilityIssuesCount = openIssues,
                    ResolvedAccessibilityIssuesCount = resolvedIssues,
                    TotalAccessibilityIssuesCount = totalIssues,
                    AccessibilityComplianceStatus = complianceStatus,
                    PerfUx1Value = perfUx1Value,
                    PerfAcc3Value = perfAcc3Value,
                    LastMetricSubmission = lastSubmission,
                    LastSubmittedBy = lastSubmittedBy,
                    RiskScore = riskScore,
                    RiskFactors = riskFactors
                });
            }
            
            // Group products by risk score ranges for better visualization
            var topAtRiskProducts = designAndRunBoardItems
                .Where(item => item.RiskScore > 0)
                .OrderByDescending(item => item.RiskScore)
                .ToList();
            
            // Calculate business area summaries
            var businessAreaSummaries = designAndRunBoardItems
                .GroupBy(item => item.BusinessArea)
                .Select(g => new BusinessAreaSummary
                {
                    BusinessArea = g.Key,
                    ProductCount = g.Count(),
                    AverageCompletionPercentage = g.Average(item => item.CompletionPercentage),
                    EnrolledInAccessibilityCount = g.Count(item => item.IsEnrolledInAccessibility),
                    TotalAccessibilityIssues = g.Sum(item => item.OpenAccessibilityIssuesCount),
                    AverageRiskScore = g.Average(item => item.RiskScore)
                })
                .OrderByDescending(ba => ba.AverageRiskScore)
                .ToList();
            
            var viewModel = new DesignAndRunBoardViewModel
            {
                Products = designAndRunBoardItems,
                AverageCompletionPercentage = designAndRunBoardItems.Any() 
                    ? designAndRunBoardItems.Average(item => item.CompletionPercentage) 
                    : 0,
                BusinessAreaSummaries = businessAreaSummaries,
                TotalProducts = designAndRunBoardItems.Count,
                EnrolledInAccessibilityCount = designAndRunBoardItems.Count(item => item.IsEnrolledInAccessibility),
                TotalOpenAccessibilityIssues = designAndRunBoardItems.Sum(item => item.OpenAccessibilityIssuesCount),
                TopAtRiskProducts = topAtRiskProducts
            };
            
            // Pass metric info to view
            ViewBag.PerfUx1Title = perfUx1Metric?.Title ?? "User experience metric (perf-ux-1)";
            ViewBag.PerfAcc3Title = perfAcc3Metric?.Title ?? "Accessibility metric (perf-acc-3)";
            ViewBag.PerfUx1Exists = perfUx1Metric != null;
            ViewBag.PerfAcc3Exists = perfAcc3Metric != null;
            
            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating Design and Run Board report");
            TempData["ErrorMessage"] = "An error occurred while generating the report. Please try again.";
            return View(new DesignAndRunBoardViewModel());
        }
    }

    private ProductCompletionItem CreateProductCompletionItem(ProductDto product)
    {
        if (product == null)
        {
            return new ProductCompletionItem();
        }

        var categoryValues = product.CategoryValues ?? new List<CategoryValueDto>();

        var phaseValue = categoryValues
            .FirstOrDefault(cv => cv.CategoryType?.Name?.Equals("Phase", StringComparison.OrdinalIgnoreCase) == true);

        var phaseName = !string.IsNullOrWhiteSpace(product.Phase)
            ? product.Phase
            : phaseValue?.Name;

        var hasPhase = !string.IsNullOrWhiteSpace(phaseName);

        var businessAreaValue = categoryValues
            .FirstOrDefault(cv => cv.CategoryType?.Name?.Equals("Business area", StringComparison.OrdinalIgnoreCase) == true);
        var hasBusinessArea = businessAreaValue != null;
        var businessAreaName = businessAreaValue?.Name ?? "Unassigned";

        var contactsCount = product.ProductContacts?.Count ?? 0;
        var productUrl = string.IsNullOrWhiteSpace(product.ProductUrl) ? null : product.ProductUrl;
        var hasProductUrl = !string.IsNullOrEmpty(productUrl);

        var userGroupCategories = categoryValues
            .Where(cv => IsUserGroupCategory(cv.CategoryType?.Name))
            .ToList();

        var userGroupNames = userGroupCategories
            .Select(cv => cv.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var userGroupIds = userGroupCategories
            .Select(cv => cv.Id)
            .Distinct()
            .ToList();

        var sroContacts = new List<string>();
        var iaoContacts = new List<string>();
        var deliveryManagerContacts = new List<string>();
        var contactDetails = new List<string>();

        if (product.ProductContacts != null)
        {
            _logger.LogInformation("Product {FipsId} has {Count} product contacts", product.FipsId, product.ProductContacts.Count);
            
            foreach (var contact in product.ProductContacts)
            {
                if (contact == null) continue;
                var role = contact.Role?.Trim();
                
                // Log all contact details for debugging
                _logger.LogInformation("Product {FipsId} contact - Role: '{Role}', ContactName: '{ContactName}', LegacyName: '{LegacyName}', UserDisplayName: '{UserDisplayName}', UserUsername: '{UserUsername}'",
                    product.FipsId, role ?? "null", contact.ContactName ?? "null", contact.LegacyName ?? "null",
                    contact.UsersPermissionsUser?.DisplayName ?? "null", contact.UsersPermissionsUser?.Username ?? "null");

                if (string.IsNullOrEmpty(role))
                {
                    _logger.LogWarning("Product {FipsId} contact has empty or null role", product.FipsId);
                    continue;
                }

                var display = BuildContactDisplay(contact);
                if (string.IsNullOrEmpty(display))
                {
                    _logger.LogWarning("Product {FipsId} contact with role '{Role}' has no display name", product.FipsId, role);
                    continue;
                }

                var detail = string.IsNullOrWhiteSpace(role)
                    ? display
                    : $"{display} - {role}";
                contactDetails.Add(detail);

                if (RoleContainsAny(role, SeniorResponsibleOfficerRoleKeywords))
                {
                    _logger.LogInformation("Matched SRO role: '{Role}' for {Display} in product {FipsId}", role, display, product.FipsId);
                    AddContactIfMissing(sroContacts, display);
                }
                else if (RoleContainsAny(role, InformationAssetOwnerRoleKeywords))
                {
                    var nameOnly = BuildContactNameOnly(contact);
                    if (!string.IsNullOrWhiteSpace(nameOnly))
                    {
                        AddContactIfMissing(iaoContacts, nameOnly);
                    }
                }
                else if (RoleContainsAny(role, DeliveryManagerRoleKeywords))
                {
                    var nameOnly = BuildContactNameOnly(contact);
                    if (!string.IsNullOrWhiteSpace(nameOnly))
                    {
                        AddContactIfMissing(deliveryManagerContacts, nameOnly);
                    }
                }
                else
                {
                    _logger.LogDebug("Product {FipsId} contact role '{Role}' did not match any known role keywords", product.FipsId, role);
                }
            }
        }
        else
        {
            _logger.LogInformation("Product {FipsId} has no product contacts", product.FipsId);
        }

        var userGroupsCount = userGroupNames.Count;

        var completedCriteria = 0;
        if (hasPhase) completedCriteria++;
        if (hasBusinessArea) completedCriteria++;
        if (contactsCount > 0) completedCriteria++;
        if (hasProductUrl) completedCriteria++;
        if (userGroupsCount > 0) completedCriteria++;

        var completionPercentage = (completedCriteria / 5.0) * 100;

        return new ProductCompletionItem
        {
            FipsId = product.FipsId ?? string.Empty,
            ProductTitle = product.Title,
            BusinessArea = businessAreaName,
            PhaseName = phaseName,
            State = product.State,
            SeniorResponsibleOfficer = sroContacts.Count > 0 ? string.Join(", ", sroContacts) : null,
            InformationAssetOwner = iaoContacts.Count > 0 ? string.Join(", ", iaoContacts) : null,
            DeliveryManager = deliveryManagerContacts.Count > 0 ? string.Join(", ", deliveryManagerContacts) : null,
            SeniorResponsibleOfficerContacts = new List<string>(sroContacts),
            InformationAssetOwnerContacts = new List<string>(iaoContacts),
            DeliveryManagerContacts = new List<string>(deliveryManagerContacts),
            ContactDetails = contactDetails,
            UserGroupNames = userGroupNames,
            UserGroupCategoryValueIds = userGroupIds,
            ProductUrl = productUrl,
            HasPhase = hasPhase,
            HasBusinessArea = hasBusinessArea,
            ContactsCount = contactsCount,
            HasProductUrl = hasProductUrl,
            UserGroupsCount = userGroupsCount,
            CompletionPercentage = completionPercentage
        };
    }

    private static string? BuildContactDisplay(ProductContactDto contact)
    {
        // Prioritize display_name from users_permissions_user, then fall back to other name fields
        // No email addresses in display
        var nameCandidates = new[]
        {
            contact.UsersPermissionsUser?.DisplayName,
            contact.ContactName,
            contact.LegacyName,
            contact.UsersPermissionsUser?.Username
        };

        return nameCandidates.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
    }

    private static string? BuildContactNameOnly(ProductContactDto contact)
    {
        // Prioritize display_name from users_permissions_user, then fall back to other name fields
        var nameCandidates = new[]
        {
            contact.UsersPermissionsUser?.DisplayName,
            contact.ContactName,
            contact.LegacyName,
            contact.UsersPermissionsUser?.Username
        };

        return nameCandidates.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
    }

    private static void AddContactIfMissing(List<string> contacts, string value)
    {
        if (!contacts.Any(existing => existing.Equals(value, StringComparison.OrdinalIgnoreCase)))
        {
            contacts.Add(value);
        }
    }

    private static bool IsUserGroupCategory(string? categoryTypeName) =>
        !string.IsNullOrWhiteSpace(categoryTypeName) &&
        UserGroupCategoryTypeNames.Any(name => name.Equals(categoryTypeName, StringComparison.OrdinalIgnoreCase));

    private bool IsAjaxRequest() =>
        string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);

    private static bool RoleContains(string role, string keyword) =>
        !string.IsNullOrWhiteSpace(role) &&
        !string.IsNullOrWhiteSpace(keyword) &&
        role.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;

    private static bool RoleContainsAny(string role, IEnumerable<string> keywords) =>
        !string.IsNullOrWhiteSpace(role) &&
        keywords.Any(keyword => RoleContains(role, keyword));
}

