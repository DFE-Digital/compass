using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

public class IssueAction
{
    [Required]
    public int IssueId { get; set; }

    [ForeignKey(nameof(IssueId))]
    public Issue Issue { get; set; } = null!;

    [Required]
    public int ActionId { get; set; }

    [ForeignKey(nameof(ActionId))]
    public Action Action { get; set; } = null!;
}

