using System.Security.Claims;
using Compass.Data;
using Compass.Models;
using Compass.Security;
using Compass.Services;
using Microsoft.EntityFrameworkCore;

namespace Compass.Middlewares;

public class EnsureCompassUserMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<EnsureCompassUserMiddleware> _logger;

    public EnsureCompassUserMiddleware(RequestDelegate next, ILogger<EnsureCompassUserMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        CompassDbContext dbContext,
        IUserDirectoryService userDirectoryService)
    {
        if (context.User?.Identity?.IsAuthenticated != true)
        {
            await _next(context);
            return;
        }

        var cancellationToken = context.RequestAborted;
        var emailClaim = context.User.FindFirstValue(ClaimTypes.Email)
            ?? context.User.Identity?.Name;
        var objectIdClaim = context.User.FindFirstValue(CompassClaimTypes.ObjectIdentifier);
        User? user = null;

        if (Guid.TryParse(objectIdClaim, out var objectId))
        {
            user = await dbContext.Users
                .FirstOrDefaultAsync(u => u.AzureObjectId == objectId.ToString(), cancellationToken);

            if (user == null)
            {
                try
                {
                    user = await userDirectoryService.EnsureUserAsync(objectId, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Unable to ensure directory user {ObjectId}", objectId);
                }
            }
        }

        if (user == null && !string.IsNullOrWhiteSpace(emailClaim))
        {
            var normalizedEmail = emailClaim.Trim().ToLowerInvariant();
            user = await dbContext.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail, cancellationToken);
        }

        if (user == null && !string.IsNullOrWhiteSpace(emailClaim))
        {
            user = new User
            {
                Email = emailClaim.Trim().ToLowerInvariant(),
                Name = context.User.Identity?.Name ?? emailClaim,
                Role = UserRole.Visitor,
                AzureObjectId = objectIdClaim,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        else if (user != null && string.IsNullOrWhiteSpace(user.AzureObjectId) && Guid.TryParse(objectIdClaim, out var objectGuid))
        {
            user.AzureObjectId = objectGuid.ToString();
            user.UpdatedAt = DateTime.UtcNow;
            dbContext.Users.Update(user);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        await _next(context);
    }
}

