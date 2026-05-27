namespace Compass.ViewModels.Modern;

/// <summary>Row data for a single RAID register spreadsheet Excel export.</summary>
public class RaidRegisterSpreadsheetExportModel
{
    public string RegisterName { get; set; } = string.Empty;
    public IReadOnlyList<RaidRegisterRiskRow> Risks { get; set; } = Array.Empty<RaidRegisterRiskRow>();
    public IReadOnlyList<RaidRegisterIssueRow> Issues { get; set; } = Array.Empty<RaidRegisterIssueRow>();
    public IReadOnlyList<RaidRegisterAssumptionRow> Assumptions { get; set; } = Array.Empty<RaidRegisterAssumptionRow>();
    public IReadOnlyList<RaidRegisterNearMissRow> NearMisses { get; set; } = Array.Empty<RaidRegisterNearMissRow>();
    public IReadOnlyList<RaidRegisterDependencyRow> Dependencies { get; set; } = Array.Empty<RaidRegisterDependencyRow>();
}
