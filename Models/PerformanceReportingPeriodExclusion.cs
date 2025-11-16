using System.ComponentModel.DataAnnotations;

namespace Compass.Models;

/// <summary>
/// Allows admins to exclude entire reporting periods (months) from performance reporting requirements.
/// Business area configurations can override these base-level exclusions.
/// </summary>
public class PerformanceReportingPeriodExclusion
{
    [Key]
    public int Id { get; set; }
    
    /// <summary>
    /// Year of the reporting period to exclude (e.g., 2025)
    /// </summary>
    [Required]
    public int Year { get; set; }
    
    /// <summary>
    /// Month of the reporting period to exclude (1-12)
    /// </summary>
    [Required]
    public int Month { get; set; }
    
    /// <summary>
    /// Reason for excluding this period (e.g., "System migration", "Holiday period")
    /// </summary>
    [Required]
    [StringLength(1000)]
    public string Reason { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether this exclusion is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Optional notes
    /// </summary>
    [StringLength(2000)]
    public string? Notes { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    [StringLength(255)]
    public string? CreatedBy { get; set; }
    
    [StringLength(255)]
    public string? UpdatedBy { get; set; }
}

