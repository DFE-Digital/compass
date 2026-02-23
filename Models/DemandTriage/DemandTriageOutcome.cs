using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models.DemandTriage;

/// <summary>
/// Triage outcome recorded by Central Operations Admin.
/// One-to-one with DemandTriageRequest.
/// </summary>
public class DemandTriageOutcome
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int DemandTriageRequestId { get; set; }

    [ForeignKey(nameof(DemandTriageRequestId))]
    public DemandTriageRequest DemandTriageRequest { get; set; } = null!;

    /// <summary>One of: Progress to next stage | Reject | Pause | Further investigation required</summary>
    [Required]
    [StringLength(100)]
    public string OutcomeSelection { get; set; } = string.Empty;

    /// <summary>Summarise outcome / recommendation (free text)</summary>
    public string? OutcomeSummary { get; set; }

    /// <summary>Which area is the demand going to?</summary>
    [StringLength(255)]
    public string? RoutedToArea { get; set; }

    /// <summary>True when the chosen outcome conflicts with the suggestion band</summary>
    public bool OverrodeRecommendation { get; set; }

    public string? OverrideReason { get; set; }

    public DateTime? DecidedAt { get; set; }

    [StringLength(255)]
    public string? DecidedBy { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [StringLength(255)]
    public string? CreatedBy { get; set; }

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [StringLength(255)]
    public string? UpdatedBy { get; set; }
}

public static class TriageOutcomeValues
{
    public const string ProgressToNextStage = "Progress to next stage";
    public const string Reject = "Reject";
    public const string Pause = "Pause";
    public const string FurtherInvestigationRequired = "Further investigation required";

    public static readonly IReadOnlyList<string> All = new[]
    {
        ProgressToNextStage, Reject, Pause, FurtherInvestigationRequired
    };
}
