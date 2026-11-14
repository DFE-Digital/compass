using System.Security.Claims;
using Compass.Services.Census;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Compass.Attributes;

/// <summary>Loads a service census completion prompt onto ViewBag for dashboard pages.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class CensusPromptBannerAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        await next();

        if (context.Controller is not Controller controller)
            return;
        if (context.HttpContext.User?.Identity?.IsAuthenticated != true)
            return;

        var fipsOn = controller.ViewBag.ShowFipsDatabaseServiceRegister as bool? == true
            || controller.ViewBag.FipsServiceRegisterEnabled as bool? == true;
        if (!fipsOn)
            return;

        var email = context.HttpContext.User.Identity?.Name
            ?? context.HttpContext.User.FindFirst(ClaimTypes.Email)?.Value
            ?? context.HttpContext.User.FindFirst("preferred_username")?.Value
            ?? context.HttpContext.User.FindFirst("email")?.Value
            ?? "";
        if (string.IsNullOrWhiteSpace(email))
            return;

        try
        {
            var census = context.HttpContext.RequestServices.GetRequiredService<IServiceCensusService>();
            controller.ViewBag.CensusPrompt = await census.GetPromptAsync(email, context.HttpContext.RequestAborted);
        }
        catch
        {
            controller.ViewBag.CensusPrompt = null;
        }
    }
}
