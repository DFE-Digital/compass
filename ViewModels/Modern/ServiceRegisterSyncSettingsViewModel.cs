using Compass.Models.Fips;
using Compass.Services.Fips;

namespace Compass.ViewModels.Modern;

public sealed class ServiceRegisterSyncSettingsViewModel
{
    public List<FipsCmdbSyncRule> Rules { get; init; } = [];

    /// <summary>Tab counts and active tab for service register sub-navigation.</summary>
    public FipsProductsViewModel SubNav { get; init; } = new() { ActiveTab = "sync" };

    public bool CanSyncFromCmdb { get; init; }

    public FipsCompletionImportResult? LastImportResult { get; init; }
}
