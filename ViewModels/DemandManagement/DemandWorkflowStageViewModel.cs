using System.Collections.Generic;

namespace Compass.ViewModels.DemandManagement;

public class DemandWorkflowStageViewModel
{
    public string Key { get; set; } = string.Empty; // Draft, New, Explore, etc.
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = "upcoming"; // complete, current, upcoming
    public string? Summary { get; set; }
    public IEnumerable<DemandWorkflowTaskViewModel> Tasks { get; set; } = new List<DemandWorkflowTaskViewModel>();
}

