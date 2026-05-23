using Compass.Attributes;
using Compass.Models;
using Compass.Services.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Compass.Controllers.Modern;

public partial class ModernAdminController
{
    [HttpGet("api-token-requests/{id:int}")]
    [RequireSuperAdmin]
    public async Task<IActionResult> ApiTokenRequestDetail(
        int id,
        [FromServices] IApiTokenPortalService portal,
        CancellationToken cancellationToken)
    {
        var request = await portal.GetRequestAsync(id, cancellationToken);
        if (request == null)
        {
            TempData["AdminError"] = "Request not found.";
            return RedirectToAction(nameof(Index), new { panel = "api-token-requests" });
        }

        ViewBag.Request = request;
        ViewBag.Permissions = ApiTokenPortalService.DeserializePermissions(request.PermissionsJson);
        ViewBag.Resources = ApiTokenResourceCatalog.Resources;
        if (TempData["AdminMessage"] is string msg)
            ViewBag.AdminMessage = msg;
        if (TempData["AdminError"] is string err)
            ViewBag.AdminError = err;
        SetAdminChrome("admin-index");
        return View("~/Views/Modern/Admin/ApiTokenRequestDetail.cshtml");
    }

    [HttpPost("api-token-requests/{id:int}/approve")]
    [ValidateAntiForgeryToken]
    [RequireSuperAdmin]
    public async Task<IActionResult> ApiTokenRequestApprove(
        int id,
        string? reviewNotes,
        [FromServices] IApiTokenPortalService portal,
        CancellationToken cancellationToken)
    {
        var email = User.Identity?.Name ?? "unknown";
        var result = await portal.ApproveRequestAsync(email, id, reviewNotes, cancellationToken);
        if (!result.Success)
        {
            TempData["AdminError"] = result.ErrorMessage ?? "Could not approve request.";
            return RedirectToAction(nameof(ApiTokenRequestDetail), new { id });
        }

        TempData["AdminMessage"] = "Request approved. The requestor has been emailed their bearer token.";
        if (result.IssuedToken != null)
        {
            TempData["NewToken"] = result.IssuedTokenValue;
            return RedirectToAction(nameof(ApiTokenDetail), new { id = result.IssuedToken.Id });
        }
        return RedirectToAction(nameof(Index), new { panel = "api-token-requests" });
    }

    [HttpPost("api-token-requests/{id:int}/reject")]
    [ValidateAntiForgeryToken]
    [RequireSuperAdmin]
    public async Task<IActionResult> ApiTokenRequestReject(
        int id,
        string reviewNotes,
        [FromServices] IApiTokenPortalService portal,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(reviewNotes))
        {
            TempData["AdminError"] = "Enter a reason when rejecting a request.";
            return RedirectToAction(nameof(ApiTokenRequestDetail), new { id });
        }

        var email = User.Identity?.Name ?? "unknown";
        if (!await portal.RejectRequestAsync(email, id, reviewNotes, cancellationToken))
            TempData["AdminError"] = "Could not reject request.";
        else
            TempData["AdminMessage"] = "Request rejected. The requestor has been notified.";

        return RedirectToAction(nameof(Index), new { panel = "api-token-requests" });
    }
}
