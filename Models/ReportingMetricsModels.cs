using FipsReporting.Data;

namespace FipsReporting.Models
{
    public class MetricReportingViewModel
    {
        public ReportingMetric Metric { get; set; } = new ReportingMetric();
        public int MetricId { get; set; }
        public string? ProductId { get; set; }
        public string Value { get; set; } = string.Empty;
        public string ReportingPeriod { get; set; } = string.Empty;
        public string? Comment { get; set; }
        public bool IsNotApplicable { get; set; }
    }
}
