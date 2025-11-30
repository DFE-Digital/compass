using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Compass.Models;
using Compass.Services;
using Compass.Helpers;
using Compass.ViewModels;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

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

        // GET: Standard
        public IActionResult Index()
        {
            var sections = new List<StandardLandingSectionViewModel>
            {
                new()
                {
                    Key = "Published",
                    Title = "Published DDT standards",
                    Subtitle = "Fully assured and ready to reference",
                    Description = "Browse the definitive list of DfE digital, data and technology standards that have cleared the full assurance process.",
                    IconClass = "fas fa-check-circle",
                    Highlights = new[]
                    {
                        "Search and filter every published standard",
                        "View categories, stage and publishing metadata",
                        "Drill into detailed context, rationale and evidence"
                    },
                    ActionText = "Browse published standards",
                    ActionUrl = Url.Action(nameof(Published), "Standard") ?? string.Empty,
                    ActionAriaLabel = "Browse published DDT standards"
                },
                new()
                {
                    Key = "Draft",
                    Title = "Draft DDT standards",
                    Subtitle = "Work in progress in the CMS",
                    Description = "Keep track of standards still going through peer review, consultation or approvals before they are published.",
                    IconClass = "fas fa-file-pen",
                    Highlights = new[]
                    {
                        "See everything currently in drafting or review",
                        "Filter by category, stage or owner",
                        "Pick up actions to keep drafts moving"
                    },
                    ActionText = "Review draft standards",
                    ActionUrl = Url.Action(nameof(Draft), "Standard") ?? string.Empty,
                    ActionAriaLabel = "Review draft DDT standards"
                },
                new()
                {
                    Key = "Create",
                    Title = "Create a DDT standard",
                    Subtitle = "Start a new entry in Strapi",
                    Description = "Jump straight into the authoring experience with the latest template, governance questions and metadata requirements.",
                    IconClass = "fas fa-plus-circle",
                    Highlights = new[]
                    {
                        "Use the standardised template and workflow",
                        "Track progress from Compass once saved",
                        "Keep drafts aligned with assurance expectations"
                    },
                    ActionText = "Create a new standard",
                    ActionUrl = Url.Action(nameof(Create), "Standard") ?? string.Empty,
                    ActionAriaLabel = "Create a new DDT standard in the CMS"
                },
                new()
                {
                    Key = "Service",
                    Title = "Service standards view",
                    Subtitle = "Curated for service teams",
                    Description = "Filter the catalogue to just the items that apply to service design and delivery, including GOV.UK Service Standard alignment.",
                    IconClass = "fas fa-seedling",
                    Highlights = new[]
                    {
                        "Focus on service-aligned categories and stages",
                        "Share a tailored list with multidisciplinary teams",
                        "Export the subset for assurance packs"
                    },
                    ActionText = "Explore service standards",
                    ActionUrl = Url.Action(nameof(Service), "Standard") ?? string.Empty,
                    ActionAriaLabel = "Explore service standards"
                },
                new()
                {
                    Key = "Functional",
                    Title = "Functional standards assessments",
                    Subtitle = "Plan, run and review assessments",
                    Description = "Access the Cabinet Office functional standards held in Compass, including themes, practice areas and assessment tooling.",
                    IconClass = "fas fa-clipboard-check",
                    Highlights = new[]
                    {
                        "Launch the functional standards workspace",
                        "Track assessment submissions and evidence",
                        "Review themes, practice areas and criteria"
                    },
                    ActionText = "Open functional standards",
                    ActionUrl = Url.Action("FunctionalStandards", "EnterpriseReporting") ?? string.Empty,
                    ActionAriaLabel = "Open the functional standards assessment workspace"
                }
            };

            return View(sections);
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
        public IActionResult Service(string search, string category, string stage)
        {
            // Redirect to the new Service Standards dashboard
            return RedirectToAction("Index", "ServiceStandards");
        }

    }
}

