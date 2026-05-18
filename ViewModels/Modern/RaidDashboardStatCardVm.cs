namespace Compass.ViewModels.Modern;

public sealed class RaidDashboardStatCardVm
{
    public string Label { get; init; } = "";
    public string Value { get; init; } = "";
    public string? Trend { get; init; }
    public string? Href { get; init; }
    public string? AriaLabel { get; init; }
    public string? TintClass { get; init; }
}
