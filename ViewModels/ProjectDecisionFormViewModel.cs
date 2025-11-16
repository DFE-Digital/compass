using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace Compass.ViewModels;

public class ProjectDecisionFormViewModel
{
    public ProjectDecisionInputModel Input { get; set; } = new();

    public string ProjectTitle { get; set; } = string.Empty;

    public ProjectSummaryViewModel ProjectSummary { get; set; } = new();

    public IEnumerable<SelectListItem> StatusOptions { get; set; } = new List<SelectListItem>();

    public IEnumerable<SelectListItem> BusinessAreaOptions { get; set; } = new List<SelectListItem>();

    public IEnumerable<SelectListItem> RiskOptions { get; set; } = new List<SelectListItem>();

    public IEnumerable<SelectListItem> IssueOptions { get; set; } = new List<SelectListItem>();

    public IEnumerable<SelectListItem> ActionOptions { get; set; } = new List<SelectListItem>();

    public bool ShowDeleteButton { get; set; }
}
