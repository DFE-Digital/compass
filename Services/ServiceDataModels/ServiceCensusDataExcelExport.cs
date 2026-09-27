using ClosedXML.Excel;
using Compass.ViewModels.Modern.ServiceDataModels;

namespace Compass.Services.ServiceDataModels;

/// <summary>
/// Builds the Service Census data workbook: one worksheet per shared theme,
/// service identity columns then that theme's question answers.
/// </summary>
public static class ServiceCensusDataExcelExport
{
    public const string FileName = "service-census-data.xlsx";
    public const string ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public static byte[] BuildWorkbook(ServiceCensusDataExportModel model)
    {
        using var workbook = new XLWorkbook();
        var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var themes = model.Themes
            .OrderBy(t => t.SortOrder)
            .ThenBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (themes.Count == 0)
        {
            var empty = workbook.Worksheets.Add(UniqueSheetName("Census", usedNames));
            WriteHeaderRow(empty, "Service name", "Register ID", "Product ID");
            empty.SheetView.FreezeRows(1);
            empty.Columns().AdjustToContents();
        }
        else
        {
            foreach (var theme in themes)
            {
                var sheet = workbook.Worksheets.Add(UniqueSheetName(theme.Name, usedNames));
                WriteThemeSheet(sheet, theme, model.Services);
            }
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void WriteThemeSheet(
        IXLWorksheet sheet,
        ServiceCensusThemeDefinition theme,
        IReadOnlyList<ServiceCensusDataExportServiceRow> services)
    {
        var fields = theme.Fields
            .OrderBy(f => f.SortOrder)
            .ThenBy(f => f.Label, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var headers = new List<string> { "Service name", "Register ID", "Product ID" };
        headers.AddRange(fields.Select(f => string.IsNullOrWhiteSpace(f.Label) ? f.StableKey : f.Label));
        WriteHeaderRow(sheet, headers.ToArray());

        var rowNumber = 2;
        foreach (var service in services
                     .OrderBy(s => s.ServiceName, StringComparer.OrdinalIgnoreCase)
                     .ThenBy(s => s.RegisterId))
        {
            sheet.Cell(rowNumber, 1).Value = service.ServiceName;
            sheet.Cell(rowNumber, 2).Value = service.RegisterId;
            sheet.Cell(rowNumber, 3).Value = service.ProductId.ToString();

            for (var i = 0; i < fields.Count; i++)
            {
                var field = fields[i];
                var value = service.AnswersByFieldStableKey.TryGetValue(field.StableKey, out var display)
                    ? display
                    : "";
                sheet.Cell(rowNumber, 4 + i).Value = value ?? "";
            }

            rowNumber++;
        }

        sheet.SheetView.FreezeRows(1);
        sheet.Columns().AdjustToContents();
    }

    private static void WriteHeaderRow(IXLWorksheet sheet, params string[] headers)
    {
        for (var col = 0; col < headers.Length; col++)
        {
            var cell = sheet.Cell(1, col + 1);
            cell.Value = headers[col];
            cell.Style.Font.Bold = true;
        }
    }

    /// <summary>Excel-safe unique sheet name (max 31 chars; invalid chars replaced).</summary>
    public static string UniqueSheetName(string? name, ISet<string> usedNames)
    {
        var cleaned = SanitizeSheetName(name);
        if (string.IsNullOrWhiteSpace(cleaned))
            cleaned = "Theme";

        var candidate = cleaned;
        var n = 2;
        while (!usedNames.Add(candidate))
        {
            var suffix = $" ({n})";
            var maxBase = Math.Max(1, 31 - suffix.Length);
            var truncated = cleaned.Length > maxBase ? cleaned[..maxBase] : cleaned;
            candidate = truncated + suffix;
            n++;
        }

        return candidate;
    }

    public static string SanitizeSheetName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "";

        var invalid = new[] { '\\', '/', '*', '?', ':', '[', ']' };
        var cleaned = new string(name.Trim().Select(c => invalid.Contains(c) ? '-' : c).ToArray());
        // Excel disallows leading/trailing apostrophe quirks; trim and clamp.
        cleaned = cleaned.Trim('\'', ' ');
        return cleaned.Length > 31 ? cleaned[..31] : cleaned;
    }
}
