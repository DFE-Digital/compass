using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

public class IssueRisk
{
    public int IssueId { get; set; }

    [ForeignKey(nameof(IssueId))]
    public Issue Issue { get; set; } = null!;

    public int RiskId { get; set; }

    [ForeignKey(nameof(RiskId))]
    public Risk Risk { get; set; } = null!;
}

