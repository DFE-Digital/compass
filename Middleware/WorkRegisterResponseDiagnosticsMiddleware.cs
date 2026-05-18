using Compass.Configuration;
using Microsoft.Extensions.Options;

namespace Compass.Middleware;

/// <summary>Logs HTML response size for work register pages (All work / Manage work).</summary>
public sealed class WorkRegisterResponseDiagnosticsMiddleware
{
    private static readonly PathString[] RegisterPaths =
    [
        new("/modern/work/all"),
        new("/ModernWork/AllWork"),
        new("/modern/operations/manage-work"),
        new("/ModernOperations/ManageWork"),
    ];

    private readonly RequestDelegate _next;
    private readonly ILogger<WorkRegisterResponseDiagnosticsMiddleware> _logger;
    private readonly WorkRegisterDiagnosticsOptions _options;

    public WorkRegisterResponseDiagnosticsMiddleware(
        RequestDelegate next,
        ILogger<WorkRegisterResponseDiagnosticsMiddleware> logger,
        IOptions<WorkRegisterDiagnosticsOptions> options)
    {
        _next = next;
        _logger = logger;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_options.Enabled || !_options.LogHtmlResponseSize || !IsRegisterRequest(context.Request))
        {
            await _next(context);
            return;
        }

        var originalBody = context.Response.Body;
        await using var buffer = new MemoryStream();
        context.Response.Body = buffer;

        try
        {
            await _next(context);
        }
        finally
        {
            buffer.Position = 0;
            var bytes = buffer.Length;
            var status = context.Response.StatusCode;
            _logger.LogInformation(
                "WorkRegister HTTP response {Method} {Path}{Query} status={Status} bodyBytes={BodyBytes:N0} (~{BodyKb:F1} KB)",
                context.Request.Method,
                context.Request.Path.Value,
                context.Request.QueryString.Value,
                status,
                bytes,
                bytes / 1024.0);

            buffer.Position = 0;
            context.Response.Body = originalBody;
            await buffer.CopyToAsync(originalBody);
        }
    }

    private static bool IsRegisterRequest(HttpRequest request)
    {
        if (!HttpMethods.IsGet(request.Method))
            return false;

        var path = request.Path;
        foreach (var p in RegisterPaths)
        {
            if (path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
