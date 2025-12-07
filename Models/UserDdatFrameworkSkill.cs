using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

/// <summary>
/// Represents an additional DDAT Framework Skill selected by a user (outside their profession skill set)
/// </summary>
public class UserDdatFrameworkSkill
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int UserProfessionalProfileId { get; set; }

    [ForeignKey(nameof(UserProfessionalProfileId))]
    public UserProfessionalProfile UserProfessionalProfile { get; set; } = null!;

    /// <summary>
    /// The DDAT Framework Skill ID
    /// </summary>
    [Required]
    public int DdatFrameworkSkillId { get; set; }

    [ForeignKey(nameof(DdatFrameworkSkillId))]
    public DdatFrameworkSkill DdatFrameworkSkill { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

