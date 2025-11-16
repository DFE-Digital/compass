using Microsoft.AspNetCore.Mvc.Rendering;

namespace Compass.ViewModels;

public class ProjectMilestoneFormViewModel
{
    public ProjectMilestoneInputModel Input { get; set; } = new();

    public string ProjectTitle { get; set; } = string.Empty;

    public ProjectSummaryViewModel ProjectSummary { get; set; } = new();

    public IEnumerable<SelectListItem> StatusOptions { get; set; } = Enumerable.Empty<SelectListItem>();

    public IEnumerable<SelectListItem> ObjectiveOptions { get; set; } = Enumerable.Empty<SelectListItem>();

    public bool ShowDeleteButton { get; set; }
}
