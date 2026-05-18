using Compass.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Compass.Controllers.Modern;

public partial class ModernAdminController
{
    // ─── Functional Standards ────────────────────────────────────────────

    [HttpGet("functional-standards/{id:int}")]
    public async Task<IActionResult> FunctionalStandardDetail(int id)
    {
        SetAdminChrome("admin-index");
        var standard = await _context.FunctionalStandards.AsNoTracking()
            .Include(s => s.Themes.OrderBy(t => t.ThemeId))
                .ThenInclude(t => t.PracticeAreas)
            .FirstOrDefaultAsync(s => s.Id == id);
        if (standard == null) return NotFound();

        if (TempData["AdminMessage"] is string msg) ViewBag.AdminMessage = msg;
        if (TempData["AdminError"] is string err) ViewBag.AdminError = err;
        return View("~/Views/Modern/Admin/FunctionalStandard/Detail.cshtml", standard);
    }

    [HttpGet("functional-standards/create")]
    public IActionResult FunctionalStandardCreate()
    {
        SetAdminChrome("admin-index");
        if (TempData["AdminError"] is string err) ViewBag.AdminError = err;
        return View("~/Views/Modern/Admin/FunctionalStandard/Create.cshtml");
    }

    [HttpPost("functional-standards/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FunctionalStandardCreate(int id, string title, string? description, DateTime? publishedDate)
    {
        title = (title ?? "").Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            TempData["AdminError"] = "Title is required.";
            return RedirectToAction(nameof(FunctionalStandardCreate));
        }

        if (await _context.FunctionalStandards.AnyAsync(f => f.Id == id))
        {
            TempData["AdminError"] = $"A functional standard with ID {id} already exists.";
            return RedirectToAction(nameof(FunctionalStandardCreate));
        }

        _context.FunctionalStandards.Add(new FunctionalStandard
        {
            Id = id,
            Title = title,
            Description = (description ?? "").Trim(),
            PublishedDate = publishedDate,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        TempData["AdminMessage"] = $"Functional standard \"{title}\" created.";
        return RedirectToAction(nameof(FunctionalStandardDetail), new { id });
    }

    [HttpGet("functional-standards/{id:int}/edit")]
    public async Task<IActionResult> FunctionalStandardEdit(int id)
    {
        SetAdminChrome("admin-index");
        var standard = await _context.FunctionalStandards.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
        if (standard == null) return NotFound();

        if (TempData["AdminMessage"] is string msg) ViewBag.AdminMessage = msg;
        if (TempData["AdminError"] is string err) ViewBag.AdminError = err;
        return View("~/Views/Modern/Admin/FunctionalStandard/Edit.cshtml", standard);
    }

    [HttpPost("functional-standards/{id:int}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FunctionalStandardEdit(int id, string title, string? description, DateTime? publishedDate)
    {
        var entity = await _context.FunctionalStandards.FindAsync(id);
        if (entity == null) return NotFound();

        title = (title ?? "").Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            TempData["AdminError"] = "Title is required.";
            return RedirectToAction(nameof(FunctionalStandardEdit), new { id });
        }

        entity.Title = title;
        entity.Description = (description ?? "").Trim();
        entity.PublishedDate = publishedDate;
        entity.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        TempData["AdminMessage"] = $"Functional standard \"{title}\" updated.";
        return RedirectToAction(nameof(FunctionalStandardDetail), new { id });
    }

    [HttpPost("functional-standards/{id:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FunctionalStandardDelete(int id)
    {
        var entity = await _context.FunctionalStandards
            .Include(f => f.Themes)
            .FirstOrDefaultAsync(f => f.Id == id);
        if (entity == null) return NotFound();

        // Block delete if submitted assessments exist
        var hasSubmitted = await _context.FunctionalStandardAssessments
            .AnyAsync(a => a.FunctionalStandardId == id && a.SubmittedAt != null);
        if (hasSubmitted)
        {
            TempData["AdminError"] = "Cannot delete this standard because it has submitted assessments.";
            return RedirectToAction(nameof(FunctionalStandardDetail), new { id });
        }

        _context.FunctionalStandards.Remove(entity);
        await _context.SaveChangesAsync();

        TempData["AdminMessage"] = $"Functional standard \"{entity.Title}\" deleted.";
        return RedirectToAction(nameof(Index), new { panel = "std-functional" });
    }

    // ─── Themes ──────────────────────────────────────────────────────────

    [HttpGet("functional-standards/{standardId:int}/themes/create")]
    public async Task<IActionResult> FsThemeCreate(int standardId)
    {
        SetAdminChrome("admin-index");
        var standard = await _context.FunctionalStandards.AsNoTracking().FirstOrDefaultAsync(s => s.Id == standardId);
        if (standard == null) return NotFound();

        ViewBag.Standard = standard;
        if (TempData["AdminError"] is string err) ViewBag.AdminError = err;
        return View("~/Views/Modern/Admin/FunctionalStandard/ThemeCreate.cshtml");
    }

    [HttpPost("functional-standards/{standardId:int}/themes/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FsThemeCreate(int standardId, int themeId, string title, string? description)
    {
        title = (title ?? "").Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            TempData["AdminError"] = "Title is required.";
            return RedirectToAction(nameof(FsThemeCreate), new { standardId });
        }

        if (await _context.FunctionalStandardThemes.AnyAsync(t => t.FunctionalStandardId == standardId && t.ThemeId == themeId))
        {
            TempData["AdminError"] = $"Theme ID {themeId} already exists for this standard.";
            return RedirectToAction(nameof(FsThemeCreate), new { standardId });
        }

        _context.FunctionalStandardThemes.Add(new FunctionalStandardTheme
        {
            FunctionalStandardId = standardId,
            ThemeId = themeId,
            Title = title,
            Description = (description ?? "").Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        TempData["AdminMessage"] = $"Theme \"{title}\" created.";
        return RedirectToAction(nameof(FunctionalStandardDetail), new { id = standardId });
    }

    [HttpGet("functional-standards/{standardId:int}/themes/{themeId:int}")]
    public async Task<IActionResult> FsThemeDetail(int standardId, int themeId)
    {
        SetAdminChrome("admin-index");
        var theme = await _context.FunctionalStandardThemes.AsNoTracking()
            .Include(t => t.FunctionalStandard)
            .Include(t => t.PracticeAreas.OrderBy(p => p.PracticeAreaId))
                .ThenInclude(p => p.Criteria)
            .FirstOrDefaultAsync(t => t.FunctionalStandardId == standardId && t.ThemeId == themeId);
        if (theme == null) return NotFound();

        if (TempData["AdminMessage"] is string msg) ViewBag.AdminMessage = msg;
        if (TempData["AdminError"] is string err) ViewBag.AdminError = err;
        return View("~/Views/Modern/Admin/FunctionalStandard/ThemeDetail.cshtml", theme);
    }

    [HttpGet("functional-standards/{standardId:int}/themes/{themeId:int}/edit")]
    public async Task<IActionResult> FsThemeEdit(int standardId, int themeId)
    {
        SetAdminChrome("admin-index");
        var theme = await _context.FunctionalStandardThemes.AsNoTracking()
            .Include(t => t.FunctionalStandard)
            .FirstOrDefaultAsync(t => t.FunctionalStandardId == standardId && t.ThemeId == themeId);
        if (theme == null) return NotFound();

        ViewBag.Standard = theme.FunctionalStandard;
        if (TempData["AdminError"] is string err) ViewBag.AdminError = err;
        return View("~/Views/Modern/Admin/FunctionalStandard/ThemeEdit.cshtml", theme);
    }

    [HttpPost("functional-standards/{standardId:int}/themes/{themeId:int}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FsThemeEdit(int standardId, int themeId, string title, string? description)
    {
        var entity = await _context.FunctionalStandardThemes
            .FirstOrDefaultAsync(t => t.FunctionalStandardId == standardId && t.ThemeId == themeId);
        if (entity == null) return NotFound();

        title = (title ?? "").Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            TempData["AdminError"] = "Title is required.";
            return RedirectToAction(nameof(FsThemeEdit), new { standardId, themeId });
        }

        entity.Title = title;
        entity.Description = (description ?? "").Trim();
        entity.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        TempData["AdminMessage"] = $"Theme \"{title}\" updated.";
        return RedirectToAction(nameof(FsThemeDetail), new { standardId, themeId });
    }

    [HttpPost("functional-standards/{standardId:int}/themes/{themeId:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FsThemeDelete(int standardId, int themeId)
    {
        var entity = await _context.FunctionalStandardThemes
            .FirstOrDefaultAsync(t => t.FunctionalStandardId == standardId && t.ThemeId == themeId);
        if (entity == null) return NotFound();

        _context.FunctionalStandardThemes.Remove(entity);
        await _context.SaveChangesAsync();

        TempData["AdminMessage"] = $"Theme \"{entity.Title}\" deleted.";
        return RedirectToAction(nameof(FunctionalStandardDetail), new { id = standardId });
    }

    // ─── Practice Areas ──────────────────────────────────────────────────

    [HttpGet("functional-standards/{standardId:int}/themes/{themeId:int}/practice-areas/create")]
    public async Task<IActionResult> FsPracticeAreaCreate(int standardId, int themeId)
    {
        SetAdminChrome("admin-index");
        var theme = await _context.FunctionalStandardThemes.AsNoTracking()
            .Include(t => t.FunctionalStandard)
            .FirstOrDefaultAsync(t => t.FunctionalStandardId == standardId && t.ThemeId == themeId);
        if (theme == null) return NotFound();

        ViewBag.Theme = theme;
        ViewBag.Standard = theme.FunctionalStandard;
        if (TempData["AdminError"] is string err) ViewBag.AdminError = err;
        return View("~/Views/Modern/Admin/FunctionalStandard/PracticeAreaCreate.cshtml");
    }

    [HttpPost("functional-standards/{standardId:int}/themes/{themeId:int}/practice-areas/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FsPracticeAreaCreate(int standardId, int themeId, decimal practiceAreaId, string title, string? description)
    {
        title = (title ?? "").Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            TempData["AdminError"] = "Title is required.";
            return RedirectToAction(nameof(FsPracticeAreaCreate), new { standardId, themeId });
        }

        if (await _context.PracticeAreas.AnyAsync(p => p.FunctionalStandardId == standardId && p.ThemeId == themeId && p.PracticeAreaId == practiceAreaId))
        {
            TempData["AdminError"] = $"Practice area {practiceAreaId} already exists for this theme.";
            return RedirectToAction(nameof(FsPracticeAreaCreate), new { standardId, themeId });
        }

        _context.PracticeAreas.Add(new PracticeArea
        {
            FunctionalStandardId = standardId,
            ThemeId = themeId,
            PracticeAreaId = practiceAreaId,
            Title = title,
            Description = (description ?? "").Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        TempData["AdminMessage"] = $"Practice area \"{title}\" created.";
        return RedirectToAction(nameof(FsThemeDetail), new { standardId, themeId });
    }

    [HttpGet("functional-standards/{standardId:int}/themes/{themeId:int}/practice-areas/{paId:int}")]
    public async Task<IActionResult> FsPracticeAreaDetail(int standardId, int themeId, int paId)
    {
        SetAdminChrome("admin-index");
        var pa = await _context.PracticeAreas.AsNoTracking()
            .Include(p => p.Theme).ThenInclude(t => t!.FunctionalStandard)
            .Include(p => p.Criteria.OrderBy(c => c.CriteriaCode))
            .FirstOrDefaultAsync(p => p.Id == paId && p.FunctionalStandardId == standardId && p.ThemeId == themeId);
        if (pa == null) return NotFound();

        if (TempData["AdminMessage"] is string msg) ViewBag.AdminMessage = msg;
        if (TempData["AdminError"] is string err) ViewBag.AdminError = err;
        return View("~/Views/Modern/Admin/FunctionalStandard/PracticeAreaDetail.cshtml", pa);
    }

    [HttpGet("functional-standards/{standardId:int}/themes/{themeId:int}/practice-areas/{paId:int}/edit")]
    public async Task<IActionResult> FsPracticeAreaEdit(int standardId, int themeId, int paId)
    {
        SetAdminChrome("admin-index");
        var pa = await _context.PracticeAreas.AsNoTracking()
            .Include(p => p.Theme).ThenInclude(t => t!.FunctionalStandard)
            .FirstOrDefaultAsync(p => p.Id == paId && p.FunctionalStandardId == standardId && p.ThemeId == themeId);
        if (pa == null) return NotFound();

        ViewBag.Standard = pa.Theme!.FunctionalStandard;
        ViewBag.Theme = pa.Theme;
        if (TempData["AdminError"] is string err) ViewBag.AdminError = err;
        return View("~/Views/Modern/Admin/FunctionalStandard/PracticeAreaEdit.cshtml", pa);
    }

    [HttpPost("functional-standards/{standardId:int}/themes/{themeId:int}/practice-areas/{paId:int}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FsPracticeAreaEdit(int standardId, int themeId, int paId, string title, string? description)
    {
        var entity = await _context.PracticeAreas.FindAsync(paId);
        if (entity == null || entity.FunctionalStandardId != standardId || entity.ThemeId != themeId) return NotFound();

        title = (title ?? "").Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            TempData["AdminError"] = "Title is required.";
            return RedirectToAction(nameof(FsPracticeAreaEdit), new { standardId, themeId, paId });
        }

        entity.Title = title;
        entity.Description = (description ?? "").Trim();
        entity.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        TempData["AdminMessage"] = $"Practice area \"{title}\" updated.";
        return RedirectToAction(nameof(FsPracticeAreaDetail), new { standardId, themeId, paId });
    }

    [HttpPost("functional-standards/{standardId:int}/themes/{themeId:int}/practice-areas/{paId:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FsPracticeAreaDelete(int standardId, int themeId, int paId)
    {
        var entity = await _context.PracticeAreas.FindAsync(paId);
        if (entity == null || entity.FunctionalStandardId != standardId || entity.ThemeId != themeId) return NotFound();

        _context.PracticeAreas.Remove(entity);
        await _context.SaveChangesAsync();

        TempData["AdminMessage"] = $"Practice area \"{entity.Title}\" deleted.";
        return RedirectToAction(nameof(FsThemeDetail), new { standardId, themeId });
    }

    // ─── Criteria ────────────────────────────────────────────────────────

    [HttpGet("functional-standards/{standardId:int}/themes/{themeId:int}/practice-areas/{paId:int}/criteria/create")]
    public async Task<IActionResult> FsCriterionCreate(int standardId, int themeId, int paId)
    {
        SetAdminChrome("admin-index");
        var pa = await _context.PracticeAreas.AsNoTracking()
            .Include(p => p.Theme).ThenInclude(t => t!.FunctionalStandard)
            .FirstOrDefaultAsync(p => p.Id == paId && p.FunctionalStandardId == standardId && p.ThemeId == themeId);
        if (pa == null) return NotFound();

        ViewBag.PracticeArea = pa;
        ViewBag.Theme = pa.Theme;
        ViewBag.Standard = pa.Theme!.FunctionalStandard;
        if (TempData["AdminError"] is string err) ViewBag.AdminError = err;
        return View("~/Views/Modern/Admin/FunctionalStandard/CriterionCreate.cshtml");
    }

    [HttpPost("functional-standards/{standardId:int}/themes/{themeId:int}/practice-areas/{paId:int}/criteria/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FsCriterionCreate(int standardId, int themeId, int paId,
        string criteriaCode, string criteria, CriteriaRating rating)
    {
        var pa = await _context.PracticeAreas.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == paId && p.FunctionalStandardId == standardId && p.ThemeId == themeId);
        if (pa == null) return NotFound();

        criteriaCode = (criteriaCode ?? "").Trim();
        criteria = (criteria ?? "").Trim();

        if (string.IsNullOrWhiteSpace(criteriaCode) || string.IsNullOrWhiteSpace(criteria))
        {
            TempData["AdminError"] = "Criteria code and criteria text are required.";
            return RedirectToAction(nameof(FsCriterionCreate), new { standardId, themeId, paId });
        }

        if (await _context.Criteria.AnyAsync(c =>
            c.FunctionalStandardId == standardId && c.ThemeId == themeId
            && c.PracticeAreaId == pa.PracticeAreaId && c.CriteriaCode == criteriaCode))
        {
            TempData["AdminError"] = $"Criteria code \"{criteriaCode}\" already exists for this practice area.";
            return RedirectToAction(nameof(FsCriterionCreate), new { standardId, themeId, paId });
        }

        _context.Criteria.Add(new Criterion
        {
            FunctionalStandardId = standardId,
            ThemeId = themeId,
            PracticeAreaId = pa.PracticeAreaId,
            CriteriaCode = criteriaCode,
            Criteria = criteria,
            Rating = rating,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        TempData["AdminMessage"] = $"Criterion \"{criteriaCode}\" created.";
        return RedirectToAction(nameof(FsPracticeAreaDetail), new { standardId, themeId, paId });
    }

    [HttpGet("functional-standards/criteria/{criterionId:int}/edit")]
    public async Task<IActionResult> FsCriterionEdit(int criterionId)
    {
        SetAdminChrome("admin-index");
        var entity = await _context.Criteria.AsNoTracking()
            .Include(c => c.PracticeArea).ThenInclude(p => p!.Theme).ThenInclude(t => t!.FunctionalStandard)
            .FirstOrDefaultAsync(c => c.Id == criterionId);
        if (entity == null) return NotFound();

        ViewBag.PracticeArea = entity.PracticeArea;
        ViewBag.Theme = entity.PracticeArea!.Theme;
        ViewBag.Standard = entity.PracticeArea.Theme!.FunctionalStandard;
        if (TempData["AdminError"] is string err) ViewBag.AdminError = err;
        return View("~/Views/Modern/Admin/FunctionalStandard/CriterionEdit.cshtml", entity);
    }

    [HttpPost("functional-standards/criteria/{criterionId:int}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FsCriterionEdit(int criterionId, string criteria, CriteriaRating rating)
    {
        var entity = await _context.Criteria.FindAsync(criterionId);
        if (entity == null) return NotFound();

        criteria = (criteria ?? "").Trim();
        if (string.IsNullOrWhiteSpace(criteria))
        {
            TempData["AdminError"] = "Criteria text is required.";
            return RedirectToAction(nameof(FsCriterionEdit), new { criterionId });
        }

        entity.Criteria = criteria;
        entity.Rating = rating;
        entity.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Find the practice area to redirect back
        var pa = await _context.PracticeAreas.AsNoTracking().FirstOrDefaultAsync(p =>
            p.FunctionalStandardId == entity.FunctionalStandardId
            && p.ThemeId == entity.ThemeId
            && p.PracticeAreaId == entity.PracticeAreaId);

        TempData["AdminMessage"] = $"Criterion \"{entity.CriteriaCode}\" updated.";
        return RedirectToAction(nameof(FsPracticeAreaDetail), new
        {
            standardId = entity.FunctionalStandardId,
            themeId = entity.ThemeId,
            paId = pa?.Id ?? 0
        });
    }

    [HttpPost("functional-standards/criteria/{criterionId:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FsCriterionDelete(int criterionId)
    {
        var entity = await _context.Criteria.FindAsync(criterionId);
        if (entity == null) return NotFound();

        var stdId = entity.FunctionalStandardId;
        var themeId = entity.ThemeId;
        var paDecId = entity.PracticeAreaId;

        _context.Criteria.Remove(entity);
        await _context.SaveChangesAsync();

        var pa = await _context.PracticeAreas.AsNoTracking().FirstOrDefaultAsync(p =>
            p.FunctionalStandardId == stdId && p.ThemeId == themeId && p.PracticeAreaId == paDecId);

        TempData["AdminMessage"] = $"Criterion \"{entity.CriteriaCode}\" deleted.";
        return RedirectToAction(nameof(FsPracticeAreaDetail), new { standardId = stdId, themeId, paId = pa?.Id ?? 0 });
    }
}
