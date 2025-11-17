namespace Compass.ViewModels;

public class ProjectProblemStatementFormViewModel
{
    public ProjectProblemStatementInputModel Input { get; set; } = new();

    public string ProjectTitle { get; set; } = string.Empty;

    public ProjectSummaryViewModel ProjectSummary { get; set; } = new();

    public bool ShowDeleteButton { get; set; }
}

