using Compass.Models;

namespace Compass.Services;

public interface IUserDirectoryService
{
    Task<User> EnsureUserAsync(Guid objectId, CancellationToken cancellationToken = default);

    Task<User?> GetByObjectIdAsync(Guid objectId, CancellationToken cancellationToken = default);
}


