using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Compass.ViewModels;

public class ProjectNeedFormViewModel
{
    public ProjectNeedInputModel Input { get; set; } = new();

    public string ProjectTitle { get; set; } = string.Empty;

    public ProjectSummaryViewModel ProjectSummary { get; set; } = new();

    public IEnumerable<SelectListItem> ValidatedOptions { get; set; } = Enumerable.Empty<SelectListItem>();

    public bool ShowDeleteButton { get; set; }
}

