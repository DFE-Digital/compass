using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

public class ProjectStatusUpdate
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int ProjectId { get; set; }

    [ForeignKey(nameof(ProjectId))]
    public Project Project { get; set; } = null!;

    [Required]
    public string Narrative { get; set; } = string.Empty;

    // EntraUser fields
    public string? CreatedByEntraId { get; set; }

    public string? CreatedByName { get; set; }

    public string? CreatedByEmail { get; set; }

    // Legacy User fields (nullable for migration compatibility)
    public int? CreatedByUserId { get; set; }

    [ForeignKey(nameof(CreatedByUserId))]
    public User? CreatedByUser { get; set; }

    public int? UpdatedByUserId { get; set; }

    [ForeignKey(nameof(UpdatedByUserId))]
    public User? UpdatedByUser { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public bool IsBulkImport { get; set; } = false;
}

