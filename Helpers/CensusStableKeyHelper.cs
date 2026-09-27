using System.Text.RegularExpressions;

namespace Compass.Helpers;

/// <summary>
/// Generates permanent lowercase-hyphenated stable keys from display names.
/// Keys are allocated once on create and must not be regenerated on rename.
/// </summary>
public static class CensusStableKeyHelper
{
    private const int MaxLength = 100;

    /// <summary>
    /// Lowercase hyphenated slug of <paramref name="name"/>.
    /// Strips punctuation, collapses whitespace/hyphens. Empty or punctuation-only
    /// input yields <paramref name="fallback"/>.
    /// </summary>
    public static string FromName(string? name, string fallback = "item")
    {
        if (string.IsNullOrWhiteSpace(name))
            return Clamp(fallback);

        var s = name.Trim().ToLowerInvariant();
        s = Regex.Replace(s, @"[^a-z0-9\s\-]+", "", RegexOptions.None, TimeSpan.FromSeconds(1));
        s = Regex.Replace(s, @"[\s_\-]+", "-", RegexOptions.None, TimeSpan.FromSeconds(1));
        s = s.Trim('-');

        if (string.IsNullOrEmpty(s))
            return Clamp(fallback);

        return Clamp(s);
    }

    /// <summary>
    /// Returns <paramref name="baseKey"/>, or <c>baseKey-2</c>, <c>baseKey-3</c>, …
    /// until the key is not present in <paramref name="existing"/> (ordinal ignore case).
    /// </summary>
    public static string EnsureUnique(string baseKey, IEnumerable<string> existing)
    {
        if (string.IsNullOrWhiteSpace(baseKey))
            baseKey = "item";
        baseKey = Clamp(baseKey.Trim().ToLowerInvariant());

        var taken = new HashSet<string>(
            existing.Where(k => !string.IsNullOrWhiteSpace(k)).Select(k => k.Trim()),
            StringComparer.OrdinalIgnoreCase);

        if (!taken.Contains(baseKey))
            return baseKey;

        for (var n = 2; n < 10_000; n++)
        {
            var suffix = $"-{n}";
            var candidate = Clamp(baseKey, MaxLength - suffix.Length) + suffix;
            if (!taken.Contains(candidate))
                return candidate;
        }

        return Clamp($"{baseKey}-{Guid.NewGuid():N}"[..8]);
    }

    private static string Clamp(string value, int maxLength = MaxLength)
    {
        if (maxLength < 1)
            maxLength = 1;
        if (value.Length <= maxLength)
            return value;
        return value[..maxLength].TrimEnd('-');
    }
}
