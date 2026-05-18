using Compass.Helpers;
using Compass.Models;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Compass.Services.FunctionalStandards;

/// <summary>Builds a structured Word document for a functional standard assessment.</summary>
public static class FunctionalAssessmentWordExporter
{
    private const string ContentType =
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

    public static (byte[] Bytes, string ContentType, string FileName) Export(FunctionalStandardAssessment assessment)
    {
        using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document, true))
        {
            var mainPart = document.AddMainDocumentPart();
            mainPart.Document = new Document(new Body());
            var body = mainPart.Document.Body!;

            var (total, completed, fullyMet, partiallyMet, notMet) =
                FunctionalAssessmentProgress.CountAgainstStandardTree(assessment);

            var standardTitle = assessment.FunctionalStandard?.Title ?? "Functional standard";
            var status = assessment.SubmittedAt.HasValue
                ? $"Submitted {assessment.SubmittedAt.Value:dd MMMM yyyy}"
                : "In progress";

            AddHeading(body, assessment.AssessmentName, 32);
            AddParagraph(body, standardTitle, bold: true, fontSize: 24);
            AddParagraph(body, $"Assessed by: {assessment.AssessedBy}");
            AddParagraph(body, $"Assessment date: {assessment.AssessmentDate:dd MMMM yyyy}");
            AddParagraph(body, $"Status: {status}");
            AddParagraph(body, $"Exported: {DateTime.UtcNow:dd MMMM yyyy HH:mm} UTC");
            AddSpacer(body);

            AddHeading(body, "Assessment summary", 28);
            AddSummaryTable(body, total, completed, fullyMet, partiallyMet, notMet);
            AddSpacer(body);

            var themes = assessment.FunctionalStandard?.Themes?
                .OrderBy(t => t.ThemeId)
                .ToList() ?? new List<FunctionalStandardTheme>();

            for (var themeIndex = 0; themeIndex < themes.Count; themeIndex++)
            {
                if (themeIndex > 0)
                    AddPageBreak(body);

                var theme = themes[themeIndex];
                AddHeading(body, $"Theme {theme.ThemeId}: {theme.Title}", 28);

                if (!string.IsNullOrWhiteSpace(theme.Description))
                    AddParagraph(body, theme.Description);

                foreach (var pa in (theme.PracticeAreas ?? new List<PracticeArea>()).OrderBy(p => p.PracticeAreaId))
                {
                    AddHeading(body, $"Practice area {pa.PracticeAreaId}: {pa.Title}", 24);

                    if (!string.IsNullOrWhiteSpace(pa.Description))
                        AddParagraph(body, pa.Description, italic: true);

                    var criteria = (pa.Criteria ?? new List<Criterion>())
                        .OrderBy(c => c.CriteriaCode)
                        .ToList();

                    AddCriteriaTable(body, theme, pa, criteria, assessment.CriteriaResponses);
                    AddSpacer(body);
                }
            }

            mainPart.Document.Save();
        }

        var safeName = SanitizeFileName(assessment.AssessmentName);
        var fileName = $"FSA-{safeName}-{DateTime.UtcNow:yyyyMMdd}.docx";
        return (stream.ToArray(), ContentType, fileName);
    }

    private static void AddSummaryTable(Body body, int total, int completed, int fullyMet, int partiallyMet, int notMet)
    {
        var rows = new[]
        {
            ("Criteria assessed", $"{completed} of {total}"),
            ("Fully met", fullyMet.ToString()),
            ("Partially met", partiallyMet.ToString()),
            ("Not met", notMet.ToString()),
            ("Unanswered", (total - completed).ToString())
        };

        var table = new Table(
            new TableProperties(
                new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct },
                new TableBorders(
                    new TopBorder { Val = BorderValues.Single, Size = 4 },
                    new LeftBorder { Val = BorderValues.Single, Size = 4 },
                    new BottomBorder { Val = BorderValues.Single, Size = 4 },
                    new RightBorder { Val = BorderValues.Single, Size = 4 },
                    new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4 },
                    new InsideVerticalBorder { Val = BorderValues.Single, Size = 4 })));

        foreach (var (label, value) in rows)
        {
            table.Append(new TableRow(
                CreateTableCell(label, header: true),
                CreateTableCell(value)));
        }

        body.Append(table);
    }

    private static void AddCriteriaTable(
        Body body,
        FunctionalStandardTheme theme,
        PracticeArea pa,
        List<Criterion> criteria,
        List<AssessmentCriteriaResponse> responses)
    {
        var table = new Table(
            new TableProperties(
                new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct },
                new TableBorders(
                    new TopBorder { Val = BorderValues.Single, Size = 4 },
                    new LeftBorder { Val = BorderValues.Single, Size = 4 },
                    new BottomBorder { Val = BorderValues.Single, Size = 4 },
                    new RightBorder { Val = BorderValues.Single, Size = 4 },
                    new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4 },
                    new InsideVerticalBorder { Val = BorderValues.Single, Size = 4 })));

        table.Append(new TableRow(
            CreateTableCell("Code", header: true),
            CreateTableCell("Criterion", header: true),
            CreateTableCell("Rating", header: true),
            CreateTableCell("Attainment", header: true),
            CreateTableCell("Notes", header: true)));

        foreach (var criterion in criteria)
        {
            var response = responses.FirstOrDefault(r =>
                r.ThemeId == theme.ThemeId
                && r.PracticeAreaId == pa.PracticeAreaId
                && string.Equals(r.CriteriaCode, criterion.CriteriaCode, StringComparison.Ordinal));

            table.Append(new TableRow(
                CreateTableCell(criterion.CriteriaCode),
                CreateTableCell(criterion.Criteria),
                CreateTableCell(RatingLabel(criterion.Rating)),
                CreateTableCell(AttainmentLabel(response?.Attainment)),
                CreateTableCell(response?.Notes ?? "")));
        }

        body.Append(table);
    }

    private static TableCell CreateTableCell(string text, bool header = false)
    {
        var cellProps = new TableCellProperties(new TableCellWidth { Type = TableWidthUnitValues.Auto });
        if (header)
            cellProps.Append(new Shading { Val = ShadingPatternValues.Clear, Fill = "DEE0E2", Color = "auto" });

        var run = new Run(new Text(SanitizeXmlText(text)) { Space = SpaceProcessingModeValues.Preserve });
        if (header)
            run.RunProperties = new RunProperties(new Bold());

        return new TableCell(
            cellProps,
            new Paragraph(
                new ParagraphProperties(new SpacingBetweenLines { After = "0", Before = "0" }),
                run));
    }

    private static void AddHeading(Body body, string text, int fontSizeHalfPoints)
    {
        body.Append(new Paragraph(
            new ParagraphProperties(
                new SpacingBetweenLines { Before = "240", After = "120" }),
            new Run(
                new RunProperties(new Bold(), new FontSize { Val = fontSizeHalfPoints.ToString() }),
                new Text(SanitizeXmlText(text)) { Space = SpaceProcessingModeValues.Preserve })));
    }

    private static void AddParagraph(Body body, string text, bool bold = false, bool italic = false, int? fontSize = null)
    {
        var run = new Run(new Text(SanitizeXmlText(text)) { Space = SpaceProcessingModeValues.Preserve });
        if (bold || italic || fontSize.HasValue)
        {
            var props = new RunProperties();
            if (bold) props.Append(new Bold());
            if (italic) props.Append(new Italic());
            if (fontSize.HasValue) props.Append(new FontSize { Val = fontSize.Value.ToString() });
            run.RunProperties = props;
        }

        body.Append(new Paragraph(
            new ParagraphProperties(new SpacingBetweenLines { After = "120" }),
            run));
    }

    private static void AddSpacer(Body body) =>
        body.Append(new Paragraph(new ParagraphProperties(new SpacingBetweenLines { After = "200" })));

    private static void AddPageBreak(Body body) =>
        body.Append(new Paragraph(new Run(new Break { Type = BreakValues.Page })));

    private static string RatingLabel(CriteriaRating rating) => rating switch
    {
        CriteriaRating.Good => "Good",
        CriteriaRating.Better => "Better",
        CriteriaRating.Best => "Best",
        _ => rating.ToString()
    };

    private static string AttainmentLabel(AttainmentLevel? attainment) => attainment switch
    {
        AttainmentLevel.FullyMet => "Fully met",
        AttainmentLevel.PartiallyMet => "Partially met",
        AttainmentLevel.NotOrSeldomMet => "Not, or seldom met",
        _ => "Not assessed"
    };

    private static string SanitizeXmlText(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        return new string(value.Where(ch => ch == '\t' || ch == '\n' || ch == '\r' || ch >= 0x20).ToArray());
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray()).Trim();
        return string.IsNullOrWhiteSpace(cleaned) ? "assessment" : cleaned.Replace(' ', '_');
    }
}
