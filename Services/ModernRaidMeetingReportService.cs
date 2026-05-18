using Compass.Controllers.Modern;
using Compass.Data;
using Compass.Models;
using Compass.ViewModels;
using Compass.ViewModels.Modern;
using Microsoft.EntityFrameworkCore;

namespace Compass.Services;

/// <summary>Builds the meeting-oriented RAID report at <c>/modern/reporting/raid</c>.</summary>
public sealed class ModernRaidMeetingReportService
{
    private readonly CompassDbContext _db;
    private readonly ModernRaidReportingService _reporting;
    private readonly ModernRaidReviewProgressService _reviewProgress;

    public ModernRaidMeetingReportService(
        CompassDbContext db,
        ModernRaidReportingService reporting,
        ModernRaidReviewProgressService reviewProgress)
    {
        _db = db;
        _reporting = reporting;
        _reviewProgress = reviewProgress;
    }

    public async Task<ModernRaidMeetingReportViewModel> BuildAsync(
        int? year,
        int? month,
        int? businessAreaId,
        int? directorateId,
        CancellationToken cancellationToken = default)
    {
        var reviewVm = await _reviewProgress.BuildAsync(year, month, businessAreaId, directorateId, cancellationToken);
        var reporting = await _reporting.BuildAsync("risks", "patterns", businessAreaId, directorateId, _db, cancellationToken);

        var headline = await BuildHeadlineAsync(businessAreaId, directorateId, cancellationToken);
        var assumptions = await BuildAssumptionsSectionAsync(businessAreaId, directorateId, cancellationToken);
        var dependencies = await BuildDependenciesSectionAsync(businessAreaId, directorateId, cancellationToken);
        var nearMisses = await BuildNearMissesSectionAsync(businessAreaId, directorateId, cancellationToken);

        static string NavUrl(int? y, int? mo, int? ba, int? dir)
        {
            var parts = new List<string>();
            if (y.HasValue) parts.Add($"year={y.Value}");
            if (mo.HasValue) parts.Add($"month={mo.Value}");
            if (ba.HasValue) parts.Add($"businessAreaId={ba.Value}");
            if (dir.HasValue) parts.Add($"directorateId={dir.Value}");
            return parts.Count == 0 ? "/modern/reporting/raid" : "/modern/reporting/raid?" + string.Join("&", parts);
        }

        var periodToolbar = MonthlyReportPeriodToolbarViewModel.FromRaidMeetingReport(
            reviewVm,
            "/modern/reporting/raid",
            NavUrl,
            $"Review due by {reviewVm.ReviewDueLabel}");

        return new ModernRaidMeetingReportViewModel
        {
            PeriodToolbar = periodToolbar,
            FilterScopeSummary = reporting.FilterScopeSummary,
            FilterBusinessAreaId = businessAreaId,
            FilterDirectorateId = directorateId,
            Headline = headline,
            Review = reviewVm.Summary,
            ExpectedProgressPercent = reviewVm.ExpectedProgressPercentToday,
            ReviewDueLabel = reviewVm.ReviewDueLabel,
            ReviewWindowStart = reviewVm.ReviewWindowStart,
            ReviewWindowEnd = reviewVm.ReviewWindowEnd,
            Reporting = reporting,
            Assumptions = assumptions,
            Dependencies = dependencies,
            NearMisses = nearMisses
        };
    }

    private async Task<MonthlyReportRaidSummary> BuildHeadlineAsync(
        int? businessAreaId,
        int? directorateId,
        CancellationToken cancellationToken)
    {
        var riskQuery = _db.Risks.AsNoTracking().Where(r => !r.IsDeleted && r.ClosedDate == null);
        var issueQuery = _db.Issues.AsNoTracking().Where(i => !i.IsDeleted && i.ClosedDate == null);
        riskQuery = ApplyRiskScope(riskQuery, businessAreaId, directorateId);
        issueQuery = ApplyIssueScope(issueQuery, businessAreaId, directorateId);

        var today = DateTime.UtcNow.Date;
        var criticalSeverityIds = await _db.IssueSeverities.AsNoTracking()
            .Where(s => s.IsActive && s.Label == "Critical")
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);

        var nearMissOpen = await CountOpenNearMissesAsync(businessAreaId, directorateId, cancellationToken);

        return new MonthlyReportRaidSummary
        {
            OpenRisks = await riskQuery.CountAsync(cancellationToken),
            OpenIssues = await issueQuery.CountAsync(cancellationToken),
            OpenNearMisses = nearMissOpen,
            HighRisks = await riskQuery.CountAsync(r => r.RiskScore >= 15, cancellationToken),
            RisksReviewOverdue = await riskQuery.CountAsync(
                r => r.NextReviewDate.HasValue && r.NextReviewDate.Value < today,
                cancellationToken),
            OpenCriticalIssues = criticalSeverityIds.Count == 0
                ? 0
                : await issueQuery.CountAsync(
                    i => i.SeverityId.HasValue && criticalSeverityIds.Contains(i.SeverityId.Value),
                    cancellationToken)
        };
    }

    private async Task<RaidMeetingAssumptionsSection> BuildAssumptionsSectionAsync(
        int? businessAreaId,
        int? directorateId,
        CancellationToken cancellationToken)
    {
        var closedStatusIds = await _db.AssumptionStatuses.AsNoTracking()
            .Where(s => s.IsActive && s.Code == "CLOSED")
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);

        var query = _db.Assumptions.AsNoTracking()
            .Where(a => !a.IsDeleted)
            .Where(a => !a.AssumptionStatusId.HasValue || !closedStatusIds.Contains(a.AssumptionStatusId.Value));

        query = ApplyAssumptionScope(query, businessAreaId, directorateId);

        var today = DateTime.UtcNow.Date;
        var openCount = await query.CountAsync(cancellationToken);
        var reviewOverdue = await query.CountAsync(
            a => a.ReviewDate.HasValue && a.ReviewDate.Value < today,
            cancellationToken);

        var statusCounts = await query
            .GroupBy(a => a.StatusLookup != null ? a.StatusLookup.Label : "Not set")
            .Select(g => new { Label = g.Key ?? "Not set", Count = g.Count() })
            .ToDictionaryAsync(x => x.Label, x => x.Count, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var criticalityCounts = await query
            .GroupBy(a => a.CriticalityLookup != null ? a.CriticalityLookup.Label : "Not set")
            .Select(g => new { Label = g.Key ?? "Not set", Count = g.Count() })
            .ToDictionaryAsync(x => x.Label, x => x.Count, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var priority = await query
            .Include(a => a.CriticalityLookup)
            .Include(a => a.StatusLookup)
            .OrderByDescending(a => a.CriticalityLookup != null ? a.CriticalityLookup.SortOrder : 0)
            .ThenBy(a => a.ReviewDate ?? DateTime.MaxValue)
            .Take(12)
            .Select(a => new
            {
                a.Id,
                a.Description,
                Status = a.StatusLookup != null ? a.StatusLookup.Label : null,
                Criticality = a.CriticalityLookup != null ? a.CriticalityLookup.Label : null,
                a.ReviewDate
            })
            .ToListAsync(cancellationToken);

        var rows = priority.Select(a =>
        {
            var title = Truncate(a.Description, 120);
            var meta = string.Join(" · ", new[] { a.Criticality, a.Status }.Where(x => !string.IsNullOrWhiteSpace(x)));
            var insight = a.ReviewDate.HasValue && a.ReviewDate.Value < today
                ? $"Review overdue (due {a.ReviewDate.Value:d MMM yyyy})"
                : a.ReviewDate.HasValue
                    ? $"Review due {a.ReviewDate.Value:d MMM yyyy}"
                    : "No review date set";
            return new RaidMeetingTableRow(a.Id, title, $"ASM-{a.Id}", meta, insight, nameof(ModernRaidController.AssumptionDetail));
        }).ToList();

        return new RaidMeetingAssumptionsSection
        {
            OpenCount = openCount,
            ReviewOverdueCount = reviewOverdue,
            StatusCounts = statusCounts,
            CriticalityCounts = criticalityCounts,
            PriorityRows = rows
        };
    }

    private async Task<RaidMeetingDependenciesSection> BuildDependenciesSectionAsync(
        int? businessAreaId,
        int? directorateId,
        CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var dueSoonEnd = today.AddDays(30);

        var query = _db.Dependencies.AsNoTracking()
            .Where(d => d.Status == "Active" || (d.Status != "Resolved" && d.Status != "Cancelled" && d.ResolvedDate == null));

        if (businessAreaId != null || directorateId != null)
        {
            var scopedRiskIds = await ScopedOpenRiskIdsAsync(businessAreaId, directorateId, cancellationToken);
            var scopedIssueIds = await ScopedOpenIssueIdsAsync(businessAreaId, directorateId, cancellationToken);
            var scopedProjectIds = await ScopedProjectIdsAsync(businessAreaId, directorateId, cancellationToken);
            query = query.Where(d =>
                (d.SourceEntityType == "Risk" && scopedRiskIds.Contains(d.SourceEntityId))
                || (d.TargetEntityType == "Risk" && scopedRiskIds.Contains(d.TargetEntityId))
                || (d.SourceEntityType == "Issue" && scopedIssueIds.Contains(d.SourceEntityId))
                || (d.TargetEntityType == "Issue" && scopedIssueIds.Contains(d.TargetEntityId))
                || (d.SourceEntityType == "Project" && scopedProjectIds.Contains(d.SourceEntityId))
                || (d.TargetEntityType == "Project" && scopedProjectIds.Contains(d.TargetEntityId)));
        }

        var activeCount = await query.CountAsync(cancellationToken);
        var overdue = await query.CountAsync(d => d.DueDate.HasValue && d.DueDate.Value < today, cancellationToken);
        var dueSoon = await query.CountAsync(
            d => d.DueDate.HasValue && d.DueDate.Value >= today && d.DueDate.Value <= dueSoonEnd,
            cancellationToken);

        var criticalityCounts = await query
            .GroupBy(d => d.CriticalityLookup != null ? d.CriticalityLookup.Label : "Not set")
            .Select(g => new { Label = g.Key ?? "Not set", Count = g.Count() })
            .ToDictionaryAsync(x => x.Label, x => x.Count, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var priority = await query
            .Include(d => d.CriticalityLookup)
            .Include(d => d.LinkTypeLookup)
            .OrderBy(d => d.DueDate ?? DateTime.MaxValue)
            .ThenByDescending(d => d.CriticalityLookup != null ? d.CriticalityLookup.SortOrder : 0)
            .Take(12)
            .Select(d => new
            {
                d.Id,
                d.Description,
                d.SourceEntityType,
                d.SourceEntityId,
                d.TargetEntityType,
                d.TargetEntityId,
                Link = d.LinkTypeLookup != null ? d.LinkTypeLookup.Label : d.DependencyType,
                Criticality = d.CriticalityLookup != null ? d.CriticalityLookup.Label : null,
                d.DueDate,
                d.Organisation
            })
            .ToListAsync(cancellationToken);

        var rows = priority.Select(d =>
        {
            var title = Truncate(d.Description ?? $"{d.SourceEntityType} {d.SourceEntityId} → {d.TargetEntityType} {d.TargetEntityId}", 100);
            var meta = string.Join(" · ", new[] { d.Link, d.Criticality, d.Organisation }.Where(x => !string.IsNullOrWhiteSpace(x)));
            var insight = d.DueDate.HasValue
                ? d.DueDate.Value < today
                    ? $"Overdue (due {d.DueDate.Value:d MMM yyyy})"
                    : d.DueDate.Value <= dueSoonEnd
                        ? $"Due soon ({d.DueDate.Value:d MMM yyyy})"
                        : $"Due {d.DueDate.Value:d MMM yyyy}"
                : "No due date";
            return new RaidMeetingTableRow(d.Id, title, $"DEP-{d.Id}", meta, insight, nameof(ModernRaidController.DependencyDetail));
        }).ToList();

        return new RaidMeetingDependenciesSection
        {
            ActiveCount = activeCount,
            OverdueCount = overdue,
            DueWithin30DaysCount = dueSoon,
            CriticalityCounts = criticalityCounts,
            PriorityRows = rows
        };
    }

    private async Task<RaidMeetingNearMissesSection> BuildNearMissesSectionAsync(
        int? businessAreaId,
        int? directorateId,
        CancellationToken cancellationToken)
    {
        var query = OpenNearMissQuery(businessAreaId, directorateId);
        var openCount = await query.CountAsync(cancellationToken);

        var highSeriousnessIds = await _db.NearMissSeriousnesses.AsNoTracking()
            .Where(s => s.IsActive && (s.Code == "4" || s.SortOrder >= 30))
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);

        var highCount = highSeriousnessIds.Count == 0
            ? 0
            : await query.CountAsync(n => n.NearMissSeriousnessId.HasValue && highSeriousnessIds.Contains(n.NearMissSeriousnessId.Value), cancellationToken);

        var seriousnessCounts = await query
            .GroupBy(n => n.SeriousnessLookup != null ? n.SeriousnessLookup.Label : "Not set")
            .Select(g => new { Label = g.Key ?? "Not set", Count = g.Count() })
            .ToDictionaryAsync(x => x.Label, x => x.Count, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var priority = await query
            .Include(n => n.SeriousnessLookup)
            .Include(n => n.TypeLookup)
            .Include(n => n.StatusLookup)
            .Include(n => n.RiskTier)
            .OrderByDescending(n => n.SeriousnessLookup != null ? n.SeriousnessLookup.SortOrder : 0)
            .ThenByDescending(n => n.DateLogged)
            .Take(12)
            .Select(n => new
            {
                n.Id,
                n.Reference,
                Type = n.TypeLookup != null ? n.TypeLookup.Label : null,
                Seriousness = n.SeriousnessLookup != null ? n.SeriousnessLookup.Label : null,
                Tier = n.RiskTier != null ? n.RiskTier.Name : null,
                Status = n.StatusLookup != null ? n.StatusLookup.Label : null,
                n.DateLogged,
                Impact = n.Impact
            })
            .ToListAsync(cancellationToken);

        var rows = priority.Select(n =>
        {
            var title = Truncate(n.Impact ?? n.Reference, 100);
            var meta = string.Join(" · ", new[] { n.Type, n.Seriousness, n.Tier, n.Status }.Where(x => !string.IsNullOrWhiteSpace(x)));
            var insight = $"Logged {n.DateLogged:d MMM yyyy}";
            return new RaidMeetingTableRow(n.Id, title, n.Reference, meta, insight, nameof(ModernRaidController.NearMissDetail));
        }).ToList();

        return new RaidMeetingNearMissesSection
        {
            OpenCount = openCount,
            HighSeriousnessCount = highCount,
            SeriousnessCounts = seriousnessCounts,
            PriorityRows = rows
        };
    }

    private async Task<int> CountOpenNearMissesAsync(
        int? businessAreaId,
        int? directorateId,
        CancellationToken cancellationToken) =>
        await OpenNearMissQuery(businessAreaId, directorateId).CountAsync(cancellationToken);

    private IQueryable<NearMiss> OpenNearMissQuery(int? businessAreaId, int? directorateId)
    {
        var query = _db.NearMisses.AsNoTracking().Where(n => !n.IsDeleted);
        if (businessAreaId is { } baid)
            query = query.Where(n => n.BusinessAreaLookupId == baid);
        if (directorateId is { } did)
            query = query.Where(n => n.DirectorateLookupId == did);

        var closedIds = _db.NearMissStatuses.AsNoTracking()
            .Where(s => s.IsActive && s.Code == "CLOSED")
            .Select(s => s.Id);
        query = query.Where(n => !n.NearMissStatusId.HasValue || !closedIds.Contains(n.NearMissStatusId.Value));
        return query;
    }

    private async Task<HashSet<int>> ScopedOpenRiskIdsAsync(
        int? businessAreaId,
        int? directorateId,
        CancellationToken cancellationToken)
    {
        var q = _db.Risks.AsNoTracking().Where(r => !r.IsDeleted && r.ClosedDate == null);
        q = ApplyRiskScope(q, businessAreaId, directorateId);
        return (await q.Select(r => r.Id).ToListAsync(cancellationToken)).ToHashSet();
    }

    private async Task<HashSet<int>> ScopedOpenIssueIdsAsync(
        int? businessAreaId,
        int? directorateId,
        CancellationToken cancellationToken)
    {
        var q = _db.Issues.AsNoTracking().Where(i => !i.IsDeleted && i.ClosedDate == null);
        q = ApplyIssueScope(q, businessAreaId, directorateId);
        return (await q.Select(i => i.Id).ToListAsync(cancellationToken)).ToHashSet();
    }

    private async Task<HashSet<int>> ScopedProjectIdsAsync(
        int? businessAreaId,
        int? directorateId,
        CancellationToken cancellationToken)
    {
        var q = _db.Projects.AsNoTracking().Where(p => !p.IsDeleted && p.Status != "Cancelled" && p.Status != "Completed");
        if (businessAreaId is { } baid)
            q = q.Where(p => p.BusinessAreaId == baid);
        if (directorateId is { } did)
            q = q.Where(p => p.Directorates.Any(d => d.DivisionId == did));
        return (await q.Select(p => p.Id).ToListAsync(cancellationToken)).ToHashSet();
    }

    private static IQueryable<Risk> ApplyRiskScope(IQueryable<Risk> query, int? businessAreaId, int? directorateId)
    {
        if (businessAreaId is { } baid)
        {
            query = query.Where(r =>
                r.RiskBusinessAreas.Any(b => b.BusinessAreaLookupId == baid)
                || (r.Project != null && r.Project.BusinessAreaId == baid));
        }

        if (directorateId is { } did)
            query = query.Where(r => r.RiskDivisions.Any(d => d.DivisionId == did));

        return query;
    }

    private static IQueryable<Issue> ApplyIssueScope(IQueryable<Issue> query, int? businessAreaId, int? directorateId)
    {
        if (businessAreaId is { } baid)
        {
            query = query.Where(i =>
                i.IssueBusinessAreas.Any(b => b.BusinessAreaLookupId == baid)
                || (i.Project != null && i.Project.BusinessAreaId == baid));
        }

        if (directorateId is { } did)
            query = query.Where(i => i.IssueDivisions.Any(d => d.DivisionId == did));

        return query;
    }

    private static IQueryable<Assumption> ApplyAssumptionScope(
        IQueryable<Assumption> query,
        int? businessAreaId,
        int? directorateId)
    {
        if (businessAreaId is { } baid)
        {
            query = query.Where(a =>
                a.AssumptionBusinessAreas.Any(b => b.BusinessAreaLookupId == baid)
                || (a.Project != null && a.Project.BusinessAreaId == baid));
        }

        if (directorateId is { } did)
            query = query.Where(a => a.AssumptionDivisions.Any(d => d.DivisionId == did));

        return query;
    }

    private static string Truncate(string? text, int max)
    {
        if (string.IsNullOrWhiteSpace(text)) return "—";
        var t = text.Trim();
        return t.Length <= max ? t : t[..(max - 1)] + "…";
    }
}
