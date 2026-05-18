using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

/// <summary>Junction: an issue can be tagged with multiple <see cref="BusinessAreaLookup"/> rows.</summary>
public class IssueBusinessArea
{
    public int IssueId { get; set; }

    [ForeignKey(nameof(IssueId))]
    public Issue Issue { get; set; } = null!;

    public int BusinessAreaLookupId { get; set; }

    [ForeignKey(nameof(BusinessAreaLookupId))]
    public BusinessAreaLookup BusinessAreaLookup { get; set; } = null!;
}
