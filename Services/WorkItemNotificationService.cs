using Compass.Data;
using Compass.Models;
using Microsoft.EntityFrameworkCore;

namespace Compass.Services;

public sealed class WorkItemNotificationService : IWorkItemNotificationService
{
    private const string WorkItemCreatedSubject = "New work item added to COMPASS";

    private readonly CompassDbContext _db;
    private readonly ICompassNotificationSettingsService _notificationSettings;
    private readonly INotificationService _notificationService;
    private readonly ICompassNotificationEmailLogService _emailLog;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<WorkItemNotificationService> _logger;

    public WorkItemNotificationService(
        CompassDbContext db,
        ICompassNotificationSettingsService notificationSettings,
        INotificationService notificationService,
        ICompassNotificationEmailLogService emailLog,
        IConfiguration configuration,
        IHostEnvironment environment,
        ILogger<WorkItemNotificationService> logger)
    {
        _db = db;
        _notificationSettings = notificationSettings;
        _notificationService = notificationService;
        _emailLog = emailLog;
        _configuration = configuration;
        _environment = environment;
        _logger = logger;
    }

    public async Task TrySendWorkItemCreatedAsync(
        int projectId,
        string creatorEmail,
        string? creatorName,
        CancellationToken cancellationToken = default)
    {
        const string eventKey = CompassNotificationEventKeys.WorkItemCreated;

        if (!await _notificationSettings.IsEventEnabledAsync(eventKey, cancellationToken))
            return;

        var recipientFlags = await _notificationSettings.GetRecipientFlagsAsync(eventKey, cancellationToken);
        if (recipientFlags == CompassNotificationRecipientFlags.None)
            return;

        var project = await _db.Projects.AsNoTracking()
            .Where(p => p.Id == projectId)
            .Select(p => new { p.Id, p.Title })
            .FirstOrDefaultAsync(cancellationToken);

        if (project == null)
        {
            _logger.LogWarning("Work item created notification skipped: project {ProjectId} not found", projectId);
            return;
        }

        var creatorLabel = FormatPersonLabel(creatorName, creatorEmail);
        var workItemUrl = BuildWorkItemUrl(project.Id);
        var body = $"""
            {creatorLabel} added a new work item to COMPASS.

            Work item: {project.Title.Trim()}

            View work item: {workItemUrl}
            """;

        var contextReference = $"work:{project.Id}";
        var recipients = ResolveRecipients(recipientFlags, creatorEmail, creatorName).ToList();
        if (recipients.Count == 0)
        {
            _logger.LogInformation(
                "Work item created notification skipped for project {ProjectId}: no recipients resolved",
                projectId);
            return;
        }

        foreach (var (email, name) in recipients)
        {
            if (await WasAlreadySentAsync(eventKey, contextReference, email, cancellationToken))
                continue;

            var result = await _notificationService.SendEmailAsync(
                email,
                WorkItemCreatedSubject,
                body,
                triggerCode: eventKey,
                cancellationToken: cancellationToken);

            await _emailLog.LogAsync(
                email,
                name,
                eventKey,
                WorkItemCreatedSubject,
                body,
                result.Success,
                result.ErrorMessage,
                contextReference,
                cancellationToken);
        }
    }

    private IEnumerable<(string Email, string? Name)> ResolveRecipients(
        CompassNotificationRecipientFlags flags,
        string creatorEmail,
        string? creatorName)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (flags.HasFlag(CompassNotificationRecipientFlags.WorkItemCreator))
        {
            var email = creatorEmail.Trim();
            if (!string.IsNullOrEmpty(email) && email.Contains('@') && seen.Add(email))
                yield return (email, creatorName);
        }

        if (flags.HasFlag(CompassNotificationRecipientFlags.CentralOps))
        {
            foreach (var email in ResolveCentralOpsEmails())
            {
                if (seen.Add(email))
                    yield return (email, "Central Operations");
            }
        }
    }

    private IEnumerable<string> ResolveCentralOpsEmails()
    {
        var configured = _configuration["CompassNotifications:CentralOpsEmail"]
                         ?? _configuration["GovUkNotify:ContactChangeRecipientEmail"];
        if (string.IsNullOrWhiteSpace(configured))
            yield break;

        foreach (var part in configured.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (part.Contains('@'))
                yield return part;
        }
    }

    private async Task<bool> WasAlreadySentAsync(
        string eventKey,
        string contextReference,
        string recipientEmail,
        CancellationToken cancellationToken)
    {
        return await _db.CompassNotificationEmailLogs.AsNoTracking()
            .AnyAsync(l =>
                    l.EventKey == eventKey &&
                    l.ContextReference == contextReference &&
                    l.RecipientEmail == recipientEmail &&
                    l.SendSucceeded,
                cancellationToken);
    }

    private string BuildWorkItemUrl(int projectId)
    {
        var baseUrl = (_configuration["Compass:PublicBaseUrl"]
                       ?? (_environment.IsProduction()
                           ? _configuration["Docs:ApiExplorer:ProductionBaseUrl"]
                           : _configuration["Docs:ApiExplorer:TestBaseUrl"])
                       ?? "https://compass.education.gov.uk").TrimEnd('/');

        return $"{baseUrl}/modern/work/{projectId}";
    }

    private static string FormatPersonLabel(string? name, string email)
    {
        var trimmedName = name?.Trim();
        if (!string.IsNullOrEmpty(trimmedName))
            return trimmedName;

        var trimmedEmail = email.Trim();
        return trimmedEmail.Contains('@') ? trimmedEmail : "A COMPASS user";
    }
}
