using Compass.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace Compass.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireCentralOpsAdminAttribute : Attribute, IAsyncAuthorizationFilter
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
            
            // Check if user is Super Admin or in Central Operations Admin group
            var isSuperAdmin = await permissionService.IsSuperAdminAsync(userEmail);
            var isInGroup = await permissionService.IsInGroupAsync(userEmail, "Central Operations Admin");
            
            if (!isSuperAdmin && !isInGroup)
            {
                context.Result = new RedirectToActionResult("AccessDenied", "CentralOps", null);
                return;
            }
        }
        catch (Exception)
        {
            // If there's any error checking permissions, deny access and redirect to AccessDenied page
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

