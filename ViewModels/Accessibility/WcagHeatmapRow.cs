namespace Compass.ViewModels.Accessibility;

public class WcagHeatmapRow
{
    public int CriterionId { get; set; }
    public string Criterion { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public int IssueCount { get; set; }
    public string Principle { get; set; } = string.Empty;
}
