namespace Compass.ViewModels;

public class ProjectStatusUpdateDetailsViewModel
{
    public int ProjectId { get; set; }

    public int StatusUpdateId { get; set; }

    public string ProjectTitle { get; set; } = string.Empty;

    public string Narrative { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public string? CreatedByName { get; set; }

    public string? CreatedByEmail { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? UpdatedByName { get; set; }

    public bool IsBulkImport { get; set; }

    public ProjectSummaryViewModel? ProjectSummary { get; set; }
}

