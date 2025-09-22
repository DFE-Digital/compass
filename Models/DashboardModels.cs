using FipsReporting.Data;

namespace FipsReporting.Models
{
    public class DashboardViewModel
    {
        public DateTime CurrentDate { get; set; }
        public List<ProductViewModel> Products { get; set; } = new List<ProductViewModel>();
        public List<ReportingCycle> ReportingCycles { get; set; } = new List<ReportingCycle>();
        public List<MetricHistoryData> MetricsHistory { get; set; } = new List<MetricHistoryData>();
        public MilestonesOverview MilestonesOverview { get; set; } = new MilestonesOverview();
        public ReportsStatus ReportsStatus { get; set; } = new ReportsStatus();
        public string UserId { get; set; } = string.Empty;
    }

    public class ReportingCycle
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public DateTime CycleStart { get; set; }
        public DateTime CycleEnd { get; set; }
        public DateTime DueDate { get; set; }
        public ReportingCycleStatus Status { get; set; }
        public int DaysRemaining => (DueDate - DateTime.UtcNow).Days;
        public bool IsOverdue => Status == ReportingCycleStatus.Overdue;
        public bool IsDue => Status == ReportingCycleStatus.Due;
        public bool IsCurrent => Status == ReportingCycleStatus.Current;
        public bool IsUpcoming => Status == ReportingCycleStatus.Upcoming;
    }

    public enum ReportingCycleStatus
    {
        Upcoming,
        Current,
        Due,
        Overdue,
        Completed
    }

    public class MetricHistoryData
    {
        public string MetricName { get; set; } = string.Empty;
        public List<MetricDataPoint> DataPoints { get; set; } = new List<MetricDataPoint>();
        public bool HasData => DataPoints.Any();
        public int DataPointCount => DataPoints.Count;
        public DateTime? LastSubmission => DataPoints.OrderByDescending(d => d.Date).FirstOrDefault()?.Date;
    }

    public class MetricDataPoint
    {
        public DateTime Date { get; set; }
        public string Value { get; set; } = string.Empty;
        public string ReportingPeriod { get; set; } = string.Empty;
        public string? ProductId { get; set; }
        public double? NumericValue
        {
            get
            {
                if (double.TryParse(Value, out double result))
                    return result;
                return null;
            }
        }
    }

    public class MilestonesOverview
    {
        public int TotalMilestones { get; set; }
        public int CompletedMilestones { get; set; }
        public int InProgressMilestones { get; set; }
        public int OverdueMilestones { get; set; }
        public int UpcomingMilestones { get; set; }
        public List<Milestone> RecentMilestones { get; set; } = new List<Milestone>();
        
        public double CompletionPercentage => TotalMilestones > 0 ? (double)CompletedMilestones / TotalMilestones * 100 : 0;
        public bool HasOverdueMilestones => OverdueMilestones > 0;
        public bool HasInProgressMilestones => InProgressMilestones > 0;
    }

    public class ReportsStatus
    {
        public int TotalMetrics { get; set; }
        public int SubmittedReports { get; set; }
        public int PendingReports { get; set; }
        public int OverdueReports { get; set; }
        public int DueThisWeek { get; set; }
        
        public double CompletionPercentage => TotalMetrics > 0 ? (double)SubmittedReports / TotalMetrics * 100 : 0;
        public bool HasOverdueReports => OverdueReports > 0;
        public bool HasDueThisWeek => DueThisWeek > 0;
        public bool IsFullyComplete => PendingReports == 0 && OverdueReports == 0;
    }
}
