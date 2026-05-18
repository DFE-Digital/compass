using Compass.Data;
using Compass.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Compass.Services;

public class NotificationRuleService : INotificationRuleService
{
    private readonly CompassDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly ILogger<NotificationRuleService> _logger;

    public NotificationRuleService(
        CompassDbContext context,
        INotificationService notificationService,
        ILogger<NotificationRuleService> logger)
    {
        _context = context;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<List<NotificationRule>> GetActiveRulesForTriggerAsync(string triggerCode)
    {
        return await _context.NotificationRules
            .Include(r => r.NotificationTemplate)
            .Where(r => r.TriggerCode == triggerCode && 
                       r.IsEnabled && 
                       r.NotificationTemplate != null && 
                       r.NotificationTemplate.IsActive)
            .ToListAsync();
    }

    public async Task<List<string>> GetRecipientEmailsAsync(
        NotificationRule rule,
        int? projectId = null,
        int? userId = null,
        string? ragStatus = null,
        Dictionary<string, object>? additionalContext = null)
    {
        var recipientEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Parse recipient configuration
        var recipientConfig = ParseJsonConfig(rule.RecipientConfiguration);

        if (recipientConfig != null)
        {
            // Handle specific emails
            if (recipientConfig.TryGetValue("specific_emails", out var specificEmailsObj) &&
                specificEmailsObj is JsonElement specificEmailsArray &&
                specificEmailsArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var emailElement in specificEmailsArray.EnumerateArray())
                {
                    if (emailElement.ValueKind == JsonValueKind.String)
                    {
                        var email = emailElement.GetString();
                        if (!string.IsNullOrWhiteSpace(email))
                        {
                            recipientEmails.Add(email);
                        }
                    }
                }
            }

            // Handle role-based recipients
            if (recipientConfig.TryGetValue("recipients", out var recipientsObj) &&
                recipientsObj is JsonElement recipientsArray &&
                recipientsArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var recipientElement in recipientsArray.EnumerateArray())
                {
                    if (recipientElement.ValueKind == JsonValueKind.String)
                    {
                        var recipientType = recipientElement.GetString();
                        await AddRecipientsByTypeAsync(
                            recipientEmails,
                            recipientType ?? string.Empty,
                            projectId,
                            userId);
                    }
                }
            }
        }

        // If no recipients configured, default to the user who triggered the event
        if (recipientEmails.Count == 0 && userId.HasValue)
        {
            var user = await _context.Users.FindAsync(userId.Value);
            if (user != null && !string.IsNullOrWhiteSpace(user.Email))
            {
                recipientEmails.Add(user.Email);
            }
        }

        return recipientEmails.ToList();
    }

    private async Task AddRecipientsByTypeAsync(
        HashSet<string> recipientEmails,
        string recipientType,
        int? projectId,
        int? userId)
    {
        if (!projectId.HasValue)
        {
            return;
        }

        var project = await _context.Projects
            .Include(p => p.SeniorResponsibleOfficers)
                .ThenInclude(sro => sro.User)
            .Include(p => p.PrimaryContactUser)
            .Include(p => p.ProjectContacts)
                .ThenInclude(pc => pc.User)
            .FirstOrDefaultAsync(p => p.Id == projectId.Value);

        if (project == null)
        {
            return;
        }

        switch (recipientType.ToLowerInvariant())
        {
            case "team_member":
            case "added_team_member":
                if (userId.HasValue)
                {
                    var addedUser = await _context.Users.FindAsync(userId.Value);
                    if (addedUser != null && !string.IsNullOrWhiteSpace(addedUser.Email))
                    {
                        recipientEmails.Add(addedUser.Email);
                    }
                }
                break;

            case "sro":
            case "senior_responsible_officer":
                foreach (var sro in project.SeniorResponsibleOfficers)
                {
                    if (sro.User != null && !string.IsNullOrWhiteSpace(sro.User.Email))
                    {
                        recipientEmails.Add(sro.User.Email);
                    }
                }
                break;

            case "primary_contact":
                if (project.PrimaryContactUser != null &&
                    !string.IsNullOrWhiteSpace(project.PrimaryContactUser.Email))
                {
                    recipientEmails.Add(project.PrimaryContactUser.Email);
                }
                break;

            case "team":
                foreach (var contact in project.ProjectContacts)
                {
                    if (contact.User != null && !string.IsNullOrWhiteSpace(contact.User.Email))
                    {
                        recipientEmails.Add(contact.User.Email);
                    }
                    else if (!string.IsNullOrWhiteSpace(contact.Email))
                    {
                        recipientEmails.Add(contact.Email);
                    }
                }
                break;

            case "all":
                // Add all project stakeholders
                if (project.PrimaryContactUser != null &&
                    !string.IsNullOrWhiteSpace(project.PrimaryContactUser.Email))
                {
                    recipientEmails.Add(project.PrimaryContactUser.Email);
                }

                foreach (var sro in project.SeniorResponsibleOfficers)
                {
                    if (sro.User != null && !string.IsNullOrWhiteSpace(sro.User.Email))
                    {
                        recipientEmails.Add(sro.User.Email);
                    }
                }

                foreach (var contact in project.ProjectContacts)
                {
                    if (contact.User != null && !string.IsNullOrWhiteSpace(contact.User.Email))
                    {
                        recipientEmails.Add(contact.User.Email);
                    }
                    else if (!string.IsNullOrWhiteSpace(contact.Email))
                    {
                        recipientEmails.Add(contact.Email);
                    }
                }
                break;
        }
    }

    public async Task<bool> ShouldSendNotificationAsync(
        NotificationRule rule,
        int? projectId = null,
        string? ragStatus = null,
        Dictionary<string, object>? additionalContext = null)
    {
        // Check if rule is enabled
        if (!rule.IsEnabled)
        {
            return false;
        }

        // Parse and evaluate conditions
        if (string.IsNullOrWhiteSpace(rule.Conditions))
        {
            return true; // No conditions means always send
        }

        var conditions = ParseJsonConfig(rule.Conditions);
        if (conditions == null)
        {
            return true; // Invalid conditions means always send
        }

        // Check RAG status condition
        if (conditions.TryGetValue("rag_statuses", out var ragStatusesObj) &&
            ragStatusesObj is JsonElement ragStatusesArray &&
            ragStatusesArray.ValueKind == JsonValueKind.Array)
        {
            var allowedRagStatuses = ragStatusesArray.EnumerateArray()
                .Select(e => e.GetString() ?? string.Empty)
                .ToList();

            if (!string.IsNullOrWhiteSpace(ragStatus) &&
                !allowedRagStatuses.Contains(ragStatus, StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        // Check project status condition
        if (conditions.TryGetValue("project_statuses", out var projectStatusesObj) &&
            projectStatusesObj is JsonElement projectStatusesArray &&
            projectStatusesArray.ValueKind == JsonValueKind.Array &&
            projectId.HasValue)
        {
            var project = await _context.Projects.FindAsync(projectId.Value);
            if (project != null)
            {
                var allowedStatuses = projectStatusesArray.EnumerateArray()
                    .Select(e => e.GetString() ?? string.Empty)
                    .ToList();

                if (!string.IsNullOrWhiteSpace(project.Status) &&
                    !allowedStatuses.Contains(project.Status, StringComparer.OrdinalIgnoreCase))
                {
                    return false;
                }
            }
        }

        return true;
    }

    public async Task ProcessNotificationTriggerAsync(
        string triggerCode,
        int? projectId = null,
        int? userId = null,
        string? ragStatus = null,
        Dictionary<string, object>? templateVariables = null,
        Dictionary<string, object>? additionalContext = null,
        CancellationToken cancellationToken = default)
    {
        var rules = await GetActiveRulesForTriggerAsync(triggerCode);

        if (rules.Count == 0)
        {
            _logger.LogDebug("No active rules found for trigger: {TriggerCode}", triggerCode);
            return;
        }

        foreach (var rule in rules)
        {
            // Check if we should send this notification
            var shouldSend = await ShouldSendNotificationAsync(
                rule,
                projectId,
                ragStatus,
                additionalContext);

            if (!shouldSend)
            {
                _logger.LogDebug(
                    "Rule {RuleId} conditions not met for trigger {TriggerCode}",
                    rule.Id,
                    triggerCode);
                continue;
            }

            // Get recipients
            var recipientEmails = await GetRecipientEmailsAsync(
                rule,
                projectId,
                userId,
                ragStatus,
                additionalContext);

            if (recipientEmails.Count == 0)
            {
                _logger.LogWarning(
                    "No recipients found for rule {RuleId} with trigger {TriggerCode}",
                    rule.Id,
                    triggerCode);
                continue;
            }

            // Render template
            var template = rule.NotificationTemplate!;
            var renderedSubject = await _notificationService.RenderTemplateAsync(
                template.Subject,
                templateVariables ?? new Dictionary<string, object>());
            var renderedBody = await _notificationService.RenderTemplateAsync(
                template.Body,
                templateVariables ?? new Dictionary<string, object>());

            // Send to each recipient
            foreach (var recipientEmail in recipientEmails)
            {
                var contextData = new Dictionary<string, object>
                {
                    { "trigger_code", triggerCode },
                    { "rule_id", rule.Id },
                    { "project_id", projectId ?? 0 },
                    { "user_id", userId ?? 0 }
                };

                if (additionalContext != null)
                {
                    foreach (var kvp in additionalContext)
                    {
                        contextData[kvp.Key] = kvp.Value;
                    }
                }

                var result = await _notificationService.SendEmailAsync(
                    recipientEmail,
                    renderedSubject,
                    renderedBody,
                    triggerCode,
                    rule.Id,
                    contextData,
                    cancellationToken: cancellationToken);

                if (result.Success)
                {
                    _logger.LogInformation(
                        "Notification sent successfully to {Email} for trigger {TriggerCode} using rule {RuleId}",
                        recipientEmail,
                        triggerCode,
                        rule.Id);
                }
                else
                {
                    _logger.LogError(
                        "Failed to send notification to {Email} for trigger {TriggerCode} using rule {RuleId}: {Error}",
                        recipientEmail,
                        triggerCode,
                        rule.Id,
                        result.ErrorMessage);
                }
            }
        }
    }

    private Dictionary<string, JsonElement>? ParseJsonConfig(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            var result = new Dictionary<string, JsonElement>();
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                result[prop.Name] = prop.Value;
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse JSON config: {Json}", json);
            return null;
        }
    }
}
