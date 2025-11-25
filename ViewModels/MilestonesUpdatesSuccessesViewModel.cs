namespace Compass.ViewModels;

public class MilestonesUpdatesSuccessesOverviewViewModel
{
    public int ProjectId { get; set; }
    public string ProjectTitle { get; set; } = string.Empty;
    public string ProjectCode { get; set; } = string.Empty;
    
    // Counts
    public int TotalMilestones { get; set; }
    public int ActiveMilestones { get; set; }
    public int CompletedMilestones { get; set; }
    public int OverdueMilestones { get; set; }
    
    public int TotalMonthlyUpdates { get; set; }
    public int DueMonthlyUpdates { get; set; }
    public int LateMonthlyUpdates { get; set; }
    public int SubmittedMonthlyUpdates { get; set; }
    
    public int TotalWeeklySuccesses { get; set; }
    public int ThisWeekSuccesses { get; set; }
    
    // Monthly update periods
    public List<MonthlyUpdatePeriodStatus> MonthlyUpdatePeriods { get; set; } = new();
}

public class MonthlyUpdatePeriodStatus
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string PeriodName { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public string Status { get; set; } = string.Empty; // "upcoming", "due", "late", "submitted"
    public DateTime? SubmittedDate { get; set; }
    public bool HasUpdate { get; set; }
}

