using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

/// <summary>
/// RAID-aligned Risk entity retaining legacy fields for backwards compatibility.
/// </summary>
public class Risk
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

    public int? RiskTierId { get; set; }

    [ForeignKey(nameof(RiskTierId))]
    public RiskTier? RiskTier { get; set; }

    [MaxLength(255)]
    public string? OwnerEmail { get; set; }

    [Required]
    [Range(1, 5)]
    public int ImpactRating { get; set; }

    [Required]
    [Range(1, 5)]
    public int LikelihoodRating { get; set; }

    public int RiskScore { get; set; }

    public DateTime? ProximityDate { get; set; }

    [MaxLength(20)]
    public string? Response { get; set; }

    [Range(1, 5)]
    public int? ResidualImpact { get; set; }

    [Range(1, 5)]
    public int? ResidualLikelihood { get; set; }

    public DateTime? TargetDate { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "new";

    public DateTime? ClosedDate { get; set; }

    public string? Notes { get; set; }

    #endregion

    #region RAID redesign fields

    [MaxLength(150)]
    public string? Source { get; set; }

    [MaxLength(100)]
    public string? SourceId { get; set; }

    public ICollection<RiskTag> Tags { get; set; } = new List<RiskTag>();

    public int? OwnerUserId { get; set; }

    [ForeignKey(nameof(OwnerUserId))]
    public User? OwnerUser { get; set; }

    public int? RiskStatusId { get; set; }

    [ForeignKey(nameof(RiskStatusId))]
    public RiskStatus? RiskStatus { get; set; }

    public int? RiskPriorityId { get; set; }

    [ForeignKey(nameof(RiskPriorityId))]
    public RiskPriority? RiskPriority { get; set; }

    public int? RiskLikelihoodId { get; set; }

    [ForeignKey(nameof(RiskLikelihoodId))]
    public RiskLikelihood? Likelihood { get; set; }

    public int? RiskImpactLevelId { get; set; }

    [ForeignKey(nameof(RiskImpactLevelId))]
    public RiskImpactLevel? ImpactLevel { get; set; }

    public int? RiskProximityId { get; set; }

    [ForeignKey(nameof(RiskProximityId))]
    public RiskProximity? Proximity { get; set; }

    public int? RiskCategoryId { get; set; }

    [ForeignKey(nameof(RiskCategoryId))]
    public RiskCategory? RiskCategory { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? InherentScore { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? ResidualScore { get; set; }

    public DateTime? IdentifiedDate { get; set; }

    public DateTime? NextReviewDate { get; set; }

    public DateTime? LastReviewDate { get; set; }

    public int? GovernanceBoardId { get; set; }

    [ForeignKey(nameof(GovernanceBoardId))]
    public GovernanceBoard? GovernanceBoard { get; set; }

    public int? PrimaryProductId { get; set; }

    [ForeignKey(nameof(PrimaryProductId))]
    public FipsService? PrimaryProduct { get; set; }

    public string? ResponseStrategy { get; set; }

    [MaxLength(1000)]
    public string? HowIdentified { get; set; }

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
    public ICollection<RiskAction> RiskActions { get; set; } = new List<RiskAction>();
    public ICollection<MilestoneRisk> MilestoneRisks { get; set; } = new List<MilestoneRisk>();

    [NotMapped]
    public ICollection<Dependency> DependenciesAsSource { get; set; } = new List<Dependency>();

    [NotMapped]
    public ICollection<Dependency> DependenciesAsTarget { get; set; } = new List<Dependency>();

    public ICollection<RiskRiskType> RiskRiskTypes { get; set; } = new List<RiskRiskType>();
    public ICollection<RiskDecision> RiskDecisions { get; set; } = new List<RiskDecision>();

    public ICollection<IssueRisk> IssueRisks { get; set; } = new List<IssueRisk>();
}

