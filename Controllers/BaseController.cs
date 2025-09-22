using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FipsReporting.Controllers
{
    public abstract class BaseController : Controller
    {
        protected string GetUserEmail()
        {
            return User.FindFirst(ClaimTypes.Email)?.Value ?? User.FindFirst("preferred_username")?.Value ?? string.Empty;
        }

        protected string GetUserName()
        {
            return User.FindFirst(ClaimTypes.Name)?.Value ?? User.FindFirst("name")?.Value ?? GetUserEmail();
        }

        protected bool IsAdmin()
        {
            return User.IsInRole("Central Operations") || User.IsInRole("Admin");
        }

        protected bool IsReportingUser()
        {
            return User.IsInRole("reporting_user") || IsAdmin();
        }

        protected IActionResult HandleException(Exception ex, string action)
        {
            Logger.LogError(ex, "Error in {Action}", action);
            TempData["Error"] = "An error occurred while processing your request. Please try again.";
            return RedirectToAction("Index");
        }

        protected ILogger Logger => HttpContext.RequestServices.GetRequiredService<ILogger<BaseController>>();
    }
}
