using Compass.Models.DemandTriage;
using Compass.Services;
using Compass.Services.DemandTriage;
using Compass.ViewModels.DemandTriage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Compass.Controllers;

/// <summary>
/// Demand Triage controller — spec-aligned v3.
/// Role model:
///   authenticated_user  → create/edit own drafts, submit, view own
///   demand_management   → exploratory review, scoring, view all
///   central_operations_admin → everything including triage outcome, admin override
/// Feature flag: FeatureFlags:EnableDemandManagement
/// </summary>
[Authorize]
public class DemandTriageController : Controller
{
    private readonly IDemandTriageService _service;
    private readonly IPermissionService _permissionService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DemandTriageController> _logger;

    public DemandTriageController(
        IDemandTriageService service,
        IPermissionService permissionService,
        IConfiguration configuration,
        ILogger<DemandTriageController> logger)
    {
        _service = service;
        _permissionService = permissionService;
        _configuration = configuration;
        _logger = logger;
    }

    // ── Feature flag guard ───────────────────────────────────────────────────

    private bool IsEnabled() =>
        _configuration.GetValue<bool>("FeatureFlags:EnableDemandManagement", false);

    private IActionResult FeatureDisabled() =>
        NotFound("Demand triage is not currently enabled.");

    // ── Role helpers ─────────────────────────────────────────────────────────

    private string CurrentUserEmail =>
        User.Identity?.Name
        ?? User.FindFirst(ClaimTypes.Email)?.Value
        ?? User.FindFirst("preferred_username")?.Value
        ?? string.Empty;

    private string CurrentUserName =>
        User.FindFirst("name")?.Value
        ?? User.FindFirst(ClaimTypes.Name)?.Value
        ?? CurrentUserEmail;

    private async Task<bool> IsCentralOpsAdminAsync()
    {
        var email = CurrentUserEmail;
        if (string.IsNullOrWhiteSpace(email)) return false;
        try
        {
            return await _permissionService.IsSuperAdminAsync(email) ||
                   await _permissionService.IsInGroupAsync(email, "Central Operations Admin");
        }
        catch { return false; }
    }

    private async Task<bool> IsDemandManagementAsync()
    {
        var email = CurrentUserEmail;
        if (string.IsNullOrWhiteSpace(email)) return false;
        try
        {
            if (await _permissionService.IsSuperAdminAsync(email)) return true;
            foreach (var group in new[] { "Central Operations Admin", "Demand Management", "Demand Triage", "HOP" })
            {
                if (await _permissionService.IsInGroupAsync(email, group)) return true;
            }
            return false;
        }
        catch { return false; }
    }

    private async Task SetViewBagRolesAsync()
    {
        var isCoa = await IsCentralOpsAdminAsync();
        var isDm = isCoa || await IsDemandManagementAsync();
        ViewBag.IsCentralOpsAdmin = isCoa;
        ViewBag.IsDemandManagement = isDm;
        ViewBag.CurrentUserEmail = CurrentUserEmail;
    }

    // ── Compute allowed actions for a request ────────────────────────────────

    private DemandTriageDetailViewModel BuildDetailViewModel(
        DemandTriageRequest request, bool isDm, bool isCoa)
    {
        var email = CurrentUserEmail;
        var isOwner = string.Equals(request.OwnerUserEmail, email, StringComparison.OrdinalIgnoreCase);
        var status = request.Status;

        return new DemandTriageDetailViewModel
        {
            Request = request,
            IsDemandManagement = isDm,
            IsCentralOpsAdmin = isCoa,
            IsOwner = isOwner,
            CurrentUserEmail = email,

            CanEdit = (isOwner || isCoa) &&
                      (status == DemandTriageStatus.Draft || status == DemandTriageStatus.ReturnedForClarification),

            CanSubmit = (isOwner || isCoa) &&
                        (status == DemandTriageStatus.Draft || status == DemandTriageStatus.ReturnedForClarification),

            CanStartExploratoryReview = isDm && status == DemandTriageStatus.Submitted,

            CanCompleteExploratoryReview = isDm && status == DemandTriageStatus.ExploratoryReviewInProgress,

            CanReturnForClarification = isDm &&
                (status == DemandTriageStatus.ExploratoryReviewInProgress ||
                 status == DemandTriageStatus.ScoringInProgress),

            CanStartScoring = isDm && status == DemandTriageStatus.ExploratoryReviewComplete,

            CanFinaliseScorecard = isDm && status == DemandTriageStatus.ScoringInProgress,

            CanSendToTriage = isCoa && status == DemandTriageStatus.ScoredFinalised,

            CanRecordOutcome = isCoa && status == DemandTriageStatus.TriagePending,

            CanClose = isCoa && status == DemandTriageStatus.Triaged,

            CanAdminOverride = isCoa,

            CanUnlockScorecard = isCoa && request.Scorecard?.IsLocked == true,

            CanCreateProject = isCoa && status == DemandTriageStatus.Triaged &&
                               !request.ConvertedProjectId.HasValue
        };
    }

    // ════════════════════════════════════════════════════════════════════════
    // REGISTER
    // ════════════════════════════════════════════════════════════════════════

    [HttpGet]
    public async Task<IActionResult> Index(string? status)
    {
        if (!IsEnabled()) return FeatureDisabled();

        await SetViewBagRolesAsync();
        var isCoa = (bool)ViewBag.IsCentralOpsAdmin;
        var isDm = (bool)ViewBag.IsDemandManagement;
        var email = CurrentUserEmail;

        // Non-privileged users only see their own requests
        var requests = isDm
            ? await _service.GetAllAsync(status)
            : await _service.GetAllAsync(status, email);

        var vm = new DemandTriageRegisterViewModel
        {
            Requests = requests,
            StatusFilter = status,
            IsDemandManagement = isDm,
            IsCentralOpsAdmin = isCoa,
            CurrentUserEmail = email
        };

        return View(vm);
    }

    // ════════════════════════════════════════════════════════════════════════
    // DETAIL
    // ════════════════════════════════════════════════════════════════════════

    [HttpGet]
    public async Task<IActionResult> Detail(int id)
    {
        if (!IsEnabled()) return FeatureDisabled();

        await SetViewBagRolesAsync();
        var isCoa = (bool)ViewBag.IsCentralOpsAdmin;
        var isDm = (bool)ViewBag.IsDemandManagement;

        var request = await _service.GetByIdAsync(id);
        if (request == null) return NotFound();

        // Non-privileged users can only view their own requests
        if (!isDm && !string.Equals(request.OwnerUserEmail, CurrentUserEmail, StringComparison.OrdinalIgnoreCase))
            return Forbid();

        var vm = BuildDetailViewModel(request, isDm, isCoa);
        return View(vm);
    }

    // ════════════════════════════════════════════════════════════════════════
    // CREATE DRAFT
    // ════════════════════════════════════════════════════════════════════════

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        if (!IsEnabled()) return FeatureDisabled();

        var draft = await _service.CreateDraftAsync(CurrentUserEmail, CurrentUserName);
        return RedirectToAction(nameof(Edit), new { id = draft.Id });
    }

    // ════════════════════════════════════════════════════════════════════════
    // EDIT CAPTURE FORM
    // ════════════════════════════════════════════════════════════════════════

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        if (!IsEnabled()) return FeatureDisabled();

        await SetViewBagRolesAsync();
        var isCoa = (bool)ViewBag.IsCentralOpsAdmin;

        var request = await _service.GetByIdAsync(id, includeAll: false);
        if (request == null) return NotFound();

        if (!isCoa && !string.Equals(request.OwnerUserEmail, CurrentUserEmail, StringComparison.OrdinalIgnoreCase))
            return Forbid();

        if (request.Status != DemandTriageStatus.Draft &&
            request.Status != DemandTriageStatus.ReturnedForClarification)
            return RedirectToAction(nameof(Detail), new { id });

        var vm = MapToEditViewModel(request);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, DemandCaptureFormViewModel vm)
    {
        if (!IsEnabled()) return FeatureDisabled();

        await SetViewBagRolesAsync();
        var isCoa = (bool)ViewBag.IsCentralOpsAdmin;

        var request = await _service.GetByIdAsync(id, includeAll: false);
        if (request == null) return NotFound();

        if (!isCoa && !string.Equals(request.OwnerUserEmail, CurrentUserEmail, StringComparison.OrdinalIgnoreCase))
            return Forbid();

        try
        {
            var updates = MapFromEditViewModel(vm);
            await _service.UpdateCaptureFormAsync(id, updates, CurrentUserEmail, CurrentUserName);
            TempData["SuccessMessage"] = "Request saved.";
            return RedirectToAction(nameof(Detail), new { id });
        }
        catch (InvalidOperationException ex)
        {
            vm.ValidationErrors.Add(ex.Message);
            return View(vm);
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    // SUBMIT
    // ════════════════════════════════════════════════════════════════════════

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(int id)
    {
        if (!IsEnabled()) return FeatureDisabled();

        await SetViewBagRolesAsync();
        var isCoa = (bool)ViewBag.IsCentralOpsAdmin;

        var request = await _service.GetByIdAsync(id, includeAll: false);
        if (request == null) return NotFound();

        if (!isCoa && !string.Equals(request.OwnerUserEmail, CurrentUserEmail, StringComparison.OrdinalIgnoreCase))
            return Forbid();

        try
        {
            await _service.SubmitAsync(id, CurrentUserEmail, CurrentUserName);
            TempData["SuccessMessage"] = "Request submitted successfully.";
        }
        catch (ValidationException ex)
        {
            TempData["ErrorMessages"] = string.Join("|", ex.Errors);
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Detail), new { id });
    }

    // ════════════════════════════════════════════════════════════════════════
    // EXPLORATORY REVIEW
    // ════════════════════════════════════════════════════════════════════════

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StartExploratoryReview(int id)
    {
        if (!IsEnabled()) return FeatureDisabled();
        if (!await IsDemandManagementAsync()) return Forbid();

        try
        {
            await _service.StartExploratoryReviewAsync(id, CurrentUserEmail, CurrentUserName);
            TempData["SuccessMessage"] = "Exploratory review started.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(ExploratoryReview), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> ExploratoryReview(int id)
    {
        if (!IsEnabled()) return FeatureDisabled();
        if (!await IsDemandManagementAsync()) return Forbid();

        await SetViewBagRolesAsync();
        var request = await _service.GetByIdAsync(id);
        if (request == null) return NotFound();

        var review = request.ExploratoryReview;
        var vm = new ExploratoryReviewViewModel
        {
            DemandTriageRequestId = id,
            RequestReference = request.RequestReference,
            RequestName = request.RequestName ?? request.ProposedRequestTitle ?? string.Empty,
            SummaryFindings = review?.SummaryFindings,
            KeyRisks = review?.KeyRisks,
            Dependencies = review?.Dependencies,
            RecommendationToProceed = review?.RecommendationToProceed,
            ReasonNotProceeding = review?.ReasonNotProceeding,
            IsCompleted = review?.CompletedAt != null
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveExploratoryReview(int id, ExploratoryReviewViewModel vm)
    {
        if (!IsEnabled()) return FeatureDisabled();
        if (!await IsDemandManagementAsync()) return Forbid();

        try
        {
            var data = new DemandExploratoryReview
            {
                SummaryFindings = vm.SummaryFindings,
                KeyRisks = vm.KeyRisks,
                Dependencies = vm.Dependencies,
                RecommendationToProceed = vm.RecommendationToProceed,
                ReasonNotProceeding = vm.ReasonNotProceeding
            };
            await _service.SaveExploratoryReviewAsync(id, data, CurrentUserEmail, CurrentUserName);
            TempData["SuccessMessage"] = "Exploratory review saved.";
        }
        catch (Exception ex)
        {
            vm.DemandTriageRequestId = id;
            vm.ValidationErrors.Add(ex.Message);
            return View(nameof(ExploratoryReview), vm);
        }

        return RedirectToAction(nameof(ExploratoryReview), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompleteExploratoryReview(int id)
    {
        if (!IsEnabled()) return FeatureDisabled();
        if (!await IsDemandManagementAsync()) return Forbid();

        try
        {
            await _service.CompleteExploratoryReviewAsync(id, CurrentUserEmail, CurrentUserName);
            TempData["SuccessMessage"] = "Exploratory review completed.";
        }
        catch (ValidationException ex)
        {
            TempData["ErrorMessages"] = string.Join("|", ex.Errors);
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Detail), new { id });
    }

    // ════════════════════════════════════════════════════════════════════════
    // RETURN FOR CLARIFICATION
    // ════════════════════════════════════════════════════════════════════════

    [HttpGet]
    public async Task<IActionResult> ReturnForClarification(int id)
    {
        if (!IsEnabled()) return FeatureDisabled();
        if (!await IsDemandManagementAsync()) return Forbid();

        var request = await _service.GetByIdAsync(id, includeAll: false);
        if (request == null) return NotFound();

        var vm = new ReturnForClarificationViewModel
        {
            DemandTriageRequestId = id,
            RequestReference = request.RequestReference,
            RequestName = request.RequestName ?? request.ProposedRequestTitle ?? string.Empty
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReturnForClarification(int id, ReturnForClarificationViewModel vm)
    {
        if (!IsEnabled()) return FeatureDisabled();
        if (!await IsDemandManagementAsync()) return Forbid();

        if (!ModelState.IsValid)
            return View(vm);

        try
        {
            await _service.ReturnForClarificationAsync(id, vm.Reason!, CurrentUserEmail, CurrentUserName);
            TempData["SuccessMessage"] = "Request returned for clarification.";
            return RedirectToAction(nameof(Detail), new { id });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(vm);
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    // SCORING
    // ════════════════════════════════════════════════════════════════════════

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StartScoring(int id)
    {
        if (!IsEnabled()) return FeatureDisabled();
        if (!await IsDemandManagementAsync()) return Forbid();

        try
        {
            await _service.StartScoringAsync(id, CurrentUserEmail, CurrentUserName);
            TempData["SuccessMessage"] = "Scoring started.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Score), new { id, section = 1 });
    }

    [HttpGet]
    public async Task<IActionResult> Score(int id, int section = 1)
    {
        if (!IsEnabled()) return FeatureDisabled();
        if (!await IsDemandManagementAsync()) return Forbid();

        await SetViewBagRolesAsync();
        var request = await _service.GetByIdAsync(id);
        if (request == null) return NotFound();

        if (request.Status != DemandTriageStatus.ScoringInProgress)
            return RedirectToAction(nameof(Detail), new { id });

        var scorecard = request.Scorecard;
        var answers = scorecard?.Answers.ToList() ?? new List<DemandAnswer>();

        var vm = new ScoringWizardViewModel
        {
            DemandTriageRequestId = id,
            RequestReference = request.RequestReference,
            RequestName = request.RequestName ?? request.ProposedRequestTitle ?? string.Empty,
            Section = Math.Clamp(section, 1, 4),
            ScorecardIsLocked = scorecard?.IsLocked ?? false,
            StrategicAlignmentScore = scorecard?.StrategicAlignmentScore ?? 0,
            UrgencyScore = scorecard?.UrgencyScore ?? 0,
            FundingScore = scorecard?.FundingScore ?? 0,
            RiceScore = scorecard?.RiceScore ?? 0,
            TotalScore = scorecard?.TotalScore ?? 0,
            SuggestionBand = scorecard?.SuggestionBand
        };

        // Populate existing answers
        foreach (var answer in answers)
        {
            if (!string.IsNullOrWhiteSpace(answer.AnswerValue))
            {
                if (vm.Answers.ContainsKey(answer.QuestionCode))
                    vm.Answers[answer.QuestionCode] += "," + answer.AnswerValue;
                else
                    vm.Answers[answer.QuestionCode] = answer.AnswerValue;
            }
            if (!string.IsNullOrWhiteSpace(answer.FreeText))
                vm.FreeTexts[answer.QuestionCode] = answer.FreeText;
        }

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveSection(int id, int section, ScoringWizardViewModel vm)
    {
        if (!IsEnabled()) return FeatureDisabled();
        if (!await IsDemandManagementAsync()) return Forbid();

        // Build answer inputs from posted form
        var inputs = BuildAnswerInputs(vm, section);

        try
        {
            await _service.SaveAnswersAsync(id, inputs, CurrentUserEmail, CurrentUserName);

            // Navigate to next section or back to detail
            if (section < 4)
                return RedirectToAction(nameof(Score), new { id, section = section + 1 });

            TempData["SuccessMessage"] = "Scoring answers saved. Review and finalise when ready.";
            return RedirectToAction(nameof(Detail), new { id });
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Score), new { id, section });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FinaliseScorecard(int id)
    {
        if (!IsEnabled()) return FeatureDisabled();
        if (!await IsDemandManagementAsync()) return Forbid();

        try
        {
            await _service.FinaliseScorecard(id, CurrentUserEmail, CurrentUserName);
            TempData["SuccessMessage"] = "Scorecard finalised and locked.";
        }
        catch (ValidationException ex)
        {
            TempData["ErrorMessages"] = string.Join("|", ex.Errors);
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnlockScorecard(int id)
    {
        if (!IsEnabled()) return FeatureDisabled();
        if (!await IsCentralOpsAdminAsync()) return Forbid();

        try
        {
            await _service.UnlockScorecardAsync(id, CurrentUserEmail, CurrentUserName);
            TempData["SuccessMessage"] = "Scorecard unlocked.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Detail), new { id });
    }

    // ════════════════════════════════════════════════════════════════════════
    // TRIAGE
    // ════════════════════════════════════════════════════════════════════════

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendToTriage(int id)
    {
        if (!IsEnabled()) return FeatureDisabled();
        if (!await IsCentralOpsAdminAsync()) return Forbid();

        try
        {
            await _service.SendToTriageAsync(id, CurrentUserEmail, CurrentUserName);
            TempData["SuccessMessage"] = "Request sent to triage.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> RecordOutcome(int id)
    {
        if (!IsEnabled()) return FeatureDisabled();
        if (!await IsCentralOpsAdminAsync()) return Forbid();

        await SetViewBagRolesAsync();
        var request = await _service.GetByIdAsync(id);
        if (request == null) return NotFound();

        var scorecard = request.Scorecard;
        var existing = request.TriageOutcome;

        var vm = new TriageOutcomeViewModel
        {
            DemandTriageRequestId = id,
            RequestReference = request.RequestReference,
            RequestName = request.RequestName ?? request.ProposedRequestTitle ?? string.Empty,
            TotalScore = scorecard?.TotalScore ?? 0,
            StrategicAlignmentScore = scorecard?.StrategicAlignmentScore ?? 0,
            UrgencyScore = scorecard?.UrgencyScore ?? 0,
            FundingScore = scorecard?.FundingScore ?? 0,
            RiceScore = scorecard?.RiceScore ?? 0,
            StrategicAlignmentBand = scorecard?.StrategicAlignmentBand,
            UrgencyBand = scorecard?.UrgencyBand,
            FundingBand = scorecard?.FundingBand,
            RiceBand = scorecard?.RiceBand,
            SuggestionBand = scorecard?.SuggestionBand,
            OutcomeSelection = existing?.OutcomeSelection,
            OutcomeSummary = existing?.OutcomeSummary,
            RoutedToArea = existing?.RoutedToArea,
            OverrideReason = existing?.OverrideReason
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RecordOutcome(int id, TriageOutcomeViewModel vm)
    {
        if (!IsEnabled()) return FeatureDisabled();
        if (!await IsCentralOpsAdminAsync()) return Forbid();

        try
        {
            var outcome = new DemandTriageOutcome
            {
                OutcomeSelection = vm.OutcomeSelection ?? string.Empty,
                OutcomeSummary = vm.OutcomeSummary,
                RoutedToArea = vm.RoutedToArea,
                OverrideReason = vm.OverrideReason
            };
            await _service.RecordTriageOutcomeAsync(id, outcome, CurrentUserEmail, CurrentUserName);
            TempData["SuccessMessage"] = "Triage outcome recorded.";
            return RedirectToAction(nameof(Detail), new { id });
        }
        catch (ValidationException ex)
        {
            vm.ValidationErrors.AddRange(ex.Errors);
            return View(vm);
        }
        catch (Exception ex)
        {
            vm.ValidationErrors.Add(ex.Message);
            return View(vm);
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    // CLOSE
    // ════════════════════════════════════════════════════════════════════════

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Close(int id)
    {
        if (!IsEnabled()) return FeatureDisabled();
        if (!await IsCentralOpsAdminAsync()) return Forbid();

        try
        {
            await _service.CloseAsync(id, CurrentUserEmail, CurrentUserName);
            TempData["SuccessMessage"] = "Request closed.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Detail), new { id });
    }

    // ════════════════════════════════════════════════════════════════════════
    // ADMIN OVERRIDE
    // ════════════════════════════════════════════════════════════════════════

    [HttpGet]
    public async Task<IActionResult> AdminOverride(int id)
    {
        if (!IsEnabled()) return FeatureDisabled();
        if (!await IsCentralOpsAdminAsync()) return Forbid();

        var request = await _service.GetByIdAsync(id, includeAll: false);
        if (request == null) return NotFound();

        var vm = new AdminOverrideViewModel
        {
            DemandTriageRequestId = id,
            RequestReference = request.RequestReference,
            CurrentStatus = request.Status
        };

        ViewBag.AllStatuses = DemandTriageStatus.All;
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AdminOverride(int id, AdminOverrideViewModel vm)
    {
        if (!IsEnabled()) return FeatureDisabled();
        if (!await IsCentralOpsAdminAsync()) return Forbid();

        if (!ModelState.IsValid)
        {
            ViewBag.AllStatuses = DemandTriageStatus.All;
            return View(vm);
        }

        try
        {
            await _service.AdminOverrideStatusAsync(id, vm.NewStatus!, vm.Reason!, CurrentUserEmail, CurrentUserName);
            TempData["SuccessMessage"] = $"Status overridden to '{DemandTriageStatus.DisplayName(vm.NewStatus!)}'.";
            return RedirectToAction(nameof(Detail), new { id });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            ViewBag.AllStatuses = DemandTriageStatus.All;
            return View(vm);
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    // CREATE PROJECT
    // ════════════════════════════════════════════════════════════════════════

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateProject(int id)
    {
        if (!IsEnabled()) return FeatureDisabled();
        if (!await IsCentralOpsAdminAsync()) return Forbid();

        try
        {
            var projectId = await _service.CreateProjectFromDemandAsync(id, CurrentUserEmail, CurrentUserName);
            TempData["SuccessMessage"] = $"Project created (ID: {projectId}).";
            return RedirectToAction("Details", "Project", new { id = projectId });
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Detail), new { id });
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    // PRIVATE HELPERS
    // ════════════════════════════════════════════════════════════════════════

    private static DemandCaptureFormViewModel MapToEditViewModel(DemandTriageRequest r) => new()
    {
        Id = r.Id,
        RequestReference = r.RequestReference,
        Status = r.Status,
        RequestName = r.RequestName,
        RequesterFullName = r.RequesterFullName,
        DepartmentGroup = r.DepartmentGroup,
        DdtPortfolioSupport = r.DdtPortfolioSupport,
        PointsOfContact = r.PointsOfContact,
        SroName = r.SroName,
        ProposedRequestTitle = r.ProposedRequestTitle,
        RequestOverview = r.RequestOverview,
        PreviousResearch = r.PreviousResearch,
        ManifestoOrStatute = r.ManifestoOrStatute,
        SosOpportunityMissionPillars = r.SosOpportunityMissionPillars,
        DdtStrategicTheme = r.DdtStrategicTheme,
        ExpectedBenefits = r.ExpectedBenefits,
        RiskConsequence = r.RiskConsequence,
        FundingProvided = r.FundingProvided,
        HeadcountProvided = r.HeadcountProvided,
        HeadcountDetails = r.HeadcountDetails,
        TargetDeliveryDate = r.TargetDeliveryDate,
        NewOrChangedDigitalService = r.NewOrChangedDigitalService,
        NewOrChangedServiceDetails = r.NewOrChangedServiceDetails,
        PublicFacingDigitalService = r.PublicFacingDigitalService,
        BusinessCaseId = r.BusinessCaseId
    };

    private static DemandTriageRequest MapFromEditViewModel(DemandCaptureFormViewModel vm) => new()
    {
        RequestName = vm.RequestName,
        RequesterFullName = vm.RequesterFullName,
        DepartmentGroup = vm.DepartmentGroup,
        DdtPortfolioSupport = vm.DdtPortfolioSupport,
        PointsOfContact = vm.PointsOfContact,
        SroName = vm.SroName,
        ProposedRequestTitle = vm.ProposedRequestTitle,
        RequestOverview = vm.RequestOverview,
        PreviousResearch = vm.PreviousResearch,
        ManifestoOrStatute = vm.ManifestoOrStatute,
        SosOpportunityMissionPillars = vm.SosOpportunityMissionPillars,
        DdtStrategicTheme = vm.DdtStrategicTheme,
        ExpectedBenefits = vm.ExpectedBenefits,
        RiskConsequence = vm.RiskConsequence,
        FundingProvided = vm.FundingProvided,
        HeadcountProvided = vm.HeadcountProvided,
        HeadcountDetails = vm.HeadcountDetails,
        TargetDeliveryDate = vm.TargetDeliveryDate,
        NewOrChangedDigitalService = vm.NewOrChangedDigitalService,
        NewOrChangedServiceDetails = vm.NewOrChangedServiceDetails,
        PublicFacingDigitalService = vm.PublicFacingDigitalService,
        BusinessCaseId = vm.BusinessCaseId
    };

    private static List<AnswerInput> BuildAnswerInputs(ScoringWizardViewModel vm, int section)
    {
        var inputs = new List<AnswerInput>();

        // Question codes per section
        var sectionCodes = section switch
        {
            1 => new[] { "1.1", "1.2", "1.3", "1.4", "1.5", "1.6" },
            2 => new[] { "2.1", "2.2", "2.3", "2.4", "2.5" },
            3 => new[] { "3.1", "3.2", "3.3", "3.4", "3.5", "3.6" },
            4 => new[] { "4.1", "4.2", "4.3", "4.4", "4.5", "4.6", "4.7", "4.8", "4.9", "4.10", "4.11" },
            _ => Array.Empty<string>()
        };

        var multiSelectCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "4.2", "4.3" };
        var freeTextCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "1.1", "1.5", "1.6", "2.2", "2.4", "2.5", "3.4", "4.5", "4.10", "4.11" };

        foreach (var code in sectionCodes)
        {
            if (freeTextCodes.Contains(code))
            {
                if (vm.FreeTexts.TryGetValue(code, out var ft) && !string.IsNullOrWhiteSpace(ft))
                    inputs.Add(new AnswerInput { QuestionCode = code, FreeText = ft });
            }
            else if (multiSelectCodes.Contains(code))
            {
                if (vm.Answers.TryGetValue(code, out var multi) && !string.IsNullOrWhiteSpace(multi))
                {
                    foreach (var val in multi.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                        inputs.Add(new AnswerInput { QuestionCode = code, AnswerValue = val });
                }
            }
            else
            {
                if (vm.Answers.TryGetValue(code, out var single) && !string.IsNullOrWhiteSpace(single))
                    inputs.Add(new AnswerInput { QuestionCode = code, AnswerValue = single });
            }
        }

        return inputs;
    }
}
