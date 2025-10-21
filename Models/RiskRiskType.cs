using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

public class RiskRiskType
{
    [Required]
    public int RiskId { get; set; }

    [ForeignKey(nameof(RiskId))]
    public Risk Risk { get; set; } = null!;

    [Required]
    public int RiskTypeId { get; set; }

    [ForeignKey(nameof(RiskTypeId))]
    public RiskType RiskType { get; set; } = null!;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

