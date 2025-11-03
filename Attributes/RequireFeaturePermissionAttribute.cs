using Compass.Models;
using Compass.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace Compass.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireFeaturePermissionAttribute : Attribute, IAsyncActionFilter
{
    private readonly string _featureCode;
    private readonly PermissionType _permission;

    public RequireFeaturePermissionAttribute(string featureCode, PermissionType permission)
    {
        _featureCode = featureCode;
        _permission = permission;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var userEmail = GetUserEmail(context.HttpContext);
        if (string.IsNullOrEmpty(userEmail))
        {
            context.Result = new UnauthorizedObjectResult(new
            {
                error = new
                {
                    code = "UNAUTHORIZED",
                    message = "You must be signed in to access this resource"
                }
            });
            return;
        }

        var permissionService = context.HttpContext.RequestServices.GetRequiredService<IPermissionService>();
        
        var hasPermission = await permissionService.HasPermissionAsync(userEmail, _featureCode, _permission);
        
        if (!hasPermission)
        {
            context.Result = new ObjectResult(new
            {
                error = new
                {
                    code = "FORBIDDEN",
                    message = "You don't have access to this. If you think you should - ask for permission"
                }
            })
            {
                StatusCode = 403
            };
            return;
        }

        await next();
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

