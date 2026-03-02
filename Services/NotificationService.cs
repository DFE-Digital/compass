using Compass.Data;
using Compass.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Linq;

namespace Compass.Services;

public class NotificationService : INotificationService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<NotificationService> _logger;
    private readonly CompassDbContext _context;
    private readonly object? _notifyClient;

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
                // Use reflection to load NotificationClient to avoid assembly loading issues
                var notifyAssembly = Assembly.Load("GovukNotify");
                var clientType = notifyAssembly.GetType("Notify.Client.NotificationClient");
                if (clientType != null)
                {
                    _notifyClient = Activator.CreateInstance(clientType, apiKey);
                    _logger.LogInformation("GOV.UK Notify client initialized successfully");
                }
                else
                {
                    _logger.LogWarning("GOV.UK Notify NotificationClient type not found. Email notifications will not be sent.");
                    _notifyClient = null;
                }
            }
            catch (System.IO.FileNotFoundException ex)
            {
                _logger.LogError(ex, "GOV.UK Notify assembly not found. Email notifications will not be sent. Please ensure the GovukNotify NuGet package is properly installed.");
                _notifyClient = null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize GOV.UK Notify client: {Error}", ex.Message);
                _notifyClient = null;
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

            // Send email via GOV.UK Notify using reflection
            object? notifyResponse = null;
            try
            {
                // Find SendEmail method - try different signatures
                var sendEmailMethod = _notifyClient.GetType().GetMethod("SendEmail", new[] { typeof(string), typeof(string), typeof(Dictionary<string, dynamic>) });
                
                // If not found, try with Dictionary<string, object>
                if (sendEmailMethod == null)
                {
                    sendEmailMethod = _notifyClient.GetType().GetMethod("SendEmail", new[] { typeof(string), typeof(string), typeof(Dictionary<string, object>) });
                }
                
                // If still not found, try to find any SendEmail method and match parameters
                if (sendEmailMethod == null)
                {
                    var allMethods = _notifyClient.GetType().GetMethods().Where(m => m.Name == "SendEmail");
                    foreach (var method in allMethods)
                    {
                        var parameters = method.GetParameters();
                        if (parameters.Length >= 2 && 
                            parameters[0].ParameterType == typeof(string) && 
                            parameters[1].ParameterType == typeof(string))
                        {
                            sendEmailMethod = method;
                            break;
                        }
                    }
                }
                
                if (sendEmailMethod != null)
                {
                    // Convert Dictionary<string, dynamic> to the expected type
                    var methodParams = sendEmailMethod.GetParameters();
                    object[] invokeParams;
                    
                    if (methodParams.Length == 2)
                    {
                        invokeParams = new object[] { recipientEmail, templateId };
                    }
                    else if (methodParams.Length == 3)
                    {
                        // Convert personalisation to match the method's expected type
                        if (methodParams[2].ParameterType == typeof(Dictionary<string, object>))
                        {
                            var personalisationObj = new Dictionary<string, object>();
                            foreach (var kvp in personalisation)
                            {
                                personalisationObj[kvp.Key] = kvp.Value;
                            }
                            invokeParams = new object[] { recipientEmail, templateId, personalisationObj };
                        }
                        else
                        {
                            invokeParams = new object[] { recipientEmail, templateId, personalisation };
                        }
                    }
                    else if (methodParams.Length >= 4)
                    {
                        // GOV.UK Notify v7+ signature:
                        // SendEmail(emailAddress, templateId, personalisation, clientReference, emailReplyToId)
                        // Pass null for all optional parameters beyond personalisation
                        var personalisationObj = new Dictionary<string, object>();
                        foreach (var kvp in personalisation)
                        {
                            personalisationObj[kvp.Key] = kvp.Value;
                        }
                        invokeParams = new object[methodParams.Length];
                        invokeParams[0] = recipientEmail;
                        invokeParams[1] = templateId;
                        invokeParams[2] = methodParams[2].ParameterType == typeof(Dictionary<string, object>)
                            ? (object)personalisationObj
                            : personalisation;
                        for (int i = 3; i < methodParams.Length; i++)
                        {
                            invokeParams[i] = Type.Missing;
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException($"SendEmail method has unexpected number of parameters: {methodParams.Length}");
                    }
                    
                    notifyResponse = sendEmailMethod.Invoke(_notifyClient, invokeParams);
                }
                else
                {
                    // Log available methods for debugging
                    var availableMethods = string.Join(", ", _notifyClient.GetType().GetMethods().Select(m => m.Name));
                    throw new InvalidOperationException($"SendEmail method not found on NotificationClient. Available methods: {availableMethods}");
                }

                // Update log with success
                notificationLog.Status = "sent";
                notificationLog.StatusMessage = "Email sent successfully";
                
                // Get the id property using reflection
                if (notifyResponse != null)
                {
                    var idProperty = notifyResponse.GetType().GetProperty("id");
                    if (idProperty != null)
                {
                    var messageId = idProperty.GetValue(notifyResponse)?.ToString();
                    notificationLog.NotifyMessageId = messageId;
                    notificationLog.SentAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync(cancellationToken);

                    result.Success = true;
                    result.MessageId = messageId;
                    result.SentAt = DateTime.UtcNow;

                    _logger.LogInformation(
                        "Notification sent successfully to {Email} via GOV.UK Notify. Message ID: {MessageId}",
                        recipientEmail,
                        messageId);
                    }
                }
            }
            catch (Exception ex) when (ex.GetType().FullName == "Notify.Exceptions.NotifyClientException" || ex.InnerException?.GetType().FullName == "Notify.Exceptions.NotifyClientException")
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

    public async Task<NotificationResult> SendEmailWithTemplateAsync(
        string recipientEmail,
        string templateId,
        Dictionary<string, dynamic> personalisation,
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

        // Validate template ID
        if (string.IsNullOrWhiteSpace(templateId))
        {
            result.Success = false;
            result.ErrorMessage = "Template ID is required";
            _logger.LogWarning("Attempted to send notification without template ID");
            return result;
        }

        // Log the notification attempt
        var notificationLog = new NotificationLog
        {
            NotificationRuleId = notificationRuleId,
            TriggerCode = triggerCode ?? "contact_change",
            RecipientEmail = recipientEmail,
            Subject = "Contact Change Notification",
            Body = System.Text.Json.JsonSerializer.Serialize(personalisation),
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

            // Send email via GOV.UK Notify using reflection
            object? notifyResponse = null;
            try
            {
                // Find SendEmail method - try different signatures
                var sendEmailMethod = _notifyClient.GetType().GetMethod("SendEmail", new[] { typeof(string), typeof(string), typeof(Dictionary<string, dynamic>) });
                
                // If not found, try with Dictionary<string, object>
                if (sendEmailMethod == null)
                {
                    sendEmailMethod = _notifyClient.GetType().GetMethod("SendEmail", new[] { typeof(string), typeof(string), typeof(Dictionary<string, object>) });
                }
                
                // If still not found, try to find any SendEmail method and match parameters
                if (sendEmailMethod == null)
                {
                    var allMethods = _notifyClient.GetType().GetMethods().Where(m => m.Name == "SendEmail");
                    foreach (var method in allMethods)
                    {
                        var parameters = method.GetParameters();
                        if (parameters.Length >= 2 && 
                            parameters[0].ParameterType == typeof(string) && 
                            parameters[1].ParameterType == typeof(string))
                        {
                            sendEmailMethod = method;
                            break;
                        }
                    }
                }
                
                if (sendEmailMethod != null)
                {
                    // Convert Dictionary<string, dynamic> to the expected type
                    var methodParams = sendEmailMethod.GetParameters();
                    object[] invokeParams;
                    
                    if (methodParams.Length == 2)
                    {
                        invokeParams = new object[] { recipientEmail, templateId };
                    }
                    else if (methodParams.Length == 3)
                    {
                        // Convert personalisation to match the method's expected type
                        if (methodParams[2].ParameterType == typeof(Dictionary<string, object>))
                        {
                            var personalisationObj = new Dictionary<string, object>();
                            foreach (var kvp in personalisation)
                            {
                                personalisationObj[kvp.Key] = kvp.Value;
                            }
                            invokeParams = new object[] { recipientEmail, templateId, personalisationObj };
                        }
                        else
                        {
                            invokeParams = new object[] { recipientEmail, templateId, personalisation };
                        }
                    }
                    else if (methodParams.Length >= 4)
                    {
                        // GOV.UK Notify v7+ signature:
                        // SendEmail(emailAddress, templateId, personalisation, clientReference, emailReplyToId)
                        // Pass null for all optional parameters beyond personalisation
                        var personalisationObj = new Dictionary<string, object>();
                        foreach (var kvp in personalisation)
                        {
                            personalisationObj[kvp.Key] = kvp.Value;
                        }
                        invokeParams = new object[methodParams.Length];
                        invokeParams[0] = recipientEmail;
                        invokeParams[1] = templateId;
                        invokeParams[2] = methodParams[2].ParameterType == typeof(Dictionary<string, object>)
                            ? (object)personalisationObj
                            : personalisation;
                        for (int i = 3; i < methodParams.Length; i++)
                        {
                            invokeParams[i] = Type.Missing;
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException($"SendEmail method has unexpected number of parameters: {methodParams.Length}");
                    }
                    
                    notifyResponse = sendEmailMethod.Invoke(_notifyClient, invokeParams);
                }
                else
                {
                    // Log available methods for debugging
                    var availableMethods = string.Join(", ", _notifyClient.GetType().GetMethods().Select(m => m.Name));
                    throw new InvalidOperationException($"SendEmail method not found on NotificationClient. Available methods: {availableMethods}");
                }

                // Update log with success
                notificationLog.Status = "sent";
                notificationLog.StatusMessage = "Email sent successfully";
                
                // Get the id property using reflection
                if (notifyResponse != null)
                {
                    var idProperty = notifyResponse.GetType().GetProperty("id");
                    if (idProperty != null)
                    {
                        var messageId = idProperty.GetValue(notifyResponse)?.ToString();
                        notificationLog.NotifyMessageId = messageId;
                        notificationLog.SentAt = DateTime.UtcNow;
                        await _context.SaveChangesAsync(cancellationToken);

                        result.Success = true;
                        result.MessageId = messageId;
                        result.SentAt = DateTime.UtcNow;

                        _logger.LogInformation(
                            "Contact change notification sent successfully to {Email} via GOV.UK Notify. Message ID: {MessageId}",
                            recipientEmail,
                            messageId);
                    }
                }
            }
            catch (Exception ex) when (ex.GetType().FullName == "Notify.Exceptions.NotifyClientException" || ex.InnerException?.GetType().FullName == "Notify.Exceptions.NotifyClientException")
            {
                // Handle GOV.UK Notify specific errors
                notificationLog.Status = "failed";
                notificationLog.StatusMessage = ex.Message;
                await _context.SaveChangesAsync(cancellationToken);

                result.Success = false;
                result.ErrorMessage = ex.Message;

                _logger.LogError(
                    ex,
                    "Failed to send contact change notification via GOV.UK Notify to {Email}",
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
                "Unexpected error sending contact change notification to {Email}",
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
