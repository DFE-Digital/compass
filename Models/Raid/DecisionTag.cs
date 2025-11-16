using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

public class DecisionTag
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int DecisionId { get; set; }

    [ForeignKey(nameof(DecisionId))]
    public Decision Decision { get; set; } = null!;

    [Required]
    [MaxLength(100)]
    public string Value { get; set; } = string.Empty;
}
