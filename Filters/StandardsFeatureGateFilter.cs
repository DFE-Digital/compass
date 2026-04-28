using Compass.Models;
using Compass.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Compass.Filters;

/// <summary>Sends users to the modern dashboard when the Standards feature is switched off globally.</summary>
public sealed class StandardsFeatureGateFilter : IAsyncActionFilter
{
    private readonly IGlobalFeatureToggleService _toggle;

    public StandardsFeatureGateFilter(IGlobalFeatureToggleService toggle)
    {
        _toggle = toggle;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!await _toggle.IsFeatureEnabledForPrincipalAsync(FeatureCodes.Standards, context.HttpContext.User))
        {
            context.Result = new RedirectToActionResult("Index", "ModernDashboard", null);
            return;
        }

        await next();
    }
}
