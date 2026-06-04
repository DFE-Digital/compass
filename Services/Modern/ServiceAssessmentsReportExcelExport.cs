using ClosedXML.Excel;
using Compass.Services;
using Compass.ViewModels.Modern;

namespace Compass.Services.Modern;

/// <summary>Excel export for the service assessments report — summary, assessments, actions, and actions by standard.</summary>
public static class ServiceAssessmentsReportExcelExport
{
    public static byte[] BuildWorkbook(
        SasPublishedSummaryResponse? summary,
        ServiceAssessmentResponse? allWithActions,
        IReadOnlyList<SasStandardActionRow> standardRows,
        bool outcomeBreakdownAvailable)
    {
        using var workbook = new XLWorkbook();
        WriteSummarySheet(workbook.Worksheets.Add("Summary"), summary);
        WriteAssessmentsSheet(workbook.Worksheets.Add("Assessments"), summary?.Assessments);
        WriteActionsSheet(workbook.Worksheets.Add("Actions"), allWithActions, summary?.Assessments);
        WriteActionsByStandardSheet(
            workbook.Worksheets.Add("Actions by standard"),
            standardRows,
            outcomeBreakdownAvailable);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void WriteSummarySheet(IXLWorksheet worksheet, SasPublishedSummaryResponse? summary)
    {
        worksheet.Cell(1, 1).Value = "Field";
        worksheet.Cell(1, 2).Value = "Value";
        worksheet.Row(1).Style.Font.Bold = true;

        var rowNumber = 2;
        var total = summary?.Summaries?.TotalAssessments ?? summary?.Assessments?.Count ?? 0;
        worksheet.Cell(rowNumber++, 1).Value = "Published assessments";
        worksheet.Cell(rowNumber - 1, 2).Value = total;

        rowNumber = WriteBreakdownSection(worksheet, rowNumber, "By outcome", summary?.Summaries?.ByOutcome);
        rowNumber = WriteBreakdownSection(worksheet, rowNumber, "By type", summary?.Summaries?.ByType);
        rowNumber = WriteBreakdownSection(worksheet, rowNumber, "By phase", summary?.Summaries?.ByPhase);
        WriteBreakdownSection(worksheet, rowNumber, "By year", summary?.Summaries?.ByYear);

        worksheet.SheetView.FreezeRows(1);
        worksheet.Columns(1, 2).AdjustToContents();
    }

    private static int WriteBreakdownSection(
        IXLWorksheet worksheet,
        int rowNumber,
        string heading,
        Dictionary<string, int>? values)
    {
        if (values is not { Count: > 0 })
            return rowNumber;

        worksheet.Cell(rowNumber, 1).Value = heading;
        worksheet.Row(rowNumber).Style.Font.Bold = true;
        rowNumber++;

        foreach (var pair in values.OrderByDescending(x => x.Value).ThenBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
        {
            worksheet.Cell(rowNumber, 1).Value = pair.Key;
            worksheet.Cell(rowNumber, 2).Value = pair.Value;
            rowNumber++;
        }

        return rowNumber + 1;
    }

    private static void WriteAssessmentsSheet(
        IXLWorksheet worksheet,
        IReadOnlyList<SasPublishedAssessmentRow>? assessments)
    {
        WriteHeaderRow(
            worksheet,
            "Assessment ID",
            "FIPS ID",
            "Name",
            "Type",
            "Phase",
            "Outcome",
            "Portfolio",
            "Assessment date");

        var rowNumber = 2;
        foreach (var a in (assessments ?? Array.Empty<SasPublishedAssessmentRow>())
                     .OrderByDescending(x => x.AssessmentDateTime)
                     .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
        {
            worksheet.Cell(rowNumber, 1).Value = a.AssessmentID;
            worksheet.Cell(rowNumber, 2).Value = a.FIPS_ID ?? "";
            worksheet.Cell(rowNumber, 3).Value = a.Name ?? "";
            worksheet.Cell(rowNumber, 4).Value = a.Type ?? "";
            worksheet.Cell(rowNumber, 5).Value = a.Phase ?? "";
            worksheet.Cell(rowNumber, 6).Value = a.Outcome ?? "";
            worksheet.Cell(rowNumber, 7).Value = a.Portfolio ?? "";
            if (a.AssessmentDateTime.HasValue)
                worksheet.Cell(rowNumber, 8).Value = a.AssessmentDateTime.Value;
            rowNumber++;
        }

        FinishTable(worksheet, 8);
    }

    private static void WriteActionsSheet(
        IXLWorksheet worksheet,
        ServiceAssessmentResponse? allWithActions,
        IReadOnlyList<SasPublishedAssessmentRow>? published)
    {
        WriteHeaderRow(
            worksheet,
            "Assessment ID",
            "Assessment name",
            "Assessment type",
            "Assessment outcome",
            "Assessment phase",
            "Published outcome",
            "Standard",
            "Standard title",
            "Standard outcome",
            "Action ID",
            "Unique ID",
            "Status",
            "Comment",
            "Created",
            "Estimated resolution");

        var publishedById = (published ?? Array.Empty<SasPublishedAssessmentRow>())
            .GroupBy(a => a.AssessmentID)
            .ToDictionary(g => g.Key, g => g.First());

        var rowNumber = 2;
        foreach (var row in FlattenActions(allWithActions))
        {
            publishedById.TryGetValue(row.AssessmentId, out var publishedRow);

            worksheet.Cell(rowNumber, 1).Value = row.AssessmentId;
            worksheet.Cell(rowNumber, 2).Value = row.AssessmentName ?? "";
            worksheet.Cell(rowNumber, 3).Value = row.AssessmentType ?? "";
            worksheet.Cell(rowNumber, 4).Value = row.AssessmentOutcome ?? "";
            worksheet.Cell(rowNumber, 5).Value = row.AssessmentPhase ?? "";
            worksheet.Cell(rowNumber, 6).Value = publishedRow?.Outcome ?? "";
            worksheet.Cell(rowNumber, 7).Value = row.Standard;
            worksheet.Cell(rowNumber, 8).Value = row.StandardTitle ?? "";
            worksheet.Cell(rowNumber, 9).Value = row.StandardOutcome ?? "";
            worksheet.Cell(rowNumber, 10).Value = row.ActionId ?? "";
            worksheet.Cell(rowNumber, 11).Value = row.UniqueId ?? "";
            worksheet.Cell(rowNumber, 12).Value = row.Status ?? "";
            worksheet.Cell(rowNumber, 13).Value = row.Comments ?? "";
            if (row.Created.HasValue)
                worksheet.Cell(rowNumber, 14).Value = row.Created.Value;
            if (row.EstimatedResolutionDate.HasValue)
                worksheet.Cell(rowNumber, 15).Value = row.EstimatedResolutionDate.Value;
            rowNumber++;
        }

        FinishTable(worksheet, 15);
    }

    private static void WriteActionsByStandardSheet(
        IXLWorksheet worksheet,
        IReadOnlyList<SasStandardActionRow> standardRows,
        bool outcomeBreakdownAvailable)
    {
        if (outcomeBreakdownAvailable)
        {
            WriteHeaderRow(
                worksheet,
                "Standard",
                "Action count",
                "Assessment count",
                "Red outcome actions",
                "Amber outcome actions",
                "Green outcome actions",
                "Other outcome actions",
                "Amber + red %");
        }
        else
        {
            WriteHeaderRow(
                worksheet,
                "Standard",
                "Action count",
                "Assessment count");
        }

        var rowNumber = 2;
        foreach (var row in standardRows.OrderBy(r => r.Standard))
        {
            worksheet.Cell(rowNumber, 1).Value = row.Standard;
            worksheet.Cell(rowNumber, 2).Value = row.ActionCount;
            worksheet.Cell(rowNumber, 3).Value = row.AssessmentCount;
            if (outcomeBreakdownAvailable)
            {
                worksheet.Cell(rowNumber, 4).Value = row.ActionsFromRedOutcome;
                worksheet.Cell(rowNumber, 5).Value = row.ActionsFromAmberOutcome;
                worksheet.Cell(rowNumber, 6).Value = row.ActionsFromGreenOutcome;
                worksheet.Cell(rowNumber, 7).Value = row.ActionsFromOtherOutcome;
                if (row.PctOnAmberOrRed is { } pct)
                    worksheet.Cell(rowNumber, 8).Value = pct / 100.0;
            }

            rowNumber++;
        }

        if (outcomeBreakdownAvailable)
            worksheet.Column(8).Style.NumberFormat.Format = "0.0%";

        FinishTable(worksheet, outcomeBreakdownAvailable ? 8 : 3);
    }

    private static IReadOnlyList<FlattenedActionRow> FlattenActions(ServiceAssessmentResponse? response)
    {
        if (response?.Assessments is not { Count: > 0 } assessments)
            return Array.Empty<FlattenedActionRow>();

        var list = new List<FlattenedActionRow>();
        foreach (var assessment in assessments)
        {
            if (assessment.ActionsByStandard is not { Count: > 0 } blocks)
                continue;

            foreach (var block in blocks)
            {
                if (block.Actions is not { Count: > 0 } actions)
                    continue;

                foreach (var action in block.Actions)
                {
                    list.Add(new FlattenedActionRow
                    {
                        AssessmentId = assessment.AssessmentID,
                        AssessmentName = assessment.AssessmentName,
                        AssessmentType = assessment.AssessmentType,
                        AssessmentOutcome = assessment.AssessmentOutcome,
                        AssessmentPhase = assessment.AssessmentPhase,
                        Standard = action.Standard > 0 ? action.Standard : block.Standard,
                        StandardTitle = !string.IsNullOrWhiteSpace(action.StandardTitle)
                            ? action.StandardTitle
                            : block.StandardTitle,
                        StandardOutcome = !string.IsNullOrWhiteSpace(action.StandardOutcome)
                            ? action.StandardOutcome
                            : block.StandardOutcome,
                        ActionId = action.ActionID,
                        UniqueId = action.UniqueID,
                        Status = action.Status,
                        Comments = action.Comments,
                        Created = action.Created,
                        EstimatedResolutionDate = action.EstimatedResolutionDate
                    });
                }
            }
        }

        return list
            .OrderBy(x => x.AssessmentName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Standard)
            .ThenByDescending(x => x.Created ?? DateTime.MinValue)
            .ToList();
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

    private sealed class FlattenedActionRow
    {
        public int AssessmentId { get; init; }
        public string? AssessmentName { get; init; }
        public string? AssessmentType { get; init; }
        public string? AssessmentOutcome { get; init; }
        public string? AssessmentPhase { get; init; }
        public int Standard { get; init; }
        public string? StandardTitle { get; init; }
        public string? StandardOutcome { get; init; }
        public string? ActionId { get; init; }
        public string? UniqueId { get; init; }
        public string? Status { get; init; }
        public string? Comments { get; init; }
        public DateTime? Created { get; init; }
        public DateTime? EstimatedResolutionDate { get; init; }
    }
}
