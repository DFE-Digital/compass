using Compass.Models;

namespace Compass.Services;

/// <summary>Resolves display text for a monthly return (legacy <see cref="ProjectMonthlyUpdate.Narrative"/> vs <see cref="MonthlyUpdateNarrative"/> rows).</summary>
public static class ProjectMonthlyUpdateNarrative
{
    public static string Compose(ProjectMonthlyUpdate mu)
    {
        if (!string.IsNullOrWhiteSpace(mu.Narrative))
            return mu.Narrative.Trim();

        if (mu.MonthlyUpdateNarratives is { Count: > 0 })
        {
            var parts = mu.MonthlyUpdateNarratives
                .OrderBy(n => n.CreatedAt)
                .Select(n => n.Narrative)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .ToList();
            if (parts.Count > 0)
                return string.Join("\n\n", parts);
        }

        return "";
    }

    /// <summary>Text from the most recently submitted monthly return, or null if none / empty.</summary>
    public static string? LatestSubmittedText(Project project)
    {
        var latest = project.MonthlyUpdates?
            .Where(u => u.SubmittedAt.HasValue)
            .OrderByDescending(u => u.SubmittedAt)
            .FirstOrDefault();
        if (latest == null)
            return null;
        var text = Compose(latest);
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }
}
