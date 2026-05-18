using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

/// <summary>Junction: an issue can be tagged with multiple <see cref="Division"/> rows.</summary>
public class IssueDivision
{
    public int IssueId { get; set; }

    [ForeignKey(nameof(IssueId))]
    public Issue Issue { get; set; } = null!;

    public int DivisionId { get; set; }

    [ForeignKey(nameof(DivisionId))]
    public Division Division { get; set; } = null!;
}
