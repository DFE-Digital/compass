namespace Compass.Services.Fips;

/// <summary>Keeps <see cref="Compass.Models.Fips.FipsDirectorate"/> rows aligned with <see cref="Compass.Models.DirectorateLookup"/>.</summary>
public interface IFipsDirectorateLookupSyncService
{
    Task SyncFromDirectorateLookupsAsync(CancellationToken cancellationToken = default);

    Task<int[]> ResolveToFipsDirectorateIdsAsync(int[] directorateLookupIds, CancellationToken cancellationToken = default);
}
