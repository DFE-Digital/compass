using Compass.Attributes;
using Compass.Data;
using Compass.Models;
using Compass.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Compass.Controllers.Api.V1;

[ApiController]
[Route("api/v1/[controller]")]
public class AccessibilityController : ControllerBase
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

    /// <summary>
    /// Get accessibility information for a product by FIPS ID or Legacy ID
    /// </summary>
    [HttpGet("products/{fipsId}")]
    [RequireApiPermission("AccessibilityIssues", "read")]
    public async Task<IActionResult> GetProductAccessibility(string fipsId)
    {
        if (string.IsNullOrWhiteSpace(fipsId))
        {
            return BadRequest(new
            {
                error = new
                {
                    code = "INVALID_REQUEST",
                    message = "FIPS ID is required"
                }
            });
        }

        try
        {
            // Check if the provided ID is a legacy ID (numeric) or FIPS ID (string)
            string? resolvedFipsId = fipsId;
            
            // Try to parse as integer to check if it's a legacy ID
            if (int.TryParse(fipsId, out int legacyId))
            {
                // Look up by legacy ID first
                var productByLegacyId = await _context.ProductAccessibilities
                    .FirstOrDefaultAsync(pa => pa.LegacyId == legacyId && !pa.IsDeleted);
                
                if (productByLegacyId != null)
                {
                    resolvedFipsId = productByLegacyId.FipsId;
                    _logger.LogInformation("Resolved legacy ID {LegacyId} to FIPS ID {FipsId}", legacyId, resolvedFipsId);
                }
            }
            
            // Get product from CMS API
            var cmsProduct = await _productsApiService.GetProductByFipsIdAsync(resolvedFipsId!);
            if (cmsProduct == null)
            {
                return NotFound(new
                {
                    error = new
                    {
                        code = "NOT_FOUND",
                        message = $"Product with ID '{fipsId}' not found"
                    }
                });
            }

            // Get product accessibility data
            var productAccessibility = await _context.ProductAccessibilities
                .Include(pa => pa.ContactMethods)
                .Include(pa => pa.AuditHistories)
                .Include(pa => pa.Issues)
                    .ThenInclude(i => i.WcagCriteriaLinks)
                        .ThenInclude(link => link.WcagCriterion)
                .FirstOrDefaultAsync(pa => pa.FipsId == resolvedFipsId && !pa.IsDeleted);

            // If product not enrolled in accessibility service, return basic product info
            if (productAccessibility == null)
            {
                return Ok(new
                {
                    data = new
                    {
                        fipsId = cmsProduct.FipsId,
                        productName = cmsProduct.Title,
                        productUrl = cmsProduct.ProductUrl,
                        enrolled = false
                    }
                });
            }

            // Get open/active WCAG issues
            var openIssues = productAccessibility.Issues
                .Where(i => !i.IsDeleted && (i.Status == "open" || i.Status == "in_progress"))
                .OrderBy(i => i.IdentifiedDate)
                .Select(i => new
                {
                    id = i.Id,
                    issueType = i.IssueType,
                    issueTitle = i.IssueTitle,
                    issueDescription = i.IssueDescription,
                    identifiedDate = i.IdentifiedDate,
                    identifiedVia = i.IdentifiedVia,
                    status = i.Status,
                    plannedResolutionDate = i.PlannedResolutionDate,
                    wcagCriteria = i.WcagCriteriaLinks.Select(link => new
                    {
                        criterion = link.WcagCriterion.Criterion,
                        title = link.WcagCriterion.Title,
                        level = link.WcagCriterion.Level,
                        version = link.WcagCriterion.Version
                    }).ToList()
                })
                .ToList();

            // Calculate compliance status
            string complianceStatus;
            string templateName;
            
            if (!openIssues.Any())
            {
                complianceStatus = "compliant";
                templateName = "Compliant";
            }
            else
            {
                // Get distinct WCAG criteria that have issues
                var distinctCriteriaWithIssues = openIssues
                    .Where(i => i.wcagCriteria != null && i.wcagCriteria.Any())
                    .SelectMany(i => i.wcagCriteria)
                    .Select(c => c.criterion)
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
                    // Fallback if no criteria found
                    complianceStatus = openIssues.Any() ? "non-compliant" : "compliant";
                    templateName = "Non-compliant";
                }
                else
                {
                    // Calculate percentage: more than 50% = non-compliant
                    var percentage = (double)distinctCriteriaCount / totalDistinctCriteria * 100;
                    var threshold = totalDistinctCriteria / 2.0;
                    
                    if (distinctCriteriaCount > threshold)
                    {
                        // More than half of distinct criteria have issues = non-compliant
                        complianceStatus = "non-compliant";
                        templateName = "Non-compliant";
                    }
                    else if (distinctCriteriaCount > 0)
                    {
                        // 1 or more but less than or equal to half = partially-compliant
                        complianceStatus = "partially-compliant";
                        templateName = "Non-compliant"; // Non-compliant template is used for both partially and non-compliant
                    }
                    else
                    {
                        // 0 distinct criteria with issues = compliant
                        complianceStatus = "compliant";
                        templateName = "Compliant";
                    }
                }
            }

            // Get the appropriate statement template
            var statementTemplate = await _context.StatementTemplates
                .Where(st => st.Name == templateName && !st.IsDeleted && st.IsActive)
                .OrderByDescending(st => st.Version)
                .FirstOrDefaultAsync();

            // Build response
            var response = new
            {
                data = new
                {
                    fipsId = cmsProduct.FipsId,
                    productName = cmsProduct.Title,
                    productUrl = cmsProduct.ProductUrl,
                    enrolled = true,
                    complianceStatus = complianceStatus,
                    statementTemplate = statementTemplate != null ? new
                    {
                        name = statementTemplate.Name,
                        version = statementTemplate.Version,
                        content = statementTemplate.Content,
                        description = statementTemplate.Description
                    } : null,
                    responseSlaDays = productAccessibility.SlaResponseDays,
                    complaintsEmail = productAccessibility.ComplaintsEmail,
                    wcagVersion = productAccessibility.WcagVersion,
                    wcagLevel = productAccessibility.WcagLevel,
                    contactMethods = productAccessibility.ContactMethods
                        .Where(cm => cm.IsActive)
                        .OrderBy(cm => cm.SortOrder)
                        .Select(cm => new
                        {
                            contactType = cm.ContactType,
                            contactDetail = cm.ContactDetail,
                            description = cm.Description
                        })
                        .ToList(),
                    auditHistory = productAccessibility.AuditHistories
                        .Where(ah => !ah.IsDeleted)
                        .OrderByDescending(ah => ah.AuditDate)
                        .Select(ah => new
                        {
                            auditDate = ah.AuditDate,
                            auditedBy = ah.AuditedBy,
                            auditType = ah.AuditType,
                            cost = ah.Cost,
                            notes = ah.Notes,
                            reportUrl = ah.ReportUrl
                        })
                        .ToList(),
                    openIssues = openIssues
                }
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving accessibility data for product {FipsId}", fipsId);
            return StatusCode(500, new
            {
                error = new
                {
                    code = "INTERNAL_ERROR",
                    message = "An error occurred while retrieving accessibility data"
                }
            });
        }
    }

    /// <summary>
    /// Get all enrolled products with their accessibility statements
    /// Matches the logic used in the Accessibility app's "Enrolled Products" section
    /// </summary>
    [HttpGet("products")]
    [RequireApiPermission("AccessibilityIssues", "read")]
    public async Task<IActionResult> GetAllEnrolledProducts()
    {
        try
        {
            // Get all enrolled ProductAccessibilities (same as Accessibility app)
            var enrolledProductAccessibilities = await _context.ProductAccessibilities
                .Where(pa => !pa.IsDeleted && pa.IsActive)
                .ToListAsync();

            // Create dictionary for quick lookup (same as Accessibility app)
            var enrolledDict = enrolledProductAccessibilities
                .GroupBy(pa => pa.FipsId)
                .ToDictionary(g => g.Key, g => g.First()); // Handle duplicates

            // Get all CMS products (same as Accessibility app - iterate through CMS products)
            var cmsProducts = await _productsApiService.GetProductsAsync();
            
            if (cmsProducts == null || !cmsProducts.Any())
            {
                return Ok(new
                {
                    data = new List<object>()
                });
            }

            // Build response - iterate through CMS products and only include enrolled ones (same logic as Accessibility app)
            var productList = new List<(string FipsId, int? LegacyId, string ProductName, string? ProductUrl)>();
            
            foreach (var cmsProduct in cmsProducts)
            {
                // Skip if no FIPS ID
                if (string.IsNullOrEmpty(cmsProduct.FipsId))
                {
                    continue;
                }

                // Check if product is enrolled (matches Accessibility app logic)
                if (!enrolledDict.TryGetValue(cmsProduct.FipsId, out var enrolled))
                {
                    continue; // Not enrolled, skip
                }

                // Only add if enrolled (matches exactly what shows in "Enrolled Products" section)
                productList.Add((
                    FipsId: cmsProduct.FipsId,
                    LegacyId: enrolled.LegacyId,
                    ProductName: cmsProduct.Title ?? enrolled.ProductName ?? cmsProduct.FipsId,
                    ProductUrl: cmsProduct.ProductUrl
                ));
            }

            // Sort by product name (same as Accessibility app) and convert to response format
            var sortedProducts = productList
                .OrderBy(p => p.ProductName)
                .Select(p => new
                {
                    fipsId = p.FipsId,
                    legacyId = p.LegacyId,
                    productName = p.ProductName,
                    productUrl = p.ProductUrl
                })
                .ToList();

            return Ok(new
            {
                data = sortedProducts
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving enrolled products");
            return StatusCode(500, new
            {
                error = new
                {
                    code = "INTERNAL_ERROR",
                    message = "An error occurred while retrieving enrolled products"
                }
            });
        }
    }
}

