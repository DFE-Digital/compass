using Compass.Controllers;
using Compass.Models;

namespace Compass.ViewModels;

/// <summary>Monthly submission progress report (<c>/modern/reporting/monthly-submission-progress</c>).</summary>
public class ModernMonthlySubmissionProgressViewModel
{
    public int ReportYear { get; set; }
    public int ReportMonth { get; set; }
    public string MonthName { get; set; } = "";
    public DateTime MonthStart { get; set; }
    public DateTime MonthEnd { get; set; }

    public int DefaultReportYear { get; set; }
    public int DefaultReportMonth { get; set; }
    public int MinReportYear { get; set; } = 2026;
    public int MaxReportYear { get; set; } = 2026;

    public int? FilterBusinessAreaId { get; set; }
    public int? FilterDirectorateId { get; set; }

    public List<BusinessAreaLookup> BusinessAreas { get; set; } = new();
    public List<Division> Directorates { get; set; } = new();

    public MonthlyUpdateStats? MonthlyUpdateStats { get; set; }

    /// <summary>First day of the submission window (inclusive).</summary>
    public DateTime SubmissionWindowStart { get; set; }

    /// <summary>Last day of the submission window (inclusive).</summary>
    public DateTime SubmissionWindowEnd { get; set; }

    public bool UsesExplicitReportingPeriod { get; set; }
    public string SubmissionWindowDescription { get; set; } = "";

    /// <summary>Target completion % for today based on elapsed time in the submission window.</summary>
    public decimal ExpectedProgressPercentToday { get; set; }

    public List<SubmissionProgressDayPoint> DailyProgress { get; set; } = new();

    public List<MonthlySubmissionLeagueRow> BusinessAreaLeague { get; set; } = new();
    public List<MonthlySubmissionLeagueRow> DirectorateLeague { get; set; } = new();

    /// <summary>Last six reporting months ending at <see cref="ReportYear"/>/<see cref="ReportMonth"/>.</summary>
    public List<SubmissionTrendMonthColumn> TrendMonthColumns { get; set; } = new();

    public List<BusinessAreaMonthlySubmissionTrendRow> BusinessAreaTrendRows { get; set; } = new();

    public bool HasPreviousMonthNav { get; set; }
    public bool HasNextMonthNav { get; set; }
    public int? PreviousNavYear { get; set; }
    public int? PreviousNavMonth { get; set; }
    public int? NextNavYear { get; set; }
    public int? NextNavMonth { get; set; }
}

public class SubmissionProgressDayPoint
{
    public string Label { get; set; } = "";
    public DateTime Date { get; set; }
    /// <summary>Count of work items submitted on or before this day (cumulative).</summary>
    public int ActualCumulative { get; set; }
    /// <summary>Linear target count for this day (cumulative).</summary>
    public decimal ExpectedCumulative { get; set; }
    /// <summary>Cumulative completion 0–100 for charting against the linear target.</summary>
    public decimal ActualCompletionPercent { get; set; }
    /// <summary>Linear target completion 0–100 for this day.</summary>
    public decimal ExpectedCompletionPercent { get; set; }
    public int TotalInScope { get; set; }
}

/// <summary>League table row with actual vs expected submission progress.</summary>
public class MonthlySubmissionLeagueRow
{
    public string Name { get; set; } = "";
    public int? EntityId { get; set; }
    public int TotalToReport { get; set; }
    public int Submitted { get; set; }
    public int InProgress { get; set; }
    public int Late { get; set; }
    public int NotStarted { get; set; }
    public decimal ActualProgressPercent { get; set; }
    public decimal ExpectedProgressPercent { get; set; }

    public decimal ProgressGapPercent =>
        Math.Round(ActualProgressPercent - ExpectedProgressPercent, 1, MidpointRounding.AwayFromZero);
}

public class SubmissionTrendMonthColumn
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string Label { get; set; } = "";
}

public class BusinessAreaMonthlySubmissionCell
{
    public int TotalInScope { get; set; }
    public int Submitted { get; set; }
    public decimal CompletionPercent { get; set; }

    /// <summary>Change vs the previous month in the window; <see cref="SubmissionReportingTrend.InsufficientData"/> for the first month.</summary>
    public SubmissionReportingTrend MonthOverMonth { get; set; } = SubmissionReportingTrend.InsufficientData;
}

public enum SubmissionReportingTrend
{
    InsufficientData,
    Improving,
    Stable,
    Worsening
}

public class BusinessAreaMonthlySubmissionTrendRow
{
    public string BusinessAreaName { get; set; } = "";
    public int? BusinessAreaId { get; set; }
    public List<BusinessAreaMonthlySubmissionCell> Months { get; set; } = new();
    public SubmissionReportingTrend Trend { get; set; }
    public string TrendSummary { get; set; } = "";
}
