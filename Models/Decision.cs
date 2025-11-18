using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

/// <summary>
/// Core Decision record aligned to the RAID redesign.
/// </summary>
public class Decision
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int? ObjectiveId { get; set; }

    [ForeignKey(nameof(ObjectiveId))]
    public Objective? Objective { get; set; }

    public int? ProjectId { get; set; }

    [ForeignKey(nameof(ProjectId))]
    public Project? Project { get; set; }

    [MaxLength(50)]
    public string? FipsId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "pending";

    [MaxLength(50)]
    public string? DecisionType { get; set; }

    public DateTime? DecisionDate { get; set; }

    public string? Summary { get; set; }

    [MaxLength(100)]
    public string? BusinessArea { get; set; }

    [MaxLength(255)]
    public string? OwnerEmail { get; set; }

    public string? Outcome { get; set; }

    public string? Notes { get; set; }

    [MaxLength(100)]
    public string? SourceType { get; set; }

    [MaxLength(100)]
    public string? SourceReference { get; set; }

    [MaxLength(500)]
    public string? SourceRecordUrl { get; set; }

    #region RAID redesign fields

    public ICollection<DecisionTag> Tags { get; set; } = new List<DecisionTag>();

    public int? StatusId { get; set; }

    [ForeignKey(nameof(StatusId))]
    public DecisionStatus? StatusLookup { get; set; }

    public int? PriorityId { get; set; }

    [ForeignKey(nameof(PriorityId))]
    public DecisionPriority? PriorityLookup { get; set; }

    public int? OutcomeId { get; set; }

    [ForeignKey(nameof(OutcomeId))]
    public DecisionOutcome? OutcomeLookup { get; set; }

    public DateTime? EffectiveDate { get; set; }

    public DateTime? ExpiresDate { get; set; }

    public int? GovernanceBoardId { get; set; }

    [ForeignKey(nameof(GovernanceBoardId))]
    public GovernanceBoard? GovernanceBoard { get; set; }

    public int? OwnerUserId { get; set; }

    [ForeignKey(nameof(OwnerUserId))]
    public User? OwnerUser { get; set; }

    [MaxLength(100)]
    public string? AgendaItemReference { get; set; }

    public int? DecisionMakerUserId { get; set; }

    [ForeignKey(nameof(DecisionMakerUserId))]
    public User? DecisionMaker { get; set; }

    public int? ProposedByUserId { get; set; }

    [ForeignKey(nameof(ProposedByUserId))]
    public User? ProposedByUser { get; set; }

    public int? SponsorUserId { get; set; }

    [ForeignKey(nameof(SponsorUserId))]
    public User? SponsorUser { get; set; }

    public string? ResponsibleTeam { get; set; }

    public int? ImplementationOwnerUserId { get; set; }

    [ForeignKey(nameof(ImplementationOwnerUserId))]
    public User? ImplementationOwnerUser { get; set; }

    public int? ImplementationStatusId { get; set; }

    [ForeignKey(nameof(ImplementationStatusId))]
    public DecisionImplementationStatus? ImplementationStatus { get; set; }

    public DateTime? ImplementationDeadline { get; set; }

    public bool VerificationRequired { get; set; }

    public int? VerifiedByUserId { get; set; }

    [ForeignKey(nameof(VerifiedByUserId))]
    public User? VerifiedByUser { get; set; }

    public DateTime? VerifiedDate { get; set; }

    public string? VerificationNotes { get; set; }

    public string? Evidence { get; set; }

    public int? EvidenceTypeId { get; set; }

    [ForeignKey(nameof(EvidenceTypeId))]
    public RaidEvidenceType? EvidenceType { get; set; }

    public string? AdditionalDocuments { get; set; }

    public int? PrimaryProductId { get; set; }

    [ForeignKey(nameof(PrimaryProductId))]
    public FipsService? PrimaryProduct { get; set; }

    public int? MilestoneId { get; set; }

    [ForeignKey(nameof(MilestoneId))]
    public Milestone? Milestone { get; set; }

    public string? Source { get; set; }

    public string? SourceId { get; set; }

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
    public ICollection<Action> Actions { get; set; } = new List<Action>();
    public ICollection<RiskDecision> RiskDecisions { get; set; } = new List<RiskDecision>();
    public ICollection<IssueDecision> IssueDecisions { get; set; } = new List<IssueDecision>();
    public ICollection<ActionDecision> ActionDecisions { get; set; } = new List<ActionDecision>();
}

