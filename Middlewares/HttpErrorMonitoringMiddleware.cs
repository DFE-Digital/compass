using Compass.Services;
using Microsoft.AspNetCore.Diagnostics;

namespace Compass.Middlewares;

/// <summary>Logs and optionally emails HTTP 405/500 responses; records exceptions in Application Insights.</summary>
public sealed class HttpErrorMonitoringMiddleware
{
    private readonly RequestDelegate _next;

    public HttpErrorMonitoringMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, IHttpErrorMonitoringService monitoring)
    {
        Exception? captured = null;
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            captured = ex;
            throw;
        }
        finally
        {
            var statusCode = context.Response.StatusCode;
            if (statusCode is 405 or 500)
            {
                var exception = captured
                    ?? context.Features.Get<IExceptionHandlerPathFeature>()?.Error
                    ?? context.Features.Get<IExceptionHandlerFeature>()?.Error;

                try
                {
                    await monitoring.HandleHttpErrorAsync(context, statusCode, exception, context.RequestAborted);
                }
                catch
                {
                    // Never fail the response because monitoring failed
                }
            }
        }
    }
}
