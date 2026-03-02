using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Compass.Data;
using Compass.Models;
using Compass.Services;
using Compass.Helpers;
using Compass.Security;
using Compass.ViewModels;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Text.Json;

namespace Compass.Controllers;

/// <summary>
/// Controller for managing DDT Standards within COMPASS.
/// Handles creation, editing, approval, rejection, and publishing workflows.
/// </summary>
[Authorize]
public class DdtStandardsController : Controller
{
    private readonly CompassDbContext _context;
    private readonly ILogger<DdtStandardsController> _logger;
    private readonly IUserDirectoryService _userDirectoryService;
    private readonly IPermissionService _permissionService;
    private readonly IStandardsCmsApiService _standardsCmsApiService;

    public DdtStandardsController(
        CompassDbContext context,
        ILogger<DdtStandardsController> logger,
        IUserDirectoryService userDirectoryService,
        IPermissionService permissionService,
        IStandardsCmsApiService standardsCmsApiService)
    {
        _context = context;
        _logger = logger;
        _userDirectoryService = userDirectoryService;
        _permissionService = permissionService;
        _standardsCmsApiService = standardsCmsApiService;
    }

    /// <summary>
    /// Get current user ID from claims
    /// </summary>
    private int? GetCurrentUserId()
    {
        var objectIdClaim = User.FindFirstValue(CompassClaimTypes.ObjectIdentifier);
        if (Guid.TryParse(objectIdClaim, out var objectId))
        {
            var user = _context.Users
                .FirstOrDefault(u => u.AzureObjectId == objectId.ToString());
            return user?.Id;
        }
        return null;
    }

    /// <summary>
    /// Helper method to get standards by stage (my and all)
    /// </summary>
    private async Task<(List<DdtStandard> my, List<DdtStandard> all)> GetStandardsByStageAsync(
        string stageName, 
        int? currentUserId, 
        string? search, 
        string? category,
        int? creatorId = null,
        int? ownerId = null,
        int? contactId = null,
        bool? legalStandard = null,
        bool loadFullData = false)
    {
        // Base query for all standards in this stage
        // Use AsNoTracking for better performance on read-only operations
        // For Published stage, also require IsPublished == true
        var allQuery = _context.DdtStandards
            .AsNoTracking()
            .Where(s => !s.IsDeleted && s.Stage == stageName)
            .AsQueryable();
        
        if (stageName == "Published")
        {
            allQuery = allQuery.Where(s => s.IsPublished);
        }

        // Query for my standards
        // For Published stage: standards where user is owner OR contact
        // For other stages: standards created by current user
        var myQuery = _context.DdtStandards
            .AsNoTracking()
            .Where(s => !s.IsDeleted && s.Stage == stageName)
            .AsQueryable();
        
        if (stageName == "Published")
        {
            myQuery = myQuery.Where(s => s.IsPublished);
        }

        // Only load related data if needed (for edit modal or details view)
        if (loadFullData)
        {
            allQuery = allQuery
            .Include(s => s.CreatorUser)
            .Include(s => s.Owners).ThenInclude(o => o.User)
            .Include(s => s.Contacts).ThenInclude(c => c.User)
            .Include(s => s.Categories).ThenInclude(c => c.Category)
                .Include(s => s.SubCategories).ThenInclude(sc => sc.SubCategory).ThenInclude(sc => sc.Category)
                .Include(s => s.Phases).ThenInclude(p => p.PhaseLookup);

            myQuery = myQuery
            .Include(s => s.CreatorUser)
            .Include(s => s.Owners).ThenInclude(o => o.User)
            .Include(s => s.Contacts).ThenInclude(c => c.User)
            .Include(s => s.Categories).ThenInclude(c => c.Category)
                .Include(s => s.SubCategories).ThenInclude(sc => sc.SubCategory).ThenInclude(sc => sc.Category)
                .Include(s => s.Phases).ThenInclude(p => p.PhaseLookup);
        }
        else
        {
            // For list views, only load what's needed for display
            allQuery = allQuery
                .Include(s => s.CreatorUser)
                .Include(s => s.Owners).ThenInclude(o => o.User)
                .Include(s => s.Contacts).ThenInclude(c => c.User)
                .Include(s => s.Categories).ThenInclude(c => c.Category);
            
            myQuery = myQuery
                .Include(s => s.CreatorUser)
                .Include(s => s.Owners).ThenInclude(o => o.User)
                .Include(s => s.Contacts).ThenInclude(c => c.User)
                .Include(s => s.Categories).ThenInclude(c => c.Category);
        }

        if (currentUserId.HasValue)
        {
            if (stageName == "Published" || stageName == "Unpublished")
            {
                // My published/unpublished: standards where user is owner OR contact
                myQuery = myQuery.Where(s => 
                    s.Owners.Any(o => o.UserId == currentUserId.Value) ||
                    s.Contacts.Any(c => c.UserId == currentUserId.Value));
            }
            else
            {
                // My standards for other stages: standards created by current user
            myQuery = myQuery.Where(s => s.CreatorUserId == currentUserId.Value);
            }
        }
        else
        {
            myQuery = myQuery.Where(s => false); // Empty if no user
        }

        // Apply filters
        if (!string.IsNullOrWhiteSpace(search))
        {
            allQuery = allQuery.Where(s => 
                s.Title.Contains(search) || 
                (s.Summary != null && s.Summary.Contains(search)));
            myQuery = myQuery.Where(s => 
                s.Title.Contains(search) || 
                (s.Summary != null && s.Summary.Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            allQuery = allQuery.Where(s => s.Categories.Any(c => c.Category.Name == category));
            myQuery = myQuery.Where(s => s.Categories.Any(c => c.Category.Name == category));
        }

        if (creatorId.HasValue)
        {
            allQuery = allQuery.Where(s => s.CreatorUserId == creatorId.Value);
            myQuery = myQuery.Where(s => s.CreatorUserId == creatorId.Value);
        }

        if (ownerId.HasValue)
        {
            allQuery = allQuery.Where(s => s.Owners.Any(o => o.UserId == ownerId.Value));
            myQuery = myQuery.Where(s => s.Owners.Any(o => o.UserId == ownerId.Value));
        }

        if (contactId.HasValue)
        {
            allQuery = allQuery.Where(s => s.Contacts.Any(c => c.UserId == contactId.Value));
            myQuery = myQuery.Where(s => s.Contacts.Any(c => c.UserId == contactId.Value));
        }

        if (legalStandard.HasValue)
        {
            allQuery = allQuery.Where(s => s.LegalStandard == legalStandard.Value);
            myQuery = myQuery.Where(s => s.LegalStandard == legalStandard.Value);
        }

        var all = await allQuery
            .OrderByDescending(s => s.UpdatedAt)
            .ToListAsync();

        var my = await myQuery
            .OrderByDescending(s => s.UpdatedAt)
            .ToListAsync();

        return (my, all);
    }

    /// <summary>
    /// Dashboard view for DDT Standards - shows different content based on user role
    /// Redirects to DdtStandardsViewController
    /// </summary>
    public async Task<IActionResult> Dashboard()
    {
        return RedirectToAction("Dashboard", "DdtStandardsView");
    }

    /// <summary>
    /// Drafts view - list all drafts (moved from Index)
    /// Redirects to DdtStandardsManagementController
    /// </summary>
    public async Task<IActionResult> Drafts(string? search, string? category, int? creator, int? owner, int? contact, bool? legalStandard)
    {
        return RedirectToAction("Drafts", "DdtStandardsManagement", new { search, category, creator, owner, contact, legalStandard });
    }


    /// <summary>
    /// Manage standards - list standards by stage (in review, for approval, published, unpublished)
    /// Redirects to DdtStandardsManagementController
    /// </summary>
    public async Task<IActionResult> Index(string? view, string? search, string? category, int? creator, int? owner, int? contact, bool? legalStandard)
    {
        return RedirectToAction("Index", "DdtStandardsManagement", new { view, search, category, creator, owner, contact, legalStandard });
    }


    /// <summary>
    /// Updates view - shows recent changes and recently published standards
    /// Redirects to DdtStandardsViewController
    /// </summary>
    public async Task<IActionResult> Updates()
    {
        return RedirectToAction("Updates", "DdtStandardsView");
    }

    /// <summary>
    /// Published standards - public view of published standards
    /// Redirects to DdtStandardsViewController
    /// </summary>
    public async Task<IActionResult> Published(string? search, string? category)
    {
        return RedirectToAction("Published", "DdtStandardsView", new { search, category });
    }

    /// <summary>
    /// Create new standard - GET (or edit draft if id provided)
    /// Redirects to DdtStandardsManagementController
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Create(int? id)
    {
        return RedirectToAction("Create", "DdtStandardsManagement", new { id });
    }

    /// <summary>
    /// Create new standard - POST
    /// Redirects to DdtStandardsManagementController
    /// NOTE: This method should not be called directly - use DdtStandardsManagementController instead
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Obsolete("Use DdtStandardsManagementController.Create instead")]
    public async Task<IActionResult> Create(
        int? id,
        string title,
        string? summary,
        string? purpose,
        string? criteria,
        string? howToMeet,
        string? governance,
        string? legalBasis,
        bool legalStandard = false,
        int? validityPeriod = null,
        string? relatedGuidance = null,
        List<int>? categoryIds = null,
        List<int>? subCategoryIds = null,
        List<int>? phaseIds = null,
        string? ownerObjectIds = null,
        string? contactObjectIds = null,
        string? approvedProductIds = null,
        string? toleratedProductIds = null,
        string? exceptionIds = null)
    {
        return RedirectToAction("Create", "DdtStandardsManagement", new 
        { 
            id, title, summary, purpose, criteria, howToMeet, governance, legalBasis, 
            legalStandard, validityPeriod, relatedGuidance, categoryIds, subCategoryIds, 
            phaseIds, ownerObjectIds, contactObjectIds, approvedProductIds, 
            toleratedProductIds, exceptionIds 
        });
    }

    /// <summary>
    /// Autosave draft standard via AJAX
    /// Redirects to DdtStandardsManagementController
    /// Note: The view should call DdtStandardsManagement/Autosave directly for AJAX requests
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Autosave(
        int? id,
        string? title,
        string? summary,
        string? purpose,
        string? criteria,
        string? howToMeet,
        string? governance,
        string? legalBasis,
        bool legalStandard = false,
        int? validityPeriod = null,
        string? relatedGuidance = null,
        List<int>? categoryIds = null,
        List<int>? subCategoryIds = null,
        List<int>? phaseIds = null,
        string? ownerObjectIds = null,
        string? contactObjectIds = null)
    {
        return Task.FromResult<IActionResult>(RedirectToAction("Autosave", "DdtStandardsManagement", new 
        { 
            id, title, summary, purpose, criteria, howToMeet, governance, legalBasis, 
            legalStandard, validityPeriod, relatedGuidance, categoryIds, subCategoryIds, 
            phaseIds, ownerObjectIds, contactObjectIds 
        }));
    }


    /// <summary>
    /// View standard details
    /// Redirects to DdtStandardsViewController
    /// </summary>
    public async Task<IActionResult> Details(int id, bool showAllVersions = false)
    {
        return RedirectToAction("Details", "DdtStandardsView", new { id, showAllVersions });
    }

    /// <summary>
    /// Unpublish standard - GET
    /// Redirects to DdtStandardsManagementController
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Unpublish(int id)
    {
        return RedirectToAction("Unpublish", "DdtStandardsManagement", new { id });
    }

    /// <summary>
    /// Unpublish standard - POST
    /// Redirects to DdtStandardsManagementController
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unpublish(int id, string reason)
    {
        return RedirectToAction("Unpublish", "DdtStandardsManagement", new { id, reason });
    }

    /// <summary>
    /// Helper method to parse semantic version for comparison
    /// </summary>
    private Version? TryParseVersion(string versionString)
    {
        if (string.IsNullOrWhiteSpace(versionString))
            return null;
            
        try
        {
            return Version.Parse(versionString);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Get count of unpublished standards (only latest version per standard)
    /// </summary>
    private async Task<int> GetUnpublishedCountAsync()
    {
        var allUnpublished = await _context.DdtStandards
            .AsNoTracking()
            .Where(s => !s.IsDeleted && s.Stage == "Unpublished")
            .ToListAsync();
        
        // Group by title and get only the latest version per standard
        var latestUnpublishedCount = allUnpublished
            .GroupBy(s => s.Title)
            .Select(g => g.OrderByDescending(s => 
            {
                var version = TryParseVersion(s.Version);
                return version ?? new Version(0, 0, 0);
            })
            .ThenByDescending(s => s.UpdatedAt)
            .First())
            .Count();
        
        return latestUnpublishedCount;
    }

    /// <summary>
    /// View all unpublished standards (only latest version per standard)
    /// Redirects to DdtStandardsViewController
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Unpublished(string? search, string? category)
    {
        return RedirectToAction("Unpublished", "DdtStandardsView", new { search, category });
    }


    /// <summary>
    /// Make a change - create a new draft from a published standard
    /// Redirects to DdtStandardsManagementController
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MakeChange(int id)
    {
        return RedirectToAction("MakeChange", "DdtStandardsManagement", new { id });
    }

    /// <summary>
    /// Edit standard - GET
    /// Redirects to DdtStandardsManagementController
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        return RedirectToAction("Edit", "DdtStandardsManagement", new { id });
    }


    /// <summary>
    /// Edit standard - POST
    /// Redirects to DdtStandardsManagementController
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        string title,
        string? summary,
        string? purpose,
        string? criteria,
        string? howToMeet,
        string? governance,
        string? legalBasis,
        bool legalStandard = false,
        int? validityPeriod = null,
        string? relatedGuidance = null,
        List<int>? categoryIds = null,
        List<int>? subCategoryIds = null,
        List<int>? phaseIds = null,
        List<int>? ownerUserIds = null,
        List<int>? contactUserIds = null)
    {
        return RedirectToAction("Edit", "DdtStandardsManagement", new 
        { 
            id, title, summary, purpose, criteria, howToMeet, governance, legalBasis, 
            legalStandard, validityPeriod, relatedGuidance, categoryIds, subCategoryIds, 
            phaseIds, ownerUserIds, contactUserIds 
        });
    }


    /// <summary>
    /// Submit standard for review
    /// Redirects to DdtStandardsManagementController
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitForReview(int id)
    {
        return RedirectToAction("SubmitForReview", "DdtStandardsManagement", new { id });
    }


    /// <summary>
    /// Approve standard - only Standards Managers can approve
    /// Redirects to DdtStandardsManagementController
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id, string? comment)
    {
        return RedirectToAction("Approve", "DdtStandardsManagement", new { id, comment });
    }

    /// <summary>
    /// Reject standard - only Standards Managers can reject
    /// Redirects to DdtStandardsManagementController
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, string reason)
    {
        return RedirectToAction("Reject", "DdtStandardsManagement", new { id, reason });
    }


    /// <summary>
    /// Publish standard - Admins or Standards Managers who are owners can publish
    /// Redirects to DdtStandardsManagementController
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Publish(int id)
    {
        return RedirectToAction("Publish", "DdtStandardsManagement", new { id });
    }


    /// <summary>
    /// Generate URL-friendly slug from title
    /// </summary>
    private static string GenerateSlug(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return string.Empty;

        var slug = title.ToLowerInvariant();
        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = Regex.Replace(slug, @"\s+", " ").Trim();
        slug = Regex.Replace(slug, @"\s", "-");
        slug = Regex.Replace(slug, @"-+", "-");

        return slug;
    }

    /// <summary>
    /// Determine version type for increment
    /// Per spec 4.2.2: Defaults to PATCH increment
    /// Future enhancement: automatic detection of change type
    /// </summary>
    private static string DetermineVersionType(string version)
    {
        // Per spec 4.2.2: Default to patch for published updates
        // Future enhancement: Analyze changes to determine major/minor/patch
        // For now, default to patch as per spec 13.1
        return "patch";
    }

    /// <summary>
    /// Increment semantic version
    /// </summary>
    /// <summary>
    /// Generate LegacyReference for a standard
    /// Format: STD-{number}
    /// - If migrating from CMS, use StandardId from CMS
    /// - For new standards, find the highest number and increment
    /// - For child standards (from Make a Change), inherit from parent
    /// </summary>
    private async Task<string> GenerateLegacyReferenceAsync(int? cmsStandardId = null, int? parentStandardId = null)
    {
        // If migrating from CMS, use StandardId from CMS
        if (cmsStandardId.HasValue)
        {
            return $"STD-{cmsStandardId.Value}";
        }

        // If creating from parent (Make a Change), inherit from parent
        if (parentStandardId.HasValue)
        {
            var parent = await _context.DdtStandards
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == parentStandardId.Value);
            
            if (parent != null && !string.IsNullOrWhiteSpace(parent.LegacyReference))
            {
                return parent.LegacyReference;
            }
        }

        // For new standards, find the highest LegacyReference number and increment
        var allLegacyReferences = await _context.DdtStandards
            .AsNoTracking()
            .Where(s => !string.IsNullOrEmpty(s.LegacyReference) && s.LegacyReference.StartsWith("STD-"))
            .Select(s => s.LegacyReference)
            .ToListAsync();

        int maxNumber = 0;
        foreach (var refStr in allLegacyReferences)
        {
            if (refStr != null && refStr.StartsWith("STD-"))
            {
                var numberPart = refStr.Substring(4); // Remove "STD-"
                if (int.TryParse(numberPart, out var number) && number > maxNumber)
                {
                    maxNumber = number;
                }
            }
        }

        return $"STD-{maxNumber + 1}";
    }

    private static string IncrementVersion(string currentVersion, string versionType)
    {
        if (string.IsNullOrWhiteSpace(currentVersion))
            return "1.0.0";

        var parts = currentVersion.Split('.');
        if (parts.Length != 3)
            return "1.0.0";

        if (!int.TryParse(parts[0], out var major) ||
            !int.TryParse(parts[1], out var minor) ||
            !int.TryParse(parts[2], out var patch))
        {
            return "1.0.0";
        }

        return versionType switch
        {
            "major" => $"{major + 1}.0.0",
            "minor" => $"{major}.{minor + 1}.0",
            "patch" => $"{major}.{minor}.{patch + 1}",
            _ => $"{major}.{minor}.{patch + 1}"
        };
    }

    /// <summary>
    /// Add comment to standard
    /// Redirects to DdtStandardsManagementController
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddComment(int id, string title, string comments, string? commentType, string? field, int? parentCommentId = null)
    {
        return RedirectToAction("AddComment", "DdtStandardsManagement", new { id, title, comments, commentType, field, parentCommentId });
    }


    /// <summary>
    /// Resolve comment
    /// Redirects to DdtStandardsManagementController
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResolveComment(int id, int commentId)
    {
        return RedirectToAction("ResolveComment", "DdtStandardsManagement", new { id, commentId });
    }


    /// <summary>
    /// Get comments for a standard
    /// Redirects to DdtStandardsManagementController
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetComments(int id)
    {
        return RedirectToAction("GetComments", "DdtStandardsManagement", new { id });
    }


    /// <summary>
    /// Unresolve comment
    /// Redirects to DdtStandardsManagementController
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnresolveComment(int id, int commentId)
    {
        return RedirectToAction("UnresolveComment", "DdtStandardsManagement", new { id, commentId });
    }


    /// <summary>
    /// Delete draft standard - only creator or admin can delete
    /// Redirects to DdtStandardsManagementController
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        return RedirectToAction("Delete", "DdtStandardsManagement", new { id });
    }


    /// <summary>
    /// Preview standard
    /// Redirects to DdtStandardsViewController
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Preview(int id)
    {
        return RedirectToAction("Preview", "DdtStandardsView", new { id });
    }


    /// <summary>
    /// Check if current user is in Standards Managers group
    /// </summary>
    private async Task<bool> IsStandardsManagerAsync()
    {
        var userEmail = User.Identity?.Name 
            ?? User.FindFirst(ClaimTypes.Email)?.Value
            ?? string.Empty;
        
        if (string.IsNullOrEmpty(userEmail))
            return false;

        return await _permissionService.IsInGroupAsync(userEmail, "Standards Managers");
    }

    /// <summary>
    /// Create audit log entry for DDT Standard lifecycle events
    /// </summary>
    private async Task CreateAuditLogAsync(int standardId, string action, string? details = null)
    {
        var currentUserId = GetCurrentUserId();
        var user = currentUserId.HasValue 
            ? await _context.Users.FindAsync(currentUserId.Value) 
            : null;
        
        var userEmail = User.Identity?.Name 
            ?? User.FindFirst(ClaimTypes.Email)?.Value
            ?? user?.Email
            ?? "Unknown";

        var auditLog = new AuditLog
        {
            Entity = "DdtStandard",
            EntityId = standardId.ToString(),
            EntityReference = $"DDT-{standardId}",
            Action = action,
            ChangedBy = user?.Name ?? "Unknown",
            ChangedByUserId = currentUserId?.ToString(),
            ChangedByEmail = userEmail,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers["User-Agent"].ToString(),
            ChangedUtc = DateTime.UtcNow,
            AfterJson = details != null ? JsonSerializer.Serialize(new { Details = details }) : null
        };

        _context.AuditLogs.Add(auditLog);
    }

    /// <summary>
    /// Manage approved products - view, add, edit, delete approved and tolerated products
    /// </summary>
    public async Task<IActionResult> ApprovedProducts()
    {
        return RedirectToAction("ApprovedProducts", "DdtStandardsManagement");
    }


    /// <summary>
    /// Create a new standard product
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateProduct(string name, string? description, string? provider, string? version, string approvalStatus)
    {
        return RedirectToAction("CreateProduct", "DdtStandardsManagement", new { name, description, provider, version, approvalStatus });
    }


    /// <summary>
    /// Update an existing standard product
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProduct(int id, string name, string? description, string? provider, string? version, string approvalStatus)
    {
        return RedirectToAction("UpdateProduct", "DdtStandardsManagement", new { id, name, description, provider, version, approvalStatus });
    }


    /// <summary>
    /// Assign a product to a standard (as approved or tolerated)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignProductToStandard(int productId, int standardId, string productType, string? notes)
    {
        return RedirectToAction("AssignProductToStandard", "DdtStandardsManagement", new { productId, standardId, productType, notes });
    }


    /// <summary>
    /// Manage exceptions - view, add, edit, delete known exceptions to standards
    /// </summary>
    public async Task<IActionResult> Exceptions()
    {
        return RedirectToAction("Exceptions", "DdtStandardsManagement");
    }


    /// <summary>
    /// Create a new exception
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateException(
        string title, 
        int standardId, 
        string? description, 
        string? reason,
        int? productId,
        string? fipsProductId,
        DateTime grantedAt,
        DateTime? expiresAt,
        string status,
        string? notes)
    {
        return RedirectToAction("CreateException", "DdtStandardsManagement", new { title, standardId, description, reason, productId, fipsProductId, grantedAt, expiresAt, status, notes });
    }


    /// <summary>
    /// Update an existing exception
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateException(
        int id,
        string title,
        int standardId,
        string? description,
        string? reason,
        int? productId,
        string? fipsProductId,
        DateTime grantedAt,
        DateTime? expiresAt,
        string status,
        string? notes)
    {
        return RedirectToAction("UpdateException", "DdtStandardsManagement", new { id, title, standardId, description, reason, productId, fipsProductId, grantedAt, expiresAt, status, notes });
    }


    /// <summary>
    /// Delete an exception
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteException(int id)
    {
        return RedirectToAction("DeleteException", "DdtStandardsManagement", new { id });
    }


    /// <summary>
    /// Get exceptions for autocomplete (AJAX)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetExceptions(string? search = null)
    {
        return RedirectToAction("GetExceptions", "DdtStandardsManagement", new { search });
    }


    /// <summary>
    /// Migrate published standards from CMS to DDT Standards database
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> MigrateFromCms(bool skipExisting = true)
    {
        return RedirectToAction("MigrateFromCms", "DdtStandardsManagement", new { skipExisting });
    }


    /// <summary>
    /// Update standard title
    /// Only accessible by Standards Managers
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStandardTitle(int id, string title)
    {
        return RedirectToAction("UpdateStandardTitle", "DdtStandardsManagement", new { id, title });
    }


    /// <summary>
    /// Update standard owners
    /// Only accessible by Standards Managers
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStandardOwners(int id, int[]? ownerIds)
    {
        return RedirectToAction("UpdateStandardOwners", "DdtStandardsManagement", new { id, ownerIds });
    }


    /// <summary>
    /// Update standard contacts
    /// Only accessible by Standards Managers
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStandardContacts(int id, int[]? contactIds)
    {
        return RedirectToAction("UpdateStandardContacts", "DdtStandardsManagement", new { id, contactIds });
    }


    /// <summary>
    /// Update standard categories
    /// Only accessible by Standards Managers
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStandardCategories(int id, int[]? categoryIds)
    {
        return RedirectToAction("UpdateStandardCategories", "DdtStandardsManagement", new { id, categoryIds });
    }


    /// <summary>
    /// Update standard sub-categories
    /// Only accessible by Standards Managers
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStandardSubCategories(int id, int[]? subCategoryIds)
    {
        return RedirectToAction("UpdateStandardSubCategories", "DdtStandardsManagement", new { id, subCategoryIds });
    }


    /// <summary>
    /// Update standard legacy reference
    /// Only accessible by Standards Managers
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStandardLegacyReference(int id, string? legacyReference)
    {
        return RedirectToAction("UpdateStandardLegacyReference", "DdtStandardsManagement", new { id, legacyReference });
    }


    /// <summary>
    /// Migrate old version formats to semantic versioning (MAJOR.MINOR.PATCH)
    /// Converts versions like 1.0, 0.1, 0.0 to 1.0.0, 0.1.0, 1.0.0
    /// Also links standards with the same title as parent/child relationships
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> MigrateVersionsToSemantic()
    {
        return RedirectToAction("MigrateVersionsToSemantic", "DdtStandardsManagement");
    }


    /// <summary>
    /// Update LegacyReference for existing standards from CMS StandardId
    /// This updates standards that were migrated but don't have LegacyReference set
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateLegacyReferencesFromCms()
    {
        return RedirectToAction("UpdateLegacyReferencesFromCms", "DdtStandardsManagement");
    }


    /// <summary>
    /// Convert old version format to semantic versioning (MAJOR.MINOR.PATCH)
    /// Examples:
    /// - "1.0" → "1.0.0"
    /// - "0.1" → "0.1.0"
    /// - "0.0" → "1.0.0" (first version)
    /// - "1.1" → "1.1.0"
    /// - "1.0.0" → "1.0.0" (already semantic)
    /// </summary>
    private string ConvertToSemanticVersion(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            return "1.0.0";
        }

        version = version.Trim();

        // If already in semantic format (has 3 parts), return as-is
        var parts = version.Split('.');
        if (parts.Length == 3)
        {
            // Validate it's a valid semantic version
            if (int.TryParse(parts[0], out var major) && 
                int.TryParse(parts[1], out var minor) && 
                int.TryParse(parts[2], out var patch))
            {
                return version; // Already semantic
            }
        }

        // Convert old format to semantic
        if (parts.Length == 2)
        {
            // "1.0" → "1.0.0", "0.1" → "0.1.0"
            if (int.TryParse(parts[0], out var major) && int.TryParse(parts[1], out var minor))
            {
                return $"{major}.{minor}.0";
            }
        }
        else if (parts.Length == 1)
        {
            // "1" → "1.0.0"
            if (int.TryParse(parts[0], out var major))
            {
                return $"{major}.0.0";
            }
        }

        // If parsing fails, try to extract numbers
        var numbers = Regex.Matches(version, @"\d+").Select(m => int.Parse(m.Value)).ToList();
        if (numbers.Count >= 2)
        {
            return $"{numbers[0]}.{numbers[1]}.0";
        }
        else if (numbers.Count == 1)
        {
            return $"{numbers[0]}.0.0";
        }

        // Fallback: if version is "0.0", treat as first version "1.0.0"
        if (version == "0.0" || version == "0")
        {
            return "1.0.0";
        }

        // Last resort: return as 1.0.0
        _logger.LogWarning("Could not parse version '{Version}', defaulting to 1.0.0", version);
        return "1.0.0";
    }

    /// <summary>
    /// Clean up orphaned standards - standards in "For Approval" or "Awaiting Publication" 
    /// that have a later published version of the same standard
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CleanupOrphanedStandard(int id)
    {
        return RedirectToAction("CleanupOrphanedStandard", "DdtStandardsManagement", new { id });
    }

}

