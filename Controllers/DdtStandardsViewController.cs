using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Compass.Data;
using Compass.Models;
using Compass.Services;
using Compass.Security;
using System.Security.Claims;

namespace Compass.Controllers;

/// <summary>
/// Controller for viewing DDT Standards (public read-only access)
/// All authenticated users can view standards
/// </summary>
[Authorize]
public class DdtStandardsViewController : Controller
{
    private readonly CompassDbContext _context;
    private readonly ILogger<DdtStandardsViewController> _logger;

    public DdtStandardsViewController(
        CompassDbContext context,
        ILogger<DdtStandardsViewController> logger)
    {
        _context = context;
        _logger = logger;
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
        var allUnpublishedCount = await _context.DdtStandards
            .AsNoTracking()
            .CountAsync(s => !s.IsDeleted && s.Stage == "Published" && !s.IsPublished);

        ViewBag.CurrentSearch = search;
        ViewBag.CurrentCategory = category;
        ViewBag.Categories = categories;
        ViewBag.MyPublished = myPublished;
        ViewBag.AllPublished = allPublished;
        ViewBag.AllDraftsCount = allDraftsCount;
        ViewBag.AllInReviewCount = allInReviewCount;
        ViewBag.AllForApprovalCount = allForApprovalCount;
        ViewBag.AllPublishedCount = allPublishedCount;
        ViewBag.AllUnpublishedCount = allUnpublishedCount;

        return View("~/Views/DdtStandards/Published.cshtml", allPublished);
    }

    /// <summary>
    /// View standard details (read-only)
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
                .AsNoTracking()
                .Include(s => s.Categories).ThenInclude(c => c.Category)
                .Where(s => !s.IsDeleted 
                    && s.IsPublished 
                    && s.Stage == "Published"
                    && s.Id != id
                    && s.Categories.Any(c => categoryIds.Contains(c.CategoryId)))
                .OrderByDescending(s => s.PublishedAt)
                .Take(10)
                .ToListAsync();
        }

        ViewBag.RelatedStandards = relatedStandards;
        ViewBag.CurrentUserId = GetCurrentUserId();
        ViewBag.ShowAllVersions = showAllVersions;

        // Get previous versions of this standard
        var relatedStandardIds = new List<int>();
        
        if (standard.ParentStandardId.HasValue)
        {
            relatedStandardIds.Add(standard.ParentStandardId.Value);
        }
        
        var childIds = await _context.DdtStandards
            .AsNoTracking()
            .Where(s => !s.IsDeleted && s.ParentStandardId == id)
            .Select(s => s.Id)
            .ToListAsync();
        relatedStandardIds.AddRange(childIds);
        
        var sameTitleIds = await _context.DdtStandards
            .AsNoTracking()
            .Where(s => !s.IsDeleted && s.Id != id && s.Title == standard.Title)
            .Select(s => s.Id)
            .ToListAsync();
        relatedStandardIds.AddRange(sameTitleIds);
        
        var previousVersions = await _context.DdtStandards
            .AsNoTracking()
            .Include(s => s.CreatorUser)
            .Include(s => s.Products).ThenInclude(p => p.StandardProduct)
            .Where(s => relatedStandardIds.Distinct().Contains(s.Id))
            .ToListAsync();
        
        previousVersions = previousVersions
            .OrderByDescending(s => 
            {
                var version = TryParseVersion(s.Version);
                return version ?? new Version(0, 0, 0);
            })
            .ThenByDescending(s => s.UpdatedAt)
            .ToList();
        
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

        return View("~/Views/DdtStandards/Details.cshtml", standard);
    }

    /// <summary>
    /// Updates view - shows recent changes and recently published standards
    /// </summary>
    public async Task<IActionResult> Updates()
    {
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

        return View("~/Views/DdtStandards/Updates.cshtml");
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
}
