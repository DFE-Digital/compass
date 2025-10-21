using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

public class MilestoneIssue
{
    [Required]
    public int MilestoneId { get; set; }

    [ForeignKey(nameof(MilestoneId))]
    public Milestone Milestone { get; set; } = null!;

    [Required]
    public int IssueId { get; set; }

    [ForeignKey(nameof(IssueId))]
    public Issue Issue { get; set; } = null!;
}

