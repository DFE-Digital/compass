namespace Compass.Services;

/// <summary>Compass notification emails for work item lifecycle events.</summary>
public interface IWorkItemNotificationService
{
    /// <summary>Sends work item created emails when enabled in admin notification settings.</summary>
    Task TrySendWorkItemCreatedAsync(
        int projectId,
        string creatorEmail,
        string? creatorName,
        CancellationToken cancellationToken = default);
}
