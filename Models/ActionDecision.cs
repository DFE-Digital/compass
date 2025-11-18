using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

public class ActionDecision
{
    public int ActionId { get; set; }

    [ForeignKey(nameof(ActionId))]
    public Action Action { get; set; } = null!;

    public int DecisionId { get; set; }

    [ForeignKey(nameof(DecisionId))]
    public Decision Decision { get; set; } = null!;
}

