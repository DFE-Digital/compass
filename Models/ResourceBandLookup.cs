using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

/// <summary>Configurable resourcing bands for reporting totals (FTE + MSC/FTE).</summary>
public class ResourceBandLookup
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>Inclusive lower bound for the band.</summary>
    [Column(TypeName = "decimal(9,2)")]
    public decimal MinFte { get; set; }

    /// <summary>Inclusive upper bound. Null means no upper limit.</summary>
    [Column(TypeName = "decimal(9,2)")]
    public decimal? MaxFte { get; set; }

    /// <summary>DfE badge color modifier (e.g. green, blue, orange, red).</summary>
    [MaxLength(50)]
    public string? CssClass { get; set; }

    public int SortOrder { get; set; } = 0;

    public bool IsActive { get; set; } = true;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
