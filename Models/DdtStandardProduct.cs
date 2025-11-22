using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

/// <summary>
/// Junction table linking DDT Standards to Products (many-to-many)
/// Represents whether a product is approved or tolerated for a standard
/// </summary>
public class DdtStandardProduct
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int DdtStandardId { get; set; }

    [ForeignKey(nameof(DdtStandardId))]
    public DdtStandard DdtStandard { get; set; } = null!;

    [Required]
    public int StandardProductId { get; set; }

    [ForeignKey(nameof(StandardProductId))]
    public StandardProduct StandardProduct { get; set; } = null!;

    /// <summary>
    /// Product type: "Approved" or "Tolerated"
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string ProductType { get; set; } = "Approved"; // Approved, Tolerated

    /// <summary>
    /// Optional notes about why this product is approved/tolerated for this standard
    /// </summary>
    [MaxLength(1000)]
    public string? Notes { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

