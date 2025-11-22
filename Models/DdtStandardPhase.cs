using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

/// <summary>
/// Junction table linking DDT Standards to Phases (many-to-many relationship)
/// </summary>
public class DdtStandardPhase
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int DdtStandardId { get; set; }

    [ForeignKey(nameof(DdtStandardId))]
    public DdtStandard DdtStandard { get; set; } = null!;

    /// <summary>
    /// Phase this standard applies to (e.g., Discovery, Alpha, Beta, Live)
    /// </summary>
    [Required]
    public int PhaseLookupId { get; set; }

    [ForeignKey(nameof(PhaseLookupId))]
    public PhaseLookup PhaseLookup { get; set; } = null!;

    /// <summary>
    /// Whether this phase is enabled for this standard
    /// </summary>
    public bool Enabled { get; set; } = true;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

