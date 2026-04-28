using Compass.Controllers;
using Compass.Models;
using Compass.Services.Aiss;

namespace Compass.ViewModels;

/// <summary>Data for the modern monthly reporting dashboard (<c>/modern/reporting/monthly-update</c>).</summary>
public class ModernMonthlyReportDashboardViewModel
{
    public int ReportYear { get; set; }
    public int ReportMonth { get; set; }
    public string MonthName { get; set; } = "";
    public DateTime MonthStart { get; set; }
    public DateTime MonthEnd { get; set; }

    /// <summary>Latest period allowed by the 10-day rule (for next-month navigation).</summary>
    public int DefaultReportYear { get; set; }
    public int DefaultReportMonth { get; set; }

    /// <summary>First calendar year with monthly report data (dropdown lower bound).</summary>
    public int MinReportYear { get; set; } = 2026;

    /// <summary>Latest calendar year in the year filter (current UTC year; future years excluded).</summary>
    public int MaxReportYear { get; set; } = 2026;

    public int? FilterBusinessAreaId { get; set; }
    public int? FilterDirectorateId { get; set; }

    public List<BusinessAreaLookup> BusinessAreas { get; set; } = new();
    public List<Division> Directorates { get; set; } = new();

    /// <summary>
    /// Auto-generated prose when viewing a filtered business area (single-BA narrative under Summary metrics).
    /// Null when viewing all areas or no BA selected.
    /// </summary>
    public string? BusinessAreaSummaryNarrative { get; set; }

    public int TotalActiveProjects { get; set; }
    public int NewProjectsCount { get; set; }
    public int MilestonesAchievedCount { get; set; }

    public List<Project> NewProjectsThisMonth { get; set; } = new();
    public List<MilestoneWithProject> MilestonesAchieved { get; set; } = new();
    public List<MilestoneWithProject> UpcomingMilestonesNext30Days { get; set; } = new();
    public List<MilestoneWithProject> LateMilestones { get; set; } = new();

    public MonthlyUpdateStats? MonthlyUpdateStats { get; set; }

    public Dictionary<string, int> RagDistribution { get; set; } = new();
    public Dictionary<string, int> PriorityDistribution { get; set; } = new();

    public Dictionary<string, int> PrevMonthRagDistribution { get; set; } = new();
    public Dictionary<string, int> PrevMonthPriorityDistribution { get; set; } = new();
    public string PrevMonthName { get; set; } = "";

    /// <summary>Submission stats per business area for the selected reporting period.</summary>
    public List<BusinessAreaSubmissionProgressRow> BusinessAreaSubmissionProgress { get; set; } = new();

    public List<ModernBusinessAreaDashboardRow> BusinessAreaRows { get; set; } = new();
    public List<Project> ProjectsWithPathToGreen { get; set; } = new();

    public List<RagPriorityMatrixCell> RagPriorityMatrix { get; set; } = new();

    public int ProjectsWithRagChangeInPeriod { get; set; }
    public int ProjectsWithPriorityChangeInPeriod { get; set; }

    /// <summary>Monthly RAG counts trimmed to the range with actual data.</summary>
    public List<RagTrendMonthPoint> RagTrend { get; set; } = new();

    /// <summary>Monthly priority counts trimmed to the range with actual data.</summary>
    public List<PriorityTrendMonthPoint> PriorityTrend { get; set; } = new();

    /// <summary>Individual project RAG changes during the reporting period.</summary>
    public List<ProjectChangeRow> RagChanges { get; set; } = new();

    /// <summary>Individual project priority changes during the reporting period.</summary>
    public List<ProjectChangeRow> PriorityChanges { get; set; } = new();

    public bool HasPreviousMonthNav { get; set; }
    public bool HasNextMonthNav { get; set; }
    public int? PreviousNavYear { get; set; }
    public int? PreviousNavMonth { get; set; }
    public int? NextNavYear { get; set; }
    public int? NextNavMonth { get; set; }

    /// <summary>Live AISS <c>/api/v1/summary</c> payload (issues, <see cref="AissPlatformSummary.ByBusinessArea"/>, services).</summary>
    public AissPlatformSummary? AccessibilitySummary { get; set; }
    public string? AccessibilitySummaryError { get; set; }

    /// <summary>Rows to show: all business areas, or a single row when the report is filtered to one business area (matched by name).</summary>
    public IReadOnlyList<AissByBusinessAreaRow> AccessibilityAreaRows { get; set; } = Array.Empty<AissByBusinessAreaRow>();

    /// <summary>When not filtered, platform <see cref="AissPlatformSummary.IssueCriteria"/>; when filtered to one BA, that row’s <see cref="AissByBusinessAreaRow.IssueCriteria"/>.</summary>
    public AissIssueCriteriaBlock? AccessibilityIssueCriteria { get; set; }
}

/// <summary>Per–business-area monthly return progress for the selected period.</summary>
public class BusinessAreaSubmissionProgressRow
{
    public string BusinessArea { get; set; } = "";
    public int? BusinessAreaId { get; set; }
    public int TotalToReport { get; set; }
    public int Submitted { get; set; }
    public int InProgress { get; set; }
    public int Late { get; set; }
    public int NotStarted { get; set; }

    /// <summary>0–100 when <see cref="TotalToReport"/> &gt; 0.</summary>
    public decimal CompletionRatePercent { get; set; }
}

public class ModernBusinessAreaDashboardRow
{
    public string BusinessArea { get; set; } = "";
    public int? BusinessAreaId { get; set; }
    public int TotalProjects { get; set; }
    public int SubmittedCount { get; set; }

    /// <summary>Updates started but not submitted before the due date.</summary>
    public int InProgressCount { get; set; }

    /// <summary>Nothing submitted after the due date.</summary>
    public int LateCount { get; set; }

    /// <summary>No update record for the period (and not yet late).</summary>
    public int NotStartedCount { get; set; }

    /// <summary>Submitted / total × 100 (one decimal).</summary>
    public decimal CompletionRatePercent { get; set; }
    public int NewThisMonth { get; set; }
    public int MilestonesCompleted { get; set; }
    public int MilestonesUpcoming30Days { get; set; }
    public int MilestonesLate { get; set; }
    public int RagRed { get; set; }
    public int RagAmberRed { get; set; }
    public int RagAmberGreen { get; set; }
    public int RagGreen { get; set; }
    public int RagNotSet { get; set; }
    public int PriCritical { get; set; }
    public int PriHigh { get; set; }
    public int PriMedium { get; set; }
    public int PriLow { get; set; }
    public int PriNotSet { get; set; }

    /// <summary>Lightweight project list for drill-down when a cell is clicked.</summary>
    public List<BusinessAreaProjectItem> Projects { get; set; } = new();
}

/// <summary>Lightweight item for the business area drill-down table.</summary>
public class BusinessAreaProjectItem
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Rag { get; set; } = "";
    public string Priority { get; set; } = "";
    public bool SubmittedUpdate { get; set; }
    public bool IsNew { get; set; }

    /// <summary>Matches <see cref="ModernBusinessAreaDashboardRow.MilestonesCompleted"/> — ≥1 milestone completed in the reporting month.</summary>
    public bool HasMilestoneCompletedInPeriod { get; set; }

    /// <summary>Matches milestone “due soon” column — ≥1 open milestone due in [month start, month start + 30 days).</summary>
    public bool HasMilestoneUpcomingInWindow { get; set; }

    /// <summary>Matches milestone “late” column — ≥1 open milestone past due.</summary>
    public bool HasLateMilestone { get; set; }
}

public class RagPriorityMatrixCell
{
    public string Rag { get; set; } = "";
    public string Priority { get; set; } = "";
    public int Count { get; set; }
}

public class RagTrendMonthPoint
{
    public string Label { get; set; } = "";
    public int Year { get; set; }
    public int Month { get; set; }
    public int Red { get; set; }
    public int AmberRed { get; set; }
    public int AmberGreen { get; set; }
    public int Green { get; set; }
    public int NotSet { get; set; }
}

public class PriorityTrendMonthPoint
{
    public string Label { get; set; } = "";
    public int Year { get; set; }
    public int Month { get; set; }
    public int Critical { get; set; }
    public int High { get; set; }
    public int Medium { get; set; }
    public int Low { get; set; }
    public int NotSet { get; set; }
}

public class ProjectChangeRow
{
    public int ProjectId { get; set; }
    public string Title { get; set; } = "";
    public string? BusinessArea { get; set; }
    public string From { get; set; } = "";
    public string To { get; set; } = "";
    public DateTime ChangedAt { get; set; }
    public string? Justification { get; set; }
    public string? RagJustification { get; set; }
    public string? LatestNarrative { get; set; }
}
