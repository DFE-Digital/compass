using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

/// <summary>
/// Records history of FIPS data sync operations between environments
/// </summary>
public class FipsSyncHistory
{
    [Key]
    public int Id { get; set; }
    
    /// <summary>
    /// Type of sync operation (e.g., "CMDB to Strapi", "Strapi to Strapi")
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string SyncType { get; set; } = string.Empty;
    
    /// <summary>
    /// Source environment (e.g., "Production", "Test", "CMDB")
    /// </summary>
    [MaxLength(50)]
    public string? SourceEnvironment { get; set; }
    
    /// <summary>
    /// Target environment (e.g., "Development", "Test", "Production")
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string TargetEnvironment { get; set; } = string.Empty;
    
    /// <summary>
    /// Status of the sync operation
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Pending";
    
    /// <summary>
    /// When the sync operation started
    /// </summary>
    public DateTime StartedAt { get; set; }
    
    /// <summary>
    /// When the sync operation completed (or failed)
    /// </summary>
    public DateTime? CompletedAt { get; set; }
    
    /// <summary>
    /// Duration of sync in seconds
    /// </summary>
    public int? DurationSeconds { get; set; }
    
    /// <summary>
    /// User who initiated the sync
    /// </summary>
    [MaxLength(255)]
    public string? InitiatedBy { get; set; }
    
    /// <summary>
    /// Number of products created
    /// </summary>
    public int ProductsCreated { get; set; }
    
    /// <summary>
    /// Number of products updated
    /// </summary>
    public int ProductsUpdated { get; set; }
    
    /// <summary>
    /// Number of products skipped
    /// </summary>
    public int ProductsSkipped { get; set; }
    
    /// <summary>
    /// Number of assurances synced
    /// </summary>
    public int AssurancesSynced { get; set; }
    
    /// <summary>
    /// Number of accessibility records synced
    /// </summary>
    public int AccessibilitySynced { get; set; }
    
    /// <summary>
    /// Number of errors encountered
    /// </summary>
    public int ErrorsEncountered { get; set; }
    
    /// <summary>
    /// Detailed log of sync actions (JSON format)
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? ActionsLog { get; set; }
    
    /// <summary>
    /// Error details if sync failed
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? ErrorDetails { get; set; }
    
    /// <summary>
    /// Additional configuration or parameters used for the sync
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? Configuration { get; set; }
}
