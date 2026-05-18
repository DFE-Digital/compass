namespace Compass.ViewModels.Modern;

/// <summary>Account profile — basic identity on Profile; directory groups on Permissions.</summary>
public class ModernProfileViewModel
{
    public string? Email { get; init; }

    /// <summary>Best display name from the user directory record when available.</summary>
    public string? DisplayName { get; init; }

    public IReadOnlyList<string> DirectoryGroups { get; init; } = Array.Empty<string>();
}
