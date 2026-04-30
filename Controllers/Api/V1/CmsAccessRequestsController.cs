using Compass.Attributes;
using Compass.Data;
using Compass.Helpers;
using Compass.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Compass.Controllers.Api.V1;

[ApiController]
[Route("api/v1")]
public class CmsAccessRequestsController : ControllerBase
{
    private readonly CompassDbContext _db;
    private readonly CmsAccessRequestApiOptions _options;
    private readonly ILogger<CmsAccessRequestsController> _logger;

    public CmsAccessRequestsController(
        CompassDbContext db,
        IOptions<CmsAccessRequestApiOptions> options,
        ILogger<CmsAccessRequestsController> logger)
    {
        _db = db;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Submit a CMS access request for central Operations processing.
    /// Requires Bearer API token with permission <c>CmsAccessRequests:create</c>.
    /// Rate limited per client IP. Email must be <c>@education.gov.uk</c>.
    /// CMS names are resolved from <b>Admin → Access and integration → CMS access products</b> (database) first,
    /// then from <c>CmsAccessRequest:SignInUrlsByCmsName</c> in application settings if no match.
    /// </summary>
    [HttpPost("cms-access-requests")]
    [EnableRateLimiting("CmsAccessRequestsCreatePolicy")]
    [RequireApiPermission("CmsAccessRequests", "create")]
    [RequestSizeLimit(32_768)]
    [ProducesResponseType(typeof(CreateCmsAccessRequestResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Create([FromBody] CreateCmsAccessRequestApiDto? dto, CancellationToken cancellationToken)
    {
        if (dto == null)
            return BadRequest(new { error = "validation_error", message = "Request body is required" });

        if (!string.IsNullOrWhiteSpace(dto.Website))
        {
            _logger.LogWarning("CMS access request rejected (anti-bot field populated)");
            return BadRequest(new { error = "validation_error", message = "Request could not be processed" });
        }

        dto.CmsName = dto.CmsName.Trim();
        dto.Email = dto.Email.Trim();
        dto.FirstName = dto.FirstName.Trim();
        dto.LastName = dto.LastName.Trim();
        if (dto.Comments != null)
            dto.Comments = dto.Comments.Trim();

        ModelState.Clear();
        if (!TryValidateModel(dto))
            return ValidationProblem(ModelState);

        var resolved = await ResolveCmsNameAsync(dto.CmsName, cancellationToken);
        if (!resolved.Found)
            return BadRequest(new
            {
                error = "unknown_cms",
                message = "cms_name is not recognised. Add the product in Admin (Access and integration → CMS access products), or configure CmsAccessRequest:SignInUrlsByCmsName."
            });

        var canonicalCmsName = resolved.CanonicalName;
        var signInUrl = resolved.SignInUrl;

        if (!Uri.TryCreate(signInUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
        {
            _logger.LogError("Invalid sign-in URL for CMS {CmsName}", canonicalCmsName);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                error = "configuration_error",
                message = "CMS sign-in URL is misconfigured"
            });
        }

        if (!EducationGovUkEmailValidator.IsAllowed(dto.Email))
        {
            return BadRequest(new
            {
                error = "invalid_email",
                message = "Only education.gov.uk email addresses are allowed"
            });
        }

        var entity = new Models.CmsAccessRequest
        {
            CmsName = canonicalCmsName,
            SignInPageUrl = signInUrl,
            RequestorEmail = dto.Email,
            RequestorFirstName = dto.FirstName,
            RequestorLastName = dto.LastName,
            Comments = string.IsNullOrEmpty(dto.Comments) ? null : dto.Comments,
            PublisherAccessRequired = dto.PublisherAccessRequired,
            DateRequested = DateTime.UtcNow,
            Status = "New"
        };

        _db.CmsAccessRequests.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return Created(
            $"/api/v1/cms-access-requests/{entity.Id}",
            new CreateCmsAccessRequestResponseDto { Id = entity.Id, Status = entity.Status });
    }

    private async Task<(bool Found, string CanonicalName, string SignInUrl)> ResolveCmsNameAsync(string requested, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(requested))
            return (false, "", "");

        var req = requested.Trim();

        var dbRows = await _db.CmsAccessRequestProducts.AsNoTracking()
            .Where(p => p.IsActive)
            .ToListAsync(ct);
        var dbMatch = dbRows.FirstOrDefault(p => string.Equals(p.Name.Trim(), req, StringComparison.OrdinalIgnoreCase));
        if (dbMatch != null)
            return (true, dbMatch.Name.Trim(), dbMatch.SignInPageUrl.Trim());

        if (TryResolveCmsFromOptions(req, out var optName, out var optUrl))
            return (true, optName, optUrl);

        return (false, "", "");
    }

    private bool TryResolveCmsFromOptions(string requestedTrimmed, out string canonicalName, out string signInUrl)
    {
        canonicalName = "";
        signInUrl = "";
        foreach (var kv in _options.SignInUrlsByCmsName)
        {
            if (string.IsNullOrWhiteSpace(kv.Value))
                continue;
            if (!string.Equals(kv.Key.Trim(), requestedTrimmed, StringComparison.OrdinalIgnoreCase))
                continue;
            canonicalName = kv.Key.Trim();
            signInUrl = kv.Value.Trim();
            return true;
        }

        return false;
    }
}

public sealed class CreateCmsAccessRequestResponseDto
{
    public int Id { get; set; }
    public string Status { get; set; } = "";
}
