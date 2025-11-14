using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

public class RiskTag
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int RiskId { get; set; }

    [ForeignKey(nameof(RiskId))]
    public Risk Risk { get; set; } = null!;

    [Required]
    [MaxLength(100)]
    public string Value { get; set; } = string.Empty;
}
