namespace Compass.ViewModels;

public class TasksViewModel
{
    public List<ProjectTask> Tasks { get; set; } = new();
}

public class ProjectTask
{
    public int ProjectId { get; set; }
    public string ProjectTitle { get; set; } = string.Empty;
    public string ProjectCode { get; set; } = string.Empty;
    public string TaskDescription { get; set; } = string.Empty;
    public string ActionUrl { get; set; } = string.Empty;
    public string ProjectOverviewUrl { get; set; } = string.Empty;
}

