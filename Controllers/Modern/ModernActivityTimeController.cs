using System.Globalization;
using System.Security.Claims;
using Compass.Attributes;
using Compass.Data;
using Compass.Helpers;
using Compass.Models;
using Compass.ViewModels.Modern;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Compass.Controllers.Modern;

/// <summary>Activity time UI at <c>/modern/activity-time/*</c> — Central Ops Admin / Admin / Super admin only.</summary>
[Authorize]
[RequireOperationConsoleUser]
[Route("modern/activity-time")]
public class ModernActivityTimeController : Controller
{
    private readonly CompassDbContext _db;

    public ModernActivityTimeController(CompassDbContext db)
    {
        _db = db;
    }

    private string CurrentUserEmail =>
        User.Identity?.Name
        ?? User.FindFirst(ClaimTypes.Email)?.Value
        ?? User.FindFirst("preferred_username")?.Value
        ?? User.FindFirst("email")?.Value
        ?? "";

    private string? CurrentUserName =>
        User.FindFirst("name")?.Value
        ?? User.Identity?.Name;

    private void SetNav(string subNavItem = "activity-time-entries")
    {
        ViewBag.MainNavSection = "activity-time";
        ViewBag.SubNavItem = subNavItem;
    }

    [HttpGet("")]
    [HttpGet("entries")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        SetNav();
        var entries = await _db.ActivityTimeEntries.AsNoTracking()
            .Include(e => e.ActivityTimeTypeLookup)
            .Include(e => e.Project)
            .Include(e => e.BusinessAreaLookup)
            .Include(e => e.Themes).ThenInclude(t => t.ActivityTimeThemeLookup)
            .OrderByDescending(e => e.ActivityDate)
            .ThenByDescending(e => e.Id)
            .Take(500)
            .ToListAsync(cancellationToken);

        var vm = new ActivityTimeIndexViewModel
        {
            Entries = entries.Select(MapRow).ToList()
        };
        return View("~/Views/Modern/ActivityTime/Index.cshtml", vm);
    }

    [HttpGet("add")]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        SetNav("activity-time-add");
        var vm = new ActivityTimeFormViewModel { ActivityDate = TodayIsoDate() };
        await PopulateLookupsAsync(vm, cancellationToken);
        return View("~/Views/Modern/ActivityTime/Form.cshtml", vm);
    }

    [HttpPost("add")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ActivityTimeFormViewModel model, CancellationToken cancellationToken)
    {
        SetNav("activity-time-add");
        if (!TryResolveActivityDate(model, out var activityDate) || !ValidateForm(model))
        {
            await PopulateLookupsAsync(model, cancellationToken);
            return View("~/Views/Modern/ActivityTime/Form.cshtml", model);
        }

        var entry = new ActivityTimeEntry
        {
            ActivityDate = activityDate,
            DurationMinutes = model.Hours * 60 + model.Minutes,
            Narrative = string.IsNullOrWhiteSpace(model.Narrative) ? null : model.Narrative.Trim(),
            ActivityTimeTypeLookupId = model.ActivityTimeTypeLookupId,
            ProjectId = model.RelatesToWorkItem ? model.ProjectId : null,
            BusinessAreaLookupId = model.SupportsBusinessArea ? model.BusinessAreaLookupId : null,
            CreatedByEmail = CurrentUserEmail,
            CreatedByName = CurrentUserName,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        foreach (var themeId in model.SelectedThemeIds.Distinct())
        {
            entry.Themes.Add(new ActivityTimeEntryTheme { ActivityTimeThemeLookupId = themeId });
        }

        _db.ActivityTimeEntries.Add(entry);
        await _db.SaveChangesAsync(cancellationToken);
        TempData["Message"] = "Activity time entry saved.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("edit/{id:int}")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        SetNav();
        var entry = await _db.ActivityTimeEntries
            .Include(e => e.Themes)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (entry == null) return NotFound();

        var vm = new ActivityTimeFormViewModel
        {
            Id = entry.Id,
            ActivityDate = entry.ActivityDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            Hours = entry.DurationMinutes / 60,
            Minutes = entry.DurationMinutes % 60,
            Narrative = entry.Narrative,
            ActivityTimeTypeLookupId = entry.ActivityTimeTypeLookupId,
            SelectedThemeIds = entry.Themes.Select(t => t.ActivityTimeThemeLookupId).ToList(),
            RelatesToWorkItem = entry.ProjectId.HasValue,
            ProjectId = entry.ProjectId,
            SupportsBusinessArea = entry.BusinessAreaLookupId.HasValue,
            BusinessAreaLookupId = entry.BusinessAreaLookupId
        };
        await PopulateLookupsAsync(vm, cancellationToken);
        return View("~/Views/Modern/ActivityTime/Form.cshtml", vm);
    }

    [HttpPost("edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ActivityTimeFormViewModel model, CancellationToken cancellationToken)
    {
        SetNav();
        var entry = await _db.ActivityTimeEntries
            .Include(e => e.Themes)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (entry == null) return NotFound();

        model.Id = id;
        if (!TryResolveActivityDate(model, out var activityDate) || !ValidateForm(model))
        {
            await PopulateLookupsAsync(model, cancellationToken);
            return View("~/Views/Modern/ActivityTime/Form.cshtml", model);
        }

        entry.ActivityDate = activityDate;
        entry.DurationMinutes = model.Hours * 60 + model.Minutes;
        entry.Narrative = string.IsNullOrWhiteSpace(model.Narrative) ? null : model.Narrative.Trim();
        entry.ActivityTimeTypeLookupId = model.ActivityTimeTypeLookupId;
        entry.ProjectId = model.RelatesToWorkItem ? model.ProjectId : null;
        entry.BusinessAreaLookupId = model.SupportsBusinessArea ? model.BusinessAreaLookupId : null;
        entry.UpdatedAt = DateTime.UtcNow;

        _db.ActivityTimeEntryThemes.RemoveRange(entry.Themes);
        entry.Themes.Clear();
        foreach (var themeId in model.SelectedThemeIds.Distinct())
        {
            entry.Themes.Add(new ActivityTimeEntryTheme
            {
                ActivityTimeEntryId = entry.Id,
                ActivityTimeThemeLookupId = themeId
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
        TempData["Message"] = "Activity time entry updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var entry = await _db.ActivityTimeEntries.FindAsync(new object[] { id }, cancellationToken);
        if (entry == null) return NotFound();
        _db.ActivityTimeEntries.Remove(entry);
        await _db.SaveChangesAsync(cancellationToken);
        TempData["Message"] = "Activity time entry deleted.";
        return RedirectToAction(nameof(Index));
    }

    private static string TodayIsoDate() =>
        UkDateTime.Now().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    /// <summary>Parse <c>yyyy-MM-dd</c> from the date input without culture-sensitive DateTime binding.</summary>
    private bool TryResolveActivityDate(ActivityTimeFormViewModel model, out DateTime activityDate)
    {
        activityDate = default;
        var raw = (model.ActivityDate ?? "").Trim();
        if (string.IsNullOrEmpty(raw))
        {
            ModelState.AddModelError(nameof(model.ActivityDate), "Select a date.");
            return false;
        }

        if (!DateOnly.TryParseExact(raw, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateOnly))
        {
            ModelState.AddModelError(nameof(model.ActivityDate), "Enter a valid date.");
            return false;
        }

        activityDate = dateOnly.ToDateTime(TimeOnly.MinValue);
        model.ActivityDate = dateOnly.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        return true;
    }

    private bool ValidateForm(ActivityTimeFormViewModel model)
    {
        if (model.Hours < 0 || model.Minutes < 0 || model.Minutes > 59)
            ModelState.AddModelError(nameof(model.Minutes), "Enter a valid time in hours and minutes.");
        if (model.Hours * 60 + model.Minutes <= 0)
            ModelState.AddModelError(nameof(model.Hours), "Enter time spent (hours and/or minutes).");
        if (model.ActivityTimeTypeLookupId <= 0)
            ModelState.AddModelError(nameof(model.ActivityTimeTypeLookupId), "Select a work activity type.");
        if (model.SelectedThemeIds == null || model.SelectedThemeIds.Count == 0)
            ModelState.AddModelError(nameof(model.SelectedThemeIds), "Select at least one theme.");
        if (model.RelatesToWorkItem && (!model.ProjectId.HasValue || model.ProjectId <= 0))
            ModelState.AddModelError(nameof(model.ProjectId), "Select a work item.");
        if (model.SupportsBusinessArea && (!model.BusinessAreaLookupId.HasValue || model.BusinessAreaLookupId <= 0))
            ModelState.AddModelError(nameof(model.BusinessAreaLookupId), "Select a business area.");
        return ModelState.IsValid;
    }

    private async Task PopulateLookupsAsync(ActivityTimeFormViewModel vm, CancellationToken cancellationToken)
    {
        vm.ActivityTypes = await _db.ActivityTimeTypeLookups.AsNoTracking()
            .Where(x => x.IsActive || x.Id == vm.ActivityTimeTypeLookupId)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
            .Select(x => new SelectListItem { Value = x.Id.ToString(), Text = x.Name, Selected = x.Id == vm.ActivityTimeTypeLookupId })
            .ToListAsync(cancellationToken);

        var selected = vm.SelectedThemeIds?.ToHashSet() ?? new HashSet<int>();
        vm.Themes = await _db.ActivityTimeThemeLookups.AsNoTracking()
            .Where(x => x.IsActive || selected.Contains(x.Id))
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
            .Select(x => new SelectListItem { Value = x.Id.ToString(), Text = x.Name, Selected = selected.Contains(x.Id) })
            .ToListAsync(cancellationToken);

        vm.WorkItems = await _db.Projects.AsNoTracking()
            .Where(p => !p.IsDeleted && (p.Status == "Active" || p.Id == vm.ProjectId))
            .OrderBy(p => p.Title)
            .Select(p => new SelectListItem { Value = p.Id.ToString(), Text = p.Title, Selected = p.Id == vm.ProjectId })
            .ToListAsync(cancellationToken);

        vm.BusinessAreas = await _db.BusinessAreaLookups.AsNoTracking()
            .Where(x => x.IsActive || x.Id == vm.BusinessAreaLookupId)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
            .Select(x => new SelectListItem { Value = x.Id.ToString(), Text = x.Name, Selected = x.Id == vm.BusinessAreaLookupId })
            .ToListAsync(cancellationToken);
    }

    private static ActivityTimeEntryRowViewModel MapRow(ActivityTimeEntry e) => new()
    {
        Id = e.Id,
        ActivityDate = e.ActivityDate,
        DurationMinutes = e.DurationMinutes,
        ActivityTypeName = e.ActivityTimeTypeLookup?.Name ?? "",
        Narrative = e.Narrative,
        ThemesDisplay = string.Join(", ", e.Themes
            .Select(t => t.ActivityTimeThemeLookup?.Name)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .OrderBy(n => n)),
        WorkItemTitle = e.Project?.Title,
        BusinessAreaName = e.BusinessAreaLookup?.Name,
        CreatedByDisplay = !string.IsNullOrWhiteSpace(e.CreatedByName)
            ? e.CreatedByName!
            : e.CreatedByEmail
    };
}
