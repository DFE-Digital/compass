using System.Text;
using Compass.Data;
using Compass.Helpers;
using Compass.Models;
using Compass.Models.Fips;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Compass.Services.Fips;

public sealed class FipsCmdbDailySyncService : IFipsCmdbDailySyncService
{
    public const string EmailEventKey = "fips_cmdb_daily_sync";
    private const int MaxNewProductsInEmail = 40;

    private readonly CompassDbContext _db;
    private readonly IFipsCmdbProductSyncService _sync;
    private readonly INotificationService _notificationService;
    private readonly ICompassNotificationEmailLogService _emailLog;
    private readonly IGlobalFeatureToggleService _features;
    private readonly IOptions<FipsSyncConfiguration> _options;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<FipsCmdbDailySyncService> _logger;

    public FipsCmdbDailySyncService(
        CompassDbContext db,
        IFipsCmdbProductSyncService sync,
        INotificationService notificationService,
        ICompassNotificationEmailLogService emailLog,
        IGlobalFeatureToggleService features,
        IOptions<FipsSyncConfiguration> options,
        IConfiguration configuration,
        IHostEnvironment environment,
        ILogger<FipsCmdbDailySyncService> logger)
    {
        _db = db;
        _sync = sync;
        _notificationService = notificationService;
        _emailLog = emailLog;
        _features = features;
        _options = options;
        _configuration = configuration;
        _environment = environment;
        _logger = logger;
    }

    public async Task<FipsCmdbDailySyncOutcome> RunDailySyncIfDueAsync(CancellationToken cancellationToken = default)
    {
        var config = _options.Value;
        if (!config.DailySyncEnabled)
            return FipsCmdbDailySyncOutcome.Disabled;

        if (!FipsCmdbSyncSchedule.IsInsideWindow(UkDateTime.Now(), config))
            return FipsCmdbDailySyncOutcome.OutsideWindow;

        if (!await IsFipsRegisterOnAsync(cancellationToken))
        {
            _logger.LogInformation("Skipping scheduled CMDB sync because the FIPS service register feature is off.");
            return FipsCmdbDailySyncOutcome.FeatureOff;
        }

        var due = await GetScheduledRunDueStateAsync(cancellationToken);
        if (due == FipsCmdbDailySyncOutcome.AlreadyRanThisSlot)
            return FipsCmdbDailySyncOutcome.AlreadyRanThisSlot;
        if (due == FipsCmdbDailySyncOutcome.Busy)
        {
            _logger.LogInformation("Scheduled CMDB sync waiting because another bulk sync is already running.");
            return FipsCmdbDailySyncOutcome.Busy;
        }

        await FipsCmdbSyncDefaultRules.EnsureSeededAsync(_db, cancellationToken);

        FipsCmdbProductSyncResult? result = null;
        Exception? error = null;
        try
        {
            result = await _sync.SyncActiveServiceOfferingsAsync(
                FipsCmdbCompassSyncHistory.ScheduledInitiatedBy,
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            error = ex;
            _logger.LogError(ex, "Scheduled CMDB sync failed");
        }

        if (result is { AlreadyRunning: true })
        {
            _logger.LogInformation("Scheduled CMDB sync skipped because another bulk sync is already running.");
            return FipsCmdbDailySyncOutcome.Busy;
        }

        if (error != null || result == null || result.Created > 0 || result.Updated > 0 || result.Errors > 0)
            await SendSummaryEmailAsync(result, error, cancellationToken);

        return error != null || result == null
            ? FipsCmdbDailySyncOutcome.Failed
            : FipsCmdbDailySyncOutcome.Completed;
    }

    private async Task<bool> IsFipsRegisterOnAsync(CancellationToken cancellationToken)
    {
        var row = await _db.Features.AsNoTracking()
            .FirstOrDefaultAsync(f => f.Code == FeatureCodes.Fips, cancellationToken);
        if (row == null)
            return await _features.IsFeatureEnabledForUserAsync(FeatureCodes.Fips, null);
        return row.AccessMode != FeatureAccessMode.Off;
    }

    private async Task<FipsCmdbDailySyncOutcome> GetScheduledRunDueStateAsync(CancellationToken cancellationToken)
    {
        var config = _options.Value;
        var interval = FipsCmdbSyncSchedule.IntervalMinutes(config);
        var slotStartUk = FipsCmdbSyncSchedule.CurrentSlotStartUk(UkDateTime.Now(), interval);
        var slotStartUtc = UkDateTime.ToUtc(slotStartUk);
        var staleBefore = DateTime.UtcNow.AddHours(-2);
        var scheduledBy = new[]
        {
            FipsCmdbCompassSyncHistory.ScheduledInitiatedBy,
            FipsCmdbCompassSyncHistory.LegacyScheduledInitiatedBy
        };
        var slotRuns = await _db.FipsSyncHistories.AsNoTracking()
            .Where(h => h.SyncType == FipsCmdbCompassSyncHistory.SyncType
                        && h.InitiatedBy != null
                        && scheduledBy.Contains(h.InitiatedBy)
                        && h.StartedAt >= slotStartUtc)
            .Select(h => new { h.Status, h.StartedAt })
            .ToListAsync(cancellationToken);

        if (slotRuns.Any(h =>
                h.Status == FipsCmdbCompassSyncHistory.StatusCompleted
                || h.Status == FipsCmdbCompassSyncHistory.StatusFailed))
            return FipsCmdbDailySyncOutcome.AlreadyRanThisSlot;

        if (slotRuns.Any(h =>
                h.Status == FipsCmdbCompassSyncHistory.StatusRunning
                && h.StartedAt >= staleBefore))
            return FipsCmdbDailySyncOutcome.Busy;

        return FipsCmdbDailySyncOutcome.Due;
    }

    private async Task SendSummaryEmailAsync(
        FipsCmdbProductSyncResult? result,
        Exception? error,
        CancellationToken cancellationToken)
    {
        var recipient = _options.Value.DailySyncSummaryEmail?.Trim();
        if (string.IsNullOrWhiteSpace(recipient))
        {
            _logger.LogWarning("CMDB sync summary email skipped — FipsSync:DailySyncSummaryEmail is empty.");
            return;
        }

        var ukNow = UkDateTime.Now();
        var envLabel = _environment.IsProduction() ? null : _environment.EnvironmentName;
        var subject = error != null || result?.Errors > 0
            ? AppendEnvironment($"CMDB sync issues — {ukNow:d MMMM yyyy HH:mm}", envLabel)
            : AppendEnvironment($"CMDB sync summary — {ukNow:d MMMM yyyy HH:mm}", envLabel);
        var body = BuildEmailBody(result, error);

        var send = await _notificationService.SendEmailAsync(
            recipient,
            subject,
            body,
            triggerCode: EmailEventKey,
            cancellationToken: cancellationToken);

        await _emailLog.LogAsync(
            recipient,
            "FIPS service",
            EmailEventKey,
            subject,
            body,
            send.Success,
            send.ErrorMessage,
            $"cmdb-sync:{ukNow:yyyy-MM-dd-HH-mm}",
            cancellationToken);

        if (!send.Success)
            _logger.LogWarning("CMDB sync summary email was not sent: {Error}", send.ErrorMessage);
    }

    private string BuildEmailBody(FipsCmdbProductSyncResult? result, Exception? error)
    {
        var baseUrl = ResolvePublicBaseUrl();
        var newTabUrl = $"{baseUrl}/modern/operations/service-register?tab=new";
        var settingsUrl = $"{baseUrl}/modern/operations/service-register/sync-settings";
        var runsUrl = result?.HistoryId is int historyId
            ? $"{baseUrl}/modern/operations/service-register/sync-settings/runs/{historyId}"
            : $"{baseUrl}/modern/operations/service-register/sync-settings/runs";
        var sb = new StringBuilder();

        if (error != null)
        {
            sb.AppendLine("The scheduled CMDB sync to the COMPASS service register failed.");
            sb.AppendLine();
            sb.AppendLine($"Error: {error.Message}");
            sb.AppendLine();
        }
        else
        {
            sb.AppendLine("The scheduled CMDB sync to the COMPASS service register has finished.");
            sb.AppendLine();
        }

        sb.AppendLine($"Run by: {FipsCmdbCompassSyncHistory.ScheduledInitiatedBy}");
        sb.AppendLine($"Finished: {UkDateTime.FormatUk(DateTime.UtcNow)}");
        sb.AppendLine();

        if (result != null && !result.AlreadyRunning)
        {
            sb.AppendLine("Summary");
            sb.AppendLine($"Created: {result.Created}");
            sb.AppendLine($"Updated: {result.Updated}");
            sb.AppendLine($"Status set by sync rules: {result.StatusSetByRules}");
            sb.AppendLine($"Skipped (retired in Compass): {result.SkippedRetired}");
            sb.AppendLine($"Unchanged: {result.Unchanged}");
            sb.AppendLine($"Skipped (no sys_id): {result.SkippedNoSysId}");
            sb.AppendLine($"Errors: {result.Errors}");
            sb.AppendLine();

            if (result.ErrorSamples.Count > 0)
            {
                sb.AppendLine("Error samples");
                foreach (var sample in result.ErrorSamples)
                    sb.AppendLine($"- {sample}");
                sb.AppendLine();
            }

            var needingInfo = result.NewProductsNeedingInfo.ToList();
            sb.AppendLine(
                needingInfo.Count == 1
                    ? "1 new entry needs information adding."
                    : $"{needingInfo.Count} new entries need information adding.");
            sb.AppendLine(
                result.NewStatusCount == 1
                    ? "There is currently 1 new entry in the service register."
                    : $"There are currently {result.NewStatusCount} new entries in the service register.");
            sb.AppendLine();
            sb.AppendLine("Review all new entries:");
            sb.AppendLine(newTabUrl);
            sb.AppendLine();

            if (needingInfo.Count > 0)
            {
                sb.AppendLine("New entries from this sync");
                foreach (var product in needingInfo.Take(MaxNewProductsInEmail))
                {
                    var title = string.IsNullOrWhiteSpace(product.Title) ? "Untitled service" : product.Title.Trim();
                    sb.AppendLine(title);
                    sb.AppendLine($"{baseUrl}/modern/manage/fips/{product.Id:D}");
                    sb.AppendLine();
                }

                if (needingInfo.Count > MaxNewProductsInEmail)
                {
                    sb.AppendLine(
                        $"And {needingInfo.Count - MaxNewProductsInEmail} more. Open the new entries list for the rest.");
                    sb.AppendLine();
                }
            }
        }

        sb.AppendLine("Sync run report:");
        sb.AppendLine(runsUrl);
        sb.AppendLine();
        sb.AppendLine("Sync settings:");
        sb.AppendLine(settingsUrl);
        return sb.ToString().TrimEnd();
    }

    private string ResolvePublicBaseUrl() =>
        (_configuration["Compass:PublicBaseUrl"]
         ?? (_environment.IsProduction()
             ? _configuration["Docs:ApiExplorer:ProductionBaseUrl"]
             : _configuration["Docs:ApiExplorer:TestBaseUrl"])
         ?? "https://compass.education.gov.uk").TrimEnd('/');

    private static string AppendEnvironment(string subject, string? environmentName) =>
        string.IsNullOrWhiteSpace(environmentName)
            ? subject
            : $"{subject} ({environmentName})";
}
