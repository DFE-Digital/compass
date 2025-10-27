using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

public class ProjectRagHistory
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int ProjectId { get; set; }

    [ForeignKey(nameof(ProjectId))]
    public Project Project { get; set; } = null!;

    [Required]
    [MaxLength(20)]
    public string RagStatus { get; set; } = string.Empty; // Green, Amber-Green, Amber-Red, Red

    public string? Justification { get; set; }

    public string? PathToGreen { get; set; }

    [Required]
    [MaxLength(255)]
    public string ChangedByEmail { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? ChangedByName { get; set; }

    [Required]
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}
