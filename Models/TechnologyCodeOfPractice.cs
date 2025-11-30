using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

/// <summary>
/// Represents a Technology Code of Practice point (1-13)
/// </summary>
public class TechnologyCodeOfPractice
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// The point number (1-13)
    /// </summary>
    [Required]
    public int PointNumber { get; set; }

    /// <summary>
    /// The title of the point
    /// </summary>
    [Required]
    [MaxLength(450)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// URL-friendly slug
    /// </summary>
    [Required]
    [MaxLength(450)]
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// Brief summary of the point
    /// </summary>
    [MaxLength(1000)]
    public string? Summary { get; set; }

    /// <summary>
    /// Full description/guidance for this point (markdown supported)
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? Description { get; set; }

    /// <summary>
    /// Link to external guidance (e.g., GOV.UK page)
    /// </summary>
    [MaxLength(500)]
    public string? GuidanceUrl { get; set; }

    /// <summary>
    /// Whether this point is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Display order for sorting
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// User who created this point
    /// </summary>
    public int? CreatedByUserId { get; set; }

    [ForeignKey(nameof(CreatedByUserId))]
    public User? CreatedByUser { get; set; }

    /// <summary>
    /// User who last updated this point
    /// </summary>
    public int? UpdatedByUserId { get; set; }

    [ForeignKey(nameof(UpdatedByUserId))]
    public User? UpdatedByUser { get; set; }

    /// <summary>
    /// When this point was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this point was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// DDaT professions that this point applies to
    /// </summary>
    public virtual ICollection<TechnologyCodeOfPracticeProfession> TechnologyCodeOfPracticeProfessions { get; set; } = new List<TechnologyCodeOfPracticeProfession>();

    /// <summary>
    /// Phase-specific guidance for this point
    /// </summary>
    public virtual ICollection<TechnologyCodeOfPracticePhaseGuidance> PhaseGuidance { get; set; } = new List<TechnologyCodeOfPracticePhaseGuidance>();
}

/// <summary>
/// Join table for many-to-many relationship between Technology Code of Practice points and DDaT Professions
/// </summary>
public class TechnologyCodeOfPracticeProfession
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// The TCoP point ID
    /// </summary>
    [Required]
    public int TechnologyCodeOfPracticeId { get; set; }

    [ForeignKey(nameof(TechnologyCodeOfPracticeId))]
    public TechnologyCodeOfPractice TechnologyCodeOfPractice { get; set; } = null!;

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

