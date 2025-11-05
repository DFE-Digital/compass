using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Compass.Data;
using Compass.Models;
using Compass.Services;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json;

namespace Compass.Controllers;

[Authorize]
public class ProductReportingController : Controller
{
    private readonly CompassDbContext _context;
    private readonly IProductsApiService _productsApiService;
    private readonly IReturnStatusService _returnStatusService;
    private readonly ILogger<ProductReportingController> _logger;

    public ProductReportingController(
        CompassDbContext context,
        IProductsApiService productsApiService,
        IReturnStatusService returnStatusService,
        ILogger<ProductReportingController> logger)
    {
        _context = context;
        _productsApiService = productsApiService;
        _returnStatusService = returnStatusService;
        _logger = logger;
    }

    // GET: ProductReporting/PerformanceMetrics
    public async Task<IActionResult> PerformanceMetrics()
    {
        // Get the current user's email
        var userEmail = User.Identity?.Name;
        
        // Fetch user's products and all products
        var userProducts = await _productsApiService.GetProductsAsync(userEmail);
        var allProducts = await _productsApiService.GetProductsAsync(null);
        
        // Get current reporting period (previous month)
        var now = DateTime.UtcNow;
        var currentYear = now.Month == 1 ? now.Year - 1 : now.Year;
        var currentMonth = now.Month == 1 ? 12 : now.Month - 1;
        
        // Get all returns for current period in one query
        var fipsIds = userProducts.Where(p => !string.IsNullOrEmpty(p.FipsId)).Select(p => p.FipsId).ToList();
        var userReturns = await _context.ProductReturns
            .Include(pr => pr.MetricValues)
            .Where(pr => fipsIds.Contains(pr.FipsId) && pr.Year == currentYear && pr.Month == currentMonth)
            .ToDictionaryAsync(pr => pr.FipsId, pr => pr);

        // Create view model for user's products
        var userProductStatuses = new List<ProductReturnStatusViewModel>();
        foreach (var product in userProducts)
        {
            if (string.IsNullOrEmpty(product.FipsId)) continue;
            
            var currentReturn = userReturns.TryGetValue(product.FipsId, out var returnValue) ? returnValue : null;
            
            ReturnStatus? status = null;
            int? completedMetrics = null;
            int? totalMetrics = null;
            
            if (currentReturn != null)
            {
                status = _returnStatusService.CalculateReturnStatus(
                    currentReturn.Year, 
                    currentReturn.Month, 
                    currentReturn.SubmittedDate);
                    
                totalMetrics = currentReturn.MetricValues?.Count ?? 0;
                completedMetrics = currentReturn.MetricValues?.Count(mv => mv.IsComplete) ?? 0;
            }
            else
            {
                // Check if this period has started yet
                var periodStart = new DateTime(currentYear, currentMonth, 1);
                if (periodStart >= new DateTime(2025, 10, 1))
                {
                    status = _returnStatusService.CalculateReturnStatus(currentYear, currentMonth, null);
                }
            }
            
            // Find the user's role for this product
            string? userRole = null;
            if (!string.IsNullOrEmpty(userEmail) && product.ProductContacts != null)
            {
                var userContact = product.ProductContacts.FirstOrDefault(pc => 
                    pc.UsersPermissionsUser != null && 
                    !string.IsNullOrEmpty(pc.UsersPermissionsUser.Email) &&
                    pc.UsersPermissionsUser.Email.Equals(userEmail, StringComparison.OrdinalIgnoreCase));
                
                userRole = userContact?.Role;
            }
            
            userProductStatuses.Add(new ProductReturnStatusViewModel
            {
                Product = product,
                CurrentPeriodYear = currentYear,
                CurrentPeriodMonth = currentMonth,
                Status = status,
                CompletedMetrics = completedMetrics,
                TotalMetrics = totalMetrics,
                UserRole = userRole
            });
        }
        
        // Get all returns for all products in one query
        var allFipsIds = allProducts.Where(p => !string.IsNullOrEmpty(p.FipsId)).Select(p => p.FipsId).ToList();
        var allReturns = await _context.ProductReturns
            .Include(pr => pr.MetricValues)
            .Where(pr => allFipsIds.Contains(pr.FipsId) && pr.Year == currentYear && pr.Month == currentMonth)
            .ToDictionaryAsync(pr => pr.FipsId, pr => pr);

        // Create view model for all products with service owner
        var allProductStatuses = new List<ProductReturnStatusViewModel>();
        foreach (var product in allProducts)
        {
            if (string.IsNullOrEmpty(product.FipsId)) continue;
            
            var currentReturn = allReturns.TryGetValue(product.FipsId, out var returnValue) ? returnValue : null;
            
            ReturnStatus? status = null;
            int? completedMetrics = null;
            int? totalMetrics = null;
            
            if (currentReturn != null)
            {
                status = _returnStatusService.CalculateReturnStatus(
                    currentReturn.Year, 
                    currentReturn.Month, 
                    currentReturn.SubmittedDate);
                    
                totalMetrics = currentReturn.MetricValues?.Count ?? 0;
                completedMetrics = currentReturn.MetricValues?.Count(mv => mv.IsComplete) ?? 0;
            }
            else
            {
                // Check if this period has started yet
                var periodStart = new DateTime(currentYear, currentMonth, 1);
                if (periodStart >= new DateTime(2025, 10, 1))
                {
                    status = _returnStatusService.CalculateReturnStatus(currentYear, currentMonth, null);
                }
            }
            
            // Find the service owner for this product
            string? serviceOwner = null;
            if (product.ProductContacts != null)
            {
                var serviceOwnerContact = product.ProductContacts.FirstOrDefault(pc => 
                    !string.IsNullOrEmpty(pc.Role) &&
                    pc.Role.Equals("service_owner", StringComparison.OrdinalIgnoreCase) &&
                    pc.UsersPermissionsUser != null);
                
                serviceOwner = serviceOwnerContact?.UsersPermissionsUser?.Username 
                    ?? serviceOwnerContact?.UsersPermissionsUser?.Email;
            }
            
            allProductStatuses.Add(new ProductReturnStatusViewModel
            {
                Product = product,
                CurrentPeriodYear = currentYear,
                CurrentPeriodMonth = currentMonth,
                Status = status,
                CompletedMetrics = completedMetrics,
                TotalMetrics = totalMetrics,
                UserRole = serviceOwner  // Reusing UserRole field for service owner name
            });
        }
        
        ViewBag.AllProducts = allProductStatuses;
        return View("~/Views/ProductReporting/PerformanceMetrics/Index.cshtml", userProductStatuses);
    }

    // GET: ProductReporting/ProductHistory/FIPS123
    public async Task<IActionResult> ProductHistory(string fipsId)
    {
        if (string.IsNullOrEmpty(fipsId))
        {
            return NotFound();
        }

        var product = await _productsApiService.GetProductByFipsIdAsync(fipsId);
        if (product == null)
        {
            return NotFound();
        }

        // Get or create returns starting from October 2025 and 1 upcoming month
        var returns = await GetOrCreateReturns(fipsId, 1);

        ViewBag.Product = product;
        return View("~/Views/ProductReporting/PerformanceMetrics/History.cshtml", returns);
    }

    // GET: ProductReporting/SubmitMetrics/FIPS123/2025/10
    public async Task<IActionResult> SubmitMetrics(string fipsId, int year, int month)
    {
        if (string.IsNullOrEmpty(fipsId))
        {
            return NotFound();
        }

        var product = await _productsApiService.GetProductByFipsIdAsync(fipsId);
        if (product == null)
        {
            return NotFound();
        }

        // Get or create the return
        var productReturn = await GetOrCreateReturn(fipsId, year, month);

        // Check if return is in a state that allows editing
        if (productReturn.Status == ReturnStatus.Submitted)
        {
            TempData["ErrorMessage"] = "This return has already been submitted and cannot be edited.";
            return RedirectToAction(nameof(ProductHistory), new { fipsId });
        }

        if (productReturn.Status == ReturnStatus.Upcoming)
        {
            TempData["WarningMessage"] = "This return is not yet due. You can view the metrics but cannot enter data yet.";
        }

        // Get all performance metrics that are valid for this reporting period
        // A metric is valid if ValidFromYear/Month is <= the reporting period
        var allMetrics = await _context.PerformanceMetrics
            .Where(m => !m.IsDisabled && // Exclude disabled metrics
                   (m.ValidFromYear < year || 
                   (m.ValidFromYear == year && m.ValidFromMonth <= month)))
            .OrderBy(m => m.Identifier)
            .ToListAsync();
        
        // Filter by phase - only include metrics that apply to the product's phase
        var phaseFilteredMetrics = allMetrics.Where(m => 
        {
            // If no phases specified, metric applies to all phases
            if (string.IsNullOrEmpty(m.ApplicablePhases))
                return true;
            
            // If product has no phase, show all metrics
            if (string.IsNullOrEmpty(product.Phase))
                return true;
            
            // Check if product's phase is in the metric's applicable phases
            var applicablePhases = m.ApplicablePhases.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .ToList();
            
            return applicablePhases.Contains(product.Phase, StringComparer.OrdinalIgnoreCase);
        }).ToList();

        // Get product type from category values
        string? productType = null;
        if (product.CategoryValues != null)
        {
            _logger.LogInformation("Product {FipsId} has {Count} category values", fipsId, product.CategoryValues.Count);
            foreach (var cv in product.CategoryValues)
            {
                _logger.LogInformation("  Category: {Name} (Type: {TypeName})", cv.Name, cv.CategoryType?.Name);
            }
            
            var typeCategory = product.CategoryValues
                .FirstOrDefault(cv => cv.CategoryType?.Name?.Equals("Type", StringComparison.OrdinalIgnoreCase) == true);
            if (typeCategory != null)
            {
                productType = typeCategory.Name;
                _logger.LogInformation("Product {FipsId} has Type: {Type}", fipsId, productType);
            }
            else
            {
                _logger.LogWarning("Product {FipsId} does not have a Type category assigned", fipsId);
            }
        }
        else
        {
            _logger.LogWarning("Product {FipsId} has no CategoryValues", fipsId);
        }

        // Filter by type - only include metrics that apply to the product's type
        var typeFilteredMetrics = phaseFilteredMetrics.Where(m => 
        {
            // If no types specified, metric applies to all types
            if (string.IsNullOrEmpty(m.ApplicableTypes))
            {
                _logger.LogInformation("Metric {MetricId} ({Title}) has no type restriction - including", m.Id, m.Title);
                return true;
            }
            
            // If product has no type, show all metrics
            if (string.IsNullOrEmpty(productType))
            {
                _logger.LogInformation("Metric {MetricId} ({Title}) has type restriction but product has no type - including anyway", m.Id, m.Title);
                return true;
            }
            
            // Check if product's type is in the metric's applicable types
            var applicableTypes = m.ApplicableTypes.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .ToList();
            
            var matches = applicableTypes.Contains(productType, StringComparer.OrdinalIgnoreCase);
            if (matches)
            {
                _logger.LogInformation("Metric {MetricId} ({Title}) matches product type '{ProductType}' - including", m.Id, m.Title, productType);
            }
            else
            {
                _logger.LogInformation("Metric {MetricId} ({Title}) requires types [{Types}] but product has '{ProductType}' - excluding", 
                    m.Id, m.Title, string.Join(", ", applicableTypes), productType);
            }
            
            return matches;
        }).ToList();
        
        _logger.LogInformation("After phase filtering: {Count} metrics. After type filtering: {Count2} metrics", 
            phaseFilteredMetrics.Count, typeFilteredMetrics.Count);

        // Get existing metric values for this return to check conditional dependencies
        var existingMetricValues = await _context.ProductMetricValues
            .Include(mv => mv.PerformanceMetric)
            .Where(mv => mv.ProductReturnId == productReturn.Id)
            .ToListAsync();

        // Filter by conditional dependencies - only show metrics whose conditions are met
        var metrics = typeFilteredMetrics.Where(m => 
        {
            // If no conditional dependency, always show
            if (!m.ConditionalOnMetricId.HasValue)
                return true;
            
            // Check if the parent metric has a value
            var parentMetricValue = existingMetricValues
                .FirstOrDefault(mv => mv.PerformanceMetricId == m.ConditionalOnMetricId.Value);
            
            // Show if parent metric exists and has a value
            return parentMetricValue != null && !string.IsNullOrWhiteSpace(parentMetricValue.Value);
        }).ToList();

        // Ensure we have a metric value entry for each valid metric
        foreach (var metric in metrics)
        {
            if (!existingMetricValues.Any(mv => mv.PerformanceMetricId == metric.Id))
            {
                var newValue = new ProductMetricValue
                {
                    ProductReturnId = productReturn.Id,
                    PerformanceMetricId = metric.Id,
                    IsComplete = false
                };
                _context.ProductMetricValues.Add(newValue);
                existingMetricValues.Add(newValue);
            }
        }
        
        // Use only the metrics that passed all filters
        var metricValues = existingMetricValues
            .Where(mv => metrics.Any(m => m.Id == mv.PerformanceMetricId))
            .ToList();

        await _context.SaveChangesAsync();

        ViewBag.Product = product;
        ViewBag.ProductReturn = productReturn;
        ViewBag.IsReadOnly = productReturn.Status == ReturnStatus.Submitted || productReturn.Status == ReturnStatus.Upcoming;
        
        return View("~/Views/ProductReporting/PerformanceMetrics/Submit.cshtml", metricValues.OrderBy(mv => mv.PerformanceMetric?.Identifier).ToList());
    }

    // POST: ProductReporting/SaveMetricValue
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveMetricValue(int id, string value)
    {
        var metricValue = await _context.ProductMetricValues
            .Include(mv => mv.PerformanceMetric)
            .Include(mv => mv.ProductReturn)
            .FirstOrDefaultAsync(mv => mv.Id == id);

        if (metricValue == null)
        {
            return Json(new { success = false, message = "Metric value not found" });
        }

        // Check if return is editable
        if (metricValue.ProductReturn?.Status == ReturnStatus.Submitted)
        {
            return Json(new { success = false, message = "This return has been submitted and cannot be edited" });
        }

        if (metricValue.ProductReturn?.Status == ReturnStatus.Upcoming)
        {
            return Json(new { success = false, message = "This return is not yet due for submission" });
        }

        try
        {
            // Validate the value based on the metric's validation rules
            if (metricValue.PerformanceMetric != null)
            {
                var validationResult = ValidateMetricValue(value, metricValue.PerformanceMetric);
                if (!validationResult.IsValid)
                {
                    return Json(new { success = false, message = validationResult.ErrorMessage });
                }
            }

            metricValue.Value = value;
            
            // Mark as complete if value is provided OR if empty but allowNull is true (not captured)
            var isNotCaptured = string.IsNullOrWhiteSpace(value);
            if (isNotCaptured && metricValue.PerformanceMetric != null)
            {
                try
                {
                    var rules = JsonSerializer.Deserialize<ValidationRules>(metricValue.PerformanceMetric.ValidationRules);
                    metricValue.IsComplete = rules?.AllowNull == true;
                }
                catch
                {
                    metricValue.IsComplete = !string.IsNullOrWhiteSpace(value);
                }
            }
            else
            {
                metricValue.IsComplete = !string.IsNullOrWhiteSpace(value);
            }
            
            metricValue.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Value saved successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving metric value");
            return Json(new { success = false, message = "An error occurred while saving" });
        }
    }

    // POST: ProductReporting/SubmitReturn
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitReturn(int returnId)
    {
        var productReturn = await _context.ProductReturns
            .Include(pr => pr.MetricValues)
            .FirstOrDefaultAsync(pr => pr.Id == returnId);

        if (productReturn == null)
        {
            TempData["ErrorMessage"] = "Return not found";
            return RedirectToAction(nameof(PerformanceMetrics));
        }

        // Check if all metrics are complete
        var incompleteCount = productReturn.MetricValues.Count(mv => !mv.IsComplete);
        if (incompleteCount > 0)
        {
            TempData["ErrorMessage"] = $"Cannot submit: {incompleteCount} metric(s) still need to be completed";
            return RedirectToAction(nameof(SubmitMetrics), new { fipsId = productReturn.FipsId, year = productReturn.Year, month = productReturn.Month });
        }

        try
        {
            productReturn.Status = ReturnStatus.Submitted;
            productReturn.SubmittedDate = DateTime.UtcNow;
            productReturn.SubmittedBy = User.Identity?.Name ?? "Unknown";
            productReturn.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Return for {productReturn.Month:D2}/{productReturn.Year} has been submitted successfully";
            return RedirectToAction(nameof(ProductHistory), new { fipsId = productReturn.FipsId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting return");
            TempData["ErrorMessage"] = "An error occurred while submitting the return";
            return RedirectToAction(nameof(SubmitMetrics), new { fipsId = productReturn.FipsId, year = productReturn.Year, month = productReturn.Month });
        }
    }

    // POST: ProductReporting/UnsubmitReturn
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnsubmitReturn(int returnId)
    {
        var productReturn = await _context.ProductReturns
            .FirstOrDefaultAsync(pr => pr.Id == returnId);

        if (productReturn == null)
        {
            TempData["ErrorMessage"] = "Return not found";
            return RedirectToAction(nameof(PerformanceMetrics));
        }

        // Check if return is actually submitted
        if (productReturn.Status != ReturnStatus.Submitted)
        {
            TempData["ErrorMessage"] = "This return has not been submitted yet";
            return RedirectToAction(nameof(ProductHistory), new { fipsId = productReturn.FipsId });
        }

        try
        {
            // Recalculate status (will be Due or Late depending on due date)
            productReturn.Status = _returnStatusService.CalculateReturnStatus(productReturn.Year, productReturn.Month, null);
            productReturn.SubmittedDate = null;
            productReturn.SubmittedBy = null;
            productReturn.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Return for {new DateTime(productReturn.Year, productReturn.Month, 1).ToString("MMMM yyyy")} has been unsubmitted and is now available for editing";
            return RedirectToAction(nameof(ProductHistory), new { fipsId = productReturn.FipsId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unsubmitting return");
            TempData["ErrorMessage"] = "An error occurred while unsubmitting the return";
            return RedirectToAction(nameof(ProductHistory), new { fipsId = productReturn.FipsId });
        }
    }

    #region Helper Methods

    private async Task<List<ProductReturn>> GetOrCreateReturns(string fipsId, int upcomingMonths)
    {
        var returns = new List<ProductReturn>();
        var now = DateTime.UtcNow;

        // Start from October 2025
        var startDate = new DateTime(2025, 10, 1);
        
        // End date is current month + upcoming months, but not before October 2025
        var endDate = now.AddMonths(upcomingMonths);
        if (endDate < startDate)
        {
            endDate = startDate.AddMonths(upcomingMonths);
        }

        for (var date = startDate; 
             date <= endDate; 
             date = date.AddMonths(1))
        {
            var existingReturn = await _context.ProductReturns
                .Include(pr => pr.MetricValues)
                    .ThenInclude(mv => mv.PerformanceMetric)
                .AsSplitQuery()
                .FirstOrDefaultAsync(pr => pr.FipsId == fipsId && pr.Year == date.Year && pr.Month == date.Month);

            if (existingReturn != null)
            {
                existingReturn.Status = _returnStatusService.CalculateReturnStatus(existingReturn.Year, existingReturn.Month, existingReturn.SubmittedDate);
                returns.Add(existingReturn);
            }
            else
            {
                var newReturn = new ProductReturn
                {
                    FipsId = fipsId,
                    Year = date.Year,
                    Month = date.Month,
                    Status = _returnStatusService.CalculateReturnStatus(date.Year, date.Month, null)
                };
                returns.Add(newReturn);
            }
        }

        return returns.OrderByDescending(r => r.Year).ThenByDescending(r => r.Month).ToList();
    }

    private async Task<ProductReturn> GetOrCreateReturn(string fipsId, int year, int month)
    {
        var existingReturn = await _context.ProductReturns
            .FirstOrDefaultAsync(pr => pr.FipsId == fipsId && pr.Year == year && pr.Month == month);

        if (existingReturn != null)
        {
            existingReturn.Status = _returnStatusService.CalculateReturnStatus(year, month, existingReturn.SubmittedDate);
            return existingReturn;
        }

        var newReturn = new ProductReturn
        {
            FipsId = fipsId,
            Year = year,
            Month = month,
            Status = _returnStatusService.CalculateReturnStatus(year, month, null)
        };

        _context.ProductReturns.Add(newReturn);
        await _context.SaveChangesAsync();

        return newReturn;
    }

    private (bool IsValid, string? ErrorMessage) ValidateMetricValue(string value, PerformanceMetric metric)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            // Check if null is allowed
            try
            {
                var rules = JsonSerializer.Deserialize<ValidationRules>(metric.ValidationRules);
                // Only return error if required AND allowNull is not true
                if (rules?.Required == true && rules?.AllowNull != true)
                {
                    return (false, "This field is required");
                }
                return (true, null);
            }
            catch
            {
                return (true, null);
            }
        }

        try
        {
            var rules = JsonSerializer.Deserialize<ValidationRules>(metric.ValidationRules);
            
            switch (metric.ValueType)
            {
                case Models.ValueType.Number:
                    if (!int.TryParse(value, out var intValue))
                    {
                        return (false, "Value must be a whole number");
                    }
                    
                    // Check new format first
                    if (rules?.MinimumValue.HasValue == true && intValue < rules.MinimumValue.Value)
                    {
                        return (false, $"Value must be at least {rules.MinimumValue.Value}");
                    }
                    if (rules?.MaximumValue.HasValue == true && intValue > rules.MaximumValue.Value)
                    {
                        return (false, $"Value must be at most {rules.MaximumValue.Value}");
                    }
                    
                    // Legacy format support
                    if (rules?.Range != null)
                    {
                        if (rules.Range.Min.HasValue && intValue < rules.Range.Min.Value)
                        {
                            return (false, $"Value must be at least {rules.Range.Min.Value}");
                        }
                        if (rules.Range.Max.HasValue && intValue > rules.Range.Max.Value)
                        {
                            return (false, $"Value must be at most {rules.Range.Max.Value}");
                        }
                    }
                    break;

                case Models.ValueType.Decimal:
                    if (!decimal.TryParse(value, out var decimalValue))
                    {
                        return (false, "Value must be a decimal number");
                    }
                    
                    // Check new format first
                    if (rules?.MinimumValue.HasValue == true && decimalValue < rules.MinimumValue.Value)
                    {
                        return (false, $"Value must be at least {rules.MinimumValue.Value}");
                    }
                    if (rules?.MaximumValue.HasValue == true && decimalValue > rules.MaximumValue.Value)
                    {
                        return (false, $"Value must be at most {rules.MaximumValue.Value}");
                    }
                    
                    // Legacy format support
                    if (rules?.Range != null)
                    {
                        if (rules.Range.Min.HasValue && decimalValue < rules.Range.Min.Value)
                        {
                            return (false, $"Value must be at least {rules.Range.Min.Value}");
                        }
                        if (rules.Range.Max.HasValue && decimalValue > rules.Range.Max.Value)
                        {
                            return (false, $"Value must be at most {rules.Range.Max.Value}");
                        }
                    }
                    
                    if (rules?.DecimalPlaces.HasValue == true)
                    {
                        var decimalPart = value.Split('.').Skip(1).FirstOrDefault();
                        if (!string.IsNullOrEmpty(decimalPart) && decimalPart.Length > rules.DecimalPlaces.Value)
                        {
                            return (false, $"Value must have at most {rules.DecimalPlaces.Value} decimal places");
                        }
                    }
                    break;

                case Models.ValueType.Text:
                    // Text doesn't need special validation beyond required check
                    break;
            }

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating metric value");
            return (true, null); // Allow if validation rules can't be parsed
        }
    }

    #endregion
}

