using ClosedXML.Excel;
using Compass.ViewModels.Modern;

namespace Compass.Services.Census;

public static class ServiceCensusReportExcelExport
{
    public static byte[] BuildWorkbook(ServiceCensusReportViewModel report)
    {
        using var workbook = new XLWorkbook();
        WriteOverview(workbook.Worksheets.Add("Overview"), report);
        WriteQuestions(workbook.Worksheets.Add("Questions"), report);
        WriteQuestionSheets(workbook, report);
        WriteProducts(workbook.Worksheets.Add("Products"), report);
        WriteAreas(workbook.Worksheets.Add("Business areas"), report);
        WriteWeeks(workbook.Worksheets.Add("Daily"), report);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void WriteOverview(IXLWorksheet sheet, ServiceCensusReportViewModel report)
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
            ("Products submitted today", report.SubmittedThisWeek.ToString("N0")),
            ("Time to finish", report.WeeksToFinishLabel)
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

    private static void WriteQuestions(IXLWorksheet sheet, ServiceCensusReportViewModel report)
    {
        StyleHeader(sheet, ["Question", "Section", "Products complete", "Products", "Completion %"]);
        var row = 2;
        foreach (var question in report.Questions)
        {
            sheet.Cell(row, 1).Value = question.Question;
            sheet.Cell(row, 2).Value = question.Section;
            sheet.Cell(row, 3).Value = question.CompleteProducts;
            sheet.Cell(row, 4).Value = question.ProductCount;
            sheet.Cell(row, 5).Value = question.AveragePercent / 100.0;
            sheet.Cell(row, 5).Style.NumberFormat.Format = "0%";
            row++;
        }
        Finish(sheet);
    }

    private static void WriteQuestionSheets(XLWorkbook workbook, ServiceCensusReportViewModel report)
    {
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Overview", "Questions", "Products", "Business areas", "Weekly"
        };
        var index = 1;
        foreach (var question in CensusDefinition.Tasks)
        {
            var sheet = workbook.Worksheets.Add(UniqueSheetName($"{index:00} {question.NavLabel}", used));
            var answers = report.SubmissionAnswers
                .Where(a => string.Equals(a.QuestionKey, question.Key, StringComparison.OrdinalIgnoreCase))
                .ToList();
            StyleHeader(sheet, [
                "Product", "Business area", "Census", "Submitted at (UTC)", "Submitted by",
                "Question status", "Cannot answer", "Cannot answer reason",
                "Answer", "Details", "Reference"
            ]);
            var row = 2;
            foreach (var answer in answers)
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
                sheet.Cell(row, 6).Value = answer.QuestionStatus;
                sheet.Cell(row, 7).Value = answer.CannotAnswer ? "Yes" : "No";
                sheet.Cell(row, 8).Value = answer.CannotAnswerReason ?? "";
                sheet.Cell(row, 9).Value = answer.Item ?? "";
                sheet.Cell(row, 10).Value = answer.Details ?? "";
                sheet.Cell(row, 11).Value = answer.Reference ?? "";
                row++;
            }
            Finish(sheet, wrapColumn: 10);
            index++;
        }
    }

    private static string UniqueSheetName(string name, HashSet<string> used)
    {
        var clean = new string(name.Select(c => ":\\/?*[]".Contains(c) ? ' ' : c).ToArray()).Trim();
        if (string.IsNullOrWhiteSpace(clean))
            clean = "Question";
        if (clean.Length > 31)
            clean = clean[..31].Trim();
        var candidate = clean;
        var i = 2;
        while (!used.Add(candidate))
        {
            var suffix = $" ({i})";
            var max = Math.Max(1, 31 - suffix.Length);
            candidate = (clean.Length > max ? clean[..max].Trim() : clean) + suffix;
            i++;
        }
        return candidate;
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

    private static void WriteWeeks(IXLWorksheet sheet, ServiceCensusReportViewModel report)
    {
        StyleHeader(sheet, ["Day", "Date", "Total in progress", "Started", "Submitted", "Cumulative submitted"]);
        var row = 2;
        foreach (var week in report.Weeks)
        {
            sheet.Cell(row, 1).Value = week.Label;
            sheet.Cell(row, 2).Value = week.IsoWeek;
            sheet.Cell(row, 3).Value = week.ProductsInProgress;
            sheet.Cell(row, 4).Value = week.ProductsStarted;
            sheet.Cell(row, 5).Value = week.ProductsSubmitted;
            sheet.Cell(row, 6).Value = week.CumulativeSubmitted;
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
