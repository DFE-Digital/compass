using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

public class RiskAction
{
    [Required]
    public int RiskId { get; set; }

    [ForeignKey(nameof(RiskId))]
    public Risk Risk { get; set; } = null!;

    [Required]
    public int ActionId { get; set; }

    [ForeignKey(nameof(ActionId))]
    public Action Action { get; set; } = null!;
}

