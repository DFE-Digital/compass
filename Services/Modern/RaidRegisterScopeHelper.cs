using Compass.Models;

namespace Compass.Services.Modern;

/// <summary>Sync and display RAID register directorate / portfolio scope (multi-select).</summary>
public static class RaidRegisterScopeHelper
{
    public static List<int> GetDirectorateIds(RaidRegister register)
    {
        var ids = register.Directorates.Select(d => d.DirectorateLookupId).ToList();
        if (ids.Count == 0 && register.DirectorateLookupId.HasValue)
            ids.Add(register.DirectorateLookupId.Value);
        return ids;
    }

    public static List<int> GetBusinessAreaIds(RaidRegister register)
    {
        var ids = register.BusinessAreas.Select(b => b.BusinessAreaLookupId).ToList();
        if (ids.Count == 0 && register.BusinessAreaLookupId.HasValue)
            ids.Add(register.BusinessAreaLookupId.Value);
        return ids;
    }

    public static string? FormatDirectorateNames(RaidRegister register)
    {
        var names = register.Directorates
            .Select(d => d.DirectorateLookup?.Name)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Cast<string>()
            .ToList();
        if (names.Count == 0 && !string.IsNullOrWhiteSpace(register.DirectorateLookup?.Name))
            names.Add(register.DirectorateLookup.Name);
        return names.Count == 0 ? null : string.Join(", ", names.OrderBy(n => n, StringComparer.OrdinalIgnoreCase));
    }

    public static string? FormatBusinessAreaNames(RaidRegister register)
    {
        var names = register.BusinessAreas
            .Select(b => b.BusinessAreaLookup?.Name)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Cast<string>()
            .ToList();
        if (names.Count == 0 && !string.IsNullOrWhiteSpace(register.BusinessAreaLookup?.Name))
            names.Add(register.BusinessAreaLookup.Name);
        return names.Count == 0 ? null : string.Join(", ", names.OrderBy(n => n, StringComparer.OrdinalIgnoreCase));
    }

    public static void SyncDirectorates(RaidRegister register, IEnumerable<int> selectedIds)
    {
        var desired = selectedIds.Where(id => id > 0).Distinct().ToHashSet();
        foreach (var toRemove in register.Directorates.Where(d => !desired.Contains(d.DirectorateLookupId)).ToList())
            register.Directorates.Remove(toRemove);
        var existing = register.Directorates.Select(d => d.DirectorateLookupId).ToHashSet();
        foreach (var id in desired.Where(id => !existing.Contains(id)))
            register.Directorates.Add(new RaidRegisterDirectorate { DirectorateLookupId = id });
        SyncLegacyFkColumns(register);
    }

    public static void SyncBusinessAreas(RaidRegister register, IEnumerable<int> selectedIds)
    {
        var desired = selectedIds.Where(id => id > 0).Distinct().ToHashSet();
        foreach (var toRemove in register.BusinessAreas.Where(b => !desired.Contains(b.BusinessAreaLookupId)).ToList())
            register.BusinessAreas.Remove(toRemove);
        var existing = register.BusinessAreas.Select(b => b.BusinessAreaLookupId).ToHashSet();
        foreach (var id in desired.Where(id => !existing.Contains(id)))
            register.BusinessAreas.Add(new RaidRegisterBusinessArea { BusinessAreaLookupId = id });
        SyncLegacyFkColumns(register);
    }

    public static void SyncScope(RaidRegister register, IEnumerable<int> directorateIds, IEnumerable<int> businessAreaIds)
    {
        SyncDirectorates(register, directorateIds);
        SyncBusinessAreas(register, businessAreaIds);
    }

    private static void SyncLegacyFkColumns(RaidRegister register)
    {
        register.DirectorateLookupId = register.Directorates.Count > 0
            ? register.Directorates.Min(d => d.DirectorateLookupId)
            : null;
        register.BusinessAreaLookupId = register.BusinessAreas.Count > 0
            ? register.BusinessAreas.Min(b => b.BusinessAreaLookupId)
            : null;
    }
}
