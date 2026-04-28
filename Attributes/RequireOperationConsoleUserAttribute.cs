using System.Security.Claims;
using Compass.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Compass.Attributes;

/// <summary>Restricts to users in the <c>Super admin</c>, <c>Central Operations Admin</c>, or <c>Admin</c> group.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireOperationConsoleUserAttribute : Attribute, IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        try
        {
            var userEmail = GetUserEmail(context.HttpContext);
            if (string.IsNullOrEmpty(userEmail))
            {
                context.Result = new RedirectToActionResult("AccessDenied", "CentralOps", null);
                return;
            }

            var permissionService = context.HttpContext.RequestServices.GetRequiredService<IPermissionService>();
            if (!await permissionService.IsOperationConsoleUserAsync(userEmail))
                context.Result = new RedirectToActionResult("AccessDenied", "CentralOps", null);
        }
        catch (Exception)
        {
            context.Result = new RedirectToActionResult("AccessDenied", "CentralOps", null);
        }
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
