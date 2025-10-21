using System.Diagnostics;
using System.Text;
using Compass.Data;
using Compass.Models;

namespace Compass.Middlewares;

public class ApiLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiLoggingMiddleware> _logger;

    public ApiLoggingMiddleware(RequestDelegate next, ILogger<ApiLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, CompassDbContext dbContext)
    {
        // Only log /api/v1/* endpoints
        if (!context.Request.Path.StartsWithSegments("/api/v1"))
        {
            await _next(context);
            return;
        }

        // Skip health check endpoint logging
        if (context.Request.Path.Value?.Contains("/health") == true)
        {
            await _next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();

        // Get API token from context
        var apiToken = context.Items["ApiToken"] as ApiToken;
        if (apiToken == null)
        {
            await _next(context);
            return;
        }

        // Read request body
        context.Request.EnableBuffering();
        var requestBody = await ReadRequestBodyAsync(context.Request);

        // Capture original response body stream
        var originalResponseBody = context.Response.Body;

        using var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        Exception? exception = null;
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            exception = ex;
            throw;
        }
        finally
        {
            stopwatch.Stop();

            // Read response body
            responseBodyStream.Seek(0, SeekOrigin.Begin);
            var responseBody = await new StreamReader(responseBodyStream).ReadToEndAsync();
            responseBodyStream.Seek(0, SeekOrigin.Begin);

            // Copy response body back to original stream
            await responseBodyStream.CopyToAsync(originalResponseBody);
            context.Response.Body = originalResponseBody;

            // Log the request
            try
            {
                var log = new ApiRequestLog
                {
                    ApiTokenId = apiToken.Id,
                    RequestTimestamp = DateTime.UtcNow,
                    HttpMethod = context.Request.Method,
                    RequestPath = context.Request.Path.Value ?? string.Empty,
                    QueryString = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : null,
                    RequestBody = TruncateIfNeeded(requestBody, 10000),
                    ResponseStatusCode = context.Response.StatusCode,
                    ResponseBody = TruncateIfNeeded(responseBody, 10000),
                    ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds,
                    IpAddress = context.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = context.Request.Headers["User-Agent"].FirstOrDefault(),
                    IsSuccess = context.Response.StatusCode >= 200 && context.Response.StatusCode < 300,
                    ErrorMessage = exception?.Message
                };

                dbContext.ApiRequestLogs.Add(log);
                await dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log API request");
            }
        }
    }

    private static async Task<string?> ReadRequestBodyAsync(HttpRequest request)
    {
        if (request.ContentLength == null || request.ContentLength == 0)
        {
            return null;
        }

        request.Body.Position = 0;
        using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        request.Body.Position = 0;

        return body;
    }

    private static string? TruncateIfNeeded(string? text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
        {
            return text;
        }

        return text.Substring(0, maxLength) + "... [truncated]";
    }
}

