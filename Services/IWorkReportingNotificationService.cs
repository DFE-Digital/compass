namespace Compass.Services;

/// <summary>Sends Compass work reporting reminder/chase emails based on admin notification settings.</summary>
public interface IWorkReportingNotificationService
{
    /// <summary>Evaluates reporting periods for today and sends any due open/reminder/overdue notifications.</summary>
    Task ProcessDailyNotificationsAsync(CancellationToken cancellationToken = default);
}
