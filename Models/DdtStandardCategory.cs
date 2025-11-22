using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

/// <summary>
/// Junction table for DDT Standard categories (many-to-many relationship)
/// Categories are stored as lookup values or can reference external CMS categories
/// </summary>
public class DdtStandardCategory
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int DdtStandardId { get; set; }

    [ForeignKey(nameof(DdtStandardId))]
    public DdtStandard DdtStandard { get; set; } = null!;

    /// <summary>
    /// Reference to StandardCategory
    /// </summary>
    [Required]
    public int CategoryId { get; set; }

    [ForeignKey(nameof(CategoryId))]
    public StandardCategory Category { get; set; } = null!;

    /// <summary>
    /// External category ID if synced from CMS (legacy)
    /// </summary>
    public int? ExternalCategoryId { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

