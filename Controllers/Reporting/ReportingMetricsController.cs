using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FipsReporting.Services;
using FipsReporting.Models;
using FipsReporting.Data;

namespace FipsReporting.Controllers.Reporting
{
    public class ReportingMetricsController : BaseController
    {
        private readonly IMetricsService _metricsService;
        private readonly IReportingService _reportingService;
        private readonly ILogger<ReportingMetricsController> _logger;

        public ReportingMetricsController(IMetricsService metricsService, IReportingService reportingService, ILogger<ReportingMetricsController> logger)
        {
            _metricsService = metricsService;
            _reportingService = reportingService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var metrics = await _metricsService.GetActiveMetricsAsync();
                return View(metrics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading metrics for reporting user");
                TempData["Error"] = "Unable to load metrics. Please try again.";
                return RedirectToAction("Index", "Home");
            }
        }

        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var metric = await _metricsService.GetMetricByIdAsync(id);
                if (metric == null)
                {
                    TempData["Error"] = "Metric not found.";
                    return RedirectToAction(nameof(Index));
                }

                return View(metric);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading metric details for ID: {MetricId}", id);
                TempData["Error"] = "Unable to load metric details. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> ReportData(int metricId, string? productId = null)
        {
            try
            {
                var metric = await _metricsService.GetMetricByIdAsync(metricId);
                if (metric == null)
                {
                    TempData["Error"] = "Metric not found.";
                    return RedirectToAction(nameof(Index));
                }

                var viewModel = new MetricReportingViewModel
                {
                    Metric = metric,
                    ProductId = productId
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading metric reporting form for ID: {MetricId}", metricId);
                TempData["Error"] = "Unable to load reporting form. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitReportData(MetricReportingViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View("ReportData", model);
                }

                // Create reporting data entry
                var reportingData = new ReportingData
                {
                    MetricId = model.MetricId,
                    ProductId = model.ProductId,
                    Value = model.Value,
                    ReportingPeriod = model.ReportingPeriod,
                    Comment = model.Comment,
                    SubmittedBy = User.Identity?.Name ?? "Unknown",
                    SubmittedAt = DateTime.UtcNow
                };

                await _metricsService.SubmitReportingDataAsync(reportingData);

                TempData["Success"] = "Reporting data submitted successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting reporting data for metric ID: {MetricId}", model.MetricId);
                TempData["Error"] = "Unable to submit reporting data. Please try again.";
                return View("ReportData", model);
            }
        }

        public async Task<IActionResult> MyReports()
        {
            try
            {
                var userId = User.Identity?.Name ?? "";
                var reports = await _metricsService.GetUserReportsAsync(userId);
                return View(reports);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user reports for: {UserId}", User.Identity?.Name);
                TempData["Error"] = "Unable to load your reports. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
