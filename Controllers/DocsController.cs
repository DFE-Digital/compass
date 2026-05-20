using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Compass.Controllers;

/// <summary>
/// Internal Compass documentation at /docs (developer-style layout).
/// </summary>
[Authorize]
[Route("docs")]
public class DocsController : Controller
{
    private readonly ILogger<DocsController> _logger;

    public DocsController(ILogger<DocsController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Index()
    {
        ViewData["Title"] = "Documentation";
        ViewData["DocsSection"] = "index";
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
        return View();
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
}
