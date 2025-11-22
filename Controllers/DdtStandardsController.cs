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

    public DdtStandardsController(
        CompassDbContext context,
        ILogger<DdtStandardsController> logger,
        IUserDirectoryService userDirectoryService)
    {
        _context = context;
        _logger = logger;
        _userDirectoryService = userDirectoryService;
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
        bool? legalStandard = null)
    {
        // Base query for all standards in this stage
        var allQuery = _context.DdtStandards
            .Include(s => s.CreatorUser)
            .Include(s => s.Owners).ThenInclude(o => o.User)
            .Include(s => s.Contacts).ThenInclude(c => c.User)
            .Include(s => s.Categories).ThenInclude(c => c.Category)
            .Include(s => s.SubCategories).ThenInclude(sc => sc.SubCategory)
            .Include(s => s.Phases).ThenInclude(p => p.PhaseLookup)
            .Where(s => !s.IsDeleted && s.Stage == stageName)
            .AsQueryable();

        // Query for my standards (standards created by current user)
        var myQuery = _context.DdtStandards
            .Include(s => s.CreatorUser)
            .Include(s => s.Owners).ThenInclude(o => o.User)
            .Include(s => s.Contacts).ThenInclude(c => c.User)
            .Include(s => s.Categories).ThenInclude(c => c.Category)
            .Include(s => s.SubCategories).ThenInclude(sc => sc.SubCategory)
            .Include(s => s.Phases).ThenInclude(p => p.PhaseLookup)
            .Where(s => !s.IsDeleted && s.Stage == stageName && s.CreatorUserId.HasValue)
            .AsQueryable();

        if (currentUserId.HasValue)
        {
            myQuery = myQuery.Where(s => s.CreatorUserId == currentUserId.Value);
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
    /// Manage standards - list standards by stage (drafts, in review, for approval, published)
    /// </summary>
    public async Task<IActionResult> Index(string? view, string? search, string? category, int? creator, int? owner, int? contact, bool? legalStandard)
    {
        var currentUserId = GetCurrentUserId();
        var activeView = view ?? "drafts";
        
        // Normalize view names
        if (activeView == "in-review")
            activeView = "in-review";
        else if (activeView == "for-approval")
            activeView = "for-approval";
        else if (activeView == "published")
            activeView = "published";
        else
            activeView = "drafts";

        // Get standards for each stage
        var (myDrafts, allDrafts) = await GetStandardsByStageAsync("Draft", currentUserId, search, category, creator, owner, contact, legalStandard);
        var (myInReview, allInReview) = await GetStandardsByStageAsync("Under Review", currentUserId, search, category, creator, owner, contact, legalStandard);
        var (myForApproval, allForApproval) = await GetStandardsByStageAsync("Approved", currentUserId, search, category, creator, owner, contact, legalStandard);
        var (myPublished, allPublished) = await GetStandardsByStageAsync("Published", currentUserId, search, category, creator, owner, contact, legalStandard);

        // Get filter options
        var stages = await _context.DdtStandards
            .Where(s => !s.IsDeleted)
            .Select(s => s.Stage)
            .Distinct()
            .OrderBy(s => s)
            .ToListAsync();

        var categories = await _context.DdtStandardCategories
            .Include(c => c.Category)
            .Select(c => c.Category.Name)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();

        // Get creators (users who have created standards)
        var creators = await _context.DdtStandards
            .Where(s => !s.IsDeleted && s.CreatorUserId.HasValue)
            .Include(s => s.CreatorUser)
            .Select(s => new { s.CreatorUserId, s.CreatorUser!.Name })
            .Distinct()
            .OrderBy(x => x.Name)
            .ToListAsync();

        // Get owners (users who are owners of standards)
        var owners = await _context.DdtStandardOwners
            .Include(o => o.User)
            .Select(o => new { o.UserId, o.User.Name })
            .Distinct()
            .OrderBy(x => x.Name)
            .ToListAsync();

        // Get contacts (users who are contacts for standards)
        var contacts = await _context.DdtStandardContacts
            .Include(c => c.User)
            .Select(c => new { c.UserId, c.User.Name })
            .Distinct()
            .OrderBy(x => x.Name)
            .ToListAsync();

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
            Stages = stages,
            Categories = categories,
            Creators = creators.Select(c => (c.CreatorUserId!.Value, c.Name)).ToList(),
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

        return View(viewModel);
    }

    /// <summary>
    /// Published standards - public view of published standards
    /// </summary>
    public async Task<IActionResult> Published(string? search, string? category)
    {
        var query = _context.DdtStandards
            .Include(s => s.CreatorUser)
            .Include(s => s.Owners).ThenInclude(o => o.User)
            .Include(s => s.Categories).ThenInclude(c => c.Category)
            .Include(s => s.SubCategories).ThenInclude(sc => sc.SubCategory)
            .Include(s => s.Phases).ThenInclude(p => p.PhaseLookup)
            .Where(s => !s.IsDeleted && s.IsPublished && s.Stage == "Published")
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(s => 
                s.Title.Contains(search) || 
                (s.Summary != null && s.Summary.Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(s => s.Categories.Any(c => c.Category.Name == category));
        }

        var standards = await query
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

        return View(standards);
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

        ViewBag.Phases = phases;
        ViewBag.Categories = categories;

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

            // Only allow editing drafts
            if (standard.Stage != "Draft")
            {
                TempData["ErrorMessage"] = "Only draft standards can be edited in create mode.";
                return RedirectToAction(nameof(Details), new { id = id.Value });
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
        string? contactObjectIds = null)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            ModelState.AddModelError("Title", "Title is required");
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
            DdtStandard standard;
            
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
                    CreatorUserId = currentUserId.Value,
                    DraftCreated = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.DdtStandards.Add(standard);
            }

            await _context.SaveChangesAsync();

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
            DdtStandard standard;
            
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
    public async Task<IActionResult> Details(int id)
    {
        var standard = await _context.DdtStandards
            .Include(s => s.CreatorUser)
            .Include(s => s.Owners).ThenInclude(o => o.User)
            .Include(s => s.Contacts).ThenInclude(c => c.User)
            .Include(s => s.Categories).ThenInclude(c => c.Category)
            .Include(s => s.SubCategories).ThenInclude(sc => sc.SubCategory)
            .Include(s => s.Phases).ThenInclude(p => p.PhaseLookup)
            .Include(s => s.ValidationRules)
            .Include(s => s.Comments).ThenInclude(c => c.User)
            .Include(s => s.Versions).ThenInclude(v => v.CreatedByUser)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

        if (standard == null)
        {
            return NotFound();
        }

        return View(standard);
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
        var standard = await _context.DdtStandards.FindAsync(id);
        if (standard == null || standard.IsDeleted)
        {
            return NotFound();
        }

        if (standard.Stage != "Draft")
        {
            TempData["ErrorMessage"] = "Only draft standards can be submitted for review.";
            return RedirectToAction(nameof(Details), new { id });
        }

        standard.Stage = "Under Review";
        standard.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Standard submitted for review.";
        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>
    /// Approve standard
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Approve(int id, string? comment)
    {
        var standard = await _context.DdtStandards
            .Include(s => s.Versions)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

        if (standard == null)
        {
            return NotFound();
        }

        if (standard.Stage != "Under Review")
        {
            TempData["ErrorMessage"] = "Only standards under review can be approved.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var currentUserId = GetCurrentUserId();

        standard.Stage = "Approved";
        standard.GovernanceApproval = true;
        standard.UpdatedAt = DateTime.UtcNow;

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
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Standard approved successfully.";
        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>
    /// Reject standard
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Reject(int id, string reason)
    {
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

        if (standard.Stage != "Under Review")
        {
            TempData["ErrorMessage"] = "Only standards under review can be rejected.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var currentUserId = GetCurrentUserId();

        standard.Stage = "Rejected";
        standard.UpdatedAt = DateTime.UtcNow;

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
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Standard rejected. The owner can revise and resubmit.";
        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>
    /// Publish standard
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Publish(int id)
    {
        var standard = await _context.DdtStandards
            .Include(s => s.Versions)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

        if (standard == null)
        {
            return NotFound();
        }

        if (standard.Stage != "Approved")
        {
            TempData["ErrorMessage"] = "Only approved standards can be published.";
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

        // Create version entry
        var previousVersion = standard.Version;
        var versionType = DetermineVersionType(previousVersion);
        var newVersion = IncrementVersion(previousVersion, versionType);

        standard.PreviousVersion = previousVersion;
        standard.Version = newVersion;

        // Create version history entry
        var version = new DdtStandardVersion
        {
            DdtStandardId = standard.Id,
            VersionNumber = newVersion,
            PreviousVersion = previousVersion,
            VersionType = versionType,
            ChangeSummary = "Published standard",
            Status = "published",
            CreatedByUserId = currentUserId,
            PublishedByUserId = currentUserId,
            CreatedAt = now,
            PublishedAt = now
        };

        _context.DdtStandardVersions.Add(version);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Standard published successfully as version {newVersion}.";
        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>
    /// Unpublish standard
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Unpublish(int id, string? reason)
    {
        var standard = await _context.DdtStandards.FindAsync(id);
        if (standard == null || standard.IsDeleted)
        {
            return NotFound();
        }

        if (standard.Stage != "Published")
        {
            TempData["ErrorMessage"] = "Only published standards can be unpublished.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var currentUserId = GetCurrentUserId();

        standard.Stage = "Archived";
        standard.IsPublished = false;
        standard.UpdatedAt = DateTime.UtcNow;

        // Add comment if provided
        if (!string.IsNullOrWhiteSpace(reason) && currentUserId.HasValue)
        {
            _context.DdtStandardComments.Add(new DdtStandardComment
            {
                DdtStandardId = standard.Id,
                UserId = currentUserId.Value,
                Title = "Unpublish reason",
                Comments = reason,
                CommentType = "unpublish",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Standard unpublished and archived.";
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
    /// </summary>
    private static string DetermineVersionType(string version)
    {
        // For now, default to patch for published updates
        // Can be enhanced to detect breaking changes
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
}

