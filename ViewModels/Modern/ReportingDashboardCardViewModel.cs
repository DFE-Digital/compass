namespace Compass.ViewModels.Modern;

/// <summary>Single link card on <see cref="ModernReportingDashboardViewModel"/> landing page.</summary>
public sealed class ReportingDashboardCardViewModel
{
    public required string Title { get; init; }
    public required string Href { get; init; }
    public required string Description { get; init; }
}
