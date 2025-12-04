using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

public class ProjectProduct
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int ProjectId { get; set; }

    [ForeignKey(nameof(ProjectId))]
    public Project Project { get; set; } = null!;

    [Required]
    [MaxLength(100)]
    public string ProductDocumentId { get; set; } = string.Empty; // Product DocumentID from CMS (primary identifier)

    [MaxLength(50)]
    public string? ProductFipsId { get; set; } // Product FIPS ID (legacy, kept for backwards compatibility)

    [Required]
    [MaxLength(200)]
    public string ProductTitle { get; set; } = string.Empty;

    public string? ProductDescription { get; set; }

    [MaxLength(500)]
    public string? ProductUrl { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
