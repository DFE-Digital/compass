using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models.Ddr;

/// <summary>Links a DDR to a FIPS product / service (<c>ddr_record_product_link</c>, §7.2).</summary>
public class DdrProductLink
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int DesignDecisionRecordId { get; set; }

    [ForeignKey(nameof(DesignDecisionRecordId))]
    public DesignDecisionRecord? DesignDecisionRecord { get; set; }

    /// <summary>Reference to <see cref="Models.Fips.CMDBProduct.Id"/>. Loose FK — products may move between sources.</summary>
    public Guid FipsProductId { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [MaxLength(255)]
    public string CreatedBy { get; set; } = string.Empty;
}

/// <summary>Links a DDR to a COMPASS work item (<c>ddr_record_work_item_link</c>, §7.3).</summary>
public class DdrWorkItemLink
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int DesignDecisionRecordId { get; set; }

    [ForeignKey(nameof(DesignDecisionRecordId))]
    public DesignDecisionRecord? DesignDecisionRecord { get; set; }

    /// <summary>Reference to <see cref="Project.Id"/>. Loose FK so DDRs survive Project lifecycle changes.</summary>
    public int WorkItemId { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [MaxLength(255)]
    public string CreatedBy { get; set; } = string.Empty;
}

/// <summary>Alternatives considered (<c>ddr_alternative</c>, §7.4).</summary>
public class DdrAlternative
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int DesignDecisionRecordId { get; set; }

    [ForeignKey(nameof(DesignDecisionRecordId))]
    public DesignDecisionRecord? DesignDecisionRecord { get; set; }

    [Required]
    public string AlternativeText { get; set; } = string.Empty;

    /// <summary>Why this alternative was not chosen (or its outcome if it was tried).</summary>
    public string? Outcome { get; set; }

    public int SortOrder { get; set; }
}

/// <summary>Evidence supporting the decision (<c>ddr_evidence</c>, §7.4).</summary>
public class DdrEvidence
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int DesignDecisionRecordId { get; set; }

    [ForeignKey(nameof(DesignDecisionRecordId))]
    public DesignDecisionRecord? DesignDecisionRecord { get; set; }

    [Required]
    [MaxLength(200)]
    public string EvidenceTitle { get; set; } = string.Empty;

    [MaxLength(80)]
    public string? EvidenceType { get; set; }

    [MaxLength(2048)]
    public string? EvidenceUrl { get; set; }

    public string? EvidenceSummary { get; set; }
}

/// <summary>Linked standards / WCAG criteria / guidance (<c>ddr_standard_link</c>, §7.4).</summary>
public class DdrStandardLink
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int DesignDecisionRecordId { get; set; }

    [ForeignKey(nameof(DesignDecisionRecordId))]
    public DesignDecisionRecord? DesignDecisionRecord { get; set; }

    [MaxLength(80)]
    public string? StandardType { get; set; }

    [MaxLength(120)]
    public string? StandardReference { get; set; }

    [MaxLength(255)]
    public string? StandardTitle { get; set; }

    [MaxLength(2048)]
    public string? StandardUrl { get; set; }
}

/// <summary>Linked components or patterns from GOV.UK / DfE Frontend (<c>ddr_component_pattern_link</c>, §7.4).</summary>
public class DdrComponentPatternLink
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int DesignDecisionRecordId { get; set; }

    [ForeignKey(nameof(DesignDecisionRecordId))]
    public DesignDecisionRecord? DesignDecisionRecord { get; set; }

    [MaxLength(80)]
    public string? SourceSystem { get; set; }

    /// <summary>"component", "pattern", "guidance" etc.</summary>
    [MaxLength(40)]
    public string? ItemType { get; set; }

    [MaxLength(200)]
    public string? ItemName { get; set; }

    [MaxLength(2048)]
    public string? ItemUrl { get; set; }
}

/// <summary>Related and superseded DDRs (<c>ddr_related_record</c>, §7.4).</summary>
public class DdrRelatedRecord
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int DesignDecisionRecordId { get; set; }

    [ForeignKey(nameof(DesignDecisionRecordId))]
    public DesignDecisionRecord? DesignDecisionRecord { get; set; }

    public int RelatedDesignDecisionRecordId { get; set; }

    [MaxLength(40)]
    public string RelationshipType { get; set; } = "Related to";
}

/// <summary>Comments and observations on a DDR (<c>ddr_comment</c>, §7.4).</summary>
public class DdrComment
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int DesignDecisionRecordId { get; set; }

    [ForeignKey(nameof(DesignDecisionRecordId))]
    public DesignDecisionRecord? DesignDecisionRecord { get; set; }

    [MaxLength(40)]
    public string CommentType { get; set; } = "Comment";

    [Required]
    [MaxLength(4000)]
    public string CommentText { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string CreatedBy { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? CreatedByName { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>DesignOps insight classification (<c>ddr_insight_classification</c>, §7.4 / §8.5).</summary>
public class DdrInsightClassification
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int DesignDecisionRecordId { get; set; }

    [ForeignKey(nameof(DesignDecisionRecordId))]
    public DesignDecisionRecord? DesignDecisionRecord { get; set; }

    [Required]
    [MaxLength(80)]
    public string Classification { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string CreatedBy { get; set; } = string.Empty;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Recommended follow-up actions (<c>ddr_recommended_follow_up</c>, §7.4 / §8.6).</summary>
public class DdrRecommendedFollowUp
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int DesignDecisionRecordId { get; set; }

    [ForeignKey(nameof(DesignDecisionRecordId))]
    public DesignDecisionRecord? DesignDecisionRecord { get; set; }

    [Required]
    [MaxLength(120)]
    public string FollowUpAction { get; set; } = string.Empty;

    [MaxLength(120)]
    public string? TargetBacklog { get; set; }

    [MaxLength(40)]
    public string Status { get; set; } = "Open";

    public string? Notes { get; set; }

    [Required]
    [MaxLength(255)]
    public string CreatedBy { get; set; } = string.Empty;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>GitHub issue linked from a DDR (<c>ddr_github_issue_link</c>, §7.4).</summary>
public class DdrGitHubIssueLink
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int DesignDecisionRecordId { get; set; }

    [ForeignKey(nameof(DesignDecisionRecordId))]
    public DesignDecisionRecord? DesignDecisionRecord { get; set; }

    [Required]
    [MaxLength(2048)]
    public string IssueUrl { get; set; } = string.Empty;

    [MaxLength(120)]
    public string? BacklogType { get; set; }

    [MaxLength(255)]
    public string? IssueTitle { get; set; }
}

/// <summary>Immutable audit history for a DDR (<c>ddr_audit_event</c>, §7.4).</summary>
public class DdrAuditEvent
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int DesignDecisionRecordId { get; set; }

    [ForeignKey(nameof(DesignDecisionRecordId))]
    public DesignDecisionRecord? DesignDecisionRecord { get; set; }

    [Required]
    [MaxLength(40)]
    public string EventType { get; set; } = string.Empty;

    [MaxLength(120)]
    public string? FieldName { get; set; }

    public string? PreviousValue { get; set; }

    public string? NewValue { get; set; }

    [Required]
    [MaxLength(255)]
    public string CreatedBy { get; set; } = string.Empty;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Persisted record of DDR feature flag changes (<c>ddr_feature_setting</c>, §7.4).
/// Companion to the global <see cref="Feature"/> table — used to record a free-text reason
/// alongside the global toggle change so it can be exported with the audit trail.</summary>
public class DdrFeatureSetting
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(80)]
    public string SettingKey { get; set; } = string.Empty;

    [Required]
    [MaxLength(40)]
    public string SettingValue { get; set; } = string.Empty;

    public string? Reason { get; set; }

    [Required]
    [MaxLength(255)]
    public string UpdatedBy { get; set; } = string.Empty;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
