using Notify.Client;
using Notify.Models.Responses;
using Compass.Data;
using Compass.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace Compass.Services;

public class NotificationService : INotificationService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<NotificationService> _logger;
    private readonly CompassDbContext _context;
    private readonly NotificationClient? _notifyClient;

    public NotificationService(
        IConfiguration configuration,
        ILogger<NotificationService> logger,
        CompassDbContext context)
    {
        _configuration = configuration;
        _logger = logger;
        _context = context;

        var apiKey = _configuration["GovUkNotify:ApiKey"];
        var templateId = _configuration["GovUkNotify:TemplateId"];

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("GOV.UK Notify API key is not configured. Email notifications will not be sent.");
        }
        else
        {
            try
            {
                _notifyClient = new NotificationClient(apiKey);
                _logger.LogInformation("GOV.UK Notify client initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize GOV.UK Notify client");
            }
        }

        if (string.IsNullOrWhiteSpace(templateId))
        {
            _logger.LogWarning("GOV.UK Notify template ID is not configured. Using default template.");
        }
    }

    public async Task<NotificationResult> SendEmailAsync(
        string recipientEmail,
        string subject,
        string body,
        string? triggerCode = null,
        int? notificationRuleId = null,
        Dictionary<string, object>? contextData = null,
        CancellationToken cancellationToken = default)
    {
        var result = new NotificationResult();

        // Validate email address
        if (string.IsNullOrWhiteSpace(recipientEmail) || !recipientEmail.Contains('@'))
        {
            result.Success = false;
            result.ErrorMessage = "Invalid email address";
            _logger.LogWarning("Attempted to send notification to invalid email: {Email}", recipientEmail);
            return result;
        }

        // Log the notification attempt
        var notificationLog = new NotificationLog
        {
            NotificationRuleId = notificationRuleId,
            TriggerCode = triggerCode ?? "unknown",
            RecipientEmail = recipientEmail,
            Subject = subject,
            Body = body,
            Status = "pending",
            CreatedAt = DateTime.UtcNow,
            ContextData = contextData != null ? System.Text.Json.JsonSerializer.Serialize(contextData) : null
        };

        try
        {
            _context.NotificationLogs.Add(notificationLog);
            await _context.SaveChangesAsync(cancellationToken);

            // If Notify client is not available, mark as failed
            if (_notifyClient == null)
            {
                notificationLog.Status = "failed";
                notificationLog.StatusMessage = "GOV.UK Notify client not initialized";
                await _context.SaveChangesAsync(cancellationToken);

                result.Success = false;
                result.ErrorMessage = "Notification service not configured";
                _logger.LogWarning("Notification not sent - GOV.UK Notify client not initialized");
                return result;
            }

            // Get template ID from configuration
            var templateId = _configuration["GovUkNotify:TemplateId"];
            if (string.IsNullOrWhiteSpace(templateId))
            {
                notificationLog.Status = "failed";
                notificationLog.StatusMessage = "Template ID not configured";
                await _context.SaveChangesAsync(cancellationToken);

                result.Success = false;
                result.ErrorMessage = "Template ID not configured";
                _logger.LogWarning("Notification not sent - Template ID not configured");
                return result;
            }

            // Prepare personalisation for GOV.UK Notify
            // The template should have 2 placeholders: subject and body
            var personalisation = new Dictionary<string, dynamic>
            {
                { "subject", subject },
                { "body", body }
            };

            // Send email via GOV.UK Notify
            EmailNotificationResponse? notifyResponse = null;
            try
            {
                notifyResponse = _notifyClient.SendEmail(
                    emailAddress: recipientEmail,
                    templateId: templateId,
                    personalisation: personalisation);

                // Update log with success
                notificationLog.Status = "sent";
                notificationLog.StatusMessage = "Email sent successfully";
                notificationLog.NotifyMessageId = notifyResponse.id;
                notificationLog.SentAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);

                result.Success = true;
                result.MessageId = notifyResponse.id;
                result.SentAt = DateTime.UtcNow;

                _logger.LogInformation(
                    "Notification sent successfully to {Email} via GOV.UK Notify. Message ID: {MessageId}",
                    recipientEmail,
                    notifyResponse.id);
            }
            catch (Notify.Exceptions.NotifyClientException ex)
            {
                // Handle GOV.UK Notify specific errors
                notificationLog.Status = "failed";
                notificationLog.StatusMessage = ex.Message;
                await _context.SaveChangesAsync(cancellationToken);

                result.Success = false;
                result.ErrorMessage = ex.Message;

                _logger.LogError(
                    ex,
                    "Failed to send notification via GOV.UK Notify to {Email}",
                    recipientEmail);
                
                return result;
            }
        }
        catch (Exception ex)
        {
            // Handle general errors
            notificationLog.Status = "failed";
            notificationLog.StatusMessage = ex.Message;
            await _context.SaveChangesAsync(cancellationToken);

            result.Success = false;
            result.ErrorMessage = ex.Message;

            _logger.LogError(
                ex,
                "Unexpected error sending notification to {Email}",
                recipientEmail);
        }

        return result;
    }

    public Task<string> RenderTemplateAsync(string template, Dictionary<string, object> variables)
    {
        if (string.IsNullOrWhiteSpace(template))
        {
            return Task.FromResult(string.Empty);
        }

        var rendered = template;

        // Replace placeholders in format {{variable_name}}
        foreach (var variable in variables)
        {
            var placeholder = $"{{{{{variable.Key}}}}}";
            var value = variable.Value?.ToString() ?? string.Empty;
            rendered = rendered.Replace(placeholder, value, StringComparison.OrdinalIgnoreCase);
        }

        // Remove any remaining placeholders
        rendered = Regex.Replace(rendered, @"\{\{[\w]+\}\}", "", RegexOptions.IgnoreCase);

        return Task.FromResult(rendered);
    }
}
