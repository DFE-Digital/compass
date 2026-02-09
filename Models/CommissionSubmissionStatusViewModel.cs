namespace Compass.Models;

public class CommissionSubmissionStatusViewModel
{
    public ProductDto Product { get; set; } = new();
    public Commission Commission { get; set; } = new();
    public CommissionSubmission? Submission { get; set; }
    public CommissionSubmissionStatus Status { get; set; }
    public int CompletedMetrics { get; set; }
    public int TotalMetrics { get; set; }
    public bool IsOpen { get; set; }
    public bool IsPastDue { get; set; }
}
