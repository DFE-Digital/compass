using Compass.Services.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Compass.Controllers;

/// <summary>
/// Internal Compass documentation at /docs (developer-style layout).
/// </summary>
[Authorize]
[Route("docs")]
public class DocsController : Controller
{
    private readonly ILogger<DocsController> _logger;
    private readonly IApiTokenPortalService _apiTokenPortal;

    public DocsController(ILogger<DocsController> logger, IApiTokenPortalService apiTokenPortal)
    {
        _logger = logger;
        _apiTokenPortal = apiTokenPortal;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Documentation";
        ViewData["DocsSection"] = "index";
        var email = User.Identity?.Name?.Trim().ToLowerInvariant() ?? "";
        if (!string.IsNullOrEmpty(email))
        {
            var tokens = await _apiTokenPortal.GetAccessibleTokensAsync(email, cancellationToken);
            ViewBag.UserApiKeyCount = tokens.Count;
        }
        else
        {
            ViewBag.UserApiKeyCount = 0;
        }
        return View();
    }

    [HttpGet("api")]
    public IActionResult Api()
    {
        ViewData["Title"] = "API reference";
        ViewData["DocsSection"] = "api";
        return View();
    }

    [HttpGet("api-explorer")]
    public async Task<IActionResult> ApiExplorer(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "API explorer";
        ViewData["DocsSection"] = "api-explorer";
        ViewData["DocsFullWidth"] = true;
        var email = User.Identity?.Name?.Trim().ToLowerInvariant() ?? "";
        var summaries = string.IsNullOrEmpty(email)
            ? []
            : await _apiTokenPortal.GetExplorerTokenSummariesAsync(email, cancellationToken);
        ViewBag.ExplorerUserTokensJson = JsonSerializer.Serialize(
            summaries,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        ViewBag.ExplorerUserTokenCount = summaries.Count;
        return View();
    }

    [HttpGet("api-explorer/bearer/{id:int}")]
    public async Task<IActionResult> ApiExplorerBearer(int id, CancellationToken cancellationToken)
    {
        var email = User.Identity?.Name?.Trim().ToLowerInvariant() ?? "";
        if (string.IsNullOrEmpty(email))
            return Unauthorized();

        var bearer = await _apiTokenPortal.GetExplorerBearerTokenAsync(email, id, cancellationToken);
        if (bearer == null)
            return NotFound();

        return Json(new { bearer });
    }

    [HttpGet("data-models")]
    public IActionResult DataModels()
    {
        ViewData["Title"] = "Data models";
        ViewData["DocsSection"] = "data-models";
        return View();
    }

    [HttpGet("data-flows")]
    public IActionResult DataFlows()
    {
        ViewData["Title"] = "Data flows";
        ViewData["DocsSection"] = "data-flows";
        return View();
    }

    [HttpGet("features")]
    public IActionResult Features()
    {
        ViewData["Title"] = "Feature reference";
        ViewData["DocsSection"] = "features";
        return View();
    }

    [HttpGet("powerbi")]
    public IActionResult PowerBi()
    {
        ViewData["Title"] = "PowerBI developer guide";
        ViewData["DocsSection"] = "powerbi";
        return View();
    }

    [HttpGet("rbac")]
    public IActionResult Rbac()
    {
        ViewData["Title"] = "RBAC & authorization";
        ViewData["DocsSection"] = "rbac";
        return View();
    }

    [HttpGet("specifications")]
    public IActionResult Specifications()
    {
        ViewData["Title"] = "Functional specifications";
        ViewData["DocsSection"] = "specifications";
        return View();
    }
}
