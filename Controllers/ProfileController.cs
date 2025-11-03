using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Compass.Models;
using Compass.Services;
using Compass.Data;
using System.Security.Claims;

namespace Compass.Controllers;

[Authorize]
public class ProfileController : Controller
{
    private readonly ILogger<ProfileController> _logger;
    private readonly IGraphService _graphService;
    private readonly CompassDbContext _context;

    public ProfileController(
        ILogger<ProfileController> logger,
        IGraphService graphService,
        CompassDbContext context)
    {
        _logger = logger;
        _graphService = graphService;
        _context = context;
    }

    public async Task<IActionResult> UserInfo()
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
            var userEmail = User.Identity.Name;
            
            // Get Entra ID specific claims
            viewModel.Name = User.FindFirst("name")?.Value 
                ?? User.FindFirst(ClaimTypes.Name)?.Value;
                
            viewModel.Email = User.FindFirst("preferred_username")?.Value 
                ?? User.FindFirst(ClaimTypes.Email)?.Value
                ?? userEmail;
                
            viewModel.ObjectId = User.FindFirst("oid")?.Value 
                ?? User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;
                
            viewModel.TenantId = User.FindFirst("tid")?.Value 
                ?? User.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid")?.Value;
                
            viewModel.UserPrincipalName = User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/upn")?.Value 
                ?? User.FindFirst(ClaimTypes.Upn)?.Value;
            
            // Get roles from claims
            viewModel.Roles = User.FindAll(ClaimTypes.Role)
                .Union(User.FindAll("roles"))
                .Select(c => c.Value)
                .Distinct()
                .ToList();
            
            // Get user from database and their groups
            if (!string.IsNullOrEmpty(userEmail))
            {
                var dbUser = await _context.Users
                    .Include(u => u.UserGroups)
                        .ThenInclude(ug => ug.Group)
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == userEmail.ToLower());
                
                if (dbUser != null)
                {
                    viewModel.DatabaseUser = dbUser;
                    
                    // Get groups
                    viewModel.Groups = dbUser.UserGroups
                        .Where(ug => ug.Group.IsActive)
                        .Select(ug => new GroupInfo
                        {
                            Id = ug.Group.Id,
                            Name = ug.Group.Name,
                            Description = ug.Group.Description,
                            IsSystemGroup = ug.Group.IsSystemGroup,
                            AssignedAt = ug.AssignedAt
                        })
                        .OrderBy(g => g.Name)
                        .ToList();
                }
            }
            
            // Get all claims with descriptions
            viewModel.Claims = User.Claims
                .Select(c => new ClaimInfo
                {
                    Type = c.Type,
                    Value = c.Value,
                    Description = GetClaimDescription(c.Type)
                })
                .OrderBy(c => c.Type)
                .ToList();
        }

        return View(viewModel);
    }
    
    private string GetClaimDescription(string claimType)
    {
        // Check ClaimTypes constants first (they resolve to full URIs)
        if (claimType == ClaimTypes.Name || claimType == "name")
        {
            return "The user's name";
        }
        if (claimType == ClaimTypes.Email || claimType == "email")
        {
            return "The user's email address";
        }
        if (claimType == ClaimTypes.Upn || claimType == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/upn")
        {
            return "User principal name - the user's login name";
        }
        if (claimType == ClaimTypes.Role || claimType == "roles")
        {
            return "Application role assigned to the user";
        }
        
        return claimType switch
        {
            "preferred_username" => "The user's preferred username (email address)",
            "oid" => "Object identifier - unique ID for the user in Microsoft Entra ID",
            "http://schemas.microsoft.com/identity/claims/objectidentifier" => "Object identifier - unique ID for the user in Microsoft Entra ID",
            "tid" => "Tenant identifier - unique ID for the organisation",
            "http://schemas.microsoft.com/identity/claims/tenantid" => "Tenant identifier - unique ID for the organisation",
            "groups" => "Microsoft Entra ID groups the user belongs to",
            "http://schemas.microsoft.com/ws/2008/06/identity/claims/groups" => "Microsoft Entra ID groups the user belongs to",
            "aud" => "Audience - the intended recipient of the token",
            "iss" => "Issuer - the entity that issued the token",
            "exp" => "Expiration time - when the token expires",
            "iat" => "Issued at - when the token was issued",
            "nbf" => "Not before - token is not valid before this time",
            "sub" => "Subject - the principal about which the token asserts information",
            "auth_time" => "Authentication time - when the user authenticated",
            "nonce" => "Nonce - a unique value used to prevent replay attacks",
            "acr" => "Authentication context class reference",
            "aio" => "Anonymous identifier - internal token identifier",
            "amr" => "Authentication method reference - methods used to authenticate",
            "appid" => "Application ID - identifier of the client application",
            "appidacr" => "Application authentication context class reference",
            "azp" => "Authorized party - the party to which the token was issued",
            "ipaddr" => "IP address - the IP address the user authenticated from",
            "onprem_sid" => "On-premises security identifier",
            "family_name" => "User's surname",
            "given_name" => "User's first name",
            "unique_name" => "Unique name - an older claim for username",
            "ver" => "Version - the token version",
            _ => "Additional authentication claim"
        };
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

