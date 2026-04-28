namespace Compass.Models.Modern.Work;

public class MonthlyReportViewModel
{
    public int WorkItemId { get; set; }
    public string WorkItemTitle { get; set; } = string.Empty;
    public string? WorkItemReference { get; set; }

    public int Year { get; set; }
    public int Month { get; set; }
    public string PeriodLabel => new DateTime(Year, Month, 1).ToString("MMMM yyyy");

    public int? UpdateId { get; set; }
    public bool IsSubmitted { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public string? SubmittedByName { get; set; }

    public string? Narrative { get; set; }
    public decimal? PermFte { get; set; }
    public decimal? MspFte { get; set; }

    public int? RagStatusId { get; set; }
    public string? RagJustification { get; set; }
    public string? PathToGreen { get; set; }

    public DateTime DueDate { get; set; }
    public DateTime CloseDate { get; set; }
    public bool CanUnsubmit { get; set; }
    public bool IsPastCloseDate { get; set; }

    /// <summary>Configured explicit reporting period dates are present for this month.</summary>
    public bool UsesExplicitReportingPeriod { get; set; }

    /// <summary>Whether this page can create/edit submission for this reporting month (submission window).</summary>
    public bool CanEditMonthlySubmission { get; set; }

    public MonthlyReportPreviousMonth? PreviousMonth { get; set; }

    public List<RagStatus> RagStatuses { get; set; } = new();
}

public class MonthlyReportPreviousMonth
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string PeriodLabel => new DateTime(Year, Month, 1).ToString("MMMM yyyy");
    public string? Narrative { get; set; }
    public decimal? PermFte { get; set; }
    public decimal? MspFte { get; set; }
    public string? RagStatusName { get; set; }
    public string? RagJustification { get; set; }
    public string? PathToGreen { get; set; }
    public DateTime? SubmittedAt { get; set; }
}
