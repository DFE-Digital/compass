using System.Globalization;
using ClosedXML.Excel;
using Compass.Models;
using Compass.Models.Modern.Work;
using Compass.ViewModels;
using Compass.ViewModels.Modern;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Compass.Controllers.Modern;

public partial class ModernReportingController
{
    [HttpGet("thematic/export")]
    public async Task<IActionResult> ExportThematicReport(
        int? themeId,
        string? tab,
        CancellationToken cancellationToken = default)
    {
        var summaryRows = await BuildThematicSummaryAsync(cancellationToken);
        var exportTab = NormalizeThematicExportTab(tab);
        var excel = await TryBuildThematicExcelAsync(
            summaryRows,
            themeId,
            exportTab,
            includeAllTaggedWork: false,
            cancellationToken);
        if (excel == null)
            return Unauthorized();

        var suffix = themeId.HasValue && themeId.Value > 0 ? $"theme-{themeId.Value}-{exportTab}" : "summary";
        return ReturnExcelFile(excel, $"thematic-report-{suffix}-{Timestamp()}.xlsx");
    }

    [HttpGet("thematic/export/all")]
    public async Task<IActionResult> ExportThematicReportAll(CancellationToken cancellationToken = default)
    {
        var summaryRows = await BuildThematicSummaryAsync(cancellationToken);
        var excel = await TryBuildThematicExcelAsync(
            summaryRows,
            themeId: null,
            exportTab: "all",
            includeAllTaggedWork: true,
            cancellationToken);
        if (excel == null)
            return Unauthorized();

        return ReturnExcelFile(excel, $"thematic-report-all-{Timestamp()}.xlsx");
    }

    [HttpGet("raid/export")]
    public async Task<IActionResult> ExportRaidReport(
        string? tab,
        int? businessAreaId,
        int? directorateId,
        CancellationToken cancellationToken = default)
    {
        var model = await _raidReportService.BuildAsync(tab, businessAreaId, directorateId, cancellationToken);
        var panel = model.ActivePanel;
        var tabKey = model.ActiveTab;

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(SanitizeSheetName(tabKey));
        WriteRaidReportWorksheet(worksheet, panel);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return ReturnExcelFile(stream.ToArray(), $"raid-report-{tabKey}-{Timestamp()}.xlsx");
    }

    private async Task<byte[]?> TryBuildThematicExcelAsync(
        List<ThematicReportTagSummaryRow> summaryRows,
        int? themeId,
        string exportTab,
        bool includeAllTaggedWork,
        CancellationToken cancellationToken)
    {
        using var workbook = new XLWorkbook();
        WriteThematicSummaryWorksheet(workbook.Worksheets.Add("Themes"), summaryRows);

        int[]? tagIds = null;
        if (includeAllTaggedWork)
        {
            tagIds = summaryRows.Select(r => r.TagId).ToArray();
        }
        else if (themeId.HasValue && themeId.Value > 0 && summaryRows.Any(r => r.TagId == themeId.Value))
        {
            tagIds = new[] { themeId.Value };
        }

        if (tagIds is { Length: > 0 })
        {
            var user = await GetCurrentUserAsync(cancellationToken);
            if (user == null)
                return null;

            var rows = await _modernWork.BuildWorkRegisterExportRowsAsync(
                isMyWork: false,
                search: null,
                portfolioId: null,
                directorateId: null,
                phaseId: null,
                ragId: null,
                priorityId: null,
                monthlyUpdate: null,
                user.Value.CurrentUser,
                user.Value.Email,
                Url,
                exportTab,
                tagIds: tagIds,
                cancellationToken: cancellationToken);

            WriteWorkRegisterWorksheet(workbook.Worksheets.Add("Work items"), rows);
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private async Task<(User CurrentUser, string Email)?> GetCurrentUserAsync(CancellationToken cancellationToken)
    {
        var userEmail = User.Identity?.Name;
        if (string.IsNullOrEmpty(userEmail))
            return null;

        var currentUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == userEmail.ToLower(), cancellationToken);
        if (currentUser == null)
            return null;

        return (currentUser, userEmail);
    }

    private static string NormalizeThematicExportTab(string? tab)
    {
        var key = (tab ?? "active").Trim().ToLowerInvariant();
        return key is "completed" ? "completed" : "active";
    }

    private static string Timestamp() => DateTime.UtcNow.ToString("yyyyMMdd-HHmm", CultureInfo.InvariantCulture);

    private IActionResult ReturnExcelFile(byte[] bytes, string fileName) =>
        File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);

    private static string SanitizeSheetName(string name)
    {
        var invalid = new[] { '\\', '/', '*', '?', ':', '[', ']' };
        var cleaned = new string(name.Select(c => invalid.Contains(c) ? '-' : c).ToArray());
        return cleaned.Length > 31 ? cleaned[..31] : cleaned;
    }

    private static void WriteThematicSummaryWorksheet(IXLWorksheet worksheet, IEnumerable<ThematicReportTagSummaryRow> rows)
    {
        worksheet.Cell(1, 1).Value = "Theme";
        worksheet.Cell(1, 2).Value = "Description";
        worksheet.Cell(1, 3).Value = "Active work items";
        worksheet.Cell(1, 4).Value = "Completed work items";

        var rowNumber = 2;
        foreach (var row in rows)
        {
            worksheet.Cell(rowNumber, 1).Value = row.Name ?? "";
            worksheet.Cell(rowNumber, 2).Value = row.Description ?? "";
            worksheet.Cell(rowNumber, 3).Value = row.ActiveCount;
            worksheet.Cell(rowNumber, 4).Value = row.CompletedCount;
            rowNumber++;
        }

        worksheet.Range(1, 1, 1, 4).Style.Font.Bold = true;
        worksheet.Columns().AdjustToContents();
    }

    private static void WriteRaidReportWorksheet(IXLWorksheet worksheet, RaidReportTabPanel panel)
    {
        worksheet.Cell(1, 1).Value = "Reference";
        worksheet.Cell(1, 2).Value = "Title";
        var headerCount = 2 + panel.TableHeaders.Count;
        for (var i = 0; i < panel.TableHeaders.Count; i++)
            worksheet.Cell(1, 3 + i).Value = panel.TableHeaders[i];

        var rowNumber = 2;
        foreach (var row in panel.Rows)
        {
            worksheet.Cell(rowNumber, 1).Value = row.Reference;
            worksheet.Cell(rowNumber, 2).Value = row.Title ?? "";
            for (var i = 0; i < row.Cells.Count; i++)
                worksheet.Cell(rowNumber, 3 + i).Value = row.Cells[i];
            rowNumber++;
        }

        worksheet.Range(1, 1, 1, headerCount).Style.Font.Bold = true;
        worksheet.Columns().AdjustToContents();
    }

    private static void WriteWorkRegisterWorksheet(IXLWorksheet worksheet, IEnumerable<WorkRegisterRow> rows)
    {
        worksheet.Cell(1, 1).Value = "Work item";
        worksheet.Cell(1, 2).Value = "Reference";
        worksheet.Cell(1, 3).Value = "Status";
        worksheet.Cell(1, 4).Value = "Business area";
        worksheet.Cell(1, 5).Value = "SRO";
        worksheet.Cell(1, 6).Value = "Primary contact";
        worksheet.Cell(1, 7).Value = "Portfolio";
        worksheet.Cell(1, 8).Value = "Phase";
        worksheet.Cell(1, 9).Value = "Priority";
        worksheet.Cell(1, 10).Value = "RAG";
        worksheet.Cell(1, 11).Value = "Milestones";
        worksheet.Cell(1, 12).Value = "Monthly update";
        worksheet.Cell(1, 13).Value = "Risk ref";
        worksheet.Cell(1, 14).Value = "Completed";
        worksheet.Cell(1, 15).Value = "Cancelled reason";
        worksheet.Cell(1, 16).Value = "Tags";

        var rowNumber = 2;
        foreach (var row in rows)
        {
            worksheet.Cell(rowNumber, 1).Value = row.Title ?? "";
            worksheet.Cell(rowNumber, 2).Value = "WI-" + row.Id.ToString("D8", CultureInfo.InvariantCulture);
            worksheet.Cell(rowNumber, 3).Value = row.Status ?? "";
            worksheet.Cell(rowNumber, 4).Value = row.BusinessAreaName ?? row.DirectorateSummary ?? "";
            worksheet.Cell(rowNumber, 5).Value = row.SroDisplayName ?? "";
            worksheet.Cell(rowNumber, 6).Value = row.PrimaryContactName ?? "";
            worksheet.Cell(rowNumber, 7).Value = row.PortfolioName ?? "";
            worksheet.Cell(rowNumber, 8).Value = row.PhaseName ?? "";
            worksheet.Cell(rowNumber, 9).Value = row.PriorityName ?? "";
            worksheet.Cell(rowNumber, 10).Value = row.RagName ?? "";
            worksheet.Cell(rowNumber, 11).Value = row.MilestoneCount;
            worksheet.Cell(rowNumber, 12).Value = row.MonthlyUpdateStatus ?? "";
            worksheet.Cell(rowNumber, 13).Value = row.FirstRiskReference ?? "";
            worksheet.Cell(rowNumber, 14).Value = row.CompletedAt ?? "";
            worksheet.Cell(rowNumber, 15).Value = row.CancelledReason ?? "";
            worksheet.Cell(rowNumber, 16).Value = row.TagNamesSummary ?? "";
            rowNumber++;
        }

        worksheet.Range(1, 1, 1, 16).Style.Font.Bold = true;
        worksheet.Columns().AdjustToContents();
    }
}
