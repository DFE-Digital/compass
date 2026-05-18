using System.Globalization;
using ClosedXML.Excel;
using Compass.Models;
using Compass.Models.Modern.Work;
using Compass.ViewModels;
using Compass.ViewModels.Modern;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Compass.Controllers.Modern;

public partial class ModernRaidController
{
    [HttpGet("dashboard/export.xlsx")]
    public async Task<IActionResult> DashboardExportExcel(CancellationToken cancellationToken = default)
    {
        var risks = await _db.Risks.AsNoTracking()
            .Where(r => !r.IsDeleted)
            .AsSplitQuery()
            .Include(r => r.RiskTier)
            .Include(r => r.RiskStatus)
            .Include(r => r.RiskPriority)
            .Include(r => r.Likelihood)
            .Include(r => r.ImpactLevel)
            .Include(r => r.OwnerUser)
            .Include(r => r.Project).ThenInclude(p => p!.BusinessAreaLookup)
            .Include(r => r.PrimaryProduct)
            .Include(r => r.Proximity)
            .Include(r => r.RiskBusinessAreas).ThenInclude(x => x.BusinessAreaLookup)
            .Include(r => r.RiskActions).ThenInclude(ra => ra.Action)
            .Include(r => r.KeyRiskIndicators)
            .OrderBy(r => r.Id)
            .ToListAsync(cancellationToken);

        var issues = await _db.Issues.AsNoTracking()
            .Where(i => !i.IsDeleted)
            .AsSplitQuery()
            .Include(i => i.StatusLookup)
            .Include(i => i.PriorityLookup)
            .Include(i => i.SeverityLookup)
            .Include(i => i.OwnerUser)
            .Include(i => i.Project).ThenInclude(p => p!.BusinessAreaLookup)
            .Include(i => i.PrimaryProduct)
            .Include(i => i.IssueBusinessAreas).ThenInclude(x => x.BusinessAreaLookup)
            .OrderBy(i => i.Id)
            .ToListAsync(cancellationToken);

        using var wb = new XLWorkbook();

        var riskWs = wb.Worksheets.Add("Risks");
        var rr = 1;
        string[] riskHeaders =
        {
            "Id", "Reference", "Title", "Status", "Tier", "Inherent score", "Impact", "Likelihood",
            "Owner", "Business area", "Work item", "Product", "Proximity", "Proximity date",
            "Identified", "Next review", "Target date", "Closed", "Mitigation actions", "KRIs",
            "Created", "Updated"
        };
        for (var c = 0; c < riskHeaders.Length; c++)
            riskWs.Cell(rr, c + 1).Value = riskHeaders[c];
        foreach (var r in risks)
        {
            rr++;
            var ba = RaidRegisterTableFormatting.FormatRiskBusinessAreaLabels(r);
            var rel = RaidRegisterTableFormatting.BuildRiskRelation(r);
            riskWs.Cell(rr, 1).Value = r.Id;
            riskWs.Cell(rr, 2).Value = $"R-{r.Id:D4}";
            riskWs.Cell(rr, 3).Value = r.Title;
            riskWs.Cell(rr, 4).Value = r.RiskStatus?.Label ?? r.Status;
            riskWs.Cell(rr, 5).Value = r.RiskTier?.Name;
            riskWs.Cell(rr, 6).Value = r.RiskScore;
            riskWs.Cell(rr, 7).Value = r.ImpactLevel?.Label ?? r.ImpactRating.ToString();
            riskWs.Cell(rr, 8).Value = r.Likelihood?.Label ?? r.LikelihoodRating.ToString();
            riskWs.Cell(rr, 9).Value = r.OwnerUser != null ? (r.OwnerUser.Name ?? r.OwnerUser.Email) : r.OwnerEmail;
            riskWs.Cell(rr, 10).Value = ba;
            riskWs.Cell(rr, 11).Value = rel.Kind == RaidRegisterRelationKinds.Work ? rel.Target : "";
            riskWs.Cell(rr, 12).Value = rel.Kind == RaidRegisterRelationKinds.Fips ? rel.Target : "";
            riskWs.Cell(rr, 13).Value = r.Proximity?.Label;
            riskWs.Cell(rr, 14).Value = r.ProximityDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "";
            riskWs.Cell(rr, 15).Value = r.IdentifiedDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "";
            riskWs.Cell(rr, 16).Value = r.NextReviewDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "";
            riskWs.Cell(rr, 17).Value = r.TargetDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "";
            riskWs.Cell(rr, 18).Value = r.ClosedDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "";
            riskWs.Cell(rr, 19).Value = r.RiskActions.Count;
            riskWs.Cell(rr, 20).Value = r.KeyRiskIndicators.Count;
            riskWs.Cell(rr, 21).Value = r.CreatedAt.ToString("u", CultureInfo.InvariantCulture);
            riskWs.Cell(rr, 22).Value = r.UpdatedAt.ToString("u", CultureInfo.InvariantCulture);
        }

        riskWs.Row(1).Style.Font.Bold = true;
        riskWs.SheetView.FreezeRows(1);
        riskWs.Columns().AdjustToContents();

        var issueWs = wb.Worksheets.Add("Issues");
        var ir = 1;
        string[] issueHeaders =
        {
            "Id", "Reference", "Title", "Status", "Severity", "Priority", "Owner",
            "Business area", "Work item", "Product", "Target resolution", "Closed", "Created", "Updated"
        };
        for (var c = 0; c < issueHeaders.Length; c++)
            issueWs.Cell(ir, c + 1).Value = issueHeaders[c];
        foreach (var i in issues)
        {
            ir++;
            var ba = RaidRegisterTableFormatting.FormatIssueBusinessAreaLabels(i);
            var rel = RaidRegisterTableFormatting.BuildIssueRelation(i);
            issueWs.Cell(ir, 1).Value = i.Id;
            issueWs.Cell(ir, 2).Value = $"I-{i.Id:D4}";
            issueWs.Cell(ir, 3).Value = i.Title;
            issueWs.Cell(ir, 4).Value = i.StatusLookup?.Label ?? i.Status;
            issueWs.Cell(ir, 5).Value = i.SeverityLookup?.Label ?? i.Severity;
            issueWs.Cell(ir, 6).Value = i.PriorityLookup?.Label ?? i.Priority;
            issueWs.Cell(ir, 7).Value = i.OwnerUser != null ? (i.OwnerUser.Name ?? i.OwnerUser.Email) : null;
            issueWs.Cell(ir, 8).Value = ba;
            issueWs.Cell(ir, 9).Value = rel.Kind == RaidRegisterRelationKinds.Work ? rel.Target : "";
            issueWs.Cell(ir, 10).Value = rel.Kind == RaidRegisterRelationKinds.Fips ? rel.Target : "";
            issueWs.Cell(ir, 11).Value = i.TargetResolutionDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "";
            issueWs.Cell(ir, 12).Value = i.ClosedDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "";
            issueWs.Cell(ir, 13).Value = i.CreatedAt.ToString("u", CultureInfo.InvariantCulture);
            issueWs.Cell(ir, 14).Value = i.UpdatedAt.ToString("u", CultureInfo.InvariantCulture);
        }

        issueWs.Row(1).Style.Font.Bold = true;
        issueWs.SheetView.FreezeRows(1);
        issueWs.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return File(
            ms.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"raid-export-{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx");
    }

    private async Task<ModernRaidDashboardViewModel> BuildRaidDashboardViewModelAsync(
        int? userId,
        string emailLower,
        string displayName,
        List<int> projectIds,
        List<int> productServiceIds,
        List<int> adminBusinessAreaIds,
        RaidDashboardScopeContext? scope = null,
        string? risksRegisterUrlOverride = null,
        string? issuesRegisterUrlOverride = null,
        string? tierUrlOverride = null,
        CancellationToken cancellationToken = default)
    {
        var isScoped = scope != null;

        IQueryable<Risk> filPersonalRisksQ;
        IQueryable<Issue> filPersonalIssuesQ;
        IQueryable<Risk> filOrgRisksQ;
        IQueryable<Issue> filOrgIssuesQ;

        if (isScoped)
        {
            filPersonalRisksQ = scope!.ScopedRisks;
            filPersonalIssuesQ = scope.ScopedIssues;
            filOrgRisksQ = scope.ScopedRisks;
            filOrgIssuesQ = scope.ScopedIssues;
        }
        else
        {
            var personalRiskScope = RaidRiskMatchesViewerScope(userId, emailLower, projectIds, productServiceIds, adminBusinessAreaIds);
            var personalIssueScope = RaidIssueMatchesViewerScope(userId, projectIds, productServiceIds, adminBusinessAreaIds);

            filPersonalRisksQ = _db.Risks.AsNoTracking().Where(r => !r.IsDeleted).Where(personalRiskScope);
            filPersonalIssuesQ = _db.Issues.AsNoTracking().Where(i => !i.IsDeleted).Where(personalIssueScope);
            filOrgRisksQ = _db.Risks.AsNoTracking().Where(r => !r.IsDeleted);
            filOrgIssuesQ = _db.Issues.AsNoTracking().Where(i => !i.IsDeleted);
        }

        var yourOpenRiskCount = await filPersonalRisksQ.Where(r => r.ClosedDate == null).CountAsync(cancellationToken);
        var yourOpenIssueCount = await filPersonalIssuesQ.Where(i => i.ClosedDate == null).CountAsync(cancellationToken);

        var risksRegisterUrl = risksRegisterUrlOverride ?? Url.Action("Risks", "ModernRaid") ?? "/modern/raid/risks";
        var issuesRegisterUrl = issuesRegisterUrlOverride ?? Url.Action("Issues", "ModernRaid") ?? "/modern/raid/issues";

        var utcNow = DateTime.UtcNow;

        var tableRisks = await filPersonalRisksQ
            .Where(r => r.ClosedDate == null)
            .Include(r => r.OwnerUser)
            .Include(r => r.Likelihood)
            .Include(r => r.ImpactLevel)
            .Include(r => r.RiskPriority)
            .Include(r => r.RiskTier)
            .OrderByDescending(r => r.UpdatedAt)
            .Take(250)
            .ToListAsync(cancellationToken);

        var tableIssues = await filPersonalIssuesQ
            .Where(i => i.ClosedDate == null)
            .Include(i => i.OwnerUser)
            .Include(i => i.SeverityLookup)
            .Include(i => i.PriorityLookup)
            .OrderByDescending(i => i.UpdatedAt)
            .Take(250)
            .ToListAsync(cancellationToken);

        var elevated = tableRisks.Count(r => r.RiskScore >= 15);
        var attentionRows = BuildDashboardAttentionRows(tableRisks, tableIssues, utcNow);

        var tableRows = tableRisks.Select(r =>
            {
                var score = r.RiskScore;
                return new ModernRaidDashboardSummaryRowVm(
                    "Risk",
                    r.Id,
                    $"R-{r.Id:D4}",
                    r.Title,
                    WorkBadgeCss.RaidRiskScoreDfeFrontendBadgeClass(score),
                    score.ToString(CultureInfo.InvariantCulture),
                    $"Risk score {score}",
                    DashboardOwnerName(r.OwnerUser),
                    r.UpdatedAt);
            })
            .Concat(tableIssues.Select(i =>
            {
                var sev = i.SeverityLookup?.Label ?? i.Severity ?? "—";
                return new ModernRaidDashboardSummaryRowVm(
                    "Issue",
                    i.Id,
                    $"I-{i.Id:D4}",
                    i.Title,
                    WorkBadgeCss.RaidSeverityLabelDfeFrontendBadgeClass(sev) + " dfe-f-badge--small",
                    sev.ToUpperInvariant(),
                    $"Severity {sev}",
                    DashboardOwnerName(i.OwnerUser),
                    i.UpdatedAt);
            }))
            .OrderByDescending(x => x.UpdatedAt)
            .ToList();

        var openNearMissCount = await GetOpenNearMissCountAsync(cancellationToken);

        return new ModernRaidDashboardViewModel
        {
            ScopeKind = isScoped ? scope!.Kind : RaidDashboardScopeKind.Viewer,
            ScopeTitle = isScoped ? scope!.Title : null,
            ScopeListUrl = isScoped ? scope!.ListUrl : null,
            ScopeDirectorateId = isScoped ? scope!.DirectorateId : null,
            ScopeBusinessAreaId = isScoped ? scope!.BusinessAreaId : null,
            ScopeSubtitle = isScoped ? scope!.Subtitle : null,
            ScopeSummary = isScoped ? scope!.Summary : "",
            YourOpenRiskCount = yourOpenRiskCount,
            YourOpenIssueCount = yourOpenIssueCount,
            ElevatedOpenRiskCount = elevated,
            TableRows = tableRows,
            AttentionRows = attentionRows,
            DataAsAtUtc = utcNow,
            OpenNearMissCount = openNearMissCount
        };
    }

    private static string? DashboardOwnerName(User? user)
    {
        if (user == null)
            return null;

        if (!string.IsNullOrWhiteSpace(user.Name))
            return user.Name.Trim();

        var firstLast = $"{user.FirstName} {user.LastName}".Trim();
        return string.IsNullOrWhiteSpace(firstLast) ? null : firstLast;
    }

    private static List<ModernRaidDashboardAttentionRowVm> BuildDashboardAttentionRows(
        List<Risk> openRisks,
        List<Issue> openIssues,
        DateTime todayUtc)
    {
        var items = new List<ModernRaidDashboardAttentionRowVm>();

        foreach (var r in openRisks)
        {
            string? reason = null;
            if (RiskIsOverdue(r, todayUtc))
                reason = "Review or target date overdue";
            else if (RiskScoreBandHighest(r.RiskScore))
                reason = "Highest inherent risk score";
            else if (RiskScoreBandElevated(r.RiskScore))
                reason = "Elevated inherent risk score";
            if (reason == null)
                continue;
            items.Add(new ModernRaidDashboardAttentionRowVm(
                "Risk", r.Id, $"R-{r.Id:D4}", r.Title, reason));
        }

        foreach (var i in openIssues)
        {
            var bucket = IssueSeverityBucket(i);
            string? reason = null;
            if (IssueIsOverdue(i, todayUtc))
                reason = "Target resolution overdue";
            else if (bucket is "critical" or "high")
                reason = "High severity issue";
            if (reason == null)
                continue;
            items.Add(new ModernRaidDashboardAttentionRowVm(
                "Issue", i.Id, $"I-{i.Id:D4}", i.Title, reason));
        }

        return items
            .OrderByDescending(x => x.Reason.Contains("Highest", StringComparison.OrdinalIgnoreCase))
            .ThenBy(x => x.Kind)
            .ThenBy(x => x.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

}
