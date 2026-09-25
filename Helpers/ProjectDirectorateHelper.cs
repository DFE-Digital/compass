using Compass.Models;

namespace Compass.Helpers;

/// <summary>
/// Manages primary Directorate selection on Work items without discarding historical multi-value rows.
/// </summary>
public static class ProjectDirectorateHelper
{
    /// <summary>
    /// Sets the main delivery directorate. Existing non-selected junction rows are kept with IsPrimary=false.
    /// Passing null clears the primary flag on all rows but does not delete them.
    /// </summary>
    public static void SetPrimaryDirectorate(Project project, int? divisionId, Action<ProjectDirectorate>? addEntity = null)
    {
        project.Directorates ??= new List<ProjectDirectorate>();

        foreach (var existing in project.Directorates)
        {
            existing.IsPrimary = false;
        }

        if (!divisionId.HasValue)
        {
            return;
        }

        var match = project.Directorates.FirstOrDefault(d => d.DivisionId == divisionId.Value);
        if (match != null)
        {
            match.IsPrimary = true;
            return;
        }

        var created = new ProjectDirectorate
        {
            ProjectId = project.Id,
            DivisionId = divisionId.Value,
            IsPrimary = true,
            CreatedAt = DateTime.UtcNow
        };
        project.Directorates.Add(created);
        addEntity?.Invoke(created);
    }

    /// <summary>
    /// For create/edit forms that may still post a list: use the first id as primary.
    /// Extra submitted ids are attached as non-primary so nothing posted is dropped, then UI prevents multi-select going forward.
    /// </summary>
    public static void ApplySelectedDirectorateIds(Project project, IReadOnlyList<int> selectedIds, Action<ProjectDirectorate>? addEntity = null)
    {
        selectedIds ??= Array.Empty<int>();
        var distinct = selectedIds.Where(id => id > 0).Distinct().ToList();

        if (distinct.Count == 0)
        {
            SetPrimaryDirectorate(project, null, addEntity);
            return;
        }

        var primaryId = distinct[0];
        SetPrimaryDirectorate(project, primaryId, addEntity);

        // Preserve any additional submitted ids as non-primary (safe; UI should only send one).
        foreach (var extraId in distinct.Skip(1))
        {
            if (project.Directorates.Any(d => d.DivisionId == extraId))
            {
                continue;
            }

            var created = new ProjectDirectorate
            {
                ProjectId = project.Id,
                DivisionId = extraId,
                IsPrimary = false,
                CreatedAt = DateTime.UtcNow
            };
            project.Directorates.Add(created);
            addEntity?.Invoke(created);
        }
    }

    public static int? GetPrimaryDivisionId(Project project)
    {
        if (project.Directorates == null || project.Directorates.Count == 0)
        {
            return null;
        }

        return project.Directorates.FirstOrDefault(d => d.IsPrimary)?.DivisionId;
    }
}
