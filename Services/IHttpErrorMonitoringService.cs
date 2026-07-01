namespace Compass.Services;

public interface IHttpErrorMonitoringService
{
    Task HandleHttpErrorAsync(
        HttpContext context,
        int statusCode,
        Exception? exception,
        CancellationToken cancellationToken = default);
}
