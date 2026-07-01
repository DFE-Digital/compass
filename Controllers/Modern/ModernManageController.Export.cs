using Compass.Services.Fips;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Compass.Controllers.Modern;

public partial class ModernManageController
{
    [HttpGet("fips/products/export")]
    public async Task<IActionResult> ExportFipsProducts(
        bool allData = false,
        string? tab = null,
        string? search = null,
        int? businessAreaId = null,
        int? channelId = null,
        int? userGroupId = null,
        int? typeId = null,
        int? phaseId = null,
        int? categorisationItemId = null,
        int? categorisationGroupId = null,
        CancellationToken ct = default)
    {
        var disabled = await RequireFipsDatabaseAsync();
        if (disabled != null)
            return disabled;

        var email = CurrentUserEmail;
        string activeTab;
        string? exportSearch;
        int? exportBa;
        int? exportCh;
        int? exportUg;
        int? exportTy;
        int? exportPh;
        int? exportCatItem;
        int? exportCatGroup;

        if (allData)
        {
            activeTab = "all";
            exportSearch = null;
            exportBa = null;
            exportCh = null;
            exportUg = null;
            exportTy = null;
            exportPh = null;
            exportCatItem = null;
            exportCatGroup = null;
        }
        else
        {
            activeTab = string.IsNullOrWhiteSpace(tab) ? "my" : tab.Trim().ToLowerInvariant();
            exportSearch = search;
            exportBa = businessAreaId;
            exportCh = channelId;
            exportUg = userGroupId;
            exportTy = typeId;
            exportPh = phaseId;
            exportCatItem = categorisationItemId;
            exportCatGroup = categorisationGroupId;
        }

        var vm = await FipsProductListingHelper.BuildProductsViewModelAsync(
            _context,
            activeTab,
            email,
            exportSearch,
            exportBa,
            exportCh,
            exportUg,
            exportTy,
            exportPh,
            exportCatItem,
            exportCatGroup,
            ct);

        if (vm.Products.Count == 0)
            return BadRequest("No products to export for this view.");

        var ids = vm.Products.Select(p => p.Id).ToList();
        var cmdbIds = await _context.CMDBProducts.AsNoTracking()
            .Where(p => ids.Contains(p.Id))
            .Select(p => new { p.Id, p.CMDBID })
            .ToDictionaryAsync(p => p.Id, p => p.CMDBID, ct);

        var bytes = FipsProductExcelExport.BuildWorkbook(vm.Products, cmdbIds);
        var scopeLabel = allData ? "all-data" : (vm.IsSearchResults ? "search" : activeTab);
        var fileName = $"service-register-{scopeLabel}-{DateTime.UtcNow:yyyyMMdd-HHmm}.xlsx";
        return File(
            bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }
}
