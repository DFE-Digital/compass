using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FipsReporting.Helpers
{
    public static class SecurityHelper
    {
        public static string GetNonce(this IHtmlHelper htmlHelper)
        {
            // Get the nonce from the current HTTP context (set by middleware)
            var context = htmlHelper.ViewContext.HttpContext;
            if (context.Items.TryGetValue("Nonce", out var nonce) && nonce is string nonceString)
            {
                return nonceString;
            }
            
            // Fallback: generate a new nonce if not found in context
            return Guid.NewGuid().ToString("N")[..16];
        }
    }
}
