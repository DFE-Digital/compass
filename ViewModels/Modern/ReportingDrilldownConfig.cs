namespace Compass.ViewModels.Modern;

public static class ReportingDrilldownConfig
{
    public static ReportingDrilldownScriptViewModel Build(
        Dictionary<string, List<BusinessAreaProjectItem>> projectsByKey,
        IEnumerable<BusinessAreaProjectItem> scopeProjects,
        string exportBaseUrl,
        Dictionary<string, string?> exportParams,
        string workDetailPrefix,
        Dictionary<string, string>? dimensionLabels = null,
        string drillRootSelector = ".mr-dash")
    {
        var scope = scopeProjects.ToList();
        var details = scope
            .Concat(projectsByKey.Values.SelectMany(x => x))
            .GroupBy(p => p.Id)
            .ToDictionary(g => g.Key.ToString(), g => g.First());

        return new ReportingDrilldownScriptViewModel
        {
            ProjectsByKey = projectsByKey,
            ScopeProjectItems = scope,
            DetailsById = details,
            WorkDetailPrefix = workDetailPrefix,
            ExportBaseUrl = exportBaseUrl,
            ExportParams = exportParams,
            DimensionLabels = dimensionLabels ?? new Dictionary<string, string>(),
            DrillRootSelector = drillRootSelector
        };
    }
}
