using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

/// <summary>
/// Audit log for unpublishing DDT Standards
/// </summary>
public class DdtStandardUnpublishAudit
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int DdtStandardId { get; set; }

    [ForeignKey(nameof(DdtStandardId))]
    public DdtStandard DdtStandard { get; set; } = null!;

    /// <summary>
    /// Version of the standard that was unpublished
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Reason for unpublishing
    /// </summary>
    [Required]
    [Column(TypeName = "nvarchar(max)")]
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// User who unpublished the standard
    /// </summary>
    [Required]
    public int UnpublishedByUserId { get; set; }

    [ForeignKey(nameof(UnpublishedByUserId))]
    public User UnpublishedByUser { get; set; } = null!;

    /// <summary>
    /// When the standard was unpublished
    /// </summary>
    [Required]
    public DateTime UnpublishedAt { get; set; } = DateTime.UtcNow;
}

