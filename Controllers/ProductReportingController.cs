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
    private readonly IPerformanceReportingEligibilityService _eligibilityService;
    private readonly ILogger<ProductReportingController> _logger;

    public ProductReportingController(
        CompassDbContext context,
        IProductsApiService productsApiService,
        IReturnStatusService returnStatusService,
        IPerformanceReportingEligibilityService eligibilityService,
        ILogger<ProductReportingController> logger)
    {
        _context = context;
        _productsApiService = productsApiService;
        _returnStatusService = returnStatusService;
        _eligibilityService = eligibilityService;
        _logger = logger;
    }

    // GET: ProductReporting/PerformanceMetrics
    public async Task<IActionResult> PerformanceMetrics(string view = "tasks")
    {
        // Get the current user's email
        var userEmail = User.Identity?.Name;
        
        // Fetch user's products - both from product_contacts and service_owner
        var productsByContact = await _productsApiService.GetProductsAsync(userEmail);
        var productsByServiceOwner = await _productsApiService.GetProductsByServiceOwnerAsync(userEmail);
        
        // Combine and deduplicate products (by FipsId)
        var userProducts = productsByContact
            .Concat(productsByServiceOwner)
            .GroupBy(p => p.FipsId)
            .Where(g => !string.IsNullOrEmpty(g.Key))
            .Select(g => g.First())
            .ToList();
        
        // Get current reporting period (previous month)
        var now = DateTime.UtcNow;
        var currentYear = now.Month == 1 ? now.Year - 1 : now.Year;
        var currentMonth = now.Month == 1 ? 12 : now.Month - 1;
        
        // PERFORMANCE OPTIMIZATION: Load eligibility cache once upfront
        var eligibilityCache = await _eligibilityService.LoadEligibilityCacheAsync();
        
        // Get all returns for current period in one query
        var fipsIds = userProducts.Where(p => !string.IsNullOrEmpty(p.FipsId)).Select(p => p.FipsId).ToList();
        var userReturns = await _context.ProductReturns
            .Include(pr => pr.MetricValues)
            .Where(pr => fipsIds.Contains(pr.FipsId) && pr.Year == currentYear && pr.Month == currentMonth)
            .ToDictionaryAsync(pr => pr.FipsId, pr => pr);

        // Create view model for user's products (show ALL products user is responsible for)
        var userProductStatuses = new List<ProductReturnStatusViewModel>();
        foreach (var product in userProducts)
        {
            var vm = await CreateProductStatusViewModelAsync(product, userReturns, currentYear, currentMonth, userEmail, eligibilityCache);
            if (vm != null)
            {
                userProductStatuses.Add(vm);
            }
        }
        
        // Calculate counts for navigation badges
        // Tasks count: products that require reporting AND are Due or Late
        var tasksCount = userProductStatuses.Count(p => 
            p.IsReportingRequired && (p.Status == ReturnStatus.Due || p.Status == ReturnStatus.Late));
        var yourProductsCount = userProductStatuses.Count;
        
        // For "All products" view, get all eligible products
        List<ProductReturnStatusViewModel> allProductStatuses = new List<ProductReturnStatusViewModel>();
        int allProductsCount = 0;
        
        if (view == "all")
        {
            var allProducts = await _productsApiService.GetAllProductsAsync();
            // Filter to only Active products
            var activeProducts = allProducts
                .Where(p => p.State != null && p.State.Equals("Active", StringComparison.OrdinalIgnoreCase))
                .ToList();
            
            var allFipsIds = activeProducts
                .Where(p => !string.IsNullOrEmpty(p.FipsId))
                .Select(p => p.FipsId!)
                .ToList();
            
            var allReturns = await _context.ProductReturns
                .Include(pr => pr.MetricValues)
                .Where(pr => allFipsIds.Contains(pr.FipsId) && pr.Year == currentYear && pr.Month == currentMonth)
                .ToDictionaryAsync(pr => pr.FipsId, pr => pr);
            
            foreach (var product in activeProducts)
            {
                var vm = await CreateProductStatusViewModelAsync(product, allReturns, currentYear, currentMonth, userEmail, eligibilityCache);
                if (vm != null)
                {
                    allProductStatuses.Add(vm);
                }
            }
            
            allProductsCount = allProductStatuses.Count;
        }
        // Note: allProductsCount will be 0 when not on "all" view - badge will show 0 or be hidden
        
        // Determine which data to show based on view
        List<ProductReturnStatusViewModel> displayData;
        if (view == "tasks")
        {
            // Show products that:
            // 1. Require reporting (includes business area overrides), OR
            // 2. Have business area override (even if period excluded)
            // AND have status Due or Late
            displayData = userProductStatuses
                .Where(p => (p.IsReportingRequired || (p.IsBusinessAreaInScope && p.Status.HasValue)) && 
                           (p.Status == ReturnStatus.Due || p.Status == ReturnStatus.Late))
                .ToList();
        }
        else if (view == "all")
        {
            displayData = allProductStatuses;
        }
        else // "products" or default
        {
            displayData = userProductStatuses;
        }
        
        // Check if current period is excluded and there are no business area overrides
        var periodExcluded = eligibilityCache.PeriodExclusions.Any(e => e.Year == currentYear && e.Month == currentMonth);
        var hasBusinessAreaOverrides = eligibilityCache.BusinessAreaConfigs.Any(c => 
            (c.ApplicableFromYear < currentYear || 
             (c.ApplicableFromYear == currentYear && c.ApplicableFromMonth <= currentMonth)) &&
            (!c.ApplicableUntilYear.HasValue || 
             c.ApplicableUntilYear.Value > currentYear || 
             (c.ApplicableUntilYear.Value == currentYear && c.ApplicableUntilMonth >= currentMonth)));
        
        string? globalSuspensionMessage = null;
        DateTime? globalNextReportingPeriod = null;
        DateTime? globalNextReportingPeriodDueDate = null;
        
        if (periodExcluded && !hasBusinessAreaOverrides)
        {
            globalSuspensionMessage = "Reporting is not required at this time due to period exclusion";
            var nextPeriod = await _eligibilityService.FindNextActiveReportingPeriodAsync(currentYear, currentMonth, null, eligibilityCache);
            if (nextPeriod.HasValue)
            {
                globalNextReportingPeriod = new DateTime(nextPeriod.Value.Year, nextPeriod.Value.Month, 1);
                globalNextReportingPeriodDueDate = _returnStatusService.GetReturnDueDate(nextPeriod.Value.Year, nextPeriod.Value.Month);
            }
        }
        
        // Calculate upcoming reporting dates (3 months in advance)
        // Exclude periods that are in the period exclusions list
        var upcomingReportingDates = new List<(int Year, int Month, DateTime DueDate, string PeriodName)>();
        
        // Start from current reporting period and go forward 3 months
        for (int i = 0; i <= 3; i++)
        {
            var futureMonth = currentMonth + i;
            var futureYear = currentYear;
            
            // Handle year rollover
            while (futureMonth > 12)
            {
                futureMonth -= 12;
                futureYear += 1;
            }
            
            // Skip if this period is excluded
            var isExcluded = eligibilityCache.PeriodExclusions.Any(e => e.Year == futureYear && e.Month == futureMonth);
            if (isExcluded)
            {
                continue;
            }
            
            var dueDate = _returnStatusService.GetReturnDueDate(futureYear, futureMonth);
            var periodName = new DateTime(futureYear, futureMonth, 1).ToString("MMMM yyyy");
            upcomingReportingDates.Add((futureYear, futureMonth, dueDate, periodName));
        }
        
        // Get products table data (user's products that need reporting)
        var productsForTable = userProductStatuses
            .Where(p => p.IsReportingRequired)
            .OrderBy(p => p.Product.Title)
            .ToList();

        ViewBag.CurrentView = view;
        ViewBag.TasksCount = tasksCount;
        ViewBag.YourProductsCount = yourProductsCount;
        ViewBag.AllProductsCount = allProductsCount;
        ViewBag.CurrentYear = currentYear;
        ViewBag.CurrentMonth = currentMonth;
        ViewBag.GlobalSuspensionMessage = globalSuspensionMessage;
        ViewBag.GlobalNextReportingPeriod = globalNextReportingPeriod;
        ViewBag.GlobalNextReportingPeriodDueDate = globalNextReportingPeriodDueDate;
        ViewBag.UpcomingReportingDates = upcomingReportingDates;
        ViewBag.ProductsForTable = productsForTable;
        ViewBag.UserProducts = userProductStatuses;
        
        return View("~/Views/ProductReporting/PerformanceMetrics/Index.cshtml", displayData);
    }
    
    // GET: ProductReporting/ReportOtherProduct
    public async Task<IActionResult> ReportOtherProduct()
    {
        var allProducts = await _productsApiService.GetAllProductsAsync();
        
        // Get current reporting period
        var now = DateTime.UtcNow;
        var currentYear = now.Month == 1 ? now.Year - 1 : now.Year;
        var currentMonth = now.Month == 1 ? 12 : now.Month - 1;
        
        // Load eligibility cache once
        var eligibilityCache = await _eligibilityService.LoadEligibilityCacheAsync();
        
        // Filter to only products requiring reporting
        var eligibleProducts = allProducts
            .Where(p => !string.IsNullOrEmpty(p.FipsId))
            .Where(p => p.State.Equals("Active", StringComparison.OrdinalIgnoreCase))
            .Where(p =>
            {
                var businessArea = p.CategoryValues?
                    .FirstOrDefault(cv => cv.CategoryType?.Name?.Equals("Business area", StringComparison.OrdinalIgnoreCase) == true)
                    ?.Name;
                return _eligibilityService.IsReportingRequired(p.FipsId!, businessArea, currentYear, currentMonth, eligibilityCache);
            })
            .OrderBy(p => p.Title)
            .ToList();
        
        ViewBag.CurrentYear = currentYear;
        ViewBag.CurrentMonth = currentMonth;
        return View("~/Views/ProductReporting/PerformanceMetrics/ReportOtherProduct.cshtml", eligibleProducts);
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

        // Get all product types from category values
        var productTypes = new List<string>();
        if (product.CategoryValues != null)
        {
            _logger.LogInformation("Product {FipsId} has {Count} category values", fipsId, product.CategoryValues.Count);
            foreach (var cv in product.CategoryValues)
            {
                _logger.LogInformation("  Category: {Name} (Type: {TypeName})", cv.Name, cv.CategoryType?.Name);
            }
            
            productTypes = product.CategoryValues
                .Where(cv => cv.CategoryType?.Name?.Equals("Type", StringComparison.OrdinalIgnoreCase) == true)
                .Select(cv => cv.Name)
                .ToList();
                
            if (productTypes.Any())
            {
                _logger.LogInformation("Product {FipsId} has Types: {Types}", fipsId, string.Join(", ", productTypes));
            }
            else
            {
                _logger.LogWarning("Product {FipsId} does not have any Type categories assigned", fipsId);
            }
        }
        else
        {
            _logger.LogWarning("Product {FipsId} has no CategoryValues", fipsId);
        }

        // Filter by type - only include metrics where ANY of the product's types match ANY of the metric's applicable types
        var typeFilteredMetrics = phaseFilteredMetrics.Where(m => 
        {
            // If no types specified, metric applies to all types
            if (string.IsNullOrEmpty(m.ApplicableTypes))
            {
                _logger.LogInformation("Metric {MetricId} ({Title}) has no type restriction - including", m.Id, m.Title);
                return true;
            }
            
            // If product has no types, show all metrics anyway
            if (!productTypes.Any())
            {
                _logger.LogInformation("Metric {MetricId} ({Title}) has type restriction but product has no types - including anyway", m.Id, m.Title);
                return true;
            }
            
            // Check if ANY of the product's types match ANY of the metric's applicable types
            var applicableTypes = m.ApplicableTypes.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .ToList();
            
            var matches = productTypes.Any(pt => applicableTypes.Contains(pt, StringComparer.OrdinalIgnoreCase));
            if (matches)
            {
                var matchingTypes = productTypes.Where(pt => applicableTypes.Contains(pt, StringComparer.OrdinalIgnoreCase)).ToList();
                _logger.LogInformation("Metric {MetricId} ({Title}) matches product types '{ProductTypes}' - including", 
                    m.Id, m.Title, string.Join(", ", matchingTypes));
            }
            else
            {
                _logger.LogInformation("Metric {MetricId} ({Title}) requires types [{MetricTypes}] but product has [{ProductTypes}] with no overlap - excluding", 
                    m.Id, m.Title, string.Join(", ", applicableTypes), string.Join(", ", productTypes));
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

        // Get previous month's values for comparison
        var previousMonth = month == 1 ? 12 : month - 1;
        var previousYear = month == 1 ? year - 1 : year;
        
        var previousReturn = await _context.ProductReturns
            .Include(pr => pr.MetricValues)
            .FirstOrDefaultAsync(pr => pr.FipsId == fipsId && 
                                      pr.Year == previousYear && 
                                      pr.Month == previousMonth);
        
        if (previousReturn != null)
        {
            foreach (var metricValue in metricValues)
            {
                var previousMetricValue = previousReturn.MetricValues
                    .FirstOrDefault(mv => mv.PerformanceMetricId == metricValue.PerformanceMetricId);
                
                if (previousMetricValue != null && !string.IsNullOrWhiteSpace(previousMetricValue.Value))
                {
                    metricValue.PreviousValue = previousMetricValue.Value;
                }
            }
        }

        ViewBag.Product = product;
        ViewBag.ProductReturn = productReturn;
        ViewBag.IsReadOnly = productReturn.Status == ReturnStatus.Submitted || productReturn.Status == ReturnStatus.Upcoming;
        
        return View("~/Views/ProductReporting/PerformanceMetrics/Submit.cshtml", metricValues.OrderBy(mv => mv.PerformanceMetric?.Identifier).ToList());
    }

    // POST: ProductReporting/SaveMetricValue
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveMetricValue(int id, string? value, bool isNotCaptured = false, string? notCapturedReason = null, string? reasonForDifference = null)
    {
        Console.WriteLine("========================================");
        Console.WriteLine($"SaveMetricValue called at {DateTime.Now:HH:mm:ss}");
        Console.WriteLine($"  ID: {id}");
        Console.WriteLine($"  Value: '{value ?? "(null)"}'");
        Console.WriteLine($"  IsNotCaptured: {isNotCaptured}");
        Console.WriteLine($"  NotCapturedReason: '{notCapturedReason ?? "(null)"}'");
        Console.WriteLine($"  ReasonForDifference: '{reasonForDifference ?? "(null)"}'");
        Console.WriteLine("========================================");
        
        _logger.LogInformation("SaveMetricValue called - ID: {Id}, Value: '{Value}', IsNotCaptured: {IsNotCaptured}, NotCapturedReason: '{Reason}'", 
            id, value ?? "(null)", isNotCaptured, notCapturedReason ?? "(null)");
        
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
            // If metric is marked as not captured, log it and mark as complete
            if (isNotCaptured)
            {
                _logger.LogInformation(
                    "Metric not captured. FIPS ID: {FipsId}, Metric: {MetricId} ({MetricTitle}), " +
                    "Period: {Year}-{Month}, Reason: {Reason}",
                    metricValue.ProductReturn?.FipsId,
                    metricValue.PerformanceMetricId,
                    metricValue.PerformanceMetric?.Title,
                    metricValue.ProductReturn?.Year,
                    metricValue.ProductReturn?.Month,
                    notCapturedReason ?? "No reason provided"
                );
                
                metricValue.Value = string.Empty;
                metricValue.IsComplete = true;
                metricValue.IsNotCaptured = true;
                metricValue.NotCapturedReason = notCapturedReason;
            }
            else
            {
                // Validate the value based on the metric's validation rules
                if (metricValue.PerformanceMetric != null && !string.IsNullOrWhiteSpace(value))
                {
                    var validationResult = ValidateMetricValue(value, metricValue.PerformanceMetric);
                    if (!validationResult.IsValid)
                    {
                        return Json(new { success = false, message = validationResult.ErrorMessage });
                    }
                }

                // Log reason for significant difference if provided
                if (!string.IsNullOrWhiteSpace(reasonForDifference))
                {
                    _logger.LogInformation(
                        "Metric value change with significant difference. FIPS ID: {FipsId}, Metric: {MetricId} ({MetricTitle}), " +
                        "Period: {Year}-{Month}, New Value: {NewValue}, Reason: {Reason}",
                        metricValue.ProductReturn?.FipsId,
                        metricValue.PerformanceMetricId,
                        metricValue.PerformanceMetric?.Title,
                        metricValue.ProductReturn?.Year,
                        metricValue.ProductReturn?.Month,
                        value ?? "",
                        reasonForDifference
                    );
                }

                metricValue.Value = value ?? string.Empty;
                metricValue.IsNotCaptured = false;
                metricValue.NotCapturedReason = null;
                metricValue.ReasonForDifference = reasonForDifference;
                
                // Mark as complete based on whether value is provided
                metricValue.IsComplete = !string.IsNullOrWhiteSpace(value);
            }
            
            metricValue.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            Console.WriteLine($"✓ Metric value saved successfully!");
            Console.WriteLine($"  → IsComplete: {metricValue.IsComplete}");
            Console.WriteLine($"  → Value: '{metricValue.Value}'");
            Console.WriteLine($"  → IsNotCaptured: {metricValue.IsNotCaptured}");
            Console.WriteLine($"  → NotCapturedReason: '{metricValue.NotCapturedReason ?? "(null)"}'");
            Console.WriteLine($"  → ReasonForDifference: '{metricValue.ReasonForDifference ?? "(null)"}'");
            
            return Json(new { success = true, message = "Value saved successfully" });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ ERROR saving metric value: {ex.Message}");
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

        // Get product to check business area
        var product = await _productsApiService.GetProductByFipsIdAsync(fipsId);
        var businessArea = product?.CategoryValues?
            .FirstOrDefault(cv => cv.CategoryType?.Name?.Equals("Business area", StringComparison.OrdinalIgnoreCase) == true)
            ?.Name;

        // PERFORMANCE OPTIMIZATION: Load eligibility cache once
        var eligibilityCache = await _eligibilityService.LoadEligibilityCacheAsync();

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
            // Check if reporting is required (using cached data - NO database query)
            var reportingRequired = _eligibilityService.IsReportingRequired(
                fipsId, 
                businessArea, 
                date.Year, 
                date.Month,
                eligibilityCache);
            
            // Skip this period if reporting is not required
            if (!reportingRequired)
            {
                continue;
            }
            
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

    private async Task<ProductReturnStatusViewModel?> CreateProductStatusViewModelAsync(
        ProductDto product, 
        Dictionary<string, ProductReturn> returnsDict,
        int currentYear,
        int currentMonth,
        string? userEmail,
        PerformanceReportingEligibilityCache eligibilityCache)
    {
        if (string.IsNullOrEmpty(product.FipsId)) return null;
        
        // Extract business area from category_values
        var businessArea = product.CategoryValues?
            .FirstOrDefault(cv => cv.CategoryType?.Name?.Equals("Business area", StringComparison.OrdinalIgnoreCase) == true)
            ?.Name;
        
        // Check if reporting is required (using cached data - NO database query)
        var reportingRequired = _eligibilityService.IsReportingRequired(
            product.FipsId, 
            businessArea, 
            currentYear, 
            currentMonth,
            eligibilityCache);
        
        // Check if period is excluded and business area has override
        var periodExcluded = eligibilityCache.PeriodExclusions.Any(e => e.Year == currentYear && e.Month == currentMonth);
        var businessAreaReporting = false;
        var isBusinessAreaInScope = false;
        if (!string.IsNullOrEmpty(businessArea))
        {
            // Check if business area is configured for reporting (in scope)
            isBusinessAreaInScope = eligibilityCache.BusinessAreaConfigs.Any(c => 
                c.BusinessAreaName == businessArea &&
                c.IsActive);
            
            // Check if business area reporting applies for current period
            businessAreaReporting = eligibilityCache.BusinessAreaConfigs.Any(c => 
                c.BusinessAreaName == businessArea &&
                c.IsActive &&
                (c.ApplicableFromYear < currentYear || 
                 (c.ApplicableFromYear == currentYear && c.ApplicableFromMonth <= currentMonth)) &&
                (!c.ApplicableUntilYear.HasValue || 
                 c.ApplicableUntilYear.Value > currentYear || 
                 (c.ApplicableUntilYear.Value == currentYear && c.ApplicableUntilMonth >= currentMonth)));
        }
        
        // Determine suspension reason and next reporting period
        string? suspensionReason = null;
        DateTime? nextReportingPeriod = null;
        DateTime? nextReportingPeriodDueDate = null;
        
        if (!reportingRequired && periodExcluded && !businessAreaReporting)
        {
            suspensionReason = "Reporting is not required at this time due to period exclusion";
            var nextPeriod = await _eligibilityService.FindNextActiveReportingPeriodAsync(currentYear, currentMonth, businessArea, eligibilityCache);
            if (nextPeriod.HasValue)
            {
                nextReportingPeriod = new DateTime(nextPeriod.Value.Year, nextPeriod.Value.Month, 1);
                // Calculate the due date for the next reporting period
                nextReportingPeriodDueDate = _returnStatusService.GetReturnDueDate(nextPeriod.Value.Year, nextPeriod.Value.Month);
            }
        }
        
        var currentReturn = returnsDict.TryGetValue(product.FipsId, out var returnValue) ? returnValue : null;
        
        ReturnStatus? status = null;
        int? completedMetrics = null;
        int? totalMetrics = null;
        DateTime? currentPeriodDueDate = null;
        
        // Calculate status if:
        // 1. There's an existing return, OR
        // 2. Reporting is required (includes business area overrides), OR
        // 3. Business area is in scope and has an override (even if period is excluded)
        var shouldCalculateStatus = currentReturn != null || 
                                   reportingRequired || 
                                   (isBusinessAreaInScope && businessAreaReporting);
        
        if (currentReturn != null)
        {
            status = _returnStatusService.CalculateReturnStatus(
                currentReturn.Year, 
                currentReturn.Month, 
                currentReturn.SubmittedDate);
                
            totalMetrics = currentReturn.MetricValues?.Count ?? 0;
            completedMetrics = currentReturn.MetricValues?.Count(mv => mv.IsComplete) ?? 0;
            currentPeriodDueDate = _returnStatusService.GetReturnDueDate(currentReturn.Year, currentReturn.Month);
        }
        else if (shouldCalculateStatus)
        {
            // Check if this period has started yet
            var periodStart = new DateTime(currentYear, currentMonth, 1);
            if (periodStart >= new DateTime(2025, 10, 1))
            {
                status = _returnStatusService.CalculateReturnStatus(currentYear, currentMonth, null);
                currentPeriodDueDate = _returnStatusService.GetReturnDueDate(currentYear, currentMonth);
            }
        }
        
        // Find the user's role for this product
        // Check product_contacts first, then check service_owner and other role fields
        string? userRole = null;
        List<string> roles = new List<string>();
        
        if (!string.IsNullOrEmpty(userEmail))
        {
            // Check product_contacts
            if (product.ProductContacts != null)
            {
                var userContact = product.ProductContacts.FirstOrDefault(pc => 
                    pc.UsersPermissionsUser != null && 
                    !string.IsNullOrEmpty(pc.UsersPermissionsUser.Email) &&
                    pc.UsersPermissionsUser.Email.Equals(userEmail, StringComparison.OrdinalIgnoreCase));
                
                if (!string.IsNullOrEmpty(userContact?.Role))
                {
                    roles.Add(userContact.Role);
                }
            }
            
            // Check service_owner (relation to entra-user)
            if (product.ServiceOwner != null && 
                !string.IsNullOrEmpty(product.ServiceOwner.EmailAddress) &&
                product.ServiceOwner.EmailAddress.Equals(userEmail, StringComparison.OrdinalIgnoreCase))
            {
                roles.Add("Service Owner");
            }
        }
        
        // Combine roles if user has multiple
        userRole = roles.Any() ? string.Join(", ", roles) : null;
        
        return new ProductReturnStatusViewModel
        {
            Product = product,
            CurrentPeriodYear = currentYear,
            CurrentPeriodMonth = currentMonth,
            Status = status,
            CompletedMetrics = completedMetrics,
            TotalMetrics = totalMetrics,
            UserRole = userRole,
            IsReportingRequired = reportingRequired,
            ReportingSuspensionReason = suspensionReason,
            NextReportingPeriod = nextReportingPeriod,
            NextReportingPeriodDueDate = nextReportingPeriodDueDate,
            IsBusinessAreaInScope = isBusinessAreaInScope,
            CurrentPeriodDueDate = currentPeriodDueDate,
            IsPeriodExcluded = periodExcluded,
            HasBusinessAreaOverride = businessAreaReporting
        };
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

    // GET: ProductReporting/ReportingDates
    public async Task<IActionResult> ReportingDates()
    {
        // Get the current user's email
        var userEmail = User.Identity?.Name;
        
        // Fetch user's products - both from product_contacts and service_owner
        var productsByContact = await _productsApiService.GetProductsAsync(userEmail);
        var productsByServiceOwner = await _productsApiService.GetProductsByServiceOwnerAsync(userEmail);
        
        // Combine and deduplicate products (by FipsId)
        var userProducts = productsByContact
            .Concat(productsByServiceOwner)
            .GroupBy(p => p.FipsId)
            .Where(g => !string.IsNullOrEmpty(g.Key))
            .Select(g => g.First())
            .ToList();
        
        // Get current reporting period (previous month)
        var now = DateTime.UtcNow;
        var currentYear = now.Month == 1 ? now.Year - 1 : now.Year;
        var currentMonth = now.Month == 1 ? 12 : now.Month - 1;
        
        // Load eligibility cache
        var eligibilityCache = await _eligibilityService.LoadEligibilityCacheAsync();
        
        // Get all returns for current period
        var fipsIds = userProducts.Where(p => !string.IsNullOrEmpty(p.FipsId)).Select(p => p.FipsId).ToList();
        var userReturns = await _context.ProductReturns
            .Include(pr => pr.MetricValues)
            .Where(pr => fipsIds.Contains(pr.FipsId) && pr.Year == currentYear && pr.Month == currentMonth)
            .ToDictionaryAsync(pr => pr.FipsId, pr => pr);

        // Create view models for user's products to calculate counts
        var userProductStatuses = new List<ProductReturnStatusViewModel>();
        foreach (var product in userProducts)
        {
            var vm = await CreateProductStatusViewModelAsync(product, userReturns, currentYear, currentMonth, userEmail, eligibilityCache);
            if (vm != null)
            {
                userProductStatuses.Add(vm);
            }
        }
        
        // Calculate counts for navigation badges
        var tasksCount = userProductStatuses.Count(p => 
            (p.IsReportingRequired || (p.IsBusinessAreaInScope && p.Status.HasValue)) && 
            (p.Status == ReturnStatus.Due || p.Status == ReturnStatus.Late));
        var yourProductsCount = userProductStatuses.Count;
        
        // Get all products count (Active only)
        var allProducts = await _productsApiService.GetAllProductsAsync();
        var activeProducts = allProducts
            .Where(p => p.State != null && p.State.Equals("Active", StringComparison.OrdinalIgnoreCase))
            .ToList();
        var allFipsIds = activeProducts
            .Where(p => !string.IsNullOrEmpty(p.FipsId))
            .Select(p => p.FipsId!)
            .ToList();
        var allReturns = await _context.ProductReturns
            .Include(pr => pr.MetricValues)
            .Where(pr => allFipsIds.Contains(pr.FipsId) && pr.Year == currentYear && pr.Month == currentMonth)
            .ToDictionaryAsync(pr => pr.FipsId, pr => pr);
        
        var allProductStatuses = new List<ProductReturnStatusViewModel>();
        foreach (var product in activeProducts)
        {
            var vm = await CreateProductStatusViewModelAsync(product, allReturns, currentYear, currentMonth, userEmail, eligibilityCache);
            if (vm != null)
            {
                allProductStatuses.Add(vm);
            }
        }
        var allProductsCount = allProductStatuses.Count;
        
        var startDate = new DateTime(2025, 10, 1); // Reporting started in October 2025
        var endDate = now.AddMonths(12); // Show next 12 months
        
        // Get all active due date overrides
        var overrides = await _context.PerformanceReportingDueDateOverrides
            .Where(o => o.IsActive)
            .ToDictionaryAsync(o => (o.ReportingYear, o.ReportingMonth), o => o);
        
        // Build list of reporting periods with their due dates
        var reportingPeriods = new List<(int Year, int Month, DateTime DueDate, bool HasOverride, string? OverrideReason)>();
        
        for (var date = startDate; date <= endDate; date = date.AddMonths(1))
        {
            var year = date.Year;
            var month = date.Month;
            
            // Check if there's an override
            var hasOverride = overrides.TryGetValue((year, month), out var overrideValue);
            
            // Calculate due date (will use override if exists, otherwise default rule)
            var dueDate = _returnStatusService.GetReturnDueDate(year, month);
            
            reportingPeriods.Add((
                year,
                month,
                dueDate,
                hasOverride,
                overrideValue?.Reason
            ));
        }
        
        // Get active period exclusions
        var periodExclusions = await _context.PerformanceReportingPeriodExclusions
            .Where(e => e.IsActive)
            .OrderBy(e => e.Year)
            .ThenBy(e => e.Month)
            .ToListAsync();
        
        // Get active business area configs
        var businessAreaConfigs = await _context.PerformanceReportingBusinessAreaConfigs
            .Where(c => c.IsActive)
            .OrderBy(c => c.BusinessAreaName)
            .ThenBy(c => c.ApplicableFromYear)
            .ThenBy(c => c.ApplicableFromMonth)
            .ToListAsync();
        
        ViewBag.ReportingPeriods = reportingPeriods;
        ViewBag.DefaultRule = "Returns are due by the 3rd working day of the following month";
        ViewBag.PeriodExclusions = periodExclusions;
        ViewBag.BusinessAreaConfigs = businessAreaConfigs;
        ViewBag.TasksCount = tasksCount;
        ViewBag.YourProductsCount = yourProductsCount;
        ViewBag.AllProductsCount = allProductsCount;
        
        return View("~/Views/ProductReporting/PerformanceMetrics/ReportingDates.cshtml");
    }

    // GET: ProductReporting/WhatYouNeedToReport
    public async Task<IActionResult> WhatYouNeedToReport()
    {
        // Get the current user's email
        var userEmail = User.Identity?.Name;
        
        // Fetch user's products for counts
        var productsByContact = await _productsApiService.GetProductsAsync(userEmail);
        var productsByServiceOwner = await _productsApiService.GetProductsByServiceOwnerAsync(userEmail);
        
        // Combine and deduplicate products (by FipsId)
        var userProducts = productsByContact
            .Concat(productsByServiceOwner)
            .GroupBy(p => p.FipsId)
            .Where(g => !string.IsNullOrEmpty(g.Key))
            .Select(g => g.First())
            .ToList();
        
        // Get current reporting period (previous month)
        var now = DateTime.UtcNow;
        var currentYear = now.Month == 1 ? now.Year - 1 : now.Year;
        var currentMonth = now.Month == 1 ? 12 : now.Month - 1;
        
        // Load eligibility cache
        var eligibilityCache = await _eligibilityService.LoadEligibilityCacheAsync();
        
        // Get all returns for current period
        var fipsIds = userProducts.Where(p => !string.IsNullOrEmpty(p.FipsId)).Select(p => p.FipsId).ToList();
        var userReturns = await _context.ProductReturns
            .Include(pr => pr.MetricValues)
            .Where(pr => fipsIds.Contains(pr.FipsId) && pr.Year == currentYear && pr.Month == currentMonth)
            .ToDictionaryAsync(pr => pr.FipsId, pr => pr);

        // Create view models for user's products to calculate counts
        var userProductStatuses = new List<ProductReturnStatusViewModel>();
        foreach (var product in userProducts)
        {
            var vm = await CreateProductStatusViewModelAsync(product, userReturns, currentYear, currentMonth, userEmail, eligibilityCache);
            if (vm != null)
            {
                userProductStatuses.Add(vm);
            }
        }
        
        // Calculate counts for navigation badges
        var tasksCount = userProductStatuses.Count(p => 
            (p.IsReportingRequired || (p.IsBusinessAreaInScope && p.Status.HasValue)) && 
            (p.Status == ReturnStatus.Due || p.Status == ReturnStatus.Late));
        var yourProductsCount = userProductStatuses.Count;
        
        // Get all products count (Active only)
        var allProducts = await _productsApiService.GetAllProductsAsync();
        var activeProducts = allProducts
            .Where(p => p.State != null && p.State.Equals("Active", StringComparison.OrdinalIgnoreCase))
            .ToList();
        var allFipsIds = activeProducts
            .Where(p => !string.IsNullOrEmpty(p.FipsId))
            .Select(p => p.FipsId!)
            .ToList();
        var allReturns = await _context.ProductReturns
            .Include(pr => pr.MetricValues)
            .Where(pr => allFipsIds.Contains(pr.FipsId) && pr.Year == currentYear && pr.Month == currentMonth)
            .ToDictionaryAsync(pr => pr.FipsId, pr => pr);
        
        var allProductStatuses = new List<ProductReturnStatusViewModel>();
        foreach (var product in activeProducts)
        {
            var vm = await CreateProductStatusViewModelAsync(product, allReturns, currentYear, currentMonth, userEmail, eligibilityCache);
            if (vm != null)
            {
                allProductStatuses.Add(vm);
            }
        }
        var allProductsCount = allProductStatuses.Count;
        
        // Get all active performance metrics
        var metrics = await _context.PerformanceMetrics
            .Where(m => !m.IsDisabled)
            .OrderBy(m => m.Identifier)
            .ToListAsync();
        
        ViewBag.Metrics = metrics;
        ViewBag.TasksCount = tasksCount;
        ViewBag.YourProductsCount = yourProductsCount;
        ViewBag.AllProductsCount = allProductsCount;
        
        return View("~/Views/ProductReporting/PerformanceMetrics/WhatYouNeedToReport.cshtml");
    }

    #endregion
}

