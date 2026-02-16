using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Compass.Data;
using Compass.Models;
using Compass.Services;
using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json;

namespace Compass.Controllers;

[Authorize]
public class PerformanceMetricController : Controller
{
    private readonly CompassDbContext _context;
    private readonly ILogger<PerformanceMetricController> _logger;
    private readonly IProductsApiService _productsApiService;

    public PerformanceMetricController(
        CompassDbContext context, 
        ILogger<PerformanceMetricController> logger,
        IProductsApiService productsApiService)
    {
        _context = context;
        _logger = logger;
        _productsApiService = productsApiService;
    }

    // GET: PerformanceMetric
    public async Task<IActionResult> Index()
    {
        var metrics = await _context.PerformanceMetrics
            .OrderBy(pm => pm.Identifier)
            .ToListAsync();
        
        return View("~/Views/Admin/PerformanceMetric/Index.cshtml", metrics);
    }

    // GET: PerformanceMetric/Create
    public async Task<IActionResult> Create()
    {
        var metric = new PerformanceMetric
        {
            ValidFromYear = DateTime.UtcNow.Year,
            ValidFromMonth = 10 // Default to October
        };
        metric.ValidationRules = "{\"required\": true, \"allowNull\": false}";
        
        ViewBag.Phases = await _productsApiService.GetPhasesAsync();
        ViewBag.Types = await _productsApiService.GetTypesAsync();
        ViewBag.AvailableMetrics = await _context.PerformanceMetrics
            .Where(m => !m.IsDisabled)
            .OrderBy(m => m.Title)
            .ToListAsync();
        
        return View("~/Views/Admin/PerformanceMetric/Create.cshtml", metric);
    }

    // POST: PerformanceMetric/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PerformanceMetric metric, string validationRulesJson, List<string>? selectedPhases, List<string>? selectedTypes)
    {
        ModelState.Remove(nameof(PerformanceMetric.Identifier));

        if (ModelState.IsValid)
        {
            try
            {
                // Validate JSON structure
                if (!string.IsNullOrEmpty(validationRulesJson))
                {
                    try
                    {
                        JsonConvert.DeserializeObject<ValidationRules>(validationRulesJson);
                        metric.ValidationRules = validationRulesJson;
                    }
                    catch
                    {
                        ModelState.AddModelError("ValidationRules", "Invalid JSON format for validation rules.");
                        metric.ValidationRules = validationRulesJson;
                        ViewBag.Phases = await _productsApiService.GetPhasesAsync();
                        ViewBag.Types = await _productsApiService.GetTypesAsync();
                        ViewBag.AvailableMetrics = await _context.PerformanceMetrics
                            .Where(m => !m.IsDisabled)
                            .OrderBy(m => m.Title)
                            .ToListAsync();
                        return View("~/Views/Admin/PerformanceMetric/Create.cshtml", metric);
                    }
                }
                else
                {
                    metric.ValidationRules = "{}";
                }
                
                // Store selected phases as comma-separated string
                metric.ApplicablePhases = selectedPhases != null && selectedPhases.Any() 
                    ? string.Join(",", selectedPhases) 
                    : string.Empty;
                
                // Store selected types as comma-separated string
                metric.ApplicableTypes = selectedTypes != null && selectedTypes.Any() 
                    ? string.Join(",", selectedTypes) 
                    : string.Empty;
                
                metric.CreatedAt = DateTime.UtcNow;
                metric.UpdatedAt = DateTime.UtcNow;
                metric.Identifier = $"perf-temp-{Guid.NewGuid():N}";
                
                _context.PerformanceMetrics.Add(metric);
                await _context.SaveChangesAsync();

                metric.Identifier = $"perf-{metric.Id}";
                metric.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = $"Performance metric '{metric.Title}' has been created successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating performance metric");
                ModelState.AddModelError("", "An error occurred while creating the performance metric. Please try again.");
            }
        }
        
        ViewBag.Phases = await _productsApiService.GetPhasesAsync();
        ViewBag.Types = await _productsApiService.GetTypesAsync();
        ViewBag.AvailableMetrics = await _context.PerformanceMetrics
            .Where(m => !m.IsDisabled)
            .OrderBy(m => m.Title)
            .ToListAsync();
        
        metric.ValidationRules = !string.IsNullOrWhiteSpace(validationRulesJson)
            ? validationRulesJson
            : (metric.ValidationRules ?? "{}");
        
        return View("~/Views/Admin/PerformanceMetric/Create.cshtml", metric);
    }

    // GET: PerformanceMetric/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var metric = await _context.PerformanceMetrics.FindAsync(id);
        if (metric == null)
        {
            return NotFound();
        }

        ViewBag.Phases = await _productsApiService.GetPhasesAsync();
        ViewBag.Types = await _productsApiService.GetTypesAsync();
        ViewBag.AvailableMetrics = await _context.PerformanceMetrics
            .Where(m => !m.IsDisabled && m.Id != id) // Exclude self to prevent circular dependencies
            .OrderBy(m => m.Title)
            .ToListAsync();
        
        return View("~/Views/Admin/PerformanceMetric/Edit.cshtml", metric);
    }

    // POST: PerformanceMetric/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, PerformanceMetric metric, string validationRulesJson, List<string>? selectedPhases, List<string>? selectedTypes)
    {
        if (id != metric.Id)
        {
            return NotFound();
        }

        ModelState.Remove(nameof(PerformanceMetric.Identifier));

        var existingMetric = await _context.PerformanceMetrics.FindAsync(id);
        if (existingMetric == null)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                var validationRulesToPersist = existingMetric.ValidationRules;

                if (!string.IsNullOrWhiteSpace(validationRulesJson))
                {
                    try
                    {
                        JsonConvert.DeserializeObject<ValidationRules>(validationRulesJson);
                        validationRulesToPersist = validationRulesJson;
                    }
                    catch
                    {
                        ModelState.AddModelError("ValidationRules", "Invalid JSON format for validation rules.");
                    }
                }

                if (ModelState.ErrorCount == 0)
                {
                    existingMetric.ValidationRules = validationRulesToPersist;
                    existingMetric.ApplicablePhases = selectedPhases != null && selectedPhases.Any()
                        ? string.Join(",", selectedPhases)
                        : string.Empty;

                    existingMetric.ApplicableTypes = selectedTypes != null && selectedTypes.Any()
                        ? string.Join(",", selectedTypes)
                        : string.Empty;

                    existingMetric.Title = metric.Title;
                    existingMetric.Description = metric.Description;
                    existingMetric.HintText = metric.HintText;
                    existingMetric.Purpose = metric.Purpose;
                    existingMetric.ValueType = metric.ValueType;
                    existingMetric.ValidFromYear = metric.ValidFromYear;
                    existingMetric.ValidFromMonth = metric.ValidFromMonth;
                    existingMetric.ConditionalOnMetricId = metric.ConditionalOnMetricId;
                    existingMetric.IsDisabled = metric.IsDisabled;
                    existingMetric.UpdatedAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = $"Performance metric '{existingMetric.Title}' has been updated successfully.";
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PerformanceMetricExists(metric.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating performance metric");
                ModelState.AddModelError("", "An error occurred while updating the performance metric. Please try again.");
            }
        }

        ViewBag.Phases = await _productsApiService.GetPhasesAsync();
        ViewBag.Types = await _productsApiService.GetTypesAsync();
        ViewBag.AvailableMetrics = await _context.PerformanceMetrics
            .Where(m => !m.IsDisabled && m.Id != id)
            .OrderBy(m => m.Title)
            .ToListAsync();

        metric.ValidationRules = !string.IsNullOrWhiteSpace(validationRulesJson)
            ? validationRulesJson
            : existingMetric.ValidationRules;
        metric.ApplicablePhases = selectedPhases != null && selectedPhases.Any()
            ? string.Join(",", selectedPhases)
            : existingMetric.ApplicablePhases;
        metric.ApplicableTypes = selectedTypes != null && selectedTypes.Any()
            ? string.Join(",", selectedTypes)
            : existingMetric.ApplicableTypes;
        metric.Identifier = existingMetric.Identifier;

        return View("~/Views/Admin/PerformanceMetric/Edit.cshtml", metric);
    }

    // GET: PerformanceMetric/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var metric = await _context.PerformanceMetrics.FindAsync(id);
        if (metric == null)
        {
            return NotFound();
        }

        return View("~/Views/Admin/PerformanceMetric/Delete.cshtml", metric);
    }

    // POST: PerformanceMetric/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        try
        {
            var metric = await _context.PerformanceMetrics.FindAsync(id);
            if (metric != null)
            {
                _context.PerformanceMetrics.Remove(metric);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = $"Performance metric '{metric.Title}' has been deleted successfully.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting performance metric");
            TempData["ErrorMessage"] = "An error occurred while deleting the performance metric. Please try again.";
        }

        return RedirectToAction(nameof(Index));
    }

    private bool PerformanceMetricExists(int id)
    {
        return _context.PerformanceMetrics.Any(e => e.Id == id);
    }
}

