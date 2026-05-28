using Compass.Models;

namespace Compass.Services;

public interface IHttpErrorEmailSettingsService
{
    Task<HttpErrorEmailSettings> GetOrCreateAsync(CancellationToken cancellationToken = default);

    Task<HttpErrorEmailSettings> SaveAsync(
        bool isEnabled,
        string? contactEmail,
        string? updatedByEmail,
        CancellationToken cancellationToken = default);
}
