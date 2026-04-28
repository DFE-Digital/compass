using Compass.Models;
using Compass.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Compass.Filters;

/// <summary>Sends users to the modern dashboard when the Demand feature is switched off globally.</summary>
public sealed class DemandFeatureGateFilter : IAsyncActionFilter
{
    private readonly IGlobalFeatureToggleService _toggle;

    public DemandFeatureGateFilter(IGlobalFeatureToggleService toggle)
    {
        _toggle = toggle;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!await _toggle.IsFeatureEnabledForPrincipalAsync(FeatureCodes.Demand, context.HttpContext.User))
        {
            context.Result = new RedirectToActionResult("Index", "ModernDashboard", null);
            return;
        }

        await next();
    }
}
