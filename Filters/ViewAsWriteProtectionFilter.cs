using Compass.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Compass.Filters;

/// <summary>
/// Blocks mutating requests while a Central Operations Admin is in view-as mode
/// (read-only simulation; real identity is unchanged).
/// </summary>
public sealed class ViewAsWriteProtectionFilter : IAsyncActionFilter
{
    private readonly IViewAsUserService _viewAs;

    public ViewAsWriteProtectionFilter(IViewAsUserService viewAs)
    {
        _viewAs = viewAs;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!_viewAs.IsActive(context.HttpContext))
        {
            await next();
            return;
        }

        var method = context.HttpContext.Request.Method;
        if (HttpMethods.IsGet(method) || HttpMethods.IsHead(method) || HttpMethods.IsOptions(method))
        {
            await next();
            return;
        }

        var controller = context.RouteData.Values["controller"]?.ToString();
        if (string.Equals(controller, "ModernViewAs", StringComparison.OrdinalIgnoreCase))
        {
            await next();
            return;
        }

        if (context.Controller is Controller mvc)
            mvc.TempData["ErrorMessage"] = "You cannot make changes while viewing as another user. Exit view-as mode first.";

        var referer = context.HttpContext.Request.Headers.Referer.ToString();
        if (!string.IsNullOrWhiteSpace(referer) && Uri.TryCreate(referer, UriKind.Absolute, out _))
        {
            context.Result = new RedirectResult(referer);
            return;
        }

        context.Result = new RedirectToActionResult("Dashboard", "ModernWork", null);
    }
}
