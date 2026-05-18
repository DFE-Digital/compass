using Compass.Models;

namespace Compass.ViewModels;

/// <summary>RAID monthly review progress report (<c>/modern/reporting/raid-review-progress</c>).</summary>
public class ModernRaidReviewProgressViewModel
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

    public RaidReviewProgressSummary Summary { get; set; } = new();

    public DateTime ReviewWindowStart { get; set; }
    public DateTime ReviewWindowEnd { get; set; }
    public string ReviewDueLabel { get; set; } = "";

    public decimal ExpectedProgressPercentToday { get; set; }

    public List<SubmissionProgressDayPoint> DailyProgress { get; set; } = new();

    public List<RaidReviewLeagueRow> BusinessAreaLeague { get; set; } = new();
    public List<RaidReviewLeagueRow> DirectorateLeague { get; set; } = new();

    public bool HasPreviousMonthNav { get; set; }
    public bool HasNextMonthNav { get; set; }
    public int? PreviousNavYear { get; set; }
    public int? PreviousNavMonth { get; set; }
    public int? NextNavYear { get; set; }
    public int? NextNavMonth { get; set; }
}

public class RaidReviewProgressSummary
{
    public int OpenRisks { get; set; }
    public int OpenIssues { get; set; }
    public int TotalOpen => OpenRisks + OpenIssues;

    public int ReviewedRisks { get; set; }
    public int ReviewedIssues { get; set; }
    public int TotalReviewed => ReviewedRisks + ReviewedIssues;

    public int NotReviewedRisks => OpenRisks - ReviewedRisks;
    public int NotReviewedIssues => OpenIssues - ReviewedIssues;
    public int TotalNotReviewed => TotalOpen - TotalReviewed;

    public decimal ActualProgressPercent { get; set; }
}

/// <summary>League table row for RAID review completion by organisation unit.</summary>
public class RaidReviewLeagueRow
{
    public string Name { get; set; } = "";
    public int? EntityId { get; set; }
    public int OpenRisks { get; set; }
    public int OpenIssues { get; set; }
    public int TotalOpen => OpenRisks + OpenIssues;
    public int ReviewedRisks { get; set; }
    public int ReviewedIssues { get; set; }
    public int TotalReviewed => ReviewedRisks + ReviewedIssues;
    public int NotReviewed => TotalOpen - TotalReviewed;
    public decimal ActualProgressPercent { get; set; }
    public decimal ExpectedProgressPercent { get; set; }

    public decimal ProgressGapPercent =>
        Math.Round(ActualProgressPercent - ExpectedProgressPercent, 1, MidpointRounding.AwayFromZero);
}
