using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Compass.Data;
using Compass.Models;
using Compass.Security;
using System.Security.Claims;

namespace Compass.Controllers;

/// <summary>
/// Controller for managing GOV.UK Service Standards and phase guidance
/// </summary>
[Authorize]
[Route("standards/service-standards")]
public class ServiceStandardsController : Controller
{
    private readonly CompassDbContext _context;
    private readonly ILogger<ServiceStandardsController> _logger;

    public ServiceStandardsController(
        CompassDbContext context,
        ILogger<ServiceStandardsController> logger)
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
    /// Dashboard view showing all 14 Service Standards
    /// </summary>
    [Route("")]
    [Route("Index")]
    public async Task<IActionResult> Index()
    {
        var standards = await _context.ServiceStandards
            .Include(s => s.PhaseGuidance)
            .Include(s => s.ServiceStandardProfessions)
                .ThenInclude(ssp => ssp.DdatProfession)
            .OrderBy(s => s.StandardNumber)
            .ToListAsync();

        ViewBag.TotalStandards = standards.Count;
        ViewBag.ActiveStandards = standards.Count(s => s.IsActive);
        ViewBag.StandardsWithGuidance = standards.Count(s => s.PhaseGuidance.Any());

        return View(standards);
    }

    /// <summary>
    /// View details of a specific standard
    /// </summary>
    [Route("Details/{id}")]
    public async Task<IActionResult> Details(int id)
    {
        var standard = await _context.ServiceStandards
            .Include(s => s.PhaseGuidance.OrderBy(pg => pg.Phase))
            .Include(s => s.ServiceStandardProfessions)
                .ThenInclude(ssp => ssp.DdatProfession)
            .Include(s => s.CreatedByUser)
            .Include(s => s.UpdatedByUser)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (standard == null)
        {
            return NotFound();
        }

        return View(standard);
    }

    /// <summary>
    /// Edit a standard
    /// </summary>
    [HttpGet]
    [Route("Edit/{id}")]
    public async Task<IActionResult> Edit(int id)
    {
        var standard = await _context.ServiceStandards
            .Include(s => s.ServiceStandardProfessions)
            .FirstOrDefaultAsync(s => s.Id == id);
        
        if (standard == null)
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
        ViewBag.SelectedProfessionIds = standard.ServiceStandardProfessions
            .Select(ssp => ssp.DdatProfessionId)
            .ToList();

        return View(standard);
    }

    /// <summary>
    /// Update a standard
    /// </summary>
    [HttpPost]
    [Route("Edit/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ServiceStandard model, [FromForm] int[]? selectedProfessionIds)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                var standard = await _context.ServiceStandards
                    .Include(s => s.ServiceStandardProfessions)
                    .FirstOrDefaultAsync(s => s.Id == id);
                
                if (standard == null)
                {
                    return NotFound();
                }

                standard.Title = model.Title;
                standard.Slug = model.Slug;
                standard.Summary = model.Summary;
                standard.Description = model.Description;
                standard.IsActive = model.IsActive;
                standard.DisplayOrder = model.DisplayOrder;
                standard.UpdatedAt = DateTime.UtcNow;
                standard.UpdatedByUserId = GetCurrentUserId();

                // Update professions
                var selectedIds = selectedProfessionIds ?? Array.Empty<int>();
                
                // Remove professions that are no longer selected
                var toRemove = standard.ServiceStandardProfessions
                    .Where(ssp => !selectedIds.Contains(ssp.DdatProfessionId))
                    .ToList();
                
                foreach (var remove in toRemove)
                {
                    _context.ServiceStandardProfessions.Remove(remove);
                }

                // Add new professions
                var existingIds = standard.ServiceStandardProfessions
                    .Select(ssp => ssp.DdatProfessionId)
                    .ToList();
                
                var toAdd = selectedIds
                    .Where(pid => !existingIds.Contains(pid))
                    .ToList();

                foreach (var professionId in toAdd)
                {
                    var profession = await _context.DdatProfessions.FindAsync(professionId);
                    if (profession != null)
                    {
                        standard.ServiceStandardProfessions.Add(new ServiceStandardProfession
                        {
                            ServiceStandardId = standard.Id,
                            DdatProfessionId = professionId,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Service standard updated successfully.";
                return RedirectToAction(nameof(Details), new { id = standard.Id });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.ServiceStandards.AnyAsync(e => e.Id == id))
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
    /// Manage phase guidance for a standard
    /// </summary>
    [HttpGet]
    [Route("ManagePhaseGuidance/{id}")]
    public async Task<IActionResult> ManagePhaseGuidance(int id)
    {
        var standard = await _context.ServiceStandards
            .Include(s => s.PhaseGuidance)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (standard == null)
        {
            return NotFound();
        }

        ViewBag.Standard = standard;
        ViewBag.Phases = new[] { "Discovery", "Alpha", "Beta", "Live" };

        return View(standard);
    }

    /// <summary>
    /// Create or update phase guidance
    /// </summary>
    [HttpPost]
    [Route("SavePhaseGuidance")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SavePhaseGuidance(int standardId, string phase, string Guidance, string KeyActivities, string QuestionsToConsider)
    {
        var standard = await _context.ServiceStandards.FindAsync(standardId);
        if (standard == null)
        {
            return NotFound();
        }

        var existingGuidance = await _context.ServiceStandardPhaseGuidance
            .FirstOrDefaultAsync(pg => pg.ServiceStandardId == standardId && pg.Phase == phase);

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
            var newGuidance = new ServiceStandardPhaseGuidance
            {
                ServiceStandardId = standardId,
                Phase = phase,
                Guidance = Guidance,
                KeyActivities = KeyActivities,
                QuestionsToConsider = QuestionsToConsider,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedByUserId = GetCurrentUserId(),
                UpdatedByUserId = GetCurrentUserId()
            };
            _context.ServiceStandardPhaseGuidance.Add(newGuidance);
        }

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Phase guidance for {phase} saved successfully.";
        return RedirectToAction(nameof(ManagePhaseGuidance), new { id = standardId });
    }
}

