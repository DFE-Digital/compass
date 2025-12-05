using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

/// <summary>
/// Join table for many-to-many relationship between User Professional Profiles and Skills
/// </summary>
public class UserProfessionalProfileSkill
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// The user professional profile ID
    /// </summary>
    [Required]
    public int UserProfessionalProfileId { get; set; }

    [ForeignKey(nameof(UserProfessionalProfileId))]
    public UserProfessionalProfile UserProfessionalProfile { get; set; } = null!;

    /// <summary>
    /// The skill ID
    /// </summary>
    [Required]
    public int SkillId { get; set; }

    [ForeignKey(nameof(SkillId))]
    public Skill Skill { get; set; } = null!;

    /// <summary>
    /// When this relationship was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

