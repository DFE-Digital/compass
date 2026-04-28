using System;
using Compass.Models;
using Compass.ViewModels.Modern;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Compass.Controllers.Modern;

/// <summary>Directorate matrix and drill at <c>/modern/raid/directorate</c>.</summary>
public partial class ModernRaidController
{
    private static bool RiskBelongsToDirectorate(
        Risk r,
        int divisionId,
        IReadOnlyDictionary<int, HashSet<int>> projectDivisionMap,
        IReadOnlyDictionary<int, HashSet<int>> riskDivisionMap)
    {
        if (r.ProjectId is int pid &&
            projectDivisionMap.TryGetValue(pid, out var set) &&
            set.Contains(divisionId))
            return true;
        return riskDivisionMap.TryGetValue(r.Id, out var set2) && set2.Contains(divisionId);
    }

    private static bool IssueBelongsToDirectorate(
        Issue i,
        int divisionId,
        IReadOnlyDictionary<int, HashSet<int>> projectDivisionMap)
    {
        if (i.ProjectId is int pid &&
            projectDivisionMap.TryGetValue(pid, out var set) &&
            set.Contains(divisionId))
            return true;
        return i.IssueDivisions.Any(idv => idv.DivisionId == divisionId);
    }

    private async Task<List<RaidDirectorateSummaryRowVm>> BuildDirectorateMatrixRowsAsync(
        int? effectiveBusinessAreaId,
        string? search,
        int? projectId,
        int? directorateFilterId,
        CancellationToken cancellationToken)
    {
        var divisions = await _db.Divisions.AsNoTracking()
            .Where(d => d.IsActive)
            .OrderBy(d => d.Name)
            .ToListAsync(cancellationToken);

        if (directorateFilterId is > 0)
            divisions = divisions.Where(d => d.Id == directorateFilterId.Value).ToList();

        var divisionIds = divisions.Select(d => d.Id).ToList();

        var pd = await _db.ProjectDirectorates.AsNoTracking()
            .Select(x => new { x.ProjectId, x.DivisionId })
            .ToListAsync(cancellationToken);

        var rd = await _db.RiskDivisions.AsNoTracking()
            .Select(x => new { x.RiskId, x.DivisionId })
            .ToListAsync(cancellationToken);

        var projectDivisionMap = pd.GroupBy(x => x.ProjectId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.DivisionId).ToHashSet());

        var riskDivisionMap = rd.GroupBy(x => x.RiskId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.DivisionId).ToHashSet());

        var riskQuery = RaidTierReportingRiskQuery(search, projectId, null, effectiveBusinessAreaId);
        var risks = await riskQuery.ToListAsync(cancellationToken);

        var issueQuery = IssueQueryableAlignedToRiskRegisterFilters(projectId, null, effectiveBusinessAreaId, search);
        var issues = await issueQuery
            .Include(i => i.SeverityLookup)
            .Include(i => i.IssueDivisions)
            .ToListAsync(cancellationToken);

        var leaderLines = await _db.DivisionUsers.AsNoTracking()
            .Where(m => divisionIds.Contains(m.DivisionId))
            .Select(m => new { m.DivisionId, m.User.Name, m.User.Email })
            .ToListAsync(cancellationToken);
        var leaderLookup = leaderLines
            .GroupBy(x => x.DivisionId)
            .ToDictionary(
                g => g.Key,
                g => string.Join(", ", g
                    .Select(x => string.IsNullOrWhiteSpace(x.Name) ? (x.Email ?? "") : x.Name!.Trim())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Distinct(StringComparer.OrdinalIgnoreCase)));

        var today = DateTime.UtcNow;
        var rows = new List<RaidDirectorateSummaryRowVm>();

        foreach (var d in divisions)
        {
            var did = d.Id;
            var linkedProjects = pd.Count(x => x.DivisionId == did);

            var risksHere = risks.Where(r => RiskBelongsToDirectorate(r, did, projectDivisionMap, riskDivisionMap)).ToList();
            var issuesHere = issues.Where(i => IssueBelongsToDirectorate(i, did, projectDivisionMap)).ToList();

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

            leaderLookup.TryGetValue(did, out var leaders);

            rows.Add(new RaidDirectorateSummaryRowVm
            {
                DirectorateId = did,
                Name = d.Name ?? $"Directorate #{did}",
                LeadershipNames = string.IsNullOrWhiteSpace(leaders) ? null : leaders,
                LinkedWorkItemCount = linkedProjects,
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

    /// <summary>Risk and issue matrix by directorate (same columns as tier view).</summary>
    [HttpGet("directorate")]
    [HttpGet("/ModernRaid/Directorate")]
    public async Task<IActionResult> Directorate(
        string? search,
        int? divisionId,
        int? projectId,
        int? businessAreaId,
        CancellationToken cancellationToken = default)
    {
        SetRaidChrome("raid-directorate");
        var (effectiveBa, explicitNone, _) =
            await ResolveRaidRegisterBusinessAreaAsync(businessAreaId, cancellationToken);

        var matrix = await BuildDirectorateMatrixRowsAsync(effectiveBa, search, projectId, divisionId, cancellationToken);

        var drillUrl = Url.Action(nameof(DirectorateDrill), "ModernRaid") ?? "";

        var vm = new ModernRaidDirectoratePageViewModel
        {
            Search = search,
            ProjectId = projectId,
            BusinessAreaId = effectiveBa,
            RaidBusinessAreaExplicitNone = explicitNone,
            DirectorateId = divisionId,
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

        return View("~/Views/Modern/Raid/Directorate.cshtml", vm);
    }

    [HttpGet("directorate/drill")]
    public async Task<IActionResult> DirectorateDrill(
        int directorateId,
        string slice,
        string? search,
        int? projectId,
        int? businessAreaId,
        CancellationToken cancellationToken = default)
    {
        var (effectiveBa, _, _) = await ResolveRaidRegisterBusinessAreaAsync(businessAreaId, cancellationToken);
        var today = DateTime.UtcNow;

        var pd = await _db.ProjectDirectorates.AsNoTracking()
            .Select(x => new { x.ProjectId, x.DivisionId })
            .ToListAsync(cancellationToken);
        var rd = await _db.RiskDivisions.AsNoTracking()
            .Select(x => new { x.RiskId, x.DivisionId })
            .ToListAsync(cancellationToken);
        var projectDivisionMap = pd.GroupBy(x => x.ProjectId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.DivisionId).ToHashSet());
        var riskDivisionMap = rd.GroupBy(x => x.RiskId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.DivisionId).ToHashSet());

        var div = await _db.Divisions.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == directorateId && d.IsActive, cancellationToken);
        if (div == null)
        {
            return Json(new RaidTierReportingDrillResponseVm
            {
                Title = "Directorate not found",
                Items = Array.Empty<RaidTierReportingDrillItemVm>()
            });
        }

        slice = (slice ?? "").Trim().ToLowerInvariant();
        var dirLabel = div.Name ?? $"Directorate #{directorateId}";
        List<RaidTierReportingDrillItemVm> items = new();

        string RiskUrl(int id) => Url.Action("RiskDetail", "ModernRaid", new { id }) ?? "#";
        string IssueUrl(int id) => Url.Action("IssueDetail", "ModernRaid", new { id }) ?? "#";

        if (slice.StartsWith("risk-", StringComparison.Ordinal))
        {
            var list = await RaidTierReportingRiskQuery(search, projectId, null, effectiveBa)
                .Include(r => r.Project).ThenInclude(p => p!.BusinessAreaLookup)
                .Include(r => r.PrimaryProduct)
                .Include(r => r.OwnerUser)
                .Include(r => r.RiskStatus)
                .Include(r => r.Likelihood)
                .Include(r => r.ImpactLevel)
                .Include(r => r.RiskBusinessAreas).ThenInclude(rba => rba.BusinessAreaLookup)
                .OrderByDescending(r => r.UpdatedAt)
                .Take(500)
                .ToListAsync(cancellationToken);

            list = list.Where(r => RiskBelongsToDirectorate(r, directorateId, projectDivisionMap, riskDivisionMap)).ToList();

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

            var title = $"{dirLabel} — risks — {slice.Replace("risk-", "", StringComparison.Ordinal)}";
            items = filtered
                .Take(500)
                .Select(r =>
                {
                    var rel = RaidRegisterTableFormatting.BuildRiskRelation(r);
                    return new RaidTierReportingDrillItemVm
                    {
                        Kind = "risk",
                        Id = r.Id,
                        Title = r.Title,
                        Url = RiskUrl(r.Id),
                        Reference = $"R-{r.Id:D4}",
                        BusinessAreaLabel = RaidRegisterTableFormatting.FormatRiskBusinessAreaLabels(r),
                        RelationKind = rel.Kind,
                        RelationProjectId = rel.ProjectId,
                        RelationTarget = rel.Target,
                        Status = r.RiskStatus?.Label ?? r.Status,
                        Owner = r.OwnerUser != null ? (r.OwnerUser.Name ?? r.OwnerUser.Email) : r.OwnerEmail,
                        LikelihoodLabel = r.Likelihood?.Label ?? r.LikelihoodRating.ToString(),
                        ImpactLabel = r.ImpactLevel?.Label ?? r.ImpactRating.ToString(),
                        RiskScore = r.RiskScore
                    };
                })
                .ToList();

            return Json(new RaidTierReportingDrillResponseVm { Title = title, Items = items });
        }

        if (slice.StartsWith("issue-", StringComparison.Ordinal))
        {
            var issues = await IssueQueryableAlignedToRiskRegisterFilters(projectId, null, effectiveBa, search)
                .Include(i => i.SeverityLookup)
                .Include(i => i.StatusLookup)
                .Include(i => i.Project).ThenInclude(p => p!.BusinessAreaLookup)
                .Include(i => i.PrimaryProduct)
                .Include(i => i.OwnerUser)
                .Include(i => i.IssueBusinessAreas).ThenInclude(iba => iba.BusinessAreaLookup)
                .Include(i => i.IssueDivisions)
                .OrderByDescending(i => i.UpdatedAt)
                .Take(500)
                .ToListAsync(cancellationToken);

            var mapped = issues.Where(i => IssueBelongsToDirectorate(i, directorateId, projectDivisionMap));

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

            var title = $"{dirLabel} — issues — {slice.Replace("issue-", "", StringComparison.Ordinal)}";
            items = filtered
                .Take(500)
                .Select(i =>
                {
                    var rel = RaidRegisterTableFormatting.BuildIssueRelation(i);
                    return new RaidTierReportingDrillItemVm
                    {
                        Kind = "issue",
                        Id = i.Id,
                        Title = i.Title,
                        Url = IssueUrl(i.Id),
                        Reference = $"I-{i.Id:D4}",
                        BusinessAreaLabel = RaidRegisterTableFormatting.FormatIssueBusinessAreaLabels(i),
                        RelationKind = rel.Kind,
                        RelationProjectId = rel.ProjectId,
                        RelationTarget = rel.Target,
                        Status = i.StatusLookup?.Label ?? i.Status,
                        Owner = i.OwnerUser != null ? (i.OwnerUser.Name ?? i.OwnerUser.Email) : null,
                        IssueSeverityLabel = i.SeverityLookup?.Label ?? i.Severity
                    };
                })
                .ToList();

            return Json(new RaidTierReportingDrillResponseVm { Title = title, Items = items });
        }

        return Json(new RaidTierReportingDrillResponseVm
        {
            Title = dirLabel,
            Items = Array.Empty<RaidTierReportingDrillItemVm>()
        });
    }

    /// <summary>Permanent redirect from the old divisions URL.</summary>
    [HttpGet("divisions")]
    [HttpGet("/ModernRaid/Divisions")]
    public IActionResult DivisionsRedirectToDirectorate()
    {
        var q = Request.QueryString.Value;
        return RedirectPermanent((Url.Action(nameof(Directorate), "ModernRaid") ?? "/modern/raid/directorate") + q);
    }
}
