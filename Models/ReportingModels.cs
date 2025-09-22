namespace FipsReporting.Models
{
    public class ReportingViewModel
    {
        public List<CmsProduct> AssignedProducts { get; set; } = new List<CmsProduct>();
        public ReportingPeriod CurrentPeriod { get; set; } = new ReportingPeriod();
        public List<ReportingPeriod> ReportingPeriods { get; set; } = new List<ReportingPeriod>();
        public List<FipsReporting.Data.PerformanceSubmission> SubmittedReturns { get; set; } = new List<FipsReporting.Data.PerformanceSubmission>();
        public int DueReportsCount { get; set; }
        public int OverdueReportsCount { get; set; }
        public int MilestonesCount { get; set; }
    }

    public class ReportingPeriod
    {
        public string Month { get; set; } = "";
        public DateTime DueDate { get; set; }
        public string Status { get; set; } = "";
        public string Period { get; set; } = "";
    }

    public class MonthReportingViewModel
    {
        public int Year { get; set; }
        public string Month { get; set; } = "";
        public string FullMonthName { get; set; } = "";
        public List<CmsProduct> AssignedProducts { get; set; } = new List<CmsProduct>();
        public DateTime DueDate { get; set; }
        public List<ReportingPeriod> ReportingPeriods { get; set; } = new List<ReportingPeriod>();
        
        // Status tracking properties
        public string OverallSubmissionStatus { get; set; } = "Cannot submit"; // Cannot submit, Ready to submit, Submitted
        public string DueDateStatus { get; set; } = ""; // Overdue, Upcoming, etc.
        public int CompletedServicesCount { get; set; }
        public int TotalServicesCount { get; set; }
        public bool IsSubmitted { get; set; }
        public Dictionary<string, string> ProductStatuses { get; set; } = new Dictionary<string, string>(); // FipsId -> Status
    }

    public class MilestoneReportingViewModel
    {
        public CmsProduct Product { get; set; } = new CmsProduct();
        public string UserEmail { get; set; } = "";
    }

    public class ServiceReportingViewModel
    {
        public int Year { get; set; }
        public string Month { get; set; } = "";
        public string FullMonthName { get; set; } = "";
        public string FipsId { get; set; } = "";
        public CmsProduct Product { get; set; } = new CmsProduct();
        public string UserEmail { get; set; } = "";
        public DateTime DueDate { get; set; }
    }

    public class PerformanceReportingViewModel
    {
        public int Year { get; set; }
        public string Month { get; set; } = "";
        public string FullMonthName { get; set; } = "";
        public string FipsId { get; set; } = "";
        public CmsProduct Product { get; set; } = new CmsProduct();
        public string UserEmail { get; set; } = "";
        public string ReportingPeriod { get; set; } = "";
        public List<FipsReporting.Data.PerformanceMetric> Metrics { get; set; } = new List<FipsReporting.Data.PerformanceMetric>();
        public Dictionary<int, FipsReporting.Data.PerformanceMetricData> ExistingData { get; set; } = new Dictionary<int, FipsReporting.Data.PerformanceMetricData>();
    }

    public class PerformanceDataSubmissionViewModel
    {
        public int Year { get; set; }
        public string Month { get; set; } = "";
        public string FipsId { get; set; } = "";
        public string ReportingPeriod { get; set; } = "";
        public List<MetricDataItem> MetricData { get; set; } = new List<MetricDataItem>();
    }

    public class MetricDataItem
    {
        public int MetricId { get; set; }
        public string? Value { get; set; }
        public string? Comment { get; set; }
        public bool IsNullReturn { get; set; }
    }

    public class PerformanceTaskListViewModel
    {
        public int Year { get; set; }
        public string Month { get; set; } = "";
        public string FullMonthName { get; set; } = "";
        public string FipsId { get; set; } = "";
        public CmsProduct Product { get; set; } = new CmsProduct();
        public string UserEmail { get; set; } = "";
        public string ReportingPeriod { get; set; } = "";
        public List<FipsReporting.Data.PerformanceMetric> Metrics { get; set; } = new List<FipsReporting.Data.PerformanceMetric>();
        public Dictionary<int, FipsReporting.Data.PerformanceMetricData> ExistingData { get; set; } = new Dictionary<int, FipsReporting.Data.PerformanceMetricData>();
        
        // Status tracking properties
        public string PerformanceStatus { get; set; } = "Not started"; // Not started, In progress, Complete
        public string SubmissionStatus { get; set; } = "Cannot submit"; // Cannot submit, Ready to submit, Submitted
        public DateTime DueDate { get; set; }
        public bool IsSubmitted { get; set; }
    }

    public class PerformanceMetricViewModel
    {
        public int Year { get; set; }
        public string Month { get; set; } = "";
        public string FullMonthName { get; set; } = "";
        public string FipsId { get; set; } = "";
        public ProductViewModel Product { get; set; } = new ProductViewModel();
        public string UserEmail { get; set; } = "";
        public string ReportingPeriod { get; set; } = "";
        public FipsReporting.Data.PerformanceMetric Metric { get; set; } = new FipsReporting.Data.PerformanceMetric();
        public FipsReporting.Data.PerformanceMetricData? ExistingData { get; set; }
        public List<PerformanceMetricFormItem> Metrics { get; set; } = new List<PerformanceMetricFormItem>();
        public DateTime DueDate { get; set; }
    }

    public class ServiceSummaryViewModel
    {
        public int Year { get; set; }
        public string Month { get; set; } = "";
        public string FullMonthName { get; set; } = "";
        public string FipsId { get; set; } = "";
        public CmsProduct Product { get; set; } = new CmsProduct();
        public string UserEmail { get; set; } = "";
        public string ReportingPeriod { get; set; } = "";
        
        // RAG Status
        public string RagStatus { get; set; } = "Amber"; // Red, Amber, Green
        public string RagDescription { get; set; } = "No change from last month";
        public List<string> RagHistory { get; set; } = new List<string>(); // Last 4 months
        
        // Progress Status
        public string ProgressStatus { get; set; } = "On track"; // On track, At risk, Off track
        public DateTime NextUpdateDue { get; set; }
        
        // Outcomes
        public List<string> Outcomes { get; set; } = new List<string>();
        
        // Milestones
        public List<ServiceMilestone> Milestones { get; set; } = new List<ServiceMilestone>();
        
        // Performance Metrics Summary
        public int TotalMetrics { get; set; }
        public int CompletedMetrics { get; set; }
        public int OverdueMetrics { get; set; }
    }

    public class ServiceMilestone
    {
        public string Name { get; set; } = "";
        public DateTime DueDate { get; set; }
        public string LastMonthRag { get; set; } = "";
        public string CurrentRag { get; set; } = "";
        public string Change { get; set; } = ""; // Improved, Worsened, Unchanged
        public string Url { get; set; } = "";
    }
}
