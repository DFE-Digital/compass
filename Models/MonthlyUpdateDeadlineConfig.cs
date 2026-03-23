using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

/// <summary>
/// Configuration for monthly project update due dates: calendar day of month in the due month,
/// and when submission opens (commission days before end of reporting month).
/// </summary>
public class MonthlyUpdateDeadlineConfig
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Name/description of this configuration (e.g., "Default", "2025 Configuration")
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = "Default";

    /// <summary>
    /// Which month the due date falls in relative to the reporting period.
    /// </summary>
    [Required]
    public MonthlyUpdateDueMonthOffset DueMonthOffset { get; set; } = MonthlyUpdateDueMonthOffset.FollowingMonth;

    /// <summary>
    /// Calendar day of that month (1–31). If the month has fewer days (e.g. February), the due date is the last day of the month.
    /// </summary>
    [Required]
    [Range(1, 31)]
    public int DueCalendarDay { get; set; } = 5;

    /// <summary>
    /// Legacy column retained for existing databases; mirrors <see cref="DueCalendarDay"/> on save. Not shown in admin UI.
    /// </summary>
    [Required]
    public int WorkingDayDeadline { get; set; } = 5;

    /// <summary>
    /// Legacy column retained for existing databases; unused by current logic. Not shown in admin UI.
    /// </summary>
    [Required]
    public int DueDayRule { get; set; } = 0;

    /// <summary>
    /// Number of days before the end of the month when updates become available for submission (default: 6)
    /// </summary>
    [Required]
    [Range(1, 30)]
    public int CommissionDaysBeforeMonthEnd { get; set; } = 6;

    /// <summary>
    /// Start date for when this configuration applies
    /// </summary>
    [Required]
    public DateTime EffectiveFrom { get; set; } = new DateTime(2025, 11, 1);

    /// <summary>
    /// End date for when this configuration applies (null = indefinite)
    /// </summary>
    public DateTime? EffectiveUntil { get; set; }

    /// <summary>
    /// Whether this configuration is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Whether this is the default configuration
    /// </summary>
    public bool IsDefault { get; set; } = false;

    /// <summary>
    /// Optional notes about this configuration
    /// </summary>
    [MaxLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(255)]
    public string? CreatedBy { get; set; }

    [MaxLength(255)]
    public string? UpdatedBy { get; set; }

    /// <summary>Short label for admin lists (matches runtime due-date logic).</summary>
    public string SummarizeDueRule()
    {
        var monthPart = DueMonthOffset == MonthlyUpdateDueMonthOffset.ReportingMonth
            ? "same month as period"
            : "month after period";
        return $"Day {DueCalendarDay} ({monthPart})";
    }
}
