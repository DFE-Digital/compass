using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

public class MilestoneRisk
{
    [Required]
    public int MilestoneId { get; set; }

    [ForeignKey(nameof(MilestoneId))]
    public Milestone Milestone { get; set; } = null!;

    [Required]
    public int RiskId { get; set; }

    [ForeignKey(nameof(RiskId))]
    public Risk Risk { get; set; } = null!;
}

