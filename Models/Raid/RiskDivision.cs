using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

/// <summary>Junction: a risk can be tagged with multiple <see cref="Division"/> rows.</summary>
public class RiskDivision
{
    public int RiskId { get; set; }

    [ForeignKey(nameof(RiskId))]
    public Risk Risk { get; set; } = null!;

    public int DivisionId { get; set; }

    [ForeignKey(nameof(DivisionId))]
    public Division Division { get; set; } = null!;
}
