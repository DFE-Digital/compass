using Compass.Models;
using Compass.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Compass.Filters;

/// <summary>Sends users to the modern dashboard when the Design Decision Records (DDR) feature is switched off globally.</summary>
public sealed class DdrFeatureGateFilter : IAsyncActionFilter
{
    private readonly IGlobalFeatureToggleService _toggle;

    public DdrFeatureGateFilter(IGlobalFeatureToggleService toggle)
    {
        _toggle = toggle;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!await _toggle.IsFeatureEnabledForPrincipalAsync(FeatureCodes.Ddr, context.HttpContext.User))
        {
            context.Result = new RedirectToActionResult("Index", "ModernDashboard", null);
            return;
        }

        await next();
    }
}
