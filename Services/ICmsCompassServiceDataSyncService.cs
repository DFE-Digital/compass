using Compass.ViewModels.Modern;

namespace Compass.Services;

public interface ICmsCompassServiceDataSyncService
{
    Task<CmsCompassSyncResultViewModel> ApplyCmsToCompassAsync(
        CmsCompassSyncRequest request,
        string actorEmail,
        string? auditDisplayName,
        CancellationToken cancellationToken = default);
}
