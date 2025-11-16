using System.ComponentModel.DataAnnotations;

namespace Compass.ViewModels;

public class ProjectMilestoneInputModel
{
    [Required]
    public int ProjectId { get; set; }

    public int? MilestoneId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    [Required]
    [DataType(DataType.Date)]
    public DateTime? DueDate { get; set; }

    [DataType(DataType.Date)]
    public DateTime? ActualDate { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "not_started";

    [Range(0, 100)]
    public int? ProgressPercent { get; set; }

    public int? ObjectiveId { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }
}
