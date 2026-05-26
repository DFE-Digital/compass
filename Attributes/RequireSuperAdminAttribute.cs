using Compass.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace Compass.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireSuperAdminAttribute : Attribute, IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        try
        {
            var userEmail = GetUserEmail(context.HttpContext);
            if (string.IsNullOrEmpty(userEmail))
            {
                context.Result = new RedirectToActionResult("Index", "ModernAdmin", null);
                return;
            }

            var permissionService = context.HttpContext.RequestServices.GetRequiredService<IPermissionService>();
            
            // Check if user is in Super admin group
            var isSuperAdmin = await permissionService.IsSuperAdminAsync(userEmail);
            
            if (!isSuperAdmin)
            {
                context.Result = new RedirectToActionResult("Index", "ModernAdmin", null);
                return;
            }
        }
        catch (Exception)
        {
            // If there's any error checking permissions, deny access
            context.Result = new RedirectToActionResult("Index", "ModernAdmin", null);
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

