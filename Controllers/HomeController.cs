using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FipsReporting.Controllers
{
    public class HomeController : BaseController
    {
        public IActionResult Index()
        {
            // Always show the home page with role-based content
            // The view will handle showing appropriate options based on user roles
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }
    }
}
