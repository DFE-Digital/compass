using Compass.Models;

namespace Compass.ViewModels;

public class ProjectDependencyDetailsViewModel
{
    public int ProjectId { get; set; }

    public string ProjectTitle { get; set; } = string.Empty;

    public Dependency Dependency { get; set; } = null!;

    public string SourceDisplay { get; set; } = string.Empty;

    public string TargetDisplay { get; set; } = string.Empty;

    public string? Description => string.IsNullOrWhiteSpace(Dependency.Description) ? null : Dependency.Description;

    public ProjectSummaryViewModel ProjectSummary { get; set; } = new();
}
