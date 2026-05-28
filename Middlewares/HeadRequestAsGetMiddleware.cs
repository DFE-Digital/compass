namespace Compass.Middlewares;

/// <summary>
/// Routes HEAD requests to the same endpoints as GET (link checkers, probes, App Insights).
/// Discards the response body so HEAD stays lightweight.
/// </summary>
public sealed class HeadRequestAsGetMiddleware
{
    private readonly RequestDelegate _next;

    public HeadRequestAsGetMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        if (!HttpMethods.IsHead(context.Request.Method))
        {
            await _next(context);
            return;
        }

        context.Request.Method = HttpMethods.Get;
        var originalBody = context.Response.Body;
        try
        {
            context.Response.Body = Stream.Null;
            await _next(context);
        }
        finally
        {
            context.Response.Body = originalBody;
        }
    }
}
