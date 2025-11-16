using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;

namespace Compass.ViewModels;

public class ProjectDependencyFormViewModel
{
    public ProjectDependencyInputModel Input { get; set; } = new();

    public string ProjectTitle { get; set; } = string.Empty;

    public ProjectSummaryViewModel ProjectSummary { get; set; } = new();

    public IEnumerable<SelectListItem> TargetEntityTypeOptions { get; set; } = new List<SelectListItem>();

    public IEnumerable<SelectListItem> DependencyTypeOptions { get; set; } = new List<SelectListItem>();

    public IEnumerable<SelectListItem> StatusOptions { get; set; } = new List<SelectListItem>();

    public IDictionary<string, IReadOnlyList<SelectListItem>> TargetOptionsByType { get; set; } = new Dictionary<string, IReadOnlyList<SelectListItem>>(StringComparer.OrdinalIgnoreCase);

    public string? SelectedEntitySummary { get; set; }

    public bool ShowDeleteButton { get; set; }
}
