using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models.DemandPipeline;

/// <summary>
/// Formal demand request on the Compass2-style pipeline. Separate from legacy demand triage tables.
/// </summary>
[Table("DemandPipelineRequests")]
public class DemandPipelineRequest
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(20)]
    public string Reference { get; set; } = string.Empty;

    public Guid? BusinessCaseId { get; set; }

    [Required]
    [MaxLength(500)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int? DemandTypeId { get; set; }

    [MaxLength(450)]
    public string? SubmittedBy { get; set; }

    public int? SubmittedByUserId { get; set; }

    public DateTime? SubmittedDate { get; set; }

    [MaxLength(450)]
    public string? Sro { get; set; }

    public int? SroUserId { get; set; }

    public string? PointsOfContact { get; set; }

    public int? AssigneeUserId { get; set; }

    [MaxLength(450)]
    public string? Assignee { get; set; }

    [MaxLength(200)]
    public string? DepartmentGroup { get; set; }

    public int? DirectorateId { get; set; }
    public int? PortfolioId { get; set; }
    public int? GovernmentDepartmentId { get; set; }

    public string? PreviousResearch { get; set; }

    public DateTime? TargetDeliveryDate { get; set; }

    public string? ManifestoCommitment { get; set; }
    public string? ExpectedBenefits { get; set; }
    public string? RiskIfNotDelivered { get; set; }

    public bool? StatutoryDriver { get; set; }

    public int? PriorityOutcomeId { get; set; }
    public int? MissionPillarId { get; set; }

    [MaxLength(500)]
    public string? PriorityOutcomeIds { get; set; }

    [MaxLength(500)]
    public string? MissionPillarIds { get; set; }

    public bool? FundingProvided { get; set; }
    public string? FundingProvidedDetails { get; set; }
    public bool? HeadcountProvided { get; set; }
    public string? HeadcountProvidedDetails { get; set; }
    public bool? IsNewDigitalService { get; set; }
    public string? DigitalServiceChangeDetails { get; set; }
    public bool? IsPublicFacing { get; set; }

    // ── Exploratory review fields ──────────────────────────────────────────
    public string? ExploreNotes { get; set; }

    /// <summary>Structured links (work items + FIPS live services), JSON array. See <see cref="ExploreRelatedLinkDto"/>.</summary>
    public string? ExploreRelatedLinksJson { get; set; }

    /// <summary>Free-text comments for related work (URLs, narrative) in addition to structured links.</summary>
    public string? ExploreLinksToExistingWork { get; set; }

    /// <summary>Research, evidence, and insights gathered during explore.</summary>
    public string? ExploreResearchAndInsights { get; set; }

    /// <summary>Clarification of aim, problem, or outcomes.</summary>
    public string? ExploreAimClarification { get; set; }

    /// <summary>Relevant policies, standards, or statutory context.</summary>
    public string? ExplorePolicies { get; set; }

    /// <summary>User groups, cohorts, or accessibility considerations.</summary>
    public string? ExploreUserGroups { get; set; }

    [MaxLength(50)]
    public string? ExploreFeasibility { get; set; }
    [MaxLength(50)]
    public string? ExploreRecommendation { get; set; }
    public DateTime? ExploreCompletedAt { get; set; }
    [MaxLength(450)]
    public string? ExploreCompletedBy { get; set; }

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Draft";

    public int? TotalScore { get; set; }

    [MaxLength(20)]
    public string? SuggestedBand { get; set; }

    /// <summary>Section 1 — DDT strategic alignment (max 15 raw points).</summary>
    public int? ScoreStrategic { get; set; }

    /// <summary>Section 2 — Urgency (max 10).</summary>
    public int? ScoreUrgency { get; set; }

    /// <summary>Section 3 — Funding and resource (max 22).</summary>
    public int? ScoreFunding { get; set; }

    /// <summary>Section 4 — RICE (max 42).</summary>
    public int? ScoreRice { get; set; }

    public string? ScoringAssessmentNotes { get; set; }

    public string? ScoringConcernsNotes { get; set; }

    /// <summary>JSON map of question code → answer (option id for radios, text/number for others).</summary>
    public string? ScoringAnswersJson { get; set; }

    public DateTime? ScoredAt { get; set; }

    [MaxLength(450)]
    public string? ScoredBy { get; set; }

    /// <summary>Short triage decision code (e.g. ProgressedToDelivery, Rejected).</summary>
    [MaxLength(50)]
    public string? TriageOutcome { get; set; }

    /// <summary>Full narrative recorded at triage.</summary>
    public string? TriageOutcomeNarrative { get; set; }

    /// <summary>Work item created from triage when requested.</summary>
    public int? TriageCreatedProjectId { get; set; }

    public DateTime? TriagedAt { get; set; }

    [MaxLength(450)]
    public string? TriagedBy { get; set; }

    /// <summary>Optional triage meeting this demand is scheduled for (Scored / Triage pending).</summary>
    public Guid? TriageMeetingId { get; set; }

    /// <summary>Selected triage outcome stage from admin lookup (when an outcome is recorded).</summary>
    public int? TriageStageLookupId { get; set; }

    /// <summary>Business area assigned at triage (especially when progressing to delivery).</summary>
    public int? TriageAssignedBusinessAreaId { get; set; }

    /// <summary>Primary contact for delivery, captured at triage.</summary>
    public int? TriagePrimaryContactUserId { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    [MaxLength(450)]
    public string? CreatedBy { get; set; }

    [MaxLength(450)]
    public string? UpdatedBy { get; set; }
}
