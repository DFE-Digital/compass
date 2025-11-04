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
            var serviceHealthIssues = 0; // We'd need to calculate this from products
            
            // Operational returns health check
            var now = DateTime.UtcNow;
            var currentYear = now.Month == 1 ? now.Year - 1 : now.Year;
            var currentMonth = now.Month == 1 ? 12 : now.Month - 1;
            
            foreach (var product in allProducts.Where(p => !string.IsNullOrEmpty(p.FipsId)))
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
    public async Task<IActionResult> BusinessArea(string area, DateTime? weekStart, string section = "summary")
    {
        try
        {
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

            ViewBag.CurrentSection = section;
            ViewBag.WeekStart = weekStart.Value;
            ViewBag.WeekEnd = weekStart.Value.AddDays(6);
            ViewBag.BusinessArea = area;
            
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
            
            // Count products with 0% completion
            var zeroCompletionCount = completionItems.Count(p => p.CompletionPercentage == 0);
            
            // Get category values for dropdowns
            var phaseCategoryValues = await _productsApiService.GetPhaseCategoryValuesAsync();
            var businessAreaCategoryValues = await _productsApiService.GetBusinessAreaCategoryValuesAsync();
            
            var viewModel = new FipsCompletionViewModel
            {
                Products = completionItems,
                AverageCompletionPercentage = averageCompletion,
                BusinessAreaCompletions = businessAreaCompletions,
                ZeroCompletionCount = zeroCompletionCount
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
    public async Task<IActionResult> UpdateProductBusinessArea(string fipsId, int businessAreaCategoryValueId)
    {
        try
        {
            // Get product title before update
            var product = await _productsApiService.GetProductByFipsIdAsync(fipsId);
            var productTitle = product?.Title ?? fipsId;
            
            // Get business area name
            var businessAreas = await _productsApiService.GetBusinessAreaCategoryValuesAsync();
            var businessAreaName = businessAreas.FirstOrDefault(ba => ba.Id == businessAreaCategoryValueId)?.Name ?? "Business Area";
            
            var success = await _productsApiService.UpdateProductBusinessAreaAsync(fipsId, businessAreaCategoryValueId);
            
            if (success)
            {
                TempData["SuccessMessage"] = $"<strong>{productTitle}</strong> - Business Area updated to <strong>{businessAreaName}</strong>.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to update product Business Area. Please try again.";
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
}

