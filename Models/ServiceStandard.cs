using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

/// <summary>
/// Represents a GOV.UK Service Standard (1-14)
/// </summary>
public class ServiceStandard
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// The standard number (1-14)
    /// </summary>
    [Required]
    public int StandardNumber { get; set; }

    /// <summary>
    /// The title of the standard
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
    /// Brief summary of the standard
    /// </summary>
    [MaxLength(450)]
    public string? Summary { get; set; }

    /// <summary>
    /// Full description/content of the standard (markdown supported)
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? Description { get; set; }

    /// <summary>
    /// Whether this standard is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Display order for sorting
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// User who created this standard
    /// </summary>
    public int? CreatedByUserId { get; set; }

    [ForeignKey(nameof(CreatedByUserId))]
    public User? CreatedByUser { get; set; }

    /// <summary>
    /// User who last updated this standard
    /// </summary>
    public int? UpdatedByUserId { get; set; }

    [ForeignKey(nameof(UpdatedByUserId))]
    public User? UpdatedByUser { get; set; }

    /// <summary>
    /// When this standard was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this standard was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Phase guidance for this standard
    /// </summary>
    public virtual ICollection<ServiceStandardPhaseGuidance> PhaseGuidance { get; set; } = new List<ServiceStandardPhaseGuidance>();

    /// <summary>
    /// DDaT professions that this standard applies to
    /// </summary>
    public virtual ICollection<ServiceStandardProfession> ServiceStandardProfessions { get; set; } = new List<ServiceStandardProfession>();
}

