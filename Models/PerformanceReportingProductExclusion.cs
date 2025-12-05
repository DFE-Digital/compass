using System.ComponentModel.DataAnnotations;

namespace Compass.Models;

/// <summary>
/// Allows admins to exclude specific products from performance reporting requirements
/// Products on this list will not be required to submit performance metrics
/// </summary>
public class PerformanceReportingProductExclusion
{
    [Key]
    public int Id { get; set; }
    
    /// <summary>
    /// DocumentID of the product to exclude (primary identifier)
    /// </summary>
    [Required]
    [StringLength(100)]
    public string ProductDocumentId { get; set; } = string.Empty;
    
    /// <summary>
    /// FIPS ID of the product to exclude (legacy, kept for backwards compatibility)
    /// </summary>
    [StringLength(50)]
    public string? FipsId { get; set; }
    
    /// <summary>
    /// Optional: Product name (denormalized for easy reference)
    /// </summary>
    [StringLength(255)]
    public string? ProductName { get; set; }
    
    /// <summary>
    /// Reason for exclusion (e.g., "Product in discovery phase", "Being decommissioned")
    /// </summary>
    [Required]
    [StringLength(1000)]
    public string ExclusionReason { get; set; } = string.Empty;
    
    /// <summary>
    /// Year from which the exclusion is effective (e.g., 2025)
    /// </summary>
    [Required]
    public int ExclusionFromYear { get; set; }
    
    /// <summary>
    /// Month from which the exclusion is effective (1-12)
    /// </summary>
    [Required]
    public int ExclusionFromMonth { get; set; }
    
    /// <summary>
    /// Optional: Year until which exclusion is effective (null means until manually removed)
    /// </summary>
    public int? ExclusionUntilYear { get; set; }
    
    /// <summary>
    /// Optional: Month until which exclusion is effective (null means until manually removed)
    /// </summary>
    public int? ExclusionUntilMonth { get; set; }
    
    /// <summary>
    /// Whether this exclusion is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    [StringLength(255)]
    public string? CreatedBy { get; set; }
    
    [StringLength(255)]
    public string? UpdatedBy { get; set; }
}

