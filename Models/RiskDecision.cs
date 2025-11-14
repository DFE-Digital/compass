using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

public class RiskDecision
{
    public int RiskId { get; set; }

    [ForeignKey(nameof(RiskId))]
    public Risk Risk { get; set; } = null!;

    public int DecisionId { get; set; }

    [ForeignKey(nameof(DecisionId))]
    public Decision Decision { get; set; } = null!;
}

