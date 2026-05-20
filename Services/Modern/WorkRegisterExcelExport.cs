using System.Globalization;
using ClosedXML.Excel;
using Compass.Models;
using Compass.Models.Modern.Work;

namespace Compass.Services.Modern;

/// <summary>Shared Excel writers for modern work register and milestone exports.</summary>
public static class WorkRegisterExcelExport
{
    public static void WriteWorkListSheet(IXLWorksheet worksheet, IEnumerable<WorkRegisterRow> rows)
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
        worksheet.Cell(1, 11).Value = "Milestone count";
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
            worksheet.Cell(rowNumber, 11).Value = row.TotalMilestoneCount;
            worksheet.Cell(rowNumber, 12).Value = row.MonthlyUpdateStatus ?? "";
            worksheet.Cell(rowNumber, 13).Value = row.FirstRiskReference ?? "";
            worksheet.Cell(rowNumber, 14).Value = row.CompletedAt ?? "";
            worksheet.Cell(rowNumber, 15).Value = row.CancelledReason ?? "";
            worksheet.Cell(rowNumber, 16).Value = row.TagNamesSummary ?? "";
            rowNumber++;
        }

        worksheet.Range(1, 1, 1, 16).Style.Font.Bold = true;
        worksheet.SheetView.FreezeRows(1);
        worksheet.Columns(1, 16).AdjustToContents();
    }

    public static void WriteMilestonesSheet(
        IXLWorksheet worksheet,
        IEnumerable<Milestone> milestones,
        IReadOnlyDictionary<int, string> workItemTitleByProjectId)
    {
        var headers = new[] { "Milestone Id", "Work item", "Milestone name", "Due date", "Actual date", "Status", "Created at" };
        for (var c = 0; c < headers.Length; c++)
            worksheet.Cell(1, c + 1).Value = headers[c];
        worksheet.Row(1).Style.Font.Bold = true;

        var row = 2;
        foreach (var m in milestones)
        {
            var projectId = m.ProjectId ?? 0;
            var workItemTitle = projectId > 0 && workItemTitleByProjectId.TryGetValue(projectId, out var title)
                ? title
                : "";

            worksheet.Cell(row, 1).Value = m.Id;
            worksheet.Cell(row, 2).Value = workItemTitle;
            worksheet.Cell(row, 3).Value = m.Name ?? "";
            worksheet.Cell(row, 4).Value = m.DueDate;
            worksheet.Cell(row, 5).Value = m.ActualDate;
            worksheet.Cell(row, 6).Value = m.Status ?? "";
            worksheet.Cell(row, 7).Value = m.CreatedAt;
            row++;
        }

        worksheet.SheetView.FreezeRows(1);
        worksheet.Columns(1, headers.Length).AdjustToContents();
    }
}
