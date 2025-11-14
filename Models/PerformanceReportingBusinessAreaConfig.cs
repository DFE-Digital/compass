using System.ComponentModel.DataAnnotations;

namespace Compass.Models;

/// <summary>
/// Defines which business areas/portfolios are subject to performance reporting requirements
/// and from which date they are applicable
/// </summary>
public class PerformanceReportingBusinessAreaConfig
{
    [Key]
    public int Id { get; set; }
    
    /// <summary>
    /// Name of the business area/portfolio (e.g., "Infrastructure", "Operations")
    /// </summary>
    [Required]
    [StringLength(255)]
    public string BusinessAreaName { get; set; } = string.Empty;
    
    /// <summary>
    /// Year from which performance reporting is applicable (e.g., 2025)
    /// </summary>
    [Required]
    public int ApplicableFromYear { get; set; }
    
    /// <summary>
    /// Month from which performance reporting is applicable (1-12)
    /// </summary>
    [Required]
    public int ApplicableFromMonth { get; set; }
    
    /// <summary>
    /// Optional: Year until which reporting is applicable (null means indefinitely)
    /// </summary>
    public int? ApplicableUntilYear { get; set; }
    
    /// <summary>
    /// Optional: Month until which reporting is applicable (null means indefinitely)
    /// </summary>
    public int? ApplicableUntilMonth { get; set; }
    
    /// <summary>
    /// Whether this configuration is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Optional notes about why this business area is subject to reporting
    /// </summary>
    [StringLength(1000)]
    public string? Notes { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    [StringLength(255)]
    public string? CreatedBy { get; set; }
    
    [StringLength(255)]
    public string? UpdatedBy { get; set; }
}

