using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models.DemandTriage;

/// <summary>
/// Exploratory review record for a demand triage request.
/// One-to-one with DemandTriageRequest.
/// </summary>
public class DemandExploratoryReview
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int DemandTriageRequestId { get; set; }

    [ForeignKey(nameof(DemandTriageRequestId))]
    public DemandTriageRequest DemandTriageRequest { get; set; } = null!;

    public string? SummaryFindings { get; set; }

    public string? KeyRisks { get; set; }

    public string? Dependencies { get; set; }

    /// <summary>True = proceed, False = do not proceed, null = not yet answered</summary>
    public bool? RecommendationToProceed { get; set; }

    public string? ReasonNotProceeding { get; set; }

    public DateTime? CompletedAt { get; set; }

    [StringLength(255)]
    public string? CompletedBy { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [StringLength(255)]
    public string? CreatedBy { get; set; }

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [StringLength(255)]
    public string? UpdatedBy { get; set; }
}
