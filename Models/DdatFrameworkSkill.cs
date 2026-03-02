using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

/// <summary>
/// Represents a DDAT Framework Skill with capability levels and grade mappings
/// </summary>
public class DdatFrameworkSkill
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// The unique name for each skill in the framework
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string SkillName { get; set; } = string.Empty;

    /// <summary>
    /// A short summary of what the skill involves doing
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? SkillDescription { get; set; }

    /// <summary>
    /// Description of what someone with awareness-level proficiency can do
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? AwarenessDescription { get; set; }

    /// <summary>
    /// Description of what someone with working-level proficiency can do
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? WorkingDescription { get; set; }

    /// <summary>
    /// Description of what someone with practitioner-level proficiency can do
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? PractitionerDescription { get; set; }

    /// <summary>
    /// Description of what someone with expert-level proficiency can do
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? ExpertDescription { get; set; }

    /// <summary>
    /// Comma-separated list of roles that require this skill
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? RolesThatRequireSkill { get; set; }

    /// <summary>
    /// The framework version this skill belongs to
    /// </summary>
    [Required]
    public int FrameworkVersionId { get; set; }

    [ForeignKey(nameof(FrameworkVersionId))]
    public DdatFrameworkVersion FrameworkVersion { get; set; } = null!;

    /// <summary>
    /// Whether this skill is archived (removed from current framework)
    /// </summary>
    public bool IsArchived { get; set; } = false;

    /// <summary>
    /// When this skill was archived
    /// </summary>
    public DateTime? ArchivedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Grade mappings for each capability level
    /// </summary>
    public virtual ICollection<DdatFrameworkSkillGradeMapping> GradeMappings { get; set; } = new List<DdatFrameworkSkillGradeMapping>();
}

/// <summary>
/// Maps Civil Service grades to skill capability levels
/// </summary>
public class DdatFrameworkSkillGradeMapping
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int DdatFrameworkSkillId { get; set; }

    [ForeignKey(nameof(DdatFrameworkSkillId))]
    public DdatFrameworkSkill DdatFrameworkSkill { get; set; } = null!;

    /// <summary>
    /// Capability level: Awareness, Working, Practitioner, Expert
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string CapabilityLevel { get; set; } = string.Empty;

    /// <summary>
    /// Civil Service grade (e.g., "AA", "AO", "EO", "HEO", "SEO", "G7", "G6", "G6 HOP", "SCS1", "SCS2", "SCS3")
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Grade { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

