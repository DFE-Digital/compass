using System;
using Compass.Models;
using Compass.Services.Raid;
using Compass.ViewModels.Modern;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Compass.Controllers.Modern;

/// <summary>Business area matrix and drill at <c>/modern/raid/business-areas</c> (mirrors <see cref="Directorate"/> / tier).</summary>
public partial class ModernRaidController
{
    private static bool RiskBelongsToBusinessArea(
        Risk r,
        int businessAreaId,
        IReadOnlyDictionary<int, int?> projectBusinessAreaId,
        IReadOnlyDictionary<int, HashSet<int>> riskBusinessAreaMap)
    {
        if (r.ProjectId is int pid &&
            projectBusinessAreaId.TryGetValue(pid, out var pba) &&
            pba == businessAreaId)
            return true;
        return riskBusinessAreaMap.TryGetValue(r.Id, out var set) && set.Contains(businessAreaId);
    }

    private static bool IssueBelongsToBusinessArea(
        Issue i,
        int businessAreaId,
        IReadOnlyDictionary<int, int?> projectBusinessAreaId)
    {
        if (i.ProjectId is int pid &&
            projectBusinessAreaId.TryGetValue(pid, out var pba) &&
            pba == businessAreaId)
            return true;
        return i.IssueBusinessAreas.Any(x => x.BusinessAreaLookupId == businessAreaId);
    }

    private async Task<List<RaidBusinessAreaSummaryRowVm>> BuildBusinessAreaMatrixRowsAsync(
        int? effectiveBusinessAreaId,
        string? search,
        int? projectId,
        int? directorateId,
        int? filterAreaId,
        CancellationToken cancellationToken)
    {
        var areas = await _db.BusinessAreaLookups.AsNoTracking()
            .Where(b => b.IsActive)
            .OrderBy(b => b.SortOrder).ThenBy(b => b.Name)
            .ToListAsync(cancellationToken);

        // Matrix rows: optional "table" filter wins; otherwise when data scope is a specific BA, show only that row
        if (filterAreaId is > 0)
            areas = areas.Where(b => b.Id == filterAreaId.Value).ToList();
        else if (effectiveBusinessAreaId is > 0)
            areas = areas.Where(b => b.Id == effectiveBusinessAreaId.Value).ToList();

        var areaIds = areas.Select(b => b.Id).ToList();
        if (areaIds.Count == 0)
            return new List<RaidBusinessAreaSummaryRowVm>();

        var projects = await _db.Projects.AsNoTracking()
            .Where(p => !p.IsDeleted)
            .Select(p => new { p.Id, p.BusinessAreaId })
            .ToListAsync(cancellationToken);

        var workItemCountByBa = projects
            .Where(p => p.BusinessAreaId is > 0)
            .GroupBy(p => p.BusinessAreaId!.Value)
            .ToDictionary(g => g.Key, g => g.Count());

        var projectBusinessAreaId = projects.ToDictionary(p => p.Id, p => p.BusinessAreaId);

        var rbaRows = await _db.RiskBusinessAreas.AsNoTracking()
            .Select(x => new { x.RiskId, x.BusinessAreaLookupId })
            .ToListAsync(cancellationToken);

        var riskBusinessAreaMap = rbaRows.GroupBy(x => x.RiskId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.BusinessAreaLookupId).ToHashSet());

        var riskQuery = RaidTierReportingRiskQuery(search, projectId, directorateId, effectiveBusinessAreaId);
        var risks = await riskQuery.ToListAsync(cancellationToken);

        var issueQuery = IssueQueryableAlignedToRiskRegisterFilters(
            projectId,
            MergeRaidRegisterFilterIds(directorateId, null),
            MergeRaidRegisterFilterIds(effectiveBusinessAreaId, null),
            search);
        var issues = await issueQuery
            .Include(i => i.SeverityLookup)
            .Include(i => i.IssueBusinessAreas)
            .ToListAsync(cancellationToken);

        var leadershipLines = await _db.BusinessAreaLeadershipMembers.AsNoTracking()
            .Where(m => areaIds.Contains(m.BusinessAreaLookupId))
            .Select(m => new { m.BusinessAreaLookupId, m.User.Name, m.User.Email })
            .ToListAsync(cancellationToken);

        var ddLookup = leadershipLines
            .GroupBy(x => x.BusinessAreaLookupId)
            .ToDictionary(
                g => g.Key,
                g => string.Join(", ", g
                    .Select(x => string.IsNullOrWhiteSpace(x.Name) ? (x.Email ?? "") : x.Name!.Trim())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Distinct(StringComparer.OrdinalIgnoreCase)));

        var today = DateTime.UtcNow;
        var rows = new List<RaidBusinessAreaSummaryRowVm>();

        foreach (var ba in areas)
        {
            var baId = ba.Id;
            var linkedWork = workItemCountByBa.TryGetValue(baId, out var w) ? w : 0;
            var risksHere = risks.Where(r => RiskBelongsToBusinessArea(r, baId, projectBusinessAreaId, riskBusinessAreaMap))
                .ToList();
            var issuesHere = issues.Where(i => IssueBelongsToBusinessArea(i, baId, projectBusinessAreaId)).ToList();

            var rOpen = 0;
            var rOd = 0;
            var rClosed = 0;
            var sH = 0;
            var sE = 0;
            var sM = 0;
            var sL = 0;
            foreach (var r in risksHere)
            {
                if (RiskIsOpen(r))
                {
                    rOpen++;
                    if (RiskIsOverdue(r, today))
                        rOd++;

                    var sc = r.RiskScore;
                    if (RiskScoreBandHighest(sc))
                        sH++;
                    else if (RiskScoreBandElevated(sc))
                        sE++;
                    else if (RiskScoreBandMedium(sc))
                        sM++;
                    else if (RiskScoreBandLower(sc))
                        sL++;
                }
                else
                    rClosed++;
            }

            var iOpen = 0;
            var iOd = 0;
            var iClosed = 0;
            var iLow = 0;
            var iMed = 0;
            var iHigh = 0;
            var iCrit = 0;
            foreach (var i in issuesHere)
            {
                if (IssueIsOpen(i))
                {
                    iOpen++;
                    if (IssueIsOverdue(i, today))
                        iOd++;

                    switch (IssueSeverityBucket(i))
                    {
                        case "critical":
                            iCrit++;
                            break;
                        case "high":
                            iHigh++;
                            break;
                        case "medium":
                            iMed++;
                            break;
                        default:
                            iLow++;
                            break;
                    }
                }
                else
                    iClosed++;
            }

            ddLookup.TryGetValue(baId, out var dd);

            rows.Add(new RaidBusinessAreaSummaryRowVm
            {
                BusinessAreaId = baId,
                Name = ba.Name ?? $"Business area #{baId}",
                DeputyDirectorNames = string.IsNullOrWhiteSpace(dd) ? null : dd,
                LinkedWorkItemCount = linkedWork,
                RisksOpen = rOpen,
                RisksOverdue = rOd,
                RisksClosed = rClosed,
                RiskScoreHighest = sH,
                RiskScoreElevated = sE,
                RiskScoreMedium = sM,
                RiskScoreLower = sL,
                IssuesOpen = iOpen,
                IssuesOverdue = iOd,
                IssuesClosed = iClosed,
                IssuesLow = iLow,
                IssuesMedium = iMed,
                IssuesHigh = iHigh,
                IssuesCritical = iCrit
            });
        }

        return rows;
    }

    [HttpGet("business-areas")]
    [HttpGet("/ModernRaid/BusinessAreas")]
    public async Task<IActionResult> BusinessAreas(
        string? search,
        int? businessAreaId,
        int? divisionId,
        int? projectId,
        int? filterAreaId,
        CancellationToken cancellationToken = default)
    {
        SetRaidChrome("raid-business-areas");
        var (effectiveBa, explicitNone, _) =
            await ResolveRaidRegisterBusinessAreaAsync(businessAreaId, cancellationToken);

        var matrix = await BuildBusinessAreaMatrixRowsAsync(
            effectiveBa, search, projectId, divisionId, filterAreaId, cancellationToken);

        var drillUrl = Url.Action(nameof(BusinessAreaDrill), "ModernRaid") ?? "";

        var vm = new ModernRaidBusinessAreasPageViewModel
        {
            Search = search,
            ProjectId = projectId,
            BusinessAreaId = effectiveBa,
            RaidBusinessAreaExplicitNone = explicitNone,
            DirectorateId = divisionId,
            FilterAreaId = filterAreaId,
            ProjectOptions = await RaidProjectFilterOptionsAsync(cancellationToken),
            BusinessAreaOptions = await _db.BusinessAreaLookups.AsNoTracking()
                .Where(b => b.IsActive)
                .OrderBy(b => b.SortOrder).ThenBy(b => b.Name)
                .Select(b => new RaidLookupOptionVm(b.Id, b.Name))
                .ToListAsync(cancellationToken),
            DirectorateOptions = await _db.Divisions.AsNoTracking()
                .Where(d => d.IsActive)
                .OrderBy(d => d.Name)
                .Select(d => new RaidLookupOptionVm(d.Id, d.Name))
                .ToListAsync(cancellationToken),
            SummaryRows = matrix,
            DrillEndpoint = drillUrl
        };

        return View("~/Views/Modern/Raid/BusinessAreas.cshtml", vm);
    }

    [HttpGet("business-areas/drill")]
    public async Task<IActionResult> BusinessAreaDrill(
        int areaId,
        string slice,
        string? search,
        int? projectId,
        int? businessAreaId,
        int? divisionId,
        CancellationToken cancellationToken = default)
    {
        var (effectiveBa, _, _) = await ResolveRaidRegisterBusinessAreaAsync(businessAreaId, cancellationToken);
        var today = DateTime.UtcNow;

        var projects = await _db.Projects.AsNoTracking()
            .Where(p => !p.IsDeleted)
            .Select(p => new { p.Id, p.BusinessAreaId })
            .ToListAsync(cancellationToken);
        var projectBusinessAreaId = projects.ToDictionary(p => p.Id, p => p.BusinessAreaId);

        var rbaRows = await _db.RiskBusinessAreas.AsNoTracking()
            .Select(x => new { x.RiskId, x.BusinessAreaLookupId })
            .ToListAsync(cancellationToken);
        var riskBusinessAreaMap = rbaRows.GroupBy(x => x.RiskId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.BusinessAreaLookupId).ToHashSet());

        var ba = await _db.BusinessAreaLookups.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == areaId && b.IsActive, cancellationToken);
        if (ba == null)
        {
            return Json(new RaidTierReportingDrillResponseVm
            {
                Title = "Business area not found",
                Items = Array.Empty<RaidTierReportingDrillItemVm>()
            });
        }

        slice = (slice ?? "").Trim().ToLowerInvariant();
        var areaLabel = ba.Name ?? $"Business area #{areaId}";
        List<RaidTierReportingDrillItemVm> items = new();

        string RiskUrl(int id) => Url.Action("RiskDetail", "ModernRaid", new { id }) ?? "#";
        string IssueUrl(int id) => Url.Action("IssueDetail", "ModernRaid", new { id }) ?? "#";

        var allTiers = await _db.RiskTiers.AsNoTracking()
            .Where(t => t.IsActive)
            .OrderBy(t => t.SortOrder)
            .ToListAsync(cancellationToken);
        var levelToTierName = new Dictionary<int, string>();
        foreach (var t in allTiers.Where(x => !x.IsProposedTier))
        {
            var lv = RiskTierGovernance.ResolveLevel(t, allTiers);
            if (!levelToTierName.ContainsKey(lv))
                levelToTierName[lv] = t.Name ?? "—";
        }

        string TierNameForIssue(Issue i)
        {
            var gl = MapIssueToGovernanceLevel(i);
            return levelToTierName.TryGetValue(gl, out var n) ? n : "—";
        }

        if (slice.StartsWith("risk-", StringComparison.Ordinal))
        {
            var list = await RaidTierReportingRiskQuery(search, projectId, divisionId, effectiveBa)
                .Include(r => r.Project).ThenInclude(p => p!.BusinessAreaLookup)
                .Include(r => r.PrimaryProduct)
                .Include(r => r.RiskTier)
                .Include(r => r.Likelihood)
                .Include(r => r.ImpactLevel)
                .Include(r => r.RiskBusinessAreas).ThenInclude(rba => rba.BusinessAreaLookup)
                .OrderByDescending(r => r.UpdatedAt)
                .Take(500)
                .ToListAsync(cancellationToken);

            list = list.Where(r => RiskBelongsToBusinessArea(r, areaId, projectBusinessAreaId, riskBusinessAreaMap)).ToList();

            IEnumerable<Risk> filtered = slice switch
            {
                "risk-open" => list.Where(RiskIsOpen),
                "risk-overdue" => list.Where(r => RiskIsOverdue(r, today)),
                "risk-closed" => list.Where(r => !RiskIsOpen(r)),
                "risk-highest" => list.Where(r => RiskIsOpen(r) && RiskScoreBandHighest(r.RiskScore)),
                "risk-elevated" => list.Where(r => RiskIsOpen(r) && RiskScoreBandElevated(r.RiskScore)),
                "risk-medium" => list.Where(r => RiskIsOpen(r) && RiskScoreBandMedium(r.RiskScore)),
                "risk-lower" => list.Where(r => RiskIsOpen(r) && RiskScoreBandLower(r.RiskScore)),
                _ => Array.Empty<Risk>()
            };

            var title = $"{areaLabel} — risks — {slice.Replace("risk-", "", StringComparison.Ordinal)}";
            items = filtered
                .Take(500)
                .Select(r => new RaidTierReportingDrillItemVm
                {
                    Kind = "risk",
                    Id = r.Id,
                    Title = r.Title,
                    Url = RiskUrl(r.Id),
                    Reference = $"R-{r.Id:D4}",
                    TierName = r.RiskTier?.Name ?? "—",
                    BusinessAreaLabel = RaidRegisterTableFormatting.FormatRiskBusinessAreaLabels(r),
                    LikelihoodLabel = r.Likelihood?.Label ?? r.LikelihoodRating.ToString(),
                    ImpactLabel = r.ImpactLevel?.Label ?? r.ImpactRating.ToString(),
                    RiskScore = r.RiskScore,
                    OpenedDate = FormatRaidDrillOpenedDate(r.IdentifiedDate ?? r.CreatedAt)
                })
                .ToList();

            return Json(new RaidTierReportingDrillResponseVm { Title = title, Items = items });
        }

        if (slice.StartsWith("issue-", StringComparison.Ordinal))
        {
            var issueList = await IssueQueryableAlignedToRiskRegisterFilters(
                    projectId,
                    MergeRaidRegisterFilterIds(divisionId, null),
                    MergeRaidRegisterFilterIds(effectiveBa, null),
                    search)
                .Include(i => i.SeverityLookup)
                .Include(i => i.Project).ThenInclude(p => p!.BusinessAreaLookup)
                .Include(i => i.PrimaryProduct)
                .Include(i => i.IssueBusinessAreas).ThenInclude(iba => iba.BusinessAreaLookup)
                .OrderByDescending(i => i.UpdatedAt)
                .Take(500)
                .ToListAsync(cancellationToken);

            var mapped = issueList.Where(i => IssueBelongsToBusinessArea(i, areaId, projectBusinessAreaId));

            IEnumerable<Issue> filtered = slice switch
            {
                "issue-open" => mapped.Where(IssueIsOpen),
                "issue-overdue" => mapped.Where(i => IssueIsOverdue(i, today)),
                "issue-closed" => mapped.Where(i => !IssueIsOpen(i)),
                "issue-low" => mapped.Where(i => IssueIsOpen(i) && IssueSeverityBucket(i) == "low"),
                "issue-medium" => mapped.Where(i => IssueIsOpen(i) && IssueSeverityBucket(i) == "medium"),
                "issue-high" => mapped.Where(i => IssueIsOpen(i) && IssueSeverityBucket(i) == "high"),
                "issue-critical" => mapped.Where(i => IssueIsOpen(i) && IssueSeverityBucket(i) == "critical"),
                _ => Array.Empty<Issue>()
            };

            var title = $"{areaLabel} — issues — {slice.Replace("issue-", "", StringComparison.Ordinal)}";
            items = filtered
                .Take(500)
                .Select(i => new RaidTierReportingDrillItemVm
                {
                    Kind = "issue",
                    Id = i.Id,
                    Title = i.Title,
                    Url = IssueUrl(i.Id),
                    Reference = $"I-{i.Id:D4}",
                    TierName = TierNameForIssue(i),
                    BusinessAreaLabel = RaidRegisterTableFormatting.FormatIssueBusinessAreaLabels(i),
                    IssueSeverityLabel = i.SeverityLookup?.Label ?? i.Severity,
                    OpenedDate = FormatRaidDrillOpenedDate(i.DetectedDate)
                })
                .ToList();

            return Json(new RaidTierReportingDrillResponseVm { Title = title, Items = items });
        }

        return Json(new RaidTierReportingDrillResponseVm
        {
            Title = areaLabel,
            Items = Array.Empty<RaidTierReportingDrillItemVm>()
        });
    }
}
