using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

/// <summary>
/// Represents a DDaT (Digital, Data and Technology) profession
/// </summary>
public class DdatProfession
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// The name of the profession (e.g., "User Research", "Content Design")
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// URL-friendly slug
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// Brief description of the profession
    /// </summary>
    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// The role group this profession belongs to (e.g., "Architecture roles", "Data roles")
    /// </summary>
    [MaxLength(200)]
    public string? RoleGroup { get; set; }

    /// <summary>
    /// Display order for sorting
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Whether this profession is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// When this profession was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this profession was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Service standards that apply to this profession
    /// </summary>
    public virtual ICollection<ServiceStandardProfession> ServiceStandardProfessions { get; set; } = new List<ServiceStandardProfession>();

    /// <summary>
    /// Skills associated with this profession
    /// </summary>
    public virtual ICollection<ProfessionSkill> ProfessionSkills { get; set; } = new List<ProfessionSkill>();
}

/// <summary>
/// Join table for many-to-many relationship between Service Standards and DDaT Professions
/// </summary>
public class ServiceStandardProfession
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// The service standard ID
    /// </summary>
    [Required]
    public int ServiceStandardId { get; set; }

    [ForeignKey(nameof(ServiceStandardId))]
    public ServiceStandard ServiceStandard { get; set; } = null!;

    /// <summary>
    /// The profession ID
    /// </summary>
    [Required]
    public int DdatProfessionId { get; set; }

    [ForeignKey(nameof(DdatProfessionId))]
    public DdatProfession DdatProfession { get; set; } = null!;

    /// <summary>
    /// When this relationship was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

