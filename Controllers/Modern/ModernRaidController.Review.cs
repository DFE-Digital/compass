using System.Globalization;
using Compass.Models;
using Compass.Models.Raid;
using Compass.Services.Raid;
using Compass.ViewModels.Modern;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Compass.Controllers.Modern;

/// <summary>Monthly RAID review workflow at <c>/modern/raid/review</c>.</summary>
public partial class ModernRaidController
{
    private const string RaidReviewStorageKey = "compass.raidReview.businessAreaIds";

    private static List<int> ParseBusinessAreaIds(string? baParam)
    {
        if (string.IsNullOrWhiteSpace(baParam))
            return new List<int>();
        return baParam.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => int.TryParse(s, out var id) ? id : 0)
            .Where(id => id > 0)
            .Distinct()
            .ToList();
    }

    private static string BuildBusinessAreaQuery(IReadOnlyList<int> ids) =>
        string.Join(",", ids);

    private async Task<List<int>> ValidateBusinessAreaIdsAsync(IReadOnlyList<int> ids, CancellationToken ct)
    {
        if (ids.Count == 0)
            return new List<int>();
        var active = await _db.BusinessAreaLookups.AsNoTracking()
            .Where(b => b.IsActive && ids.Contains(b.Id))
            .Select(b => b.Id)
            .ToListAsync(ct);
        return ids.Where(active.Contains).Distinct().ToList();
    }

    private IQueryable<Risk> RisksInBusinessAreas(IReadOnlyList<int> baIds)
    {
        if (baIds.Count == 0)
            return _db.Risks.Where(_ => false);
        return _db.Risks.AsNoTracking().Where(r => !r.IsDeleted && (
            (r.ProjectId != null &&
             _db.Projects.Any(p => p.Id == r.ProjectId && p.BusinessAreaId != null && baIds.Contains(p.BusinessAreaId.Value))) ||
            r.RiskBusinessAreas.Any(rba => baIds.Contains(rba.BusinessAreaLookupId))));
    }

    private IQueryable<Issue> IssuesInBusinessAreas(IReadOnlyList<int> baIds)
    {
        if (baIds.Count == 0)
            return _db.Issues.Where(_ => false);
        return _db.Issues.AsNoTracking().Where(i => !i.IsDeleted && (
            (i.ProjectId != null &&
             _db.Projects.Any(p => p.Id == i.ProjectId && p.BusinessAreaId != null && baIds.Contains(p.BusinessAreaId.Value))) ||
            i.IssueBusinessAreas.Any(iba => baIds.Contains(iba.BusinessAreaLookupId))));
    }

    private async Task<IReadOnlyList<int>> SuggestBusinessAreaIdsForUserAsync(int userId, CancellationToken ct)
    {
        var fromLeadership = await _db.BusinessAreaLeadershipMembers.AsNoTracking()
            .Where(m => m.UserId == userId)
            .Select(m => m.BusinessAreaLookupId)
            .ToListAsync(ct);
        var fromAdmin = await _db.BusinessAreaAdminMembers.AsNoTracking()
            .Where(m => m.UserId == userId)
            .Select(m => m.BusinessAreaLookupId)
            .ToListAsync(ct);
        var pref = await GetSavedRaidRegisterBusinessAreaIdAsync(userId, ct);
        var combined = fromLeadership.Concat(fromAdmin).ToList();
        if (pref is > 0)
            combined.Add(pref.Value);
        return combined.Distinct().ToList();
    }

    private static (int year, int month, string label) CurrentReviewPeriod()
    {
        var now = DateTime.UtcNow;
        var label = now.ToString("MMMM yyyy", CultureInfo.GetCultureInfo("en-GB"));
        return (now.Year, now.Month, label);
    }

    [HttpGet("review")]
    public IActionResult ReviewStart()
    {
        SetRaidChrome("raid-review");
        ViewBag.RaidReviewStorageKey = RaidReviewStorageKey;
        return View("~/Views/Modern/Raid/ReviewStart.cshtml");
    }

    [HttpGet("review/setup")]
    public async Task<IActionResult> ReviewSetup(CancellationToken cancellationToken)
    {
        SetRaidChrome("raid-review");
        var userId = await ResolveCurrentUserIdAsync(cancellationToken);
        var suggested = userId.HasValue
            ? await SuggestBusinessAreaIdsForUserAsync(userId.Value, cancellationToken)
            : new List<int>();

        var options = await _db.BusinessAreaLookups.AsNoTracking()
            .Where(b => b.IsActive)
            .OrderBy(b => b.SortOrder).ThenBy(b => b.Name)
            .Select(b => new RaidLookupOptionVm(b.Id, b.Name))
            .ToListAsync(cancellationToken);

        var vm = new ModernRaidReviewSetupViewModel
        {
            BusinessAreaOptions = options,
            SuggestedBusinessAreaIds = suggested
        };
        ViewBag.RaidReviewStorageKey = RaidReviewStorageKey;
        return View("~/Views/Modern/Raid/ReviewSetup.cshtml", vm);
    }

    [HttpGet("review/overview")]
    public async Task<IActionResult> ReviewOverview(string? ba, CancellationToken cancellationToken)
    {
        SetRaidChrome("raid-review");
        var baIds = await ValidateBusinessAreaIdsAsync(ParseBusinessAreaIds(ba), cancellationToken);
        if (baIds.Count == 0)
        {
            TempData["ErrorMessage"] = "Choose at least one business area to review.";
            return RedirectToAction(nameof(ReviewSetup));
        }

        var baQuery = BuildBusinessAreaQuery(baIds);
        var baLabels = await _db.BusinessAreaLookups.AsNoTracking()
            .Where(b => baIds.Contains(b.Id))
            .OrderBy(b => b.SortOrder).ThenBy(b => b.Name)
            .Select(b => b.Name)
            .ToListAsync(cancellationToken);

        var (year, month, monthLabel) = CurrentReviewPeriod();
        var today = DateTime.UtcNow;

        var openRisks = await RisksInBusinessAreas(baIds)
            .Where(r => r.ClosedDate == null)
            .Include(r => r.RiskTier)
            .ToListAsync(cancellationToken);

        var openIssues = await IssuesInBusinessAreas(baIds)
            .Where(i => i.ClosedDate == null)
            .Include(i => i.SeverityLookup)
            .ToListAsync(cancellationToken);

        var reviewedRiskIds = await _db.RaidMonthlyReviews.AsNoTracking()
            .Where(x => x.RecordType == "risk" && x.ReviewYear == year && x.ReviewMonth == month)
            .Select(x => x.RecordId)
            .ToListAsync(cancellationToken);
        var reviewedIssueIds = await _db.RaidMonthlyReviews.AsNoTracking()
            .Where(x => x.RecordType == "issue" && x.ReviewYear == year && x.ReviewMonth == month)
            .Select(x => x.RecordId)
            .ToListAsync(cancellationToken);
        var reviewedRiskIdSet = reviewedRiskIds.ToHashSet();
        var reviewedIssueIdSet = reviewedIssueIds.ToHashSet();
        var reviewedInScopeCount = openRisks.Count(r => reviewedRiskIdSet.Contains(r.Id))
            + openIssues.Count(i => reviewedIssueIdSet.Contains(i.Id));

        var reviewedSet = reviewedRiskIds.Select(id => $"risk:{id}")
            .Concat(reviewedIssueIds.Select(id => $"issue:{id}"))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var risksByTier = openRisks
            .GroupBy(r => string.IsNullOrWhiteSpace(r.RiskTier?.Name) ? "Unassigned" : r.RiskTier!.Name.Trim())
            .OrderBy(g => g.Key)
            .Select(g => new ModernRaidLabelCountVm(g.Key, g.Count()))
            .ToList();

        var attention = BuildAttentionItems(openRisks, openIssues, today, reviewedSet);

        var reviewDueBy = _returnStatus.GetLastWorkingDayOfMonth(year, month);
        var reviewDueByLabel = reviewDueBy.ToString("dddd d MMMM", CultureInfo.GetCultureInfo("en-GB"));

        var vm = new ModernRaidReviewOverviewViewModel
        {
            ReviewMonthLabel = monthLabel,
            ReviewYear = year,
            ReviewMonth = month,
            BusinessAreaQuery = baQuery,
            BusinessAreaLabels = baLabels,
            ReviewDueByLabel = reviewDueByLabel,
            OpenRiskCount = openRisks.Count,
            OpenIssueCount = openIssues.Count,
            ReviewedThisMonthCount = reviewedInScopeCount,
            HasStartedReview = reviewedInScopeCount > 0,
            AttentionCount = attention.Count,
            RisksByTier = risksByTier,
            AttentionItems = attention.Take(12).ToList()
        };

        ViewBag.RaidReviewStorageKey = RaidReviewStorageKey;
        return View("~/Views/Modern/Raid/ReviewOverview.cshtml", vm);
    }

    private static List<ModernRaidReviewAttentionItemVm> BuildAttentionItems(
        List<Risk> openRisks,
        List<Issue> openIssues,
        DateTime todayUtc,
        HashSet<string> reviewedSet)
    {
        var items = new List<ModernRaidReviewAttentionItemVm>();

        foreach (var r in openRisks)
        {
            if (reviewedSet.Contains($"risk:{r.Id}"))
                continue;
            string? reason = null;
            if (RiskIsOverdue(r, todayUtc))
                reason = "Review or target date overdue";
            else if (RiskScoreBandHighest(r.RiskScore))
                reason = "Highest inherent risk score";
            else if (RiskScoreBandElevated(r.RiskScore))
                reason = "Elevated inherent risk score";
            if (reason == null)
                continue;
            items.Add(new ModernRaidReviewAttentionItemVm
            {
                Kind = "risk",
                Id = r.Id,
                Reference = $"R-{r.Id:D4}",
                Title = r.Title,
                Reason = reason,
                DetailHref = null
            });
        }

        foreach (var i in openIssues)
        {
            if (reviewedSet.Contains($"issue:{i.Id}"))
                continue;
            var bucket = IssueSeverityBucket(i);
            string? reason = null;
            if (IssueIsOverdue(i, todayUtc))
                reason = "Target resolution overdue";
            else if (bucket is "critical" or "high")
                reason = "High severity issue";
            if (reason == null)
                continue;
            items.Add(new ModernRaidReviewAttentionItemVm
            {
                Kind = "issue",
                Id = i.Id,
                Reference = $"I-{i.Id:D4}",
                Title = i.Title,
                Reason = reason,
                DetailHref = null
            });
        }

        return items
            .OrderBy(x => x.Kind)
            .ThenByDescending(x => x.Reason.Contains("Highest", StringComparison.OrdinalIgnoreCase))
            .ThenBy(x => x.Title)
            .ToList();
    }

    [HttpGet("review/work")]
    public async Task<IActionResult> ReviewWork(string? ba, string? kind, CancellationToken cancellationToken)
    {
        SetRaidChrome("raid-review");
        var baIds = await ValidateBusinessAreaIdsAsync(ParseBusinessAreaIds(ba), cancellationToken);
        if (baIds.Count == 0)
        {
            TempData["ErrorMessage"] = "Choose at least one business area to review.";
            return RedirectToAction(nameof(ReviewSetup));
        }

        var baQuery = BuildBusinessAreaQuery(baIds);
        var baLabels = await _db.BusinessAreaLookups.AsNoTracking()
            .Where(b => baIds.Contains(b.Id))
            .OrderBy(b => b.SortOrder).ThenBy(b => b.Name)
            .Select(b => b.Name)
            .ToListAsync(cancellationToken);

        var (year, month, monthLabel) = CurrentReviewPeriod();

        var risks = await RisksInBusinessAreas(baIds)
            .Where(r => r.ClosedDate == null)
            .Include(r => r.RiskTier)
            .Include(r => r.RiskPriority)
            .Include(r => r.Likelihood)
            .Include(r => r.ImpactLevel)
            .Include(r => r.OwnerUser)
            .Include(r => r.UpdatedByUser)
            .ToListAsync(cancellationToken);

        var issues = await IssuesInBusinessAreas(baIds)
            .Where(i => i.ClosedDate == null)
            .Include(i => i.SeverityLookup)
            .Include(i => i.PriorityLookup)
            .Include(i => i.OwnerUser)
            .Include(i => i.UpdatedByUser)
            .ToListAsync(cancellationToken);

        var reviews = await _db.RaidMonthlyReviews.AsNoTracking()
            .Where(x => x.ReviewYear == year && x.ReviewMonth == month)
            .Include(x => x.ReviewedByUser)
            .ToListAsync(cancellationToken);
        var reviewByKey = reviews.ToDictionary(
            x => $"{x.RecordType}:{x.RecordId}",
            x => x,
            StringComparer.OrdinalIgnoreCase);

        var items = new List<ModernRaidReviewWorkItemVm>();

        foreach (var r in risks)
        {
            reviewByKey.TryGetValue($"risk:{r.Id}", out var rev);
            var opened = r.IdentifiedDate ?? r.CreatedAt;
            items.Add(new ModernRaidReviewWorkItemVm
            {
                Kind = "risk",
                Id = r.Id,
                Reference = $"R-{r.Id:D4}",
                Title = r.Title,
                SeverityOrPriorityLabel = r.RiskPriority?.Label,
                LikelihoodLabel = r.Likelihood?.Label ?? r.LikelihoodRating.ToString(),
                ImpactLabel = r.ImpactLevel?.Label ?? r.ImpactRating.ToString(),
                RiskScore = r.RiskScore,
                Tier = r.RiskTier?.Name,
                OpenedDate = opened,
                Owner = FormatUserDisplay(r.OwnerUser, r.OwnerEmail),
                UpdatedAt = r.UpdatedAt,
                UpdatedBy = FormatUserDisplay(r.UpdatedByUser, null),
                Description = Snippet(r.Description, 2000),
                DetailHref = Url.Action(nameof(RiskDetail), new { id = r.Id }) ?? "#",
                ReviewedThisMonth = rev != null,
                ReviewedAtUtc = rev?.ReviewedAtUtc,
                ExistingMonthlyComment = rev?.MonthlyComment,
                RiskLikelihoodId = r.RiskLikelihoodId,
                RiskImpactLevelId = r.RiskImpactLevelId,
                RiskPriorityId = r.RiskPriorityId
            });
        }

        foreach (var i in issues)
        {
            reviewByKey.TryGetValue($"issue:{i.Id}", out var rev);
            items.Add(new ModernRaidReviewWorkItemVm
            {
                Kind = "issue",
                Id = i.Id,
                Reference = $"I-{i.Id:D4}",
                Title = i.Title,
                SeverityOrPriorityLabel = i.SeverityLookup?.Label ?? i.Severity,
                LikelihoodLabel = null,
                ImpactLabel = i.PriorityLookup?.Label ?? i.Priority,
                RiskScore = null,
                Tier = null,
                OpenedDate = i.DetectedDate,
                Owner = FormatUserDisplay(i.OwnerUser, null),
                UpdatedAt = i.UpdatedAt,
                UpdatedBy = FormatUserDisplay(i.UpdatedByUser, null),
                Description = Snippet(i.Description, 2000),
                DetailHref = Url.Action(nameof(IssueDetail), new { id = i.Id }) ?? "#",
                ReviewedThisMonth = rev != null,
                ReviewedAtUtc = rev?.ReviewedAtUtc,
                ExistingMonthlyComment = rev?.MonthlyComment,
                IssueSeverityId = i.SeverityId,
                IssuePriorityId = i.PriorityId,
                IssueSeveritySortOrder = i.SeverityLookup?.SortOrder,
                IssuePrioritySortOrder = i.PriorityLookup?.SortOrder
            });
        }

        var pendingRisks = SortRisksByScoreDescending(items.Where(x => x.Kind == "risk" && !x.ReviewedThisMonth));
        var pendingIssues = SortIssuesByPriorityAndSeverityDescending(items.Where(x => x.Kind == "issue" && !x.ReviewedThisMonth));
        var reviewedRisks = SortRisksByScoreDescending(items.Where(x => x.Kind == "risk" && x.ReviewedThisMonth));
        var reviewedIssues = SortIssuesByPriorityAndSeverityDescending(items.Where(x => x.Kind == "issue" && x.ReviewedThisMonth));

        var activeKind = ResolveReviewWorkActiveKind(kind, pendingRisks.Count + reviewedRisks.Count, pendingIssues.Count + reviewedIssues.Count);

        var vm = new ModernRaidReviewWorkViewModel
        {
            ReviewMonthLabel = monthLabel,
            ReviewYear = year,
            ReviewMonth = month,
            BusinessAreaQuery = baQuery,
            BusinessAreaLabels = baLabels,
            ActiveKind = activeKind,
            PendingRisks = pendingRisks,
            PendingIssues = pendingIssues,
            ReviewedRisks = reviewedRisks,
            ReviewedIssues = reviewedIssues,
            RiskLikelihoodOptions = await _db.RiskLikelihoods.AsNoTracking()
                .Where(x => x.IsActive).OrderBy(x => x.SortOrder)
                .Select(x => new RaidLookupOptionVm(x.Id, x.Label)).ToListAsync(cancellationToken),
            RiskImpactOptions = await _db.RiskImpactLevels.AsNoTracking()
                .Where(x => x.IsActive).OrderBy(x => x.SortOrder)
                .Select(x => new RaidLookupOptionVm(x.Id, x.Label)).ToListAsync(cancellationToken),
            RiskPriorityOptions = await _db.RiskPriorities.AsNoTracking()
                .Where(x => x.IsActive).OrderBy(x => x.SortOrder)
                .Select(x => new RaidLookupOptionVm(x.Id, x.Label)).ToListAsync(cancellationToken),
            IssueSeverityOptions = await _db.IssueSeverities.AsNoTracking()
                .Where(x => x.IsActive).OrderBy(x => x.SortOrder)
                .Select(x => new RaidLookupOptionVm(x.Id, x.Label)).ToListAsync(cancellationToken),
            IssuePriorityOptions = await _db.IssuePriorities.AsNoTracking()
                .Where(x => x.IsActive).OrderBy(x => x.SortOrder)
                .Select(x => new RaidLookupOptionVm(x.Id, x.Label)).ToListAsync(cancellationToken)
        };

        ViewBag.RaidReviewStorageKey = RaidReviewStorageKey;
        return View("~/Views/Modern/Raid/ReviewWork.cshtml", vm);
    }

    [HttpGet("review/work/item/{kind}/{id:int}/timeline")]
    public async Task<IActionResult> ReviewWorkItemTimeline(string kind, int id, CancellationToken cancellationToken)
    {
        var recordType = kind.ToLowerInvariant();
        if (recordType is not ("risk" or "issue"))
            return NotFound();

        var (year, month, _) = CurrentReviewPeriod();
        var vm = await RaidMonthlyReviewWorkTimelineBuilder.BuildAsync(
            _db, recordType, id, year, month, cancellationToken);
        return PartialView("~/Views/Modern/Raid/_RaidReviewPreviousMonth.cshtml", vm);
    }

    [HttpPost("review/work/risk/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReviewSaveRisk(int id, ModernRaidReviewSaveRiskForm form, CancellationToken cancellationToken)
    {
        var userId = await ResolveCurrentUserIdAsync(cancellationToken);
        if (userId is null)
            return Unauthorized();

        var risk = await _db.Risks.FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, cancellationToken);
        if (risk == null)
            return NotFound();

        var (year, month, _) = CurrentReviewPeriod();

        if (form.RiskLikelihoodId is > 0 || form.RiskImpactLevelId is > 0)
        {
            var likId = form.RiskLikelihoodId > 0 ? form.RiskLikelihoodId : risk.RiskLikelihoodId;
            var impId = form.RiskImpactLevelId > 0 ? form.RiskImpactLevelId : risk.RiskImpactLevelId;
            var (likelihoodRating, impactRating, riskScore, inherentScore) =
                await ComputeRaidRiskScoresAsync(likId, impId, cancellationToken);
            if (form.RiskLikelihoodId is > 0)
            {
                risk.RiskLikelihoodId = form.RiskLikelihoodId;
                risk.LikelihoodRating = likelihoodRating;
            }
            if (form.RiskImpactLevelId is > 0)
            {
                risk.RiskImpactLevelId = form.RiskImpactLevelId;
                risk.ImpactRating = impactRating;
            }
            risk.RiskScore = riskScore;
            risk.InherentScore = inherentScore;
        }

        if (form.RiskPriorityId is > 0)
            risk.RiskPriorityId = form.RiskPriorityId;

        risk.LastReviewDate = DateTime.UtcNow.Date;
        risk.UpdatedAt = DateTime.UtcNow;
        risk.UpdatedByUserId = userId;

        await UpsertMonthlyReviewAsync("risk", id, year, month, userId.Value, form.MonthlyComment, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        TempData["Message"] = $"R-{id:D4} marked as reviewed for {CultureInfo.GetCultureInfo("en-GB").DateTimeFormat.GetMonthName(month)}.";
        return RedirectToAction(nameof(ReviewWork), ReviewWorkRoute(form.Ba, "risk"));
    }

    [HttpPost("review/work/issue/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReviewSaveIssue(int id, ModernRaidReviewSaveIssueForm form, CancellationToken cancellationToken)
    {
        var userId = await ResolveCurrentUserIdAsync(cancellationToken);
        if (userId is null)
            return Unauthorized();

        var issue = await _db.Issues.FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted, cancellationToken);
        if (issue == null)
            return NotFound();

        var (year, month, _) = CurrentReviewPeriod();

        if (form.IssueSeverityId is > 0)
        {
            var sev = await _db.IssueSeverities.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == form.IssueSeverityId.Value && x.IsActive, cancellationToken);
            if (sev != null)
            {
                issue.SeverityId = sev.Id;
                issue.Severity = TruncateLowerRaid(sev.Label, 10);
            }
        }

        if (form.IssuePriorityId is > 0)
        {
            var pri = await _db.IssuePriorities.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == form.IssuePriorityId.Value && x.IsActive, cancellationToken);
            if (pri != null)
            {
                issue.PriorityId = pri.Id;
                issue.Priority = TruncateRaid(pri.Label, 10);
            }
        }

        issue.UpdatedAt = DateTime.UtcNow;
        issue.UpdatedByUserId = userId;

        await UpsertMonthlyReviewAsync("issue", id, year, month, userId.Value, form.MonthlyComment, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        TempData["Message"] = $"I-{id:D4} marked as reviewed for {CultureInfo.GetCultureInfo("en-GB").DateTimeFormat.GetMonthName(month)}.";
        return RedirectToAction(nameof(ReviewWork), ReviewWorkRoute(form.Ba, "issue"));
    }

    [HttpPost("review/work/risk/{id:int}/close")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReviewCloseRisk(int id, ModernRaidReviewCloseForm form, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(form.ClosureComment))
        {
            TempData["Error"] = "Enter a closure comment before closing the risk.";
            return RedirectToAction(nameof(ReviewWork), ReviewWorkRoute(form.Ba, "risk"));
        }

        var userId = await ResolveCurrentUserIdAsync(cancellationToken);
        if (userId is null)
            return Unauthorized();

        var risk = await _db.Risks.FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, cancellationToken);
        if (risk == null)
            return NotFound();
        if (risk.ClosedDate.HasValue)
        {
            TempData["Error"] = $"R-{id:D4} is already closed.";
            return RedirectToAction(nameof(ReviewWork), ReviewWorkRoute(form.Ba, "risk"));
        }

        var closeNow = DateTime.UtcNow;
        var comment = form.ClosureComment.Trim();
        if (comment.Length > 4000)
            comment = comment[..4000];

        risk.ClosedDate = closeNow;
        risk.LastReviewDate = closeNow.Date;
        risk.UpdatedAt = closeNow;
        risk.UpdatedByUserId = userId;
        AppendRaidClosureNote(risk, comment, closeNow);

        var closedStatusId = await ResolveRaidRiskStatusIdByCodeAsync("CLOSED", cancellationToken)
            ?? await ResolveRaidRiskStatusIdByLabelAsync("closed", cancellationToken);
        if (closedStatusId.HasValue)
        {
            risk.RiskStatusId = closedStatusId;
            var statusRow = await _db.RiskStatuses.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == closedStatusId.Value, cancellationToken);
            if (statusRow != null)
                risk.Status = TruncateLowerRaid(statusRow.Label, 20);
        }

        var (year, month, _) = CurrentReviewPeriod();
        await UpsertMonthlyReviewAsync("risk", id, year, month, userId.Value, comment, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        TempData["Message"] = $"R-{id:D4} closed.";
        return RedirectToAction(nameof(ReviewWork), ReviewWorkRoute(form.Ba, "risk"));
    }

    [HttpPost("review/work/issue/{id:int}/close")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReviewCloseIssue(int id, ModernRaidReviewCloseForm form, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(form.ClosureComment))
        {
            TempData["Error"] = "Enter a closure comment before closing the issue.";
            return RedirectToAction(nameof(ReviewWork), ReviewWorkRoute(form.Ba, "issue"));
        }

        var userId = await ResolveCurrentUserIdAsync(cancellationToken);
        if (userId is null)
            return Unauthorized();

        var issue = await _db.Issues.FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted, cancellationToken);
        if (issue == null)
            return NotFound();
        if (issue.ClosedDate.HasValue)
        {
            TempData["Error"] = $"I-{id:D4} is already closed.";
            return RedirectToAction(nameof(ReviewWork), ReviewWorkRoute(form.Ba, "issue"));
        }

        var closeNow = DateTime.UtcNow;
        var comment = form.ClosureComment.Trim();
        if (comment.Length > 4000)
            comment = comment[..4000];

        issue.ClosedDate = closeNow;
        issue.ResolutionSummary = comment;
        issue.UpdatedAt = closeNow;
        issue.UpdatedByUserId = userId;

        var closedStatusId = await ResolveRaidIssueStatusIdByCodeAsync("CLOSED", cancellationToken)
            ?? await ResolveRaidIssueStatusIdByLabelAsync("closed", cancellationToken);
        if (closedStatusId.HasValue)
        {
            issue.StatusId = closedStatusId;
            var statusRow = await _db.IssueStatuses.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == closedStatusId.Value, cancellationToken);
            if (statusRow != null)
                issue.Status = TruncateLowerRaid(statusRow.Label, 20);
        }

        var (year, month, _) = CurrentReviewPeriod();
        await UpsertMonthlyReviewAsync("issue", id, year, month, userId.Value, comment, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        TempData["Message"] = $"I-{id:D4} closed.";
        return RedirectToAction(nameof(ReviewWork), ReviewWorkRoute(form.Ba, "issue"));
    }

    private static string ResolveReviewWorkActiveKind(string? kind, int riskCount, int issueCount)
    {
        if (string.Equals(kind, "issues", StringComparison.OrdinalIgnoreCase)
            || string.Equals(kind, "issue", StringComparison.OrdinalIgnoreCase))
            return "issue";
        if (string.Equals(kind, "risks", StringComparison.OrdinalIgnoreCase)
            || string.Equals(kind, "risk", StringComparison.OrdinalIgnoreCase))
            return "risk";
        if (riskCount == 0 && issueCount > 0)
            return "issue";
        return "risk";
    }

    private static object ReviewWorkRoute(string? ba, string activeKind) =>
        new { ba, kind = activeKind == "issue" ? "issues" : "risks" };

    private async Task<int?> ResolveRaidRiskStatusIdByCodeAsync(string code, CancellationToken cancellationToken) =>
        await _db.RiskStatuses.AsNoTracking()
            .Where(x => x.IsActive && x.Code.ToLower() == code.ToLower())
            .OrderBy(x => x.SortOrder)
            .Select(x => (int?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);

    private async Task<int?> ResolveRaidRiskStatusIdByLabelAsync(string labelFragment, CancellationToken cancellationToken) =>
        await _db.RiskStatuses.AsNoTracking()
            .Where(x => x.IsActive && x.Label.ToLower().Contains(labelFragment.ToLower()))
            .OrderBy(x => x.SortOrder)
            .Select(x => (int?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);

    private async Task<int?> ResolveRaidIssueStatusIdByCodeAsync(string code, CancellationToken cancellationToken) =>
        await _db.IssueStatuses.AsNoTracking()
            .Where(x => x.IsActive && x.Code.ToLower() == code.ToLower())
            .OrderBy(x => x.SortOrder)
            .Select(x => (int?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);

    private async Task<int?> ResolveRaidIssueStatusIdByLabelAsync(string labelFragment, CancellationToken cancellationToken) =>
        await _db.IssueStatuses.AsNoTracking()
            .Where(x => x.IsActive && x.Label.ToLower().Contains(labelFragment.ToLower()))
            .OrderBy(x => x.SortOrder)
            .Select(x => (int?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);

    private static void AppendRaidClosureNote(Risk risk, string comment, DateTime closedAtUtc)
    {
        var line = $"Closed {closedAtUtc:dd MMM yyyy}: {comment}";
        risk.Notes = string.IsNullOrWhiteSpace(risk.Notes)
            ? line
            : risk.Notes.Trim() + "\n\n" + line;
    }

    private async Task UpsertMonthlyReviewAsync(
        string recordType,
        int recordId,
        int year,
        int month,
        int userId,
        string? comment,
        CancellationToken ct)
    {
        var existing = await _db.RaidMonthlyReviews
            .FirstOrDefaultAsync(x =>
                x.RecordType == recordType &&
                x.RecordId == recordId &&
                x.ReviewYear == year &&
                x.ReviewMonth == month, ct);

        var trimmed = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
        if (trimmed?.Length > 4000)
            trimmed = trimmed[..4000];

        if (existing == null)
        {
            _db.RaidMonthlyReviews.Add(new RaidMonthlyReview
            {
                RecordType = recordType,
                RecordId = recordId,
                ReviewYear = year,
                ReviewMonth = month,
                ReviewedByUserId = userId,
                ReviewedAtUtc = DateTime.UtcNow,
                MonthlyComment = trimmed
            });
        }
        else
        {
            existing.ReviewedByUserId = userId;
            existing.ReviewedAtUtc = DateTime.UtcNow;
            existing.MonthlyComment = trimmed;
        }
    }

    private static List<ModernRaidReviewWorkItemVm> SortRisksByScoreDescending(IEnumerable<ModernRaidReviewWorkItemVm> items) =>
        items
            .OrderByDescending(x => x.RiskScore ?? 0)
            .ThenBy(x => x.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static List<ModernRaidReviewWorkItemVm> SortIssuesByPriorityAndSeverityDescending(
        IEnumerable<ModernRaidReviewWorkItemVm> items) =>
        items
            .OrderByDescending(x => x.IssuePrioritySortOrder ?? int.MinValue)
            .ThenByDescending(x => x.IssueSeveritySortOrder ?? int.MinValue)
            .ThenByDescending(x => IssueSeveritySortWeight(x.SeverityOrPriorityLabel))
            .ThenBy(x => x.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static int IssueSeveritySortWeight(string? label)
    {
        var ll = (label ?? "").ToLowerInvariant();
        if (ll.Contains("critical")) return 4;
        if (ll.Contains("high")) return 3;
        if (ll.Contains("medium")) return 2;
        if (ll.Contains("low")) return 1;
        return 0;
    }

    private static string? FormatUserDisplay(User? user, string? fallbackEmail)
    {
        if (user != null)
        {
            if (!string.IsNullOrWhiteSpace(user.Name))
                return user.Name.Trim();
            if (!string.IsNullOrWhiteSpace(user.Email))
                return user.Email.Trim();
        }
        return string.IsNullOrWhiteSpace(fallbackEmail) ? null : fallbackEmail.Trim();
    }

}
