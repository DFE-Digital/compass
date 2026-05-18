using Compass.Models;

namespace Compass.Services;

/// <summary>Reads default notification toggles and recipient flags from <see cref="CompassNotificationSetting"/> (used when sending emails).</summary>
public interface ICompassNotificationSettingsService
{
    /// <summary>Returns false when the event is missing, disabled, or not yet configured.</summary>
    Task<bool> IsEventEnabledAsync(string eventKey, CancellationToken cancellationToken = default);

    /// <summary>Recipient flags for the event; <see cref="CompassNotificationRecipientFlags.None"/> if disabled or missing.</summary>
    Task<CompassNotificationRecipientFlags> GetRecipientFlagsAsync(string eventKey, CancellationToken cancellationToken = default);
}
