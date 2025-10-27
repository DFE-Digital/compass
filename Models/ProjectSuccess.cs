using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

public class ProjectSuccess
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int ProjectId { get; set; }

    [ForeignKey(nameof(ProjectId))]
    public Project Project { get; set; } = null!;

    [Required]
    public string SuccessDescription { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string RecordedByEmail { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? RecordedByName { get; set; }

    [Required]
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

    public bool IsReportedToSlt { get; set; } = false;
}
