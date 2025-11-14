using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Compass.ViewModels;

public class ProjectKpiFormViewModel
{
    public ProjectKpiInputModel Input { get; set; } = new();

    public string ProjectTitle { get; set; } = string.Empty;

    public ProjectSummaryViewModel ProjectSummary { get; set; } = new();

    public IEnumerable<SelectListItem> CategoryOptions { get; set; } = new List<SelectListItem>();

    public IEnumerable<SelectListItem> ReportingStageOptions { get; set; } = new List<SelectListItem>();

    public IEnumerable<SelectListItem> StatusOptions { get; set; } = new List<SelectListItem>();

    public IEnumerable<SelectListItem> UnitOfMeasureOptions { get; set; } = new List<SelectListItem>();

    public IEnumerable<SelectListItem> FrequencyOptions { get; set; } = new List<SelectListItem>();

    public IEnumerable<SelectListItem> DataSourceOptions { get; set; } = new List<SelectListItem>();

    public IEnumerable<SelectListItem> ObjectiveOptions { get; set; } = new List<SelectListItem>();

    public IEnumerable<SelectListItem> MilestoneOptions { get; set; } = new List<SelectListItem>();

    public bool ShowActiveToggle { get; set; }
}
