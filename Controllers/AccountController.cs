using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FipsReporting.Controllers
{
    public class AccountController : Controller
    {
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            // Redirect to Entra ID sign-in
            var redirectUrl = returnUrl ?? Url.Action("Index", "Home");
            return Challenge(new AuthenticationProperties { RedirectUri = redirectUrl }, OpenIdConnectDefaults.AuthenticationScheme);
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            // Sign out and redirect to Entra ID sign-out
            await HttpContext.SignOutAsync();
            return SignOut(new AuthenticationProperties(), OpenIdConnectDefaults.AuthenticationScheme);
        }

        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
