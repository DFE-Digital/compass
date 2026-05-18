namespace Compass.Services.DdtStandards;

/// <summary>Semantic versioning helpers for DDT standards drafts and publication.</summary>
public static class DdtStandardVersioning
{
    /// <summary>Initial draft version for brand-new standards (never published).</summary>
    public const string NewStandardDraftVersion = "0.1.0";

    public static bool IsNewStandardDraftVersion(string? version) =>
        string.IsNullOrWhiteSpace(version) ||
        string.Equals(version, NewStandardDraftVersion, StringComparison.Ordinal) ||
        version.StartsWith("0.", StringComparison.Ordinal);

    /// <summary>Proposed next version when editing a published standard (patch bump).</summary>
    public static string ResolveDraftVersionFromParent(string parentVersion) =>
        IncrementVersion(parentVersion, "patch");

    public static string IncrementVersion(string currentVersion, string versionType = "patch")
    {
        if (string.IsNullOrWhiteSpace(currentVersion))
            return "1.0.0";

        var parts = currentVersion.Split('.');
        if (parts.Length != 3 ||
            !int.TryParse(parts[0], out var major) ||
            !int.TryParse(parts[1], out var minor) ||
            !int.TryParse(parts[2], out var patch))
        {
            return "1.0.0";
        }

        return versionType switch
        {
            "major" => $"{major + 1}.0.0",
            "minor" => $"{major}.{minor + 1}.0",
            "patch" => $"{major}.{minor}.{patch + 1}",
            _ => $"{major}.{minor}.{patch + 1}"
        };
    }
}
