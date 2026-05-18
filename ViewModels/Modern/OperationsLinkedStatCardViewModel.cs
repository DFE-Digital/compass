namespace Compass.ViewModels.Modern;

/// <summary>Linked DfE stat card for Operations dashboards.</summary>
public sealed class OperationsLinkedStatCardViewModel
{
    public required string Label { get; init; }
    public required string Value { get; init; }
    public required string TrendText { get; init; }
    public required string Href { get; init; }
    public string? Title { get; init; }
    public string? AriaLabel { get; init; }
    public string Tint { get; init; } = "grey";
    public string LinkClass { get; init; } = "ops-dashboard-stat-metric";
}
