using Compass.Models;

namespace Compass.ViewModels;

/// <summary>Projects linked to a single priority outcome (Objective), for the Priority outcomes report.</summary>
public class PriorityOutcomesObjectiveGroupViewModel
{
    public Objective Objective { get; set; } = null!;
    public List<Project> Projects { get; set; } = new();
}
