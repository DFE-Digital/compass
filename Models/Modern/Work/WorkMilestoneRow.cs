namespace Compass.Models.Modern.Work;

/// <summary>Milestone row for work dashboard tabs (compass-2 used Milestone + WorkItem navigation).</summary>
public class WorkMilestoneRow
{
    public int WorkItemId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public string? Status { get; set; }
    public WorkItem? WorkItem { get; set; }
}
