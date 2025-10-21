using Compass.Models;
using Compass.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Compass.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireApiPermissionAttribute : Attribute, IAsyncActionFilter
{
    private readonly string _resource;
    private readonly string _operation;

    public RequireApiPermissionAttribute(string resource, string operation)
    {
        _resource = resource;
        _operation = operation;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var apiToken = context.HttpContext.Items["ApiToken"] as ApiToken;
        if (apiToken == null)
        {
            context.Result = new UnauthorizedObjectResult(new
            {
                error = new
                {
                    code = "UNAUTHORIZED",
                    message = "API token not found"
                }
            });
            return;
        }

        var apiTokenService = context.HttpContext.RequestServices.GetRequiredService<IApiTokenService>();
        
        if (!apiTokenService.ValidatePermission(apiToken, _resource, _operation))
        {
            context.Result = new ObjectResult(new
            {
                error = new
                {
                    code = "FORBIDDEN",
                    message = $"Insufficient permissions. Required: {_operation} on {_resource}"
                }
            })
            {
                StatusCode = 403
            };
            return;
        }

        await next();
    }
}

