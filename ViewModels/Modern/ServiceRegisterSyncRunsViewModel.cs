using Compass.Models.Fips;
using Compass.Services.Fips;

namespace Compass.ViewModels.Modern;

public sealed class ServiceRegisterSyncRunRow
{
    public int Id { get; init; }
    public DateTime StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public int? DurationSeconds { get; init; }
    public string Status { get; init; } = "";
    public string? InitiatedBy { get; init; }
    public int ProductsCreated { get; init; }
    public int ProductsUpdated { get; init; }
    public int ProductsSkipped { get; init; }
    public int ErrorsEncountered { get; init; }
}

public sealed class ServiceRegisterSyncRunsViewModel
{
    public FipsProductsViewModel SubNav { get; init; } = new() { ActiveTab = "sync" };
    public string Filter { get; init; } = "all";
    public int TotalCount { get; init; }
    public List<ServiceRegisterSyncRunRow> Runs { get; init; } = [];
    public GovUkPaginationViewModel? Pagination { get; init; }
}

public sealed class ServiceRegisterSyncRunDetailViewModel
{
    public FipsProductsViewModel SubNav { get; init; } = new() { ActiveTab = "sync" };
    public required ServiceRegisterSyncRunRow Run { get; init; }
    public string? ErrorDetails { get; init; }
    public FipsCmdbSyncRunReport Report { get; init; } = new();
}
