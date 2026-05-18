namespace Compass.Services;

/// <summary>Persists audit rows for Compass notification emails. Call from send paths when implementing delivery.</summary>
public interface ICompassNotificationEmailLogService
{
    /// <param name="contextReference">Optional key e.g. <c>risk-12</c> for support / admin review.</param>
    Task LogAsync(
        string recipientEmail,
        string? recipientName,
        string eventKey,
        string subject,
        string body,
        bool sendSucceeded,
        string? errorMessage,
        string? contextReference,
        CancellationToken cancellationToken = default);
}
