namespace Compass.ViewModels.Modern;

public sealed class ModernRaidRegisterCoverageReportViewModel
{
    public int RegisterCount { get; init; }

    public int InScopeWorkItemCount { get; init; }
    public int UntrackedWorkItemCount { get; init; }
    public IReadOnlyList<RaidRegisterCoverageWorkItemRow> UntrackedWorkItems { get; init; } =
        Array.Empty<RaidRegisterCoverageWorkItemRow>();

    public int InScopeServiceCount { get; init; }
    public int UntrackedServiceCount { get; init; }
    public IReadOnlyList<RaidRegisterCoverageServiceRow> UntrackedServices { get; init; } =
        Array.Empty<RaidRegisterCoverageServiceRow>();
}

public sealed class RaidRegisterCoverageWorkItemRow
{
    public int Id { get; init; }
    public string Title { get; init; } = "";
    public string Status { get; init; } = "";
    public string BusinessAreaName { get; init; } = "—";
    public string DetailUrl { get; init; } = "#";
}

public sealed class RaidRegisterCoverageServiceRow
{
    public int ServiceId { get; init; }
    public string Name { get; init; } = "";
    public string? CmdbId { get; init; }
    public string? DetailUrl { get; init; }
}
