using System.Text.Json;
using Compass.Data;
using Compass.Models.Fips;
using Compass.Models.Modern.Work;
using Compass.ViewModels.Modern;
using Microsoft.EntityFrameworkCore;

namespace Compass.Services.Fips;

public static class FipsProductStrategicAlignmentPresentation
{
    public static async Task PopulateAsync(
        CompassDbContext db,
        FipsProductDetailViewModel vm,
        CMDBProduct product,
        CancellationToken ct)
    {
        var priorityOutcomeNames = product.Objectives
            .Select(o => o.Objective?.Title)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Cast<string>()
            .OrderBy(n => n)
            .ToList();

        var missionPillarNames = product.Missions
            .Select(m => m.Mission?.Title)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Cast<string>()
            .OrderBy(n => n)
            .ToList();

        var tags = product.WorkItemTags
            .Select(t => t.WorkItemTagLookup)
            .Where(t => t != null && !string.IsNullOrWhiteSpace(t.Name))
            .Select(t => new FipsProductStrategicAlignmentTag(t!.Id, t.Name!))
            .OrderBy(t => t.Name)
            .ToList();

        var governmentDepartmentNames = await LoadGovernmentDepartmentNamesAsync(db, product.OtherDepartments, ct);

        var riskAppetiteOptions = await db.RiskAppetiteLookups.AsNoTracking()
            .Where(r => r.IsActive)
            .OrderBy(r => r.SortOrder)
            .Select(r => new LookupOption { Id = r.Id, Name = r.Name ?? "", Value = r.Description ?? "" })
            .ToListAsync(ct);

        var riskAppetiteName = product.RiskAppetiteLookupId.HasValue
            ? riskAppetiteOptions.FirstOrDefault(r => r.Id == product.RiskAppetiteLookupId)?.Name ?? "—"
            : "—";

        vm.StrategicAlignment = new FipsProductStrategicAlignmentPanel
        {
            PriorityOutcomeNames = priorityOutcomeNames,
            MissionPillarNames = missionPillarNames,
            Tags = tags,
            GovernmentDepartmentNames = governmentDepartmentNames,
            SubjectToSpendControl = product.IsSubjectToSpendControl == true,
            RiskAppetiteId = product.RiskAppetiteLookupId,
            RiskAppetiteName = riskAppetiteName,
            RiskAppetiteOptions = riskAppetiteOptions,
        };
    }

    public static async Task<List<FipsProductGovernmentDepartmentRow>> LoadGovernmentDepartmentsAsync(
        CompassDbContext db,
        string? otherDepartmentsJson,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(otherDepartmentsJson))
            return new List<FipsProductGovernmentDepartmentRow>();

        int[] ids;
        try
        {
            ids = JsonSerializer.Deserialize<int[]>(otherDepartmentsJson) ?? Array.Empty<int>();
        }
        catch
        {
            return new List<FipsProductGovernmentDepartmentRow>();
        }

        if (ids.Length == 0)
            return new List<FipsProductGovernmentDepartmentRow>();

        return await db.GovernmentDepartments.AsNoTracking()
            .Where(g => ids.Contains(g.Id))
            .OrderBy(g => g.Title)
            .Select(g => new FipsProductGovernmentDepartmentRow(g.Id, g.Title ?? ""))
            .ToListAsync(ct);
    }

    private static async Task<List<string>> LoadGovernmentDepartmentNamesAsync(
        CompassDbContext db,
        string? otherDepartmentsJson,
        CancellationToken ct)
    {
        var rows = await LoadGovernmentDepartmentsAsync(db, otherDepartmentsJson, ct);
        return rows.Select(r => r.Title).Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
    }
}
