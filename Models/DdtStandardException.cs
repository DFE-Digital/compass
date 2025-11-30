using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

/// <summary>
/// Known exceptions to DDT Standards
/// Represents cases where a standard does not apply or has been granted an exception
/// </summary>
public class DdtStandardException
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int DdtStandardId { get; set; }

    [ForeignKey(nameof(DdtStandardId))]
    public DdtStandard DdtStandard { get; set; } = null!;

    /// <summary>
    /// Exception title/name
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description of the exception
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? Description { get; set; }

    /// <summary>
    /// Reason for the exception
    /// </summary>
    [MaxLength(2000)]
    public string? Reason { get; set; }

    /// <summary>
    /// Product or service this exception applies to (optional)
    /// Links to StandardProduct if applicable
    /// </summary>
    public int? StandardProductId { get; set; }

    [ForeignKey(nameof(StandardProductId))]
    public StandardProduct? StandardProduct { get; set; }

    /// <summary>
    /// FIPS Product ID if this exception applies to a specific FIPS product
    /// </summary>
    [MaxLength(50)]
    public string? FipsProductId { get; set; }

    /// <summary>
    /// Exception status: Active, Expired, Revoked
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Active"; // Active, Expired, Revoked

    /// <summary>
    /// When the exception was granted
    /// </summary>
    [Required]
    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the exception expires (if applicable)
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// User who granted the exception
    /// </summary>
    public int? GrantedByUserId { get; set; }

    [ForeignKey(nameof(GrantedByUserId))]
    public User? GrantedByUser { get; set; }

    /// <summary>
    /// User who created this exception record
    /// </summary>
    public int? CreatedByUserId { get; set; }

    [ForeignKey(nameof(CreatedByUserId))]
    public User? CreatedByUser { get; set; }

    /// <summary>
    /// Additional notes or conditions
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? Notes { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

