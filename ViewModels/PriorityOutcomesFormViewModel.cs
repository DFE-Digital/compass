using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Compass.ViewModels;

public class PriorityOutcomesFormViewModel
{
    public PriorityOutcomesInputModel Input { get; set; } = new();

    public string ProjectTitle { get; set; } = string.Empty;

    public ProjectSummaryViewModel ProjectSummary { get; set; } = new();

    public IEnumerable<SelectListItem> ObjectiveOptions { get; set; } = new List<SelectListItem>();
}

public class PriorityOutcomesInputModel
{
    public int ProjectId { get; set; }

    public string? StrategicObjectives { get; set; }

    public List<int> SelectedObjectiveIds { get; set; } = new();
}

