using System;
using System.Collections.Generic;
using Compass.Models;

namespace Compass.ViewModels.DemandManagement;

public class DemandWorkflowViewModel
{
    public DemandRequest Request { get; set; } = null!;
    public IEnumerable<DemandWorkflowStageViewModel> Stages { get; set; } = new List<DemandWorkflowStageViewModel>();
    public IEnumerable<DemandWorkflowTaskViewModel> Tasks { get; set; } = new List<DemandWorkflowTaskViewModel>();
    public IEnumerable<DemandWorkflowActivityViewModel> Activity { get; set; } = new List<DemandWorkflowActivityViewModel>();

    public string CurrentStageKey { get; set; } = "Draft";
    public string CurrentStageName { get; set; } = "Draft";
    public string CurrentStageSummary { get; set; } = string.Empty;

    public int CompletedTaskCount { get; set; }
    public int TotalTaskCount { get; set; }

    public bool IsDocumentView { get; set; }
    public string ActiveSectionKey { get; set; } = "Overview";

    public IDictionary<string, bool> SectionCompletionEligibility { get; set; } = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

    public IDictionary<string, IReadOnlyCollection<string>> SectionMissingFields { get; set; } = new Dictionary<string, IReadOnlyCollection<string>>(StringComparer.OrdinalIgnoreCase);

    public IDictionary<string, string?> SectionStatusMessages { get; set; } = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<string> BusinessAreas { get; set; } = Array.Empty<string>();
    public IReadOnlyCollection<string> MissionPillars { get; set; } = Array.Empty<string>();
    public IReadOnlyCollection<string> StrategicObjectives { get; set; } = Array.Empty<string>();
    public IReadOnlyCollection<RiskType> RiskTypes { get; set; } = Array.Empty<RiskType>();
    public IReadOnlyCollection<TriageMeeting> TriageMeetings { get; set; } = Array.Empty<TriageMeeting>();
}

