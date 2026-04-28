using System.Text.Json;
using Compass.Data;
using Compass.Models;
using Compass.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Compass.Services;

/// <summary>Builds aggregated RAID statistics for <see cref="Controllers.Modern.ModernReportingController.Raid"/>.</summary>
public sealed class ModernRaidReportingService
{
    private static string NormalizeTab(string? tab) =>
        tab?.Trim().ToLowerInvariant() switch
        {
            "issues" => "issues",
            "intelligence" => "intelligence",
            _ => "risks"
        };

    private static string NormalizeRiskIntel(string? intel) =>
        intel?.Trim().ToLowerInvariant() switch
        {
            "emerging" => "emerging",
            "trends" => "trends",
            "materialised" => "materialised",
            "patterns" => "patterns",
            _ => "patterns"
        };

    /// <summary>Restrict risks to junction tag and/or project business area, and/or directorate.</summary>
    private static IQueryable<Risk> ApplyRiskScopeFilter(
        IQueryable<Risk> query,
        int? businessAreaId,
        int? directorateId)
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

    /// <summary>Restrict issues to junction tag and/or project business area, and/or directorate.</summary>
    private static IQueryable<Issue> ApplyIssueScopeFilter(
        IQueryable<Issue> query,
        int? businessAreaId,
        int? directorateId)
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

    public async Task<ModernRaidReportingViewModel> BuildAsync(
        string? tab,
        string? riskIntel,
        int? businessAreaId,
        int? directorateId,
        CompassDbContext db,
        CancellationToken cancellationToken = default)
    {
        var activeTab = NormalizeTab(tab);
        var intelTab = NormalizeRiskIntel(riskIntel);
        var now = DateTime.UtcNow;
        var monthStarts = Enumerable.Range(0, 12)
            .Select(i => new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-11 + i))
            .ToList();

        var riskAll = db.Risks.AsNoTracking().Where(r => !r.IsDeleted);
        var issueAll = db.Issues.AsNoTracking().Where(i => !i.IsDeleted);
        riskAll = ApplyRiskScopeFilter(riskAll, businessAreaId, directorateId);
        issueAll = ApplyIssueScopeFilter(issueAll, businessAreaId, directorateId);
        var riskOpen = riskAll.Where(r => r.ClosedDate == null);
        var issueOpen = issueAll.Where(i => i.ClosedDate == null);

        /* Risk 5×5: LikelihoodRating × ImpactRating (1–5 each). Rows: likelihood 5 at top → 1 at bottom (open risks). */
        var riskPoints = await riskOpen
            .Select(r => new { r.LikelihoodRating, r.ImpactRating })
            .ToListAsync(cancellationToken);

        var riskGrid = Enumerable.Range(0, 5).Select(_ => Enumerable.Range(0, 5).Select(__ => 0).ToArray()).ToArray();
        foreach (var r in riskPoints)
        {
            var li = Math.Clamp(r.LikelihoodRating, 1, 5);
            var ii = Math.Clamp(r.ImpactRating, 1, 5);
            var row = 5 - li;
            var col = ii - 1;
            riskGrid[row][col]++;
        }

        /* Issue axes */
        var sevCols = await db.IssueSeverities.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder)
            .Select(x => new RaidReportAxisCell(x.Id, x.Label))
            .ToListAsync(cancellationToken);

        var priRows = await db.IssuePriorities.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder)
            .Select(x => new RaidReportAxisCell(x.Id, x.Label))
            .ToListAsync(cancellationToken);

        var sevIndex = sevCols.Select((x, i) => (x.Id, i)).ToDictionary(t => t.Id, t => t.i);
        var priIndex = priRows.Select((x, i) => (x.Id, i)).ToDictionary(t => t.Id, t => t.i);

        var issueCells = await issueOpen
            .Where(i => i.SeverityId.HasValue && i.PriorityId.HasValue)
            .GroupBy(i => new { i.SeverityId, i.PriorityId })
            .Select(g => new { g.Key.SeverityId, g.Key.PriorityId, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var issueGrid = Enumerable.Range(0, Math.Max(1, priRows.Count))
            .Select(_ => Enumerable.Range(0, Math.Max(1, sevCols.Count)).Select(__ => 0).ToArray())
            .ToArray();

        if (priRows.Count > 0 && sevCols.Count > 0)
        {
            foreach (var c in issueCells)
            {
                if (c.SeverityId is not { } sid || c.PriorityId is not { } pid)
                    continue;
                if (!sevIndex.TryGetValue(sid, out var sx) || !priIndex.TryGetValue(pid, out var px))
                    continue;
                issueGrid[px][sx] += c.Count;
            }
        }

        var issueMissing = await issueOpen
            .CountAsync(i => !i.SeverityId.HasValue || !i.PriorityId.HasValue, cancellationToken);

        var openRisks = await riskOpen.CountAsync(cancellationToken);
        var avgScore = await riskOpen.AverageAsync(r => (double?)r.RiskScore, cancellationToken) ?? 0;
        var highRisk = await riskOpen.CountAsync(r => r.RiskScore >= 15, cancellationToken);
        var today = DateTime.UtcNow.Date;
        var reviewOverdue = await riskOpen.CountAsync(r => r.NextReviewDate.HasValue && r.NextReviewDate.Value < today, cancellationToken);
        var thirtyAgo = today.AddDays(-30);
        var ninetyAgo = today.AddDays(-90);
        var riskLast30 = await riskAll.CountAsync(r => r.CreatedAt >= thirtyAgo, cancellationToken);

        var openIssues = await issueOpen.CountAsync(cancellationToken);
        var blocked = await issueOpen.CountAsync(i => i.BlockedFlag, cancellationToken);

        var topSeverityIds = await db.IssueSeverities.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.SortOrder)
            .Take(2)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        var severeOpen = topSeverityIds.Count == 0
            ? 0
            : await issueOpen.CountAsync(i => i.SeverityId.HasValue && topSeverityIds.Contains(i.SeverityId.Value), cancellationToken);

        var issuesLast30 = await issueAll.CountAsync(i => i.CreatedAt >= thirtyAgo, cancellationToken);

        /* Trends */
        var trendLabels = monthStarts.Select(d => d.ToString("MMM yy")).ToList();

        var newRisksPerMonth = new int[12];
        var avgRiskNew = new double?[12];
        var newIssuesPerMonth = new int[12];

        var risksWithCreated = await riskAll
            .Select(r => new { r.CreatedAt, r.RiskScore })
            .ToListAsync(cancellationToken);

        var issuesWithCreated = await issueAll
            .Select(i => new { i.CreatedAt, SevId = i.SeverityId })
            .ToListAsync(cancellationToken);

        var sevIdToLabel = await db.IssueSeverities.AsNoTracking()
            .Where(x => x.IsActive)
            .ToDictionaryAsync(x => x.Id, x => x.Label, cancellationToken);

        var severityKeys = sevCols.Select(s => s.Label).Distinct().ToList();
        var trendSevSeries = severityKeys.ToDictionary(
            k => k,
            _ => new int[12],
            StringComparer.OrdinalIgnoreCase);

        for (var m = 0; m < 12; m++)
        {
            var start = monthStarts[m];
            var end = start.AddMonths(1);
            var risksInMonth = risksWithCreated.Where(r => r.CreatedAt >= start && r.CreatedAt < end).ToList();
            newRisksPerMonth[m] = risksInMonth.Count;
            avgRiskNew[m] = risksInMonth.Count > 0 ? risksInMonth.Average(x => (double)x.RiskScore) : null;

            var issuesInMonth = issuesWithCreated.Where(i => i.CreatedAt >= start && i.CreatedAt < end).ToList();
            newIssuesPerMonth[m] = issuesInMonth.Count;
            foreach (var iss in issuesInMonth)
            {
                if (iss.SevId is { } sid && sevIdToLabel.TryGetValue(sid, out var lab))
                {
                    if (trendSevSeries.TryGetValue(lab, out var arr))
                        arr[m]++;
                }
            }
        }

        var bucketCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["1–5"] = 0,
            ["6–10"] = 0,
            ["11–15"] = 0,
            ["16–20"] = 0,
            ["21–25"] = 0
        };
        foreach (var sc in await riskOpen.Select(x => x.RiskScore).ToListAsync(cancellationToken))
        {
            var k = sc switch
            {
                <= 5 => "1–5",
                <= 10 => "6–10",
                <= 15 => "11–15",
                <= 20 => "16–20",
                _ => "21–25"
            };
            bucketCounts[k]++;
        }

        var openSev = await issueOpen
            .Where(i => i.SeverityId.HasValue)
            .GroupBy(i => i.SeverityId!.Value)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var openIssueSev = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in openSev)
        {
            if (sevIdToLabel.TryGetValue(row.Id, out var lab))
                openIssueSev[lab] = row.Count;
        }

        var topRaw = await riskOpen
            .OrderByDescending(r => r.RiskScore)
            .Take(8)
            .Select(r => new { r.Id, r.Title, r.RiskScore, TierName = r.RiskTier != null ? r.RiskTier.Name : null })
            .ToListAsync(cancellationToken);
        var topRisks = topRaw
            .Select(r => new RaidReportingTopRiskRow(r.Id, r.Title, r.RiskScore, r.TierName))
            .ToList();

        var tierDict = await db.Risks.AsNoTracking()
            .Where(r => !r.IsDeleted && r.ClosedDate == null && r.RiskTierId != null)
            .GroupBy(r => r.RiskTier!.Name)
            .Select(g => new { Name = g.Key!, Count = g.Count() })
            .ToDictionaryAsync(x => x.Name, x => x.Count, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var proxDict = await db.Risks.AsNoTracking()
            .Where(r => !r.IsDeleted && r.ClosedDate == null && r.RiskProximityId != null)
            .GroupBy(r => r.Proximity!.Label)
            .Select(g => new { Name = g.Key!, Count = g.Count() })
            .ToDictionaryAsync(x => x.Name, x => x.Count, StringComparer.OrdinalIgnoreCase, cancellationToken);

        /* Dimensional breakdowns & age buckets (materialised lists to support junction expansions). */
        var riskDimRows = await riskOpen
            .AsNoTracking()
            .Select(r => new
            {
                r.ImpactRating,
                r.IdentifiedDate,
                r.CreatedAt,
                BusinessAreas = r.RiskBusinessAreas.Select(x => x.BusinessAreaLookup.Name),
                Categories = r.RiskRiskCategories.Select(x => x.RiskCategory.Label),
                PrimaryCategory = r.RiskCategory != null ? r.RiskCategory.Label : null,
                ProjBusinessArea = r.Project != null && r.Project.BusinessAreaLookup != null
                    ? r.Project.BusinessAreaLookup.Name
                    : null,
                r.BusinessArea,
                StatusLabel = r.RiskStatus != null ? r.RiskStatus.Label : null
            })
            .ToListAsync(cancellationToken);

        var issueDimRows = await issueOpen
            .AsNoTracking()
            .Select(i => new
            {
                i.DetectedDate,
                i.CreatedAt,
                SeverityLabel = i.SeverityLookup != null ? i.SeverityLookup.Label : i.Severity,
                PriorityLabel = i.PriorityLookup != null ? i.PriorityLookup.Label : (i.Priority ?? "Not set"),
                StatusLabel = i.StatusLookup != null ? i.StatusLookup.Label : i.Status,
                Categories = i.IssueIssueCategories.Select(x => x.IssueCategory.Label),
                PrimaryCategory = i.CategoryLookup != null ? i.CategoryLookup.Label : null,
                BusinessAreas = i.IssueBusinessAreas.Select(x => x.BusinessAreaLookup.Name),
                ProjBusinessArea = i.Project != null && i.Project.BusinessAreaLookup != null
                    ? i.Project.BusinessAreaLookup.Name
                    : null,
                i.BusinessArea
            })
            .ToListAsync(cancellationToken);

        var riskBa = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var riskImpact = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var riskCat = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var riskStat = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var riskAge = RaidReportAggregates.EmptyAgeBuckets();

        foreach (var row in riskDimRows)
        {
            var bas = row.BusinessAreas.ToList();
            if (bas.Count == 0)
            {
                var fb = row.ProjBusinessArea ?? row.BusinessArea ?? "Unassigned";
                RaidReportAggregates.AddCount(riskBa, fb);
            }
            else
            {
                foreach (var b in bas)
                    RaidReportAggregates.AddCount(riskBa, string.IsNullOrWhiteSpace(b) ? "Unassigned" : b);
            }

            var ir = Math.Clamp(row.ImpactRating, 1, 5);
            RaidReportAggregates.AddCount(riskImpact, $"Impact {ir}");

            var catLabels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var c in row.Categories)
            {
                if (!string.IsNullOrWhiteSpace(c))
                    catLabels.Add(c.Trim());
            }

            if (catLabels.Count == 0 && !string.IsNullOrWhiteSpace(row.PrimaryCategory))
                catLabels.Add(row.PrimaryCategory.Trim());
            if (catLabels.Count == 0)
                catLabels.Add("Uncategorised");
            foreach (var c in catLabels)
                RaidReportAggregates.AddCount(riskCat, c);

            RaidReportAggregates.AddCount(riskStat, string.IsNullOrWhiteSpace(row.StatusLabel) ? "Unknown" : row.StatusLabel);

            var start = row.IdentifiedDate ?? row.CreatedAt;
            var days = (now - start).TotalDays;
            RaidReportAggregates.AddCount(riskAge, RaidReportAggregates.AgeBucket(days));
        }

        var issueBa = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var issueCat = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var issueStat = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var issuePri = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var issueSev = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var issueAge = RaidReportAggregates.EmptyAgeBuckets();

        foreach (var row in issueDimRows)
        {
            var bas = row.BusinessAreas.ToList();
            if (bas.Count == 0)
            {
                var fb = row.ProjBusinessArea ?? row.BusinessArea ?? "Unassigned";
                RaidReportAggregates.AddCount(issueBa, fb);
            }
            else
            {
                foreach (var b in bas)
                    RaidReportAggregates.AddCount(issueBa, string.IsNullOrWhiteSpace(b) ? "Unassigned" : b);
            }

            var catLabels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var c in row.Categories)
            {
                if (!string.IsNullOrWhiteSpace(c))
                    catLabels.Add(c.Trim());
            }

            if (catLabels.Count == 0 && !string.IsNullOrWhiteSpace(row.PrimaryCategory))
                catLabels.Add(row.PrimaryCategory.Trim());
            if (catLabels.Count == 0)
                catLabels.Add("Uncategorised");
            foreach (var c in catLabels)
                RaidReportAggregates.AddCount(issueCat, c);

            RaidReportAggregates.AddCount(issueStat, string.IsNullOrWhiteSpace(row.StatusLabel) ? "Unknown" : row.StatusLabel);
            RaidReportAggregates.AddCount(issuePri, string.IsNullOrWhiteSpace(row.PriorityLabel) ? "Not set" : row.PriorityLabel);
            RaidReportAggregates.AddCount(issueSev, string.IsNullOrWhiteSpace(row.SeverityLabel) ? "Unknown" : row.SeverityLabel);

            var start = row.DetectedDate > DateTime.MinValue ? row.DetectedDate : row.CreatedAt;
            var days = (now - start).TotalDays;
            RaidReportAggregates.AddCount(issueAge, RaidReportAggregates.AgeBucket(days));
        }

        var auditSince = monthStarts[0];
        var riskAuditPayloads = await db.AuditLogs.AsNoTracking()
            .Where(a => a.Entity == nameof(Risk) && a.Action == "Update" && a.ChangedUtc >= auditSince
                && (
                    (a.BeforeJson != null && a.BeforeJson.Contains("\"RiskStatusId\""))
                    || (a.AfterJson != null && a.AfterJson.Contains("\"RiskStatusId\""))))
            .Select(a => new { a.BeforeJson, a.AfterJson, a.ChangedUtc })
            .ToListAsync(cancellationToken);

        var issueAuditPayloads = await db.AuditLogs.AsNoTracking()
            .Where(a => a.Entity == nameof(Issue) && a.Action == "Update" && a.ChangedUtc >= auditSince
                && (
                    (a.BeforeJson != null && (a.BeforeJson.Contains("\"StatusId\"") || a.BeforeJson.Contains("\"Status\"")))
                    || (a.AfterJson != null && (a.AfterJson.Contains("\"StatusId\"") || a.AfterJson.Contains("\"Status\"")))))
            .Select(a => new { a.BeforeJson, a.AfterJson, a.ChangedUtc })
            .ToListAsync(cancellationToken);

        var riskStatusMonths = new int[12];
        var issueStatusMonths = new int[12];
        var r30 = 0;
        var r90 = 0;
        var i30 = 0;
        var i90 = 0;
        foreach (var a in riskAuditPayloads)
        {
            if (!RaidReportAggregates.RiskStatusIdChanged(a.BeforeJson, a.AfterJson))
                continue;
            if (a.ChangedUtc >= thirtyAgo)
                r30++;
            if (a.ChangedUtc >= ninetyAgo)
                r90++;
            var mi = RaidReportAggregates.MonthIndexUtc(a.ChangedUtc, monthStarts);
            if (mi >= 0 && mi < 12)
                riskStatusMonths[mi]++;
        }

        foreach (var a in issueAuditPayloads)
        {
            if (!RaidReportAggregates.IssueStatusChanged(a.BeforeJson, a.AfterJson))
                continue;
            if (a.ChangedUtc >= thirtyAgo)
                i30++;
            if (a.ChangedUtc >= ninetyAgo)
                i90++;
            var mi = RaidReportAggregates.MonthIndexUtc(a.ChangedUtc, monthStarts);
            if (mi >= 0 && mi < 12)
                issueStatusMonths[mi]++;
        }

        var intel = await BuildRiskIntelligenceAsync(
            db,
            nowUtc: now,
            thirtyAgo,
            ninetyAgo,
            riskOpen,
            riskAll,
            issueAll,
            cancellationToken);

        var raidIntel = await BuildRaidIntelAsync(
            nowUtc: now,
            todayDate: today,
            riskOpen,
            riskAll,
            issueOpen,
            issueAll,
            topSeverityIds,
            cancellationToken);

        var baFilterOptions = await db.BusinessAreaLookups.AsNoTracking()
            .Where(ba => ba.IsActive)
            .OrderBy(ba => ba.SortOrder)
            .ThenBy(ba => ba.Name)
            .Select(ba => new RaidReportFilterSelectOption(ba.Id, ba.Name))
            .ToListAsync(cancellationToken);

        var dirFilterOptions = await db.Divisions.AsNoTracking()
            .Where(d => d.IsActive)
            .OrderBy(d => d.SortOrder)
            .ThenBy(d => d.Name)
            .Select(d => new RaidReportFilterSelectOption(d.Id, d.Name))
            .ToListAsync(cancellationToken);

        var selBaName = businessAreaId is { } baid
            ? baFilterOptions.FirstOrDefault(x => x.Id == baid)?.Name
            : null;
        var selDirName = directorateId is { } did
            ? dirFilterOptions.FirstOrDefault(x => x.Id == did)?.Name
            : null;
        var filterScopeSummary = (selBaName, selDirName) switch
        {
            (null, null) => "All business areas · All directorates",
            (not null, null) => $"{selBaName} · All directorates",
            (null, not null) => $"All business areas · {selDirName}",
            (not null, not null) => $"{selBaName} · {selDirName}"
        };

        return new ModernRaidReportingViewModel
        {
            ActiveTab = activeTab,
            Intel = raidIntel,
            RiskIntelTab = intelTab,
            FilterBusinessAreaId = businessAreaId,
            FilterDirectorateId = directorateId,
            FilterScopeSummary = filterScopeSummary,
            BusinessAreaFilterOptions = baFilterOptions,
            DirectorateFilterOptions = dirFilterOptions,
            Posture = intel.Posture,
            PatternHighlights = intel.Highlights,
            ThemeRows = intel.Themes,
            EmergingRisks = intel.Emerging,
            TrendRising = intel.TrendUp,
            TrendFalling = intel.TrendDown,
            MaterialisedRows = intel.Materialised,
            RiskLikelihoodImpactGrid = riskGrid,
            RiskMatrixTotal = riskPoints.Count,
            IssueSeverityColumns = sevCols,
            IssuePriorityRows = priRows,
            IssuePrioritySeverityGrid = issueGrid,
            IssueMatrixTotal = issueCells.Sum(x => x.Count),
            IssuesMissingSeverityOrPriority = issueMissing,
            RiskKpis = new RaidReportingRiskKpis(openRisks, Math.Round(avgScore, 2), highRisk, reviewOverdue, riskLast30),
            IssueKpis = new RaidReportingIssueKpis(openIssues, blocked, severeOpen, issuesLast30),
            TrendMonthLabels = trendLabels,
            TrendNewRisksPerMonth = newRisksPerMonth,
            TrendAvgRiskScoreNewPerMonth = avgRiskNew,
            TrendNewIssuesPerMonth = newIssuesPerMonth,
            TrendNewIssuesBySeveritySeries = trendSevSeries,
            RiskScoreBucketCounts = bucketCounts,
            OpenIssueSeverityCounts = openIssueSev,
            TopRisks = topRisks,
            RiskTierCounts = tierDict,
            RiskProximityCounts = proxDict,
            RiskBusinessAreaCounts = riskBa,
            RiskImpactRatingCounts = riskImpact,
            RiskCategoryCounts = riskCat,
            RiskStatusCounts = riskStat,
            RiskOpenAgeBucketCounts = riskAge,
            IssueBusinessAreaCounts = issueBa,
            IssueCategoryCounts = issueCat,
            IssueStatusCounts = issueStat,
            IssuePriorityCounts = issuePri,
            IssueSeverityLabelCounts = issueSev,
            IssueOpenAgeBucketCounts = issueAge,
            RiskStatusChangesLast30Days = r30,
            RiskStatusChangesLast90Days = r90,
            IssueStatusChangesLast30Days = i30,
            IssueStatusChangesLast90Days = i90,
            RiskStatusChangesPerMonth = riskStatusMonths,
            IssueStatusChangesPerMonth = issueStatusMonths
        };
    }

    private async Task<RaidIntelPack> BuildRaidIntelAsync(
        DateTime nowUtc,
        DateTime todayDate,
        IQueryable<Risk> riskOpen,
        IQueryable<Risk> riskAll,
        IQueryable<Issue> issueOpen,
        IQueryable<Issue> issueAll,
        IReadOnlyList<int> topSeverityIds,
        CancellationToken cancellationToken)
    {
        var topSevSet = topSeverityIds.Count > 0 ? topSeverityIds.ToHashSet() : new HashSet<int>();

        var openRisks = await riskOpen
            .AsNoTracking()
            .Select(r => new
            {
                r.Id,
                r.Title,
                r.RiskScore,
                r.ImpactRating,
                r.IdentifiedDate,
                r.CreatedAt,
                r.UpdatedAt,
                r.NextReviewDate,
                r.ProximityDate,
                BusinessAreas = r.RiskBusinessAreas.Select(x => x.BusinessAreaLookup.Name),
                Divisions = r.RiskDivisions.Select(x => x.Division.Name),
                ProjBusinessArea = r.Project != null && r.Project.BusinessAreaLookup != null
                    ? r.Project.BusinessAreaLookup.Name
                    : null,
                r.BusinessArea
            })
            .ToListAsync(cancellationToken);

        var openIssues = await issueOpen
            .AsNoTracking()
            .Select(i => new
            {
                i.Id,
                i.Title,
                i.BlockedFlag,
                i.DetectedDate,
                i.CreatedAt,
                i.UpdatedAt,
                i.SeverityId,
                SeverityLabel = i.SeverityLookup != null ? i.SeverityLookup.Label : i.Severity,
                BusinessAreas = i.IssueBusinessAreas.Select(x => x.BusinessAreaLookup.Name),
                Divisions = i.IssueDivisions.Select(x => x.Division.Name),
                ProjBusinessArea = i.Project != null && i.Project.BusinessAreaLookup != null
                    ? i.Project.BusinessAreaLookup.Name
                    : null,
                i.BusinessArea
            })
            .ToListAsync(cancellationToken);

        var baHot = new Dictionary<string, IntelHotAgg>(StringComparer.OrdinalIgnoreCase);
        var divHot = new Dictionary<string, IntelHotAgg>(StringComparer.OrdinalIgnoreCase);

        foreach (var r in openRisks)
        {
            foreach (var label in IntelHotspotLabels.BusinessAreas(r.BusinessAreas, r.ProjBusinessArea, r.BusinessArea))
                IntelHotspotLabels.AddRisk(baHot, label, r.RiskScore, r.ImpactRating);
            foreach (var label in IntelHotspotLabels.Divisions(r.Divisions))
                IntelHotspotLabels.AddRisk(divHot, label, r.RiskScore, r.ImpactRating);
        }

        foreach (var i in openIssues)
        {
            foreach (var label in IntelHotspotLabels.BusinessAreas(i.BusinessAreas, i.ProjBusinessArea, i.BusinessArea))
                IntelHotspotLabels.AddIssue(baHot, label, i.BlockedFlag);
            foreach (var label in IntelHotspotLabels.Divisions(i.Divisions))
                IntelHotspotLabels.AddIssue(divHot, label, i.BlockedFlag);
        }

        var baRows = baHot
            .Select(kv => IntelHotspotLabels.ToRow(kv.Key, kv.Value))
            .OrderByDescending(x => x.HeatScore)
            .Take(12)
            .ToList();

        var divRows = divHot
            .Select(kv => IntelHotspotLabels.ToRow(kv.Key, kv.Value))
            .OrderByDescending(x => x.HeatScore)
            .Take(12)
            .ToList();

        var riskFocus = openRisks
            .Select(r =>
            {
                var start = r.IdentifiedDate ?? r.CreatedAt;
                var daysOpen = (int)Math.Floor((nowUtc - start).TotalDays);
                var daysSinceUp = (int)Math.Floor((nowUtc - r.UpdatedAt).TotalDays);
                var reviewOd = r.NextReviewDate.HasValue && r.NextReviewDate.Value.Date < todayDate;
                var insightParts = new List<string>();
                if (r.RiskScore >= 15)
                    insightParts.Add("Elevated score");
                if (r.ImpactRating >= 5)
                    insightParts.Add("Maximum inherent impact");
                if (reviewOd)
                    insightParts.Add("Review overdue");
                if (daysSinceUp >= 90)
                    insightParts.Add("No substantive update in 90+ days");
                else if (daysSinceUp >= 60)
                    insightParts.Add("Quiet register entry (60+ days since update)");
                if (daysOpen >= 180)
                    insightParts.Add("Long-standing exposure");
                var insight = insightParts.Count > 0 ? string.Join(" · ", insightParts) : "Monitor in portfolio reviews";

                var pri = r.RiskScore * 8.0
                    + daysOpen * 0.12
                    + (daysSinceUp >= 90 ? 28.0 : daysSinceUp >= 60 ? 12.0 : 0)
                    + (reviewOd ? 22.0 : 0)
                    + (r.ImpactRating >= 5 ? 14.0 : 0)
                    + (r.RiskScore >= 15 ? 16.0 : 0);

                return (
                    Row: new RaidIntelPriorityRiskRow(
                        r.Id,
                        r.Title,
                        "R-" + r.Id.ToString("D4"),
                        r.RiskScore,
                        r.ImpactRating,
                        daysOpen,
                        daysSinceUp,
                        reviewOd,
                        insight),
                    Pri: pri);
            })
            .OrderByDescending(x => x.Pri)
            .Take(12)
            .Select(x => x.Row)
            .ToList();

        var issueFocus = openIssues
            .Select(i =>
            {
                var start = i.DetectedDate > DateTime.MinValue ? i.DetectedDate : i.CreatedAt;
                var daysOpen = (int)Math.Floor((nowUtc - start).TotalDays);
                var sevId = i.SeverityId;
                var severe = sevId.HasValue && topSevSet.Contains(sevId.Value)
                             || (!string.IsNullOrEmpty(i.SeverityLabel)
                                 && i.SeverityLabel.Contains("critical", StringComparison.OrdinalIgnoreCase));
                var insightParts = new List<string>();
                if (i.BlockedFlag)
                    insightParts.Add("Delivery blocked");
                if (severe)
                    insightParts.Add("Top-quartile severity");
                if (daysOpen >= 90)
                    insightParts.Add("Aged backlog");
                var insight = insightParts.Count > 0 ? string.Join(" · ", insightParts) : "Track in RAID hygiene";

                var pri = (i.BlockedFlag ? 32.0 : 0)
                    + (severe ? 20.0 : 0)
                    + daysOpen * 0.18;

                return (
                    Row: new RaidIntelPriorityIssueRow(
                        i.Id,
                        i.Title,
                        "I-" + i.Id.ToString("D4"),
                        i.SeverityLabel,
                        i.BlockedFlag,
                        daysOpen,
                        insight),
                    Pri: pri);
            })
            .OrderByDescending(x => x.Pri)
            .Take(12)
            .Select(x => x.Row)
            .ToList();

        var longRisks = openRisks
            .Select(r =>
            {
                var start = r.IdentifiedDate ?? r.CreatedAt;
                var daysOpen = (int)Math.Floor((nowUtc - start).TotalDays);
                var daysSinceUp = (int)Math.Floor((nowUtc - r.UpdatedAt).TotalDays);
                var reviewOd = r.NextReviewDate.HasValue && r.NextReviewDate.Value.Date < todayDate;
                return new RaidIntelPriorityRiskRow(
                    r.Id,
                    r.Title,
                    "R-" + r.Id.ToString("D4"),
                    r.RiskScore,
                    r.ImpactRating,
                    daysOpen,
                    daysSinceUp,
                    reviewOd,
                    $"Open {daysOpen} days · score {r.RiskScore}");
            })
            .OrderByDescending(x => x.DaysOpen)
            .Take(10)
            .ToList();

        var longIssues = openIssues
            .Select(i =>
            {
                var start = i.DetectedDate > DateTime.MinValue ? i.DetectedDate : i.CreatedAt;
                var daysOpen = (int)Math.Floor((nowUtc - start).TotalDays);
                return new RaidIntelPriorityIssueRow(
                    i.Id,
                    i.Title,
                    "I-" + i.Id.ToString("D4"),
                    i.SeverityLabel,
                    i.BlockedFlag,
                    daysOpen,
                    $"Open {daysOpen} days");
            })
            .OrderByDescending(x => x.DaysOpen)
            .Take(10)
            .ToList();

        var staleCutoff = nowUtc.AddDays(-90);
        var staleRisks = openRisks
            .Where(r => r.UpdatedAt < staleCutoff)
            .OrderByDescending(r => r.RiskScore)
            .ThenByDescending(r => r.IdentifiedDate ?? r.CreatedAt)
            .Take(10)
            .Select(r =>
            {
                var start = r.IdentifiedDate ?? r.CreatedAt;
                var daysOpen = (int)Math.Floor((nowUtc - start).TotalDays);
                var daysSinceUp = (int)Math.Floor((nowUtc - r.UpdatedAt).TotalDays);
                return new RaidIntelStaleRiskRow(
                    r.Id,
                    r.Title,
                    "R-" + r.Id.ToString("D4"),
                    r.RiskScore,
                    daysSinceUp,
                    daysOpen,
                    $"Still open with no update recorded for {daysSinceUp} days (score {r.RiskScore}).");
            })
            .ToList();

        var staleIssues = openIssues
            .Where(i => i.UpdatedAt < staleCutoff)
            .OrderByDescending(i => i.BlockedFlag)
            .ThenBy(i => i.UpdatedAt)
            .Take(10)
            .Select(i =>
            {
                var start = i.DetectedDate > DateTime.MinValue ? i.DetectedDate : i.CreatedAt;
                var daysOpen = (int)Math.Floor((nowUtc - start).TotalDays);
                var daysSinceUp = (int)Math.Floor((nowUtc - i.UpdatedAt).TotalDays);
                return new RaidIntelStaleIssueRow(
                    i.Id,
                    i.Title,
                    "I-" + i.Id.ToString("D4"),
                    i.SeverityLabel,
                    daysSinceUp,
                    daysOpen,
                    $"No update in {daysSinceUp} days.");
            })
            .ToList();

        /* Proximity: raised ≥120d ago, proximity date within 6 months of raise, and proximity in the next 90d (nearing). */
        const int minDaysSinceRaiseForProximity = 120;
        const int maxDaysFromRaiseToProximity = 186; /* ~6 months */
        const int maxDaysUntilProximity = 90;
        var proxNearing = openRisks
            .Where(r => r.ProximityDate.HasValue)
            .Select(r =>
            {
                var raise = (r.IdentifiedDate ?? r.CreatedAt).Date;
                var prox = r.ProximityDate!.Value.Date;
                var daysFromRaiseToProx = (int)Math.Floor((prox - raise).TotalDays);
                var daysToProx = (int)Math.Floor((prox - todayDate).TotalDays);
                var daysSinceRaise = (int)Math.Floor((todayDate - raise).TotalDays);
                return new RaidIntelProximityNearingRow(
                    r.Id,
                    r.Title,
                    "R-" + r.Id.ToString("D4"),
                    raise,
                    prox,
                    daysToProx,
                    r.RiskScore,
                    $"Raised {daysSinceRaise}d ago · proximity window {daysFromRaiseToProx}d from ID · in {Math.Max(0, daysToProx)}d");
            })
            .Where(x =>
                x.DaysToProximity >= 0
                && x.DaysToProximity <= maxDaysUntilProximity
                && (int)Math.Floor((todayDate - x.RaisedDate).TotalDays) >= minDaysSinceRaiseForProximity)
            .Where(x =>
            {
                var w = (int)Math.Floor((x.ProximityDate - x.RaisedDate).TotalDays);
                return w > 0 && w <= maxDaysFromRaiseToProximity;
            })
            .OrderBy(x => x.ProximityDate)
            .Take(20)
            .ToList();

        var quickWindowStart = nowUtc.AddDays(-120);
        const int quickMaxOpenDays = 29; /* closed in under 30 days */

        var closedRisks = await riskAll
            .AsNoTracking()
            .Where(r => !r.IsDeleted && r.ClosedDate != null && r.ClosedDate >= quickWindowStart)
            .Select(r => new { r.Id, r.Title, r.ClosedDate, r.IdentifiedDate, r.CreatedAt })
            .ToListAsync(cancellationToken);

        var quickRisks = closedRisks
            .Select(r =>
            {
                var start = r.IdentifiedDate ?? r.CreatedAt;
                var span = (int)Math.Floor((r.ClosedDate!.Value - start).TotalDays);
                return (Id: r.Id, Title: r.Title, ClosedAt: r.ClosedDate!.Value, Span: span);
            })
            .Where(x => x.Span >= 0 && x.Span <= quickMaxOpenDays)
            .OrderByDescending(x => x.ClosedAt)
            .Take(10)
            .Select(x => new RaidIntelThroughputRow(
                x.Id,
                x.Title,
                "R-" + x.Id.ToString("D4"),
                true,
                x.Span,
                x.ClosedAt,
                $"Closed after only {x.Span} day(s) on register — capture what worked."))
            .ToList();

        var closedIssues = await issueAll
            .AsNoTracking()
            .Where(i => !i.IsDeleted && i.ClosedDate != null && i.ClosedDate >= quickWindowStart)
            .Select(i => new { i.Id, i.Title, i.ClosedDate, i.DetectedDate, i.CreatedAt })
            .ToListAsync(cancellationToken);

        var quickIssues = closedIssues
            .Select(i =>
            {
                var start = i.DetectedDate > DateTime.MinValue ? i.DetectedDate : i.CreatedAt;
                var span = (int)Math.Floor((i.ClosedDate!.Value - start).TotalDays);
                return (Id: i.Id, Title: i.Title, ClosedAt: i.ClosedDate!.Value, Span: span);
            })
            .Where(x => x.Span >= 0 && x.Span <= quickMaxOpenDays)
            .OrderByDescending(x => x.ClosedAt)
            .Take(10)
            .Select(x => new RaidIntelThroughputRow(
                x.Id,
                x.Title,
                "I-" + x.Id.ToString("D4"),
                false,
                x.Span,
                x.ClosedAt,
                $"Closed after {x.Span} day(s) — good turnaround signal."))
            .ToList();

        var bullets = new List<string>();
        if (baRows.Count > 0 && baRows[0].HeatScore > 0)
        {
            var h = baRows[0];
            bullets.Add(
                $"Strongest concentration: {h.Label} — {h.OpenRisks} open risk(s), {h.OpenIssues} open issue(s), {h.HighScoreRisks} with score ≥15.");
        }

        if (divRows.Count > 0 && divRows[0].HeatScore > 0)
        {
            var d = divRows[0];
            bullets.Add(
                $"Division spotlight: {d.Label} — heat score {d.HeatScore} ({d.Rationale}).");
        }

        if (staleRisks.Count + staleIssues.Count > 0)
        {
            bullets.Add(
                staleIssues.Count == 0
                    ? $"{staleRisks.Count} open risk(s) have no update in 90+ days — validate owners and controls."
                    : staleRisks.Count == 0
                        ? $"{staleIssues.Count} open issue(s) have no update in 90+ days — validate owners and delivery."
                        : $"{staleRisks.Count} risk(s) and {staleIssues.Count} issue(s) have no update in 90+ days — validate owners.");
        }

        if (quickRisks.Count + quickIssues.Count > 0)
            bullets.Add(
                $"{quickRisks.Count + quickIssues.Count} risk/issue(s) closed in under 30 days recently — good candidates for playbook patterns.");

        if (proxNearing.Count > 0)
            bullets.Add(
                $"{proxNearing.Count} open risk(s) have proximity within 6 months of being raised and a proximity date in the next 90 days — review before the window lands.");

        var blocked = openIssues.Count(i => i.BlockedFlag);
        if (blocked > 0)
            bullets.Add($"{blocked} issue(s) are blocked — unblock or escalate before they absorb delivery capacity.");

        var hi = openRisks.Count(r => r.RiskScore >= 15);
        if (hi > 0)
            bullets.Add($"{hi} open risk(s) sit at score ≥15 — align SRO attention and mitigation depth.");

        var imp5 = openRisks.Count(r => r.ImpactRating >= 5);
        if (imp5 > 0)
            bullets.Add($"{imp5} open risk(s) carry inherent impact 5 — stress-test contingencies even if scores are mid-band.");

        return new RaidIntelPack(
            bullets,
            baRows,
            divRows,
            riskFocus,
            issueFocus,
            quickRisks,
            quickIssues,
            staleRisks,
            staleIssues,
            longRisks,
            longIssues,
            proxNearing);
    }

    private sealed record RiskIntelBundle(
        RaidRiskPostureStrip Posture,
        RaidPatternHighlightCards Highlights,
        IReadOnlyList<RaidThemeAnalyticsRow> Themes,
        IReadOnlyList<RaidEmergingRiskReportRow> Emerging,
        IReadOnlyList<RaidRiskScoreTrendReportRow> TrendUp,
        IReadOnlyList<RaidRiskScoreTrendReportRow> TrendDown,
        IReadOnlyList<RaidMaterialisedRiskReportRow> Materialised);

    private async Task<RiskIntelBundle> BuildRiskIntelligenceAsync(
        CompassDbContext db,
        DateTime nowUtc,
        DateTime thirtyAgo,
        DateTime ninetyAgo,
        IQueryable<Risk> riskOpen,
        IQueryable<Risk> riskAll,
        IQueryable<Issue> issueAll,
        CancellationToken cancellationToken)
    {
        var materialised30 = await issueAll.CountAsync(
            i => !i.IsDeleted && i.SourceRiskId != null && i.CreatedAt >= thirtyAgo,
            cancellationToken);

        var elevated90 = await riskOpen.CountAsync(
            r => r.RiskScore >= 16 && r.UpdatedAt >= ninetyAgo,
            cancellationToken);

        var reduced90 = await riskOpen.CountAsync(
            r => r.RiskScore <= 10 && r.UpdatedAt >= ninetyAgo,
            cancellationToken);

        var nearBoundary30 = await riskOpen.CountAsync(
            r =>
                (r.ProximityDate.HasValue && r.ProximityDate.Value >= nowUtc.Date && r.ProximityDate.Value <= nowUtc.Date.AddDays(30))
                || (r.RiskScore >= 12 && r.RiskScore <= 14 && r.UpdatedAt >= thirtyAgo),
            cancellationToken);

        var posture = new RaidRiskPostureStrip(
            TotalOpen: await riskOpen.CountAsync(cancellationToken),
            MaterialisedLast30Days: materialised30,
            ScoreElevatedActive90d: elevated90,
            ScoreReducedActive90d: reduced90,
            NearTermProximityOrElevatedBand30d: nearBoundary30);

        var linkedIssueCounts = await issueAll
            .Where(i => i.SourceRiskId != null && !i.IsDeleted)
            .GroupBy(i => i.SourceRiskId!.Value)
            .Select(g => new { RiskId = g.Key, C = g.Count() })
            .ToDictionaryAsync(x => x.RiskId, x => x.C, cancellationToken);

        var emergingRows = await riskOpen
            .OrderByDescending(r => r.RiskScore)
            .ThenByDescending(r => r.UpdatedAt)
            .Take(12)
            .Select(r => new
            {
                r.Id,
                r.Title,
                r.RiskScore,
                r.UpdatedAt,
                r.NextReviewDate,
                r.ProximityDate,
                TierName = r.RiskTier != null ? r.RiskTier.Name : null,
                Portfolio = r.Project != null && r.Project.PrimaryOrganizationalGroup != null
                    ? r.Project.PrimaryOrganizationalGroup.Name
                    : null,
                OwnerName = r.OwnerUser != null ? r.OwnerUser.Name ?? r.OwnerUser.Email : null,
                StatusLabel = r.RiskStatus != null ? r.RiskStatus.Label : null
            })
            .ToListAsync(cancellationToken);

        var emerging = emergingRows.Select(r =>
        {
            var ic = linkedIssueCounts.GetValueOrDefault(r.Id);
            var prox = r.ProximityDate.HasValue
                ? $" Proximity window {r.ProximityDate:d MMM yyyy}."
                : "";
            var rev = r.NextReviewDate.HasValue && r.NextReviewDate.Value < nowUtc.Date
                ? "Review date passed — escalate."
                : r.NextReviewDate.HasValue
                    ? $"Next review {r.NextReviewDate:d MMM yyyy}."
                    : "Schedule a formal review.";
            var why =
                $"Open risk · score {r.RiskScore}.{prox}"
                + (ic > 0 ? $" {ic} linked issue(s) from this risk." : "");
            var traj =
                $"Current score {r.RiskScore} · last updated {r.UpdatedAt:dd MMM yyyy}";
            var sevCss = r.RiskScore >= 20 ? "rag-r" : r.RiskScore >= 15 ? "rag-a" : "";
            var subtitleParts = new List<string>();
            if (!string.IsNullOrEmpty(r.TierName))
                subtitleParts.Add(r.TierName);
            if (!string.IsNullOrEmpty(r.Portfolio))
                subtitleParts.Add(r.Portfolio);
            if (!string.IsNullOrEmpty(r.OwnerName))
                subtitleParts.Add("Owner: " + r.OwnerName);
            return new RaidEmergingRiskReportRow(
                r.Id,
                r.Title,
                "R-" + r.Id.ToString("D4"),
                r.RiskScore,
                r.TierName,
                subtitleParts.Count > 0 ? string.Join(" · ", subtitleParts) : null,
                why,
                traj,
                rev,
                sevCss);
        }).ToList();

        var trendUpRows = await riskOpen
            .Where(r => r.RiskScore >= 15 && r.UpdatedAt >= ninetyAgo)
            .OrderByDescending(r => r.RiskScore)
            .Take(12)
            .Select(r => new
            {
                r.Id,
                r.Title,
                r.RiskScore,
                Portfolio = r.Project != null && r.Project.PrimaryOrganizationalGroup != null
                    ? r.Project.PrimaryOrganizationalGroup.Name
                    : null,
                StatusLabel = r.RiskStatus != null ? r.RiskStatus.Label : null
            })
            .ToListAsync(cancellationToken);

        var trendUp = trendUpRows
            .Select(r => new RaidRiskScoreTrendReportRow(
                r.Id,
                r.Title,
                "R-" + r.Id.ToString("D4"),
                r.Portfolio,
                r.RiskScore,
                r.StatusLabel,
                "Elevated exposure · updated in last 90 days"))
            .ToList();

        var trendDownRows = await riskOpen
            .Where(r => r.RiskScore <= 10 && r.UpdatedAt >= ninetyAgo)
            .OrderBy(r => r.RiskScore)
            .Take(12)
            .Select(r => new
            {
                r.Id,
                r.Title,
                r.RiskScore,
                Portfolio = r.Project != null && r.Project.PrimaryOrganizationalGroup != null
                    ? r.Project.PrimaryOrganizationalGroup.Name
                    : null
            })
            .ToListAsync(cancellationToken);

        var trendDown = trendDownRows
            .Select(r => new RaidRiskScoreTrendReportRow(
                r.Id,
                r.Title,
                "R-" + r.Id.ToString("D4"),
                r.Portfolio,
                r.RiskScore,
                null,
                "Lower residual band · touched in last 90 days"))
            .ToList();

        var ninetyDayCutoff = nowUtc.AddDays(-90);
        var materialisedQuery = issueAll
            .Where(i => !i.IsDeleted && i.SourceRiskId != null && i.CreatedAt >= ninetyDayCutoff)
            .OrderByDescending(i => i.CreatedAt)
            .Take(25);

        var materialisedRaw = await materialisedQuery
            .Select(i => new
            {
                i.Id,
                i.Title,
                i.CreatedAt,
                i.DetectedDate,
                RiskId = i.SourceRiskId!.Value,
                RiskTitle = i.SourceRisk!.Title,
                RiskScore = i.SourceRisk!.RiskScore,
                Mitigation = i.SourceRisk!.ResponseStrategy ?? i.SourceRisk!.Notes ?? ""
            })
            .ToListAsync(cancellationToken);

        var materialised = materialisedRaw.Select(i =>
        {
            var mit = (i.Mitigation ?? "").Trim();
            string tag;
            string css;
            if (mit.Length >= 80)
            {
                tag = "YES";
                css = "govuk-tag--green";
            }
            else if (mit.Length >= 20)
            {
                tag = "PARTIAL";
                css = "govuk-tag--yellow";
            }
            else
            {
                tag = "NO";
                css = "govuk-tag--red";
            }

            var when = i.DetectedDate > DateTime.MinValue ? i.DetectedDate : i.CreatedAt;
            return new RaidMaterialisedRiskReportRow(
                i.RiskId,
                i.RiskTitle,
                "R-" + i.RiskId.ToString("D4"),
                when,
                i.Id,
                i.Title,
                "I-" + i.Id.ToString("D4"),
                i.RiskScore,
                tag,
                css);
        }).ToList();

        var themeSource = await riskOpen
            .Select(r => new
            {
                r.RiskScore,
                r.CreatedAt,
                Cat = r.RiskCategory != null ? r.RiskCategory.Label : "Uncategorised",
                Portfolio = r.Project != null && r.Project.PrimaryOrganizationalGroup != null
                    ? r.Project.PrimaryOrganizationalGroup.Name
                    : null
            })
            .ToListAsync(cancellationToken);

        var sixMonthsAgo = nowUtc.AddMonths(-6);
        var matFromRisk = await issueAll
            .Where(i => !i.IsDeleted && i.SourceRiskId != null && i.CreatedAt >= sixMonthsAgo)
            .Select(i => i.SourceRiskId!.Value)
            .ToListAsync(cancellationToken);

        var matCountByRiskId = matFromRisk.GroupBy(id => id).ToDictionary(g => g.Key, g => g.Count());

        var themesGrouped = themeSource
            .GroupBy(x => x.Cat)
            .Select(g =>
            {
                var rows = g.ToList();
                var avg = rows.Count > 0 ? rows.Average(x => x.RiskScore) : 0;
                var recent = rows.Count(x => x.CreatedAt >= nowUtc.AddDays(-60));
                var older = rows.Count(x => x.CreatedAt < nowUtc.AddDays(-60) && x.CreatedAt >= nowUtc.AddDays(-120));
                string sym;
                string lbl;
                string css;
                if (recent > older * 1.25 && recent >= 2)
                {
                    sym = "\u2197";
                    lbl = "Increasing";
                    css = "rag-r";
                }
                else if (older > recent * 1.25 && older >= 2)
                {
                    sym = "\u2198";
                    lbl = "Decreasing";
                    css = "rag-g";
                }
                else
                {
                    sym = "\u2192";
                    lbl = "Stable";
                    css = "muted";
                }

                var portfolios = rows
                    .Select(x => x.Portfolio)
                    .Where(x => !string.IsNullOrEmpty(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(x => x)
                    .ToList();
                var pfSummary = portfolios.Count > 0 ? string.Join(", ", portfolios.Take(6)) + (portfolios.Count > 6 ? "…" : "") : "—";
                return new RaidThemeAnalyticsRow(g.Key, rows.Count, Math.Round(avg, 1), sym, lbl, css, pfSummary);
            })
            .OrderByDescending(t => t.RiskCount)
            .ToList();

        RaidPatternHighlightCards highlights;
        if (themesGrouped.Count == 0)
        {
            highlights = new RaidPatternHighlightCards(null, 0, 0, null, 0, 0, 0, null, null);
        }
        else
        {
            var topCat = themesGrouped.OrderByDescending(x => x.RiskCount).First();
            var riskIdsInCat = await riskOpen
                .Where(r => topCat.Theme == "Uncategorised"
                    ? r.RiskCategoryId == null
                    : r.RiskCategory != null && r.RiskCategory.Label == topCat.Theme)
                .Select(r => r.Id)
                .ToListAsync(cancellationToken);
            var matCat = riskIdsInCat.Sum(rid => matCountByRiskId.GetValueOrDefault(rid));

            var portfolioAgg = themeSource
                .Where(x => !string.IsNullOrEmpty(x.Portfolio))
                .GroupBy(x => x.Portfolio!, StringComparer.OrdinalIgnoreCase)
                .Select(g => new { Name = g.Key!, Risks = g.Count(), Crit = g.Count(x => x.RiskScore >= 16) })
                .OrderByDescending(x => x.Risks)
                .FirstOrDefault();

            var improving = themesGrouped
                .Where(t => t.RiskCount >= 3 && t.TrendLabel == "Decreasing")
                .OrderBy(t => t.AvgScore)
                .FirstOrDefault()
                ?? themesGrouped.Where(t => t.RiskCount >= 3).OrderBy(t => t.AvgScore).FirstOrDefault();

            var topPortMat = 0;
            if (portfolioAgg != null)
            {
                var pIds = await riskOpen
                    .Where(r => r.Project != null && r.Project.PrimaryOrganizationalGroup != null &&
                                r.Project.PrimaryOrganizationalGroup.Name == portfolioAgg.Name)
                    .Select(r => r.Id)
                    .ToListAsync(cancellationToken);
                topPortMat = pIds.Sum(id => matCountByRiskId.GetValueOrDefault(id));
            }

            highlights = new RaidPatternHighlightCards(
                topCat.Theme,
                topCat.RiskCount,
                matCat,
                portfolioAgg?.Name,
                portfolioAgg?.Risks ?? 0,
                portfolioAgg?.Crit ?? 0,
                topPortMat,
                improving?.Theme,
                improving != null ? $"Average score {improving.AvgScore:0.#} · trend {improving.TrendLabel}" : null);
        }

        return new RiskIntelBundle(posture, highlights, themesGrouped, emerging, trendUp, trendDown, materialised);
    }
}

file sealed class IntelHotAgg
{
    public int OpenRisks;
    public int OpenIssues;
    public int HighScoreRisks;
    public int MaxImpactRisks;
    public int BlockedIssues;

    public double Heat =>
        OpenRisks * 1.5 + OpenIssues + HighScoreRisks * 2.5 + MaxImpactRisks * 2 + BlockedIssues * 2;
}

file static class IntelHotspotLabels
{
    public static IEnumerable<string> BusinessAreas(
        IEnumerable<string> junction,
        string? proj,
        string? legacy)
    {
        var bas = junction
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (bas.Count > 0)
            return bas;
        var fb = proj ?? legacy;
        return new[] { string.IsNullOrWhiteSpace(fb) ? "Unassigned" : fb.Trim() };
    }

    public static IEnumerable<string> Divisions(IEnumerable<string> junction)
    {
        var d = junction
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        return d.Count > 0 ? d : new[] { "Unassigned" };
    }

    public static void AddRisk(Dictionary<string, IntelHotAgg> map, string key, int score, int impact)
    {
        if (!map.TryGetValue(key, out var a))
        {
            a = new IntelHotAgg();
            map[key] = a;
        }

        a.OpenRisks++;
        if (score >= 15)
            a.HighScoreRisks++;
        if (impact >= 5)
            a.MaxImpactRisks++;
    }

    public static void AddIssue(Dictionary<string, IntelHotAgg> map, string key, bool blocked)
    {
        if (!map.TryGetValue(key, out var a))
        {
            a = new IntelHotAgg();
            map[key] = a;
        }

        a.OpenIssues++;
        if (blocked)
            a.BlockedIssues++;
    }

    public static RaidIntelHotspotRow ToRow(string label, IntelHotAgg a) =>
        new(
            label,
            a.OpenRisks,
            a.OpenIssues,
            a.HighScoreRisks,
            a.MaxImpactRisks,
            a.BlockedIssues,
            (int)Math.Round(a.Heat),
            $"{a.HighScoreRisks} score ≥15 · {a.MaxImpactRisks} max impact · {a.BlockedIssues} blocked issues");
}

file static class RaidReportAggregates
{
    public const string Age0_7 = "0–7 days";
    public const string Age8_30 = "8–30 days";
    public const string Age31_90 = "31–90 days";
    public const string Age91_180 = "91–180 days";
    public const string Age180Plus = "181+ days";

    public static Dictionary<string, int> EmptyAgeBuckets() => new(StringComparer.OrdinalIgnoreCase)
    {
        [Age0_7] = 0,
        [Age8_30] = 0,
        [Age31_90] = 0,
        [Age91_180] = 0,
        [Age180Plus] = 0
    };

    public static string AgeBucket(double daysOpen)
    {
        if (daysOpen <= 7) return Age0_7;
        if (daysOpen <= 30) return Age8_30;
        if (daysOpen <= 90) return Age31_90;
        if (daysOpen <= 180) return Age91_180;
        return Age180Plus;
    }

    public static void AddCount(Dictionary<string, int> map, string key)
    {
        map[key] = map.GetValueOrDefault(key) + 1;
    }

    public static int MonthIndexUtc(DateTime changedUtc, IReadOnlyList<DateTime> monthStartsUtc)
    {
        var when = DateTime.SpecifyKind(changedUtc, DateTimeKind.Utc);
        for (var i = 0; i < monthStartsUtc.Count; i++)
        {
            var start = DateTime.SpecifyKind(monthStartsUtc[i], DateTimeKind.Utc);
            if (when >= start && when < start.AddMonths(1))
                return i;
        }

        return -1;
    }

    public static bool RiskStatusIdChanged(string? beforeJson, string? afterJson)
    {
        var b = TryGetJsonProp(beforeJson, "RiskStatusId");
        var a = TryGetJsonProp(afterJson, "RiskStatusId");
        if (b.Kind == JsonPropKind.Absent && a.Kind == JsonPropKind.Absent)
            return false;
        return !ValueEquals(b, a);
    }

    public static bool IssueStatusChanged(string? beforeJson, string? afterJson)
    {
        var bId = TryGetJsonProp(beforeJson, "StatusId");
        var aId = TryGetJsonProp(afterJson, "StatusId");
        if (bId.Kind != JsonPropKind.Absent || aId.Kind != JsonPropKind.Absent)
            return !ValueEquals(bId, aId);

        var bs = TryGetJsonString(beforeJson, "Status");
        var astr = TryGetJsonString(afterJson, "Status");
        if (bs != null || astr != null)
            return !string.Equals(bs, astr, StringComparison.OrdinalIgnoreCase);

        return false;
    }

    private static readonly JsonProp Absent = new(JsonPropKind.Absent);

    private static JsonProp TryGetJsonProp(string? json, string name)
    {
        if (string.IsNullOrWhiteSpace(json))
            return Absent;
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty(name, out var el))
                return Absent;
            if (el.ValueKind == JsonValueKind.Null)
                return new JsonProp(JsonPropKind.Null, null);
            if (el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out var n))
                return new JsonProp(JsonPropKind.Number, n);
            if (el.ValueKind == JsonValueKind.String)
                return new JsonProp(JsonPropKind.String, el.GetString());
        }
        catch (JsonException)
        {
            // ignore malformed rows
        }

        return Absent;
    }

    private static string? TryGetJsonString(string? json, string name)
    {
        var p = TryGetJsonProp(json, name);
        return p.Kind == JsonPropKind.String ? (string?)p.Boxed : null;
    }

    private static bool ValueEquals(JsonProp b, JsonProp a)
    {
        if (b.Kind == JsonPropKind.Absent && a.Kind == JsonPropKind.Absent)
            return true;
        if (b.Kind != a.Kind)
            return false;
        return b.Kind switch
        {
            JsonPropKind.Null => true,
            JsonPropKind.Number => (int?)b.Boxed == (int?)a.Boxed,
            JsonPropKind.String => string.Equals((string?)b.Boxed, (string?)a.Boxed, StringComparison.Ordinal),
            _ => false
        };
    }

    private enum JsonPropKind
    {
        Absent,
        Null,
        Number,
        String
    }

    private readonly struct JsonProp(JsonPropKind kind, object? boxed = null)
    {
        public JsonPropKind Kind { get; } = kind;
        public object? Boxed { get; } = boxed;
    }
}
