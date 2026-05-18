using System.Text.RegularExpressions;
using Compass.Data;
using Microsoft.EntityFrameworkCore;

namespace Compass.Helpers;

public static class ServiceLineSlugHelper
{
    private static readonly HashSet<string> Reserved = new(
        new[] { "new", "pick-products", "pick-work" },
        StringComparer.OrdinalIgnoreCase);

    public static string GenerateBaseSlug(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "service-line";
        var s = name.Trim().ToLowerInvariant();
        s = Regex.Replace(s, @"\s+", "-", RegexOptions.None, TimeSpan.FromSeconds(1));
        s = s.Replace('_', '-');
        s = Regex.Replace(s, @"[^a-z0-9\-]+", "-", RegexOptions.None, TimeSpan.FromSeconds(1));
        s = Regex.Replace(s, @"-{2,}", "-", RegexOptions.None, TimeSpan.FromSeconds(1));
        s = s.Trim('-');
        if (s.Length > 200)
            s = s[..200].TrimEnd('-');
        if (string.IsNullOrEmpty(s))
            s = "service-line";
        if (Reserved.Contains(s))
            s = s + "-line";
        if (s.Length > 200)
            s = s[..200].TrimEnd('-');
        return s;
    }

    public static async Task<string> EnsureUniqueSlugAsync(
        CompassDbContext db,
        string baseSlug,
        Guid? exceptServiceLineId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(baseSlug))
            baseSlug = "service-line";
        if (baseSlug.Length > 200)
            baseSlug = baseSlug[..200].TrimEnd('-');

        for (var n = 0; n < 5000; n++)
        {
            // n=0: base, n=1: base-2, n=2: base-3, …
            var trySlug = n == 0 ? baseSlug : $"{baseSlug}-{(n + 1)}";
            if (trySlug.Length > 200)
                trySlug = trySlug[..200].TrimEnd('-');
            var taken = await db.ServiceLines
                .AsNoTracking()
                .AnyAsync(
                    x => x.Slug == trySlug
                         && (!exceptServiceLineId.HasValue || x.Id != exceptServiceLineId.Value),
                    cancellationToken);
            if (!taken)
                return trySlug;
        }

        return $"{baseSlug.AsSpan(0, Math.Min(32, baseSlug.Length)).ToString()}-{Guid.NewGuid().ToString("N")[..8]}";
    }
}
