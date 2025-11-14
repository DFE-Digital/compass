using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

public class ActionTag
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int ActionId { get; set; }

    [ForeignKey(nameof(ActionId))]
    public Action Action { get; set; } = null!;

    [Required]
    [MaxLength(100)]
    public string Value { get; set; } = string.Empty;
}
