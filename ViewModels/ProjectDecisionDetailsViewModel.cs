using System.Collections.Generic;
using Compass.Models;

namespace Compass.ViewModels;

public class ProjectDecisionDetailsViewModel
{
    public int ProjectId { get; set; }

    public string ProjectTitle { get; set; } = string.Empty;

    public Decision Decision { get; set; } = null!;

    public IReadOnlyCollection<Risk> LinkedRisks { get; set; } = new List<Risk>();

    public IReadOnlyCollection<Issue> LinkedIssues { get; set; } = new List<Issue>();

    public IReadOnlyCollection<Compass.Models.Action> LinkedActions { get; set; } = new List<Compass.Models.Action>();
}
