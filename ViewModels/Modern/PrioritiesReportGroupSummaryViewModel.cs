using Compass.ViewModels;

namespace Compass.ViewModels.Modern;

public sealed class PrioritiesReportGroupSummaryViewModel
{
    public string Dimension { get; init; } = "mission";

    public string GroupColumnLabel { get; init; } = "Group";

    public IReadOnlyList<ModernBusinessAreaDashboardRow> Rows { get; init; } = Array.Empty<ModernBusinessAreaDashboardRow>();

    public string TableId { get; init; } = "pr-group-table";
}
