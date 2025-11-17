namespace Compass.ViewModels;

public class ProjectNeedDetailsViewModel
{
    public int ProjectId { get; set; }

    public int NeedId { get; set; }

    public string ProjectTitle { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Need { get; set; } = string.Empty;

    public string? Source { get; set; }

    public string Validated { get; set; } = string.Empty;

    public string? ValidationNotes { get; set; }

    public DateTime? ValidatedAt { get; set; }

    public int SortOrder { get; set; }

    public string? CreatedByEmail { get; set; }

    public string? CreatedByName { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public ProjectSummaryViewModel ProjectSummary { get; set; } = new();
}

