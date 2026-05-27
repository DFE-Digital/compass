using Compass.Models;
using Compass.Services.Raid;
using Compass.ViewModels.Modern;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Compass.Controllers.Modern;

public partial class ModernAdminController
{
    private async Task PopulateRaidRegisterTableViewPanelAsync(AdminHubViewModel vm, string? entityType, CancellationToken ct)
    {
        var layoutService = HttpContext.RequestServices.GetRequiredService<IRaidRegisterSpreadsheetLayoutService>();
        var active = string.IsNullOrWhiteSpace(entityType)
            ? RaidRegisterSpreadsheetColumnCatalog.EntityRisk
            : entityType.Trim().ToLowerInvariant();
        if (!RaidRegisterSpreadsheetColumnCatalog.IsKnownEntityType(active))
            active = RaidRegisterSpreadsheetColumnCatalog.EntityRisk;

        var savedRows = await _context.RaidRegisterSpreadsheetLayouts.AsNoTracking()
            .ToDictionaryAsync(x => x.EntityType, x => x, StringComparer.OrdinalIgnoreCase, ct);

        var tabs = RaidRegisterSpreadsheetColumnCatalog.EntityTypes.Select(et => new AdminRaidRegisterTableEntityTab
        {
            EntityType = et,
            Label = RaidRegisterSpreadsheetColumnCatalog.GetEntityLabel(et),
            IsActive = string.Equals(et, active, StringComparison.OrdinalIgnoreCase),
            HasCustomLayout = savedRows.ContainsKey(et)
        }).ToList();

        var order = await layoutService.GetColumnOrderAsync(active, ct);
        var columnMap = RaidRegisterSpreadsheetColumnCatalog.GetColumns(active)
            .ToDictionary(c => c.Key, c => c.Label, StringComparer.OrdinalIgnoreCase);

        var columns = order
            .Where(columnMap.ContainsKey)
            .Select(key => new AdminRaidRegisterTableColumnRow
            {
                Key = key,
                Label = columnMap[key]
            })
            .ToList();

        RaidRegisterSpreadsheetLayout? saved = null;
        savedRows.TryGetValue(active, out saved);

        vm.RaidRegisterTableView = new AdminRaidRegisterTableViewPanel
        {
            ActiveEntityType = active,
            Tabs = tabs,
            Columns = columns,
            HasCustomLayout = saved != null,
            UpdatedAt = saved?.UpdatedAt,
            UpdatedByName = saved?.UpdatedByUserId is int uid
                ? await _context.Users.AsNoTracking()
                    .Where(u => u.Id == uid)
                    .Select(u => u.Name ?? u.Email)
                    .FirstOrDefaultAsync(ct)
                : null
        };
    }

    [HttpPost("raid-register-table-view/save")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RaidRegisterTableViewSave(
        string entityType,
        [FromForm] List<string>? columnOrder,
        CancellationToken ct = default)
    {
        var layoutService = HttpContext.RequestServices.GetRequiredService<IRaidRegisterSpreadsheetLayoutService>();
        var normalized = (entityType ?? "").Trim().ToLowerInvariant();
        if (!RaidRegisterSpreadsheetColumnCatalog.IsKnownEntityType(normalized))
        {
            TempData["AdminError"] = "Unknown table type.";
            return RedirectToAction(nameof(Index), new { panel = "raid-register-table-view", entity = RaidRegisterSpreadsheetColumnCatalog.EntityRisk });
        }

        if (columnOrder == null || columnOrder.Count == 0)
        {
            TempData["AdminError"] = "Column order is required.";
            return RedirectToAction(nameof(Index), new { panel = "raid-register-table-view", entity = normalized });
        }

        var userId = await ResolveAdminUserIdAsync(ct);
        await layoutService.SaveColumnOrderAsync(normalized, columnOrder, userId, ct);
        TempData["AdminMessage"] = $"Default column order saved for {RaidRegisterSpreadsheetColumnCatalog.GetEntityLabel(normalized).ToLowerInvariant()} table.";
        return RedirectToAction(nameof(Index), new { panel = "raid-register-table-view", entity = normalized });
    }

    [HttpPost("raid-register-table-view/reset")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RaidRegisterTableViewReset(string entityType, CancellationToken ct = default)
    {
        var layoutService = HttpContext.RequestServices.GetRequiredService<IRaidRegisterSpreadsheetLayoutService>();
        var normalized = (entityType ?? "").Trim().ToLowerInvariant();
        if (!RaidRegisterSpreadsheetColumnCatalog.IsKnownEntityType(normalized))
        {
            TempData["AdminError"] = "Unknown table type.";
            return RedirectToAction(nameof(Index), new { panel = "raid-register-table-view", entity = RaidRegisterSpreadsheetColumnCatalog.EntityRisk });
        }

        await layoutService.DeleteSavedLayoutAsync(normalized, ct);
        TempData["AdminMessage"] = $"Reset to built-in default column order for {RaidRegisterSpreadsheetColumnCatalog.GetEntityLabel(normalized).ToLowerInvariant()} table.";
        return RedirectToAction(nameof(Index), new { panel = "raid-register-table-view", entity = normalized });
    }

    private async Task<int?> ResolveAdminUserIdAsync(CancellationToken ct)
    {
        var email = User?.Identity?.Name;
        if (string.IsNullOrWhiteSpace(email)) return null;
        return await _context.Users.AsNoTracking()
            .Where(u => u.Email == email)
            .Select(u => (int?)u.Id)
            .FirstOrDefaultAsync(ct);
    }
}
