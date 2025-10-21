using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Compass.Models;
using System.Security.Claims;

namespace Compass.Controllers;

[Authorize]
public class ProfileController : Controller
{
    private readonly ILogger<ProfileController> _logger;

    public ProfileController(ILogger<ProfileController> logger)
    {
        _logger = logger;
    }

    public IActionResult UserInfo()
    {
        var viewModel = new UserInfoViewModel
        {
            PageTitle = "User information",
            PageDescription = "View your account details and authentication information"
        };

        // Get user claims
        if (User.Identity?.IsAuthenticated == true)
        {
            viewModel.IsAuthenticated = true;
            
            // Get Entra ID specific claims
            viewModel.Name = User.FindFirst("name")?.Value 
                ?? User.FindFirst(ClaimTypes.Name)?.Value;
                
            viewModel.Email = User.FindFirst("preferred_username")?.Value 
                ?? User.FindFirst(ClaimTypes.Email)?.Value;
                
            viewModel.ObjectId = User.FindFirst("oid")?.Value 
                ?? User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;
                
            viewModel.TenantId = User.FindFirst("tid")?.Value 
                ?? User.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid")?.Value;
                
            viewModel.UserPrincipalName = User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/upn")?.Value 
                ?? User.FindFirst(ClaimTypes.Upn)?.Value;
            
            // Get roles
            viewModel.Roles = User.FindAll(ClaimTypes.Role)
                .Union(User.FindAll("roles"))
                .Select(c => c.Value)
                .Distinct()
                .ToList();
            
            // Get all claims for display
            viewModel.Claims = User.Claims
                .Select(c => new KeyValuePair<string, string>(c.Type, c.Value))
                .OrderBy(c => c.Key)
                .ToList();
        }

        return View(viewModel);
    }
}

