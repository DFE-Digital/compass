using System;
using System.Globalization;
using ClosedXML.Excel;
using Compass.Models;
using Compass.Services.Raid;
using Compass.ViewModels.Modern;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Compass.Controllers.Modern;

/// <summary>Tier governance summary matrix and drill/export at <c>/modern/raid/tier</c>.</summary>
public partial class ModernRaidController
{
    private static bool RiskScoreBandLower(int s) => s <= 7;
    private static bool RiskScoreBandMedium(int s) => s is >= 8 and <= 14;
    private static bool RiskScoreBandElevated(int s) => s is >= 15 and <= 19;
    private static bool RiskScoreBandHighest(int s) => s is >= 20 and <= 25;

    private static int MapIssueToGovernanceLevel(Issue i)
    {
        var lab = (i.SeverityLookup?.Label ?? i.Severity ?? "").Trim();
        var ll = lab.ToLowerInvariant();
        if (ll.Contains("critical") || ll.Contains("high") || ll.Contains("major"))
            return 1;
        if (ll.Contains("medium"))
            return 2;
        if (ll.Contains("low"))
            return 3;
        return 3;
    }

    private static string IssueSeverityBucket(Issue i)
    {
        var lab = (i.SeverityLookup?.Label ?? i.Severity ?? "").Trim();
        var ll = lab.ToLowerInvariant();
        if (ll.Contains("critical"))
            return "critical";
        if (ll.Contains("high"))
            return "high";
        if (ll.Contains("medium"))
            return "medium";
        if (ll.Contains("low"))
            return "low";
        return "low";
    }

    private static bool RiskIsOpen(Risk r) => r.ClosedDate == null;

    private static bool RiskIsOverdue(Risk r, DateTime todayUtc)
    {
        if (!RiskIsOpen(r))
            return false;
        if (r.NextReviewDate.HasValue && r.NextReviewDate.Value.Date < todayUtc.Date)
            return true;
        if (r.TargetDate.HasValue && r.TargetDate.Value.Date < todayUtc.Date)
            return true;
        return false;
    }

    private static bool IssueIsOpen(Issue i) => i.ClosedDate == null;

    private static bool IssueIsOverdue(Issue i, DateTime todayUtc) =>
        IssueIsOpen(i) &&
        i.TargetResolutionDate.HasValue &&
        i.TargetResolutionDate.Value.Date < todayUtc.Date;

    /// <summary>Aligned to the risk register (no tab, no tier filter).</summary>
    private IQueryable<Risk> RaidTierReportingRiskQuery(
        string? search,
        int? projectId,
        int? divisionId,
        int? effectiveBusinessAreaId)
    {
        IQueryable<Risk> q = _db.Risks.AsNoTracking().Where(r => !r.IsDeleted);

        if (projectId is > 0)
            q = q.Where(r => r.ProjectId == projectId);

        if (divisionId is > 0)
        {
            q = q.Where(r => r.ProjectId != null &&
                _db.ProjectDirectorates.Any(pd => pd.ProjectId == r.ProjectId && pd.DivisionId == divisionId));
        }

        if (effectiveBusinessAreaId is > 0)
        {
            var baId = effectiveBusinessAreaId.Value;
            q = q.Where(r =>
                (r.ProjectId != null &&
                 _db.Projects.Any(p => p.Id == r.ProjectId && p.BusinessAreaId == baId)) ||
                r.RiskBusinessAreas.Any(rba => rba.BusinessAreaLookupId == baId));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var t = search.Trim();
            q = q.Where(r =>
                r.Title.Contains(t) ||
                (r.Description != null && r.Description.Contains(t)) ||
                (r.Notes != null && r.Notes.Contains(t)));
        }

        return q;
    }

    [HttpGet("tier")]
    [HttpGet("/ModernRaid/Tier")]
    public async Task<IActionResult> Tier(
        string? search,
        int? projectId,
        int? directorateId,
        int? businessAreaId,
        CancellationToken cancellationToken = default)
    {
        SetRaidChrome("raid-tier");
        var (effectiveBa, explicitNone, _) =
            await ResolveRaidRegisterBusinessAreaAsync(businessAreaId, cancellationToken);

        var today = DateTime.UtcNow;
        var allTiers = await _db.RiskTiers.AsNoTracking()
            .Where(t => t.IsActive)
            .OrderBy(t => t.SortOrder).ThenBy(t => t.Id)
            .ToListAsync(cancellationToken);

        var riskQuery = RaidTierReportingRiskQuery(search, projectId, directorateId, effectiveBa);
        var issueQuery = IssueQueryableAlignedToRiskRegisterFilters(projectId, directorateId, effectiveBa, search);

        var risks = await riskQuery
            .Include(r => r.RiskTier)
            .Include(r => r.Project)
            .OrderByDescending(r => r.UpdatedAt)
            .ToListAsync(cancellationToken);

        var issues = await issueQuery
            .Include(i => i.SeverityLookup)
            .Include(i => i.Project)
            .OrderByDescending(i => i.UpdatedAt)
            .ToListAsync(cancellationToken);

        var levelToOperationalTierId = new Dictionary<int, int>();
        foreach (var t in allTiers.Where(x => !x.IsProposedTier))
        {
            var lv = RiskTierGovernance.ResolveLevel(t, allTiers);
            if (!levelToOperationalTierId.ContainsKey(lv))
                levelToOperationalTierId[lv] = t.Id;
        }

        int? OperationalTierForIssue(Issue i)
        {
            var gl = MapIssueToGovernanceLevel(i);
            return levelToOperationalTierId.TryGetValue(gl, out var tid) ? tid : null;
        }

        var matrixRows = new List<RaidTierReportingMatrixRowVm>();
        foreach (var tier in allTiers)
        {
            var risksInTier = risks.Where(r => r.RiskTierId == tier.Id).ToList();

            List<Issue> issuesForRow;
            if (tier.IsProposedTier)
                issuesForRow = new List<Issue>();
            else
                issuesForRow = issues.Where(i => OperationalTierForIssue(i) == tier.Id).ToList();

            int rOpen = 0, rOd = 0, rClosed = 0;
            int sH = 0, sE = 0, sM = 0, sL = 0;
            foreach (var r in risksInTier)
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

            int iOpen = 0, iOd = 0, iClosed = 0;
            int iLow = 0, iMed = 0, iHigh = 0, iCrit = 0;
            foreach (var i in issuesForRow)
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

            matrixRows.Add(new RaidTierReportingMatrixRowVm
            {
                TierId = tier.Id,
                TierName = tier.IsProposedTier ? $"{tier.Name} (proposed)" : tier.Name,
                IsProposedTier = tier.IsProposedTier,
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

        var drillUrl = Url.Action("TierDrill", "ModernRaid") ?? "";
        var exportBase = Url.Action("TierExportExcel", "ModernRaid") ?? "";
        var exportUrl = exportBase +
            "?search=" + Uri.EscapeDataString(search ?? "") +
            "&projectId=" + (projectId ?? 0) +
            "&directorateId=" + (directorateId ?? 0) +
            (effectiveBa is > 0 ? "&businessAreaId=" + effectiveBa : explicitNone ? "&businessAreaId=0" : "");

        var vm = new ModernRaidTierReportingViewModel
        {
            Search = search,
            ProjectId = projectId,
            BusinessAreaId = effectiveBa,
            RaidBusinessAreaExplicitNone = explicitNone,
            DirectorateId = directorateId,
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
            MatrixRows = matrixRows,
            DrillEndpoint = drillUrl,
            ExportExcelUrl = exportUrl
        };

        return View("~/Views/Modern/Raid/TierReporting.cshtml", vm);
    }

    [HttpGet("tier/drill")]
    public async Task<IActionResult> TierDrill(
        int tierId,
        string slice,
        string? search,
        int? projectId,
        int? directorateId,
        int? businessAreaId,
        CancellationToken cancellationToken = default)
    {
        var (effectiveBa, _, _) = await ResolveRaidRegisterBusinessAreaAsync(businessAreaId, cancellationToken);
        var today = DateTime.UtcNow;

        var allTiers = await _db.RiskTiers.AsNoTracking()
            .Where(t => t.IsActive)
            .ToListAsync(cancellationToken);
        var tier = allTiers.FirstOrDefault(t => t.Id == tierId);
        if (tier == null)
            return Json(new RaidTierReportingDrillResponseVm { Title = "Tier not found", Items = Array.Empty<RaidTierReportingDrillItemVm>() });

        var riskQuery = RaidTierReportingRiskQuery(search, projectId, directorateId, effectiveBa);
        var issueBase = IssueQueryableAlignedToRiskRegisterFilters(projectId, directorateId, effectiveBa, search);

        var levelToOperationalTierId = new Dictionary<int, int>();
        foreach (var t in allTiers.Where(x => !x.IsProposedTier))
        {
            var lv = RiskTierGovernance.ResolveLevel(t, allTiers);
            if (!levelToOperationalTierId.ContainsKey(lv))
                levelToOperationalTierId[lv] = t.Id;
        }

        slice = (slice ?? "").Trim().ToLowerInvariant();
        string title = $"{tier.Name} — {slice}";
        List<RaidTierReportingDrillItemVm> items = new();

        string RiskUrl(int id) => Url.Action("RiskDetail", "ModernRaid", new { id }) ?? "#";
        string IssueUrl(int id) => Url.Action("IssueDetail", "ModernRaid", new { id }) ?? "#";

        if (slice.StartsWith("risk-", StringComparison.Ordinal))
        {
            var list = await riskQuery
                .Where(r => r.RiskTierId == tierId)
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

            title = $"{tier.Name} — risks — {slice.Replace("risk-", "", StringComparison.Ordinal)}";
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
        }
        else if (slice.StartsWith("issue-", StringComparison.Ordinal))
        {
            if (tier.IsProposedTier)
            {
                return Json(new RaidTierReportingDrillResponseVm
                {
                    Title = "Issues by tier",
                    Items = Array.Empty<RaidTierReportingDrillItemVm>()
                });
            }

            var issues = await issueBase
                .Include(i => i.SeverityLookup)
                .Include(i => i.StatusLookup)
                .Include(i => i.Project).ThenInclude(p => p!.BusinessAreaLookup)
                .Include(i => i.PrimaryProduct)
                .Include(i => i.OwnerUser)
                .Include(i => i.IssueBusinessAreas).ThenInclude(iba => iba.BusinessAreaLookup)
                .OrderByDescending(i => i.UpdatedAt)
                .ToListAsync(cancellationToken);

            IEnumerable<Issue> mapped = issues.Where(i =>
            {
                var gl = MapIssueToGovernanceLevel(i);
                return levelToOperationalTierId.TryGetValue(gl, out var tid) && tid == tierId;
            });

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

            title = $"{tier.Name} — issues — {slice.Replace("issue-", "", StringComparison.Ordinal)}";
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
        }

        return Json(new RaidTierReportingDrillResponseVm { Title = title, Items = items });
    }

    [HttpGet("tier/export.xlsx")]
    public async Task<IActionResult> TierExportExcel(
        string? search,
        int? projectId,
        int? directorateId,
        int? businessAreaId,
        CancellationToken cancellationToken = default)
    {
        var (effectiveBa, _, _) = await ResolveRaidRegisterBusinessAreaAsync(businessAreaId, cancellationToken);
        var today = DateTime.UtcNow;

        var allTiers = await _db.RiskTiers.AsNoTracking()
            .Where(t => t.IsActive)
            .OrderBy(t => t.SortOrder)
            .ToListAsync(cancellationToken);

        var risks = await RaidTierReportingRiskQuery(search, projectId, directorateId, effectiveBa)
            .Include(r => r.RiskTier)
            .Include(r => r.Project)
            .Include(r => r.RiskStatus)
            .OrderBy(r => r.RiskTierId).ThenBy(r => r.Id)
            .ToListAsync(cancellationToken);

        var issues = await IssueQueryableAlignedToRiskRegisterFilters(projectId, directorateId, effectiveBa, search)
            .Include(i => i.SeverityLookup)
            .Include(i => i.Project)
            .Include(i => i.StatusLookup)
            .OrderBy(i => i.Id)
            .ToListAsync(cancellationToken);

        using var wb = new XLWorkbook();

        void WriteSummarySheet()
        {
            var ws = wb.Worksheets.Add("Tier summary");
            var r = 1;
            ws.Cell(r, 1).Value = "Tier";
            ws.Cell(r, 2).Value = "Proposed";
            ws.Cell(r, 3).Value = "Risks open";
            ws.Cell(r, 4).Value = "Risks overdue";
            ws.Cell(r, 5).Value = "Risks closed";
            ws.Cell(r, 6).Value = "Score 20–25";
            ws.Cell(r, 7).Value = "Score 15–19";
            ws.Cell(r, 8).Value = "Score 8–14";
            ws.Cell(r, 9).Value = "Score 1–7";
            ws.Cell(r, 10).Value = "Issues open";
            ws.Cell(r, 11).Value = "Issues overdue";
            ws.Cell(r, 12).Value = "Issues closed";
            ws.Cell(r, 13).Value = "Issue low";
            ws.Cell(r, 14).Value = "Issue medium";
            ws.Cell(r, 15).Value = "Issue high";
            ws.Cell(r, 16).Value = "Issue critical";

            var levelToOperationalTierId = new Dictionary<int, int>();
            foreach (var t in allTiers.Where(x => !x.IsProposedTier))
            {
                var lv = RiskTierGovernance.ResolveLevel(t, allTiers);
                if (!levelToOperationalTierId.ContainsKey(lv))
                    levelToOperationalTierId[lv] = t.Id;
            }

            int? OperationalTierForIssue(Issue i)
            {
                var gl = MapIssueToGovernanceLevel(i);
                return levelToOperationalTierId.TryGetValue(gl, out var tid) ? tid : null;
            }

            foreach (var tier in allTiers)
            {
                r++;
                var risksInTier = risks.Where(x => x.RiskTierId == tier.Id).ToList();
                List<Issue> issuesForRow = tier.IsProposedTier
                    ? new List<Issue>()
                    : issues.Where(i => OperationalTierForIssue(i) == tier.Id).ToList();

                int rOpen = 0, rOd = 0, rClosed = 0, sH = 0, sE = 0, sM = 0, sL = 0;
                foreach (var rk in risksInTier)
                {
                    if (RiskIsOpen(rk))
                    {
                        rOpen++;
                        if (RiskIsOverdue(rk, today))
                            rOd++;
                        var sc = rk.RiskScore;
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

                int iOpen = 0, iOd = 0, iClosed = 0, iLow = 0, iMed = 0, iHigh = 0, iCrit = 0;
                foreach (var i in issuesForRow)
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

                ws.Cell(r, 1).Value = tier.Name;
                ws.Cell(r, 2).Value = tier.IsProposedTier ? "Yes" : "No";
                ws.Cell(r, 3).Value = rOpen;
                ws.Cell(r, 4).Value = rOd;
                ws.Cell(r, 5).Value = rClosed;
                ws.Cell(r, 6).Value = sH;
                ws.Cell(r, 7).Value = sE;
                ws.Cell(r, 8).Value = sM;
                ws.Cell(r, 9).Value = sL;
                ws.Cell(r, 10).Value = iOpen;
                ws.Cell(r, 11).Value = iOd;
                ws.Cell(r, 12).Value = iClosed;
                ws.Cell(r, 13).Value = iLow;
                ws.Cell(r, 14).Value = iMed;
                ws.Cell(r, 15).Value = iHigh;
                ws.Cell(r, 16).Value = iCrit;
            }

            ws.Row(1).Style.Font.Bold = true;
            ws.Columns().AdjustToContents();
        }

        void WriteRisksSheet()
        {
            var ws = wb.Worksheets.Add("Risks");
            var r = 1;
            ws.Cell(r, 1).Value = "ID";
            ws.Cell(r, 2).Value = "Title";
            ws.Cell(r, 3).Value = "Tier";
            ws.Cell(r, 4).Value = "Score";
            ws.Cell(r, 5).Value = "Status";
            ws.Cell(r, 6).Value = "Work item";
            ws.Cell(r, 7).Value = "Closed";
            ws.Cell(r, 8).Value = "Next review";
            ws.Cell(r, 9).Value = "Target date";
            foreach (var rk in risks)
            {
                r++;
                ws.Cell(r, 1).Value = rk.Id;
                ws.Cell(r, 2).Value = rk.Title;
                ws.Cell(r, 3).Value = rk.RiskTier?.Name ?? "—";
                ws.Cell(r, 4).Value = rk.RiskScore;
                ws.Cell(r, 5).Value = rk.RiskStatus?.Label ?? rk.Status;
                ws.Cell(r, 6).Value = rk.Project?.Title ?? "—";
                ws.Cell(r, 7).Value = rk.ClosedDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "";
                ws.Cell(r, 8).Value = rk.NextReviewDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "";
                ws.Cell(r, 9).Value = rk.TargetDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "";
            }

            ws.Row(1).Style.Font.Bold = true;
            ws.Columns().AdjustToContents();
        }

        void WriteIssuesSheet()
        {
            var ws = wb.Worksheets.Add("Issues");
            var r = 1;
            ws.Cell(r, 1).Value = "ID";
            ws.Cell(r, 2).Value = "Title";
            ws.Cell(r, 3).Value = "Severity";
            ws.Cell(r, 4).Value = "Status";
            ws.Cell(r, 5).Value = "Work item";
            ws.Cell(r, 6).Value = "Target resolution";
            ws.Cell(r, 7).Value = "Closed";
            foreach (var i in issues)
            {
                r++;
                ws.Cell(r, 1).Value = i.Id;
                ws.Cell(r, 2).Value = i.Title;
                ws.Cell(r, 3).Value = i.SeverityLookup?.Label ?? i.Severity;
                ws.Cell(r, 4).Value = i.StatusLookup?.Label ?? i.Status;
                ws.Cell(r, 5).Value = i.Project?.Title ?? "—";
                ws.Cell(r, 6).Value = i.TargetResolutionDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "";
                ws.Cell(r, 7).Value = i.ClosedDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "";
            }

            ws.Row(1).Style.Font.Bold = true;
            ws.Columns().AdjustToContents();
        }

        WriteSummarySheet();
        WriteRisksSheet();
        WriteIssuesSheet();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        var fname = $"raid-tier-report-{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx";
        return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fname);
    }
}
