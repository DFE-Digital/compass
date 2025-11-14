using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

public class IssueDecision
{
    public int IssueId { get; set; }

    [ForeignKey(nameof(IssueId))]
    public Issue Issue { get; set; } = null!;

    public int DecisionId { get; set; }

    [ForeignKey(nameof(DecisionId))]
    public Decision Decision { get; set; } = null!;
}

