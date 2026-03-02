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
/// Controller for managing DDT Standards (role-based access)
/// Handles creation, editing, approval, rejection, and publishing workflows.
/// Requires appropriate role permissions (Standard Owner, Approver, or Publisher)
/// </summary>
[Authorize]
public class DdtStandardsManagementController : Controller
{
    private readonly CompassDbContext _context;
    private readonly ILogger<DdtStandardsManagementController> _logger;
    private readonly IUserDirectoryService _userDirectoryService;
    private readonly IPermissionService _permissionService;
    private readonly IStandardsCmsApiService _standardsCmsApiService;

    public DdtStandardsManagementController(
        CompassDbContext context,
        ILogger<DdtStandardsManagementController> logger,
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
    /// Check if user can draft standards
    /// </summary>
    private async Task<bool> CanDraftStandardsAsync()
    {
        return await StandardsPermissionHelper.CanDraftStandardsAsync(_permissionService, User);
    }

    /// <summary>
    /// Check if user can approve standards
    /// </summary>
    private async Task<bool> CanApproveStandardsAsync()
    {
        return await StandardsPermissionHelper.CanApproveStandardsAsync(_permissionService, User);
    }

    /// <summary>
    /// Check if user can publish standards
    /// </summary>
    private async Task<bool> CanPublishStandardsAsync()
    {
        return await StandardsPermissionHelper.CanPublishStandardsAsync(_permissionService, User);
    }

    /// <summary>
    /// Manage standards - list standards by stage (for approval, awaiting publication, published, unpublished)
    /// Only accessible by Standard Publishers or Standard Managers
    /// This is separate from Drafts - management is for approval/publishing workflows
    /// </summary>
    public async Task<IActionResult> Index(string? view, string? search, string? category, int? creator, int? owner, int? contact, bool? legalStandard)
    {
        // Check permissions - must be Standard Publisher or Standard Manager (not just any manager)
        if (!await StandardsPermissionHelper.CanManageStandardsWorkflowAsync(_permissionService, User))
        {
            TempData["ErrorMessage"] = "You do not have permission to manage standards. You must be a Standard Publisher or Standards Manager.";
            return RedirectToAction("Published", "DdtStandardsView");
        }

        // If no view specified, default to "in-review" (For Approval)
        if (string.IsNullOrEmpty(view))
        {
            view = "in-review";
        }
        
        // Do NOT redirect to Drafts - this is management, not drafting
        
        var currentUserId = GetCurrentUserId();
        var activeView = view;
        
        // Normalize view names
        if (activeView == "in-review")
            activeView = "in-review";
        else if (activeView == "for-approval")
            activeView = "for-approval";
        else if (activeView == "published")
            activeView = "published";
        else if (activeView == "unpublished")
            activeView = "unpublished";
        else
            activeView = "drafts";

        // Map view names to stage names
        var stageMap = new Dictionary<string, string>
        {
            { "drafts", "Draft" },
            { "in-review", "For Approval" },
            { "for-approval", "Awaiting Publication" },
            { "published", "Published" },
            { "unpublished", "Unpublished" }
        };
        
        var activeStageName = stageMap.GetValueOrDefault(activeView, "Draft");
        
        // Get standards for active stage
        var (myActive, allActive) = await GetStandardsByStageAsync(
            activeStageName, currentUserId, search, category, creator, owner, contact, legalStandard, false);
        
        // Get counts for other stages
        var allDraftsCount = await _context.DdtStandards
            .AsNoTracking()
            .CountAsync(s => !s.IsDeleted && s.Stage == "Draft");
        var allInReviewCount = await _context.DdtStandards
            .AsNoTracking()
            .CountAsync(s => !s.IsDeleted && s.Stage == "For Approval");
        var allForApprovalCount = await _context.DdtStandards
            .AsNoTracking()
            .CountAsync(s => !s.IsDeleted && s.Stage == "Awaiting Publication");
        var allPublishedCount = await _context.DdtStandards
            .AsNoTracking()
            .CountAsync(s => !s.IsDeleted && s.IsPublished && s.Stage == "Published");
        var allUnpublishedCount = await GetUnpublishedCountAsync();
        
        // Map active results to appropriate variables
        var (myDrafts, allDrafts, myInReview, allInReview, myForApproval, allForApproval, myPublished, allPublished, myUnpublished, allUnpublished) = 
            activeView switch
            {
                "drafts" => (myActive, allActive, new List<DdtStandard>(), new List<DdtStandard>(), new List<DdtStandard>(), new List<DdtStandard>(), new List<DdtStandard>(), new List<DdtStandard>(), new List<DdtStandard>(), new List<DdtStandard>()),
                "in-review" => (new List<DdtStandard>(), new List<DdtStandard>(), myActive, allActive, new List<DdtStandard>(), new List<DdtStandard>(), new List<DdtStandard>(), new List<DdtStandard>(), new List<DdtStandard>(), new List<DdtStandard>()),
                "for-approval" => (new List<DdtStandard>(), new List<DdtStandard>(), new List<DdtStandard>(), new List<DdtStandard>(), myActive, allActive, new List<DdtStandard>(), new List<DdtStandard>(), new List<DdtStandard>(), new List<DdtStandard>()),
                "published" => (new List<DdtStandard>(), new List<DdtStandard>(), new List<DdtStandard>(), new List<DdtStandard>(), new List<DdtStandard>(), new List<DdtStandard>(), myActive, allActive, new List<DdtStandard>(), new List<DdtStandard>()),
                "unpublished" => (new List<DdtStandard>(), new List<DdtStandard>(), new List<DdtStandard>(), new List<DdtStandard>(), new List<DdtStandard>(), new List<DdtStandard>(), new List<DdtStandard>(), new List<DdtStandard>(), myActive, allActive),
                _ => (myActive, allActive, new List<DdtStandard>(), new List<DdtStandard>(), new List<DdtStandard>(), new List<DdtStandard>(), new List<DdtStandard>(), new List<DdtStandard>(), new List<DdtStandard>(), new List<DdtStandard>())
            };

        // Get filter options
        var stages = await _context.DdtStandards
            .AsNoTracking()
            .Where(s => !s.IsDeleted)
            .Select(s => s.Stage)
            .Distinct()
            .OrderBy(s => s)
            .ToListAsync();

        var categories = await _context.DdtStandardCategories
            .AsNoTracking()
            .Include(c => c.Category)
            .Select(c => c.Category.Name)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();

        var creators = await _context.DdtStandards
            .AsNoTracking()
            .Where(s => !s.IsDeleted && s.CreatorUserId.HasValue)
            .Join(_context.Users,
                s => s.CreatorUserId,
                u => u.Id,
                (s, u) => new { CreatorUserId = s.CreatorUserId!.Value, u.Name })
            .Distinct()
            .OrderBy(x => x.Name)
            .ToListAsync();

        var owners = await _context.DdtStandardOwners
            .AsNoTracking()
            .Join(_context.Users,
                o => o.UserId,
                u => u.Id,
                (o, u) => new { o.UserId, u.Name })
            .Distinct()
            .OrderBy(x => x.Name)
            .ToListAsync();

        var contacts = await _context.DdtStandardContacts
            .AsNoTracking()
            .Join(_context.Users,
                c => c.UserId,
                u => u.Id,
                (c, u) => new { c.UserId, u.Name })
            .Distinct()
            .OrderBy(x => x.Name)
            .ToListAsync();

        var approvedProductsCount = await _context.StandardProducts.CountAsync();
        var exceptionsCount = await _context.DdtStandardExceptions.CountAsync();

        var viewModel = new DdtStandardsManageViewModel
        {
            MyDrafts = myDrafts,
            AllDrafts = allDrafts,
            MyInReview = myInReview,
            AllInReview = allInReview,
            MyForApproval = myForApproval,
            AllForApproval = allForApproval,
            MyPublished = myPublished,
            AllPublished = allPublished,
            MyUnpublished = myUnpublished,
            AllUnpublished = allUnpublished,
            Stages = stages,
            Categories = categories,
            Creators = creators.Select(c => (c.CreatorUserId, c.Name)).ToList(),
            Owners = owners.Select(o => (o.UserId, o.Name)).ToList(),
            Contacts = contacts.Select(c => (c.UserId, c.Name)).ToList(),
            CurrentSearch = search,
            CurrentCategory = category,
            CurrentCreator = creator,
            CurrentOwner = owner,
            CurrentContact = contact,
            CurrentLegalStandard = legalStandard,
            ActiveView = activeView
        };

        ViewBag.ApprovedProductsCount = approvedProductsCount;
        ViewBag.ExceptionsCount = exceptionsCount;
        ViewBag.AllDraftsCount = allDraftsCount;
        ViewBag.AllInReviewCount = allInReviewCount;
        ViewBag.AllForApprovalCount = allForApprovalCount;
        ViewBag.AllPublishedCount = allPublishedCount;
        ViewBag.AllUnpublishedCount = allUnpublishedCount;
        ViewBag.CanApprove = await CanApproveStandardsAsync();
        ViewBag.CanPublish = await CanPublishStandardsAsync();

        // Add data for edit modal (only if user can manage)
        ViewBag.AllUsers = await _context.Users
            .OrderBy(u => u.Name)
            .ToListAsync();
        ViewBag.AllCategories = await _context.StandardCategories
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync();
        ViewBag.AllSubCategories = await _context.StandardSubCategories
            .Include(sc => sc.Category)
            .Where(sc => sc.IsActive)
            .OrderBy(sc => sc.Category.Name)
            .ThenBy(sc => sc.Name)
            .ToListAsync();

        return View("~/Views/DdtStandards/Index.cshtml", viewModel);
    }

    /// <summary>
    /// Drafts view - list all drafts
    /// Accessible to all authenticated users - everyone can view drafts
    /// </summary>
    public async Task<IActionResult> Drafts(string? view, string? search, string? category, int? creator, int? owner, int? contact, bool? legalStandard)
    {
        // No permission check - everyone can view drafts

        var currentUserId = GetCurrentUserId();
        var activeView = "drafts";
        var activeStageName = "Draft";
        
        // Determine if showing "mine" or "all" view
        var isViewMine = string.Equals(view, "mine", StringComparison.OrdinalIgnoreCase);
        ViewBag.CurrentView = isViewMine ? "mine" : "all";
        
        // Get standards for drafts
        var (myDrafts, allDrafts) = await GetStandardsByStageAsync(
            activeStageName, currentUserId, search, category, creator, owner, contact, legalStandard, false);
        
        // Filter based on view
        if (isViewMine)
        {
            allDrafts = new List<DdtStandard>(); // Don't show "all" when viewing "mine"
        }
        
        // Get counts for navigation
        var allDraftsCount = await _context.DdtStandards
            .AsNoTracking()
            .CountAsync(s => !s.IsDeleted && s.Stage == "Draft");
        var allInReviewCount = await _context.DdtStandards
            .AsNoTracking()
            .CountAsync(s => !s.IsDeleted && s.Stage == "For Approval");
        var allForApprovalCount = await _context.DdtStandards
            .AsNoTracking()
            .CountAsync(s => !s.IsDeleted && s.Stage == "Awaiting Publication");
        var allPublishedCount = await _context.DdtStandards
            .AsNoTracking()
            .CountAsync(s => !s.IsDeleted && s.IsPublished && s.Stage == "Published");
        var allUnpublishedCount = await GetUnpublishedCountAsync();
        
        // Get filter options
        var stages = await _context.DdtStandards
            .AsNoTracking()
            .Where(s => !s.IsDeleted)
            .Select(s => s.Stage)
            .Distinct()
            .OrderBy(s => s)
            .ToListAsync();

        var categories = await _context.DdtStandardCategories
            .AsNoTracking()
            .Include(c => c.Category)
            .Select(c => c.Category.Name)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();

        var creators = await _context.DdtStandards
            .AsNoTracking()
            .Where(s => !s.IsDeleted && s.CreatorUserId.HasValue)
            .Join(_context.Users,
                s => s.CreatorUserId,
                u => u.Id,
                (s, u) => new { CreatorUserId = s.CreatorUserId!.Value, u.Name })
            .Distinct()
            .OrderBy(x => x.Name)
            .ToListAsync();

        var owners = await _context.DdtStandardOwners
            .AsNoTracking()
            .Join(_context.Users,
                o => o.UserId,
                u => u.Id,
                (o, u) => new { o.UserId, u.Name })
            .Distinct()
            .OrderBy(x => x.Name)
            .ToListAsync();

        var contacts = await _context.DdtStandardContacts
            .AsNoTracking()
            .Join(_context.Users,
                c => c.UserId,
                u => u.Id,
                (c, u) => new { c.UserId, u.Name })
            .Distinct()
            .OrderBy(x => x.Name)
            .ToListAsync();

        var approvedProductsCount = await _context.StandardProducts.CountAsync();
        var exceptionsCount = await _context.DdtStandardExceptions.CountAsync();

        var viewModel = new DdtStandardsManageViewModel
        {
            MyDrafts = myDrafts,
            AllDrafts = allDrafts,
            MyInReview = new List<DdtStandard>(),
            AllInReview = new List<DdtStandard>(),
            MyForApproval = new List<DdtStandard>(),
            AllForApproval = new List<DdtStandard>(),
            MyPublished = new List<DdtStandard>(),
            AllPublished = new List<DdtStandard>(),
            MyUnpublished = new List<DdtStandard>(),
            AllUnpublished = new List<DdtStandard>(),
            Stages = stages,
            Categories = categories,
            Creators = creators.Select(c => (c.CreatorUserId, c.Name)).ToList(),
            Owners = owners.Select(o => (o.UserId, o.Name)).ToList(),
            Contacts = contacts.Select(c => (c.UserId, c.Name)).ToList(),
            CurrentSearch = search,
            CurrentCategory = category,
            CurrentCreator = creator,
            CurrentOwner = owner,
            CurrentContact = contact,
            CurrentLegalStandard = legalStandard,
            ActiveView = activeView
        };

        ViewBag.ApprovedProductsCount = approvedProductsCount;
        ViewBag.ExceptionsCount = exceptionsCount;
        ViewBag.AllDraftsCount = allDraftsCount;
        ViewBag.AllInReviewCount = allInReviewCount;
        ViewBag.AllForApprovalCount = allForApprovalCount;
        ViewBag.AllPublishedCount = allPublishedCount;
        ViewBag.AllUnpublishedCount = allUnpublishedCount;
        ViewBag.CanApprove = await CanApproveStandardsAsync();
        ViewBag.CanPublish = await CanPublishStandardsAsync();

        // Add data for edit modal
        ViewBag.AllUsers = await _context.Users
            .OrderBy(u => u.Name)
            .ToListAsync();
        ViewBag.AllCategories = await _context.StandardCategories
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync();
        ViewBag.AllSubCategories = await _context.StandardSubCategories
            .Include(sc => sc.Category)
            .Where(sc => sc.IsActive)
            .OrderBy(sc => sc.Category.Name)
            .ThenBy(sc => sc.Name)
            .ToListAsync();

        ViewBag.CurrentView = isViewMine ? "mine" : "all";
        return View("~/Views/DdtStandards/Drafts.cshtml", viewModel);
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
        var allQuery = _context.DdtStandards
            .AsNoTracking()
            .Where(s => !s.IsDeleted && s.Stage == stageName)
            .AsQueryable();
        
        if (stageName == "Published")
        {
            allQuery = allQuery.Where(s => s.IsPublished);
        }

        // Query for my standards
        var myQuery = _context.DdtStandards
            .AsNoTracking()
            .Where(s => !s.IsDeleted && s.Stage == stageName)
            .AsQueryable();
        
        if (stageName == "Published")
        {
            myQuery = myQuery.Where(s => s.IsPublished);
        }

        // Load related data
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
                myQuery = myQuery.Where(s => 
                    s.Owners.Any(o => o.UserId == currentUserId.Value) ||
                    s.Contacts.Any(c => c.UserId == currentUserId.Value));
            }
            else
            {
                myQuery = myQuery.Where(s => s.CreatorUserId == currentUserId.Value);
            }
        }
        else
        {
            myQuery = myQuery.Where(s => false);
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
    /// Get count of unpublished standards (only latest version per standard)
    /// </summary>
    private async Task<int> GetUnpublishedCountAsync()
    {
        var allUnpublished = await _context.DdtStandards
            .AsNoTracking()
            .Where(s => !s.IsDeleted && s.Stage == "Unpublished")
            .ToListAsync();
        
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
    /// Create new standard - GET (or edit draft if id provided)
    /// Accessible to all authenticated users - everyone can draft standards
    /// If no id provided, redirect to Drafts list view
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Create(int? id)
    {
        // If no id provided, redirect to drafts list (like work reporting)
        if (!id.HasValue)
        {
            return RedirectToAction("Drafts");
        }

        // No permission check - everyone can draft standards

        var phases = await _context.PhaseLookups
            .Where(p => p.IsActive)
            .OrderBy(p => p.SortOrder)
            .ToListAsync();

        var categories = await _context.StandardCategories
            .Include(c => c.SubCategories.Where(sc => sc.IsActive).OrderBy(sc => sc.SortOrder).ThenBy(sc => sc.Name))
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync();

        // Get all approved products for autocomplete
        var allProducts = await _context.StandardProducts
            .Where(p => p.ApprovalStatus == "Approved")
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Description,
                p.Provider,
                p.Version,
                p.DfeFipsProductId,
                p.DfeProductName
            })
            .OrderBy(p => p.Name)
            .ToListAsync();

        ViewBag.Phases = phases;
        ViewBag.Categories = categories;
        ViewBag.AllProducts = allProducts;
        
        // Add counts for navigation
        ViewBag.AllDraftsCount = await _context.DdtStandards
            .AsNoTracking()
            .CountAsync(s => !s.IsDeleted && s.Stage == "Draft");
        ViewBag.AllInReviewCount = await _context.DdtStandards
            .AsNoTracking()
            .CountAsync(s => !s.IsDeleted && s.Stage == "For Approval");
        ViewBag.AllForApprovalCount = await _context.DdtStandards
            .AsNoTracking()
            .CountAsync(s => !s.IsDeleted && s.Stage == "Awaiting Publication");
        ViewBag.AllPublishedCount = await _context.DdtStandards
            .AsNoTracking()
            .CountAsync(s => !s.IsDeleted && s.Stage == "Published");
        ViewBag.AllUnpublishedCount = await GetUnpublishedCountAsync();

        // If id is provided, load existing draft for editing
        if (id.HasValue)
        {
            var standard = await _context.DdtStandards
                .Include(s => s.Owners).ThenInclude(o => o.User)
                .Include(s => s.Contacts).ThenInclude(c => c.User)
                .Include(s => s.Categories).ThenInclude(c => c.Category)
                .Include(s => s.SubCategories).ThenInclude(sc => sc.SubCategory)
                .Include(s => s.Phases).ThenInclude(p => p.PhaseLookup)
                .FirstOrDefaultAsync(s => s.Id == id.Value && !s.IsDeleted);

            if (standard == null)
            {
                return NotFound();
            }

            // Allow editing drafts and unpublished standards (per spec 3.2.7: Unpublished → Draft)
            if (standard.Stage != "Draft" && standard.Stage != "Unpublished")
            {
                TempData["ErrorMessage"] = "Only draft and unpublished standards can be edited in create mode.";
                return RedirectToAction("Details", "DdtStandardsView", new { id = id.Value });
            }
            
            // If editing an unpublished standard, change stage to Draft
            if (standard.Stage == "Unpublished")
            {
                standard.Stage = "Draft";
                standard.UpdatedAt = DateTime.UtcNow;
                await CreateAuditLogAsync(standard.Id, "Edit", 
                    "Unpublished standard edited. Stage: Unpublished → Draft (per spec 3.2.7)");
                await _context.SaveChangesAsync();
            }

            // Check if user is owner or creator (everyone can edit their own drafts)
            var currentUserId = GetCurrentUserId();
            var isOwner = standard.Owners.Any(o => o.UserId == currentUserId);
            var isCreator = standard.CreatorUserId == currentUserId;
            var canManage = await StandardsPermissionHelper.CanManageStandardsAsync(_permissionService, User);

            if (!isOwner && !isCreator && !canManage)
            {
                TempData["ErrorMessage"] = "You can only edit standards that you created or own.";
                return RedirectToAction("Details", "DdtStandardsView", new { id = id.Value });
            }

            ViewBag.Standard = standard;
            ViewBag.SelectedPhaseIds = standard.Phases.Select(p => p.PhaseLookupId).ToList();
            ViewBag.SelectedCategoryIds = standard.Categories.Select(c => c.CategoryId).ToList();
            ViewBag.SelectedSubCategoryIds = standard.SubCategories.Select(sc => sc.SubCategoryId).ToList();
            
            // Get owners and contacts with their Entra Object IDs
            var ownerUsers = await _context.Users
                .Where(u => standard.Owners.Select(o => o.UserId).Contains(u.Id))
                .ToListAsync();
            var contactUsers = await _context.Users
                .Where(u => standard.Contacts.Select(c => c.UserId).Contains(u.Id))
                .ToListAsync();
            
            ViewBag.SelectedOwners = ownerUsers.Select(u => new { 
                ObjectId = u.AzureObjectId, 
                Name = u.Name, 
                Email = u.Email 
            }).ToList();
            ViewBag.SelectedContacts = contactUsers.Select(u => new { 
                ObjectId = u.AzureObjectId, 
                Name = u.Name, 
                Email = u.Email 
            }).ToList();
            
            // Load comments for this standard
            var comments = await _context.DdtStandardComments
                .Include(c => c.User)
                .Include(c => c.ResolvedByUser)
                .Include(c => c.Replies).ThenInclude(r => r.User)
                .Where(c => c.DdtStandardId == standard.Id && c.ParentCommentId == null)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
            
            ViewBag.Comments = comments;
            ViewBag.CommentsCount = comments.Count;
            
            // Load existing products and exceptions for this standard
            var standardProducts = await _context.DdtStandardProducts
                .Include(dsp => dsp.StandardProduct)
                .Where(dsp => dsp.DdtStandardId == standard.Id)
                .ToListAsync();
            ViewBag.StandardProducts = standardProducts;
            
            var standardExceptions = await _context.DdtStandardExceptions
                .Where(e => e.DdtStandardId == standard.Id)
                .ToListAsync();
            ViewBag.StandardExceptions = standardExceptions;
        }

        return View("~/Views/DdtStandards/Create.cshtml");
    }

    /// <summary>
    /// Create new standard - POST
    /// Only accessible by Standard Owners, Approvers, or Publishers
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
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
        // No permission check - everyone can draft standards

        if (string.IsNullOrWhiteSpace(title))
        {
            ModelState.AddModelError("Title", "Title is required");
            return await Create(id);
        }

        if (string.IsNullOrWhiteSpace(governance))
        {
            ModelState.AddModelError("Governance", "Governance is required");
            return await Create(id);
        }

        if (!validityPeriod.HasValue || validityPeriod.Value < 1)
        {
            ModelState.AddModelError("ValidityPeriod", "Validity period is required and must be at least 1 month");
            return await Create(id);
        }

        if (string.IsNullOrWhiteSpace(ownerObjectIds))
        {
            ModelState.AddModelError("Owners", "At least one owner is required");
            return await Create(id);
        }

        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            TempData["ErrorMessage"] = "Unable to identify current user.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            DdtStandard? standard;
            
            if (id.HasValue)
            {
                // Update existing draft
                standard = await _context.DdtStandards
                    .Include(s => s.Owners)
                    .Include(s => s.Contacts)
                    .Include(s => s.Categories)
                    .Include(s => s.SubCategories)
                    .Include(s => s.Phases)
                    .FirstOrDefaultAsync(s => s.Id == id.Value && !s.IsDeleted);

                if (standard == null)
                {
                    return NotFound();
                }

                if (standard.Stage != "Draft")
                {
                    TempData["ErrorMessage"] = "Only draft standards can be edited in create mode.";
                    return RedirectToAction("Details", "DdtStandardsView", new { id = id.Value });
                }

                // Check permissions
                var isOwner = standard.Owners.Any(o => o.UserId == currentUserId);
                var isCreator = standard.CreatorUserId == currentUserId;
                var canManage = await StandardsPermissionHelper.CanManageStandardsAsync(_permissionService, User);

                if (!isOwner && !isCreator && !canManage)
                {
                    TempData["ErrorMessage"] = "You can only edit standards that you created or own.";
                    return RedirectToAction("Details", "DdtStandardsView", new { id = id.Value });
                }

                // Update properties
                standard.Title = title.Trim();
                standard.Slug = GenerateSlug(title);
                standard.Summary = summary;
                standard.Purpose = purpose;
                standard.Criteria = criteria;
                standard.HowToMeet = howToMeet;
                standard.Governance = governance;
                standard.LegalBasis = legalBasis;
                standard.LegalStandard = legalStandard;
                standard.ValidityPeriod = validityPeriod;
                standard.RelatedGuidance = relatedGuidance;
                standard.UpdatedAt = DateTime.UtcNow;

                // Clear existing relationships
                _context.DdtStandardOwners.RemoveRange(standard.Owners);
                _context.DdtStandardContacts.RemoveRange(standard.Contacts);
                _context.DdtStandardCategories.RemoveRange(standard.Categories);
                _context.DdtStandardSubCategories.RemoveRange(standard.SubCategories);
                _context.DdtStandardPhases.RemoveRange(standard.Phases);
            }
            else
            {
                // Create new standard
                standard = new DdtStandard
                {
                    Title = title.Trim(),
                    Slug = GenerateSlug(title),
                    Summary = summary,
                    Purpose = purpose,
                    Criteria = criteria,
                    HowToMeet = howToMeet,
                    Governance = governance,
                    LegalBasis = legalBasis,
                    LegalStandard = legalStandard,
                    ValidityPeriod = validityPeriod,
                    RelatedGuidance = relatedGuidance,
                    Stage = "Draft",
                    Version = "0.1.0",
                    LegacyReference = await GenerateLegacyReferenceAsync(),
                    CreatorUserId = currentUserId.Value,
                    DraftCreated = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.DdtStandards.Add(standard);
            }

            await _context.SaveChangesAsync();

            // Create audit log entry per spec 9.1 (Standard Created) - only for new standards
            if (!id.HasValue && standard.Id > 0)
            {
                await CreateAuditLogAsync(standard.Id, "Created", 
                    $"Standard created. Title: {standard.Title}, Stage: {standard.Stage}, Version: {standard.Version}");
            }

            // Add categories
            if (categoryIds != null && categoryIds.Any())
            {
                foreach (var categoryId in categoryIds)
                {
                    var categoryExists = await _context.StandardCategories.AnyAsync(c => c.Id == categoryId);
                    if (categoryExists)
                    {
                        _context.DdtStandardCategories.Add(new DdtStandardCategory
                        {
                            DdtStandardId = standard.Id,
                            CategoryId = categoryId,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }
            }

            // Add sub-categories
            if (subCategoryIds != null && subCategoryIds.Any())
            {
                foreach (var subCategoryId in subCategoryIds)
                {
                    var subCategoryExists = await _context.StandardSubCategories.AnyAsync(sc => sc.Id == subCategoryId);
                    if (subCategoryExists)
                    {
                        _context.DdtStandardSubCategories.Add(new DdtStandardSubCategory
                        {
                            DdtStandardId = standard.Id,
                            SubCategoryId = subCategoryId,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }
            }

            // Add phases
            if (phaseIds != null && phaseIds.Any())
            {
                foreach (var phaseId in phaseIds)
                {
                    _context.DdtStandardPhases.Add(new DdtStandardPhase
                    {
                        DdtStandardId = standard.Id,
                        PhaseLookupId = phaseId,
                        Enabled = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
            }

            // Add owners - convert Entra object IDs to COMPASS User IDs
            if (!string.IsNullOrWhiteSpace(ownerObjectIds))
            {
                var ownerObjectIdList = ownerObjectIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var objectIdStr in ownerObjectIdList)
                {
                    if (Guid.TryParse(objectIdStr, out var objectIdGuid))
                    {
                        try
                        {
                            var directoryUser = await _userDirectoryService.EnsureUserAsync(objectIdGuid);
                            var compassUser = await _context.Users.FirstOrDefaultAsync(u => u.AzureObjectId == directoryUser.AzureObjectId);
                            if (compassUser != null)
                            {
                                _context.DdtStandardOwners.Add(new DdtStandardOwner
                                {
                                    DdtStandardId = standard.Id,
                                    UserId = compassUser.Id,
                                    CreatedAt = DateTime.UtcNow,
                                    UpdatedAt = DateTime.UtcNow
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to convert owner object ID {ObjectId} to COMPASS user", objectIdStr);
                        }
                    }
                }
            }

            // Add contacts - convert Entra object IDs to COMPASS User IDs
            if (!string.IsNullOrWhiteSpace(contactObjectIds))
            {
                var contactObjectIdList = contactObjectIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var objectIdStr in contactObjectIdList)
                {
                    if (Guid.TryParse(objectIdStr, out var objectIdGuid))
                    {
                        try
                        {
                            var directoryUser = await _userDirectoryService.EnsureUserAsync(objectIdGuid);
                            var compassUser = await _context.Users.FirstOrDefaultAsync(u => u.AzureObjectId == directoryUser.AzureObjectId);
                            if (compassUser != null)
                            {
                                _context.DdtStandardContacts.Add(new DdtStandardContact
                                {
                                    DdtStandardId = standard.Id,
                                    UserId = compassUser.Id,
                                    CreatedAt = DateTime.UtcNow,
                                    UpdatedAt = DateTime.UtcNow
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to convert contact object ID {ObjectId} to COMPASS user", objectIdStr);
                        }
                    }
                }
            }

            await _context.SaveChangesAsync();

            // Handle products and exceptions - clear existing first if editing
            if (id.HasValue)
            {
                var existingProducts = await _context.DdtStandardProducts
                    .Where(dsp => dsp.DdtStandardId == standard.Id)
                    .ToListAsync();
                _context.DdtStandardProducts.RemoveRange(existingProducts);
            }

            // Add approved products
            if (!string.IsNullOrWhiteSpace(approvedProductIds))
            {
                var productIdList = approvedProductIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var productIdStr in productIdList)
                {
                    if (int.TryParse(productIdStr, out var productId))
                    {
                        var productExists = await _context.StandardProducts.AnyAsync(p => p.Id == productId);
                        if (productExists)
                        {
                            _context.DdtStandardProducts.Add(new DdtStandardProduct
                            {
                                DdtStandardId = standard.Id,
                                StandardProductId = productId,
                                ProductType = "Approved",
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow
                            });
                        }
                    }
                }
            }

            // Add tolerated products
            if (!string.IsNullOrWhiteSpace(toleratedProductIds))
            {
                var productIdList = toleratedProductIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var productIdStr in productIdList)
                {
                    if (int.TryParse(productIdStr, out var productId))
                    {
                        var productExists = await _context.StandardProducts.AnyAsync(p => p.Id == productId);
                        if (productExists)
                        {
                            _context.DdtStandardProducts.Add(new DdtStandardProduct
                            {
                                DdtStandardId = standard.Id,
                                StandardProductId = productId,
                                ProductType = "Tolerated",
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow
                            });
                        }
                    }
                }
            }

            // Link exceptions (update existing exceptions to point to this standard)
            if (!string.IsNullOrWhiteSpace(exceptionIds))
            {
                var exceptionIdList = exceptionIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var exceptionIdStr in exceptionIdList)
                {
                    if (int.TryParse(exceptionIdStr, out var exceptionId))
                    {
                        var exception = await _context.DdtStandardExceptions.FindAsync(exceptionId);
                        if (exception != null)
                        {
                            exception.DdtStandardId = standard.Id;
                            exception.UpdatedAt = DateTime.UtcNow;
                        }
                    }
                }
            }

            await _context.SaveChangesAsync();

            // Create audit log entry for updates per spec 9.1 (Standard Modified)
            if (id.HasValue && standard.Id > 0)
            {
                await CreateAuditLogAsync(standard.Id, "Modified", 
                    $"Standard updated. Title: {standard.Title}");
            }

            TempData["SuccessMessage"] = id.HasValue 
                ? $"Standard '{standard.Title}' updated successfully."
                : $"Standard '{standard.Title}' created successfully.";
            return RedirectToAction(nameof(Create), new { id = standard.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating standard");
            TempData["ErrorMessage"] = "An error occurred while creating the standard.";
            return await Create(id);
        }
    }

    /// <summary>
    /// Autosave draft standard via AJAX
    /// Returns JSON response for AJAX requests
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Autosave(
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
        // Check if this is an AJAX request
        if (!Request.Headers["X-Requested-With"].ToString().Equals("XMLHttpRequest", StringComparison.OrdinalIgnoreCase))
        {
            // Not an AJAX request, redirect to Create
            return RedirectToAction("Create", new { id });
        }

        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            return Json(new { success = false, message = "Unable to identify current user." });
        }

        try
        {
            DdtStandard? standard;

            if (id.HasValue)
            {
                // Update existing draft
                standard = await _context.DdtStandards
                    .Include(s => s.Owners)
                    .Include(s => s.Contacts)
                    .Include(s => s.Categories)
                    .Include(s => s.SubCategories)
                    .Include(s => s.Phases)
                    .FirstOrDefaultAsync(s => s.Id == id.Value && !s.IsDeleted);

                if (standard == null)
                {
                    return Json(new { success = false, message = "Standard not found." });
                }

                if (standard.Stage != "Draft")
                {
                    return Json(new { success = false, message = "Only draft standards can be autosaved." });
                }

                // Check permissions
                var isOwner = standard.Owners.Any(o => o.UserId == currentUserId);
                var isCreator = standard.CreatorUserId == currentUserId;
                var canManage = await StandardsPermissionHelper.CanManageStandardsAsync(_permissionService, User);

                if (!isOwner && !isCreator && !canManage)
                {
                    return Json(new { success = false, message = "You can only autosave standards that you created or own." });
                }

                // Update properties (only if provided)
                if (!string.IsNullOrWhiteSpace(title))
                {
                    standard.Title = title.Trim();
                    standard.Slug = GenerateSlug(title);
                }
                if (summary != null) standard.Summary = summary;
                if (purpose != null) standard.Purpose = purpose;
                if (criteria != null) standard.Criteria = criteria;
                if (howToMeet != null) standard.HowToMeet = howToMeet;
                if (governance != null) standard.Governance = governance;
                if (legalBasis != null) standard.LegalBasis = legalBasis;
                standard.LegalStandard = legalStandard;
                if (validityPeriod.HasValue) standard.ValidityPeriod = validityPeriod;
                if (relatedGuidance != null) standard.RelatedGuidance = relatedGuidance;
                standard.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                // Create new draft (minimal validation for autosave)
                if (string.IsNullOrWhiteSpace(title))
                {
                    return Json(new { success = false, message = "Title is required." });
                }

                standard = new DdtStandard
                {
                    Title = title.Trim(),
                    Slug = GenerateSlug(title),
                    Summary = summary,
                    Purpose = purpose,
                    Criteria = criteria,
                    HowToMeet = howToMeet,
                    Governance = governance,
                    LegalBasis = legalBasis,
                    LegalStandard = legalStandard,
                    ValidityPeriod = validityPeriod,
                    RelatedGuidance = relatedGuidance,
                    Stage = "Draft",
                    Version = "1.0.0",
                    IsPublished = false,
                    CreatorUserId = currentUserId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.DdtStandards.Add(standard);
                await _context.SaveChangesAsync(); // Save first to get the ID

                // Generate legacy reference
                standard.LegacyReference = await GenerateLegacyReferenceAsync();
            }

            // Update relationships
            if (categoryIds != null)
            {
                standard.Categories.Clear();
                foreach (var categoryId in categoryIds)
                {
                    standard.Categories.Add(new DdtStandardCategory
                    {
                        DdtStandardId = standard.Id,
                        CategoryId = categoryId
                    });
                }
            }

            if (subCategoryIds != null)
            {
                standard.SubCategories.Clear();
                foreach (var subCategoryId in subCategoryIds)
                {
                    standard.SubCategories.Add(new DdtStandardSubCategory
                    {
                        DdtStandardId = standard.Id,
                        SubCategoryId = subCategoryId
                    });
                }
            }

            if (phaseIds != null)
            {
                standard.Phases.Clear();
                foreach (var phaseId in phaseIds)
                {
                    standard.Phases.Add(new DdtStandardPhase
                    {
                        DdtStandardId = standard.Id,
                        PhaseLookupId = phaseId,
                        Enabled = true
                    });
                }
            }

            // Update owners and contacts from object IDs
            if (!string.IsNullOrWhiteSpace(ownerObjectIds))
            {
                standard.Owners.Clear();
                var ownerObjectIdList = ownerObjectIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(o => o.Trim())
                    .ToList();
                
                foreach (var objectId in ownerObjectIdList)
                {
                    var ownerUser = await _context.Users
                        .FirstOrDefaultAsync(u => u.AzureObjectId == objectId);
                    
                    if (ownerUser != null)
                    {
                        standard.Owners.Add(new DdtStandardOwner
                        {
                            DdtStandardId = standard.Id,
                            UserId = ownerUser.Id
                        });
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(contactObjectIds))
            {
                standard.Contacts.Clear();
                var contactObjectIdList = contactObjectIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(c => c.Trim())
                    .ToList();
                
                foreach (var objectId in contactObjectIdList)
                {
                    var contactUser = await _context.Users
                        .FirstOrDefaultAsync(u => u.AzureObjectId == objectId);
                    
                    if (contactUser != null)
                    {
                        standard.Contacts.Add(new DdtStandardContact
                        {
                            DdtStandardId = standard.Id,
                            UserId = contactUser.Id
                        });
                    }
                }
            }

            standard.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Json(new { success = true, id = standard.Id, message = "Draft autosaved successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error autosaving standard");
            return Json(new { success = false, message = "An error occurred while autosaving the draft." });
        }
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
    /// Generate LegacyReference for a standard
    /// </summary>
    private async Task<string> GenerateLegacyReferenceAsync(int? cmsStandardId = null, int? parentStandardId = null)
    {
        if (cmsStandardId.HasValue)
        {
            return $"STD-{cmsStandardId.Value}";
        }

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
                var numberPart = refStr.Substring(4);
                if (int.TryParse(numberPart, out var number) && number > maxNumber)
                {
                    maxNumber = number;
                }
            }
        }

        return $"STD-{maxNumber + 1}";
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
    /// Submit standard for review
    /// Only accessible by Standard Owners, Approvers, or Publishers
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitForReview(int id)
    {
        // No group permission check - everyone can submit their own drafts
        // But we still check if they're the owner/creator below

        _logger.LogInformation("SubmitForReview called with id: {Id}", id);

        if (id <= 0)
        {
            _logger.LogWarning("SubmitForReview: Invalid standard ID {Id}", id);
            TempData["ErrorMessage"] = "Invalid standard ID. Please save the standard first before submitting for review.";
            return RedirectToAction(nameof(Create));
        }

        var standard = await _context.DdtStandards
            .Include(s => s.Owners)
            .Include(s => s.Products)
            .Include(s => s.Exceptions)
            .Include(s => s.ParentStandard)
                .ThenInclude(p => p.Products)
            .Include(s => s.ParentStandard)
                .ThenInclude(p => p.Exceptions)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

        if (standard == null)
        {
            _logger.LogWarning("SubmitForReview: Standard {Id} not found", id);
            TempData["ErrorMessage"] = "Standard not found. Please save the standard first before submitting for review.";
            return RedirectToAction(nameof(Create));
        }

        // Check if user is owner or creator (everyone can submit their own drafts)
        var currentUserId = GetCurrentUserId();
        var isOwner = standard.Owners.Any(o => o.UserId == currentUserId);
        var isCreator = standard.CreatorUserId == currentUserId;
        var canManage = await StandardsPermissionHelper.CanManageStandardsAsync(_permissionService, User);

        if (!isOwner && !isCreator && !canManage)
        {
            TempData["ErrorMessage"] = "You can only submit standards that you created or own.";
            return RedirectToAction("Details", "DdtStandardsView", new { id });
        }

        _logger.LogInformation("SubmitForReview: Standard {Id} found. Stage: {Stage}, Owners: {OwnerCount}, Governance: {HasGovernance}, ValidityPeriod: {ValidityPeriod}",
            id, standard.Stage, standard.Owners.Count, !string.IsNullOrWhiteSpace(standard.Governance), standard.ValidityPeriod);

        if (standard.Stage != "Draft")
        {
            _logger.LogWarning("SubmitForReview: Standard {Id} is not in Draft stage. Current stage: {Stage}", id, standard.Stage);
            TempData["ErrorMessage"] = $"Only draft standards can be submitted for review. Current stage: {standard.Stage}.";
            return RedirectToAction(nameof(Create), new { id });
        }

        // Validation per spec 3.2.1: At least one owner must be assigned
        if (!standard.Owners.Any())
        {
            _logger.LogWarning("SubmitForReview: Standard {Id} has no owners", id);
            TempData["ErrorMessage"] = "At least one owner must be assigned before submitting for review. Please add an owner in the 'Owners & Contacts' section.";
            return RedirectToAction(nameof(Create), new { id });
        }

        // Validation per spec 3.2.1: Governance field must be completed
        if (string.IsNullOrWhiteSpace(standard.Governance))
        {
            _logger.LogWarning("SubmitForReview: Standard {Id} has no governance", id);
            TempData["ErrorMessage"] = "Governance field must be completed before submitting for review. Please complete the 'Compliance and Governance' section.";
            return RedirectToAction(nameof(Create), new { id });
        }

        // Validation per spec 3.2.1: Validity period must be set
        if (!standard.ValidityPeriod.HasValue || standard.ValidityPeriod.Value <= 0)
        {
            _logger.LogWarning("SubmitForReview: Standard {Id} has invalid validity period: {ValidityPeriod}", id, standard.ValidityPeriod);
            TempData["ErrorMessage"] = "Validity period must be set before submitting for review. Please set a validity period in months in the 'Compliance and Governance' section.";
            return RedirectToAction(nameof(Create), new { id });
        }

        // Only increment version if this is a new submission (not a resubmission after rejection)
        // and if it has a parent standard (was created from "Make a change")
        bool shouldIncrementVersion = standard.ParentStandardId.HasValue && 
                                      standard.ParentStandard != null &&
                                      !standard.Version.Contains("-resubmit");

        if (shouldIncrementVersion && standard.ParentStandard != null)
        {
            var parentStandard = standard.ParentStandard;
            var versionParts = parentStandard.Version.Split('.');
            
            if (versionParts.Length == 3 && 
                int.TryParse(versionParts[0], out var major) &&
                int.TryParse(versionParts[1], out var minor) &&
                int.TryParse(versionParts[2], out var patch))
            {
                // Check what changed to determine version increment per spec 4.2.2
                bool productsAdded = false;
                bool productsRemoved = false;
                bool exceptionsAdded = false;
                bool exceptionsRemoved = false;
                bool descriptionChanged = false;
                bool otherChanged = false;

                // Compare products
                var parentProductIds = parentStandard.Products.Select(p => p.StandardProductId).OrderBy(x => x).ToList();
                var currentProductIds = standard.Products.Select(p => p.StandardProductId).OrderBy(x => x).ToList();
                var addedProducts = currentProductIds.Except(parentProductIds).ToList();
                var removedProducts = parentProductIds.Except(currentProductIds).ToList();
                
                if (addedProducts.Any()) productsAdded = true;
                if (removedProducts.Any()) productsRemoved = true;

                // Compare exceptions
                var parentExceptionIds = await _context.DdtStandardExceptions
                    .Where(e => e.DdtStandardId == parentStandard.Id && e.Status == "Active")
                    .Select(e => e.Id)
                    .OrderBy(x => x)
                    .ToListAsync();
                var currentExceptionIds = await _context.DdtStandardExceptions
                    .Where(e => e.DdtStandardId == standard.Id && e.Status == "Active")
                    .Select(e => e.Id)
                    .OrderBy(x => x)
                    .ToListAsync();
                var addedExceptions = currentExceptionIds.Except(parentExceptionIds).ToList();
                var removedExceptions = parentExceptionIds.Except(currentExceptionIds).ToList();
                
                if (addedExceptions.Any()) exceptionsAdded = true;
                if (removedExceptions.Any()) exceptionsRemoved = true;

                // Compare description (Summary field)
                if (parentStandard.Summary != standard.Summary)
                {
                    descriptionChanged = true;
                }

                // Check for other changes
                if (parentStandard.Purpose != standard.Purpose ||
                    parentStandard.HowToMeet != standard.HowToMeet ||
                    parentStandard.Criteria != standard.Criteria ||
                    parentStandard.Governance != standard.Governance ||
                    parentStandard.LegalBasis != standard.LegalBasis ||
                    parentStandard.RelatedGuidance != standard.RelatedGuidance ||
                    parentStandard.Title != standard.Title)
                {
                    otherChanged = true;
                }

                // Check for legal status change
                bool legalStatusChanged = parentStandard.LegalStandard != standard.LegalStandard;

                // Increment version based on change type
                string versionType = "patch";
                if (productsAdded || productsRemoved || exceptionsAdded || exceptionsRemoved || legalStatusChanged)
                {
                    major++;
                    minor = 0;
                    patch = 0;
                    versionType = "major";
                }
                else if (descriptionChanged)
                {
                    patch++;
                    versionType = "patch";
                }
                else if (otherChanged)
                {
                    minor++;
                    patch = 0;
                    versionType = "minor";
                }

                var newVersion = $"{major}.{minor}.{patch}";
                standard.PreviousVersion = standard.Version;
                standard.Version = newVersion;
                
                // Determine the stage based on version type
                if (versionType == "major")
                {
                    standard.Stage = "For Approval";
                }
                else
                {
                    standard.Stage = "Awaiting Publication";
                    standard.GovernanceApproval = true;
                }
            }
            else
            {
                standard.Stage = "For Approval";
            }
        }
        else
        {
            standard.Stage = "For Approval";
        }

        try
        {
            standard.UpdatedAt = DateTime.UtcNow;

            // Create audit log entry per spec 9.1
            await CreateAuditLogAsync(standard.Id, "Submitted", $"Standard submitted for review. Stage: Draft → {standard.Stage}");

            await _context.SaveChangesAsync();

            _logger.LogInformation("SubmitForReview: Standard {Id} successfully submitted. Stage: {Stage}", id, standard.Stage);

            if (standard.Stage == "Awaiting Publication")
            {
                TempData["SuccessMessage"] = "Standard submitted successfully. It is now in 'Awaiting Publication' status and can be published by a Publisher.";
            }
            else
            {
                TempData["SuccessMessage"] = "Standard submitted for review successfully. It is now in 'For Approval' status.";
            }
            return RedirectToAction("Details", "DdtStandardsView", new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SubmitForReview: Error submitting standard {Id} for review", id);
            TempData["ErrorMessage"] = $"An error occurred while submitting the standard for review: {ex.Message}";
            return RedirectToAction(nameof(Create), new { id });
        }
    }

    /// <summary>
    /// Approve standard - only Standard Approvers can approve
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id, string? comment)
    {
        // Check permissions - must be able to approve standards
        if (!await CanApproveStandardsAsync())
        {
            TempData["ErrorMessage"] = "You do not have permission to approve standards. Only Standard Approvers can approve.";
            return RedirectToAction("Details", "DdtStandardsView", new { id });
        }

        var standard = await _context.DdtStandards
            .Include(s => s.Versions)
            .Include(s => s.ParentStandard)
            .Include(s => s.Products)
            .Include(s => s.Exceptions)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

        if (standard == null)
        {
            return NotFound();
        }

        if (standard.Stage != "For Approval")
        {
            TempData["ErrorMessage"] = "Only standards awaiting approval can be approved.";
            return RedirectToAction("Details", "DdtStandardsView", new { id });
        }

        var currentUserId = GetCurrentUserId();
        var now = DateTime.UtcNow;

        // Per spec 3.2.2: If standard has a parent (created via "Make a Change"), 
        // unpublish parent and immediately publish new standard
        bool hasParent = standard.ParentStandardId.HasValue && standard.ParentStandard != null;
        
        if (hasParent && standard.ParentStandard != null)
        {
            var parentStandard = standard.ParentStandard;
            var previousVersion = standard.Version;
            string versionType = "patch";
            string newVersion = previousVersion;
            bool isMajorChange = false;
            
            var versionParts = parentStandard.Version.Split('.');
            
            if (versionParts.Length == 3 && 
                int.TryParse(versionParts[0], out var major) &&
                int.TryParse(versionParts[1], out var minor) &&
                int.TryParse(versionParts[2], out var patch))
            {
                // Check what changed to determine version increment
                bool productsAdded = false;
                bool productsRemoved = false;
                bool exceptionsAdded = false;
                bool exceptionsRemoved = false;
                bool descriptionChanged = false;
                bool otherChanged = false;

                var parentProductIds = parentStandard.Products.Select(p => p.StandardProductId).OrderBy(x => x).ToList();
                var currentProductIds = standard.Products.Select(p => p.StandardProductId).OrderBy(x => x).ToList();
                var addedProducts = currentProductIds.Except(parentProductIds).ToList();
                var removedProducts = parentProductIds.Except(currentProductIds).ToList();
                
                if (addedProducts.Any()) productsAdded = true;
                if (removedProducts.Any()) productsRemoved = true;

                var parentExceptionIds = await _context.DdtStandardExceptions
                    .Where(e => e.DdtStandardId == parentStandard.Id && e.Status == "Active")
                    .Select(e => e.Id)
                    .OrderBy(x => x)
                    .ToListAsync();
                var currentExceptionIds = await _context.DdtStandardExceptions
                    .Where(e => e.DdtStandardId == standard.Id && e.Status == "Active")
                    .Select(e => e.Id)
                    .OrderBy(x => x)
                    .ToListAsync();
                var addedExceptions = currentExceptionIds.Except(parentExceptionIds).ToList();
                var removedExceptions = parentExceptionIds.Except(currentExceptionIds).ToList();
                
                if (addedExceptions.Any()) exceptionsAdded = true;
                if (removedExceptions.Any()) exceptionsRemoved = true;

                if (parentStandard.Summary != standard.Summary) descriptionChanged = true;

                if (parentStandard.Purpose != standard.Purpose ||
                    parentStandard.HowToMeet != standard.HowToMeet ||
                    parentStandard.Criteria != standard.Criteria ||
                    parentStandard.Governance != standard.Governance ||
                    parentStandard.LegalBasis != standard.LegalBasis ||
                    parentStandard.RelatedGuidance != standard.RelatedGuidance ||
                    parentStandard.Title != standard.Title)
                {
                    otherChanged = true;
                }

                bool legalStatusChanged = parentStandard.LegalStandard != standard.LegalStandard;

                if (productsAdded || productsRemoved || exceptionsAdded || exceptionsRemoved || legalStatusChanged)
                {
                    versionType = "major";
                    isMajorChange = true;
                    major++;
                    minor = 0;
                    patch = 0;
                    newVersion = $"{major}.{minor}.{patch}";
                }
                else if (descriptionChanged)
                {
                    versionType = "patch";
                    patch++;
                    newVersion = $"{major}.{minor}.{patch}";
                }
                else if (otherChanged)
                {
                    versionType = "minor";
                    minor++;
                    patch = 0;
                    newVersion = $"{major}.{minor}.{patch}";
                }
                else
                {
                    versionType = "patch";
                    newVersion = IncrementVersion(previousVersion, versionType);
                }
            }
            else
            {
                versionType = DetermineVersionType(previousVersion);
                newVersion = IncrementVersion(previousVersion, versionType);
            }

            standard.PreviousVersion = previousVersion;
            standard.Version = newVersion;

            // Unpublish parent when child is approved
            if (parentStandard.Stage == "Published")
            {
                var audit = new DdtStandardUnpublishAudit
                {
                    DdtStandardId = parentStandard.Id,
                    Version = parentStandard.Version,
                    Reason = $"Replaced by new version {newVersion}",
                    UnpublishedByUserId = currentUserId ?? 0,
                    UnpublishedAt = now
                };
                _context.DdtStandardUnpublishAudits.Add(audit);

                parentStandard.Stage = "Unpublished";
                parentStandard.IsPublished = false;
                parentStandard.UpdatedAt = now;

                await CreateAuditLogAsync(parentStandard.Id, "Unpublished", 
                    $"Parent standard unpublished. Replaced by version {newVersion}");
            }

            if (isMajorChange)
            {
                standard.Stage = "For Approval";
                standard.GovernanceApproval = false;
                standard.UpdatedAt = now;

                await CreateAuditLogAsync(standard.Id, "Approved", 
                    $"Standard approved but requires re-review for MAJOR changes. Stage: For Approval → For Approval. Version: {previousVersion} → {newVersion} ({versionType})");
            }
            else
            {
                standard.Stage = "Awaiting Publication";
                standard.GovernanceApproval = true;
                standard.UpdatedAt = now;

                await CreateAuditLogAsync(standard.Id, "Approved", 
                    $"Standard approved. Stage: For Approval → Awaiting Publication. Version: {previousVersion} → {newVersion} ({versionType})");
            }
        }
        else
        {
            standard.Stage = "Awaiting Publication";
            standard.GovernanceApproval = true;
            standard.UpdatedAt = now;

            await CreateAuditLogAsync(standard.Id, "Approved", 
                "Standard approved. Stage: For Approval → Awaiting Publication");
        }

        // Add comment if provided
        if (!string.IsNullOrWhiteSpace(comment) && currentUserId.HasValue)
        {
            _context.DdtStandardComments.Add(new DdtStandardComment
            {
                DdtStandardId = standard.Id,
                UserId = currentUserId.Value,
                Title = "Approval comment",
                Comments = comment,
                CommentType = "approval",
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        await _context.SaveChangesAsync();

        if (hasParent)
        {
            TempData["SuccessMessage"] = "Standard approved successfully (replaced parent standard).";
        }
        else
        {
            TempData["SuccessMessage"] = "Standard approved. It can now be published by a Publisher.";
        }
        return RedirectToAction("Details", "DdtStandardsView", new { id });
    }

    /// <summary>
    /// Reject standard - only Standard Approvers can reject
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, string reason)
    {
        // Check permissions - must be able to approve standards (approvers can also reject)
        if (!await CanApproveStandardsAsync())
        {
            TempData["ErrorMessage"] = "You do not have permission to reject standards. Only Standard Approvers can reject.";
            return RedirectToAction("Details", "DdtStandardsView", new { id });
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["ErrorMessage"] = "Rejection reason is required.";
            return RedirectToAction("Details", "DdtStandardsView", new { id });
        }

        var standard = await _context.DdtStandards.FindAsync(id);
        if (standard == null || standard.IsDeleted)
        {
            return NotFound();
        }

        if (standard.Stage != "For Approval")
        {
            TempData["ErrorMessage"] = "Only standards awaiting approval can be rejected.";
            return RedirectToAction("Details", "DdtStandardsView", new { id });
        }

        var currentUserId = GetCurrentUserId();
        var now = DateTime.UtcNow;

        // Per user requirement: Set stage to "Draft" so it can be revised and resubmitted
        standard.Stage = "Draft";
        standard.UpdatedAt = now;

        // Add rejection comment with notes for the drafter
        if (currentUserId.HasValue)
        {
            _context.DdtStandardComments.Add(new DdtStandardComment
            {
                DdtStandardId = standard.Id,
                UserId = currentUserId.Value,
                Title = "Rejection reason",
                Comments = reason,
                CommentType = "rejection",
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        // Create audit log entry per spec 9.1
        await CreateAuditLogAsync(standard.Id, "Rejected", 
            $"Standard rejected. Stage: For Approval → Draft. Reason: {reason}");

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Standard rejected. The owner can revise and resubmit.";
        return RedirectToAction("Details", "DdtStandardsView", new { id });
    }

    /// <summary>
    /// Publish standard - only Standard Publishers can publish
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Publish(int id)
    {
        // Check permissions - must be able to publish standards
        if (!await CanPublishStandardsAsync())
        {
            TempData["ErrorMessage"] = "You do not have permission to publish standards. Only Standard Publishers can publish.";
            return RedirectToAction("Details", "DdtStandardsView", new { id });
        }

        var standard = await _context.DdtStandards
            .Include(s => s.Versions)
            .Include(s => s.Owners)
            .Include(s => s.ParentStandard)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

        if (standard == null)
        {
            return NotFound();
        }

        if (standard.Stage != "Awaiting Publication")
        {
            TempData["ErrorMessage"] = "Only standards awaiting publication can be published.";
            return RedirectToAction("Details", "DdtStandardsView", new { id });
        }

        var currentUserId = GetCurrentUserId();
        var now = DateTime.UtcNow;

        standard.Stage = "Published";
        standard.IsPublished = true;
        standard.PublishedAt = now;
        standard.FirstPublished ??= now;
        standard.LastUpdated = now;
        standard.UpdatedAt = now;
        standard.IsModified = false;

        // Per spec 4.2.1: Initial publication (0.1.0 → 1.0.0)
        var previousVersion = standard.Version;
        string newVersion;
        string versionType;

        if (previousVersion.StartsWith("0.") || string.IsNullOrWhiteSpace(previousVersion) || previousVersion == "0.1.0")
        {
            newVersion = "1.0.0";
            versionType = "major";
            standard.PreviousVersion = previousVersion;
        }
        else
        {
            newVersion = previousVersion;
            
            if (!string.IsNullOrEmpty(standard.PreviousVersion))
            {
                var prevParts = standard.PreviousVersion.Split('.');
                var currParts = newVersion.Split('.');
                if (prevParts.Length == 3 && currParts.Length == 3 &&
                    int.TryParse(prevParts[0], out var prevMajor) &&
                    int.TryParse(currParts[0], out var currMajor) &&
                    int.TryParse(prevParts[1], out var prevMinor) &&
                    int.TryParse(currParts[1], out var currMinor) &&
                    int.TryParse(prevParts[2], out var prevPatch) &&
                    int.TryParse(currParts[2], out var currPatch))
                {
                    if (currMajor > prevMajor)
                        versionType = "major";
                    else if (currMinor > prevMinor)
                        versionType = "minor";
                    else if (currPatch > prevPatch)
                        versionType = "patch";
                    else
                        versionType = "patch";
                }
                else
                {
                    versionType = "patch";
                }
            }
            else
            {
                versionType = "patch";
            }
            
            if (string.IsNullOrEmpty(standard.PreviousVersion))
            {
                standard.PreviousVersion = previousVersion;
            }
        }

        standard.Version = newVersion;

        // Create snapshot of standard at publication (per spec 10.1)
        var snapshot = JsonSerializer.Serialize(new
        {
            Id = standard.Id,
            Title = standard.Title,
            Summary = standard.Summary,
            Purpose = standard.Purpose,
            Criteria = standard.Criteria,
            HowToMeet = standard.HowToMeet,
            Governance = standard.Governance,
            Version = newVersion,
            PublishedAt = now
        }, new JsonSerializerOptions { WriteIndented = false });

        // Create version history entry per spec 10.2
        var version = new DdtStandardVersion
        {
            DdtStandardId = standard.Id,
            VersionNumber = newVersion,
            PreviousVersion = previousVersion,
            VersionType = versionType,
            ChangeSummary = previousVersion.StartsWith("0.") 
                ? "Initial publication" 
                : "Published standard",
            Status = "published",
            Snapshot = snapshot,
            CreatedByUserId = currentUserId,
            PublishedByUserId = currentUserId,
            CreatedAt = now,
            PublishedAt = now
        };

        _context.DdtStandardVersions.Add(version);

        // Per spec 3.2.2: If standard has a parent (created via "Make a Change"), 
        // unpublish parent when child is published
        if (standard.ParentStandardId.HasValue && standard.ParentStandard != null)
        {
            var parentStandard = standard.ParentStandard;
            
            if (parentStandard.Stage == "Published" && parentStandard.IsPublished)
            {
                var audit = new DdtStandardUnpublishAudit
                {
                    DdtStandardId = parentStandard.Id,
                    Version = parentStandard.Version,
                    Reason = $"Replaced by new version {newVersion}",
                    UnpublishedByUserId = currentUserId ?? 0,
                    UnpublishedAt = now
                };
                _context.DdtStandardUnpublishAudits.Add(audit);

                parentStandard.Stage = "Unpublished";
                parentStandard.IsPublished = false;
                parentStandard.UpdatedAt = now;

                await CreateAuditLogAsync(parentStandard.Id, "Unpublished", 
                    $"Parent standard unpublished. Replaced by version {newVersion}");
            }
        }

        // Create audit log entry per spec 9.1
        await CreateAuditLogAsync(standard.Id, "Published", 
            $"Standard published. Version: {previousVersion} → {newVersion} ({versionType})");

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Standard published successfully as version {newVersion}.";
        return RedirectToAction("Details", "DdtStandardsView", new { id });
    }

    /// <summary>
    /// Determine version type for increment
    /// </summary>
    private static string DetermineVersionType(string version)
    {
        return "patch";
    }

    /// <summary>
    /// Increment semantic version
    /// </summary>
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
    /// Manage approved products - view, add, edit, delete approved and tolerated products
    /// Only accessible by Standards Managers
    /// </summary>
    public async Task<IActionResult> ApprovedProducts()
    {
        // Check permissions - must be Standards Manager
        var userEmail = User.Identity?.Name 
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
            ?? string.Empty;
        
        if (string.IsNullOrEmpty(userEmail) || !await _permissionService.IsInGroupAsync(userEmail, "Standards Managers"))
        {
            TempData["ErrorMessage"] = "You do not have permission to manage approved products. You must be a Standards Manager.";
            return RedirectToAction("Index");
        }

        // Get all standard products
        var standardProducts = await _context.StandardProducts
            .AsNoTracking()
            .OrderBy(sp => sp.Name)
            .ToListAsync();

        // Get all DDT standard products (links between standards and products)
        var ddtStandardProducts = await _context.DdtStandardProducts
            .AsNoTracking()
            .Include(dsp => dsp.DdtStandard)
            .Include(dsp => dsp.StandardProduct)
            .ToListAsync();

        // Get all published standards for assigning products
        var standards = await _context.DdtStandards
            .AsNoTracking()
            .Where(s => !s.IsDeleted && s.IsPublished && s.Stage == "Published")
            .OrderBy(s => s.Title)
            .ToListAsync();

        // Get counts for navigation
        var allDraftsCount = await _context.DdtStandards
            .AsNoTracking()
            .CountAsync(s => !s.IsDeleted && s.Stage == "Draft");
        var allInReviewCount = await _context.DdtStandards
            .AsNoTracking()
            .CountAsync(s => !s.IsDeleted && s.Stage == "For Approval");
        var allForApprovalCount = await _context.DdtStandards
            .AsNoTracking()
            .CountAsync(s => !s.IsDeleted && s.Stage == "Awaiting Publication");
        var allPublishedCount = await _context.DdtStandards
            .AsNoTracking()
            .CountAsync(s => !s.IsDeleted && s.IsPublished && s.Stage == "Published");
        var allUnpublishedCount = await GetUnpublishedCountAsync();
        var approvedProductsCount = await _context.StandardProducts.CountAsync();
        var exceptionsCount = await _context.DdtStandardExceptions.CountAsync();

        ViewBag.StandardProducts = standardProducts;
        ViewBag.DdtStandardProducts = ddtStandardProducts;
        ViewBag.Standards = standards;
        ViewBag.AllDraftsCount = allDraftsCount;
        ViewBag.AllInReviewCount = allInReviewCount;
        ViewBag.AllForApprovalCount = allForApprovalCount;
        ViewBag.AllPublishedCount = allPublishedCount;
        ViewBag.AllUnpublishedCount = allUnpublishedCount;
        ViewBag.ApprovedProductsCount = approvedProductsCount;
        ViewBag.ExceptionsCount = exceptionsCount;

        return View("~/Views/DdtStandards/ApprovedProducts.cshtml");
    }

    /// <summary>
    /// Manage exceptions - view, add, edit, delete known exceptions to standards
    /// Only accessible by Standards Managers
    /// </summary>
    public async Task<IActionResult> Exceptions()
    {
        // Check permissions - must be Standards Manager
        var userEmail = User.Identity?.Name 
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
            ?? string.Empty;
        
        if (string.IsNullOrEmpty(userEmail) || !await _permissionService.IsInGroupAsync(userEmail, "Standards Managers"))
        {
            TempData["ErrorMessage"] = "You do not have permission to manage exceptions. You must be a Standards Manager.";
            return RedirectToAction("Index");
        }

        // Get all exceptions
        var exceptions = await _context.DdtStandardExceptions
            .AsNoTracking()
            .Include(e => e.DdtStandard)
            .Include(e => e.StandardProduct)
            .Include(e => e.GrantedByUser)
            .OrderByDescending(e => e.GrantedAt)
            .ToListAsync();

        // Get all published standards for creating exceptions
        var standards = await _context.DdtStandards
            .AsNoTracking()
            .Where(s => !s.IsDeleted && s.IsPublished && s.Stage == "Published")
            .OrderBy(s => s.Title)
            .ToListAsync();

        // Get all standard products for creating exceptions
        var standardProducts = await _context.StandardProducts
            .AsNoTracking()
            .OrderBy(sp => sp.Name)
            .ToListAsync();

        // Get counts for navigation
        var allDraftsCount = await _context.DdtStandards
            .AsNoTracking()
            .CountAsync(s => !s.IsDeleted && s.Stage == "Draft");
        var allInReviewCount = await _context.DdtStandards
            .AsNoTracking()
            .CountAsync(s => !s.IsDeleted && s.Stage == "For Approval");
        var allForApprovalCount = await _context.DdtStandards
            .AsNoTracking()
            .CountAsync(s => !s.IsDeleted && s.Stage == "Awaiting Publication");
        var allPublishedCount = await _context.DdtStandards
            .AsNoTracking()
            .CountAsync(s => !s.IsDeleted && s.IsPublished && s.Stage == "Published");
        var allUnpublishedCount = await GetUnpublishedCountAsync();
        var approvedProductsCount = await _context.StandardProducts.CountAsync();
        var exceptionsCount = await _context.DdtStandardExceptions.CountAsync();

        ViewBag.Standards = standards;
        ViewBag.StandardProducts = standardProducts;
        ViewBag.AllDraftsCount = allDraftsCount;
        ViewBag.AllInReviewCount = allInReviewCount;
        ViewBag.AllForApprovalCount = allForApprovalCount;
        ViewBag.AllPublishedCount = allPublishedCount;
        ViewBag.AllUnpublishedCount = allUnpublishedCount;
        ViewBag.ApprovedProductsCount = approvedProductsCount;
        ViewBag.ExceptionsCount = exceptionsCount;

        return View("~/Views/DdtStandards/Exceptions.cshtml", exceptions);
    }

    /// <summary>
    /// Create a new standard product
    /// Only accessible by Standards Managers
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateProduct(string name, string? description, string? provider, string? version, string approvalStatus)
    {
        // Check permissions
        var userEmail = User.Identity?.Name 
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
            ?? string.Empty;
        
        if (string.IsNullOrEmpty(userEmail) || !await _permissionService.IsInGroupAsync(userEmail, "Standards Managers"))
        {
            TempData["ErrorMessage"] = "You do not have permission to create products. You must be a Standards Manager.";
            return RedirectToAction("ApprovedProducts");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["ErrorMessage"] = "Product name is required.";
            return RedirectToAction("ApprovedProducts");
        }

        var currentUserId = GetCurrentUserId();

        var product = new StandardProduct
        {
            Name = name.Trim(),
            Description = description?.Trim(),
            Provider = provider?.Trim(),
            Version = version?.Trim(),
            ApprovalStatus = approvalStatus ?? "Pending",
            CreatedByUserId = currentUserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.StandardProducts.Add(product);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Product '{product.Name}' created successfully.";
        return RedirectToAction("ApprovedProducts");
    }

    /// <summary>
    /// Update an existing standard product
    /// Only accessible by Standards Managers
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProduct(int id, string name, string? description, string? provider, string? version, string approvalStatus)
    {
        // Check permissions
        var userEmail = User.Identity?.Name 
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
            ?? string.Empty;
        
        if (string.IsNullOrEmpty(userEmail) || !await _permissionService.IsInGroupAsync(userEmail, "Standards Managers"))
        {
            TempData["ErrorMessage"] = "You do not have permission to update products. You must be a Standards Manager.";
            return RedirectToAction("ApprovedProducts");
        }

        var product = await _context.StandardProducts.FindAsync(id);
        if (product == null)
        {
            TempData["ErrorMessage"] = "Product not found.";
            return RedirectToAction("ApprovedProducts");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["ErrorMessage"] = "Product name is required.";
            return RedirectToAction("ApprovedProducts");
        }

        product.Name = name.Trim();
        product.Description = description?.Trim();
        product.Provider = provider?.Trim();
        product.Version = version?.Trim();
        product.ApprovalStatus = approvalStatus ?? "Pending";
        product.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Product '{product.Name}' updated successfully.";
        return RedirectToAction("ApprovedProducts");
    }

    /// <summary>
    /// Assign a product to a standard (as approved or tolerated)
    /// Only accessible by Standards Managers
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignProductToStandard(int productId, int standardId, string productType, string? notes)
    {
        // Check permissions
        var userEmail = User.Identity?.Name 
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
            ?? string.Empty;
        
        if (string.IsNullOrEmpty(userEmail) || !await _permissionService.IsInGroupAsync(userEmail, "Standards Managers"))
        {
            TempData["ErrorMessage"] = "You do not have permission to assign products. You must be a Standards Manager.";
            return RedirectToAction("ApprovedProducts");
        }

        // Validate product type
        if (productType != "Approved" && productType != "Tolerated")
        {
            TempData["ErrorMessage"] = "Product type must be either 'Approved' or 'Tolerated'.";
            return RedirectToAction("ApprovedProducts");
        }

        // Check if assignment already exists
        var existing = await _context.DdtStandardProducts
            .FirstOrDefaultAsync(dsp => dsp.DdtStandardId == standardId && dsp.StandardProductId == productId);

        if (existing != null)
        {
            // Update existing assignment
            existing.ProductType = productType;
            existing.Notes = notes?.Trim();
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            // Create new assignment
            var assignment = new DdtStandardProduct
            {
                DdtStandardId = standardId,
                StandardProductId = productId,
                ProductType = productType,
                Notes = notes?.Trim(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.DdtStandardProducts.Add(assignment);
        }

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Product assigned to standard successfully.";
        return RedirectToAction("ApprovedProducts");
    }

    /// <summary>
    /// Create a new exception
    /// Only accessible by Standards Managers
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
        // Check permissions
        var userEmail = User.Identity?.Name 
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
            ?? string.Empty;
        
        if (string.IsNullOrEmpty(userEmail) || !await _permissionService.IsInGroupAsync(userEmail, "Standards Managers"))
        {
            TempData["ErrorMessage"] = "You do not have permission to create exceptions. You must be a Standards Manager.";
            return RedirectToAction("Exceptions");
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            TempData["ErrorMessage"] = "Exception title is required.";
            return RedirectToAction("Exceptions");
        }

        var currentUserId = GetCurrentUserId();

        var exception = new DdtStandardException
        {
            Title = title.Trim(),
            DdtStandardId = standardId,
            Description = description?.Trim(),
            Reason = reason?.Trim(),
            StandardProductId = productId,
            FipsProductId = fipsProductId?.Trim(),
            GrantedAt = grantedAt,
            ExpiresAt = expiresAt,
            Status = status ?? "Active",
            Notes = notes?.Trim(),
            GrantedByUserId = currentUserId,
            CreatedByUserId = currentUserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.DdtStandardExceptions.Add(exception);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Exception '{exception.Title}' created successfully.";
        return RedirectToAction("Exceptions");
    }

    /// <summary>
    /// Update an existing exception
    /// Only accessible by Standards Managers
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
        // Check permissions
        var userEmail = User.Identity?.Name 
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
            ?? string.Empty;
        
        if (string.IsNullOrEmpty(userEmail) || !await _permissionService.IsInGroupAsync(userEmail, "Standards Managers"))
        {
            TempData["ErrorMessage"] = "You do not have permission to update exceptions. You must be a Standards Manager.";
            return RedirectToAction("Exceptions");
        }

        var exception = await _context.DdtStandardExceptions.FindAsync(id);
        if (exception == null)
        {
            TempData["ErrorMessage"] = "Exception not found.";
            return RedirectToAction("Exceptions");
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            TempData["ErrorMessage"] = "Exception title is required.";
            return RedirectToAction("Exceptions");
        }

        exception.Title = title.Trim();
        exception.DdtStandardId = standardId;
        exception.Description = description?.Trim();
        exception.Reason = reason?.Trim();
        exception.StandardProductId = productId;
        exception.FipsProductId = fipsProductId?.Trim();
        exception.GrantedAt = grantedAt;
        exception.ExpiresAt = expiresAt;
        exception.Status = status ?? "Active";
        exception.Notes = notes?.Trim();
        exception.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Exception '{exception.Title}' updated successfully.";
        return RedirectToAction("Exceptions");
    }

    /// <summary>
    /// Delete an exception
    /// Only accessible by Standards Managers
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteException(int id)
    {
        // Check permissions
        var userEmail = User.Identity?.Name 
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
            ?? string.Empty;
        
        if (string.IsNullOrEmpty(userEmail) || !await _permissionService.IsInGroupAsync(userEmail, "Standards Managers"))
        {
            TempData["ErrorMessage"] = "You do not have permission to delete exceptions. You must be a Standards Manager.";
            return RedirectToAction("Exceptions");
        }

        var exception = await _context.DdtStandardExceptions.FindAsync(id);
        if (exception == null)
        {
            TempData["ErrorMessage"] = "Exception not found.";
            return RedirectToAction("Exceptions");
        }

        _context.DdtStandardExceptions.Remove(exception);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Exception '{exception.Title}' deleted successfully.";
        return RedirectToAction("Exceptions");
    }

    /// <summary>
    /// Unpublish standard - GET
    /// Shows confirmation page for unpublishing
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Unpublish(int id)
    {
        // Check permissions - must be able to publish/unpublish standards
        if (!await CanPublishStandardsAsync())
        {
            TempData["ErrorMessage"] = "You do not have permission to unpublish standards. Only Standard Publishers can unpublish.";
            return RedirectToAction("Details", "DdtStandardsView", new { id });
        }

        var standard = await _context.DdtStandards
            .Include(s => s.Owners).ThenInclude(o => o.User)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

        if (standard == null)
        {
            return NotFound();
        }

        if (!standard.IsPublished || standard.Stage != "Published")
        {
            TempData["ErrorMessage"] = "Only published standards can be unpublished.";
            return RedirectToAction("Details", "DdtStandardsView", new { id });
        }

        ViewBag.Standard = standard;
        return View("~/Views/DdtStandards/Unpublish.cshtml", standard);
    }

    /// <summary>
    /// Unpublish standard - POST
    /// Unpublishes a published standard
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unpublish(int id, string reason)
    {
        // Check permissions - must be able to publish/unpublish standards
        if (!await CanPublishStandardsAsync())
        {
            TempData["ErrorMessage"] = "You do not have permission to unpublish standards. Only Standard Publishers can unpublish.";
            return RedirectToAction("Details", "DdtStandardsView", new { id });
        }

        var standard = await _context.DdtStandards
            .Include(s => s.ParentStandard)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

        if (standard == null)
        {
            return NotFound();
        }

        if (!standard.IsPublished || standard.Stage != "Published")
        {
            TempData["ErrorMessage"] = "Only published standards can be unpublished.";
            return RedirectToAction("Details", "DdtStandardsView", new { id });
        }

        var currentUserId = GetCurrentUserId();
        var now = DateTime.UtcNow;

        // Create unpublish audit entry
        var audit = new DdtStandardUnpublishAudit
        {
            DdtStandardId = standard.Id,
            Version = standard.Version,
            Reason = reason?.Trim() ?? "No reason provided",
            UnpublishedByUserId = currentUserId ?? 0,
            UnpublishedAt = now
        };
        _context.DdtStandardUnpublishAudits.Add(audit);

        // Update standard status
        standard.Stage = "Unpublished";
        standard.IsPublished = false;
        standard.UpdatedAt = now;

        // Create audit log entry
        await CreateAuditLogAsync(standard.Id, "Unpublished", 
            $"Standard unpublished. Reason: {reason?.Trim() ?? "No reason provided"}");

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Standard '{standard.Title}' unpublished successfully.";
        return RedirectToAction("Details", "DdtStandardsView", new { id });
    }

    /// <summary>
    /// Make a change - create a new draft from a published standard
    /// Creates a new draft standard based on a published standard (for making changes)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MakeChange(int id)
    {
        // Check permissions - must be able to draft standards
        if (!await CanDraftStandardsAsync())
        {
            TempData["ErrorMessage"] = "You do not have permission to create draft standards.";
            return RedirectToAction("Details", "DdtStandardsView", new { id });
        }

        var sourceStandard = await _context.DdtStandards
            .Include(s => s.Owners)
            .Include(s => s.Contacts)
            .Include(s => s.Categories)
            .Include(s => s.SubCategories)
            .Include(s => s.Phases)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted && s.IsPublished && s.Stage == "Published");

        if (sourceStandard == null)
        {
            TempData["ErrorMessage"] = "Published standard not found.";
            return RedirectToAction("Published", "DdtStandardsView");
        }

        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            TempData["ErrorMessage"] = "Unable to identify current user.";
            return RedirectToAction("Details", "DdtStandardsView", new { id });
        }

        var now = DateTime.UtcNow;

        // Create new draft standard based on published one
        var newStandard = new DdtStandard
        {
            Title = sourceStandard.Title,
            Slug = GenerateSlug(sourceStandard.Title),
            Summary = sourceStandard.Summary,
            Purpose = sourceStandard.Purpose,
            Criteria = sourceStandard.Criteria,
            HowToMeet = sourceStandard.HowToMeet,
            Governance = sourceStandard.Governance,
            LegalBasis = sourceStandard.LegalBasis,
            LegalStandard = sourceStandard.LegalStandard,
            ValidityPeriod = sourceStandard.ValidityPeriod,
            RelatedGuidance = sourceStandard.RelatedGuidance,
            Stage = "Draft",
            Version = "0.1.0",
            PreviousVersion = sourceStandard.Version,
            IsPublished = false,
            ParentStandardId = sourceStandard.Id,
            CreatorUserId = currentUserId,
            CreatedAt = now,
            UpdatedAt = now
        };

        _context.DdtStandards.Add(newStandard);
        await _context.SaveChangesAsync(); // Save to get the ID

        // Copy owners
        foreach (var owner in sourceStandard.Owners)
        {
            newStandard.Owners.Add(new DdtStandardOwner
            {
                DdtStandardId = newStandard.Id,
                UserId = owner.UserId
            });
        }

        // Copy contacts
        foreach (var contact in sourceStandard.Contacts)
        {
            newStandard.Contacts.Add(new DdtStandardContact
            {
                DdtStandardId = newStandard.Id,
                UserId = contact.UserId
            });
        }

        // Copy categories
        foreach (var category in sourceStandard.Categories)
        {
            newStandard.Categories.Add(new DdtStandardCategory
            {
                DdtStandardId = newStandard.Id,
                CategoryId = category.CategoryId
            });
        }

        // Copy sub-categories
        foreach (var subCategory in sourceStandard.SubCategories)
        {
            newStandard.SubCategories.Add(new DdtStandardSubCategory
            {
                DdtStandardId = newStandard.Id,
                SubCategoryId = subCategory.SubCategoryId
            });
        }

        // Copy phases
        foreach (var phase in sourceStandard.Phases.Where(p => p.Enabled))
        {
            newStandard.Phases.Add(new DdtStandardPhase
            {
                DdtStandardId = newStandard.Id,
                PhaseLookupId = phase.PhaseLookupId,
                Enabled = true
            });
        }

        // Generate legacy reference
        newStandard.LegacyReference = await GenerateLegacyReferenceAsync(parentStandardId: sourceStandard.Id);

        await _context.SaveChangesAsync();

        // Create audit log entry
        await CreateAuditLogAsync(newStandard.Id, "Created", 
            $"Draft created from published standard '{sourceStandard.Title}' (version {sourceStandard.Version})");

        TempData["SuccessMessage"] = $"New draft created from '{sourceStandard.Title}'. You can now make changes.";
        return RedirectToAction("Create", new { id = newStandard.Id });
    }
}
