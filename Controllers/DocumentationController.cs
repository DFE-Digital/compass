using Microsoft.AspNetCore.Mvc;

namespace Compass.Controllers
{
    /// <summary>
    /// Controller for the Compass documentation section.
    /// Provides comprehensive documentation for features, processes, and data models.
    /// </summary>
    public class DocumentationController : Controller
    {
        /// <summary>
        /// Documentation home page - Overview
        /// </summary>
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Overview of the Compass platform
        /// </summary>
        public IActionResult Overview()
        {
            return View();
        }

        /// <summary>
        /// Features documentation
        /// </summary>
        public IActionResult Features()
        {
            return View();
        }

        /// <summary>
        /// User guide with user journeys and processes
        /// </summary>
        public IActionResult UserGuide()
        {
            return View();
        }

        /// <summary>
        /// Product Governance documentation
        /// </summary>
        public IActionResult RaidManagement()
        {
            return View();
        }

        /// <summary>
        /// Reporting features documentation
        /// </summary>
        public IActionResult Reporting()
        {
            return View();
        }

        /// <summary>
        /// Data models documentation
        /// </summary>
        public IActionResult DataModels()
        {
            return View();
        }

        /// <summary>
        /// Data dictionary for the database schema
        /// </summary>
        public IActionResult DataDictionary()
        {
            return View();
        }

        /// <summary>
        /// Administration features documentation
        /// </summary>
        public IActionResult Administration()
        {
            return View();
        }

        /// <summary>
        /// API documentation
        /// </summary>
        public IActionResult Api()
        {
            return View();
        }
    }
}

