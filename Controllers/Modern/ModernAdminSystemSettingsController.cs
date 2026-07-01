using Compass.Attributes;
using Compass.Services;
using Compass.ViewModels.Modern;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Compass.Controllers.Modern;

[Authorize]
[RequireAdmin]
[Route("modern/admin/system-settings")]
public sealed class ModernAdminSystemSettingsController : Controller
{
    private readonly IHttpErrorEmailSettingsService _settingsService;
    private readonly ILogger<ModernAdminSystemSettingsController> _logger;

    public ModernAdminSystemSettingsController(
        IHttpErrorEmailSettingsService settingsService,
        ILogger<ModernAdminSystemSettingsController> logger)
    {
        _settingsService = settingsService;
        _logger = logger;
    }

    [HttpGet("errors-to-email")]
    public async Task<IActionResult> ErrorsToEmail(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Errors to email";
        ViewBag.SystemSettingsSection = "errors-to-email";
        ViewBag.MainNavSection = "admin";
        ViewBag.SubNavItem = "admin-system-settings-errors";

        var settings = await _settingsService.GetOrCreateAsync(cancellationToken);
        var model = new HttpErrorEmailSettingsViewModel
        {
            IsEnabled = settings.IsEnabled,
            ContactEmail = settings.ContactEmail,
        };

        if (TempData["AdminMessage"] is string ok)
            ViewBag.AdminMessage = ok;
        if (TempData["AdminError"] is string err)
            ViewBag.AdminError = err;

        return View("~/Views/Modern/Admin/SystemSettings/ErrorsToEmail.cshtml", model);
    }

    [HttpPost("errors-to-email")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ErrorsToEmail(
        HttpErrorEmailSettingsViewModel model,
        CancellationToken cancellationToken)
    {
        ViewBag.SystemSettingsSection = "errors-to-email";

        if (model.IsEnabled && string.IsNullOrWhiteSpace(model.ContactEmail))
        {
            ModelState.AddModelError(nameof(model.ContactEmail), "Enter a contact email when error alerts are enabled.");
        }

        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "Errors to email";
            ViewBag.MainNavSection = "admin";
            ViewBag.SubNavItem = "admin-system-settings-errors";
            return View("~/Views/Modern/Admin/SystemSettings/ErrorsToEmail.cshtml", model);
        }

        var updatedBy = User.Identity?.Name;
        await _settingsService.SaveAsync(model.IsEnabled, model.ContactEmail, updatedBy, cancellationToken);
        _logger.LogInformation(
            "HTTP error email settings updated by {User}: enabled={Enabled}, contact={Contact}",
            updatedBy, model.IsEnabled, model.ContactEmail);

        TempData["AdminMessage"] = "Error email settings saved.";
        return RedirectToAction(nameof(ErrorsToEmail));
    }
}
