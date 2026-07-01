namespace Compass.Services;

/// <summary>Runs work reporting notification checks once per day at 08:00 UK time.</summary>
public sealed class WorkReportingNotificationHostedService : BackgroundService
{
    private static readonly TimeSpan RunAtUkTimeOfDay = TimeSpan.FromHours(8);

    private static TimeZoneInfo UkTimeZone =>
        TimeZoneInfo.FindSystemTimeZoneById(
            OperatingSystem.IsWindows() ? "GMT Standard Time" : "Europe/London");

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WorkReportingNotificationHostedService> _logger;

    public WorkReportingNotificationHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<WorkReportingNotificationHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // If the app starts after today's 08:00 UK, run once now (per-recipient dedupe avoids double sends).
        if (GetUkNow().TimeOfDay >= RunAtUkTimeOfDay)
            await RunJobSafelyAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = GetDelayUntilNextUkRunTime();
            var nextRunUk = GetUkNow().Add(delay);
            _logger.LogInformation(
                "Work reporting notifications next run at {NextRunUk:yyyy-MM-dd HH:mm} UK (in {Delay})",
                nextRunUk,
                delay);

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            await RunJobSafelyAsync(stoppingToken);
        }
    }

    private async Task RunJobSafelyAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IWorkReportingNotificationService>();
            await service.ProcessDailyNotificationsAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Host shutdown
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Work reporting notification daily job failed");
        }
    }

    private static DateTime GetUkNow() =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, UkTimeZone);

    private static TimeSpan GetDelayUntilNextUkRunTime()
    {
        var nowUk = GetUkNow();
        var nextRunUk = nowUk.Date + RunAtUkTimeOfDay;
        if (nowUk >= nextRunUk)
            nextRunUk = nextRunUk.AddDays(1);

        var nextRunUtc = TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(nextRunUk, DateTimeKind.Unspecified),
            UkTimeZone);
        var delay = nextRunUtc - DateTime.UtcNow;
        return delay > TimeSpan.Zero ? delay : TimeSpan.FromMinutes(1);
    }
}
