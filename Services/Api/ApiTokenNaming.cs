using System.Text.RegularExpressions;

namespace Compass.Services.Api;

public static class ApiTokenNaming
{
    public static readonly string[] Environments = { "DEV", "TEST", "PROD" };

    private static readonly Regex ProjectSlugPattern = new(@"^[a-z0-9][a-z0-9-]{0,30}[a-z0-9]$|^[a-z0-9]$", RegexOptions.Compiled);

    public static bool IsValidEnvironment(string? environment) =>
        !string.IsNullOrWhiteSpace(environment) &&
        Environments.Contains(environment.Trim().ToUpperInvariant());

    public static bool TryNormalizeProjectSlug(string? input, out string slug, out string? error)
    {
        slug = (input ?? "").Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(slug))
        {
            error = "Enter a short project name (lowercase letters, numbers and hyphens).";
            return false;
        }
        if (slug.Length > 32)
        {
            error = "Project name must be 32 characters or fewer.";
            return false;
        }
        if (!ProjectSlugPattern.IsMatch(slug))
        {
            error = "Use lowercase letters, numbers and hyphens only (for example my-integration).";
            return false;
        }
        error = null;
        return true;
    }

    public static string ResolveAccessTier(Dictionary<string, (bool read, bool create, bool update, bool delete)> permissions)
    {
        if (ApiTokenResourceCatalog.HasDelete(permissions))
            return "FULL";
        var anyCreate = permissions.Values.Any(p => p.create);
        var anyUpdate = permissions.Values.Any(p => p.update);
        var anyRead = permissions.Values.Any(p => p.read);
        if (anyCreate && anyUpdate)
            return "CRU";
        if (anyCreate)
            return "CREATE";
        if (anyUpdate)
            return "UPDATE";
        if (anyRead)
            return "RO";
        return "RO";
    }

    public static string BuildTokenName(string environment, string accessTier, string projectSlug) =>
        $"{environment.Trim().ToUpperInvariant()}-{accessTier.Trim().ToUpperInvariant()}-{projectSlug.Trim().ToLowerInvariant()}";
}
