using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

/// <summary>Junction: an assumption can be tagged with multiple <see cref="Division"/> rows.</summary>
public class AssumptionDivision
{
    public int AssumptionId { get; set; }

    [ForeignKey(nameof(AssumptionId))]
    public Assumption Assumption { get; set; } = null!;

    public int DivisionId { get; set; }

    [ForeignKey(nameof(DivisionId))]
    public Division Division { get; set; } = null!;
}
