using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FipsReporting.Services;
using FipsReporting.Models;
using FipsReporting.Data;
using System.Globalization;

namespace FipsReporting.Controllers
{
    public class DashboardController : BaseController
    {
        private readonly IMetricsService _metricsService;
        private readonly IMilestoneService _milestoneService;
        private readonly IReportingService _reportingService;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(IMetricsService metricsService, IMilestoneService milestoneService, IReportingService reportingService, ILogger<DashboardController> logger)
        {
            _metricsService = metricsService;
            _milestoneService = milestoneService;
            _reportingService = reportingService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var userId = User.Identity?.Name ?? "";
                var currentDate = DateTime.UtcNow;
                
                // Get user's products
                var products = await _reportingService.GetProductsForUserAsync(userId, new ProductFilter());
                
                // Get reporting cycles
                var reportingCycles = await GetReportingCyclesAsync(currentDate);
                
                // Get metrics history for charts
                var metricsHistory = await GetMetricsHistoryAsync(userId);
                
                // Get milestones overview
                var milestonesOverview = await GetMilestonesOverviewAsync(products.Select(p => p.FipsId).ToList());
                
                // Get due/overdue reports
                var reportsStatus = await GetReportsStatusAsync(userId, currentDate);

                var viewModel = new DashboardViewModel
                {
                    CurrentDate = currentDate,
                    Products = products,
                    ReportingCycles = reportingCycles,
                    MetricsHistory = metricsHistory,
                    MilestonesOverview = milestonesOverview,
                    ReportsStatus = reportsStatus,
                    UserId = userId
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard for user: {UserId}", User.Identity?.Name);
                TempData["Error"] = "Unable to load dashboard. Please try again.";
                return RedirectToAction("Index", "Home");
            }
        }

        private async Task<List<ReportingCycle>> GetReportingCyclesAsync(DateTime currentDate)
        {
            var cycles = new List<ReportingCycle>();
            
            // Generate cycles for current year and next year
            for (int year = currentDate.Year; year <= currentDate.Year + 1; year++)
            {
                for (int month = 1; month <= 12; month++)
                {
                    var cycleStart = new DateTime(year, month, 1);
                    var cycleEnd = cycleStart.AddMonths(1).AddDays(-1);
                    var dueDate = cycleStart.AddMonths(1).AddDays(5); // 5 days grace period
                    
                    var cycle = new ReportingCycle
                    {
                        Year = year,
                        Month = month,
                        MonthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month),
                        CycleStart = cycleStart,
                        CycleEnd = cycleEnd,
                        DueDate = dueDate,
                        Status = GetCycleStatus(cycleStart, cycleEnd, dueDate, currentDate)
                    };
                    
                    cycles.Add(cycle);
                }
            }
            
            return cycles.OrderBy(c => c.CycleStart).ToList();
        }

        private ReportingCycleStatus GetCycleStatus(DateTime cycleStart, DateTime cycleEnd, DateTime dueDate, DateTime currentDate)
        {
            if (currentDate < cycleStart)
                return ReportingCycleStatus.Upcoming;
            else if (currentDate >= cycleStart && currentDate <= cycleEnd)
                return ReportingCycleStatus.Current;
            else if (currentDate > cycleEnd && currentDate <= dueDate)
                return ReportingCycleStatus.Due;
            else if (currentDate > dueDate)
                return ReportingCycleStatus.Overdue;
            else
                return ReportingCycleStatus.Completed;
        }

        private async Task<List<MetricHistoryData>> GetMetricsHistoryAsync(string userId)
        {
            try
            {
                var userReports = await _metricsService.GetUserReportsAsync(userId);
                var metricsHistory = new List<MetricHistoryData>();
                
                // Group by metric and create history data
                var groupedReports = userReports
                    .Where(r => r.Metric != null && !string.IsNullOrEmpty(r.Value))
                    .GroupBy(r => r.Metric!.Name)
                    .ToList();

                foreach (var group in groupedReports)
                {
                    var metricName = group.Key;
                    var reports = group.OrderBy(r => r.SubmittedAt).ToList();
                    
                    var historyData = new MetricHistoryData
                    {
                        MetricName = metricName,
                        DataPoints = reports.Select(r => new MetricDataPoint
                        {
                            Date = r.SubmittedAt,
                            Value = r.Value,
                            ReportingPeriod = r.ReportingPeriod,
                            ProductId = r.ProductId
                        }).ToList()
                    };
                    
                    metricsHistory.Add(historyData);
                }
                
                return metricsHistory;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting metrics history for user: {UserId}", userId);
                return new List<MetricHistoryData>();
            }
        }

        private async Task<MilestonesOverview> GetMilestonesOverviewAsync(List<string> productIds)
        {
            try
            {
                var allMilestones = new List<FipsReporting.Data.Milestone>();
                
                foreach (var productId in productIds)
                {
                    var milestones = await _milestoneService.GetMilestonesForProductAsync(productId);
                    allMilestones.AddRange(milestones);
                }
                
                var overview = new MilestonesOverview
                {
                    TotalMilestones = allMilestones.Count,
                    CompletedMilestones = allMilestones.Count(m => m.Status == "Completed"),
                    InProgressMilestones = allMilestones.Count(m => m.Status == "In Progress"),
                    OverdueMilestones = allMilestones.Count(m => m.Status == "Overdue"),
                    UpcomingMilestones = allMilestones.Count(m => m.Status == "Not Started"),
                    RecentMilestones = allMilestones
                        .OrderByDescending(m => m.UpdatedAt)
                        .Take(5)
                        .ToList()
                };
                
                return overview;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting milestones overview");
                return new MilestonesOverview();
            }
        }

        private async Task<ReportsStatus> GetReportsStatusAsync(string userId, DateTime currentDate)
        {
            try
            {
                var userReports = await _metricsService.GetUserReportsAsync(userId);
                var activeMetrics = await _metricsService.GetActiveMetricsAsync();
                
                var status = new ReportsStatus
                {
                    TotalMetrics = activeMetrics.Count,
                    SubmittedReports = userReports.Count,
                    PendingReports = activeMetrics.Count - userReports.Count,
                    OverdueReports = 0, // Will be calculated based on reporting cycles
                    DueThisWeek = 0 // Will be calculated based on reporting cycles
                };
                
                return status;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting reports status for user: {UserId}", userId);
                return new ReportsStatus();
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetMetricChartData(string metricName)
        {
            try
            {
                var userId = User.Identity?.Name ?? "";
                var userReports = await _metricsService.GetUserReportsAsync(userId);
                
                var metricReports = userReports
                    .Where(r => r.Metric?.Name == metricName && !string.IsNullOrEmpty(r.Value))
                    .OrderBy(r => r.SubmittedAt)
                    .ToList();
                
                var chartData = new
                {
                    labels = metricReports.Select(r => r.SubmittedAt.ToString("MMM yyyy")).ToArray(),
                    datasets = new[]
                    {
                        new
                        {
                            label = metricName,
                            data = metricReports.Select(r => 
                            {
                                if (double.TryParse(r.Value, out double numericValue))
                                    return numericValue;
                                return 0;
                            }).ToArray(),
                            borderColor = "rgb(29, 112, 184)",
                            backgroundColor = "rgba(29, 112, 184, 0.1)",
                            tension = 0.1
                        }
                    }
                };
                
                return Json(chartData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting chart data for metric: {MetricName}", metricName);
                return Json(new { error = "Unable to load chart data" });
            }
        }
    }
}
