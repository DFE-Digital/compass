using System.Globalization;
using ClosedXML.Excel;
using Compass.ViewModels;

namespace Compass.Services.Modern;

/// <summary>Excel export for the monthly submission progress report.</summary>
public static class MonthlySubmissionProgressExcelExport
{
    public static byte[] BuildWorkbook(ModernMonthlySubmissionProgressViewModel report)
    {
        using var workbook = new XLWorkbook();
        WriteSummarySheet(workbook.Worksheets.Add("Summary"), report);
        WriteWorkItemsSheet(workbook.Worksheets.Add("Work items"), report);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void WriteSummarySheet(IXLWorksheet worksheet, ModernMonthlySubmissionProgressViewModel report)
    {
        var st = report.MonthlyUpdateStats;
        var businessAreaFilter = ResolveFilterName(
            report.FilterBusinessAreaId,
            report.BusinessAreas.Select(b => (b.Id, b.Name)));
        var directorateFilter = ResolveFilterName(
            report.FilterDirectorateId,
            report.Directorates.Select(d => (d.Id, d.Name)));

        var rows = new List<(string Label, string Value)>
        {
            ("Reporting period", report.MonthName),
            ("Submission window", $"{report.SubmissionWindowStart:d MMM yyyy} – {report.SubmissionWindowEnd:d MMM yyyy}"),
            ("Business area filter", businessAreaFilter),
            ("Directorate filter", directorateFilter),
            ("Work items in scope", (st?.TotalProjects ?? 0).ToString(CultureInfo.InvariantCulture)),
            ("Submitted", (st?.Submitted ?? 0).ToString(CultureInfo.InvariantCulture)),
            ("In progress", (st?.InProgress ?? 0).ToString(CultureInfo.InvariantCulture)),
            ("Late", (st?.Late ?? 0).ToString(CultureInfo.InvariantCulture)),
            ("Not started", (st?.NotStarted ?? 0).ToString(CultureInfo.InvariantCulture)),
            ("Target completion today", $"{report.ExpectedProgressPercentToday}%"),
        };

        worksheet.Cell(1, 1).Value = "Field";
        worksheet.Cell(1, 2).Value = "Value";
        worksheet.Row(1).Style.Font.Bold = true;

        var rowNumber = 2;
        foreach (var (label, value) in rows)
        {
            worksheet.Cell(rowNumber, 1).Value = label;
            worksheet.Cell(rowNumber, 2).Value = value;
            rowNumber++;
        }

        worksheet.SheetView.FreezeRows(1);
        worksheet.Columns(1, 2).AdjustToContents();
    }

    private static void WriteWorkItemsSheet(IXLWorksheet worksheet, ModernMonthlySubmissionProgressViewModel report)
    {
        var periodColumns = report.ExportPeriodColumns;
        var fixedHeaders = new[]
        {
            "Work item ID",
            "Title",
            "Business area",
            "Directorate",
            "Current period status",
            "Current period submitted date"
        };

        var col = 1;
        foreach (var header in fixedHeaders)
        {
            worksheet.Cell(1, col).Value = header;
            col++;
        }

        foreach (var period in periodColumns)
        {
            worksheet.Cell(1, col).Value = period.Label;
            col++;
        }

        worksheet.Row(1).Style.Font.Bold = true;

        var rowNumber = 2;
        foreach (var row in report.ExportRows)
        {
            col = 1;
            worksheet.Cell(rowNumber, col++).Value = row.ProjectId;
            worksheet.Cell(rowNumber, col++).Value = row.Title;
            worksheet.Cell(rowNumber, col++).Value = row.BusinessAreaName;
            worksheet.Cell(rowNumber, col++).Value = row.DirectorateName;
            worksheet.Cell(rowNumber, col++).Value = row.CurrentPeriodStatus;
            worksheet.Cell(rowNumber, col++).Value = row.CurrentPeriodSubmittedAt.HasValue
                ? row.CurrentPeriodSubmittedAt.Value.ToString("d MMM yyyy", CultureInfo.GetCultureInfo("en-GB"))
                : "—";

            for (var i = 0; i < periodColumns.Count; i++)
            {
                var status = i < row.PeriodStatuses.Count ? row.PeriodStatuses[i] : "—";
                worksheet.Cell(rowNumber, col++).Value = status;
            }

            rowNumber++;
        }

        worksheet.SheetView.FreezeRows(1);
        worksheet.SheetView.FreezeColumns(fixedHeaders.Length);
        if (rowNumber > 2)
            worksheet.Range(1, 1, rowNumber - 1, col - 1).SetAutoFilter();
        worksheet.Columns().AdjustToContents();
    }

    private static string ResolveFilterName(int? filterId, IEnumerable<(int Id, string Name)> options)
    {
        if (!filterId.HasValue)
            return "All";

        return options.FirstOrDefault(o => o.Id == filterId.Value).Name ?? "All";
    }
}
