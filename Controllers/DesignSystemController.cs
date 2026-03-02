using Microsoft.AspNetCore.Mvc;

namespace Compass.Controllers
{
    /// <summary>
    /// Controller for the COMPASS design system documentation.
    /// Provides comprehensive documentation for UI components, patterns, styles, and accessibility guidelines.
    /// </summary>
    public class DesignSystemController : Controller
    {
        /// <summary>
        /// Design system home page - Overview
        /// </summary>
        public IActionResult Index()
        {
            return View();
        }

        // Styles actions
        public IActionResult Typography() => View();
        public IActionResult Colour() => View();
        public IActionResult PageLayouts() => View();
        public IActionResult Links() => View();
        public IActionResult Dates() => View();
        public IActionResult Names() => View();

        // Components actions
        public IActionResult BreadcrumbBar() => View();
        public IActionResult Accordion() => View();
        public IActionResult TogglePanel() => View();
        public IActionResult Container() => View();
        public IActionResult StatCard() => View();
        public IActionResult Navbar() => View();
        public IActionResult Badge() => View();
        public IActionResult Button() => View();
        public IActionResult Table() => View();
        public IActionResult Modal() => View();
        public IActionResult DataList() => View();
        public IActionResult ProgressBar() => View();
        public IActionResult Charts() => View();
        public IActionResult NotificationBanner() => View();

        // Patterns actions
        public IActionResult FilterTable() => View();
        public IActionResult Dashboard() => View();
        public IActionResult Delete() => View();
        public IActionResult Download() => View();
        public IActionResult Submit() => View();
        public IActionResult SearchPerson() => View();

        // Accessibility actions
        public IActionResult ColourContrast() => View();
        public IActionResult ScreenReaders() => View();
        public IActionResult KeyboardUsers() => View();
        public IActionResult Zoom() => View();
        public IActionResult Reflow() => View();
        public IActionResult VoiceControl() => View();
    }
}
