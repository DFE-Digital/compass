using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

/// <summary>
/// Guidance for applying a Service Standard at a specific Agile phase
/// </summary>
public class ServiceStandardPhaseGuidance
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// The Service Standard this guidance applies to
    /// </summary>
    [Required]
    public int ServiceStandardId { get; set; }

    [ForeignKey(nameof(ServiceStandardId))]
    public virtual ServiceStandard ServiceStandard { get; set; } = null!;

    /// <summary>
    /// The Agile phase: Discovery, Alpha, Beta, or Live
    /// </summary>
    [Required]
    [MaxLength(450)]
    public string Phase { get; set; } = string.Empty;

    /// <summary>
    /// Main guidance content for this phase (markdown supported)
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? Guidance { get; set; }

    /// <summary>
    /// Key activities to consider in this phase (markdown supported)
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? KeyActivities { get; set; }

    /// <summary>
    /// Questions to consider in this phase (markdown supported)
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? QuestionsToConsider { get; set; }

    /// <summary>
    /// User who created this guidance
    /// </summary>
    public int? CreatedByUserId { get; set; }

    [ForeignKey(nameof(CreatedByUserId))]
    public User? CreatedByUser { get; set; }

    /// <summary>
    /// User who last updated this guidance
    /// </summary>
    public int? UpdatedByUserId { get; set; }

    [ForeignKey(nameof(UpdatedByUserId))]
    public User? UpdatedByUser { get; set; }

    /// <summary>
    /// When this guidance was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this guidance was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

