using Compass.Data;
using Compass.Models.Fips;
using Microsoft.EntityFrameworkCore;

namespace Compass.Services.Fips;

public static class FipsProductCategorisationPresentation
{
    public static void ApplySummaryLines(FipsProductDetailViewModel vm)
    {
        var product = vm.Product;
        vm.CategorisationSummaryLines.Clear();
        var pairs = product.CategorisationItems
            .Where(x => x.FipsCategorisationItem?.Group != null &&
                        x.FipsCategorisationItem.Active &&
                        x.FipsCategorisationItem.Group.Active)
            .GroupBy(x => x.FipsCategorisationItem!.Group!)
            .OrderBy(g => g.Key.DisplayOrder)
            .ThenBy(g => g.Key.Name)
            .Select(g =>
            {
                var names = g.Select(v => v.FipsCategorisationItem!.Name.Trim())
                    .Where(n => n.Length > 0)
                    .OrderBy(n => n)
                    .ToList();
                return new FipsCategorisationSummaryLine(g.Key.Id, g.Key.Name, names.Count == 0 ? "—" : string.Join(", ", names));
            })
            .ToList();

        vm.CategorisationSummaryLines.AddRange(pairs);
    }

    public static async Task PopulateEditSectionsAsync(
        CompassDbContext db,
        FipsProductDetailViewModel vm,
        CancellationToken cancellationToken)
    {
        vm.CategorisationGroupSections.Clear();

        var groups = await db.FipsCategorisationGroups.AsNoTracking()
            .Where(g => g.Active)
            .OrderBy(g => g.DisplayOrder).ThenBy(g => g.Name)
            .Select(g => new { g.Id, g.Name })
            .ToListAsync(cancellationToken);

        await AppendCategorisationEditSectionsAsync(db, vm, groups.Select(g => g.Id).ToList(), groups.ToDictionary(g => g.Id, g => g.Name), cancellationToken);
    }

    public static async Task PopulateEditSectionForGroupAsync(
        CompassDbContext db,
        FipsProductDetailViewModel vm,
        int groupId,
        CancellationToken cancellationToken)
    {
        vm.CategorisationGroupSections.Clear();
        var group = await db.FipsCategorisationGroups.AsNoTracking()
            .Where(g => g.Id == groupId && g.Active)
            .Select(g => new { g.Id, g.Name })
            .FirstOrDefaultAsync(cancellationToken);
        if (group == null)
            return;

        await AppendCategorisationEditSectionsAsync(
            db,
            vm,
            [groupId],
            new Dictionary<int, string> { [groupId] = group.Name },
            cancellationToken);
    }

    private static async Task AppendCategorisationEditSectionsAsync(
        CompassDbContext db,
        FipsProductDetailViewModel vm,
        List<int> groupIds,
        Dictionary<int, string> groupNames,
        CancellationToken cancellationToken)
    {
        var groupIdSet = groupIds.ToHashSet();
        var items = await db.FipsCategorisationItems.AsNoTracking()
            .Where(i => i.Active && groupIdSet.Contains(i.FipsCategorisationGroupId))
            .OrderBy(i => i.FipsCategorisationGroupId).ThenBy(i => i.DisplayOrder).ThenBy(i => i.Name)
            .Select(i => new { i.Id, i.Name, i.FipsCategorisationGroupId })
            .ToListAsync(cancellationToken);

        foreach (var groupId in groupIds)
        {
            if (!groupNames.TryGetValue(groupId, out var groupName))
                continue;

            var rowItems = items.Where(i => i.FipsCategorisationGroupId == groupId)
                .Select(i => new FipsCategorisationItemCheckboxOption { ItemId = i.Id, Name = i.Name })
                .ToList();
            if (rowItems.Count == 0)
                continue;

            vm.CategorisationGroupSections.Add(new FipsCategorisationGroupEditSection
            {
                GroupId = groupId,
                GroupName = groupName,
                Items = rowItems
            });
        }
    }

    /// <summary>Loads summary (and edit sections when <paramref name="includeEditSections"/>).</summary>
    public static async Task PopulateAsync(
        CompassDbContext db,
        FipsProductDetailViewModel vm,
        bool includeEditSections,
        CancellationToken cancellationToken)
    {
        ApplySummaryLines(vm);
        if (!includeEditSections)
            return;
        await PopulateEditSectionsAsync(db, vm, cancellationToken);
    }
}
