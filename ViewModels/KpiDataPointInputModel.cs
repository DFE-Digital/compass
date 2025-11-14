using System.ComponentModel.DataAnnotations;

namespace Compass.ViewModels;

public class KpiDataPointInputModel
{
    [Required]
    public int ProjectId { get; set; }

    [Required]
    public int KpiId { get; set; }

    [Required]
    public DateTime ReportingPeriodStart { get; set; }

    public DateTime? ReportingPeriodEnd { get; set; }

    public decimal? Value { get; set; }

    public string? ValueNarrative { get; set; }

    public string? Notes { get; set; }

    public bool IsValidated { get; set; }

    public string? SubmittedByEmail { get; set; }
}
