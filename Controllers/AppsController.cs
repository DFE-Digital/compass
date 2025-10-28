using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Compass.Controllers
{
    [Authorize]
    public class AppsController : Controller
    {
        private readonly ILogger<AppsController> _logger;

        public AppsController(ILogger<AppsController> logger)
        {
            _logger = logger;
        }

        // GET: Apps/Index
        public IActionResult Index()
        {
            ViewData["Title"] = "Apps";
            return View();
        }
    }
}

