using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

/// <summary>
/// Represents a user's professional profile including profession, skills, and capability gaps
/// </summary>
public class UserProfessionalProfile
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    /// <summary>
    /// The primary profession ID (DDaT profession)
    /// </summary>
    public int? DdatProfessionId { get; set; }

    [ForeignKey(nameof(DdatProfessionId))]
    public DdatProfession? DdatProfession { get; set; }

    /// <summary>
    /// The DDAT Framework Role ID (from DdatFrameworkRole)
    /// </summary>
    public int? DdatFrameworkRoleId { get; set; }

    [ForeignKey(nameof(DdatFrameworkRoleId))]
    public DdatFrameworkRole? DdatFrameworkRole { get; set; }

    /// <summary>
    /// User's substantive Civil Service grade (e.g., "AA", "AO", "EO", "HEO", "SEO", "G7", "G6", "G6 HOP", "SCS1", "SCS2", "SCS3")
    /// </summary>
    [MaxLength(20)]
    public string? SubstantiveGrade { get; set; }

    /// <summary>
    /// Legacy profession field (kept for backward compatibility during migration)
    /// </summary>
    [StringLength(100)]
    public string? Profession { get; set; } // DDaT or equivalent

    [Column(TypeName = "nvarchar(max)")]
    public string? CapabilityGapsLegacy { get; set; } // Legacy field - kept for backward compatibility

    /// <summary>
    /// Head of Profession user ID (email or object ID)
    /// </summary>
    [StringLength(255)]
    public string? HeadOfProfessionId { get; set; } // Governance mapping to Entra Users (email or object ID)

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("UserId")]
    public User? User { get; set; }

    /// <summary>
    /// User's selected skills (many-to-many relationship)
    /// </summary>
    public virtual ICollection<UserProfessionalProfileSkill> UserSkills { get; set; } = new List<UserProfessionalProfileSkill>();

    /// <summary>
    /// User's identified capability gaps
    /// </summary>
    public virtual ICollection<CapabilityGap> CapabilityGaps { get; set; } = new List<CapabilityGap>();

    /// <summary>
    /// Additional DDAT Framework Skills (outside profession skill set)
    /// </summary>
    public virtual ICollection<UserDdatFrameworkSkill> AdditionalDdatFrameworkSkills { get; set; } = new List<UserDdatFrameworkSkill>();
}

