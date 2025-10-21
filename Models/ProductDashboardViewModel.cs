namespace Compass.Models;

public class ProductDashboardViewModel
{
    // Product Information
    public string FipsId { get; set; } = string.Empty;
    public string ProductTitle { get; set; } = string.Empty;
    
    // Risk Summary
    public int TotalRisks { get; set; }
    public int OpenRisks { get; set; }
    public int HighRisks { get; set; }
    public int MediumRisks { get; set; }
    public double AverageRiskScore { get; set; }
    public List<Risk> TopRisks { get; set; } = new();
    
    // Issue Summary
    public int TotalIssues { get; set; }
    public int OpenIssues { get; set; }
    public int CriticalIssues { get; set; }
    public int HighIssues { get; set; }
    public int BlockedIssues { get; set; }
    public DateTime? OldestOpenIssue { get; set; }
    public List<Issue> TopIssues { get; set; } = new();
    
    // Action Summary
    public int TotalActions { get; set; }
    public int OverdueActions { get; set; }
    public int InProgressActions { get; set; }
    public int CompletedActions { get; set; }
    public List<Action> UpcomingActions { get; set; } = new();
    
    // Milestone Summary
    public int TotalMilestones { get; set; }
    public int DelayedMilestones { get; set; }
    public int CompletedMilestones { get; set; }
    public int UpcomingMilestones { get; set; }
    public List<Milestone> KeyMilestones { get; set; } = new();
    
    // Performance Metrics
    public decimal? UserSatisfaction { get; set; }
    public DateTime? LastReportDate { get; set; }
    public bool HasMetricsData { get; set; }
    public List<ProductMetricSummary> RecentMetrics { get; set; } = new();
    
    // Health Indicators
    public int ProblemScore { get; set; }
    public bool NeedsAttention { get; set; }
    public string HealthStatus { get; set; } = "Unknown";
}

public class ProductMetricSummary
{
    public string MetricName { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public string Rag { get; set; } = string.Empty;
}

