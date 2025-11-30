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
    /// </summary>
    public async Task<IActionResult> Dashboard()
    {
        var currentUserId = GetCurrentUserId();
        var isStandardsManager = await IsStandardsManagerAsync();
        
        // Get counts for all stages
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
        
        // Get counts for approved products and exceptions
        var approvedProductsCount = await _context.StandardProducts.CountAsync();
        var exceptionsCount = await _context.DdtStandardExceptions.CountAsync();
        
        ViewBag.AllDraftsCount = allDraftsCount;
        ViewBag.AllInReviewCount = allInReviewCount;
        ViewBag.AllForApprovalCount = allForApprovalCount;
        ViewBag.AllPublishedCount = allPublishedCount;
        ViewBag.AllUnpublishedCount = allUnpublishedCount;
        ViewBag.ApprovedProductsCount = approvedProductsCount;
        ViewBag.ExceptionsCount = exceptionsCount;
        ViewBag.IsStandardsManager = isStandardsManager;
        
        // Get all published standards for the list (exclude unpublished)
        var allStandards = await _context.DdtStandards
            .AsNoTracking()
            .Include(s => s.CreatorUser)
            .Include(s => s.Owners).ThenInclude(o => o.User)
            .Include(s => s.Categories).ThenInclude(c => c.Category)
            .Where(s => !s.IsDeleted && s.Stage != "Unpublished")
            .OrderByDescending(s => s.UpdatedAt)
            .ToListAsync();
        
        ViewBag.AllStandards = allStandards;
        
        if (isStandardsManager)
        {
            // For Standards Managers: Get standards requiring approval
            var standardsRequiringApproval = await _context.DdtStandards
                .AsNoTracking()
                .Include(s => s.CreatorUser)
                .Include(s => s.Owners).ThenInclude(o => o.User)
                .Include(s => s.Categories).ThenInclude(c => c.Category)
                .Where(s => !s.IsDeleted && (s.Stage == "For Approval" || s.Stage == "Awaiting Publication"))
                .OrderByDescending(s => s.UpdatedAt)
                .Take(20)
                .ToListAsync();
            
            ViewBag.StandardsRequiringApproval = standardsRequiringApproval;
        }
        else
        {
            // For regular users: Get new and recently updated standards
            var newStandards = await _context.DdtStandards
                .AsNoTracking()
                .Include(s => s.CreatorUser)
                .Include(s => s.Categories).ThenInclude(c => c.Category)
                .Where(s => !s.IsDeleted && s.CreatedAt >= DateTime.UtcNow.AddDays(-30))
                .OrderByDescending(s => s.CreatedAt)
                .Take(10)
                .ToListAsync();
            
            var recentlyUpdated = await _context.DdtStandards
                .AsNoTracking()
                .Include(s => s.CreatorUser)
                .Include(s => s.Categories).ThenInclude(c => c.Category)
                .Where(s => !s.IsDeleted && s.UpdatedAt >= DateTime.UtcNow.AddDays(-30) && s.CreatedAt < DateTime.UtcNow.AddDays(-30))
                .OrderByDescending(s => s.UpdatedAt)
                .Take(10)
                .ToListAsync();
            
            ViewBag.NewStandards = newStandards;
            ViewBag.RecentlyUpdated = recentlyUpdated;
        }
        
        return View();
    }

    /// <summary>
    /// Drafts view - list all drafts (moved from Index)
    /// </summary>
    public async Task<IActionResult> Drafts(string? search, string? category, int? creator, int? owner, int? contact, bool? legalStandard)
    {
        var currentUserId = GetCurrentUserId();
        var activeView = "drafts";
        var activeStageName = "Draft";
        
        var isStandardsManager = await IsStandardsManagerAsync();
        var loadFullDataForActive = isStandardsManager;
        
        // Get standards for drafts with full data if needed
        var (myDrafts, allDrafts) = await GetStandardsByStageAsync(
            activeStageName, currentUserId, search, category, creator, owner, contact, legalStandard, loadFullDataForActive);
        
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
        ViewBag.IsStandardsManager = isStandardsManager;

        // Add data for edit modal (only if user is Standards Manager)
        if (isStandardsManager)
        {
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
        }

        return View(viewModel);
    }

    /// <summary>
    /// Manage standards - list standards by stage (in review, for approval, published, unpublished)
    /// Drafts now has its own view. This handles other stages.
    /// </summary>
    public async Task<IActionResult> Index(string? view, string? search, string? category, int? creator, int? owner, int? contact, bool? legalStandard)
    {
        // If no view specified, redirect to Dashboard
        if (string.IsNullOrEmpty(view))
        {
            return RedirectToAction(nameof(Dashboard));
        }
        
        // If view is "drafts", redirect to Drafts action
        if (view == "drafts")
        {
            return RedirectToAction(nameof(Drafts), new { search, category, creator, owner, contact, legalStandard });
        }
        
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
        
        // Only load full data for the active view (for edit modal support)
        var isStandardsManager = await IsStandardsManagerAsync();
        var loadFullDataForActive = isStandardsManager;
        
        // Get standards for active stage with full data if needed
        var (myActive, allActive) = await GetStandardsByStageAsync(
            activeStageName, currentUserId, search, category, creator, owner, contact, legalStandard, loadFullDataForActive);
        
        // Get counts only for other stages (much faster - no includes)
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
        
        // For unpublished count, get only latest version per standard
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

        // Get filter options (optimized with AsNoTracking and direct selects)
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

        // Get creators (users who have created standards) - optimized
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

        // Get owners (users who are owners of standards) - optimized
        var owners = await _context.DdtStandardOwners
            .AsNoTracking()
            .Join(_context.Users,
                o => o.UserId,
                u => u.Id,
                (o, u) => new { o.UserId, u.Name })
            .Distinct()
            .OrderBy(x => x.Name)
            .ToListAsync();

        // Get contacts (users who are contacts for standards) - optimized
        var contacts = await _context.DdtStandardContacts
            .AsNoTracking()
            .Join(_context.Users,
                c => c.UserId,
                u => u.Id,
                (c, u) => new { c.UserId, u.Name })
            .Distinct()
            .OrderBy(x => x.Name)
            .ToListAsync();

        // Get counts for Standards Managers section
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

        // Get recent changes (standards updated in last 30 days)
        var recentChanges = await _context.DdtStandards
            .AsNoTracking()
            .Include(s => s.CreatorUser)
            .Include(s => s.Categories).ThenInclude(c => c.Category)
            .Where(s => !s.IsDeleted && s.UpdatedAt >= DateTime.UtcNow.AddDays(-30))
            .OrderByDescending(s => s.UpdatedAt)
            .Take(20)
            .ToListAsync();

        // Get recently published standards (published in last 30 days)
        var recentPublished = await _context.DdtStandards
            .AsNoTracking()
            .Include(s => s.CreatorUser)
            .Include(s => s.Categories).ThenInclude(c => c.Category)
            .Where(s => !s.IsDeleted && s.IsPublished && s.Stage == "Published" && s.PublishedAt.HasValue && s.PublishedAt.Value >= DateTime.UtcNow.AddDays(-30))
            .OrderByDescending(s => s.PublishedAt)
            .Take(20)
            .ToListAsync();

        ViewBag.RecentChanges = recentChanges;
        ViewBag.RecentPublished = recentPublished;

        // Add data for edit modal (only if user is Standards Manager)
        if (isStandardsManager)
        {
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
        }

        return View(viewModel);
    }

    /// <summary>
    /// Published standards - public view of published standards
    /// </summary>
    public async Task<IActionResult> Published(string? search, string? category)
    {
        var currentUserId = GetCurrentUserId();

        // Base query for all published standards (optimized with AsNoTracking)
        var allQuery = _context.DdtStandards
            .AsNoTracking()
            .Include(s => s.CreatorUser)
            .Include(s => s.Owners).ThenInclude(o => o.User)
            .Include(s => s.Contacts).ThenInclude(c => c.User)
            .Include(s => s.Categories).ThenInclude(c => c.Category)
            .Where(s => !s.IsDeleted && s.IsPublished && s.Stage == "Published")
            .AsQueryable();

        // Query for my published standards (where user is owner or contact)
        var myQuery = _context.DdtStandards
            .AsNoTracking()
            .Include(s => s.CreatorUser)
            .Include(s => s.Owners).ThenInclude(o => o.User)
            .Include(s => s.Contacts).ThenInclude(c => c.User)
            .Include(s => s.Categories).ThenInclude(c => c.Category)
            .Where(s => !s.IsDeleted && s.IsPublished && s.Stage == "Published")
            .AsQueryable();

        if (currentUserId.HasValue)
        {
            // My published: standards where user is owner OR contact
            myQuery = myQuery.Where(s => 
                s.Owners.Any(o => o.UserId == currentUserId.Value) ||
                s.Contacts.Any(c => c.UserId == currentUserId.Value));
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

        var allPublished = await allQuery
            .OrderByDescending(s => s.PublishedAt)
            .ToListAsync();

        var myPublished = await myQuery
            .OrderByDescending(s => s.PublishedAt)
            .ToListAsync();

        var categories = await _context.DdtStandardCategories
            .Include(c => c.Category)
            .Select(c => c.Category.Name)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();

        ViewBag.CurrentSearch = search;
        ViewBag.CurrentCategory = category;
        ViewBag.Categories = categories;
        ViewBag.MyPublished = myPublished;
        ViewBag.AllPublished = allPublished;
        
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

        return View(allPublished);
    }

    /// <summary>
    /// Create new standard - GET (or edit draft if id provided)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Create(int? id)
    {
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
        // Select only needed properties to avoid circular reference issues during JSON serialization
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
                return RedirectToAction(nameof(Details), new { id = id.Value });
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

            // Check permissions
            var currentUserId = GetCurrentUserId();
            var isOwner = standard.Owners.Any(o => o.UserId == currentUserId);
            var isCreator = standard.CreatorUserId == currentUserId;
            var isAdmin = User.IsCompassAdmin();

            if (!isOwner && !isCreator && !isAdmin)
            {
                TempData["ErrorMessage"] = "You do not have permission to edit this standard.";
                return RedirectToAction(nameof(Details), new { id = id.Value });
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

        return View();
    }

    /// <summary>
    /// Create new standard - POST
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
                    return RedirectToAction(nameof(Details), new { id = id.Value });
                }

                // Check permissions
                var isOwner = standard.Owners.Any(o => o.UserId == currentUserId);
                var isCreator = standard.CreatorUserId == currentUserId;
                var isAdmin = User.IsCompassAdmin();

                if (!isOwner && !isCreator && !isAdmin)
                {
                    TempData["ErrorMessage"] = "You do not have permission to edit this standard.";
                    return RedirectToAction(nameof(Details), new { id = id.Value });
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
        // Only autosave if there's at least a title
        if (string.IsNullOrWhiteSpace(title))
        {
            return Json(new { success = false, message = "Title is required" });
        }

        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            return Json(new { success = false, message = "Unable to identify current user" });
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
                    return Json(new { success = false, message = "Standard not found" });
                }

                if (standard.Stage != "Draft")
                {
                    return Json(new { success = false, message = "Only draft standards can be autosaved" });
                }

                // Check permissions
                var isOwner = standard.Owners.Any(o => o.UserId == currentUserId);
                var isCreator = standard.CreatorUserId == currentUserId;
                var isAdmin = User.IsCompassAdmin();

                if (!isOwner && !isCreator && !isAdmin)
                {
                    return Json(new { success = false, message = "You do not have permission to edit this standard" });
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

            // Add categories, sub-categories, phases, owners, contacts (same logic as Create POST)
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

            // Add owners
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

            // Add contacts
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

            return Json(new { success = true, id = standard.Id, message = "Draft saved" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error autosaving standard");
            return Json(new { success = false, message = "An error occurred while saving" });
        }
    }

    /// <summary>
    /// View standard details
    /// </summary>
    public async Task<IActionResult> Details(int id, bool showAllVersions = false)
    {
        var standard = await _context.DdtStandards
            .AsNoTracking() // Read-only view, no need to track changes
            .Include(s => s.CreatorUser)
            .Include(s => s.Owners).ThenInclude(o => o.User)
            .Include(s => s.Contacts).ThenInclude(c => c.User)
            .Include(s => s.Categories).ThenInclude(c => c.Category)
            .Include(s => s.SubCategories).ThenInclude(sc => sc.SubCategory)
            .Include(s => s.Phases).ThenInclude(p => p.PhaseLookup)
            .Include(s => s.ValidationRules)
            .Include(s => s.Comments).ThenInclude(c => c.User)
            .Include(s => s.Versions).ThenInclude(v => v.CreatedByUser)
            .Include(s => s.Products).ThenInclude(p => p.StandardProduct)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

        if (standard == null)
        {
            return NotFound();
        }

        // Get related standards (published standards with same categories)
        var categoryIds = standard.Categories.Select(c => c.CategoryId).ToList();
        var relatedStandards = new List<DdtStandard>();
        
        if (categoryIds.Any())
        {
            relatedStandards = await _context.DdtStandards
                .AsNoTracking() // Read-only, no need to track
                .Include(s => s.Categories).ThenInclude(c => c.Category)
                .Where(s => !s.IsDeleted 
                    && s.IsPublished 
                    && s.Stage == "Published"
                    && s.Id != id // Exclude current standard
                    && s.Categories.Any(c => categoryIds.Contains(c.CategoryId)))
                .OrderByDescending(s => s.PublishedAt)
                .Take(10) // Limit to 10 related standards
                .ToListAsync();
        }

        ViewBag.RelatedStandards = relatedStandards;
        ViewBag.CurrentUserId = GetCurrentUserId();
        
        // Get rejection comments for this standard
        var rejectionComments = await _context.DdtStandardComments
            .AsNoTracking()
            .Include(c => c.User)
            .Where(c => c.DdtStandardId == id && c.CommentType == "rejection")
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
        ViewBag.RejectionComments = rejectionComments;
        
        // Check if user is Standards Manager
        bool isStandardsManager = false;
        try
        {
            var userEmail = User.Identity?.Name 
                ?? User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                ?? string.Empty;
            if (!string.IsNullOrEmpty(userEmail))
            {
                isStandardsManager = await _permissionService.IsInGroupAsync(userEmail, "Standards Managers");
            }
        }
        catch
        {
            // Non-blocking
        }
        ViewBag.IsStandardsManager = isStandardsManager;
        
        // Check if user is Admin/SuperAdmin (for publishing)
        ViewBag.IsAdmin = User.IsInRole("Admin") || User.IsInRole("SuperAdmin");
        
        // Get previous versions of this standard
        // This includes:
        // 1. Standards with same title (different versions)
        // 2. Parent standard (if this is a child created via "Make a Change")
        // 3. Child standards (if this is a parent that was unpublished)
        var relatedStandardIds = new List<int>();
        
        // If this standard has a parent, include the parent
        if (standard.ParentStandardId.HasValue)
        {
            relatedStandardIds.Add(standard.ParentStandardId.Value);
        }
        
        // Find all children of this standard (standards that have this as parent)
        var childIds = await _context.DdtStandards
            .AsNoTracking()
            .Where(s => !s.IsDeleted && s.ParentStandardId == id)
            .Select(s => s.Id)
            .ToListAsync();
        relatedStandardIds.AddRange(childIds);
        
        // Find standards with same title
        var sameTitleIds = await _context.DdtStandards
            .AsNoTracking()
            .Where(s => !s.IsDeleted && s.Id != id && s.Title == standard.Title)
            .Select(s => s.Id)
            .ToListAsync();
        relatedStandardIds.AddRange(sameTitleIds);
        
        // Get all related standards (distinct, ordered by version)
        var previousVersions = await _context.DdtStandards
            .AsNoTracking()
            .Include(s => s.CreatorUser)
            .Include(s => s.Products).ThenInclude(p => p.StandardProduct)
            .Where(s => relatedStandardIds.Distinct().Contains(s.Id))
            .ToListAsync();
        
        // Order in memory since TryParseVersion can't be translated to SQL
        previousVersions = previousVersions
            .OrderByDescending(s => 
            {
                var version = TryParseVersion(s.Version);
                return version ?? new Version(0, 0, 0);
            })
            .ThenByDescending(s => s.UpdatedAt)
            .ToList();
        
        // Get the published standard for comparison (if current standard is published, use it; otherwise find the latest published version)
        DdtStandard? publishedStandard = null;
        if (standard.Stage == "Published")
        {
            publishedStandard = standard;
        }
        else
        {
            publishedStandard = previousVersions.FirstOrDefault(s => s.Stage == "Published");
        }
        
        ViewBag.PreviousVersions = previousVersions;
        ViewBag.PublishedStandard = publishedStandard;
        ViewBag.ShowAllVersions = showAllVersions;
        
        // If viewing a previous version (not the published one), prepare comparison data
        if (publishedStandard != null && standard.Id != publishedStandard.Id)
        {
            // Load products for published standard if not already loaded
            if (publishedStandard.Products == null || !publishedStandard.Products.Any())
            {
                publishedStandard.Products = await _context.DdtStandardProducts
                    .AsNoTracking()
                    .Include(p => p.StandardProduct)
                    .Where(p => p.DdtStandardId == publishedStandard.Id)
                    .ToListAsync();
            }
            
            // Compare this version with the published version
            var changes = new Dictionary<string, object>();
            
            // Compare basic fields
            if (standard.Title != publishedStandard.Title)
                changes["Title"] = new { Old = publishedStandard.Title, New = standard.Title };
            if (standard.Summary != publishedStandard.Summary)
                changes["Summary"] = new { Old = publishedStandard.Summary, New = standard.Summary };
            if (standard.Purpose != publishedStandard.Purpose)
                changes["Purpose"] = new { Old = publishedStandard.Purpose, New = standard.Purpose };
            if (standard.HowToMeet != publishedStandard.HowToMeet)
                changes["HowToMeet"] = new { Old = publishedStandard.HowToMeet, New = standard.HowToMeet };
            if (standard.Criteria != publishedStandard.Criteria)
                changes["Criteria"] = new { Old = publishedStandard.Criteria, New = standard.Criteria };
            if (standard.Governance != publishedStandard.Governance)
                changes["Governance"] = new { Old = publishedStandard.Governance, New = standard.Governance };
            if (standard.LegalBasis != publishedStandard.LegalBasis)
                changes["LegalBasis"] = new { Old = publishedStandard.LegalBasis, New = standard.LegalBasis };
            if (standard.RelatedGuidance != publishedStandard.RelatedGuidance)
                changes["RelatedGuidance"] = new { Old = publishedStandard.RelatedGuidance, New = standard.RelatedGuidance };
            if (standard.LegalStandard != publishedStandard.LegalStandard)
                changes["LegalStandard"] = new { Old = publishedStandard.LegalStandard, New = standard.LegalStandard };
            
            // Compare products
            var publishedProductIds = publishedStandard.Products?.Select(p => p.StandardProductId).OrderBy(x => x).ToList() ?? new List<int>();
            var currentProductIds = standard.Products?.Select(p => p.StandardProductId).OrderBy(x => x).ToList() ?? new List<int>();
            if (!publishedProductIds.SequenceEqual(currentProductIds))
            {
                var addedProducts = currentProductIds.Except(publishedProductIds).ToList();
                var removedProducts = publishedProductIds.Except(currentProductIds).ToList();
                changes["Products"] = new { Added = addedProducts, Removed = removedProducts };
            }
            
            ViewBag.Changes = changes;
            ViewBag.PublishedStandardForComparison = publishedStandard;
        }
        
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

        return View(standard);
    }

    /// <summary>
    /// Unpublish standard - GET
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Unpublish(int id)
    {
        var standard = await _context.DdtStandards
            .Include(s => s.Owners).ThenInclude(o => o.User)
            .Include(s => s.Contacts).ThenInclude(c => c.User)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

        if (standard == null)
        {
            return NotFound();
        }

        if (standard.Stage != "Published")
        {
            TempData["ErrorMessage"] = "Only published standards can be unpublished.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // Check permissions
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            TempData["ErrorMessage"] = "You must be signed in to perform this action.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var isStandardsManager = await IsStandardsManagerAsync();
        var isOwner = standard.Owners.Any(o => o.UserId == currentUserId.Value);
        var isContact = standard.Contacts.Any(c => c.UserId == currentUserId.Value);

        if (!isStandardsManager && !isOwner && !isContact)
        {
            TempData["ErrorMessage"] = "You do not have permission to unpublish this standard.";
            return RedirectToAction(nameof(Details), new { id });
        }

        return View(standard);
    }

    /// <summary>
    /// Unpublish standard - POST
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unpublish(int id, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["ErrorMessage"] = "Reason for unpublishing is required.";
            return RedirectToAction(nameof(Unpublish), new { id });
        }

        var standard = await _context.DdtStandards
            .Include(s => s.Owners).ThenInclude(o => o.User)
            .Include(s => s.Contacts).ThenInclude(c => c.User)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

        if (standard == null)
        {
            return NotFound();
        }

        if (standard.Stage != "Published")
        {
            TempData["ErrorMessage"] = "Only published standards can be unpublished.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // Check permissions
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            TempData["ErrorMessage"] = "You must be signed in to perform this action.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var isStandardsManager = await IsStandardsManagerAsync();
        var isOwner = standard.Owners.Any(o => o.UserId == currentUserId.Value);
        var isContact = standard.Contacts.Any(c => c.UserId == currentUserId.Value);

        if (!isStandardsManager && !isOwner && !isContact)
        {
            TempData["ErrorMessage"] = "You do not have permission to unpublish this standard.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var now = DateTime.UtcNow;

        // Create unpublish audit log (per spec 9.3)
        var audit = new DdtStandardUnpublishAudit
        {
            DdtStandardId = standard.Id,
            Version = standard.Version,
            Reason = reason.Trim(),
            UnpublishedByUserId = currentUserId.Value,
            UnpublishedAt = now
        };
        _context.DdtStandardUnpublishAudits.Add(audit);

        // Update standard status per spec 3.2.5
        standard.Stage = "Unpublished";
        standard.IsPublished = false;
        standard.UpdatedAt = now;
        // PublishedAt is preserved for history per spec 3.2.5

        // Create audit log entry per spec 9.1
        await CreateAuditLogAsync(standard.Id, "Unpublished", 
            $"Standard unpublished. Version: {standard.Version}. Reason: {reason.Trim()}");

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Standard has been unpublished successfully.";
        return RedirectToAction(nameof(Details), new { id });
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
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Unpublished(string? search, string? category)
    {
        var currentUserId = GetCurrentUserId();

        // Base query for all unpublished standards
        var allUnpublished = await _context.DdtStandards
            .AsNoTracking()
            .Include(s => s.CreatorUser)
            .Include(s => s.Owners).ThenInclude(o => o.User)
            .Include(s => s.Contacts).ThenInclude(c => c.User)
            .Include(s => s.Categories).ThenInclude(c => c.Category)
            .Where(s => !s.IsDeleted && s.Stage == "Unpublished")
            .ToListAsync();

        // Group by slug and get only the latest version (highest version number or most recent UpdatedAt)
        var latestUnpublished = allUnpublished
            .GroupBy(s => s.Slug)
            .Select(g => g.OrderByDescending(s => 
                {
                    var version = TryParseVersion(s.Version);
                    return version ?? new Version(0, 0, 0);
                })
                .ThenByDescending(s => s.UpdatedAt)
                .First())
            .ToList();

        // Apply filters
        var allFiltered = latestUnpublished.AsEnumerable();
        var myFiltered = latestUnpublished.AsEnumerable();

        if (currentUserId.HasValue)
        {
            myFiltered = myFiltered.Where(s => 
                s.Owners.Any(o => o.UserId == currentUserId.Value) ||
                s.Contacts.Any(c => c.UserId == currentUserId.Value));
        }
        else
        {
            myFiltered = Enumerable.Empty<DdtStandard>();
        }

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(search))
        {
            allFiltered = allFiltered.Where(s => 
                s.Title.Contains(search, StringComparison.OrdinalIgnoreCase) || 
                (s.Summary != null && s.Summary.Contains(search, StringComparison.OrdinalIgnoreCase)));
            myFiltered = myFiltered.Where(s => 
                s.Title.Contains(search, StringComparison.OrdinalIgnoreCase) || 
                (s.Summary != null && s.Summary.Contains(search, StringComparison.OrdinalIgnoreCase)));
        }

        // Apply category filter
        if (!string.IsNullOrWhiteSpace(category))
        {
            allFiltered = allFiltered.Where(s => s.Categories.Any(c => c.Category.Name == category));
            myFiltered = myFiltered.Where(s => s.Categories.Any(c => c.Category.Name == category));
        }

        var allUnpublishedList = allFiltered
            .OrderByDescending(s => s.UpdatedAt)
            .ToList();

        var myUnpublishedList = myFiltered
            .OrderByDescending(s => s.UpdatedAt)
            .ToList();

        // Get filter options
        var categories = await _context.DdtStandardCategories
            .AsNoTracking()
            .Include(c => c.Category)
            .Select(c => c.Category.Name)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();

        ViewBag.MyUnpublished = myUnpublishedList;
        ViewBag.AllUnpublished = allUnpublishedList;
        ViewBag.Categories = categories;
        ViewBag.CurrentSearch = search;
        ViewBag.CurrentCategory = category;
        
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

        return View(allUnpublished);
    }

    /// <summary>
    /// Make a change - create a new draft from a published standard
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MakeChange(int id)
    {
        var sourceStandard = await _context.DdtStandards
            .Include(s => s.Owners)
            .Include(s => s.Contacts)
            .Include(s => s.Categories)
            .Include(s => s.SubCategories)
            .Include(s => s.Phases)
            .Include(s => s.Products)
            .Include(s => s.Exceptions)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

        if (sourceStandard == null)
        {
            return NotFound();
        }

        if (sourceStandard.Stage != "Published")
        {
            TempData["ErrorMessage"] = "You can only make changes to published standards.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // Check permissions
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            TempData["ErrorMessage"] = "You must be signed in to perform this action.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var isStandardsManager = await IsStandardsManagerAsync();
        var isOwner = sourceStandard.Owners.Any(o => o.UserId == currentUserId.Value);
        var isContact = sourceStandard.Contacts.Any(c => c.UserId == currentUserId.Value);

        if (!isStandardsManager && !isOwner && !isContact)
        {
            TempData["ErrorMessage"] = "You do not have permission to make changes to this standard.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // Per spec 5.4: Child starts with parent's version (no increment yet)
        // Version will be incremented when child is published
        var parentVersion = sourceStandard.Version;

        // Create new draft standard
        var newStandard = new DdtStandard
        {
            StandardUuid = Guid.NewGuid().ToString(),
            Title = sourceStandard.Title,
            Slug = sourceStandard.Slug + "-draft-" + DateTime.UtcNow.Ticks,
            Summary = sourceStandard.Summary,
            Purpose = sourceStandard.Purpose,
            Criteria = sourceStandard.Criteria,
            HowToMeet = sourceStandard.HowToMeet,
            Governance = sourceStandard.Governance,
            GovernanceApproval = false,
            Version = parentVersion, // Per spec 5.4: Keep parent version
            PreviousVersion = null, // Will be set when published
            LegacyReference = await GenerateLegacyReferenceAsync(parentStandardId: sourceStandard.Id), // Inherit from parent
            Stage = "Draft",
            LegalStandard = sourceStandard.LegalStandard,
            LegalBasis = sourceStandard.LegalBasis,
            ValidityPeriod = sourceStandard.ValidityPeriod,
            RelatedGuidance = sourceStandard.RelatedGuidance,
            IsModified = false,
            IsPublished = false,
            CreatorUserId = currentUserId,
            ParentStandardId = sourceStandard.Id,
            DraftCreated = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.DdtStandards.Add(newStandard);
        await _context.SaveChangesAsync(); // Save to get the ID

        // Copy owners
        foreach (var owner in sourceStandard.Owners)
        {
            _context.DdtStandardOwners.Add(new DdtStandardOwner
            {
                DdtStandardId = newStandard.Id,
                UserId = owner.UserId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        // Copy contacts
        foreach (var contact in sourceStandard.Contacts)
        {
            _context.DdtStandardContacts.Add(new DdtStandardContact
            {
                DdtStandardId = newStandard.Id,
                UserId = contact.UserId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        // Copy categories
        foreach (var category in sourceStandard.Categories)
        {
            _context.DdtStandardCategories.Add(new DdtStandardCategory
            {
                DdtStandardId = newStandard.Id,
                CategoryId = category.CategoryId,
                CreatedAt = DateTime.UtcNow
            });
        }

        // Copy sub-categories
        foreach (var subCategory in sourceStandard.SubCategories)
        {
            _context.DdtStandardSubCategories.Add(new DdtStandardSubCategory
            {
                DdtStandardId = newStandard.Id,
                SubCategoryId = subCategory.SubCategoryId,
                CreatedAt = DateTime.UtcNow
            });
        }

        // Copy phases
        foreach (var phase in sourceStandard.Phases)
        {
            _context.DdtStandardPhases.Add(new DdtStandardPhase
            {
                DdtStandardId = newStandard.Id,
                PhaseLookupId = phase.PhaseLookupId,
                Enabled = phase.Enabled,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        // Copy products
        foreach (var product in sourceStandard.Products)
        {
            _context.DdtStandardProducts.Add(new DdtStandardProduct
            {
                DdtStandardId = newStandard.Id,
                StandardProductId = product.StandardProductId,
                ProductType = product.ProductType,
                Notes = product.Notes,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();

        // Create audit log entry
        await CreateAuditLogAsync(sourceStandard.Id, "MakeChange", 
            $"New draft created from published standard. Parent version: {parentVersion}");

        TempData["SuccessMessage"] = $"New draft created from published standard. Version: {parentVersion} (will be incremented on publication).";
        return RedirectToAction(nameof(Create), new { id = newStandard.Id });
    }

    /// <summary>
    /// Edit standard - GET
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var standard = await _context.DdtStandards
            .Include(s => s.Owners)
            .Include(s => s.Contacts)
            .Include(s => s.Categories)
            .Include(s => s.SubCategories)
            .Include(s => s.Phases)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

        if (standard == null)
        {
            return NotFound();
        }

        // Check permissions - creator, owner, or admin
        var currentUserId = GetCurrentUserId();
        var isOwner = standard.Owners.Any(o => o.UserId == currentUserId);
        var isCreator = standard.CreatorUserId == currentUserId;
        var isAdmin = User.IsInRole("Admin") || User.IsInRole("SuperAdmin");

        if (!isOwner && !isCreator && !isAdmin)
        {
            TempData["ErrorMessage"] = "You do not have permission to edit this standard.";
            return RedirectToAction(nameof(Details), new { id });
        }

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

        ViewBag.Phases = phases;
        ViewBag.Categories = categories;
        ViewBag.SelectedPhaseIds = standard.Phases.Select(p => p.PhaseLookupId).ToList();
        ViewBag.SelectedOwnerIds = standard.Owners.Select(o => o.UserId).ToList();
        ViewBag.SelectedContactIds = standard.Contacts.Select(c => c.UserId).ToList();
        ViewBag.SelectedCategories = standard.Categories.Select(c => c.CategoryId).ToList();
        ViewBag.SelectedSubCategories = standard.SubCategories.Select(c => c.SubCategoryId).ToList();

        return View(standard);
    }

    /// <summary>
    /// Edit standard - POST
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
        var standard = await _context.DdtStandards
            .Include(s => s.Owners)
            .Include(s => s.Contacts)
            .Include(s => s.Categories)
            .Include(s => s.SubCategories)
            .Include(s => s.Phases)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

        if (standard == null)
        {
            return NotFound();
        }

        // Check permissions
        var currentUserId = GetCurrentUserId();
        var isOwner = standard.Owners.Any(o => o.UserId == currentUserId);
        var isCreator = standard.CreatorUserId == currentUserId;
        var isAdmin = User.IsInRole("Admin") || User.IsInRole("SuperAdmin");

        if (!isOwner && !isCreator && !isAdmin)
        {
            TempData["ErrorMessage"] = "You do not have permission to edit this standard.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // Per spec 3.2.7: Rejected → Draft when edited
        bool wasRejected = standard.Stage == "Rejected";
        if (wasRejected)
        {
            standard.Stage = "Draft";
        }

        try
        {
            // Update basic fields
            if (standard.Title != title)
            {
                standard.Title = title.Trim();
                standard.Slug = GenerateSlug(title);
                standard.IsModified = true;
            }

            standard.Summary = summary;
            standard.Purpose = purpose;
            standard.Criteria = criteria;
            standard.HowToMeet = howToMeet;
            standard.Governance = governance;
            standard.LegalBasis = legalBasis;
            standard.LegalStandard = legalStandard;
            standard.ValidityPeriod = validityPeriod;
            standard.RelatedGuidance = relatedGuidance;
            standard.LastUpdated = DateTime.UtcNow;
            standard.UpdatedAt = DateTime.UtcNow;

            // Update categories
            _context.DdtStandardCategories.RemoveRange(standard.Categories);
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

            // Update sub-categories
            _context.DdtStandardSubCategories.RemoveRange(standard.SubCategories);
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

            // Update phases
            _context.DdtStandardPhases.RemoveRange(standard.Phases);
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

            // Update owners
            _context.DdtStandardOwners.RemoveRange(standard.Owners);
            if (ownerUserIds != null && ownerUserIds.Any())
            {
                foreach (var userId in ownerUserIds)
                {
                    _context.DdtStandardOwners.Add(new DdtStandardOwner
                    {
                        DdtStandardId = standard.Id,
                        UserId = userId,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
            }

            // Update contacts
            _context.DdtStandardContacts.RemoveRange(standard.Contacts);
            if (contactUserIds != null && contactUserIds.Any())
            {
                foreach (var userId in contactUserIds)
                {
                    _context.DdtStandardContacts.Add(new DdtStandardContact
                    {
                        DdtStandardId = standard.Id,
                        UserId = userId,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
            }

            await _context.SaveChangesAsync();

            // Create audit log entry per spec 9.1
            if (wasRejected)
            {
                await CreateAuditLogAsync(standard.Id, "Modified", 
                    $"Standard edited. Stage: Rejected → Draft (per spec 3.2.7)");
            }
            else
            {
                await CreateAuditLogAsync(standard.Id, "Modified", 
                    $"Standard updated. Title: {standard.Title}");
            }

            TempData["SuccessMessage"] = $"Standard '{standard.Title}' updated successfully.";
            return RedirectToAction(nameof(Details), new { id = standard.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating standard {StandardId}", id);
            TempData["ErrorMessage"] = "An error occurred while updating the standard.";
            return RedirectToAction(nameof(Edit), new { id });
        }
    }

    /// <summary>
    /// Submit standard for review
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitForReview(int id)
    {
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

        _logger.LogInformation("SubmitForReview: Standard {Id} found. Stage: {Stage}, Owners: {OwnerCount}, Governance: {HasGovernance}, ValidityPeriod: {ValidityPeriod}",
            id, standard.Stage, standard.Owners.Count, !string.IsNullOrWhiteSpace(standard.Governance), standard.ValidityPeriod);

        if (standard.Stage != "Draft")
        {
            _logger.LogWarning("SubmitForReview: Standard {Id} is not in Draft stage. Current stage: {Stage}", id, standard.Stage);
            TempData["ErrorMessage"] = $"Only draft standards can be submitted for review. Current stage: {standard.Stage}.";
            // Redirect to Create if coming from Create page, otherwise Details
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
                                      !standard.Version.Contains("-resubmit"); // Track if already resubmitted

        if (shouldIncrementVersion)
        {
            var parentStandard = standard.ParentStandard;
            if (parentStandard == null) return RedirectToAction(nameof(Details), new { id });
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

                // Compare products - check for additions and removals separately
                var parentProductIds = parentStandard.Products.Select(p => p.StandardProductId).OrderBy(x => x).ToList();
                var currentProductIds = standard.Products.Select(p => p.StandardProductId).OrderBy(x => x).ToList();
                var addedProducts = currentProductIds.Except(parentProductIds).ToList();
                var removedProducts = parentProductIds.Except(currentProductIds).ToList();
                
                if (addedProducts.Any())
                {
                    productsAdded = true;
                }
                if (removedProducts.Any())
                {
                    productsRemoved = true;
                }

                // Compare exceptions - check for additions and removals separately
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
                
                if (addedExceptions.Any())
                {
                    exceptionsAdded = true;
                }
                if (removedExceptions.Any())
                {
                    exceptionsRemoved = true;
                }

                // Compare description (Summary field)
                if (parentStandard.Summary != standard.Summary)
                {
                    descriptionChanged = true;
                }

                // Check for other changes (Purpose, HowToMeet, Criteria, etc.)
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
                // MAJOR: Adding/removing products/exceptions or changing legal status (breaking changes - new criteria for meeting standard)
                // MINOR: Other content changes
                // PATCH: Description changes only
                string versionType = "patch"; // Default
                if (productsAdded || productsRemoved || exceptionsAdded || exceptionsRemoved || legalStatusChanged)
                {
                    // Adding or removing products/exceptions or changing legal status results in new criteria for meeting a standard = MAJOR
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
                    // Other changes default to minor
                    minor++;
                    patch = 0;
                    versionType = "minor";
                }
                // If nothing changed, keep current version (shouldn't happen, but handle gracefully)

                var newVersion = $"{major}.{minor}.{patch}";
                standard.PreviousVersion = standard.Version;
                standard.Version = newVersion;
                
                // Determine the stage based on version type
                // MAJOR changes go to "For Approval" (need review)
                // MINOR and PATCH changes go to "Awaiting Publication" (no approval needed)
                if (versionType == "major")
                {
                    standard.Stage = "For Approval";
                }
                else
                {
                    // MINOR or PATCH changes go straight to "Awaiting Publication"
                    standard.Stage = "Awaiting Publication";
                    standard.GovernanceApproval = true; // Auto-approved for minor/patch
                }
            }
            else
            {
                // New standard (no parent) - always goes to "For Approval"
                standard.Stage = "For Approval";
            }
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
                TempData["SuccessMessage"] = "Standard submitted successfully. It is now in 'Awaiting Publication' status and can be published by an Administrator.";
            }
            else
            {
                TempData["SuccessMessage"] = "Standard submitted for review successfully. It is now in 'For Approval' status.";
            }
            // Redirect to Details since standard is no longer in Draft and shouldn't be edited
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SubmitForReview: Error submitting standard {Id} for review", id);
            TempData["ErrorMessage"] = $"An error occurred while submitting the standard for review: {ex.Message}";
            return RedirectToAction(nameof(Create), new { id });
        }
    }

    /// <summary>
    /// Approve standard - only Standards Managers can approve
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id, string? comment)
    {
        if (!await IsStandardsManagerAsync())
        {
            TempData["ErrorMessage"] = "You do not have permission to approve standards. Only Standards Managers can approve.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var standard = await _context.DdtStandards
            .Include(s => s.Versions)
            .Include(s => s.ParentStandard)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

        if (standard == null)
        {
            return NotFound();
        }

        if (standard.Stage != "For Approval")
        {
            TempData["ErrorMessage"] = "Only standards awaiting approval can be approved.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var currentUserId = GetCurrentUserId();
        var now = DateTime.UtcNow;

        // Per spec 3.2.2: If standard has a parent (created via "Make a Change"), 
        // unpublish parent and immediately publish new standard
        bool hasParent = standard.ParentStandardId.HasValue && standard.ParentStandard != null;
        
        if (hasParent)
        {
            var parentStandard = standard.ParentStandard;
            if (parentStandard == null) return RedirectToAction(nameof(Details), new { id });
            
            // Determine change type first to decide if we should immediately publish or go to "Awaiting Publication"
            var previousVersion = standard.Version;
            string versionType = "patch";
            string newVersion = previousVersion;
            bool isMajorChange = false;
            
            // Compare with parent to determine change type
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

                // Compare products - check for additions and removals separately
                var parentProductIds = parentStandard.Products.Select(p => p.StandardProductId).OrderBy(x => x).ToList();
                var currentProductIds = standard.Products.Select(p => p.StandardProductId).OrderBy(x => x).ToList();
                var addedProducts = currentProductIds.Except(parentProductIds).ToList();
                var removedProducts = parentProductIds.Except(currentProductIds).ToList();
                
                if (addedProducts.Any())
                {
                    productsAdded = true;
                }
                if (removedProducts.Any())
                {
                    productsRemoved = true;
                }

                // Compare exceptions - check for additions and removals separately
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
                
                if (addedExceptions.Any())
                {
                    exceptionsAdded = true;
                }
                if (removedExceptions.Any())
                {
                    exceptionsRemoved = true;
                }

                // Compare description (Summary field)
                if (parentStandard.Summary != standard.Summary)
                {
                    descriptionChanged = true;
                }

                // Check for other changes (Purpose, HowToMeet, Criteria, etc.)
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
                // MAJOR: Adding/removing products/exceptions or changing legal status (breaking changes - new criteria for meeting standard)
                // MINOR: Other content changes
                // PATCH: Description changes only
                if (productsAdded || productsRemoved || exceptionsAdded || exceptionsRemoved || legalStatusChanged)
                {
                    // Adding or removing products/exceptions or changing legal status results in new criteria for meeting a standard = MAJOR
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
                    // Other changes default to minor
                    versionType = "minor";
                    minor++;
                    patch = 0;
                    newVersion = $"{major}.{minor}.{patch}";
                }
                else
                {
                    // No changes detected, default to patch
                    versionType = "patch";
                    newVersion = IncrementVersion(previousVersion, versionType);
                }
            }
            else
            {
                // Fallback if version parsing fails
                versionType = DetermineVersionType(previousVersion);
                newVersion = IncrementVersion(previousVersion, versionType);
            }

            standard.PreviousVersion = previousVersion;
            standard.Version = newVersion;

            // Unpublish parent when child is approved (regardless of change type)
            // The child is the approved replacement
            if (parentStandard.Stage == "Published")
            {
                // Create audit log for unpublishing parent
                var audit = new DdtStandardUnpublishAudit
                {
                    DdtStandardId = parentStandard.Id,
                    Version = parentStandard.Version,
                    Reason = $"Replaced by new version {newVersion}",
                    UnpublishedByUserId = currentUserId ?? 0,
                    UnpublishedAt = now
                };
                _context.DdtStandardUnpublishAudits.Add(audit);

                // Unpublish parent per spec 3.2.2
                parentStandard.Stage = "Unpublished";
                parentStandard.IsPublished = false;
                parentStandard.UpdatedAt = now;

                await CreateAuditLogAsync(parentStandard.Id, "Unpublished", 
                    $"Parent standard unpublished. Replaced by version {newVersion}");
            }

            // Handle different change types:
            // MAJOR changes: Go back to "For Approval" for re-review
            // MINOR/PATCH changes: Go to "Awaiting Publication" (can be published when ready)
            if (isMajorChange)
            {
                // MAJOR changes require additional review - go back to "For Approval"
                standard.Stage = "For Approval";
                standard.GovernanceApproval = false; // Reset approval flag for re-review
                standard.UpdatedAt = now;

                await CreateAuditLogAsync(standard.Id, "Approved", 
                    $"Standard approved but requires re-review for MAJOR changes. Stage: For Approval → For Approval. Version: {previousVersion} → {newVersion} ({versionType})");
            }
            else
            {
                // MINOR or PATCH changes go to "Awaiting Publication" stage
                standard.Stage = "Awaiting Publication";
                standard.GovernanceApproval = true;
                standard.UpdatedAt = now;

                await CreateAuditLogAsync(standard.Id, "Approved", 
                    $"Standard approved. Stage: For Approval → Awaiting Publication. Version: {previousVersion} → {newVersion} ({versionType})");
            }
        }
        else
        {
            // Per spec 3.2.2: Normal approval - set to "Awaiting Publication" stage (not published yet)
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
            TempData["SuccessMessage"] = "Standard approved and published successfully (replaced parent standard).";
        }
        else
        {
            TempData["SuccessMessage"] = "Standard approved. It can now be published by an Admin.";
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>
    /// Reject standard - only Standards Managers can reject
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, string reason)
    {
        if (!await IsStandardsManagerAsync())
        {
            TempData["ErrorMessage"] = "You do not have permission to reject standards. Only Standards Managers can reject.";
            return RedirectToAction(nameof(Details), new { id });
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["ErrorMessage"] = "Rejection reason is required.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var standard = await _context.DdtStandards.FindAsync(id);
        if (standard == null || standard.IsDeleted)
        {
            return NotFound();
        }

        if (standard.Stage != "For Approval")
        {
            TempData["ErrorMessage"] = "Only standards awaiting approval can be rejected.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var currentUserId = GetCurrentUserId();
        var now = DateTime.UtcNow;

        // Per user requirement: Set stage to "Draft" so it can be revised and resubmitted
        standard.Stage = "Draft";
        standard.UpdatedAt = now;

        // Add rejection comment
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
        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>
    /// Publish standard - Admins or Standards Managers who are owners can publish
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Publish(int id)
    {
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
            return RedirectToAction(nameof(Details), new { id });
        }

        // Check permissions: Admin/SuperAdmin OR (Standards Manager AND owner)
        var isAdmin = User.IsInRole("Admin") || User.IsInRole("SuperAdmin");
        var isStandardsManager = await IsStandardsManagerAsync();
        var publishUserId = GetCurrentUserId();
        var isOwner = publishUserId.HasValue && standard.Owners.Any(o => o.UserId == publishUserId.Value);

        if (!isAdmin && !(isStandardsManager && isOwner))
        {
            TempData["ErrorMessage"] = "You do not have permission to publish this standard. Only Administrators or Standards Managers who are owners can publish.";
            return RedirectToAction(nameof(Details), new { id });
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
        // Per spec 4.2.2: For standards that went through SubmitForReview, version was already incremented
        var previousVersion = standard.Version;
        string newVersion;
        string versionType;

        // Check if this is initial publication (0.1.0 or similar)
        if (previousVersion.StartsWith("0.") || string.IsNullOrWhiteSpace(previousVersion) || previousVersion == "0.1.0")
        {
            // Per spec 4.2.1: First publication becomes 1.0.0
            newVersion = "1.0.0";
            versionType = "major";
            standard.PreviousVersion = previousVersion;
        }
        else
        {
            // For standards in "Awaiting Publication", the version was already incremented during SubmitForReview
            // Do NOT increment again - just use the version as-is
            newVersion = previousVersion;
            
            // Determine version type from the increment that was already made
            // If PreviousVersion is set, we can infer the type from the difference
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
                        versionType = "patch"; // Default
                }
                else
                {
                    versionType = "patch"; // Default
                }
            }
            else
            {
                // No PreviousVersion set - this shouldn't happen for standards in Awaiting Publication
                // but handle gracefully
                versionType = "patch"; // Default
            }
            
            // PreviousVersion should already be set from SubmitForReview, but ensure it's set
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
        // unpublish parent when child is published (if parent is still published)
        if (standard.ParentStandardId.HasValue && standard.ParentStandard != null)
        {
            var parentStandard = standard.ParentStandard;
            
            // Unpublish parent if it's still published
            if (parentStandard.Stage == "Published" && parentStandard.IsPublished)
            {
                // Create audit log for unpublishing parent
                var audit = new DdtStandardUnpublishAudit
                {
                    DdtStandardId = parentStandard.Id,
                    Version = parentStandard.Version,
                    Reason = $"Replaced by new version {newVersion}",
                    UnpublishedByUserId = currentUserId ?? 0,
                    UnpublishedAt = now
                };
                _context.DdtStandardUnpublishAudits.Add(audit);

                // Unpublish parent per spec 3.2.2
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
        return RedirectToAction(nameof(Details), new { id });
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
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddComment(int id, string title, string comments, string? commentType, string? field, int? parentCommentId = null)
    {
        var standard = await _context.DdtStandards.FindAsync(id);
        if (standard == null || standard.IsDeleted)
        {
            return Json(new { success = false, message = "Standard not found." });
        }

        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            return Json(new { success = false, message = "Unable to identify current user." });
        }

        // Use field as comment type if provided, otherwise use commentType
        var finalCommentType = !string.IsNullOrWhiteSpace(field) ? field : (commentType ?? "feedback");

        // Generate title from field name if not provided (only for top-level comments, not replies)
        var commentTitle = title;
        if (string.IsNullOrWhiteSpace(commentTitle) && !parentCommentId.HasValue)
        {
            var fieldDisplayName = !string.IsNullOrWhiteSpace(field) 
                ? field switch
                {
                    "title" => "Title",
                    "summary" => "Summary",
                    "purpose" => "Purpose",
                    "howToMeet" => "How to meet",
                    "phases" => "Phases",
                    "categorisation" => "Categorisation",
                    "governance" => "Governance",
                    "legalStandard" => "Legal standard",
                    "legalBasis" => "Legal basis",
                    "validityPeriod" => "Validity period",
                    "relatedGuidance" => "Related guidance",
                    "owners" => "Owners",
                    "contacts" => "Contacts",
                    "products" => "Products",
                    "exemptions" => "Exemptions",
                    _ => "General"
                }
                : "General";
            commentTitle = $"Comment on {fieldDisplayName}";
        }
        else if (string.IsNullOrWhiteSpace(commentTitle) && parentCommentId.HasValue)
        {
            commentTitle = "Reply";
        }

        var comment = new DdtStandardComment
        {
            DdtStandardId = id,
            Title = commentTitle,
            Comments = comments,
            CommentType = finalCommentType,
            UserId = currentUserId.Value,
            ParentCommentId = parentCommentId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.DdtStandardComments.Add(comment);
        await _context.SaveChangesAsync();

        // Load the comment with user details for response
        var savedComment = await _context.DdtStandardComments
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.Id == comment.Id);

        if (savedComment == null || savedComment.User == null)
        {
            return Json(new { success = false, message = "Failed to save comment" });
        }

        return Json(new { 
            success = true, 
            commentId = comment.Id,
            comment = new {
                id = savedComment.Id,
                title = savedComment.Title,
                comments = savedComment.Comments,
                commentType = savedComment.CommentType,
                userName = savedComment.User.Name,
                userEmail = savedComment.User.Email,
                createdAt = savedComment.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                isResolved = savedComment.IsResolved
            }
        });
    }

    /// <summary>
    /// Resolve comment
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResolveComment(int id, int commentId)
    {
        var comment = await _context.DdtStandardComments.FindAsync(commentId);
        if (comment == null || comment.DdtStandardId != id)
        {
            return Json(new { success = false, message = "Comment not found." });
        }

        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            return Json(new { success = false, message = "Unable to identify current user." });
        }

        comment.IsResolved = true;
        comment.ResolvedByUserId = currentUserId.Value;
        comment.ResolvedAt = DateTime.UtcNow;
        comment.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Json(new { success = true });
    }

    /// <summary>
    /// Get comments for a standard
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetComments(int id)
    {
        var comments = await _context.DdtStandardComments
            .Include(c => c.User)
            .Include(c => c.ResolvedByUser)
            .Include(c => c.Replies).ThenInclude(r => r.User)
            .Where(c => c.DdtStandardId == id && c.ParentCommentId == null)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new
            {
                id = c.Id,
                title = c.Title,
                comments = c.Comments ?? "",
                commentType = c.CommentType ?? "",
                field = c.CommentType ?? "",
                userName = c.User.Name,
                userEmail = c.User.Email,
                createdAt = c.CreatedAt,
                isResolved = c.IsResolved,
                resolvedBy = c.ResolvedByUser != null ? c.ResolvedByUser.Name : null,
                resolvedAt = c.ResolvedAt,
                replies = c.Replies.Select(r => new
                {
                    id = r.Id,
                    comments = r.Comments ?? "",
                    userName = r.User.Name,
                    userEmail = r.User.Email,
                    createdAt = r.CreatedAt
                }).ToList()
            })
            .ToListAsync();

        return Json(new { success = true, comments });
    }

    /// <summary>
    /// Unresolve comment
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnresolveComment(int id, int commentId)
    {
        var comment = await _context.DdtStandardComments.FindAsync(commentId);
        if (comment == null || comment.DdtStandardId != id)
        {
            return Json(new { success = false, message = "Comment not found." });
        }

        comment.IsResolved = false;
        comment.ResolvedByUserId = null;
        comment.ResolvedAt = null;
        comment.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Json(new { success = true });
    }

    /// <summary>
    /// Delete draft standard - only creator or admin can delete
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var standard = await _context.DdtStandards
            .Include(s => s.CreatorUser)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

        if (standard == null)
        {
            return NotFound();
        }

        // Only allow deletion of drafts
        if (standard.Stage != "Draft")
        {
            TempData["ErrorMessage"] = "Only draft standards can be deleted.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var currentUserId = GetCurrentUserId();
        var isCreator = currentUserId.HasValue && standard.CreatorUserId == currentUserId.Value;
        var isAdmin = User.IsCompassAdmin();

        // Only creator or admin can delete
        if (!isCreator && !isAdmin)
        {
            TempData["ErrorMessage"] = "You don't have permission to delete this standard. Only the creator or an administrator can delete drafts.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // Soft delete - mark as deleted
        standard.IsDeleted = true;
        standard.UpdatedAt = DateTime.UtcNow;

        // Create audit log entry per spec 9.1 (Standard Deleted)
        await CreateAuditLogAsync(standard.Id, "Deleted", 
            $"Standard soft-deleted. Title: {standard.Title}, Version: {standard.Version}");

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Standard '{standard.Title}' has been deleted.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Preview standard
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Preview(int id)
    {
        var standard = await _context.DdtStandards
            .Include(s => s.CreatorUser)
            .Include(s => s.Owners).ThenInclude(o => o.User)
            .Include(s => s.Contacts).ThenInclude(c => c.User)
            .Include(s => s.Categories).ThenInclude(c => c.Category)
            .Include(s => s.SubCategories).ThenInclude(sc => sc.SubCategory)
            .Include(s => s.Phases).ThenInclude(p => p.PhaseLookup)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

        if (standard == null)
        {
            return NotFound();
        }

        return View(standard);
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
        if (!await IsStandardsManagerAsync())
        {
            TempData["ErrorMessage"] = "You do not have permission to access this page. Only Standards Managers can manage approved products.";
            return RedirectToAction(nameof(Index));
        }

        var standardProducts = await _context.StandardProducts
            .AsNoTracking() // Read-only list view
            .Include(sp => sp.CreatedByUser)
            .Include(sp => sp.ReviewedByUser)
            .OrderBy(sp => sp.Name) // Default order, client-side sorting will handle the rest
            .ToListAsync();

        var ddtStandardProducts = await _context.DdtStandardProducts
            .Include(dsp => dsp.DdtStandard)
            .Include(dsp => dsp.StandardProduct)
            .Where(dsp => dsp.DdtStandard.IsPublished && dsp.DdtStandard.Stage == "Published" && !dsp.DdtStandard.IsDeleted)
            .ToListAsync();

        // Get counts for navigation
        var allDraftsCount = await _context.DdtStandards.CountAsync(s => !s.IsDeleted && s.Stage == "Draft");
        var allInReviewCount = await _context.DdtStandards.CountAsync(s => !s.IsDeleted && s.Stage == "For Approval");
        var allForApprovalCount = await _context.DdtStandards.CountAsync(s => !s.IsDeleted && s.Stage == "Awaiting Publication");
        var allPublishedCount = await _context.DdtStandards.CountAsync(s => !s.IsDeleted && s.Stage == "Published");
        var approvedProductsCount = await _context.StandardProducts.CountAsync();
        var exceptionsCount = await _context.DdtStandardExceptions.CountAsync();

        ViewBag.StandardProducts = standardProducts;
        ViewBag.DdtStandardProducts = ddtStandardProducts;
        ViewBag.Standards = await _context.DdtStandards
            .Where(s => !s.IsDeleted)
            .OrderBy(s => s.Title)
            .ToListAsync();
        ViewBag.AllDraftsCount = allDraftsCount;
        ViewBag.AllInReviewCount = allInReviewCount;
        ViewBag.AllForApprovalCount = allForApprovalCount;
        ViewBag.AllPublishedCount = allPublishedCount;
        ViewBag.AllUnpublishedCount = await GetUnpublishedCountAsync();
        ViewBag.ApprovedProductsCount = approvedProductsCount;
        ViewBag.ExceptionsCount = exceptionsCount;

        return View();
    }

    /// <summary>
    /// Create a new standard product
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateProduct(string name, string? description, string? provider, string? version, string approvalStatus)
    {
        if (!await IsStandardsManagerAsync())
        {
            TempData["ErrorMessage"] = "You do not have permission to perform this action.";
            return RedirectToAction(nameof(ApprovedProducts));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["ErrorMessage"] = "Product name is required.";
            return RedirectToAction(nameof(ApprovedProducts));
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
        return RedirectToAction(nameof(ApprovedProducts));
    }

    /// <summary>
    /// Update an existing standard product
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProduct(int id, string name, string? description, string? provider, string? version, string approvalStatus)
    {
        if (!await IsStandardsManagerAsync())
        {
            TempData["ErrorMessage"] = "You do not have permission to perform this action.";
            return RedirectToAction(nameof(ApprovedProducts));
        }

        var product = await _context.StandardProducts.FindAsync(id);
        if (product == null)
        {
            TempData["ErrorMessage"] = "Product not found.";
            return RedirectToAction(nameof(ApprovedProducts));
        }

        product.Name = name.Trim();
        product.Description = description?.Trim();
        product.Provider = provider?.Trim();
        product.Version = version?.Trim();
        product.ApprovalStatus = approvalStatus ?? "Pending";
        product.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Product '{product.Name}' updated successfully.";
        return RedirectToAction(nameof(ApprovedProducts));
    }

    /// <summary>
    /// Assign a product to a standard (as approved or tolerated)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignProductToStandard(int productId, int standardId, string productType, string? notes)
    {
        if (!await IsStandardsManagerAsync())
        {
            TempData["ErrorMessage"] = "You do not have permission to perform this action.";
            return RedirectToAction(nameof(ApprovedProducts));
        }

        var product = await _context.StandardProducts.FindAsync(productId);
        var standard = await _context.DdtStandards.FindAsync(standardId);

        if (product == null || standard == null)
        {
            TempData["ErrorMessage"] = "Product or standard not found.";
            return RedirectToAction(nameof(ApprovedProducts));
        }

        // Check if already assigned
        var existing = await _context.DdtStandardProducts
            .FirstOrDefaultAsync(dsp => dsp.DdtStandardId == standardId && dsp.StandardProductId == productId);

        if (existing != null)
        {
            existing.ProductType = productType;
            existing.Notes = notes?.Trim();
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            var ddtStandardProduct = new DdtStandardProduct
            {
                DdtStandardId = standardId,
                StandardProductId = productId,
                ProductType = productType,
                Notes = notes?.Trim(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.DdtStandardProducts.Add(ddtStandardProduct);
        }

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Product '{product.Name}' assigned to standard '{standard.Title}' as {productType}.";
        return RedirectToAction(nameof(ApprovedProducts));
    }

    /// <summary>
    /// Manage exceptions - view, add, edit, delete known exceptions to standards
    /// </summary>
    public async Task<IActionResult> Exceptions()
    {
        if (!await IsStandardsManagerAsync())
        {
            TempData["ErrorMessage"] = "You do not have permission to access this page. Only Standards Managers can manage exceptions.";
            return RedirectToAction(nameof(Index));
        }

        var exceptions = await _context.DdtStandardExceptions
            .AsNoTracking() // Read-only list view
            .Include(e => e.DdtStandard)
            .Include(e => e.StandardProduct)
            .Include(e => e.GrantedByUser)
            .Include(e => e.CreatedByUser)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();

        ViewBag.Standards = await _context.DdtStandards
            .Where(s => !s.IsDeleted)
            .OrderBy(s => s.Title)
            .ToListAsync();
        ViewBag.StandardProducts = await _context.StandardProducts
            .OrderBy(sp => sp.Name)
            .ToListAsync();

        // Add counts for navigation (optimized - just counts, no data loading)
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
        ViewBag.ApprovedProductsCount = await _context.StandardProducts.CountAsync(p => p.ApprovalStatus == "Approved");
        ViewBag.ExceptionsCount = exceptions.Count;

        return View(exceptions);
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
        if (!await IsStandardsManagerAsync())
        {
            TempData["ErrorMessage"] = "You do not have permission to perform this action.";
            return RedirectToAction(nameof(Exceptions));
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            TempData["ErrorMessage"] = "Exception title is required.";
            return RedirectToAction(nameof(Exceptions));
        }

        var standard = await _context.DdtStandards.FindAsync(standardId);
        if (standard == null)
        {
            TempData["ErrorMessage"] = "Standard not found.";
            return RedirectToAction(nameof(Exceptions));
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
            Status = status ?? "Active",
            GrantedAt = grantedAt,
            ExpiresAt = expiresAt,
            GrantedByUserId = currentUserId,
            CreatedByUserId = currentUserId,
            Notes = notes?.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.DdtStandardExceptions.Add(exception);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Exception '{exception.Title}' created successfully.";
        return RedirectToAction(nameof(Exceptions));
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
        if (!await IsStandardsManagerAsync())
        {
            TempData["ErrorMessage"] = "You do not have permission to perform this action.";
            return RedirectToAction(nameof(Exceptions));
        }

        var exception = await _context.DdtStandardExceptions.FindAsync(id);
        if (exception == null)
        {
            TempData["ErrorMessage"] = "Exception not found.";
            return RedirectToAction(nameof(Exceptions));
        }

        var standard = await _context.DdtStandards.FindAsync(standardId);
        if (standard == null)
        {
            TempData["ErrorMessage"] = "Standard not found.";
            return RedirectToAction(nameof(Exceptions));
        }

        exception.Title = title.Trim();
        exception.DdtStandardId = standardId;
        exception.Description = description?.Trim();
        exception.Reason = reason?.Trim();
        exception.StandardProductId = productId;
        exception.FipsProductId = fipsProductId?.Trim();
        exception.Status = status ?? "Active";
        exception.GrantedAt = grantedAt;
        exception.ExpiresAt = expiresAt;
        exception.Notes = notes?.Trim();
        exception.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Exception '{exception.Title}' updated successfully.";
        return RedirectToAction(nameof(Exceptions));
    }

    /// <summary>
    /// Delete an exception
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteException(int id)
    {
        if (!await IsStandardsManagerAsync())
        {
            TempData["ErrorMessage"] = "You do not have permission to perform this action.";
            return RedirectToAction(nameof(Exceptions));
        }

        var exception = await _context.DdtStandardExceptions.FindAsync(id);
        if (exception == null)
        {
            TempData["ErrorMessage"] = "Exception not found.";
            return RedirectToAction(nameof(Exceptions));
        }

        _context.DdtStandardExceptions.Remove(exception);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Exception '{exception.Title}' deleted successfully.";
        return RedirectToAction(nameof(Exceptions));
    }

    /// <summary>
    /// Get exceptions for autocomplete (AJAX)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetExceptions(string? search = null)
    {
        var query = _context.DdtStandardExceptions
            .AsNoTracking()
            .Include(e => e.DdtStandard)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(e => e.Title.Contains(search) || 
                                     (e.Description != null && e.Description.Contains(search)));
        }

        var exceptions = await query
            .OrderByDescending(e => e.CreatedAt)
            .Take(20)
            .Select(e => new
            {
                id = e.Id,
                title = e.Title,
                description = e.Description,
                standardTitle = e.DdtStandard.Title
            })
            .ToListAsync();

        return Json(new { success = true, exceptions = exceptions });
    }

    /// <summary>
    /// Migrate published standards from CMS to DDT Standards database
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> MigrateFromCms(bool skipExisting = true)
    {
        try
        {
            _logger.LogInformation("Starting migration of published standards from CMS");

            // Fetch all published standards from CMS
            var cmsStandards = await _standardsCmsApiService.GetStandardsAsync(
                published: true,
                cacheDuration: null // Don't cache for migration
            );

            if (!cmsStandards.Any())
            {
                TempData["ErrorMessage"] = "No published standards found in CMS.";
                return RedirectToAction(nameof(Index));
            }

            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                TempData["ErrorMessage"] = "Unable to identify current user.";
                return RedirectToAction(nameof(Index));
            }

            int migrated = 0;
            int skipped = 0;
            int errors = 0;
            var errorMessages = new List<string>();

            foreach (var cmsStandard in cmsStandards)
            {
                try
                {
                    // Check if already migrated (by LegacyId or DocumentId)
                    var existing = await _context.DdtStandards
                        .FirstOrDefaultAsync(s => 
                            (!string.IsNullOrEmpty(cmsStandard.DocumentId) && s.LegacyId == cmsStandard.DocumentId) ||
                            (cmsStandard.LegacyId.HasValue && s.LegacyId == cmsStandard.LegacyId.ToString()));

                    if (existing != null && skipExisting)
                    {
                        _logger.LogInformation("Skipping standard '{Title}' - already exists (ID: {Id})", cmsStandard.Title, existing.Id);
                        skipped++;
                        continue;
                    }

                    // Create new DDT Standard
                    var ddtStandard = new DdtStandard
                    {
                        LegacyId = cmsStandard.DocumentId ?? cmsStandard.LegacyId?.ToString(),
                        LegacyReference = await GenerateLegacyReferenceAsync(cmsStandardId: cmsStandard.StandardId), // Use StandardId from CMS
                        Title = cmsStandard.Title,
                        Slug = cmsStandard.Slug,
                        Summary = cmsStandard.Summary,
                        Purpose = cmsStandard.Purpose,
                        HowToMeet = cmsStandard.HowToMeet,
                        Governance = cmsStandard.Governance,
                        GovernanceApproval = cmsStandard.GovernanceApproval,
                        Version = cmsStandard.Version.ToString("F1"),
                        PreviousVersion = cmsStandard.PreviousVersion > 0 ? cmsStandard.PreviousVersion.ToString("F1") : null,
                        Stage = cmsStandard.Stage?.Title ?? "Published",
                        DraftCreated = cmsStandard.DraftCreated ?? cmsStandard.CreatedAt ?? DateTime.UtcNow,
                        FirstPublished = cmsStandard.FirstPublished,
                        LastUpdated = cmsStandard.LastUpdated,
                        LegalStandard = cmsStandard.LegalStandard ?? false,
                        LegalBasis = cmsStandard.LegalBasis,
                        ValidityPeriod = cmsStandard.ValidityPeriod,
                        RelatedGuidance = cmsStandard.RelatedGuidance,
                        IsModified = cmsStandard.IsModified ?? false,
                        IsPublished = cmsStandard.PublishedAt.HasValue,
                        PublishedAt = cmsStandard.PublishedAt,
                        CreatorUserId = currentUserId.Value,
                        CreatedAt = cmsStandard.CreatedAt ?? DateTime.UtcNow,
                        UpdatedAt = cmsStandard.UpdatedAt ?? DateTime.UtcNow
                    };

                    _context.DdtStandards.Add(ddtStandard);
                    await _context.SaveChangesAsync(); // Save to get the ID

                    // Map Categories
                    if (cmsStandard.Categories != null && cmsStandard.Categories.Any())
                    {
                        foreach (var cmsCategory in cmsStandard.Categories)
                        {
                            var category = await _context.StandardCategories
                                .FirstOrDefaultAsync(c => c.Name == cmsCategory.Title);

                            if (category != null)
                            {
                                var ddtCategory = new DdtStandardCategory
                                {
                                    DdtStandardId = ddtStandard.Id,
                                    CategoryId = category.Id,
                                    CreatedAt = DateTime.UtcNow
                                };
                                _context.DdtStandardCategories.Add(ddtCategory);
                            }
                            else
                            {
                                _logger.LogWarning("Category '{CategoryName}' not found in database for standard '{Title}'", 
                                    cmsCategory.Title, cmsStandard.Title);
                            }
                        }
                    }

                    // Map Sub-Categories
                    if (cmsStandard.SubCategories != null && cmsStandard.SubCategories.Any())
                    {
                        foreach (var cmsSubCategory in cmsStandard.SubCategories)
                        {
                            var subCategory = await _context.StandardSubCategories
                                .FirstOrDefaultAsync(sc => sc.Name == cmsSubCategory.Title);

                            if (subCategory != null)
                            {
                                var ddtSubCategory = new DdtStandardSubCategory
                                {
                                    DdtStandardId = ddtStandard.Id,
                                    SubCategoryId = subCategory.Id,
                                    CreatedAt = DateTime.UtcNow
                                };
                                _context.DdtStandardSubCategories.Add(ddtSubCategory);
                            }
                            else
                            {
                                _logger.LogWarning("Sub-category '{SubCategoryName}' not found in database for standard '{Title}'", 
                                    cmsSubCategory.Title, cmsStandard.Title);
                            }
                        }
                    }

                    // Map Phases
                    if (cmsStandard.Phases != null && cmsStandard.Phases.Any())
                    {
                        foreach (var cmsPhase in cmsStandard.Phases)
                        {
                            var phase = await _context.PhaseLookups
                                .FirstOrDefaultAsync(p => p.Name == cmsPhase.Title && p.IsActive);

                            if (phase != null)
                            {
                                var ddtPhase = new DdtStandardPhase
                                {
                                    DdtStandardId = ddtStandard.Id,
                                    PhaseLookupId = phase.Id,
                                    Enabled = cmsPhase.Enabled ?? true,
                                    CreatedAt = DateTime.UtcNow,
                                    UpdatedAt = DateTime.UtcNow
                                };
                                _context.DdtStandardPhases.Add(ddtPhase);
                            }
                            else
                            {
                                _logger.LogWarning("Phase '{PhaseName}' not found in database for standard '{Title}'", 
                                    cmsPhase.Title, cmsStandard.Title);
                            }
                        }
                    }

                    // Map Owners (by email)
                    if (cmsStandard.Owners != null && cmsStandard.Owners.Any())
                    {
                        foreach (var cmsOwner in cmsStandard.Owners)
                        {
                            if (!string.IsNullOrWhiteSpace(cmsOwner.Email))
                            {
                                var user = await _context.Users
                                    .FirstOrDefaultAsync(u => u.Email.ToLower() == cmsOwner.Email.ToLower());

                                if (user != null)
                                {
                                    var owner = new DdtStandardOwner
                                    {
                                        DdtStandardId = ddtStandard.Id,
                                        UserId = user.Id,
                                        CreatedAt = DateTime.UtcNow,
                                        UpdatedAt = DateTime.UtcNow
                                    };
                                    _context.DdtStandardOwners.Add(owner);
                                }
                                else
                                {
                                    _logger.LogWarning("Owner user with email '{Email}' not found for standard '{Title}'", 
                                        cmsOwner.Email, cmsStandard.Title);
                                }
                            }
                        }
                    }

                    // Map Contacts (by email)
                    if (cmsStandard.Contacts != null && cmsStandard.Contacts.Any())
                    {
                        foreach (var cmsContact in cmsStandard.Contacts)
                        {
                            if (!string.IsNullOrWhiteSpace(cmsContact.Email))
                            {
                                var user = await _context.Users
                                    .FirstOrDefaultAsync(u => u.Email.ToLower() == cmsContact.Email.ToLower());

                                if (user != null)
                                {
                                    var contact = new DdtStandardContact
                                    {
                                        DdtStandardId = ddtStandard.Id,
                                        UserId = user.Id,
                                        CreatedAt = DateTime.UtcNow,
                                        UpdatedAt = DateTime.UtcNow
                                    };
                                    _context.DdtStandardContacts.Add(contact);
                                }
                                else
                                {
                                    _logger.LogWarning("Contact user with email '{Email}' not found for standard '{Title}'", 
                                        cmsContact.Email, cmsStandard.Title);
                                }
                            }
                        }
                    }

                    // Map Approved Products
                    if (cmsStandard.ApprovedProducts != null && cmsStandard.ApprovedProducts.Any())
                    {
                        foreach (var cmsProduct in cmsStandard.ApprovedProducts)
                        {
                            // Find or create StandardProduct
                            var standardProduct = await _context.StandardProducts
                                .FirstOrDefaultAsync(sp => sp.Name == cmsProduct.Title);

                            if (standardProduct == null)
                            {
                                standardProduct = new StandardProduct
                                {
                                    Name = cmsProduct.Title,
                                    Description = cmsProduct.UseCase,
                                    Provider = cmsProduct.Vendor,
                                    Version = cmsProduct.Version,
                                    ApprovalStatus = "Approved",
                                    CreatedByUserId = currentUserId.Value,
                                    CreatedAt = DateTime.UtcNow,
                                    UpdatedAt = DateTime.UtcNow
                                };
                                _context.StandardProducts.Add(standardProduct);
                                await _context.SaveChangesAsync();
                            }

                            var ddtProduct = new DdtStandardProduct
                            {
                                DdtStandardId = ddtStandard.Id,
                                StandardProductId = standardProduct.Id,
                                ProductType = "Approved",
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow
                            };
                            _context.DdtStandardProducts.Add(ddtProduct);
                        }
                    }

                    // Map Tolerated Products
                    if (cmsStandard.ToleratedProducts != null && cmsStandard.ToleratedProducts.Any())
                    {
                        foreach (var cmsProduct in cmsStandard.ToleratedProducts)
                        {
                            // Find or create StandardProduct
                            var standardProduct = await _context.StandardProducts
                                .FirstOrDefaultAsync(sp => sp.Name == cmsProduct.Title);

                            if (standardProduct == null)
                            {
                                standardProduct = new StandardProduct
                                {
                                    Name = cmsProduct.Title,
                                    Description = cmsProduct.UseCase,
                                    Provider = cmsProduct.Vendor,
                                    Version = cmsProduct.Version,
                                    ApprovalStatus = "Approved",
                                    CreatedByUserId = currentUserId.Value,
                                    CreatedAt = DateTime.UtcNow,
                                    UpdatedAt = DateTime.UtcNow
                                };
                                _context.StandardProducts.Add(standardProduct);
                                await _context.SaveChangesAsync();
                            }

                            var ddtProduct = new DdtStandardProduct
                            {
                                DdtStandardId = ddtStandard.Id,
                                StandardProductId = standardProduct.Id,
                                ProductType = "Tolerated",
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow
                            };
                            _context.DdtStandardProducts.Add(ddtProduct);
                        }
                    }

                    await _context.SaveChangesAsync();
                    migrated++;
                    _logger.LogInformation("Successfully migrated standard '{Title}' (ID: {Id})", cmsStandard.Title, ddtStandard.Id);
                }
                catch (Exception ex)
                {
                    errors++;
                    var errorMsg = $"Error migrating standard '{cmsStandard.Title}': {ex.Message}";
                    errorMessages.Add(errorMsg);
                    _logger.LogError(ex, "Error migrating standard '{Title}'", cmsStandard.Title);
                }
            }

            var message = $"Migration completed: {migrated} migrated, {skipped} skipped, {errors} errors.";
            if (errorMessages.Any())
            {
                message += $" Errors: {string.Join("; ", errorMessages.Take(5))}";
                if (errorMessages.Count > 5)
                {
                    message += $" (and {errorMessages.Count - 5} more)";
                }
            }

            if (errors > 0)
            {
                TempData["ErrorMessage"] = message;
            }
            else
            {
                TempData["SuccessMessage"] = message;
            }

            _logger.LogInformation("Migration completed: {Migrated} migrated, {Skipped} skipped, {Errors} errors", 
                migrated, skipped, errors);

            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during CMS migration");
            TempData["ErrorMessage"] = $"Migration failed: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// Update standard title
    /// Only accessible by Standards Managers
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStandardTitle(int id, string title)
    {
        if (!await IsStandardsManagerAsync())
        {
            TempData["ErrorMessage"] = "You do not have permission to perform this action.";
            return RedirectToAction(nameof(Index));
        }

        var standard = await _context.DdtStandards
            .Include(s => s.Owners)
            .Include(s => s.Contacts)
            .Include(s => s.Categories)
            .Include(s => s.SubCategories)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

        if (standard == null)
        {
            TempData["ErrorMessage"] = "Standard not found.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                TempData["ErrorMessage"] = "Title is required.";
                return RedirectToAction(nameof(Index));
            }

            standard.Title = title.Trim();
            standard.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Standard title updated successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating standard title for standard {StandardId}", id);
            TempData["ErrorMessage"] = $"Error updating title: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Update standard owners
    /// Only accessible by Standards Managers
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStandardOwners(int id, int[]? ownerIds)
    {
        if (!await IsStandardsManagerAsync())
        {
            TempData["ErrorMessage"] = "You do not have permission to perform this action.";
            return RedirectToAction(nameof(Index));
        }

        var standard = await _context.DdtStandards
            .Include(s => s.Owners)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

        if (standard == null)
        {
            TempData["ErrorMessage"] = "Standard not found.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            // Remove existing owners not in the new list
            if (ownerIds != null && ownerIds.Length > 0)
            {
                var ownersToRemove = standard.Owners.Where(o => !ownerIds.Contains(o.UserId)).ToList();
                foreach (var owner in ownersToRemove)
                {
                    _context.DdtStandardOwners.Remove(owner);
                }

                // Add new owners
                var existingOwnerIds = standard.Owners.Select(o => o.UserId).ToList();
                var newOwnerIds = ownerIds.Where(uid => !existingOwnerIds.Contains(uid)).ToList();
                foreach (var userId in newOwnerIds)
                {
                    var owner = new DdtStandardOwner
                    {
                        DdtStandardId = standard.Id,
                        UserId = userId,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.DdtStandardOwners.Add(owner);
                }
            }
            else
            {
                // Remove all owners if none specified
                _context.DdtStandardOwners.RemoveRange(standard.Owners);
            }

            standard.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Standard owners updated successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating standard owners for standard {StandardId}", id);
            TempData["ErrorMessage"] = $"Error updating owners: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Update standard contacts
    /// Only accessible by Standards Managers
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStandardContacts(int id, int[]? contactIds)
    {
        if (!await IsStandardsManagerAsync())
        {
            TempData["ErrorMessage"] = "You do not have permission to perform this action.";
            return RedirectToAction(nameof(Index));
        }

        var standard = await _context.DdtStandards
            .Include(s => s.Contacts)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

        if (standard == null)
        {
            TempData["ErrorMessage"] = "Standard not found.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            // Remove existing contacts not in the new list
            if (contactIds != null && contactIds.Length > 0)
            {
                var contactsToRemove = standard.Contacts.Where(c => !contactIds.Contains(c.UserId)).ToList();
                foreach (var contact in contactsToRemove)
                {
                    _context.DdtStandardContacts.Remove(contact);
                }

                // Add new contacts
                var existingContactIds = standard.Contacts.Select(c => c.UserId).ToList();
                var newContactIds = contactIds.Where(uid => !existingContactIds.Contains(uid)).ToList();
                foreach (var userId in newContactIds)
                {
                    var contact = new DdtStandardContact
                    {
                        DdtStandardId = standard.Id,
                        UserId = userId,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.DdtStandardContacts.Add(contact);
                }
            }
            else
            {
                // Remove all contacts if none specified
                _context.DdtStandardContacts.RemoveRange(standard.Contacts);
            }

            standard.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Standard contacts updated successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating standard contacts for standard {StandardId}", id);
            TempData["ErrorMessage"] = $"Error updating contacts: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Update standard categories
    /// Only accessible by Standards Managers
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStandardCategories(int id, int[]? categoryIds)
    {
        if (!await IsStandardsManagerAsync())
        {
            TempData["ErrorMessage"] = "You do not have permission to perform this action.";
            return RedirectToAction(nameof(Index));
        }

        var standard = await _context.DdtStandards
            .Include(s => s.Categories)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

        if (standard == null)
        {
            TempData["ErrorMessage"] = "Standard not found.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            // Remove existing categories not in the new list
            if (categoryIds != null && categoryIds.Length > 0)
            {
                var categoriesToRemove = standard.Categories.Where(c => !categoryIds.Contains(c.CategoryId)).ToList();
                foreach (var category in categoriesToRemove)
                {
                    _context.DdtStandardCategories.Remove(category);
                }

                // Add new categories
                var existingCategoryIds = standard.Categories.Select(c => c.CategoryId).ToList();
                var newCategoryIds = categoryIds.Where(cid => !existingCategoryIds.Contains(cid)).ToList();
                foreach (var categoryId in newCategoryIds)
                {
                    var category = new DdtStandardCategory
                    {
                        DdtStandardId = standard.Id,
                        CategoryId = categoryId,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.DdtStandardCategories.Add(category);
                }
            }
            else
            {
                // Remove all categories if none specified
                _context.DdtStandardCategories.RemoveRange(standard.Categories);
            }

            standard.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Standard categories updated successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating standard categories for standard {StandardId}", id);
            TempData["ErrorMessage"] = $"Error updating categories: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Update standard sub-categories
    /// Only accessible by Standards Managers
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStandardSubCategories(int id, int[]? subCategoryIds)
    {
        if (!await IsStandardsManagerAsync())
        {
            TempData["ErrorMessage"] = "You do not have permission to perform this action.";
            return RedirectToAction(nameof(Index));
        }

        var standard = await _context.DdtStandards
            .Include(s => s.SubCategories)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

        if (standard == null)
        {
            TempData["ErrorMessage"] = "Standard not found.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            // Remove existing sub-categories not in the new list
            if (subCategoryIds != null && subCategoryIds.Length > 0)
            {
                var subCategoriesToRemove = standard.SubCategories.Where(sc => !subCategoryIds.Contains(sc.SubCategoryId)).ToList();
                foreach (var subCategory in subCategoriesToRemove)
                {
                    _context.DdtStandardSubCategories.Remove(subCategory);
                }

                // Add new sub-categories
                var existingSubCategoryIds = standard.SubCategories.Select(sc => sc.SubCategoryId).ToList();
                var newSubCategoryIds = subCategoryIds.Where(scid => !existingSubCategoryIds.Contains(scid)).ToList();
                foreach (var subCategoryId in newSubCategoryIds)
                {
                    var subCategory = new DdtStandardSubCategory
                    {
                        DdtStandardId = standard.Id,
                        SubCategoryId = subCategoryId,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.DdtStandardSubCategories.Add(subCategory);
                }
            }
            else
            {
                // Remove all sub-categories if none specified
                _context.DdtStandardSubCategories.RemoveRange(standard.SubCategories);
            }

            standard.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Standard sub-categories updated successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating standard sub-categories for standard {StandardId}", id);
            TempData["ErrorMessage"] = $"Error updating sub-categories: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Update standard legacy reference
    /// Only accessible by Standards Managers
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStandardLegacyReference(int id, string? legacyReference)
    {
        if (!await IsStandardsManagerAsync())
        {
            TempData["ErrorMessage"] = "You do not have permission to perform this action.";
            return RedirectToAction(nameof(Index));
        }

        var standard = await _context.DdtStandards
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

        if (standard == null)
        {
            TempData["ErrorMessage"] = "Standard not found.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            standard.LegacyReference = legacyReference?.Trim();
            standard.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Standard legacy reference updated successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating legacy reference for standard {Id}", id);
            TempData["ErrorMessage"] = $"Error updating legacy reference: {ex.Message}";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>
    /// Migrate old version formats to semantic versioning (MAJOR.MINOR.PATCH)
    /// Converts versions like 1.0, 0.1, 0.0 to 1.0.0, 0.1.0, 1.0.0
    /// Also links standards with the same title as parent/child relationships
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> MigrateVersionsToSemantic()
    {
        if (!await IsStandardsManagerAsync())
        {
            TempData["ErrorMessage"] = "You do not have permission to migrate versions. Only Standards Managers can perform this action.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            _logger.LogInformation("Starting version migration to semantic versioning");

            // Get all standards
            var standards = await _context.DdtStandards
                .Where(s => !s.IsDeleted)
                .ToListAsync();

            int updated = 0;
            int linked = 0;
            int errors = 0;
            var changes = new List<string>();

            // Step 1: Convert all versions to semantic format
            foreach (var standard in standards)
            {
                try
                {
                    var originalVersion = standard.Version;
                    var originalPreviousVersion = standard.PreviousVersion;
                    bool versionChanged = false;
                    bool previousVersionChanged = false;

                    // Convert Version to semantic format if needed
                    if (!string.IsNullOrWhiteSpace(standard.Version))
                    {
                        var newVersion = ConvertToSemanticVersion(standard.Version);
                        if (newVersion != standard.Version)
                        {
                            standard.Version = newVersion;
                            versionChanged = true;
                        }
                    }
                    else
                    {
                        // If version is empty, set to 1.0.0
                        standard.Version = "1.0.0";
                        versionChanged = true;
                    }

                    // Convert PreviousVersion to semantic format if needed
                    if (!string.IsNullOrWhiteSpace(standard.PreviousVersion))
                    {
                        var newPreviousVersion = ConvertToSemanticVersion(standard.PreviousVersion);
                        if (newPreviousVersion != standard.PreviousVersion)
                        {
                            standard.PreviousVersion = newPreviousVersion;
                            previousVersionChanged = true;
                        }
                    }

                    // Fix cases where PreviousVersion equals Version (should be null for first version)
                    if (standard.PreviousVersion == standard.Version)
                    {
                        standard.PreviousVersion = null;
                        previousVersionChanged = true;
                    }

                    if (versionChanged || previousVersionChanged)
                    {
                        standard.UpdatedAt = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                        updated++;
                        
                        var changeMsg = $"Standard ID {standard.Id} ({standard.Title}): Version {originalVersion} → {standard.Version}";
                        if (previousVersionChanged)
                        {
                            changeMsg += $", PreviousVersion {originalPreviousVersion ?? "NULL"} → {standard.PreviousVersion ?? "NULL"}";
                        }
                        changes.Add(changeMsg);
                        
                        _logger.LogInformation("Updated version for standard {Id}: {OriginalVersion} → {NewVersion}", 
                            standard.Id, originalVersion, standard.Version);
                    }
                }
                catch (Exception ex)
                {
                    errors++;
                    _logger.LogError(ex, "Error migrating version for standard {Id}", standard.Id);
                }
            }

            // Step 2: Link standards with the same title as parent/child relationships
            // Group by title (case-insensitive)
            var standardsByTitle = standards
                .Where(s => !string.IsNullOrWhiteSpace(s.Title))
                .GroupBy(s => s.Title.Trim(), StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .ToList();

            foreach (var group in standardsByTitle)
            {
                try
                {
                    var titleStandards = group.OrderBy(s =>
                    {
                        var version = TryParseVersion(s.Version);
                        return version ?? new Version(0, 0, 0);
                    }).ThenBy(s => s.UpdatedAt).ToList();

                    // The first standard (oldest/lowest version) is the root
                    // Each subsequent standard should have the previous one as parent
                    for (int i = 1; i < titleStandards.Count; i++)
                    {
                        var currentStandard = titleStandards[i];
                        var parentStandard = titleStandards[i - 1];

                        // Only set parent if not already set or if it's different
                        if (currentStandard.ParentStandardId != parentStandard.Id)
                        {
                            currentStandard.ParentStandardId = parentStandard.Id;
                            currentStandard.UpdatedAt = DateTime.UtcNow;
                            await _context.SaveChangesAsync();
                            linked++;
                            
                            changes.Add($"Linked standard ID {currentStandard.Id} ({currentStandard.Title} v{currentStandard.Version}) to parent ID {parentStandard.Id} (v{parentStandard.Version})");
                            _logger.LogInformation("Linked standard {CurrentId} to parent {ParentId}", 
                                currentStandard.Id, parentStandard.Id);
                        }
                    }
                }
                catch (Exception ex)
                {
                    errors++;
                    _logger.LogError(ex, "Error linking standards for title '{Title}'", group.Key);
                }
            }

            var message = $"Version migration completed: {updated} versions updated, {linked} relationships linked, {errors} errors.";
            if (changes.Any())
            {
                _logger.LogInformation("Version migration changes:\n{Changes}", string.Join("\n", changes.Take(50)));
            }

            if (errors > 0)
            {
                TempData["ErrorMessage"] = message;
            }
            else
            {
                TempData["SuccessMessage"] = message;
            }

            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during version migration");
            TempData["ErrorMessage"] = $"Version migration failed: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// Update LegacyReference for existing standards from CMS StandardId
    /// This updates standards that were migrated but don't have LegacyReference set
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateLegacyReferencesFromCms()
    {
        if (!await IsStandardsManagerAsync())
        {
            TempData["ErrorMessage"] = "You do not have permission to perform this action. Only Standards Managers can update legacy references.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            _logger.LogInformation("Starting LegacyReference update from CMS StandardId");

            // Get all standards without LegacyReference
            var standardsWithoutLegacyRef = await _context.DdtStandards
                .Include(s => s.ParentStandard)
                .Where(s => !s.IsDeleted && string.IsNullOrEmpty(s.LegacyReference))
                .ToListAsync();

            if (!standardsWithoutLegacyRef.Any())
            {
                TempData["SuccessMessage"] = "All standards already have LegacyReference set.";
                return RedirectToAction(nameof(Index));
            }

            // Fetch all standards from CMS to get StandardId mappings
            var cmsStandards = await _standardsCmsApiService.GetStandardsAsync(
                published: null, // Get both published and unpublished
                cacheDuration: null // Don't cache for migration
            );

            // Create a lookup: LegacyId -> StandardId from CMS
            var legacyIdToStandardId = new Dictionary<string, int?>();
            foreach (var cmsStandard in cmsStandards)
            {
                if (cmsStandard.StandardId.HasValue)
                {
                    // Map by DocumentId
                    if (!string.IsNullOrEmpty(cmsStandard.DocumentId))
                    {
                        legacyIdToStandardId[cmsStandard.DocumentId] = cmsStandard.StandardId;
                    }
                    // Map by LegacyId (numeric)
                    if (cmsStandard.LegacyId.HasValue)
                    {
                        legacyIdToStandardId[cmsStandard.LegacyId.Value.ToString()] = cmsStandard.StandardId;
                    }
                }
            }

            int updated = 0;
            int inherited = 0;
            int generated = 0;
            int errors = 0;
            var changes = new List<string>();

            // First pass: Update standards with LegacyId from CMS
            foreach (var standard in standardsWithoutLegacyRef)
            {
                try
                {
                    // If has parent, inherit from parent first
                    if (standard.ParentStandardId.HasValue && standard.ParentStandard != null)
                    {
                        if (!string.IsNullOrEmpty(standard.ParentStandard.LegacyReference))
                        {
                            standard.LegacyReference = standard.ParentStandard.LegacyReference;
                            inherited++;
                            changes.Add($"Standard ID {standard.Id} ({standard.Title}): Inherited LegacyReference '{standard.LegacyReference}' from parent");
                        }
                    }
                    // If has LegacyId, try to get StandardId from CMS
                    else if (!string.IsNullOrEmpty(standard.LegacyId))
                    {
                        if (legacyIdToStandardId.TryGetValue(standard.LegacyId, out var standardId) && standardId.HasValue)
                        {
                            standard.LegacyReference = $"STD-{standardId.Value}";
                            updated++;
                            changes.Add($"Standard ID {standard.Id} ({standard.Title}): Set LegacyReference '{standard.LegacyReference}' from CMS StandardId {standardId.Value}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    errors++;
                    _logger.LogError(ex, "Error updating LegacyReference for standard {Id}", standard.Id);
                }
            }

            // Second pass: Generate LegacyReference for standards without LegacyId or CMS mapping
            // Group by slug to ensure all versions share the same LegacyReference
            var standardsNeedingGeneration = standardsWithoutLegacyRef
                .Where(s => string.IsNullOrEmpty(s.LegacyReference))
                .GroupBy(s => s.Slug)
                .ToList();

            // Get the highest existing LegacyReference number
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

            // Assign LegacyReference to each group (all versions of same standard get same LegacyReference)
            foreach (var group in standardsNeedingGeneration)
            {
                maxNumber++;
                var legacyRef = $"STD-{maxNumber}";
                
                foreach (var standard in group)
                {
                    standard.LegacyReference = legacyRef;
                    generated++;
                    changes.Add($"Standard ID {standard.Id} ({standard.Title}): Generated LegacyReference '{legacyRef}'");
                }
            }

            await _context.SaveChangesAsync();

            var message = $"LegacyReference update completed. Updated: {updated} from CMS, Inherited: {inherited} from parents, Generated: {generated} new, Errors: {errors}";
            _logger.LogInformation(message);

            if (changes.Any())
            {
                _logger.LogInformation("LegacyReference update changes:\n{Changes}", string.Join("\n", changes.Take(50)));
            }

            if (errors > 0)
            {
                TempData["ErrorMessage"] = message;
            }
            else
            {
                TempData["SuccessMessage"] = message;
            }

            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during LegacyReference update");
            TempData["ErrorMessage"] = $"LegacyReference update failed: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
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
        if (!await IsStandardsManagerAsync())
        {
            TempData["ErrorMessage"] = "You do not have permission to clean up standards. Only Standards Managers can perform this action.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var standard = await _context.DdtStandards
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

        if (standard == null)
        {
            TempData["ErrorMessage"] = "Standard not found.";
            return RedirectToAction(nameof(Index));
        }

        // Only allow cleanup of standards in "For Approval" or "Awaiting Publication"
        if (standard.Stage != "For Approval" && standard.Stage != "Awaiting Publication")
        {
            TempData["ErrorMessage"] = "This standard is not in a state that can be cleaned up. Only standards in 'For Approval' or 'Awaiting Publication' can be cleaned up.";
            return RedirectToAction(nameof(Details), new { id });
        }

        try
        {
            // Find related standards with the same title
            var relatedStandards = await _context.DdtStandards
                .Where(s => !s.IsDeleted 
                    && s.Id != id 
                    && s.Title == standard.Title
                    && s.Stage == "Published"
                    && s.IsPublished)
                .ToListAsync();

            if (!relatedStandards.Any())
            {
                TempData["ErrorMessage"] = "No published version of this standard was found. This standard cannot be cleaned up.";
                return RedirectToAction(nameof(Details), new { id });
            }

            // Check if any published version is newer (higher version number)
            var currentVersion = TryParseVersion(standard.Version) ?? new Version(0, 0, 0);
            var hasNewerPublishedVersion = relatedStandards.Any(s =>
            {
                var publishedVersion = TryParseVersion(s.Version) ?? new Version(0, 0, 0);
                return publishedVersion > currentVersion;
            });

            if (!hasNewerPublishedVersion)
            {
                TempData["ErrorMessage"] = "No newer published version of this standard was found. This standard cannot be cleaned up.";
                return RedirectToAction(nameof(Details), new { id });
            }

            // Soft delete the orphaned standard
            standard.IsDeleted = true;
            standard.UpdatedAt = DateTime.UtcNow;

            // Create audit log entry
            await CreateAuditLogAsync(standard.Id, "Deleted", 
                $"Orphaned standard cleaned up. Title: {standard.Title}, Version: {standard.Version}, Stage: {standard.Stage}. A newer published version exists.");

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Orphaned standard '{standard.Title}' (Version {standard.Version}) has been cleaned up. A newer published version exists.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up orphaned standard {Id}", id);
            TempData["ErrorMessage"] = $"Error cleaning up standard: {ex.Message}";
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}

