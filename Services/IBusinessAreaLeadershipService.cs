namespace Compass.Services;

/// <summary>DD / leadership assignments per business area (Admin → Business area leadership).</summary>
public interface IBusinessAreaLeadershipService
{
    /// <summary>Same scope check as delegated business area admins — DD for one of the given lookup ids.</summary>
    Task<bool> IsUserLeaderForAnyBusinessAreaAsync(
        int userId,
        IReadOnlyCollection<int> businessAreaLookupIds,
        CancellationToken cancellationToken = default);

    /// <summary>Active business area lookup ids where the user is recorded as DD / leadership.</summary>
    Task<IReadOnlyList<int>> GetLeadershipBusinessAreaLookupIdsAsync(
        int userId,
        CancellationToken cancellationToken = default);
}
