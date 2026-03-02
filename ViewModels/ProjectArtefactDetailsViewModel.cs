namespace Compass.ViewModels;

public class ProjectArtefactDetailsViewModel
{
    public int ProjectId { get; set; }

    public int ArtefactId { get; set; }

    public string ProjectTitle { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string Url { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public string? CreatedByName { get; set; }

    public string? CreatedByEmail { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? UpdatedByName { get; set; }

    public ProjectSummaryViewModel? ProjectSummary { get; set; }
}

