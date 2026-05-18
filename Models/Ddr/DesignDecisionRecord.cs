using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models.Ddr;

/// <summary>
/// A Design Decision Record. Captures a structured design / service / accessibility decision made
/// against a FIPS product or COMPASS work item. Mapped to <c>ddr_record</c>; field set follows
/// <c>compass/documentation/ddr.md</c> §7.1.
/// </summary>
public class DesignDecisionRecord
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>Human-readable identifier — e.g. <c>DDR-0042</c>. Generated on first save.</summary>
    [Required]
    [MaxLength(20)]
    public string Reference { get; set; } = string.Empty;

    [Required]
    [MaxLength(80)]
    public string Category { get; set; } = string.Empty;

    public int? AuthorUserId { get; set; }

    [Required]
    [MaxLength(200)]
    public string AuthorDisplayName { get; set; } = string.Empty;

    [Required]
    [MaxLength(120)]
    public string ShortTitle { get; set; } = string.Empty;

    /// <summary>50–4,000 chars. Required.</summary>
    public string ContextProblemStatement { get; set; } = string.Empty;

    /// <summary>50–3,000 chars. Required.</summary>
    public string Decision { get; set; } = string.Empty;

    /// <summary>50–4,000 chars. Required.</summary>
    public string Rationale { get; set; } = string.Empty;

    /// <summary>30–3,000 chars. Required.</summary>
    public string ConsequencesTradeoffs { get; set; } = string.Empty;

    public bool DeviationFlag { get; set; }

    [MaxLength(80)]
    public string? DeviationType { get; set; }

    public string? DeviationDetails { get; set; }

    [MaxLength(120)]
    public string? ApprovalRoute { get; set; }

    [MaxLength(200)]
    public string? ApprovedBy { get; set; }

    [Required]
    [MaxLength(40)]
    public string Status { get; set; } = "Draft";

    /// <summary>1–4,000 chars. Required. Plain-English description of when / why this decision should be revisited.</summary>
    [MaxLength(4000)]
    public string ReviewTrigger { get; set; } = string.Empty;

    public DateTime? ReviewDate { get; set; }

    public bool RetrospectiveRecord { get; set; }

    /// <summary>Free-text date for historic decisions where exact date is unknown (e.g. "Spring 2023").</summary>
    [MaxLength(50)]
    public string? OriginalDecisionDate { get; set; }

    public string? RetrospectiveContext { get; set; }

    [MaxLength(40)]
    public string? CurrentValidity { get; set; }

    public string? CurrentValidityRationale { get; set; }

    public string? MessageToDesignOps { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [MaxLength(255)]
    public string CreatedBy { get; set; } = string.Empty;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [MaxLength(255)]
    public string UpdatedBy { get; set; } = string.Empty;

    public DateTime? SubmittedAt { get; set; }

    [MaxLength(255)]
    public string? SubmittedBy { get; set; }

    /// <summary>Soft-delete timestamp. Records are never hard-deleted (BR audit-history protection).</summary>
    public DateTime? DeletedAt { get; set; }

    [MaxLength(255)]
    public string? DeletedBy { get; set; }

    // Navigation
    public ICollection<DdrAlternative> Alternatives { get; set; } = new List<DdrAlternative>();
    public ICollection<DdrEvidence> Evidence { get; set; } = new List<DdrEvidence>();
    public ICollection<DdrProductLink> ProductLinks { get; set; } = new List<DdrProductLink>();
    public ICollection<DdrWorkItemLink> WorkItemLinks { get; set; } = new List<DdrWorkItemLink>();
    public ICollection<DdrStandardLink> StandardLinks { get; set; } = new List<DdrStandardLink>();
    public ICollection<DdrComponentPatternLink> ComponentPatternLinks { get; set; } = new List<DdrComponentPatternLink>();
    public ICollection<DdrRelatedRecord> RelatedRecords { get; set; } = new List<DdrRelatedRecord>();
    public ICollection<DdrComment> Comments { get; set; } = new List<DdrComment>();
    public ICollection<DdrInsightClassification> InsightClassifications { get; set; } = new List<DdrInsightClassification>();
    public ICollection<DdrRecommendedFollowUp> RecommendedFollowUps { get; set; } = new List<DdrRecommendedFollowUp>();
    public ICollection<DdrGitHubIssueLink> GitHubIssueLinks { get; set; } = new List<DdrGitHubIssueLink>();
    public ICollection<DdrAuditEvent> AuditEvents { get; set; } = new List<DdrAuditEvent>();
}
