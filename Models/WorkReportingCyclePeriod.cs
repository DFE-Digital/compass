using System.ComponentModel.DataAnnotations;

namespace Compass.Models;

/// <summary>
/// One reporting period for work monthly updates. Maps to <c>ReportingCyclePeriods</c>.
/// Aligns with performance commissions: period bounds + submission window + active flag.
/// <see cref="DueDate"/> is kept for backwards compatibility and should match <see cref="SubmissionCloses"/>.
/// </summary>
public class WorkReportingCyclePeriod
{
    public int Id { get; set; }

    public int ReportingCycleId { get; set; }

    [Required]
    [MaxLength(20)]
    public string PeriodKey { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string PeriodLabel { get; set; } = string.Empty;

    /// <summary>Reporting period inclusive start (calendar date).</summary>
    public DateTime PeriodStart { get; set; }

    /// <summary>Reporting period inclusive end (calendar date).</summary>
    public DateTime PeriodEnd { get; set; }

    /// <summary>First calendar day submissions are accepted (same semantics as commission open date).</summary>
    public DateTime SubmissionOpens { get; set; }

    /// <summary>Last calendar day submissions are accepted (same semantics as commission due date).</summary>
    public DateTime SubmissionCloses { get; set; }

    /// <summary>Legacy column: mirrors submission close; retained for existing queries and migrations.</summary>
    public DateTime DueDate { get; set; }

    /// <summary>When false, this period is ignored for submission windows (admins may keep history).</summary>
    public bool IsActive { get; set; } = true;

    public int DisplayOrder { get; set; }

    public WorkReportingCycle ReportingCycle { get; set; } = null!;
}
