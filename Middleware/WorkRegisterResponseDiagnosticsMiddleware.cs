using System.Diagnostics;
using System.Text.Json;
using Compass.Configuration;
using Compass.Services;
using Microsoft.Extensions.Options;

namespace Compass.Middleware;

/// <summary>Logs request/response for work register pages and export to <see cref="WorkRegisterPerfFileLog"/>.</summary>
public sealed class WorkRegisterResponseDiagnosticsMiddleware
{
    private static readonly PathString[] RegisterViewPaths =
    [
        new("/modern/work/all"),
        new("/ModernWork/AllWork"),
        new("/modern/operations/manage-work"),
        new("/ModernOperations/ManageWork"),
        new("/modern/reporting/thematic"),
    ];

    private static readonly PathString[] ExportPaths =
    [
        new("/modern/work/export-register"),
    ];

    private readonly RequestDelegate _next;
    private readonly ILogger<WorkRegisterResponseDiagnosticsMiddleware> _logger;
    private readonly WorkRegisterDiagnosticsOptions _options;
    private readonly WorkRegisterPerfFileLog _fileLog;

    public WorkRegisterResponseDiagnosticsMiddleware(
        RequestDelegate next,
        ILogger<WorkRegisterResponseDiagnosticsMiddleware> logger,
        IOptions<WorkRegisterDiagnosticsOptions> options,
        WorkRegisterPerfFileLog fileLog)
    {
        _next = next;
        _logger = logger;
        _options = options.Value;
        _fileLog = fileLog;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_options.Enabled || !IsDiagnosticsRequest(context.Request))
        {
            await _next(context);
            return;
        }

        var isExport = IsExportRequest(context.Request);
        var sw = Stopwatch.StartNew();
        var method = context.Request.Method;
        var path = context.Request.Path.Value ?? "";
        var query = context.Request.QueryString.Value ?? "";

        using var apiScope = WorkRegisterDiagnosticsScope.Begin();

        if (_options.LogApiData)
        {
            var queryParams = context.Request.Query.ToDictionary(
                q => q.Key,
                q => q.Value.Count == 1 ? q.Value[0] : q.Value.ToArray() as object);
            _fileLog.WriteJsonBlock("API REQUEST (HTTP)", new
            {
                method,
                path,
                query = queryParams,
                note = isExport ? "export uses BuildWorkRegisterExportRowsAsync (tab-scoped)" : null,
            });
        }
        else
        {
            var requestLine =
                $"HTTP {method} {path}{query}" +
                (isExport ? " | export-current-view" : "");
            await _fileLog.WriteAsync($"---------- {requestLine}");
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
            sw.Stop();

            if (_options.LogSqlCommands)
            {
                var sqlEntries = WorkRegisterDiagnosticsScope.TakeSqlBuffer();
                if (sqlEntries is { Count: > 0 })
                    _fileLog.WriteSqlSummary(sqlEntries);
            }
            else
            {
                WorkRegisterDiagnosticsScope.TakeSqlBuffer();
            }

            buffer.Position = 0;
            var bytes = buffer.Length;
            var status = context.Response.StatusCode;
            var contentType = context.Response.ContentType ?? "(none)";

            var responseMeta = new
            {
                method,
                path,
                query = context.Request.QueryString.Value,
                status,
                durationMs = sw.ElapsedMilliseconds,
                bodyBytes = bytes,
                contentType,
            };

            if (_options.LogApiData && contentType.Contains("json", StringComparison.OrdinalIgnoreCase))
            {
                buffer.Position = 0;
                using var reader = new StreamReader(buffer, leaveOpen: true);
                var bodyText = await reader.ReadToEndAsync();
                buffer.Position = 0;
                object? bodyJson;
                try
                {
                    bodyJson = JsonSerializer.Deserialize<JsonElement>(bodyText);
                }
                catch
                {
                    bodyJson = bodyText.Length > _fileLog.MaxJsonChars
                        ? bodyText[.._fileLog.MaxJsonChars] + "…"
                        : bodyText;
                }

                _fileLog.WriteJsonBlock("API RESPONSE (HTTP)", new { responseMeta, body = bodyJson });
            }
            else
            {
                var responseLine =
                    $"API RESPONSE (HTTP) status={status} durationMs={sw.ElapsedMilliseconds} " +
                    $"bodyBytes={bytes:N0} (~{bytes / 1024.0:F1} KB) contentType={contentType} " +
                    $"(HTML/view payload is in API RESPONSE (BuildWorkRegisterAsync) blocks above)";
                _logger.LogInformation("WorkRegister {ResponseLine}", responseLine);
                await _fileLog.WriteAsync(responseLine);
            }

            buffer.Position = 0;
            context.Response.Body = originalBody;
            await buffer.CopyToAsync(originalBody);
        }
    }

    private static bool IsDiagnosticsRequest(HttpRequest request)
    {
        if (!HttpMethods.IsGet(request.Method))
            return false;

        return IsRegisterViewRequest(request) || IsExportRequest(request);
    }

    private static bool IsRegisterViewRequest(HttpRequest request)
    {
        var path = request.Path;
        foreach (var p in RegisterViewPaths)
        {
            if (path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static bool IsExportRequest(HttpRequest request)
    {
        var path = request.Path;
        foreach (var p in ExportPaths)
        {
            if (path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return path.Value?.Contains("export-register", StringComparison.OrdinalIgnoreCase) == true;
    }
}
