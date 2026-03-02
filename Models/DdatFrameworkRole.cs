using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

/// <summary>
/// Represents a DDAT Framework Role with role levels and skill requirements
/// </summary>
public class DdatFrameworkRole
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// The wider group that each role is part of (e.g., "Architecture", "Data roles")
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string RoleFamily { get; set; } = string.Empty;

    /// <summary>
    /// The name of the digital and data role (e.g., "Business architect", "Data analyst")
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Short descriptions and typical responsibilities for each role in general
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? RoleDescription { get; set; }

    /// <summary>
    /// The names of role levels that each role has (e.g., "Associate business architect", "Business architect")
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string RoleLevel { get; set; } = string.Empty;

    /// <summary>
    /// Short descriptions and typical responsibilities for each role level
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? RoleLevelDescription { get; set; }

    /// <summary>
    /// Whether the role uses the skills for Senior Civil Service roles
    /// </summary>
    [MaxLength(50)]
    public string? RoleType { get; set; }

    /// <summary>
    /// The framework version this role belongs to
    /// </summary>
    [Required]
    public int FrameworkVersionId { get; set; }

    [ForeignKey(nameof(FrameworkVersionId))]
    public DdatFrameworkVersion FrameworkVersion { get; set; } = null!;

    /// <summary>
    /// Whether this role is archived (removed from current framework)
    /// </summary>
    public bool IsArchived { get; set; } = false;

    /// <summary>
    /// When this role was archived
    /// </summary>
    public DateTime? ArchivedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Skills required by this role level
    /// </summary>
    public virtual ICollection<DdatFrameworkRoleSkill> RoleSkills { get; set; } = new List<DdatFrameworkRoleSkill>();
}

/// <summary>
/// Represents a skill requirement for a specific role level
/// </summary>
public class DdatFrameworkRoleSkill
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int DdatFrameworkRoleId { get; set; }

    [ForeignKey(nameof(DdatFrameworkRoleId))]
    public DdatFrameworkRole DdatFrameworkRole { get; set; } = null!;

    /// <summary>
    /// The name of the skill required
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string SkillName { get; set; } = string.Empty;

    /// <summary>
    /// Short description of what the skill involves
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? SkillDescription { get; set; }

    /// <summary>
    /// Required skill level: 'awareness', 'working', 'practitioner' or 'expert'
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string SkillLevel { get; set; } = string.Empty;

    /// <summary>
    /// What you are able to do with the skills if you have that level of proficiency
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? SkillLevelDescription { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

