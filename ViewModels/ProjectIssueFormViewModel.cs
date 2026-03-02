using System.Linq;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Compass.ViewModels;

public class ProjectIssueFormViewModel
{
    public ProjectIssueInputModel Input { get; set; } = new();

    public string ProjectTitle { get; set; } = string.Empty;

    public ProjectSummaryViewModel ProjectSummary { get; set; } = new();

    public string? ContextDescription { get; set; }

    public IEnumerable<SelectListItem> SeverityOptions { get; set; } = Enumerable.Empty<SelectListItem>();

    public IEnumerable<SelectListItem> StatusOptions { get; set; } = Enumerable.Empty<SelectListItem>();

    public IEnumerable<SelectListItem> RiskOptions { get; set; } = Enumerable.Empty<SelectListItem>();

    public IEnumerable<SelectListItem> ActionOptions { get; set; } = Enumerable.Empty<SelectListItem>();

    public IEnumerable<SelectListItem> DecisionOptions { get; set; } = Enumerable.Empty<SelectListItem>();

    public IEnumerable<SelectListItem> BusinessAreaOptions { get; set; } = Enumerable.Empty<SelectListItem>();

    public bool ShowDeleteButton { get; set; }
}
