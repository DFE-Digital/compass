using System.Globalization;
using Compass.Data;
using Compass.Models;
using Microsoft.EntityFrameworkCore;

namespace Compass.Services;

public sealed class WorkReportingNotificationService : IWorkReportingNotificationService
{
    private static readonly string[] ReportingEligibleStatuses = ["Active", "Paused"];

    private readonly CompassDbContext _db;
    private readonly IMonthlyUpdateService _monthlyUpdateService;
    private readonly ICompassNotificationSettingsService _notificationSettings;
    private readonly INotificationService _notificationService;
    private readonly ICompassNotificationEmailLogService _emailLog;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<WorkReportingNotificationService> _logger;

    public WorkReportingNotificationService(
        CompassDbContext db,
        IMonthlyUpdateService monthlyUpdateService,
        ICompassNotificationSettingsService notificationSettings,
        INotificationService notificationService,
        ICompassNotificationEmailLogService emailLog,
        IConfiguration configuration,
        IHostEnvironment environment,
        ILogger<WorkReportingNotificationService> logger)
    {
        _db = db;
        _monthlyUpdateService = monthlyUpdateService;
        _notificationSettings = notificationSettings;
        _notificationService = notificationService;
        _emailLog = emailLog;
        _configuration = configuration;
        _environment = environment;
        _logger = logger;
    }

    public async Task ProcessDailyNotificationsAsync(CancellationToken cancellationToken = default)
    {
        var today = GetUkToday();

        var explicitPeriods = await _db.WorkReportingCyclePeriods.AsNoTracking()
            .Include(p => p.ReportingCycle)
            .Where(p => p.IsActive &&
                        p.ReportingCycle.Code == WorkReportingMonthlyCycleCodes.MonthlyWorkUpdates &&
                        p.ReportingCycle.IsActive)
            .ToListAsync(cancellationToken);

        if (explicitPeriods.Count > 0)
        {
            foreach (var period in explicitPeriods)
                await ProcessExplicitPeriodAsync(period, today, cancellationToken);
            return;
        }

        var now = DateTime.UtcNow;
        var (year, month) = _monthlyUpdateService.ResolveDashboardReportingPeriod(now);
        var dueDate = _monthlyUpdateService.GetMonthlyUpdateDueDate(year, month).Date;
        var opensDate = _monthlyUpdateService.GetSubmissionWindowOpens(year, month).Date;
        var periodLabel = new DateTime(year, month, 1).ToString("MMMM yyyy", CultureInfo.GetCultureInfo("en-GB"));

        await TrySendForPeriodAsync(
            year,
            month,
            periodLabel,
            opensDate,
            dueDate,
            today,
            cancellationToken);
    }

    private async Task ProcessExplicitPeriodAsync(
        WorkReportingCyclePeriod period,
        DateTime today,
        CancellationToken cancellationToken)
    {
        var year = period.PeriodStart.Year;
        var month = period.PeriodStart.Month;
        var periodLabel = string.IsNullOrWhiteSpace(period.PeriodLabel)
            ? new DateTime(year, month, 1).ToString("MMMM yyyy", CultureInfo.GetCultureInfo("en-GB"))
            : period.PeriodLabel.Trim();

        await TrySendForPeriodAsync(
            year,
            month,
            periodLabel,
            period.SubmissionOpens.Date,
            period.SubmissionCloses.Date,
            today,
            cancellationToken);
    }

    private async Task TrySendForPeriodAsync(
        int year,
        int month,
        string periodLabel,
        DateTime opensDate,
        DateTime dueDate,
        DateTime today,
        CancellationToken cancellationToken)
    {
        if (today == opensDate)
        {
            await SendForEligibleProjectsAsync(
                CompassNotificationEventKeys.WorkReportingMonthlyOpen,
                year,
                month,
                periodLabel,
                dueDate,
                p => BuildOpenSubject(periodLabel),
                p => BuildOpenBody(p, periodLabel, dueDate, BuildMonthlyReportUrl(p.Id, year, month)),
                cancellationToken);
        }

        if (today == dueDate.AddDays(-1))
        {
            await SendForEligibleProjectsAsync(
                CompassNotificationEventKeys.WorkReportingMonthlyDueReminder,
                year,
                month,
                periodLabel,
                dueDate,
                p => BuildDueReminderSubject(periodLabel),
                p => BuildDueReminderBody(p, periodLabel, dueDate, BuildMonthlyReportUrl(p.Id, year, month)),
                cancellationToken);
        }

        if (today == dueDate.AddDays(1))
        {
            await SendForEligibleProjectsAsync(
                CompassNotificationEventKeys.WorkReportingMonthlyOverdue,
                year,
                month,
                periodLabel,
                dueDate,
                p => BuildOverdueSubject(periodLabel),
                p => BuildOverdueBody(p, periodLabel, dueDate, BuildMonthlyReportUrl(p.Id, year, month)),
                cancellationToken);
        }
    }

    private async Task SendForEligibleProjectsAsync(
        string eventKey,
        int year,
        int month,
        string periodLabel,
        DateTime dueDate,
        Func<Project, string> subjectBuilder,
        Func<Project, string> bodyBuilder,
        CancellationToken cancellationToken)
    {
        if (!await _notificationSettings.IsEventEnabledAsync(eventKey, cancellationToken))
            return;

        var recipientFlags = await _notificationSettings.GetRecipientFlagsAsync(eventKey, cancellationToken);
        if (recipientFlags == CompassNotificationRecipientFlags.None)
            return;

        var projects = await _db.Projects.AsNoTracking()
            .Include(p => p.PrimaryContactUser)
            .Include(p => p.PmoContacts).ThenInclude(pmo => pmo.User)
            .Include(p => p.MonthlyUpdates)
            .Where(p => p.Status != null && ReportingEligibleStatuses.Contains(p.Status))
            .ToListAsync(cancellationToken);

        foreach (var project in projects)
        {
            var update = project.MonthlyUpdates.FirstOrDefault(u => u.Year == year && u.Month == month);
            if (update?.SubmittedAt != null)
                continue;

            var contextReference = $"work:{project.Id}|{year:D4}-{month:D2}";
            var recipients = ResolveRecipients(project, recipientFlags).ToList();
            if (recipients.Count == 0)
                continue;

            var subject = subjectBuilder(project);
            var body = bodyBuilder(project);

            foreach (var (email, name) in recipients)
            {
                if (await WasAlreadySentAsync(eventKey, contextReference, email, cancellationToken))
                    continue;

                var result = await _notificationService.SendEmailAsync(
                    email,
                    subject,
                    body,
                    triggerCode: eventKey,
                    cancellationToken: cancellationToken);

                await _emailLog.LogAsync(
                    email,
                    name,
                    eventKey,
                    subject,
                    body,
                    result.Success,
                    result.ErrorMessage,
                    contextReference,
                    cancellationToken);
            }
        }
    }

    private static IEnumerable<(string Email, string? Name)> ResolveRecipients(
        Project project,
        CompassNotificationRecipientFlags flags)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (flags.HasFlag(CompassNotificationRecipientFlags.PrimaryWorkContact))
        {
            var email = project.PrimaryContactUser?.Email?.Trim();
            if (!string.IsNullOrEmpty(email) && seen.Add(email))
            {
                yield return (email, project.PrimaryContactUser?.Name);
            }
        }

        if (flags.HasFlag(CompassNotificationRecipientFlags.PmoContact))
        {
            foreach (var pmo in project.PmoContacts.OrderBy(p => p.Id))
            {
                var email = pmo.User?.Email?.Trim();
                if (!string.IsNullOrEmpty(email) && seen.Add(email))
                    yield return (email, pmo.User?.Name);
            }
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

    private string BuildMonthlyReportUrl(int projectId, int year, int month)
    {
        var baseUrl = (_configuration["Compass:PublicBaseUrl"]
                       ?? (_environment.IsProduction()
                           ? _configuration["Docs:ApiExplorer:ProductionBaseUrl"]
                           : _configuration["Docs:ApiExplorer:TestBaseUrl"])
                       ?? "https://compass.education.gov.uk").TrimEnd('/');

        return $"{baseUrl}/modern/work/{projectId}/monthly-report/{year}/{month}";
    }

    private static DateTime GetUkToday()
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById(
            OperatingSystem.IsWindows() ? "GMT Standard Time" : "Europe/London");
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz).Date;
    }

    private static string BuildOpenSubject(string periodLabel) =>
        $"Monthly reporting open — {periodLabel}";

    private static string BuildDueReminderSubject(string periodLabel) =>
        $"Reminder: monthly return due tomorrow — {periodLabel}";

    private static string BuildOverdueSubject(string periodLabel) =>
        $"Overdue: monthly return not submitted — {periodLabel}";

    private static string BuildOpenBody(Project project, string periodLabel, DateTime dueDate, string reportUrl) =>
        $"""
        The monthly reporting period for {periodLabel} is now open for {project.Title}.

        Please submit your monthly return by {dueDate:dd MMMM yyyy}.

        Submit your return: {reportUrl}
        """;

    private static string BuildDueReminderBody(Project project, string periodLabel, DateTime dueDate, string reportUrl) =>
        $"""
        This is a reminder that the monthly return for {periodLabel} is due tomorrow ({dueDate:dd MMMM yyyy}) and has not yet been submitted for {project.Title}.

        Please complete and submit your return.

        Submit your return: {reportUrl}
        """;

    private static string BuildOverdueBody(Project project, string periodLabel, DateTime dueDate, string reportUrl) =>
        $"""
        The monthly return for {periodLabel} was due on {dueDate:dd MMMM yyyy} and has not been submitted for {project.Title}.

        Please submit your return as soon as possible.

        Submit your return: {reportUrl}
        """;
}
