namespace Compass.Configuration;

/// <summary>Maps API <c>cmsName</c> values to sign-in URLs (only known CMS products are accepted).</summary>
public sealed class CmsAccessRequestApiOptions
{
    public const string SectionName = "CmsAccessRequest";

    /// <summary>Display name (key) to sign-in page URL. Keys are matched case-insensitively.</summary>
    public Dictionary<string, string> SignInUrlsByCmsName { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
