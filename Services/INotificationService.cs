using Compass.Models;

namespace Compass.Services;

public interface INotificationService
{
    Task<NotificationResult> SendEmailAsync(
        string recipientEmail,
        string subject,
        string body,
        string? triggerCode = null,
        int? notificationRuleId = null,
        Dictionary<string, object>? contextData = null,
        string? notifyTemplateId = null,
        IReadOnlyDictionary<string, object>? notifyPersonalisationExtras = null,
        CancellationToken cancellationToken = default);

    Task<NotificationResult> SendEmailWithTemplateAsync(
        string recipientEmail,
        string templateId,
        Dictionary<string, dynamic> personalisation,
        string? triggerCode = null,
        int? notificationRuleId = null,
        Dictionary<string, object>? contextData = null,
        CancellationToken cancellationToken = default);

    Task<string> RenderTemplateAsync(string template, Dictionary<string, object> variables);
}

public class NotificationResult
{
    public bool Success { get; set; }
    public string? MessageId { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? SentAt { get; set; }
}
