namespace Compass.ViewModels;

public class ProjectSuccessDetailsViewModel
{
    public int ProjectId { get; set; }

    public int SuccessId { get; set; }

    public string ProjectTitle { get; set; } = string.Empty;

    public string SuccessDescription { get; set; } = string.Empty;

    public string RecordedByEmail { get; set; } = string.Empty;

    public string? RecordedByName { get; set; }

    public DateTime RecordedAt { get; set; }

    public bool IsReportedToSlt { get; set; }

    public ProjectSummaryViewModel ProjectSummary { get; set; } = new();

    public bool CanDelete { get; set; } = true;
}
