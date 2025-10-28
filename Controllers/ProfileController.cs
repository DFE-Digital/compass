using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Compass.Models;
using Compass.Services;
using System.Security.Claims;

namespace Compass.Controllers;

[Authorize]
public class ProfileController : Controller
{
    private readonly ILogger<ProfileController> _logger;
    private readonly IGraphService _graphService;

    public ProfileController(
        ILogger<ProfileController> logger,
        IGraphService graphService)
    {
        _logger = logger;
        _graphService = graphService;
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

    [HttpGet]
    public async Task<IActionResult> SearchUsers(string term)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
            {
                return Json(new List<object>());
            }

            var users = await _graphService.SearchStaffAsync(term, 20);
            
            // Format for autocomplete
            var results = users.Select(u => new
            {
                label = $"{u.DisplayName} ({u.Email})",
                value = u.DisplayName,
                email = u.Email,
                displayName = u.DisplayName,
                jobTitle = u.JobTitle ?? "",
                department = u.Department ?? ""
            });

            return Json(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching users for term: {Term}", term);
            return Json(new List<object>());
        }
    }
}

