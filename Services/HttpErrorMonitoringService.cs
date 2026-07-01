using System.Diagnostics;
using System.Text;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Caching.Memory;

namespace Compass.Services;

public sealed class HttpErrorMonitoringService : IHttpErrorMonitoringService
{
    private static readonly TimeSpan EmailThrottle = TimeSpan.FromMinutes(15);

    private readonly IHttpErrorEmailSettingsService _settingsService;
    private readonly INotificationService _notificationService;
    private readonly TelemetryClient? _telemetryClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<HttpErrorMonitoringService> _logger;
    private readonly IHostEnvironment _environment;

    public HttpErrorMonitoringService(
        IHttpErrorEmailSettingsService settingsService,
        INotificationService notificationService,
        IMemoryCache cache,
        ILogger<HttpErrorMonitoringService> logger,
        IHostEnvironment environment,
        TelemetryClient? telemetryClient = null)
    {
        _settingsService = settingsService;
        _notificationService = notificationService;
        _cache = cache;
        _logger = logger;
        _environment = environment;
        _telemetryClient = telemetryClient;
    }

    public async Task HandleHttpErrorAsync(
        HttpContext context,
        int statusCode,
        Exception? exception,
        CancellationToken cancellationToken = default)
    {
        if (statusCode is not (405 or 500))
            return;

        if (ShouldSkipRequest(context))
            return;

        var path = context.Request.Path.Value ?? "/";
        var method = context.Request.Method;
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
        var user = context.User?.Identity?.Name ?? "anonymous";

        LogToApplicationInsights(statusCode, method, path, traceId, user, exception);
        LogToStructuredLogger(statusCode, method, path, traceId, user, exception);

        await TrySendEmailAsync(context, statusCode, method, path, traceId, user, exception, cancellationToken);
    }

    private void LogToApplicationInsights(
        int statusCode,
        string method,
        string path,
        string traceId,
        string user,
        Exception? exception)
    {
        if (_telemetryClient == null)
            return;

        var properties = BuildProperties(statusCode, method, path, traceId, user, exception);

        if (exception != null)
        {
            _telemetryClient.TrackException(exception, properties);
            return;
        }

        var message = $"HTTP {statusCode} {method} {path}";
        _telemetryClient.TrackTrace(
            message,
            SeverityLevel.Error,
            properties);
    }

    private void LogToStructuredLogger(
        int statusCode,
        string method,
        string path,
        string traceId,
        string user,
        Exception? exception)
    {
        if (exception != null)
        {
            _logger.LogError(
                exception,
                "HTTP {StatusCode} {Method} {Path} (trace: {TraceId}, user: {User})",
                statusCode, method, path, traceId, user);
            return;
        }

        _logger.LogError(
            "HTTP {StatusCode} {Method} {Path} (trace: {TraceId}, user: {User}) — no exception was thrown; response returned status only",
            statusCode, method, path, traceId, user);
    }

    private async Task TrySendEmailAsync(
        HttpContext context,
        int statusCode,
        string method,
        string path,
        string traceId,
        string user,
        Exception? exception,
        CancellationToken cancellationToken)
    {
        var settings = await _settingsService.GetOrCreateAsync(cancellationToken);
        if (!settings.IsEnabled || string.IsNullOrWhiteSpace(settings.ContactEmail))
            return;

        var throttleKey = $"http-error-email:{statusCode}:{method}:{path}";
        if (_cache.TryGetValue(throttleKey, out _))
            return;

        _cache.Set(throttleKey, true, EmailThrottle);

        var subject = $"[{_environment.EnvironmentName}] COMPASS HTTP {statusCode}: {method} {path}";
        var body = BuildEmailBody(context, statusCode, method, path, traceId, user, exception);

        var result = await _notificationService.SendEmailAsync(
            settings.ContactEmail.Trim(),
            subject,
            body,
            triggerCode: "http_error_alert",
            contextData: new Dictionary<string, object>
            {
                ["statusCode"] = statusCode,
                ["method"] = method,
                ["path"] = path,
                ["traceId"] = traceId,
            },
            cancellationToken: cancellationToken);

        if (!result.Success)
        {
            _logger.LogWarning(
                "Failed to send HTTP error alert email for {StatusCode} {Method} {Path}: {Error}",
                statusCode, method, path, result.ErrorMessage);
        }
    }

    private static bool ShouldSkipRequest(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";
        if (path.StartsWith("/css/", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/js/", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/lib/", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/_content/", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/health", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (path.Equals("/Home/Error", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/Home/NotFound", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    private static Dictionary<string, string> BuildProperties(
        int statusCode,
        string method,
        string path,
        string traceId,
        string user,
        Exception? exception)
    {
        var properties = new Dictionary<string, string>
        {
            ["statusCode"] = statusCode.ToString(),
            ["httpMethod"] = method,
            ["requestPath"] = path,
            ["traceId"] = traceId,
            ["user"] = user,
        };

        if (exception != null)
        {
            properties["exceptionType"] = exception.GetType().FullName ?? exception.GetType().Name;
            properties["exceptionMessage"] = exception.Message;
            properties["stackTrace"] = exception.StackTrace ?? string.Empty;
            if (exception.InnerException != null)
            {
                properties["innerExceptionType"] = exception.InnerException.GetType().FullName
                    ?? exception.InnerException.GetType().Name;
                properties["innerExceptionMessage"] = exception.InnerException.Message;
                properties["innerStackTrace"] = exception.InnerException.StackTrace ?? string.Empty;
            }
        }

        return properties;
    }

    private string BuildEmailBody(
        HttpContext context,
        int statusCode,
        string method,
        string path,
        string traceId,
        string user,
        Exception? exception)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"COMPASS returned HTTP {statusCode}.");
        sb.AppendLine();
        sb.AppendLine($"Environment: {_environment.EnvironmentName}");
        sb.AppendLine($"When (UTC): {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"Request: {method} {path}{context.Request.QueryString}");
        sb.AppendLine($"Trace ID: {traceId}");
        sb.AppendLine($"User: {user}");
        sb.AppendLine($"Host: {context.Request.Host}");
        sb.AppendLine($"User-Agent: {context.Request.Headers.UserAgent}");

        if (exception != null)
        {
            sb.AppendLine();
            sb.AppendLine("Exception:");
            sb.AppendLine(exception.ToString());
        }
        else
        {
            sb.AppendLine();
            sb.AppendLine("No exception was thrown — the application returned this status code directly.");
        }

        return sb.ToString();
    }
}
