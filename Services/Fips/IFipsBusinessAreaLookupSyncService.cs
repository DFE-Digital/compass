namespace Compass.Services.Fips;

/// <summary>Keeps <see cref="Compass.Models.Fips.FipsBusinessArea"/> rows aligned with <see cref="Compass.Models.BusinessAreaLookup"/>.</summary>
public interface IFipsBusinessAreaLookupSyncService
{
    /// <summary>
    /// Upserts FIPS business area rows for every business area lookup (mirrors name, description, sort order, active flag).
    /// Safe to call frequently.
    /// </summary>
    Task SyncFromBusinessAreaLookupsAsync(CancellationToken cancellationToken = default);

    /// <summary>Maps lookup ids selected on the Manage FIPS form to corresponding <see cref="Compass.Models.Fips.FipsBusinessArea"/> ids (creates rows if missing).</summary>
    Task<int[]> ResolveToFipsBusinessAreaIdsAsync(int[] businessAreaLookupIds, CancellationToken cancellationToken = default);
}
