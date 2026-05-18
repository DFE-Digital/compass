using Compass.Data;
using Compass.Filters;
using Compass.Helpers;
using Compass.Models;
using Compass.Security;
using Compass.Services;
using Compass.Services.DdtStandards;
using Compass.Services.FunctionalStandards;
using Compass.ViewModels.Modern;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Security.Claims;
using System.Text;

namespace Compass.Controllers.Modern;

/// <summary>Modern standards UI at <c>/modern/standards/*</c> — DDT standards register, functional standards, and management.</summary>
[Authorize]
[ServiceFilter(typeof(StandardsFeatureGateFilter))]
[Route("modern/standards")]
public partial class ModernStandardsController : Controller
{
    private readonly CompassDbContext _context;
    private readonly IPermissionService _permissions;
    private readonly IDdtStandardsWorkflowService _ddtWorkflow;

    public ModernStandardsController(
        CompassDbContext context,
        IPermissionService permissions,
        IDdtStandardsWorkflowService ddtWorkflow)
    {
        _context = context;
        _permissions = permissions;
        _ddtWorkflow = ddtWorkflow;
    }

    private void SetChrome(string subNavItem)
    {
        ViewBag.MainNavSection = "standards";
        ViewBag.SubNavItem = subNavItem;
    }

    private string GetUserEmail() =>
        User.FindFirstValue(ClaimTypes.Email)
        ?? User.FindFirstValue("preferred_username")
        ?? User.Identity?.Name
        ?? "";

    private int? GetCurrentUserId()
    {
        var objectIdClaim = User.FindFirstValue(CompassClaimTypes.ObjectIdentifier);
        if (Guid.TryParse(objectIdClaim, out var objectId))
        {
            var user = _context.Users.AsNoTracking()
                .FirstOrDefault(u => u.AzureObjectId == objectId.ToString());
            return user?.Id;
        }

        return null;
    }

    [HttpGet("")]
    public IActionResult Index() => RedirectToAction(nameof(Dashboard));

    #region Dashboard

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard()
    {
        SetChrome("standards-dashboard");

        var allPublishedDdt = await _context.DdtStandards.AsNoTracking()
            .Where(s => !s.IsDeleted && s.IsPublished && s.Stage == "Published")
            .Select(s => new DdtStandard { Id = s.Id, ParentStandardId = s.ParentStandardId, PublishedAt = s.PublishedAt, UpdatedAt = s.UpdatedAt, IsPublished = s.IsPublished, Stage = s.Stage })
            .ToListAsync();
        var publishedDdtCount = DdtStandardsListingHelper.LatestPublishedOnly(allPublishedDdt).Count();

        var functionalStandardsCount = await _context.FunctionalStandards.AsNoTracking()
            .CountAsync();

        var inProgressAssessments = await _context.FunctionalStandardAssessments.AsNoTracking()
            .Where(a => a.SubmittedAt == null)
            .CountAsync();

        var ytdStart = new DateTime(DateTime.UtcNow.Year, 1, 1);
        var completeAssessmentsYtd = await _context.FunctionalStandardAssessments.AsNoTracking()
            .Where(a => a.SubmittedAt != null && a.SubmittedAt >= ytdStart)
            .CountAsync();

        var cultureGb = CultureInfo.GetCultureInfo("en-GB");
        var recentPublishedAll = await _context.DdtStandards.AsNoTracking()
            .Where(s => !s.IsDeleted && s.IsPublished && s.Stage == "Published")
            .ToListAsync();
        var recentDdtStandards = DdtStandardsListingHelper.LatestPublishedOnly(recentPublishedAll)
            .OrderByDescending(s => s.FirstPublished ?? s.PublishedAt ?? s.UpdatedAt)
            .Take(6)
            .ToList();

        var ddtRows = recentDdtStandards.Select(s => new StandardsDashboardDdtRow
        {
            StandardId = s.Id,
            Title = s.Title,
            VersionDisplay = $"v{s.Version}",
            PublishedDisplay = (s.FirstPublished ?? s.PublishedAt)?.ToString("d MMMM yyyy", cultureGb) ?? "—"
        }).ToList();

        var recentAssessments = await _context.FunctionalStandardAssessments.AsNoTracking()
            .Include(a => a.FunctionalStandard)
            .Include(a => a.CriteriaResponses)
            .Where(a => a.SubmittedAt == null)
            .OrderByDescending(a => a.AssessmentDate)
            .Take(8)
            .ToListAsync();

        var assessmentRows = recentAssessments.Select(a =>
        {
            var submitted = a.SubmittedAt != null;
            var total = a.CriteriaResponses.Count;
            var answered = a.CriteriaResponses.Count(r => r.Attainment != null);

            string? attainmentTagClass = null;
            string? attainmentLabel = null;
            if (submitted && total > 0)
            {
                var fullyMet = a.CriteriaResponses.Count(r => r.Attainment == AttainmentLevel.FullyMet);
                var pct = (double)fullyMet / total * 100;
                (attainmentTagClass, attainmentLabel) = AttainmentBadge(pct);
            }

            return new StandardsDashboardAssessmentRow
            {
                Id = a.Id,
                FunctionalStandardId = a.FunctionalStandardId,
                IsSubmitted = submitted,
                AssessmentTitle = a.AssessmentName,
                Reference = $"FSA-{a.Id:D4}",
                StandardRef = a.FunctionalStandard?.Title ?? $"FS-{a.FunctionalStandardId}",
                StatusTagClass = submitted ? "dfe-f-badge--green" : "dfe-f-badge--blue",
                StatusLabel = submitted ? "Complete" : "In progress",
                AttainmentTagClass = attainmentTagClass,
                AttainmentLabel = attainmentLabel
            };
        }).ToList();

        string? reviewCallout = null;
        var reviewCount = await _context.DdtStandards.AsNoTracking()
            .Where(s => !s.IsDeleted && s.Stage == "Under Review")
            .CountAsync();
        if (reviewCount > 0)
            reviewCallout = $"{reviewCount} standard{(reviewCount == 1 ? "" : "s")} awaiting review";

        var vm = new StandardsDashboardViewModel
        {
            PublishedDdtCount = publishedDdtCount,
            FunctionalStandardsCount = functionalStandardsCount,
            InProgressAssessmentsCount = inProgressAssessments,
            CompleteAssessmentsYtdCount = completeAssessmentsYtd,
            ReviewCalloutHeading = reviewCallout,
            DdtRows = ddtRows,
            AssessmentRows = assessmentRows
        };

        return View("~/Views/Modern/Standards/Dashboard.cshtml", vm);
    }

    #endregion

    #region DDT Standards

    [HttpGet("ddt")]
    public async Task<IActionResult> DdtStandards(
        string? tab = null,
        string? search = null,
        string? category = null,
        int? owner = null,
        string? export = null)
    {
        SetChrome("standards-ddt");

        var query = _context.DdtStandards.AsNoTracking()
            .Include(s => s.Categories).ThenInclude(c => c.Category)
            .Include(s => s.Owners).ThenInclude(o => o.User)
            .Include(s => s.Contacts).ThenInclude(c => c.User)
            .Where(s => !s.IsDeleted);

        var allNonDeleted = await query.ToListAsync();

        int? currentUserId = GetCurrentUserId();
        var yourStandardsCount = 0;
        if (currentUserId is int userId && userId > 0)
        {
            yourStandardsCount = DdtStandardsListingHelper.YourPublishedOnly(allNonDeleted, userId).Count();
        }

        var showYourStandardsTab = yourStandardsCount > 0;
        var tabLower = string.IsNullOrWhiteSpace(tab)
            ? (showYourStandardsTab ? "yours" : "published")
            : tab.Trim().ToLowerInvariant();

        var publishedCount = DdtStandardsListingHelper.LatestPublishedOnly(allNonDeleted).Count();
        var draftCount = DdtStandardsListingHelper.ActiveDraftsOnly(allNonDeleted).Count();
        var reviewCount = allNonDeleted.Count(s => s.Stage == "For Approval");
        var awaitingPublishCount = allNonDeleted.Count(s => s.Stage == "Awaiting Publication");
        var withdrawnCount = allNonDeleted.Count(s => !s.IsPublished && s.Stage == "Archived");

        IEnumerable<DdtStandard> filtered = tabLower switch
        {
            "yours" when currentUserId is int uid && uid > 0 =>
                DdtStandardsListingHelper.YourPublishedOnly(allNonDeleted, uid),
            "draft" => DdtStandardsListingHelper.ActiveDraftsOnly(allNonDeleted),
            "review" => allNonDeleted.Where(s => s.Stage == "For Approval"),
            "awaiting" => allNonDeleted.Where(s => s.Stage == "Awaiting Publication"),
            "withdrawn" => allNonDeleted.Where(s => !s.IsPublished && s.Stage == "Archived"),
            "published" => DdtStandardsListingHelper.LatestPublishedOnly(allNonDeleted),
            _ => DdtStandardsListingHelper.LatestPublishedOnly(allNonDeleted)
        };

        if (tabLower == "yours" && !showYourStandardsTab)
            tabLower = "published";

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            filtered = filtered.Where(s =>
                s.Title.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                (s.Summary != null && s.Summary.Contains(term, StringComparison.OrdinalIgnoreCase)));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            filtered = filtered.Where(s =>
                s.Categories.Any(c => c.Category.Name.Equals(category, StringComparison.OrdinalIgnoreCase)));
        }

        if (owner.HasValue)
        {
            filtered = filtered.Where(s =>
                s.Owners.Any(o => o.UserId == owner.Value));
        }

        var items = filtered
            .OrderBy(s => s.Title, StringComparer.OrdinalIgnoreCase)
            .Select(s => new DdtStandardsRegisterRow
            {
                Id = s.Id,
                Title = s.Title,
                Slug = s.Slug,
                Version = s.Version,
                Stage = s.Stage,
                CategoryDisplay = string.Join(", ", s.Categories.Select(c => c.Category.Name)),
                OwnerDisplay = string.Join(", ", s.Owners.Select(o => o.User.Name)),
                PublishedAt = s.PublishedAt,
                DraftCreated = s.DraftCreated,
                IsPublished = s.IsPublished
            })
            .ToList();

        if (string.Equals(export, "csv", StringComparison.OrdinalIgnoreCase))
            return ExportDdtCsv(items, tabLower);

        var categoryOptions = allNonDeleted
            .SelectMany(s => s.Categories.Select(c => c.Category.Name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(c => c)
            .ToList();

        var ownerOptions = allNonDeleted
            .SelectMany(s => s.Owners.Select(o => new { o.UserId, o.User.Name }))
            .DistinctBy(o => o.UserId)
            .OrderBy(o => o.Name)
            .Select(o => new DdtStandardsOwnerOption { Id = o.UserId, Name = o.Name })
            .ToList();

        var vm = new DdtStandardsRegisterViewModel
        {
            Tab = tabLower,
            TotalCount = allNonDeleted.Count,
            YourStandardsCount = yourStandardsCount,
            ShowYourStandardsTab = showYourStandardsTab,
            PublishedCount = publishedCount,
            DraftCount = draftCount,
            ReviewCount = reviewCount,
            AwaitingPublishCount = awaitingPublishCount,
            WithdrawnCount = withdrawnCount,
            Items = items,
            CategoryOptions = categoryOptions,
            CategoryFilter = category,
            Search = search,
            OwnerFilter = owner,
            OwnerOptions = ownerOptions
        };

        return View("~/Views/Modern/Standards/DdtStandards.cshtml", vm);
    }

    [HttpGet("ddt/{id:int}")]
    public async Task<IActionResult> DdtDetail(int id)
    {
        SetChrome("standards-ddt");

        var standard = await _context.DdtStandards
            .AsNoTracking()
            .Include(s => s.CreatorUser)
            .Include(s => s.Owners).ThenInclude(o => o.User)
            .Include(s => s.Contacts).ThenInclude(c => c.User)
            .Include(s => s.Categories).ThenInclude(c => c.Category)
            .Include(s => s.SubCategories).ThenInclude(sc => sc.SubCategory)
            .Include(s => s.Phases).ThenInclude(p => p.PhaseLookup)
            .Include(s => s.ValidationRules)
            .Include(s => s.Products).ThenInclude(p => p.StandardProduct)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

        if (standard == null) return NotFound();

        if (standard.ParentStandardId.HasValue && standard.Stage == "Draft")
            return RedirectToAction(nameof(DdtCreate), new { id = standard.ParentStandardId.Value });

        var allForLineage = await _context.DdtStandards.AsNoTracking()
            .Where(s => !s.IsDeleted)
            .Select(s => new DdtStandard { Id = s.Id, ParentStandardId = s.ParentStandardId, IsPublished = s.IsPublished, Stage = s.Stage, PublishedAt = s.PublishedAt, UpdatedAt = s.UpdatedAt })
            .ToListAsync();
        var latestPublishedId = DdtStandardsListingHelper.GetLatestPublishedIdInLineage(allForLineage, id);
        if (latestPublishedId.HasValue && latestPublishedId.Value != id && standard.IsPublished && standard.Stage == "Published")
            return RedirectToAction(nameof(DdtDetail), new { id = latestPublishedId.Value });

        var categoryIds = standard.Categories.Select(c => c.CategoryId).ToList();
        var relatedStandards = categoryIds.Any()
            ? DdtStandardsListingHelper.LatestPublishedOnly(
                await _context.DdtStandards.AsNoTracking()
                    .Include(s => s.Categories).ThenInclude(c => c.Category)
                    .Where(s => !s.IsDeleted && s.IsPublished && s.Stage == "Published" && s.Id != id
                        && s.Categories.Any(c => categoryIds.Contains(c.CategoryId)))
                    .ToListAsync())
                .OrderByDescending(s => s.PublishedAt)
                .Take(5)
                .ToList()
            : new List<DdtStandard>();

        ViewBag.RelatedStandards = relatedStandards;
        ViewBag.Workflow = await _ddtWorkflow.BuildWorkflowContextAsync(id, User);
        ViewBag.Toolbar = await _ddtWorkflow.BuildDetailToolbarAsync(id, User);
        return View("~/Views/Modern/Standards/DdtDetail.cshtml", standard);
    }

    [HttpGet("ddt/{id:int}/history")]
    public async Task<IActionResult> DdtHistory(int id)
    {
        SetChrome("standards-ddt");

        var vm = await _ddtWorkflow.BuildHistoryViewModelAsync(id);
        if (vm == null) return NotFound();

        return View("~/Views/Modern/Standards/DdtHistory.cshtml", vm);
    }

    [HttpGet("ddt/{id:int}/edit-confirm")]
    public async Task<IActionResult> DdtStartEditConfirm(int id, CancellationToken cancellationToken = default)
    {
        SetChrome("standards-ddt");

        var standard = await _context.DdtStandards.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted, cancellationToken);
        if (standard == null)
            return NotFound();

        var toolbar = await _ddtWorkflow.BuildDetailToolbarAsync(id, User, cancellationToken);
        if (!toolbar.CanOpenEdit)
        {
            TempData["ErrorMessage"] = "You do not have permission to edit this standard.";
            return RedirectToAction(nameof(DdtDetail), new { id });
        }

        if (toolbar.DraftEditId.HasValue)
            return RedirectToAction(nameof(DdtCreate), new { id = toolbar.DraftEditId });

        if (!toolbar.NeedsDraftFromPublished)
        {
            if (standard.Stage == "Draft")
                return RedirectToAction(nameof(DdtCreate), new { id });
            TempData["ErrorMessage"] = "This standard cannot be edited in its current stage.";
            return RedirectToAction(nameof(DdtDetail), new { id });
        }

        return View("~/Views/Modern/Standards/DdtStartEditConfirm.cshtml", new DdtStartEditConfirmViewModel
        {
            StandardId = id,
            Title = standard.Title,
            Version = standard.Version
        });
    }

    [HttpPost("ddt/{id:int}/start-edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DdtStartEdit(int id)
    {
        var result = await _ddtWorkflow.EnsureDraftForEditAsync(id, User);
        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
            return RedirectToAction(nameof(DdtDetail), new { id });
        }

        return RedirectToAction(nameof(DdtCreate), new { id = result.StandardId });
    }

    [HttpGet("ddt/create")]
    [HttpGet("ddt/{id:int}/edit")]
    public async Task<IActionResult> DdtCreate(int? id)
    {
        SetChrome("standards-ddt");
        var vm = await _ddtWorkflow.BuildEditViewModelAsync(id, User);
        if (id.HasValue && vm.Id == null)
            return NotFound();

        if (id.HasValue)
        {
            var editTarget = await _context.DdtStandards.AsNoTracking()
                .Where(s => s.Id == id.Value && !s.IsDeleted)
                .Select(s => new { s.ParentStandardId, s.Stage })
                .FirstOrDefaultAsync();
            if (editTarget is { Stage: "Draft", ParentStandardId: int parentId })
                return RedirectToAction(nameof(DdtCreate), new { id = parentId });
        }

        if (id.HasValue && !vm.CanEdit)
        {
            var published = await _context.DdtStandards.AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id.Value && !s.IsDeleted);
            if (published is { IsPublished: true, Stage: "Published" })
            {
                var toolbar = await _ddtWorkflow.BuildDetailToolbarAsync(id.Value, User);
                if (toolbar.DraftEditId.HasValue)
                    return RedirectToAction(nameof(DdtCreate), new { id = toolbar.DraftEditId });
                if (toolbar.NeedsDraftFromPublished)
                    return RedirectToAction(nameof(DdtStartEditConfirm), new { id = id.Value });

                TempData["ErrorMessage"] = "You do not have permission to edit this standard.";
                return RedirectToAction(nameof(DdtDetail), new { id });
            }

            TempData["ErrorMessage"] = "This standard cannot be edited in its current stage.";
            return RedirectToAction(nameof(DdtDetail), new { id });
        }
        return View("~/Views/Modern/Standards/DdtCreate.cshtml", vm);
    }

    [HttpPost("ddt/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DdtCreatePost(
        int? id,
        string title,
        string? summary,
        string? purpose,
        string? criteria,
        string? howToMeet,
        string? governance,
        string? legalBasis,
        bool legalStandard = false,
        int? validityPeriod = null,
        string? relatedGuidance = null,
        List<int>? categoryIds = null,
        List<int>? phaseIds = null,
        string? ownerObjectIds = null,
        string? contactObjectIds = null,
        string? submitAction = null)
    {
        SetChrome("standards-ddt");
        var input = new DdtStandardDraftInput
        {
            Id = id,
            Title = title,
            Summary = summary,
            Purpose = purpose,
            Criteria = criteria,
            HowToMeet = howToMeet,
            Governance = governance,
            LegalBasis = legalBasis,
            LegalStandard = legalStandard,
            ValidityPeriod = validityPeriod,
            RelatedGuidance = relatedGuidance,
            CategoryIds = categoryIds,
            PhaseIds = phaseIds,
            OwnerObjectIds = ownerObjectIds,
            ContactObjectIds = contactObjectIds
        };

        var result = await _ddtWorkflow.SaveDraftAsync(input, User);
        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
            var vm = await _ddtWorkflow.BuildEditViewModelAsync(id, User);
            vm.Title = title;
            vm.Summary = summary;
            vm.Purpose = purpose;
            vm.Criteria = criteria;
            vm.HowToMeet = howToMeet;
            vm.Governance = governance;
            vm.LegalBasis = legalBasis;
            vm.LegalStandard = legalStandard;
            vm.ValidityPeriod = validityPeriod;
            vm.RelatedGuidance = relatedGuidance;
            vm.SelectedCategoryIds = categoryIds ?? new List<int>();
            vm.SelectedPhaseIds = phaseIds ?? new List<int>();
            vm.OwnerObjectIds = ownerObjectIds;
            vm.ContactObjectIds = contactObjectIds;
            return View("~/Views/Modern/Standards/DdtCreate.cshtml", vm);
        }

        if (string.Equals(submitAction, "submit", StringComparison.OrdinalIgnoreCase) && result.StandardId.HasValue)
        {
            var submit = await _ddtWorkflow.SubmitForReviewAsync(result.StandardId.Value, User);
            SetFlash(submit);
            return RedirectToAction(nameof(DdtDetail), new { id = result.StandardId.Value });
        }

        TempData["SuccessMessage"] = result.SuccessMessage;
        return RedirectToAction(nameof(DdtCreate), new { id = result.StandardId });
    }

    [HttpPost("ddt/{id:int}/submit-for-review")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DdtSubmitForReview(int id)
    {
        var result = await _ddtWorkflow.SubmitForReviewAsync(id, User);
        SetFlash(result);
        return RedirectToAction(result.Success ? nameof(DdtDetail) : nameof(DdtCreate), new { id });
    }

    [HttpPost("ddt/{id:int}/approve")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DdtApprove(int id, string? comment)
    {
        var result = await _ddtWorkflow.ApproveAsync(id, comment, User);
        SetFlash(result);
        return RedirectToAction(nameof(DdtDetail), new { id });
    }

    [HttpPost("ddt/{id:int}/reject")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DdtReject(int id, string reason)
    {
        var result = await _ddtWorkflow.RejectAsync(id, reason, User);
        SetFlash(result);
        return RedirectToAction(nameof(DdtDetail), new { id });
    }

    [HttpPost("ddt/{id:int}/publish")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DdtPublish(int id)
    {
        var result = await _ddtWorkflow.PublishAsync(id, User);
        SetFlash(result);
        return RedirectToAction(result.Success ? nameof(DdtDetail) : nameof(DdtDetail), new { id });
    }

    [HttpPost("ddt/{id:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DdtDeleteDraft(int id)
    {
        var result = await _ddtWorkflow.DeleteDraftAsync(id, User);
        SetFlash(result);
        return result.Success
            ? (result.StandardId.HasValue
                ? RedirectToAction(nameof(DdtDetail), new { id = result.StandardId.Value })
                : RedirectToAction(nameof(DdtStandards), new { tab = "draft" }))
            : RedirectToAction(nameof(DdtCreate), new { id });
    }

    private void SetFlash(WorkflowOperationResult result)
    {
        if (result.Success)
            TempData["SuccessMessage"] = result.SuccessMessage;
        else
            TempData["ErrorMessage"] = result.ErrorMessage;
    }

    private IActionResult ExportDdtCsv(List<DdtStandardsRegisterRow> rows, string tab)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Title,Version,Stage,Categories,Owners,Published,Draft Created");

        foreach (var r in rows)
        {
            sb.Append(CsvField(r.Title)).Append(',');
            sb.Append(CsvField(r.Version)).Append(',');
            sb.Append(CsvField(r.Stage)).Append(',');
            sb.Append(CsvField(r.CategoryDisplay)).Append(',');
            sb.Append(CsvField(r.OwnerDisplay)).Append(',');
            sb.Append(CsvField(r.PublishedAt?.ToString("yyyy-MM-dd") ?? "")).Append(',');
            sb.AppendLine(CsvField(r.DraftCreated.ToString("yyyy-MM-dd")));
        }

        var fileName = $"ddt-standards-{tab}-{DateTime.UtcNow:yyyyMMdd}.csv";
        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", fileName);
    }

    private static string CsvField(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }

    #endregion

    #region Functional Standards

    [HttpGet("functional")]
    public async Task<IActionResult> FunctionalStandards(string? tab = null)
    {
        SetChrome("standards-functional");

        var tabKey = tab?.Trim().ToLowerInvariant() switch
        {
            "completed" => "completed",
            "all" or "all-standards" => "all",
            _ => "in-progress"
        };

        var standards = await _context.FunctionalStandards.AsNoTracking()
            .Include(fs => fs.Themes)
                .ThenInclude(t => t.PracticeAreas)
                    .ThenInclude(pa => pa.Criteria)
            .OrderBy(fs => fs.Id)
            .ToListAsync();

        var assessments = await _context.FunctionalStandardAssessments.AsNoTracking()
            .Include(a => a.CriteriaResponses)
            .ToListAsync();

        var assessmentsByStandard = assessments
            .GroupBy(a => a.FunctionalStandardId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var rows = standards.Select(fs =>
        {
            var criteriaCount = fs.Themes.SelectMany(t => t.PracticeAreas).SelectMany(pa => pa.Criteria).Count();
            var standardAssessments = assessmentsByStandard.GetValueOrDefault(fs.Id, new List<FunctionalStandardAssessment>());
            var inProgress = standardAssessments.FirstOrDefault(a => a.SubmittedAt == null);
            var lastComplete = standardAssessments
                .Where(a => a.SubmittedAt != null)
                .OrderByDescending(a => a.SubmittedAt)
                .FirstOrDefault();

            FunctionalStandardLastCycleVm? lastCycle = null;
            if (lastComplete != null)
            {
                var responses = lastComplete.CriteriaResponses;
                var total = responses.Count;
                var answered = responses.Count(r => r.Attainment != null);
                var fullyMet = responses.Count(r => r.Attainment == AttainmentLevel.FullyMet);
                var partiallyMet = responses.Count(r => r.Attainment == AttainmentLevel.PartiallyMet);
                var notMet = responses.Count(r => r.Attainment == AttainmentLevel.NotOrSeldomMet);

                var pct = total > 0 ? (double)fullyMet / total * 100 : 0;
                var (tagClass, headline) = AttainmentBadge(pct);

                lastCycle = new FunctionalStandardLastCycleVm
                {
                    Answered = answered,
                    Total = total,
                    FullyMet = fullyMet,
                    PartiallyMet = partiallyMet,
                    NotMet = notMet,
                    Headline = headline,
                    HeadlineColorVar = pct >= 80 ? "var(--rag-g)" : pct >= 50 ? "var(--rag-a)" : "var(--rag-r)",
                    LastCycleTagClass = tagClass
                };
            }

            return new FunctionalStandardLandingRow
            {
                Id = fs.Id,
                Title = fs.Title,
                Description = fs.Description,
                ThemeCount = fs.Themes.Count,
                CriteriaCount = criteriaCount,
                AssessmentCount = standardAssessments.Count,
                ContinueAssessmentId = inProgress?.Id,
                LastCycle = lastCycle
            };
        }).ToList();

        var inProgressAssessments = assessments
            .Where(a => a.SubmittedAt == null)
            .Select(a =>
            {
                var total = a.CriteriaResponses.Count;
                var answered = a.CriteriaResponses.Count(r => r.Attainment != null);
                var standard = standards.FirstOrDefault(fs => fs.Id == a.FunctionalStandardId);

                return new FunctionalStandardInProgressRow
                {
                    AssessmentId = a.Id,
                    StandardTitle = standard?.Title ?? $"FS-{a.FunctionalStandardId}",
                    AssessmentName = a.AssessmentName,
                    AssessmentDate = a.AssessmentDate,
                    CriteriaAnswered = answered,
                    CriteriaTotal = total
                };
            })
            .ToList();

        var completedAssessments = assessments
            .Where(a => a.SubmittedAt != null)
            .OrderByDescending(a => a.SubmittedAt)
            .Select(a =>
            {
                var total = a.CriteriaResponses.Count;
                var answered = a.CriteriaResponses.Count(r => r.Attainment != null);
                var standard = standards.FirstOrDefault(fs => fs.Id == a.FunctionalStandardId);

                return new FunctionalStandardCompletedAssessmentRow
                {
                    AssessmentId = a.Id,
                    StandardTitle = standard?.Title ?? $"FS-{a.FunctionalStandardId}",
                    AssessmentName = a.AssessmentName,
                    AssessmentDate = a.AssessmentDate,
                    SubmittedAt = a.SubmittedAt!.Value,
                    CriteriaAnswered = answered,
                    CriteriaTotal = total
                };
            })
            .ToList();

        var vm = new FunctionalStandardsLandingViewModel
        {
            Tab = tabKey,
            Standards = rows,
            InProgressAssessments = inProgressAssessments,
            CompletedAssessments = completedAssessments
        };

        return View("~/Views/Modern/Standards/FunctionalStandards.cshtml", vm);
    }

    [HttpGet("functional/{id:int}")]
    public async Task<IActionResult> FunctionalStandardDashboard(int id)
    {
        SetChrome("standards-functional");

        var standard = await _context.FunctionalStandards
            .Include(fs => fs.Themes)
                .ThenInclude(t => t.PracticeAreas)
                    .ThenInclude(pa => pa.Criteria)
            .FirstOrDefaultAsync(fs => fs.Id == id);

        if (standard == null) return NotFound();

        var allAssessments = await _context.FunctionalStandardAssessments
            .Include(a => a.CriteriaResponses)
            .Where(a => a.FunctionalStandardId == id)
            .OrderByDescending(a => a.AssessmentDate)
            .ToListAsync();

        var allUsers = await _context.Users.AsNoTracking().ToListAsync();
        var userMap = allUsers
            .Where(u => !string.IsNullOrEmpty(u.Email))
            .ToDictionary(u => u.Email.ToLowerInvariant(), u => u.Name);

        var totalCriteria = standard.Themes?
            .Sum(t => t.PracticeAreas?.Sum(pa => pa.Criteria?.Count ?? 0) ?? 0) ?? 0;

        var inProgressList = new List<dynamic>();
        var submittedList = new List<dynamic>();

        foreach (var a in allAssessments)
        {
            var completed = a.CriteriaResponses.Count(r => r.Attainment.HasValue);
            var assessedByLower = a.AssessedBy.ToLowerInvariant();
            var userName = userMap.TryGetValue(assessedByLower, out var n) ? n : a.AssessedBy;
            dynamic info = new
            {
                Assessment = a,
                CompletedCount = completed,
                TotalCount = totalCriteria,
                IsComplete = completed == totalCriteria && totalCriteria > 0,
                UserName = userName
            };
            if (a.SubmittedAt.HasValue) submittedList.Add(info); else inProgressList.Add(info);
        }

        ViewBag.InProgressAssessments = inProgressList;
        ViewBag.SubmittedAssessments = submittedList;
        ViewBag.HasInProgressAssessment = inProgressList.Any();

        return View("~/Views/Modern/Standards/FunctionalStandardDetail.cshtml", standard);
    }

    [HttpGet("functional/assessment/{assessmentId:int}/conduct")]
    public async Task<IActionResult> ConductAssessment(int assessmentId)
    {
        SetChrome("standards-functional");

        var assessment = await _context.FunctionalStandardAssessments
            .Include(a => a.FunctionalStandard)
                .ThenInclude(fs => fs!.Themes)
                    .ThenInclude(t => t.PracticeAreas)
                        .ThenInclude(pa => pa.Criteria)
            .Include(a => a.CriteriaResponses)
            .FirstOrDefaultAsync(a => a.Id == assessmentId);

        if (assessment == null) return NotFound();

        ViewBag.IsReadOnly = assessment.SubmittedAt.HasValue;
        return View("~/Views/Modern/Standards/ConductAssessment.cshtml", assessment);
    }

    [HttpGet("functional/assessment/{assessmentId:int}/summary")]
    public async Task<IActionResult> AssessmentSummary(int assessmentId)
    {
        SetChrome("standards-functional");

        var assessment = await _context.FunctionalStandardAssessments
            .Include(a => a.FunctionalStandard)
                .ThenInclude(fs => fs!.Themes)
                    .ThenInclude(t => t.PracticeAreas)
                        .ThenInclude(pa => pa.Criteria)
            .Include(a => a.CriteriaResponses)
            .FirstOrDefaultAsync(a => a.Id == assessmentId);

        if (assessment == null) return NotFound();

        return View("~/Views/Modern/Standards/AssessmentSummary.cshtml", assessment);
    }

    [HttpPost("functional/assessment/save-response")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveCriteriaResponse(int responseId, int? attainment, string? notes)
    {
        var response = await _context.AssessmentCriteriaResponses
            .FirstOrDefaultAsync(r => r.Id == responseId);

        if (response == null)
            return Json(new { success = false, message = "Response not found" });

        if (attainment.HasValue)
            response.Attainment = (AttainmentLevel)attainment.Value;

        response.Notes = notes;
        response.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Json(new { success = true });
    }

    [HttpPost("functional/assessment/submit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitAssessment(int assessmentId)
    {
        var assessment = await _context.FunctionalStandardAssessments
            .Include(a => a.FunctionalStandard)
                .ThenInclude(fs => fs!.Themes)
                    .ThenInclude(t => t.PracticeAreas)
                        .ThenInclude(pa => pa.Criteria)
            .Include(a => a.CriteriaResponses)
            .FirstOrDefaultAsync(a => a.Id == assessmentId);

        if (assessment == null) return NotFound();

        var (totalCriteria, completed, _, _, _) = FunctionalAssessmentProgress.CountAgainstStandardTree(assessment);

        if (completed < totalCriteria)
        {
            TempData["ErrorMessage"] = $"Cannot submit — only {completed} of {totalCriteria} criteria assessed.";
            return RedirectToAction(nameof(ConductAssessment), new { assessmentId });
        }

        assessment.SubmittedAt = DateTime.UtcNow;
        assessment.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Assessment submitted successfully";
        return RedirectToAction(nameof(AssessmentSummary), new { assessmentId });
    }

    [HttpPost("functional/assessment/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAssessment(int assessmentId, int standardId)
    {
        var assessment = await _context.FunctionalStandardAssessments
            .Include(a => a.CriteriaResponses)
            .FirstOrDefaultAsync(a => a.Id == assessmentId);

        if (assessment != null)
        {
            _context.AssessmentCriteriaResponses.RemoveRange(assessment.CriteriaResponses);
            _context.FunctionalStandardAssessments.Remove(assessment);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Assessment '{assessment.AssessmentName}' deleted";
        }

        return RedirectToAction(nameof(FunctionalStandardDashboard), new { id = standardId });
    }

    [HttpPost("functional/start")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StartAssessment(int standardId, string assessmentName)
    {
        if (standardId <= 0 || string.IsNullOrWhiteSpace(assessmentName))
        {
            TempData["ErrorMessage"] = "Please provide an assessment name";
            return RedirectToAction(nameof(FunctionalStandardDashboard), new { id = standardId });
        }

        var standard = await _context.FunctionalStandards
            .Include(fs => fs.Themes)
                .ThenInclude(t => t.PracticeAreas)
                    .ThenInclude(pa => pa.Criteria)
            .FirstOrDefaultAsync(fs => fs.Id == standardId);

        if (standard == null) return NotFound();

        var existingInProgress = await _context.FunctionalStandardAssessments
            .AnyAsync(a => a.FunctionalStandardId == standardId && !a.SubmittedAt.HasValue);

        if (existingInProgress)
        {
            TempData["ErrorMessage"] = "An assessment is already in progress";
            return RedirectToAction(nameof(FunctionalStandardDashboard), new { id = standardId });
        }

        var userEmail = GetUserEmail();
        var assessment = new FunctionalStandardAssessment
        {
            FunctionalStandardId = standardId,
            AssessmentName = assessmentName,
            AssessedBy = userEmail,
            AssessmentDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.FunctionalStandardAssessments.Add(assessment);
        await _context.SaveChangesAsync();

        foreach (var theme in standard.Themes ?? new List<FunctionalStandardTheme>())
        {
            foreach (var pa in theme.PracticeAreas ?? new List<PracticeArea>())
            {
                foreach (var criterion in pa.Criteria ?? new List<Criterion>())
                {
                    _context.AssessmentCriteriaResponses.Add(new AssessmentCriteriaResponse
                    {
                        AssessmentId = assessment.Id,
                        FunctionalStandardId = standardId,
                        ThemeId = theme.ThemeId,
                        PracticeAreaId = pa.PracticeAreaId,
                        CriteriaCode = criterion.CriteriaCode,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
            }
        }

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Assessment '{assessmentName}' created";
        return RedirectToAction(nameof(ConductAssessment), new { assessmentId = assessment.Id });
    }

    [HttpGet("functional/assessment/{assessmentId:int}/export/word")]
    public async Task<IActionResult> ExportAssessmentWord(int assessmentId)
    {
        var assessment = await _context.FunctionalStandardAssessments
            .AsNoTracking()
            .Include(a => a.FunctionalStandard)
                .ThenInclude(fs => fs!.Themes)
                    .ThenInclude(t => t.PracticeAreas)
                        .ThenInclude(pa => pa.Criteria)
            .Include(a => a.CriteriaResponses)
            .FirstOrDefaultAsync(a => a.Id == assessmentId);

        if (assessment == null) return NotFound();

        var (bytes, contentType, fileName) = FunctionalAssessmentWordExporter.Export(assessment);
        return File(bytes, contentType, fileName);
    }

    [HttpGet("functional/assessment/{assessmentId:int}/export")]
    public async Task<IActionResult> ExportAssessmentCsv(int assessmentId)
    {
        var assessment = await _context.FunctionalStandardAssessments
            .AsNoTracking()
            .Include(a => a.FunctionalStandard)
                .ThenInclude(fs => fs!.Themes)
                    .ThenInclude(t => t.PracticeAreas)
                        .ThenInclude(pa => pa.Criteria)
            .Include(a => a.CriteriaResponses)
            .FirstOrDefaultAsync(a => a.Id == assessmentId);

        if (assessment == null) return NotFound();

        var sb = new StringBuilder();
        sb.AppendLine("Theme,Theme Title,Practice Area,Practice Area Title,Rating,Criteria Code,Criteria,Attainment,Notes");

        if (assessment.FunctionalStandard?.Themes != null)
        {
            foreach (var theme in assessment.FunctionalStandard.Themes.OrderBy(t => t.ThemeId))
            {
                if (theme.PracticeAreas == null) continue;
                foreach (var pa in theme.PracticeAreas.OrderBy(p => p.PracticeAreaId))
                {
                    if (pa.Criteria == null) continue;
                    foreach (var c in pa.Criteria.OrderBy(x => x.CriteriaCode))
                    {
                        var resp = assessment.CriteriaResponses
                            .FirstOrDefault(r => r.ThemeId == theme.ThemeId && r.PracticeAreaId == pa.PracticeAreaId && r.CriteriaCode == c.CriteriaCode);
                        var att = resp?.Attainment switch
                        {
                            AttainmentLevel.FullyMet => "Fully met",
                            AttainmentLevel.PartiallyMet => "Partially met",
                            AttainmentLevel.NotOrSeldomMet => "Not met",
                            _ => ""
                        };
                        sb.AppendLine($"{theme.ThemeId},{CsvField(theme.Title)},{pa.PracticeAreaId},{CsvField(pa.Title)},{c.Rating},{c.CriteriaCode},{CsvField(c.Criteria)},{CsvField(att)},{CsvField(resp?.Notes)}");
                    }
                }
            }
        }

        var fileName = $"FSA-{assessment.AssessmentName.Replace(" ", "_")}-{DateTime.UtcNow:yyyyMMdd}.csv";
        var bom = System.Text.Encoding.UTF8.GetPreamble();
        var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
        var fileBytes = new byte[bom.Length + bytes.Length];
        Buffer.BlockCopy(bom, 0, fileBytes, 0, bom.Length);
        Buffer.BlockCopy(bytes, 0, fileBytes, bom.Length, bytes.Length);
        return File(fileBytes, "text/csv", fileName);
    }

    #endregion

    #region Management

    [HttpGet("management")]
    public async Task<IActionResult> Management(string? section = null, string? productStatus = null)
    {
        SetChrome("standards-management");

        var canManage = await StandardsPermissionHelper.CanAccessModernStandardsManagementAsync(_permissions, User);
        ViewBag.CanAccessStandardsManagement = canManage;

        var sectionKey = NormalizeManagementSection(section);
        var productFilter = string.IsNullOrWhiteSpace(productStatus) ? null : productStatus.Trim();

        if (!canManage)
            return View("~/Views/Modern/Standards/Management.cshtml", new StandardsManagementViewModel { ActiveSection = sectionKey });

        var userEmail = GetUserEmail();
        var canManageLookups =
            !string.IsNullOrEmpty(userEmail) &&
            (await _permissions.IsSuperAdminAsync(userEmail) ||
             await _permissions.IsInGroupAsync(userEmail, "Central Operations Admin"));
        ViewBag.CanManageStandardLookups = canManageLookups;

        var currentUserId = GetCurrentUserId();

        var yourStandardsCount = 0;
        if (currentUserId is int userId && userId > 0)
        {
            var yourStandards = await _context.DdtStandards.AsNoTracking()
                .Include(s => s.Owners)
                .Include(s => s.Contacts)
                .Where(s => !s.IsDeleted &&
                    (s.Owners.Any(o => o.UserId == userId) || s.Contacts.Any(c => c.UserId == userId)))
                .ToListAsync();
            yourStandardsCount = DdtStandardsListingHelper.YourPublishedOnly(yourStandards, userId).Count();
        }

        var allForDraftCount = await _context.DdtStandards.AsNoTracking()
            .Where(s => !s.IsDeleted)
            .Select(s => new DdtStandard { Id = s.Id, ParentStandardId = s.ParentStandardId, Stage = s.Stage, UpdatedAt = s.UpdatedAt })
            .ToListAsync();
        var draftCount = DdtStandardsListingHelper.ActiveDraftsOnly(allForDraftCount).Count();

        var pendingReview = await _context.DdtStandards.AsNoTracking()
            .Include(s => s.Categories).ThenInclude(c => c.Category)
            .Include(s => s.Owners).ThenInclude(o => o.User)
            .Where(s => !s.IsDeleted && (s.Stage == "For Approval" || s.Stage == "Under Review"))
            .OrderByDescending(s => s.UpdatedAt)
            .ToListAsync();

        var reviewQueueCount = await _context.DdtStandards.AsNoTracking()
            .CountAsync(s => !s.IsDeleted && s.Stage == "For Approval");

        var awaitingPublishCount = await _context.DdtStandards.AsNoTracking()
            .CountAsync(s => !s.IsDeleted && s.Stage == "Awaiting Publication");

        var pendingRows = pendingReview.Select(s => new DdtStandardsRegisterRow
        {
            Id = s.Id,
            Title = s.Title,
            Slug = s.Slug,
            Version = s.Version,
            Stage = s.Stage,
            CategoryDisplay = string.Join(", ", s.Categories.Select(c => c.Category.Name)),
            OwnerDisplay = string.Join(", ", s.Owners.Select(o => o.User.Name)),
            PublishedAt = s.PublishedAt,
            DraftCreated = s.DraftCreated,
            IsPublished = s.IsPublished
        }).ToList();

        var functionalStandards = await _context.FunctionalStandards.AsNoTracking()
            .Include(fs => fs.Themes)
                .ThenInclude(t => t.PracticeAreas)
                    .ThenInclude(pa => pa.Criteria)
            .OrderBy(fs => fs.Id)
            .ToListAsync();

        var assessmentCounts = await _context.FunctionalStandardAssessments.AsNoTracking()
            .GroupBy(a => a.FunctionalStandardId)
            .Select(g => new { StandardId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.StandardId, g => g.Count);

        var fsAdminRows = functionalStandards.Select(fs => new FunctionalStandardAdminRow
        {
            Id = fs.Id,
            Title = fs.Title,
            Description = fs.Description,
            ThemeCount = fs.Themes.Count,
            CriteriaCount = fs.Themes.SelectMany(t => t.PracticeAreas).SelectMany(pa => pa.Criteria).Count(),
            AssessmentCount = assessmentCounts.GetValueOrDefault(fs.Id, 0)
        }).ToList();

        var categoryRows = await LoadManagementCategoriesAsync();
        var productRows = await LoadManagementProductsAsync(productFilter);
        var productsCount = await _context.StandardProducts.AsNoTracking().CountAsync();

        var vm = new StandardsManagementViewModel
        {
            ActiveSection = sectionKey,
            YourStandardsCount = yourStandardsCount,
            DraftStandardsCount = draftCount,
            StandardsToReviewCount = reviewQueueCount,
            StandardsAwaitingPublishCount = awaitingPublishCount,
            FunctionalStandardsCount = fsAdminRows.Count,
            StandardCategoriesCount = categoryRows.Count,
            StandardProductsCount = productsCount,
            ProductFilterStatus = productFilter,
            PendingReviewStandards = pendingRows,
            FunctionalStandards = fsAdminRows,
            Categories = categoryRows,
            Products = productRows
        };

        return View("~/Views/Modern/Standards/Management.cshtml", vm);
    }

    #endregion

    #region Helpers

    private static string DdtStageTagClass(string? stage) => stage switch
    {
        "Published" => "dfe-c-tag--green",
        "Draft" => "dfe-c-tag--grey",
        "Under Review" => "dfe-c-tag--blue",
        "For Approval" => "dfe-c-tag--amber",
        "Approved" => "dfe-c-tag--turquoise",
        "Archived" => "dfe-c-tag--orange",
        "Rejected" => "dfe-c-tag--red",
        _ => "dfe-c-tag--grey"
    };

    private static (string TagClass, string Label) AttainmentBadge(double fullyMetPct)
    {
        if (fullyMetPct >= 80) return ("dfe-c-tag--green", "Strong");
        if (fullyMetPct >= 50) return ("dfe-c-tag--amber", "Developing");
        return ("dfe-c-tag--red", "Needs attention");
    }

    #endregion
}
