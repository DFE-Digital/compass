using System.Globalization;
using ClosedXML.Excel;

namespace Compass.Services.Fips;

/// <summary>Parse and build FIPS completion export workbooks (DdtReports export format).</summary>
public static class FipsCompletionSpreadsheet
{
    public static readonly string[] ExportHeaders =
    {
        "Product title",
        "FIPS ID",
        "State",
        "Phase",
        "Has phase",
        "Business area",
        "Has business area",
        "Contacts count",
        "Contacts",
        "Senior responsible officer",
        "Service owner",
        "Information asset owner",
        "Delivery manager",
        "Product URL",
        "Has product URL",
        "User groups",
        "User groups count",
        "Completion %"
    };

    public sealed class ImportRow
    {
        public int RowNumber { get; init; }
        public string ProductTitle { get; init; } = "";
        public string? FipsId { get; init; }
        public string? Phase { get; init; }
        public string? ProductUrl { get; init; }

        public bool HasPhaseUpdate => !string.IsNullOrWhiteSpace(Phase);
        public bool HasProductUrlUpdate => !string.IsNullOrWhiteSpace(ProductUrl);
        public bool HasAnyUpdate => HasPhaseUpdate || HasProductUrlUpdate;
    }

    public static byte[] BuildHeaderOnlyWorkbook()
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("FIPS completion");
        for (var column = 0; column < ExportHeaders.Length; column++)
        {
            var cell = worksheet.Cell(1, column + 1);
            cell.Value = ExportHeaders[column];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#f1f3f5");
            cell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
        }

        worksheet.Columns().AdjustToContents();
        worksheet.SheetView.FreezeRows(1);
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public static List<ImportRow> ParseImportRows(Stream stream)
    {
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        if (ms.Length == 0)
            return [];

        ms.Position = 0;
        using var workbook = new XLWorkbook(ms);
        if (workbook.Worksheets.Count == 0)
            throw new InvalidOperationException("Workbook has no worksheets.");

        var ws = workbook.Worksheet(1);
        var firstRow = ws.FirstRowUsed()?.RowNumber() ?? 1;
        var lastRow = ws.LastRowUsed()?.RowNumber() ?? firstRow;
        var lastCol = ws.Row(firstRow).LastCellUsed()?.Address.ColumnNumber ?? 1;

        var headerList = new List<string>();
        for (var c = 1; c <= lastCol; c++)
            headerList.Add(NormalizeHeaderKey(GetCellText(ws.Cell(firstRow, c))));

        var titleIdx = FindColumn(headerList, "producttitle", "title");
        var fipsIdx = FindColumn(headerList, "fipsid");
        var phaseIdx = FindColumn(headerList, "phase");
        var urlIdx = FindColumn(headerList, "producturl");

        if (titleIdx < 0 && fipsIdx < 0)
            throw new InvalidOperationException("Missing Product title or FIPS ID column.");

        if (phaseIdx < 0 && urlIdx < 0)
            throw new InvalidOperationException("Missing Phase and Product URL columns — nothing to import.");

        var rows = new List<ImportRow>();
        for (var r = firstRow + 1; r <= lastRow; r++)
        {
            var title = titleIdx >= 0 ? GetCellText(ws.Cell(r, titleIdx + 1)) : "";
            var fipsId = fipsIdx >= 0 ? NormalizeKey(GetCellText(ws.Cell(r, fipsIdx + 1))) : null;
            var phase = phaseIdx >= 0 ? NullIfEmpty(GetCellText(ws.Cell(r, phaseIdx + 1))) : null;
            var url = urlIdx >= 0 ? NullIfEmpty(GetCellText(ws.Cell(r, urlIdx + 1))) : null;

            if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(fipsId))
                continue;

            if (string.IsNullOrWhiteSpace(phase) && string.IsNullOrWhiteSpace(url))
                continue;

            rows.Add(new ImportRow
            {
                RowNumber = r,
                ProductTitle = title.Trim(),
                FipsId = string.IsNullOrWhiteSpace(fipsId) ? null : fipsId,
                Phase = phase,
                ProductUrl = url
            });
        }

        return rows;
    }

    private static int FindColumn(IReadOnlyList<string> headers, params string[] keys)
    {
        for (var i = 0; i < headers.Count; i++)
        {
            if (keys.Contains(headers[i], StringComparer.OrdinalIgnoreCase))
                return i;
        }

        return -1;
    }

    private static string NormalizeHeaderKey(string? h)
    {
        if (string.IsNullOrWhiteSpace(h))
            return "";
        var t = h.Trim().TrimStart('\uFEFF').ToLowerInvariant();
        return string.Concat(t.Where(c => !char.IsWhiteSpace(c) && c != '-'));
    }

    private static string NormalizeKey(string? s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return "";
        var t = s.Trim().TrimStart('\uFEFF');
        if (t.Length >= 2 && t[0] == '{' && t[^1] == '}')
            t = t[1..^1].Trim();
        return t;
    }

    private static string? NullIfEmpty(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private static string GetCellText(IXLCell cell)
    {
        if (cell.IsEmpty())
            return "";

        try
        {
            if (cell.DataType == XLDataType.Text)
                return cell.GetString().Trim();

            if (cell.DataType == XLDataType.Number)
            {
                var d = cell.GetDouble();
                if (d is >= 0 && d <= long.MaxValue && Math.Abs(d - Math.Truncate(d)) < 1e-9)
                    return ((long)d).ToString(CultureInfo.InvariantCulture);
            }

            return cell.GetFormattedString().Trim();
        }
        catch
        {
            return cell.Value.ToString()?.Trim() ?? "";
        }
    }
}
