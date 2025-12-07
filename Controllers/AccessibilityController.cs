using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Compass.Data;
using Compass.Models;
using Compass.Services;
using Compass.ViewModels.Accessibility;

namespace Compass.Controllers
{
    public class AccessibilityController : Controller
    {
        private readonly CompassDbContext _context;
        private readonly ILogger<AccessibilityController> _logger;
        private readonly IProductsApiService _productsApiService;
        private readonly IPermissionService _permissionService;

        public AccessibilityController(
            CompassDbContext context, 
            ILogger<AccessibilityController> logger,
            IProductsApiService productsApiService,
            IPermissionService permissionService)
        {
            _context = context;
            _logger = logger;
            _productsApiService = productsApiService;
            _permissionService = permissionService;
        }

        // Helper method to get documentId from ProductAccessibility (prefer ProductDocumentId, fallback to FipsId)
        private static string GetDocumentId(ProductAccessibility productAccessibility)
        {
            return !string.IsNullOrEmpty(productAccessibility.ProductDocumentId) 
                ? productAccessibility.ProductDocumentId 
                : productAccessibility.FipsId ?? "unknown";
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
                    documentId = p.DocumentId ?? p.FipsId, // Use DocumentId as primary identifier
                    fipsId = p.FipsId, // Keep for backwards compatibility
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
            string? auditType = null,
            List<string>? heatmapTypes = null)
        {
            const int pageSize = 25;
            
            // Load all products with issues and audits for summary calculations
            // Load all issues (not filtered) and filter in code for reliability
            var allProducts = await _context.ProductAccessibilities
                .Include(pa => pa.Issues)
                    .ThenInclude(i => i.WcagCriteriaLinks)
                        .ThenInclude(link => link.WcagCriterion)
                .Include(pa => pa.AuditHistories)
                .Where(pa => !pa.IsDeleted && pa.IsActive)
                .ToListAsync();
            
            // Explicitly load all issues for each product to ensure they're available
            // This is more reliable than relying solely on Include
            foreach (var product in allProducts)
            {
                // Always explicitly load to ensure all issues (including Best Practice) are loaded
                await _context.Entry(product)
                    .Collection(pa => pa.Issues)
                    .Query()
                    .Include(i => i.WcagCriteriaLinks)
                        .ThenInclude(link => link.WcagCriterion)
                    .LoadAsync();
            }

            // Get total products from CMS (for "all-products" tab, we'll get all products regardless of state)
            var cmsProducts = await _productsApiService.GetAllProductsAsync();
            var totalProducts = cmsProducts?.Count ?? 0;

            // Calculate summary statistics
            var totalOpenIssues = allProducts.Sum(p => p.Issues.Count(i => !i.IsDeleted && i.Status != "resolved"));
            var totalEnrolledProducts = allProducts.Count;
            var overdueIssues = allProducts.Sum(p => p.Issues.Count(i => 
                !i.IsDeleted &&
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

            // Normalize tab for backward compatibility
            if (tab == "products") tab = "your-products";
            if (tab == "issues") tab = "your-issues";
            if (tab == "audits") tab = "your-audits";
            
            ViewBag.ActiveTab = tab;

            // Calculate navigation counts for badges (for user-assigned products)
            var currentUserEmail = User.Identity?.Name?.ToLower();
            var userProductFipsIds = new HashSet<string>();
            
            if (!string.IsNullOrEmpty(currentUserEmail))
            {
                // Get user's assigned products from CMS
                var productsByContact = await _productsApiService.GetProductsAsync(currentUserEmail);
                var productsByServiceOwner = await _productsApiService.GetProductsByServiceOwnerAsync(currentUserEmail);
                
                var allUserProducts = productsByContact
                    .Concat(productsByServiceOwner)
                    .GroupBy(p => p.FipsId)
                    .Select(g => g.First())
                    .ToList();
                
                userProductFipsIds = allUserProducts
                    .Where(p => !string.IsNullOrEmpty(p.FipsId))
                    .Select(p => p.FipsId!)
                    .ToHashSet();
            }

            // Calculate "Your" Products count (user-assigned products that are enrolled)
            var yourProductsCount = 0;
            if (userProductFipsIds.Any())
            {
                yourProductsCount = allProducts
                    .Count(pa => userProductFipsIds.Contains(pa.FipsId));
            }

            // Calculate "All" Products count (will be recalculated after we determine enrolled CMS products)
            var allProductsCount = allProducts.Count;

            // Calculate "Your" Issues count (open issues for user-assigned products)
            var yourIssuesCount = 0;
            if (userProductFipsIds.Any())
            {
                yourIssuesCount = allProducts
                    .Where(pa => userProductFipsIds.Contains(pa.FipsId))
                    .Sum(pa => pa.Issues.Count(i => !i.IsDeleted && i.Status != "resolved"));
            }

            // Calculate "All" Issues count (all open issues)
            var allIssuesCount = allProducts
                .Sum(pa => pa.Issues.Count(i => !i.IsDeleted && i.Status != "resolved"));

            // Calculate "Your" Audits count (for user-assigned products)
            var yourAuditsCount = 0;
            if (userProductFipsIds.Any())
            {
                yourAuditsCount = allProducts
                    .Where(pa => userProductFipsIds.Contains(pa.FipsId))
                    .Sum(pa => pa.AuditHistories.Count(ah => !ah.IsDeleted));
            }

            // Calculate "All" Audits count (all audits)
            var allAuditsCount = allProducts
                .Sum(pa => pa.AuditHistories.Count(ah => !ah.IsDeleted));

            ViewBag.YourProductsCount = yourProductsCount;
            ViewBag.AllProductsCount = allProductsCount;
            ViewBag.YourIssuesCount = yourIssuesCount;
            ViewBag.AllIssuesCount = allIssuesCount;
            ViewBag.YourAuditsCount = yourAuditsCount;
            ViewBag.AllAuditsCount = allAuditsCount;

            // Handle Products tabs
            if (tab == "your-products" || tab == "all-products")
            {
                var isYourProducts = tab == "your-products";
                
                // Get products based on tab type
                if (isYourProducts)
                {
                    // For "your-products", get user's assigned products
                    var userAssignedProducts = new List<ProductDto>();
                    if (!string.IsNullOrEmpty(currentUserEmail))
                    {
                        // Get products where user is a contact (via product_contacts)
                        var productsByContact = await _productsApiService.GetProductsAsync(currentUserEmail);
                        
                        // Get products where user is a service owner
                        var productsByServiceOwner = await _productsApiService.GetProductsByServiceOwnerAsync(currentUserEmail);
                        
                        // Combine and deduplicate by FipsId
                        var allUserProducts = productsByContact
                            .Concat(productsByServiceOwner)
                            .GroupBy(p => p.FipsId)
                            .Select(g => g.First())
                            .ToList();
                        
                        userAssignedProducts = allUserProducts;
                    }
                    
                    cmsProducts = userAssignedProducts;
                }
                else
                {
                    // For "all-products", get all products regardless of state
                    if (cmsProducts == null)
                    {
                        cmsProducts = await _productsApiService.GetAllProductsAsync();
                    }
                }
                
                // Create a dictionary for quick lookup of enrolled products
                var enrolledDict = allProducts.ToDictionary(pa => pa.FipsId, pa => pa);

                // Create view model lists
                var myProductViewModels = new List<dynamic>();
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
                        // Query issues directly from database to ensure we get all issues (including Best Practice)
                        // This is more reliable than using the navigation property
                        openIssuesCount = await _context.AccessibilityIssues
                            .CountAsync(i => i.ProductAccessibilityId == enrolled.Id && 
                                           i.Status != "resolved" && 
                                           !i.IsDeleted);
                        pastDueCount = await _context.AccessibilityIssues
                            .CountAsync(i => i.ProductAccessibilityId == enrolled.Id &&
                                           !i.IsDeleted && 
                                           i.Status != "resolved" && 
                                           i.PlannedResolutionDate.HasValue &&
                                           i.PlannedResolutionDate.Value < DateTime.UtcNow.Date);
                        isVerified = enrolled.StatementInstalled && enrolled.VerifiedAt.HasValue;
                        
                        // Calculate compliance status with WCAG criteria percentage
                        // Query issues directly from database to ensure we get all issues (including Best Practice)
                        var openIssues = await _context.AccessibilityIssues
                            .Include(i => i.WcagCriteriaLinks)
                                .ThenInclude(link => link.WcagCriterion)
                            .Where(i => i.ProductAccessibilityId == enrolled.Id && 
                                       i.Status != "resolved" && 
                                       !i.IsDeleted)
                            .ToListAsync();
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
                        DocumentId = cmsProduct.DocumentId ?? cmsProduct.FipsId, // Use DocumentId as primary identifier
                        FipsId = cmsProduct.FipsId, // Keep for backwards compatibility
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

                    // Check if this product is assigned to the current user
                    // For "your-products" tab, cmsProducts is already filtered to user-assigned products
                    // For "all-products" tab, we need to check if the product is in the user's assigned list
                    var isMyProduct = isYourProducts || 
                        (!string.IsNullOrEmpty(currentUserEmail) && 
                         userProductFipsIds.Contains(cmsProduct.FipsId ?? string.Empty));

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
                            
                            // Add to "My Products" if user is responsible
                            if (isMyProduct)
                            {
                                myProductViewModels.Add(productViewModel);
                            }
                        }
                    }
                    else
                    {
                        // Non-enrolled products - no filtering needed
                        allProductViewModels.Add(productViewModel);
                    }
                }

                // Sort "My Products"
                myProductViewModels = myProductViewModels
                    .OrderBy(p => ((dynamic)p).ProductName)
                    .ToList();
                
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

                // Calculate user-specific statistics
                var myProductsOpenIssues = myProductViewModels.Sum(p => ((dynamic)p).OpenIssuesCount);
                var myProductsOverdueIssues = myProductViewModels.Sum(p => ((dynamic)p).PastDueCount);
                var myProductsCompliant = myProductViewModels.Count(p => ((dynamic)p).ComplianceStatus == "compliant");
                var myProductsPartiallyCompliant = myProductViewModels.Count(p => ((dynamic)p).ComplianceStatus == "partially compliant");
                var myProductsNonCompliant = myProductViewModels.Count(p => ((dynamic)p).ComplianceStatus == "non-compliant");
                var myProductsVerified = myProductViewModels.Count(p => ((dynamic)p).IsVerified);
                var myProductsTotal = myProductViewModels.Count;

                ViewBag.MyProducts = myProductViewModels;
                ViewBag.EnrolledProducts = enrolledProductViewModels;
                ViewBag.NonEnrolledProducts = pagedNonEnrolledProducts;
                ViewBag.ProductsPage = productsPage;
                ViewBag.ProductsTotalPages = totalPages;
                ViewBag.ProductsTotalCount = totalCount;
                
                // User-specific statistics
                ViewBag.MyProductsOpenIssues = myProductsOpenIssues;
                ViewBag.MyProductsOverdueIssues = myProductsOverdueIssues;
                ViewBag.MyProductsCompliant = myProductsCompliant;
                ViewBag.MyProductsPartiallyCompliant = myProductsPartiallyCompliant;
                ViewBag.MyProductsNonCompliant = myProductsNonCompliant;
                ViewBag.MyProductsVerified = myProductsVerified;
                ViewBag.MyProductsTotal = myProductsTotal;
                
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
                
                // Recalculate "All" Products count for "all-products" tab to match what's displayed
                // This ensures the badge count matches the actual enrolled CMS products shown
                if (tab == "all-products")
                {
                    allProductsCount = enrolledProductViewModels.Count;
                    ViewBag.AllProductsCount = allProductsCount;
                }
                
                return View("~/Views/Apps/Accessibility/Index.cshtml", enrolledProductAccessibilities);
            }

            // Handle Issues tabs
            if (tab == "your-issues" || tab == "all-issues")
            {
                var isYourIssues = tab == "your-issues";
                
                var issuesQuery = _context.AccessibilityIssues
                    .Include(i => i.ProductAccessibility)
                    .Include(i => i.WcagCriteriaLinks)
                        .ThenInclude(link => link.WcagCriterion)
                    .Where(i => !i.IsDeleted)
                    .AsQueryable();
                
                // Filter based on tab type
                if (isYourIssues)
                {
                    // Filter to only issues for user-assigned products
                    // If user has no assigned products, show no issues
                    if (userProductFipsIds.Any())
                    {
                        issuesQuery = issuesQuery.Where(i => 
                            i.ProductAccessibility != null && 
                            !string.IsNullOrEmpty(i.ProductAccessibility.FipsId) &&
                            userProductFipsIds.Contains(i.ProductAccessibility.FipsId));
                    }
                    else
                    {
                        // User has no assigned products, so return empty query
                        issuesQuery = issuesQuery.Where(i => false);
                    }
                }
                // For "all-issues", no additional filtering needed - show all issues

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

            // Handle Audits tabs
            if (tab == "your-audits" || tab == "all-audits")
            {
                var isYourAudits = tab == "your-audits";
                
                var auditsQuery = _context.AuditHistories
                    .Include(ah => ah.ProductAccessibility)
                    .Where(ah => !ah.IsDeleted)
                    .AsQueryable();

                // Filter based on tab type
                if (isYourAudits)
                {
                    // Filter to only audits for user-assigned products
                    // If user has no assigned products, show no audits
                    if (userProductFipsIds.Any())
                    {
                        auditsQuery = auditsQuery.Where(ah => 
                            ah.ProductAccessibility != null && 
                            !string.IsNullOrEmpty(ah.ProductAccessibility.FipsId) &&
                            userProductFipsIds.Contains(ah.ProductAccessibility.FipsId));
                    }
                    else
                    {
                        // User has no assigned products, so return empty query
                        auditsQuery = auditsQuery.Where(ah => false);
                    }
                }
                // For "all-audits", no additional filtering needed - show all audits

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

            if (tab == "heatmap")
            {
                var heatmapTypeConfigs = new (string Key, string[] Keywords, string Description)[]
                {
                    ("Color", new[] { "color", "colour", "contrast" }, "Colour contrast and use of colour"),
                    ("Links", new[] { "link", "hyperlink", "focus", "target" }, "Link identification and focus states"),
                    ("Forms", new[] { "form", "input", "label", "error", "name", "value" }, "Form labels, instructions and errors"),
                    ("Navigation", new[] { "navigate", "navigation", "bypass", "sequence", "multiple ways", "heading", "focus order" }, "Navigation, multiple ways and focus order"),
                    ("Text", new[] { "text", "read", "reading", "resize", "line", "paragraph" }, "Readable text, resizing and spacing"),
                    ("Audio & Video", new[] { "audio", "video", "media", "caption", "transcript", "sign", "time-based" }, "Audio, video and time-based media"),
                    ("Keyboard", new[] { "keyboard", "trap", "focus", "shortcut" }, "Keyboard access and traps")
                };

                var heatmapTypeDefinitions = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
                var heatmapTypeDescriptions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var heatmapTypeCanonicalKeys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                foreach (var config in heatmapTypeConfigs)
                {
                    heatmapTypeDefinitions[config.Key] = config.Keywords;
                    heatmapTypeDescriptions[config.Key] = config.Description;
                    heatmapTypeCanonicalKeys[config.Key] = config.Key;
                }

                var selectedHeatmapTypeSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                if (heatmapTypes != null && heatmapTypes.Count > 0)
                {
                    foreach (var requested in heatmapTypes)
                    {
                        if (string.IsNullOrWhiteSpace(requested))
                        {
                            continue;
                        }

                        if (heatmapTypeCanonicalKeys.TryGetValue(requested, out var canonical))
                        {
                            selectedHeatmapTypeSet.Add(canonical);
                        }
                    }
                }

                var activeCriteria = await _context.WcagCriteria
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.Version)
                    .ThenBy(c => c.SortOrder)
                    .ThenBy(c => c.Criterion)
                    .ToListAsync();

                var linkCounts = await _context.IssueWcagCriteria
                    .Where(link => !link.AccessibilityIssue.IsDeleted)
                    .GroupBy(link => link.WcagCriterionId)
                    .Select(group => new
                    {
                        CriterionId = group.Key,
                        Count = group.Count()
                    })
                    .ToListAsync();

                var heatmapRows = activeCriteria
                    .Select(c => new WcagHeatmapRow
                    {
                        CriterionId = c.Id,
                        Criterion = c.Criterion,
                        Title = c.Title,
                        Level = c.Level,
                        Version = c.Version,
                        IssueCount = linkCounts.FirstOrDefault(lc => lc.CriterionId == c.Id)?.Count ?? 0,
                        Principle = c.Criterion.Split('.', 2)[0]
                    })
                    .ToList();

                var rowLookup = new Dictionary<string, WcagHeatmapRow>(StringComparer.OrdinalIgnoreCase);
                foreach (var row in heatmapRows)
                {
                    if (!rowLookup.ContainsKey(row.Criterion))
                    {
                        rowLookup[row.Criterion] = row;
                    }
                }

                var issuesWithLegacyCriteria = await _context.AccessibilityIssues
                    .Where(i => !i.IsDeleted && i.IssueType == "WCAG" && !i.WcagCriteriaLinks.Any() && !string.IsNullOrWhiteSpace(i.WcagCriteria))
                    .Select(i => i.WcagCriteria!)
                    .ToListAsync();

                var unmatchedCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var raw in issuesWithLegacyCriteria)
                {
                    foreach (var code in ExtractCriterionCodes(raw))
                    {
                        if (rowLookup.TryGetValue(code, out var row))
                        {
                            row.IssueCount += 1;
                        }
                        else
                        {
                            unmatchedCodes.Add(code);
                        }
                    }
                }

                bool MatchesType(WcagHeatmapRow row, string typeKey)
                {
                    if (!heatmapTypeDefinitions.TryGetValue(typeKey, out var keywords))
                    {
                        return false;
                    }

                    foreach (var keyword in keywords)
                    {
                        if (row.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                            row.Criterion.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }

                    return false;
                }

                if (selectedHeatmapTypeSet.Any())
                {
                    heatmapRows = heatmapRows
                        .Where(row => selectedHeatmapTypeSet.Any(typeKey => MatchesType(row, typeKey)))
                        .ToList();
                }

                var maxCount = heatmapRows.Any() ? heatmapRows.Max(r => r.IssueCount) : 0;
                var totalIssuesMapped = heatmapRows.Sum(r => r.IssueCount);
                var criteriaWithIssues = heatmapRows.Count(r => r.IssueCount > 0);
                var levelOrder = new[] { "A", "AA", "AAA" };
                var levelDistribution = levelOrder
                    .Select(level => new KeyValuePair<string, int>(
                        level,
                        heatmapRows
                            .Where(r => string.Equals(r.Level, level, StringComparison.OrdinalIgnoreCase))
                            .Sum(r => r.IssueCount)))
                    .ToList();
                var knownLevelTotal = levelDistribution.Sum(kvp => kvp.Value);
                var otherLevelCount = totalIssuesMapped - knownLevelTotal;
                if (otherLevelCount > 0)
                {
                    levelDistribution.Add(new KeyValuePair<string, int>("Other", otherLevelCount));
                }

                var topCriteria = heatmapRows
                    .Where(r => r.IssueCount > 0)
                    .OrderByDescending(r => r.IssueCount)
                    .ThenBy(r => r.Criterion, StringComparer.OrdinalIgnoreCase)
                    .Take(5)
                    .Select(r => Tuple.Create(r.Criterion, r.Title, r.IssueCount, r.Level))
                    .ToList();
 
                ViewBag.WcagHeatmap = heatmapRows;
                ViewBag.HeatmapMaxCount = maxCount;
                ViewBag.HeatmapTotalIssues = totalIssuesMapped;
                ViewBag.HeatmapCriteriaWithIssues = criteriaWithIssues;
                ViewBag.HeatmapUnmatchedCodes = unmatchedCodes.ToList();
                ViewBag.HeatmapLevelDistribution = levelDistribution;
                ViewBag.HeatmapLevelTotal = totalIssuesMapped;
                ViewBag.HeatmapTopCriteria = topCriteria;
                ViewBag.HeatmapTypeOptions = heatmapTypeConfigs.Select(config => config.Key).ToList();
                ViewBag.HeatmapTypeDescriptions = heatmapTypeDescriptions;
                ViewBag.SelectedHeatmapTypes = selectedHeatmapTypeSet.ToList();

                return View("~/Views/Apps/Accessibility/Index.cshtml", allProducts);
            }
 
            // Default to your-products tab
            ViewBag.ActiveTab = "your-products";
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

        // GET: Accessibility/Details/{documentId}?tab=overview
        // Supports both DocumentId (primary) and FipsId (legacy) for backwards compatibility
        public async Task<IActionResult> Details(string documentId, string tab = "overview")
        {
            // Get CMS product info first - try to find by DocumentId (primary) or FipsId (legacy)
            var cmsProducts = await _productsApiService.GetProductsAsync();
            var productInfo = cmsProducts?.FirstOrDefault(p => 
                p.DocumentId == documentId || p.FipsId == documentId);
            
            if (productInfo == null)
            {
                return NotFound();
            }
            
            // Use DocumentId for database lookup (primary identifier)
            var productDocumentId = productInfo.DocumentId ?? documentId;
            var productAccessibility = await _context.ProductAccessibilities
                .Include(pa => pa.ContactMethods.Where(cm => cm.IsActive))
                .Include(pa => pa.AuditHistories.Where(ah => !ah.IsDeleted))
                .FirstOrDefaultAsync(pa => 
                    (pa.ProductDocumentId == productDocumentId || (string.IsNullOrEmpty(pa.ProductDocumentId) && pa.FipsId == documentId)) 
                    && !pa.IsDeleted);
            
            // If product not enrolled, show enrollment option
            if (productAccessibility == null)
            {
                ViewBag.CmsProduct = productInfo;
                ViewBag.IsNotEnrolled = true;
                ViewBag.CurrentTab = tab;
                return View("~/Views/Apps/Accessibility/Details.cshtml", (ProductAccessibility?)null);
            }
            
            // Explicitly load issues with all related data (load all, filter in memory for reliability)
            // Query issues directly to ensure they're loaded correctly
            var issues = await _context.AccessibilityIssues
                .Include(i => i.Comments.Where(c => !c.IsDeleted))
                .Include(i => i.History)
                .Include(i => i.WcagCriteriaLinks)
                    .ThenInclude(link => link.WcagCriterion)
                .Where(i => i.ProductAccessibilityId == productAccessibility.Id)
                .ToListAsync();
            
            // Manually populate the Issues collection to ensure it's available
            // Clear existing collection and add loaded issues
            productAccessibility.Issues.Clear();
            foreach (var issue in issues)
            {
                productAccessibility.Issues.Add(issue);
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
            
            // Check if user is in Design Operations group for bulk edit functionality
            var userEmail = User.Identity?.Name ?? string.Empty;
            var isDesignOps = !string.IsNullOrEmpty(userEmail) && 
                            await _permissionService.IsInGroupAsync(userEmail, "Design Operations");
            ViewBag.IsDesignOps = isDesignOps;
            
            return View("~/Views/Apps/Accessibility/Details.cshtml", productAccessibility);
        }

        // GET: Accessibility/ViewIssue/{id}?documentId={documentId}
        // Supports both DocumentId (primary) and FipsId (legacy) for backwards compatibility
        public async Task<IActionResult> ViewIssue(int id, string documentId)
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
            
            // Verify the issue belongs to the correct product (check by DocumentId or FipsId)
            var productDocumentId = issue.ProductAccessibility.ProductDocumentId ?? issue.ProductAccessibility.FipsId;
            if (productDocumentId != documentId && issue.ProductAccessibility.FipsId != documentId)
            {
                return NotFound();
            }
            
            // Get CMS product info for sidebar
            var cmsProducts = await _productsApiService.GetProductsAsync();
            var cmsProduct = cmsProducts?.FirstOrDefault(p => 
                p.DocumentId == documentId || p.FipsId == documentId);
            
            ViewBag.DocumentId = documentId;
            ViewBag.FipsId = issue.ProductAccessibility.FipsId; // Keep for backwards compatibility
            ViewBag.ProductName = issue.ProductAccessibility.ProductName;
            ViewBag.CmsProduct = cmsProduct;
            return View("~/Views/Apps/Accessibility/IssueDetails.cshtml", issue);
        }

        // GET: Accessibility/Enroll
        // Supports both DocumentId (primary) and FipsId (legacy) for backwards compatibility
        public async Task<IActionResult> Enroll(string? documentId)
        {
            ViewBag.DocumentId = documentId;
            ViewBag.FipsId = documentId; // Keep for backwards compatibility
            
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
                // Get product info from CMS first to get DocumentId
                // Support both documentId (from form) and fipsId (legacy)
                var documentIdFromForm = Request.Form["documentId"].ToString();
                var fipsIdFromForm = productAccessibility.FipsId ?? Request.Form["fipsId"].ToString();
                
                var cmsProducts = await _productsApiService.GetProductsAsync();
                var productInfo = cmsProducts?.FirstOrDefault(p => 
                    (!string.IsNullOrEmpty(documentIdFromForm) && (p.DocumentId == documentIdFromForm || p.FipsId == documentIdFromForm)) ||
                    (!string.IsNullOrEmpty(fipsIdFromForm) && (p.FipsId == fipsIdFromForm || p.DocumentId == fipsIdFromForm)));
                
                if (productInfo == null)
                {
                    TempData["ErrorMessage"] = "Product not found in CMS.";
                    return View(productAccessibility);
                }
                
                // Set ProductDocumentId (primary identifier)
                if (string.IsNullOrEmpty(productInfo.DocumentId))
                {
                    TempData["ErrorMessage"] = "Product DocumentId is required but not found in CMS.";
                    return View(productAccessibility);
                }
                productAccessibility.ProductDocumentId = productInfo.DocumentId;
                
                // Check if product already enrolled (by DocumentId or FipsId for backwards compatibility)
                var existing = await _context.ProductAccessibilities
                    .FirstOrDefaultAsync(pa => 
                        (pa.ProductDocumentId == productAccessibility.ProductDocumentId || 
                         (string.IsNullOrEmpty(pa.ProductDocumentId) && pa.FipsId == productAccessibility.FipsId)) 
                        && !pa.IsDeleted);
                
                if (existing != null)
                {
                    TempData["ErrorMessage"] = "This product is already enrolled in the accessibility management service.";
                    return RedirectToAction(nameof(Details), new { documentId = productAccessibility.ProductDocumentId ?? productAccessibility.FipsId });
                }
                
                if (productInfo != null)
                {
                    productAccessibility.ProductName = productInfo.Title;
                    productAccessibility.ProductPhase = productInfo.Phase;
                    // Ensure FipsId is set for backwards compatibility
                    if (string.IsNullOrEmpty(productAccessibility.FipsId) && !string.IsNullOrEmpty(productInfo.FipsId))
                    {
                        productAccessibility.FipsId = productInfo.FipsId;
                    }
                }
                
                productAccessibility.EnrolledAt = DateTime.UtcNow;
                productAccessibility.EnrolledBy = User.Identity?.Name;
                productAccessibility.UpdatedAt = DateTime.UtcNow;
                
                // Clear the navigation property to prevent duplicate contact methods
                // The model binder populates both the navigation property and the separate parameter
                productAccessibility.ContactMethods = new List<ContactMethod>();
                
                _context.ProductAccessibilities.Add(productAccessibility);
                await _context.SaveChangesAsync();
                
                // Add contact methods from the parameter
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
                return RedirectToAction(nameof(Details), new { documentId = productAccessibility.ProductDocumentId ?? productAccessibility.FipsId });
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
        // Supports both DocumentId (primary) and FipsId (legacy) for backwards compatibility
        public async Task<IActionResult> UpdateSettings(string documentId, int slaResponseDays, string complaintsEmail)
        {
            ProductAccessibility? productAccessibility = null;
            try
            {
                // Try to find by DocumentId first, then FipsId for backwards compatibility
                productAccessibility = await _context.ProductAccessibilities
                    .FirstOrDefaultAsync(pa => 
                        (pa.ProductDocumentId == documentId || (string.IsNullOrEmpty(pa.ProductDocumentId) && pa.FipsId == documentId)) 
                        && !pa.IsDeleted);
                
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
            
            var productDocId = productAccessibility?.ProductDocumentId ?? documentId;
            return RedirectToAction(nameof(Details), new { documentId = productDocId, tab = "settings" });
        }

        // POST: Accessibility/AddContactMethod
        [HttpPost]
        [ValidateAntiForgeryToken]
        // Supports both DocumentId (primary) and FipsId (legacy) for backwards compatibility
        public async Task<IActionResult> AddContactMethod(int productAccessibilityId, string documentId, string type, string detail, string? description, int sortOrder)
        {
            ProductAccessibility? productAccessibility = null;
            try
            {
                productAccessibility = await _context.ProductAccessibilities
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
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding contact method");
                TempData["ErrorMessage"] = "An error occurred while adding contact method.";
                if (productAccessibility == null)
                {
                    productAccessibility = await _context.ProductAccessibilities
                        .FirstOrDefaultAsync(pa => pa.Id == productAccessibilityId && !pa.IsDeleted);
                }
            }
            
            var productDocId = productAccessibility?.ProductDocumentId ?? documentId;
            return RedirectToAction(nameof(Details), new { documentId = productDocId, tab = "settings" });
        }

        // POST: Accessibility/DeleteContactMethod
        [HttpPost]
        [ValidateAntiForgeryToken]
        // Supports both DocumentId (primary) and FipsId (legacy) for backwards compatibility
        public async Task<IActionResult> DeleteContactMethod(int id, string documentId)
        {
            ProductAccessibility? productAccessibility = null;
            try
            {
                var contactMethod = await _context.ContactMethods
                    .Include(cm => cm.ProductAccessibility)
                    .FirstOrDefaultAsync(cm => cm.Id == id);
                
                if (contactMethod == null)
                {
                    return NotFound();
                }
                
                productAccessibility = contactMethod.ProductAccessibility;
                _context.ContactMethods.Remove(contactMethod);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Contact method deleted successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting contact method");
                TempData["ErrorMessage"] = "An error occurred while deleting contact method.";
                if (productAccessibility == null)
                {
                    var contactMethod = await _context.ContactMethods
                        .Include(cm => cm.ProductAccessibility)
                        .FirstOrDefaultAsync(cm => cm.Id == id);
                    productAccessibility = contactMethod?.ProductAccessibility;
                }
            }
            
            var productDocId = productAccessibility?.ProductDocumentId ?? documentId;
            return RedirectToAction(nameof(Details), new { documentId = productDocId, tab = "settings" });
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
                return RedirectToAction(nameof(Details), new { documentId = GetDocumentId(productAccessibility), tab = "audits" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding audit record");
                var productAccessibility = await _context.ProductAccessibilities
                    .FirstOrDefaultAsync(pa => pa.Id == productAccessibilityId && !pa.IsDeleted);
                if (productAccessibility == null)
                {
                    TempData["ErrorMessage"] = "An error occurred while adding audit record. Product not found.";
                    return RedirectToAction(nameof(Index));
                }
                TempData["ErrorMessage"] = "An error occurred while adding audit record.";
                return RedirectToAction(nameof(Details), new { documentId = GetDocumentId(productAccessibility), tab = "audits" });
            }
        }

        // POST: Accessibility/DeleteAudit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAudit(int id)
        {
            try
            {
                var audit = await _context.AuditHistories
                    .Include(ah => ah.ProductAccessibility)
                    .FirstOrDefaultAsync(ah => ah.Id == id);
                
                if (audit == null)
                {
                    return NotFound();
                }
                
                var productAccessibility = audit.ProductAccessibility;
                audit.IsDeleted = true;
                audit.UpdatedAt = DateTime.UtcNow;
                audit.UpdatedBy = User.Identity?.Name;
                
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Audit record deleted successfully.";
                return RedirectToAction(nameof(Details), new { documentId = GetDocumentId(productAccessibility), tab = "audits" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting audit record");
                // Try to get productAccessibility from the audit if it wasn't loaded
                var audit = await _context.AuditHistories
                    .Include(ah => ah.ProductAccessibility)
                    .FirstOrDefaultAsync(ah => ah.Id == id);
                if (audit?.ProductAccessibility == null)
                {
                    TempData["ErrorMessage"] = "An error occurred while deleting audit record. Product not found.";
                    return RedirectToAction(nameof(Index));
                }
                TempData["ErrorMessage"] = "An error occurred while deleting audit record.";
                return RedirectToAction(nameof(Details), new { documentId = GetDocumentId(audit.ProductAccessibility), tab = "audits" });
            }
        }

        // POST: Accessibility/AddIssue
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddIssue(int productAccessibilityId, AccessibilityIssue issue, string? wcagCriteriaIds, string? issueTitle)
        {
            try
            {
                var productAccessibility = await _context.ProductAccessibilities
                    .FirstOrDefaultAsync(pa => pa.Id == productAccessibilityId && !pa.IsDeleted);
                
                if (productAccessibility == null)
                {
                    return NotFound();
                }

                // Note: IssueDescription now supports unlimited length (nvarchar(MAX))
                // No length validation needed
                
                // Explicitly set IssueType from form data (handles model binding issues)
                // Check both "issue.IssueType" (with prefix) and "issueType" (without prefix)
                if (Request.Form.ContainsKey("issue.IssueType") && !string.IsNullOrWhiteSpace(Request.Form["issue.IssueType"]))
                {
                    issue.IssueType = Request.Form["issue.IssueType"].ToString().Trim();
                }
                else if (Request.Form.ContainsKey("issueType") && !string.IsNullOrWhiteSpace(Request.Form["issueType"]))
                {
                    issue.IssueType = Request.Form["issueType"].ToString().Trim();
                }
                // If model binding worked, issue.IssueType should already be set, but ensure it has a default
                if (string.IsNullOrWhiteSpace(issue.IssueType))
                {
                    issue.IssueType = "WCAG"; // Default fallback
                }
                
                // Explicitly set IssueTitle from form data (handles model binding issues)
                // Try from explicit parameter first, then from Request.Form, then from model binding
                if (!string.IsNullOrWhiteSpace(issueTitle))
                {
                    issue.IssueTitle = issueTitle.Trim();
                }
                else if (Request.Form.ContainsKey("issueTitle") && !string.IsNullOrWhiteSpace(Request.Form["issueTitle"]))
                {
                    issue.IssueTitle = Request.Form["issueTitle"].ToString().Trim();
                }
                // If model binding worked, issue.IssueTitle should already be set
                
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
                return RedirectToAction(nameof(Details), new { documentId = GetDocumentId(productAccessibility), tab = "issues" });
            }
            catch (DbUpdateException dbEx) when (dbEx.InnerException is SqlException sqlEx && sqlEx.Message.Contains("String or binary data would be truncated"))
            {
                _logger.LogError(dbEx, "Error adding accessibility issue - data truncation");
                var productAccessibility = await _context.ProductAccessibilities
                    .FirstOrDefaultAsync(pa => pa.Id == productAccessibilityId && !pa.IsDeleted);
                if (productAccessibility == null)
                {
                    TempData["ErrorMessage"] = "An error occurred while saving the issue. Product not found.";
                    return RedirectToAction(nameof(Index));
                }
                TempData["ErrorMessage"] = "An error occurred while saving the issue. Please check that all fields are within their limits and try again.";
                return RedirectToAction(nameof(Details), new { documentId = GetDocumentId(productAccessibility), tab = "issues" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding accessibility issue");
                var productAccessibility = await _context.ProductAccessibilities
                    .FirstOrDefaultAsync(pa => pa.Id == productAccessibilityId && !pa.IsDeleted);
                if (productAccessibility == null)
                {
                    TempData["ErrorMessage"] = "An error occurred while adding the issue. Product not found.";
                    return RedirectToAction(nameof(Index));
                }
                TempData["ErrorMessage"] = "An error occurred while adding the issue. Please try again.";
                return RedirectToAction(nameof(Details), new { documentId = GetDocumentId(productAccessibility), tab = "issues" });
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
                
                // Only update fields that are actually provided in the form to avoid wiping out data
                // Check Request.Form to see which fields were submitted
                
                if (Request.Form.ContainsKey("issueDescription"))
                {
                    if (issue.IssueDescription != updatedIssue.IssueDescription)
                    {
                        changes.Add(new IssueHistory { 
                            FieldChanged = "Description", 
                            OldValue = issue.IssueDescription ?? "", 
                            NewValue = updatedIssue.IssueDescription ?? "" 
                        });
                        issue.IssueDescription = updatedIssue.IssueDescription;
                    }
                }
                
                if (Request.Form.ContainsKey("status"))
                {
                    if (issue.Status != updatedIssue.Status)
                    {
                        changes.Add(new IssueHistory { 
                            FieldChanged = "Status", 
                            OldValue = issue.Status, 
                            NewValue = updatedIssue.Status 
                        });
                        issue.Status = updatedIssue.Status;
                    }
                }
                
                if (Request.Form.ContainsKey("isResolving"))
                {
                    if (issue.IsResolving != updatedIssue.IsResolving)
                    {
                        changes.Add(new IssueHistory { 
                            FieldChanged = "Resolving", 
                            OldValue = issue.IsResolving.ToString(), 
                            NewValue = updatedIssue.IsResolving.ToString() 
                        });
                        issue.IsResolving = updatedIssue.IsResolving;
                    }
                }
                
                if (Request.Form.ContainsKey("plannedResolutionDate"))
                {
                    if (issue.PlannedResolutionDate != updatedIssue.PlannedResolutionDate)
                    {
                        changes.Add(new IssueHistory { 
                            FieldChanged = "Planned Resolution Date", 
                            OldValue = issue.PlannedResolutionDate?.ToString("yyyy-MM-dd"), 
                            NewValue = updatedIssue.PlannedResolutionDate?.ToString("yyyy-MM-dd") 
                        });
                        issue.PlannedResolutionDate = updatedIssue.PlannedResolutionDate;
                    }
                }
                
                if (Request.Form.ContainsKey("nonResolutionReason"))
                {
                    if (issue.NonResolutionReason != updatedIssue.NonResolutionReason)
                    {
                        changes.Add(new IssueHistory { 
                            FieldChanged = "Reason Not Resolving", 
                            OldValue = issue.NonResolutionReason ?? "", 
                            NewValue = updatedIssue.NonResolutionReason ?? "" 
                        });
                        issue.NonResolutionReason = updatedIssue.NonResolutionReason;
                    }
                }
                
                if (Request.Form.ContainsKey("actualResolutionDate"))
                {
                    if (issue.ActualResolutionDate != updatedIssue.ActualResolutionDate)
                    {
                        changes.Add(new IssueHistory { 
                            FieldChanged = "Closure Date", 
                            OldValue = issue.ActualResolutionDate?.ToString("yyyy-MM-dd"), 
                            NewValue = updatedIssue.ActualResolutionDate?.ToString("yyyy-MM-dd") 
                        });
                        issue.ActualResolutionDate = updatedIssue.ActualResolutionDate;
                    }
                }
                
                if (Request.Form.ContainsKey("resolutionNotes"))
                {
                    if (issue.ResolutionNotes != updatedIssue.ResolutionNotes)
                    {
                        changes.Add(new IssueHistory { 
                            FieldChanged = issue.Status == "closed" ? "Closure Explanation" : "Resolution Notes", 
                            OldValue = issue.ResolutionNotes ?? "", 
                            NewValue = updatedIssue.ResolutionNotes ?? "" 
                        });
                        issue.ResolutionNotes = updatedIssue.ResolutionNotes;
                    }
                }
                
                if (Request.Form.ContainsKey("verificationNotes"))
                {
                    if (issue.VerificationNotes != updatedIssue.VerificationNotes)
                    {
                        changes.Add(new IssueHistory { 
                            FieldChanged = "Verification Notes", 
                            OldValue = issue.VerificationNotes ?? "", 
                            NewValue = updatedIssue.VerificationNotes ?? "" 
                        });
                        issue.VerificationNotes = updatedIssue.VerificationNotes;
                    }
                }
                
                if (Request.Form.ContainsKey("IssueTitle") || Request.Form.ContainsKey("issueTitle"))
                {
                    var newTitle = Request.Form.ContainsKey("IssueTitle") ? Request.Form["IssueTitle"].ToString() : Request.Form["issueTitle"].ToString();
                    if (issue.IssueTitle != newTitle)
                    {
                        changes.Add(new IssueHistory { 
                            FieldChanged = "Issue Title", 
                            OldValue = issue.IssueTitle ?? "", 
                            NewValue = newTitle ?? "" 
                        });
                        issue.IssueTitle = newTitle;
                    }
                }
                
                // Update other fields only if they're provided in the form (to avoid wiping out data when only updating specific fields)
                // These fields don't have history tracking, so only update if provided and different
                if (Request.Form.ContainsKey("identifiedDate") && !string.IsNullOrWhiteSpace(Request.Form["identifiedDate"]))
                {
                    if (DateTime.TryParse(Request.Form["identifiedDate"], out var identifiedDate) && issue.IdentifiedDate != identifiedDate)
                    {
                        issue.IdentifiedDate = identifiedDate;
                    }
                }
                
                if (Request.Form.ContainsKey("identifiedVia") && !string.IsNullOrWhiteSpace(Request.Form["identifiedVia"]))
                {
                    if (issue.IdentifiedVia != updatedIssue.IdentifiedVia)
                    {
                        issue.IdentifiedVia = updatedIssue.IdentifiedVia;
                    }
                }
                
                if (Request.Form.ContainsKey("wcagCriteria"))
                {
                    if (issue.WcagCriteria != updatedIssue.WcagCriteria)
                    {
                        issue.WcagCriteria = updatedIssue.WcagCriteria;
                    }
                }
                
                if (Request.Form.ContainsKey("wcagLevel") && !string.IsNullOrWhiteSpace(Request.Form["wcagLevel"]))
                {
                    if (issue.WcagLevel != updatedIssue.WcagLevel)
                    {
                        issue.WcagLevel = updatedIssue.WcagLevel;
                    }
                }
                
                if (Request.Form.ContainsKey("wcagVersion") && !string.IsNullOrWhiteSpace(Request.Form["wcagVersion"]))
                {
                    if (issue.WcagVersion != updatedIssue.WcagVersion)
                    {
                        issue.WcagVersion = updatedIssue.WcagVersion;
                    }
                }
                
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
                return RedirectToAction(nameof(ViewIssue), new { id, documentId = GetDocumentId(issue.ProductAccessibility) });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating accessibility issue");
                var issue = await _context.AccessibilityIssues
                    .Include(i => i.ProductAccessibility)
                    .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted);
                if (issue == null)
                {
                    TempData["ErrorMessage"] = "An error occurred while updating the issue. Issue not found.";
                    return RedirectToAction(nameof(Index));
                }
                TempData["ErrorMessage"] = "An error occurred while updating the issue.";
                return RedirectToAction(nameof(ViewIssue), new { id, documentId = GetDocumentId(issue.ProductAccessibility) });
            }
        }

        // POST: Accessibility/BulkUpdateIssues
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkUpdateIssues(string documentId, List<int> issueIds, string? status, DateTime? identifiedDate, string? isResolving, DateTime? plannedResolutionDate, string? changeNote)
        {
            try
            {
                // Check if user is in Design Operations group
                var userEmail = User.Identity?.Name ?? string.Empty;
                if (string.IsNullOrEmpty(userEmail) || !await _permissionService.IsInGroupAsync(userEmail, "Design Operations"))
                {
                    TempData["ErrorMessage"] = "You do not have permission to perform bulk updates.";
                    return RedirectToAction(nameof(Details), new { documentId, tab = "issues" });
                }
                
                if (issueIds == null || !issueIds.Any())
                {
                    TempData["ErrorMessage"] = "No issues selected for update.";
                    return RedirectToAction(nameof(Details), new { documentId, tab = "issues" });
                }
                
                // Get the product to verify documentId (supports both DocumentId and FipsId)
                var productAccessibility = await _context.ProductAccessibilities
                    .FirstOrDefaultAsync(pa => 
                        (pa.ProductDocumentId == documentId || (string.IsNullOrEmpty(pa.ProductDocumentId) && pa.FipsId == documentId)) 
                        && !pa.IsDeleted);
                
                if (productAccessibility == null)
                {
                    return NotFound();
                }
                
                // Get all issues to update
                var issues = await _context.AccessibilityIssues
                    .Where(i => issueIds.Contains(i.Id) && 
                               i.ProductAccessibilityId == productAccessibility.Id && 
                               !i.IsDeleted)
                    .ToListAsync();
                
                if (!issues.Any())
                {
                    TempData["ErrorMessage"] = "No valid issues found to update.";
                    return RedirectToAction(nameof(Details), new { documentId = GetDocumentId(productAccessibility), tab = "issues" });
                }
                
                var changes = new List<IssueHistory>();
                var updatedCount = 0;
                
                foreach (var issue in issues)
                {
                    var issueChanged = false;
                    
                    // Update status if provided
                    if (!string.IsNullOrEmpty(status) && issue.Status != status)
                    {
                        changes.Add(new IssueHistory
                        {
                            AccessibilityIssueId = issue.Id,
                            FieldChanged = "Status",
                            OldValue = issue.Status,
                            NewValue = status,
                            ChangedAt = DateTime.UtcNow,
                            ChangedBy = userEmail,
                            ChangeNote = changeNote
                        });
                        issue.Status = status;
                        issueChanged = true;
                    }
                    
                    // Update identified date if provided
                    if (identifiedDate.HasValue && issue.IdentifiedDate != identifiedDate.Value)
                    {
                        issue.IdentifiedDate = identifiedDate.Value;
                        issueChanged = true;
                    }
                    
                    // Update resolving status if provided
                    if (!string.IsNullOrEmpty(isResolving))
                    {
                        bool newResolvingValue = bool.Parse(isResolving);
                        if (issue.IsResolving != newResolvingValue)
                        {
                            changes.Add(new IssueHistory
                            {
                                AccessibilityIssueId = issue.Id,
                                FieldChanged = "Resolving",
                                OldValue = issue.IsResolving.ToString(),
                                NewValue = newResolvingValue.ToString(),
                                ChangedAt = DateTime.UtcNow,
                                ChangedBy = userEmail,
                                ChangeNote = changeNote
                            });
                            issue.IsResolving = newResolvingValue;
                            issueChanged = true;
                        }
                    }
                    
                    // Update planned resolution date if provided
                    if (plannedResolutionDate.HasValue && issue.PlannedResolutionDate != plannedResolutionDate.Value)
                    {
                        changes.Add(new IssueHistory
                        {
                            AccessibilityIssueId = issue.Id,
                            FieldChanged = "Planned Resolution Date",
                            OldValue = issue.PlannedResolutionDate?.ToString("yyyy-MM-dd") ?? "",
                            NewValue = plannedResolutionDate.Value.ToString("yyyy-MM-dd"),
                            ChangedAt = DateTime.UtcNow,
                            ChangedBy = userEmail,
                            ChangeNote = changeNote
                        });
                        issue.PlannedResolutionDate = plannedResolutionDate.Value;
                        issueChanged = true;
                    }
                    
                    if (issueChanged)
                    {
                        issue.UpdatedAt = DateTime.UtcNow;
                        issue.UpdatedBy = userEmail;
                        updatedCount++;
                    }
                }
                
                // Add all history entries
                if (changes.Any())
                {
                    _context.IssueHistories.AddRange(changes);
                }
                
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = $"Successfully updated {updatedCount} issue(s).";
                return RedirectToAction(nameof(Details), new { documentId = GetDocumentId(productAccessibility), tab = "issues" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing bulk update on issues");
                // Try to get productAccessibility for redirect
                var productAccessibility = await _context.ProductAccessibilities
                    .FirstOrDefaultAsync(pa => 
                        (pa.ProductDocumentId == documentId || (string.IsNullOrEmpty(pa.ProductDocumentId) && pa.FipsId == documentId)) 
                        && !pa.IsDeleted);
                if (productAccessibility == null)
                {
                    TempData["ErrorMessage"] = "An error occurred while updating issues. Product not found.";
                    return RedirectToAction(nameof(Index));
                }
                TempData["ErrorMessage"] = "An error occurred while updating issues. Please try again.";
                return RedirectToAction(nameof(Details), new { documentId = GetDocumentId(productAccessibility), tab = "issues" });
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
                
                issue.IsDeleted = true;
                issue.UpdatedAt = DateTime.UtcNow;
                issue.UpdatedBy = User.Identity?.Name;
                
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Issue deleted successfully.";
                return RedirectToAction(nameof(Details), new { documentId = GetDocumentId(issue.ProductAccessibility), tab = "issues" });
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
                return RedirectToAction(nameof(ViewIssue), new { id, documentId = GetDocumentId(issue.ProductAccessibility) });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error closing accessibility issue");
                var issue = await _context.AccessibilityIssues
                    .Include(i => i.ProductAccessibility)
                    .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted);
                if (issue == null)
                {
                    TempData["ErrorMessage"] = "An error occurred while closing the issue. Issue not found.";
                    return RedirectToAction(nameof(Index));
                }
                TempData["ErrorMessage"] = "An error occurred while closing the issue.";
                return RedirectToAction(nameof(ViewIssue), new { id, documentId = GetDocumentId(issue.ProductAccessibility) });
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
                    return RedirectToAction(nameof(Details), new { documentId = GetDocumentId(issue.ProductAccessibility), tab = "issues" });
                }

                var retestRequest = new AccessibilityRetestRequest
                {
                    AccessibilityIssueId = issueId,
                    RequestedBy = User.Identity?.Name ?? "Unknown",
                    RequestorEmail = requestorEmail?.Trim() ?? User.Identity?.Name ?? "Unknown",
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
                return RedirectToAction(nameof(Details), new { documentId = GetDocumentId(issue.ProductAccessibility), tab = "issues" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating retest request");
                var issue = await _context.AccessibilityIssues
                    .Include(i => i.ProductAccessibility)
                    .FirstOrDefaultAsync(i => i.Id == issueId && !i.IsDeleted);
                if (issue == null)
                {
                    TempData["ErrorMessage"] = "An error occurred while submitting the retest request. Issue not found.";
                    return RedirectToAction(nameof(Index));
                }
                TempData["ErrorMessage"] = "An error occurred while submitting the retest request.";
                return RedirectToAction(nameof(Details), new { documentId = GetDocumentId(issue.ProductAccessibility), tab = "issues" });
            }
        }

        // POST: Accessibility/RequestVerification
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestVerification(string fipsId, string? requestNotes)
        {
            try
            {
                var productAccessibility = await _context.ProductAccessibilities
                    .FirstOrDefaultAsync(pa => pa.FipsId == fipsId && !pa.IsDeleted);
                
                if (productAccessibility == null)
                {
                    return NotFound();
                }

                // Check if there's already a pending verification request
                var existingRequest = await _context.StatementVerificationRequests
                    .FirstOrDefaultAsync(vr => 
                        vr.ProductAccessibilityId == productAccessibility.Id && 
                        vr.IsCompleted == null);
                
                if (existingRequest != null)
                {
                    TempData["ErrorMessage"] = "There is already a pending verification request for this statement.";
                    return RedirectToAction(nameof(Details), new { fipsId, tab = "statement" });
                }

                var verificationRequest = new StatementVerificationRequest
                {
                    ProductAccessibilityId = productAccessibility.Id,
                    RequestedBy = User.Identity?.Name ?? "Unknown",
                    RequestorEmail = User.Identity?.Name ?? "Unknown",
                    RequestNotes = requestNotes?.Trim(),
                    RequestedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.StatementVerificationRequests.Add(verificationRequest);
                await _context.SaveChangesAsync();

                // TODO: Send email notifications to configured admin emails
                // await SendVerificationRequestEmails(verificationRequest);

                TempData["SuccessMessage"] = "Verification request submitted successfully. An administrator will review your request.";
                return RedirectToAction(nameof(Details), new { fipsId, tab = "statement" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating verification request");
                TempData["ErrorMessage"] = "An error occurred while submitting the verification request.";
                return RedirectToAction(nameof(Details), new { fipsId, tab = "statement" });
            }
        }

        // POST: Accessibility/AddComment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(int issueId, string commentText)
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
                
                TempData["SuccessMessage"] = "Comment added successfully.";
                return RedirectToAction(nameof(ViewIssue), new { id = issueId, documentId = GetDocumentId(issue.ProductAccessibility) });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding comment");
                var issue = await _context.AccessibilityIssues
                    .Include(i => i.ProductAccessibility)
                    .FirstOrDefaultAsync(i => i.Id == issueId && !i.IsDeleted);
                if (issue == null)
                {
                    TempData["ErrorMessage"] = "An error occurred while adding the comment. Issue not found.";
                    return RedirectToAction(nameof(Index));
                }
                TempData["ErrorMessage"] = "An error occurred while adding the comment.";
                return RedirectToAction(nameof(ViewIssue), new { id = issueId, documentId = GetDocumentId(issue.ProductAccessibility) });
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

        private static IEnumerable<string> ExtractCriterionCodes(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                yield break;
            }

            var separators = new[] { ',', ';', '\n', '|', '/' };
            var parts = raw.Split(separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var part in parts)
            {
                var cleaned = part.Trim();
                if (string.IsNullOrEmpty(cleaned))
                {
                    continue;
                }

                if (cleaned.StartsWith("WCAG", StringComparison.OrdinalIgnoreCase))
                {
                    cleaned = cleaned.Substring(4).TrimStart(' ', ':', '-', '.');
                }

                var code = cleaned.Split(new[] { ':', ' ' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();

                if (string.IsNullOrWhiteSpace(code))
                {
                    continue;
                }

                if (char.IsDigit(code[0]))
                {
                    yield return code;
                }
            }
        }
    }
}

