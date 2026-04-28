namespace Compass.Services;

public interface IBusinessAreaAdminService
{
    /// <summary>Returns true if the user is registered as an admin for any of the given business area ids.</summary>
    Task<bool> IsUserAdminForAnyBusinessAreaAsync(
        int userId,
        IReadOnlyCollection<int> businessAreaLookupIds,
        CancellationToken cancellationToken = default);

    /// <summary>Maps a Compass / FIPS display name (e.g. product &quot;Business area&quot; category) to an active lookup id.</summary>
    Task<int?> GetLookupIdForBusinessAreaDisplayNameAsync(string? businessAreaDisplayName, CancellationToken cancellationToken = default);

    /// <summary>Active business area lookup ids the user is registered to administer (see Business area admins in admin hub).</summary>
    Task<IReadOnlyList<int>> GetAdministeredBusinessAreaLookupIdsAsync(
        int userId,
        CancellationToken cancellationToken = default);
}
