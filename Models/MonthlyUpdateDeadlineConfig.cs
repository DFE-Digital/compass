using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

/// <summary>
/// Configuration for monthly update deadlines - configurable like operational reporting
/// Allows setting the working day deadline (default: 5th working day)
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
    /// Which working day of the following month the update is due (default: 5)
    /// </summary>
    [Required]
    [Range(1, 20)]
    public int WorkingDayDeadline { get; set; } = 5;

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
}

