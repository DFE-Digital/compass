using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

/// <summary>
/// RAID-aligned Action entity. Keeps legacy columns for backward compatibility while new flows are introduced.
/// </summary>
public class Action
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

    public int? DecisionId { get; set; }

    [ForeignKey(nameof(DecisionId))]
    public Decision? Decision { get; set; }

    [MaxLength(50)]
    public string? FipsId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    [MaxLength(100)]
    public string? BusinessArea { get; set; }

    [MaxLength(100)]
    public string? SourceType { get; set; }

    [MaxLength(100)]
    public string? SourceReference { get; set; }

    [MaxLength(500)]
    public string? SourceRecordUrl { get; set; }

    public int? ActionSourceId { get; set; }

    [ForeignKey(nameof(ActionSourceId))]
    public ActionSource? ActionSource { get; set; }

    [MaxLength(255)]
    public string? AssignedToEmail { get; set; }

    [MaxLength(10)]
    public string? Priority { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "not_started";

    public DateTime? StartDate { get; set; }

    public DateTime? DueDate { get; set; }

    public DateTime? CompletedDate { get; set; }

    public int? ParentActionId { get; set; }

    [ForeignKey(nameof(ParentActionId))]
    public Action? ParentAction { get; set; }

    [MaxLength(500)]
    public string? EvidenceUrl { get; set; }

    public string? Notes { get; set; }

    #endregion

    #region RAID redesign fields

    [MaxLength(150)]
    public string? Source { get; set; }

    [MaxLength(100)]
    public string? SourceId { get; set; }

    public ICollection<ActionTag> Tags { get; set; } = new List<ActionTag>();

    public int? AssignedToUserId { get; set; }

    [ForeignKey(nameof(AssignedToUserId))]
    public User? AssignedToUser { get; set; }

    public int? AccountablePersonUserId { get; set; }

    [ForeignKey(nameof(AccountablePersonUserId))]
    public User? AccountablePerson { get; set; }

    [MaxLength(150)]
    public string? TeamName { get; set; }

    public DateTime? ClosedDate { get; set; }

    public DateTime? LastProgressUpdate { get; set; }

    [Range(0, 100)]
    public int ProgressPercent { get; set; }

    public bool Blocked { get; set; }

    [MaxLength(1000)]
    public string? BlockedReason { get; set; }

    [MaxLength(20)]
    public string? Rag { get; set; }

    public string? Evidence { get; set; }

    public int? EvidenceTypeId { get; set; }

    [ForeignKey(nameof(EvidenceTypeId))]
    public RaidEvidenceType? EvidenceType { get; set; }

    public bool VerificationRequired { get; set; }

    public int? VerifiedByUserId { get; set; }

    [ForeignKey(nameof(VerifiedByUserId))]
    public User? VerifiedByUser { get; set; }

    public DateTime? VerifiedDate { get; set; }

    public string? VerificationNotes { get; set; }

    public int? ActionTypeId { get; set; }

    [ForeignKey(nameof(ActionTypeId))]
    public ActionType? ActionType { get; set; }

    public int? CategoryId { get; set; }

    [ForeignKey(nameof(CategoryId))]
    public ActionCategory? Category { get; set; }

    public int? ImpactLevelId { get; set; }

    [ForeignKey(nameof(ImpactLevelId))]
    public ActionImpactLevel? ImpactLevel { get; set; }

    public int? PriorityId { get; set; }

    [ForeignKey(nameof(PriorityId))]
    public ActionPriority? PriorityLookup { get; set; }

    public int? StatusId { get; set; }

    [ForeignKey(nameof(StatusId))]
    public ActionStatus? StatusLookup { get; set; }

    public int? RiskId { get; set; }

    [ForeignKey(nameof(RiskId))]
    public Risk? Risk { get; set; }

    public int? IssueId { get; set; }

    [ForeignKey(nameof(IssueId))]
    public Issue? Issue { get; set; }

    public int? MilestoneId { get; set; }

    [ForeignKey(nameof(MilestoneId))]
    public Milestone? Milestone { get; set; }

    public int? ReminderFrequencyId { get; set; }

    [ForeignKey(nameof(ReminderFrequencyId))]
    public ActionReminderFrequency? ReminderFrequency { get; set; }

    public int? EscalationThresholdId { get; set; }

    [ForeignKey(nameof(EscalationThresholdId))]
    public ActionEscalationThreshold? EscalationThreshold { get; set; }

    public bool EscalationTriggered { get; set; }

    public int? ServiceId { get; set; }

    [ForeignKey(nameof(ServiceId))]
    public FipsService? Service { get; set; }

    public int? PrimaryProductId { get; set; }

    [ForeignKey(nameof(PrimaryProductId))]
    public FipsService? PrimaryProduct { get; set; }

    [MaxLength(50)]
    public string LifecyclePhase { get; set; } = "Explore";

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
    public ICollection<Action> SubActions { get; set; } = new List<Action>();
    public ICollection<RiskAction> RiskActions { get; set; } = new List<RiskAction>();
    public ICollection<IssueAction> IssueActions { get; set; } = new List<IssueAction>();
    public ICollection<MilestoneAction> MilestoneActions { get; set; } = new List<MilestoneAction>();

    [NotMapped]
    public ICollection<Dependency> DependenciesAsSource { get; set; } = new List<Dependency>();

    [NotMapped]
    public ICollection<Dependency> DependenciesAsTarget { get; set; } = new List<Dependency>();
}
