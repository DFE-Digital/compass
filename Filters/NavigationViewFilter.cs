using Compass.Helpers;
using Compass.Models;
using Compass.Services;
using Compass.ViewModels.Modern;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace Compass.Filters;

/// <summary>
/// Global action filter that populates ViewBag flags used by the shared
/// navigation partials (_Navigation, _SubNavigation) so they can
/// conditionally show/hide nav items based on the signed-in user's groups.
/// </summary>
public class NavigationViewFilter : IAsyncActionFilter
{
    private readonly IPermissionService _permissionService;
    private readonly IGlobalFeatureToggleService _globalFeatures;
    private readonly SubNavExportResolver _subNavExport;
    private readonly SubNavDataAccessResolver _subNavDataAccess;

    public NavigationViewFilter(
        IPermissionService permissionService,
        IGlobalFeatureToggleService globalFeatures,
        SubNavExportResolver subNavExport,
        SubNavDataAccessResolver subNavDataAccess)
    {
        _permissionService = permissionService;
        _globalFeatures = globalFeatures;
        _subNavExport = subNavExport;
        _subNavDataAccess = subNavDataAccess;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.Controller is Controller controller)
        {
            var userEmail = GetUserEmail(context.HttpContext);
            if (!string.IsNullOrEmpty(userEmail))
            {
                try
                {
                    var canUseOperations = await _permissionService.IsOperationConsoleUserAsync(userEmail);

                    controller.ViewBag.CanAccessOperations = canUseOperations;

                    controller.ViewBag.ShowDemandNavigation =
                        await _globalFeatures.IsFeatureEnabledForPrincipalAsync(
                            FeatureCodes.Demand, context.HttpContext.User);

                    controller.ViewBag.ShowStandardsNavigation =
                        await _globalFeatures.IsFeatureEnabledForPrincipalAsync(
                            FeatureCodes.Standards, context.HttpContext.User);

                    controller.ViewBag.ShowFipsDatabaseServiceRegister =
                        await _globalFeatures.IsFeatureEnabledForPrincipalAsync(
                            FeatureCodes.Fips, context.HttpContext.User);

                    controller.ViewBag.ShowRaidNavigation =
                        await _globalFeatures.IsFeatureEnabledForPrincipalAsync(
                            FeatureCodes.Raid, context.HttpContext.User);

                    controller.ViewBag.ShowDdrNavigation =
                        await _globalFeatures.IsFeatureEnabledForPrincipalAsync(
                            FeatureCodes.Ddr, context.HttpContext.User);

                    var canManageStandards =
                        await StandardsPermissionHelper.CanAccessModernStandardsManagementAsync(
                            _permissionService,
                            context.HttpContext.User);
                    controller.ViewBag.CanAccessStandardsManagement = canManageStandards;

                    // Same gate as <see cref="Attributes.RequireAdminAttribute"/> (modern admin hub).
                    controller.ViewBag.CanAccessModernAdmin =
                        await _permissionService.IsCentralOperationsAdminOrSuperAdminAsync(userEmail);
                }
                catch
                {
                    controller.ViewBag.CanAccessOperations = false;
                    controller.ViewBag.CanAccessStandardsManagement = false;
                    controller.ViewBag.ShowDemandNavigation = false;
                    controller.ViewBag.ShowStandardsNavigation = false;
                    controller.ViewBag.ShowFipsDatabaseServiceRegister = false;
                    controller.ViewBag.ShowRaidNavigation = false;
                    controller.ViewBag.ShowDdrNavigation = false;
                    controller.ViewBag.CanAccessModernAdmin = false;
                }
            }
        }

        await next();

        if (context.Controller is Controller controllerAfter)
        {
            if (controllerAfter.ViewBag.SubNavDataAccess is not SubNavDataAccessOptions)
            {
                controllerAfter.ViewBag.SubNavDataAccess =
                    _subNavDataAccess.Resolve(controllerAfter, context.HttpContext);
            }

            if (controllerAfter.ViewBag.SubNavExport is not SubNavExportOptions)
            {
                var dataAccess = controllerAfter.ViewBag.SubNavDataAccess as SubNavDataAccessOptions;
                if (dataAccess?.Export is { HasLinks: true } export)
                {
                    controllerAfter.ViewBag.SubNavExport = new SubNavExportOptions
                    {
                        Show = true,
                        CurrentViewUrl = export.Links.FirstOrDefault()?.Href,
                        AllDataUrl = export.Links.Count > 1 ? export.Links[^1].Href : null
                    };
                }
                else
                {
                    controllerAfter.ViewBag.SubNavExport =
                        _subNavExport.Resolve(controllerAfter, context.HttpContext);
                }
            }
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
