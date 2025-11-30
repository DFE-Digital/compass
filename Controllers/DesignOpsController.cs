using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Compass.Attributes;
using Compass.Data;
using Compass.Services;
using Compass.Models;

namespace Compass.Controllers;

[Authorize]
[RequireDesignOpsAdmin]
public class DesignOpsController : Controller
{
    private readonly ILogger<DesignOpsController> _logger;
    private readonly CompassDbContext _context;
    private readonly IProductsApiService _productsApiService;

    public DesignOpsController(
        ILogger<DesignOpsController> logger, 
        CompassDbContext context, 
        IProductsApiService productsApiService)
    {
        _logger = logger;
        _context = context;
        _productsApiService = productsApiService;
    }

    // GET: DesignOps/Dashboard
    public async Task<IActionResult> Dashboard()
    {
        try
        {
            ViewData["Title"] = "Design Operations Dashboard";
            
            // Get pending retest requests count
            var pendingRetestCount = await _context.AccessibilityRetestRequests
                .CountAsync(rr => rr.IsCompleted == null);
            
            // Get all enrolled products with issues
            var enrolledProducts = await _context.ProductAccessibilities
                .Include(pa => pa.Issues.Where(i => !i.IsDeleted))
                .Where(pa => !pa.IsDeleted && pa.IsActive)
                .ToListAsync();
            
            // Count open accessibility issues (not resolved, not wont_fix, not deleted)
            var openIssuesCount = enrolledProducts
                .Sum(p => p.Issues.Count(i => i.Status != "resolved" && i.Status != "wont_fix"));
            
            // Get total products from CMS
            var cmsProducts = await _productsApiService.GetProductsAsync();
            var totalProducts = cmsProducts?.Count ?? 0;
            var enrolledProductsCount = enrolledProducts.Count;
            var enrollmentPercentage = totalProducts > 0 
                ? Math.Round((double)enrolledProductsCount / totalProducts * 100, 1) 
                : 0;
            
            ViewBag.PendingRetestCount = pendingRetestCount;
            ViewBag.OpenIssuesCount = openIssuesCount;
            ViewBag.EnrolledProductsCount = enrolledProductsCount;
            ViewBag.TotalProducts = totalProducts;
            ViewBag.EnrollmentPercentage = enrollmentPercentage;
            
            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading Design Operations dashboard");
            TempData["ErrorMessage"] = "An error occurred while loading the dashboard. Please try again.";
            return View();
        }
    }

    // GET: DesignOps/AccessibilityOversight
    public async Task<IActionResult> AccessibilityOversight()
    {
        try
        {
            ViewData["Title"] = "Accessibility Issues and Statements - Oversight";
            
            // Get all products from CMS
            var allCmsProducts = await _productsApiService.GetProductsAsync();
            var totalProducts = allCmsProducts?.Count ?? 0;
            
            // Get all enrolled products with full details
            var enrolledProducts = await _context.ProductAccessibilities
                .Include(pa => pa.Issues.Where(i => !i.IsDeleted))
                    .ThenInclude(i => i.WcagCriteriaLinks)
                        .ThenInclude(link => link.WcagCriterion)
                .Where(pa => !pa.IsDeleted && pa.IsActive)
                .ToListAsync();
            
            var enrolledCount = enrolledProducts.Count;
            var notEnrolledCount = totalProducts - enrolledCount;
            
            // Enrollment progress to 100% by 31 March 2026
            var targetDate = new DateTime(2026, 3, 31);
            var daysRemaining = (targetDate - DateTime.UtcNow).Days;
            var enrollmentProgress = totalProducts > 0 ? (double)enrolledCount / totalProducts * 100 : 0;
            var enrollmentTarget = 100.0;
            var productsNeeded = totalProducts - enrolledCount;
            
            // Statement validation status
            var validatedStatements = enrolledProducts.Count(p => p.StatementInstalled && p.VerifiedAt.HasValue);
            var notValidatedStatements = enrolledProducts.Count(p => !p.StatementInstalled || !p.VerifiedAt.HasValue);
            
            // Issues by WCAG criteria
            var allIssues = enrolledProducts
                .SelectMany(p => p.Issues.Where(i => !i.IsDeleted && i.Status != "resolved" && i.Status != "wont_fix"))
                .ToList();
            
            var issuesByWcagCriteria = allIssues
                .Where(i => i.WcagCriteriaLinks.Any())
                .SelectMany(i => i.WcagCriteriaLinks)
                .GroupBy(link => link.WcagCriterion.Criterion)
                .Select(g => new
                {
                    Criterion = g.Key,
                    Count = g.Count(),
                    Level = g.First().WcagCriterion.Level,
                    Version = g.First().WcagCriterion.Version
                })
                .OrderByDescending(x => x.Count)
                .ToList();
            
            // Retest requests
            var retestRequests = await _context.AccessibilityRetestRequests
                .Include(rr => rr.AccessibilityIssue)
                    .ThenInclude(i => i.ProductAccessibility)
                .Where(rr => !rr.IsCompleted.HasValue)
                .OrderBy(rr => rr.RequestedAt)
                .ToListAsync();
            
            // Issues with upcoming due dates (next 30 days)
            var upcomingDueDate = DateTime.UtcNow.AddDays(30);
            var issuesWithUpcomingDueDates = allIssues
                .Where(i => i.PlannedResolutionDate.HasValue && 
                           i.PlannedResolutionDate.Value >= DateTime.UtcNow.Date &&
                           i.PlannedResolutionDate.Value <= upcomingDueDate &&
                           i.Status != "resolved")
                .OrderBy(i => i.PlannedResolutionDate)
                .ToList();
            
            // Issues not being resolved (status is open but no planned resolution date or past due)
            var issuesNotBeingResolved = allIssues
                .Where(i => i.Status == "open" && 
                           (!i.PlannedResolutionDate.HasValue || 
                            i.PlannedResolutionDate.Value < DateTime.UtcNow.Date))
                .ToList();
            
            // Business area risk analysis
            var businessAreaRisk = new List<dynamic>();
            if (allCmsProducts != null)
            {
                var businessAreaGroups = allCmsProducts
                    .GroupBy(p => p.CategoryValues?
                        .FirstOrDefault(cv => cv.CategoryType?.Name?.Equals("Business area", StringComparison.OrdinalIgnoreCase) == true)
                        ?.Name ?? "Unknown")
                    .ToList();
                
                foreach (var group in businessAreaGroups)
                {
                    var businessArea = group.Key;
                    var productsInArea = group.ToList();
                    var enrolledInArea = productsInArea
                        .Where(p => enrolledProducts.Any(ep => ep.FipsId == p.FipsId))
                        .ToList();
                    var notEnrolledInArea = productsInArea.Count - enrolledInArea.Count;
                    
                    var enrolledProductAccessibilities = enrolledProducts
                        .Where(ep => enrolledInArea.Any(p => p.FipsId == ep.FipsId))
                        .ToList();
                    
                    var totalIssuesInArea = enrolledProductAccessibilities
                        .Sum(ep => ep.Issues.Count(i => !i.IsDeleted && i.Status != "resolved" && i.Status != "wont_fix"));
                    
                    var overdueIssuesInArea = enrolledProductAccessibilities
                        .Sum(ep => ep.Issues.Count(i => 
                            !i.IsDeleted && 
                            i.Status != "resolved" && 
                            i.Status != "wont_fix" &&
                            i.PlannedResolutionDate.HasValue &&
                            i.PlannedResolutionDate.Value < DateTime.UtcNow.Date));
                    
                    var enrollmentRate = productsInArea.Count > 0 
                        ? (double)enrolledInArea.Count / productsInArea.Count * 100 
                        : 0;
                    
                    // Calculate risk level
                    var riskLevel = "low";
                    var riskScore = 0;
                    
                    if (notEnrolledInArea > 0) riskScore += 2;
                    if (totalIssuesInArea > 10) riskScore += 2;
                    else if (totalIssuesInArea > 5) riskScore += 1;
                    if (overdueIssuesInArea > 5) riskScore += 2;
                    else if (overdueIssuesInArea > 0) riskScore += 1;
                    if (enrollmentRate < 50) riskScore += 2;
                    else if (enrollmentRate < 75) riskScore += 1;
                    
                    if (riskScore >= 5) riskLevel = "high";
                    else if (riskScore >= 3) riskLevel = "medium";
                    
                    businessAreaRisk.Add(new
                    {
                        BusinessArea = businessArea,
                        TotalProducts = productsInArea.Count,
                        EnrolledProducts = enrolledInArea.Count,
                        NotEnrolledProducts = notEnrolledInArea,
                        EnrollmentRate = enrollmentRate,
                        TotalIssues = totalIssuesInArea,
                        OverdueIssues = overdueIssuesInArea,
                        RiskLevel = riskLevel,
                        RiskScore = riskScore
                    });
                }
                
                businessAreaRisk = businessAreaRisk
                    .OrderByDescending(ba => ((dynamic)ba).RiskScore)
                    .ThenByDescending(ba => ((dynamic)ba).NotEnrolledProducts)
                    .ThenByDescending(ba => ((dynamic)ba).TotalIssues)
                    .ToList();
            }
            
            // Products with validated statements
            var productsWithValidatedStatements = enrolledProducts
                .Where(p => p.StatementInstalled && p.VerifiedAt.HasValue)
                .Select(p => new { FipsId = p.FipsId, ProductName = p.ProductName, VerifiedAt = p.VerifiedAt })
                .ToList();
            
            // Products without validated statements
            var productsWithoutValidatedStatements = enrolledProducts
                .Where(p => !p.StatementInstalled || !p.VerifiedAt.HasValue)
                .Select(p => new { FipsId = p.FipsId, ProductName = p.ProductName, StatementInstalled = p.StatementInstalled })
                .ToList();
            
            ViewBag.TotalProducts = totalProducts;
            ViewBag.EnrolledCount = enrolledCount;
            ViewBag.NotEnrolledCount = notEnrolledCount;
            ViewBag.EnrollmentProgress = enrollmentProgress;
            ViewBag.EnrollmentTarget = enrollmentTarget;
            ViewBag.TargetDate = targetDate;
            ViewBag.DaysRemaining = daysRemaining;
            ViewBag.ProductsNeeded = productsNeeded;
            
            ViewBag.ValidatedStatements = validatedStatements;
            ViewBag.NotValidatedStatements = notValidatedStatements;
            
            ViewBag.IssuesByWcagCriteria = issuesByWcagCriteria;
            ViewBag.TotalOpenIssues = allIssues.Count;
            
            ViewBag.RetestRequests = retestRequests;
            ViewBag.RetestRequestsCount = retestRequests.Count;
            
            ViewBag.IssuesWithUpcomingDueDates = issuesWithUpcomingDueDates;
            ViewBag.IssuesWithUpcomingDueDatesCount = issuesWithUpcomingDueDates.Count;
            
            ViewBag.IssuesNotBeingResolved = issuesNotBeingResolved;
            ViewBag.IssuesNotBeingResolvedCount = issuesNotBeingResolved.Count;
            
            ViewBag.BusinessAreaRisk = businessAreaRisk;
            
            ViewBag.ProductsWithValidatedStatements = productsWithValidatedStatements;
            ViewBag.ProductsWithoutValidatedStatements = productsWithoutValidatedStatements;
            
            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading Accessibility Oversight dashboard");
            TempData["ErrorMessage"] = "An error occurred while loading the accessibility oversight dashboard. Please try again.";
            return View();
        }
    }

    // GET: DesignOps/AccessibilityOversight/RetestRequests
    public async Task<IActionResult> RetestRequests()
    {
        try
        {
            ViewData["Title"] = "Accessibility Retest Requests - Oversight";
            
            var retestRequests = await _context.AccessibilityRetestRequests
                .Include(rr => rr.AccessibilityIssue)
                    .ThenInclude(i => i.ProductAccessibility)
                .Include(rr => rr.AccessibilityIssue)
                    .ThenInclude(i => i.WcagCriteriaLinks)
                        .ThenInclude(link => link.WcagCriterion)
                .Where(rr => !rr.IsCompleted.HasValue)
                .OrderBy(rr => rr.RequestedAt)
                .ToListAsync();
            
            ViewBag.RetestRequests = retestRequests;
            ViewBag.RetestRequestsCount = retestRequests.Count;
            
            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading Retest Requests");
            TempData["ErrorMessage"] = "An error occurred while loading retest requests. Please try again.";
            return View();
        }
    }

    // GET: DesignOps/AccessibilityOversight/OpenIssues
    public async Task<IActionResult> OpenIssues()
    {
        try
        {
            ViewData["Title"] = "Open Accessibility Issues - Oversight";
            
            var enrolledProducts = await _context.ProductAccessibilities
                .Include(pa => pa.Issues.Where(i => !i.IsDeleted))
                    .ThenInclude(i => i.WcagCriteriaLinks)
                        .ThenInclude(link => link.WcagCriterion)
                .Where(pa => !pa.IsDeleted && pa.IsActive)
                .ToListAsync();
            
            var openIssues = enrolledProducts
                .SelectMany(p => p.Issues.Where(i => !i.IsDeleted && i.Status != "resolved" && i.Status != "wont_fix"))
                .OrderByDescending(i => i.IdentifiedDate)
                .ToList();
            
            ViewBag.OpenIssues = openIssues;
            ViewBag.OpenIssuesCount = openIssues.Count;
            
            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading Open Issues");
            TempData["ErrorMessage"] = "An error occurred while loading open issues. Please try again.";
            return View();
        }
    }

    // GET: DesignOps/AccessibilityOversight/ValidatedStatements
    public async Task<IActionResult> ValidatedStatements()
    {
        try
        {
            ViewData["Title"] = "Validated Accessibility Statements - Oversight";
            
            var enrolledProducts = await _context.ProductAccessibilities
                .Where(pa => !pa.IsDeleted && pa.IsActive && pa.StatementInstalled && pa.VerifiedAt.HasValue)
                .OrderBy(pa => pa.ProductName)
                .ToListAsync();
            
            ViewBag.ValidatedStatements = enrolledProducts;
            ViewBag.ValidatedStatementsCount = enrolledProducts.Count;
            
            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading Validated Statements");
            TempData["ErrorMessage"] = "An error occurred while loading validated statements. Please try again.";
            return View();
        }
    }

    // GET: DesignOps/AccessDenied
    public IActionResult AccessDenied()
    {
        ViewData["Title"] = "Access Denied";
        return View();
    }
}

