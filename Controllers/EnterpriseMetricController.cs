using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Compass.Data;
using Compass.Models;
using Newtonsoft.Json;

namespace Compass.Controllers;

public class EnterpriseMetricController : Controller
{
    private readonly CompassDbContext _context;
    private readonly ILogger<EnterpriseMetricController> _logger;

    public EnterpriseMetricController(
        CompassDbContext context, 
        ILogger<EnterpriseMetricController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: EnterpriseMetric
    public async Task<IActionResult> Index()
    {
        var metrics = await _context.EnterpriseMetrics
            .OrderBy(em => em.Identifier)
            .ToListAsync();
        
        return View("~/Views/Admin/EnterpriseMetric/Index.cshtml", metrics);
    }

    // GET: EnterpriseMetric/Create
    public IActionResult Create()
    {
        var metric = new EnterpriseMetric
        {
            ValidFromYear = DateTime.UtcNow.Year,
            ValidFromMonth = 9 // Default to September
        };
        
        return View("~/Views/Admin/EnterpriseMetric/Create.cshtml", metric);
    }

    // POST: EnterpriseMetric/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EnterpriseMetric metric, string validationRulesJson)
    {
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
                        return View("~/Views/Admin/EnterpriseMetric/Create.cshtml", metric);
                    }
                }
                
                metric.CreatedAt = DateTime.UtcNow;
                metric.UpdatedAt = DateTime.UtcNow;
                
                _context.EnterpriseMetrics.Add(metric);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = $"Enterprise metric '{metric.Title}' has been created successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating enterprise metric");
                ModelState.AddModelError("", "An error occurred while creating the enterprise metric. Please try again.");
            }
        }
        return View("~/Views/Admin/EnterpriseMetric/Create.cshtml", metric);
    }

    // GET: EnterpriseMetric/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var metric = await _context.EnterpriseMetrics.FindAsync(id);
        if (metric == null)
        {
            return NotFound();
        }

        return View("~/Views/Admin/EnterpriseMetric/Edit.cshtml", metric);
    }

    // POST: EnterpriseMetric/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EnterpriseMetric metric, string validationRulesJson)
    {
        if (id != metric.Id)
        {
            return NotFound();
        }

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
                        return View("~/Views/Admin/EnterpriseMetric/Edit.cshtml", metric);
                    }
                }
                
                metric.UpdatedAt = DateTime.UtcNow;
                
                _context.Update(metric);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = $"Enterprise metric '{metric.Title}' has been updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!EnterpriseMetricExists(metric.Id))
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
                _logger.LogError(ex, "Error updating enterprise metric");
                ModelState.AddModelError("", "An error occurred while updating the enterprise metric. Please try again.");
            }
        }
        return View("~/Views/Admin/EnterpriseMetric/Edit.cshtml", metric);
    }

    // GET: EnterpriseMetric/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var metric = await _context.EnterpriseMetrics
            .FirstOrDefaultAsync(m => m.Id == id);
        if (metric == null)
        {
            return NotFound();
        }

        return View("~/Views/Admin/EnterpriseMetric/Delete.cshtml", metric);
    }

    // POST: EnterpriseMetric/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        try
        {
            var metric = await _context.EnterpriseMetrics.FindAsync(id);
            if (metric != null)
            {
                _context.EnterpriseMetrics.Remove(metric);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Enterprise metric '{metric.Title}' has been deleted successfully.";
            }
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting enterprise metric");
            TempData["ErrorMessage"] = "An error occurred while deleting the enterprise metric. It may be in use.";
            return RedirectToAction(nameof(Index));
        }
    }

    private bool EnterpriseMetricExists(int id)
    {
        return _context.EnterpriseMetrics.Any(e => e.Id == id);
    }
}

