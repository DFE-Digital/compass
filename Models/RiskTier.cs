using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

public class RiskTier
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [MaxLength(200)]
    public string? Summary { get; set; }

    public int SortOrder { get; set; } = 0;

    /// <summary>
    /// 1 = highest governance intensity (typically “Tier 1”), larger values = lower intensity.
    /// Used for escalation/de-escalation direction. When 0, level is inferred from ordering among non-proposed tiers (see <see cref="RiskTierGovernance.ResolveLevel"/>).
    /// </summary>
    public int GovernanceLevel { get; set; }

    /// <summary>When true, this tier is only for pending tier-change requests (Operations), not for assigning directly on risks.</summary>
    public bool IsProposedTier { get; set; }

    public bool IsActive { get; set; } = true;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

