using System.Text;
using Compass.Data;
using Compass.Helpers;
using Compass.Models.Fips;
using Microsoft.EntityFrameworkCore;

namespace Compass.Services.Fips;

/// <summary>Emails the service owner once when a service register entry is first created with status New.</summary>
public sealed class FipsNewEntryOwnerEmailService
{
    public const string EmailEventKey = "fips_new_entry_complete";
    private const string ServiceOwnerRole = "Service Owner";

    private readonly CompassDbContext _db;
    private readonly INotificationService _notificationService;
    private readonly ICompassNotificationEmailLogService _emailLog;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<FipsNewEntryOwnerEmailService> _logger;

    public FipsNewEntryOwnerEmailService(
        CompassDbContext db,
        INotificationService notificationService,
        ICompassNotificationEmailLogService emailLog,
        IConfiguration configuration,
        IHostEnvironment environment,
        ILogger<FipsNewEntryOwnerEmailService> logger)
    {
        _db = db;
        _notificationService = notificationService;
        _emailLog = emailLog;
        _configuration = configuration;
        _environment = environment;
        _logger = logger;
    }

    public async Task SendPendingAsync(CancellationToken cancellationToken = default)
    {
        var pending = await _db.CMDBProducts
            .AsNoTracking()
            .Where(p => p.Status == CMDBProductStatus.New && p.NewOwnerCompletionEmailSentAt == null)
            .Select(p => new PendingEntry
            {
                Id = p.Id,
                Title = p.Title,
                Owners = p.Contacts
                    .Where(c => c.FipsContactRole.Name == ServiceOwnerRole
                                && c.UserEmail != null
                                && c.UserEmail != "")
                    .Select(c => new OwnerContact
                    {
                        Email = c.UserEmail!,
                        Name = c.UserName
                    })
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        foreach (var entry in pending)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await SendOneAsync(entry, cancellationToken);
        }
    }

    private async Task SendOneAsync(PendingEntry entry, CancellationToken cancellationToken)
    {
        var owners = entry.Owners
            .Select(owner => new OwnerContact
            {
                Email = owner.Email.Trim(),
                Name = string.IsNullOrWhiteSpace(owner.Name) ? null : owner.Name.Trim()
            })
            .Where(owner => owner.Email.Length > 0)
            .GroupBy(owner => owner.Email, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();

        if (owners.Count == 0)
        {
            _logger.LogInformation(
                "New service register entry {ProductId} has no service owner email yet, so the completion email was not sent.",
                entry.Id);
            return;
        }

        var title = string.IsNullOrWhiteSpace(entry.Title) ? "Untitled service" : entry.Title.Trim();
        var recordUrl = $"{ResolvePublicBaseUrl()}/modern/manage/fips/{entry.Id:D}";
        var envLabel = _environment.IsProduction() ? null : _environment.EnvironmentName;
        var subject = AppendEnvironment($"Complete the service register entry — {title}", envLabel);
        var body = BuildBody(title, recordUrl);
        var anySent = false;

        foreach (var owner in owners)
        {
            var send = await _notificationService.SendEmailAsync(
                owner.Email,
                subject,
                body,
                triggerCode: EmailEventKey,
                cancellationToken: cancellationToken);

            await _emailLog.LogAsync(
                owner.Email,
                owner.Name,
                EmailEventKey,
                subject,
                body,
                send.Success,
                send.ErrorMessage,
                $"fips-new-entry:{entry.Id:D}",
                cancellationToken);

            if (send.Success)
                anySent = true;
            else
                _logger.LogWarning(
                    "New service register entry email to {Email} for {ProductId} was not sent: {Error}",
                    owner.Email,
                    entry.Id,
                    send.ErrorMessage);
        }

        if (!anySent)
            return;

        var row = await _db.CMDBProducts.FirstOrDefaultAsync(p => p.Id == entry.Id, cancellationToken);
        if (row == null || row.NewOwnerCompletionEmailSentAt != null)
            return;

        row.NewOwnerCompletionEmailSentAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static string BuildBody(string title, string recordUrl)
    {
        var sb = new StringBuilder();
        sb.AppendLine("A new service has been added to the COMPASS service register and its status is New.");
        sb.AppendLine();
        sb.AppendLine(title);
        sb.AppendLine();
        sb.AppendLine("Please complete the additional information on this entry. That includes whether it should appear in Find information about products and services, where people look up:");
        sb.AppendLine();
        sb.AppendLine("- the type of product or service");
        sb.AppendLine("- the channels it is provided through");
        sb.AppendLine("- the user groups who use it");
        sb.AppendLine("- supporting business information, such as the description, phase, and business area");
        sb.AppendLine();
        sb.AppendLine("Open the record to update it:");
        sb.AppendLine(recordUrl);
        sb.AppendLine();
        sb.AppendLine("Find information about products and services:");
        sb.AppendLine("https://find-products-services.education.gov.uk");
        sb.AppendLine();
        sb.AppendLine("Named contacts on this entry, admins, and Central Operations can set the status to Active when the information is ready.");
        sb.AppendLine();
        sb.AppendLine("The service name and contacts are updated from CMDB, not in Compass.");
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

    private sealed class PendingEntry
    {
        public Guid Id { get; init; }
        public string Title { get; init; } = "";
        public List<OwnerContact> Owners { get; init; } = [];
    }

    private sealed class OwnerContact
    {
        public string Email { get; init; } = "";
        public string? Name { get; init; }
    }
}
