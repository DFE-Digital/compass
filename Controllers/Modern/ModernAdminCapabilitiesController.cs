using System.Security.Claims;
using Compass.Attributes;
using Compass.Services;
using Compass.Services.ServiceDataModels;
using Compass.ViewModels.Modern;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Compass.Controllers.Modern;

/// <summary>Admin UI for the Capabilities catalogue at <c>/modern/admin/capabilities</c>.</summary>
[Authorize]
[RequireAdmin]
[Route("modern/admin/capabilities")]
public sealed class ModernAdminCapabilitiesController : Controller
{
    private readonly ICapabilityAdminService _capabilities;
    private readonly IServiceDataModelAccessService _access;

    public ModernAdminCapabilitiesController(
        ICapabilityAdminService capabilities,
        IServiceDataModelAccessService access)
    {
        _capabilities = capabilities;
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
        ViewBag.SubNavItem = "admin-capabilities";
    }

    private void ApplyFlash()
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
        ApplyFlash();
        var canManage = await _access.CanManageModelsAsync(CurrentUserEmail);
        var items = await _capabilities.ListAsync();
        return View("~/Views/Modern/Admin/Capabilities/Index.cshtml", new CapabilityListViewModel
        {
            CanManage = canManage,
            Items = items.Select(x => new CapabilityRowViewModel
            {
                Id = x.Id,
                Title = x.Title,
                Description = x.Description,
                Reference = x.Reference,
                SortOrder = x.SortOrder,
                IsActive = x.IsActive
            }).ToList()
        });
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create()
    {
        SetAdminChrome();
        ApplyFlash();
        if (!await _access.CanManageModelsAsync(CurrentUserEmail))
            return Forbid();

        return View("~/Views/Modern/Admin/Capabilities/Edit.cshtml", new CapabilityEditViewModel
        {
            IsCreate = true,
            IsActive = true
        });
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreatePost(CapabilityEditViewModel input)
    {
        var (ok, error, id) = await _capabilities.CreateAsync(input, CurrentUserEmail);
        if (!ok)
        {
            TempData["AdminError"] = error ?? "Could not create capability.";
            SetAdminChrome();
            input.IsCreate = true;
            return View("~/Views/Modern/Admin/Capabilities/Edit.cshtml", input);
        }

        TempData["AdminMessage"] = $"Capability \"{CapabilityDisplay.Format(input.Title, input.Reference)}\" added.";
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Edit(Guid id)
    {
        SetAdminChrome();
        ApplyFlash();
        var entity = await _capabilities.GetAsync(id);
        if (entity == null)
            return NotFound();

        return View("~/Views/Modern/Admin/Capabilities/Edit.cshtml", new CapabilityEditViewModel
        {
            Id = entity.Id,
            IsCreate = false,
            Title = entity.Title,
            Description = entity.Description,
            Reference = entity.Reference,
            SortOrder = entity.SortOrder,
            IsActive = entity.IsActive
        });
    }

    [HttpPost("{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditPost(Guid id, CapabilityEditViewModel input)
    {
        var (ok, error) = await _capabilities.UpdateAsync(id, input, CurrentUserEmail);
        if (!ok)
        {
            TempData["AdminError"] = error ?? "Could not update capability.";
            SetAdminChrome();
            input.Id = id;
            input.IsCreate = false;
            return View("~/Views/Modern/Admin/Capabilities/Edit.cshtml", input);
        }

        TempData["AdminMessage"] = $"Capability \"{CapabilityDisplay.Format(input.Title, input.Reference)}\" updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:guid}/disable")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Disable(Guid id, bool disabled = true)
    {
        var ok = await _capabilities.SetDisabledAsync(id, disabled, CurrentUserEmail);
        if (!ok)
        {
            TempData["AdminError"] = "Could not update capability status.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        TempData["AdminMessage"] = disabled
            ? "Capability disabled."
            : "Capability re-enabled.";
        return RedirectToAction(nameof(Edit), new { id });
    }
}
