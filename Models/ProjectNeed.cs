using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

public class ProjectNeed
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int ProjectId { get; set; }

    [ForeignKey(nameof(ProjectId))]
    public Project Project { get; set; } = null!;

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(1000)]
    public string Need { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Source { get; set; }

    [Required]
    [MaxLength(20)]
    public string Validated { get; set; } = "No"; // Yes, No, Partially

    public int SortOrder { get; set; } = 0;

    [MaxLength(320)]
    public string? CreatedByEmail { get; set; }

    [MaxLength(200)]
    public string? CreatedByName { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

