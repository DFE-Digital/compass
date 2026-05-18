using Compass.Data;
using Compass.Models;
using Compass.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Compass.Services;

/// <summary>Builds the RAID monthly review progress reporting dashboard.</summary>
public class ModernRaidReviewProgressService
{
    private readonly CompassDbContext _db;
    private readonly IReturnStatusService _returnStatus;

    public ModernRaidReviewProgressService(CompassDbContext db, IReturnStatusService returnStatus)
    {
        _db = db;
        _returnStatus = returnStatus;
    }

    public async Task<ModernRaidReviewProgressViewModel> BuildAsync(
        int? year,
        int? month,
        int? businessAreaId,
        int? directorateId,
        CancellationToken cancellationToken = default)
    {
        var currentDate = DateTime.UtcNow;
        var calendarYearUtc = currentDate.Year;
        var currentMonth = currentDate.Month;

        const int minReportYear = 2026;
        var maxSelectableYear = calendarYearUtc >= minReportYear ? calendarYearUtc : minReportYear;

        var defaultReportYear = calendarYearUtc;
        var defaultReportMonth = currentMonth;
        defaultReportYear = Math.Max(minReportYear, defaultReportYear);

        var reportYear = year ?? defaultReportYear;
        var reportMonth = month ?? defaultReportMonth;
        if (reportMonth < 1 || reportMonth > 12)
            reportMonth = defaultReportMonth;
        reportYear = Math.Clamp(reportYear, minReportYear, maxSelectableYear);

        var monthStart = new DateTime(reportYear, reportMonth, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);
        var reviewWindowStart = monthStart;
        var reviewWindowEnd = _returnStatus.GetLastWorkingDayOfMonth(reportYear, reportMonth).Date;
        if (reviewWindowEnd < reviewWindowStart)
            reviewWindowEnd = reviewWindowStart;

        var reviewDueLabel = reviewWindowEnd.ToString("dddd d MMMM yyyy",
            System.Globalization.CultureInfo.GetCultureInfo("en-GB"));

        var businessAreas = await _db.BusinessAreaLookups
            .AsNoTracking()
            .Where(ba => ba.IsActive)
            .OrderBy(ba => ba.SortOrder)
            .ThenBy(ba => ba.Name)
            .ToListAsync(cancellationToken);

        var directorates = await _db.Divisions
            .AsNoTracking()
            .Where(d => d.IsActive)
            .OrderBy(d => d.SortOrder)
            .ThenBy(d => d.Name)
            .ToListAsync(cancellationToken);

        var openRisks = await LoadOpenRisksAsync(businessAreaId, directorateId, cancellationToken);
        var openIssues = await LoadOpenIssuesAsync(businessAreaId, directorateId, cancellationToken);

        var reviewedRiskIds = await _db.RaidMonthlyReviews.AsNoTracking()
            .Where(x => x.RecordType == "risk" && x.ReviewYear == reportYear && x.ReviewMonth == reportMonth)
            .Select(x => x.RecordId)
            .ToListAsync(cancellationToken);
        var reviewedIssueIds = await _db.RaidMonthlyReviews.AsNoTracking()
            .Where(x => x.RecordType == "issue" && x.ReviewYear == reportYear && x.ReviewMonth == reportMonth)
            .Select(x => x.RecordId)
            .ToListAsync(cancellationToken);
        var reviewedRiskSet = reviewedRiskIds.ToHashSet();
        var reviewedIssueSet = reviewedIssueIds.ToHashSet();

        var riskItems = openRisks.Select(r => ToScopeItem(
            "risk",
            r.Id,
            ResolveBusinessAreaIds(r.RiskBusinessAreas.Select(b => b.BusinessAreaLookupId), r.Project?.BusinessAreaId),
            ResolveDivisionIds(r.RiskDivisions.Select(d => d.DivisionId), r.Project?.Directorates),
            reviewedRiskSet.Contains(r.Id))).ToList();

        var issueItems = openIssues.Select(i => ToScopeItem(
            "issue",
            i.Id,
            ResolveBusinessAreaIds(i.IssueBusinessAreas.Select(b => b.BusinessAreaLookupId), i.Project?.BusinessAreaId),
            ResolveDivisionIds(i.IssueDivisions.Select(d => d.DivisionId), i.Project?.Directorates),
            reviewedIssueSet.Contains(i.Id))).ToList();

        var allItems = riskItems.Concat(issueItems).ToList();

        var summary = new RaidReviewProgressSummary
        {
            OpenRisks = riskItems.Count,
            OpenIssues = issueItems.Count,
            ReviewedRisks = riskItems.Count(i => i.IsReviewed),
            ReviewedIssues = issueItems.Count(i => i.IsReviewed)
        };
        summary.ActualProgressPercent = summary.TotalOpen == 0
            ? 0
            : Math.Round(100m * summary.TotalReviewed / summary.TotalOpen, 1, MidpointRounding.AwayFromZero);

        var expectedProgressToday = ComputeExpectedProgressPercent(reviewWindowStart, reviewWindowEnd, DateTime.UtcNow.Date);

        var openRiskIdSet = openRisks.Select(r => r.Id).ToHashSet();
        var openIssueIdSet = openIssues.Select(i => i.Id).ToHashSet();
        var reviewDates = await _db.RaidMonthlyReviews.AsNoTracking()
            .Where(x => x.ReviewYear == reportYear && x.ReviewMonth == reportMonth &&
                ((x.RecordType == "risk" && openRiskIdSet.Contains(x.RecordId)) ||
                 (x.RecordType == "issue" && openIssueIdSet.Contains(x.RecordId))))
            .Select(x => x.ReviewedAtUtc.Date)
            .OrderBy(d => d)
            .ToListAsync(cancellationToken);

        var dailyProgress = BuildDailyReviewProgress(
            summary.TotalOpen,
            reviewWindowStart,
            reviewWindowEnd,
            reviewDates);

        var baNameById = businessAreas.ToDictionary(b => b.Id, b => b.Name);
        var dirNameById = directorates.ToDictionary(d => d.Id, d => d.Name);

        var businessAreaLeague = BuildLeagueByBusinessArea(allItems, baNameById, expectedProgressToday);
        var directorateLeague = BuildLeagueByDirectorate(allItems, dirNameById, expectedProgressToday);

        var nextMonthDate = monthStart.AddMonths(1);
        var nextMonthAllowed =
            (nextMonthDate.Year < defaultReportYear ||
             (nextMonthDate.Year == defaultReportYear && nextMonthDate.Month <= defaultReportMonth)) &&
            nextMonthDate.Year <= calendarYearUtc;
        var prevMonthDate = monthStart.AddMonths(-1);
        var earliestReportPeriod = new DateTime(minReportYear, 1, 1);
        var hasPreviousMonthNav = prevMonthDate >= earliestReportPeriod;

        return new ModernRaidReviewProgressViewModel
        {
            ReportYear = reportYear,
            ReportMonth = reportMonth,
            MonthName = monthStart.ToString("MMMM yyyy"),
            MonthStart = monthStart,
            MonthEnd = monthEnd,
            DefaultReportYear = defaultReportYear,
            DefaultReportMonth = defaultReportMonth,
            MinReportYear = minReportYear,
            MaxReportYear = maxSelectableYear,
            FilterBusinessAreaId = businessAreaId,
            FilterDirectorateId = directorateId,
            BusinessAreas = businessAreas,
            Directorates = directorates,
            Summary = summary,
            ReviewWindowStart = reviewWindowStart,
            ReviewWindowEnd = reviewWindowEnd,
            ReviewDueLabel = reviewDueLabel,
            ExpectedProgressPercentToday = expectedProgressToday,
            DailyProgress = dailyProgress,
            BusinessAreaLeague = businessAreaLeague,
            DirectorateLeague = directorateLeague,
            HasPreviousMonthNav = hasPreviousMonthNav,
            HasNextMonthNav = nextMonthAllowed,
            PreviousNavYear = hasPreviousMonthNav ? prevMonthDate.Year : null,
            PreviousNavMonth = hasPreviousMonthNav ? prevMonthDate.Month : null,
            NextNavYear = nextMonthAllowed ? nextMonthDate.Year : null,
            NextNavMonth = nextMonthAllowed ? nextMonthDate.Month : null
        };
    }

    private async Task<List<Risk>> LoadOpenRisksAsync(
        int? businessAreaId,
        int? directorateId,
        CancellationToken cancellationToken)
    {
        var query = _db.Risks.AsNoTracking()
            .Include(r => r.RiskBusinessAreas)
            .Include(r => r.RiskDivisions)
            .Include(r => r.Project)
                .ThenInclude(p => p!.Directorates)
            .Where(r => !r.IsDeleted && r.ClosedDate == null);

        if (businessAreaId is { } ba)
        {
            query = query.Where(r =>
                r.RiskBusinessAreas.Any(b => b.BusinessAreaLookupId == ba) ||
                (r.ProjectId != null && r.Project!.BusinessAreaId == ba));
        }

        if (directorateId is { } dir)
        {
            query = query.Where(r =>
                r.RiskDivisions.Any(d => d.DivisionId == dir) ||
                (r.ProjectId != null && r.Project!.Directorates.Any(pd => pd.DivisionId == dir)));
        }

        return await query.ToListAsync(cancellationToken);
    }

    private async Task<List<Issue>> LoadOpenIssuesAsync(
        int? businessAreaId,
        int? directorateId,
        CancellationToken cancellationToken)
    {
        var query = _db.Issues.AsNoTracking()
            .Include(i => i.IssueBusinessAreas)
            .Include(i => i.IssueDivisions)
            .Include(i => i.Project)
                .ThenInclude(p => p!.Directorates)
            .Where(i => !i.IsDeleted && i.ClosedDate == null);

        if (businessAreaId is { } ba)
        {
            query = query.Where(i =>
                i.IssueBusinessAreas.Any(b => b.BusinessAreaLookupId == ba) ||
                (i.ProjectId != null && i.Project!.BusinessAreaId == ba));
        }

        if (directorateId is { } dir)
        {
            query = query.Where(i =>
                i.IssueDivisions.Any(d => d.DivisionId == dir) ||
                (i.ProjectId != null && i.Project!.Directorates.Any(pd => pd.DivisionId == dir)));
        }

        return await query.ToListAsync(cancellationToken);
    }

    private static List<int> ResolveBusinessAreaIds(IEnumerable<int> fromJunction, int? projectBusinessAreaId)
    {
        var ids = fromJunction.Distinct().ToList();
        if (ids.Count == 0 && projectBusinessAreaId is > 0)
            ids.Add(projectBusinessAreaId.Value);
        return ids;
    }

    private static List<int> ResolveDivisionIds(
        IEnumerable<int> fromJunction,
        ICollection<ProjectDirectorate>? projectDirectorates)
    {
        var ids = fromJunction.Distinct().ToList();
        if (ids.Count == 0 && projectDirectorates != null)
        {
            ids.AddRange(projectDirectorates.Select(d => d.DivisionId).Distinct());
        }
        return ids;
    }

    private static RaidReviewScopeItem ToScopeItem(
        string recordType,
        int recordId,
        IReadOnlyList<int> businessAreaIds,
        IReadOnlyList<int> divisionIds,
        bool isReviewed) =>
        new(recordType, recordId, businessAreaIds, divisionIds, isReviewed);

    private static int? GetPrimaryBusinessAreaId(IReadOnlyList<int> ids) =>
        ids.Count == 0 ? null : ids.OrderBy(id => id).First();

    private static int? GetPrimaryDivisionId(IReadOnlyList<int> ids) =>
        ids.Count == 0 ? null : ids.OrderBy(id => id).First();

    private static List<RaidReviewLeagueRow> BuildLeagueByBusinessArea(
        IReadOnlyList<RaidReviewScopeItem> items,
        IReadOnlyDictionary<int, string> baNameById,
        decimal expectedProgressPercent)
    {
        return items
            .GroupBy(i => GetPrimaryBusinessAreaId(i.BusinessAreaIds))
            .Select(g =>
            {
                var name = g.Key is int id && baNameById.TryGetValue(id, out var n) ? n : "Not set";
                return BuildLeagueRow(name, g.Key, g.ToList(), expectedProgressPercent);
            })
            .OrderByDescending(r => r.ActualProgressPercent)
            .ThenBy(r => r.Name == "Not set" ? "zzzzzz" : r.Name)
            .ToList();
    }

    private static List<RaidReviewLeagueRow> BuildLeagueByDirectorate(
        IReadOnlyList<RaidReviewScopeItem> items,
        IReadOnlyDictionary<int, string> dirNameById,
        decimal expectedProgressPercent)
    {
        return items
            .GroupBy(i => GetPrimaryDivisionId(i.DivisionIds))
            .Select(g =>
            {
                var name = g.Key is int id && dirNameById.TryGetValue(id, out var n) ? n : "Not set";
                return BuildLeagueRow(name, g.Key, g.ToList(), expectedProgressPercent);
            })
            .OrderByDescending(r => r.ActualProgressPercent)
            .ThenBy(r => r.Name == "Not set" ? "zzzzzz" : r.Name)
            .ToList();
    }

    private static RaidReviewLeagueRow BuildLeagueRow(
        string name,
        int? entityId,
        IReadOnlyList<RaidReviewScopeItem> items,
        decimal expectedProgressPercent)
    {
        var risks = items.Where(i => i.RecordType == "risk").ToList();
        var issues = items.Where(i => i.RecordType == "issue").ToList();
        var reviewed = items.Count(i => i.IsReviewed);
        var total = items.Count;
        var actual = total == 0 ? 0 : Math.Round(100m * reviewed / total, 1, MidpointRounding.AwayFromZero);

        return new RaidReviewLeagueRow
        {
            Name = name,
            EntityId = entityId,
            OpenRisks = risks.Count,
            OpenIssues = issues.Count,
            ReviewedRisks = risks.Count(r => r.IsReviewed),
            ReviewedIssues = issues.Count(i => i.IsReviewed),
            ActualProgressPercent = actual,
            ExpectedProgressPercent = expectedProgressPercent
        };
    }

    private static decimal ComputeExpectedProgressPercent(DateTime windowStart, DateTime windowEnd, DateTime asOfDate)
    {
        if (windowEnd < windowStart)
            return 100m;

        var totalDays = (windowEnd - windowStart).Days + 1;
        if (totalDays <= 0)
            return 100m;

        if (asOfDate < windowStart)
            return 0m;
        if (asOfDate >= windowEnd)
            return 100m;

        var elapsedDays = (asOfDate - windowStart).Days + 1;
        return Math.Round(100m * elapsedDays / totalDays, 1, MidpointRounding.AwayFromZero);
    }

    private static List<SubmissionProgressDayPoint> BuildDailyReviewProgress(
        int totalInScope,
        DateTime windowStart,
        DateTime windowEnd,
        IReadOnlyList<DateTime> reviewDates)
    {
        var points = new List<SubmissionProgressDayPoint>();
        if (windowEnd < windowStart)
            return points;

        var totalDays = (windowEnd - windowStart).Days + 1;
        var reviewIndex = 0;
        var cumulative = 0;

        for (var day = windowStart; day <= windowEnd; day = day.AddDays(1))
        {
            while (reviewIndex < reviewDates.Count && reviewDates[reviewIndex] <= day)
            {
                cumulative++;
                reviewIndex++;
            }

            var dayNumber = (day - windowStart).Days + 1;
            var expected = totalInScope == 0 || totalDays == 0
                ? 0m
                : Math.Round((decimal)totalInScope * dayNumber / totalDays, 1, MidpointRounding.AwayFromZero);
            var expectedPercent = totalDays == 0
                ? 0m
                : Math.Round(100m * dayNumber / totalDays, 1, MidpointRounding.AwayFromZero);
            var actualPercent = totalInScope == 0
                ? 0m
                : Math.Round(100m * cumulative / totalInScope, 1, MidpointRounding.AwayFromZero);

            points.Add(new SubmissionProgressDayPoint
            {
                Label = day.ToString("d MMM"),
                Date = day,
                ActualCumulative = cumulative,
                ExpectedCumulative = expected,
                ActualCompletionPercent = actualPercent,
                ExpectedCompletionPercent = expectedPercent,
                TotalInScope = totalInScope
            });
        }

        return points;
    }

    private sealed record RaidReviewScopeItem(
        string RecordType,
        int RecordId,
        IReadOnlyList<int> BusinessAreaIds,
        IReadOnlyList<int> DivisionIds,
        bool IsReviewed);
}
