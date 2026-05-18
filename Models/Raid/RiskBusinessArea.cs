using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

/// <summary>Junction: a risk can be tagged with multiple <see cref="BusinessAreaLookup"/> rows.</summary>
public class RiskBusinessArea
{
    public int RiskId { get; set; }

    [ForeignKey(nameof(RiskId))]
    public Risk Risk { get; set; } = null!;

    public int BusinessAreaLookupId { get; set; }

    [ForeignKey(nameof(BusinessAreaLookupId))]
    public BusinessAreaLookup BusinessAreaLookup { get; set; } = null!;
}
