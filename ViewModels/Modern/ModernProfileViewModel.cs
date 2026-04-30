namespace Compass.ViewModels.Modern;

/// <summary>Profile page — signed-in identity and Compass user record when present.</summary>
public class ModernProfileViewModel
{
    public string? SignInName { get; init; }
    public string? Email { get; init; }

    public string? DatabaseName { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? JobTitle { get; init; }
    public string? UserPrincipalName { get; init; }
    public string? AzureObjectId { get; init; }
    public string? ApplicationRole { get; init; }

    public IReadOnlyList<string> DirectoryGroups { get; init; } = Array.Empty<string>();
    public bool HasCompassUserRecord { get; init; }
}
