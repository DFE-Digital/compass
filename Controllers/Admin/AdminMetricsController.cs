using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FipsReporting.Services;
using FipsReporting.Models;
using FipsReporting.Data;
using Microsoft.EntityFrameworkCore;

namespace FipsReporting.Controllers.Admin
{
    [Authorize]
    public class AdminMetricsController : BaseController
    {
        private readonly IPerformanceMetricService _performanceMetricService;
        private readonly CmsApiService _cmsApiService;
        private readonly ReportingDbContext _context;
        private readonly ILogger<AdminMetricsController> _logger;

        public AdminMetricsController(
            IPerformanceMetricService performanceMetricService, 
            CmsApiService cmsApiService,
            ReportingDbContext context,
            ILogger<AdminMetricsController> logger)
        {
            _performanceMetricService = performanceMetricService;
            _cmsApiService = cmsApiService;
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

                return View("~/Views/Admin/Metrics/Index.cshtml", metrics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading metrics");
                TempData["Error"] = "Error loading metrics. Please try again.";
                return View("~/Views/Admin/Metrics/Index.cshtml", new List<PerformanceMetric>());
            }
        }

        public async Task<IActionResult> Create()
        {
            try
            {
                ViewData["ActiveNav"] = "admin";
                ViewData["ActiveNavItem"] = "manage-metrics";

                // Get phases from CMS API
                var phases = await _performanceMetricService.GetPhasesFromCmsAsync();
                ViewBag.Phases = phases;

                var metric = new PerformanceMetric();
                return View("~/Views/Admin/Metrics/Create.cshtml", metric);
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
        public async Task<IActionResult> Create(PerformanceMetric metric, string[] selectedPhases)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    metric.CreatedBy = GetUserEmail();
                    metric.CreatedAt = DateTime.Now;
                    metric.UpdatedBy = GetUserEmail();
                    metric.UpdatedAt = DateTime.Now;

                    // Store selected phases as JSON
                    if (selectedPhases != null && selectedPhases.Length > 0)
                    {
                        metric.ApplicablePhases = System.Text.Json.JsonSerializer.Serialize(selectedPhases);
                    }
                    else
                    {
                        metric.ApplicablePhases = null;
                    }

                    _context.PerformanceMetrics.Add(metric);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = $"Performance metric '{metric.Name}' created successfully.";
                    return RedirectToAction("Index");
                }

                ViewData["ActiveNav"] = "admin";
                ViewData["ActiveNavItem"] = "manage-metrics";
                
                // Get phases from CMS API for form re-display
                var phasesForRedisplay = await _performanceMetricService.GetPhasesFromCmsAsync();
                ViewBag.Phases = phasesForRedisplay;
                
                return View("~/Views/Admin/Metrics/Create.cshtml", metric);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating performance metric");
                TempData["Error"] = "Error creating performance metric. Please try again.";
                
                // Get phases from CMS API for form re-display
                var phasesForRedisplay = await _performanceMetricService.GetPhasesFromCmsAsync();
                ViewBag.Phases = phasesForRedisplay;
                
                return View("~/Views/Admin/Metrics/Create.cshtml", metric);
            }
        }

        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                ViewData["ActiveNav"] = "admin";
                ViewData["ActiveNavItem"] = "manage-metrics";

                // Get phases from CMS API
                var phases = await _performanceMetricService.GetPhasesFromCmsAsync();
                ViewBag.Phases = phases;

                var metric = await _context.PerformanceMetrics.FindAsync(id);
                if (metric == null)
                {
                    TempData["Error"] = "Performance metric not found.";
                    return RedirectToAction("Index");
                }

                // Pre-populate selected phases for editing
                if (!string.IsNullOrEmpty(metric.ApplicablePhases))
                {
                    try
                    {
                        var selectedPhases = System.Text.Json.JsonSerializer.Deserialize<string[]>(metric.ApplicablePhases);
                        ViewBag.SelectedPhases = selectedPhases;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to deserialize ApplicablePhases for metric {Id}", id);
                        ViewBag.SelectedPhases = new string[0];
                    }
                }
                else
                {
                    ViewBag.SelectedPhases = new string[0];
                }

                return View("~/Views/Admin/Metrics/Edit.cshtml", metric);
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
        public async Task<IActionResult> Edit(int id, PerformanceMetric metric, string[] selectedPhases)
        {
            try
            {
                if (id != metric.Id)
                {
                    return NotFound();
                }

                if (ModelState.IsValid)
                {
                    // Debug logging
                    _logger.LogInformation($"Edit: selectedPhases = {string.Join(", ", selectedPhases ?? new string[0])}");
                    
                    metric.UpdatedBy = GetUserEmail();
                    metric.UpdatedAt = DateTime.Now;

                    // Store selected phases as JSON
                    if (selectedPhases != null && selectedPhases.Length > 0)
                    {
                        metric.ApplicablePhases = System.Text.Json.JsonSerializer.Serialize(selectedPhases);
                    }
                    else
                    {
                        metric.ApplicablePhases = null;
                    }
                    
                    _context.Update(metric);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = $"Performance metric '{metric.Name}' updated successfully.";
                    return RedirectToAction("Index");
                }

                ViewData["ActiveNav"] = "admin";
                ViewData["ActiveNavItem"] = "manage-metrics";
                
                // Get phases from CMS API for form re-display
                var phasesForRedisplay = await _performanceMetricService.GetPhasesFromCmsAsync();
                ViewBag.Phases = phasesForRedisplay;
                
                return View("~/Views/Admin/Metrics/Edit.cshtml", metric);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating performance metric");
                TempData["Error"] = "Error updating performance metric. Please try again.";
                
                // Get phases from CMS API for form re-display
                var phasesForRedisplay = await _performanceMetricService.GetPhasesFromCmsAsync();
                ViewBag.Phases = phasesForRedisplay;
                
                return View("~/Views/Admin/Metrics/Edit.cshtml", metric);
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

    }
}
