using Microsoft.AspNetCore.Mvc.Rendering;

namespace Compass.Helpers;

public static class SecurityHelper
{
    public static string GetNonce(this IHtmlHelper htmlHelper)
    {
        var httpContext = htmlHelper.ViewContext.HttpContext;
        var nonce = httpContext.Items["Nonce"]?.ToString() ?? string.Empty;
        return nonce;
    }
}

