using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

/// <summary>Junction: an issue can sit under multiple <see cref="IssueCategory"/> labels.</summary>
public class IssueIssueCategory
{
    public int IssueId { get; set; }

    [ForeignKey(nameof(IssueId))]
    public Issue Issue { get; set; } = null!;

    public int IssueCategoryId { get; set; }

    [ForeignKey(nameof(IssueCategoryId))]
    public IssueCategory IssueCategory { get; set; } = null!;
}
