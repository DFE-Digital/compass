using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Compass.Models;
using Compass.Services;
using Compass.Helpers;
using Microsoft.Extensions.Configuration;

namespace Compass.Controllers
{
    [Authorize]
    public class StandardController : Controller
    {
        private readonly IStandardsCmsApiService _standardsCmsApiService;
        private readonly ILogger<StandardController> _logger;
        private readonly IConfiguration _configuration;

        public StandardController(IStandardsCmsApiService standardsCmsApiService, ILogger<StandardController> logger, IConfiguration configuration)
        {
            _standardsCmsApiService = standardsCmsApiService;
            _logger = logger;
            _configuration = configuration;
        }

        // GET: Standard/Published
        public async Task<IActionResult> Published(string search, string category, string stage)
        {
            try
            {
                var standards = await _standardsCmsApiService.GetStandardsAsync(
                    published: true,
                    search: search,
                    category: category,
                    stage: stage,
                    cacheDuration: TimeSpan.FromMinutes(5)
                );

                // Get filter options
                var categories = await _standardsCmsApiService.GetCategoriesAsync();
                var stages = await _standardsCmsApiService.GetStagesAsync();

                ViewBag.CurrentSearch = search;
                ViewBag.CurrentCategory = category;
                ViewBag.CurrentStage = stage;
                ViewBag.Categories = categories.Select(c => c.Title).Distinct().OrderBy(t => t).ToList();
                ViewBag.Stages = stages.Select(s => s.Title).Distinct().OrderBy(t => t).ToList();
                ViewBag.IsPublishedView = true;

                return View(standards);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading published standards");
                TempData["ErrorMessage"] = "An error occurred while loading published standards.";
                return View(new List<StandardDto>());
            }
        }

        // GET: Standard/Draft
        public async Task<IActionResult> Draft(string search, string category, string stage)
        {
            try
            {
                // For draft standards, filter by stage title 'Draft'
                // If user provided a stage filter, use it; otherwise default to 'Draft'
                var stageFilter = !string.IsNullOrEmpty(stage) ? stage : "Draft";
                
                var standards = await _standardsCmsApiService.GetStandardsAsync(
                    published: null, // Don't filter by publishedAt, filter by stage instead
                    search: search,
                    category: category,
                    stage: stageFilter,
                    cacheDuration: TimeSpan.FromMinutes(2)
                );

                // Get filter options
                var categories = await _standardsCmsApiService.GetCategoriesAsync();
                var stages = await _standardsCmsApiService.GetStagesAsync();

                ViewBag.CurrentSearch = search;
                ViewBag.CurrentCategory = category;
                ViewBag.CurrentStage = stage; // Keep the original stage filter for display
                ViewBag.Categories = categories.Select(c => c.Title).Distinct().OrderBy(t => t).ToList();
                ViewBag.Stages = stages.Select(s => s.Title).Distinct().OrderBy(t => t).ToList();
                ViewBag.IsPublishedView = false;

                return View(standards);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading draft standards");
                TempData["ErrorMessage"] = "An error occurred while loading draft standards.";
                return View(new List<StandardDto>());
            }
        }

        // GET: Standard/Create
        public IActionResult Create()
        {
            // Get the base URL and convert to admin URL
            var baseUrl = _configuration["StandardsCmsApi:BaseUrl"] ?? "https://dfe-standards-cms-217ce4e280a0.herokuapp.com/api/";
            var adminUrl = baseUrl.Replace("/api/", "/admin/content-manager/collection-types/api::standard.standard/create");
            ViewBag.AdminUrl = adminUrl;
            ViewBag.BaseUrl = baseUrl.Replace("/api/", "");
            return View();
        }

        // GET: Standard/Details/{documentId}
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            try
            {
                // Use documentId to find the standard
                var standard = await _standardsCmsApiService.GetStandardByDocumentIdAsync(id, TimeSpan.FromMinutes(5));
                
                if (standard == null)
                {
                    _logger.LogWarning("Standard not found with documentId: {DocumentId}", id);
                    return NotFound();
                }

                // Process markdown fields
                ViewBag.ProcessedPurpose = MarkdownHelper.ToHtml(standard.Purpose);
                ViewBag.ProcessedHowToMeet = MarkdownHelper.ToHtml(standard.HowToMeet);

                // Sub-categories should now have their category relations populated from the API call
                // Log to verify they're populated
                if (standard.SubCategories != null && standard.SubCategories.Any())
                {
                    var withCategories = standard.SubCategories.Where(sc => sc.Category != null).Count();
                    _logger.LogInformation("Standard has {Total} sub-categories, {WithCategories} have category relations populated", 
                        standard.SubCategories.Count, withCategories);
                    
                    foreach (var subCategory in standard.SubCategories.Where(sc => sc.Category != null))
                    {
                        _logger.LogInformation("Sub-category {Id} ({Title}) belongs to category {CategoryId} ({CategoryTitle})", 
                            subCategory.Id, subCategory.Title, subCategory.Category.Id, subCategory.Category.Title);
                    }
                }

                return View(standard);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading standard details for documentId {DocumentId}", id);
                TempData["ErrorMessage"] = "An error occurred while loading the standard.";
                return RedirectToAction("Published");
            }
        }
    }
}

