namespace Compass.ViewModels.Modern;

/// <summary>JSON config for <c>reporting-drilldown.js</c> on modern reporting pages.</summary>
public sealed class ReportingDrilldownScriptViewModel
{
    public Dictionary<string, List<BusinessAreaProjectItem>> ProjectsByKey { get; init; } = new();

    public List<BusinessAreaProjectItem> ScopeProjectItems { get; init; } = new();

    public Dictionary<string, BusinessAreaProjectItem> DetailsById { get; init; } = new();

    public string WorkDetailPrefix { get; init; } = "/modern/work/detail/";

    public string ExportBaseUrl { get; init; } = "";

    public Dictionary<string, string?> ExportParams { get; init; } = new();

    public Dictionary<string, string> DimensionLabels { get; init; } = new();

    public string DrillRootSelector { get; init; } = ".mr-dash";
}
