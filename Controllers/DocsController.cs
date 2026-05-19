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
}
