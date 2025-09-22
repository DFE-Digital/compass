using System.ComponentModel.DataAnnotations;

namespace FipsReporting.Models
{
    public class AdminPerformanceMetricsViewModel
    {
        public int TotalMetrics { get; set; }
        public int MetricsOnTarget { get; set; }
        public int MetricsAtRisk { get; set; }
        public int MetricsOffTarget { get; set; }
        public int TotalServices { get; set; }
        public List<PerformanceMetricSummary> Metrics { get; set; } = new List<PerformanceMetricSummary>();
        public List<ActivityItem> RecentActivity { get; set; } = new List<ActivityItem>();
    }

    public class PerformanceMetricSummary
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string FipsId { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;
        public string Target { get; set; } = string.Empty;
        public string CurrentValue { get; set; } = string.Empty;
        public decimal ProgressPercentage { get; set; }
        public string RagStatus { get; set; } = string.Empty; // Green, Amber, Red
        public DateTime LastUpdated { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Measure { get; set; } = string.Empty;
        public bool IsMandatory { get; set; }
        public string ValidationCriteria { get; set; } = string.Empty;
        public string Mandate { get; set; } = string.Empty;
        public bool StageD { get; set; }
        public bool StageA { get; set; }
        public bool StageB { get; set; }
        public bool StageL { get; set; }
        public bool StageR { get; set; }
    }

    public class ActivityItem
    {
        public string Type { get; set; } = string.Empty; // Update, Alert, Info
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public string FipsId { get; set; } = string.Empty;
    }

    public class PerformanceMetricsDashboardViewModel
    {
        public MetricsOverview Overview { get; set; } = new MetricsOverview();
        public List<ServiceMetricsSummary> ServiceSummaries { get; set; } = new List<ServiceMetricsSummary>();
        public List<MetricsCategorySummary> CategorySummaries { get; set; } = new List<MetricsCategorySummary>();
        public List<TrendingMetric> TrendingMetrics { get; set; } = new List<TrendingMetric>();
        public List<AlertItem> ActiveAlerts { get; set; } = new List<AlertItem>();
    }

    public class MetricsOverview
    {
        public int TotalMetrics { get; set; }
        public int MetricsOnTarget { get; set; }
        public int MetricsAtRisk { get; set; }
        public int MetricsOffTarget { get; set; }
        public int TotalServices { get; set; }
        public int ServicesWithIssues { get; set; }
        public decimal OverallCompliancePercentage { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public class ServiceMetricsSummary
    {
        public string FipsId { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;
        public int TotalMetrics { get; set; }
        public int MetricsOnTarget { get; set; }
        public int MetricsAtRisk { get; set; }
        public int MetricsOffTarget { get; set; }
        public decimal CompliancePercentage { get; set; }
        public string OverallRagStatus { get; set; } = string.Empty;
        public DateTime LastUpdated { get; set; }
        public List<string> Categories { get; set; } = new List<string>();
    }

    public class MetricsCategorySummary
    {
        public string Category { get; set; } = string.Empty;
        public int TotalMetrics { get; set; }
        public int MetricsOnTarget { get; set; }
        public int MetricsAtRisk { get; set; }
        public int MetricsOffTarget { get; set; }
        public decimal CompletionPercentage { get; set; }
        public string RagStatus { get; set; } = string.Empty;
        public List<string> Services { get; set; } = new List<string>();
    }

    public class TrendingMetric
    {
        public string Name { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;
        public string Trend { get; set; } = string.Empty; // Improving, Declining, Stable
        public decimal ChangePercentage { get; set; }
        public string CurrentValue { get; set; } = string.Empty;
        public string PreviousValue { get; set; } = string.Empty;
        public DateTime LastUpdated { get; set; }
    }

    public class AlertItem
    {
        public string Type { get; set; } = string.Empty; // Critical, Warning, Info
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;
        public string FipsId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsResolved { get; set; }
        public string Priority { get; set; } = string.Empty; // High, Medium, Low
    }

    public class MetricsReportingViewModel
    {
        public DateTime ReportDate { get; set; }
        public string ReportingPeriod { get; set; } = string.Empty;
        public MetricsOverview Summary { get; set; } = new MetricsOverview();
        public List<ServiceMetricsSummary> ServiceBreakdown { get; set; } = new List<ServiceMetricsSummary>();
        public List<MetricsCategorySummary> CategoryBreakdown { get; set; } = new List<MetricsCategorySummary>();
        public List<PerformanceMetricSummary> DetailedMetrics { get; set; } = new List<PerformanceMetricSummary>();
        public List<TrendingMetric> Trends { get; set; } = new List<TrendingMetric>();
        public List<AlertItem> Alerts { get; set; } = new List<AlertItem>();
        public string ReportGeneratedBy { get; set; } = string.Empty;
        public DateTime ReportGeneratedAt { get; set; }
    }

    public class MetricsAnalyticsViewModel
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<MetricsTrendData> TrendData { get; set; } = new List<MetricsTrendData>();
        public List<ServicePerformanceComparison> ServiceComparisons { get; set; } = new List<ServicePerformanceComparison>();
        public List<CategoryPerformanceAnalysis> CategoryAnalysis { get; set; } = new List<CategoryPerformanceAnalysis>();
        public List<ComplianceScoreHistory> ComplianceHistory { get; set; } = new List<ComplianceScoreHistory>();
    }

    public class MetricsTrendData
    {
        public DateTime Date { get; set; }
        public int TotalMetrics { get; set; }
        public int MetricsOnTarget { get; set; }
        public int MetricsAtRisk { get; set; }
        public int MetricsOffTarget { get; set; }
        public decimal OverallCompliancePercentage { get; set; }
    }

    public class ServicePerformanceComparison
    {
        public string ServiceName { get; set; } = string.Empty;
        public string FipsId { get; set; } = string.Empty;
        public decimal CurrentScore { get; set; }
        public decimal PreviousScore { get; set; }
        public decimal ChangePercentage { get; set; }
        public string Trend { get; set; } = string.Empty;
        public int Rank { get; set; }
    }

    public class CategoryPerformanceAnalysis
    {
        public string Category { get; set; } = string.Empty;
        public decimal AverageScore { get; set; }
        public decimal MedianScore { get; set; }
        public decimal BestScore { get; set; }
        public decimal WorstScore { get; set; }
        public int TotalServices { get; set; }
        public int ServicesAboveAverage { get; set; }
        public List<string> TopPerformingServices { get; set; } = new List<string>();
        public List<string> BottomPerformingServices { get; set; } = new List<string>();
    }

    public class ComplianceScoreHistory
    {
        public DateTime Date { get; set; }
        public decimal OverallScore { get; set; }
        public decimal AccessibilityScore { get; set; }
        public decimal PerformanceScore { get; set; }
        public decimal SecurityScore { get; set; }
        public decimal UserSatisfactionScore { get; set; }
    }
}
