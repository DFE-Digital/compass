using Compass.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace Compass.Attributes;

/// <summary>
/// Allows Super admin, Central Operations Admin, or Admin group members to review API key requests.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireApiTokenReviewerAttribute : Attribute, IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var email = GetUserEmail(context.HttpContext);
        if (string.IsNullOrWhiteSpace(email))
        {
            context.Result = new RedirectToActionResult("Index", "ModernAdmin", new { panel = "api-token-requests" });
            return;
        }

        var permissionService = context.HttpContext.RequestServices.GetRequiredService<IPermissionService>();
        if (await permissionService.IsOperationConsoleUserAsync(email))
            return;

        context.Result = new RedirectToActionResult("Index", "ModernAdmin", new { panel = "api-token-requests" });
    }

    private static string GetUserEmail(HttpContext httpContext)
    {
        var user = httpContext.User;
        if (user?.Identity?.IsAuthenticated != true)
            return string.Empty;

        return user.Identity?.Name
            ?? user.FindFirst(ClaimTypes.Email)?.Value
            ?? user.FindFirst("preferred_username")?.Value
            ?? user.FindFirst("email")?.Value
            ?? string.Empty;
    }
}
