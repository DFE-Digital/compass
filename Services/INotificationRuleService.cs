using Compass.Models;

namespace Compass.Services;

public interface INotificationRuleService
{
    Task<List<NotificationRule>> GetActiveRulesForTriggerAsync(string triggerCode);
    
    Task<List<string>> GetRecipientEmailsAsync(
        NotificationRule rule,
        int? projectId = null,
        int? userId = null,
        string? ragStatus = null,
        Dictionary<string, object>? additionalContext = null);

    Task<bool> ShouldSendNotificationAsync(
        NotificationRule rule,
        int? projectId = null,
        string? ragStatus = null,
        Dictionary<string, object>? additionalContext = null);

    Task ProcessNotificationTriggerAsync(
        string triggerCode,
        int? projectId = null,
        int? userId = null,
        string? ragStatus = null,
        Dictionary<string, object>? templateVariables = null,
        Dictionary<string, object>? additionalContext = null,
        CancellationToken cancellationToken = default);
}
