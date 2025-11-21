namespace Compass.ViewModels;

public class ProjectOutcomeDetailsViewModel
{
    public int ProjectId { get; set; }

    public int OutcomeId { get; set; }

    public string ProjectTitle { get; set; } = string.Empty;

    public string Outcome { get; set; } = string.Empty;

    public string MeasureOfSuccess { get; set; } = string.Empty;

    public string ConfidenceLevel { get; set; } = string.Empty;

    public string? ConfidenceExplanation { get; set; }

    public string? AchievementStatus { get; set; }

    public int SortOrder { get; set; }

    public string? CreatedByEmail { get; set; }

    public string? CreatedByName { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public RaidLinkSummaryViewModel RaidSummary { get; set; } = new();

    public ProjectSummaryViewModel ProjectSummary { get; set; } = new();
}
