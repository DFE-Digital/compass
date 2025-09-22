using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FipsReporting.Controllers
{
    public class DebugController : Controller
    {
        public IActionResult Claims()
        {
            var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
            var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
            var groups = User.FindAll("groups").Select(c => c.Value).ToList();
            var directRoles = User.FindAll("roles").Select(c => c.Value).ToList();

            var debugInfo = new
            {
                UserName = User.Identity?.Name,
                Email = User.FindFirst(ClaimTypes.Email)?.Value,
                IsAuthenticated = User.Identity?.IsAuthenticated,
                AuthenticationType = User.Identity?.AuthenticationType,
                Claims = claims,
                Roles = roles,
                Groups = groups,
                DirectRoles = directRoles,
                IsAdmin = User.IsInRole("Admin"),
                IsCentralOperations = User.IsInRole("Central Operations"),
                IsReportingUser = User.IsInRole("reporting_user")
            };

            return Json(debugInfo);
        }
    }
}
