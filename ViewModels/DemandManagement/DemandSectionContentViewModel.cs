using System;
using System.Collections.Generic;
using Compass.Models;

namespace Compass.ViewModels.DemandManagement;

public class DemandSectionContentViewModel
{
    public DemandWorkflowViewModel Workflow { get; set; } = null!;

    public IReadOnlyList<SectionDefinition> RequestSections { get; set; } = new List<SectionDefinition>();

    public IReadOnlyList<SectionDefinition> AssessmentSections { get; set; } = new List<SectionDefinition>();

    public SectionStatusPanelViewModel SectionStatus { get; set; } = new();

    public string? SuccessMessage { get; set; }

    public string? ErrorMessage { get; set; }
}

public class DemandSectionDetailViewModel
{
    public DemandRequest Request { get; set; } = null!;

    public SectionStatusPanelViewModel Status { get; set; } = new();

    public IEnumerable<string> BusinessAreas { get; set; } = Array.Empty<string>();

    public IEnumerable<string> MissionPillars { get; set; } = Array.Empty<string>();

    public IEnumerable<string> StrategicObjectives { get; set; } = Array.Empty<string>();

    public IEnumerable<RiskType> RiskTypes { get; set; } = Array.Empty<RiskType>();

    public string ActiveSectionKey { get; set; } = string.Empty;
}

public record SectionDefinition(string Key, string Name);

