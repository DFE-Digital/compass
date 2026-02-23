using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models.DemandTriage;

/// <summary>
/// Scorecard for a demand triage request.
/// Stores section scores, bands, and the overall suggestion band.
/// One-to-one with DemandTriageRequest.
/// </summary>
public class DemandScorecard
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int DemandTriageRequestId { get; set; }

    [ForeignKey(nameof(DemandTriageRequestId))]
    public DemandTriageRequest DemandTriageRequest { get; set; } = null!;

    // ── Status ───────────────────────────────────────────────────────────────
    [Required]
    [StringLength(30)]
    public string ScorecardStatus { get; set; } = ScorecardStatusValues.NotStarted;

    public bool IsLocked { get; set; } = false;

    // ── Section scores ───────────────────────────────────────────────────────
    public int StrategicAlignmentScore { get; set; }
    public int UrgencyScore { get; set; }
    public int FundingScore { get; set; }
    public int RiceScore { get; set; }
    public int TotalScore { get; set; }

    // ── Bands ────────────────────────────────────────────────────────────────
    [StringLength(60)]
    public string? StrategicAlignmentBand { get; set; }

    [StringLength(60)]
    public string? UrgencyBand { get; set; }

    [StringLength(60)]
    public string? FundingBand { get; set; }

    [StringLength(60)]
    public string? RiceBand { get; set; }

    [StringLength(60)]
    public string? SuggestionBand { get; set; }

    // ── Finalisation ─────────────────────────────────────────────────────────
    public DateTime? FinalisedAt { get; set; }

    [StringLength(255)]
    public string? FinalisedBy { get; set; }

    // ── Audit ────────────────────────────────────────────────────────────────
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [StringLength(255)]
    public string? CreatedBy { get; set; }

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [StringLength(255)]
    public string? UpdatedBy { get; set; }

    // ── Answers ──────────────────────────────────────────────────────────────
    public ICollection<DemandAnswer> Answers { get; set; } = new List<DemandAnswer>();
}

public static class ScorecardStatusValues
{
    public const string NotStarted = "not_started";
    public const string InProgress = "in_progress";
    public const string Finalised = "finalised";
}

/// <summary>
/// Individual answer to a scoring question.
/// Multi-select questions produce multiple rows with the same QuestionCode.
/// </summary>
public class DemandAnswer
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int DemandScorecardId { get; set; }

    [ForeignKey(nameof(DemandScorecardId))]
    public DemandScorecard Scorecard { get; set; } = null!;

    /// <summary>e.g. "1.2", "4.2"</summary>
    [Required]
    [StringLength(10)]
    public string QuestionCode { get; set; } = string.Empty;

    /// <summary>Exact label of the selected answer option (for auditability)</summary>
    [StringLength(500)]
    public string? AnswerValue { get; set; }

    /// <summary>Score awarded for this answer (null for free-text / informational)</summary>
    public int? AnswerScore { get; set; }

    /// <summary>Free-text response (for textarea questions)</summary>
    public string? FreeText { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [StringLength(255)]
    public string? CreatedBy { get; set; }

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [StringLength(255)]
    public string? UpdatedBy { get; set; }
}
