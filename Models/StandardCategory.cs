using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

/// <summary>
/// Standard Category - top-level categorization for standards
/// </summary>
public class StandardCategory
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public int SortOrder { get; set; } = 0;

    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Sub-categories within this category
    /// </summary>
    public ICollection<StandardSubCategory> SubCategories { get; set; } = new List<StandardSubCategory>();

    /// <summary>
    /// Standards in this category (via junction table)
    /// </summary>
    public ICollection<DdtStandardCategory> DdtStandardCategories { get; set; } = new List<DdtStandardCategory>();

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

