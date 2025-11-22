using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

/// <summary>
/// Represents a product that can be approved or tolerated for use with standards.
/// These are different from FIPS products (like Microsoft Office, Heroku, etc.)
/// </summary>
public class StandardProduct
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    [MaxLength(200)]
    public string? Provider { get; set; }

    [MaxLength(100)]
    public string? Version { get; set; }

    /// <summary>
    /// Approval status: Pending, Approved, Rejected
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string ApprovalStatus { get; set; } = "Pending"; // Pending, Approved, Rejected

    /// <summary>
    /// Optional link to a DfE product from FIPS (e.g., DfE Sign-in)
    /// This links to the FIPS product ID
    /// </summary>
    [MaxLength(50)]
    public string? DfeFipsProductId { get; set; }

    /// <summary>
    /// Optional link to DfE product name for display
    /// </summary>
    [MaxLength(200)]
    public string? DfeProductName { get; set; }

    /// <summary>
    /// User who created this product
    /// </summary>
    public int? CreatedByUserId { get; set; }

    [ForeignKey(nameof(CreatedByUserId))]
    public User? CreatedByUser { get; set; }

    /// <summary>
    /// User who approved/rejected this product
    /// </summary>
    public int? ReviewedByUserId { get; set; }

    [ForeignKey(nameof(ReviewedByUserId))]
    public User? ReviewedByUser { get; set; }

    public DateTime? ReviewedAt { get; set; }

    [MaxLength(1000)]
    public string? ReviewNotes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<DdtStandardProduct> StandardProducts { get; set; } = new List<DdtStandardProduct>();
}

