using System.Security.Claims;

namespace Compass.Services;

public interface IAuditContextProvider
{
    string? UserId { get; }

    string? UserEmail { get; }

    string? UserName { get; }

    string? IpAddress { get; }

    string? UserAgent { get; }
}


