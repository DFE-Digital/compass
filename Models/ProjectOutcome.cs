using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

public class ProjectOutcome
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int ProjectId { get; set; }

    [ForeignKey(nameof(ProjectId))]
    public Project Project { get; set; } = null!;

    [Required]
    [MaxLength(500)]
    public string Outcome { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string MeasureOfSuccess { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string ConfidenceLevel { get; set; } = "Medium"; // Low, Medium, High

    public string? ConfidenceExplanation { get; set; }

    public int SortOrder { get; set; } = 0;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
