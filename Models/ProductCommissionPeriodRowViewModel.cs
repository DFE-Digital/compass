namespace Compass.Models;

/// <summary>
/// One commission period for a single product (completion state + navigation to submit/view).
/// </summary>
public class ProductCommissionPeriodRowViewModel
{
    public Commission Commission { get; set; } = null!;
    public CommissionSubmission? Submission { get; set; }
    public CommissionSubmissionStatus Status { get; set; }
    public bool IsOpen { get; set; }
    public bool IsPastDue { get; set; }
}
