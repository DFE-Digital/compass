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
        public async Task<IActionResult> Index(string? search, string? status, string? issues, string? statement)
        {
            var query = _context.ProductAccessibilities
                .Include(pa => pa.Issues.Where(i => !i.IsDeleted))
                .Include(pa => pa.AuditHistories.Where(ah => !ah.IsDeleted))
                .Where(pa => !pa.IsDeleted && pa.IsActive)
                .AsQueryable();
            
            // Apply search filter
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(pa => pa.ProductName.Contains(search) || pa.FipsId.Contains(search));
                ViewBag.CurrentSearch = search;
            }
            
            var enrolledProducts = await query.OrderBy(pa => pa.ProductName).ToListAsync();
            
            // Apply compliance status filter (needs to be done in memory after loading issues)
            if (!string.IsNullOrWhiteSpace(status))
            {
                enrolledProducts = enrolledProducts.Where(pa =>
                {
                    var openIssues = pa.Issues.Count(i => i.Status != "resolved" && !i.IsDeleted);
                    var levelAAIssues = pa.Issues.Count(i => !i.IsDeleted && (i.WcagLevel == "A" || i.WcagLevel == "AA") && i.Status != "resolved");
                    
                    return status switch
                    {
                        "compliant" => levelAAIssues == 0 && openIssues == 0,
                        "partially" => levelAAIssues == 0 && openIssues > 0,
                        "non-compliant" => levelAAIssues > 0,
                        _ => true
                    };
                }).ToList();
                ViewBag.CurrentStatus = status;
            }
            
            // Apply issues filter
            if (!string.IsNullOrWhiteSpace(issues))
            {
                enrolledProducts = enrolledProducts.Where(pa =>
                {
                    var openIssues = pa.Issues.Count(i => i.Status != "resolved" && !i.IsDeleted);
                    return issues switch
                    {
                        "with-issues" => openIssues > 0,
                        "no-issues" => openIssues == 0,
                        _ => true
                    };
                }).ToList();
                ViewBag.CurrentIssues = issues;
            }
            
            // Apply statement filter
            if (!string.IsNullOrWhiteSpace(statement))
            {
                enrolledProducts = enrolledProducts.Where(pa =>
                {
                    return statement switch
                    {
                        "verified" => pa.StatementInstalled && pa.VerifiedAt.HasValue,
                        "not-verified" => !pa.StatementInstalled || !pa.VerifiedAt.HasValue,
                        _ => true
                    };
                }).ToList();
                ViewBag.CurrentStatement = statement;
            }
            
            return View("~/Views/Apps/Accessibility/Index.cshtml", enrolledProducts);
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
            
            if (productAccessibility == null)
            {
                return NotFound();
            }
            
            // Update cached product info from CMS
            var cmsProducts = await _productsApiService.GetProductsAsync();
            var productInfo = cmsProducts?.FirstOrDefault(p => p.FipsId == fipsId);
            
            if (productInfo != null && 
                (productAccessibility.ProductName != productInfo.Title || 
                 productAccessibility.ProductPhase != productInfo.Phase))
            {
                productAccessibility.ProductName = productInfo.Title;
                productAccessibility.ProductPhase = productInfo.Phase;
                productAccessibility.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            
            ViewBag.CurrentTab = tab;
            return View("~/Views/Apps/Accessibility/Details.cshtml", productAccessibility);
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
                
                if (issue.WcagCriteria != updatedIssue.WcagCriteria)
                {
                    changes.Add(new IssueHistory { 
                        FieldChanged = "WCAG Criteria", 
                        OldValue = issue.WcagCriteria, 
                        NewValue = updatedIssue.WcagCriteria 
                    });
                    issue.WcagCriteria = updatedIssue.WcagCriteria;
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
                
                if (issue.Status != updatedIssue.Status)
                {
                    changes.Add(new IssueHistory { 
                        FieldChanged = "Status", 
                        OldValue = issue.Status, 
                        NewValue = updatedIssue.Status 
                    });
                    issue.Status = updatedIssue.Status;
                }
                
                // Update other fields
                issue.WcagLevel = updatedIssue.WcagLevel;
                issue.WcagVersion = updatedIssue.WcagVersion;
                issue.IdentifiedDate = updatedIssue.IdentifiedDate;
                issue.IdentifiedVia = updatedIssue.IdentifiedVia;
                issue.IssueDescription = updatedIssue.IssueDescription;
                issue.NonResolutionReason = updatedIssue.NonResolutionReason;
                issue.ActualResolutionDate = updatedIssue.ActualResolutionDate;
                issue.ResolutionNotes = updatedIssue.ResolutionNotes;
                issue.VerificationNotes = updatedIssue.VerificationNotes;
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
                return RedirectToAction(nameof(Details), new { fipsId = issue.ProductAccessibility.FipsId, tab = "issues" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating accessibility issue");
                return Json(new { success = false, message = "Error updating issue" });
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
                return RedirectToAction(nameof(Details), new { fipsId = issue.ProductAccessibility.FipsId, tab = "issues" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding comment");
                return Json(new { success = false, message = "Error adding comment" });
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

