using Compass.Configuration;
using Compass.Services.Api;
using Compass.Services.Docs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
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
    private static readonly JsonSerializerOptions JsonCamelCase = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly ILogger<DocsController> _logger;
    private readonly IApiTokenPortalService _apiTokenPortal;
    private readonly IApiExplorerRequestProxyService _apiExplorerProxy;
    private readonly DocsApiExplorerOptions _apiExplorerOptions;

    public DocsController(
        ILogger<DocsController> logger,
        IApiTokenPortalService apiTokenPortal,
        IApiExplorerRequestProxyService apiExplorerProxy,
        IOptions<DocsApiExplorerOptions> apiExplorerOptions)
    {
        _logger = logger;
        _apiTokenPortal = apiTokenPortal;
        _apiExplorerProxy = apiExplorerProxy;
        _apiExplorerOptions = apiExplorerOptions.Value;
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
    public IActionResult ApiExplorer()
    {
        ViewData["Title"] = "API explorer";
        ViewData["DocsSection"] = "api-explorer";
        ViewData["DocsFullWidth"] = true;
        ViewBag.ExplorerTotalEndpoints = ApiExplorerCatalogueBuilder.CountEndpoints();
        ViewBag.ExplorerProductionUrl = _apiExplorerOptions.ProductionBaseUrl.TrimEnd('/');
        ViewBag.ExplorerTestUrl = _apiExplorerOptions.TestBaseUrl?.TrimEnd('/');
        return View();
    }

    [HttpGet("api-explorer/catalogue")]
    public IActionResult ApiExplorerCatalogue() =>
        Json(ApiExplorerCatalogueBuilder.BuildSections(), JsonCamelCase);

    [HttpGet("api-explorer/user-tokens")]
    public async Task<IActionResult> ApiExplorerUserTokens(CancellationToken cancellationToken)
    {
        var email = User.Identity?.Name?.Trim().ToLowerInvariant() ?? "";
        if (string.IsNullOrEmpty(email))
            return Json(Array.Empty<object>());

        var summaries = await _apiTokenPortal.GetExplorerTokenSummariesAsync(email, cancellationToken);
        return Json(summaries, JsonCamelCase);
    }

    [HttpPost("api-explorer/proxy")]
    public async Task<IActionResult> ApiExplorerProxy(
        [FromBody] ApiExplorerProxyRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var authority = Request.Host.Value ?? "";
            var result = await _apiExplorerProxy.ForwardAsync(request, authority, cancellationToken);
            return Json(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
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
