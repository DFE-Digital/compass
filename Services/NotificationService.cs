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
    /// <summary>Notify client built from <c>GovUkNotify:CompassKey</c>; used for CMS access grant emails when configured.</summary>
    private readonly object? _notifyClientCompass;

    public NotificationService(
        IConfiguration configuration,
        ILogger<NotificationService> logger,
        CompassDbContext context)
    {
        _configuration = configuration;
        _logger = logger;
        _context = context;

        var apiKey = _configuration["GovUkNotify:ApiKey"];
        var compassKey = _configuration["GovUkNotify:CompassKey"];
        var templateId = _configuration["GovUkNotify:TemplateId"];

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("GOV.UK Notify API key (GovUkNotify:ApiKey) is not configured. Most email notifications will not be sent.");
        }

        _notifyClient = CreateNotificationClient(apiKey, "GovUkNotify:ApiKey");
        _notifyClientCompass = CreateNotificationClient(compassKey, "GovUkNotify:CompassKey");

        if (_notifyClientCompass != null)
            _logger.LogInformation("GOV.UK Notify CompassKey client initialised (used for CMS access request emails).");

        if (string.IsNullOrWhiteSpace(templateId))
        {
            _logger.LogWarning("GOV.UK Notify template ID is not configured. Using default template.");
        }
    }

    private object? CreateNotificationClient(string? apiKey, string configKeyName)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            return null;

        try
        {
            var notifyAssembly = Assembly.Load("GovukNotify");
            var clientType = notifyAssembly.GetType("Notify.Client.NotificationClient");
            if (clientType != null)
            {
                var client = Activator.CreateInstance(clientType, apiKey);
                _logger.LogInformation("GOV.UK Notify client initialised for {ConfigKey}", configKeyName);
                return client;
            }

            _logger.LogWarning("GOV.UK Notify NotificationClient type not found ({ConfigKey}).", configKeyName);
            return null;
        }
        catch (System.IO.FileNotFoundException ex)
        {
            _logger.LogError(ex, "GOV.UK Notify assembly not found ({ConfigKey}). Ensure the GovukNotify package is installed.", configKeyName);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialise GOV.UK Notify client ({ConfigKey}): {Error}", configKeyName, ex.Message);
            return null;
        }
    }

    /// <summary>
    /// CMS access grant emails use the CompassKey client when <c>GovUkNotify:CompassKey</c> is set; otherwise the default ApiKey client.
    /// </summary>
    private object? ResolveNotifyClientForSend(string? triggerCode)
    {
        if (string.Equals(triggerCode, "cms_access_request", StringComparison.OrdinalIgnoreCase)
            && _notifyClientCompass != null)
            return _notifyClientCompass;

        return _notifyClient;
    }

    /// <summary>GovukNotify v7+ uses <c>SendEmailAsync</c>; older versions use sync <c>SendEmail</c>.</summary>
    private static MethodInfo? ResolveGovUkNotifySendMethod(Type notifyClientType)
    {
        foreach (var methodName in new[] { "SendEmailAsync", "SendEmail" })
        {
            var m = notifyClientType.GetMethod(methodName, new[] { typeof(string), typeof(string), typeof(Dictionary<string, dynamic>) });
            if (m != null)
                return m;
            m = notifyClientType.GetMethod(methodName, new[] { typeof(string), typeof(string), typeof(Dictionary<string, object>) });
            if (m != null)
                return m;
        }

        foreach (var methodName in new[] { "SendEmailAsync", "SendEmail" })
        {
            var best = notifyClientType.GetMethods()
                .Where(x =>
                    x.Name == methodName &&
                    x.GetParameters().Length >= 2 &&
                    x.GetParameters()[0].ParameterType == typeof(string) &&
                    x.GetParameters()[1].ParameterType == typeof(string))
                .OrderByDescending(x => x.GetParameters().Length)
                .FirstOrDefault();
            if (best != null)
                return best;
        }

        return null;
    }

    /// <summary><see cref="System.Reflection.MethodInfo.Invoke"/> wraps failures in <see cref="TargetInvocationException"/>.</summary>
    private static Exception UnwrapReflectionException(Exception ex)
    {
        var e = ex;
        while (e is System.Reflection.TargetInvocationException { InnerException: { } inner })
            e = inner;
        return e;
    }

    /// <summary>GovukNotify v7 returns <see cref="Task{TResult}"/> from async send methods.</summary>
    private static async Task<object?> AwaitNotifySendResultAsync(object? invokeResult, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (invokeResult is null)
            return null;
        if (invokeResult is not Task task)
            return invokeResult;

        await task.ConfigureAwait(false);
        var taskType = task.GetType();
        if (taskType.IsGenericType)
            return taskType.GetProperty("Result")?.GetValue(task);

        return null;
    }

    private static string? TryGetNotifyResponseMessageId(object? response)
    {
        if (response is null)
            return null;
        var t = response.GetType();
        var idProp = t.GetProperty("id", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
            ?? t.GetProperty("notification_id", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        return idProp?.GetValue(response)?.ToString();
    }

    public async Task<NotificationResult> SendEmailAsync(
        string recipientEmail,
        string subject,
        string body,
        string? triggerCode = null,
        int? notificationRuleId = null,
        Dictionary<string, object>? contextData = null,
        string? notifyTemplateId = null,
        IReadOnlyDictionary<string, object>? notifyPersonalisationExtras = null,
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

            var notifyClient = ResolveNotifyClientForSend(triggerCode);
            if (notifyClient == null)
            {
                notificationLog.Status = "failed";
                notificationLog.StatusMessage = "GOV.UK Notify client not initialized";
                await _context.SaveChangesAsync(cancellationToken);

                result.Success = false;
                result.ErrorMessage = "Notification service not configured";
                _logger.LogWarning(
                    "Notification not sent - GOV.UK Notify client not initialised (set GovUkNotify:ApiKey, or GovUkNotify:CompassKey for CMS access request emails).");
                return result;
            }

            // Template: explicit override, then GovUkNotify:TemplateId, then SubjectBodyEmailTemplateId / CmsAccessRequestTemplateId
            var templateId = notifyTemplateId;
            if (string.IsNullOrWhiteSpace(templateId))
                templateId = _configuration["GovUkNotify:TemplateId"];
            if (string.IsNullOrWhiteSpace(templateId))
                templateId = _configuration["GovUkNotify:SubjectBodyEmailTemplateId"];
            if (string.IsNullOrWhiteSpace(templateId))
                templateId = _configuration["GovUkNotify:CmsAccessRequestTemplateId"];
            if (string.IsNullOrWhiteSpace(templateId))
            {
                notificationLog.Status = "failed";
                notificationLog.StatusMessage = "Template ID not configured";
                await _context.SaveChangesAsync(cancellationToken);

                result.Success = false;
                result.ErrorMessage = "Template ID not configured";
                _logger.LogWarning(
                    "Notification not sent - set GovUkNotify:TemplateId, GovUkNotify:SubjectBodyEmailTemplateId, or GovUkNotify:CmsAccessRequestTemplateId (subject/body personalisation; CMS grants may also send cms_name, registration_link, requestor_first_name).");
                return result;
            }

            // Prepare personalisation for GOV.UK Notify — required keys: subject, body (see GovUkNotify:CmsAccessRequestTemplateId for CMS-specific templates).
            var personalisation = new Dictionary<string, dynamic>
            {
                { "subject", subject },
                { "body", body }
            };
            if (notifyPersonalisationExtras != null)
            {
                foreach (var kvp in notifyPersonalisationExtras)
                    personalisation[kvp.Key] = kvp.Value;
            }

            // Send email via GOV.UK Notify using reflection
            object? notifyResponse = null;
            try
            {
                var sendEmailMethod = ResolveGovUkNotifySendMethod(notifyClient.GetType());

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
                    
                    var rawInvoke = sendEmailMethod.Invoke(notifyClient, invokeParams);
                    notifyResponse = await AwaitNotifySendResultAsync(rawInvoke, cancellationToken);
                }
                else
                {
                    var availableMethods = string.Join(", ", notifyClient.GetType().GetMethods().Select(m => m.Name));
                    throw new InvalidOperationException($"SendEmail/SendEmailAsync not found on NotificationClient. Available methods: {availableMethods}");
                }

                if (notifyResponse == null)
                {
                    notificationLog.Status = "failed";
                    notificationLog.StatusMessage = "No response from GOV.UK Notify";
                    await _context.SaveChangesAsync(cancellationToken);

                    result.Success = false;
                    result.ErrorMessage = "No response from GOV.UK Notify";
                    return result;
                }

                var messageId = TryGetNotifyResponseMessageId(notifyResponse);

                notificationLog.Status = "sent";
                notificationLog.StatusMessage = "Email sent successfully";
                notificationLog.NotifyMessageId = messageId;
                notificationLog.SentAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);

                result.Success = true;
                result.MessageId = messageId;
                result.SentAt = DateTime.UtcNow;

                if (string.IsNullOrEmpty(messageId))
                    _logger.LogWarning("GOV.UK Notify response had no message id; send still treated as success for {Email}", recipientEmail);
                else
                    _logger.LogInformation(
                        "Notification sent successfully to {Email} via GOV.UK Notify. Message ID: {MessageId}",
                        recipientEmail,
                        messageId);
            }
            catch (Exception ex)
            {
                var root = UnwrapReflectionException(ex);
                notificationLog.Status = "failed";
                notificationLog.StatusMessage = root.Message;
                await _context.SaveChangesAsync(cancellationToken);

                result.Success = false;
                result.ErrorMessage = root.Message;

                _logger.LogError(
                    root,
                    "Failed to send notification via GOV.UK Notify to {Email}",
                    recipientEmail);

                return result;
            }
        }
        catch (Exception ex)
        {
            // Handle general errors
            var root = UnwrapReflectionException(ex);
            notificationLog.Status = "failed";
            notificationLog.StatusMessage = root.Message;
            await _context.SaveChangesAsync(cancellationToken);

            result.Success = false;
            result.ErrorMessage = root.Message;

            _logger.LogError(
                root,
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
                var sendEmailMethod = ResolveGovUkNotifySendMethod(_notifyClient.GetType());

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
                    
                    var rawInvoke = sendEmailMethod.Invoke(_notifyClient, invokeParams);
                    notifyResponse = await AwaitNotifySendResultAsync(rawInvoke, cancellationToken);
                }
                else
                {
                    var availableMethods = string.Join(", ", _notifyClient.GetType().GetMethods().Select(m => m.Name));
                    throw new InvalidOperationException($"SendEmail/SendEmailAsync not found on NotificationClient. Available methods: {availableMethods}");
                }

                if (notifyResponse == null)
                {
                    notificationLog.Status = "failed";
                    notificationLog.StatusMessage = "No response from GOV.UK Notify";
                    await _context.SaveChangesAsync(cancellationToken);

                    result.Success = false;
                    result.ErrorMessage = "No response from GOV.UK Notify";
                    return result;
                }

                var messageId = TryGetNotifyResponseMessageId(notifyResponse);

                notificationLog.Status = "sent";
                notificationLog.StatusMessage = "Email sent successfully";
                notificationLog.NotifyMessageId = messageId;
                notificationLog.SentAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);

                result.Success = true;
                result.MessageId = messageId;
                result.SentAt = DateTime.UtcNow;

                if (string.IsNullOrEmpty(messageId))
                    _logger.LogWarning("GOV.UK Notify response had no message id; send still treated as success for {Email}", recipientEmail);
                else
                    _logger.LogInformation(
                        "Contact change notification sent successfully to {Email} via GOV.UK Notify. Message ID: {MessageId}",
                        recipientEmail,
                        messageId);
            }
            catch (Exception ex)
            {
                var root = UnwrapReflectionException(ex);
                notificationLog.Status = "failed";
                notificationLog.StatusMessage = root.Message;
                await _context.SaveChangesAsync(cancellationToken);

                result.Success = false;
                result.ErrorMessage = root.Message;

                _logger.LogError(
                    root,
                    "Failed to send contact change notification via GOV.UK Notify to {Email}",
                    recipientEmail);

                return result;
            }
        }
        catch (Exception ex)
        {
            // Handle general errors
            var root = UnwrapReflectionException(ex);
            notificationLog.Status = "failed";
            notificationLog.StatusMessage = root.Message;
            await _context.SaveChangesAsync(cancellationToken);

            result.Success = false;
            result.ErrorMessage = root.Message;

            _logger.LogError(
                root,
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
