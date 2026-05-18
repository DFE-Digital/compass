using Compass.Data;
using Compass.Models;
using Microsoft.EntityFrameworkCore;

namespace Compass.Services;

public sealed class CompassNotificationSettingsService : ICompassNotificationSettingsService
{
    private readonly CompassDbContext _db;

    public CompassNotificationSettingsService(CompassDbContext db) => _db = db;

    public async Task<bool> IsEventEnabledAsync(string eventKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(eventKey))
            return false;
        var row = await _db.CompassNotificationSettings.AsNoTracking()
            .FirstOrDefaultAsync(s => s.EventKey == eventKey, cancellationToken);
        return row is { IsEnabled: true };
    }

    public async Task<CompassNotificationRecipientFlags> GetRecipientFlagsAsync(
        string eventKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(eventKey))
            return CompassNotificationRecipientFlags.None;
        var row = await _db.CompassNotificationSettings.AsNoTracking()
            .FirstOrDefaultAsync(s => s.EventKey == eventKey, cancellationToken);
        if (row is not { IsEnabled: true })
            return CompassNotificationRecipientFlags.None;
        return (CompassNotificationRecipientFlags)row.RecipientFlags;
    }
}
