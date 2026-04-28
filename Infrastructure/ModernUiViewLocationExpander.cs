using Microsoft.AspNetCore.Mvc.Razor;

namespace Compass.Infrastructure;

/// <summary>
/// <see cref="Compass.Controllers.Modern.ModernWorkController"/>, <see cref="Compass.Controllers.Modern.ModernReportingController"/>, <see cref="Compass.Controllers.Modern.ModernPerformanceController"/>, and <see cref="Compass.Controllers.Modern.ModernDashboardController"/> return views from
/// <c>Views/Modern/...</c>, but partial resolution uses the controller name (e.g. <c>ModernWork</c>),
/// so <c>PartialAsync("_Foo")</c> only searched <c>Views/ModernWork/</c> and <c>Views/Shared/</c>.
/// This expander prepends the actual modern view folders so co-located partials resolve.
/// </summary>
public sealed class ModernUiViewLocationExpander : IViewLocationExpander
{
    public void PopulateValues(ViewLocationExpanderContext context)
    {
    }

    public IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext context, IEnumerable<string> viewLocations)
    {
        foreach (var path in GetExtraLocations(context.ControllerName))
            yield return path;

        foreach (var location in viewLocations)
            yield return location;
    }

    private static IEnumerable<string> GetExtraLocations(string? controllerName)
    {
        if (string.Equals(controllerName, "ModernWork", StringComparison.OrdinalIgnoreCase))
        {
            yield return "/Views/Modern/Work/{0}.cshtml";
            yield return "/Views/Modern/Shared/{0}.cshtml";
        }
        else if (string.Equals(controllerName, "ModernReporting", StringComparison.OrdinalIgnoreCase))
        {
            yield return "/Views/Modern/Reporting/{0}.cshtml";
            yield return "/Views/Modern/Shared/{0}.cshtml";
        }
        else if (string.Equals(controllerName, "ModernDashboard", StringComparison.OrdinalIgnoreCase))
        {
            yield return "/Views/Modern/Dashboard/{0}.cshtml";
            yield return "/Views/Modern/Shared/{0}.cshtml";
        }
        else if (string.Equals(controllerName, "ModernManage", StringComparison.OrdinalIgnoreCase))
        {
            yield return "/Views/Modern/Manage/{0}.cshtml";
            yield return "/Views/Modern/Shared/{0}.cshtml";
        }
        else if (string.Equals(controllerName, "ModernPerformance", StringComparison.OrdinalIgnoreCase))
        {
            yield return "/Views/Modern/Performance/{0}.cshtml";
            yield return "/Views/Modern/Shared/{0}.cshtml";
        }
        else if (string.Equals(controllerName, "ModernStandards", StringComparison.OrdinalIgnoreCase))
        {
            yield return "/Views/Modern/Standards/{0}.cshtml";
            yield return "/Views/Modern/Shared/{0}.cshtml";
        }
        else if (string.Equals(controllerName, "ModernAdmin", StringComparison.OrdinalIgnoreCase))
        {
            yield return "/Views/Modern/Admin/{0}.cshtml";
            yield return "/Views/Modern/Shared/{0}.cshtml";
        }
        else if (string.Equals(controllerName, "ModernOperations", StringComparison.OrdinalIgnoreCase))
        {
            yield return "/Views/Modern/Operations/{0}.cshtml";
            yield return "/Views/Modern/Shared/{0}.cshtml";
        }
    }
}
