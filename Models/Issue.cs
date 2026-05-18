using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

/// <summary>
/// RAID-aligned Issue entity that retains legacy columns so existing flows continue to work.
/// </summary>
public class Issue
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    #region Legacy fields (to retire)

    public int? ObjectiveId { get; set; }

    [ForeignKey(nameof(ObjectiveId))]
    public Objective? Objective { get; set; }

    public int? ProjectId { get; set; }

    [ForeignKey(nameof(ProjectId))]
    public Project? Project { get; set; }

    [MaxLength(50)]
    public string? FipsId { get; set; }

    [MaxLength(100)]
    public string? ProductDocumentId { get; set; } // Product DocumentID from CMS (primary identifier)

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    [MaxLength(100)]
    public string? Category { get; set; }

    [MaxLength(100)]
    public string? BusinessArea { get; set; }

    public int? OwnerUserId { get; set; }

    [ForeignKey(nameof(OwnerUserId))]
    public User? OwnerUser { get; set; }

    [Required]
    [MaxLength(10)]
    public string Severity { get; set; } = "medium";

    [MaxLength(10)]
    public string? Priority { get; set; }

    [Required]
    public DateTime DetectedDate { get; set; } = DateTime.UtcNow;

    public DateTime? TargetResolutionDate { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "open";

    public string? ResolutionSummary { get; set; }

    public string? Workaround { get; set; }

    [MaxLength(100)]
    public string? SourceType { get; set; }

    [MaxLength(100)]
    public string? SourceReference { get; set; }

    [MaxLength(500)]
    public string? SourceRecordUrl { get; set; }

    public int? SourceRiskId { get; set; }

    [ForeignKey(nameof(SourceRiskId))]
    public Risk? SourceRisk { get; set; }

    public bool BlockedFlag { get; set; } = false;

    public DateTime? ClosedDate { get; set; }

    #endregion

    #region RAID redesign fields

    [MaxLength(150)]
    public string? Source { get; set; }

    [MaxLength(100)]
    public string? SourceId { get; set; }

    public ICollection<IssueTag> Tags { get; set; } = new List<IssueTag>();

    public int? MilestoneId { get; set; }

    [ForeignKey(nameof(MilestoneId))]
    public Milestone? Milestone { get; set; }

    public int? RiskId { get; set; }

    [ForeignKey(nameof(RiskId))]
    public Risk? Risk { get; set; }

    public int? PrimaryProductId { get; set; }

    [ForeignKey(nameof(PrimaryProductId))]
    public FipsService? PrimaryProduct { get; set; }

    /// <summary>WorkItem, Product, or Organisation — see <see cref="RaidAssociationKinds"/>.</summary>
    [MaxLength(20)]
    public string? RaidAssociationKind { get; set; }

    public int? SroUserId { get; set; }

    [ForeignKey(nameof(SroUserId))]
    public User? SroUser { get; set; }

    public string? UserImpactSummary { get; set; }

    public string? ServiceImpactSummary { get; set; }

    [NotMapped]
    public bool Blocked => BlockedFlag;

    public bool BlocksRelease
    {
        get => BlockedFlag;
        set => BlockedFlag = value;
    }

    public int? StatusId { get; set; }

    [ForeignKey(nameof(StatusId))]
    public IssueStatus? StatusLookup { get; set; }

    public int? PriorityId { get; set; }

    [ForeignKey(nameof(PriorityId))]
    public IssuePriority? PriorityLookup { get; set; }

    public int? SeverityId { get; set; }

    [ForeignKey(nameof(SeverityId))]
    public IssueSeverity? SeverityLookup { get; set; }

    public int? IssueCategoryId { get; set; }

    [ForeignKey(nameof(IssueCategoryId))]
    public IssueCategory? CategoryLookup { get; set; }

    public DateTime? ResolvedDate { get; set; }

    public int? CreatedByUserId { get; set; }

    [ForeignKey(nameof(CreatedByUserId))]
    public User? CreatedByUser { get; set; }

    public int? UpdatedByUserId { get; set; }

    [ForeignKey(nameof(UpdatedByUserId))]
    public User? UpdatedByUser { get; set; }

    public int? ClosedByUserId { get; set; }

    [ForeignKey(nameof(ClosedByUserId))]
    public User? ClosedByUser { get; set; }

    #endregion

    public bool IsDeleted { get; set; } = false;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<IssueAction> IssueActions { get; set; } = new List<IssueAction>();
    public ICollection<MilestoneIssue> MilestoneIssues { get; set; } = new List<MilestoneIssue>();

    [NotMapped]
    public ICollection<Dependency> DependenciesAsSource { get; set; } = new List<Dependency>();

    [NotMapped]
    public ICollection<Dependency> DependenciesAsTarget { get; set; } = new List<Dependency>();

    public ICollection<IssueDecision> IssueDecisions { get; set; } = new List<IssueDecision>();

    public ICollection<IssueRisk> IssueRisks { get; set; } = new List<IssueRisk>();
    public ICollection<IssueComment> Comments { get; set; } = new List<IssueComment>();
    public ICollection<IssueHistory> HistoryEntries { get; set; } = new List<IssueHistory>();
    public ICollection<IssueWcagCriterion> WcagCriteria { get; set; } = new List<IssueWcagCriterion>();

    /// <summary>Modern RAID: multiple category labels.</summary>
    public ICollection<IssueIssueCategory> IssueIssueCategories { get; set; } = new List<IssueIssueCategory>();

    /// <summary>Modern RAID: divisions (multi-select).</summary>
    public ICollection<IssueDivision> IssueDivisions { get; set; } = new List<IssueDivision>();

    /// <summary>Modern RAID: business areas from admin lookup (multi-select).</summary>
    public ICollection<IssueBusinessArea> IssueBusinessAreas { get; set; } = new List<IssueBusinessArea>();

    /// <summary>Detailed root cause narrative for RAID issues.</summary>
    public string? DetailedCause { get; set; }

    /// <summary>Summary of significant assurance arrangements (narrative).</summary>
    public string? AssuranceArrangements { get; set; }

    public ICollection<IssueAssuranceEvent> AssuranceEvents { get; set; } = new List<IssueAssuranceEvent>();
}

