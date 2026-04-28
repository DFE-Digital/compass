using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

/// <summary>Junction: a risk can be tagged with multiple <see cref="RiskCategory"/> rows.</summary>
public class RiskRiskCategory
{
    public int RiskId { get; set; }

    [ForeignKey(nameof(RiskId))]
    public Risk Risk { get; set; } = null!;

    public int RiskCategoryId { get; set; }

    [ForeignKey(nameof(RiskCategoryId))]
    public RiskCategory RiskCategory { get; set; } = null!;
}
