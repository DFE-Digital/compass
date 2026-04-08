using ClosedXML.Excel;
using Compass.Data;
using Compass.Models;
using Compass.Models.DemandTriage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Compass.Controllers;

/// <summary>Data exports hub — <c>/Exports</c>. Used by Work and Demand sub-navigation.</summary>
[Authorize]
public class ExportsController : Controller
{
    private readonly CompassDbContext _db;

    public ExportsController(CompassDbContext db) => _db = db;

    /// <summary>Exports landing — <c>section=work</c> or <c>section=demand</c> highlights the matching sub-nav.</summary>
    [HttpGet]
    public IActionResult Index(string? section = "demand")
    {
        if (string.Equals(section, "work", StringComparison.OrdinalIgnoreCase))
        {
            ViewBag.MainNavSection = "work";
            ViewBag.SubNavItem = "work-exports";
        }
        else
        {
            ViewBag.MainNavSection = "demand";
            ViewBag.SubNavItem = "demand-exports";
        }

        return View("~/Views/Modern/Exports/Index.cshtml");
    }

    [HttpGet]
    public async Task<IActionResult> DownloadWorkExcel(CancellationToken cancellationToken = default)
    {
        using var wb = new XLWorkbook();

        var projects = await _db.Projects.AsNoTracking()
            .Where(p => !p.IsDeleted)
            .OrderBy(p => p.Title)
            .ToListAsync(cancellationToken);
        var wsWork = wb.AddWorksheet("Work items");
        WriteProjectsWorksheet(wsWork, projects);

        var milestones = await _db.Milestones.AsNoTracking()
            .Where(m => m.ProjectId != null && !m.IsDeleted)
            .OrderBy(m => m.ProjectId).ThenBy(m => m.DueDate)
            .ToListAsync(cancellationToken);
        var wsMilestones = wb.AddWorksheet("Milestones");
        WriteProjectMilestones(wsMilestones, milestones);

        var monthly = await _db.ProjectMonthlyUpdates.AsNoTracking()
            .OrderByDescending(m => m.Year).ThenByDescending(m => m.Month)
            .ToListAsync(cancellationToken);
        var wsMonthly = wb.AddWorksheet("Monthly updates");
        WriteProjectMonthlyUpdates(wsMonthly, monthly);

        using var stream = new MemoryStream();
        wb.SaveAs(stream);
        stream.Position = 0;
        var fileName = $"Compass-work-export-{DateTime.UtcNow:yyyyMMdd-HHmm}.xlsx";
        return File(
            stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }

    [HttpGet]
    public async Task<IActionResult> DownloadExcel(CancellationToken cancellationToken = default)
    {
        using var wb = new XLWorkbook();

        var businessCases = await _db.BusinessCases.AsNoTracking().OrderBy(b => b.BusinessCaseId).ToListAsync(cancellationToken);
        WriteBusinessCasesSheet(wb.AddWorksheet("Business cases"), businessCases);

        var demands = await _db.DemandTriageRequests.AsNoTracking().OrderBy(d => d.RequestReference).ToListAsync(cancellationToken);
        WriteDemandsSheet(wb.AddWorksheet("Demands"), demands);

        var outcomes = await _db.DemandTriageOutcomes.AsNoTracking()
            .OrderByDescending(t => t.DecidedAt ?? t.CreatedAt)
            .ToListAsync(cancellationToken);
        WriteTriageOutcomesSheet(wb.AddWorksheet("Triage outcomes"), outcomes);

        var scorecards = await _db.DemandScorecards.AsNoTracking()
            .OrderByDescending(s => s.UpdatedAt)
            .ToListAsync(cancellationToken);
        WriteScorecardsSheet(wb.AddWorksheet("Scoring"), scorecards);

        var reviews = await _db.DemandExploratoryReviews.AsNoTracking()
            .OrderByDescending(r => r.CompletedAt ?? r.CreatedAt)
            .ToListAsync(cancellationToken);
        WriteExploratoryReviewsSheet(wb.AddWorksheet("Exploratory reviews"), reviews);

        using var stream = new MemoryStream();
        wb.SaveAs(stream);
        stream.Position = 0;
        var fileName = $"Compass-demand-export-{DateTime.UtcNow:yyyyMMdd-HHmm}.xlsx";
        return File(
            stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }

    private static void WriteProjectsWorksheet(IXLWorksheet ws, List<Project> list)
    {
        var headers = new[] { "Id", "ProjectCode", "Title", "Status", "PortfolioId", "PhaseId", "PriorityId", "RagStatusLookupId", "StartDate", "TargetDeliveryDate", "CreatedAt", "UpdatedAt" };
        for (var c = 0; c < headers.Length; c++)
            ws.Cell(1, c + 1).Value = headers[c];
        ws.Row(1).Style.Font.Bold = true;
        var row = 2;
        foreach (var p in list)
        {
            ws.Cell(row, 1).Value = p.Id;
            ws.Cell(row, 2).Value = p.ProjectCode;
            ws.Cell(row, 3).Value = p.Title;
            ws.Cell(row, 4).Value = p.Status;
            ws.Cell(row, 5).Value = p.PrimaryOrganizationalGroupId;
            ws.Cell(row, 6).Value = p.PhaseId;
            ws.Cell(row, 7).Value = p.DeliveryPriorityId;
            ws.Cell(row, 8).Value = p.RagStatusLookupId;
            ws.Cell(row, 9).Value = p.StartDate;
            ws.Cell(row, 10).Value = p.TargetDeliveryDate;
            ws.Cell(row, 11).Value = p.CreatedAt;
            ws.Cell(row, 12).Value = p.UpdatedAt;
            row++;
        }
        ws.SheetView.FreezeRows(1);
        ws.Columns(1, 12).AdjustToContents();
    }

    private static void WriteProjectMilestones(IXLWorksheet ws, List<Milestone> list)
    {
        var headers = new[] { "Id", "ProjectId", "Name", "DueDate", "ActualDate", "Status", "CreatedAt" };
        for (var c = 0; c < headers.Length; c++)
            ws.Cell(1, c + 1).Value = headers[c];
        ws.Row(1).Style.Font.Bold = true;
        var row = 2;
        foreach (var m in list)
        {
            ws.Cell(row, 1).Value = m.Id;
            ws.Cell(row, 2).Value = m.ProjectId;
            ws.Cell(row, 3).Value = m.Name;
            ws.Cell(row, 4).Value = m.DueDate;
            ws.Cell(row, 5).Value = m.ActualDate;
            ws.Cell(row, 6).Value = m.Status;
            ws.Cell(row, 7).Value = m.CreatedAt;
            row++;
        }
        ws.SheetView.FreezeRows(1);
        ws.Columns(1, 7).AdjustToContents();
    }

    private static void WriteProjectMonthlyUpdates(IXLWorksheet ws, List<ProjectMonthlyUpdate> list)
    {
        var headers = new[] { "Id", "ProjectId", "Year", "Month", "Narrative", "SubmittedAt", "CreatedAt" };
        for (var c = 0; c < headers.Length; c++)
            ws.Cell(1, c + 1).Value = headers[c];
        ws.Row(1).Style.Font.Bold = true;
        var row = 2;
        foreach (var m in list)
        {
            ws.Cell(row, 1).Value = m.Id;
            ws.Cell(row, 2).Value = m.ProjectId;
            ws.Cell(row, 3).Value = m.Year;
            ws.Cell(row, 4).Value = m.Month;
            ws.Cell(row, 5).Value = m.Narrative;
            ws.Cell(row, 6).Value = m.SubmittedAt;
            ws.Cell(row, 7).Value = m.CreatedAt;
            row++;
        }
        ws.SheetView.FreezeRows(1);
        ws.Columns(1, 7).AdjustToContents();
    }

    private static void WriteBusinessCasesSheet(IXLWorksheet ws, List<BusinessCase> list)
    {
        var headers = new[] { "Id", "BusinessCaseId", "Title", "Status", "BusinessArea", "CreatedAt", "UpdatedAt" };
        for (var c = 0; c < headers.Length; c++)
            ws.Cell(1, c + 1).Value = headers[c];
        ws.Row(1).Style.Font.Bold = true;
        var row = 2;
        foreach (var b in list)
        {
            ws.Cell(row, 1).Value = b.Id;
            ws.Cell(row, 2).Value = b.BusinessCaseId;
            ws.Cell(row, 3).Value = b.Title;
            ws.Cell(row, 4).Value = b.Status;
            ws.Cell(row, 5).Value = b.BusinessArea;
            ws.Cell(row, 6).Value = b.CreatedAt;
            ws.Cell(row, 7).Value = b.UpdatedAt;
            row++;
        }
        ws.Columns().AdjustToContents();
    }

    private static void WriteDemandsSheet(IXLWorksheet ws, List<DemandTriageRequest> list)
    {
        var headers = new[] { "Id", "RequestReference", "Status", "RequestName", "RequesterFullName", "ProposedRequestTitle", "TargetDeliveryDate", "CreatedAt", "UpdatedAt" };
        for (var c = 0; c < headers.Length; c++)
            ws.Cell(1, c + 1).Value = headers[c];
        ws.Row(1).Style.Font.Bold = true;
        var row = 2;
        foreach (var d in list)
        {
            ws.Cell(row, 1).Value = d.Id;
            ws.Cell(row, 2).Value = d.RequestReference;
            ws.Cell(row, 3).Value = d.Status;
            ws.Cell(row, 4).Value = d.RequestName;
            ws.Cell(row, 5).Value = d.RequesterFullName;
            ws.Cell(row, 6).Value = d.ProposedRequestTitle;
            ws.Cell(row, 7).Value = d.TargetDeliveryDate;
            ws.Cell(row, 8).Value = d.CreatedAt;
            ws.Cell(row, 9).Value = d.UpdatedAt;
            row++;
        }
        ws.Columns().AdjustToContents();
    }

    private static void WriteTriageOutcomesSheet(IXLWorksheet ws, List<DemandTriageOutcome> list)
    {
        var headers = new[] { "Id", "DemandTriageRequestId", "OutcomeSelection", "OutcomeSummary", "RoutedToArea", "DecidedAt", "DecidedBy", "CreatedAt" };
        for (var c = 0; c < headers.Length; c++)
            ws.Cell(1, c + 1).Value = headers[c];
        ws.Row(1).Style.Font.Bold = true;
        var row = 2;
        foreach (var t in list)
        {
            ws.Cell(row, 1).Value = t.Id;
            ws.Cell(row, 2).Value = t.DemandTriageRequestId;
            ws.Cell(row, 3).Value = t.OutcomeSelection;
            ws.Cell(row, 4).Value = t.OutcomeSummary;
            ws.Cell(row, 5).Value = t.RoutedToArea;
            ws.Cell(row, 6).Value = t.DecidedAt;
            ws.Cell(row, 7).Value = t.DecidedBy;
            ws.Cell(row, 8).Value = t.CreatedAt;
            row++;
        }
        ws.Columns().AdjustToContents();
    }

    private static void WriteScorecardsSheet(IXLWorksheet ws, List<DemandScorecard> list)
    {
        var headers = new[] { "Id", "DemandTriageRequestId", "ScorecardStatus", "TotalScore", "SuggestionBand", "FinalisedAt", "CreatedAt", "UpdatedAt" };
        for (var c = 0; c < headers.Length; c++)
            ws.Cell(1, c + 1).Value = headers[c];
        ws.Row(1).Style.Font.Bold = true;
        var row = 2;
        foreach (var s in list)
        {
            ws.Cell(row, 1).Value = s.Id;
            ws.Cell(row, 2).Value = s.DemandTriageRequestId;
            ws.Cell(row, 3).Value = s.ScorecardStatus;
            ws.Cell(row, 4).Value = s.TotalScore;
            ws.Cell(row, 5).Value = s.SuggestionBand;
            ws.Cell(row, 6).Value = s.FinalisedAt;
            ws.Cell(row, 7).Value = s.CreatedAt;
            ws.Cell(row, 8).Value = s.UpdatedAt;
            row++;
        }
        ws.Columns().AdjustToContents();
    }

    private static void WriteExploratoryReviewsSheet(IXLWorksheet ws, List<DemandExploratoryReview> list)
    {
        var headers = new[] { "Id", "DemandTriageRequestId", "RecommendationToProceed", "CompletedAt", "CompletedBy", "CreatedAt", "UpdatedAt" };
        for (var c = 0; c < headers.Length; c++)
            ws.Cell(1, c + 1).Value = headers[c];
        ws.Row(1).Style.Font.Bold = true;
        var row = 2;
        foreach (var r in list)
        {
            ws.Cell(row, 1).Value = r.Id;
            ws.Cell(row, 2).Value = r.DemandTriageRequestId;
            ws.Cell(row, 3).Value = r.RecommendationToProceed.HasValue ? (r.RecommendationToProceed.Value ? "Yes" : "No") : "";
            ws.Cell(row, 4).Value = r.CompletedAt;
            ws.Cell(row, 5).Value = r.CompletedBy;
            ws.Cell(row, 6).Value = r.CreatedAt;
            ws.Cell(row, 7).Value = r.UpdatedAt;
            row++;
        }
        ws.Columns().AdjustToContents();
    }
}
