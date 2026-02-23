using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models.DemandTriage;

/// <summary>
/// Demand triage request — spec-aligned model (v3 specification).
/// Status state machine: draft → submitted → exploratory_review_in_progress →
/// exploratory_review_complete → scoring_in_progress → scored_finalised →
/// triage_pending → triaged → closed | returned_for_clarification
/// </summary>
public class DemandTriageRequest
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>Immutable human-readable reference, e.g. DR-000001</summary>
    [Required]
    [StringLength(20)]
    public string RequestReference { get; set; } = string.Empty;

    // ── Status ──────────────────────────────────────────────────────────────
    [Required]
    [StringLength(60)]
    public string Status { get; set; } = DemandTriageStatus.Draft;

    // ── Capture form fields (Section 5 of spec) ─────────────────────────────
    [StringLength(255)]
    public string? RequestName { get; set; }

    [StringLength(255)]
    public string? RequesterFullName { get; set; }

    [StringLength(255)]
    public string? DepartmentGroup { get; set; }

    [StringLength(255)]
    public string? DdtPortfolioSupport { get; set; }

    public string? PointsOfContact { get; set; }

    [StringLength(255)]
    public string? SroName { get; set; }

    [StringLength(255)]
    public string? ProposedRequestTitle { get; set; }

    public string? RequestOverview { get; set; }

    public string? PreviousResearch { get; set; }

    public string? ManifestoOrStatute { get; set; }

    public string? SosOpportunityMissionPillars { get; set; }

    public string? DdtStrategicTheme { get; set; }

    public string? ExpectedBenefits { get; set; }

    public string? RiskConsequence { get; set; }

    public string? FundingProvided { get; set; }

    public string? HeadcountProvided { get; set; }

    public string? HeadcountDetails { get; set; }

    public DateTime? TargetDeliveryDate { get; set; }

    public bool? NewOrChangedDigitalService { get; set; }

    public string? NewOrChangedServiceDetails { get; set; }

    public bool? PublicFacingDigitalService { get; set; }

    // ── Ownership ────────────────────────────────────────────────────────────
    [StringLength(255)]
    public string? OwnerUserEmail { get; set; }

    [StringLength(255)]
    public string? OwnerUserName { get; set; }

    // ── Submission ───────────────────────────────────────────────────────────
    public DateTime? SubmittedAt { get; set; }

    [StringLength(255)]
    public string? SubmittedBy { get; set; }

    // ── Return for clarification ─────────────────────────────────────────────
    public string? ReturnReason { get; set; }

    [StringLength(255)]
    public string? ReturnedBy { get; set; }

    public DateTime? ReturnedAt { get; set; }

    // ── Soft delete ──────────────────────────────────────────────────────────
    public DateTime? DeletedAt { get; set; }

    // ── Audit timestamps ─────────────────────────────────────────────────────
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [StringLength(255)]
    public string? CreatedBy { get; set; }

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [StringLength(255)]
    public string? UpdatedBy { get; set; }

    // ── Link to business case (optional) ─────────────────────────────────────
    public int? BusinessCaseId { get; set; }

    [ForeignKey(nameof(BusinessCaseId))]
    public BusinessCase? BusinessCase { get; set; }

    // ── Link to converted project (optional) ─────────────────────────────────
    public int? ConvertedProjectId { get; set; }

    [ForeignKey(nameof(ConvertedProjectId))]
    public Project? ConvertedProject { get; set; }

    // ── Navigation ───────────────────────────────────────────────────────────
    public DemandExploratoryReview? ExploratoryReview { get; set; }
    public DemandScorecard? Scorecard { get; set; }
    public DemandTriageOutcome? TriageOutcome { get; set; }
    public ICollection<DemandTriageAuditEvent> AuditEvents { get; set; } = new List<DemandTriageAuditEvent>();
}

/// <summary>Allowed status values for DemandTriageRequest.</summary>
public static class DemandTriageStatus
{
    public const string Draft = "draft";
    public const string Submitted = "submitted";
    public const string ExploratoryReviewInProgress = "exploratory_review_in_progress";
    public const string ExploratoryReviewComplete = "exploratory_review_complete";
    public const string ScoringInProgress = "scoring_in_progress";
    public const string ScoredFinalised = "scored_finalised";
    public const string TriagePending = "triage_pending";
    public const string Triaged = "triaged";
    public const string Closed = "closed";
    public const string ReturnedForClarification = "returned_for_clarification";

    public static readonly IReadOnlyList<string> All = new[]
    {
        Draft, Submitted, ExploratoryReviewInProgress, ExploratoryReviewComplete,
        ScoringInProgress, ScoredFinalised, TriagePending, Triaged, Closed,
        ReturnedForClarification
    };

    public static string DisplayName(string status) => status switch
    {
        Draft => "Draft",
        Submitted => "Submitted",
        ExploratoryReviewInProgress => "Exploratory review in progress",
        ExploratoryReviewComplete => "Exploratory review complete",
        ScoringInProgress => "Scoring in progress",
        ScoredFinalised => "Scored and finalised",
        TriagePending => "Triage pending",
        Triaged => "Triaged",
        Closed => "Closed",
        ReturnedForClarification => "Returned for clarification",
        _ => status
    };

    public static string BadgeCssClass(string status) => status switch
    {
        Draft => "govuk-tag govuk-tag--grey",
        Submitted => "govuk-tag govuk-tag--blue",
        ExploratoryReviewInProgress => "govuk-tag govuk-tag--light-blue",
        ExploratoryReviewComplete => "govuk-tag govuk-tag--turquoise",
        ScoringInProgress => "govuk-tag govuk-tag--purple",
        ScoredFinalised => "govuk-tag govuk-tag--pink",
        TriagePending => "govuk-tag govuk-tag--yellow",
        Triaged => "govuk-tag govuk-tag--green",
        Closed => "govuk-tag govuk-tag--grey",
        ReturnedForClarification => "govuk-tag govuk-tag--orange",
        _ => "govuk-tag govuk-tag--grey"
    };
}
