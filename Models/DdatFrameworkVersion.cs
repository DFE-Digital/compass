using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

/// <summary>
/// Represents a version of the DDAT Capability Framework
/// Tracks when the framework was imported and provides historical context
/// </summary>
public class DdatFrameworkVersion
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Version identifier (e.g., "2025-11-30", "2025-12-05")
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string VersionIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable version name (e.g., "November 2025", "December 2025")
    /// </summary>
    [MaxLength(200)]
    public string? VersionName { get; set; }

    /// <summary>
    /// URL where the Skills CSV was downloaded from
    /// </summary>
    [MaxLength(1000)]
    public string? SkillsCsvUrl { get; set; }

    /// <summary>
    /// URL where the Roles CSV was downloaded from
    /// </summary>
    [MaxLength(1000)]
    public string? RolesCsvUrl { get; set; }

    /// <summary>
    /// Local file path where Skills CSV was stored
    /// </summary>
    [MaxLength(500)]
    public string? SkillsCsvPath { get; set; }

    /// <summary>
    /// Local file path where Roles CSV was stored
    /// </summary>
    [MaxLength(500)]
    public string? RolesCsvPath { get; set; }

    /// <summary>
    /// Whether this is the current active version
    /// </summary>
    public bool IsActive { get; set; } = false;

    /// <summary>
    /// When this version was imported
    /// </summary>
    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User who imported this version
    /// </summary>
    [MaxLength(255)]
    public string? ImportedBy { get; set; }

    /// <summary>
    /// Notes about this version (e.g., "Initial import", "Updated with new roles")
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? Notes { get; set; }

    /// <summary>
    /// Number of skills in this version
    /// </summary>
    public int SkillsCount { get; set; }

    /// <summary>
    /// Number of roles in this version
    /// </summary>
    public int RolesCount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Skills in this framework version
    /// </summary>
    public virtual ICollection<DdatFrameworkSkill> Skills { get; set; } = new List<DdatFrameworkSkill>();

    /// <summary>
    /// Roles in this framework version
    /// </summary>
    public virtual ICollection<DdatFrameworkRole> Roles { get; set; } = new List<DdatFrameworkRole>();

    /// <summary>
    /// Change notes for this version
    /// </summary>
    public virtual ICollection<DdatFrameworkChangeNote> ChangeNotes { get; set; } = new List<DdatFrameworkChangeNote>();
}

/// <summary>
/// Represents a change note for framework updates
/// </summary>
public class DdatFrameworkChangeNote
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int FrameworkVersionId { get; set; }

    [ForeignKey(nameof(FrameworkVersionId))]
    public DdatFrameworkVersion FrameworkVersion { get; set; } = null!;

    /// <summary>
    /// The page/entity the change note relates to (e.g., role name, skill name)
    /// </summary>
    [MaxLength(500)]
    public string? Page { get; set; }

    /// <summary>
    /// The content of the note explaining what has been updated
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? ChangeNote { get; set; }

    /// <summary>
    /// Timestamp of the change
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

