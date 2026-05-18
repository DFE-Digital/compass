using Compass.ViewModels.Modern;

namespace Compass.ViewModels;

/// <summary>Meeting-oriented RAID report at <c>/modern/reporting/raid</c>.</summary>
public sealed class ModernRaidMeetingReportViewModel
{
    public MonthlyReportPeriodToolbarViewModel PeriodToolbar { get; init; } = new();

    public string FilterScopeSummary { get; init; } = "All business areas · All directorates";

    public int? FilterBusinessAreaId { get; init; }

    public int? FilterDirectorateId { get; init; }

    /// <summary>Headline open-register counts for the overview panel.</summary>
    public MonthlyReportRaidSummary Headline { get; init; } = new();

    public RaidReviewProgressSummary Review { get; init; } = new();

    public decimal ExpectedProgressPercent { get; init; }

    public string ReviewDueLabel { get; init; } = "";

    public DateTime ReviewWindowStart { get; init; }

    public DateTime ReviewWindowEnd { get; init; }

    /// <summary>Risks, issues, matrices and intel lists from the reporting service.</summary>
    public ModernRaidReportingViewModel Reporting { get; init; } = new();

    public RaidMeetingAssumptionsSection Assumptions { get; init; } = RaidMeetingAssumptionsSection.Empty;

    public RaidMeetingDependenciesSection Dependencies { get; init; } = RaidMeetingDependenciesSection.Empty;

    public RaidMeetingNearMissesSection NearMisses { get; init; } = RaidMeetingNearMissesSection.Empty;
}

public sealed record RaidMeetingTableRow(
    int Id,
    string Title,
    string Reference,
    string? MetaLine,
    string Insight,
    string DetailAction,
    string DetailController = "ModernRaid");

public sealed class RaidMeetingAssumptionsSection
{
    public static RaidMeetingAssumptionsSection Empty { get; } = new();

    public int OpenCount { get; init; }

    public int ReviewOverdueCount { get; init; }

    public IReadOnlyDictionary<string, int> StatusCounts { get; init; }
        = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, int> CriticalityCounts { get; init; }
        = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<RaidMeetingTableRow> PriorityRows { get; init; } = [];
}

public sealed class RaidMeetingDependenciesSection
{
    public static RaidMeetingDependenciesSection Empty { get; } = new();

    public int ActiveCount { get; init; }

    public int OverdueCount { get; init; }

    public int DueWithin30DaysCount { get; init; }

    public IReadOnlyDictionary<string, int> CriticalityCounts { get; init; }
        = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<RaidMeetingTableRow> PriorityRows { get; init; } = [];
}

public sealed class RaidMeetingNearMissesSection
{
    public static RaidMeetingNearMissesSection Empty { get; } = new();

    public int OpenCount { get; init; }

    public int HighSeriousnessCount { get; init; }

    public IReadOnlyDictionary<string, int> SeriousnessCounts { get; init; }
        = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<RaidMeetingTableRow> PriorityRows { get; init; } = [];
}
