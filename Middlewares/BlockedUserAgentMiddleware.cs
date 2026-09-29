using Microsoft.Extensions.Primitives;

namespace Compass.Middlewares;

/// <summary>
/// Rejects scanner user agents before authentication and error monitoring.
/// Detectify's protocol fingerprinter probes HTTP methods and produces HTTP 405 alerts.
/// </summary>
public sealed class BlockedUserAgentMiddleware
{
    private static readonly string[] BlockedMarkers = ["Detectify"];

    private readonly RequestDelegate _next;

    public BlockedUserAgentMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        if (IsBlocked(context.Request.Headers.UserAgent))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        await _next(context);
    }

    private static bool IsBlocked(StringValues userAgent)
    {
        foreach (var value in userAgent)
        {
            if (string.IsNullOrEmpty(value))
                continue;

            foreach (var marker in BlockedMarkers)
            {
                if (value.Contains(marker, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }

        return false;
    }
}
