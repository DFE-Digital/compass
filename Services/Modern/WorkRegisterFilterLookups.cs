using Compass.Models;
using Compass.Models.Modern.Work;

namespace Compass.Services.Modern;

/// <summary>Cached filter dropdown data for the work register.</summary>
public sealed class WorkRegisterFilterLookups
{
    public IReadOnlyList<Portfolio> Portfolios { get; init; } = Array.Empty<Portfolio>();
    public IReadOnlyList<BusinessAreaLookup> BusinessAreas { get; init; } = Array.Empty<BusinessAreaLookup>();
    public IReadOnlyList<Directorate> Directorates { get; init; } = Array.Empty<Directorate>();
    public IReadOnlyList<WorkLookupOption> PhaseOptions { get; init; } = Array.Empty<WorkLookupOption>();
    public IReadOnlyList<RagStatusLookupOption> RagOptions { get; init; } = Array.Empty<RagStatusLookupOption>();
    public IReadOnlyList<WorkLookupOption> PriorityOptions { get; init; } = Array.Empty<WorkLookupOption>();
    public IReadOnlyList<WorkLookupOption> TagOptions { get; init; } = Array.Empty<WorkLookupOption>();
    public int RedRagStatusId { get; init; }
}

public sealed record WorkRegisterStatusCounts(int Active, int Paused, int Completed, int Cancelled, int RagRed)
{
    public int ActivePaused => Active + Paused;

    public int TabTotal(string tabKey) => tabKey switch
    {
        "completed" => Completed,
        "cancelled" => Cancelled,
        "all" => Active + Paused + Completed + Cancelled,
        _ => Active + Paused,
    };
}
