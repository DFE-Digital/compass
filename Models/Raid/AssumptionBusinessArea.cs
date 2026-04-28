using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

/// <summary>Junction: an assumption can be tagged with multiple <see cref="BusinessAreaLookup"/> rows.</summary>
public class AssumptionBusinessArea
{
    public int AssumptionId { get; set; }

    [ForeignKey(nameof(AssumptionId))]
    public Assumption Assumption { get; set; } = null!;

    public int BusinessAreaLookupId { get; set; }

    [ForeignKey(nameof(BusinessAreaLookupId))]
    public BusinessAreaLookup BusinessAreaLookup { get; set; } = null!;
}
