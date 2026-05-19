using Compass.ViewModels;

namespace Compass.ViewModels.Modern;

/// <summary>Priorities reporting — monthly metrics for mission pillars, priority outcomes, and delivery priorities.</summary>
public sealed class ModernPrioritiesReportViewModel
{
    public ModernMonthlyReportDashboardViewModel Report { get; init; } = new();

    public IReadOnlyList<PrioritiesReportDimensionSection> DimensionSections { get; init; } =
        Array.Empty<PrioritiesReportDimensionSection>();
}

public sealed class PrioritiesReportDimensionSection
{
    /// <summary>mission | outcomes | priority</summary>
    public string Dimension { get; init; } = "mission";

    public string GroupColumnLabel { get; init; } = "";

    public List<ModernBusinessAreaDashboardRow> Rows { get; init; } = new();

    public string TableId => $"pr-group-table-{Dimension}";
}

public sealed class PrioritiesReportGroupOption
{
    public int? GroupId { get; init; }

    public string Name { get; init; } = "";

    public int WorkItemCount { get; init; }
}

/// <summary>Scopes <see cref="ModernMonthlyReportService.BuildDashboardAsync"/> to priorities reporting.</summary>
public sealed class PrioritiesReportOptions
{
    public required string Dimension { get; init; }

    public int? GroupId { get; init; }

    /// <summary>When true, build summary rows for mission, outcomes, and delivery priority (ignores single-dimension filter).</summary>
    public bool IncludeAllDimensions { get; init; }
}
