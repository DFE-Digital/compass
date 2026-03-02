namespace Compass.Models;

public class ProductReturnStatusViewModel
{
    public ProductDto Product { get; set; } = new();
    public int CurrentPeriodYear { get; set; }
    public int CurrentPeriodMonth { get; set; }
    public ReturnStatus? Status { get; set; }
    public int? CompletedMetrics { get; set; }
    public int? TotalMetrics { get; set; }
    public string? UserRole { get; set; }
    public bool IsReportingRequired { get; set; }
    public string? ReportingSuspensionReason { get; set; }
    public DateTime? NextReportingPeriod { get; set; }
    public DateTime? NextReportingPeriodDueDate { get; set; }
    public bool IsBusinessAreaInScope { get; set; }
    public DateTime? CurrentPeriodDueDate { get; set; }
    public bool IsPeriodExcluded { get; set; }
    public bool HasBusinessAreaOverride { get; set; }
}

