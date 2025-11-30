using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Compass.Data;
using Compass.Models;
using Compass.Security;
using System.Security.Claims;

namespace Compass.Controllers;

/// <summary>
/// Controller for managing Technology Code of Practice points
/// </summary>
[Authorize]
[Route("standards/technology-code-of-practice")]
public class TechnologyCodeOfPracticeController : Controller
{
    private readonly CompassDbContext _context;
    private readonly ILogger<TechnologyCodeOfPracticeController> _logger;

    public TechnologyCodeOfPracticeController(
        CompassDbContext context,
        ILogger<TechnologyCodeOfPracticeController> logger)
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
    /// Dashboard view showing all 13 TCoP points
    /// </summary>
    [Route("")]
    [Route("Index")]
    public async Task<IActionResult> Index()
    {
        var points = await _context.TechnologyCodeOfPractice
            .Include(t => t.TechnologyCodeOfPracticeProfessions)
                .ThenInclude(tcp => tcp.DdatProfession)
            .OrderBy(t => t.PointNumber)
            .ToListAsync();

        ViewBag.TotalPoints = points.Count;
        ViewBag.ActivePoints = points.Count(p => p.IsActive);

        return View(points);
    }

    /// <summary>
    /// View details of a specific point
    /// </summary>
    [Route("Details/{id}")]
    public async Task<IActionResult> Details(int id)
    {
        var point = await _context.TechnologyCodeOfPractice
            .Include(t => t.TechnologyCodeOfPracticeProfessions)
                .ThenInclude(tcp => tcp.DdatProfession)
            .Include(t => t.PhaseGuidance)
            .Include(t => t.CreatedByUser)
            .Include(t => t.UpdatedByUser)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (point == null)
        {
            return NotFound();
        }

        return View(point);
    }

    /// <summary>
    /// Edit a point
    /// </summary>
    [HttpGet]
    [Route("Edit/{id}")]
    public async Task<IActionResult> Edit(int id)
    {
        var point = await _context.TechnologyCodeOfPractice
            .Include(t => t.TechnologyCodeOfPracticeProfessions)
            .FirstOrDefaultAsync(t => t.Id == id);
        
        if (point == null)
        {
            return NotFound();
        }

        // Load all active professions, grouped by RoleGroup and alphabetized
        // Note: RoleGroup is not in database yet, so we order in memory
        var allProfessions = (await _context.DdatProfessions
            .Where(p => p.IsActive)
            .ToListAsync())
            .OrderBy(p => p.RoleGroup ?? "ZZZ") // Put nulls at the end
            .ThenBy(p => p.Name)
            .ToList();

        ViewBag.AllProfessions = allProfessions;
        ViewBag.SelectedProfessionIds = point.TechnologyCodeOfPracticeProfessions
            .Select(tcp => tcp.DdatProfessionId)
            .ToList();

        return View(point);
    }

    /// <summary>
    /// Update a point
    /// </summary>
    [HttpPost]
    [Route("Edit/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, TechnologyCodeOfPractice model, [FromForm] int[]? selectedProfessionIds)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                var point = await _context.TechnologyCodeOfPractice
                    .Include(t => t.TechnologyCodeOfPracticeProfessions)
                    .FirstOrDefaultAsync(t => t.Id == id);
                
                if (point == null)
                {
                    return NotFound();
                }

                point.Title = model.Title;
                point.Slug = model.Slug;
                point.Summary = model.Summary;
                point.Description = model.Description;
                point.GuidanceUrl = model.GuidanceUrl;
                point.IsActive = model.IsActive;
                point.DisplayOrder = model.DisplayOrder;
                point.UpdatedAt = DateTime.UtcNow;
                point.UpdatedByUserId = GetCurrentUserId();

                // Update professions
                var selectedIds = selectedProfessionIds ?? Array.Empty<int>();
                
                // Remove professions that are no longer selected
                var toRemove = point.TechnologyCodeOfPracticeProfessions
                    .Where(tcp => !selectedIds.Contains(tcp.DdatProfessionId))
                    .ToList();
                
                foreach (var remove in toRemove)
                {
                    _context.TechnologyCodeOfPracticeProfessions.Remove(remove);
                }

                // Add new professions
                var existingIds = point.TechnologyCodeOfPracticeProfessions
                    .Select(tcp => tcp.DdatProfessionId)
                    .ToList();
                
                var toAdd = selectedIds
                    .Where(pid => !existingIds.Contains(pid))
                    .ToList();

                foreach (var professionId in toAdd)
                {
                    var profession = await _context.DdatProfessions.FindAsync(professionId);
                    if (profession != null)
                    {
                        point.TechnologyCodeOfPracticeProfessions.Add(new TechnologyCodeOfPracticeProfession
                        {
                            TechnologyCodeOfPracticeId = point.Id,
                            DdatProfessionId = professionId,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Technology Code of Practice point updated successfully.";
                return RedirectToAction(nameof(Details), new { id = point.Id });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.TechnologyCodeOfPractice.AnyAsync(e => e.Id == id))
                {
                    return NotFound();
                }
                throw;
            }
        }

        // Reload professions for the view if validation fails, grouped by RoleGroup and alphabetized
        // Note: RoleGroup is not in database yet, so we order in memory
        var allProfessions = (await _context.DdatProfessions
            .Where(p => p.IsActive)
            .ToListAsync())
            .OrderBy(p => p.RoleGroup ?? "ZZZ") // Put nulls at the end
            .ThenBy(p => p.Name)
            .ToList();

        ViewBag.AllProfessions = allProfessions;
        ViewBag.SelectedProfessionIds = selectedProfessionIds?.ToList() ?? new List<int>();

        return View(model);
    }

    /// <summary>
    /// Manage phase guidance for a TCoP point
    /// </summary>
    [HttpGet("ManagePhaseGuidance/{id}")]
    public async Task<IActionResult> ManagePhaseGuidance(int id)
    {
        var point = await _context.TechnologyCodeOfPractice
            .Include(t => t.PhaseGuidance)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (point == null)
        {
            return NotFound();
        }

        ViewBag.Point = point;
        ViewBag.Phases = new[] { "Discovery", "Alpha", "Beta", "Live" };

        return View(point);
    }

    /// <summary>
    /// Create or update phase guidance
    /// </summary>
    [HttpPost("SavePhaseGuidance")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SavePhaseGuidance(int pointId, string phase, string Guidance, string KeyActivities, string QuestionsToConsider)
    {
        var point = await _context.TechnologyCodeOfPractice.FindAsync(pointId);
        if (point == null)
        {
            return NotFound();
        }

        var existingGuidance = await _context.TechnologyCodeOfPracticePhaseGuidance
            .FirstOrDefaultAsync(pg => pg.TechnologyCodeOfPracticeId == pointId && pg.Phase == phase);

        if (existingGuidance != null)
        {
            existingGuidance.Guidance = Guidance;
            existingGuidance.KeyActivities = KeyActivities;
            existingGuidance.QuestionsToConsider = QuestionsToConsider;
            existingGuidance.UpdatedAt = DateTime.UtcNow;
            existingGuidance.UpdatedByUserId = GetCurrentUserId();
        }
        else
        {
            var newGuidance = new TechnologyCodeOfPracticePhaseGuidance
            {
                TechnologyCodeOfPracticeId = pointId,
                Phase = phase,
                Guidance = Guidance,
                KeyActivities = KeyActivities,
                QuestionsToConsider = QuestionsToConsider,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedByUserId = GetCurrentUserId(),
                UpdatedByUserId = GetCurrentUserId()
            };
            _context.TechnologyCodeOfPracticePhaseGuidance.Add(newGuidance);
        }

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Phase guidance for {phase} saved successfully.";
        return RedirectToAction(nameof(ManagePhaseGuidance), new { id = pointId });
    }

    private bool TechnologyCodeOfPracticeExists(int id)
    {
        return _context.TechnologyCodeOfPractice.Any(e => e.Id == id);
    }
}

