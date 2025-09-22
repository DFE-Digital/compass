using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FipsReporting.Services;
using FipsReporting.Models;
using FipsReporting.Data;
using Microsoft.EntityFrameworkCore;

namespace FipsReporting.Controllers.Admin
{
    [Authorize]
    public class AdminPerformanceMetricsController : BaseController
    {
        private readonly IPerformanceMetricService _performanceMetricService;
        private readonly CmsApiService _cmsApiService;
        private readonly IReportingService _reportingService;
        private readonly ReportingDbContext _context;
        private readonly ILogger<AdminPerformanceMetricsController> _logger;

        public AdminPerformanceMetricsController(
            IPerformanceMetricService performanceMetricService,
            CmsApiService cmsApiService,
            IReportingService reportingService,
            ReportingDbContext context,
            ILogger<AdminPerformanceMetricsController> logger)
        {
            _performanceMetricService = performanceMetricService;
            _cmsApiService = cmsApiService;
            _reportingService = reportingService;
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                ViewData["ActiveNav"] = "admin";
                ViewData["ActiveNavItem"] = "manage-metrics";

                // Get all performance metrics
                var metrics = await _context.PerformanceMetrics
                    .Where(pm => pm.Enabled)
                    .OrderBy(pm => pm.Name)
                    .ToListAsync();

                // Get all products/services
                var productsResponse = await _cmsApiService.GetProductsAsync(new ProductFilter());
                var products = productsResponse.Data;

                // Calculate summary statistics
                var totalMetrics = metrics.Count;
                var metricsOnTarget = metrics.Count(m => m.Category == "On Target");
                var metricsAtRisk = metrics.Count(m => m.Category == "At Risk");
                var metricsOffTarget = metrics.Count(m => m.Category == "Off Target");

                // Build metric summaries
                var metricSummaries = metrics.Select(m => new PerformanceMetricSummary
                {
                    Id = m.Id,
                    Name = m.Name,
                    Description = m.Description ?? "",
                    FipsId = m.UniqueId,
                    ServiceName = "All Services", // This would need to be linked to specific services
                    Target = m.Measure,
                    CurrentValue = "85%", // This would come from actual reporting data
                    ProgressPercentage = 85m, // This would be calculated from actual data
                    RagStatus = GetRagStatusFromCategory(m.Category),
                    LastUpdated = m.UpdatedAt,
                    Category = m.Category,
                    Measure = m.Measure,
                    IsMandatory = m.Mandatory,
                    ValidationCriteria = m.ValidationCriteria ?? "",
                    Mandate = m.Mandate,
                    // Parse ApplicablePhases JSON to determine stage flags for backward compatibility
                    StageD = !string.IsNullOrEmpty(m.ApplicablePhases) && m.ApplicablePhases.Contains("Discovery"),
                    StageA = !string.IsNullOrEmpty(m.ApplicablePhases) && m.ApplicablePhases.Contains("Alpha"),
                    StageB = !string.IsNullOrEmpty(m.ApplicablePhases) && m.ApplicablePhases.Contains("Beta"),
                    StageL = !string.IsNullOrEmpty(m.ApplicablePhases) && m.ApplicablePhases.Contains("Live"),
                    StageR = !string.IsNullOrEmpty(m.ApplicablePhases) && m.ApplicablePhases.Contains("Retired")
                }).ToList();

                // Generate recent activity (mock data for now)
                var recentActivity = new List<ActivityItem>
                {
                    new ActivityItem
                    {
                        Type = "Update",
                        Title = "Performance metrics updated",
                        Description = "Monthly performance data has been updated for all services",
                        Timestamp = DateTime.Now.AddHours(-2),
                        ServiceName = "All Services",
                        FipsId = "ALL"
                    },
                    new ActivityItem
                    {
                        Type = "Alert",
                        Title = "Metrics off target",
                        Description = "3 services have metrics that are off target",
                        Timestamp = DateTime.Now.AddHours(-6),
                        ServiceName = "Multiple Services",
                        FipsId = "MULTIPLE"
                    },
                    new ActivityItem
                    {
                        Type = "Info",
                        Title = "New metric added",
                        Description = "Accessibility compliance metric added to reporting",
                        Timestamp = DateTime.Now.AddDays(-1),
                        ServiceName = "All Services",
                        FipsId = "ALL"
                    }
                };

                var viewModel = new AdminPerformanceMetricsViewModel
                {
                    TotalMetrics = totalMetrics,
                    MetricsOnTarget = metricsOnTarget,
                    MetricsAtRisk = metricsAtRisk,
                    MetricsOffTarget = metricsOffTarget,
                    TotalServices = products.Count(),
                    Metrics = metricSummaries,
                    RecentActivity = recentActivity
                };

                return View("~/Views/Admin/PerformanceMetrics/Index.cshtml", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading performance metrics admin dashboard");
                TempData["Error"] = "Error loading performance metrics dashboard. Please try again.";
                return View("~/Views/Admin/PerformanceMetrics/Index.cshtml", new AdminPerformanceMetricsViewModel());
            }
        }

        public async Task<IActionResult> Dashboard()
        {
            try
            {
                ViewData["ActiveNav"] = "admin";
                ViewData["ActiveNavItem"] = "manage-metrics";

                // Get comprehensive dashboard data
                var metrics = await _context.PerformanceMetrics
                    .Where(pm => pm.Enabled)
                    .ToListAsync();

                var productsResponse = await _cmsApiService.GetProductsAsync(new ProductFilter());
                var products = productsResponse.Data;

                var viewModel = new PerformanceMetricsDashboardViewModel
                {
                    Overview = new MetricsOverview
                    {
                        TotalMetrics = metrics.Count,
                        MetricsOnTarget = metrics.Count(m => GetRagStatusFromCategory(m.Category) == "Green"),
                        MetricsAtRisk = metrics.Count(m => GetRagStatusFromCategory(m.Category) == "Amber"),
                        MetricsOffTarget = metrics.Count(m => GetRagStatusFromCategory(m.Category) == "Red"),
                        TotalServices = products.Count(),
                        ServicesWithIssues = 5, // Mock data
                        OverallCompliancePercentage = 73m,
                        LastUpdated = DateTime.Now
                    },
                    ServiceSummaries = products.Take(10).Select(p => new ServiceMetricsSummary
                    {
                        FipsId = p.FipsId,
                        ServiceName = p.Title,
                        TotalMetrics = metrics.Count,
                        MetricsOnTarget = metrics.Count(m => GetRagStatusFromCategory(m.Category) == "Green"),
                        MetricsAtRisk = metrics.Count(m => GetRagStatusFromCategory(m.Category) == "Amber"),
                        MetricsOffTarget = metrics.Count(m => GetRagStatusFromCategory(m.Category) == "Red"),
                        CompliancePercentage = 85m,
                        OverallRagStatus = "Green",
                        LastUpdated = DateTime.Now,
                        Categories = metrics.Select(m => m.Category).Distinct().ToList()
                    }).ToList(),
                    CategorySummaries = metrics.GroupBy(m => m.Category)
                        .Select(g => new MetricsCategorySummary
                        {
                            Category = g.Key,
                            TotalMetrics = g.Count(),
                            MetricsOnTarget = g.Count(m => GetRagStatusFromCategory(m.Category) == "Green"),
                            MetricsAtRisk = g.Count(m => GetRagStatusFromCategory(m.Category) == "Amber"),
                            MetricsOffTarget = g.Count(m => GetRagStatusFromCategory(m.Category) == "Red"),
                            CompletionPercentage = 80m,
                            RagStatus = "Green",
                            Services = products.Take(3).Select(p => p.Title).ToList()
                        }).ToList(),
                    TrendingMetrics = new List<TrendingMetric>
                    {
                        new TrendingMetric
                        {
                            Name = "User Satisfaction",
                            ServiceName = "Example Service",
                            Trend = "Improving",
                            ChangePercentage = 5.2m,
                            CurrentValue = "4.2/5",
                            PreviousValue = "4.0/5",
                            LastUpdated = DateTime.Now.AddDays(-1)
                        }
                    },
                    ActiveAlerts = new List<AlertItem>
                    {
                        new AlertItem
                        {
                            Type = "Warning",
                            Title = "Performance degradation detected",
                            Description = "Response times have increased by 15% in the last week",
                            ServiceName = "Example Service",
                            FipsId = "EXAMPLE-001",
                            CreatedAt = DateTime.Now.AddHours(-3),
                            Priority = "High"
                        }
                    }
                };

                return View("~/Views/Admin/PerformanceMetrics/Create.cshtml", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading performance metrics dashboard");
                TempData["Error"] = "Error loading dashboard. Please try again.";
                return RedirectToAction("Index");
            }
        }

        public async Task<IActionResult> Analytics()
        {
            try
            {
            ViewData["ActiveNav"] = "admin";
                ViewData["ActiveNavItem"] = "manage-metrics";

                var endDate = DateTime.Now;
                var startDate = endDate.AddMonths(-6);

                var viewModel = new MetricsAnalyticsViewModel
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    TrendData = GenerateMockTrendData(startDate, endDate),
                    ServiceComparisons = new List<ServicePerformanceComparison>(),
                    CategoryAnalysis = new List<CategoryPerformanceAnalysis>(),
                    ComplianceHistory = GenerateMockComplianceHistory(startDate, endDate)
                };

                return View("~/Views/Admin/PerformanceMetrics/Create.cshtml", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading analytics");
                TempData["Error"] = "Error loading analytics. Please try again.";
                return RedirectToAction("Index");
            }
        }

        public async Task<IActionResult> Create()
        {
            try
            {
            ViewData["ActiveNav"] = "admin";
                ViewData["ActiveNavItem"] = "manage-metrics";

                var metric = new Data.PerformanceMetric();
                return View("~/Views/Admin/PerformanceMetrics/Create.cshtml", metric);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create metric form");
                TempData["Error"] = "Error loading form. Please try again.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Data.PerformanceMetric metric)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    metric.CreatedBy = GetUserEmail();
                    metric.CreatedAt = DateTime.Now;
                    metric.UpdatedBy = GetUserEmail();
                    metric.UpdatedAt = DateTime.Now;

                    _context.PerformanceMetrics.Add(metric);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = $"Performance metric '{metric.Name}' created successfully.";
                    return RedirectToAction("Index");
                }

                ViewData["ActiveNav"] = "admin";
                ViewData["ActiveNavItem"] = "manage-metrics";
                return View("~/Views/Admin/PerformanceMetrics/Create.cshtml", metric);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating performance metric");
                TempData["Error"] = "Error creating performance metric. Please try again.";
                return View("~/Views/Admin/PerformanceMetrics/Create.cshtml", metric);
            }
        }

        public async Task<IActionResult> Edit(int id)
        {
            try
            {
            ViewData["ActiveNav"] = "admin";
                ViewData["ActiveNavItem"] = "manage-metrics";

                var metric = await _context.PerformanceMetrics.FindAsync(id);
            if (metric == null)
            {
                    TempData["Error"] = "Performance metric not found.";
                    return RedirectToAction("Index");
                }

                return View("~/Views/Admin/PerformanceMetrics/Edit.cshtml", metric);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit metric form");
                TempData["Error"] = "Error loading form. Please try again.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Data.PerformanceMetric metric)
        {
            try
            {
                if (id != metric.Id)
                {
                    return NotFound();
                }

                if (ModelState.IsValid)
                {
                    metric.UpdatedBy = GetUserEmail();
                    metric.UpdatedAt = DateTime.Now;

                    _context.Update(metric);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = $"Performance metric '{metric.Name}' updated successfully.";
                    return RedirectToAction("Index");
                }

                ViewData["ActiveNav"] = "admin";
                ViewData["ActiveNavItem"] = "manage-metrics";
                return View("~/Views/Admin/PerformanceMetrics/Edit.cshtml", metric);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating performance metric");
                TempData["Error"] = "Error updating performance metric. Please try again.";
                return View("~/Views/Admin/PerformanceMetrics/Create.cshtml", metric);
            }
        }

        public async Task<IActionResult> Details(int id)
        {
            try
            {
                ViewData["ActiveNav"] = "admin";
                ViewData["ActiveNavItem"] = "manage-metrics";

                var metric = await _context.PerformanceMetrics.FindAsync(id);
                if (metric == null)
                {
                    TempData["Error"] = "Performance metric not found.";
                    return RedirectToAction("Index");
                }

                // Get phases from CMS API
                var phases = await _performanceMetricService.GetPhasesFromCmsAsync();
                ViewBag.Phases = phases;

                return View("~/Views/Admin/PerformanceMetrics/Details.cshtml", metric);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading metric details");
                TempData["Error"] = "Error loading metric details. Please try again.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Enable(int id)
        {
            try
            {
                var metric = await _context.PerformanceMetrics.FindAsync(id);
                if (metric == null)
                {
                    TempData["Error"] = "Performance metric not found.";
                    return RedirectToAction("Index");
                }

                metric.Enabled = true;
                metric.UpdatedBy = GetUserEmail();
                metric.UpdatedAt = DateTime.Now;

                _context.Update(metric);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Performance metric '{metric.Name}' enabled successfully.";
                return RedirectToAction("Details", new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enabling performance metric");
                TempData["Error"] = "Error enabling performance metric. Please try again.";
                return RedirectToAction("Details", new { id });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Disable(int id)
        {
            try
            {
                var metric = await _context.PerformanceMetrics.FindAsync(id);
                if (metric == null)
                {
                    TempData["Error"] = "Performance metric not found.";
                    return RedirectToAction("Index");
                }

                metric.Enabled = false;
                metric.UpdatedBy = GetUserEmail();
                metric.UpdatedAt = DateTime.Now;

                _context.Update(metric);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Performance metric '{metric.Name}' disabled successfully.";
                return RedirectToAction("Details", new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disabling performance metric");
                TempData["Error"] = "Error disabling performance metric. Please try again.";
                return RedirectToAction("Details", new { id });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var metric = await _context.PerformanceMetrics.FindAsync(id);
                if (metric == null)
                {
                    TempData["Error"] = "Performance metric not found.";
                    return RedirectToAction("Index");
                }

                _context.PerformanceMetrics.Remove(metric);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Performance metric '{metric.Name}' deleted successfully.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting performance metric");
                TempData["Error"] = "Error deleting performance metric. Please try again.";
                return RedirectToAction("Index");
            }
        }

        public async Task<IActionResult> Export()
        {
            try
            {
                var metrics = await _context.PerformanceMetrics
                    .Where(pm => pm.Enabled)
                    .OrderBy(pm => pm.Name)
                    .ToListAsync();

                var csv = GenerateMetricsCsv(metrics);
                var fileName = $"performance_metrics_{DateTime.Now:yyyyMMdd}.csv";

                return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting performance metrics");
                TempData["Error"] = "Error exporting data. Please try again.";
                return RedirectToAction("Index");
            }
        }

        private string GetRagStatusFromCategory(string category)
        {
            return category switch
            {
                "On Target" => "Green",
                "At Risk" => "Amber",
                "Off Target" => "Red",
                _ => "Amber"
            };
        }

        private List<MetricsTrendData> GenerateMockTrendData(DateTime startDate, DateTime endDate)
        {
            var data = new List<MetricsTrendData>();
            var currentDate = startDate;

            while (currentDate <= endDate)
            {
                data.Add(new MetricsTrendData
                {
                    Date = currentDate,
                    TotalMetrics = 89,
                    MetricsOnTarget = (int)(89 * (0.7 + (currentDate - startDate).TotalDays / (endDate - startDate).TotalDays * 0.1)),
                    MetricsAtRisk = 8,
                    MetricsOffTarget = 3,
                    OverallCompliancePercentage = 70m + (decimal)((currentDate - startDate).TotalDays / (endDate - startDate).TotalDays * 15)
                });
                currentDate = currentDate.AddDays(7);
            }

            return data;
        }

        private List<ComplianceScoreHistory> GenerateMockComplianceHistory(DateTime startDate, DateTime endDate)
        {
            var data = new List<ComplianceScoreHistory>();
            var currentDate = startDate;

            while (currentDate <= endDate)
            {
                data.Add(new ComplianceScoreHistory
                {
                    Date = currentDate,
                    OverallScore = 75m + (decimal)((currentDate - startDate).TotalDays / (endDate - startDate).TotalDays * 15),
                    AccessibilityScore = 80m + (decimal)((currentDate - startDate).TotalDays / (endDate - startDate).TotalDays * 10),
                    PerformanceScore = 70m + (decimal)((currentDate - startDate).TotalDays / (endDate - startDate).TotalDays * 20),
                    SecurityScore = 85m + (decimal)((currentDate - startDate).TotalDays / (endDate - startDate).TotalDays * 10),
                    UserSatisfactionScore = 78m + (decimal)((currentDate - startDate).TotalDays / (endDate - startDate).TotalDays * 12)
                });
                currentDate = currentDate.AddDays(7);
            }

            return data;
        }

        private string GenerateMetricsCsv(List<Data.PerformanceMetric> metrics)
        {
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("Name,Description,Category,Measure,Mandatory,Validation Criteria,Created By,Created At,Updated By,Updated At");

            foreach (var metric in metrics)
            {
                csv.AppendLine($"\"{metric.Name}\",\"{metric.Description}\",\"{metric.Category}\",\"{metric.Measure}\",\"{metric.Mandatory}\",\"{metric.ValidationCriteria}\",\"{metric.CreatedBy}\",\"{metric.CreatedAt:yyyy-MM-dd}\",\"{metric.UpdatedBy}\",\"{metric.UpdatedAt:yyyy-MM-dd}\"");
            }

            return csv.ToString();
        }
    }
}