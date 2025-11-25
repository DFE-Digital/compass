using System.ComponentModel.DataAnnotations;

namespace Compass.Models;

/// <summary>
/// Configurable timescales for determining upcoming, due, and late status for milestones and deliverables
/// Similar to operational reporting configuration
/// </summary>
public class MonthlyStatusReportTimescaleConfig
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Name/description of this configuration (e.g., "Default", "Q1 2025")
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = "Default";

    /// <summary>
    /// Number of days before due date to consider as "upcoming"
    /// </summary>
    [Required]
    [Range(0, 365)]
    public int UpcomingDays { get; set; } = 30;

    /// <summary>
    /// Number of days after due date to still consider as "due" (before marking as late)
    /// </summary>
    [Required]
    [Range(0, 365)]
    public int DueGracePeriodDays { get; set; } = 7;

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

