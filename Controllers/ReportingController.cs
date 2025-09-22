using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FipsReporting.Services;
using FipsReporting.Data;
using FipsReporting.Models;
using System.Security.Claims;

namespace FipsReporting.Controllers
{
    [AllowAnonymous] // Temporary for development
    [Route("reporting")]
    public class ReportingController : Controller
    {
        private readonly IReportingService _reportingService;
        private readonly CmsApiService _cmsApiService;
        private readonly IAuthenticationService _authenticationService;
        private readonly IPerformanceMetricService _performanceMetricService;
        private readonly IReportingStatusService _reportingStatusService;
        private readonly IMilestoneService _milestoneService;
        private readonly ILogger<ReportingController> _logger;

        public ReportingController(
            IReportingService reportingService,
            CmsApiService cmsApiService,
            IAuthenticationService authenticationService,
            IPerformanceMetricService performanceMetricService,
            IReportingStatusService reportingStatusService,
            IMilestoneService milestoneService,
            ILogger<ReportingController> logger)
        {
            _reportingService = reportingService;
            _cmsApiService = cmsApiService;
            _authenticationService = authenticationService;
            _performanceMetricService = performanceMetricService;
            _reportingStatusService = reportingStatusService;
            _milestoneService = milestoneService;
            _logger = logger;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Performance Reporting Dashboard";
            ViewData["ActiveNav"] = "reporting";
            ViewData["ActiveNavItem"] = "dashboard";

            // Get user's email from claims
            var userEmail = _authenticationService.GetUserEmailFromClaims(User);
            
            // For development, use a hardcoded email if no user is authenticated
            if (string.IsNullOrEmpty(userEmail))
            {
                userEmail = "andy.jones@education.gov.uk"; // Your email for testing (lowercase)
                _logger.LogInformation("No authenticated user found, using development email: {Email}", userEmail);
            }

            // Get products assigned to current user directly from product_contacts
            var assignedProducts = await _cmsApiService.GetProductsByUserEmailAsync(userEmail);

            // Debug logging
            _logger.LogInformation("Found {Count} assigned products for user {Email}", assignedProducts.Count, userEmail);
            foreach (var product in assignedProducts)
            {
                _logger.LogInformation("Product: {Title} (ID: {Id}, FipsId: {FipsId})", product.Title, product.Id, product.FipsId);
            }

            var reportingPeriods = GetDashboardReportingPeriods();
            var viewModel = new ReportingViewModel
            {
                AssignedProducts = assignedProducts,
                CurrentPeriod = GetCurrentReportingPeriod(),
                ReportingPeriods = reportingPeriods,
                DueReportsCount = reportingPeriods.Count(p => p.Status == "Due Soon"),
                OverdueReportsCount = reportingPeriods.Count(p => p.Status == "Overdue"),
                MilestonesCount = 0 // TODO: Get actual milestone count
            };

            return View(viewModel);
        }

        [HttpGet("products")]
        public async Task<IActionResult> Products()
        {
            ViewData["Title"] = "Your Products";
            ViewData["ActiveNav"] = "reporting";
            ViewData["ActiveNavItem"] = "products";

            // Get user's email from claims
            var userEmail = _authenticationService.GetUserEmailFromClaims(User);
            
            // For development, use a hardcoded email if no user is authenticated
            if (string.IsNullOrEmpty(userEmail))
            {
                userEmail = "andy.jones@education.gov.uk"; // Your email for testing (lowercase)
                _logger.LogInformation("No authenticated user found, using development email: {Email}", userEmail);
            }

            // Get products assigned to current user
            var assignedProducts = await _cmsApiService.GetProductsByUserEmailAsync(userEmail);

            // Map CmsProduct to ProductViewModel
            var productViewModels = assignedProducts.Select(p => _cmsApiService.MapToViewModel(p, true)).ToList();

            return View("~/Views/Reporting/Products/Index.cshtml", productViewModels);
        }

        [HttpGet]
        [Route("reporting/{year:int}/{month}/performance/{fipsId}")]
        public async Task<IActionResult> PerformanceByProduct(int year, string month, string fipsId)
        {
            ViewData["Title"] = $"Performance Reporting - {CapitalizeMonth(month)} {year}";
            ViewData["ActiveNav"] = "reporting";
            ViewData["ActiveNavItem"] = "performance";

            // Force use of andy.jones@education.gov.uk for development
            var userEmail = "andy.jones@education.gov.uk";
            _logger.LogInformation("Using development email: {Email}", userEmail);

            // Get products assigned to current user to verify access
            var assignedProducts = await _cmsApiService.GetProductsByUserEmailAsync(userEmail);
            var product = assignedProducts.FirstOrDefault(p => p.FipsId == fipsId);

            if (product == null)
            {
                TempData["Error"] = "Product not found or you don't have access to this product.";
                return RedirectToAction("PerformanceByMonth", new { year, month });
            }

            ViewBag.Year = year;
            ViewBag.Month = CapitalizeMonth(month);
            ViewBag.FipsId = fipsId;

            // Map CmsProduct to ProductViewModel
            var productViewModel = _cmsApiService.MapToViewModel(product, true);

            return View("~/Views/Reporting/PerformanceByProduct.cshtml", productViewModel);
        }

        [HttpGet]
        [Route("{year:int}/{month}/performance")]
        public async Task<IActionResult> PerformanceByMonth(int year, string month)
        {
            ViewData["Title"] = $"Performance Reporting - {CapitalizeMonth(month)} {year}";
            ViewData["ActiveNav"] = "reporting";
            ViewData["ActiveNavItem"] = "performance";

            // Force use of andy.jones@education.gov.uk for development
            var userEmail = "andy.jones@education.gov.uk";
            _logger.LogInformation("Using development email: {Email}", userEmail);

            // Get products assigned to current user
            var assignedProducts = await _cmsApiService.GetProductsByUserEmailAsync(userEmail);
            _logger.LogInformation("Found {Count} products for user {Email}", assignedProducts.Count, userEmail);
            
            // Log details about each product
            foreach (var product in assignedProducts)
            {
                _logger.LogInformation("Product: {Title}, FipsId: {FipsId}, Id: {Id}", product.Title, product.FipsId, product.Id);
            }

            // Map CmsProduct to ProductViewModel
            var productViewModels = assignedProducts.Select(p => _cmsApiService.MapToViewModel(p, true)).ToList();
            _logger.LogInformation("Mapped {Count} products to ProductViewModel", productViewModels.Count);
            
            // Log details about each mapped product
            foreach (var product in productViewModels)
            {
                _logger.LogInformation("Mapped Product: {Title}, FipsId: {FipsId}, Id: {Id}", product.Title, product.FipsId, product.Id);
            }

            ViewBag.Year = year;
            ViewBag.Month = CapitalizeMonth(month);

            return View("~/Views/Reporting/PerformanceByMonth.cshtml", productViewModels);
        }

        [HttpPost]
        [Route("reporting/{year:int}/{month}/performance/{fipsId}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PerformanceByProduct(int year, string month, string fipsId, string metrics)
        {
            ViewData["Title"] = $"Performance Reporting - {CapitalizeMonth(month)} {year}";
            ViewData["ActiveNav"] = "reporting";
            ViewData["ActiveNavItem"] = "performance";

            // Force use of andy.jones@education.gov.uk for development
            var userEmail = "andy.jones@education.gov.uk";
            _logger.LogInformation("Using development email: {Email}", userEmail);

            // Get products assigned to current user to verify access
            var assignedProducts = await _cmsApiService.GetProductsByUserEmailAsync(userEmail);
            var product = assignedProducts.FirstOrDefault(p => p.FipsId == fipsId);

            if (product == null)
            {
                TempData["Error"] = "Product not found or you don't have access to this product.";
                return RedirectToAction("PerformanceByMonth", new { year, month });
            }

            // TODO: Save the performance metrics to the database
            _logger.LogInformation("Saving performance metrics for product {FipsId}: {Metrics}", fipsId, metrics);

            TempData["Success"] = $"Performance return for {CapitalizeMonth(month)} {year} has been saved successfully.";
            return RedirectToAction("PerformanceByMonth", new { year, month });
        }

        [HttpGet]
        [Route("reporting/performance")]
        public async Task<IActionResult> Performance()
        {
            ViewData["Title"] = "Monthly Performance Reporting";
            ViewData["ActiveNav"] = "reporting";
            ViewData["ActiveNavItem"] = "performance";

            // Get user's email from claims
            var userEmail = _authenticationService.GetUserEmailFromClaims(User);
            
            // For development, use a hardcoded email if no user is authenticated
            if (string.IsNullOrEmpty(userEmail))
            {
                userEmail = "andy.jones@education.gov.uk"; // Your email for testing (lowercase)
                _logger.LogInformation("No authenticated user found, using development email: {Email}", userEmail);
            }

            // Get products assigned to current user
            var assignedProducts = await _cmsApiService.GetProductsByUserEmailAsync(userEmail);

            // Use the same reporting periods as the dashboard for consistency
            var reportingPeriods = GetDashboardReportingPeriods();

            var viewModel = new ReportingViewModel
            {
                AssignedProducts = assignedProducts,
                ReportingPeriods = reportingPeriods,
                DueReportsCount = reportingPeriods.Count(p => p.Status == "Due Soon"),
                OverdueReportsCount = reportingPeriods.Count(p => p.Status == "Overdue"),
                MilestonesCount = 0 // TODO: Get actual milestone count
            };

            return View(viewModel);
        }

        [HttpGet("milestones")]
        public async Task<IActionResult> Milestones()
        {
            ViewData["Title"] = "Milestones";
            ViewData["ActiveNav"] = "reporting";
            ViewData["ActiveNavItem"] = "milestones";

            // Force use of andy.jones@education.gov.uk for development
            var userEmail = "andy.jones@education.gov.uk";
            _logger.LogInformation("Using development email: {Email}", userEmail);

            // Get products assigned to current user
            var assignedProducts = await _cmsApiService.GetProductsByUserEmailAsync(userEmail);

            // Create a list of products with milestone counts
            var productsWithMilestoneCounts = new List<object>();
            foreach (var product in assignedProducts)
            {
                var milestones = await _milestoneService.GetMilestonesByFipsIdAsync(product.FipsId);
                productsWithMilestoneCounts.Add(new
                {
                    ProductName = product.Title,
                    FipsId = product.FipsId,
                    MilestoneCount = milestones.Count
                });
            }

            return View("~/Views/Reporting/Milestones/Index.cshtml", productsWithMilestoneCounts);
        }

        [HttpGet("{year:int}/{month}")]
        public async Task<IActionResult> Month(int year, string month)
        {
            var monthName = CapitalizeMonth(month);
            var fullMonthName = $"{monthName} {year}";
            
            ViewData["Title"] = $"Reporting {fullMonthName}";
            ViewData["ActiveNav"] = "reporting";

            // Get user's email from claims
            var userEmail = _authenticationService.GetUserEmailFromClaims(User);
            
            // For development, use a hardcoded email if no user is authenticated
            if (string.IsNullOrEmpty(userEmail))
            {
                userEmail = "andy.jones@education.gov.uk"; // Your email for testing (lowercase)
                _logger.LogInformation("No authenticated user found, using development email: {Email}", userEmail);
            }

            // Get products assigned to current user directly from product_contacts
            var assignedProducts = await _cmsApiService.GetProductsByUserEmailAsync(userEmail);

            // Debug logging
            _logger.LogInformation("Found {Count} assigned products for user {Email}", assignedProducts.Count, userEmail);
            foreach (var product in assignedProducts)
            {
                _logger.LogInformation("Product: {Title} (ID: {Id}, FipsId: {FipsId})", product.Title, product.Id, product.FipsId);
            }

            var reportingPeriod = $"{year}-{month.ToLower()}";
            var dueDate = GetDueDateForMonth(fullMonthName);
            
            // Calculate status information
            var dueDateStatus = await _reportingStatusService.GetDueDateStatusAsync(dueDate);
            var (completedCount, totalCount) = await _reportingStatusService.GetServiceCompletionCountAsync(userEmail, reportingPeriod);
            var isSubmitted = await _reportingStatusService.IsReportSubmittedAsync(userEmail, reportingPeriod);
            
            var overallSubmissionStatus = isSubmitted ? "Submitted" : 
                                        completedCount == totalCount && totalCount > 0 ? "Ready to submit" : 
                                        "Cannot submit";

            // Calculate status for each product
            var productStatuses = new Dictionary<string, string>();
            foreach (var product in assignedProducts)
            {
                var productStatus = await _reportingStatusService.GetPerformanceStatusAsync(product.FipsId, reportingPeriod);
                productStatuses[product.FipsId] = productStatus;
            }

            var viewModel = new MonthReportingViewModel
            {
                Year = year,
                Month = monthName,
                FullMonthName = fullMonthName,
                AssignedProducts = assignedProducts,
                DueDate = dueDate,
                ReportingPeriods = GetReportingPeriods(),
                OverallSubmissionStatus = overallSubmissionStatus,
                DueDateStatus = dueDateStatus,
                CompletedServicesCount = completedCount,
                TotalServicesCount = totalCount,
                IsSubmitted = isSubmitted,
                ProductStatuses = productStatuses
            };

            return View(viewModel);
        }

        [HttpGet("{year:int}/{month}/service/{fipsId}")]
        public async Task<IActionResult> Service(int year, string month, string fipsId)
        {
            var monthName = CapitalizeMonth(month);
            var fullMonthName = $"{monthName} {year}";
            var reportingPeriod = $"{year}-{month.ToLower()}";
            
            ViewData["Title"] = $"Service Summary - {fipsId}";
            ViewData["ActiveNav"] = "reporting";

            // Get user's email from claims
            var userEmail = _authenticationService.GetUserEmailFromClaims(User);
            
            // For development, use a hardcoded email if no user is authenticated
            if (string.IsNullOrEmpty(userEmail))
            {
                userEmail = "andy.jones@education.gov.uk"; // Your email for testing (lowercase)
                _logger.LogInformation("No authenticated user found, using development email: {Email}", userEmail);
            }

            // Get products assigned to current user to find the specific product
            var assignedProducts = await _cmsApiService.GetProductsByUserEmailAsync(userEmail);
            var product = assignedProducts.FirstOrDefault(p => p.FipsId == fipsId);

            if (product == null)
            {
                return NotFound($"Product with FIPS ID '{fipsId}' not found or you don't have access to this product.");
            }

            // Get performance metrics summary
            var metrics = await _performanceMetricService.GetActiveMetricsAsync();
            var existingData = await _performanceMetricService.GetMetricDataForProductAsync(fipsId, reportingPeriod);
            var completedMetrics = existingData.Count(d => !string.IsNullOrEmpty(d.Value) || d.IsNullReturn);
            var totalMetrics = metrics.Count;

            // Calculate RAG status based on completion
            string ragStatus;
            if (completedMetrics == totalMetrics)
                ragStatus = "Green";
            else if (completedMetrics > 0)
                ragStatus = "Amber";
            else
                ragStatus = "Red";

            // Calculate progress status
            string progressStatus;
            var dueDate = GetDueDateForMonth(fullMonthName);
            var daysUntilDue = (dueDate - DateTime.Now).Days;
            
            if (daysUntilDue < 0)
                progressStatus = "Off track";
            else if (daysUntilDue <= 7)
                progressStatus = "At risk";
            else
                progressStatus = "On track";

            // Create sample data for demonstration
            var viewModel = new ServiceSummaryViewModel
            {
                Year = year,
                Month = monthName,
                FullMonthName = fullMonthName,
                FipsId = fipsId,
                Product = product,
                UserEmail = userEmail,
                ReportingPeriod = reportingPeriod,
                
                // RAG Status
                RagStatus = ragStatus,
                RagDescription = completedMetrics == totalMetrics ? "All metrics completed" : 
                                completedMetrics > 0 ? "Partially completed" : "Not started",
                RagHistory = new List<string> { "G", "A", "R", "A" }, // Sample history
                
                // Progress Status
                ProgressStatus = progressStatus,
                NextUpdateDue = dueDate,
                
                // Outcomes
                Outcomes = new List<string>
                {
                    "Ensure service meets performance standards and user needs",
                    "Maintain compliance with government digital service standards",
                    "Deliver measurable improvements in service delivery"
                },
                
                // Milestones
                Milestones = new List<ServiceMilestone>
                {
                    new ServiceMilestone
                    {
                        Name = "Complete performance metrics reporting",
                        DueDate = dueDate,
                        LastMonthRag = "Amber",
                        CurrentRag = ragStatus,
                        Change = completedMetrics > 0 ? "Improved" : "Unchanged",
                        Url = $"/reporting/{year}/{month}/service/{fipsId}/performance"
                    },
                    new ServiceMilestone
                    {
                        Name = "Submit monthly compliance report",
                        DueDate = dueDate,
                        LastMonthRag = "Green",
                        CurrentRag = completedMetrics == totalMetrics ? "Green" : "Amber",
                        Change = completedMetrics == totalMetrics ? "Improved" : "Worsened",
                        Url = $"/reporting/{year}/{month}/service/{fipsId}/submit"
                    },
                    new ServiceMilestone
                    {
                        Name = "Review and update service documentation",
                        DueDate = dueDate.AddDays(14),
                        LastMonthRag = "Red",
                        CurrentRag = "Red",
                        Change = "Unchanged",
                        Url = $"/reporting/{year}/{month}/service/{fipsId}/documentation"
                    }
                },
                
                // Performance Metrics Summary
                TotalMetrics = totalMetrics,
                CompletedMetrics = completedMetrics,
                OverdueMetrics = daysUntilDue < 0 ? totalMetrics - completedMetrics : 0
            };

            return View("ServiceSummary", viewModel);
        }

        [HttpGet]
        [Route("reporting/{year}/{month}/service/{fipsId}/performance")]
        public async Task<IActionResult> Performance(int year, string month, string fipsId)
        {
            var monthName = CapitalizeMonth(month);
            var fullMonthName = $"{monthName} {year}";
            var reportingPeriod = $"{year}-{month.ToLower()}";
            
            ViewData["Title"] = $"Performance Metrics - {fipsId}";
            ViewData["ActiveNav"] = "reporting";

            // Get user's email from claims
            var userEmail = _authenticationService.GetUserEmailFromClaims(User);
            
            // For development, use a hardcoded email if no user is authenticated
            if (string.IsNullOrEmpty(userEmail))
            {
                userEmail = "andy.jones@education.gov.uk"; // Your email for testing (lowercase)
                _logger.LogInformation("No authenticated user found, using development email: {Email}", userEmail);
            }

            // Get products assigned to current user to find the specific product
            var assignedProducts = await _cmsApiService.GetProductsByUserEmailAsync(userEmail);
            var product = assignedProducts.FirstOrDefault(p => p.FipsId == fipsId);

            if (product == null)
            {
                return NotFound($"Product with FIPS ID '{fipsId}' not found or you don't have access to this product.");
            }

            // Get all active performance metrics
            var metrics = await _performanceMetricService.GetActiveMetricsAsync();
            
            // Get existing data for this product and reporting period
            var existingData = await _performanceMetricService.GetMetricDataForProductAsync(fipsId, reportingPeriod);
            
            // Calculate status information
            var performanceStatus = await _reportingStatusService.GetPerformanceStatusAsync(fipsId, reportingPeriod);
            var submissionStatus = await _reportingStatusService.GetSubmissionStatusAsync(fipsId, reportingPeriod);
            var dueDate = GetDueDateForMonth(fullMonthName);
            var isSubmitted = await _reportingStatusService.IsReportSubmittedAsync(userEmail, reportingPeriod);

            var viewModel = new PerformanceTaskListViewModel
            {
                Year = year,
                Month = monthName,
                FullMonthName = fullMonthName,
                FipsId = fipsId,
                Product = product,
                UserEmail = userEmail,
                ReportingPeriod = reportingPeriod,
                Metrics = metrics,
                ExistingData = existingData.ToDictionary(d => d.PerformanceMetricId, d => d),
                PerformanceStatus = performanceStatus,
                SubmissionStatus = submissionStatus,
                DueDate = dueDate,
                IsSubmitted = isSubmitted
            };

            return View(viewModel);
        }

        [HttpGet("{year:int}/{month}/service/{fipsId}/metric/{uniqueId}")]
        public async Task<IActionResult> PerformanceMetric(int year, string month, string fipsId, string uniqueId)
        {
            var monthName = CapitalizeMonth(month);
            var fullMonthName = $"{monthName} {year}";
            var reportingPeriod = $"{year}-{month.ToLower()}";
            
            ViewData["Title"] = $"Performance Metric - {uniqueId}";
            ViewData["ActiveNav"] = "reporting";

            // Get user's email from claims
            var userEmail = _authenticationService.GetUserEmailFromClaims(User);
            
            // For development, use a hardcoded email if no user is authenticated
            if (string.IsNullOrEmpty(userEmail))
            {
                userEmail = "andy.jones@education.gov.uk"; // Your email for testing (lowercase)
                _logger.LogInformation("No authenticated user found, using development email: {Email}", userEmail);
            }

            // Get products assigned to current user to find the specific product
            var assignedProducts = await _cmsApiService.GetProductsByUserEmailAsync(userEmail);
            var product = assignedProducts.FirstOrDefault(p => p.FipsId == fipsId);

            if (product == null)
            {
                return NotFound($"Product with FIPS ID '{fipsId}' not found or you don't have access to this product.");
            }

            // Get the specific metric by unique ID
            var metric = await _performanceMetricService.GetMetricByUniqueIdAsync(uniqueId);
            if (metric == null)
            {
                return NotFound($"Performance metric with ID '{uniqueId}' not found.");
            }

            // Get existing data for this specific metric
            var existingData = await _performanceMetricService.GetMetricDataForProductAsync(fipsId, reportingPeriod);
            var metricData = existingData.FirstOrDefault(d => d.PerformanceMetricId == metric.Id);

            var viewModel = new PerformanceMetricViewModel
            {
                Year = year,
                Month = monthName,
                FullMonthName = fullMonthName,
                FipsId = fipsId,
                Product = product,
                UserEmail = userEmail,
                ReportingPeriod = reportingPeriod,
                Metric = metric,
                ExistingData = metricData
            };

            return View(viewModel);
        }

        [HttpPost("save-performance-metric-data")]
        public async Task<IActionResult> SavePerformanceMetricData(int Year, string Month, string FipsId, int MetricId, string? Value, bool IsNullReturn = false)
        {
            try
            {
                var userEmail = _authenticationService.GetUserEmailFromClaims(User);
                if (string.IsNullOrEmpty(userEmail))
                {
                    userEmail = "andy.jones@education.gov.uk"; // Development fallback
                }

                var reportingPeriod = $"{Year}-{Month.ToLower()}";

                // Get the metric to validate against
                var metric = await _performanceMetricService.GetMetricByIdAsync(MetricId);
                if (metric == null)
                {
                    TempData["ErrorMessage"] = "Performance metric not found.";
                    return RedirectToAction("Performance", new { year = Year, month = Month, fipsId = FipsId });
                }

                // Validate the data
                var validationResult = ValidateMetricData(metric, Value, IsNullReturn);
                
                if (!validationResult.IsValid)
                {
                    TempData["ErrorMessage"] = validationResult.ErrorMessage;
                    TempData["FieldError"] = validationResult.FieldError;
                    TempData["FormValue"] = Value;
                    TempData["FormIsNullReturn"] = IsNullReturn;
                    return RedirectToAction("PerformanceMetric", new { year = Year, month = Month, fipsId = FipsId, uniqueId = metric.UniqueId });
                }

                var data = new PerformanceMetricData
                {
                    PerformanceMetricId = MetricId,
                    ProductId = FipsId,
                    ReportingPeriod = reportingPeriod,
                    Value = Value,
                    IsNullReturn = IsNullReturn,
                    SubmittedBy = userEmail,
                    SubmittedAt = DateTime.UtcNow
                };

                await _performanceMetricService.SaveMetricDataAsync(data);

                TempData["SuccessMessage"] = "Performance data saved successfully.";
                return RedirectToAction("Performance", new { year = Year, month = Month, fipsId = FipsId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving performance metric data");
                TempData["ErrorMessage"] = "An error occurred while saving the performance data.";
                return RedirectToAction("Performance", new { year = Year, month = Month, fipsId = FipsId });
            }
        }

        private (bool IsValid, string ErrorMessage, string FieldError) ValidateMetricData(PerformanceMetric metric, string? value, bool isNullReturn)
        {
            // If null return is allowed and selected, skip validation
            if (isNullReturn && metric.CanReportNullReturn)
            {
                return (true, "", "");
            }

            // Check if mandatory field is empty
            if (metric.Mandatory && string.IsNullOrWhiteSpace(value))
            {
                return (false, $"'{metric.Name}' is mandatory and must be completed.", "value");
            }

            // If value is empty and not mandatory, it's valid
            if (string.IsNullOrWhiteSpace(value))
            {
                return (true, "", "");
            }

            // Validate based on measure type
            switch (metric.Measure)
            {
                case "number":
                    if (!int.TryParse(value, out int intValue))
                    {
                        return (false, $"'{metric.Name}' must be a whole number.", "value");
                    }
                    var intRangeResult = ValidateNumericRange(metric, intValue);
                    return intRangeResult.IsValid ? (true, "", "") : (false, intRangeResult.ErrorMessage, "value");

                case "decimal":
                    if (!decimal.TryParse(value, out decimal decimalValue))
                    {
                        return (false, $"'{metric.Name}' must be a decimal number.", "value");
                    }
                    var decimalRangeResult = ValidateNumericRange(metric, decimalValue);
                    return decimalRangeResult.IsValid ? (true, "", "") : (false, decimalRangeResult.ErrorMessage, "value");

                case "boolean":
                    if (value != "Yes" && value != "No")
                    {
                        return (false, $"'{metric.Name}' must be either 'Yes' or 'No'.", "value");
                    }
                    return (true, "", "");

                case "single_option":
                case "multiple_option":
                    // Validate against validation criteria options
                    if (!string.IsNullOrEmpty(metric.ValidationCriteria))
                    {
                        var validOptions = metric.ValidationCriteria.Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(opt => opt.Trim()).ToList();
                        
                        if (metric.Measure == "single_option")
                        {
                            if (!validOptions.Contains(value))
                            {
                                return (false, $"'{metric.Name}' must be one of: {string.Join(", ", validOptions)}", "value");
                            }
                        }
                        else if (metric.Measure == "multiple_option")
                        {
                            var selectedValues = value.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                .Select(v => v.Trim()).ToList();
                            
                            foreach (var selectedValue in selectedValues)
                            {
                                if (!validOptions.Contains(selectedValue))
                                {
                                    return (false, $"'{metric.Name}' contains invalid option: {selectedValue}. Valid options are: {string.Join(", ", validOptions)}", "value");
                                }
                            }
                        }
                    }
                    return (true, "", "");

                default:
                    return (true, "", "");
            }
        }

        private (bool IsValid, string ErrorMessage) ValidateNumericRange(PerformanceMetric metric, IComparable value)
        {
            if (string.IsNullOrEmpty(metric.ValidationCriteria))
                return (true, "");

            var criteria = metric.ValidationCriteria.Trim('"');
            
            // Convert the value to decimal for comparison
            decimal decimalValue;
            if (value is int intValue)
            {
                decimalValue = intValue;
            }
            else if (value is decimal decValue)
            {
                decimalValue = decValue;
            }
            else if (decimal.TryParse(value.ToString(), out decimal parsedValue))
            {
                decimalValue = parsedValue;
            }
            else
            {
                return (false, $"'{metric.Name}' must be a valid number.");
            }
            
            var parts = criteria.Split(',');
            
            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                if (trimmed.StartsWith("min:"))
                {
                    var minStr = trimmed.Substring(4);
                    if (decimal.TryParse(minStr, out decimal minValue))
                    {
                        if (decimalValue < minValue)
                        {
                            return (false, $"'{metric.Name}' must be at least {minValue}.");
                        }
                    }
                }
                else if (trimmed.StartsWith("max:"))
                {
                    var maxStr = trimmed.Substring(4);
                    if (decimal.TryParse(maxStr, out decimal maxValue))
                    {
                        if (decimalValue > maxValue)
                        {
                            return (false, $"'{metric.Name}' must be no more than {maxValue}.");
                        }
                    }
                }
            }
            
            return (true, "");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SavePerformanceData(PerformanceDataSubmissionViewModel model)
        {
            try
            {
                var userEmail = _authenticationService.GetUserEmailFromClaims(User);
                if (string.IsNullOrEmpty(userEmail))
                {
                    userEmail = "andy.jones@education.gov.uk"; // Development fallback
                }

                foreach (var metricData in model.MetricData)
                {
                    var data = new PerformanceMetricData
                    {
                        PerformanceMetricId = metricData.MetricId,
                        ProductId = model.FipsId,
                        ReportingPeriod = model.ReportingPeriod,
                        Value = metricData.Value,
                        Comment = metricData.Comment,
                        IsNullReturn = metricData.IsNullReturn,
                        SubmittedBy = userEmail,
                        SubmittedAt = DateTime.UtcNow
                    };

                    await _performanceMetricService.SaveMetricDataAsync(data);
                }

                TempData["SuccessMessage"] = "Performance data saved successfully.";
                return RedirectToAction("Performance", new { year = model.Year, month = model.Month, fipsId = model.FipsId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving performance data");
                TempData["ErrorMessage"] = "An error occurred while saving the performance data.";
                return RedirectToAction("Performance", new { year = model.Year, month = model.Month, fipsId = model.FipsId });
            }
        }

        [HttpGet]
        [Route("reporting/milestones/product/{productId}")]
        public async Task<IActionResult> Milestones(int productId)
        {
            ViewData["Title"] = "Milestone Reporting";
            ViewData["ActiveNav"] = "reporting";

            // Force use of andy.jones@education.gov.uk for development
            var userEmail = "andy.jones@education.gov.uk";
            _logger.LogInformation("Using development email: {Email}", userEmail);

            // Get products assigned to current user to verify access
            var assignedProducts = await _cmsApiService.GetProductsByUserEmailAsync(userEmail);
            var product = assignedProducts.FirstOrDefault(p => p.FipsId == productId.ToString());

            if (product == null)
            {
                TempData["Error"] = "Product not found or you don't have access to this product.";
                return RedirectToAction("Milestones");
            }

            // Get milestones for this product
            var milestones = await _milestoneService.GetMilestonesByFipsIdAsync(productId.ToString());
            
            // Add product name to each milestone
            foreach (var milestone in milestones)
            {
                milestone.ProductName = product.Title;
            }

            ViewBag.ProductName = product.Title;
            ViewBag.FipsId = productId;

            return View("~/Views/Reporting/Milestones/ProductMilestones.cshtml", milestones);
        }

        private ReportingPeriod GetCurrentReportingPeriod()
        {
            var now = DateTime.Now;
            var currentMonth = new DateTime(now.Year, now.Month, 1);
            
            return new ReportingPeriod
            {
                Month = currentMonth.ToString("MMMM yyyy"),
                DueDate = new DateTime(currentMonth.Year, currentMonth.Month, 5).AddMonths(1),
                Status = GetPeriodStatus(currentMonth),
                Period = $"1 to {DateTime.DaysInMonth(currentMonth.Year, currentMonth.Month)} {currentMonth.ToString("MMMM")}"
            };
        }

        private List<ReportingPeriod> GetReportingPeriods()
        {
            var periods = new List<ReportingPeriod>();
            var now = DateTime.Now;
            
            // August 2025 (Overdue)
            periods.Add(new ReportingPeriod
            {
                Month = "August 2025",
                DueDate = new DateTime(2025, 9, 5),
                Status = "Overdue",
                Period = "1 to 31 August"
            });
            
            // September 2025 (Upcoming - due by 5 October)
            periods.Add(new ReportingPeriod
            {
                Month = "September 2025",
                DueDate = new DateTime(2025, 10, 5),
                Status = "Upcoming",
                Period = "1 to 30 September"
            });
            
            // October 2025 (Upcoming - due 5 November)
            periods.Add(new ReportingPeriod
            {
                Month = "October 2025",
                DueDate = new DateTime(2025, 11, 5),
                Status = "Upcoming",
                Period = "1 to 31 October"
            });
            
            return periods;
        }

        private List<ReportingPeriod> GetDashboardReportingPeriods()
        {
            var periods = new List<ReportingPeriod>();
            var now = DateTime.Now;
            
            // August 2025 (Overdue)
            periods.Add(new ReportingPeriod
            {
                Month = "August 2025",
                DueDate = new DateTime(2025, 9, 5),
                Status = "Overdue",
                Period = "1 to 31 August"
            });
            
            // September 2025 (Due Soon - due by 5 October)
            periods.Add(new ReportingPeriod
            {
                Month = "September 2025",
                DueDate = new DateTime(2025, 10, 5),
                Status = "Due Soon",
                Period = "1 to 30 September"
            });
            
            return periods;
        }

        private DateTime GetDueDateForMonth(string month)
        {
            // Parse month string (e.g., "August 2025") and return due date (7th of following month)
            if (DateTime.TryParseExact(month, "MMMM yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime monthDate))
            {
                return monthDate.AddMonths(1).AddDays(6); // 7th of following month
            }
            return DateTime.Now.AddDays(7); // Default fallback
        }

        private string CapitalizeMonth(string month)
        {
            if (string.IsNullOrEmpty(month))
                return month;
            
            return char.ToUpper(month[0]) + month.Substring(1).ToLower();
        }

        private string GetPeriodStatus(DateTime period)
        {
            var dueDate = new DateTime(period.Year, period.Month, 5).AddMonths(1);
            var now = DateTime.Now;
            
            if (now > dueDate)
                return "Overdue";
            else if (now.AddDays(7) > dueDate)
                return "Due Soon";
            else
                return "Upcoming";
        }

        [HttpGet("test-product-filtering")]
        public async Task<IActionResult> TestProductFiltering(string userEmail)
        {
            ViewData["Title"] = "Test Product Filtering";
            
            if (string.IsNullOrEmpty(userEmail))
            {
                return Json(new { error = "Please provide userEmail parameter" });
            }

            // Get products assigned to the specified user directly from product_contacts
            var assignedProducts = await _cmsApiService.GetProductsByUserEmailAsync(userEmail);

            var result = new
            {
                UserEmail = userEmail,
                AssignedProductsCount = assignedProducts.Count,
                AssignedProducts = assignedProducts.Select(p => new
                {
                    Id = p.Id,
                    Title = p.Title,
                    FipsId = p.FipsId,
                    State = p.State,
                    Contacts = p.ProductContacts?.Where(pc => 
                        pc.User?.Email?.Equals(userEmail, StringComparison.OrdinalIgnoreCase) == true
                    ).Select(pc => new
                    {
                        Role = pc.Role,
                        UserEmail = pc.User?.Email
                    }).ToList()
                }).ToList()
            };

            return Json(result);
        }

        [HttpPost("{year:int}/{month}/submit")]
        public async Task<IActionResult> SubmitReport(int year, string month)
        {
            try
            {
                var userEmail = _authenticationService.GetUserEmailFromClaims(User);
                if (string.IsNullOrEmpty(userEmail))
                {
                    userEmail = "andy.jones@education.gov.uk"; // Development fallback
                }

                var reportingPeriod = $"{year}-{month.ToLower()}";

                // Mark all performance metric data for this user and period as submitted
                var metricData = await _performanceMetricService.GetMetricDataForUserAsync(userEmail, reportingPeriod);
                
                foreach (var data in metricData)
                {
                    data.IsSubmitted = true;
                    data.UpdatedAt = DateTime.UtcNow;
                }

                await _performanceMetricService.UpdateMetricDataAsync(metricData);

                TempData["SuccessMessage"] = "Report submitted successfully.";
                return RedirectToAction("Month", new { year = year, month = month });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting report for {Year}-{Month}", year, month);
                TempData["ErrorMessage"] = "An error occurred while submitting the report.";
                return RedirectToAction("Month", new { year = year, month = month });
            }
        }

        [HttpPost("{year:int}/{month}/{fipsId}/submit")]
        public async Task<IActionResult> SubmitProductReport(int year, string month, string fipsId)
        {
            try
            {
                var userEmail = _authenticationService.GetUserEmailFromClaims(User);
                if (string.IsNullOrEmpty(userEmail))
                {
                    userEmail = "andy.jones@education.gov.uk"; // Development fallback
                }

                var reportingPeriod = $"{year}-{month.ToLower()}";

                // Get all metric data for this specific product and user
                var metricData = await _performanceMetricService.GetMetricDataForUserAsync(userEmail, reportingPeriod);
                var productMetricData = metricData.Where(d => d.ProductId == fipsId).ToList();
                
                foreach (var data in productMetricData)
                {
                    data.IsSubmitted = true;
                    data.UpdatedAt = DateTime.UtcNow;
                }

                await _performanceMetricService.UpdateMetricDataAsync(productMetricData);

                TempData["SuccessMessage"] = "Product report submitted successfully.";
                return RedirectToAction("Performance", new { year = year, month = month, fipsId = fipsId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting product report for {FipsId} in {Year}-{Month}", fipsId, year, month);
                TempData["ErrorMessage"] = "An error occurred while submitting the product report.";
                return RedirectToAction("Performance", new { year = year, month = month, fipsId = fipsId });
            }
        }
    }
}
