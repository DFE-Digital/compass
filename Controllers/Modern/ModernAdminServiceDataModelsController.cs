using System.Security.Claims;
using Compass.Attributes;
using Compass.Models.ServiceDataModels;
using Compass.Services.ServiceDataModels;
using Compass.ViewModels.Modern.ServiceDataModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Compass.Controllers.Modern;

/// <summary>Admin UI for reusable service data models at <c>/modern/admin/service-data-models</c>.</summary>
[Authorize]
[RequireAdmin]
[Route("modern/admin/service-data-models")]
public sealed class ModernAdminServiceDataModelsController : Controller
{
    private readonly IServiceDataModelAdminService _admin;
    private readonly ICoreCensusThemeService _coreThemes;

    public ModernAdminServiceDataModelsController(
        IServiceDataModelAdminService admin,
        ICoreCensusThemeService coreThemes)
    {
        _admin = admin;
        _coreThemes = coreThemes;
    }

    private string CurrentUserEmail =>
        User.Identity?.Name
        ?? User.FindFirst(ClaimTypes.Email)?.Value
        ?? User.FindFirst("preferred_username")?.Value
        ?? User.FindFirst("email")?.Value
        ?? "";

    private void SetAdminChrome(string subNavItem)
    {
        ViewBag.MainNavSection = "admin";
        ViewBag.SubNavItem = subNavItem;
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
        SetAdminChrome("admin-service-data-models");
        ApplyTempDataFlash();

        var email = CurrentUserEmail;
        var vm = await _admin.ListModelsAsync(email);
        return View("~/Views/Modern/Admin/ServiceDataModels/Index.cshtml", vm);
    }

    [HttpGet("create")]
    public IActionResult Create()
    {
        SetAdminChrome("admin-service-data-models");
        ApplyTempDataFlash();
        return View("~/Views/Modern/Admin/ServiceDataModels/Create.cshtml", new ServiceDataModelCreateInput());
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ServiceDataModelCreateInput input)
    {
        SetAdminChrome("admin-service-data-models");

        if (!ModelState.IsValid)
            return View("~/Views/Modern/Admin/ServiceDataModels/Create.cshtml", input);

        try
        {
            var id = await _admin.CreateModelAsync(
                input.StableKey,
                input.Name,
                input.Description,
                input.OwnerDisplayName,
                input.OwnerEmail,
                input.Classification,
                input.IsRepeatable,
                CurrentUserEmail);

            TempData["AdminMessage"] = "Service data model created.";
            return RedirectToAction(nameof(Detail), new { id });
        }
        catch (Exception ex)
        {
            TempData["AdminError"] = ex.Message;
            ViewBag.AdminError = ex.Message;
            return View("~/Views/Modern/Admin/ServiceDataModels/Create.cshtml", input);
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Detail(Guid id)
    {
        SetAdminChrome("admin-service-data-models");
        ApplyTempDataFlash();

        var vm = await _admin.GetModelDetailAsync(id, CurrentUserEmail);
        if (vm == null)
            return NotFound();

        if (vm.CanManage)
        {
            var shared = await _coreThemes.GetSharedStructureAsync();
            vm.SharedCensusThemes = shared.Select(t => new CoreCensusThemeListItemViewModel
            {
                Id = t.Id,
                StableKey = t.StableKey,
                Name = t.Name,
                Guidance = t.Guidance,
                FieldCount = t.Fields.Count(f => !f.IsDisabled),
                IsActive = t.IsActive
            }).ToList();
            vm.GroupCount = vm.SharedCensusThemes.Count;
            vm.FieldCount = vm.SharedCensusThemes.Sum(t => t.FieldCount);
        }

        return View("~/Views/Modern/Admin/ServiceDataModels/Detail.cshtml", vm);
    }

    [HttpPost("{id:guid}/import-core-themes")]
    [ValidateAntiForgeryToken]
    public IActionResult ImportCoreThemes(Guid id)
    {
        // Per-census theme copies are no longer supported — every census inherits the shared base set.
        TempData["AdminMessage"] =
            "Censuses inherit the shared default themes. Manage them under Default census themes.";
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost("{id:guid}/settings")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateSettings(Guid id, ServiceDataModelSettingsInput input)
    {
        if (!ModelState.IsValid)
        {
            TempData["AdminError"] = "Check the settings and try again.";
            return RedirectToAction(nameof(Detail), new { id });
        }

        var ok = await _admin.UpdateModelSettingsAsync(id, input, CurrentUserEmail);
        TempData[ok ? "AdminMessage" : "AdminError"] = ok
            ? "Model settings saved."
            : "Could not save settings. You may not have permission, or the model was not found.";
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpGet("{id:guid}/themes/create")]
    public async Task<IActionResult> CreateTheme(Guid id)
    {
        SetAdminChrome("admin-service-data-models");
        ApplyTempDataFlash();

        var detail = await _admin.GetModelDetailAsync(id, CurrentUserEmail);
        if (detail == null)
            return NotFound();

        return View("~/Views/Modern/Admin/ServiceDataModels/ThemeEditor.cshtml", new ServiceDataModelThemeEditorViewModel
        {
            Model = detail,
            IsCreateMode = true,
            CreateInput = new ServiceDataModelGroupInput
            {
                SortOrder = detail.DraftGroups.Count == 0
                    ? 1
                    : detail.DraftGroups.Max(g => g.SortOrder) + 1
            }
        });
    }

    [HttpPost("{id:guid}/themes")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddTheme(Guid id, ServiceDataModelGroupInput input)
    {
        if (!ModelState.IsValid)
        {
            TempData["AdminError"] = "Check the theme details and try again.";
            return RedirectToAction(nameof(CreateTheme), new { id });
        }

        var groupId = await _admin.AddGroupAsync(id, input, CurrentUserEmail);
        if (!groupId.HasValue)
        {
            TempData["AdminError"] = "Could not add theme. You may not have permission, or the model is retired.";
            return RedirectToAction(nameof(CreateTheme), new { id });
        }

        TempData["AdminMessage"] = "Theme created.";
        return RedirectToAction(nameof(EditTheme), new { id, groupId = groupId.Value });
    }

    [HttpGet("{id:guid}/themes/{groupId:guid}")]
    public async Task<IActionResult> EditTheme(Guid id, Guid groupId)
    {
        SetAdminChrome("admin-service-data-models");
        ApplyTempDataFlash();

        var detail = await _admin.GetModelDetailAsync(id, CurrentUserEmail);
        if (detail == null)
            return NotFound();

        var theme = detail.DraftGroups.FirstOrDefault(g => g.Id == groupId);
        if (theme == null)
            return NotFound();

        return View("~/Views/Modern/Admin/ServiceDataModels/ThemeEditor.cshtml", new ServiceDataModelThemeEditorViewModel
        {
            Model = detail,
            SelectedTheme = theme,
            IsCreateMode = false
        });
    }

    [HttpPost("{id:guid}/themes/{groupId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateTheme(Guid id, Guid groupId, ServiceDataModelGroupInput input)
    {
        if (!ModelState.IsValid)
        {
            TempData["AdminError"] = "Check the theme details and try again.";
            return RedirectToAction(nameof(EditTheme), new { id, groupId });
        }

        var ok = await _admin.UpdateGroupAsync(groupId, input, CurrentUserEmail);
        TempData[ok ? "AdminMessage" : "AdminError"] = ok
            ? "Theme saved."
            : "Could not save theme. You may not have permission, or the version is not editable.";
        return RedirectToAction(nameof(EditTheme), new { id, groupId });
    }

    [HttpPost("{id:guid}/themes/{groupId:guid}/disable")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DisableTheme(Guid id, Guid groupId, bool disabled = true, string? returnTo = null)
    {
        var ok = await _admin.SetGroupDisabledAsync(groupId, disabled, CurrentUserEmail);
        TempData[ok ? "AdminMessage" : "AdminError"] = ok
            ? (disabled ? "Theme disabled for respondents." : "Theme re-enabled.")
            : "Could not update theme. You may not have permission, or the version is not editable.";
        if (string.Equals(returnTo, "theme", StringComparison.OrdinalIgnoreCase))
            return RedirectToAction(nameof(EditTheme), new { id, groupId });
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost("{id:guid}/themes/reorder")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReorderThemes(Guid id, [FromForm] Guid[] orderedIds)
    {
        var ok = await _admin.ReorderGroupsAsync(id, orderedIds ?? Array.Empty<Guid>(), CurrentUserEmail);
        TempData[ok ? "AdminMessage" : "AdminError"] = ok
            ? "Theme order saved."
            : "Could not reorder themes.";
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost("{id:guid}/themes/{groupId:guid}/fields")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddField(Guid id, Guid groupId, ServiceDataModelFieldInput input)
    {
        if (!ModelState.IsValid)
        {
            TempData["AdminError"] = "Check the question details and try again.";
            return RedirectToAction(nameof(EditTheme), new { id, groupId });
        }

        var fieldId = await _admin.AddFieldAsync(groupId, input, CurrentUserEmail);
        TempData[fieldId.HasValue ? "AdminMessage" : "AdminError"] = fieldId.HasValue
            ? "Question added."
            : "Could not add question.";
        return RedirectToAction(nameof(EditTheme), new { id, groupId });
    }

    [HttpPost("{id:guid}/themes/{groupId:guid}/fields/{fieldId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateField(Guid id, Guid groupId, Guid fieldId, ServiceDataModelFieldInput input)
    {
        if (!ModelState.IsValid)
        {
            TempData["AdminError"] = "Check the question details and try again.";
            return RedirectToAction(nameof(EditTheme), new { id, groupId });
        }

        var ok = await _admin.UpdateFieldAsync(fieldId, input, CurrentUserEmail);
        TempData[ok ? "AdminMessage" : "AdminError"] = ok
            ? "Question saved."
            : "Could not save question.";
        return RedirectToAction(nameof(EditTheme), new { id, groupId });
    }

    [HttpPost("{id:guid}/themes/{groupId:guid}/fields/{fieldId:guid}/disable")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DisableField(Guid id, Guid groupId, Guid fieldId, bool disabled = true)
    {
        var ok = await _admin.SetFieldDisabledAsync(fieldId, disabled, CurrentUserEmail);
        TempData[ok ? "AdminMessage" : "AdminError"] = ok
            ? (disabled ? "Question disabled for respondents." : "Question re-enabled.")
            : "Could not update question.";
        return RedirectToAction(nameof(EditTheme), new { id, groupId });
    }

    [HttpPost("{id:guid}/themes/{groupId:guid}/fields/reorder")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReorderFields(Guid id, Guid groupId, [FromForm] Guid[] orderedIds)
    {
        var ok = await _admin.ReorderFieldsAsync(groupId, orderedIds ?? Array.Empty<Guid>(), CurrentUserEmail);
        TempData[ok ? "AdminMessage" : "AdminError"] = ok
            ? "Question order saved."
            : "Could not reorder questions.";
        return RedirectToAction(nameof(EditTheme), new { id, groupId });
    }

    [HttpPost("{id:guid}/fields/{fieldId:guid}/options")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddFieldOption(Guid id, Guid fieldId, ServiceDataModelFieldOptionInput input, Guid? groupId = null)
    {
        if (!ModelState.IsValid)
        {
            TempData["AdminError"] = "Check the option details and try again.";
            return groupId.HasValue
                ? RedirectToAction(nameof(EditTheme), new { id, groupId = groupId.Value })
                : RedirectToAction(nameof(Detail), new { id });
        }

        var optionId = await _admin.AddFieldOptionAsync(fieldId, input, CurrentUserEmail);
        TempData[optionId.HasValue ? "AdminMessage" : "AdminError"] = optionId.HasValue
            ? "Option added."
            : "Could not add option.";
        return groupId.HasValue
            ? RedirectToAction(nameof(EditTheme), new { id, groupId = groupId.Value })
            : RedirectToAction(nameof(Detail), new { id });
    }

    // Legacy POST routes kept for any in-flight forms; redirect into the theme editor shell.
    [HttpPost("{id:guid}/groups")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddGroup(Guid id, ServiceDataModelGroupInput input)
        => await AddTheme(id, input);

    [HttpPost("{id:guid}/groups/{groupId:guid}/fields")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddFieldLegacy(Guid id, Guid groupId, ServiceDataModelFieldInput input)
        => await AddField(id, groupId, input);

    [HttpGet("{id:guid}/applicability")]
    public async Task<IActionResult> SetApplicability(Guid id)
    {
        SetAdminChrome("admin-service-data-models");
        ApplyTempDataFlash();

        var detail = await _admin.GetModelDetailAsync(id, CurrentUserEmail);
        if (detail == null)
            return NotFound();

        ViewBag.ModelDetail = detail;
        var input = new ServiceDataModelApplicabilityInput
        {
            Mode = detail.ApplicabilityMode,
            Rules = detail.ApplicabilityRules.Select(r => new ServiceDataModelApplicabilityRuleInput
            {
                ProductStatus = r.ProductStatus,
                PhaseId = r.PhaseId,
                FipsTypeId = r.FipsTypeId,
                FipsBusinessAreaId = r.FipsBusinessAreaId,
                FipsDirectorateId = r.FipsDirectorateId
            }).ToList(),
            ExplicitIncludeProductIds = detail.ExplicitServices
                .Where(s => s.Mode == ServiceDataModelExplicitServiceMode.Include)
                .Select(s => s.CMDBProductId)
                .ToList(),
            ExplicitExcludeProductIds = detail.ExplicitServices
                .Where(s => s.Mode == ServiceDataModelExplicitServiceMode.Exclude)
                .Select(s => s.CMDBProductId)
                .ToList()
        };

        return View("~/Views/Modern/Admin/ServiceDataModels/Applicability.cshtml", input);
    }

    [HttpPost("{id:guid}/applicability")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetApplicability(Guid id, ServiceDataModelApplicabilityInput input)
    {
        SetAdminChrome("admin-service-data-models");

        var ok = await _admin.SetApplicabilityAsync(id, input, CurrentUserEmail);
        TempData[ok ? "AdminMessage" : "AdminError"] = ok
            ? "Applicability saved."
            : "Could not save applicability.";
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpGet("{id:guid}/applicability/preview")]
    public async Task<IActionResult> PreviewApplicability(Guid id)
    {
        SetAdminChrome("admin-service-data-models");
        ApplyTempDataFlash();

        var detail = await _admin.GetModelDetailAsync(id, CurrentUserEmail);
        if (detail == null)
            return NotFound();

        var preview = await _admin.PreviewApplicabilityAsync(id);
        if (preview == null)
            return NotFound();

        ViewBag.ModelDetail = detail;
        ViewBag.ApplicabilityPreview = preview;
        var input = new ServiceDataModelApplicabilityInput
        {
            Mode = detail.ApplicabilityMode,
            Rules = detail.ApplicabilityRules.Select(r => new ServiceDataModelApplicabilityRuleInput
            {
                ProductStatus = r.ProductStatus,
                PhaseId = r.PhaseId,
                FipsTypeId = r.FipsTypeId,
                FipsBusinessAreaId = r.FipsBusinessAreaId,
                FipsDirectorateId = r.FipsDirectorateId
            }).ToList(),
            ExplicitIncludeProductIds = detail.ExplicitServices
                .Where(s => s.Mode == ServiceDataModelExplicitServiceMode.Include)
                .Select(s => s.CMDBProductId)
                .ToList(),
            ExplicitExcludeProductIds = detail.ExplicitServices
                .Where(s => s.Mode == ServiceDataModelExplicitServiceMode.Exclude)
                .Select(s => s.CMDBProductId)
                .ToList()
        };

        return View("~/Views/Modern/Admin/ServiceDataModels/Applicability.cshtml", input);
    }

    [HttpGet("{id:guid}/publish")]
    public async Task<IActionResult> Publish(Guid id)
    {
        SetAdminChrome("admin-service-data-models");
        ApplyTempDataFlash();

        var detail = await _admin.GetModelDetailAsync(id, CurrentUserEmail);
        if (detail == null)
            return NotFound();

        ViewBag.ModelDetail = detail;
        return View("~/Views/Modern/Admin/ServiceDataModels/Publish.cshtml", new ServiceDataModelPublishInput
        {
            DueDays = detail.DefaultDueDaysAfterPublish
        });
    }

    [HttpPost("{id:guid}/publish")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Publish(Guid id, ServiceDataModelPublishInput input)
    {
        SetAdminChrome("admin-service-data-models");

        var ok = await _admin.PublishVersionAsync(id, input, CurrentUserEmail);
        if (!ok)
        {
            TempData["AdminError"] = "Could not publish. Ensure you have permission and a draft version exists.";
            ViewBag.ModelDetail = await _admin.GetModelDetailAsync(id, CurrentUserEmail);
            return View("~/Views/Modern/Admin/ServiceDataModels/Publish.cshtml", input);
        }

        TempData["AdminMessage"] = "Model version published. Assignments will be created for applicable services.";
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost("{id:guid}/retire")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Retire(Guid id)
    {
        var ok = await _admin.RetireModelAsync(id, CurrentUserEmail);
        TempData[ok ? "AdminMessage" : "AdminError"] = ok
            ? "Model retired."
            : "Could not retire this model.";
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost("seed-service-census")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SeedServiceCensus()
    {
        var id = await _admin.SeedDefaultServiceCensusDraftAsync(CurrentUserEmail);
        if (id == null)
        {
            TempData["AdminError"] = "Could not seed Service Census. You may not have permission, or it already exists.";
            return RedirectToAction(nameof(Index));
        }

        TempData["AdminMessage"] = "Service Census draft seeded.";
        return RedirectToAction(nameof(Detail), new { id });
    }
}
