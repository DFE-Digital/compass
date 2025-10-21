using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Compass.Data;
using Compass.Models;
using Compass.Services;
using Microsoft.AspNetCore.Authorization;

namespace Compass.Controllers;

[Authorize]
public class ProductsController : Controller
{
    private readonly CompassDbContext _context;
    private readonly IProductsApiService _productsApiService;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(
        CompassDbContext context,
        IProductsApiService productsApiService,
        ILogger<ProductsController> logger)
    {
        _context = context;
        _productsApiService = productsApiService;
        _logger = logger;
    }

    // GET: Products
    public async Task<IActionResult> Index(int page = 1, int pageSize = 20)
    {
        var userEmail = User.Identity?.Name;
        
        if (string.IsNullOrEmpty(userEmail))
        {
            TempData["ErrorMessage"] = "Unable to identify user.";
            return RedirectToAction("Index", "Home");
        }

        try
        {
            // Fetch all products
            var allProducts = await _productsApiService.GetProductsAsync(null);
            
            var productListItems = new List<ProductListViewModel>();
            
            foreach (var product in allProducts)
            {
                if (string.IsNullOrEmpty(product.FipsId)) continue;
                
                // Extract business area from category_values
                var businessArea = product.CategoryValues?
                    .FirstOrDefault(cv => cv.CategoryType?.Name?.Equals("Business area", StringComparison.OrdinalIgnoreCase) == true)
                    ?.Name;
                
                // Get RAID data for this product
                var risks = await _context.Risks
                    .Where(r => r.FipsId == product.FipsId && !r.IsDeleted)
                    .ToListAsync();
                
                var issues = await _context.Issues
                    .Where(i => i.FipsId == product.FipsId && !i.IsDeleted)
                    .ToListAsync();
                
                var actions = await _context.Actions
                    .Where(a => a.FipsId == product.FipsId && !a.IsDeleted)
                    .ToListAsync();
                
                var milestones = await _context.Milestones
                    .Where(m => m.FipsId == product.FipsId && !m.IsDeleted)
                    .ToListAsync();
                
                // Calculate statistics
                var openRisks = risks.Count(r => r.Status == "open" || r.Status == "treating");
                var highRisks = risks.Count(r => r.RiskScore >= 15);
                
                var openIssues = issues.Count(i => i.Status == "open" || i.Status == "in_progress");
                var criticalIssues = issues.Count(i => i.Severity == "critical");
                var blockedIssues = issues.Count(i => i.BlockedFlag);
                
                var overdueActions = actions.Count(a => 
                    a.DueDate.HasValue && 
                    a.DueDate < DateTime.UtcNow && 
                    a.Status != "done" && 
                    a.Status != "cancelled");
                
                var overdueMilestones = milestones.Count(m => 
                    m.DueDate < DateTime.UtcNow && 
                    m.Status != "complete" && 
                    m.Status != "cancelled");
                
                // Check if user is assigned to this product
                var isUserAssigned = product.ProductContacts?.Any(pc => 
                    pc.UsersPermissionsUser?.Email?.Equals(userEmail, StringComparison.OrdinalIgnoreCase) == true) == true;
                
                var healthScore = CalculateHealthScore(openRisks, highRisks, openIssues, 
                    criticalIssues, blockedIssues, overdueActions, overdueMilestones);
                
                productListItems.Add(new ProductListViewModel
                {
                    FipsId = product.FipsId,
                    ProductTitle = product.Title,
                    BusinessArea = businessArea,
                    Phase = product.Phase,
                    TotalRisks = risks.Count,
                    OpenRisks = openRisks,
                    HighRisks = highRisks,
                    TotalIssues = issues.Count,
                    OpenIssues = openIssues,
                    CriticalIssues = criticalIssues,
                    BlockedIssues = blockedIssues,
                    TotalActions = actions.Count,
                    OverdueActions = overdueActions,
                    TotalMilestones = milestones.Count,
                    OverdueMilestones = overdueMilestones,
                    HealthScore = healthScore,
                    IsUserAssigned = isUserAssigned
                });
            }
            
            // Separate into My Products and All Products
            var myProducts = productListItems
                .Where(p => p.IsUserAssigned)
                .OrderBy(p => p.ProductTitle)
                .ToList();
            
            var otherProducts = productListItems
                .Where(p => !p.IsUserAssigned)
                .OrderBy(p => p.ProductTitle)
                .ToList();
            
            // Paginate other products
            var paginatedProducts = otherProducts
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            
            var viewModel = new ProductsIndexViewModel
            {
                MyProducts = myProducts,
                AllProducts = paginatedProducts,
                TotalProducts = productListItems.Count,
                MyProductsCount = myProducts.Count,
                TotalRisks = productListItems.Sum(p => p.TotalRisks),
                TotalHighRisks = productListItems.Sum(p => p.HighRisks),
                TotalIssues = productListItems.Sum(p => p.TotalIssues),
                TotalCriticalIssues = productListItems.Sum(p => p.CriticalIssues)
            };
            
            ViewData["ActiveNav"] = "products";
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalOtherProducts = otherProducts.Count;
            ViewBag.TotalPages = (int)Math.Ceiling((double)otherProducts.Count / pageSize);
            
            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading products for user {Email}", userEmail);
            TempData["ErrorMessage"] = "An error occurred while loading products. Please try again.";
            return View(new ProductsIndexViewModel());
        }
    }

    // GET: Products/Dashboard/FIPS-001
    public async Task<IActionResult> Dashboard(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return NotFound();
        }

        try
        {
            // Get product details
            var products = await _productsApiService.GetProductsAsync(null);
            var product = products.FirstOrDefault(p => p.FipsId == id);
            
            if (product == null)
            {
                TempData["ErrorMessage"] = $"Product with FIPS ID '{id}' not found.";
                return RedirectToAction(nameof(Index));
            }

            // Extract business area from category_values
            var businessArea = product.CategoryValues?
                .FirstOrDefault(cv => cv.CategoryType?.Name?.Equals("Business area", StringComparison.OrdinalIgnoreCase) == true)
                ?.Name;

            // Get RAID data
            var risks = await _context.Risks
                .Where(r => r.FipsId == id && !r.IsDeleted)
                .OrderByDescending(r => r.RiskScore)
                .ToListAsync();
            
            var issues = await _context.Issues
                .Where(i => i.FipsId == id && !i.IsDeleted)
                .OrderByDescending(i => i.Severity == "critical" ? 3 : i.Severity == "high" ? 2 : i.Severity == "medium" ? 1 : 0)
                .ThenByDescending(i => i.BlockedFlag)
                .ToListAsync();
            
            var actions = await _context.Actions
                .Where(a => a.FipsId == id && !a.IsDeleted)
                .OrderBy(a => a.DueDate)
                .ToListAsync();
            
            var milestones = await _context.Milestones
                .Where(m => m.FipsId == id && !m.IsDeleted)
                .OrderBy(m => m.DueDate)
                .ToListAsync();

            // Get latest product return and metrics
            var latestReturn = await _context.ProductReturns
                .Where(pr => pr.FipsId == id)
                .OrderByDescending(pr => pr.Year)
                .ThenByDescending(pr => pr.Month)
                .FirstOrDefaultAsync();

            decimal? userSatisfaction = null;
            var recentMetrics = new List<ProductMetricSummary>();
            
            if (latestReturn != null)
            {
                // Get all metrics for this return
                var metricValues = await _context.ProductMetricValues
                    .Where(mv => mv.ProductReturnId == latestReturn.Id)
                    .Include(mv => mv.PerformanceMetric)
                    .ToListAsync();

                foreach (var mv in metricValues.Take(10))
                {
                    // Try to extract user satisfaction
                    if (mv.PerformanceMetric.Identifier.ToLower().Contains("satisfaction") || 
                        mv.PerformanceMetric.Identifier.ToLower().Contains("sat"))
                    {
                        if (decimal.TryParse(mv.Value, out var parsedValue))
                        {
                            userSatisfaction = parsedValue;
                        }
                    }

                    recentMetrics.Add(new ProductMetricSummary
                    {
                        MetricName = mv.PerformanceMetric.Title,
                        Value = mv.Value ?? "N/A",
                        Unit = "",
                        Rag = ""
                    });
                }
            }

            // Calculate problem score
            var problemScore = CalculateProblemScore(risks, issues, actions, milestones, userSatisfaction);

            // Build view model
            var viewModel = new ProductDashboardViewModel
            {
                FipsId = id,
                ProductTitle = product.Title,
                
                // Risk summary
                TotalRisks = risks.Count,
                OpenRisks = risks.Count(r => r.Status == "open" || r.Status == "treating"),
                HighRisks = risks.Count(r => r.RiskScore >= 15),
                MediumRisks = risks.Count(r => r.RiskScore >= 10 && r.RiskScore < 15),
                AverageRiskScore = risks.Any() ? risks.Average(r => r.RiskScore) : 0,
                TopRisks = risks.Take(5).ToList(),
                
                // Issue summary
                TotalIssues = issues.Count,
                OpenIssues = issues.Count(i => i.Status == "open" || i.Status == "in_progress"),
                CriticalIssues = issues.Count(i => i.Severity == "critical"),
                HighIssues = issues.Count(i => i.Severity == "high"),
                BlockedIssues = issues.Count(i => i.BlockedFlag),
                OldestOpenIssue = issues
                    .Where(i => i.Status == "open" || i.Status == "in_progress")
                    .OrderBy(i => i.DetectedDate)
                    .FirstOrDefault()?.DetectedDate,
                TopIssues = issues.Take(5).ToList(),
                
                // Action summary
                TotalActions = actions.Count,
                OverdueActions = actions.Count(a => 
                    a.DueDate.HasValue && 
                    a.DueDate < DateTime.UtcNow && 
                    a.Status != "done" && 
                    a.Status != "cancelled"),
                InProgressActions = actions.Count(a => a.Status == "in_progress"),
                CompletedActions = actions.Count(a => a.Status == "done"),
                UpcomingActions = actions
                    .Where(a => a.Status != "done" && a.Status != "cancelled")
                    .OrderBy(a => a.DueDate)
                    .Take(5)
                    .ToList(),
                
                // Milestone summary
                TotalMilestones = milestones.Count,
                DelayedMilestones = milestones.Count(m => 
                    m.Status == "delayed" || 
                    (m.DueDate < DateTime.UtcNow && m.Status != "complete" && m.Status != "cancelled")),
                CompletedMilestones = milestones.Count(m => m.Status == "complete"),
                UpcomingMilestones = milestones.Count(m => 
                    m.DueDate >= DateTime.UtcNow && 
                    m.Status != "complete" && 
                    m.Status != "cancelled"),
                KeyMilestones = milestones.Take(5).ToList(),
                
                // Performance metrics
                UserSatisfaction = userSatisfaction,
                LastReportDate = latestReturn?.SubmittedDate,
                HasMetricsData = latestReturn != null,
                RecentMetrics = recentMetrics,
                
                // Health indicators
                ProblemScore = problemScore,
                NeedsAttention = problemScore > 50,
                HealthStatus = problemScore > 70 ? "Critical" : problemScore > 50 ? "Needs attention" : problemScore > 20 ? "Monitor" : "Healthy"
            };

            ViewData["ActiveNav"] = "products";
            ViewData["ProductBusinessArea"] = businessArea;
            ViewData["ProductPhase"] = product.Phase;
            
            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating product dashboard for {FipsId}", id);
            TempData["ErrorMessage"] = "An error occurred while loading the product dashboard. Please try again.";
            return RedirectToAction(nameof(Index));
        }
    }

    private int CalculateHealthScore(int openRisks, int highRisks, int openIssues, 
        int criticalIssues, int blockedIssues, int overdueActions, int overdueMilestones)
    {
        // Higher score = better health (0-100)
        int score = 100;
        
        // Deduct points for problems
        score -= (openRisks * 2);
        score -= (highRisks * 5);
        score -= (openIssues * 3);
        score -= (criticalIssues * 10);
        score -= (blockedIssues * 8);
        score -= (overdueActions * 2);
        score -= (overdueMilestones * 5);
        
        return Math.Max(0, score);
    }

    private int CalculateProblemScore(List<Risk> risks, List<Issue> issues, 
        List<Models.Action> actions, List<Milestone> milestones, decimal? userSatisfaction)
    {
        // Higher score = more problems (0-100)
        int score = 0;
        
        // Risk factors
        score += risks.Count(r => r.RiskScore >= 15) * 10; // High risks
        score += risks.Count(r => r.RiskScore >= 10 && r.RiskScore < 15) * 5; // Medium risks
        score += risks.Count(r => r.Status == "open") * 3; // Open risks
        
        // Issue factors
        score += issues.Count(i => i.Severity == "critical") * 15; // Critical issues
        score += issues.Count(i => i.Severity == "high") * 8; // High severity
        score += issues.Count(i => i.BlockedFlag) * 10; // Blocked issues
        score += issues.Count(i => i.Status == "open" || i.Status == "in_progress") * 2;
        
        // Action factors
        var overdueActions = actions.Count(a => 
            a.DueDate.HasValue && 
            a.DueDate < DateTime.UtcNow && 
            a.Status != "done" && 
            a.Status != "cancelled");
        score += overdueActions * 3;
        
        // Milestone factors
        var delayedMilestones = milestones.Count(m => m.Status == "delayed");
        score += delayedMilestones * 8;
        
        // User satisfaction factor (if low)
        if (userSatisfaction.HasValue && userSatisfaction < 70)
        {
            score += (int)((70 - userSatisfaction.Value) / 2);
        }
        
        return Math.Min(100, score);
    }
}

