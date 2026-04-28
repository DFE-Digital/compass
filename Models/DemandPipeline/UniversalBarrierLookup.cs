using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models.DemandPipeline;

/// <summary>
/// Admin-managed options aligned with GOV.UK universal barriers to using a service
/// (inclusive design — barriers that can affect any user).
/// </summary>
[Table("UniversalBarrierLookups")]
public class UniversalBarrierLookup
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>Optional link to GOV.UK or anchor (e.g. universal barriers section).</summary>
    [MaxLength(500)]
    public string? GuidanceUrl { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
