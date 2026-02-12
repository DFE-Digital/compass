using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Compass.Attributes;
using Compass.Data;
using Compass.Services;
using Compass.Models;

namespace Compass.Controllers;

[Authorize]
[RequireDesignOpsAdmin]
public class DesignOpsController : Controller
{
    private static readonly string[] EditableProductRoleFields =
    {
        "service_owner",
        "product_manager",
        "delivery_manager",
        "Information_asset_owner",
        "reporting_user",
        "senior_responsible_officer",
        "service_designs",
        "user_researchers"
    };

    private readonly ILogger<DesignOpsController> _logger;
    private readonly CompassDbContext _context;
    private readonly IProductsApiService _productsApiService;

    public DesignOpsController(
        ILogger<DesignOpsController> logger, 
        CompassDbContext context, 
        IProductsApiService productsApiService)
    {
        _logger = logger;
        _context = context;
        _productsApiService = productsApiService;
    }

    // GET: DesignOps/Dashboard
    public async Task<IActionResult> Dashboard()
    {
        try
        {
            ViewData["Title"] = "Design Operations Dashboard";
            
            // Get pending retest requests count
            var pendingRetestCount = await _context.AccessibilityRetestRequests
                .CountAsync(rr => rr.IsCompleted == null);
            
            // Get pending verification requests count
            var pendingVerificationCount = await _context.StatementVerificationRequests
                .CountAsync(vr => vr.IsCompleted == null);
            
            // Get all enrolled products with issues
            var enrolledProducts = await _context.ProductAccessibilities
                .Include(pa => pa.Issues.Where(i => !i.IsDeleted))
                .Where(pa => !pa.IsDeleted && pa.IsActive)
                .ToListAsync();
            
            // Count open accessibility issues (not resolved, not wont_fix, not deleted)
            var openIssuesCount = enrolledProducts
                .Sum(p => p.Issues.Count(i => i.Status != "resolved" && i.Status != "wont_fix"));
            
            // Get total products from CMS
            var cmsProducts = await _productsApiService.GetProductsAsync();
            var totalProducts = cmsProducts?.Count ?? 0;
            var enrolledProductsCount = enrolledProducts.Count;
            var enrollmentPercentage = totalProducts > 0 
                ? Math.Round((double)enrolledProductsCount / totalProducts * 100, 1) 
                : 0;
            
            ViewBag.PendingRetestCount = pendingRetestCount;
            ViewBag.PendingVerificationCount = pendingVerificationCount;
            ViewBag.OpenIssuesCount = openIssuesCount;
            ViewBag.EnrolledProductsCount = enrolledProductsCount;
            ViewBag.TotalProducts = totalProducts;
            ViewBag.EnrollmentPercentage = enrollmentPercentage;
            
            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading Design Operations dashboard");
            TempData["ErrorMessage"] = "An error occurred while loading the dashboard. Please try again.";
            return View();
        }
    }

    // GET: DesignOps/AccessibilityOversight
    public async Task<IActionResult> AccessibilityOversight()
    {
        try
        {
            ViewData["Title"] = "Accessibility Issues and Statements - Oversight";
            
            // Get all products from CMS
            var allCmsProducts = await _productsApiService.GetProductsAsync();
            var totalProducts = allCmsProducts?.Count ?? 0;
            
            // Get all enrolled products with full details
            var enrolledProducts = await _context.ProductAccessibilities
                .Include(pa => pa.Issues.Where(i => !i.IsDeleted))
                    .ThenInclude(i => i.WcagCriteriaLinks)
                        .ThenInclude(link => link.WcagCriterion)
                .Where(pa => !pa.IsDeleted && pa.IsActive)
                .ToListAsync();
            
            var enrolledCount = enrolledProducts.Count;
            var notEnrolledCount = totalProducts - enrolledCount;
            
            // Enrollment progress to 100% by 31 March 2026
            var targetDate = new DateTime(2026, 3, 31);
            var daysRemaining = (targetDate - DateTime.UtcNow).Days;
            var enrollmentProgress = totalProducts > 0 ? (double)enrolledCount / totalProducts * 100 : 0;
            var enrollmentTarget = 100.0;
            var productsNeeded = totalProducts - enrolledCount;
            
            // Statement validation status
            var validatedStatements = enrolledProducts.Count(p => p.StatementInstalled && p.VerifiedAt.HasValue);
            var notValidatedStatements = enrolledProducts.Count(p => !p.StatementInstalled || !p.VerifiedAt.HasValue);
            
            // Issues by WCAG criteria
            var allIssues = enrolledProducts
                .SelectMany(p => p.Issues.Where(i => !i.IsDeleted && i.Status != "resolved" && i.Status != "wont_fix"))
                .ToList();
            
            var issuesByWcagCriteria = allIssues
                .Where(i => i.WcagCriteriaLinks.Any())
                .SelectMany(i => i.WcagCriteriaLinks)
                .GroupBy(link => link.WcagCriterion.Criterion)
                .Select(g => new
                {
                    Criterion = g.Key,
                    Count = g.Count(),
                    Level = g.First().WcagCriterion.Level,
                    Version = g.First().WcagCriterion.Version
                })
                .OrderByDescending(x => x.Count)
                .ToList();
            
            // Retest requests
            var retestRequests = await _context.AccessibilityRetestRequests
                .Include(rr => rr.AccessibilityIssue)
                    .ThenInclude(i => i.ProductAccessibility)
                .Where(rr => !rr.IsCompleted.HasValue)
                .OrderBy(rr => rr.RequestedAt)
                .ToListAsync();
            
            // Verification requests
            var verificationRequests = await _context.StatementVerificationRequests
                .Include(vr => vr.ProductAccessibility)
                .Where(vr => !vr.IsCompleted.HasValue)
                .OrderBy(vr => vr.RequestedAt)
                .ToListAsync();
            
            // Issues with upcoming due dates (next 30 days)
            var upcomingDueDate = DateTime.UtcNow.AddDays(30);
            var issuesWithUpcomingDueDates = allIssues
                .Where(i => i.PlannedResolutionDate.HasValue && 
                           i.PlannedResolutionDate.Value >= DateTime.UtcNow.Date &&
                           i.PlannedResolutionDate.Value <= upcomingDueDate &&
                           i.Status != "resolved")
                .OrderBy(i => i.PlannedResolutionDate)
                .ToList();
            
            // Issues not being resolved (status is open but no planned resolution date or past due)
            var issuesNotBeingResolved = allIssues
                .Where(i => i.Status == "open" && 
                           (!i.PlannedResolutionDate.HasValue || 
                            i.PlannedResolutionDate.Value < DateTime.UtcNow.Date))
                .ToList();
            
            // Business area risk analysis
            var businessAreaRisk = new List<dynamic>();
            if (allCmsProducts != null)
            {
                var businessAreaGroups = allCmsProducts
                    .GroupBy(p => p.CategoryValues?
                        .FirstOrDefault(cv => cv.CategoryType?.Name?.Equals("Business area", StringComparison.OrdinalIgnoreCase) == true)
                        ?.Name ?? "Unknown")
                    .ToList();
                
                foreach (var group in businessAreaGroups)
                {
                    var businessArea = group.Key;
                    var productsInArea = group.ToList();
                    var enrolledInArea = productsInArea
                        .Where(p => enrolledProducts.Any(ep => ep.FipsId == p.FipsId))
                        .ToList();
                    var notEnrolledInArea = productsInArea.Count - enrolledInArea.Count;
                    
                    var enrolledProductAccessibilities = enrolledProducts
                        .Where(ep => enrolledInArea.Any(p => p.FipsId == ep.FipsId))
                        .ToList();
                    
                    var totalIssuesInArea = enrolledProductAccessibilities
                        .Sum(ep => ep.Issues.Count(i => !i.IsDeleted && i.Status != "resolved" && i.Status != "wont_fix"));
                    
                    var overdueIssuesInArea = enrolledProductAccessibilities
                        .Sum(ep => ep.Issues.Count(i => 
                            !i.IsDeleted && 
                            i.Status != "resolved" && 
                            i.Status != "wont_fix" &&
                            i.PlannedResolutionDate.HasValue &&
                            i.PlannedResolutionDate.Value < DateTime.UtcNow.Date));
                    
                    var enrollmentRate = productsInArea.Count > 0 
                        ? (double)enrolledInArea.Count / productsInArea.Count * 100 
                        : 0;
                    
                    // Calculate risk level
                    var riskLevel = "low";
                    var riskScore = 0;
                    
                    if (notEnrolledInArea > 0) riskScore += 2;
                    if (totalIssuesInArea > 10) riskScore += 2;
                    else if (totalIssuesInArea > 5) riskScore += 1;
                    if (overdueIssuesInArea > 5) riskScore += 2;
                    else if (overdueIssuesInArea > 0) riskScore += 1;
                    if (enrollmentRate < 50) riskScore += 2;
                    else if (enrollmentRate < 75) riskScore += 1;
                    
                    if (riskScore >= 5) riskLevel = "high";
                    else if (riskScore >= 3) riskLevel = "medium";
                    
                    businessAreaRisk.Add(new
                    {
                        BusinessArea = businessArea,
                        TotalProducts = productsInArea.Count,
                        EnrolledProducts = enrolledInArea.Count,
                        NotEnrolledProducts = notEnrolledInArea,
                        EnrollmentRate = enrollmentRate,
                        TotalIssues = totalIssuesInArea,
                        OverdueIssues = overdueIssuesInArea,
                        RiskLevel = riskLevel,
                        RiskScore = riskScore
                    });
                }
                
                businessAreaRisk = businessAreaRisk
                    .OrderByDescending(ba => ((dynamic)ba).RiskScore)
                    .ThenByDescending(ba => ((dynamic)ba).NotEnrolledProducts)
                    .ThenByDescending(ba => ((dynamic)ba).TotalIssues)
                    .ToList();
            }
            
            // Products with validated statements
            var productsWithValidatedStatements = enrolledProducts
                .Where(p => p.StatementInstalled && p.VerifiedAt.HasValue)
                .Select(p => new { FipsId = p.FipsId, ProductName = p.ProductName, VerifiedAt = p.VerifiedAt })
                .ToList();
            
            // Products without validated statements
            var productsWithoutValidatedStatements = enrolledProducts
                .Where(p => !p.StatementInstalled || !p.VerifiedAt.HasValue)
                .Select(p => new { FipsId = p.FipsId, ProductName = p.ProductName, StatementInstalled = p.StatementInstalled })
                .ToList();
            
            ViewBag.TotalProducts = totalProducts;
            ViewBag.EnrolledCount = enrolledCount;
            ViewBag.NotEnrolledCount = notEnrolledCount;
            ViewBag.EnrollmentProgress = enrollmentProgress;
            ViewBag.EnrollmentTarget = enrollmentTarget;
            ViewBag.TargetDate = targetDate;
            ViewBag.DaysRemaining = daysRemaining;
            ViewBag.ProductsNeeded = productsNeeded;
            
            ViewBag.ValidatedStatements = validatedStatements;
            ViewBag.NotValidatedStatements = notValidatedStatements;
            
            ViewBag.IssuesByWcagCriteria = issuesByWcagCriteria;
            ViewBag.TotalOpenIssues = allIssues.Count;
            
            ViewBag.RetestRequests = retestRequests;
            ViewBag.RetestRequestsCount = retestRequests.Count;
            
            ViewBag.VerificationRequests = verificationRequests;
            ViewBag.VerificationRequestsCount = verificationRequests.Count;
            
            ViewBag.IssuesWithUpcomingDueDates = issuesWithUpcomingDueDates;
            ViewBag.IssuesWithUpcomingDueDatesCount = issuesWithUpcomingDueDates.Count;
            
            ViewBag.IssuesNotBeingResolved = issuesNotBeingResolved;
            ViewBag.IssuesNotBeingResolvedCount = issuesNotBeingResolved.Count;
            
            ViewBag.BusinessAreaRisk = businessAreaRisk;
            
            ViewBag.ProductsWithValidatedStatements = productsWithValidatedStatements;
            ViewBag.ProductsWithoutValidatedStatements = productsWithoutValidatedStatements;
            
            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading Accessibility Oversight dashboard");
            TempData["ErrorMessage"] = "An error occurred while loading the accessibility oversight dashboard. Please try again.";
            return View();
        }
    }

    // GET: DesignOps/AccessibilityOversight/RetestRequests
    public async Task<IActionResult> RetestRequests()
    {
        try
        {
            ViewData["Title"] = "Accessibility Retest Requests - Oversight";
            
            var retestRequests = await _context.AccessibilityRetestRequests
                .Include(rr => rr.AccessibilityIssue)
                    .ThenInclude(i => i.ProductAccessibility)
                .Include(rr => rr.AccessibilityIssue)
                    .ThenInclude(i => i.WcagCriteriaLinks)
                        .ThenInclude(link => link.WcagCriterion)
                .Where(rr => !rr.IsCompleted.HasValue)
                .OrderBy(rr => rr.RequestedAt)
                .ToListAsync();
            
            ViewBag.RetestRequests = retestRequests;
            ViewBag.RetestRequestsCount = retestRequests.Count;
            
            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading Retest Requests");
            TempData["ErrorMessage"] = "An error occurred while loading retest requests. Please try again.";
            return View();
        }
    }

    // GET: DesignOps/AccessibilityOversight/OpenIssues
    public async Task<IActionResult> OpenIssues()
    {
        try
        {
            ViewData["Title"] = "Open Accessibility Issues - Oversight";
            
            var enrolledProducts = await _context.ProductAccessibilities
                .Include(pa => pa.Issues.Where(i => !i.IsDeleted))
                    .ThenInclude(i => i.WcagCriteriaLinks)
                        .ThenInclude(link => link.WcagCriterion)
                .Where(pa => !pa.IsDeleted && pa.IsActive)
                .ToListAsync();
            
            var openIssues = enrolledProducts
                .SelectMany(p => p.Issues.Where(i => !i.IsDeleted && i.Status != "resolved" && i.Status != "wont_fix"))
                .OrderByDescending(i => i.IdentifiedDate)
                .ToList();
            
            ViewBag.OpenIssues = openIssues;
            ViewBag.OpenIssuesCount = openIssues.Count;
            
            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading Open Issues");
            TempData["ErrorMessage"] = "An error occurred while loading open issues. Please try again.";
            return View();
        }
    }

    // GET: DesignOps/AccessibilityOversight/VerificationRequests
    public async Task<IActionResult> VerificationRequests()
    {
        try
        {
            ViewData["Title"] = "Statement Verification Requests - Oversight";
            
            var verificationRequests = await _context.StatementVerificationRequests
                .Include(vr => vr.ProductAccessibility)
                .Where(vr => !vr.IsCompleted.HasValue)
                .OrderBy(vr => vr.RequestedAt)
                .ToListAsync();
            
            // Fetch product URLs from CMS
            var productUrls = new Dictionary<string, string>();
            foreach (var request in verificationRequests)
            {
                if (!string.IsNullOrEmpty(request.ProductAccessibility.FipsId))
                {
                    try
                    {
                        var product = await _productsApiService.GetProductByFipsIdAsync(request.ProductAccessibility.FipsId);
                        if (product != null && !string.IsNullOrEmpty(product.ProductUrl))
                        {
                            productUrls[request.ProductAccessibility.FipsId] = product.ProductUrl;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error fetching product URL for {FipsId}", request.ProductAccessibility.FipsId);
                    }
                }
            }
            
            ViewBag.VerificationRequests = verificationRequests;
            ViewBag.VerificationRequestsCount = verificationRequests.Count;
            ViewBag.ProductUrls = productUrls;
            
            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading Verification Requests");
            TempData["ErrorMessage"] = "An error occurred while loading verification requests. Please try again.";
            return View();
        }
    }

    // GET: DesignOps/AccessibilityOversight/ValidatedStatements
    public async Task<IActionResult> ValidatedStatements()
    {
        try
        {
            ViewData["Title"] = "Validated Accessibility Statements - Oversight";
            
            var enrolledProducts = await _context.ProductAccessibilities
                .Where(pa => !pa.IsDeleted && pa.IsActive && pa.StatementInstalled && pa.VerifiedAt.HasValue)
                .OrderBy(pa => pa.ProductName)
                .ToListAsync();
            
            ViewBag.ValidatedStatements = enrolledProducts;
            ViewBag.ValidatedStatementsCount = enrolledProducts.Count;
            
            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading Validated Statements");
            TempData["ErrorMessage"] = "An error occurred while loading validated statements. Please try again.";
            return View();
        }
    }

    // POST: DesignOps/VerifyStatement
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyStatement(int requestId, string? adminNotes)
    {
        try
        {
            var request = await _context.StatementVerificationRequests
                .Include(vr => vr.ProductAccessibility)
                .FirstOrDefaultAsync(vr => vr.Id == requestId);

            if (request == null)
            {
                TempData["ErrorMessage"] = "Verification request not found.";
                return RedirectToAction(nameof(VerificationRequests));
            }

            // Mark request as completed and verified
            request.IsCompleted = true;
            request.VerificationResult = true;
            request.AdminNotes = adminNotes;
            request.CompletedBy = User.Identity?.Name;
            request.CompletedAt = DateTime.UtcNow;
            request.UpdatedAt = DateTime.UtcNow;

            // Update ProductAccessibility
            var productAccessibility = request.ProductAccessibility;
            productAccessibility.StatementInstalled = true;
            productAccessibility.VerifiedBy = User.Identity?.Name;
            productAccessibility.VerifiedAt = DateTime.UtcNow;
            productAccessibility.StatementVerificationMethod = "Manual";
            productAccessibility.UpdatedAt = DateTime.UtcNow;
            productAccessibility.UpdatedBy = User.Identity?.Name;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Statement installation verified for {productAccessibility.ProductName}.";
            return RedirectToAction(nameof(VerificationRequests));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying statement for request {RequestId}", requestId);
            TempData["ErrorMessage"] = "An error occurred while verifying the statement. Please try again.";
            return RedirectToAction(nameof(VerificationRequests));
        }
    }

    // POST: DesignOps/RejectVerification
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectVerification(int requestId, string adminNotes, bool requestAgain = false)
    {
        try
        {
            var request = await _context.StatementVerificationRequests
                .Include(vr => vr.ProductAccessibility)
                .FirstOrDefaultAsync(vr => vr.Id == requestId);

            if (request == null)
            {
                TempData["ErrorMessage"] = "Verification request not found.";
                return RedirectToAction(nameof(VerificationRequests));
            }

            // Mark request as completed but not verified
            request.IsCompleted = true;
            request.VerificationResult = false;
            request.AdminNotes = adminNotes;
            request.CompletedBy = User.Identity?.Name;
            request.CompletedAt = DateTime.UtcNow;
            request.UpdatedAt = DateTime.UtcNow;

            // If requesting again, create a new verification request
            if (requestAgain)
            {
                var newRequest = new StatementVerificationRequest
                {
                    ProductAccessibilityId = request.ProductAccessibilityId,
                    RequestedBy = request.RequestedBy,
                    RequestorEmail = request.RequestorEmail,
                    RequestedAt = DateTime.UtcNow,
                    RequestNotes = $"Re-requested after rejection. Previous admin notes: {adminNotes}",
                    IsCompleted = null,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.StatementVerificationRequests.Add(newRequest);
            }

            await _context.SaveChangesAsync();

            var message = requestAgain 
                ? $"Verification rejected and new request created for {request.ProductAccessibility.ProductName}."
                : $"Verification rejected for {request.ProductAccessibility.ProductName}.";

            TempData["SuccessMessage"] = message;
            return RedirectToAction(nameof(VerificationRequests));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting verification for request {RequestId}", requestId);
            TempData["ErrorMessage"] = "An error occurred while rejecting the verification. Please try again.";
            return RedirectToAction(nameof(VerificationRequests));
        }
    }

    // GET: DesignOps/AccessDenied
    public IActionResult AccessDenied()
    {
        ViewData["Title"] = "Access Denied";
        return View();
    }

    // GET: DesignOps/FipsRoleManagement
    public async Task<IActionResult> FipsRoleManagement(string search = "", string state = "")
    {
        try
        {
            ViewData["Title"] = "FIPS role management";

            var allProducts = await _productsApiService.GetAllProductsAsync();
            var fipsProducts = allProducts
                // Match ProductReporting Commission "all services" eligibility rules
                .Where(p => p.State != null &&
                            p.State.Equals("Active", StringComparison.OrdinalIgnoreCase) &&
                            p.PublishedAt.HasValue &&
                            (string.IsNullOrEmpty(p.Phase) ||
                             (!p.Phase.Equals("Decommissioned", StringComparison.OrdinalIgnoreCase) &&
                              !p.Phase.Equals("Decommissioning", StringComparison.OrdinalIgnoreCase))))
                .Where(IsNotDataOnlyTypeProduct)
                .Where(p => !string.IsNullOrWhiteSpace(p.FipsId))
                .ToList();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var trimmedSearch = search.Trim();
                fipsProducts = fipsProducts
                    .Where(p =>
                        (!string.IsNullOrWhiteSpace(p.FipsId) && p.FipsId.Contains(trimmedSearch, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrWhiteSpace(p.Title) && p.Title.Contains(trimmedSearch, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(state))
            {
                fipsProducts = fipsProducts
                    .Where(p => string.Equals(p.State, state, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            var orderedProducts = fipsProducts
                .OrderBy(p => p.Title)
                .ThenBy(p => p.FipsId)
                .ToList();

            ViewBag.CurrentSearch = search;
            ViewBag.CurrentState = state;
            ViewBag.AvailableStates = allProducts
                .Select(p => p.State)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(s => s)
                .ToList();

            return View(orderedProducts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading FIPS role management view");
            TempData["ErrorMessage"] = "An error occurred while loading FIPS role management. Please try again.";
            return View(new List<ProductDto>());
        }
    }

    // POST: DesignOps/UpdateFipsRole
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateFipsRole(
        string fipsId,
        string roleFieldName,
        string roleDisplayName,
        string? entraUserObjectId,
        string? entraUserEmail,
        string? entraUserName,
        string? returnUrl = null)
    {
        if (string.IsNullOrWhiteSpace(fipsId))
        {
            TempData["ErrorMessage"] = "A FIPS ID is required.";
            return RedirectToFipsRoleManagement(returnUrl);
        }

        if (!EditableProductRoleFields.Contains(roleFieldName, StringComparer.Ordinal))
        {
            TempData["ErrorMessage"] = "Invalid role field.";
            return RedirectToFipsRoleManagement(returnUrl);
        }

        if (string.IsNullOrWhiteSpace(entraUserEmail) || string.IsNullOrWhiteSpace(entraUserObjectId))
        {
            TempData["ErrorMessage"] = "Please select a user from Entra search results before saving.";
            return RedirectToFipsRoleManagement(returnUrl);
        }

        try
        {
            var entraUser = await _productsApiService.GetOrCreateEntraUserAsync(
                entraUserEmail.Trim(),
                entraUserObjectId.Trim(),
                string.IsNullOrWhiteSpace(entraUserName) ? entraUserEmail.Trim() : entraUserName.Trim());

            if (entraUser == null)
            {
                TempData["ErrorMessage"] = "Failed to create or fetch the selected Entra user.";
                return RedirectToFipsRoleManagement(returnUrl);
            }

            bool success;
            if (string.Equals(roleFieldName, "service_owner", StringComparison.Ordinal))
            {
                success = await _productsApiService.UpdateProductServiceOwnerAsync(fipsId.Trim(), entraUser.Id);
            }
            else
            {
                success = await _productsApiService.UpdateProductRoleAsync(fipsId.Trim(), roleFieldName, entraUser.Id);
            }

            if (success)
            {
                var displayValue = !string.IsNullOrWhiteSpace(entraUser.DisplayName)
                    ? entraUser.DisplayName
                    : entraUser.EmailAddress ?? "Unknown user";
                TempData["SuccessMessage"] = $"{roleDisplayName} updated for {fipsId.Trim()} to {displayValue}.";
            }
            else
            {
                TempData["ErrorMessage"] = $"Unable to update {roleDisplayName} for {fipsId.Trim()}.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating role {RoleFieldName} for {FipsId}", roleFieldName, fipsId);
            TempData["ErrorMessage"] = "An unexpected error occurred while updating the role.";
        }

        return RedirectToFipsRoleManagement(returnUrl);
    }

    private IActionResult RedirectToFipsRoleManagement(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(FipsRoleManagement));
    }

    private static bool IsNotDataOnlyTypeProduct(ProductDto product)
    {
        var types = product.CategoryValues?
            .Where(cv => cv.CategoryType?.Name?.Trim().Equals("Type", StringComparison.OrdinalIgnoreCase) == true)
            .Select(cv => cv.Name?.Trim() ?? string.Empty)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? new List<string>();

        if (!types.Any())
        {
            return true;
        }

        if (types.Count == 1 && types[0].Equals("Data", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (types.All(t => t.Equals("Data", StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        return true;
    }
}

