using System.ComponentModel.DataAnnotations;

namespace Compass.ViewModels;

public class ProjectDependencyInputModel
{
    [Required]
    public int ProjectId { get; set; }

    public int? DependencyId { get; set; }

    [Required]
    [MaxLength(50)]
    public string SourceEntityType { get; set; } = "Project";

    [Required]
    public int SourceEntityId { get; set; }

    [Required]
    [MaxLength(50)]
    public string TargetEntityType { get; set; } = string.Empty;

    [Required]
    public int? TargetEntityId { get; set; }

    [MaxLength(50)]
    public string? TargetEntityTitle { get; set; }

    [MaxLength(50)]
    public string? DependencyType { get; set; }

    [MaxLength(30)]
    public string? Status { get; set; }

    public string? Description { get; set; }
}
