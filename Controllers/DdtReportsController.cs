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

    public DdtReportsController(CompassDbContext context, ILogger<DdtReportsController> logger, IProductsApiService productsApiService, IReturnStatusService returnStatusService)
    {
        _context = context;
        _logger = logger;
        _productsApiService = productsApiService;
        _returnStatusService = returnStatusService;
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
            // Get all business areas
            var businessAreas = await _productsApiService.GetBusinessAreasAsync();
            ViewBag.BusinessAreas = businessAreas;
            
            // If no area is selected, default to the first one
            if (string.IsNullOrEmpty(area) && businessAreas.Any())
            {
                area = businessAreas.First();
            }
            
            // Calculate week start if not provided
            if (!weekStart.HasValue)
            {
                var today = DateTime.Today;
                var dayOfWeek = (int)today.DayOfWeek;
                var diff = dayOfWeek == 0 ? -6 : 1 - dayOfWeek; // Monday = start of week
                weekStart = today.AddDays(diff);
            }

            // Get all active projects for the specified business area
            var projects = await _context.Projects
                .Include(p => p.Successes)
                .Include(p => p.Milestones)
                .Include(p => p.RagHistory)
                .Where(p => !p.IsDeleted && p.Status == "Active" && p.BusinessArea == area)
                .OrderBy(p => p.Title)
                .ToListAsync();

            // Get all products from CMS
            var allProducts = await _productsApiService.GetProductsAsync(null);
            
            // Filter products by business area
            var businessAreaProducts = allProducts
                .Where(p => p.CategoryValues?.Any(cv => 
                    cv.CategoryType?.Name?.Equals("Business area", StringComparison.OrdinalIgnoreCase) == true &&
                    cv.Name?.Equals(area, StringComparison.OrdinalIgnoreCase) == true) == true)
                .ToList();
            
            // Get accessibility data for products
            var accessibilityEnrollments = await _context.ProductAccessibilities
                .Where(pa => !pa.IsDeleted)
                .Include(pa => pa.Issues)
                .ToDictionaryAsync(pa => pa.FipsId);
            
            // Calculate product metrics
            var totalProducts = businessAreaProducts.Count;
            var enrolledProducts = businessAreaProducts.Count(p => 
                !string.IsNullOrEmpty(p.FipsId) && accessibilityEnrollments.ContainsKey(p.FipsId));
            var totalAccessibilityIssues = businessAreaProducts
                .Where(p => !string.IsNullOrEmpty(p.FipsId) && accessibilityEnrollments.ContainsKey(p.FipsId))
                .Sum(p => accessibilityEnrollments[p.FipsId].Issues?.Count(i => !i.IsDeleted && (i.Status == "open" || i.Status == "in_progress")) ?? 0);

            // Get performance metrics
            var perfUx1Metric = await _context.PerformanceMetrics
                .FirstOrDefaultAsync(pm => pm.Identifier == "perf-ux-1");
            var perfAcc3Metric = await _context.PerformanceMetrics
                .FirstOrDefaultAsync(pm => pm.Identifier == "perf-acc-3");
            
            // Get latest product returns for products in this business area
            var productFipsIds = businessAreaProducts
                .Where(p => !string.IsNullOrEmpty(p.FipsId))
                .Select(p => p.FipsId)
                .ToList();
                
            var latestReturns = await _context.ProductReturns
                .Where(pr => productFipsIds.Contains(pr.FipsId) && pr.Status == ReturnStatus.Submitted)
                .Include(pr => pr.MetricValues)
                .GroupBy(pr => pr.FipsId)
                .Select(g => g.OrderByDescending(pr => pr.Year)
                               .ThenByDescending(pr => pr.Month)
                               .First())
                .ToDictionaryAsync(pr => pr.FipsId);

            ViewBag.CurrentSection = section;
            ViewBag.WeekStart = weekStart.Value;
            ViewBag.WeekEnd = weekStart.Value.AddDays(6);
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
            var completionItems = new List<ProductCompletionItem>();
            
            // User group category type name variations
            var userGroupVariations = new[] { "User group", "User Group", "User groups", "User Groups", "User Type", "User Types", "Audience", "Target Audience" };
            
            foreach (var product in products.OrderBy(p => p.Title))
            {
                if (string.IsNullOrEmpty(product.FipsId)) continue;
                
                // Check Phase
                var hasPhase = !string.IsNullOrEmpty(product.Phase) ||
                              (product.CategoryValues?.Any(cv => 
                                  cv.CategoryType?.Name?.Equals("Phase", StringComparison.OrdinalIgnoreCase) == true) == true);
                
                // Check Business Area
                var hasBusinessArea = product.CategoryValues?.Any(cv => 
                    cv.CategoryType?.Name?.Equals("Business area", StringComparison.OrdinalIgnoreCase) == true) == true;
                
                // Extract Business Area name
                var businessAreaName = product.CategoryValues?
                    .FirstOrDefault(cv => cv.CategoryType?.Name?.Equals("Business area", StringComparison.OrdinalIgnoreCase) == true)
                    ?.Name ?? "Unassigned";
                
                // Count Contacts
                var contactsCount = product.ProductContacts?.Count ?? 0;
                
                // Check Product URL
                var hasProductUrl = !string.IsNullOrEmpty(product.ProductUrl);
                
                // Count User Groups
                var userGroupsCount = product.CategoryValues?
                    .Count(cv => cv.CategoryType != null && 
                                userGroupVariations.Any(v => 
                                    cv.CategoryType.Name.Equals(v, StringComparison.OrdinalIgnoreCase))) ?? 0;
                
                // Calculate completion percentage (5 criteria, 20% each)
                var completedCriteria = 0;
                if (hasPhase) completedCriteria++;
                if (hasBusinessArea) completedCriteria++;
                if (contactsCount > 0) completedCriteria++;
                if (hasProductUrl) completedCriteria++;
                if (userGroupsCount > 0) completedCriteria++;
                
                var completionPercentage = (completedCriteria / 5.0) * 100;
                
                completionItems.Add(new ProductCompletionItem
                {
                    FipsId = product.FipsId,
                    ProductTitle = product.Title,
                    BusinessArea = businessAreaName,
                    State = product.State,
                    HasPhase = hasPhase,
                    HasBusinessArea = hasBusinessArea,
                    ContactsCount = contactsCount,
                    HasProductUrl = hasProductUrl,
                    UserGroupsCount = userGroupsCount,
                    CompletionPercentage = completionPercentage
                });
            }
            
            // Calculate average completion percentage
            var averageCompletion = completionItems.Any() 
                ? completionItems.Average(p => p.CompletionPercentage) 
                : 0;
            
            // Calculate business area completions
            var businessAreaCompletions = completionItems
                .GroupBy(p => p.BusinessArea)
                .Select(g => new BusinessAreaCompletion
                {
                    BusinessArea = g.Key,
                    ProductCount = g.Count(),
                    AverageCompletionPercentage = g.Average(p => p.CompletionPercentage)
                })
                .OrderByDescending(ba => ba.AverageCompletionPercentage)
                .ToList();
            
            // Count products with 0% and 100% completion
            var zeroCompletionCount = completionItems.Count(p => p.CompletionPercentage == 0);
            var fullCompletionCount = completionItems.Count(p => p.CompletionPercentage == 100);
            
            // Count completed fields
            var completedPhaseCount = completionItems.Count(p => p.HasPhase);
            var completedBusinessAreaCount = completionItems.Count(p => p.HasBusinessArea);
            var completedProductUrlCount = completionItems.Count(p => p.HasProductUrl);
            
            // Get category values for dropdowns
            var phaseCategoryValues = await _productsApiService.GetPhaseCategoryValuesAsync();
            var businessAreaCategoryValues = await _productsApiService.GetBusinessAreaCategoryValuesAsync();
            
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
            
            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating FIPS completion report");
            TempData["ErrorMessage"] = "An error occurred while generating the report. Please try again.";
            return View(new FipsCompletionViewModel());
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
            var completionItems = new List<ProductCompletionItem>();
            
            // User group category type name variations
            var userGroupVariations = new[] { "User group", "User Group", "User groups", "User Groups", "User Type", "User Types", "Audience", "Target Audience" };
            
            foreach (var product in products.OrderBy(p => p.Title))
            {
                // Include products even without FipsId for full CMS visibility
                
                // Check Phase
                var hasPhase = !string.IsNullOrEmpty(product.Phase) ||
                              (product.CategoryValues?.Any(cv => 
                                  cv.CategoryType?.Name?.Equals("Phase", StringComparison.OrdinalIgnoreCase) == true) == true);
                
                // Check Business Area
                var hasBusinessArea = product.CategoryValues?.Any(cv => 
                    cv.CategoryType?.Name?.Equals("Business area", StringComparison.OrdinalIgnoreCase) == true) == true;
                
                // Extract Business Area name
                var businessAreaName = product.CategoryValues?
                    .FirstOrDefault(cv => cv.CategoryType?.Name?.Equals("Business area", StringComparison.OrdinalIgnoreCase) == true)
                    ?.Name ?? "Unassigned";
                
                // Count Contacts
                var contactsCount = product.ProductContacts?.Count ?? 0;
                
                // Check Product URL
                var hasProductUrl = !string.IsNullOrEmpty(product.ProductUrl);
                
                // Count User Groups
                var userGroupsCount = product.CategoryValues?
                    .Count(cv => cv.CategoryType != null && 
                                userGroupVariations.Any(v => 
                                    cv.CategoryType.Name.Equals(v, StringComparison.OrdinalIgnoreCase))) ?? 0;
                
                // Calculate completion percentage (5 criteria, 20% each)
                var completedCriteria = 0;
                if (hasPhase) completedCriteria++;
                if (hasBusinessArea) completedCriteria++;
                if (contactsCount > 0) completedCriteria++;
                if (hasProductUrl) completedCriteria++;
                if (userGroupsCount > 0) completedCriteria++;
                
                var completionPercentage = (completedCriteria / 5.0) * 100;
                
                completionItems.Add(new ProductCompletionItem
                {
                    FipsId = product.FipsId ?? string.Empty,
                    ProductTitle = product.Title,
                    BusinessArea = businessAreaName,
                    State = product.State,
                    HasPhase = hasPhase,
                    HasBusinessArea = hasBusinessArea,
                    ContactsCount = contactsCount,
                    HasProductUrl = hasProductUrl,
                    UserGroupsCount = userGroupsCount,
                    CompletionPercentage = completionPercentage
                });
            }
            
            // Calculate average completion percentage
            var averageCompletion = completionItems.Any() 
                ? completionItems.Average(p => p.CompletionPercentage) 
                : 0;
            
            // Calculate business area completions
            var businessAreaCompletions = completionItems
                .GroupBy(p => p.BusinessArea)
                .Select(g => new BusinessAreaCompletion
                {
                    BusinessArea = g.Key,
                    ProductCount = g.Count(),
                    AverageCompletionPercentage = g.Average(p => p.CompletionPercentage)
                })
                .OrderByDescending(ba => ba.AverageCompletionPercentage)
                .ToList();
            
            // Count products with 0% and 100% completion
            var zeroCompletionCount = completionItems.Count(p => p.CompletionPercentage == 0);
            var fullCompletionCount = completionItems.Count(p => p.CompletionPercentage == 100);
            
            // Count completed fields
            var completedPhaseCount = completionItems.Count(p => p.HasPhase);
            var completedBusinessAreaCount = completionItems.Count(p => p.HasBusinessArea);
            var completedProductUrlCount = completionItems.Count(p => p.HasProductUrl);
            
            // Get category values for dropdowns
            var phaseCategoryValues = await _productsApiService.GetPhaseCategoryValuesAsync();
            var businessAreaCategoryValues = await _productsApiService.GetBusinessAreaCategoryValuesAsync();
            
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
                TempData["SuccessMessage"] = $"<strong>{productTitle}</strong> - Phase updated to <strong>{phaseName}</strong>.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to update product Phase. Please try again.";
            }
            
            return RedirectToAction("FipsCompletion");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product Phase for {FipsId}", fipsId);
            TempData["ErrorMessage"] = "An error occurred while updating the product Phase.";
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
                // Provide different messages based on whether multiple business areas were removed
                if (existingBusinessAreas.Count > 1)
                {
                    TempData["SuccessMessage"] = $"<strong>{productTitle}</strong> - Removed {existingBusinessAreas.Count} existing business area(s) ({string.Join(", ", existingBusinessAreas)}) and assigned <strong>{businessAreaName}</strong>.";
                }
                else if (existingBusinessAreas.Count == 1)
                {
                    TempData["SuccessMessage"] = $"<strong>{productTitle}</strong> - Business area updated from <strong>{existingBusinessAreas[0]}</strong> to <strong>{businessAreaName}</strong>.";
                }
                else
                {
                    TempData["SuccessMessage"] = $"<strong>{productTitle}</strong> - Business area assigned to <strong>{businessAreaName}</strong>.";
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to update product Business Area. Please try again.";
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
            TempData["ErrorMessage"] = "An error occurred while updating the product Business Area.";
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
                TempData["ErrorMessage"] = "Product URL cannot be empty.";
                return RedirectToAction("FipsCompletion");
            }

            // Validate URL format
            if (!Uri.TryCreate(productUrl, UriKind.Absolute, out var uri) || 
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                TempData["ErrorMessage"] = "Please provide a valid HTTP or HTTPS URL.";
                return RedirectToAction("FipsCompletion");
            }

            // Get product title before update
            var product = await _productsApiService.GetProductByFipsIdAsync(fipsId);
            var productTitle = product?.Title ?? fipsId;

            var success = await _productsApiService.UpdateProductUrlAsync(fipsId, productUrl);
            
            if (success)
            {
                TempData["SuccessMessage"] = $"<strong>{productTitle}</strong> - Product URL updated to <strong>{productUrl}</strong>.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to update product URL. Please try again.";
            }
            
            return RedirectToAction("FipsCompletion");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product URL for {FipsId}", fipsId);
            TempData["ErrorMessage"] = "An error occurred while updating the product URL.";
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
}

