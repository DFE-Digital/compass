using Compass.Models;

namespace Compass.ViewModels;

public class SltWeeklyReportViewModel
{
    public int Year { get; set; }
    public int WeekNumber { get; set; }
    public DateTime WeekStartDate { get; set; }
    public DateTime WeekEndDate { get; set; }
    public List<ProjectSuccess> Successes { get; set; } = new();
    public List<Milestone> Milestones { get; set; } = new();
    public bool HasPreviousWeek { get; set; }
    public bool HasNextWeek { get; set; }
    public int? PreviousWeekYear { get; set; }
    public int? PreviousWeekNumber { get; set; }
    public int? NextWeekYear { get; set; }
    public int? NextWeekNumber { get; set; }
    public List<BusinessAreaSuccessGroup> BusinessAreaGroups { get; set; } = new();
}

public class BusinessAreaSuccessGroup
{
    public string BusinessAreaName { get; set; } = string.Empty;
    public int UpdateCount { get; set; }
    public List<ProjectSuccessGroup> ProjectGroups { get; set; } = new();
}

public class ProjectSuccessGroup
{
    public int ProjectId { get; set; }
    public string ProjectTitle { get; set; } = string.Empty;
    public string? ProjectCode { get; set; }
    public List<UnifiedSuccessItem> Successes { get; set; } = new();
}

public class UnifiedSuccessItem
{
    public string Type { get; set; } = string.Empty; // "Legacy" or "Weekly"
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string SuccessDescription { get; set; } = string.Empty;
    public string? CreatedByName { get; set; }
    public string? CreatedByEmail { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? SltResponse { get; set; }
    public string? SltRespondedByName { get; set; }
    public string? SltRespondedByEmail { get; set; }
    public DateTime? SltRespondedAt { get; set; }
}

