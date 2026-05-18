using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

/// <summary>
/// Stores a proposed risk tier change (escalation or de-escalation) for Operations review.
/// </summary>
public class RaidEscalationTierChangeRequest
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>"risk" (implemented) or "issue" (reserved).</summary>
    [Required]
    [MaxLength(20)]
    public string RecordType { get; set; } = "risk";

    public int? RiskId { get; set; }

    [ForeignKey(nameof(RiskId))]
    public Risk? Risk { get; set; }

    public int? IssueId { get; set; }

    [ForeignKey(nameof(IssueId))]
    public Issue? Issue { get; set; }

    /// <summary>Tier at time of submission (snapshot).</summary>
    public int? FromRiskTierId { get; set; }

    [ForeignKey(nameof(FromRiskTierId))]
    public RiskTier? FromRiskTier { get; set; }

    /// <summary>Requested target tier.</summary>
    public int ToRiskTierId { get; set; }

    [ForeignKey(nameof(ToRiskTierId))]
    public RiskTier ToRiskTier { get; set; } = null!;

    [MaxLength(2000)]
    public string? Rationale { get; set; }

    /// <summary>pending, approved, rejected, superseded, cancelled (withdrawn by requester before Operations decided)</summary>
    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "pending";

    [Required]
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    public int? SubmittedByUserId { get; set; }

    [ForeignKey(nameof(SubmittedByUserId))]
    public User? SubmittedByUser { get; set; }

    public DateTime? DecidedAt { get; set; }

    public int? DecidedByUserId { get; set; }

    [ForeignKey(nameof(DecidedByUserId))]
    public User? DecidedByUser { get; set; }

    [MaxLength(500)]
    public string? DecisionNote { get; set; }
}
