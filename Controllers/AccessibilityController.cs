using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Compass.Data;
using Compass.Models;
using Compass.Services;

namespace Compass.Controllers
{
    public class AccessibilityController : Controller
    {
        private readonly CompassDbContext _context;
        private readonly ILogger<AccessibilityController> _logger;
        private readonly IProductsApiService _productsApiService;

        public AccessibilityController(
            CompassDbContext context, 
            ILogger<AccessibilityController> logger,
            IProductsApiService productsApiService)
        {
            _context = context;
            _logger = logger;
            _productsApiService = productsApiService;
        }

        // GET: Accessibility/SearchProducts (for autocomplete)
        [HttpGet]
        public async Task<IActionResult> SearchProducts(string q)
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return Json(new { products = new object[0] });
            }
            
            var cmsProducts = await _productsApiService.GetProductsAsync();
            
            if (cmsProducts == null)
            {
                return Json(new { products = new object[0] });
            }
            
            var filteredProducts = cmsProducts
                .Where(p => !string.IsNullOrEmpty(p.FipsId) && 
                           (p.Title.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                            (p.FipsId != null && p.FipsId.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                            (p.Phase != null && p.Phase.Contains(q, StringComparison.OrdinalIgnoreCase))))
                .Take(10)
                .Select(p => new {
                    fipsId = p.FipsId,
                    title = p.Title,
                    phase = p.Phase
                })
                .ToList();
            
            return Json(new { products = filteredProducts });
        }

        // GET: Accessibility/Index
        public async Task<IActionResult> Index(
            string tab = "products",
            int productsPage = 1,
            int issuesPage = 1,
            int auditsPage = 1,
            string? search = null,
            string? status = null,
            string? issues = null,
            string? statement = null,
            string? issueStatus = null,
            string? issueLevel = null,
            string? auditType = null)
        {
            const int pageSize = 25;
            
            // Load all products with issues and audits for summary calculations
            var allProducts = await _context.ProductAccessibilities
                .Include(pa => pa.Issues.Where(i => !i.IsDeleted))
                    .ThenInclude(i => i.WcagCriteriaLinks)
                        .ThenInclude(link => link.WcagCriterion)
                .Include(pa => pa.AuditHistories.Where(ah => !ah.IsDeleted))
                .Where(pa => !pa.IsDeleted && pa.IsActive)
                .ToListAsync();

            // Get total products from CMS
            var cmsProducts = await _productsApiService.GetProductsAsync();
            var totalProducts = cmsProducts?.Count ?? 0;

            // Calculate summary statistics
            var totalOpenIssues = allProducts.Sum(p => p.Issues.Count(i => i.Status != "resolved"));
            var totalEnrolledProducts = allProducts.Count;
            var overdueIssues = allProducts.Sum(p => p.Issues.Count(i => 
                i.Status != "resolved" && 
                i.PlannedResolutionDate.HasValue && 
                i.PlannedResolutionDate.Value < DateTime.UtcNow.Date));
            var totalAuditSpend = allProducts.Sum(p => p.AuditHistories
                .Where(a => a.Cost.HasValue)
                .Sum(a => a.Cost.Value));

            ViewBag.SummaryStats = new
            {
                TotalOpenIssues = totalOpenIssues,
                TotalEnrolledProducts = totalEnrolledProducts,
                TotalProducts = totalProducts,
                OverdueIssues = overdueIssues,
                TotalAuditSpend = totalAuditSpend
            };

            ViewBag.ActiveTab = tab;

            // Handle Products tab
            if (tab == "products")
            {
                // Get all CMS products
                if (cmsProducts == null)
                {
                    cmsProducts = await _productsApiService.GetProductsAsync();
                }
                
                // Create a dictionary for quick lookup of enrolled products
                var enrolledDict = allProducts.ToDictionary(pa => pa.FipsId, pa => pa);

                // Create view model lists
                var enrolledProductViewModels = new List<dynamic>();
                var allProductViewModels = new List<dynamic>();

                foreach (var cmsProduct in cmsProducts)
                {
                    var isEnrolled = enrolledDict.TryGetValue(cmsProduct.FipsId, out var enrolled);
                    
                    // Apply search filter
                    if (!string.IsNullOrWhiteSpace(search))
                    {
                        var searchLower = search.ToLower();
                        var titleMatch = cmsProduct.Title.ToLower().Contains(searchLower);
                        var fipsIdMatch = !string.IsNullOrEmpty(cmsProduct.FipsId) && cmsProduct.FipsId.ToLower().Contains(searchLower);
                        
                        if (!titleMatch && !fipsIdMatch)
                        {
                            continue;
                        }
                    }

                    var openIssuesCount = 0;
                    var pastDueCount = 0;
                    var isVerified = false;
                    var complianceStatus = "compliant";
                    var complianceBadge = "badge-success";
                    var lastAudit = (dynamic?)null;

                    if (isEnrolled)
                    {
                        openIssuesCount = enrolled.Issues.Count(i => i.Status != "resolved" && !i.IsDeleted);
                        pastDueCount = enrolled.Issues.Count(i => 
                            !i.IsDeleted && 
                            i.Status != "resolved" && 
                            i.PlannedResolutionDate.HasValue &&
                            i.PlannedResolutionDate.Value < DateTime.UtcNow.Date);
                        isVerified = enrolled.StatementInstalled && enrolled.VerifiedAt.HasValue;
                        
                        // Calculate compliance status with WCAG criteria percentage
                        var openIssues = enrolled.Issues.Where(i => i.Status != "resolved" && !i.IsDeleted).ToList();
                        if (!openIssues.Any())
                        {
                            complianceStatus = "compliant";
                            complianceBadge = "badge-success";
                        }
                        else
                        {
                            // Get distinct WCAG criteria that have issues
                            var distinctCriteriaWithIssues = openIssues
                                .Where(i => i.IssueType == "WCAG" && 
                                            (i.WcagCriteriaLinks.Any() || !string.IsNullOrEmpty(i.WcagCriteria)))
                                .SelectMany(i => i.WcagCriteriaLinks.Any() 
                                    ? i.WcagCriteriaLinks.Select(link => link.WcagCriterion.Criterion)
                                    : new[] { i.WcagCriteria ?? "" })
                                .Where(c => !string.IsNullOrEmpty(c))
                                .Distinct()
                                .ToList();

                            var distinctCriteriaCount = distinctCriteriaWithIssues.Count;

                            // Get total distinct WCAG criteria for the product's WCAG version and level
                            var totalDistinctCriteria = await _context.WcagCriteria
                                .Where(wc => wc.Version == enrolled.WcagVersion && 
                                            wc.Level == enrolled.WcagLevel && 
                                            wc.IsActive)
                                .Select(wc => wc.Criterion)
                                .Distinct()
                                .CountAsync();

                            if (totalDistinctCriteria == 0)
                            {
                                // Fallback if no criteria found - treat any open issue as partially compliant
                                complianceStatus = openIssues.Any() ? "partially compliant" : "compliant";
                                complianceBadge = openIssues.Any() ? "badge-warning" : "badge-success";
                            }
                            else
                            {
                                // Calculate percentage: more than 50% = non-compliant
                                var threshold = totalDistinctCriteria / 2.0;
                                
                                if (distinctCriteriaCount > threshold)
                                {
                                    // More than half of distinct criteria have issues = non-compliant
                                    complianceStatus = "non-compliant";
                                    complianceBadge = "badge-danger";
                                }
                                else if (distinctCriteriaCount > 0)
                                {
                                    // 1 or more but less than or equal to half = partially-compliant
                                    complianceStatus = "partially compliant";
                                    complianceBadge = "badge-warning";
                                }
                                else
                                {
                                    // 0 distinct criteria with issues = compliant
                                    complianceStatus = "compliant";
                                    complianceBadge = "badge-success";
                                }
                            }
                        }
                        
                        lastAudit = enrolled.AuditHistories.Where(a => !a.IsDeleted).OrderByDescending(a => a.AuditDate).FirstOrDefault();
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
                        IsVerified = isVerified,
                        ComplianceStatus = complianceStatus,
                        ComplianceBadge = complianceBadge,
                        LastAudit = lastAudit
                    };

                    if (isEnrolled)
                    {
                        // Apply filters for enrolled products
                        var shouldInclude = true;
                        
                        if (!string.IsNullOrWhiteSpace(status))
                        {
                            shouldInclude = status switch
                            {
                                "compliant" => complianceStatus == "compliant",
                                "partially" => complianceStatus == "partially compliant",
                                "non-compliant" => complianceStatus == "non-compliant",
                                _ => true
                            };
                            ViewBag.CurrentStatus = status;
                        }
                        
                        if (shouldInclude && !string.IsNullOrWhiteSpace(issues))
                        {
                            shouldInclude = issues switch
                            {
                                "with-issues" => openIssuesCount > 0,
                                "no-issues" => openIssuesCount == 0,
                                _ => true
                            };
                            ViewBag.CurrentIssues = issues;
                        }
                        
                        if (shouldInclude && !string.IsNullOrWhiteSpace(statement))
                        {
                            shouldInclude = statement switch
                            {
                                "verified" => isVerified,
                                "not-verified" => !isVerified,
                                _ => true
                            };
                            ViewBag.CurrentStatement = statement;
                        }
                        
                        if (shouldInclude)
                        {
                            enrolledProductViewModels.Add(productViewModel);
                        }
                    }
                    else
                    {
                        // Non-enrolled products - no filtering needed
                        allProductViewModels.Add(productViewModel);
                    }
                }

                // Sort enrolled products
                enrolledProductViewModels = enrolledProductViewModels
                    .OrderBy(p => ((dynamic)p).ProductName)
                    .ToList();

                // Pagination for non-enrolled products
                var totalCount = allProductViewModels.Count;
                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
                var pagedNonEnrolledProducts = allProductViewModels
                    .OrderBy(p => ((dynamic)p).ProductName)
                    .Skip((productsPage - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                ViewBag.EnrolledProducts = enrolledProductViewModels;
                ViewBag.NonEnrolledProducts = pagedNonEnrolledProducts;
                ViewBag.ProductsPage = productsPage;
                ViewBag.ProductsTotalPages = totalPages;
                ViewBag.ProductsTotalCount = totalCount;
                
                // Pass search filter to view if set
                if (!string.IsNullOrWhiteSpace(search))
                {
                    ViewBag.CurrentSearch = search;
                }
                
                // Get the actual enrolled ProductAccessibility objects for the model
                // The view uses ViewBag for display, but needs the correct model type
                var enrolledFipsIds = enrolledProductViewModels
                    .Select(vm => ((dynamic)vm).FipsId as string)
                    .Where(fipsId => !string.IsNullOrEmpty(fipsId))
                    .ToList();
                
                var enrolledProductAccessibilities = allProducts
                    .Where(pa => enrolledFipsIds.Contains(pa.FipsId))
                    .OrderBy(pa => pa.ProductName)
                    .ToList();
                
                return View("~/Views/Apps/Accessibility/Index.cshtml", enrolledProductAccessibilities);
            }

            // Handle Issues tab
            if (tab == "issues")
            {
                var issuesQuery = _context.AccessibilityIssues
                    .Include(i => i.ProductAccessibility)
                    .Include(i => i.WcagCriteriaLinks)
                        .ThenInclude(link => link.WcagCriterion)
                    .Where(i => !i.IsDeleted)
                    .AsQueryable();

                // Filter by status
                if (!string.IsNullOrWhiteSpace(issueStatus))
                {
                    issuesQuery = issuesQuery.Where(i => i.Status == issueStatus);
                    ViewBag.CurrentIssueStatus = issueStatus;
                }
                else
                {
                    // Default to non-resolved issues
                    issuesQuery = issuesQuery.Where(i => i.Status != "resolved");
                }

                // Filter by level
                if (!string.IsNullOrWhiteSpace(issueLevel))
                {
                    if (issueLevel == "Best Practice")
                    {
                        issuesQuery = issuesQuery.Where(i => i.IssueType == "Best Practice");
                    }
                    else
                    {
                        issuesQuery = issuesQuery.Where(i => 
                            i.IssueType == "WCAG" && 
                            (i.WcagLevel == issueLevel || 
                             i.WcagCriteriaLinks.Any(link => link.WcagCriterion.Level == issueLevel)));
                    }
                    ViewBag.CurrentIssueLevel = issueLevel;
                }

                var allIssuesList = await issuesQuery.ToListAsync();

                // Order by WCAG level: AA first, then A, then Best Practice
                var orderedIssues = allIssuesList.OrderBy(i =>
                {
                    if (i.IssueType == "Best Practice") return 3; // Best Practice last
                    if (i.IssueType == "WCAG" && i.WcagCriteriaLinks.Any())
                    {
                        var highestLevel = i.WcagCriteriaLinks
                            .Select(link => link.WcagCriterion.Level)
                            .Max();
                        return highestLevel == "AA" ? 1 : highestLevel == "A" ? 2 : 4; // AA=1, A=2, AAA=4
                    }
                    // Fallback to deprecated WcagLevel
                    return i.WcagLevel switch
                    {
                        "AA" => 1,
                        "A" => 2,
                        "AAA" => 4,
                        _ => 5
                    };
                }).ThenBy(i => i.IssueType == "WCAG" && i.WcagCriteriaLinks.Any()
                    ? i.WcagCriteriaLinks.First().WcagCriterion.Criterion
                    : i.WcagCriteria ?? i.IssueTitle ?? "")
                .ToList();

                var totalIssues = orderedIssues.Count;
                var pagedIssues = orderedIssues
                    .Skip((issuesPage - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                ViewBag.IssuesPage = issuesPage;
                ViewBag.IssuesTotalPages = (int)Math.Ceiling((double)totalIssues / pageSize);
                ViewBag.IssuesTotalCount = totalIssues;
                ViewBag.Issues = pagedIssues;

                return View("~/Views/Apps/Accessibility/Index.cshtml", allProducts);
            }

            // Handle Audits tab
            if (tab == "audits")
            {
                var auditsQuery = _context.AuditHistories
                    .Include(ah => ah.ProductAccessibility)
                    .Where(ah => !ah.IsDeleted)
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(auditType))
                {
                    auditsQuery = auditsQuery.Where(ah => ah.AuditType == auditType);
                    ViewBag.CurrentAuditType = auditType;
                }

                var orderedAudits = auditsQuery
                    .OrderByDescending(ah => ah.AuditDate)
                    .ToList();

                var totalAudits = orderedAudits.Count;
                var pagedAudits = orderedAudits
                    .Skip((auditsPage - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                ViewBag.AuditsPage = auditsPage;
                ViewBag.AuditsTotalPages = (int)Math.Ceiling((double)totalAudits / pageSize);
                ViewBag.AuditsTotalCount = totalAudits;
                ViewBag.Audits = pagedAudits;

                return View("~/Views/Apps/Accessibility/Index.cshtml", allProducts);
            }

            // Default to products tab
            ViewBag.ActiveTab = "products";
            return View("~/Views/Apps/Accessibility/Index.cshtml", allProducts.Take(pageSize).ToList());
        }
        
        // GET: Accessibility/AllIssues
        public async Task<IActionResult> AllIssues()
        {
            var allIssues = await _context.AccessibilityIssues
                .Include(i => i.ProductAccessibility)
                .Where(i => !i.IsDeleted && i.Status != "resolved")
                .OrderBy(i => i.WcagCriteria)
                .ToListAsync();
            
            return View("~/Views/Apps/Accessibility/AllIssues.cshtml", allIssues);
        }

        // GET: Accessibility/Details/{fipsId}?tab=overview
        public async Task<IActionResult> Details(string fipsId, string tab = "overview")
        {
            // Get CMS product info first
            var cmsProducts = await _productsApiService.GetProductsAsync();
            var productInfo = cmsProducts?.FirstOrDefault(p => p.FipsId == fipsId);
            
            if (productInfo == null)
            {
                return NotFound();
            }
            
            var productAccessibility = await _context.ProductAccessibilities
                .Include(pa => pa.ContactMethods.Where(cm => cm.IsActive))
                .Include(pa => pa.AuditHistories.Where(ah => !ah.IsDeleted))
                .Include(pa => pa.Issues.Where(i => !i.IsDeleted))
                    .ThenInclude(i => i.Comments.Where(c => !c.IsDeleted))
                .Include(pa => pa.Issues.Where(i => !i.IsDeleted))
                    .ThenInclude(i => i.History)
                .Include(pa => pa.Issues.Where(i => !i.IsDeleted))
                    .ThenInclude(i => i.WcagCriteriaLinks)
                        .ThenInclude(link => link.WcagCriterion)
                .FirstOrDefaultAsync(pa => pa.FipsId == fipsId && !pa.IsDeleted);
            
            // If product not enrolled, show enrollment option
            if (productAccessibility == null)
            {
                ViewBag.CmsProduct = productInfo;
                ViewBag.IsNotEnrolled = true;
                ViewBag.CurrentTab = tab;
                return View("~/Views/Apps/Accessibility/Details.cshtml", (ProductAccessibility?)null);
            }
            
            // Update cached product info from CMS
            if (productInfo != null && 
                (productAccessibility.ProductName != productInfo.Title || 
                 productAccessibility.ProductPhase != productInfo.Phase))
            {
                productAccessibility.ProductName = productInfo.Title;
                productAccessibility.ProductPhase = productInfo.Phase;
                productAccessibility.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            
            // Calculate compliance status with WCAG criteria percentage
            var complianceStatus = "compliant";
            var complianceBadge = "badge-success";
            var complianceBgClass = "bg-success";
            
            var openIssues = productAccessibility.Issues.Where(i => i.Status != "resolved" && !i.IsDeleted).ToList();
            if (!openIssues.Any())
            {
                complianceStatus = "Compliant";
                complianceBadge = "badge-success";
                complianceBgClass = "bg-success";
            }
            else
            {
                // Get distinct WCAG criteria that have issues
                var distinctCriteriaWithIssues = openIssues
                    .Where(i => i.IssueType == "WCAG" && 
                                (i.WcagCriteriaLinks.Any() || !string.IsNullOrEmpty(i.WcagCriteria)))
                    .SelectMany(i => i.WcagCriteriaLinks.Any() 
                        ? i.WcagCriteriaLinks.Select(link => link.WcagCriterion.Criterion)
                        : new[] { i.WcagCriteria ?? "" })
                    .Where(c => !string.IsNullOrEmpty(c))
                    .Distinct()
                    .ToList();

                var distinctCriteriaCount = distinctCriteriaWithIssues.Count;

                // Get total distinct WCAG criteria for the product's WCAG version and level
                var totalDistinctCriteria = await _context.WcagCriteria
                    .Where(wc => wc.Version == productAccessibility.WcagVersion && 
                                wc.Level == productAccessibility.WcagLevel && 
                                wc.IsActive)
                    .Select(wc => wc.Criterion)
                    .Distinct()
                    .CountAsync();

                if (totalDistinctCriteria == 0)
                {
                    // Fallback if no criteria found - treat any open issue as partially compliant
                    complianceStatus = openIssues.Any() ? "Partially compliant" : "Compliant";
                    complianceBadge = openIssues.Any() ? "badge-warning" : "badge-success";
                    complianceBgClass = openIssues.Any() ? "bg-warning" : "bg-success";
                }
                else
                {
                    // Calculate percentage: more than 50% = non-compliant
                    var threshold = totalDistinctCriteria / 2.0;
                    
                    if (distinctCriteriaCount > threshold)
                    {
                        // More than half of distinct criteria have issues = non-compliant
                        complianceStatus = "Non-compliant";
                        complianceBadge = "badge-danger";
                        complianceBgClass = "bg-danger";
                    }
                    else if (distinctCriteriaCount > 0)
                    {
                        // 1 or more but less than or equal to half = partially-compliant
                        complianceStatus = "Partially compliant";
                        complianceBadge = "badge-warning";
                        complianceBgClass = "bg-warning";
                    }
                    else
                    {
                        // 0 distinct criteria with issues = compliant
                        complianceStatus = "Compliant";
                        complianceBadge = "badge-success";
                        complianceBgClass = "bg-success";
                    }
                }
            }
            
            // Pass CMS product data to view (for Product URL)
            ViewBag.CmsProduct = productInfo;
            ViewBag.ComplianceStatus = complianceStatus;
            ViewBag.ComplianceBadge = complianceBadge;
            ViewBag.ComplianceBgClass = complianceBgClass;
            
            ViewBag.CurrentTab = tab;
            return View("~/Views/Apps/Accessibility/Details.cshtml", productAccessibility);
        }

        // GET: Accessibility/ViewIssue/{id}?fipsId={fipsId}
        public async Task<IActionResult> ViewIssue(int id, string fipsId)
        {
            var issue = await _context.AccessibilityIssues
                .Include(i => i.ProductAccessibility)
                .Include(i => i.Comments.Where(c => !c.IsDeleted))
                .Include(i => i.History.OrderByDescending(h => h.ChangedAt))
                .Include(i => i.WcagCriteriaLinks)
                    .ThenInclude(link => link.WcagCriterion)
                .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted);
            
            if (issue == null)
            {
                return NotFound();
            }
            
            // Verify the issue belongs to the correct product
            if (issue.ProductAccessibility.FipsId != fipsId)
            {
                return NotFound();
            }
            
            ViewBag.FipsId = fipsId;
            ViewBag.ProductName = issue.ProductAccessibility.ProductName;
            return View("~/Views/Apps/Accessibility/IssueDetails.cshtml", issue);
        }

        // GET: Accessibility/Enroll
        public async Task<IActionResult> Enroll(string? fipsId)
        {
            ViewBag.FipsId = fipsId;
            
            // Get CMS products for autocomplete
            ViewBag.CmsProducts = await _productsApiService.GetProductsAsync();
            
            return View("~/Views/Apps/Accessibility/Enroll.cshtml");
        }

        // POST: Accessibility/Enroll
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Enroll(ProductAccessibility productAccessibility, List<ContactMethod> contactMethods)
        {
            try
            {
                // Check if product already enrolled
                var existing = await _context.ProductAccessibilities
                    .FirstOrDefaultAsync(pa => pa.FipsId == productAccessibility.FipsId && !pa.IsDeleted);
                
                if (existing != null)
                {
                    TempData["ErrorMessage"] = "This product is already enrolled in the accessibility management service.";
                    return RedirectToAction(nameof(Details), new { fipsId = productAccessibility.FipsId });
                }
                
                // Get product info from CMS
                var cmsProducts = await _productsApiService.GetProductsAsync();
                var productInfo = cmsProducts?.FirstOrDefault(p => p.FipsId == productAccessibility.FipsId);
                
                if (productInfo != null)
                {
                    productAccessibility.ProductName = productInfo.Title;
                    productAccessibility.ProductPhase = productInfo.Phase;
                }
                
                productAccessibility.EnrolledAt = DateTime.UtcNow;
                productAccessibility.EnrolledBy = User.Identity?.Name;
                productAccessibility.UpdatedAt = DateTime.UtcNow;
                
                _context.ProductAccessibilities.Add(productAccessibility);
                await _context.SaveChangesAsync();
                
                // Add contact methods
                if (contactMethods != null && contactMethods.Any())
                {
                    foreach (var cm in contactMethods.Where(c => !string.IsNullOrWhiteSpace(c.ContactDetail)))
                    {
                        cm.ProductAccessibilityId = productAccessibility.Id;
                        cm.CreatedAt = DateTime.UtcNow;
                        cm.UpdatedAt = DateTime.UtcNow;
                        _context.ContactMethods.Add(cm);
                    }
                    await _context.SaveChangesAsync();
                }
                
                TempData["SuccessMessage"] = "Product enrolled successfully in accessibility management service.";
                return RedirectToAction(nameof(Details), new { fipsId = productAccessibility.FipsId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enrolling product in accessibility service");
                TempData["ErrorMessage"] = "An error occurred while enrolling the product.";
                return View(productAccessibility);
            }
        }

        // POST: Accessibility/UpdateSettings
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSettings(string fipsId, int slaResponseDays, string complaintsEmail)
        {
            try
            {
                var productAccessibility = await _context.ProductAccessibilities
                    .FirstOrDefaultAsync(pa => pa.FipsId == fipsId && !pa.IsDeleted);
                
                if (productAccessibility == null)
                {
                    return NotFound();
                }
                
                productAccessibility.SlaResponseDays = slaResponseDays;
                productAccessibility.ComplaintsEmail = complaintsEmail;
                productAccessibility.UpdatedAt = DateTime.UtcNow;
                productAccessibility.UpdatedBy = User.Identity?.Name;
                
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Settings updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating accessibility settings");
                TempData["ErrorMessage"] = "An error occurred while updating settings.";
            }
            
            return RedirectToAction(nameof(Details), new { fipsId, tab = "settings" });
        }

        // POST: Accessibility/AddContactMethod
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddContactMethod(int productAccessibilityId, string fipsId, string type, string detail, string? description, int sortOrder)
        {
            try
            {
                var productAccessibility = await _context.ProductAccessibilities
                    .FirstOrDefaultAsync(pa => pa.Id == productAccessibilityId && !pa.IsDeleted);
                
                if (productAccessibility == null)
                {
                    return NotFound();
                }
                
                var contactMethod = new ContactMethod
                {
                    ProductAccessibilityId = productAccessibilityId,
                    ContactType = type,
                    ContactDetail = detail,
                    Description = description,
                    SortOrder = sortOrder,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                
                _context.ContactMethods.Add(contactMethod);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Contact method added successfully.";
                return RedirectToAction(nameof(Details), new { fipsId, tab = "settings" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding contact method");
                TempData["ErrorMessage"] = "An error occurred while adding contact method.";
                return RedirectToAction(nameof(Details), new { fipsId, tab = "settings" });
            }
        }

        // POST: Accessibility/DeleteContactMethod
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteContactMethod(int id, string fipsId)
        {
            try
            {
                var contactMethod = await _context.ContactMethods
                    .FirstOrDefaultAsync(cm => cm.Id == id);
                
                if (contactMethod == null)
                {
                    return NotFound();
                }
                
                _context.ContactMethods.Remove(contactMethod);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Contact method deleted successfully.";
                return RedirectToAction(nameof(Details), new { fipsId, tab = "settings" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting contact method");
                TempData["ErrorMessage"] = "An error occurred while deleting contact method.";
                return RedirectToAction(nameof(Details), new { fipsId, tab = "settings" });
            }
        }

        // POST: Accessibility/AddAudit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAudit(int productAccessibilityId, string fipsId, DateTime auditDate, string auditType, string auditedBy, decimal? cost, string? reportUrl, string? notes)
        {
            try
            {
                var productAccessibility = await _context.ProductAccessibilities
                    .FirstOrDefaultAsync(pa => pa.Id == productAccessibilityId && !pa.IsDeleted);
                
                if (productAccessibility == null)
                {
                    return NotFound();
                }
                
                var audit = new AuditHistory
                {
                    ProductAccessibilityId = productAccessibilityId,
                    AuditDate = auditDate,
                    AuditType = auditType,
                    AuditedBy = auditedBy,
                    Cost = cost,
                    ReportUrl = reportUrl,
                    Notes = notes,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = User.Identity?.Name,
                    UpdatedAt = DateTime.UtcNow
                };
                
                _context.AuditHistories.Add(audit);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Audit record added successfully.";
                return RedirectToAction(nameof(Details), new { fipsId, tab = "audits" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding audit record");
                TempData["ErrorMessage"] = "An error occurred while adding audit record.";
                return RedirectToAction(nameof(Details), new { fipsId, tab = "audits" });
            }
        }

        // POST: Accessibility/DeleteAudit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAudit(int id, string fipsId)
        {
            try
            {
                var audit = await _context.AuditHistories
                    .FirstOrDefaultAsync(ah => ah.Id == id);
                
                if (audit == null)
                {
                    return NotFound();
                }
                
                audit.IsDeleted = true;
                audit.UpdatedAt = DateTime.UtcNow;
                audit.UpdatedBy = User.Identity?.Name;
                
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Audit record deleted successfully.";
                return RedirectToAction(nameof(Details), new { fipsId, tab = "audits" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting audit record");
                TempData["ErrorMessage"] = "An error occurred while deleting audit record.";
                return RedirectToAction(nameof(Details), new { fipsId, tab = "audits" });
            }
        }

        // POST: Accessibility/AddIssue
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddIssue(int productAccessibilityId, AccessibilityIssue issue, string? wcagCriteriaIds)
        {
            try
            {
                var productAccessibility = await _context.ProductAccessibilities
                    .FirstOrDefaultAsync(pa => pa.Id == productAccessibilityId && !pa.IsDeleted);
                
                if (productAccessibility == null)
                {
                    return NotFound();
                }
                
                issue.ProductAccessibilityId = productAccessibilityId;
                issue.CreatedAt = DateTime.UtcNow;
                issue.CreatedBy = User.Identity?.Name;
                issue.UpdatedAt = DateTime.UtcNow;
                issue.Status = "open";
                
                _context.AccessibilityIssues.Add(issue);
                await _context.SaveChangesAsync();
                
                // Add WCAG criteria links if provided
                if (!string.IsNullOrWhiteSpace(wcagCriteriaIds) && issue.IssueType == "WCAG")
                {
                    var criteriaIds = wcagCriteriaIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(id => int.TryParse(id.Trim(), out var result) ? result : (int?)null)
                        .Where(id => id.HasValue)
                        .Select(id => id.Value)
                        .ToList();
                    
                    foreach (var criterionId in criteriaIds)
                    {
                        var link = new IssueWcagCriterion
                        {
                            AccessibilityIssueId = issue.Id,
                            WcagCriterionId = criterionId,
                            CreatedAt = DateTime.UtcNow
                        };
                        _context.IssueWcagCriteria.Add(link);
                    }
                    await _context.SaveChangesAsync();
                }
                
                // Record creation in history
                var issueTypeDisplay = issue.IssueType == "WCAG" ? $"WCAG {issue.WcagCriteria}" : issue.IssueType;
                var history = new IssueHistory
                {
                    AccessibilityIssueId = issue.Id,
                    FieldChanged = "Created",
                    NewValue = $"{issue.IssueType} issue created",
                    ChangedAt = DateTime.UtcNow,
                    ChangedBy = User.Identity?.Name
                };
                _context.IssueHistories.Add(history);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Accessibility issue added successfully.";
                return RedirectToAction(nameof(Details), new { fipsId = productAccessibility.FipsId, tab = "issues" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding accessibility issue");
                return Json(new { success = false, message = "Error adding issue" });
            }
        }

        // POST: Accessibility/UpdateIssue
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateIssue(int id, AccessibilityIssue updatedIssue, string? changeNote)
        {
            try
            {
                var issue = await _context.AccessibilityIssues
                    .Include(i => i.ProductAccessibility)
                    .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted);
                
                if (issue == null)
                {
                    return NotFound();
                }
                
                // Track changes
                var changes = new List<IssueHistory>();
                
                if (issue.IssueDescription != updatedIssue.IssueDescription)
                {
                    changes.Add(new IssueHistory { 
                        FieldChanged = "Description", 
                        OldValue = issue.IssueDescription ?? "", 
                        NewValue = updatedIssue.IssueDescription ?? "" 
                    });
                    issue.IssueDescription = updatedIssue.IssueDescription;
                }
                
                if (issue.Status != updatedIssue.Status)
                {
                    changes.Add(new IssueHistory { 
                        FieldChanged = "Status", 
                        OldValue = issue.Status, 
                        NewValue = updatedIssue.Status 
                    });
                    issue.Status = updatedIssue.Status;
                }
                
                if (issue.IsResolving != updatedIssue.IsResolving)
                {
                    changes.Add(new IssueHistory { 
                        FieldChanged = "Resolving", 
                        OldValue = issue.IsResolving.ToString(), 
                        NewValue = updatedIssue.IsResolving.ToString() 
                    });
                    issue.IsResolving = updatedIssue.IsResolving;
                }
                
                if (issue.PlannedResolutionDate != updatedIssue.PlannedResolutionDate)
                {
                    changes.Add(new IssueHistory { 
                        FieldChanged = "Planned Resolution Date", 
                        OldValue = issue.PlannedResolutionDate?.ToString("yyyy-MM-dd"), 
                        NewValue = updatedIssue.PlannedResolutionDate?.ToString("yyyy-MM-dd") 
                    });
                    issue.PlannedResolutionDate = updatedIssue.PlannedResolutionDate;
                }
                
                if (issue.NonResolutionReason != updatedIssue.NonResolutionReason)
                {
                    changes.Add(new IssueHistory { 
                        FieldChanged = "Reason Not Resolving", 
                        OldValue = issue.NonResolutionReason ?? "", 
                        NewValue = updatedIssue.NonResolutionReason ?? "" 
                    });
                    issue.NonResolutionReason = updatedIssue.NonResolutionReason;
                }
                
                if (issue.ActualResolutionDate != updatedIssue.ActualResolutionDate)
                {
                    changes.Add(new IssueHistory { 
                        FieldChanged = "Closure Date", 
                        OldValue = issue.ActualResolutionDate?.ToString("yyyy-MM-dd"), 
                        NewValue = updatedIssue.ActualResolutionDate?.ToString("yyyy-MM-dd") 
                    });
                    issue.ActualResolutionDate = updatedIssue.ActualResolutionDate;
                }
                
                if (issue.ResolutionNotes != updatedIssue.ResolutionNotes)
                {
                    changes.Add(new IssueHistory { 
                        FieldChanged = issue.Status == "closed" ? "Closure Explanation" : "Resolution Notes", 
                        OldValue = issue.ResolutionNotes ?? "", 
                        NewValue = updatedIssue.ResolutionNotes ?? "" 
                    });
                    issue.ResolutionNotes = updatedIssue.ResolutionNotes;
                }
                
                if (issue.VerificationNotes != updatedIssue.VerificationNotes)
                {
                    changes.Add(new IssueHistory { 
                        FieldChanged = "Verification Notes", 
                        OldValue = issue.VerificationNotes ?? "", 
                        NewValue = updatedIssue.VerificationNotes ?? "" 
                    });
                    issue.VerificationNotes = updatedIssue.VerificationNotes;
                }
                
                // Update other fields (no history tracking needed for these)
                issue.IssueTitle = updatedIssue.IssueTitle;
                issue.WcagCriteria = updatedIssue.WcagCriteria;
                issue.WcagLevel = updatedIssue.WcagLevel;
                issue.WcagVersion = updatedIssue.WcagVersion;
                issue.IdentifiedDate = updatedIssue.IdentifiedDate;
                issue.IdentifiedVia = updatedIssue.IdentifiedVia;
                issue.UpdatedAt = DateTime.UtcNow;
                issue.UpdatedBy = User.Identity?.Name;
                
                // Add history entries
                foreach (var change in changes)
                {
                    change.AccessibilityIssueId = issue.Id;
                    change.ChangedAt = DateTime.UtcNow;
                    change.ChangedBy = User.Identity?.Name;
                    change.ChangeNote = changeNote;
                    _context.IssueHistories.Add(change);
                }
                
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Issue updated successfully.";
                return RedirectToAction(nameof(ViewIssue), new { id, fipsId = issue.ProductAccessibility.FipsId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating accessibility issue");
                TempData["ErrorMessage"] = "An error occurred while updating the issue.";
                return RedirectToAction(nameof(ViewIssue), new { id });
            }
        }

        // POST: Accessibility/DeleteIssue
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteIssue(int id)
        {
            try
            {
                var issue = await _context.AccessibilityIssues
                    .Include(i => i.ProductAccessibility)
                    .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted);
                
                if (issue == null)
                {
                    return NotFound();
                }
                
                var fipsId = issue.ProductAccessibility.FipsId;
                issue.IsDeleted = true;
                issue.UpdatedAt = DateTime.UtcNow;
                issue.UpdatedBy = User.Identity?.Name;
                
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Issue deleted successfully.";
                return RedirectToAction(nameof(Details), new { fipsId, tab = "issues" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting accessibility issue");
                return Json(new { success = false, message = "Error deleting issue" });
            }
        }

        // POST: Accessibility/CloseIssue
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CloseIssue(int id, DateTime closureDate, string closureExplanation, string? changeNote)
        {
            try
            {
                var issue = await _context.AccessibilityIssues
                    .Include(i => i.ProductAccessibility)
                    .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted);
                
                if (issue == null)
                {
                    return NotFound();
                }
                
                var oldStatus = issue.Status;
                issue.Status = "closed";
                issue.ActualResolutionDate = closureDate;
                issue.ResolutionNotes = closureExplanation;
                issue.UpdatedAt = DateTime.UtcNow;
                issue.UpdatedBy = User.Identity?.Name;
                
                // Add history entry
                var history = new IssueHistory
                {
                    AccessibilityIssueId = issue.Id,
                    FieldChanged = "Status",
                    OldValue = oldStatus,
                    NewValue = "closed",
                    ChangeNote = changeNote,
                    ChangedAt = DateTime.UtcNow,
                    ChangedBy = User.Identity?.Name
                };
                _context.IssueHistories.Add(history);
                
                // Also track closure date change
                var oldActualResolutionDate = issue.ActualResolutionDate;
                if (!oldActualResolutionDate.HasValue || oldActualResolutionDate.Value != closureDate)
                {
                    var dateHistory = new IssueHistory
                    {
                        AccessibilityIssueId = issue.Id,
                        FieldChanged = "Closure Date",
                        OldValue = oldActualResolutionDate?.ToString("yyyy-MM-dd"),
                        NewValue = closureDate.ToString("yyyy-MM-dd"),
                        ChangeNote = null,
                        ChangedAt = DateTime.UtcNow,
                        ChangedBy = User.Identity?.Name
                    };
                    _context.IssueHistories.Add(dateHistory);
                }
                
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Issue closed successfully.";
                return RedirectToAction(nameof(ViewIssue), new { id, fipsId = issue.ProductAccessibility.FipsId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error closing accessibility issue");
                TempData["ErrorMessage"] = "An error occurred while closing the issue.";
                return RedirectToAction(nameof(ViewIssue), new { id });
            }
        }

        // POST: Accessibility/RequestRetest
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestRetest(int issueId, string? requestorEmail, string? requestNotes)
        {
            try
            {
                var issue = await _context.AccessibilityIssues
                    .Include(i => i.ProductAccessibility)
                    .FirstOrDefaultAsync(i => i.Id == issueId && !i.IsDeleted);
                
                if (issue == null)
                {
                    return NotFound();
                }

                // Check if there's already a pending retest request
                var existingRequest = await _context.AccessibilityRetestRequests
                    .FirstOrDefaultAsync(rr => 
                        rr.AccessibilityIssueId == issueId && 
                        rr.IsCompleted == null);

                if (existingRequest != null)
                {
                    TempData["ErrorMessage"] = "There is already a pending retest request for this issue.";
                    return RedirectToAction(nameof(Details), new { fipsId = issue.ProductAccessibility.FipsId, tab = "issues" });
                }

                var retestRequest = new AccessibilityRetestRequest
                {
                    AccessibilityIssueId = issueId,
                    RequestedBy = User.Identity?.Name ?? "Unknown",
                    RequestorEmail = requestorEmail?.Trim(),
                    RequestNotes = requestNotes?.Trim(),
                    RequestedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.AccessibilityRetestRequests.Add(retestRequest);
                await _context.SaveChangesAsync();

                // TODO: Send email notifications to configured admin emails
                // await SendRetestRequestEmails(retestRequest);

                TempData["SuccessMessage"] = "Retest request submitted successfully. An administrator will review your request.";
                return RedirectToAction(nameof(Details), new { fipsId = issue.ProductAccessibility.FipsId, tab = "issues" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating retest request");
                TempData["ErrorMessage"] = "An error occurred while submitting the retest request.";
                return RedirectToAction(nameof(Details), new { fipsId = "unknown", tab = "issues" });
            }
        }

        // POST: Accessibility/AddComment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(int issueId, string commentText, string? fipsId)
        {
            try
            {
                var issue = await _context.AccessibilityIssues
                    .Include(i => i.ProductAccessibility)
                    .FirstOrDefaultAsync(i => i.Id == issueId && !i.IsDeleted);
                
                if (issue == null)
                {
                    return NotFound();
                }
                
                var comment = new IssueComment
                {
                    AccessibilityIssueId = issueId,
                    CommentText = commentText,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = User.Identity?.Name
                };
                
                _context.IssueComments.Add(comment);
                await _context.SaveChangesAsync();
                
                // Use provided fipsId or get from issue
                var redirectFipsId = fipsId ?? issue.ProductAccessibility.FipsId;
                TempData["SuccessMessage"] = "Comment added successfully.";
                return RedirectToAction(nameof(ViewIssue), new { id = issueId, fipsId = redirectFipsId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding comment");
                TempData["ErrorMessage"] = "An error occurred while adding the comment.";
                var redirectFipsId = fipsId ?? "unknown";
                return RedirectToAction(nameof(ViewIssue), new { id = issueId, fipsId = redirectFipsId });
            }
        }

        // POST: Accessibility/UpdateSla
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSla(string fipsId, int slaResponseDays)
        {
            try
            {
                var productAccessibility = await _context.ProductAccessibilities
                    .FirstOrDefaultAsync(pa => pa.FipsId == fipsId && !pa.IsDeleted);
                
                if (productAccessibility == null)
                {
                    return NotFound();
                }
                
                productAccessibility.SlaResponseDays = slaResponseDays;
                productAccessibility.UpdatedAt = DateTime.UtcNow;
                productAccessibility.UpdatedBy = User.Identity?.Name;
                
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "SLA response time updated successfully.";
                return RedirectToAction(nameof(Details), new { fipsId, tab = "settings" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating SLA");
                TempData["ErrorMessage"] = "An error occurred while updating SLA.";
                return RedirectToAction(nameof(Details), new { fipsId, tab = "settings" });
            }
        }

        // POST: Accessibility/UpdateComplaintsEmail
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateComplaintsEmail(string fipsId, string complaintsEmail)
        {
            try
            {
                var productAccessibility = await _context.ProductAccessibilities
                    .FirstOrDefaultAsync(pa => pa.FipsId == fipsId && !pa.IsDeleted);
                
                if (productAccessibility == null)
                {
                    return NotFound();
                }
                
                productAccessibility.ComplaintsEmail = complaintsEmail;
                productAccessibility.UpdatedAt = DateTime.UtcNow;
                productAccessibility.UpdatedBy = User.Identity?.Name;
                
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Complaints email updated successfully.";
                return RedirectToAction(nameof(Details), new { fipsId, tab = "settings" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating complaints email");
                TempData["ErrorMessage"] = "An error occurred while updating complaints email.";
                return RedirectToAction(nameof(Details), new { fipsId, tab = "settings" });
            }
        }

        // POST: Accessibility/UpdateWcagCompliance
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateWcagCompliance(string fipsId, string wcagVersion, string wcagLevel)
        {
            try
            {
                var productAccessibility = await _context.ProductAccessibilities
                    .FirstOrDefaultAsync(pa => pa.FipsId == fipsId && !pa.IsDeleted);
                
                if (productAccessibility == null)
                {
                    return NotFound();
                }
                
                productAccessibility.WcagVersion = wcagVersion;
                productAccessibility.WcagLevel = wcagLevel;
                productAccessibility.UpdatedAt = DateTime.UtcNow;
                productAccessibility.UpdatedBy = User.Identity?.Name;
                
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "WCAG compliance settings updated successfully.";
                return RedirectToAction(nameof(Details), new { fipsId, tab = "settings" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating WCAG compliance");
                TempData["ErrorMessage"] = "An error occurred while updating WCAG compliance.";
                return RedirectToAction(nameof(Details), new { fipsId, tab = "settings" });
            }
        }

        // POST: Accessibility/UpdateLegacyId
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateLegacyId(string fipsId, int? legacyId)
        {
            try
            {
                var productAccessibility = await _context.ProductAccessibilities
                    .FirstOrDefaultAsync(pa => pa.FipsId == fipsId && !pa.IsDeleted);
                
                if (productAccessibility == null)
                {
                    return NotFound();
                }
                
                // If legacyId is provided, check if it's already used by another product
                if (legacyId.HasValue)
                {
                    var existingProduct = await _context.ProductAccessibilities
                        .FirstOrDefaultAsync(pa => pa.LegacyId == legacyId.Value && pa.Id != productAccessibility.Id && !pa.IsDeleted);
                    
                    if (existingProduct != null)
                    {
                        TempData["ErrorMessage"] = $"Legacy ID {legacyId.Value} is already assigned to another product.";
                        return RedirectToAction(nameof(Details), new { fipsId, tab = "settings" });
                    }
                }
                
                productAccessibility.LegacyId = legacyId;
                productAccessibility.UpdatedAt = DateTime.UtcNow;
                productAccessibility.UpdatedBy = User.Identity?.Name;
                
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = legacyId.HasValue 
                    ? $"Legacy ID updated successfully to {legacyId.Value}."
                    : "Legacy ID removed successfully.";
                return RedirectToAction(nameof(Details), new { fipsId, tab = "settings" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating legacy ID");
                TempData["ErrorMessage"] = "An error occurred while updating legacy ID.";
                return RedirectToAction(nameof(Details), new { fipsId, tab = "settings" });
            }
        }

        // POST: Accessibility/VerifyStatement
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyStatement(string fipsId, bool statementInstalled)
        {
            try
            {
                // Check if user is an Accessibility Administrator
                if (!User.IsInRole("AccessibilityAdministrator"))
                {
                    TempData["ErrorMessage"] = "Only Accessibility Administrators can verify statement installation.";
                    return RedirectToAction(nameof(Details), new { fipsId, tab = "statement" });
                }
                
                var productAccessibility = await _context.ProductAccessibilities
                    .FirstOrDefaultAsync(pa => pa.FipsId == fipsId && !pa.IsDeleted);
                
                if (productAccessibility == null)
                {
                    return NotFound();
                }
                
                productAccessibility.StatementInstalled = statementInstalled;
                productAccessibility.VerifiedAt = statementInstalled ? DateTime.UtcNow : null;
                productAccessibility.VerifiedBy = statementInstalled ? User.Identity?.Name : null;
                productAccessibility.UpdatedAt = DateTime.UtcNow;
                productAccessibility.UpdatedBy = User.Identity?.Name;
                
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = statementInstalled 
                    ? "Statement verified as installed successfully." 
                    : "Statement marked as not installed.";
                return RedirectToAction(nameof(Details), new { fipsId, tab = "statement" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying statement");
                TempData["ErrorMessage"] = "An error occurred while updating verification status.";
                return RedirectToAction(nameof(Details), new { fipsId, tab = "statement" });
            }
        }

        // GET: API endpoint to get open issues by FIPS_ID
        // Route: /api/v1/Accessibility/Issues/{fipsId}
        // Requires: API token with "read" permission for "AccessibilityIssues" resource
        [HttpGet("/api/v1/Accessibility/Issues/{fipsId}")]
        public async Task<IActionResult> GetOpenIssues(string fipsId)
        {
            try
            {
                // Check API authentication and permissions (handled by ApiAuthenticationMiddleware)
                var apiToken = HttpContext.Items["ApiToken"] as ApiToken;
                if (apiToken == null)
                {
                    return Unauthorized(new { 
                        error = new { 
                            code = "UNAUTHORIZED", 
                            message = "API authentication required" 
                        } 
                    });
                }
                
                // Check if token has permission to read accessibility issues
                var hasPermission = apiToken.Permissions.Any(p => 
                    p.Resource == "AccessibilityIssues" && p.CanRead);
                
                if (!hasPermission)
                {
                    return StatusCode(403, new { 
                        error = new { 
                            code = "FORBIDDEN", 
                            message = "API token does not have permission to read AccessibilityIssues" 
                        } 
                    });
                }
                
                var productAccessibility = await _context.ProductAccessibilities
                    .Include(pa => pa.Issues.Where(i => !i.IsDeleted && i.Status != "resolved"))
                    .FirstOrDefaultAsync(pa => pa.FipsId == fipsId && !pa.IsDeleted && pa.IsActive);
                
                if (productAccessibility == null)
                {
                    return NotFound(new { 
                        error = new { 
                            code = "NOT_FOUND", 
                            message = "Product not enrolled in accessibility service or not found" 
                        } 
                    });
                }
                
                var issues = productAccessibility.Issues
                    .Select(i => new
                    {
                        id = i.Id,
                        wcagCriteria = i.WcagCriteria,
                        wcagLevel = i.WcagLevel,
                        wcagVersion = i.WcagVersion,
                        identifiedDate = i.IdentifiedDate.ToString("yyyy-MM-dd"),
                        identifiedVia = i.IdentifiedVia,
                        description = i.IssueDescription,
                        isResolving = i.IsResolving,
                        plannedResolutionDate = i.PlannedResolutionDate?.ToString("yyyy-MM-dd"),
                        status = i.Status
                    })
                    .OrderBy(i => i.wcagCriteria)
                    .ToList();
                
                return Json(new
                {
                    fipsId,
                    productName = productAccessibility.ProductName,
                    productPhase = productAccessibility.ProductPhase,
                    issueCount = issues.Count,
                    issues
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving accessibility issues for API");
                return StatusCode(500, new { 
                    error = new { 
                        code = "INTERNAL_ERROR", 
                        message = "An error occurred while retrieving issues" 
                    } 
                });
            }
        }
    }
}

