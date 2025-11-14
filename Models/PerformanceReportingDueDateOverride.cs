using System.ComponentModel.DataAnnotations;

namespace Compass.Models;

/// <summary>
/// Allows admins to override the default "3rd working day" rule for specific reporting periods
/// </summary>
public class PerformanceReportingDueDateOverride
{
    [Key]
    public int Id { get; set; }
    
    /// <summary>
    /// Year of the reporting period (e.g., 2025)
    /// </summary>
    [Required]
    public int ReportingYear { get; set; }
    
    /// <summary>
    /// Month of the reporting period (1-12)
    /// </summary>
    [Required]
    public int ReportingMonth { get; set; }
    
    /// <summary>
    /// The specific date when the report is due (overrides the 3rd working day rule)
    /// </summary>
    [Required]
    public DateTime DueDate { get; set; }
    
    /// <summary>
    /// Optional reason for the override (e.g., "Bank holiday adjustment")
    /// </summary>
    [StringLength(500)]
    public string? Reason { get; set; }
    
    /// <summary>
    /// Whether this override is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    [StringLength(255)]
    public string? CreatedBy { get; set; }
    
    [StringLength(255)]
    public string? UpdatedBy { get; set; }
}

