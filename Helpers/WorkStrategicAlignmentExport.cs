using Compass.Models;

namespace Compass.Helpers;

/// <summary>
/// Formats Work export strategic-alignment fields for Excel/CSV/Power BI.
/// Multi-value convention: semicolon + space ("; "), matching existing export delimiters.
/// </summary>
public static class WorkStrategicAlignmentExport
{
    public const string MultiValueDelimiter = "; ";

    public const string DirectorateColumn = "Directorate";
    public const string AdditionalDirectoratesColumn = "Additional Directorates";
    public const string MissionPillarsColumn = "Mission Pillars";
    public const string PriorityOutcomesColumn = "Priority Outcomes";
    public const string ThematicTagsColumn = "Thematic Tags";

    /// <summary>
    /// Primary (main delivery) directorate name. Empty when unmapped or primary cleared.
    /// </summary>
    public static string GetPrimaryDirectorateName(Project project)
    {
        var directorates = project.Directorates;
        if (directorates == null || directorates.Count == 0)
        {
            return string.Empty;
        }

        var primary = directorates
            .Where(d => d.IsPrimary)
            .OrderBy(d => d.CreatedAt)
            .ThenBy(d => d.Id)
            .FirstOrDefault();

        return primary?.Division?.Name?.Trim() ?? string.Empty;
    }

    /// <summary>
    /// Non-primary directorate names preserved from historical multi-select. Empty when none.
    /// When no primary is designated, all mapped names are listed here so values are not lost.
    /// </summary>
    public static string GetAdditionalDirectorateNames(Project project)
    {
        var directorates = project.Directorates;
        if (directorates == null || directorates.Count == 0)
        {
            return string.Empty;
        }

        var hasPrimary = directorates.Any(d => d.IsPrimary);
        var additional = directorates
            .Where(d => !hasPrimary || !d.IsPrimary)
            .Select(d => d.Division?.Name?.Trim() ?? string.Empty)
            .Where(n => !string.IsNullOrEmpty(n))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(n => n)
            .ToList();

        return JoinMultiValues(additional);
    }

    public static string GetMissionPillarNames(Project project)
    {
        var names = project.ProjectMissions?
            .Select(pm => pm.Mission?.Title?.Trim() ?? string.Empty)
            .Where(n => !string.IsNullOrEmpty(n))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(n => n)
            .ToList() ?? new List<string>();

        return JoinMultiValues(names);
    }

    public static string GetPriorityOutcomeNames(Project project)
    {
        var names = project.ProjectObjectives?
            .Select(po => po.Objective?.Title?.Trim() ?? string.Empty)
            .Where(n => !string.IsNullOrEmpty(n))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(n => n)
            .ToList() ?? new List<string>();

        return JoinMultiValues(names);
    }

    /// <summary>
    /// Thematic tags are the Theme values on linked Mission Pillars and Priority Outcomes.
    /// </summary>
    public static string GetThematicTagNames(Project project)
    {
        var themes = new List<string>();

        if (project.ProjectMissions != null)
        {
            themes.AddRange(project.ProjectMissions
                .Select(pm => pm.Mission?.Theme?.Trim() ?? string.Empty)
                .Where(t => !string.IsNullOrEmpty(t)));
        }

        if (project.ProjectObjectives != null)
        {
            themes.AddRange(project.ProjectObjectives
                .Select(po => po.Objective?.Theme?.Trim() ?? string.Empty)
                .Where(t => !string.IsNullOrEmpty(t)));
        }

        var distinct = themes
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(t => t)
            .ToList();

        return JoinMultiValues(distinct);
    }

    public static string JoinMultiValues(IEnumerable<string>? values)
    {
        if (values == null)
        {
            return string.Empty;
        }

        var list = values
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v.Trim())
            .ToList();

        return list.Count == 0 ? string.Empty : string.Join(MultiValueDelimiter, list);
    }
}
