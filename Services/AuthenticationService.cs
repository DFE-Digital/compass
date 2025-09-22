using FipsReporting.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace FipsReporting.Services
{
    public interface IAuthenticationService
    {
        Task<bool> IsSuperAdminAsync(string email);
        Task<List<string>> GetUserPermissionsAsync(string email);
        Task<bool> HasPermissionAsync(string email, string permission);
        string? GetUserEmailFromClaims(ClaimsPrincipal user);
    }

    public class AuthenticationService : IAuthenticationService
    {
        private readonly IUserPermissionService _userPermissionService;
        private readonly ILogger<AuthenticationService> _logger;

        public AuthenticationService(IUserPermissionService userPermissionService, ILogger<AuthenticationService> logger)
        {
            _userPermissionService = userPermissionService;
            _logger = logger;
        }

        public string? GetUserEmailFromClaims(ClaimsPrincipal user)
        {
            // Try different claim types that Entra ID might use
            var email = user.FindFirst(ClaimTypes.Email)?.Value ??
                       user.FindFirst("email")?.Value ??
                       user.FindFirst("preferred_username")?.Value ??
                       user.FindFirst("upn")?.Value;

            return email;
        }

        public async Task<bool> IsSuperAdminAsync(string email)
        {
            return email == "andy.jones@education.gov.uk";
        }

        public async Task<List<string>> GetUserPermissionsAsync(string email)
        {
            return await _userPermissionService.GetUserPermissionsAsync(email);
        }

        public async Task<bool> HasPermissionAsync(string email, string permission)
        {
            return await _userPermissionService.HasPermissionAsync(email, permission);
        }
    }

}
