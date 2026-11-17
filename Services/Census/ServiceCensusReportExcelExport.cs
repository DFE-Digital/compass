using ClosedXML.Excel;
using Compass.ViewModels.Modern;

namespace Compass.Services.Census;

public static class ServiceCensusReportExcelExport
{
    public static byte[] BuildWorkbook(ServiceCensusReportViewModel report)
    {
        using var workbook = new XLWorkbook();
        WriteSummary(workbook.Worksheets.Add("Summary"), report);
        WriteProducts(workbook.Worksheets.Add("Products"), report);
        WriteAnswers(workbook.Worksheets.Add("Answers"), report);
        WriteContacts(workbook.Worksheets.Add("Contacts"), report);
        WriteAreas(workbook.Worksheets.Add("Business areas"), report);
        WriteSections(workbook.Worksheets.Add("Sections"), report);
        WriteWeeks(workbook.Worksheets.Add("Weekly"), report);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void WriteSummary(IXLWorksheet sheet, ServiceCensusReportViewModel report)
    {
        StyleHeader(sheet, ["Field", "Value"]);
        var rows = new (string Label, string Value)[]
        {
            ("Exported", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm") + " UTC"),
            ("Business area filter", string.IsNullOrWhiteSpace(report.BusinessAreaFilter) ? "All business areas" : report.BusinessAreaFilter),
            ("Overall completion", report.OverallPercent + "%"),
            ("Total", report.ProductCount.ToString("N0")),
            ("Not started", report.NotStarted.ToString("N0")),
            ("In progress", report.InProgress.ToString("N0")),
            ("Submitted", report.Submitted.ToString("N0")),
            ("Products submitted this week", report.SubmittedThisWeek.ToString("N0")),
            ("Weeks to finish", report.WeeksToFinishLabel)
        };
        var row = 2;
        foreach (var (label, value) in rows)
        {
            sheet.Cell(row, 1).Value = label;
            sheet.Cell(row, 2).Value = value;
            row++;
        }
        Finish(sheet);
    }

    private static void WriteProducts(IXLWorksheet sheet, ServiceCensusReportViewModel report)
    {
        StyleHeader(sheet, ["Product", "Business area", "Census", "Questions complete", "Questions", "Completion %"]);
        var row = 2;
        foreach (var product in report.Products)
        {
            sheet.Cell(row, 1).Value = product.Title;
            sheet.Cell(row, 2).Value = product.Areas;
            sheet.Cell(row, 3).Value = product.CensusStatus;
            sheet.Cell(row, 4).Value = product.TasksComplete;
            sheet.Cell(row, 5).Value = product.TaskCount;
            sheet.Cell(row, 6).Value = product.Percent / 100.0;
            sheet.Cell(row, 6).Style.NumberFormat.Format = "0%";
            row++;
        }
        Finish(sheet);
    }

    private static void WriteAnswers(IXLWorksheet sheet, ServiceCensusReportViewModel report)
    {
        StyleHeader(sheet, [
            "Product", "Business area", "Census", "Submitted at (UTC)", "Submitted by",
            "Section", "Question", "Question status", "Cannot answer", "Cannot answer reason",
            "Answer", "Details", "Reference"
        ]);
        var row = 2;
        foreach (var answer in report.SubmissionAnswers)
        {
            sheet.Cell(row, 1).Value = answer.Product;
            sheet.Cell(row, 2).Value = answer.Areas;
            sheet.Cell(row, 3).Value = answer.CensusStatus;
            if (answer.SubmittedAt is DateTime submitted)
            {
                sheet.Cell(row, 4).Value = submitted;
                sheet.Cell(row, 4).Style.DateFormat.Format = "yyyy-mm-dd hh:mm";
            }
            sheet.Cell(row, 5).Value = answer.SubmittedBy ?? "";
            sheet.Cell(row, 6).Value = answer.Section;
            sheet.Cell(row, 7).Value = answer.Question;
            sheet.Cell(row, 8).Value = answer.QuestionStatus;
            sheet.Cell(row, 9).Value = answer.CannotAnswer ? "Yes" : "No";
            sheet.Cell(row, 10).Value = answer.CannotAnswerReason ?? "";
            sheet.Cell(row, 11).Value = answer.Item ?? "";
            sheet.Cell(row, 12).Value = answer.Details ?? "";
            sheet.Cell(row, 13).Value = answer.Reference ?? "";
            row++;
        }
        Finish(sheet, wrapColumn: 12);
    }

    private static void WriteContacts(IXLWorksheet sheet, ServiceCensusReportViewModel report)
    {
        StyleHeader(sheet, ["Product", "Business area", "Census", "Role", "Name", "Email", "Change"]);
        var row = 2;
        foreach (var contact in report.SubmissionContacts)
        {
            sheet.Cell(row, 1).Value = contact.Product;
            sheet.Cell(row, 2).Value = contact.Areas;
            sheet.Cell(row, 3).Value = contact.CensusStatus;
            sheet.Cell(row, 4).Value = contact.Role;
            sheet.Cell(row, 5).Value = contact.Name ?? "";
            sheet.Cell(row, 6).Value = contact.Email ?? "";
            sheet.Cell(row, 7).Value = contact.Change;
            row++;
        }
        Finish(sheet);
    }

    private static void WriteAreas(IXLWorksheet sheet, ServiceCensusReportViewModel report)
    {
        StyleHeader(sheet, ["Business area", "Products", "Average completion %", "Not started", "In progress", "Submitted"]);
        var row = 2;
        foreach (var area in report.BusinessAreas)
        {
            sheet.Cell(row, 1).Value = area.Name;
            sheet.Cell(row, 2).Value = area.ProductCount;
            sheet.Cell(row, 3).Value = area.AveragePercent / 100.0;
            sheet.Cell(row, 3).Style.NumberFormat.Format = "0%";
            sheet.Cell(row, 4).Value = area.NotStarted;
            sheet.Cell(row, 5).Value = area.InProgress;
            sheet.Cell(row, 6).Value = area.Submitted;
            row++;
        }
        Finish(sheet);
    }

    private static void WriteSections(IXLWorksheet sheet, ServiceCensusReportViewModel report)
    {
        StyleHeader(sheet, ["Section", "Average completion %", "Products complete", "Products"]);
        var row = 2;
        foreach (var section in report.Sections)
        {
            sheet.Cell(row, 1).Value = section.Name;
            sheet.Cell(row, 2).Value = section.AveragePercent / 100.0;
            sheet.Cell(row, 2).Style.NumberFormat.Format = "0%";
            sheet.Cell(row, 3).Value = section.CompleteProducts;
            sheet.Cell(row, 4).Value = section.ProductCount;
            row++;
        }
        Finish(sheet);
    }

    private static void WriteWeeks(IXLWorksheet sheet, ServiceCensusReportViewModel report)
    {
        StyleHeader(sheet, ["Week starting", "ISO week", "In progress", "Submitted", "Cumulative submitted"]);
        var row = 2;
        foreach (var week in report.Weeks)
        {
            sheet.Cell(row, 1).Value = week.Label;
            sheet.Cell(row, 2).Value = week.IsoWeek;
            sheet.Cell(row, 3).Value = week.ProductsInProgress;
            sheet.Cell(row, 4).Value = week.ProductsSubmitted;
            sheet.Cell(row, 5).Value = week.CumulativeSubmitted;
            row++;
        }
        Finish(sheet);
    }

    private static void StyleHeader(IXLWorksheet sheet, IReadOnlyList<string> headers)
    {
        for (var col = 0; col < headers.Count; col++)
        {
            var cell = sheet.Cell(1, col + 1);
            cell.Value = headers[col];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#f3f2f1");
        }
        sheet.SheetView.FreezeRows(1);
    }

    private static void Finish(IXLWorksheet sheet, int? wrapColumn = null)
    {
        sheet.Columns().AdjustToContents();
        if (sheet.Column(1).Width > 60)
            sheet.Column(1).Width = 60;
        if (wrapColumn is int col)
        {
            if (sheet.Column(col).Width > 80)
                sheet.Column(col).Width = 80;
            sheet.Column(col).Style.Alignment.WrapText = true;
        }
    }
}
