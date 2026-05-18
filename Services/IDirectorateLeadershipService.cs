namespace Compass.Services;

/// <summary>Users assigned as directorate leadership in Admin (DivisionUsers) — scope for work and RAID like business area admins.</summary>
public interface IDirectorateLeadershipService
{
    /// <summary>
    /// True when the user is a leader of a <see cref="Compass.Models.Division"/> that either appears on the work item
    /// or is linked to one of the item&apos;s business areas via <see cref="Compass.Models.DivisionBusinessArea"/>.
    /// </summary>
    Task<bool> IsUserDirectorateLeaderForProjectContextAsync(
        int userId,
        IReadOnlyCollection<int> projectDivisionIds,
        IReadOnlyCollection<int> businessAreaLookupIds,
        CancellationToken cancellationToken = default);
}
