namespace Compass.ViewModels;

/// <summary>RAID register report at <c>/modern/reporting/raid</c>.</summary>
public sealed class ModernRaidReportViewModel
{
  /// <summary>risks | issues | near-misses | dependencies | assumptions</summary>
  public string ActiveTab { get; init; } = "risks";

  public string FilterScopeSummary { get; init; } = "All business areas · All directorates";

  public int? FilterBusinessAreaId { get; init; }

  public int? FilterDirectorateId { get; init; }

  public IReadOnlyList<RaidReportFilterSelectOption> BusinessAreaFilterOptions { get; init; } = [];

  public IReadOnlyList<RaidReportFilterSelectOption> DirectorateFilterOptions { get; init; } = [];

  public RaidReportTabPanel Risks { get; init; } = RaidReportTabPanel.Empty;

  public RaidReportTabPanel Issues { get; init; } = RaidReportTabPanel.Empty;

  public RaidReportTabPanel NearMisses { get; init; } = RaidReportTabPanel.Empty;

  public RaidReportTabPanel Dependencies { get; init; } = RaidReportTabPanel.Empty;

  public RaidReportTabPanel Assumptions { get; init; } = RaidReportTabPanel.Empty;

  public RaidReportTabPanel ActivePanel => ActiveTab switch
  {
    "issues" => Issues,
    "near-misses" => NearMisses,
    "dependencies" => Dependencies,
    "assumptions" => Assumptions,
    _ => Risks
  };
}

public sealed class RaidReportTabPanel
{
  public static RaidReportTabPanel Empty { get; } = new();

  public IReadOnlyList<RaidReportStatCard> Stats { get; init; } = [];

  public IReadOnlyList<string> TableHeaders { get; init; } = [];

  public IReadOnlyList<RaidReportRegisterRow> Rows { get; init; } = [];

  public int Count => Rows.Count;
}

public sealed record RaidReportStatCard(string Label, string Value, string Tint = "dfe-f-stat-card--tint-grey");

public sealed record RaidReportRegisterRow(
  int Id,
  string Reference,
  string Title,
  IReadOnlyList<string> Cells,
  string DetailAction,
  string DetailController = "ModernRaid");
