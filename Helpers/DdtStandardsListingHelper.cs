using Compass.Models;

namespace Compass.Helpers;

/// <summary>Filters DDT standards so only the latest published row per lineage is shown in register and dashboard views.</summary>
public static class DdtStandardsListingHelper
{
    public static int GetLineageRootId(IReadOnlyDictionary<int, int?> parentById, int standardId)
    {
        var visited = new HashSet<int>();
        var current = standardId;
        while (parentById.TryGetValue(current, out var parentId) && parentId.HasValue)
        {
            if (!visited.Add(current))
                break;
            current = parentId.Value;
        }

        return current;
    }

    public static HashSet<int> GetLineageIds(IEnumerable<DdtStandard> allStandards, int standardId)
    {
        var list = allStandards as IList<DdtStandard> ?? allStandards.ToList();
        var parentById = list.ToDictionary(s => s.Id, s => s.ParentStandardId);
        var rootId = GetLineageRootId(parentById, standardId);

        var lineage = new HashSet<int> { rootId };
        var changed = true;
        while (changed)
        {
            changed = false;
            foreach (var s in list)
            {
                if (s.ParentStandardId.HasValue && lineage.Contains(s.ParentStandardId.Value) && lineage.Add(s.Id))
                    changed = true;
            }
        }

        var ancestorsVisited = new HashSet<int>();
        var current = standardId;
        while (parentById.TryGetValue(current, out var parentId) && parentId.HasValue)
        {
            if (!ancestorsVisited.Add(current))
                break;
            lineage.Add(parentId.Value);
            current = parentId.Value;
        }

        return lineage;
    }

    public static IEnumerable<DdtStandard> LatestPublishedOnly(IEnumerable<DdtStandard> standards)
    {
        var list = standards.ToList();
        var parentById = list.ToDictionary(s => s.Id, s => s.ParentStandardId);

        return list
            .Where(s => s.IsPublished && s.Stage == "Published")
            .GroupBy(s => GetLineageRootId(parentById, s.Id))
            .Select(g => g
                .OrderByDescending(s => s.PublishedAt ?? s.UpdatedAt)
                .ThenByDescending(s => s.Id)
                .First());
    }

    /// <summary>Published standards in the register where the user is an owner or contact.</summary>
    public static IEnumerable<DdtStandard> YourPublishedOnly(IEnumerable<DdtStandard> standards, int userId)
    {
        var yours = standards.Where(s =>
            s.Owners.Any(o => o.UserId == userId) || s.Contacts.Any(c => c.UserId == userId));
        return LatestPublishedOnly(yours);
    }

    public static int? GetLatestPublishedIdInLineage(IEnumerable<DdtStandard> allStandards, int standardId)
    {
        var list = allStandards as IList<DdtStandard> ?? allStandards.ToList();
        var lineageIds = GetLineageIds(list, standardId);
        var latest = list
            .Where(s => lineageIds.Contains(s.Id) && s.IsPublished && s.Stage == "Published")
            .OrderByDescending(s => s.PublishedAt ?? s.UpdatedAt)
            .ThenByDescending(s => s.Id)
            .FirstOrDefault();

        return latest?.Id;
    }

    /// <summary>At most one active draft per standard lineage (new standards count as their own lineage).</summary>
    public static IEnumerable<DdtStandard> ActiveDraftsOnly(IEnumerable<DdtStandard> standards)
    {
        var list = standards as IList<DdtStandard> ?? standards.ToList();
        var parentById = list.ToDictionary(s => s.Id, s => s.ParentStandardId);

        return list
            .Where(s => s.Stage == "Draft")
            .GroupBy(d => GetLineageRootId(parentById, d.Id))
            .Select(g => g
                .OrderByDescending(s => s.UpdatedAt)
                .ThenByDescending(s => s.Id)
                .First());
    }

    /// <summary>Returns the standard id when it is already in draft on the same record (in-place editing).</summary>
    public static int? GetActiveDraftIdInLineage(IEnumerable<DdtStandard> allStandards, int standardId)
    {
        var list = allStandards as IList<DdtStandard> ?? allStandards.ToList();
        var standard = list.FirstOrDefault(s => s.Id == standardId);
        return standard is { Stage: "Draft" } ? standardId : null;
    }

    /// <summary>Withdrawn standards — unpublished by publishers or legacy archived rows.</summary>
    public static bool IsWithdrawn(DdtStandard standard) =>
        standard.Stage is "Unpublished" or "Archived";

    public static IEnumerable<DdtStandard> WithdrawnOnly(IEnumerable<DdtStandard> standards) =>
        standards.Where(IsWithdrawn);
}
