using System;

namespace Compass.ViewModels.DemandManagement;

public class DemandWorkflowActivityViewModel
{
    public DateTime Timestamp { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Actor { get; set; }
    public string Type { get; set; } = "note"; // note, status, assessment, attachment, system
    public string Icon { get; set; } = "fas fa-comment";
}

