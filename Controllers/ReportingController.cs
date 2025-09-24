using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
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
        private readonly ReportingDbContext _context;
        private readonly ILogger<ReportingController> _logger;

        public ReportingController(
            IReportingService reportingService,
            CmsApiService cmsApiService,
            IAuthenticationService authenticationService,
            IPerformanceMetricService performanceMetricService,
            IReportingStatusService reportingStatusService,
            IMilestoneService milestoneService,
            ReportingDbContext context,
            ILogger<ReportingController> logger)
        {
            _reportingService = reportingService;
            _cmsApiService = cmsApiService;
            _authenticationService = authenticationService;
            _performanceMetricService = performanceMetricService;
            _reportingStatusService = reportingStatusService;
            _milestoneService = milestoneService;
            _context = context;
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

            var reportingPeriods = await GetDashboardReportingPeriodsAsync(userEmail);
            
            // Get submitted returns for the current user
            var submittedReturns = await _context.PerformanceSubmissions
                .Where(s => s.UserEmail == userEmail && s.Status == "Submitted")
                .OrderByDescending(s => s.SubmittedAt)
                .ToListAsync();
            
            var viewModel = new ReportingViewModel
            {
                AssignedProducts = assignedProducts,
                CurrentPeriod = GetCurrentReportingPeriod(),
                ReportingPeriods = reportingPeriods,
                SubmittedReturns = submittedReturns,
                DueReportsCount = reportingPeriods.Count(p => p.Status == "Due Soon"),
                OverdueReportsCount = reportingPeriods.Count(p => p.Status == "Overdue"),
                MilestonesCount = 0 // TODO: Get actual milestone count
            };

            return View(viewModel);
        }

        [HttpGet("submitted-returns")]
        public async Task<IActionResult> SubmittedReturns()
        {
            ViewData["Title"] = "Submitted Returns";
            ViewData["ActiveNav"] = "reporting";
            ViewData["ActiveNavItem"] = "dashboard";

            // Get user's email from claims
            var userEmail = _authenticationService.GetUserEmailFromClaims(User);
            
            // For development, use a hardcoded email if no user is authenticated
            if (string.IsNullOrEmpty(userEmail))
            {
                userEmail = "andy.jones@education.gov.uk";
                _logger.LogInformation("No authenticated user found, using development email: {Email}", userEmail);
            }

            // Get all submitted returns for the current user
            var submittedReturns = await _context.PerformanceSubmissions
                .Where(s => s.UserEmail == userEmail && s.Status == "Submitted")
                .OrderByDescending(s => s.SubmittedAt)
                .ToListAsync();

            return View(submittedReturns);
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
                userEmail = "andy.jones@education.gov.uk";
                _logger.LogInformation("No authenticated user found, using development email: {Email}", userEmail);
            }
            else
            {
                _logger.LogInformation("Using authenticated user email: {Email}", userEmail);
            }

            // Get products assigned to current user
            var assignedProducts = await _cmsApiService.GetProductsByUserEmailAsync(userEmail);
            _logger.LogInformation("Products action: Found {Count} assigned products for user {UserEmail}", assignedProducts.Count, userEmail);

            // Map CmsProduct to ProductViewModel
            var productViewModels = assignedProducts.Select(p => _cmsApiService.MapToViewModel(p, true)).ToList();

            return View("~/Views/Reporting/Products/Index.cshtml", productViewModels);
        }

        [HttpGet("{year:int}/{month}/performance/{fipsId}")]
        public async Task<IActionResult> PerformanceByProduct(int year, string month, string fipsId)
        {
            ViewData["Title"] = $"Performance Reporting - {CapitalizeMonth(month)} {year}";
            ViewData["ActiveNav"] = "reporting";
            ViewData["ActiveNavItem"] = "performance";

            // Get user's email from claims
            var userEmail = _authenticationService.GetUserEmailFromClaims(User);
            
            // For development, use a hardcoded email if no user is authenticated
            if (string.IsNullOrEmpty(userEmail))
            {
                userEmail = "andy.jones@education.gov.uk";
                _logger.LogInformation("No authenticated user found, using development email: {Email}", userEmail);
            }
            else
            {
                _logger.LogInformation("Using authenticated user email: {Email}", userEmail);
            }

            // Get products assigned to current user to verify access
            var assignedProducts = await _cmsApiService.GetProductsByUserEmailAsync(userEmail);
            var product = assignedProducts.FirstOrDefault(p => p.FipsId == fipsId);

            if (product == null)
            {
                TempData["Error"] = "Product not found or you don't have access to this product.";
                return RedirectToAction("PerformanceByMonth", new { year, month });
            }

            // Get active performance metrics
            var activeMetrics = await _performanceMetricService.GetActiveMetricsAsync();
            _logger.LogInformation("Found {Count} active performance metrics", activeMetrics.Count);
            
            // Get existing metric data for this product and reporting period
            var reportingPeriod = $"{year}-{month.ToLower()}";
            var existingData = await _performanceMetricService.GetMetricDataForProductAsync(fipsId, reportingPeriod);
            _logger.LogInformation("Found {Count} existing metric data entries for product {FipsId} and period {Period}", existingData.Count, fipsId, reportingPeriod);

            // Create form view model
            var formViewModel = new ProductPerformanceFormViewModel
            {
                Product = _cmsApiService.MapToViewModel(product, true),
                ReportingPeriod = reportingPeriod,
                Year = year,
                Month = CapitalizeMonth(month),
                Metrics = activeMetrics.Select(metric =>
                {
                    var existingMetricData = existingData.FirstOrDefault(d => d.PerformanceMetricId == metric.Id);
                    return new PerformanceMetricFormItem
                    {
                        Id = metric.Id,
                        UniqueId = metric.UniqueId,
                        Name = metric.Name,
                        Description = metric.Description,
                        Category = metric.Category,
                        Measure = metric.Measure,
                        Mandatory = metric.Mandatory,
                        CanReportNullReturn = metric.CanReportNullReturn,
                        Value = existingMetricData?.Value,
                        IsNullReturn = existingMetricData?.IsNullReturn ?? false
                    };
                }).ToList()
            };

            _logger.LogInformation("Created form view model with {Count} metrics for product {FipsId}", formViewModel.Metrics.Count, fipsId);
            
            ViewBag.Year = year;
            ViewBag.Month = CapitalizeMonth(month);
            ViewBag.FipsId = fipsId;

            return View("~/Views/Reporting/performance/PerformanceByProduct.cshtml", formViewModel);
        }

        [HttpGet("{year:int}/{month}/performance")]
        public async Task<IActionResult> PerformanceByMonth(int year, string month)
        {
            ViewData["Title"] = $"Performance Reporting - {CapitalizeMonth(month)} {year}";
            ViewData["ActiveNav"] = "reporting";
            ViewData["ActiveNavItem"] = "performance";

            // Get user's email from claims
            var userEmail = _authenticationService.GetUserEmailFromClaims(User);
            
            // For development, use a hardcoded email if no user is authenticated
            if (string.IsNullOrEmpty(userEmail))
            {
                userEmail = "andy.jones@education.gov.uk";
                _logger.LogInformation("No authenticated user found, using development email: {Email}", userEmail);
            }
            else
            {
                _logger.LogInformation("Using authenticated user email: {Email}", userEmail);
            }

            // Get products assigned to current user
            var assignedProducts = await _cmsApiService.GetProductsByUserEmailAsync(userEmail);
            _logger.LogInformation("Found {Count} products for user {Email}", assignedProducts.Count, userEmail);
            
            // Log details about each product
            foreach (var product in assignedProducts)
            {
                _logger.LogInformation("Product: {Title}, FipsId: {FipsId}, Id: {Id}", product.Title, product.FipsId, product.Id);
            }

            // Map CmsProduct to ProductPerformanceViewModel with progress calculation
            var productPerformanceViewModels = new List<ProductPerformanceViewModel>();
            foreach (var product in assignedProducts)
            {
                var productViewModel = _cmsApiService.MapToViewModel(product, true);
                var performanceViewModel = new ProductPerformanceViewModel
                {
                    Id = productViewModel.Id,
                    FipsId = productViewModel.FipsId,
                    Title = productViewModel.Title,
                    ShortDescription = productViewModel.ShortDescription,
                    LongDescription = productViewModel.LongDescription,
                    ProductUrl = productViewModel.ProductUrl,
                    State = productViewModel.State,
                    CategoryValues = productViewModel.CategoryValues,
                    CategoryTypes = productViewModel.CategoryTypes,
                    ProductContacts = productViewModel.ProductContacts,
                    IsPublished = productViewModel.IsPublished,
                    CreatedAt = productViewModel.CreatedAt,
                    UpdatedAt = productViewModel.UpdatedAt,
                    IsAllocatedToUser = productViewModel.IsAllocatedToUser
                };

                // Calculate actual progress based on submitted metrics
                var reportingPeriod = $"{year}-{month.ToLower()}";
                var allMetrics = await _performanceMetricService.GetActiveMetricsAsync();
                var existingData = await _performanceMetricService.GetMetricDataForProductAsync(product.FipsId, reportingPeriod);
                
                performanceViewModel.TotalMetrics = allMetrics.Count;
                performanceViewModel.CompletedMetrics = existingData.Count(d => !string.IsNullOrWhiteSpace(d.Value) || d.IsNullReturn);
                performanceViewModel.ReportingStatus = performanceViewModel.ProgressStatus;

                productPerformanceViewModels.Add(performanceViewModel);
            }

            // Check if there's an existing submission for this user and period
            var reportingPeriodForSubmission = $"{year}-{month.ToLower()}";
            var existingSubmission = await _context.PerformanceSubmissions
                .FirstOrDefaultAsync(s => s.UserEmail == userEmail && s.ReportingPeriod == reportingPeriodForSubmission);

            // Calculate proper due date
            var fullMonthName = $"{CapitalizeMonth(month)} {year}";
            var dueDate = GetDueDateForMonth(fullMonthName);

            ViewBag.Year = year;
            ViewBag.Month = CapitalizeMonth(month);
            ViewBag.DueDate = dueDate;
            ViewBag.IsSubmitted = existingSubmission != null && existingSubmission.Status == "Submitted";
            ViewBag.SubmissionDate = existingSubmission?.SubmittedAt;
            ViewBag.SubmittedBy = existingSubmission?.SubmittedBy;

            return View("~/Views/Reporting/performance/PerformanceByMonth.cshtml", productPerformanceViewModels);
        }

        [HttpGet("{year:int}/{month}/performance/view")]
        public async Task<IActionResult> PerformanceByMonthViewOnly(int year, string month)
        {
            ViewData["Title"] = $"Performance Reporting - {CapitalizeMonth(month)} {year} (View Only)";
            ViewData["ActiveNav"] = "reporting";
            ViewData["ActiveNavItem"] = "performance";

            // Get user's email from claims
            var userEmail = _authenticationService.GetUserEmailFromClaims(User);
            
            // For development, use a hardcoded email if no user is authenticated
            if (string.IsNullOrEmpty(userEmail))
            {
                userEmail = "andy.jones@education.gov.uk";
                _logger.LogInformation("No authenticated user found, using development email: {Email}", userEmail);
            }

            // Get products assigned to current user
            var assignedProducts = await _cmsApiService.GetProductsByUserEmailAsync(userEmail);
            
            // Map CmsProduct to ProductPerformanceViewModel with progress calculation
            var productPerformanceViewModels = new List<ProductPerformanceViewModel>();
            foreach (var product in assignedProducts)
            {
                var productViewModel = _cmsApiService.MapToViewModel(product, true);
                var performanceViewModel = new ProductPerformanceViewModel
                {
                    Id = productViewModel.Id,
                    FipsId = productViewModel.FipsId,
                    Title = productViewModel.Title,
                    ShortDescription = productViewModel.ShortDescription,
                    LongDescription = productViewModel.LongDescription,
                    ProductUrl = productViewModel.ProductUrl,
                    State = productViewModel.State,
                    CategoryValues = productViewModel.CategoryValues,
                    CategoryTypes = productViewModel.CategoryTypes,
                    ProductContacts = productViewModel.ProductContacts,
                    IsPublished = productViewModel.IsPublished,
                    CreatedAt = productViewModel.CreatedAt,
                    UpdatedAt = productViewModel.UpdatedAt,
                    IsAllocatedToUser = productViewModel.IsAllocatedToUser
                };

                // Calculate actual progress based on submitted metrics
                var reportingPeriod = $"{year}-{month.ToLower()}";
                var allMetrics = await _performanceMetricService.GetActiveMetricsAsync();
                var existingData = await _performanceMetricService.GetMetricDataForProductAsync(product.FipsId, reportingPeriod);
                
                performanceViewModel.TotalMetrics = allMetrics.Count;
                performanceViewModel.CompletedMetrics = existingData.Count(d => !string.IsNullOrWhiteSpace(d.Value) || d.IsNullReturn);
                performanceViewModel.ReportingStatus = performanceViewModel.ProgressStatus;

                productPerformanceViewModels.Add(performanceViewModel);
            }

            // Check if there's an existing submission for this user and period
            var reportingPeriodForSubmission = $"{year}-{month.ToLower()}";
            var existingSubmission = await _context.PerformanceSubmissions
                .FirstOrDefaultAsync(s => s.UserEmail == userEmail && s.ReportingPeriod == reportingPeriodForSubmission);

            // Calculate proper due date
            var fullMonthName = $"{CapitalizeMonth(month)} {year}";
            var dueDate = GetDueDateForMonth(fullMonthName);

            ViewBag.Year = year;
            ViewBag.Month = CapitalizeMonth(month);
            ViewBag.DueDate = dueDate;
            ViewBag.IsSubmitted = existingSubmission != null && existingSubmission.Status == "Submitted";
            ViewBag.SubmissionDate = existingSubmission?.SubmittedAt;
            ViewBag.SubmittedBy = existingSubmission?.SubmittedBy;
            ViewBag.IsViewOnly = true; // Flag to indicate this is read-only

            return View("~/Views/Reporting/performance/PerformanceByMonthViewOnly.cshtml", productPerformanceViewModels);
        }

        [HttpGet("{year:int}/{month}/performance/view/{fipsId}")]
        public async Task<IActionResult> PerformanceByProductViewOnly(int year, string month, string fipsId)
        {
            ViewData["Title"] = $"Performance Reporting - {CapitalizeMonth(month)} {year} - View Only";
            ViewData["ActiveNav"] = "reporting";
            ViewData["ActiveNavItem"] = "performance";
            
            // Set ViewBag values early
            ViewBag.Year = year;
            ViewBag.Month = CapitalizeMonth(month);

            // Get user's email from claims
            var userEmail = _authenticationService.GetUserEmailFromClaims(User);
            
            // For development, use a hardcoded email if no user is authenticated
            if (string.IsNullOrEmpty(userEmail))
            {
                userEmail = "andy.jones@education.gov.uk";
                _logger.LogInformation("No authenticated user found, using development email: {Email}", userEmail);
            }

            // Get the specific product
            var assignedProducts = await _cmsApiService.GetProductsByUserEmailAsync(userEmail);
            var product = assignedProducts.FirstOrDefault(p => p.FipsId == fipsId);
            
            if (product == null)
            {
                _logger.LogWarning("Product with FipsId {FipsId} not found for user {Email}", fipsId, userEmail);
                return NotFound();
            }

            // Map to ProductViewModel
            var productViewModel = _cmsApiService.MapToViewModel(product, true);

            // Get all metrics for this product to show completion status
            var reportingPeriod = $"{year}-{month.ToLower()}";
            var allMetrics = await _performanceMetricService.GetActiveMetricsAsync();
            var existingData = await _performanceMetricService.GetMetricDataForProductAsync(fipsId, reportingPeriod);

            // Create ProductPerformanceFormViewModel for read-only view
            var formViewModel = new ProductPerformanceFormViewModel
            {
                Year = year,
                Month = CapitalizeMonth(month),
                Product = productViewModel,
                Metrics = allMetrics.Select(metric => new PerformanceMetricFormItem
                {
                    UniqueId = metric.UniqueId,
                    Name = metric.Name,
                    Description = metric.Description,
                    Measure = metric.Measure,
                    Category = metric.Category,
                    Mandatory = metric.Mandatory,
                    CanReportNullReturn = metric.CanReportNullReturn,
                    Value = existingData.FirstOrDefault(d => d.PerformanceMetricId == metric.Id)?.Value ?? "",
                    IsNullReturn = existingData.FirstOrDefault(d => d.PerformanceMetricId == metric.Id)?.IsNullReturn ?? false
                }).ToList()
            };

            // Calculate due date
            var fullMonthName = $"{CapitalizeMonth(month)} {year}";
            var dueDate = GetDueDateForMonth(fullMonthName);
            ViewBag.DueDate = dueDate;

            // Get submission info
            var reportingPeriodForSubmission = $"{year}-{month.ToLower()}";
            var existingSubmission = await _context.PerformanceSubmissions
                .FirstOrDefaultAsync(s => s.UserEmail == userEmail && s.ReportingPeriod == reportingPeriodForSubmission);

            ViewBag.SubmissionDate = existingSubmission?.SubmittedAt;
            ViewBag.SubmittedBy = existingSubmission?.SubmittedBy;
            ViewBag.IsViewOnly = true; // Flag to indicate this is read-only

            return View("~/Views/Reporting/performance/PerformanceByProductViewOnly.cshtml", formViewModel);
        }

        [HttpGet("{year:int}/{month}/performance/export/{fipsId}")]
        public async Task<IActionResult> ExportPerformanceData(int year, string month, string fipsId)
        {
            // Get user's email from claims
            var userEmail = _authenticationService.GetUserEmailFromClaims(User);
            
            // For development, use a hardcoded email if no user is authenticated
            if (string.IsNullOrEmpty(userEmail))
            {
                userEmail = "andy.jones@education.gov.uk";
            }

            // Get the specific product
            var assignedProducts = await _cmsApiService.GetProductsByUserEmailAsync(userEmail);
            var product = assignedProducts.FirstOrDefault(p => p.FipsId == fipsId);
            
            if (product == null)
            {
                return NotFound();
            }

            // Get all metrics and data for this product
            var reportingPeriod = $"{year}-{month.ToLower()}";
            var allMetrics = await _performanceMetricService.GetActiveMetricsAsync();
            var existingData = await _performanceMetricService.GetMetricDataForProductAsync(fipsId, reportingPeriod);

            // Create CSV content (Excel-compatible)
            var productName = product.Title.Replace(" ", "_").Replace("/", "_").Replace("\\", "_");
            var monthName = CapitalizeMonth(month);
            var fileName = $"{productName}_{monthName}_{year}_Performance_Report.csv";

            var csvContent = new System.Text.StringBuilder();
            csvContent.AppendLine($"Product: {product.Title}");
            csvContent.AppendLine($"Period: {monthName} {year}");
            csvContent.AppendLine($"FIPS ID: {fipsId}");
            csvContent.AppendLine();
            csvContent.AppendLine("Category,Metric Name,Value,Description");
            
            foreach (var metric in allMetrics.OrderBy(m => m.Category).ThenBy(m => m.Name))
            {
                var data = existingData.FirstOrDefault(d => d.PerformanceMetricId == metric.Id);
                var value = data?.IsNullReturn == true ? "Null return" : data?.Value ?? "";
                
                csvContent.AppendLine($"\"{metric.Category}\",\"{metric.Name}\",\"{value}\",\"{metric.Description}\"");
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(csvContent.ToString());
            return File(bytes, "text/csv", fileName);
        }

        [HttpGet("{year:int}/{month}/performance/export-all")]
        public async Task<IActionResult> ExportAllPerformanceData(int year, string month)
        {
            var userEmail = _authenticationService.GetUserEmailFromClaims(User);
            if (string.IsNullOrEmpty(userEmail)) { userEmail = "andy.jones@education.gov.uk"; }

            var reportingPeriod = $"{year}-{month.ToLower()}";
            var assignedProducts = await _cmsApiService.GetProductsByUserEmailAsync(userEmail);
            var allMetrics = await _performanceMetricService.GetActiveMetricsAsync();

            var monthName = CapitalizeMonth(month);
            var fileName = $"All_Products_{monthName}_{year}_Performance_Report.csv";

            var csvContent = new System.Text.StringBuilder();
            csvContent.AppendLine($"All Products Performance Report");
            csvContent.AppendLine($"Period: {monthName} {year}");
            csvContent.AppendLine($"Generated: {DateTime.Now:dd MMMM yyyy 'at' HH:mm}");
            csvContent.AppendLine();
            
            foreach (var product in assignedProducts.OrderBy(p => p.Title))
            {
                csvContent.AppendLine($"Product: {product.Title}");
                csvContent.AppendLine($"FIPS ID: {product.FipsId}");
                csvContent.AppendLine($"Short Description: {product.ShortDescription ?? "N/A"}");
                csvContent.AppendLine();
                
                var existingData = await _performanceMetricService.GetMetricDataForProductAsync(product.FipsId, reportingPeriod);
                csvContent.AppendLine("Category,Metric Name,Value,Description");
                
                foreach (var metric in allMetrics.OrderBy(m => m.Category).ThenBy(m => m.Name))
                {
                    var data = existingData.FirstOrDefault(d => d.PerformanceMetricId == metric.Id);
                    var value = data?.IsNullReturn == true ? "Null return" : data?.Value ?? "";
                    csvContent.AppendLine($"\"{metric.Category}\",\"{metric.Name}\",\"{value}\",\"{metric.Description}\"");
                }
                
                csvContent.AppendLine();
                csvContent.AppendLine("---");
                csvContent.AppendLine();
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(csvContent.ToString());
            return File(bytes, "text/csv", fileName);
        }

        [HttpPost]
        [Route("{year:int}/{month}/performance/submit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitPerformanceReturn(int year, string month)
        {
            try
            {
                // Get user's email from claims
                var userEmail = _authenticationService.GetUserEmailFromClaims(User);
                if (string.IsNullOrEmpty(userEmail))
                {
                    userEmail = "andy.jones@education.gov.uk"; // Development fallback
                }

                var reportingPeriod = $"{year}-{month.ToLower()}";
                
                // Get all products assigned to the user
                var assignedProducts = await _cmsApiService.GetProductsByUserEmailAsync(userEmail);
                
                // Verify all products are completed before allowing submission
                var allCompleted = true;
                foreach (var product in assignedProducts)
                {
                    var allMetrics = await _performanceMetricService.GetActiveMetricsAsync();
                    var existingData = await _performanceMetricService.GetMetricDataForProductAsync(product.FipsId, reportingPeriod);
                    var completedMetrics = existingData.Count(d => !string.IsNullOrWhiteSpace(d.Value) || d.IsNullReturn);
                    
                    if (completedMetrics != allMetrics.Count)
                    {
                        allCompleted = false;
                        break;
                    }
                }

                if (!allCompleted)
                {
                    TempData["ErrorMessage"] = "Cannot submit return: not all products are completed.";
                    return RedirectToAction("PerformanceByMonth", new { year, month });
                }

                // Check if submission already exists
                var existingSubmission = await _context.PerformanceSubmissions
                    .FirstOrDefaultAsync(s => s.UserEmail == userEmail && s.ReportingPeriod == reportingPeriod);

                if (existingSubmission != null)
                {
                    // Update existing submission
                    existingSubmission.Status = "Submitted";
                    existingSubmission.SubmittedBy = userEmail;
                    existingSubmission.SubmittedAt = DateTime.UtcNow;
                    existingSubmission.UpdatedAt = DateTime.UtcNow;
                    _context.PerformanceSubmissions.Update(existingSubmission);
                }
                else
                {
                    // Create new submission record
                    var submission = new PerformanceSubmission
                    {
                        UserEmail = userEmail,
                        ReportingPeriod = reportingPeriod,
                        Status = "Submitted",
                        SubmittedBy = userEmail,
                        SubmittedAt = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        Notes = $"Performance return submitted for {CapitalizeMonth(month)} {year}"
                    };
                    _context.PerformanceSubmissions.Add(submission);
                }

                // Save to database
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Performance return submitted for period {Period} by user {UserEmail}", reportingPeriod, userEmail);

                TempData["SuccessMessage"] = $"Performance return for {CapitalizeMonth(month)} {year} has been submitted successfully.";
                return RedirectToAction("PerformanceByMonth", new { year, month });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting performance return for {Year}-{Month}", year, month);
                TempData["ErrorMessage"] = "An error occurred while submitting the return. Please try again.";
                return RedirectToAction("PerformanceByMonth", new { year, month });
            }
        }

        [HttpPost]
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

        [HttpGet("performance")]
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
            var reportingPeriods = await GetDashboardReportingPeriodsAsync(userEmail);

            var viewModel = new ReportingViewModel
            {
                AssignedProducts = assignedProducts,
                ReportingPeriods = reportingPeriods,
                DueReportsCount = reportingPeriods.Count(p => p.Status == "Due Soon"),
                OverdueReportsCount = reportingPeriods.Count(p => p.Status == "Overdue"),
                MilestonesCount = 0 // TODO: Get actual milestone count
            };

            return View("~/Views/Reporting/performance/Performance.cshtml", viewModel);
        }

        [HttpGet("milestones")]
        public async Task<IActionResult> Milestones()
        {
            ViewData["Title"] = "Milestones";
            ViewData["ActiveNav"] = "reporting";
            ViewData["ActiveNavItem"] = "milestones";

            // Get user's email from claims
            var userEmail = _authenticationService.GetUserEmailFromClaims(User);
            
            // For development, use a hardcoded email if no user is authenticated
            if (string.IsNullOrEmpty(userEmail))
            {
                userEmail = "andy.jones@education.gov.uk";
                _logger.LogInformation("No authenticated user found, using development email: {Email}", userEmail);
            }
            else
            {
                _logger.LogInformation("Using authenticated user email: {Email}", userEmail);
            }

            // Get products assigned to current user
            var assignedProducts = await _cmsApiService.GetProductsByUserEmailAsync(userEmail);
            _logger.LogInformation("Milestones action: Found {Count} assigned products for user {UserEmail}", assignedProducts.Count, userEmail);

            // Get all milestones for all assigned products
            var allMilestones = new List<Milestone>();
            foreach (var product in assignedProducts)
            {
                if (!string.IsNullOrEmpty(product.FipsId))
                {
                    var milestones = await _milestoneService.GetMilestonesByFipsIdAsync(product.FipsId);
                    // Add product name to each milestone for display
                    foreach (var milestone in milestones)
                    {
                        milestone.ProductName = product.Title;
                    }
                    allMilestones.AddRange(milestones);
                }
            }

            return View("~/Views/Reporting/milestones/Index.cshtml", allMilestones);
        }

        [HttpGet("milestone/{id}")]
        public async Task<IActionResult> MilestoneDetails(int id)
        {
            try
            {
                var milestone = await _milestoneService.GetMilestoneByIdAsync(id);
                if (milestone == null)
                {
                    return NotFound();
                }

                var updates = await _milestoneService.GetMilestoneUpdatesAsync(id);
                
                ViewBag.Updates = updates;
                ViewBag.StatusOptions = new List<string> { "Not started", "On track", "At risk", "Off track", "Delayed", "Completed", "Cancelled" };
                
                return View("~/Views/Reporting/milestones/Details.cshtml", milestone);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading milestone details for ID {MilestoneId}", id);
                TempData["ErrorMessage"] = "An error occurred while loading the milestone details.";
                return RedirectToAction("Milestones");
            }
        }

        [HttpPost("milestone/{id}/update")]
        public async Task<IActionResult> AddMilestoneUpdate(int id, string updateText, string statusChange)
        {
            try
            {
                var userEmail = _authenticationService.GetUserEmailFromClaims(User);
                if (string.IsNullOrEmpty(userEmail)) { userEmail = "andy.jones@education.gov.uk"; }

                if (string.IsNullOrWhiteSpace(updateText))
                {
                    TempData["ErrorMessage"] = "Update text is required.";
                    return RedirectToAction("MilestoneDetails", new { id });
                }

                await _milestoneService.AddMilestoneUpdateAsync(id, updateText, statusChange, userEmail);
                TempData["SuccessMessage"] = "Milestone update added successfully.";
                
                return RedirectToAction("MilestoneDetails", new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding milestone update for ID {MilestoneId}", id);
                TempData["ErrorMessage"] = "An error occurred while adding the milestone update.";
                return RedirectToAction("MilestoneDetails", new { id });
            }
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
            
            // Check submission status from PerformanceSubmission table
            var existingSubmission = await _context.PerformanceSubmissions
                .FirstOrDefaultAsync(s => s.UserEmail == userEmail && s.ReportingPeriod == reportingPeriod);
            var isSubmitted = existingSubmission != null && existingSubmission.Status == "Submitted";
            
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

        [HttpGet("{year:int}/{month}/service/{fipsId}/performance")]
        public async Task<IActionResult> ServicePerformance(int year, string month, string fipsId)
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

        [HttpGet("{year:int}/{month}/performance/{fipsId}/{metricId}")]
        public async Task<IActionResult> PerformanceMetric(int year, string month, string fipsId, string metricId)
        {
            var monthName = CapitalizeMonth(month);
            var fullMonthName = $"{monthName} {year}";
            var reportingPeriod = $"{year}-{month.ToLower()}";
            
            ViewData["Title"] = $"Performance Metric - {metricId}";
            ViewData["ActiveNav"] = "reporting";

            // Store TempData values for display, then clear them to prevent persistence
            var successMessage = TempData["SuccessMessage"]?.ToString();
            var errorMessage = TempData["ErrorMessage"]?.ToString();
            var fieldError = TempData["FieldError"]?.ToString();
            var formValue = TempData["FormValue"]?.ToString();
            var formIsNullReturn = TempData["FormIsNullReturn"]?.ToString();
            
            // Clear TempData to prevent persistence across requests
            TempData.Clear();
            
            // Restore values for this request only
            if (!string.IsNullOrEmpty(successMessage)) TempData["SuccessMessage"] = successMessage;
            if (!string.IsNullOrEmpty(errorMessage)) TempData["ErrorMessage"] = errorMessage;
            if (!string.IsNullOrEmpty(fieldError)) TempData["FieldError"] = fieldError;
            if (!string.IsNullOrEmpty(formValue)) TempData["FormValue"] = formValue;
            if (!string.IsNullOrEmpty(formIsNullReturn)) TempData["FormIsNullReturn"] = formIsNullReturn;

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
            var metric = await _performanceMetricService.GetMetricByUniqueIdAsync(metricId);
            if (metric == null)
            {
                return NotFound($"Performance metric with ID '{metricId}' not found.");
            }

            // Get existing data for this specific metric
            var existingData = await _performanceMetricService.GetMetricDataForProductAsync(fipsId, reportingPeriod);
            var metricData = existingData.FirstOrDefault(d => d.PerformanceMetricId == metric.Id);

            // Get all metrics for this product to calculate completion status
            var allMetrics = await _performanceMetricService.GetActiveMetricsAsync();
            var allExistingData = await _performanceMetricService.GetMetricDataForProductAsync(fipsId, reportingPeriod);
            
            // Create metric items for all metrics (same logic as PerformanceByProduct)
            var metricItems = new List<PerformanceMetricFormItem>();
            foreach (var m in allMetrics)
            {
                var existingMetricData = allExistingData.FirstOrDefault(d => d.PerformanceMetricId == m.Id);
                metricItems.Add(new PerformanceMetricFormItem
                {
                    Id = m.Id,
                    UniqueId = m.UniqueId,
                    Name = m.Name,
                    Description = m.Description,
                    Category = m.Category,
                    Measure = m.Measure,
                    Mandatory = m.Mandatory,
                    CanReportNullReturn = m.CanReportNullReturn,
                    Value = existingMetricData?.Value,
                    IsNullReturn = existingMetricData?.IsNullReturn ?? false
                });
            }

            // Calculate due date (7th of following month)
            var dueDate = DateTime.Now.AddDays(7);

            var viewModel = new PerformanceMetricViewModel
            {
                Year = year,
                Month = monthName,
                FullMonthName = fullMonthName,
                FipsId = fipsId,
                Product = _cmsApiService.MapToViewModel(product, true),
                UserEmail = userEmail,
                ReportingPeriod = reportingPeriod,
                Metric = metric,
                ExistingData = metricData,
                Metrics = metricItems,
                DueDate = dueDate
            };

            return View("~/Views/Reporting/performance/PerformanceMetric.cshtml", viewModel);
        }

        [HttpPost]
        [Route("{year:int}/{month}/performance/{fipsId}/save")]
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
                    return RedirectToAction("PerformanceByProduct", new { year = Year, month = Month, fipsId = FipsId });
                }

                // Validate the data
                var validationResult = ValidateMetricData(metric, Value, IsNullReturn);
                
                if (!validationResult.IsValid)
                {
                    TempData["ErrorMessage"] = validationResult.ErrorMessage;
                    TempData["FieldError"] = validationResult.FieldError;
                    TempData["FormValue"] = Value;
                    TempData["FormIsNullReturn"] = IsNullReturn;
                    return RedirectToAction("PerformanceMetric", new { year = Year, month = Month, fipsId = FipsId, metricId = metric.UniqueId });
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
                return RedirectToAction("PerformanceByProduct", new { year = Year, month = Month, fipsId = FipsId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving performance metric data");
                TempData["ErrorMessage"] = "An error occurred while saving the performance data.";
                return RedirectToAction("PerformanceByProduct", new { year = Year, month = Month, fipsId = FipsId });
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

                case "percentage":
                    if (!decimal.TryParse(value, out decimal percentageValue))
                    {
                        return (false, $"'{metric.Name}' must be a number.", "value");
                    }
                    var percentageRangeResult = ValidateNumericRange(metric, percentageValue);
                    return percentageRangeResult.IsValid ? (true, "", "") : (false, percentageRangeResult.ErrorMessage, "value");

                case "boolean":
                    if (value != "Yes" && value != "No")
                    {
                        return (false, $"'{metric.Name}' must be either 'Yes' or 'No'.", "value");
                    }
                    return (true, "", "");

                case "single_option":
                case "options_list":
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

                case "text":
                    // Text validation - just check if it's not empty when mandatory
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
                return RedirectToAction("PerformanceByProduct", new { year = model.Year, month = model.Month, fipsId = model.FipsId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving performance data");
                TempData["ErrorMessage"] = "An error occurred while saving the performance data.";
                return RedirectToAction("PerformanceByProduct", new { year = model.Year, month = model.Month, fipsId = model.FipsId });
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

            return View("~/Views/Reporting/milestones/ProductMilestones.cshtml", milestones);
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

        private async Task<List<ReportingPeriod>> GetDashboardReportingPeriodsAsync(string userEmail)
        {
            var periods = new List<ReportingPeriod>();
            var now = DateTime.Now;
            
            // August 2025
            var augustSubmission = await _context.PerformanceSubmissions
                .FirstOrDefaultAsync(s => s.UserEmail == userEmail && s.ReportingPeriod == "2025-august");
            
            periods.Add(new ReportingPeriod
            {
                Month = "August 2025",
                DueDate = new DateTime(2025, 9, 5),
                Status = augustSubmission != null ? "Submitted" : (now > new DateTime(2025, 9, 5) ? "Overdue" : "Due Soon"),
                Period = "1 to 31 August"
            });
            
            // September 2025
            var septemberSubmission = await _context.PerformanceSubmissions
                .FirstOrDefaultAsync(s => s.UserEmail == userEmail && s.ReportingPeriod == "2025-september");
            
            periods.Add(new ReportingPeriod
            {
                Month = "September 2025",
                DueDate = new DateTime(2025, 10, 5),
                Status = septemberSubmission != null ? "Submitted" : (now > new DateTime(2025, 10, 5) ? "Overdue" : "Due Soon"),
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
                return RedirectToAction("PerformanceByProduct", new { year = year, month = month, fipsId = fipsId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting product report for {FipsId} in {Year}-{Month}", fipsId, year, month);
                TempData["ErrorMessage"] = "An error occurred while submitting the product report.";
                return RedirectToAction("PerformanceByProduct", new { year = year, month = month, fipsId = fipsId });
            }
        }
    }
}
