using Compass.Data;
using Compass.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Compass.Filters;

/// <summary>Redirects legacy <c>/Project/*</c> GET views to the modern work UI.</summary>
public sealed class ProjectLegacyRedirectFilter : IAsyncActionFilter
{
    private readonly CompassDbContext _db;
    private readonly IUrlHelperFactory _urlHelperFactory;

    public ProjectLegacyRedirectFilter(CompassDbContext db, IUrlHelperFactory urlHelperFactory)
    {
        _db = db;
        _urlHelperFactory = urlHelperFactory;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var request = context.HttpContext.Request;
        if (!HttpMethods.IsGet(request.Method) && !HttpMethods.IsHead(request.Method))
        {
            await next();
            return;
        }

        if (IsApiProjectRoute(context))
        {
            await next();
            return;
        }

        var urlHelper = _urlHelperFactory.GetUrlHelper(context);
        var redirect = await ProjectLegacyRedirectMap.TryBuildRedirectAsync(
            context,
            urlHelper,
            _db,
            context.HttpContext.RequestAborted);

        if (redirect != null)
        {
            context.Result = redirect;
            return;
        }

        await next();
    }

    private static bool IsApiProjectRoute(ActionExecutingContext context)
    {
        var path = context.HttpContext.Request.Path.Value ?? string.Empty;
        if (path.StartsWith("/api/project", StringComparison.OrdinalIgnoreCase))
            return true;

        var template = context.ActionDescriptor.AttributeRouteInfo?.Template;
        return template != null && template.StartsWith("api/project", StringComparison.OrdinalIgnoreCase);
    }
}
