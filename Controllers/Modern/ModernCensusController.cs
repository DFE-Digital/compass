using System.Security.Claims;
using System.Text.Json;
using Compass.Data;
using Compass.Models;
using Compass.Models.Fips;
using Compass.Models.ServiceDataModels;
using Compass.Services;
using Compass.Services.ServiceDataModels;
using Compass.ViewModels.Modern.ServiceDataModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Compass.Controllers.Modern;

/// <summary>Census work list and assignment forms at <c>/modern/manage/census</c>.</summary>
[Authorize]
[Route("modern/manage/census")]
public sealed class ModernCensusController : Controller
{
    private readonly IServiceDataModelCensusService _census;
    private readonly CompassDbContext _db;
    private readonly IGlobalFeatureToggleService _globalFeatureToggle;

    public ModernCensusController(
        IServiceDataModelCensusService census,
        CompassDbContext db,
        IGlobalFeatureToggleService globalFeatureToggle)
    {
        _census = census;
        _db = db;
        _globalFeatureToggle = globalFeatureToggle;
    }

    private string CurrentUserEmail =>
        User.Identity?.Name
        ?? User.FindFirst(ClaimTypes.Email)?.Value
        ?? User.FindFirst("preferred_username")?.Value
        ?? User.FindFirst("email")?.Value
        ?? "";

    private void SetNav(string subNavItem)
    {
        ViewBag.MainNavSection = "manage";
        ViewBag.SubNavItem = subNavItem;
    }

    private async Task<IActionResult?> RequireFipsDatabaseAsync()
    {
        if (!await _globalFeatureToggle.IsFeatureEnabledForPrincipalAsync(FeatureCodes.Fips, User))
        {
            TempData["ErrorMessage"] =
                "The Compass service register is turned off. Census is unavailable while this feature is off.";
            return RedirectToAction("Dashboard", "ModernWork");
        }

        return null;
    }

    [HttpGet("")]
    [HttpGet("work")]
    public async Task<IActionResult> WorkList(
        string? tab,
        string? status,
        string? search,
        int? businessAreaId,
        int? channelId,
        int? userGroupId,
        int? phaseId,
        int? typeId,
        CancellationToken ct)
    {
        var disabled = await RequireFipsDatabaseAsync();
        if (disabled != null)
            return disabled;

        SetNav("manage-census");
        _ = ct;

        var tabKey = CensusWorkListBuilder.NormalizeTab(tab);
        var vm = await _census.GetWorkListAsync(
            CurrentUserEmail, tabKey, status, search, businessAreaId, phaseId, typeId, channelId, userGroupId);

        var formAction = Url.Action(nameof(WorkList), "ModernCensus", new { tab = tabKey }) ?? "/modern/manage/census/work";
        var clearUrl = Url.Action(nameof(WorkList), "ModernCensus", new { tab = tabKey }) ?? formAction;

        var searchAndFilter = new Compass.Models.SearchAndFilterViewModel
        {
            IdPrefix = "census",
            SearchPlaceholder = "Search services…",
            SearchValue = vm.Search,
            FormActionUrl = formAction,
            FormMethod = "get",
            ClearUrl = clearUrl,
            HiddenFields = new List<KeyValuePair<string, string>> { new("tab", tabKey) },
            Fields = new List<Compass.Models.SearchAndFilterFieldViewModel>
            {
                new()
                {
                    Label = "Business area",
                    Name = "businessAreaId",
                    SelectedValue = vm.BusinessAreaId?.ToString(),
                    Options = new List<Compass.Models.SearchAndFilterOption> { new() { Value = "", Text = "All business areas" } }
                        .Concat(vm.BusinessAreaOptions.Select(o => new Compass.Models.SearchAndFilterOption { Value = o.Value, Text = o.Text }))
                        .ToList()
                },
                new()
                {
                    Label = "Channel",
                    Name = "channelId",
                    SelectedValue = vm.ChannelId?.ToString(),
                    Options = new List<Compass.Models.SearchAndFilterOption> { new() { Value = "", Text = "All channels" } }
                        .Concat(vm.ChannelOptions.Select(o => new Compass.Models.SearchAndFilterOption { Value = o.Value, Text = o.Text }))
                        .ToList()
                },
                new()
                {
                    Label = "User group",
                    Name = "userGroupId",
                    SelectedValue = vm.UserGroupId?.ToString(),
                    Options = new List<Compass.Models.SearchAndFilterOption> { new() { Value = "", Text = "All user groups" } }
                        .Concat(vm.UserGroupOptions.Select(o => new Compass.Models.SearchAndFilterOption { Value = o.Value, Text = o.Text }))
                        .ToList()
                },
                new()
                {
                    Label = "Type",
                    Name = "typeId",
                    SelectedValue = vm.TypeId?.ToString(),
                    Options = new List<Compass.Models.SearchAndFilterOption> { new() { Value = "", Text = "All types" } }
                        .Concat(vm.TypeOptions.Select(o => new Compass.Models.SearchAndFilterOption { Value = o.Value, Text = o.Text }))
                        .ToList()
                },
                new()
                {
                    Label = "Phase",
                    Name = "phaseId",
                    SelectedValue = vm.PhaseId?.ToString(),
                    Options = new List<Compass.Models.SearchAndFilterOption> { new() { Value = "", Text = "All phases" } }
                        .Concat(vm.PhaseOptions.Select(o => new Compass.Models.SearchAndFilterOption { Value = o.Value, Text = o.Text }))
                        .ToList()
                },
                new()
                {
                    Label = "Census status",
                    Name = "status",
                    SelectedValue = vm.StatusFilter,
                    Options = new List<Compass.Models.SearchAndFilterOption>
                        {
                            new() { Value = "", Text = "All statuses" }
                        }
                        .Concat(Enum.GetValues<ServiceDataModelAssignmentStatus>()
                            .Select(s => new Compass.Models.SearchAndFilterOption
                            {
                                Value = s.ToString(),
                                Text = CensusWorkListBuilder.FormatStatusLabel(s)
                            }))
                        .ToList()
                }
            }
        };
        searchAndFilter.ActiveChips = Compass.Helpers.SearchAndFilterActiveChipsBuilder.FromViewModel(
            searchAndFilter, Url, nameof(WorkList), "ModernCensus", new { tab = tabKey });
        ViewBag.SearchAndFilter = searchAndFilter;

        return View("~/Views/Modern/Census/WorkList.cshtml", vm);
    }

    /// <summary>
    /// Opens the standing Service Census for a product, creating the internal record lazily when needed.
    /// </summary>
    [HttpGet("product/{productId:guid}")]
    public async Task<IActionResult> OpenForProduct(Guid productId, CancellationToken ct)
    {
        var disabled = await RequireFipsDatabaseAsync();
        if (disabled != null)
            return disabled;

        SetNav("manage-census");
        _ = ct;

        var standingId = await _census.EnsureStandingServiceCensusAsync(productId, CurrentUserEmail);
        if (standingId == null)
        {
            TempData["CensusError"] =
                "Service Census is not available for this product. An administrator may not have published a census yet, or you may not have access.";
            return RedirectToAction(nameof(WorkList), new { tab = CensusWorkListBuilder.TabYour });
        }

        return RedirectToAction(nameof(Assignment), new { assignmentId = standingId.Value });
    }

    [HttpGet("{assignmentId:guid}")]
    public async Task<IActionResult> Assignment(Guid assignmentId, CancellationToken ct)
    {
        var disabled = await RequireFipsDatabaseAsync();
        if (disabled != null)
            return disabled;

        SetNav("manage-census");
        _ = ct;

        var form = await _census.GetAssignmentFormAsync(assignmentId, CurrentUserEmail);
        if (form == null)
            return NotFound();

        ApplyCensusFlash();
        return View("~/Views/Modern/Census/AssignmentSummary.cshtml", form);
    }

    [HttpGet("{assignmentId:guid}/theme/{groupId:guid}")]
    public async Task<IActionResult> AssignmentTheme(Guid assignmentId, Guid groupId, CancellationToken ct)
    {
        var disabled = await RequireFipsDatabaseAsync();
        if (disabled != null)
            return disabled;

        SetNav("manage-census");
        _ = ct;

        var form = await _census.GetAssignmentFormAsync(assignmentId, CurrentUserEmail);
        if (form == null)
            return NotFound();

        var group = form.Groups.FirstOrDefault(g => g.Id == groupId);
        if (group == null)
            return RedirectToAction(nameof(Assignment), new { assignmentId });

        form.SelectedGroupId = groupId;
        ApplyCensusFlash();
        return View("~/Views/Modern/Census/AssignmentTheme.cshtml", form);
    }

    [HttpPost("{assignmentId:guid}/save")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(Guid assignmentId, DateTime? expectedUpdatedUtc, Guid? groupId)
    {
        var disabled = await RequireFipsDatabaseAsync();
        if (disabled != null)
            return disabled;

        SetNav("manage-census");

        var markComplete = Request.Form.ContainsKey("completeSection");
        var answers = CollectAnswersFromForm();
        await EnsureListFieldAnswersAsync(answers, groupId);
        var result = await _census.SaveAnswersAsync(
            assignmentId,
            CurrentUserEmail,
            answers,
            expectedUpdatedUtc,
            markComplete ? groupId : null);

        if (result.NotFound)
            return NotFound();
        if (result.Forbidden)
        {
            TempData["CensusError"] = result.ErrorMessage ?? "You do not have permission to edit this assignment.";
            return RedirectAfterThemeMutation(assignmentId, groupId);
        }
        if (result.Conflict || !result.Success)
        {
            ViewBag.CensusError = result.ErrorMessage ?? "Could not save answers.";
            var form = result.Form ?? await _census.GetAssignmentFormAsync(assignmentId, CurrentUserEmail);
            return ThemeOrSummaryView(form, groupId);
        }

        if (markComplete)
        {
            TempData["CensusMessage"] = "Section marked as complete.";
            if (!string.IsNullOrWhiteSpace(result.WarningMessage))
                TempData["CensusWarning"] = result.WarningMessage;
            return RedirectToAction(nameof(Assignment), new { assignmentId });
        }

        TempData["CensusMessage"] = "Answers saved.";
        if (!string.IsNullOrWhiteSpace(result.WarningMessage))
            TempData["CensusWarning"] = result.WarningMessage;
        return RedirectAfterThemeMutation(assignmentId, groupId);
    }

    [HttpPost("{assignmentId:guid}/submit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(Guid assignmentId, DateTime? expectedUpdatedUtc)
    {
        var disabled = await RequireFipsDatabaseAsync();
        if (disabled != null)
            return disabled;

        SetNav("manage-census");

        // Persist latest answers before submit (summary usually posts none).
        var answers = CollectAnswersFromForm();
        if (answers.Count > 0)
        {
            var save = await _census.SaveAnswersAsync(assignmentId, CurrentUserEmail, answers, expectedUpdatedUtc);
            if (save.NotFound)
                return NotFound();
            if (save.Forbidden || save.Conflict || !save.Success)
            {
                ViewBag.CensusError = save.ErrorMessage ?? "Could not save before submit.";
                var form = save.Form ?? await _census.GetAssignmentFormAsync(assignmentId, CurrentUserEmail);
                return View("~/Views/Modern/Census/AssignmentSummary.cshtml", form);
            }
        }

        var result = await _census.SubmitAsync(assignmentId, CurrentUserEmail);
        if (result.NotFound)
            return NotFound();
        if (!result.Success)
        {
            ViewBag.CensusError = result.ErrorMessage ?? "Could not submit.";
            var form = result.Form ?? await _census.GetAssignmentFormAsync(assignmentId, CurrentUserEmail);
            return View("~/Views/Modern/Census/AssignmentSummary.cshtml", form);
        }

        TempData["CensusMessage"] = "Assignment submitted.";
        return RedirectToAction(nameof(Assignment), new { assignmentId });
    }

    [HttpPost("{assignmentId:guid}/request-changes")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestChanges(Guid assignmentId, string note)
    {
        var disabled = await RequireFipsDatabaseAsync();
        if (disabled != null)
            return disabled;

        SetNav("manage-census");

        var result = await _census.RequestChangesAsync(assignmentId, CurrentUserEmail, note ?? "");
        if (result.NotFound)
            return NotFound();
        if (!result.Success)
        {
            ViewBag.CensusError = result.ErrorMessage ?? "Could not request changes.";
            var form = result.Form ?? await _census.GetAssignmentFormAsync(assignmentId, CurrentUserEmail);
            return View("~/Views/Modern/Census/AssignmentSummary.cshtml", form);
        }

        TempData["CensusMessage"] = "Changes requested.";
        return RedirectToAction(nameof(Assignment), new { assignmentId });
    }

    [HttpPost("{assignmentId:guid}/review")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Review(Guid assignmentId, string? attestationNote)
    {
        var disabled = await RequireFipsDatabaseAsync();
        if (disabled != null)
            return disabled;

        SetNav("manage-census");

        var result = await _census.ReviewAsync(assignmentId, CurrentUserEmail, attestationNote);
        if (result.NotFound)
            return NotFound();
        if (!result.Success)
        {
            ViewBag.CensusError = result.ErrorMessage ?? "Could not mark as reviewed.";
            var form = result.Form ?? await _census.GetAssignmentFormAsync(assignmentId, CurrentUserEmail);
            return View("~/Views/Modern/Census/AssignmentSummary.cshtml", form);
        }

        TempData["CensusMessage"] = "Assignment marked as reviewed.";
        return RedirectToAction(nameof(Assignment), new { assignmentId });
    }

    private void ApplyCensusFlash()
    {
        if (TempData["CensusMessage"] is string msg)
            ViewBag.CensusMessage = msg;
        if (TempData["CensusWarning"] is string warn)
            ViewBag.CensusWarning = warn;
        if (TempData["CensusError"] is string err)
            ViewBag.CensusError = err;
    }

    private IActionResult RedirectAfterThemeMutation(Guid assignmentId, Guid? groupId) =>
        groupId.HasValue
            ? RedirectToAction(nameof(AssignmentTheme), new { assignmentId, groupId })
            : RedirectToAction(nameof(Assignment), new { assignmentId });

    private IActionResult ThemeOrSummaryView(CensusAssignmentFormViewModel? form, Guid? groupId)
    {
        if (form == null)
            return NotFound();

        if (groupId.HasValue && form.Groups.Any(g => g.Id == groupId.Value))
        {
            form.SelectedGroupId = groupId;
            return View("~/Views/Modern/Census/AssignmentTheme.cshtml", form);
        }

        return View("~/Views/Modern/Census/AssignmentSummary.cshtml", form);
    }

    private Dictionary<Guid, string?> CollectAnswersFromForm()
    {
        var answers = new Dictionary<Guid, string?>();
        foreach (var key in Request.Form.Keys)
        {
            if (!key.StartsWith("answer_", StringComparison.OrdinalIgnoreCase))
                continue;

            var idPart = key["answer_".Length..];
            if (!Guid.TryParse(idPart, out var fieldId))
                continue;

            var values = Request.Form[key];
            if (values.Count == 0)
            {
                answers[fieldId] = null;
                continue;
            }

            if (values.Count == 1)
            {
                var raw = values[0];
                answers[fieldId] = string.IsNullOrWhiteSpace(raw) ? null : ToJsonValue(raw!);
            }
            else
            {
                // Multi-select / checkboxes / list pickers
                var list = values.Where(v => !string.IsNullOrWhiteSpace(v)).Select(v => v!).ToList();
                answers[fieldId] = list.Count == 0 ? null : JsonSerializer.Serialize(list);
            }
        }

        // List fields may post a single id (JSON string) — mark them via list_field_* so we can
        // normalise to arrays and clear emptied lists.
        foreach (var key in Request.Form.Keys)
        {
            if (!key.StartsWith("list_field_", StringComparison.OrdinalIgnoreCase))
                continue;
            var idPart = key["list_field_".Length..];
            if (!Guid.TryParse(idPart, out var fieldId))
                continue;

            if (!answers.ContainsKey(fieldId))
                answers[fieldId] = "[]";
            else if (answers[fieldId] != null && !answers[fieldId]!.TrimStart().StartsWith('['))
            {
                // Single posted id → wrap as array of one.
                try
                {
                    using var doc = JsonDocument.Parse(answers[fieldId]!);
                    if (doc.RootElement.ValueKind == JsonValueKind.String)
                    {
                        var one = doc.RootElement.GetString();
                        answers[fieldId] = one == null ? "[]" : JsonSerializer.Serialize(new[] { one });
                    }
                }
                catch (JsonException)
                {
                    answers[fieldId] = JsonSerializer.Serialize(new[] { answers[fieldId]!.Trim().Trim('"') });
                }
            }
        }

        return answers;
    }

    private async Task EnsureListFieldAnswersAsync(Dictionary<Guid, string?> answers, Guid? groupId)
    {
        if (!groupId.HasValue)
            return;

        // Theme save always includes list_field_* markers from the form; nothing else needed.
        await Task.CompletedTask;
    }

    [HttpGet("pick-services")]
    public async Task<IActionResult> PickServices([FromQuery] string? q, CancellationToken ct)
    {
        var disabled = await RequireFipsDatabaseAsync();
        if (disabled != null)
            return new JsonResult(new { error = "FIPS is not available." }) { StatusCode = 403 };

        var term = (q ?? "").Trim();
        if (term.Length < 2)
            return Json(new { results = Array.Empty<object>() });

        var email = CurrentUserEmail;
        var isOps = !string.IsNullOrWhiteSpace(email) &&
                    await HttpContext.RequestServices.GetRequiredService<IPermissionService>()
                        .IsOperationConsoleUserAsync(email.Trim());

        IQueryable<CMDBProduct> query = _db.CMDBProducts.AsNoTracking()
            .Where(p => p.Status != CMDBProductStatus.Rejected);

        if (!isOps)
        {
            var emailLower = email.Trim().ToLowerInvariant();
            var accessibleIds = await _db.CMDBProductContacts.AsNoTracking()
                .Where(c => c.UserEmail != null && c.UserEmail.Trim().ToLower() == emailLower)
                .Select(c => c.CMDBProductId)
                .Distinct()
                .ToListAsync(ct);

            // Census editors who can open Manage already see the full register list on "All".
            // Prefer contact-scoped results when the user is not an operations console user and
            // has contacts; otherwise search the visible non-rejected register.
            if (accessibleIds.Count > 0)
                query = query.Where(p => accessibleIds.Contains(p.Id));
        }

        var results = await query
            .Where(p =>
                (p.Title != null && p.Title.Contains(term)) ||
                (p.CMDBID != null && p.CMDBID.Contains(term)))
            .OrderBy(p => p.Title)
            .Take(20)
            .Select(p => new { id = p.Id.ToString(), title = p.Title, subtitle = p.CMDBID })
            .ToListAsync(ct);

        return Json(new { results });
    }

    [HttpGet("pick-service-lines")]
    public async Task<IActionResult> PickServiceLines([FromQuery] string? q, CancellationToken ct)
    {
        var disabled = await RequireFipsDatabaseAsync();
        if (disabled != null)
            return new JsonResult(new { error = "FIPS is not available." }) { StatusCode = 403 };

        var term = (q ?? "").Trim();
        if (term.Length < 2)
            return Json(new { results = Array.Empty<object>() });

        var results = await _db.ServiceLines.AsNoTracking()
            .Where(s => s.Name.Contains(term) || s.Slug.Contains(term))
            .OrderBy(s => s.Name)
            .Take(20)
            .Select(s => new { id = s.Id.ToString(), title = s.Name, subtitle = s.Slug })
            .ToListAsync(ct);

        return Json(new { results });
    }

    private static string ToJsonValue(string raw)
    {
        raw = raw.Trim();
        if (bool.TryParse(raw, out var b))
            return b ? "true" : "false";
        if (string.Equals(raw, "Yes", StringComparison.OrdinalIgnoreCase))
            return "true";
        if (string.Equals(raw, "No", StringComparison.OrdinalIgnoreCase))
            return "false";
        if (decimal.TryParse(raw, out var n) && raw.All(c => char.IsDigit(c) || c is '.' or '-' or ','))
        {
            // Prefer string JSON for text-like numbers that aren't clearly numeric field posts —
            // still valid for Number validation when string-parsed.
            if (!raw.Contains(' ') && decimal.TryParse(raw, System.Globalization.NumberStyles.Number,
                    System.Globalization.CultureInfo.InvariantCulture, out var inv))
                return JsonSerializer.Serialize(inv);
        }

        return JsonSerializer.Serialize(raw);
    }
}
