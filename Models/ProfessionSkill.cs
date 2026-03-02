using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

/// <summary>
/// Join table for many-to-many relationship between DDaT Professions and Skills
/// </summary>
public class ProfessionSkill
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// The profession ID
    /// </summary>
    [Required]
    public int DdatProfessionId { get; set; }

    [ForeignKey(nameof(DdatProfessionId))]
    public DdatProfession DdatProfession { get; set; } = null!;

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

