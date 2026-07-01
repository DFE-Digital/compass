using System.Globalization;
using ClosedXML.Excel;
using Compass.Data;
using Compass.Models;
using Compass.Models.Modern.Work;
using Compass.Services;
using Compass.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Compass.Services.Modern;

/// <summary>
/// Standard work export workbook: Work, Milestones, Updates, Risks, Issues, Assumptions, Decisions, Near misses, Accessibility.
/// </summary>
public class WorkScopedExcelExportService : IWorkScopedExcelExportService
{
    private readonly CompassDbContext _db;
    private readonly IModernWorkService _modernWork;
    private readonly IMonthlyUpdateService _monthlyUpdateService;

    public WorkScopedExcelExportService(
        CompassDbContext db,
        IModernWorkService modernWork,
        IMonthlyUpdateService monthlyUpdateService)
    {
        _db = db;
        _modernWork = modernWork;
        _monthlyUpdateService = monthlyUpdateService;
    }

    public async Task<byte[]> BuildWorkbookAsync(
        IReadOnlyList<int> projectIds,
        User currentUser,
        string userEmail,
        IUrlHelper urlHelper,
        CancellationToken cancellationToken = default)
    {
        var ids = projectIds.Distinct().Where(id => id > 0).ToList();
        if (ids.Count == 0)
        {
            using var emptyWb = new XLWorkbook();
            emptyWb.Worksheets.Add("Work");
            using var emptyStream = new MemoryStream();
            emptyWb.SaveAs(emptyStream);
            return emptyStream.ToArray();
        }

        using var workbook = new XLWorkbook();

        var registerRows = await BuildRegisterRowsAsync(ids, currentUser, userEmail, urlHelper, cancellationToken);
        var periodColumns = await WorkRegisterMonthlySubmissionExportHelper.EnrichRegisterRowsWithMonthlyPeriodsAsync(
            _db, _monthlyUpdateService, registerRows, cancellationToken);
        WorkRegisterExcelExport.WriteWorkListSheet(workbook.Worksheets.Add("Work"), registerRows, periodColumns);

        var titleByProjectId = registerRows.ToDictionary(r => r.Id, r => r.Title ?? "");

        var milestones = await _db.Milestones.AsNoTracking()
            .Where(m => !m.IsDeleted && m.ProjectId != null && ids.Contains(m.ProjectId.Value))
            .OrderBy(m => m.ProjectId).ThenBy(m => m.DueDate)
            .ToListAsync(cancellationToken);
        WorkRegisterExcelExport.WriteMilestonesSheet(workbook.Worksheets.Add("Milestones"), milestones, titleByProjectId);

        await WriteUpdatesSheetAsync(workbook.Worksheets.Add("Updates"), ids, titleByProjectId, cancellationToken);
        await WriteRisksSheetAsync(workbook.Worksheets.Add("Risks"), ids, titleByProjectId, cancellationToken);
        await WriteIssuesSheetAsync(workbook.Worksheets.Add("Issues"), ids, titleByProjectId, cancellationToken);
        await WriteAssumptionsSheetAsync(workbook.Worksheets.Add("Assumptions"), ids, titleByProjectId, cancellationToken);
        await WriteDecisionsSheetAsync(workbook.Worksheets.Add("Decisions"), ids, titleByProjectId, cancellationToken);
        await WriteNearMissesSheetAsync(workbook.Worksheets.Add("Near misses"), ids, cancellationToken);
        await WriteAccessibilitySheetAsync(workbook.Worksheets.Add("Accessibility"), ids, titleByProjectId, cancellationToken);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private async Task<IReadOnlyList<WorkRegisterRow>> BuildRegisterRowsAsync(
        IReadOnlyList<int> projectIds,
        User currentUser,
        string userEmail,
        IUrlHelper urlHelper,
        CancellationToken cancellationToken)
    {
        return await _modernWork.BuildWorkRegisterExportRowsAsync(
            isMyWork: false,
            search: null,
            portfolioId: null,
            directorateId: null,
            phaseId: null,
            ragId: null,
            priorityId: null,
            monthlyUpdate: null,
            currentUser,
            userEmail,
            urlHelper,
            exportTab: "all",
            projectIds: projectIds.ToArray(),
            cancellationToken: cancellationToken);
    }

    private async Task WriteUpdatesSheetAsync(
        IXLWorksheet ws,
        List<int> projectIds,
        IReadOnlyDictionary<int, string> titleByProjectId,
        CancellationToken cancellationToken)
    {
        var headers = new[]
        {
            "Work item", "Work item ID", "Year", "Month", "Submitted", "Perm FTE", "MSP FTE",
            "Narrative", "Draft RAG", "RAG justification", "Path to green"
        };
        WriteHeaderRow(ws, headers);

        var updates = await _db.ProjectMonthlyUpdates.AsNoTracking()
            .Include(m => m.DraftRagStatusLookup)
            .Where(m => projectIds.Contains(m.ProjectId))
            .OrderBy(m => m.ProjectId).ThenByDescending(m => m.Year).ThenByDescending(m => m.Month)
            .ToListAsync(cancellationToken);

        var muIds = updates.Select(m => m.Id).ToList();
        var narratives = muIds.Count == 0
            ? new Dictionary<int, string>()
            : await _db.MonthlyUpdateNarratives.AsNoTracking()
                .Where(n => muIds.Contains(n.ProjectMonthlyUpdateId))
                .GroupBy(n => n.ProjectMonthlyUpdateId)
                .ToDictionaryAsync(
                    g => g.Key,
                    g => string.Join("\n---\n", g.Select(x => x.Narrative)),
                    cancellationToken);

        var row = 2;
        foreach (var m in updates)
        {
            titleByProjectId.TryGetValue(m.ProjectId, out var title);
            narratives.TryGetValue(m.Id, out var extra);
            var narrative = string.Join("\n---\n",
                new[] { m.Narrative, extra }.Where(s => !string.IsNullOrWhiteSpace(s)));

            ws.Cell(row, 1).Value = title ?? "";
            ws.Cell(row, 2).Value = m.ProjectId;
            ws.Cell(row, 3).Value = m.Year;
            ws.Cell(row, 4).Value = m.Month;
            ws.Cell(row, 5).Value = m.SubmittedAt.HasValue ? m.SubmittedAt.Value.ToString("u", CultureInfo.InvariantCulture) : "";
            ws.Cell(row, 6).Value = m.MonthlyPermFte;
            ws.Cell(row, 7).Value = m.MonthlyMspFte;
            ws.Cell(row, 8).Value = narrative;
            ws.Cell(row, 9).Value = m.DraftRagStatusLookup?.Name ?? "";
            ws.Cell(row, 10).Value = m.DraftRagJustification ?? "";
            ws.Cell(row, 11).Value = m.DraftPathToGreen ?? "";
            row++;
        }

        FinishSheet(ws, headers.Length);
    }

    private async Task WriteRisksSheetAsync(
        IXLWorksheet ws,
        List<int> projectIds,
        IReadOnlyDictionary<int, string> titleByProjectId,
        CancellationToken cancellationToken)
    {
        var headers = new[]
        {
            "Reference", "Title", "Work item", "Work item ID", "Status", "Tier", "Priority",
            "Likelihood", "Impact", "Score", "Owner", "Identified", "Closed", "Notes"
        };
        WriteHeaderRow(ws, headers);

        var risks = await _db.Risks.AsNoTracking()
            .Where(r => !r.IsDeleted && r.ProjectId != null && projectIds.Contains(r.ProjectId.Value))
            .Include(r => r.RiskStatus)
            .Include(r => r.RiskTier)
            .Include(r => r.RiskPriority)
            .Include(r => r.Likelihood)
            .Include(r => r.ImpactLevel)
            .Include(r => r.OwnerUser)
            .OrderBy(r => r.ProjectId).ThenBy(r => r.Title)
            .ToListAsync(cancellationToken);

        var row = 2;
        foreach (var r in risks)
        {
            titleByProjectId.TryGetValue(r.ProjectId!.Value, out var title);
            ws.Cell(row, 1).Value = $"R-{r.Id:D4}";
            ws.Cell(row, 2).Value = r.Title;
            ws.Cell(row, 3).Value = title ?? "";
            ws.Cell(row, 4).Value = r.ProjectId;
            ws.Cell(row, 5).Value = r.RiskStatus?.Label ?? r.Status;
            ws.Cell(row, 6).Value = r.RiskTier?.Name;
            ws.Cell(row, 7).Value = r.RiskPriority?.Label;
            ws.Cell(row, 8).Value = r.Likelihood?.Label;
            ws.Cell(row, 9).Value = r.ImpactLevel?.Label;
            ws.Cell(row, 10).Value = r.RiskScore;
            ws.Cell(row, 11).Value = UserLabel(r.OwnerUser, r.OwnerEmail);
            ws.Cell(row, 12).Value = FormatDate(r.IdentifiedDate);
            ws.Cell(row, 13).Value = FormatDate(r.ClosedDate);
            ws.Cell(row, 14).Value = r.Notes ?? "";
            row++;
        }

        FinishSheet(ws, headers.Length);
    }

    private async Task WriteIssuesSheetAsync(
        IXLWorksheet ws,
        List<int> projectIds,
        IReadOnlyDictionary<int, string> titleByProjectId,
        CancellationToken cancellationToken)
    {
        var headers = new[]
        {
            "Reference", "Title", "Work item", "Work item ID", "Status", "Priority", "Severity",
            "Category", "Owner", "Target date", "Closed", "Description"
        };
        WriteHeaderRow(ws, headers);

        var issues = await _db.Issues.AsNoTracking()
            .Where(i => !i.IsDeleted && i.ProjectId != null && projectIds.Contains(i.ProjectId.Value))
            .Include(i => i.StatusLookup)
            .Include(i => i.PriorityLookup)
            .Include(i => i.SeverityLookup)
            .Include(i => i.CategoryLookup)
            .Include(i => i.OwnerUser)
            .OrderBy(i => i.ProjectId).ThenBy(i => i.Title)
            .ToListAsync(cancellationToken);

        var row = 2;
        foreach (var i in issues)
        {
            titleByProjectId.TryGetValue(i.ProjectId!.Value, out var title);
            ws.Cell(row, 1).Value = $"I-{i.Id:D4}";
            ws.Cell(row, 2).Value = i.Title;
            ws.Cell(row, 3).Value = title ?? "";
            ws.Cell(row, 4).Value = i.ProjectId;
            ws.Cell(row, 5).Value = i.StatusLookup?.Label ?? i.Status;
            ws.Cell(row, 6).Value = i.PriorityLookup?.Label;
            ws.Cell(row, 7).Value = i.SeverityLookup?.Label;
            ws.Cell(row, 8).Value = i.CategoryLookup?.Label;
            ws.Cell(row, 9).Value = UserLabel(i.OwnerUser, null);
            ws.Cell(row, 10).Value = FormatDate(i.TargetResolutionDate);
            ws.Cell(row, 11).Value = FormatDate(i.ClosedDate);
            ws.Cell(row, 12).Value = i.Description ?? "";
            row++;
        }

        FinishSheet(ws, headers.Length);
    }

    private async Task WriteAssumptionsSheetAsync(
        IXLWorksheet ws,
        List<int> projectIds,
        IReadOnlyDictionary<int, string> titleByProjectId,
        CancellationToken cancellationToken)
    {
        var headers = new[]
        {
            "Reference", "Summary", "Work item", "Work item ID", "Status", "Criticality", "Owner", "Review date", "Validation outcome"
        };
        WriteHeaderRow(ws, headers);

        var items = await _db.Assumptions.AsNoTracking()
            .Where(a => !a.IsDeleted && a.ProjectId != null && projectIds.Contains(a.ProjectId.Value))
            .Include(a => a.StatusLookup)
            .Include(a => a.CriticalityLookup)
            .Include(a => a.OwnerUser)
            .OrderBy(a => a.ProjectId).ThenBy(a => a.Id)
            .ToListAsync(cancellationToken);

        var row = 2;
        foreach (var a in items)
        {
            titleByProjectId.TryGetValue(a.ProjectId!.Value, out var title);
            ws.Cell(row, 1).Value = $"A-{a.Id:D4}";
            ws.Cell(row, 2).Value = Truncate(a.Description, 240);
            ws.Cell(row, 3).Value = title ?? "";
            ws.Cell(row, 4).Value = a.ProjectId;
            ws.Cell(row, 5).Value = a.StatusLookup?.Label;
            ws.Cell(row, 6).Value = a.CriticalityLookup?.Label;
            ws.Cell(row, 7).Value = UserLabel(a.OwnerUser, null);
            ws.Cell(row, 8).Value = FormatDate(a.ReviewDate);
            ws.Cell(row, 9).Value = a.ValidationOutcome ?? "";
            row++;
        }

        FinishSheet(ws, headers.Length);
    }

    private async Task WriteDecisionsSheetAsync(
        IXLWorksheet ws,
        List<int> projectIds,
        IReadOnlyDictionary<int, string> titleByProjectId,
        CancellationToken cancellationToken)
    {
        var headers = new[]
        {
            "Reference", "Title", "Work item", "Work item ID", "Status", "Type", "Decision date", "Owner", "Summary"
        };
        WriteHeaderRow(ws, headers);

        var items = await _db.Decisions.AsNoTracking()
            .Where(d => !d.IsDeleted && d.ProjectId != null && projectIds.Contains(d.ProjectId.Value))
            .Include(d => d.OwnerUser)
            .Include(d => d.StatusLookup)
            .OrderBy(d => d.ProjectId).ThenBy(d => d.Title)
            .ToListAsync(cancellationToken);

        var row = 2;
        foreach (var d in items)
        {
            titleByProjectId.TryGetValue(d.ProjectId!.Value, out var title);
            ws.Cell(row, 1).Value = $"D-{d.Id:D4}";
            ws.Cell(row, 2).Value = d.Title;
            ws.Cell(row, 3).Value = title ?? "";
            ws.Cell(row, 4).Value = d.ProjectId;
            ws.Cell(row, 5).Value = d.StatusLookup?.Label ?? d.Status;
            ws.Cell(row, 6).Value = d.DecisionType;
            ws.Cell(row, 7).Value = FormatDate(d.DecisionDate);
            ws.Cell(row, 8).Value = UserLabel(d.OwnerUser, d.OwnerEmail);
            ws.Cell(row, 9).Value = d.Summary ?? "";
            row++;
        }

        FinishSheet(ws, headers.Length);
    }

    private async Task WriteNearMissesSheetAsync(
        IXLWorksheet ws,
        List<int> projectIds,
        CancellationToken cancellationToken)
    {
        var headers = new[]
        {
            "Reference", "Date logged", "Business area", "Directorate", "Type", "Seriousness", "Status", "Impact", "RAG after"
        };
        WriteHeaderRow(ws, headers);

        var baIds = await _db.Projects.AsNoTracking()
            .Where(p => projectIds.Contains(p.Id) && p.BusinessAreaId != null)
            .Select(p => p.BusinessAreaId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        var nearMisses = baIds.Count == 0
            ? new List<NearMiss>()
            : await _db.NearMisses.AsNoTracking()
                .Where(n => !n.IsDeleted && n.BusinessAreaLookupId != null && baIds.Contains(n.BusinessAreaLookupId.Value))
                .Include(n => n.BusinessAreaLookup)
                .Include(n => n.DirectorateLookup)
                .Include(n => n.TypeLookup)
                .Include(n => n.SeriousnessLookup)
                .Include(n => n.StatusLookup)
                .Include(n => n.PostMitigationRagStatusLookup)
                .OrderByDescending(n => n.DateLogged)
                .ToListAsync(cancellationToken);

        var row = 2;
        foreach (var n in nearMisses)
        {
            ws.Cell(row, 1).Value = n.Reference;
            ws.Cell(row, 2).Value = FormatDate(n.DateLogged);
            ws.Cell(row, 3).Value = n.BusinessAreaLookup?.Name;
            ws.Cell(row, 4).Value = n.DirectorateLookup?.Name;
            ws.Cell(row, 5).Value = n.TypeLookup?.Label;
            ws.Cell(row, 6).Value = n.SeriousnessLookup?.Label;
            ws.Cell(row, 7).Value = n.StatusLookup?.Label;
            ws.Cell(row, 8).Value = n.Impact ?? "";
            ws.Cell(row, 9).Value = n.PostMitigationRagStatusLookup?.Name;
            row++;
        }

        FinishSheet(ws, headers.Length);
    }

    private async Task WriteAccessibilitySheetAsync(
        IXLWorksheet ws,
        List<int> projectIds,
        IReadOnlyDictionary<int, string> titleByProjectId,
        CancellationToken cancellationToken)
    {
        var headers = new[]
        {
            "Work item", "Work item ID", "Product", "FIPS ID", "WCAG", "Statement URL", "Statement installed",
            "Open issues", "Complaints email"
        };
        WriteHeaderRow(ws, headers);

        var products = await _db.ProjectProducts.AsNoTracking()
            .Where(pp => projectIds.Contains(pp.ProjectId))
            .ToListAsync(cancellationToken);

        if (products.Count == 0)
        {
            FinishSheet(ws, headers.Length);
            return;
        }

        var docIds = products.Select(p => p.ProductDocumentId).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();
        var fipsIds = products.Select(p => p.ProductFipsId).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();

        var accessibility = await _db.ProductAccessibilities.AsNoTracking()
            .Where(pa => !pa.IsDeleted && pa.IsActive &&
                (docIds.Contains(pa.ProductDocumentId!) || (pa.FipsId != null && fipsIds.Contains(pa.FipsId))))
            .ToListAsync(cancellationToken);

        var paIds = accessibility.Select(a => a.Id).ToList();
        var openIssueCounts = paIds.Count == 0
            ? new Dictionary<int, int>()
            : await _db.AccessibilityIssues.AsNoTracking()
                .Where(i => !i.IsDeleted && i.Status != "closed" && paIds.Contains(i.ProductAccessibilityId))
                .GroupBy(i => i.ProductAccessibilityId)
                .Select(g => new { Id = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Id, x => x.Count, cancellationToken);

        var row = 2;
        foreach (var pp in products)
        {
            var pa = accessibility.FirstOrDefault(a =>
                a.ProductDocumentId == pp.ProductDocumentId ||
                (!string.IsNullOrEmpty(pp.ProductFipsId) && a.FipsId == pp.ProductFipsId));
            if (pa == null) continue;

            titleByProjectId.TryGetValue(pp.ProjectId, out var title);
            openIssueCounts.TryGetValue(pa.Id, out var openIssues);

            ws.Cell(row, 1).Value = title ?? pp.ProductTitle;
            ws.Cell(row, 2).Value = pp.ProjectId;
            ws.Cell(row, 3).Value = pa.ProductName ?? pp.ProductTitle;
            ws.Cell(row, 4).Value = pa.FipsId ?? pp.ProductFipsId;
            ws.Cell(row, 5).Value = $"{pa.WcagVersion} {pa.WcagLevel}".Trim();
            ws.Cell(row, 6).Value = pa.StatementUrl ?? "";
            ws.Cell(row, 7).Value = pa.StatementInstalled ? "Yes" : "No";
            ws.Cell(row, 8).Value = openIssues;
            ws.Cell(row, 9).Value = pa.ComplaintsEmail;
            row++;
        }

        FinishSheet(ws, headers.Length);
    }

    private static void WriteHeaderRow(IXLWorksheet ws, string[] headers)
    {
        for (var c = 0; c < headers.Length; c++)
            ws.Cell(1, c + 1).Value = headers[c];
        ws.Row(1).Style.Font.Bold = true;
    }

    private static void FinishSheet(IXLWorksheet ws, int columnCount)
    {
        ws.SheetView.FreezeRows(1);
        ws.Columns(1, columnCount).AdjustToContents();
    }

    private static string? UserLabel(User? user, string? email)
    {
        if (user != null && !string.IsNullOrWhiteSpace(user.Name))
            return user.Name;
        return string.IsNullOrWhiteSpace(email) ? null : email;
    }

    private static string FormatDate(DateTime? dt) =>
        dt.HasValue ? dt.Value.ToString("d MMM yyyy", CultureInfo.InvariantCulture) : "";

    private static string Truncate(string? text, int max)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";
        var t = text.Trim();
        return t.Length <= max ? t : t[..max] + "…";
    }
}
