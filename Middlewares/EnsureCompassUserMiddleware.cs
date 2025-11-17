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

        // Add database role to claims if user exists
        if (user != null && context.User.Identity is ClaimsIdentity identity)
        {
            try
            {
                // Map UserRole enum to role claim strings
                var roleClaimValue = user.Role switch
                {
                    UserRole.Admin => "Admin",
                    UserRole.SuperAdmin => "SuperAdmin",
                    UserRole.Reporter => "Reporter",
                    UserRole.Visitor => "Visitor",
                    _ => "Visitor"
                };

                // Check if the role claim already exists
                var existingRoleClaim = identity.FindFirst(c => 
                    c.Type == ClaimTypes.Role && 
                    c.Value.Equals(roleClaimValue, StringComparison.OrdinalIgnoreCase));

                // Only add if it doesn't exist or is different
                if (existingRoleClaim == null)
                {
                    // Remove any existing Compass role claims to avoid duplicates
                    var compassRoleClaims = identity.FindAll(c => 
                        c.Type == ClaimTypes.Role && 
                        (c.Value == "Admin" || c.Value == "SuperAdmin" || c.Value == "Reporter" || c.Value == "Visitor"))
                        .ToList();
                    
                    foreach (var claim in compassRoleClaims)
                    {
                        identity.RemoveClaim(claim);
                    }

                    // Add the role claim from database
                    identity.AddClaim(new Claim(ClaimTypes.Role, roleClaimValue));
                }
            }
            catch (Exception ex)
            {
                // Identity might be read-only in some authentication schemes
                // Log the error but continue - role checks will fall back to database lookups
                _logger.LogWarning(ex, "Unable to add role claim to identity for user {Email}. Role checks may need to query database directly.", user.Email);
            }
        }

        await _next(context);
    }
}

