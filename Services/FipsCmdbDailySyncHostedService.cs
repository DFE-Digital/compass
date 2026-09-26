using Compass.Helpers;
using Compass.Models.Fips;
using Compass.Services.Fips;
using Microsoft.Extensions.Options;

namespace Compass.Services;

/// <summary>Runs bulk CMDB → service register sync every 10 minutes, 7am–6pm UK, Monday to Friday.</summary>
public sealed class FipsCmdbDailySyncHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptionsMonitor<FipsSyncConfiguration> _options;
    private readonly ILogger<FipsCmdbDailySyncHostedService> _logger;

    public FipsCmdbDailySyncHostedService(
        IServiceScopeFactory scopeFactory,
        IOptionsMonitor<FipsSyncConfiguration> options,
        ILogger<FipsCmdbDailySyncHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (FipsCmdbSyncSchedule.IsInsideWindow(UkDateTime.Now(), _options.CurrentValue))
                await RunJobSafelyAsync(stoppingToken);

            var delay = GetDelayUntilNextWake();
            var nextRunUk = UkDateTime.Now().Add(delay);
            _logger.LogInformation(
                "CMDB sync next run at {NextRunUk:yyyy-MM-dd HH:mm} UK (in {Delay})",
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
        }
    }

    private async Task RunJobSafelyAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IFipsCmdbDailySyncService>();
            await service.RunDailySyncIfDueAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Host shutdown
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Scheduled CMDB sync job failed");
        }
    }

    private TimeSpan GetDelayUntilNextWake()
    {
        var nowUk = UkDateTime.Now();
        var nextRunUk = FipsCmdbSyncSchedule.NextWakeUk(nowUk, _options.CurrentValue);
        var delay = UkDateTime.ToUtc(nextRunUk) - DateTime.UtcNow;
        return delay > TimeSpan.Zero ? delay : TimeSpan.FromSeconds(15);
    }
}
