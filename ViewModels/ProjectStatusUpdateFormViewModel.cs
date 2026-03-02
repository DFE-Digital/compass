using System.ComponentModel.DataAnnotations;

namespace Compass.ViewModels;

public class ProjectStatusUpdateFormViewModel
{
    public ProjectStatusUpdateInputModel Input { get; set; } = new();

    public string ProjectTitle { get; set; } = string.Empty;

    public ProjectSummaryViewModel ProjectSummary { get; set; } = new();

    public string? CreatedByName { get; set; }

    public string? CreatedByEmail { get; set; }

    public DateTime? CreatedAt { get; set; }
}

public class ProjectStatusUpdateInputModel
{
    [Required]
    public int ProjectId { get; set; }

    public int? StatusUpdateId { get; set; }

    [Required]
    public string Narrative { get; set; } = string.Empty;

    public DateTime? CreatedAt { get; set; }

    public string? CreatedByEntraId { get; set; }

    public string? CreatedByEmail { get; set; }

    public string? CreatedByName { get; set; }
}

