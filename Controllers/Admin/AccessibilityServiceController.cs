using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Compass.Data;
using Compass.Models;
using Compass.Services;

namespace Compass.Controllers.Admin
{
    [Route("Admin/[controller]")]
    [Authorize]
    public class AccessibilityServiceController : Controller
    {
        private readonly CompassDbContext _context;
        private readonly ILogger<AccessibilityServiceController> _logger;
        private readonly IProductsApiService _productsApiService;

        public AccessibilityServiceController(
            CompassDbContext context,
            ILogger<AccessibilityServiceController> logger,
            IProductsApiService productsApiService)
        {
            _context = context;
            _logger = logger;
            _productsApiService = productsApiService;
        }

        private async Task<bool> IsAuthorizedAsync()
        {
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                return false;
            }

            // Get user email from claims
            var userEmail = User.Identity?.Name 
                ?? User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value 
                ?? User.FindFirst("preferred_username")?.Value
                ?? User.FindFirst("email")?.Value
                ?? string.Empty;

            if (string.IsNullOrEmpty(userEmail))
            {
                _logger.LogWarning("Cannot determine user email for authorization check");
                return false;
            }

            // Get all role claims from various claim types
            var roleClaims = User.FindAll(System.Security.Claims.ClaimTypes.Role)
                .Concat(User.FindAll("role"))
                .Concat(User.FindAll("roles"))
                .Select(c => c.Value)
                .Distinct()
                .ToList();
            
            // Check for admin roles in claims - the Admin model uses "super_admin", "admin", "editor"
            var validRoles = new[] { "super_admin", "admin", "Super admin", "SuperAdmin", "Admin" };
            var hasAdminRole = validRoles.Any(role => 
                User.IsInRole(role) || 
                roleClaims.Any(rc => rc.Equals(role, StringComparison.OrdinalIgnoreCase)));
            
            // Also check if any role contains both "super" and "admin" (case-insensitive)
            if (!hasAdminRole)
            {
                hasAdminRole = roleClaims.Any(role => 
                    role.Contains("super", StringComparison.OrdinalIgnoreCase) && 
                    role.Contains("admin", StringComparison.OrdinalIgnoreCase));
            }

            // If still not found in claims, check User model in database
            if (!hasAdminRole)
            {
                var userEmailLower = userEmail.ToLower();
                var user = await _context.Users
                    .Where(u => u.Email.ToLower() == userEmailLower)
                    .FirstOrDefaultAsync();
                
                if (user != null)
                {
                    // User model uses enum: Admin = 2, SuperAdmin = 3
                    hasAdminRole = user.Role == Compass.Models.UserRole.Admin || 
                                  user.Role == Compass.Models.UserRole.SuperAdmin;
                }
            }
            
            if (!hasAdminRole)
            {
                // Log for debugging - show all user roles and claims
                _logger.LogWarning(
                    "User {User} (email: {Email}) attempted to access Accessibility Service but doesn't have required role. " +
                    "User roles from claims: {Roles}. All role-related claims: {AllClaims}",
                    User.Identity?.Name ?? "Unknown",
                    userEmail,
                    string.Join(", ", roleClaims),
                    string.Join("; ", User.Claims.Where(c => 
                        c.Type.Contains("role", StringComparison.OrdinalIgnoreCase) || 
                        c.Type == System.Security.Claims.ClaimTypes.Role)
                        .Select(c => $"{c.Type}={c.Value}")));
            }
            
            return hasAdminRole;
        }

        // GET: Admin/AccessibilityService
        [HttpGet]
        [Route("")]
        [Route("Index")]
        public async Task<IActionResult> Index(string? search, string? statementStatus, string? enrollmentStatus, int page = 1)
        {
            // Check authorization - Admin or Super admin
            if (!await IsAuthorizedAsync())
            {
                TempData["ErrorMessage"] = "You do not have permission to access this section.";
                return RedirectToAction("Index", "Home");
            }

            const int pageSize = 20;

            // Get all products from CMS
            var cmsProducts = await _productsApiService.GetProductsAsync();
            
            // Get all enrolled products with their issues
            var enrolledProducts = await _context.ProductAccessibilities
                .Include(pa => pa.Issues.Where(i => !i.IsDeleted))
                .Where(pa => !pa.IsDeleted && pa.IsActive)
                .ToListAsync();

            // Create a dictionary for quick lookup of enrolled products
            var enrolledDict = enrolledProducts.ToDictionary(pa => pa.FipsId, pa => pa);

            // Create view model with all CMS products and enrollment status
            var allProductViewModels = new List<dynamic>();
            var enrolledProductViewModels = new List<dynamic>();

            foreach (var cmsProduct in cmsProducts)
            {
                var isEnrolled = enrolledDict.TryGetValue(cmsProduct.FipsId, out var enrolled);
                
                // Apply enrollment status filter - if filtering for enrolled, skip (they're shown in separate table)
                if (!string.IsNullOrWhiteSpace(enrollmentStatus))
                {
                    if (enrollmentStatus == "enrolled")
                    {
                        // Enrolled products are shown in separate table, skip them here
                        continue;
                    }
                    // For "not-enrolled" filter, we only want non-enrolled products (default behavior)
                }

                // Apply search filter
                if (!string.IsNullOrWhiteSpace(search))
                {
                    var searchLower = search.ToLower();
                    if (!cmsProduct.Title.ToLower().Contains(searchLower) && 
                        !cmsProduct.FipsId.ToLower().Contains(searchLower))
                    {
                        continue;
                    }
                }

                var openIssuesCount = 0;
                var pastDueCount = 0;
                var isVerified = false;

                // Note: Statement status filter removed from "All Products" view
                // It only applied to enrolled products which are now shown in separate table

                if (isEnrolled)
                {
                    openIssuesCount = enrolled.Issues.Count(i => !i.IsDeleted && i.Status != "resolved" && i.Status != "wont_fix");
                    pastDueCount = enrolled.Issues.Count(i => 
                        !i.IsDeleted && 
                        i.Status != "resolved" && 
                        i.Status != "wont_fix" &&
                        i.PlannedResolutionDate.HasValue &&
                        i.PlannedResolutionDate.Value < DateTime.UtcNow.Date);
                    isVerified = enrolled.StatementInstalled && enrolled.VerifiedAt.HasValue;
                }

                var productViewModel = new
                {
                    FipsId = cmsProduct.FipsId,
                    ProductName = cmsProduct.Title,
                    Phase = cmsProduct.Phase,
                    IsEnrolled = isEnrolled,
                    ProductAccessibility = isEnrolled ? enrolled : null,
                    OpenIssuesCount = openIssuesCount,
                    PastDueCount = pastDueCount,
                    IsVerified = isVerified
                };

                if (isEnrolled)
                {
                    enrolledProductViewModels.Add(productViewModel);
                }
                else
                {
                    // Only add non-enrolled products to "All Products" list
                    allProductViewModels.Add(productViewModel);
                }
            }

            // Pagination for all products (non-enrolled only)
            var totalCount = allProductViewModels.Count;
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            var pagedProducts = allProductViewModels
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Get pending retest count
            var pendingRetestCount = await _context.AccessibilityRetestRequests
                .CountAsync(rr => rr.IsCompleted == null);

            ViewBag.CurrentSearch = search;
            ViewBag.CurrentEnrollmentStatus = enrollmentStatus;
            ViewBag.PendingRetestCount = pendingRetestCount;
            ViewBag.EnrolledProducts = enrolledProductViewModels;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = totalCount;

            return View("~/Views/Admin/AccessibilityService/Index.cshtml", pagedProducts);
        }

        // GET: Admin/AccessibilityService/RetestRequests
        [HttpGet]
        [Route("RetestRequests")]
        public async Task<IActionResult> RetestRequests(string? status)
        {
            // Check authorization
            if (!await IsAuthorizedAsync())
            {
                TempData["ErrorMessage"] = "You do not have permission to access this section.";
                return RedirectToAction("Index", "Home");
            }

            var query = _context.AccessibilityRetestRequests
                .Include(rr => rr.AccessibilityIssue)
                    .ThenInclude(ai => ai.ProductAccessibility)
                .Include(rr => rr.AccessibilityIssue)
                    .ThenInclude(ai => ai.WcagCriteriaLinks)
                        .ThenInclude(link => link.WcagCriterion)
                .AsQueryable();

            // Apply status filter
            if (!string.IsNullOrWhiteSpace(status))
            {
                query = status switch
                {
                    "pending" => query.Where(rr => rr.IsCompleted == null),
                    "completed" => query.Where(rr => rr.IsCompleted == true),
                    "cancelled" => query.Where(rr => rr.IsCompleted == false),
                    _ => query
                };
                ViewBag.CurrentStatus = status;
            }
            else
            {
                // Default to pending requests
                query = query.Where(rr => rr.IsCompleted == null);
            }

            var requests = await query
                .OrderByDescending(rr => rr.RequestedAt)
                .ToListAsync();

            // Get pending retest count for navigation
            var pendingRetestCount = await _context.AccessibilityRetestRequests
                .CountAsync(rr => rr.IsCompleted == null);
            ViewBag.PendingRetestCount = pendingRetestCount;

            return View("~/Views/Admin/AccessibilityService/RetestRequests.cshtml", requests);
        }

        // GET: Admin/AccessibilityService/RetestRequestDetails/{id}
        [HttpGet]
        [Route("RetestRequestDetails/{id}")]
        public async Task<IActionResult> RetestRequestDetails(int id)
        {
            // Check authorization
            if (!await IsAuthorizedAsync())
            {
                TempData["ErrorMessage"] = "You do not have permission to access this section.";
                return RedirectToAction("Index", "Home");
            }

            var request = await _context.AccessibilityRetestRequests
                .Include(rr => rr.AccessibilityIssue)
                    .ThenInclude(ai => ai.ProductAccessibility)
                .Include(rr => rr.AccessibilityIssue)
                    .ThenInclude(ai => ai.WcagCriteriaLinks)
                        .ThenInclude(link => link.WcagCriterion)
                .FirstOrDefaultAsync(rr => rr.Id == id);

            if (request == null)
            {
                TempData["ErrorMessage"] = "Retest request not found.";
                return RedirectToAction(nameof(RetestRequests));
            }

            // Get pending retest count for navigation
            var pendingRetestCount = await _context.AccessibilityRetestRequests
                .CountAsync(rr => rr.IsCompleted == null);
            ViewBag.PendingRetestCount = pendingRetestCount;

            return View("~/Views/Admin/AccessibilityService/RetestRequestDetails.cshtml", request);
        }

        // POST: Admin/AccessibilityService/UpdateRetestRequest
        [HttpPost]
        [Route("UpdateRetestRequest")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRetestRequest(
            int id,
            bool isCompleted,
            string? outcome,
            string? adminNotes)
        {
            // Check authorization
            if (!await IsAuthorizedAsync())
            {
                TempData["ErrorMessage"] = "You do not have permission to perform this action.";
                return RedirectToAction("Index", "Home");
            }

            try
            {
                var request = await _context.AccessibilityRetestRequests
                    .Include(rr => rr.AccessibilityIssue)
                    .FirstOrDefaultAsync(rr => rr.Id == id);

                if (request == null)
                {
                    TempData["ErrorMessage"] = "Retest request not found.";
                    return RedirectToAction(nameof(RetestRequests));
                }

                // Validate outcome if completed
                if (isCompleted && string.IsNullOrWhiteSpace(outcome))
                {
                    TempData["ErrorMessage"] = "Outcome is required when marking request as completed.";
                    return RedirectToAction(nameof(RetestRequestDetails), new { id });
                }

                if (!isCompleted || (outcome != "Resolved" && outcome != "Not resolved"))
                {
                    TempData["ErrorMessage"] = "Invalid outcome. Must be 'Resolved' or 'Not resolved'.";
                    return RedirectToAction(nameof(RetestRequestDetails), new { id });
                }

                request.IsCompleted = isCompleted;
                request.Outcome = outcome;
                request.AdminNotes = adminNotes;
                request.CompletedBy = User.Identity?.Name;
                request.CompletedAt = DateTime.UtcNow;
                request.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // TODO: Send email notifications
                // await SendRetestRequestEmails(request);

                TempData["SuccessMessage"] = "Retest request updated successfully.";
                return RedirectToAction(nameof(RetestRequestDetails), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating retest request {RequestId}", id);
                TempData["ErrorMessage"] = "An error occurred while updating the retest request.";
                return RedirectToAction(nameof(RetestRequestDetails), new { id });
            }
        }

        // GET: Admin/AccessibilityService/EmailConfigurations
        [HttpGet]
        [Route("EmailConfigurations")]
        public async Task<IActionResult> EmailConfigurations()
        {
            // Check authorization
            if (!await IsAuthorizedAsync())
            {
                TempData["ErrorMessage"] = "You do not have permission to access this section.";
                return RedirectToAction("Index", "Home");
            }

            var configurations = await _context.AccessibilityEmailConfigurations
                .OrderBy(e => e.Purpose)
                .ThenBy(e => e.SortOrder)
                .ThenBy(e => e.EmailAddress)
                .ToListAsync();

            ViewBag.RetestRequestEmails = configurations
                .Where(e => e.Purpose == "RetestRequests" && e.IsActive)
                .ToList();

            ViewBag.ReportSummaryEmails = configurations
                .Where(e => e.Purpose == "ReportSummaries" && e.IsActive)
                .ToList();

            // Get pending retest count for navigation
            var pendingRetestCount = await _context.AccessibilityRetestRequests
                .CountAsync(rr => rr.IsCompleted == null);
            ViewBag.PendingRetestCount = pendingRetestCount;

            return View("~/Views/Admin/AccessibilityService/EmailConfigurations.cshtml", configurations);
        }

        // POST: Admin/AccessibilityService/AddEmailConfiguration
        [HttpPost]
        [Route("AddEmailConfiguration")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddEmailConfiguration(
            string emailAddress,
            string purpose,
            string? description,
            int sortOrder)
        {
            // Check authorization
            if (!await IsAuthorizedAsync())
            {
                TempData["ErrorMessage"] = "You do not have permission to perform this action.";
                return RedirectToAction("Index", "Home");
            }

            try
            {
                if (string.IsNullOrWhiteSpace(emailAddress) || string.IsNullOrWhiteSpace(purpose))
                {
                    TempData["ErrorMessage"] = "Email address and purpose are required.";
                    return RedirectToAction(nameof(EmailConfigurations));
                }

                if (purpose != "RetestRequests" && purpose != "ReportSummaries")
                {
                    TempData["ErrorMessage"] = "Purpose must be 'RetestRequests' or 'ReportSummaries'.";
                    return RedirectToAction(nameof(EmailConfigurations));
                }

                var config = new AccessibilityEmailConfiguration
                {
                    EmailAddress = emailAddress.Trim(),
                    Purpose = purpose,
                    Description = description?.Trim(),
                    SortOrder = sortOrder,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = User.Identity?.Name,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = User.Identity?.Name
                };

                _context.AccessibilityEmailConfigurations.Add(config);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Email configuration added successfully.";
                return RedirectToAction(nameof(EmailConfigurations));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding email configuration");
                TempData["ErrorMessage"] = "An error occurred while adding the email configuration.";
                return RedirectToAction(nameof(EmailConfigurations));
            }
        }

        // POST: Admin/AccessibilityService/DeleteEmailConfiguration
        [HttpPost]
        [Route("DeleteEmailConfiguration")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEmailConfiguration(int id)
        {
            // Check authorization
            if (!await IsAuthorizedAsync())
            {
                TempData["ErrorMessage"] = "You do not have permission to perform this action.";
                return RedirectToAction("Index", "Home");
            }

            try
            {
                var config = await _context.AccessibilityEmailConfigurations
                    .FirstOrDefaultAsync(e => e.Id == id);

                if (config == null)
                {
                    TempData["ErrorMessage"] = "Email configuration not found.";
                    return RedirectToAction(nameof(EmailConfigurations));
                }

                _context.AccessibilityEmailConfigurations.Remove(config);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Email configuration deleted successfully.";
                return RedirectToAction(nameof(EmailConfigurations));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting email configuration {ConfigId}", id);
                TempData["ErrorMessage"] = "An error occurred while deleting the email configuration.";
                return RedirectToAction(nameof(EmailConfigurations));
            }
        }

        // POST: Admin/AccessibilityService/ToggleEmailConfiguration
        [HttpPost]
        [Route("ToggleEmailConfiguration")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleEmailConfiguration(int id)
        {
            // Check authorization
            if (!await IsAuthorizedAsync())
            {
                TempData["ErrorMessage"] = "You do not have permission to perform this action.";
                return RedirectToAction("Index", "Home");
            }

            try
            {
                var config = await _context.AccessibilityEmailConfigurations
                    .FirstOrDefaultAsync(e => e.Id == id);

                if (config == null)
                {
                    TempData["ErrorMessage"] = "Email configuration not found.";
                    return RedirectToAction(nameof(EmailConfigurations));
                }

                config.IsActive = !config.IsActive;
                config.UpdatedAt = DateTime.UtcNow;
                config.UpdatedBy = User.Identity?.Name;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Email configuration {(config.IsActive ? "activated" : "deactivated")} successfully.";
                return RedirectToAction(nameof(EmailConfigurations));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling email configuration {ConfigId}", id);
                TempData["ErrorMessage"] = "An error occurred while updating the email configuration.";
                return RedirectToAction(nameof(EmailConfigurations));
            }
        }

        // POST: Admin/AccessibilityService/UpdateProductUrl
        [HttpPost]
        [Route("UpdateProductUrl")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProductUrl(string fipsId, string productUrl)
        {
            // Check authorization
            if (!await IsAuthorizedAsync())
            {
                TempData["ErrorMessage"] = "You do not have permission to perform this action.";
                return RedirectToAction("Index", "Home");
            }

            try
            {
                if (string.IsNullOrWhiteSpace(productUrl))
                {
                    TempData["ErrorMessage"] = "Product URL cannot be empty.";
                    return RedirectToAction(nameof(ProductDetails), new { fipsId });
                }

                // Validate URL format
                if (!Uri.TryCreate(productUrl, UriKind.Absolute, out var uri) || 
                    (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                {
                    TempData["ErrorMessage"] = "Please provide a valid HTTP or HTTPS URL.";
                    return RedirectToAction(nameof(ProductDetails), new { fipsId });
                }

                var success = await _productsApiService.UpdateProductUrlAsync(fipsId, productUrl);
                
                if (success)
                {
                    TempData["SuccessMessage"] = "Product URL updated successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to update product URL. Please try again.";
                }
                
                return RedirectToAction(nameof(ProductDetails), new { fipsId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product URL for {FipsId}", fipsId);
                TempData["ErrorMessage"] = "An error occurred while updating the product URL.";
                return RedirectToAction(nameof(ProductDetails), new { fipsId });
            }
        }

        // POST: Admin/AccessibilityService/VerifyStatement
        [HttpPost]
        [Route("VerifyStatement")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyStatement(
            string fipsId,
            bool statementInstalled,
            string? verificationMethod)
        {
            // Check authorization
            if (!await IsAuthorizedAsync())
            {
                TempData["ErrorMessage"] = "You do not have permission to perform this action.";
                return RedirectToAction("Index", "Home");
            }

            try
            {
                var productAccessibility = await _context.ProductAccessibilities
                    .FirstOrDefaultAsync(pa => pa.FipsId == fipsId && !pa.IsDeleted);

                if (productAccessibility == null)
                {
                    TempData["ErrorMessage"] = "Product not found.";
                    return RedirectToAction(nameof(ProductDetails), new { fipsId });
                }

                // If marking as verified, verificationMethod is required
                if (statementInstalled && string.IsNullOrWhiteSpace(verificationMethod))
                {
                    TempData["ErrorMessage"] = "Verification method is required when marking as verified.";
                    return RedirectToAction(nameof(ProductDetails), new { fipsId });
                }

                if (statementInstalled && verificationMethod != "Manual" && verificationMethod != "Automatic")
                {
                    TempData["ErrorMessage"] = "Verification method must be 'Manual' or 'Automatic'.";
                    return RedirectToAction(nameof(ProductDetails), new { fipsId });
                }

                // For automatic verification, check if product URL exists and verify statement URL
                if (statementInstalled && verificationMethod == "Automatic")
                {
                    // Get product from CMS to check for ProductUrl
                    var cmsProduct = await _productsApiService.GetProductByFipsIdAsync(fipsId);
                    
                    if (cmsProduct == null || string.IsNullOrWhiteSpace(cmsProduct.ProductUrl))
                    {
                        TempData["ErrorMessage"] = "Product URL is required for automatic verification. Please add a Product URL first.";
                        return RedirectToAction(nameof(ProductDetails), new { fipsId });
                    }

                    // Attempt automatic verification
                    var verificationResult = await VerifyStatementUrlAutomaticallyAsync(cmsProduct.ProductUrl, fipsId);
                    
                    if (verificationResult.Success)
                    {
                        // Automatic verification successful
                        productAccessibility.StatementInstalled = true;
                        productAccessibility.VerifiedBy = User.Identity?.Name;
                        productAccessibility.VerifiedAt = DateTime.UtcNow;
                        productAccessibility.StatementVerificationMethod = "Automatic";
                        
                        productAccessibility.UpdatedAt = DateTime.UtcNow;
                        productAccessibility.UpdatedBy = User.Identity?.Name;

                        await _context.SaveChangesAsync();

                        TempData["SuccessMessage"] = "Statement automatically verified and installed.";
                        return RedirectToAction(nameof(ProductDetails), new { fipsId });
                    }
                    else
                    {
                        // Automatic verification failed - show error
                        TempData["ErrorMessage"] = verificationResult.Message;
                        return RedirectToAction(nameof(ProductDetails), new { fipsId });
                    }
                }

                // Manual verification path
                productAccessibility.StatementInstalled = statementInstalled;
                
                if (statementInstalled)
                {
                    productAccessibility.VerifiedBy = User.Identity?.Name;
                    productAccessibility.VerifiedAt = DateTime.UtcNow;
                    productAccessibility.StatementVerificationMethod = verificationMethod;
                }
                else
                {
                    // Marking as not verified - clear verification data
                    productAccessibility.VerifiedBy = null;
                    productAccessibility.VerifiedAt = null;
                    productAccessibility.StatementVerificationMethod = null;
                }

                productAccessibility.UpdatedAt = DateTime.UtcNow;
                productAccessibility.UpdatedBy = User.Identity?.Name;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = statementInstalled 
                    ? $"Statement verified as {(verificationMethod == "Manual" ? "manually" : "automatically")} installed." 
                    : "Statement marked as not verified.";
                
                return RedirectToAction(nameof(ProductDetails), new { fipsId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying statement for product {FipsId}", fipsId);
                TempData["ErrorMessage"] = "An error occurred while updating the verification status.";
                return RedirectToAction(nameof(ProductDetails), new { fipsId });
            }
        }

        private async Task<(bool Success, string Message)> VerifyStatementUrlAutomaticallyAsync(string productUrl, string fipsId)
        {
            var statementUrl = $"https://accessibility-statements.education.gov.uk/s/{fipsId}";
            
            try
            {
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(30);
                
                // Set a user agent to avoid blocking
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Compatible; FIPS-Compass/1.0)");
                
                var response = await httpClient.GetAsync(productUrl);
                
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    return (false, "Manual confirmation needed. The product website returned an authentication error (401).");
                }
                
                if (!response.IsSuccessStatusCode)
                {
                    return (false, $"Manual verification needed. Failed to access product website (HTTP {response.StatusCode}).");
                }
                
                var htmlContent = await response.Content.ReadAsStringAsync();
                
                // Check if statement URL exists in the HTML
                // Look for href attributes containing the statement URL
                var statementUrlLower = statementUrl.ToLower();
                var htmlContentLower = htmlContent.ToLower();
                
                // Check for various patterns: href="...", href='...', href=...
                var patterns = new[]
                {
                    $"href=\"{statementUrlLower}\"",
                    $"href='{statementUrlLower}'",
                    $"href={statementUrlLower}",
                    $"href=\"{statementUrl}\"",
                    $"href='{statementUrl}'",
                    $"href={statementUrl}"
                };
                
                var found = patterns.Any(pattern => htmlContentLower.Contains(pattern));
                
                if (found)
                {
                    return (true, "Statement URL found on product website. Verification successful.");
                }
                else
                {
                    return (false, "Manual verification needed. Statement URL not found on product website.");
                }
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("401") || ex.Message.Contains("Unauthorized"))
            {
                return (false, "Manual confirmation needed. The product website returned an authentication error (401).");
            }
            catch (TaskCanceledException)
            {
                return (false, "Manual verification needed. Request to product website timed out.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during automatic verification for {FipsId}", fipsId);
                return (false, $"Manual verification needed. Error accessing product website: {ex.Message}");
            }
        }

        // POST: Admin/AccessibilityService/EnrollProduct
        [HttpPost]
        [Route("EnrollProduct")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnrollProduct(string fipsId)
        {
            // Check authorization
            if (!await IsAuthorizedAsync())
            {
                TempData["ErrorMessage"] = "You do not have permission to perform this action.";
                return RedirectToAction("Index", "Home");
            }

            try
            {
                // Check if product already enrolled
                var existing = await _context.ProductAccessibilities
                    .FirstOrDefaultAsync(pa => pa.FipsId == fipsId && !pa.IsDeleted);
                
                if (existing != null)
                {
                    TempData["ErrorMessage"] = "This product is already enrolled.";
                    return RedirectToAction(nameof(Index));
                }

                // Get product info from CMS
                var cmsProducts = await _productsApiService.GetProductsAsync();
                var productInfo = cmsProducts?.FirstOrDefault(p => p.FipsId == fipsId);
                
                var productAccessibility = new ProductAccessibility
                {
                    FipsId = fipsId,
                    ProductName = productInfo?.Title ?? fipsId,
                    ProductPhase = productInfo?.Phase,
                    SlaResponseDays = 10, // Default value
                    ComplaintsEmail = User.Identity?.Name ?? "admin@example.com", // Default, should be updated later
                    WcagVersion = "2.2",
                    WcagLevel = "AA",
                    EnrolledAt = DateTime.UtcNow,
                    EnrolledBy = User.Identity?.Name,
                    IsActive = true,
                    UpdatedAt = DateTime.UtcNow
                };
                
                _context.ProductAccessibilities.Add(productAccessibility);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Product enrolled successfully.";
                return RedirectToAction(nameof(ProductDetails), new { fipsId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enrolling product {FipsId}", fipsId);
                TempData["ErrorMessage"] = "An error occurred while enrolling the product.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Admin/AccessibilityService/ProductDetails/{fipsId}
        [HttpGet]
        [Route("ProductDetails/{fipsId}")]
        public async Task<IActionResult> ProductDetails(string fipsId)
        {
            // Check authorization
            if (!await IsAuthorizedAsync())
            {
                TempData["ErrorMessage"] = "You do not have permission to access this section.";
                return RedirectToAction("Index", "Home");
            }

            // Get pending retest count for navigation
            var pendingRetestCount = await _context.AccessibilityRetestRequests
                .CountAsync(rr => rr.IsCompleted == null);
            ViewBag.PendingRetestCount = pendingRetestCount;

            // Get CMS product data (for Product URL)
            var cmsProduct = await _productsApiService.GetProductByFipsIdAsync(fipsId);
            ViewBag.CmsProduct = cmsProduct;

            var product = await _context.ProductAccessibilities
                .Include(pa => pa.Issues.Where(i => !i.IsDeleted))
                    .ThenInclude(i => i.WcagCriteriaLinks)
                        .ThenInclude(link => link.WcagCriterion)
                .Include(pa => pa.Issues.Where(i => !i.IsDeleted))
                    .ThenInclude(i => i.RetestRequests)
                .Include(pa => pa.AuditHistories.Where(a => !a.IsDeleted))
                .FirstOrDefaultAsync(pa => pa.FipsId == fipsId && !pa.IsDeleted);

            // If product not enrolled, get CMS product info for enrollment view
            if (product == null)
            {
                // Check if product exists in CMS
                if (cmsProduct == null)
                {
                    TempData["ErrorMessage"] = "Product not found.";
                    return RedirectToAction(nameof(Index));
                }

                ViewBag.IsNotEnrolled = true;
                ViewBag.RetestRequests = new List<Compass.Models.AccessibilityRetestRequest>();

                // Return view with null model to show enrollment option
                return View("~/Views/Admin/AccessibilityService/ProductDetails.cshtml", (ProductAccessibility?)null);
            }

            // Get retest requests for this product
            var retestRequests = await _context.AccessibilityRetestRequests
                .Include(rr => rr.AccessibilityIssue)
                    .ThenInclude(ai => ai.WcagCriteriaLinks)
                        .ThenInclude(link => link.WcagCriterion)
                .Where(rr => rr.AccessibilityIssue.ProductAccessibilityId == product.Id)
                .OrderByDescending(rr => rr.RequestedAt)
                .ToListAsync();

            ViewBag.RetestRequests = retestRequests;

            return View("~/Views/Admin/AccessibilityService/ProductDetails.cshtml", product);
        }
    }
}

