using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

/// <summary>
/// Captures a point-in-time snapshot of a risk's ratings whenever the Current
/// rating is changed. Enables tracking from Original → Current over time.
/// </summary>
public class RiskRatingHistory
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int RiskId { get; set; }

    [ForeignKey(nameof(RiskId))]
    public Risk Risk { get; set; } = null!;

    [Required]
    [MaxLength(20)]
    public string RatingType { get; set; } = "Current";

    public int? LikelihoodId { get; set; }

    [ForeignKey(nameof(LikelihoodId))]
    public RiskLikelihood? Likelihood { get; set; }

    public int? ImpactLevelId { get; set; }

    [ForeignKey(nameof(ImpactLevelId))]
    public RiskImpactLevel? ImpactLevel { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? Score { get; set; }

    [MaxLength(2000)]
    public string? Reason { get; set; }

    public int? ChangedByUserId { get; set; }

    [ForeignKey(nameof(ChangedByUserId))]
    public User? ChangedByUser { get; set; }

    [Required]
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}
