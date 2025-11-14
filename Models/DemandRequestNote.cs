using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

/// <summary>
/// Notes added to a demand request
/// </summary>
public class DemandRequestNote
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int DemandRequestId { get; set; }

    [Required]
    [Column(TypeName = "nvarchar(max)")]
    public string NoteText { get; set; } = string.Empty;

    [StringLength(100)]
    public string? CreatedByEmail { get; set; }

    [StringLength(255)]
    public string? CreatedByName { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    [ForeignKey("DemandRequestId")]
    public DemandRequest DemandRequest { get; set; } = null!;
}

/// <summary>
/// Assessment information for a demand request
/// </summary>
public class DemandRequestAssessment
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int DemandRequestId { get; set; }

    [StringLength(50)]
    public string AssessmentType { get; set; } = string.Empty; // ResearchAndEvidence, NeedsAssessment, Recommendations, PrioritisationAssessment, Outcome

    [Column(TypeName = "nvarchar(max)")]
    public string? AssessmentContent { get; set; }

    [StringLength(100)]
    public string? AssessedByEmail { get; set; }

    [StringLength(255)]
    public string? AssessedByName { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    [ForeignKey("DemandRequestId")]
    public DemandRequest DemandRequest { get; set; } = null!;
}

/// <summary>
/// Tracks completion status of sections in a demand request
/// </summary>
public class DemandRequestSectionCompletion
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int DemandRequestId { get; set; }

    [Required]
    [StringLength(50)]
    public string SectionName { get; set; } = string.Empty; // Overview, StrategicAlignment, ImpactAndRisk, FundingAndHeadcount, Delivery, ResearchAndEvidence, NeedsAssessment, Recommendations, PrioritisationAssessment, Outcome

    [Required]
    [StringLength(20)]
    public string CompletionStatus { get; set; } = "ToDo"; // ToDo, InProgress, Completed

    [StringLength(100)]
    public string? CompletedByEmail { get; set; }

    [StringLength(255)]
    public string? CompletedByName { get; set; }

    public DateTime? CompletedAt { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? CompletionNotes { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? LatestErrorMessage { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    [ForeignKey("DemandRequestId")]
    public DemandRequest DemandRequest { get; set; } = null!;
}

