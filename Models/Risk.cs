using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

public class Risk
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

    public int RiskScore { get; set; } // Computed: impact * likelihood

    public DateTime? ProximityDate { get; set; }

    [MaxLength(20)]
    public string? Response { get; set; } // avoid, mitigate, transfer, accept

    [Range(1, 5)]
    public int? ResidualImpact { get; set; }

    [Range(1, 5)]
    public int? ResidualLikelihood { get; set; }

    public DateTime? TargetDate { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "new"; // new, open, treating, monitoring, closed

    public DateTime? ClosedDate { get; set; }

    public string? Notes { get; set; }

    public bool IsDeleted { get; set; } = false;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<RiskAction> RiskActions { get; set; } = new List<RiskAction>();
    public ICollection<MilestoneRisk> MilestoneRisks { get; set; } = new List<MilestoneRisk>();
    public ICollection<Dependency> DependenciesAsSource { get; set; } = new List<Dependency>();
    public ICollection<Dependency> DependenciesAsTarget { get; set; } = new List<Dependency>();
    public ICollection<RiskRiskType> RiskRiskTypes { get; set; } = new List<RiskRiskType>();
}

