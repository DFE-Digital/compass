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

    public DesignOpsController(ILogger<DesignOpsController> logger, CompassDbContext context, IProductsApiService productsApiService)
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
            
            // Get products with null perf-1 (satisfaction score) from latest returns
            var perf1Metric = await _context.PerformanceMetrics
                .FirstOrDefaultAsync(pm => pm.Identifier == "perf-1");
            
            var productsWithNullPerf1 = new List<object>();
            if (perf1Metric != null)
            {
                // Get all returns with perf-1 metric values
                var allReturns = await _context.ProductReturns
                    .Include(pr => pr.MetricValues.Where(mv => mv.PerformanceMetricId == perf1Metric.Id))
                    .Where(pr => pr.MetricValues.Any(mv => mv.PerformanceMetricId == perf1Metric.Id))
                    .ToListAsync();
                
                // Get latest return for each product
                var latestReturns = allReturns
                    .GroupBy(pr => pr.FipsId)
                    .Select(g => g.OrderByDescending(pr => pr.Year)
                        .ThenByDescending(pr => pr.Month)
                        .First())
                    .ToList();
                
                var nullPerf1Returns = latestReturns
                    .Where(pr => pr.MetricValues.Any(mv => 
                        mv.PerformanceMetricId == perf1Metric.Id && 
                        (string.IsNullOrWhiteSpace(mv.Value) || mv.IsNotCaptured)))
                    .Select(pr => new
                    {
                        FipsId = pr.FipsId,
                        ProductName = cmsProducts?.FirstOrDefault(p => p.FipsId == pr.FipsId)?.Title ?? "Unknown"
                    })
                    .ToList();
                
                productsWithNullPerf1 = nullPerf1Returns.Cast<object>().ToList();
            }
            
            // Get products with 0 open accessibility issues (perf-8) from latest returns
            var perf8Metric = await _context.PerformanceMetrics
                .FirstOrDefaultAsync(pm => pm.Identifier == "perf-8");
            
            var productsWithZeroIssues = new List<object>();
            if (perf8Metric != null)
            {
                // Get all returns with perf-8 metric values
                var allReturnsForPerf8 = await _context.ProductReturns
                    .Include(pr => pr.MetricValues.Where(mv => mv.PerformanceMetricId == perf8Metric.Id))
                    .Where(pr => pr.MetricValues.Any(mv => mv.PerformanceMetricId == perf8Metric.Id))
                    .ToListAsync();
                
                // Get latest return for each product
                var latestReturnsForPerf8 = allReturnsForPerf8
                    .GroupBy(pr => pr.FipsId)
                    .Select(g => g.OrderByDescending(pr => pr.Year)
                        .ThenByDescending(pr => pr.Month)
                        .First())
                    .ToList();
                
                var zeroIssuesReturns = latestReturnsForPerf8
                    .Where(pr => pr.MetricValues.Any(mv => 
                        mv.PerformanceMetricId == perf8Metric.Id && 
                        mv.Value == "0"))
                    .Select(pr => new
                    {
                        FipsId = pr.FipsId,
                        ProductName = cmsProducts?.FirstOrDefault(p => p.FipsId == pr.FipsId)?.Title ?? "Unknown"
                    })
                    .ToList();
                
                productsWithZeroIssues = zeroIssuesReturns.Cast<object>().ToList();
            }
            
            ViewBag.PendingRetestCount = pendingRetestCount;
            ViewBag.OpenIssuesCount = openIssuesCount;
            ViewBag.EnrolledProductsCount = enrolledProductsCount;
            ViewBag.TotalProducts = totalProducts;
            ViewBag.EnrollmentPercentage = enrollmentPercentage;
            ViewBag.ProductsWithNullPerf1 = productsWithNullPerf1;
            ViewBag.ProductsWithZeroIssues = productsWithZeroIssues;
            
            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading Design Operations dashboard");
            TempData["ErrorMessage"] = "An error occurred while loading the dashboard. Please try again.";
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

