using System.Text.Json;
using Compass.Data;
using Compass.Models;
using Compass.Services.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Compass.Controllers;

[Authorize]
[Route("docs/developer/api")]
public class DocsDeveloperApiController : Controller
{
    private readonly IApiTokenPortalService _portal;
    private readonly CompassDbContext _context;

    public DocsDeveloperApiController(IApiTokenPortalService portal, CompassDbContext context)
    {
        _portal = portal;
        _context = context;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Your API keys";
        ViewData["DocsSection"] = "developer-api-keys";
        var email = UserEmail();
        ViewBag.Tokens = await _portal.GetAccessibleTokensAsync(email, cancellationToken);
        ViewBag.PendingRequests = await GetUserPendingRequestsAsync(email, cancellationToken);
        if (TempData["PortalMessage"] is string msg)
            ViewBag.PortalMessage = msg;
        if (TempData["PortalError"] is string err)
            ViewBag.PortalError = err;
        return View("~/Views/Docs/DeveloperApi/Index.cshtml");
    }

    [HttpGet("request")]
    public IActionResult Request()
    {
        ViewData["Title"] = "Request an API key";
        ViewData["DocsSection"] = "developer-api-request";
        ViewBag.Resources = ApiTokenResourceCatalog.Resources;
        ViewBag.Environments = ApiTokenNaming.Environments;
        ViewBag.IsInternal = IApiTokenPortalService.IsInternalUser(UserEmail());
        if (TempData["PortalError"] is string requestErr)
            ViewBag.PortalError = requestErr;
        return View("~/Views/Docs/DeveloperApi/Request.cshtml");
    }

    [HttpPost("request")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestSubmit(
        string environment,
        string projectSlug,
        string? justification,
        bool readOnlyAllData,
        Dictionary<string, string> permissions,
        CancellationToken cancellationToken)
    {
        Dictionary<string, (bool read, bool create, bool update, bool delete)> map;
        if (readOnlyAllData)
            map = ApiTokenResourceCatalog.ReadOnlyAllData();
        else
            map = ParsePermissions(permissions);

        if (map.Count == 0 || map.Values.All(p => !p.read && !p.create && !p.update))
        {
            TempData["PortalError"] = "Select at least read access on one resource, or choose read-only access to all data.";
            return RedirectToAction(nameof(Request));
        }

        var result = await _portal.SubmitRequestAsync(
            UserEmail(), environment, projectSlug, justification, map, cancellationToken);

        if (!result.Success)
        {
            TempData["PortalError"] = result.ErrorMessage;
            return RedirectToAction(nameof(Request));
        }

        if (result.IssuedTokenValue != null)
        {
            TempData["IssuedTokenValue"] = result.IssuedTokenValue;
            TempData["IssuedTokenName"] = result.IssuedToken?.Name;
            return RedirectToAction(nameof(Issued), new { tokenId = result.IssuedToken!.Id });
        }

        TempData["PortalMessage"] = "Your request has been submitted for admin review. You will receive an email when it is approved or rejected.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("issued/{tokenId:int}")]
    public async Task<IActionResult> Issued(int tokenId, CancellationToken cancellationToken)
    {
        if (!await _portal.UserCanManageTokenAsync(UserEmail(), tokenId, cancellationToken))
            return RedirectToAction(nameof(Index));

        ViewData["Title"] = "API key issued";
        ViewData["DocsSection"] = "developer-api-keys";
        ViewBag.TokenId = tokenId;
        ViewBag.TokenName = TempData["IssuedTokenName"]?.ToString();
        ViewBag.TokenValue = TempData["IssuedTokenValue"]?.ToString();
        return View("~/Views/Docs/DeveloperApi/Issued.cshtml");
    }

    [HttpGet("tokens/{id:int}")]
    public async Task<IActionResult> TokenDetail(int id, CancellationToken cancellationToken)
    {
        if (!await _portal.UserCanManageTokenAsync(UserEmail(), id, cancellationToken))
        {
            TempData["PortalError"] = "You do not have access to manage this token.";
            return RedirectToAction(nameof(Index));
        }

        var tokens = await _portal.GetAccessibleTokensAsync(UserEmail(), cancellationToken);
        var token = tokens.FirstOrDefault(t => t.Id == id);
        if (token == null)
            return RedirectToAction(nameof(Index));

        ViewData["Title"] = $"API key — {token.Name}";
        ViewData["DocsSection"] = "developer-api-keys";
        ViewBag.Token = token;
        ViewBag.Resources = ApiTokenResourceCatalog.Resources;
        if (TempData["PortalMessage"] is string msg)
            ViewBag.PortalMessage = msg;
        if (TempData["PortalError"] is string err)
            ViewBag.PortalError = err;
        return View("~/Views/Docs/DeveloperApi/TokenDetail.cshtml");
    }

    [HttpPost("tokens/{id:int}/recycle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TokenRecycle(int id, CancellationToken cancellationToken)
    {
        var result = await _portal.RecycleTokenAsync(UserEmail(), id, cancellationToken);
        if (result == null)
        {
            TempData["PortalError"] = "Could not recycle this token.";
            return RedirectToAction(nameof(TokenDetail), new { id });
        }

        TempData["IssuedTokenValue"] = result.NewTokenValue;
        TempData["IssuedTokenName"] = result.Token.Name;
        return RedirectToAction(nameof(Issued), new { tokenId = id });
    }

    [HttpPost("tokens/{id:int}/members/add")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TokenAddMember(int id, string memberEmail, CancellationToken cancellationToken)
    {
        if (!await _portal.AddMemberAsync(UserEmail(), id, memberEmail, cancellationToken))
            TempData["PortalError"] = "Could not add that user.";
        else
            TempData["PortalMessage"] = "User can now manage this token.";
        return RedirectToAction(nameof(TokenDetail), new { id });
    }

    [HttpPost("tokens/{id:int}/members/remove")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TokenRemoveMember(int id, string memberEmail, CancellationToken cancellationToken)
    {
        await _portal.RemoveMemberAsync(UserEmail(), id, memberEmail, cancellationToken);
        TempData["PortalMessage"] = "User removed.";
        return RedirectToAction(nameof(TokenDetail), new { id });
    }

    [HttpGet("logs")]
    public async Task<IActionResult> Logs(int? tokenId, int? logId, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "API request logs";
        ViewData["DocsSection"] = "developer-api-logs";
        ViewData["DocsFullWidth"] = true;
        ViewBag.Tokens = await _portal.GetAccessibleTokensAsync(UserEmail(), cancellationToken);
        ViewBag.SelectedTokenId = tokenId;
        ViewBag.SelectedLogId = logId;
        var logs = await _portal.GetLogsForUserAsync(UserEmail(), tokenId, cancellationToken: cancellationToken);
        ViewBag.Logs = logs;
        ViewBag.LogsJson = JsonSerializer.Serialize(logs.Select(l => new
        {
            id = l.Id,
            at = l.RequestTimestamp.ToString("yyyy-MM-dd HH:mm:ss"),
            token = l.ApiToken?.Name,
            method = l.HttpMethod,
            path = l.RequestPath + (l.QueryString ?? ""),
            status = l.ResponseStatusCode,
            ok = l.IsSuccess,
            ms = l.ResponseTimeMs,
            ip = l.IpAddress,
            error = l.ErrorMessage,
            requestBody = l.RequestBody,
            responseBody = l.ResponseBody
        }));
        return View("~/Views/Docs/DeveloperApi/Logs.cshtml");
    }

    private string UserEmail() =>
        User.Identity?.Name?.Trim().ToLowerInvariant() ?? "unknown@local";

    private Task<List<ApiTokenRequest>> GetUserPendingRequestsAsync(string email, CancellationToken cancellationToken) =>
        _context.ApiTokenRequests
            .AsNoTracking()
            .Where(r => r.RequestorEmail == email && r.Status == ApiTokenRequestStatus.Pending)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

    private static Dictionary<string, (bool read, bool create, bool update, bool delete)> ParsePermissions(
        Dictionary<string, string> permissions)
    {
        var dict = new Dictionary<string, (bool read, bool create, bool update, bool delete)>();
        foreach (var resource in ApiTokenResourceCatalog.Resources)
        {
            var read = permissions.ContainsKey($"{resource}_read") && permissions[$"{resource}_read"] == "on";
            var create = permissions.ContainsKey($"{resource}_create") && permissions[$"{resource}_create"] == "on";
            var update = permissions.ContainsKey($"{resource}_update") && permissions[$"{resource}_update"] == "on";
            if (read || create || update)
                dict[resource] = (read, create, update, delete: false);
        }
        return dict;
    }
}
