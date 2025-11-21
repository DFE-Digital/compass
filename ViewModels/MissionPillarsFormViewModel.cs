using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Compass.ViewModels;

public class MissionPillarsFormViewModel
{
    public MissionPillarsInputModel Input { get; set; } = new();

    public string ProjectTitle { get; set; } = string.Empty;

    public ProjectSummaryViewModel ProjectSummary { get; set; } = new();

    public IEnumerable<SelectListItem> MissionOptions { get; set; } = new List<SelectListItem>();
}

public class MissionPillarsInputModel
{
    public int ProjectId { get; set; }

    public string? MissionPillars { get; set; }

    public List<int> SelectedMissionIds { get; set; } = new();
}

