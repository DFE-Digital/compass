using System;

namespace Compass.ViewModels.DemandManagement;

public class DemandWorkflowTaskViewModel
{
    public string Key { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty; // Request, Assessment, Outcome
    public string Status { get; set; } = "ToDo"; // ToDo, InProgress, Completed
    public string? Description { get; set; }
    public string? AssignedTo { get; set; }
    public DateTime? DueDate { get; set; }
    public string? Url { get; set; }
    public bool IsCurrent { get; set; }
}

