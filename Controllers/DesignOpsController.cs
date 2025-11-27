using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Compass.Attributes;

namespace Compass.Controllers;

[Authorize]
[RequireDesignOpsAdmin]
public class DesignOpsController : Controller
{
    private readonly ILogger<DesignOpsController> _logger;

    public DesignOpsController(ILogger<DesignOpsController> logger)
    {
        _logger = logger;
    }

    // GET: DesignOps/Dashboard
    public IActionResult Dashboard()
    {
        try
        {
            ViewData["Title"] = "Design Operations Dashboard";
            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading Design Operations dashboard");
            TempData["ErrorMessage"] = "An error occurred while loading the dashboard. Please try again.";
            return View();
        }
    }

    // GET: DesignOps/AccessDenied
    public IActionResult AccessDenied()
    {
        ViewData["Title"] = "Access Denied";
        return View();
    }
}

