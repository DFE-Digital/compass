using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Compass.Data;
using Compass.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Compass.ViewModels.DemandManagement;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Compass.Controllers
{
    [Authorize]
    public class DemandManagementController : Controller
    {
        private readonly CompassDbContext _context;
        private readonly ILogger<DemandManagementController> _logger;
        private readonly IConfiguration _configuration;

        private static readonly IReadOnlyDictionary<string, int> RiskLevelPriority = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["Low"] = 0,
            ["Medium"] = 1,
            ["High"] = 2
        };

        public DemandManagementController(
            CompassDbContext context, 
            ILogger<DemandManagementController> logger,
            IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
        }

        // Check if Demand Management is enabled
        private bool IsDemandManagementEnabled()
        {
            return _configuration.GetValue<bool>("FeatureFlags:EnableDemandManagement", false);
        }

        private static string? NormaliseTriLevel(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var trimmed = value.Trim();

            if (trimmed.Equals("High", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("H", StringComparison.OrdinalIgnoreCase) || trimmed == "3")
            {
                return "High";
            }

            if (trimmed.Equals("Medium", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("Med", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("M", StringComparison.OrdinalIgnoreCase) || trimmed == "2")
            {
                return "Medium";
            }

            if (trimmed.Equals("Low", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("L", StringComparison.OrdinalIgnoreCase) || trimmed == "1")
            {
                return "Low";
            }

            return null;
        }

        private string? CalculatePredictedRiskLevel(DemandRequest request)
        {
            var linkedRiskTypeIds = request.RiskTypeLinks?
                .Select(link => link.RiskTypeId)
                .Distinct()
                .ToList() ?? new List<int>();

            if (linkedRiskTypeIds.Count == 0)
            {
                return string.IsNullOrWhiteSpace(request.RiskIfNotDelivered) ? null : "Medium";
            }

            var severities = _context.RiskTypes
                .AsNoTracking()
                .Where(rt => linkedRiskTypeIds.Contains(rt.Id))
                .Select(rt => rt.Severity)
                .ToList();

            var highestScore = -1;
            string? highestLevel = null;

            foreach (var severity in severities)
            {
                var normalised = NormaliseTriLevel(severity);
                if (normalised == null)
                {
                    continue;
                }

                if (RiskLevelPriority.TryGetValue(normalised, out var candidateScore) && candidateScore > highestScore)
                {
                    highestScore = candidateScore;
                    highestLevel = normalised;
                }
            }

            if (highestLevel != null)
            {
                return highestLevel;
            }

            return string.IsNullOrWhiteSpace(request.RiskIfNotDelivered) ? null : "Medium";
        }

        private void RefreshPredictedRiskLevel(DemandRequest request)
        {
            request.PredictedRiskLevel = CalculatePredictedRiskLevel(request);
        }

        // ==========================================
        // REQUESTS SECTION
        // ==========================================

        // GET: DemandManagement/Requests
        public async Task<IActionResult> Requests(string status, string portfolio, string search, string view)
        {
            if (!IsDemandManagementEnabled())
            {
                return NotFound("Demand Management is not enabled.");
            }

            var userEmail = User.Identity?.Name ?? string.Empty;

            var baseQuery = _context.DemandRequests.AsNoTracking();

            var allRequestsCount = await baseQuery.CountAsync();
            var myRequestsCount = 0;
            var assignedToMeCount = 0;

            if (!string.IsNullOrWhiteSpace(userEmail))
            {
                myRequestsCount = await baseQuery.Where(dr => dr.ApplicantEmail == userEmail).CountAsync();
                assignedToMeCount = await baseQuery.Where(dr => dr.AssignedToEmail == userEmail).CountAsync();
            }

            var filteredByView = baseQuery;
            if (!string.IsNullOrWhiteSpace(view) && !string.IsNullOrWhiteSpace(userEmail))
            {
                if (string.Equals(view, "mine", StringComparison.OrdinalIgnoreCase))
                {
                    filteredByView = filteredByView.Where(dr => dr.ApplicantEmail == userEmail);
                }
                else if (string.Equals(view, "assigned", StringComparison.OrdinalIgnoreCase))
                {
                    filteredByView = filteredByView.Where(dr => dr.AssignedToEmail == userEmail);
                }
            }

            var filteredByPortfolio = filteredByView;
            if (!string.IsNullOrWhiteSpace(portfolio))
            {
                filteredByPortfolio = filteredByPortfolio.Where(dr => dr.PortfolioName == portfolio);
            }

            var filteredBySearch = filteredByPortfolio;
            if (!string.IsNullOrWhiteSpace(search))
            {
                filteredBySearch = filteredBySearch.Where(dr => dr.ProposedTitle.Contains(search) || dr.ReferenceNumber.Contains(search));
            }

            var preStatusQuery = filteredBySearch;

            var finalQuery = preStatusQuery;
            if (!string.IsNullOrWhiteSpace(status))
            {
                finalQuery = finalQuery.Where(dr => dr.Status == status);
            }

            var requests = await finalQuery
                .Include(dr => dr.Prioritisation)
                .OrderByDescending(dr => dr.SubmittedAt ?? dr.CreatedAt)
                .ToListAsync();

            var statusCountsRaw = await preStatusQuery
                .GroupBy(dr => dr.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            var statusOrder = new List<string>
            {
                "Submitted",
                "Explore",
                "Prioritisation",
                "Triage",
                "Delivery",
                "Run",
                "Approved",
                "Deferred",
                "Rejected",
                "Under Review",
                "Draft"
            };

            var orderedStatusCounts = statusCountsRaw
                .OrderBy(sc =>
                {
                    var index = statusOrder.IndexOf(sc.Status ?? string.Empty);
                    return index >= 0 ? index : statusOrder.Count;
                })
                .ThenBy(sc => sc.Status)
                .ToList();

            ViewBag.StatusCounts = orderedStatusCounts;
            ViewBag.TotalCount = orderedStatusCounts.Sum(sc => sc.Count);
            ViewBag.Statuses = orderedStatusCounts.Select(sc => sc.Status ?? string.Empty).Where(s => !string.IsNullOrEmpty(s)).ToList();

            ViewBag.Portfolios = await filteredByView
                .Where(dr => !string.IsNullOrEmpty(dr.PortfolioName))
                .Select(dr => dr.PortfolioName!)
                .Distinct()
                .OrderBy(p => p)
                .ToListAsync();

            ViewBag.CurrentStatus = status;
            ViewBag.CurrentPortfolio = portfolio;
            ViewBag.CurrentSearch = search;
            ViewBag.CurrentView = view;
            ViewBag.AllRequestsCount = allRequestsCount;
            ViewBag.MyRequestsCount = myRequestsCount;
            ViewBag.AssignedToMeCount = assignedToMeCount;
            ViewBag.UserEmail = userEmail;

            return View(requests);
        }

        public async Task<IActionResult> Triage(int? meetingId, string? month)
        {
            if (!IsDemandManagementEnabled())
            {
                return NotFound("Demand Management is not enabled.");
            }

            var meetingEntities = await _context.TriageMeetings
                .Include(tm => tm.DemandRequests)
                    .ThenInclude(dr => dr.Prioritisation)
                .OrderBy(tm => tm.StartAt)
                .ToListAsync();

            var meetingSummaries = new List<TriageMeetingSummaryViewModel>();
            foreach (var meeting in meetingEntities)
            {
                var requests = meeting.DemandRequests
                    .Where(dr => dr.IsSubmittedToTriage == true)
                    .ToList();

                var statusCounts = requests
                    .GroupBy(dr => string.IsNullOrWhiteSpace(dr.Status) ? "Unknown" : dr.Status)
                    .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

                var scoredRequests = requests.Where(r => r.Prioritisation != null).ToList();
                decimal? averageScore = scoredRequests.Any()
                    ? Math.Round(scoredRequests.Average(r => (decimal)r.Prioritisation!.TotalPriorityScore), 1)
                    : (decimal?)null;

                meetingSummaries.Add(new TriageMeetingSummaryViewModel
                {
                    Id = meeting.Id,
                    Title = meeting.Title,
                    StartAt = meeting.StartAt,
                    EndAt = meeting.EndAt,
                    Location = meeting.Location,
                    Description = meeting.Description,
                    IsActive = meeting.IsActive,
                    IsUpcoming = meeting.StartAt >= DateTime.UtcNow,
                    TotalRequests = requests.Count,
                    SubmittedCount = requests.Count(r => string.Equals(r.Status, "Submitted", StringComparison.OrdinalIgnoreCase)),
                    PrioritisationCount = requests.Count(r => string.Equals(r.Status, "Prioritisation", StringComparison.OrdinalIgnoreCase)),
                    TriageCount = requests.Count(r => string.Equals(r.Status, "Triage", StringComparison.OrdinalIgnoreCase)),
                    DeliveryCount = requests.Count(r => string.Equals(r.Status, "Delivery", StringComparison.OrdinalIgnoreCase) || string.Equals(r.Status, "Run", StringComparison.OrdinalIgnoreCase)),
                    DeferredCount = requests.Count(r => string.Equals(r.Status, "Deferred", StringComparison.OrdinalIgnoreCase)),
                    RejectedCount = requests.Count(r => string.Equals(r.Status, "Rejected", StringComparison.OrdinalIgnoreCase)),
                    ConvertedCount = requests.Count(r => r.ConvertedProjectId.HasValue),
                    TierOneCount = requests.Count(r => string.Equals(r.Prioritisation?.PriorityTier, "Tier 1 – Critical", StringComparison.OrdinalIgnoreCase)),
                    TierTwoCount = requests.Count(r => string.Equals(r.Prioritisation?.PriorityTier, "Tier 2 – High", StringComparison.OrdinalIgnoreCase)),
                    TierThreeCount = requests.Count(r => string.Equals(r.Prioritisation?.PriorityTier, "Tier 3 – Medium", StringComparison.OrdinalIgnoreCase)),
                    FundingConfirmedCount = requests.Count(r => r.HasFunding == true),
                    HeadcountConfirmedCount = requests.Count(r => r.HasHeadcount == true),
                    AverageScore = averageScore,
                    StatusCounts = statusCounts
                });
            }

            var monthSummaries = meetingSummaries
                .GroupBy(m => GetMonthKey(m.StartAt))
                .OrderBy(g => g.First().StartAt)
                .Select(g =>
                {
                    var first = g.First();
                    var monthDate = new DateTime(first.StartAt.Year, first.StartAt.Month, 1);
                    return new TriageMonthSummaryViewModel
                    {
                        Key = GetMonthKey(monthDate),
                        Month = monthDate,
                        Label = monthDate.ToString("MMMM yyyy", CultureInfo.InvariantCulture),
                        MeetingCount = g.Count(),
                        RequestCount = g.Sum(m => m.TotalRequests),
                        IsCurrent = monthDate.Year == DateTime.UtcNow.Year && monthDate.Month == DateTime.UtcNow.Month
                    };
                })
                .ToList();

            string? selectedMonthKey = month;
            if (!string.IsNullOrWhiteSpace(selectedMonthKey))
            {
                if (!monthSummaries.Any(ms => string.Equals(ms.Key, selectedMonthKey, StringComparison.OrdinalIgnoreCase)))
                {
                    selectedMonthKey = null;
                }
            }

            if (string.IsNullOrWhiteSpace(selectedMonthKey) && meetingId.HasValue)
            {
                var meetingForSelection = meetingSummaries.FirstOrDefault(m => m.Id == meetingId.Value);
                if (meetingForSelection != null)
                {
                    selectedMonthKey = GetMonthKey(meetingForSelection.StartAt);
                }
            }

            if (string.IsNullOrWhiteSpace(selectedMonthKey))
            {
                var currentMonthKey = GetMonthKey(DateTime.UtcNow);
                if (monthSummaries.Any(ms => string.Equals(ms.Key, currentMonthKey, StringComparison.OrdinalIgnoreCase)))
                {
                    selectedMonthKey = currentMonthKey;
                }
            }

            if (string.IsNullOrWhiteSpace(selectedMonthKey) && monthSummaries.Any())
            {
                selectedMonthKey = monthSummaries
                    .OrderBy(ms => ms.Month)
                    .Last()
                    .Key;
            }

            foreach (var monthSummary in monthSummaries)
            {
                monthSummary.IsSelected = string.Equals(monthSummary.Key, selectedMonthKey, StringComparison.OrdinalIgnoreCase);
            }

            var filteredMeetings = selectedMonthKey == null
                ? meetingSummaries
                : meetingSummaries
                    .Where(m => string.Equals(GetMonthKey(m.StartAt), selectedMonthKey, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(m => m.StartAt)
                    .ToList();

            int? selectedMeetingId = meetingId;
            if (filteredMeetings is List<TriageMeetingSummaryViewModel> filteredList)
            {
                if (selectedMeetingId.HasValue && filteredList.All(m => m.Id != selectedMeetingId.Value))
                {
                    selectedMeetingId = filteredList.FirstOrDefault()?.Id;
                }

                if (!selectedMeetingId.HasValue && filteredList.Any())
                {
                    var upcoming = filteredList.FirstOrDefault(m => m.StartAt >= DateTime.UtcNow);
                    selectedMeetingId = upcoming?.Id ?? filteredList.First().Id;
                }
            }
            else
            {
                var filtered = filteredMeetings.ToList();
                if (selectedMeetingId.HasValue && filtered.All(m => m.Id != selectedMeetingId.Value))
                {
                    selectedMeetingId = filtered.FirstOrDefault()?.Id;
                }

                if (!selectedMeetingId.HasValue && filtered.Any())
                {
                    var upcoming = filtered.FirstOrDefault(m => m.StartAt >= DateTime.UtcNow);
                    selectedMeetingId = upcoming?.Id ?? filtered.First().Id;
                }

                filteredMeetings = filtered;
            }

            var selectedMeeting = selectedMeetingId.HasValue
                ? filteredMeetings.FirstOrDefault(m => m.Id == selectedMeetingId.Value)
                : null;

            List<DemandRequest> meetingRequests;
            if (selectedMeetingId.HasValue)
            {
                meetingRequests = await _context.DemandRequests
                    .Where(dr => dr.IsSubmittedToTriage == true && dr.TriageMeetingId == selectedMeetingId.Value)
                    .Include(dr => dr.Prioritisation)
                    .Include(dr => dr.Assessments)
                    .Include(dr => dr.Notes)
                    .Include(dr => dr.Contacts)
                    .Include(dr => dr.RiskTypeLinks)
                        .ThenInclude(link => link.RiskType)
                    .OrderByDescending(dr => dr.Prioritisation != null ? dr.Prioritisation.TotalPriorityScore : 0)
                    .ThenBy(dr => dr.ProposedTitle)
                    .ToListAsync();
            }
            else
            {
                meetingRequests = new List<DemandRequest>();
            }

            var awaitingScheduling = await _context.DemandRequests
                .Where(dr => dr.IsSubmittedToTriage == true && dr.TriageMeetingId == null)
                .Include(dr => dr.Prioritisation)
                .Include(dr => dr.Assessments)
                .Include(dr => dr.Notes)
                .Include(dr => dr.Contacts)
                .Include(dr => dr.RiskTypeLinks)
                    .ThenInclude(link => link.RiskType)
                .OrderByDescending(dr => dr.TriageSubmittedAt ?? dr.UpdatedAt)
                .ToListAsync();

            var viewModel = new TriageViewModel
            {
                Meetings = (filteredMeetings is List<TriageMeetingSummaryViewModel> list ? list : filteredMeetings.ToList()).AsReadOnly(),
                SelectedMeetingId = selectedMeetingId,
                SelectedMeeting = selectedMeeting,
                MeetingRequests = meetingRequests.AsReadOnly(),
                AwaitingScheduling = awaitingScheduling.AsReadOnly(),
                Months = monthSummaries.AsReadOnly(),
                SelectedMonthKey = selectedMonthKey
            };

            return View(viewModel);
        }

        private static string GetMonthKey(DateTime date) => date.ToString("yyyy-MM");

    // GET: DemandManagement/CreateRequest
    public async Task<IActionResult> CreateRequest()
    {
        if (!IsDemandManagementEnabled())
        {
            return NotFound("Demand Management is not enabled.");
        }

        var userEmail = User.Identity?.Name ?? string.Empty;
        var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? User.FindFirst("name")?.Value ?? string.Empty;

        var model = new DemandRequest
        {
            ApplicantEmail = userEmail,
            ApplicantName = userName,
            ReferenceNumber = GenerateReferenceNumber()
        };

        // Populate dropdowns from APIs
        ViewBag.BusinessAreas = new List<string>();
        ViewBag.Missions = new List<object>();
        ViewBag.Objectives = new List<object>();
        
        try
        {
            // Get business areas from CMS (can run separately)
            var businessAreasTask = GetBusinessAreasFromCmsAsync();
            
            // Get missions from database (must be sequential with objectives)
            _logger.LogInformation("Loading missions from database...");
            var missions = await _context.Missions
                .Where(m => !m.IsDeleted && m.Status == "Active")
                .OrderBy(m => m.Title)
                .Select(m => new { m.Id, Name = m.Title })
                .ToListAsync();
            _logger.LogInformation("Loaded {Count} missions", missions.Count);
            
            // Get objectives from database (must be sequential with missions)
            _logger.LogInformation("Loading objectives from database...");
            var objectives = await _context.Objectives
                .Where(o => !o.IsDeleted && o.Status != "Closed")
                .OrderBy(o => o.Title)
                .Select(o => new { o.Id, o.Title })
                .ToListAsync();
            _logger.LogInformation("Loaded {Count} objectives", objectives.Count);

            // Wait for business areas
            var businessAreas = await businessAreasTask;
            _logger.LogInformation("Loaded {Count} business areas", businessAreas.Count);

            ViewBag.BusinessAreas = businessAreas;
            ViewBag.Missions = missions;
            ViewBag.Objectives = objectives;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading dropdown data for CreateRequest");
        }

        return View(model);
    }

    private async Task<List<string>> GetBusinessAreasFromCmsAsync()
    {
        try
        {
            var businessAreas = await _context.BusinessAreaLookups
                .Where(ba => ba.IsActive)
                .OrderBy(ba => ba.SortOrder)
                .ThenBy(ba => ba.Name)
                .Select(ba => ba.Name)
                .ToListAsync();
                
            _logger.LogInformation("Found {Count} business areas from admin settings", businessAreas.Count);
                return businessAreas;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching business areas from admin settings");
        return new List<string>();
        }
    }

        // POST: DemandManagement/CreateRequest
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRequest(DemandRequest model, List<DemandRequestContact> contacts)
        {
            if (!IsDemandManagementEnabled())
            {
                return NotFound("Demand Management is not enabled.");
            }

            // Log ModelState errors for debugging
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState is invalid. Errors:");
                foreach (var key in ModelState.Keys)
                {
                    var errors = ModelState[key]?.Errors;
                    if (errors != null && errors.Any())
                    {
                        foreach (var error in errors)
                        {
                            _logger.LogWarning("Field: {Field}, Error: {Error}", key, error.ErrorMessage);
                        }
                    }
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    model.CreatedAt = DateTime.UtcNow;
                    model.UpdatedAt = DateTime.UtcNow;
                    
                    // If submitting (not draft), set submitted timestamp
                    if (model.Status == "Submitted")
                    {
                        model.SubmittedAt = DateTime.UtcNow;
                    }

                    _context.DemandRequests.Add(model);

                    // Add contacts
                    if (contacts != null && contacts.Any())
                    {
                        foreach (var contact in contacts.Where(c => !string.IsNullOrEmpty(c.Email)))
                        {
                            contact.DemandRequest = model;
                            contact.CreatedAt = DateTime.UtcNow;
                            _context.DemandRequestContacts.Add(contact);
                        }
                    }

                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"Demand request {model.ReferenceNumber} has been created successfully.";
                    return RedirectToAction(nameof(Details), new { id = model.Id });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating demand request");
                    ModelState.AddModelError("", "An error occurred while creating the request.");
                }
            }

            // If we get here, reload the form data
            try
            {
                var businessAreasTask = GetBusinessAreasFromCmsAsync();
                var missions = await _context.Missions
                    .Where(m => !m.IsDeleted && m.Status == "Active")
                    .OrderBy(m => m.Title)
                    .Select(m => new { m.Id, Name = m.Title })
                    .ToListAsync();
                var objectives = await _context.Objectives
                    .Where(o => !o.IsDeleted && o.Status != "Closed")
                    .OrderBy(o => o.Title)
                    .Select(o => new { o.Id, o.Title })
                    .ToListAsync();
                var businessAreas = await businessAreasTask;

                ViewBag.BusinessAreas = businessAreas;
                ViewBag.Missions = missions;
                ViewBag.Objectives = objectives;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reloading dropdown data");
                ViewBag.BusinessAreas = new List<string>();
                ViewBag.Missions = new List<object>();
                ViewBag.Objectives = new List<object>();
            }

            return View(model);
        }

        // GET: DemandManagement/Details/5
        public async Task<IActionResult> Details(int? id, string? s, string? view)
        {
            if (!IsDemandManagementEnabled())
            {
                return NotFound("Demand Management is not enabled.");
            }

            if (id == null)
            {
                return NotFound();
            }

            var demandRequest = await _context.DemandRequests
                .Include(dr => dr.Contacts)
                .Include(dr => dr.Prioritisation)
                .Include(dr => dr.Notes)
                .Include(dr => dr.Assessments)
                .Include(dr => dr.SectionCompletions)
                .Include(dr => dr.RiskTypeLinks).ThenInclude(link => link.RiskType)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (demandRequest == null)
            {
                return NotFound();
            }

            var requestedSection = string.IsNullOrWhiteSpace(s) ? "Overview" : s;
            var isDocumentView = string.Equals(view, "document", StringComparison.OrdinalIgnoreCase);

            if (!isDocumentView && !string.Equals(requestedSection, "Overview", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction(nameof(Section), new { id = demandRequest.Id, s = requestedSection });
            }

            var viewModel = BuildWorkflowViewModel(
                demandRequest,
                requestedSection,
                isDocumentView);

            await PopulateReferenceDataAsync(viewModel);
            await PopulateTriageMeetingsAsync(viewModel);

            var successMessage = TempData["SectionSuccess"] as string;
            var errorMessage = TempData["SectionError"] as string;
            var definitions = GetSectionDefinitions();
            var sectionContentViewModel = CreateSectionContentViewModel(
                viewModel,
                demandRequest,
                requestedSection,
                definitions,
                successMessage,
                errorMessage);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" || string.Equals(Request.Query["ajax"], "1", StringComparison.OrdinalIgnoreCase))
            {
                return PartialView("_DemandSectionContent", sectionContentViewModel);
            }

            ViewBag.SectionContent = sectionContentViewModel;
            ViewBag.RequestSectionDefinitions = definitions.requestSections;
            ViewBag.AssessmentSectionDefinitions = definitions.assessmentSections;

            return View(viewModel);
        }

        public async Task<IActionResult> Section(int id, string? s)
        {
            if (!IsDemandManagementEnabled())
            {
                return NotFound("Demand Management is not enabled.");
            }

            var demandRequest = await _context.DemandRequests
                .Include(dr => dr.Contacts)
                .Include(dr => dr.Prioritisation)
                .Include(dr => dr.Notes)
                .Include(dr => dr.Assessments)
                .Include(dr => dr.SectionCompletions)
                .Include(dr => dr.RiskTypeLinks).ThenInclude(link => link.RiskType)
                .FirstOrDefaultAsync(dr => dr.Id == id);

            if (demandRequest == null)
            {
                return NotFound();
            }

            var sectionKey = string.IsNullOrWhiteSpace(s) ? "Overview" : s;
            var workflow = BuildWorkflowViewModel(demandRequest, sectionKey, false);
            await PopulateReferenceDataAsync(workflow);
            await PopulateTriageMeetingsAsync(workflow);
            var sectionDefinitions = GetSectionDefinitions();
            var viewModel = CreateSectionContentViewModel(
                workflow,
                demandRequest,
                sectionKey,
                sectionDefinitions,
                TempData["SectionSuccess"] as string,
                TempData["SectionError"] as string);

            return View(viewModel);
        }

        private static (List<SectionDefinition> requestSections, List<SectionDefinition> assessmentSections) GetSectionDefinitions()
        {
            var requestSections = new List<SectionDefinition>
            {
                new("StrategicAlignment", "Strategic alignment"),
                new("ImpactAndRisk", "Impact and risk"),
                new("FundingAndHeadcount", "Funding and headcount"),
                new("DeliveryPlanning", "Delivery planning")
            };

            var assessmentSections = new List<SectionDefinition>
            {
                new("ResearchAndEvidence", "Research and evidence"),
                new("NeedsAssessment", "Needs assessment"),
                new("Recommendations", "Recommendations"),
                new("Notes", "Notes"),
                new("PrioritisationAssessment", "Prioritisation assessment"),
                new("Outcome", "Outcome")
            };

            return (requestSections, assessmentSections);
        }

        private async Task PopulateReferenceDataAsync(DemandWorkflowViewModel viewModel)
        {
            try
            {
                if (viewModel.BusinessAreas == null || !viewModel.BusinessAreas.Any())
                {
                    var businessAreas = await GetBusinessAreasFromCmsAsync();
                    viewModel.BusinessAreas = businessAreas;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unable to load business areas for demand workflow view.");
                viewModel.BusinessAreas = Array.Empty<string>();
            }

            try
            {
                if (viewModel.MissionPillars == null || !viewModel.MissionPillars.Any())
                {
                    var missions = await _context.Missions
                        .Where(m => !m.IsDeleted && m.Status == "Active")
                        .OrderBy(m => m.Title)
                        .Select(m => m.Title)
                        .ToListAsync();
                    viewModel.MissionPillars = missions;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unable to load mission pillars for demand workflow view.");
                viewModel.MissionPillars = Array.Empty<string>();
            }

            try
            {
                if (viewModel.StrategicObjectives == null || !viewModel.StrategicObjectives.Any())
                {
                    var objectives = await _context.Objectives
                        .Where(o => !o.IsDeleted && o.Status != "Closed")
                        .OrderBy(o => o.Title)
                        .Select(o => o.Title)
                        .ToListAsync();
                    viewModel.StrategicObjectives = objectives;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unable to load strategic objectives for demand workflow view.");
                viewModel.StrategicObjectives = Array.Empty<string>();
            }

            try
            {
                if (viewModel.RiskTypes == null || !viewModel.RiskTypes.Any())
                {
                    var riskTypes = await _context.RiskTypes
                        .Where(rt => rt.IsActive)
                        .OrderBy(rt => rt.Name)
                        .ToListAsync();
                    viewModel.RiskTypes = riskTypes.AsReadOnly();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unable to load risk types for demand workflow view.");
                viewModel.RiskTypes = Array.Empty<RiskType>();
            }
        }

        private async Task PopulateTriageMeetingsAsync(DemandWorkflowViewModel viewModel)
        {
            var requestMeetingId = viewModel.Request.TriageMeetingId;

            var meetingsQuery = _context.TriageMeetings.AsQueryable();

            if (requestMeetingId.HasValue)
            {
                meetingsQuery = meetingsQuery.Where(tm => tm.IsActive || tm.Id == requestMeetingId.Value);
            }
            else
            {
                meetingsQuery = meetingsQuery.Where(tm => tm.IsActive);
            }

            var meetings = await meetingsQuery
                .OrderBy(tm => tm.StartAt)
                .ToListAsync();

            if (requestMeetingId.HasValue && meetings.All(tm => tm.Id != requestMeetingId.Value))
            {
                var assignedMeeting = await _context.TriageMeetings.FindAsync(requestMeetingId.Value);
                if (assignedMeeting != null)
                {
                    meetings.Add(assignedMeeting);
                    meetings = meetings
                        .OrderBy(tm => tm.StartAt)
                        .ToList();
                }
            }

            viewModel.TriageMeetings = meetings.AsReadOnly();
        }

    private DemandSectionContentViewModel CreateSectionContentViewModel(
        DemandWorkflowViewModel workflow,
        DemandRequest request,
        string sectionKey,
        (List<SectionDefinition> requestSections, List<SectionDefinition> assessmentSections) definitions,
        string? successMessage,
        string? errorMessage)
    {
        var (requestSections, assessmentSections) = definitions;
        var completion = request.SectionCompletions?.FirstOrDefault(sc => string.Equals(sc.SectionName, sectionKey, StringComparison.OrdinalIgnoreCase));
        var status = completion?.CompletionStatus ?? "ToDo";
        var isAssessment = assessmentSections.Any(s => string.Equals(s.Key, sectionKey, StringComparison.OrdinalIgnoreCase));
        var sectionName = GetSectionDisplayName(sectionKey, definitions);
        var missingFields = workflow.SectionMissingFields.TryGetValue(sectionKey, out var missing)
            ? missing
            : Array.Empty<string>();
        var canComplete = workflow.SectionCompletionEligibility.TryGetValue(sectionKey, out var eligible) && eligible;
        var editUrl = !isAssessment ? Url.Action("Edit", new { id = request.Id }) + $"#{sectionKey.ToLowerInvariant()}" : null;

        return new DemandSectionContentViewModel
        {
            Workflow = workflow,
            RequestSections = requestSections,
            AssessmentSections = assessmentSections,
            SectionStatus = new SectionStatusPanelViewModel
            {
                DemandRequestId = request.Id,
                SectionKey = sectionKey,
                SectionName = sectionName,
                Status = status,
                Completion = completion,
                IsAssessment = isAssessment,
                EditUrl = editUrl,
                CanMarkComplete = canComplete,
                MissingFields = missingFields,
                SuccessMessage = successMessage,
                ErrorMessage = errorMessage ?? completion?.LatestErrorMessage
            },
            SuccessMessage = successMessage,
            ErrorMessage = errorMessage ?? completion?.LatestErrorMessage
        };
    }

    private static string GetSectionDisplayName(string sectionKey, (List<SectionDefinition> requestSections, List<SectionDefinition> assessmentSections) definitions)
    {
        return definitions.requestSections.Concat(definitions.assessmentSections)
            .FirstOrDefault(s => string.Equals(s.Key, sectionKey, StringComparison.OrdinalIgnoreCase))?.Name ?? sectionKey;
    }

    private static DemandRequestSectionCompletion EnsureSectionCompletion(DemandRequest request, string sectionKey)
    {
        var completion = request.SectionCompletions?.FirstOrDefault(sc => string.Equals(sc.SectionName, sectionKey, StringComparison.OrdinalIgnoreCase));
        if (completion != null)
        {
            return completion;
        }

        completion = new DemandRequestSectionCompletion
        {
            DemandRequestId = request.Id,
            SectionName = sectionKey,
            CompletionStatus = "InProgress",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        if (request.SectionCompletions == null)
        {
            request.SectionCompletions = new List<DemandRequestSectionCompletion>();
        }

        request.SectionCompletions.Add(completion);
        return completion;
    }

    private static bool IsSectionComplete(DemandRequest request, string sectionKey, out List<string> missingFields)
    {
        missingFields = new List<string>();

        switch (sectionKey)
        {
            case "StrategicAlignment":
                if (string.IsNullOrWhiteSpace(request.IsManifestoOrStatutory))
                {
                    missingFields.Add("Manifesto or statutory commitment answer");
                }
                if (string.Equals(request.IsManifestoOrStatutory, "Yes", StringComparison.OrdinalIgnoreCase) &&
                    string.IsNullOrWhiteSpace(request.ManifestoStatutoryDetails))
                {
                    missingFields.Add("Manifesto or statutory commitment details");
                }
                if (!request.SupportsOpportunityMissionPillar.HasValue)
                {
                    missingFields.Add("Mission pillar alignment");
                }
                else if (request.SupportsOpportunityMissionPillar.Value && string.IsNullOrWhiteSpace(request.OpportunityMissionPillars))
                {
                    missingFields.Add("Mission pillar selection");
                }
                if (!request.SupportsDdatStrategicTheme.HasValue)
                {
                    missingFields.Add("Strategic theme alignment");
                }
                else if (request.SupportsDdatStrategicTheme.Value && string.IsNullOrWhiteSpace(request.DdatStrategicThemes))
                {
                    missingFields.Add("Strategic theme selection");
                }
                break;

            case "ImpactAndRisk":
                if (string.IsNullOrWhiteSpace(request.OverviewAndBusinessNeed))
                {
                    missingFields.Add("Request description");
                }
                if (string.IsNullOrWhiteSpace(request.ExpectedBenefits))
                {
                    missingFields.Add("Expected outcome");
                }
                if (string.IsNullOrWhiteSpace(request.RiskIfNotDelivered))
                {
                    missingFields.Add("Risk if not delivered");
                }
                break;

            case "FundingAndHeadcount":
                if (!request.HasFunding.HasValue)
                {
                    missingFields.Add("Funding availability");
                }
                else if (request.HasFunding.Value)
                {
                    if (!request.FundingAmount.HasValue && string.IsNullOrWhiteSpace(request.FundingSource))
                    {
                        missingFields.Add("Funding details");
                    }
                }

                if (!request.HasHeadcount.HasValue)
                {
                    missingFields.Add("Headcount availability");
                }
                else if (request.HasHeadcount.Value)
                {
                    if (!request.NumberOfFTE.HasValue && string.IsNullOrWhiteSpace(request.RolesProvided))
                    {
                        missingFields.Add("Headcount details");
                    }
                }
                break;

            case "DeliveryPlanning":
                if (string.IsNullOrWhiteSpace(request.DeliveryTimescales))
                {
                    missingFields.Add("Timescales");
                }
                if (!request.HasTargetDeliveryDate.HasValue)
                {
                    missingFields.Add("Is there a target delivery date?");
                }
                else if (request.HasTargetDeliveryDate.Value && !request.TargetDeliveryDate.HasValue)
                {
                    missingFields.Add("Target delivery date");
                }
                break;

            case "ResearchAndEvidence":
            case "NeedsAssessment":
            case "Recommendations":
            case "Outcome":
                var assessment = request.Assessments?.FirstOrDefault(a => string.Equals(a.AssessmentType, sectionKey, StringComparison.OrdinalIgnoreCase));
                if (assessment == null || string.IsNullOrWhiteSpace(assessment.AssessmentContent))
                {
                    missingFields.Add("Assessment notes");
                }
                break;

            case "PrioritisationAssessment":
                if (request.Prioritisation == null)
                {
                    missingFields.Add("Prioritisation score");
                }
                break;

            default:
                break;
        }

        return missingFields.Count == 0;
    }

    private static bool? ParseNullableBool(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (bool.TryParse(value, out var result))
        {
            return result;
        }

        if (string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.Equals(value, "no", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return null;
    }

    private static DateTime? ParseNullableDate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (DateTime.TryParseExact(value, new[] { "yyyy-MM-dd", "dd/MM/yyyy", "d/M/yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed))
        {
            return parsed;
        }

        return null;
    }

    private static decimal? ParseNullableDecimal(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        return null;
    }

    private static int? ParseNullableInt(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        return null;
    }

    private bool ApplySectionFieldChanges(DemandRequest request, string sectionKey, string fieldKey, IFormCollection form, out string? errorMessage)
    {
        errorMessage = null;
        var stringValue = form["stringValue"].FirstOrDefault()?.Trim();
        var secondaryValue = form["secondaryValue"].FirstOrDefault()?.Trim();
        var boolValue = ParseNullableBool(form["boolValue"].FirstOrDefault());
        var dateValue = ParseNullableDate(form["dateValue"].FirstOrDefault());
        var decimalValue = ParseNullableDecimal(form["decimalValue"].FirstOrDefault());
        var intValue = ParseNullableInt(form["intValue"].FirstOrDefault());
        var selectedRiskTypeIds = new HashSet<int>();
        var riskTypeValues = form["selectedRiskTypeIds"];
        foreach (var value in riskTypeValues)
        {
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedId))
            {
                selectedRiskTypeIds.Add(parsedId);
            }
        }

        switch (sectionKey)
        {
            case "StrategicAlignment":
                switch (fieldKey)
                {
                    case "Manifesto":
                        if (!boolValue.HasValue && string.IsNullOrWhiteSpace(stringValue))
                        {
                            errorMessage = "Select yes, no or unsure.";
                            return false;
                        }

                        var answer = stringValue;
                        if (!string.IsNullOrWhiteSpace(form["manifestoChoice"]))
                        {
                            answer = form["manifestoChoice"].First();
                        }
                        else if (boolValue.HasValue)
                        {
                            answer = boolValue.Value ? "Yes" : "No";
                        }

                        if (string.IsNullOrWhiteSpace(answer))
                        {
                            errorMessage = "Select an option.";
                            return false;
                        }

                        var details = secondaryValue;
                        if (string.Equals(answer, "Yes", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(details))
                        {
                            errorMessage = "Provide details for the manifesto or statutory commitment.";
                            return false;
                        }

                        var changed = false;
                        if (!string.Equals(request.IsManifestoOrStatutory, answer, StringComparison.OrdinalIgnoreCase))
                        {
                            request.IsManifestoOrStatutory = answer;
                            changed = true;
                        }

                        if (string.Equals(answer, "Yes", StringComparison.OrdinalIgnoreCase))
                        {
                            if (!string.Equals(request.ManifestoStatutoryDetails, details, StringComparison.Ordinal))
                            {
                                request.ManifestoStatutoryDetails = details;
                                changed = true;
                            }
                        }
                        else if (!string.IsNullOrWhiteSpace(request.ManifestoStatutoryDetails))
                        {
                            request.ManifestoStatutoryDetails = null;
                            changed = true;
                        }

                        if (!changed)
                        {
                            errorMessage = "No changes were made.";
                            return false;
                        }

                        return true;

                    case "Mission":
                        if (!boolValue.HasValue)
                        {
                            errorMessage = "Select yes or no.";
                            return false;
                        }

                        var supportsMission = boolValue.Value;
                        var missionValues = stringValue?
                            .Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(v => v.Trim())
                            .Where(v => !string.IsNullOrWhiteSpace(v))
                            .ToList() ?? new List<string>();

                        if (supportsMission && missionValues.Count == 0)
                        {
                            errorMessage = "Select at least one mission pillar.";
                            return false;
                        }

                        var newMissionValue = supportsMission ? string.Join(", ", missionValues) : null;
                        var currentMissionValue = request.OpportunityMissionPillars ?? string.Empty;

                        if (request.SupportsOpportunityMissionPillar == supportsMission &&
                            string.Equals(currentMissionValue, newMissionValue ?? string.Empty, StringComparison.Ordinal))
                        {
                            errorMessage = "No changes were made.";
                            return false;
                        }

                        request.SupportsOpportunityMissionPillar = supportsMission;
                        request.OpportunityMissionPillars = newMissionValue;
                        return true;

                    case "StrategicTheme":
                        if (!boolValue.HasValue)
                        {
                            errorMessage = "Select yes or no.";
                            return false;
                        }

                        var supportsTheme = boolValue.Value;
                        var themeValues = stringValue?
                            .Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(v => v.Trim())
                            .Where(v => !string.IsNullOrWhiteSpace(v))
                            .ToList() ?? new List<string>();

                        if (supportsTheme && themeValues.Count == 0)
                        {
                            errorMessage = "Select at least one strategic theme.";
                            return false;
                        }

                        var newThemeValue = supportsTheme ? string.Join(", ", themeValues) : null;
                        var currentThemeValue = request.DdatStrategicThemes ?? string.Empty;

                        if (request.SupportsDdatStrategicTheme == supportsTheme &&
                            string.Equals(currentThemeValue, newThemeValue ?? string.Empty, StringComparison.Ordinal))
                        {
                            errorMessage = "No changes were made.";
                            return false;
                        }

                        request.SupportsDdatStrategicTheme = supportsTheme;
                        request.DdatStrategicThemes = newThemeValue;
                        return true;
                }
                break;

            case "ImpactAndRisk":
                switch (fieldKey)
                {
                    case "OverviewAndBusinessNeed":
                        if (string.IsNullOrWhiteSpace(stringValue))
                        {
                            errorMessage = "Describe the request.";
                            return false;
                        }
                        if (string.Equals(request.OverviewAndBusinessNeed, stringValue, StringComparison.Ordinal))
                        {
                            errorMessage = "No changes were made.";
                            return false;
                        }
                        request.OverviewAndBusinessNeed = stringValue;
                        return true;

                    case "ExpectedBenefits":
                        if (string.IsNullOrWhiteSpace(stringValue))
                        {
                            errorMessage = "Describe the expected outcome.";
                            return false;
                        }
                        if (string.Equals(request.ExpectedBenefits, stringValue, StringComparison.Ordinal))
                        {
                            errorMessage = "No changes were made.";
                            return false;
                        }
                        request.ExpectedBenefits = stringValue;
                        return true;

                    case "RiskIfNotDelivered":
                        if (string.IsNullOrWhiteSpace(stringValue))
                        {
                            errorMessage = "Describe the risk if this is not delivered.";
                            return false;
                        }
                        if (string.Equals(request.RiskIfNotDelivered, stringValue, StringComparison.Ordinal))
                        {
                            errorMessage = "No changes were made.";
                            return false;
                        }
                        request.RiskIfNotDelivered = stringValue;
                        RefreshPredictedRiskLevel(request);
                        return true;

                    case "RiskTypes":
                        var existingRiskTypeIds = request.RiskTypeLinks?
                            .Select(link => link.RiskTypeId)
                            .ToHashSet() ?? new HashSet<int>();

                        if (selectedRiskTypeIds.SetEquals(existingRiskTypeIds))
                        {
                            errorMessage = "No changes were made.";
                            return false;
                        }

                        if (selectedRiskTypeIds.Any())
                        {
                            var validRiskTypeIds = _context.RiskTypes
                                .Where(rt => selectedRiskTypeIds.Contains(rt.Id) && rt.IsActive)
                                .Select(rt => rt.Id)
                                .ToList();

                            if (validRiskTypeIds.Count != selectedRiskTypeIds.Count)
                            {
                                errorMessage = "One or more selected risk categories could not be found.";
                                return false;
                            }
                        }

                        if (request.RiskTypeLinks == null)
                        {
                            request.RiskTypeLinks = new List<DemandRequestRiskType>();
                            existingRiskTypeIds = new HashSet<int>();
                        }

                        var linksToRemove = request.RiskTypeLinks
                            .Where(link => !selectedRiskTypeIds.Contains(link.RiskTypeId))
                            .ToList();

                        if (linksToRemove.Any())
                        {
                            foreach (var link in linksToRemove)
                            {
                                request.RiskTypeLinks.Remove(link);
                            }
                            _context.DemandRequestRiskTypes.RemoveRange(linksToRemove);
                        }

                        var idsToAdd = selectedRiskTypeIds.Except(existingRiskTypeIds);
                        foreach (var riskTypeId in idsToAdd)
                        {
                            request.RiskTypeLinks.Add(new DemandRequestRiskType
                            {
                                DemandRequestId = request.Id,
                                RiskTypeId = riskTypeId,
                                CreatedAt = DateTime.UtcNow
                            });
                        }

                        RefreshPredictedRiskLevel(request);
                        return true;

                    case "RiskLevelOverride":
                        if (string.IsNullOrWhiteSpace(stringValue))
                        {
                            if (string.IsNullOrWhiteSpace(request.RiskLevelOverride))
                            {
                                errorMessage = "No changes were made.";
                                return false;
                            }

                            request.RiskLevelOverride = null;
                            return true;
                        }

                        var normalisedOverride = NormaliseTriLevel(stringValue);
                        if (normalisedOverride == null)
                        {
                            errorMessage = "Select a valid risk level.";
                            return false;
                        }

                        if (string.Equals(request.RiskLevelOverride, normalisedOverride, StringComparison.Ordinal))
                        {
                            errorMessage = "No changes were made.";
                            return false;
                        }

                        request.RiskLevelOverride = normalisedOverride;
                        return true;

                    case "ImpactDetails":
                        var impactLevel = NormaliseTriLevel(stringValue);
                        if (impactLevel == null)
                        {
                            errorMessage = "Select an impact level.";
                            return false;
                        }

                        var summaryValue = string.IsNullOrWhiteSpace(secondaryValue) ? null : secondaryValue;

                        if (string.Equals(request.ImpactLevel, impactLevel, StringComparison.Ordinal) &&
                            string.Equals(request.ImpactSummary ?? string.Empty, summaryValue ?? string.Empty, StringComparison.Ordinal))
                        {
                            errorMessage = "No changes were made.";
                            return false;
                        }

                        request.ImpactLevel = impactLevel;
                        request.ImpactSummary = summaryValue;
                        return true;
                }
                break;

            case "FundingAndHeadcount":
                switch (fieldKey)
                {
                    case "HasFunding":
                        if (!boolValue.HasValue)
                        {
                            errorMessage = "Select yes, no or not sure.";
                            return false;
                        }
                        if (request.HasFunding == boolValue)
                        {
                            errorMessage = "No changes were made.";
                            return false;
                        }
                        request.HasFunding = boolValue;
                        if (boolValue == false)
                        {
                            request.FundingAmount = null;
                            request.FundingSource = null;
                            request.FundingDuration = null;
                            request.FundingNotes = null;
                        }
                        return true;

                    case "FundingAmount":
                        if (!decimalValue.HasValue)
                        {
                            errorMessage = "Enter a funding amount.";
                            return false;
                        }
                        if (request.FundingAmount == decimalValue)
                        {
                            errorMessage = "No changes were made.";
                            return false;
                        }
                        request.FundingAmount = decimalValue;
                        return true;

                    case "FundingSource":
                        if (string.IsNullOrWhiteSpace(stringValue))
                        {
                            errorMessage = "Enter a funding source.";
                            return false;
                        }
                        if (string.Equals(request.FundingSource, stringValue, StringComparison.Ordinal))
                        {
                            errorMessage = "No changes were made.";
                            return false;
                        }
                        request.FundingSource = stringValue;
                        return true;

                    case "FundingDuration":
                        if (string.Equals(request.FundingDuration, stringValue, StringComparison.Ordinal))
                        {
                            errorMessage = "No changes were made.";
                            return false;
                        }
                        request.FundingDuration = stringValue;
                        return true;

                    case "FundingNotes":
                        if (string.Equals(request.FundingNotes, stringValue, StringComparison.Ordinal))
                        {
                            errorMessage = "No changes were made.";
                            return false;
                        }
                        request.FundingNotes = stringValue;
                        return true;

                    case "HasHeadcount":
                        if (!boolValue.HasValue)
                        {
                            errorMessage = "Select yes or no.";
                            return false;
                        }
                        if (request.HasHeadcount == boolValue)
                        {
                            errorMessage = "No changes were made.";
                            return false;
                        }
                        request.HasHeadcount = boolValue;
                        if (boolValue == false)
                        {
                            request.NumberOfFTE = null;
                            request.RolesProvided = null;
                            request.HeadcountDuration = null;
                            request.HeadcountNotes = null;
                        }
                        return true;

                    case "HeadcountFte":
                        if (!intValue.HasValue)
                        {
                            errorMessage = "Enter the number of FTE.";
                            return false;
                        }
                        if (request.NumberOfFTE == intValue)
                        {
                            errorMessage = "No changes were made.";
                            return false;
                        }
                        request.NumberOfFTE = intValue;
                        return true;

                    case "HeadcountRoles":
                        if (string.IsNullOrWhiteSpace(stringValue))
                        {
                            errorMessage = "Describe the roles required.";
                            return false;
                        }
                        if (string.Equals(request.RolesProvided, stringValue, StringComparison.Ordinal))
                        {
                            errorMessage = "No changes were made.";
                            return false;
                        }
                        request.RolesProvided = stringValue;
                        return true;

                    case "HeadcountDuration":
                        if (string.Equals(request.HeadcountDuration, stringValue, StringComparison.Ordinal))
                        {
                            errorMessage = "No changes were made.";
                            return false;
                        }
                        request.HeadcountDuration = stringValue;
                        return true;

                    case "HeadcountNotes":
                        if (string.Equals(request.HeadcountNotes, stringValue, StringComparison.Ordinal))
                        {
                            errorMessage = "No changes were made.";
                            return false;
                        }
                        request.HeadcountNotes = stringValue;
                        return true;
                }
                break;

            case "DeliveryPlanning":
                switch (fieldKey)
                {
                    case "Timescales":
                        if (string.IsNullOrWhiteSpace(stringValue))
                        {
                            errorMessage = "Describe the timescales.";
                            return false;
                        }
                        if (string.Equals(request.DeliveryTimescales, stringValue, StringComparison.Ordinal))
                        {
                            errorMessage = "No changes were made.";
                            return false;
                        }
                        request.DeliveryTimescales = stringValue;
                        return true;

                    case "TargetDeliveryDate":
                        if (!boolValue.HasValue)
                        {
                            errorMessage = "Select yes or no.";
                            return false;
                        }
                        if (boolValue.Value)
                        {
                            if (!dateValue.HasValue)
                            {
                                errorMessage = "Enter the target delivery date.";
                                return false;
                            }
                            if (request.TargetDeliveryDate == dateValue && request.HasTargetDeliveryDate == boolValue)
                            {
                                errorMessage = "No changes were made.";
                                return false;
                            }
                            request.TargetDeliveryDate = dateValue;
                        }
                        else
                        {
                            if (request.HasTargetDeliveryDate == boolValue && request.TargetDeliveryDate == null)
                            {
                                errorMessage = "No changes were made.";
                                return false;
                            }
                            request.TargetDeliveryDate = null;
                        }
                        request.HasTargetDeliveryDate = boolValue;
                        return true;
                }
                break;
        }

        errorMessage = "We could not update that field.";
        return false;
    }

    private IActionResult RedirectToSectionView(int id, string sectionKey)
    {
        if (string.Equals(sectionKey, "Overview", StringComparison.OrdinalIgnoreCase))
        {
            return RedirectToAction(nameof(Details), new { id });
        }

        return RedirectToAction(nameof(Section), new { id, s = sectionKey });
    }

    private IActionResult RedirectBackToSection(string? returnUrl, int id, string sectionKey)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToSectionView(id, sectionKey);
    }

    // POST: DemandManagement/UpdateSectionField
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateSectionField(int id, string sectionKey, string fieldKey)
    {
        if (!IsDemandManagementEnabled())
        {
            return NotFound("Demand Management is not enabled.");
        }

        var demandRequest = await _context.DemandRequests
            .Include(dr => dr.Contacts)
            .Include(dr => dr.Prioritisation)
            .Include(dr => dr.Notes)
            .Include(dr => dr.Assessments)
            .Include(dr => dr.SectionCompletions)
            .Include(dr => dr.RiskTypeLinks).ThenInclude(link => link.RiskType)
            .FirstOrDefaultAsync(dr => dr.Id == id);

        if (demandRequest == null)
        {
            return NotFound();
        }

        var updated = ApplySectionFieldChanges(demandRequest, sectionKey, fieldKey, Request.Form, out var errorMessage);
        var returnUrl = Request.Form["returnUrl"].FirstOrDefault();

        if (!updated)
        {
            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                TempData["SectionError"] = errorMessage;
            }
            return RedirectBackToSection(returnUrl, id, sectionKey);
        }

        demandRequest.UpdatedAt = DateTime.UtcNow;

        var completion = EnsureSectionCompletion(demandRequest, sectionKey);
        if (string.Equals(completion.CompletionStatus, "Completed", StringComparison.OrdinalIgnoreCase))
        {
            completion.CompletedAt = null;
            completion.CompletedByEmail = null;
            completion.CompletedByName = null;
        }
        completion.CompletionStatus = "InProgress";
        completion.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var definitions = GetSectionDefinitions();
        TempData["SectionSuccess"] = $"{GetSectionDisplayName(sectionKey, definitions)} updated.";

        return RedirectBackToSection(returnUrl, id, sectionKey);
    }

    // POST: DemandManagement/CompleteSection
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompleteSection(int id, string sectionKey, string? notes, string submitAction = "complete", string? returnUrl = null)
    {
        if (!IsDemandManagementEnabled())
        {
            return NotFound("Demand Management is not enabled.");
        }

        var demandRequest = await _context.DemandRequests
            .Include(dr => dr.Contacts)
            .Include(dr => dr.Prioritisation)
            .Include(dr => dr.Notes)
            .Include(dr => dr.Assessments)
            .Include(dr => dr.SectionCompletions)
            .Include(dr => dr.RiskTypeLinks).ThenInclude(link => link.RiskType)
            .FirstOrDefaultAsync(dr => dr.Id == id);

        if (demandRequest == null)
        {
            return NotFound();
        }

        var definitions = GetSectionDefinitions();
        var sectionDisplayName = GetSectionDisplayName(sectionKey, definitions);
        var completion = EnsureSectionCompletion(demandRequest, sectionKey);
        var action = submitAction?.Trim().ToLowerInvariant() ?? "complete";
        var trimmedNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();

        if (action == "complete")
        {
            if (!IsSectionComplete(demandRequest, sectionKey, out var missingFields))
            {
                if (missingFields.Any())
                {
                    TempData["SectionError"] = $"Complete the following before marking this section as complete: {string.Join(", ", missingFields)}.";
                }
                else
                {
                    TempData["SectionError"] = "Complete the required information before marking this section as complete.";
                }

                return RedirectBackToSection(returnUrl, id, sectionKey);
            }

            completion.CompletionStatus = "Completed";
            completion.CompletedAt = DateTime.UtcNow;
            completion.CompletedByEmail = User.Identity?.Name ?? string.Empty;
            completion.CompletedByName = User.FindFirst(ClaimTypes.Name)?.Value ?? User.FindFirst("name")?.Value ?? completion.CompletedByEmail;
            completion.CompletionNotes = trimmedNotes;
            completion.UpdatedAt = DateTime.UtcNow;

            demandRequest.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var completionMessage = $"{sectionDisplayName} marked as complete.";
            TempData["SectionSuccess"] = completionMessage;
            TempData["SuccessMessage"] = completionMessage;
            return RedirectBackToSection(returnUrl, id, sectionKey);
        }

        completion.CompletionNotes = trimmedNotes;
        completion.UpdatedAt = DateTime.UtcNow;
        completion.CompletionStatus = "InProgress";
        completion.CompletedAt = null;
        completion.CompletedByEmail = null;
        completion.CompletedByName = null;

        demandRequest.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var statusMessage = action == "reopen"
            ? $"{sectionDisplayName} moved back to In progress."
            : $"{sectionDisplayName} notes saved.";
        TempData["SectionSuccess"] = statusMessage;
        TempData["SuccessMessage"] = statusMessage;

        return RedirectBackToSection(returnUrl, id, sectionKey);
    }

        // POST: DemandManagement/UpdateStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status, string statusReason, DateTime? nextReviewDate, string? currentSection, string? currentView)
        {
            if (!IsDemandManagementEnabled())
            {
                return NotFound("Demand Management is not enabled.");
            }

            var demandRequest = await _context.DemandRequests
                .Include(dr => dr.Contacts)
                .Include(dr => dr.Prioritisation)
                .Include(dr => dr.Notes)
                .Include(dr => dr.Assessments)
                .Include(dr => dr.SectionCompletions)
                .Include(dr => dr.RiskTypeLinks).ThenInclude(link => link.RiskType)
                .FirstOrDefaultAsync(dr => dr.Id == id);
            if (demandRequest == null)
            {
                return NotFound();
            }

            statusReason = statusReason?.Trim() ?? string.Empty;
            var statusChanged = !string.Equals(demandRequest.Status, status, StringComparison.OrdinalIgnoreCase);

            var sectionKey = string.IsNullOrWhiteSpace(currentSection) ? "Overview" : currentSection;
            var viewMode = string.IsNullOrWhiteSpace(currentView) ? "details" : currentView;

            ViewBag.CurrentSection = sectionKey;
            ViewBag.CurrentView = viewMode;
            ViewBag.StatusFormStatus = status;
            ViewBag.StatusFormReason = statusReason;
            ViewBag.StatusFormNextReview = nextReviewDate;

            if (statusChanged && string.IsNullOrWhiteSpace(statusReason))
            {
                ModelState.AddModelError("statusReason", "Enter a decision reason.");
            }

            if (string.Equals(status, "Deferred", StringComparison.OrdinalIgnoreCase) && !nextReviewDate.HasValue)
            {
                ModelState.AddModelError("nextReviewDate", "Select a next review date when deferring a request.");
            }

            if (!ModelState.IsValid)
            {
                var workflowModel = BuildWorkflowViewModel(
                    demandRequest,
                    sectionKey,
                    string.Equals(viewMode, "document", StringComparison.OrdinalIgnoreCase));
                return View("Details", workflowModel);
            }

            demandRequest.Status = status;
            demandRequest.StatusChangeReason = statusChanged ? statusReason.Trim() : demandRequest.StatusChangeReason;
            demandRequest.NextReviewDate = string.Equals(status, "Deferred", StringComparison.OrdinalIgnoreCase) ? nextReviewDate : null;
            demandRequest.DecisionAt = DateTime.UtcNow;
            demandRequest.UpdatedAt = DateTime.UtcNow;

            demandRequest.CurrentPhase = status switch
            {
                "Submitted" => "Request",
                "Explore" => "Explore",
                "Prioritisation" => "Prioritisation",
                "Triage" => "Triage",
                "Delivery" => "Delivery",
                "Run" => "Run",
                _ => demandRequest.CurrentPhase
            };

            if (status == "Submitted" && demandRequest.SubmittedAt == null)
            {
                demandRequest.SubmittedAt = DateTime.UtcNow;
            }

            _context.Update(demandRequest);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Status updated successfully.";
            return RedirectToAction(nameof(Details), new { id, s = sectionKey, view = viewMode });
        }

        // POST: DemandManagement/AddNote
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddNote(int demandRequestId, string noteText, string? returnSection = null, string? returnView = null)
        {
            if (!IsDemandManagementEnabled())
            {
                return NotFound("Demand Management is not enabled.");
            }

            var userEmail = User.Identity?.Name ?? string.Empty;
            var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? User.FindFirst("name")?.Value ?? string.Empty;

            var note = new DemandRequestNote
            {
                DemandRequestId = demandRequestId,
                NoteText = noteText,
                CreatedByEmail = userEmail,
                CreatedByName = userName,
                CreatedAt = DateTime.UtcNow
            };

            _context.DemandRequestNotes.Add(note);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Note added successfully.";

            if (!string.IsNullOrWhiteSpace(returnView) && string.Equals(returnView, "section", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(returnSection))
            {
                return RedirectToAction(nameof(Section), new { id = demandRequestId, s = returnSection });
            }

            if (!string.IsNullOrWhiteSpace(returnSection))
            {
                return RedirectToAction(nameof(Details), new { id = demandRequestId, s = returnSection });
            }

            return RedirectToAction(nameof(Details), new { id = demandRequestId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitToTriage(int id, int? triageMeetingId, string? triageNotes, string? returnSection = null, string? returnView = null)
        {
            if (!IsDemandManagementEnabled())
            {
                return NotFound("Demand Management is not enabled.");
            }

            var demandRequest = await _context.DemandRequests.FindAsync(id);
            if (demandRequest == null)
            {
                return NotFound();
            }

            if (!triageMeetingId.HasValue)
            {
                TempData["ErrorMessage"] = "Select a triage meeting before putting the request forward.";
                return RedirectToTriageReturn(id, returnSection, returnView);
            }

            var meeting = await _context.TriageMeetings.FindAsync(triageMeetingId.Value);
            if (meeting == null)
            {
                TempData["ErrorMessage"] = "The selected triage meeting could not be found.";
                return RedirectToTriageReturn(id, returnSection, returnView);
            }

            demandRequest.TriageMeetingId = triageMeetingId;
            demandRequest.IsSubmittedToTriage = true;
            demandRequest.TriageSubmittedAt = DateTime.UtcNow;
            demandRequest.TriageNotes = string.IsNullOrWhiteSpace(triageNotes) ? null : triageNotes.Trim();
            demandRequest.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Request added to the triage meeting on {meeting.StartAt:dd MMM yyyy HH:mm}.";
            return RedirectToTriageReturn(id, returnSection, returnView);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFromTriage(int id, string? returnSection = null, string? returnView = null)
        {
            if (!IsDemandManagementEnabled())
            {
                return NotFound("Demand Management is not enabled.");
            }

            var demandRequest = await _context.DemandRequests.FindAsync(id);
            if (demandRequest == null)
            {
                return NotFound();
            }

            demandRequest.IsSubmittedToTriage = false;
            demandRequest.TriageMeetingId = null;
            demandRequest.TriageSubmittedAt = null;
            demandRequest.TriageNotes = null;
            demandRequest.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Request removed from the triage meeting.";
            return RedirectToTriageReturn(id, returnSection, returnView);
        }

        private IActionResult RedirectToTriageReturn(int id, string? returnSection, string? returnView)
        {
            if (!string.IsNullOrWhiteSpace(returnView) && string.Equals(returnView, "section", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(returnSection))
            {
                return RedirectToAction(nameof(Section), new { id, s = returnSection });
            }

            if (!string.IsNullOrWhiteSpace(returnSection))
            {
                return RedirectToAction(nameof(Details), new { id, s = returnSection });
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: DemandManagement/UpdateSection
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSection(int demandRequestId, string sectionName, string completionStatus)
        {
            if (!IsDemandManagementEnabled())
            {
                return NotFound("Demand Management is not enabled.");
            }

            var userEmail = User.Identity?.Name ?? string.Empty;
            var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? User.FindFirst("name")?.Value ?? string.Empty;

            var sectionCompletion = await _context.DemandRequestSectionCompletions
                .FirstOrDefaultAsync(sc => sc.DemandRequestId == demandRequestId && sc.SectionName == sectionName);

            if (sectionCompletion == null)
            {
                sectionCompletion = new DemandRequestSectionCompletion
                {
                    DemandRequestId = demandRequestId,
                    SectionName = sectionName,
                    CompletionStatus = completionStatus,
                    CompletedByEmail = userEmail,
                    CompletedByName = userName,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.DemandRequestSectionCompletions.Add(sectionCompletion);
            }
            else
            {
                sectionCompletion.CompletionStatus = completionStatus;
                sectionCompletion.CompletedByEmail = userEmail;
                sectionCompletion.CompletedByName = userName;
                sectionCompletion.UpdatedAt = DateTime.UtcNow;
            }

            if (completionStatus == "Completed")
            {
                sectionCompletion.CompletedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // POST: DemandManagement/UpdateAssessment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAssessment(int demandRequestId, string assessmentType, string assessmentContent, string? returnUrl = null)
        {
            if (!IsDemandManagementEnabled())
            {
                return NotFound("Demand Management is not enabled.");
            }

            var demandRequest = await _context.DemandRequests
                .Include(dr => dr.Assessments)
                .Include(dr => dr.SectionCompletions)
                .Include(dr => dr.RiskTypeLinks).ThenInclude(link => link.RiskType)
                .FirstOrDefaultAsync(dr => dr.Id == demandRequestId);
            if (demandRequest == null)
            {
                return NotFound();
            }

            var userEmail = User.Identity?.Name ?? string.Empty;
            var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? User.FindFirst("name")?.Value ?? string.Empty;

            var assessment = demandRequest.Assessments
                .FirstOrDefault(a => string.Equals(a.AssessmentType, assessmentType, StringComparison.OrdinalIgnoreCase));

            if (assessment == null)
            {
                assessment = new DemandRequestAssessment
                {
                    DemandRequestId = demandRequestId,
                    AssessmentType = assessmentType,
                    AssessmentContent = assessmentContent,
                    AssessedByEmail = userEmail,
                    AssessedByName = userName,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                demandRequest.Assessments.Add(assessment);
            }
            else
            {
                assessment.AssessmentContent = assessmentContent;
                assessment.AssessedByEmail = userEmail;
                assessment.AssessedByName = userName;
                assessment.UpdatedAt = DateTime.UtcNow;
            }

            demandRequest.UpdatedAt = DateTime.UtcNow;

            var completion = EnsureSectionCompletion(demandRequest, assessmentType);
            if (string.Equals(completion.CompletionStatus, "Completed", StringComparison.OrdinalIgnoreCase))
            {
                completion.CompletedAt = null;
                completion.CompletedByEmail = null;
                completion.CompletedByName = null;
                completion.CompletionNotes = null;
            }
            completion.CompletionStatus = "InProgress";
            completion.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var definitions = GetSectionDefinitions();
            TempData["SectionSuccess"] = $"{GetSectionDisplayName(assessmentType, definitions)} updated.";
            return RedirectBackToSection(returnUrl, demandRequestId, assessmentType);
        }

        // GET: DemandManagement/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (!IsDemandManagementEnabled())
            {
                return NotFound("Demand Management is not enabled.");
            }

            if (id == null)
            {
                return NotFound();
            }

            var demandRequest = await _context.DemandRequests
                .Include(dr => dr.Contacts)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (demandRequest == null)
            {
                return NotFound();
            }

            return View(demandRequest);
        }

        private DemandWorkflowViewModel BuildWorkflowViewModel(DemandRequest request, string activeSection, bool isDocumentView)
        {
            var tasks = BuildWorkflowTasks(request, activeSection);
            var stages = BuildWorkflowStages(request, tasks);
            var activity = BuildWorkflowActivity(request);
            var completedCount = tasks.Count(t => string.Equals(t.Status, "Completed", StringComparison.OrdinalIgnoreCase));

            var definitions = GetSectionDefinitions();
            var sectionKeys = definitions.requestSections.Select(s => s.Key)
                .Concat(definitions.assessmentSections.Select(s => s.Key))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var eligibility = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            var missingMap = new Dictionary<string, IReadOnlyCollection<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (var key in sectionKeys)
            {
                var isComplete = IsSectionComplete(request, key, out var missingFields);
                eligibility[key] = isComplete;
                missingMap[key] = missingFields.AsReadOnly();
            }

            return new DemandWorkflowViewModel
            {
                Request = request,
                Tasks = tasks,
                Stages = stages,
                Activity = activity,
                CurrentStageKey = DetermineWorkflowStageKey(request.Status),
                CurrentStageName = GetStageDisplayName(request.Status),
                CurrentStageSummary = GetStageSummary(request),
                CompletedTaskCount = completedCount,
                TotalTaskCount = tasks.Count,
                IsDocumentView = isDocumentView,
                ActiveSectionKey = activeSection,
                SectionCompletionEligibility = eligibility,
                SectionMissingFields = missingMap,
                RiskTypes = Array.Empty<RiskType>()
            };
        }

        private static List<DemandWorkflowTaskViewModel> BuildWorkflowTasks(DemandRequest request, string activeSection)
        {
            var sectionStatusLookup = request.SectionCompletions?.ToDictionary(sc => sc.SectionName, sc => sc) 
                ?? new Dictionary<string, DemandRequestSectionCompletion>();

            DemandWorkflowTaskViewModel CreateTask(string key, string title, string group, string description = "")
            {
                sectionStatusLookup.TryGetValue(key, out var completion);
                var status = completion?.CompletionStatus ?? "ToDo";

                return new DemandWorkflowTaskViewModel
                {
                    Key = key,
                    Title = title,
                    Group = group,
                    Description = string.IsNullOrWhiteSpace(description) ? null : description,
                    Status = status,
                    AssignedTo = completion?.CompletedByName ?? completion?.CompletedByEmail,
                    DueDate = completion?.CompletedAt,
                    Url = null,
                    IsCurrent = string.Equals(activeSection, key, StringComparison.OrdinalIgnoreCase)
                };
            }

            var tasks = new List<DemandWorkflowTaskViewModel>
            {
                CreateTask("StrategicAlignment", "Strategic alignment", "Request", "Mission, statutory commitments and themes"),
                CreateTask("ImpactAndRisk", "Impact & risk", "Request", "Problem statement, benefits and risk"),
                CreateTask("FundingAndHeadcount", "Funding & headcount", "Request", "Funding sources and resourcing"),
                CreateTask("DeliveryPlanning", "Delivery planning", "Request", "Timescales and delivery context"),
                CreateTask("ResearchAndEvidence", "Research & evidence", "Assessment"),
                CreateTask("NeedsAssessment", "Needs assessment", "Assessment"),
                CreateTask("Recommendations", "Recommendations", "Assessment"),
                CreateTask("Notes", "Notes", "Assessment", "Shared notes"),
                CreateTask("PrioritisationAssessment", "Prioritisation assessment", "Assessment"),
                CreateTask("Outcome", "Outcome", "Assessment", "Decision, next steps")
            };

            return tasks;
        }

        private static List<DemandWorkflowStageViewModel> BuildWorkflowStages(DemandRequest request, IEnumerable<DemandWorkflowTaskViewModel> tasks)
        {
            var currentStage = DetermineWorkflowStageKey(request.Status);
            var stageOrder = new[] { "Draft", "Submitted", "Explore", "Prioritisation", "Triage", "Delivery", "Run", "Deferred", "Rejected" };

            var groupedTasks = tasks.GroupBy(t => t.Group).ToDictionary(g => g.Key, g => g.ToList());

            var stages = new List<DemandWorkflowStageViewModel>();
            foreach (var stageKey in stageOrder)
            {
                var status = "upcoming";
                if (string.Equals(stageKey, currentStage, StringComparison.OrdinalIgnoreCase))
                {
                    status = "current";
                }
                else
                {
                    var stageIndex = Array.IndexOf(stageOrder, stageKey);
                    var currentIndex = Array.IndexOf(stageOrder, currentStage);
                    if (stageIndex < currentIndex)
                    {
                        status = "complete";
                    }
                }

                var stageName = GetStageDisplayName(stageKey);
                var stageTasks = groupedTasks.TryGetValue(GetStageGroupForStage(stageKey), out var stageGroupTasks)
                    ? stageGroupTasks
                    : new List<DemandWorkflowTaskViewModel>();

                stages.Add(new DemandWorkflowStageViewModel
                {
                    Key = stageKey,
                    Name = stageName,
                    Status = status,
                    Summary = null,
                    Tasks = stageTasks
                });
            }

            return stages;
        }

        private static List<DemandWorkflowActivityViewModel> BuildWorkflowActivity(DemandRequest request)
        {
            var activities = new List<DemandWorkflowActivityViewModel>
            {
                new()
                {
                    Timestamp = request.CreatedAt,
                    Title = "Request created",
                    Description = $"Draft created by {request.ApplicantName}",
                    Actor = request.ApplicantName,
                    Type = "system",
                    Icon = "fas fa-plus-circle"
                }
            };

            if (request.SubmittedAt.HasValue)
            {
                activities.Add(new DemandWorkflowActivityViewModel
                {
                    Timestamp = request.SubmittedAt.Value,
                    Title = "Request submitted",
                    Description = "Marked as New",
                    Actor = request.ApplicantName,
                    Type = "status",
                    Icon = "fas fa-paper-plane"
                });
            }

            if (!string.IsNullOrWhiteSpace(request.StatusChangeReason))
            {
                activities.Add(new DemandWorkflowActivityViewModel
                {
                    Timestamp = request.UpdatedAt,
                    Title = $"Status set to {request.Status}",
                    Description = request.StatusChangeReason,
                    Actor = request.ReviewedBy,
                    Type = "status",
                    Icon = "fas fa-exchange-alt"
                });
            }

            if (request.Notes != null && request.Notes.Any())
            {
                foreach (var note in request.Notes.OrderByDescending(n => n.CreatedAt))
                {
                    activities.Add(new DemandWorkflowActivityViewModel
                    {
                        Timestamp = note.CreatedAt,
                        Title = note.CreatedByName ?? note.CreatedByEmail ?? "Note",
                        Description = note.NoteText,
                        Actor = note.CreatedByName ?? note.CreatedByEmail,
                        Type = "note",
                        Icon = "fas fa-comment"
                    });
                }
            }

            return activities
                .OrderByDescending(a => a.Timestamp)
                .ToList();
        }

        private static string DetermineWorkflowStageKey(string? status)
        {
            return status switch
            {
                "Draft" => "Draft",
                "Submitted" => "Submitted",
                "Explore" => "Explore",
                "Prioritisation" => "Prioritisation",
                "Triage" => "Triage",
                "Delivery" => "Delivery",
                "Run" => "Run",
                "Deferred" => "Deferred",
                "Rejected" => "Rejected",
                _ => "Submitted"
            };
        }

        private static string GetStageDisplayName(string? status)
        {
            return status switch
            {
                "Draft" => "Draft",
                "Submitted" => "Submitted request awaiting triage",
                "Explore" => "Explore",
                "Prioritisation" => "Prioritisation",
                "Triage" => "Triage",
                "Delivery" => "Delivery",
                "Run" => "Run",
                "Deferred" => "Deferred",
                "Rejected" => "Rejected",
                _ => "Workflow"
            };
        }

        private static string GetStageGroupForStage(string stageKey)
        {
            return stageKey switch
            {
                "Draft" => "Request",
                "Submitted" => "Request",
                "Explore" => "Assessment",
                "Prioritisation" => "Assessment",
                "Triage" => "Assessment",
                "Delivery" => "Outcome",
                "Run" => "Outcome",
                "Deferred" => "Outcome",
                "Rejected" => "Outcome",
                _ => "Request"
            };
        }

        private static string GetStageSummary(DemandRequest request)
        {
            return request.Status switch
            {
                "Draft" => "Draft request awaiting submission",
                "Submitted" => "Submitted request awaiting triage",
                "Explore" => "In discovery and evidence gathering",
                "Prioritisation" => "Being scored for prioritisation",
                "Triage" => "Triage decision in progress",
                "Delivery" => "Handed over for delivery",
                "Run" => "In live service",
                "Deferred" => request.NextReviewDate.HasValue
                    ? $"Deferred until {request.NextReviewDate.Value:dd MMM yyyy}"
                    : "Deferred",
                "Rejected" => "Rejected",
                _ => "Workflow in progress"
            };
        }

        // POST: DemandManagement/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, DemandRequest model, List<DemandRequestContact> contacts)
        {
            if (!IsDemandManagementEnabled())
            {
                return NotFound("Demand Management is not enabled.");
            }

            if (id != model.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingRequest = await _context.DemandRequests
                        .Include(dr => dr.Contacts)
                        .FirstOrDefaultAsync(dr => dr.Id == id);

                    if (existingRequest == null)
                    {
                        return NotFound();
                    }

                    // Update properties
                    existingRequest.BusinessArea = model.BusinessArea;
                    existingRequest.SeniorResponsibleOfficer = model.SeniorResponsibleOfficer;
                    existingRequest.HasPortfolioSupport = model.HasPortfolioSupport;
                    existingRequest.PortfolioName = model.PortfolioName;
                    existingRequest.PortfolioPrioritisation = model.PortfolioPrioritisation;
                    existingRequest.ProposedTitle = model.ProposedTitle;
                    existingRequest.OverviewAndBusinessNeed = model.OverviewAndBusinessNeed;
                    existingRequest.PreviousResearchOrInsight = model.PreviousResearchOrInsight;
                    existingRequest.WillCreateOrChangeDigitalService = model.WillCreateOrChangeDigitalService;
                    existingRequest.DigitalServiceDetails = model.DigitalServiceDetails;
                    existingRequest.IsManifestoOrStatutory = model.IsManifestoOrStatutory;
                    existingRequest.ManifestoStatutoryDetails = model.ManifestoStatutoryDetails;
                    existingRequest.SupportsOpportunityMissionPillar = model.SupportsOpportunityMissionPillar;
                    existingRequest.OpportunityMissionPillars = model.OpportunityMissionPillars;
                    existingRequest.SupportsDdatStrategicTheme = model.SupportsDdatStrategicTheme;
                    existingRequest.DdatStrategicThemes = model.DdatStrategicThemes;
                    existingRequest.ExpectedBenefits = model.ExpectedBenefits;
                    existingRequest.RiskIfNotDelivered = model.RiskIfNotDelivered;
                    existingRequest.HasFunding = model.HasFunding;
                    existingRequest.FundingAmount = model.FundingAmount;
                    existingRequest.FundingSource = model.FundingSource;
                    existingRequest.FundingDuration = model.FundingDuration;
                    existingRequest.FundingNotes = model.FundingNotes;
                    existingRequest.HasHeadcount = model.HasHeadcount;
                    existingRequest.NumberOfFTE = model.NumberOfFTE;
                    existingRequest.RolesProvided = model.RolesProvided;
                    existingRequest.HeadcountDuration = model.HeadcountDuration;
                    existingRequest.HeadcountNotes = model.HeadcountNotes;
                    existingRequest.HasTargetDeliveryDate = model.HasTargetDeliveryDate;
                    existingRequest.TargetDeliveryDate = model.TargetDeliveryDate;
                    existingRequest.Status = model.Status;
                    existingRequest.AssignedToEmail = model.AssignedToEmail;
                    existingRequest.AssignedToName = model.AssignedToName;
                    existingRequest.CurrentPhase = model.CurrentPhase;
                    existingRequest.DeclarationConfirmed = model.DeclarationConfirmed;
                    existingRequest.UpdatedAt = DateTime.UtcNow;

                    // If status changed to Submitted, set SubmittedAt
                    if (model.Status == "Submitted" && existingRequest.SubmittedAt == null)
                    {
                        existingRequest.SubmittedAt = DateTime.UtcNow;
                    }

                    // Update contacts
                    _context.DemandRequestContacts.RemoveRange(existingRequest.Contacts);
                    if (contacts != null && contacts.Any())
                    {
                        foreach (var contact in contacts.Where(c => !string.IsNullOrEmpty(c.Email)))
                        {
                            contact.DemandRequestId = existingRequest.Id;
                            contact.CreatedAt = DateTime.UtcNow;
                            _context.DemandRequestContacts.Add(contact);
                        }
                    }

                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Demand request has been updated successfully.";
                    return RedirectToAction(nameof(Details), new { id = model.Id });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await DemandRequestExists(model.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating demand request");
                    ModelState.AddModelError("", "An error occurred while updating the request.");
                }
            }

            return View(model);
        }

        // ==========================================
        // PRIORITISATION SECTION
        // ==========================================

        // GET: DemandManagement/Prioritisation
        public async Task<IActionResult> Prioritisation(string tier, string portfolio)
        {
            if (!IsDemandManagementEnabled())
            {
                return NotFound("Demand Management is not enabled.");
            }

            var query = _context.DemandRequests
                .Include(dr => dr.Prioritisation)
                .Include(dr => dr.Contacts)
                .Where(dr => dr.Status == "Submitted" || dr.Status == "Under Review" || dr.Status == "Approved")
                .AsQueryable();

            // Apply tier filter
            if (!string.IsNullOrEmpty(tier))
            {
                query = query.Where(dr => dr.Prioritisation != null && dr.Prioritisation.PriorityTier == tier);
            }

            // Apply portfolio filter
            if (!string.IsNullOrEmpty(portfolio))
            {
                query = query.Where(dr => dr.PortfolioName == portfolio);
            }

            var requests = await query
                .OrderByDescending(dr => dr.Prioritisation != null ? dr.Prioritisation.TotalPriorityScore : 0)
                .ThenByDescending(dr => dr.SubmittedAt)
                .ToListAsync();

            // Get distinct tiers and portfolios for filters
            ViewBag.Tiers = new List<string> 
            { 
                "Tier 1 – Critical", 
                "Tier 2 – High", 
                "Tier 3 – Medium", 
                "Tier 4 – Low" 
            };

            ViewBag.Portfolios = await _context.DemandRequests
                .Where(dr => !string.IsNullOrEmpty(dr.PortfolioName))
                .Select(dr => dr.PortfolioName)
                .Distinct()
                .OrderBy(p => p)
                .ToListAsync();

            ViewBag.CurrentTier = tier;
            ViewBag.CurrentPortfolio = portfolio;

            return View(requests);
        }

        // GET: DemandManagement/ScoreRequest/5
        public async Task<IActionResult> ScoreRequest(int? id)
        {
            if (!IsDemandManagementEnabled())
            {
                return NotFound("Demand Management is not enabled.");
            }

            if (id == null)
            {
                return NotFound();
            }

            var demandRequest = await _context.DemandRequests
                .Include(dr => dr.Prioritisation)
                .Include(dr => dr.Contacts)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (demandRequest == null)
            {
                return NotFound();
            }

            // Create prioritisation if it doesn't exist
            if (demandRequest.Prioritisation == null)
            {
                demandRequest.Prioritisation = new DemandRequestPrioritisation
                {
                    DemandRequestId = demandRequest.Id
                };
            }

            return View(demandRequest);
        }

        // POST: DemandManagement/ScoreRequest/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ScoreRequest(int id, DemandRequestPrioritisation model)
        {
            if (!IsDemandManagementEnabled())
            {
                return NotFound("Demand Management is not enabled.");
            }

            if (id != model.DemandRequestId)
            {
                return NotFound();
            }

            var demandRequest = await _context.DemandRequests
                .Include(dr => dr.Prioritisation)
                .Include(dr => dr.Contacts)
                .Include(dr => dr.SectionCompletions)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (demandRequest == null)
            {
                return NotFound();
            }

            ValidatePrioritisationModel(model, ModelState);

            if (!ModelState.IsValid)
            {
                await RecordPrioritisationSectionErrorsAsync(demandRequest, CollectModelErrors(ModelState));
                PopulatePrioritisationForDisplay(demandRequest, model);
                return View(demandRequest);
            }

            try
            {
                model.StrategicAlignmentTotal = (model.StatutoryManifestoScore + model.OpportunityMissionScore + model.DdatStrategicThemeScore) * 2;
                model.UserImpactTotal = (model.ScaleOfUsersScore + model.EvidenceOfUserNeedScore) * 2;
                model.RiskUrgencyTotal = (model.RiskIfNotDeliveredScore + model.TargetDeliveryUrgencyScore) * 2;
                model.FeasibilityTotal = (model.FundingAvailableScore + model.HeadcountAvailableScore + model.PortfolioFitScore) * 1;
                model.ValueOutcomeTotal = model.ExpectedBenefitsScore * 1;

                var rawTotal = model.StrategicAlignmentTotal + model.UserImpactTotal + model.RiskUrgencyTotal + model.FeasibilityTotal + model.ValueOutcomeTotal;
                model.TotalPriorityScore = (int)Math.Round((rawTotal / 90.0) * 100);

                if (model.TotalPriorityScore >= 80)
                {
                    model.PriorityTier = "Tier 1 – Critical";
                }
                else if (model.TotalPriorityScore >= 60)
                {
                    model.PriorityTier = "Tier 2 – High";
                }
                else if (model.TotalPriorityScore >= 40)
                {
                    model.PriorityTier = "Tier 3 – Medium";
                }
                else
                {
                    model.PriorityTier = "Tier 4 – Low";
                }

                model.ScoredBy = User.Identity?.Name ?? string.Empty;
                model.ScoredAt = DateTime.UtcNow;
                model.UpdatedAt = DateTime.UtcNow;

                var prioritisation = demandRequest.Prioritisation;
                if (prioritisation == null)
                {
                    prioritisation = new DemandRequestPrioritisation
                    {
                        DemandRequestId = demandRequest.Id,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    demandRequest.Prioritisation = prioritisation;
                    _context.DemandRequestPrioritisations.Add(prioritisation);
                }

                prioritisation.StatutoryManifestoScore = model.StatutoryManifestoScore;
                prioritisation.OpportunityMissionScore = model.OpportunityMissionScore;
                prioritisation.DdatStrategicThemeScore = model.DdatStrategicThemeScore;
                prioritisation.ScaleOfUsersScore = model.ScaleOfUsersScore;
                prioritisation.EvidenceOfUserNeedScore = model.EvidenceOfUserNeedScore;
                prioritisation.RiskIfNotDeliveredScore = model.RiskIfNotDeliveredScore;
                prioritisation.TargetDeliveryUrgencyScore = model.TargetDeliveryUrgencyScore;
                prioritisation.FundingAvailableScore = model.FundingAvailableScore;
                prioritisation.HeadcountAvailableScore = model.HeadcountAvailableScore;
                prioritisation.PortfolioFitScore = model.PortfolioFitScore;
                prioritisation.ExpectedBenefitsScore = model.ExpectedBenefitsScore;
                prioritisation.StrategicAlignmentTotal = model.StrategicAlignmentTotal;
                prioritisation.UserImpactTotal = model.UserImpactTotal;
                prioritisation.RiskUrgencyTotal = model.RiskUrgencyTotal;
                prioritisation.FeasibilityTotal = model.FeasibilityTotal;
                prioritisation.ValueOutcomeTotal = model.ValueOutcomeTotal;
                prioritisation.TotalPriorityScore = model.TotalPriorityScore;
                prioritisation.PriorityTier = model.PriorityTier;
                prioritisation.ScoringNotes = model.ScoringNotes;
                prioritisation.ScoredBy = model.ScoredBy;
                prioritisation.ScoredAt = model.ScoredAt;
                prioritisation.UpdatedAt = DateTime.UtcNow;

                if (string.Equals(demandRequest.Status, "Submitted", StringComparison.OrdinalIgnoreCase))
                {
                    demandRequest.Status = "Under Review";
                    demandRequest.ReviewedAt = DateTime.UtcNow;
                    demandRequest.ReviewedBy = User.Identity?.Name ?? string.Empty;
                }

                demandRequest.UpdatedAt = DateTime.UtcNow;

                var completion = EnsureSectionCompletion(demandRequest, "PrioritisationAssessment");
                completion.LatestErrorMessage = null;
                completion.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Prioritisation scoring has been saved successfully.";
                return RedirectToAction(nameof(Prioritisation));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scoring demand request");
                ModelState.AddModelError("", "An error occurred while scoring the request.");
                await RecordPrioritisationSectionErrorsAsync(demandRequest, CollectModelErrors(ModelState));
                PopulatePrioritisationForDisplay(demandRequest, model);
            }

            return View(demandRequest);
        }

        // ==========================================
        // REPORTING SECTION
        // ==========================================

        // GET: DemandManagement/Reporting
        public async Task<IActionResult> Reporting()
        {
            if (!IsDemandManagementEnabled())
            {
                return NotFound("Demand Management is not enabled.");
            }

            // Get summary statistics
            var allRequests = await _context.DemandRequests
                .Include(dr => dr.Prioritisation)
                .ToListAsync();

            ViewBag.TotalRequests = allRequests.Count;
            ViewBag.DraftRequests = allRequests.Count(r => r.Status == "Draft");
            ViewBag.SubmittedRequests = allRequests.Count(r => r.Status == "Submitted");
            ViewBag.UnderReviewRequests = allRequests.Count(r => r.Status == "Under Review");
            ViewBag.ApprovedRequests = allRequests.Count(r => r.Status == "Approved");
            ViewBag.DeferredRequests = allRequests.Count(r => r.Status == "Deferred");
            ViewBag.RejectedRequests = allRequests.Count(r => r.Status == "Rejected");

            // Tier breakdown
            ViewBag.Tier1Requests = allRequests.Count(r => r.Prioritisation != null && r.Prioritisation.PriorityTier == "Tier 1 – Critical");
            ViewBag.Tier2Requests = allRequests.Count(r => r.Prioritisation != null && r.Prioritisation.PriorityTier == "Tier 2 – High");
            ViewBag.Tier3Requests = allRequests.Count(r => r.Prioritisation != null && r.Prioritisation.PriorityTier == "Tier 3 – Medium");
            ViewBag.Tier4Requests = allRequests.Count(r => r.Prioritisation != null && r.Prioritisation.PriorityTier == "Tier 4 – Low");

            // Portfolio breakdown
            var portfolioBreakdown = allRequests
                .Where(r => !string.IsNullOrEmpty(r.PortfolioName))
                .GroupBy(r => r.PortfolioName)
                .Select(g => new { Portfolio = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToList();
            ViewBag.PortfolioBreakdown = portfolioBreakdown;

            // Average time to first response (in days)
            var requestsWithResponse = allRequests
                .Where(r => r.SubmittedAt.HasValue && r.ReviewedAt.HasValue)
                .ToList();
            if (requestsWithResponse.Any())
            {
                var avgResponseTime = requestsWithResponse
                    .Average(r => (r.ReviewedAt!.Value - r.SubmittedAt!.Value).TotalDays);
                ViewBag.AverageResponseTimeDays = Math.Round(avgResponseTime, 1);
            }
            else
            {
                ViewBag.AverageResponseTimeDays = 0;
            }

            // Recent requests
            var recentRequests = allRequests
                .OrderByDescending(r => r.CreatedAt)
                .Take(10)
                .ToList();

            return View(recentRequests);
        }

        // ==========================================
        // HELPER METHODS
        // ==========================================

        private string GenerateReferenceNumber()
        {
            var year = DateTime.UtcNow.Year;
            var count = _context.DemandRequests.Count(dr => dr.CreatedAt.Year == year) + 1;
            return $"DR-{year}-{count:D3}";
        }

        private async Task<bool> DemandRequestExists(int id)
        {
            return await _context.DemandRequests.AnyAsync(e => e.Id == id);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConvertToProject(int id, string? returnSection = null)
        {
            if (!IsDemandManagementEnabled())
            {
                return NotFound("Demand Management is not enabled.");
            }

            var demandRequest = await _context.DemandRequests
                .Include(dr => dr.Contacts)
                .FirstOrDefaultAsync(dr => dr.Id == id);

            if (demandRequest == null)
            {
                return NotFound();
            }

            if (demandRequest.ConvertedProjectId.HasValue)
            {
                TempData["ErrorMessage"] = "This request has already been converted to a delivery project.";
                return RedirectToAction(nameof(Details), new { id, s = returnSection });
            }

            var project = new Project
            {
                ProjectCode = demandRequest.ReferenceNumber,
                Title = demandRequest.ProposedTitle,
                Aim = demandRequest.OverviewAndBusinessNeed,
                BusinessArea = demandRequest.BusinessArea,
                MissionPillars = demandRequest.OpportunityMissionPillars,
                StrategicObjectives = demandRequest.DdatStrategicThemes,
                TargetDeliveryDate = demandRequest.TargetDeliveryDate,
                StartDate = DateTime.UtcNow.Date,
                Phase = string.IsNullOrWhiteSpace(demandRequest.CurrentPhase) ? "Delivery" : demandRequest.CurrentPhase,
                RagStatus = "Amber",
                RagJustification = "Converted from demand request",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            if (demandRequest.Contacts.Any())
            {
                var projectContacts = demandRequest.Contacts
                    .Select((contact, index) => new ProjectContact
                    {
                        ProjectId = project.Id,
                        Role = string.IsNullOrWhiteSpace(contact.Role) ? "Point of contact" : contact.Role!,
                        Name = contact.Name,
                        Email = contact.Email,
                        SortOrder = index + 1,
                        FundingArrangement = "Admin",
                        EmploymentType = "Permanent",
                        TeamStatus = "current",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    })
                    .ToList();

                if (projectContacts.Any())
                {
                    _context.ProjectContacts.AddRange(projectContacts);
                    await _context.SaveChangesAsync();
                }
            }

            demandRequest.ConvertedProjectId = project.Id;
            demandRequest.ConvertedToProjectAt = DateTime.UtcNow;
            demandRequest.Status = "Delivery";
            demandRequest.CurrentPhase = "Delivery";
            demandRequest.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Delivery project created from this request.";
            return RedirectToAction(nameof(ProjectController.Details), "Project", new { id = project.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAssignment(int id, string? assignedToName, string? assignedToEmail, string? currentSection, string? currentView, bool clearAssignment = false)
        {
            if (!IsDemandManagementEnabled())
            {
                return NotFound("Demand Management is not enabled.");
            }

            var demandRequest = await _context.DemandRequests.FindAsync(id);
            if (demandRequest == null)
            {
                return NotFound();
            }

            var sectionKey = string.IsNullOrWhiteSpace(currentSection) ? "Overview" : currentSection;
            var viewMode = string.IsNullOrWhiteSpace(currentView) ? "details" : currentView;

            if (clearAssignment)
            {
                demandRequest.AssignedToEmail = null;
                demandRequest.AssignedToName = null;
                demandRequest.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Assignment cleared.";
                return RedirectToAction(nameof(Details), new { id, s = sectionKey, view = viewMode });
            }

            assignedToEmail = assignedToEmail?.Trim();
            assignedToName = assignedToName?.Trim();

            if (string.IsNullOrWhiteSpace(assignedToEmail))
            {
                TempData["ErrorMessage"] = "Enter an email address to assign this request.";
                return RedirectToAction(nameof(Details), new { id, s = sectionKey, view = viewMode });
            }

            demandRequest.AssignedToEmail = assignedToEmail;
            demandRequest.AssignedToName = string.IsNullOrWhiteSpace(assignedToName) ? null : assignedToName;
            demandRequest.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var assignedLabel = demandRequest.AssignedToName ?? demandRequest.AssignedToEmail;
            TempData["SuccessMessage"] = $"Request assigned to {assignedLabel}.";

            return RedirectToAction(nameof(Details), new { id, s = sectionKey, view = viewMode });
        }

        private static void ValidatePrioritisationModel(DemandRequestPrioritisation model, ModelStateDictionary modelState)
        {
            var scoreChecks = new (int Value, string Field, string Description)[]
            {
                (model.StatutoryManifestoScore, nameof(model.StatutoryManifestoScore), "statutory or manifesto requirement"),
                (model.OpportunityMissionScore, nameof(model.OpportunityMissionScore), "Opportunity Mission Pillar support"),
                (model.DdatStrategicThemeScore, nameof(model.DdatStrategicThemeScore), "DDaT Strategic Theme alignment"),
                (model.ScaleOfUsersScore, nameof(model.ScaleOfUsersScore), "scale of users affected"),
                (model.EvidenceOfUserNeedScore, nameof(model.EvidenceOfUserNeedScore), "evidence of user need"),
                (model.RiskIfNotDeliveredScore, nameof(model.RiskIfNotDeliveredScore), "risk if not delivered"),
                (model.TargetDeliveryUrgencyScore, nameof(model.TargetDeliveryUrgencyScore), "target delivery urgency"),
                (model.FundingAvailableScore, nameof(model.FundingAvailableScore), "funding availability"),
                (model.HeadcountAvailableScore, nameof(model.HeadcountAvailableScore), "headcount availability"),
                (model.PortfolioFitScore, nameof(model.PortfolioFitScore), "portfolio fit"),
                (model.ExpectedBenefitsScore, nameof(model.ExpectedBenefitsScore), "expected benefits")
            };

            foreach (var (value, field, description) in scoreChecks)
            {
                if (value < 1 || value > 5)
                {
                    modelState.AddModelError(field, $"Select a score for the {description} question.");
                }
            }
        }

        private static IReadOnlyCollection<string> CollectModelErrors(ModelStateDictionary modelState)
        {
            return modelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage?.Trim())
                .Where(message => !string.IsNullOrWhiteSpace(message))
                .Distinct()
                .ToArray();
        }

        private async Task RecordPrioritisationSectionErrorsAsync(DemandRequest request, IReadOnlyCollection<string> errors)
        {
            if (request == null || errors == null || errors.Count == 0)
            {
                return;
            }

            try
            {
                var completion = EnsureSectionCompletion(request, "PrioritisationAssessment");
                completion.LatestErrorMessage = string.Join(" ", errors);
                completion.UpdatedAt = DateTime.UtcNow;
                request.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to record prioritisation errors for demand request {DemandRequestId}", request?.Id);
            }
        }

        private static void PopulatePrioritisationForDisplay(DemandRequest request, DemandRequestPrioritisation source)
        {
            if (request.Prioritisation == null)
            {
                request.Prioritisation = new DemandRequestPrioritisation
                {
                    DemandRequestId = request.Id
                };
            }

            var target = request.Prioritisation;
            target.StatutoryManifestoScore = source.StatutoryManifestoScore;
            target.OpportunityMissionScore = source.OpportunityMissionScore;
            target.DdatStrategicThemeScore = source.DdatStrategicThemeScore;
            target.ScaleOfUsersScore = source.ScaleOfUsersScore;
            target.EvidenceOfUserNeedScore = source.EvidenceOfUserNeedScore;
            target.RiskIfNotDeliveredScore = source.RiskIfNotDeliveredScore;
            target.TargetDeliveryUrgencyScore = source.TargetDeliveryUrgencyScore;
            target.FundingAvailableScore = source.FundingAvailableScore;
            target.HeadcountAvailableScore = source.HeadcountAvailableScore;
            target.PortfolioFitScore = source.PortfolioFitScore;
            target.ExpectedBenefitsScore = source.ExpectedBenefitsScore;
            target.StrategicAlignmentTotal = source.StrategicAlignmentTotal;
            target.UserImpactTotal = source.UserImpactTotal;
            target.RiskUrgencyTotal = source.RiskUrgencyTotal;
            target.FeasibilityTotal = source.FeasibilityTotal;
            target.ValueOutcomeTotal = source.ValueOutcomeTotal;
            target.TotalPriorityScore = source.TotalPriorityScore;
            target.PriorityTier = string.IsNullOrWhiteSpace(source.PriorityTier) ? target.PriorityTier : source.PriorityTier;
            target.ScoringNotes = source.ScoringNotes;
        }
    }
}

