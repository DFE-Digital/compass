using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Compass.Data;
using Compass.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Compass.Controllers;

[Authorize]
public class FunctionalStandardController : Controller
{
    private readonly CompassDbContext _context;
    private readonly ILogger<FunctionalStandardController> _logger;

    public FunctionalStandardController(CompassDbContext context, ILogger<FunctionalStandardController> logger)
    {
        _context = context;
        _logger = logger;
    }

    #region Functional Standards

    // GET: FunctionalStandard
    public async Task<IActionResult> Index()
    {
        var standards = await _context.FunctionalStandards
            .OrderBy(fs => fs.Id)
            .ToListAsync();
        
        return View("~/Views/Admin/FunctionalStandard/Index.cshtml", standards);
    }

    // GET: FunctionalStandard/Create
    public IActionResult Create()
    {
        return View("~/Views/Admin/FunctionalStandard/Create.cshtml");
    }

    // POST: FunctionalStandard/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FunctionalStandard standard)
    {
        if (ModelState.IsValid)
        {
            try
            {
                // Check if ID already exists
                if (_context.FunctionalStandards.Any(fs => fs.Id == standard.Id))
                {
                    ModelState.AddModelError("Id", "A functional standard with this ID already exists.");
                    return View(standard);
                }
                
                standard.CreatedAt = DateTime.UtcNow;
                standard.UpdatedAt = DateTime.UtcNow;
                
                _context.FunctionalStandards.Add(standard);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = $"Functional standard '{standard.Title}' has been created successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating functional standard");
                ModelState.AddModelError("", "An error occurred while creating the functional standard. Please try again.");
            }
        }
        
        return View("~/Views/Admin/FunctionalStandard/Create.cshtml", standard);
    }

    // GET: FunctionalStandard/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var standard = await _context.FunctionalStandards.FindAsync(id);
        if (standard == null)
        {
            return NotFound();
        }

        return View("~/Views/Admin/FunctionalStandard/Edit.cshtml", standard);
    }

    // POST: FunctionalStandard/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, FunctionalStandard standard)
    {
        if (id != standard.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                standard.UpdatedAt = DateTime.UtcNow;
                
                _context.Update(standard);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = $"Functional standard '{standard.Title}' has been updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!FunctionalStandardExists(standard.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating functional standard");
                ModelState.AddModelError("", "An error occurred while updating the functional standard. Please try again.");
            }
        }
        
        return View("~/Views/Admin/FunctionalStandard/Edit.cshtml", standard);
    }

    // GET: FunctionalStandard/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var standard = await _context.FunctionalStandards.FindAsync(id);
        if (standard == null)
        {
            return NotFound();
        }

        // Check if there are any submitted assessments (SubmittedAt is not null)
        var hasActiveAssessments = await _context.FunctionalStandardAssessments
            .AnyAsync(a => a.FunctionalStandardId == id && a.SubmittedAt != null);
        
        ViewBag.HasActiveAssessments = hasActiveAssessments;

        return View("~/Views/Admin/FunctionalStandard/Delete.cshtml", standard);
    }

    // POST: FunctionalStandard/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        try
        {
            var standard = await _context.FunctionalStandards.FindAsync(id);
            if (standard != null)
            {
                // Check if there are any submitted assessments (SubmittedAt is not null)
                var hasActiveAssessments = await _context.FunctionalStandardAssessments
                    .AnyAsync(a => a.FunctionalStandardId == id && a.SubmittedAt != null);
                
                if (hasActiveAssessments)
                {
                    TempData["ErrorMessage"] = $"Cannot delete functional standard '{standard.Title}' because it has submitted assessments.";
                    return RedirectToAction(nameof(Index));
                }
                
                _context.FunctionalStandards.Remove(standard);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = $"Functional standard '{standard.Title}' has been deleted successfully.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting functional standard");
            TempData["ErrorMessage"] = "An error occurred while deleting the functional standard. Please try again.";
        }

        return RedirectToAction(nameof(Index));
    }

    #endregion

    #region Themes

    // GET: FunctionalStandard/Themes/5
    public async Task<IActionResult> Themes(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var standard = await _context.FunctionalStandards.FindAsync(id);
        if (standard == null)
        {
            return NotFound();
        }

        var themes = await _context.FunctionalStandardThemes
            .Where(t => t.FunctionalStandardId == id)
            .OrderBy(t => t.ThemeId)
            .ToListAsync();

        ViewBag.Standard = standard;
        return View("~/Views/Admin/FunctionalStandard/Themes.cshtml", themes);
    }

    // GET: FunctionalStandard/CreateTheme/5
    [Route("FunctionalStandard/CreateTheme/{standardId}")]
    public async Task<IActionResult> CreateTheme(int? standardId)
    {
        if (standardId == null)
        {
            return NotFound();
        }

        var standard = await _context.FunctionalStandards.FindAsync(standardId);
        if (standard == null)
        {
            return NotFound();
        }

        ViewBag.Standard = standard;
        var theme = new FunctionalStandardTheme { FunctionalStandardId = standardId.Value };
        return View("~/Views/Admin/FunctionalStandard/CreateTheme.cshtml", theme);
    }

    // POST: FunctionalStandard/CreateTheme
    [HttpPost]
    [Route("FunctionalStandard/CreateTheme/{standardId}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateTheme([Bind("FunctionalStandardId,ThemeId,Title,Description")] FunctionalStandardTheme theme)
    {
        if (ModelState.IsValid)
        {
            try
            {
                // Check if theme with same FunctionalStandardId and ThemeId already exists
                if (_context.FunctionalStandardThemes.Any(t => 
                    t.FunctionalStandardId == theme.FunctionalStandardId && 
                    t.ThemeId == theme.ThemeId))
                {
                    ModelState.AddModelError("ThemeId", $"A theme with ID {theme.ThemeId} already exists for this functional standard.");
                    var existingStandard = await _context.FunctionalStandards.FindAsync(theme.FunctionalStandardId);
                    ViewBag.Standard = existingStandard;
                    return View("~/Views/Admin/FunctionalStandard/CreateTheme.cshtml", theme);
                }
                
                theme.CreatedAt = DateTime.UtcNow;
                theme.UpdatedAt = DateTime.UtcNow;
                
                _context.FunctionalStandardThemes.Add(theme);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = $"Theme '{theme.Title}' has been created successfully.";
                return RedirectToAction(nameof(Themes), new { id = theme.FunctionalStandardId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating theme");
                ModelState.AddModelError("", "An error occurred while creating the theme. Please try again.");
            }
        }

        var standard = await _context.FunctionalStandards.FindAsync(theme.FunctionalStandardId);
        ViewBag.Standard = standard;
        return View("~/Views/Admin/FunctionalStandard/CreateTheme.cshtml", theme);
    }

    // GET: FunctionalStandard/EditTheme/5
    public async Task<IActionResult> EditTheme(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var theme = await _context.FunctionalStandardThemes.FindAsync(id);
        if (theme == null)
        {
            return NotFound();
        }

        var standard = await _context.FunctionalStandards.FindAsync(theme.FunctionalStandardId);
        ViewBag.Standard = standard;
        return View("~/Views/Admin/FunctionalStandard/EditTheme.cshtml", theme);
    }

    // POST: FunctionalStandard/EditTheme/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditTheme(int id, FunctionalStandardTheme theme)
    {
        if (id != theme.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                theme.UpdatedAt = DateTime.UtcNow;
                
                _context.Update(theme);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = $"Theme '{theme.Title}' has been updated successfully.";
                return RedirectToAction(nameof(Themes), new { id = theme.FunctionalStandardId });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ThemeExists(theme.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating theme");
                ModelState.AddModelError("", "An error occurred while updating the theme. Please try again.");
            }
        }

        var standard = await _context.FunctionalStandards.FindAsync(theme.FunctionalStandardId);
        ViewBag.Standard = standard;
        return View("~/Views/Admin/FunctionalStandard/EditTheme.cshtml", theme);
    }

    // POST: FunctionalStandard/DeleteTheme/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteTheme(int id)
    {
        try
        {
            var theme = await _context.FunctionalStandardThemes.FindAsync(id);
            if (theme != null)
            {
                var standardId = theme.FunctionalStandardId;
                _context.FunctionalStandardThemes.Remove(theme);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = $"Theme '{theme.Title}' has been deleted successfully.";
                return RedirectToAction(nameof(Themes), new { id = standardId });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting theme");
            TempData["ErrorMessage"] = "An error occurred while deleting the theme. Please try again.";
        }

        return RedirectToAction(nameof(Index));
    }

    #endregion

    #region Practice Areas

    // GET: FunctionalStandard/PracticeAreas/5/1
    public async Task<IActionResult> PracticeAreas(int? standardId, int? themeId)
    {
        if (standardId == null || themeId == null)
        {
            return NotFound();
        }

        var theme = await _context.FunctionalStandardThemes
            .Include(t => t.FunctionalStandard)
            .FirstOrDefaultAsync(t => t.FunctionalStandardId == standardId && t.ThemeId == themeId);

        if (theme == null)
        {
            return NotFound();
        }

        var practiceAreas = await _context.PracticeAreas
            .Where(pa => pa.FunctionalStandardId == standardId && pa.ThemeId == themeId)
            .ToListAsync();
        
        // Order by decimal in memory (SQLite doesn't support ordering by decimal in queries)
        practiceAreas = practiceAreas.OrderBy(pa => pa.PracticeAreaId).ToList();

        ViewBag.Theme = theme;
        return View("~/Views/Admin/FunctionalStandard/PracticeAreas.cshtml", practiceAreas);
    }

    // GET: FunctionalStandard/CreatePracticeArea/5/1
    public async Task<IActionResult> CreatePracticeArea(int? standardId, int? themeId)
    {
        if (standardId == null || themeId == null)
        {
            return NotFound();
        }

        var theme = await _context.FunctionalStandardThemes
            .Include(t => t.FunctionalStandard)
            .FirstOrDefaultAsync(t => t.FunctionalStandardId == standardId && t.ThemeId == themeId);

        if (theme == null)
        {
            return NotFound();
        }

        ViewBag.Theme = theme;
        var practiceArea = new PracticeArea 
        { 
            FunctionalStandardId = standardId.Value,
            ThemeId = themeId.Value
        };
        return View("~/Views/Admin/FunctionalStandard/CreatePracticeArea.cshtml", practiceArea);
    }

    // POST: FunctionalStandard/CreatePracticeArea
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreatePracticeArea([Bind("FunctionalStandardId,ThemeId,PracticeAreaId,Title,Description")] PracticeArea practiceArea)
    {
        if (ModelState.IsValid)
        {
            try
            {
                // Check if practice area with same composite key already exists
                if (_context.PracticeAreas.Any(pa => 
                    pa.FunctionalStandardId == practiceArea.FunctionalStandardId && 
                    pa.ThemeId == practiceArea.ThemeId &&
                    pa.PracticeAreaId == practiceArea.PracticeAreaId))
                {
                    ModelState.AddModelError("PracticeAreaId", $"A practice area with ID {practiceArea.PracticeAreaId} already exists for this theme.");
                    var existingTheme = await _context.FunctionalStandardThemes
                        .Include(t => t.FunctionalStandard)
                        .FirstOrDefaultAsync(t => t.FunctionalStandardId == practiceArea.FunctionalStandardId && t.ThemeId == practiceArea.ThemeId);
                    ViewBag.Theme = existingTheme;
                    return View("~/Views/Admin/FunctionalStandard/CreatePracticeArea.cshtml", practiceArea);
                }
                
                practiceArea.CreatedAt = DateTime.UtcNow;
                practiceArea.UpdatedAt = DateTime.UtcNow;
                
                _context.PracticeAreas.Add(practiceArea);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = $"Practice area '{practiceArea.Title}' has been created successfully.";
                return RedirectToAction(nameof(PracticeAreas), new { standardId = practiceArea.FunctionalStandardId, themeId = practiceArea.ThemeId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating practice area");
                ModelState.AddModelError("", "An error occurred while creating the practice area. Please try again.");
            }
        }

        var theme = await _context.FunctionalStandardThemes
            .Include(t => t.FunctionalStandard)
            .FirstOrDefaultAsync(t => t.FunctionalStandardId == practiceArea.FunctionalStandardId && t.ThemeId == practiceArea.ThemeId);
        ViewBag.Theme = theme;
        return View("~/Views/Admin/FunctionalStandard/CreatePracticeArea.cshtml", practiceArea);
    }

    // GET: FunctionalStandard/EditPracticeArea/5
    public async Task<IActionResult> EditPracticeArea(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var practiceArea = await _context.PracticeAreas.FindAsync(id);
        if (practiceArea == null)
        {
            return NotFound();
        }

        var theme = await _context.FunctionalStandardThemes
            .Include(t => t.FunctionalStandard)
            .FirstOrDefaultAsync(t => t.FunctionalStandardId == practiceArea.FunctionalStandardId && t.ThemeId == practiceArea.ThemeId);
        ViewBag.Theme = theme;
        return View("~/Views/Admin/FunctionalStandard/EditPracticeArea.cshtml", practiceArea);
    }

    // POST: FunctionalStandard/EditPracticeArea/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditPracticeArea(int id, PracticeArea practiceArea)
    {
        if (id != practiceArea.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                practiceArea.UpdatedAt = DateTime.UtcNow;
                
                _context.Update(practiceArea);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = $"Practice area '{practiceArea.Title}' has been updated successfully.";
                return RedirectToAction(nameof(PracticeAreas), new { standardId = practiceArea.FunctionalStandardId, themeId = practiceArea.ThemeId });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PracticeAreaExists(practiceArea.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating practice area");
                ModelState.AddModelError("", "An error occurred while updating the practice area. Please try again.");
            }
        }

        var theme = await _context.FunctionalStandardThemes
            .Include(t => t.FunctionalStandard)
            .FirstOrDefaultAsync(t => t.FunctionalStandardId == practiceArea.FunctionalStandardId && t.ThemeId == practiceArea.ThemeId);
        ViewBag.Theme = theme;
        return View("~/Views/Admin/FunctionalStandard/EditPracticeArea.cshtml", practiceArea);
    }

    // POST: FunctionalStandard/DeletePracticeArea/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePracticeArea(int id)
    {
        try
        {
            var practiceArea = await _context.PracticeAreas.FindAsync(id);
            if (practiceArea != null)
            {
                var standardId = practiceArea.FunctionalStandardId;
                var themeId = practiceArea.ThemeId;
                
                _context.PracticeAreas.Remove(practiceArea);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = $"Practice area '{practiceArea.Title}' has been deleted successfully.";
                return RedirectToAction(nameof(PracticeAreas), new { standardId, themeId });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting practice area");
            TempData["ErrorMessage"] = "An error occurred while deleting the practice area. Please try again.";
        }

        return RedirectToAction(nameof(Index));
    }

    #endregion

    #region Criteria

    // GET: FunctionalStandard/Criteria/5/1/1.1
    public async Task<IActionResult> Criteria(int? standardId, int? themeId, decimal? practiceAreaId)
    {
        if (standardId == null || themeId == null || practiceAreaId == null)
        {
            return NotFound();
        }

        var practiceArea = await _context.PracticeAreas
            .Include(pa => pa.Theme)
            .ThenInclude(t => t.FunctionalStandard)
            .FirstOrDefaultAsync(pa => pa.FunctionalStandardId == standardId 
                && pa.ThemeId == themeId 
                && pa.PracticeAreaId == practiceAreaId);

        if (practiceArea == null)
        {
            return NotFound();
        }

        var criteria = await _context.Criteria
            .Where(c => c.FunctionalStandardId == standardId 
                && c.ThemeId == themeId 
                && c.PracticeAreaId == practiceAreaId)
            .OrderBy(c => c.CriteriaCode)
            .ToListAsync();

        ViewBag.PracticeArea = practiceArea;
        return View("~/Views/Admin/FunctionalStandard/Criteria.cshtml", criteria);
    }

    // GET: FunctionalStandard/CreateCriterion/5/1/1.1
    public async Task<IActionResult> CreateCriterion(int? standardId, int? themeId, decimal? practiceAreaId)
    {
        if (standardId == null || themeId == null || practiceAreaId == null)
        {
            return NotFound();
        }

        var practiceArea = await _context.PracticeAreas
            .Include(pa => pa.Theme)
            .ThenInclude(t => t.FunctionalStandard)
            .FirstOrDefaultAsync(pa => pa.FunctionalStandardId == standardId 
                && pa.ThemeId == themeId 
                && pa.PracticeAreaId == practiceAreaId);

        if (practiceArea == null)
        {
            return NotFound();
        }

        ViewBag.PracticeArea = practiceArea;
        var criterion = new Criterion 
        { 
            FunctionalStandardId = standardId.Value,
            ThemeId = themeId.Value,
            PracticeAreaId = practiceAreaId.Value
        };
        return View("~/Views/Admin/FunctionalStandard/CreateCriterion.cshtml", criterion);
    }

    // POST: FunctionalStandard/CreateCriterion
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCriterion([Bind("FunctionalStandardId,ThemeId,PracticeAreaId,CriteriaCode,Criteria,Rating")] Criterion criterion)
    {
        if (ModelState.IsValid)
        {
            try
            {
                // Check if criterion with same composite key already exists
                if (_context.Criteria.Any(c => 
                    c.FunctionalStandardId == criterion.FunctionalStandardId && 
                    c.ThemeId == criterion.ThemeId &&
                    c.PracticeAreaId == criterion.PracticeAreaId &&
                    c.CriteriaCode == criterion.CriteriaCode))
                {
                    ModelState.AddModelError("CriteriaCode", $"A criterion with code '{criterion.CriteriaCode}' already exists for this practice area.");
                    var existingPracticeArea = await _context.PracticeAreas
                        .Include(pa => pa.Theme)
                        .ThenInclude(t => t.FunctionalStandard)
                        .FirstOrDefaultAsync(pa => pa.FunctionalStandardId == criterion.FunctionalStandardId 
                            && pa.ThemeId == criterion.ThemeId 
                            && pa.PracticeAreaId == criterion.PracticeAreaId);
                    ViewBag.PracticeArea = existingPracticeArea;
                    return View("~/Views/Admin/FunctionalStandard/CreateCriterion.cshtml", criterion);
                }
                
                criterion.CreatedAt = DateTime.UtcNow;
                criterion.UpdatedAt = DateTime.UtcNow;
                
                _context.Criteria.Add(criterion);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = $"Criterion '{criterion.CriteriaCode}' has been created successfully.";
                return RedirectToAction(nameof(Criteria), new 
                { 
                    standardId = criterion.FunctionalStandardId, 
                    themeId = criterion.ThemeId,
                    practiceAreaId = criterion.PracticeAreaId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating criterion");
                ModelState.AddModelError("", "An error occurred while creating the criterion. Please try again.");
            }
        }

        var practiceArea = await _context.PracticeAreas
            .Include(pa => pa.Theme)
            .ThenInclude(t => t.FunctionalStandard)
            .FirstOrDefaultAsync(pa => pa.FunctionalStandardId == criterion.FunctionalStandardId 
                && pa.ThemeId == criterion.ThemeId 
                && pa.PracticeAreaId == criterion.PracticeAreaId);
        ViewBag.PracticeArea = practiceArea;
        return View("~/Views/Admin/FunctionalStandard/CreateCriterion.cshtml", criterion);
    }

    // GET: FunctionalStandard/EditCriterion/5
    public async Task<IActionResult> EditCriterion(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var criterion = await _context.Criteria.FindAsync(id);
        if (criterion == null)
        {
            return NotFound();
        }

        var practiceArea = await _context.PracticeAreas
            .Include(pa => pa.Theme)
            .ThenInclude(t => t.FunctionalStandard)
            .FirstOrDefaultAsync(pa => pa.FunctionalStandardId == criterion.FunctionalStandardId 
                && pa.ThemeId == criterion.ThemeId 
                && pa.PracticeAreaId == criterion.PracticeAreaId);
        ViewBag.PracticeArea = practiceArea;
        return View("~/Views/Admin/FunctionalStandard/EditCriterion.cshtml", criterion);
    }

    // POST: FunctionalStandard/EditCriterion/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditCriterion(int id, Criterion criterion)
    {
        if (id != criterion.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                criterion.UpdatedAt = DateTime.UtcNow;
                
                _context.Update(criterion);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = $"Criterion '{criterion.CriteriaCode}' has been updated successfully.";
                return RedirectToAction(nameof(Criteria), new 
                { 
                    standardId = criterion.FunctionalStandardId, 
                    themeId = criterion.ThemeId,
                    practiceAreaId = criterion.PracticeAreaId
                });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CriterionExists(criterion.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating criterion");
                ModelState.AddModelError("", "An error occurred while updating the criterion. Please try again.");
            }
        }

        var practiceArea = await _context.PracticeAreas
            .Include(pa => pa.Theme)
            .ThenInclude(t => t.FunctionalStandard)
            .FirstOrDefaultAsync(pa => pa.FunctionalStandardId == criterion.FunctionalStandardId 
                && pa.ThemeId == criterion.ThemeId 
                && pa.PracticeAreaId == criterion.PracticeAreaId);
        ViewBag.PracticeArea = practiceArea;
        return View("~/Views/Admin/FunctionalStandard/EditCriterion.cshtml", criterion);
    }

    // POST: FunctionalStandard/DeleteCriterion/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCriterion(int id)
    {
        try
        {
            var criterion = await _context.Criteria.FindAsync(id);
            if (criterion != null)
            {
                var standardId = criterion.FunctionalStandardId;
                var themeId = criterion.ThemeId;
                var practiceAreaId = criterion.PracticeAreaId;
                
                _context.Criteria.Remove(criterion);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = $"Criterion '{criterion.CriteriaCode}' has been deleted successfully.";
                return RedirectToAction(nameof(Criteria), new { standardId, themeId, practiceAreaId });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting criterion");
            TempData["ErrorMessage"] = "An error occurred while deleting the criterion. Please try again.";
        }

        return RedirectToAction(nameof(Index));
    }

    #endregion

    #region Helper Methods

    private bool FunctionalStandardExists(int id)
    {
        return _context.FunctionalStandards.Any(e => e.Id == id);
    }

    private bool ThemeExists(int id)
    {
        return _context.FunctionalStandardThemes.Any(e => e.Id == id);
    }

    private bool PracticeAreaExists(int id)
    {
        return _context.PracticeAreas.Any(e => e.Id == id);
    }

    private bool CriterionExists(int id)
    {
        return _context.Criteria.Any(e => e.Id == id);
    }

    #endregion
}

