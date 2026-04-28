using Compass.Data;
using Compass.Models;

namespace Compass.Services;

public sealed class CompassNotificationEmailLogService : ICompassNotificationEmailLogService
{
    private readonly CompassDbContext _db;
    private readonly ILogger<CompassNotificationEmailLogService> _logger;

    public CompassNotificationEmailLogService(CompassDbContext db, ILogger<CompassNotificationEmailLogService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task LogAsync(
        string recipientEmail,
        string? recipientName,
        string eventKey,
        string subject,
        string body,
        bool sendSucceeded,
        string? errorMessage,
        string? contextReference,
        CancellationToken cancellationToken = default)
    {
        var email = (recipientEmail ?? "").Trim();
        if (string.IsNullOrEmpty(email))
        {
            _logger.LogWarning("Compass notification log skipped: empty recipient for {EventKey}", eventKey);
            return;
        }

        _db.CompassNotificationEmailLogs.Add(new CompassNotificationEmailLog
        {
            SentAtUtc = DateTime.UtcNow,
            RecipientEmail = email.Length > 256 ? email[..256] : email,
            RecipientName = string.IsNullOrWhiteSpace(recipientName) ? null : recipientName.Trim(),
            EventKey = eventKey.Length > 100 ? eventKey[..100] : eventKey,
            Subject = subject.Length > 500 ? subject[..500] : subject,
            Body = body ?? "",
            ContextReference = string.IsNullOrWhiteSpace(contextReference)
                ? null
                : contextReference.Trim().Length > 200
                    ? contextReference.Trim()[..200]
                    : contextReference.Trim(),
            SendSucceeded = sendSucceeded,
            ErrorMessage = string.IsNullOrWhiteSpace(errorMessage)
                ? null
                : errorMessage.Length > 2000
                    ? errorMessage[..2000]
                    : errorMessage,
        });

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist Compass notification email log for {EventKey}", eventKey);
        }
    }
}
