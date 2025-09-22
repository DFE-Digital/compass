using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace FipsReporting.Services
{
    public class DevelopmentAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public DevelopmentAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, 
            ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
            : base(options, logger, encoder, clock)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            // Create a development user identity
            var claims = new[]
            {
                
                new Claim(ClaimTypes.Name, "Development User"),
                new Claim(ClaimTypes.Email, "andy.jones@education.gov.uk"),
                new Claim(ClaimTypes.NameIdentifier, "andy-jones-123"),
                new Claim("preferred_username", "andy.jones@education.gov.uk")
            };

            var identity = new ClaimsIdentity(claims, "Development");
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, "Development");

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
