using Compass.Models.Fips;

namespace Compass.ViewModels.Modern;

public static class AdminFipsUserGroupTreeHelper
{
    public static List<AdminFipsUserGroupRow> BuildFlatTree(IReadOnlyList<FipsUserGroup> allGroups)
    {
        var childrenByParentId = allGroups
            .Where(g => g.ParentId.HasValue)
            .GroupBy(g => g.ParentId!.Value)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ToList());

        var roots = allGroups
            .Where(g => !g.ParentId.HasValue)
            .OrderBy(g => g.DisplayOrder)
            .ThenBy(g => g.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var rows = new List<AdminFipsUserGroupRow>();
        var included = new HashSet<int>();

        foreach (var root in roots)
            Walk(root, depth: 0, ancestorNames: [], childrenByParentId, rows, included);

        // Groups whose parent is missing or not reachable still appear (e.g. bad ParentId after edits).
        var orphans = allGroups
            .Where(g => g.ParentId.HasValue && !included.Contains(g.Id))
            .OrderBy(g => g.DisplayOrder)
            .ThenBy(g => g.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var orphan in orphans)
        {
            var parentPath = BuildParentPath(orphan.ParentId, allGroups);
            var ancestorNames = string.IsNullOrWhiteSpace(parentPath)
                ? Array.Empty<string>()
                : parentPath.Split(" → ", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

            Walk(orphan, ancestorNames.Length, ancestorNames, childrenByParentId, rows, included);
        }

        return rows;
    }

    public static List<AdminFipsUserGroupParentOption> BuildParentOptions(IReadOnlyList<AdminFipsUserGroupRow> flatTree) =>
        flatTree
            .Select(row => new AdminFipsUserGroupParentOption
            {
                Id = row.Id,
                Label = FormatIndentedLabel(row.Name, row.Depth)
            })
            .ToList();

    public static List<AdminFipsUserGroupParentOption> BuildParentOptionsForEdit(
        IReadOnlyList<FipsUserGroup> allGroups,
        int? excludeGroupId)
    {
        var exclude = excludeGroupId.HasValue
            ? GetSelfAndDescendantIds(excludeGroupId.Value, allGroups)
            : [];

        return BuildFlatTree(allGroups)
            .Where(row => !exclude.Contains(row.Id))
            .Select(row => new AdminFipsUserGroupParentOption
            {
                Id = row.Id,
                Label = FormatIndentedLabel(row.Name, row.Depth)
            })
            .ToList();
    }

    public static HashSet<int> GetSelfAndDescendantIds(int groupId, IReadOnlyList<FipsUserGroup> allGroups)
    {
        var childrenByParentId = allGroups
            .Where(g => g.ParentId.HasValue)
            .GroupBy(g => g.ParentId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        var ids = new HashSet<int> { groupId };
        CollectDescendantIds(groupId, childrenByParentId, ids);
        return ids;
    }

    public static string? BuildParentPath(int? parentId, IReadOnlyList<FipsUserGroup> allGroups)
    {
        if (!parentId.HasValue)
            return null;

        var byId = allGroups.ToDictionary(g => g.Id);
        if (!byId.TryGetValue(parentId.Value, out var parent))
            return null;

        var names = new List<string>();
        var current = parent;
        var seen = new HashSet<int>();
        while (true)
        {
            if (!seen.Add(current.Id))
                break;

            names.Insert(0, current.Name);
            if (!current.ParentId.HasValue || !byId.TryGetValue(current.ParentId.Value, out current!))
                break;
        }

        return names.Count > 0 ? string.Join(" → ", names) : null;
    }

    public static bool IsValidParentAssignment(int? parentId, int? groupId, IReadOnlyList<FipsUserGroup> allGroups)
    {
        if (!parentId.HasValue)
            return true;

        if (!allGroups.Any(g => g.Id == parentId.Value))
            return false;

        if (!groupId.HasValue)
            return true;

        if (parentId.Value == groupId.Value)
            return false;

        return !GetSelfAndDescendantIds(groupId.Value, allGroups).Contains(parentId.Value);
    }

    private static void CollectDescendantIds(
        int groupId,
        IReadOnlyDictionary<int, List<FipsUserGroup>> childrenByParentId,
        HashSet<int> ids)
    {
        if (!childrenByParentId.TryGetValue(groupId, out var children))
            return;

        foreach (var child in children)
        {
            if (ids.Add(child.Id))
                CollectDescendantIds(child.Id, childrenByParentId, ids);
        }
    }

    public static string FormatIndentedLabel(string name, int depth)
    {
        if (depth <= 0)
            return name;

        return $"{new string('—', depth)} {name}";
    }

    private static void Walk(
        FipsUserGroup node,
        int depth,
        IReadOnlyList<string> ancestorNames,
        IReadOnlyDictionary<int, List<FipsUserGroup>> childrenByParentId,
        List<AdminFipsUserGroupRow> rows,
        HashSet<int> included)
    {
        if (!included.Add(node.Id))
            return;

        var hasChildren = childrenByParentId.ContainsKey(node.Id);

        rows.Add(new AdminFipsUserGroupRow
        {
            Id = node.Id,
            Name = node.Name,
            Description = node.Description,
            DisplayOrder = node.DisplayOrder,
            Active = node.Active,
            Depth = depth,
            ParentPath = ancestorNames.Count > 0 ? string.Join(" → ", ancestorNames) : null,
            HasChildren = hasChildren,
            SynonymNames = node.Synonyms
                .Select(s => s.Synonym)
                .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                .ToList()
        });

        if (!hasChildren)
            return;

        var path = ancestorNames.Concat([node.Name]).ToList();
        foreach (var child in childrenByParentId[node.Id])
            Walk(child, depth + 1, path, childrenByParentId, rows, included);
    }
}
