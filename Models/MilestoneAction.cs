using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

public class MilestoneAction
{
    [Required]
    public int MilestoneId { get; set; }

    [ForeignKey(nameof(MilestoneId))]
    public Milestone Milestone { get; set; } = null!;

    [Required]
    public int ActionId { get; set; }

    [ForeignKey(nameof(ActionId))]
    public Action Action { get; set; } = null!;
}

