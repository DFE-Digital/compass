using ClosedXML.Excel;
using Compass.Models.Fips;

namespace Compass.Services.Fips;

/// <summary>Excel export for service register product listings.</summary>
public static class FipsProductExcelExport
{
    public static byte[] BuildWorkbook(
        IReadOnlyList<FipsProductRow> products,
        IReadOnlyDictionary<Guid, string?> cmdbIdByProductId)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Service register");

        var headers = new[]
        {
            "Product title",
            "CMDB ID",
            "Register ID",
            "Status",
            "Phase",
            "Type",
            "Channel",
            "Business area",
            "Service owner",
            "Reporting contact",
            "Contacts",
            "User groups",
            "Data completion %",
        };

        for (var col = 0; col < headers.Length; col++)
        {
            var cell = sheet.Cell(1, col + 1);
            cell.Value = headers[col];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#f3f2f1");
        }

        var row = 2;
        foreach (var p in products)
        {
            cmdbIdByProductId.TryGetValue(p.Id, out var cmdbId);
            var completionPct = p.QualityScoreMax > 0
                ? (int)Math.Round(100.0 * p.QualityScore / p.QualityScoreMax, MidpointRounding.AwayFromZero)
                : 0;

            sheet.Cell(row, 1).Value = p.Title;
            sheet.Cell(row, 2).Value = cmdbId ?? "";
            sheet.Cell(row, 3).Value = p.UniqueID;
            sheet.Cell(row, 4).Value = StatusLabel(p.Status);
            sheet.Cell(row, 5).Value = p.PhaseName ?? "";
            sheet.Cell(row, 6).Value = p.TypesDisplay ?? "";
            sheet.Cell(row, 7).Value = p.ChannelsDisplay ?? "";
            sheet.Cell(row, 8).Value = p.BusinessAreaDisplay ?? "";
            sheet.Cell(row, 9).Value = p.ServiceOwner ?? "";
            sheet.Cell(row, 10).Value = p.ReportingContact ?? "";
            sheet.Cell(row, 11).Value = p.ContactCount;
            sheet.Cell(row, 12).Value = p.UserGroupCount;
            sheet.Cell(row, 13).Value = completionPct / 100.0;
            sheet.Cell(row, 13).Style.NumberFormat.Format = "0%";
            row++;
        }

        sheet.Columns().AdjustToContents();
        sheet.SheetView.FreezeRows(1);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static string StatusLabel(CMDBProductStatus status) =>
        status switch
        {
            CMDBProductStatus.Active => "Active",
            CMDBProductStatus.New => "New",
            CMDBProductStatus.Inactive => "Retired",
            CMDBProductStatus.Rejected => "Rejected",
            _ => status.ToString(),
        };
}
