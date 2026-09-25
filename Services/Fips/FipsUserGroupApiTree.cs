using Compass.Models.Fips;

namespace Compass.Services.Fips;

/// <summary>
/// Builds the FIPS user-group API tree. Every group is returned, nested under its parent
/// at every level. A group whose parent is missing is still returned.
/// </summary>
public static class FipsUserGroupApiTree
{
    public static List<FipsUserGroupApiRow> Build(IReadOnlyList<FipsUserGroup> all)
    {
        var knownIds = all.Select(g => g.Id).ToHashSet();
        var byParent = all
            .Where(g => g.ParentId.HasValue && knownIds.Contains(g.ParentId.Value))
            .GroupBy(g => g.ParentId!.Value)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase).ToList());

        var included = new HashSet<int>();

        FipsUserGroupApiRow Map(FipsUserGroup group, HashSet<int> trail)
        {
            if (!trail.Add(group.Id))
            {
                return new FipsUserGroupApiRow(
                    group.Id,
                    group.ParentId,
                    group.Name,
                    group.Description,
                    group.DisplayOrder,
                    group.Active,
                    [],
                    Synonyms(group),
                    []);
            }

            included.Add(group.Id);
            var childGroups = byParent.TryGetValue(group.Id, out var found)
                ? found.Select(child => Map(child, trail)).ToList()
                : [];
            trail.Remove(group.Id);

            return new FipsUserGroupApiRow(
                group.Id,
                group.ParentId,
                group.Name,
                group.Description,
                group.DisplayOrder,
                group.Active,
                childGroups,
                Synonyms(group),
                childGroups);
        }

        var roots = all
            .Where(g => !g.ParentId.HasValue || !knownIds.Contains(g.ParentId.Value))
            .OrderBy(g => g.DisplayOrder)
            .ThenBy(g => g.Name, StringComparer.OrdinalIgnoreCase)
            .Select(g => Map(g, []))
            .ToList();

        // A parent cycle can hide a group from the walk above. Still return it.
        foreach (var leftover in all
                     .Where(g => !included.Contains(g.Id))
                     .OrderBy(g => g.DisplayOrder)
                     .ThenBy(g => g.Name, StringComparer.OrdinalIgnoreCase))
        {
            roots.Add(Map(leftover, []));
        }

        return roots;
    }

    private static List<string> Synonyms(FipsUserGroup group) =>
        group.Synonyms
            .Select(s => s.Synonym)
            .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
            .ToList();

    public sealed record FipsUserGroupApiRow(
        int Id,
        int? ParentId,
        string Name,
        string? Description,
        int DisplayOrder,
        bool Active,
        IReadOnlyList<FipsUserGroupApiRow> Children,
        IReadOnlyList<string> Synonyms,
        IReadOnlyList<FipsUserGroupApiRow> ChildGroups);
}
