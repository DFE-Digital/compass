using Compass.Services;
using Compass.Models;

namespace Compass.Middlewares;

public class ApiAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiAuthenticationMiddleware> _logger;

    public ApiAuthenticationMiddleware(RequestDelegate next, ILogger<ApiAuthenticationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IApiTokenService apiTokenService)
    {
        // Only apply to /api/v1/* endpoints
        if (!context.Request.Path.StartsWithSegments("/api/v1"))
        {
            await _next(context);
            return;
        }

        // Skip health check endpoint
        if (context.Request.Path.Value?.Contains("/health") == true)
        {
            await _next(context);
            return;
        }

        // Extract bearer token
        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new
            {
                error = new
                {
                    code = "UNAUTHORIZED",
                    message = "Missing or invalid Authorization header. Expected format: Bearer {token}"
                }
            });
            return;
        }

        var token = authHeader.Substring("Bearer ".Length).Trim();

        // Validate token
        var apiToken = await apiTokenService.GetByTokenAsync(token);
        if (apiToken == null)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new
            {
                error = new
                {
                    code = "INVALID_TOKEN",
                    message = "Invalid or inactive API token"
                }
            });
            return;
        }

        // Check if token is expired
        if (apiToken.ExpiresAt.HasValue && apiToken.ExpiresAt.Value < DateTime.UtcNow)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new
            {
                error = new
                {
                    code = "TOKEN_EXPIRED",
                    message = "API token has expired"
                }
            });
            return;
        }

        // Store token in context items for use in controllers
        context.Items["ApiToken"] = apiToken;

        // Update last used timestamp (await to avoid concurrent DbContext operations)
        await apiTokenService.UpdateLastUsedAsync(apiToken.Id);

        await _next(context);
    }
}

