using System.Globalization;
using ClosedXML.Excel;
using Compass.ViewModels.Modern;

namespace Compass.Services.Modern;

/// <summary>Excel export for the monthly resourcing report.</summary>
public static class ResourcingReportExcelExport
{
    public static byte[] BuildWorkbook(ModernResourcingReportViewModel report)
    {
        using var workbook = new XLWorkbook();
        WriteSummarySheet(workbook.Worksheets.Add("Summary"), report);
        WriteBandsSheet(workbook.Worksheets.Add("Resource bands"), report.Bands);
        WriteAggregateSheet(workbook.Worksheets.Add("Directorates"), "Directorate", report.DirectorateRows);
        WriteAggregateSheet(workbook.Worksheets.Add("Business areas"), "Business area", report.BusinessAreaRows);
        WriteWorkItemsSheet(workbook.Worksheets.Add("Work items"), report.WorkItemRows);
        WriteTrendSheet(workbook.Worksheets.Add("Trend"), report.TrendPoints);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void WriteSummarySheet(IXLWorksheet worksheet, ModernResourcingReportViewModel report)
    {
        var businessAreaFilter = ResolveFilterName(report.FilterBusinessAreaId, report.BusinessAreas.Select(b => (b.Id, b.Name)));
        var directorateFilter = ResolveFilterName(report.FilterDirectorateId, report.Directorates.Select(d => (d.Id, d.Name)));

        var rows = new (string Label, string Value)[]
        {
            ("Period", report.MonthName),
            ("Scope", ScopeLabel(report)),
            ("Business area filter", businessAreaFilter),
            ("Directorate filter", directorateFilter),
            ("Submitted work items", report.SubmittedWorkItemCount.ToString(CultureInfo.InvariantCulture)),
            ("FTE total", report.TotalPermFte.ToString("0.##", CultureInfo.InvariantCulture)),
            ("MSC total", report.TotalMspFte.ToString("0.##", CultureInfo.InvariantCulture)),
            ("Combined total (FTE + MSC)", report.TotalResourcingFte.ToString("0.##", CultureInfo.InvariantCulture)),
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

    private static void WriteBandsSheet(IXLWorksheet worksheet, IReadOnlyList<ResourcingBandViewModel> bands)
    {
        WriteHeaderRow(worksheet, "Band", "Range (FTE)", "Description");

        var rowNumber = 2;
        foreach (var band in bands)
        {
            var range = band.MaxFte.HasValue
                ? $"{band.MinFte.ToString("0.##", CultureInfo.InvariantCulture)} to {band.MaxFte.Value.ToString("0.##", CultureInfo.InvariantCulture)}"
                : $"{band.MinFte.ToString("0.##", CultureInfo.InvariantCulture)} to no upper limit";

            worksheet.Cell(rowNumber, 1).Value = band.Name;
            worksheet.Cell(rowNumber, 2).Value = range;
            worksheet.Cell(rowNumber, 3).Value = band.Description ?? "—";
            rowNumber++;
        }

        FinishTable(worksheet, 3);
    }

    private static void WriteAggregateSheet(
        IXLWorksheet worksheet,
        string nameHeader,
        IReadOnlyList<ResourcingAggregateRow> rows)
    {
        WriteHeaderRow(worksheet, nameHeader, "Work items", "FTE", "MSC", "Combined", "Band");

        var rowNumber = 2;
        foreach (var row in rows)
        {
            worksheet.Cell(rowNumber, 1).Value = row.Name;
            worksheet.Cell(rowNumber, 2).Value = row.WorkItemCount;
            worksheet.Cell(rowNumber, 3).Value = row.PermFteTotal;
            worksheet.Cell(rowNumber, 4).Value = row.MspFteTotal;
            worksheet.Cell(rowNumber, 5).Value = row.ResourcingFteTotal;
            worksheet.Cell(rowNumber, 6).Value = row.BandName;
            rowNumber++;
        }

        FinishTable(worksheet, 6);
    }

    private static void WriteWorkItemsSheet(IXLWorksheet worksheet, IReadOnlyList<ResourcingWorkItemRow> rows)
    {
        WriteHeaderRow(
            worksheet,
            "Work item ID",
            "Work item",
            "Business area",
            "Directorates",
            "Priority",
            "FTE",
            "MSC",
            "Combined",
            "Band");

        var rowNumber = 2;
        foreach (var row in rows)
        {
            worksheet.Cell(rowNumber, 1).Value = row.WorkItemId;
            worksheet.Cell(rowNumber, 2).Value = row.Title;
            worksheet.Cell(rowNumber, 3).Value = row.BusinessArea;
            worksheet.Cell(rowNumber, 4).Value = row.Directorates;
            worksheet.Cell(rowNumber, 5).Value = string.IsNullOrWhiteSpace(row.Priority) ? "Not set" : row.Priority;
            worksheet.Cell(rowNumber, 6).Value = row.PermFte;
            worksheet.Cell(rowNumber, 7).Value = row.MspFte;
            worksheet.Cell(rowNumber, 8).Value = row.ResourcingFte;
            worksheet.Cell(rowNumber, 9).Value = row.BandName;
            rowNumber++;
        }

        FinishTable(worksheet, 9);
    }

    private static void WriteTrendSheet(IXLWorksheet worksheet, IReadOnlyList<ResourcingTrendMonthPoint> points)
    {
        WriteHeaderRow(
            worksheet,
            "Month",
            "Submitted work items",
            "FTE",
            "MSC",
            "Combined",
            "Change",
            "% change");

        var rowNumber = 2;
        ResourcingTrendMonthPoint? previous = null;
        foreach (var point in points)
        {
            decimal? delta = previous is null ? null : point.ResourcingFteTotal - previous.ResourcingFteTotal;
            decimal? deltaPct = previous is null || previous.ResourcingFteTotal == 0m || !delta.HasValue
                ? null
                : (delta.Value / previous.ResourcingFteTotal) * 100m;

            worksheet.Cell(rowNumber, 1).Value = point.Label;
            worksheet.Cell(rowNumber, 2).Value = point.SubmittedWorkItemCount;
            worksheet.Cell(rowNumber, 3).Value = point.PermFteTotal;
            worksheet.Cell(rowNumber, 4).Value = point.MspFteTotal;
            worksheet.Cell(rowNumber, 5).Value = point.ResourcingFteTotal;
            worksheet.Cell(rowNumber, 6).Value = delta.HasValue ? delta.Value : "";
            worksheet.Cell(rowNumber, 7).Value = deltaPct.HasValue ? deltaPct.Value : "";
            previous = point;
            rowNumber++;
        }

        FinishTable(worksheet, 7);
    }

    private static void WriteHeaderRow(IXLWorksheet worksheet, params string[] headers)
    {
        for (var i = 0; i < headers.Length; i++)
            worksheet.Cell(1, i + 1).Value = headers[i];
        worksheet.Row(1).Style.Font.Bold = true;
    }

    private static void FinishTable(IXLWorksheet worksheet, int columnCount)
    {
        worksheet.SheetView.FreezeRows(1);
        worksheet.Columns(1, columnCount).AdjustToContents();
    }

    private static string ScopeLabel(ModernResourcingReportViewModel report)
    {
        var dimension = report.Dimension switch
        {
            "mission" => "Mission pillar",
            "outcomes" => "Priority outcome",
            "priority" => "Delivery priority",
            _ => "All work"
        };

        return string.IsNullOrWhiteSpace(report.GroupName)
            ? dimension
            : $"{dimension} · {report.GroupName}";
    }

    private static string ResolveFilterName(int? id, IEnumerable<(int Id, string Name)> options)
    {
        if (!id.HasValue)
            return "All";

        return options.FirstOrDefault(o => o.Id == id.Value).Name ?? "All";
    }
}
