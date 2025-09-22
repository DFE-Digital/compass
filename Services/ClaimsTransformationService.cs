using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace FipsReporting.Services
{
    public interface IClaimsTransformationService
    {
        Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal);
    }

    public class ClaimsTransformationService : IClaimsTransformationService, IClaimsTransformation
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ClaimsTransformationService> _logger;

        public ClaimsTransformationService(IConfiguration configuration, ILogger<ClaimsTransformationService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            try
            {
                var identity = principal.Identity as ClaimsIdentity;
                if (identity == null || !identity.IsAuthenticated)
                {
                    return Task.FromResult(principal);
                }

                // Get role mapping from configuration
                var roleMapping = _configuration.GetSection("AzureAd:RoleMapping").Get<Dictionary<string, string>>();
                if (roleMapping == null || !roleMapping.Any())
                {
                    _logger.LogWarning("No role mapping configured in AzureAd:RoleMapping");
                    return Task.FromResult(principal);
                }

                // Get groups from claims
                var groups = principal.FindAll("groups").Select(c => c.Value).ToList();
                _logger.LogInformation("User has {GroupCount} groups: {Groups}", groups.Count, string.Join(", ", groups));

                // Map groups to roles
                var rolesToAdd = new List<string>();
                foreach (var group in groups)
                {
                    if (roleMapping.TryGetValue(group, out var role))
                    {
                        rolesToAdd.Add(role);
                        _logger.LogInformation("Mapped group '{Group}' to role '{Role}'", group, role);
                    }
                }

                // Add role claims if not already present
                foreach (var role in rolesToAdd)
                {
                    if (!principal.HasClaim(ClaimTypes.Role, role))
                    {
                        identity.AddClaim(new Claim(ClaimTypes.Role, role));
                        _logger.LogInformation("Added role claim: {Role}", role);
                    }
                }

                // Also check for direct role claims from Azure AD
                var directRoles = principal.FindAll("roles").Select(c => c.Value).ToList();
                foreach (var role in directRoles)
                {
                    if (!principal.HasClaim(ClaimTypes.Role, role))
                    {
                        identity.AddClaim(new Claim(ClaimTypes.Role, role));
                        _logger.LogInformation("Added direct role claim: {Role}", role);
                    }
                }

                // Log final roles for debugging
                var finalRoles = principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
                _logger.LogInformation("Final user roles: {Roles}", string.Join(", ", finalRoles));

                return Task.FromResult(principal);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during claims transformation");
                return Task.FromResult(principal);
            }
        }
    }
}
