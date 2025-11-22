using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

/// <summary>
/// Junction table for DDT Standard sub-categories (many-to-many relationship)
/// </summary>
public class DdtStandardSubCategory
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int DdtStandardId { get; set; }

    [ForeignKey(nameof(DdtStandardId))]
    public DdtStandard DdtStandard { get; set; } = null!;

    /// <summary>
    /// Reference to StandardSubCategory
    /// </summary>
    [Required]
    public int SubCategoryId { get; set; }

    [ForeignKey(nameof(SubCategoryId))]
    public StandardSubCategory SubCategory { get; set; } = null!;

    /// <summary>
    /// External sub-category ID if synced from CMS (legacy)
    /// </summary>
    public int? ExternalSubCategoryId { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

