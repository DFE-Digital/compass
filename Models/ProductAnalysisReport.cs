namespace Compass.Models;

public class ProductAnalysisReport
{
    public string FipsId { get; set; } = string.Empty;
    public string ProductTitle { get; set; } = string.Empty;
    public decimal? UserSatisfaction { get; set; }
    public int TotalRisks { get; set; }
    public int HighRisks { get; set; }
    public int TotalIssues { get; set; }
    public int CriticalIssues { get; set; }
    public int BlockedIssues { get; set; }
    public int OverdueActions { get; set; }
    public int DelayedMilestones { get; set; }
    public double AverageRiskScore { get; set; }
    public DateTime? OldestOpenIssue { get; set; }
    public int ProblemScore { get; set; }
    public bool NeedsAttention { get; set; }
    public bool HasMetricsData { get; set; }
    public DateTime? LastReportDate { get; set; }
}

