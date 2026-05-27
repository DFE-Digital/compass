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

    // ── Original risk rating (set on first save, not changed after) ──

    public int? RiskLikelihoodId { get; set; }

    [ForeignKey(nameof(RiskLikelihoodId))]
    public RiskLikelihood? Likelihood { get; set; }

    public int? RiskImpactLevelId { get; set; }

    [ForeignKey(nameof(RiskImpactLevelId))]
    public RiskImpactLevel? ImpactLevel { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? InherentScore { get; set; }

    // ── Current risk rating (initially copies Original, updated over time) ──

    public int? CurrentLikelihoodId { get; set; }

    [ForeignKey(nameof(CurrentLikelihoodId))]
    public RiskLikelihood? CurrentLikelihood { get; set; }

    public int? CurrentImpactLevelId { get; set; }

    [ForeignKey(nameof(CurrentImpactLevelId))]
    public RiskImpactLevel? CurrentImpactLevel { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? CurrentScore { get; set; }

    // ── Residual risk rating ──

    public int? ResidualLikelihoodId { get; set; }

    [ForeignKey(nameof(ResidualLikelihoodId))]
    public RiskLikelihood? ResidualLikelihoodLevel { get; set; }

    public int? ResidualImpactLevelId { get; set; }

    [ForeignKey(nameof(ResidualImpactLevelId))]
    public RiskImpactLevel? ResidualImpactLevel { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? ResidualScore { get; set; }

    // ── Tolerance risk rating ──

    public int? ToleranceLikelihoodId { get; set; }

    [ForeignKey(nameof(ToleranceLikelihoodId))]
    public RiskLikelihood? ToleranceLikelihood { get; set; }

    public int? ToleranceImpactLevelId { get; set; }

    [ForeignKey(nameof(ToleranceImpactLevelId))]
    public RiskImpactLevel? ToleranceImpactLevel { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? ToleranceScore { get; set; }

    // ── Other lookups ──

    public int? RiskProximityId { get; set; }

    [ForeignKey(nameof(RiskProximityId))]
    public RiskProximity? Proximity { get; set; }

    public int? RiskCategoryId { get; set; }

    [ForeignKey(nameof(RiskCategoryId))]
    public RiskCategory? RiskCategory { get; set; }

    public DateTime? IdentifiedDate { get; set; }

    public DateTime? NextReviewDate { get; set; }

    public DateTime? LastReviewDate { get; set; }

    public int? GovernanceBoardId { get; set; }

    [ForeignKey(nameof(GovernanceBoardId))]
    public GovernanceBoard? GovernanceBoard { get; set; }

    public int? PrimaryProductId { get; set; }

    [ForeignKey(nameof(PrimaryProductId))]
    public FipsService? PrimaryProduct { get; set; }

    /// <summary>WorkItem, Product, or Organisation — see <see cref="RaidAssociationKinds"/>.</summary>
    [MaxLength(20)]
    public string? RaidAssociationKind { get; set; }

    public int? SroUserId { get; set; }

    [ForeignKey(nameof(SroUserId))]
    public User? SroUser { get; set; }

    public string? ResponseStrategy { get; set; }

    [MaxLength(1000)]
    public string? HowIdentified { get; set; }

    /// <summary>Root cause or drivers (narrative).</summary>
    public string? Cause { get; set; }

    /// <summary>Consequence narrative if the risk materialises.</summary>
    public string? ImpactIfRealised { get; set; }

    /// <summary>Contingency arrangements if the risk materialises.</summary>
    public string? Contingency { get; set; }

    /// <summary>Assurance arrangements for this risk.</summary>
    public string? Assurance { get; set; }

    /// <summary>Financial impact narrative.</summary>
    public string? FinancialImpact { get; set; }

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

    /// <summary>Measurable indicators for this risk.</summary>
    public ICollection<RiskKeyRiskIndicator> KeyRiskIndicators { get; set; } = new List<RiskKeyRiskIndicator>();

    /// <summary>Modern RAID: multiple category labels.</summary>
    public ICollection<RiskRiskCategory> RiskRiskCategories { get; set; } = new List<RiskRiskCategory>();

    /// <summary>Modern RAID: divisions (multi-select).</summary>
    public ICollection<RiskDivision> RiskDivisions { get; set; } = new List<RiskDivision>();

    /// <summary>Modern RAID: business areas from admin lookup (multi-select).</summary>
    public ICollection<RiskBusinessArea> RiskBusinessAreas { get; set; } = new List<RiskBusinessArea>();

    /// <summary>Audit trail of rating changes (original → current over time).</summary>
    public ICollection<RiskRatingHistory> RatingHistory { get; set; } = new List<RiskRatingHistory>();
}

