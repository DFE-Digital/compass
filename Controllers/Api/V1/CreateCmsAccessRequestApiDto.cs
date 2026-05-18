using System.ComponentModel.DataAnnotations;

namespace Compass.Controllers.Api.V1;

/// <summary>Creates a CMS access request for Operations (Bearer token + permission required).</summary>
public sealed class CreateCmsAccessRequestApiDto
{
    /// <summary>Must match a key in configuration <c>CmsAccessRequest:SignInUrlsByCmsName</c> (case-insensitive).</summary>
    [Required(ErrorMessage = "cms_name is required")]
    [MaxLength(200)]
    public string CmsName { get; set; } = "";

    [Required(ErrorMessage = "email is required")]
    [MaxLength(256)]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "first_name is required")]
    [MaxLength(100)]
    public string FirstName { get; set; } = "";

    [Required(ErrorMessage = "last_name is required")]
    [MaxLength(100)]
    public string LastName { get; set; } = "";

    [MaxLength(4000)]
    public string? Comments { get; set; }

    /// <summary>Defaults to true when omitted.</summary>
    public bool PublisherAccessRequired { get; set; } = true;

    /// <summary>
    /// Anti-bot field: legitimate clients must omit this or send null/empty.
    /// If populated, the request is rejected without revealing the honeypot.
    /// </summary>
    public string? Website { get; set; }
}
