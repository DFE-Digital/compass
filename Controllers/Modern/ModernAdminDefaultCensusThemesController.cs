using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Compass.Attributes;
using Compass.Models.ServiceDataModels;
using Compass.Services.ServiceDataModels;
using Compass.ViewModels.Modern.ServiceDataModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Compass.Controllers.Modern;

/// <summary>
/// Admin UI for the shared default census theme catalogue at
/// <c>/modern/admin/default-census-themes</c>.
/// </summary>
[Authorize]
[RequireAdmin]
[Route("modern/admin/default-census-themes")]
public sealed class ModernAdminDefaultCensusThemesController : Controller
{
    private readonly ICoreCensusThemeService _themes;
    private readonly IServiceDataModelAccessService _access;

    public ModernAdminDefaultCensusThemesController(
        ICoreCensusThemeService themes,
        IServiceDataModelAccessService access)
    {
        _themes = themes;
        _access = access;
    }

    private string CurrentUserEmail =>
        User.Identity?.Name
        ?? User.FindFirst(ClaimTypes.Email)?.Value
        ?? User.FindFirst("preferred_username")?.Value
        ?? User.FindFirst("email")?.Value
        ?? "";

    private void SetAdminChrome()
    {
        ViewBag.MainNavSection = "admin";
        ViewBag.SubNavItem = "admin-default-census-themes";
    }

    private void ApplyTempDataFlash()
    {
        if (TempData["AdminMessage"] is string msg)
            ViewBag.AdminMessage = msg;
        if (TempData["AdminError"] is string err)
            ViewBag.AdminError = err;
    }

    [HttpGet("")]
    [HttpGet("index")]
    public async Task<IActionResult> Index()
    {
        SetAdminChrome();
        ApplyTempDataFlash();

        var themes = await _themes.ListManagedThemesAsync(CurrentUserEmail);
        var canManage = await _access.CanManageModelsAsync(CurrentUserEmail);
        return View("~/Views/Modern/Admin/DefaultCensusThemes/Index.cshtml",
            new DefaultCensusThemesOverviewViewModel
            {
                CanManage = canManage,
                Themes = themes.Select(MapThemeRow).ToList()
            });
    }

    [HttpGet("themes/create")]
    public async Task<IActionResult> CreateTheme()
    {
        SetAdminChrome();
        ApplyTempDataFlash();
        var overview = await BuildOverviewAsync();
        return View("~/Views/Modern/Admin/DefaultCensusThemes/ThemeEditor.cshtml",
            new DefaultCensusThemeEditorViewModel
            {
                Overview = overview,
                IsCreateMode = true,
                CreateInput = new ServiceDataModelGroupInput
                {
                    SortOrder = overview.Themes.Count == 0
                        ? 10
                        : overview.Themes.Max(t => t.SortOrder) + 10
                }
            });
    }

    [HttpPost("themes")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddTheme(ServiceDataModelGroupInput input)
    {
        if (!ModelState.IsValid)
        {
            TempData["AdminError"] = "Check the theme details and try again.";
            return RedirectToAction(nameof(CreateTheme));
        }

        var id = await _themes.CreateThemeAsync(input, CurrentUserEmail);
        if (!id.HasValue)
        {
            TempData["AdminError"] = "Could not create theme. Check permission and that the name is provided (service offering is reserved).";
            return RedirectToAction(nameof(CreateTheme));
        }

        TempData["AdminMessage"] = "Theme created. Every census inherits this shared base set.";
        return RedirectToAction(nameof(EditTheme), new { themeId = id.Value });
    }

    [HttpGet("themes/{themeId:guid}")]
    public async Task<IActionResult> EditTheme(Guid themeId)
    {
        SetAdminChrome();
        ApplyTempDataFlash();

        var theme = await _themes.GetManagedThemeAsync(themeId, CurrentUserEmail);
        if (theme == null)
            return NotFound();

        var overview = await BuildOverviewAsync();
        return View("~/Views/Modern/Admin/DefaultCensusThemes/ThemeEditor.cshtml",
            new DefaultCensusThemeEditorViewModel
            {
                Overview = overview,
                SelectedTheme = MapThemeDetail(theme),
                IsCreateMode = false
            });
    }

    [HttpPost("themes/{themeId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateTheme(Guid themeId, ServiceDataModelGroupInput input)
    {
        if (!ModelState.IsValid)
        {
            TempData["AdminError"] = "Check the theme details and try again.";
            return RedirectToAction(nameof(EditTheme), new { themeId });
        }

        var ok = await _themes.UpdateThemeAsync(themeId, input, CurrentUserEmail);
        TempData[ok ? "AdminMessage" : "AdminError"] = ok
            ? "Theme saved. All censuses use this shared definition."
            : "Could not save theme.";
        return RedirectToAction(nameof(EditTheme), new { themeId });
    }

    [HttpPost("themes/{themeId:guid}/disable")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DisableTheme(Guid themeId, bool disabled = true, string? returnTo = null)
    {
        var ok = await _themes.SetThemeActiveAsync(themeId, isActive: !disabled, CurrentUserEmail);
        TempData[ok ? "AdminMessage" : "AdminError"] = ok
            ? (disabled ? "Theme disabled for all censuses." : "Theme re-enabled for all censuses.")
            : "Could not update theme.";
        if (string.Equals(returnTo, "theme", StringComparison.OrdinalIgnoreCase))
            return RedirectToAction(nameof(EditTheme), new { themeId });
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("themes/reorder")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReorderThemes([FromForm] Guid[] orderedIds)
    {
        var ok = await _themes.ReorderThemesAsync(orderedIds ?? Array.Empty<Guid>(), CurrentUserEmail);
        TempData[ok ? "AdminMessage" : "AdminError"] = ok
            ? "Theme order saved."
            : "Could not reorder themes.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("themes/{themeId:guid}/fields")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddField(Guid themeId, ServiceDataModelFieldInput input)
    {
        if (!ModelState.IsValid)
        {
            TempData["AdminError"] = "Check the question details and try again.";
            return RedirectToAction(nameof(EditTheme), new { themeId });
        }

        var fieldId = await _themes.AddFieldAsync(themeId, input, CurrentUserEmail);
        TempData[fieldId.HasValue ? "AdminMessage" : "AdminError"] = fieldId.HasValue
            ? "Question added. Completion will recalculate for services already answering."
            : "Could not add question. Check permission and that the label is provided.";
        return RedirectToAction(nameof(EditTheme), new { themeId });
    }

    [HttpPost("themes/{themeId:guid}/fields/{fieldId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateField(Guid themeId, Guid fieldId, ServiceDataModelFieldInput input)
    {
        if (!ModelState.IsValid)
        {
            TempData["AdminError"] = "Check the question details and try again.";
            return RedirectToAction(nameof(EditTheme), new { themeId });
        }

        var ok = await _themes.UpdateFieldAsync(fieldId, input, CurrentUserEmail);
        TempData[ok ? "AdminMessage" : "AdminError"] = ok
            ? "Question saved. All censuses see this change."
            : "Could not save question. Check permission and that the label is provided.";
        return RedirectToAction(nameof(EditTheme), new { themeId });
    }

    [HttpPost("themes/{themeId:guid}/fields/{fieldId:guid}/disable")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DisableField(Guid themeId, Guid fieldId, bool disabled = true)
    {
        var ok = await _themes.SetFieldDisabledAsync(fieldId, disabled, CurrentUserEmail);
        TempData[ok ? "AdminMessage" : "AdminError"] = ok
            ? (disabled ? "Question disabled for all censuses." : "Question re-enabled for all censuses.")
            : "Could not update question.";
        return RedirectToAction(nameof(EditTheme), new { themeId });
    }

    [HttpPost("themes/{themeId:guid}/fields/reorder")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReorderFields(Guid themeId, [FromForm] Guid[] orderedIds)
    {
        var ok = await _themes.ReorderFieldsAsync(themeId, orderedIds ?? Array.Empty<Guid>(), CurrentUserEmail);
        TempData[ok ? "AdminMessage" : "AdminError"] = ok
            ? "Question order saved."
            : "Could not reorder questions.";
        return RedirectToAction(nameof(EditTheme), new { themeId });
    }

    [HttpPost("fields/{fieldId:guid}/options")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddFieldOption(Guid fieldId, ServiceDataModelFieldOptionInput input, Guid? themeId = null)
    {
        if (!ModelState.IsValid)
        {
            TempData["AdminError"] = "Check the option details and try again.";
            return themeId.HasValue
                ? RedirectToAction(nameof(EditTheme), new { themeId = themeId.Value })
                : RedirectToAction(nameof(Index));
        }

        var optionId = await _themes.AddFieldOptionAsync(fieldId, input, CurrentUserEmail);
        TempData[optionId.HasValue ? "AdminMessage" : "AdminError"] = optionId.HasValue
            ? "Option added."
            : "Could not add option.";
        return themeId.HasValue
            ? RedirectToAction(nameof(EditTheme), new { themeId = themeId.Value })
            : RedirectToAction(nameof(Index));
    }

    [HttpGet("schema/export")]
    public async Task<IActionResult> ExportSchema()
    {
        if (!await _access.CanManageModelsAsync(CurrentUserEmail))
            return Forbid();

        var package = await _themes.ExportSchemaAsync(CurrentUserEmail);
        if (package == null)
            return Forbid();

        var json = JsonSerializer.Serialize(package, SchemaJsonOptions);
        return File(Encoding.UTF8.GetBytes(json), "application/json", "census-theme-schema.json");
    }

    [HttpPost("schema/import")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportSchema(IFormFile? file, bool disableNotInFile = false, bool confirmImport = false)
    {
        if (!await _access.CanManageModelsAsync(CurrentUserEmail))
        {
            TempData["AdminError"] = "You do not have permission to import the census theme schema.";
            return RedirectToAction(nameof(Index));
        }

        if (!confirmImport)
        {
            TempData["AdminError"] = "Confirm the import before uploading a schema file.";
            return RedirectToAction(nameof(Index));
        }

        if (file == null || file.Length == 0)
        {
            TempData["AdminError"] = "Choose a census-theme-schema.json file to import.";
            return RedirectToAction(nameof(Index));
        }

        CensusThemeSchemaPackage? package;
        try
        {
            await using var stream = file.OpenReadStream();
            package = await JsonSerializer.DeserializeAsync<CensusThemeSchemaPackage>(stream, SchemaJsonOptions);
        }
        catch (JsonException ex)
        {
            TempData["AdminError"] = $"Invalid JSON: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception)
        {
            TempData["AdminError"] = "Could not read the uploaded file. Upload a valid census-theme-schema.json.";
            return RedirectToAction(nameof(Index));
        }

        if (package == null)
        {
            TempData["AdminError"] = "The uploaded file is empty or is not valid JSON.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _themes.ImportSchemaAsync(package, disableNotInFile, CurrentUserEmail);
        TempData[result.Ok ? "AdminMessage" : "AdminError"] = result.SummaryMessage;
        return RedirectToAction(nameof(Index));
    }

    private static readonly JsonSerializerOptions SchemaJsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: true) }
    };

    private async Task<DefaultCensusThemesOverviewViewModel> BuildOverviewAsync()
    {
        var themes = await _themes.ListManagedThemesAsync(CurrentUserEmail);
        return new DefaultCensusThemesOverviewViewModel
        {
            CanManage = await _access.CanManageModelsAsync(CurrentUserEmail),
            Themes = themes.Select(MapThemeRow).ToList()
        };
    }

    private static DefaultCensusThemeRowViewModel MapThemeRow(CoreCensusThemeListItem t) =>
        new()
        {
            Id = t.Id,
            StableKey = t.StableKey,
            Name = t.Name,
            Guidance = t.Guidance,
            SortOrder = t.SortOrder,
            FieldCount = t.FieldCount,
            IsActive = t.IsActive,
            IsDisabled = !t.IsActive
        };

    private static ServiceDataModelGroupViewModel MapThemeDetail(CoreCensusTheme theme) =>
        new()
        {
            Id = theme.Id,
            StableKey = theme.StableKey,
            Name = theme.Name,
            Guidance = theme.Guidance,
            SortOrder = theme.SortOrder,
            IsDisabled = !theme.IsActive,
            Fields = theme.Fields
                .OrderBy(f => f.SortOrder)
                .ThenBy(f => f.Label)
                .Select(f => new ServiceDataModelFieldViewModel
                {
                    Id = f.Id,
                    StableKey = f.StableKey,
                    Label = f.Label,
                    Guidance = f.Guidance,
                    FieldType = f.FieldType,
                    IsMandatory = f.IsMandatory,
                    CountsTowardsCompletion = f.CountsTowardsCompletion,
                    IsReportable = f.IsReportable,
                    SortOrder = f.SortOrder,
                    IsDisabled = f.IsDisabled,
                    VisibilityRuleJson = f.VisibilityRuleJson,
                    CanonicalAttributeKey = f.CanonicalAttributeKey,
                    ValidationPattern = f.ValidationPattern,
                    MinNumber = f.MinNumber,
                    MaxNumber = f.MaxNumber,
                    OptionsLookupKey = f.OptionsLookupKey,
                    AllowMultiple = f.AllowMultiple,
                    Options = f.Options
                        .OrderBy(o => o.SortOrder)
                        .Select(o => new ServiceDataModelFieldOptionViewModel
                        {
                            Id = o.Id,
                            ValueKey = o.ValueKey,
                            Label = o.Label,
                            SortOrder = o.SortOrder
                        }).ToList()
                }).ToList()
        };
}
