using System.ComponentModel.DataAnnotations;

namespace Compass.ViewModels;

public class ProjectArtefactFormViewModel
{
    public ProjectArtefactInputModel Input { get; set; } = new();

    public string ProjectTitle { get; set; } = string.Empty;

    public ProjectSummaryViewModel ProjectSummary { get; set; } = new();

    public string? CreatedByName { get; set; }

    public string? CreatedByEmail { get; set; }

    public DateTime? CreatedAt { get; set; }
}

public class ProjectArtefactInputModel
{
    [Required]
    public int ProjectId { get; set; }

    public int? ArtefactId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    [MaxLength(1000)]
    [Url]
    public string Url { get; set; } = string.Empty;

    public string? CreatedByEntraId { get; set; }

    public string? CreatedByEmail { get; set; }

    public string? CreatedByName { get; set; }
}

