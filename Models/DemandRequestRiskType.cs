using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

public class DemandRequestRiskType
{
    [Required]
    public int DemandRequestId { get; set; }

    [ForeignKey(nameof(DemandRequestId))]
    public DemandRequest DemandRequest { get; set; } = null!;

    [Required]
    public int RiskTypeId { get; set; }

    [ForeignKey(nameof(RiskTypeId))]
    public RiskType RiskType { get; set; } = null!;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
