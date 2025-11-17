namespace Compass.ViewModels;

public class ProjectProblemStatementDetailsViewModel
{
    public int ProjectId { get; set; }

    public int ProblemStatementId { get; set; }

    public string ProjectTitle { get; set; } = string.Empty;

    public string ProblemStatement { get; set; } = string.Empty;

    public string? CreatedByEmail { get; set; }

    public string? CreatedByName { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public List<ProjectProblemStatementHistoryItemViewModel> History { get; set; } = new();

    public ProjectSummaryViewModel ProjectSummary { get; set; } = new();
}

public class ProjectProblemStatementHistoryItemViewModel
{
    public string ProblemStatement { get; set; } = string.Empty;

    public string? ChangedByEmail { get; set; }

    public string? ChangedByName { get; set; }

    public DateTime ChangedAt { get; set; }
}

