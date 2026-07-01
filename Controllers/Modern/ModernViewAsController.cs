using Compass.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Compass.Controllers.Modern;

[Authorize]
[Route("modern/view-as")]
public sealed class ModernViewAsController : Controller
{
    private readonly IViewAsUserService _viewAs;

    public ModernViewAsController(IViewAsUserService viewAs)
    {
        _viewAs = viewAs;
    }

    [HttpPost("set")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Set(int userId, string? returnUrl)
    {
        var realEmail = GetUserEmail();
        if (string.IsNullOrEmpty(realEmail))
            return Unauthorized();

        var session = await _viewAs.SetActiveAsync(HttpContext, userId, realEmail);
        if (session == null)
        {
            TempData["ErrorMessage"] = "Unable to view as that user.";
            return SafeRedirect(returnUrl, Url.Action("Dashboard", "ModernWork")!);
        }

        TempData["SuccessMessage"] = $"Now viewing Compass as {session.Name}.";
        return SafeRedirect(returnUrl, Url.Action("Dashboard", "ModernWork")!);
    }

    [HttpPost("clear")]
    [ValidateAntiForgeryToken]
    public IActionResult Clear(string? returnUrl)
    {
        _viewAs.ClearActive(HttpContext);
        TempData["SuccessMessage"] = "Stopped viewing as another user.";
        return SafeRedirect(returnUrl, Url.Action("Dashboard", "ModernWork")!);
    }

    private IActionResult SafeRedirect(string? returnUrl, string fallback)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl)
            && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return Redirect(fallback);
    }

    private string GetUserEmail()
    {
        return User.Identity?.Name
            ?? User.FindFirst(ClaimTypes.Email)?.Value
            ?? User.FindFirst("preferred_username")?.Value
            ?? User.FindFirst("email")?.Value
            ?? string.Empty;
    }
}
