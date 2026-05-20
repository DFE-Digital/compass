namespace Compass.ViewModels.Modern;

/// <summary>Toolbar shown above reporting tables that export scoped work data.</summary>
public class ReportingTableExportBarModel
{
    public string Caption { get; set; } = "";
    public int? Count { get; set; }
    public string ExportUrl { get; set; } = "";
    public bool ShowExport { get; set; } = true;
}
