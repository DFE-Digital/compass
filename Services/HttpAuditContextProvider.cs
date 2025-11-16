using System.Security.Claims;
using Compass.Security;

namespace Compass.Services;

public class HttpAuditContextProvider : IAuditContextProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpAuditContextProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? UserId
    {
        get
        {
            var context = _httpContextAccessor.HttpContext;
            return context?.User?.FindFirstValue(CompassClaimTypes.ObjectIdentifier) ??
                context?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        }
    }

    public string? UserEmail
    {
        get
        {
            var context = _httpContextAccessor.HttpContext;
            return context?.User?.FindFirstValue(ClaimTypes.Email) ??
                context?.User?.Identity?.Name;
        }
    }

    public string? UserName
    {
        get
        {
            var context = _httpContextAccessor.HttpContext;
            return context?.User?.Identity?.Name;
        }
    }

    public string? IpAddress
        => _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public string? UserAgent
        => _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].FirstOrDefault();
}

