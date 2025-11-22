using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

/// <summary>
/// Version history for DDT Standards (immutable history)
/// </summary>
public class DdtStandardVersion
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int DdtStandardId { get; set; }

    [ForeignKey(nameof(DdtStandardId))]
    public DdtStandard DdtStandard { get; set; } = null!;

    /// <summary>
    /// Version number (semantic versioning)
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string VersionNumber { get; set; } = string.Empty;

    /// <summary>
    /// Previous version number
    /// </summary>
    [MaxLength(20)]
    public string? PreviousVersion { get; set; }

    /// <summary>
    /// Version type: major, minor, patch
    /// </summary>
    [MaxLength(20)]
    public string? VersionType { get; set; } // major, minor, patch

    /// <summary>
    /// Summary of changes
    /// </summary>
    [MaxLength(1000)]
    public string? ChangeSummary { get; set; }

    /// <summary>
    /// Detailed change log
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? ChangeDetails { get; set; }

    /// <summary>
    /// Whether this version contains breaking changes
    /// </summary>
    public bool IsBreakingChange { get; set; } = false;

    /// <summary>
    /// Full snapshot of standard at this version (JSON)
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? Snapshot { get; set; }

    /// <summary>
    /// User who created this version
    /// </summary>
    public int? CreatedByUserId { get; set; }

    [ForeignKey(nameof(CreatedByUserId))]
    public User? CreatedByUser { get; set; }

    /// <summary>
    /// User who published this version
    /// </summary>
    public int? PublishedByUserId { get; set; }

    [ForeignKey(nameof(PublishedByUserId))]
    public User? PublishedByUser { get; set; }

    /// <summary>
    /// Status of this version
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "draft"; // draft, published, archived

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? PublishedAt { get; set; }
}

