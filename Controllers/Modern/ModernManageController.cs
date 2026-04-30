using System.Security.Claims;
using Compass.Data;
using Compass.Helpers;
using Compass.Models;
using Compass.Models.Fips;
using Compass.Services;
using Compass.Services.Fips;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Compass.Controllers.Modern;

[Authorize]
[Route("modern/manage")]
public partial class ModernManageController : Controller
{
    private readonly CompassDbContext _context;
    private readonly IFipsProductWriteService _fipsProductWrite;
    private readonly IPermissionService _permission;
    private readonly IFipsBusinessAreaLookupSyncService _fipsBusinessAreaLookupSync;
    private readonly IGlobalFeatureToggleService _globalFeatureToggle;

    public ModernManageController(
        CompassDbContext context,
        IFipsProductWriteService fipsProductWrite,
        IPermissionService permission,
        IFipsBusinessAreaLookupSyncService fipsBusinessAreaLookupSync,
        IGlobalFeatureToggleService globalFeatureToggle)
    {
        _context = context;
        _fipsProductWrite = fipsProductWrite;
        _permission = permission;
        _fipsBusinessAreaLookupSync = fipsBusinessAreaLookupSync;
        _globalFeatureToggle = globalFeatureToggle;
    }

    private async Task<IActionResult?> RequireFipsDatabaseAsync()
    {
        if (!await _globalFeatureToggle.IsFeatureEnabledForPrincipalAsync(FeatureCodes.Fips, User))
        {
            TempData["ErrorMessage"] =
                "The Compass service register is turned off. FIPS product data is read from the CMS when this feature is off.";
            return RedirectToAction("Dashboard", "ModernWork");
        }

        return null;
    }

    private async Task<bool> IsCentralOperationsAdminAsync(CancellationToken ct)
    {
        var email = CurrentUserEmail;
        if (string.IsNullOrWhiteSpace(email)) return false;
        if (await _permission.IsSuperAdminAsync(email)) return true;
        return await _permission.IsInGroupAsync(email, "Central Operations Admin");
    }

    private string CurrentUserEmail =>
        User.Identity?.Name
        ?? User.FindFirst(ClaimTypes.Email)?.Value
        ?? User.FindFirst("preferred_username")?.Value
        ?? "";

    private void SetNav(string subNavItem)
    {
        ViewBag.MainNavSection = "manage";
        ViewBag.SubNavItem = subNavItem;
    }

    private static bool IsOperationsNav(string? nc) =>
        string.Equals(nc, "operations", StringComparison.OrdinalIgnoreCase);

    private static string NormalizeFipsDetailTab(string? tab) =>
        tab switch
        {
            _ when string.Equals(tab, "history", StringComparison.OrdinalIgnoreCase) => "history",
            _ when string.Equals(tab, "risks", StringComparison.OrdinalIgnoreCase) => "risks",
            _ when string.Equals(tab, "issues", StringComparison.OrdinalIgnoreCase) => "issues",
            _ => "information"
        };

    // ── FIPS product listing ────────────────────────────────────────────────

    [HttpGet("")]
    [HttpGet("fips")]
    public async Task<IActionResult> Fips(
        string? tab,
        string? search,
        int? businessAreaId,
        int? channelId,
        int? userGroupId,
        int? typeId,
        int? phaseId,
        CancellationToken ct)
    {
        var disabled = await RequireFipsDatabaseAsync();
        if (disabled != null)
            return disabled;

        SetNav("manage-fips");

        // Default to full catalogue. "Your products" only lists rows where your email matches a CMDB contact.
        var activeTab = string.IsNullOrWhiteSpace(tab) ? "all" : tab;
        var email = CurrentUserEmail;

        var vm = await FipsProductListingHelper.BuildProductsViewModelAsync(
            _context, activeTab, email, search, businessAreaId, channelId, userGroupId, typeId, phaseId, ct);
        vm.CanSyncFromCmdb = false;

        var baseUrl = Url.Action(nameof(Fips), "ModernManage", new { tab = activeTab }) ?? "/modern/manage/fips";
        var sf = FipsProductListingHelper.BuildSearchAndFilter(vm, activeTab, baseUrl);
        sf.ActiveChips = SearchAndFilterActiveChipsBuilder.FromViewModel(
            sf, Url, nameof(Fips), "ModernManage", new { tab = activeTab });
        ViewBag.SearchAndFilter = sf;

        return View("Index", vm);
    }

    // ── Product detail ──────────────────────────────────────────────────────

    [HttpGet("fips/{id:guid}")]
    public async Task<IActionResult> FipsProduct(Guid id, string? nc, string? tab, bool edit, CancellationToken ct)
    {
        var disabled = await RequireFipsDatabaseAsync();
        if (disabled != null)
            return disabled;

        if (IsOperationsNav(nc))
        {
            var opsTab = string.Equals(tab, "history", StringComparison.OrdinalIgnoreCase) ? "history"
                : string.Equals(tab, "cmdb", StringComparison.OrdinalIgnoreCase) ? "cmdb"
                : "information";
            return RedirectToAction(
                nameof(ModernOperationsController.ServiceRegisterProduct),
                "ModernOperations",
                new { id, tab = opsTab, edit });
        }

        SetNav("manage-fips");

        var product = await _context.CMDBProducts
            .Include(p => p.Phase)
            .Include(p => p.BusinessAreas).ThenInclude(ba => ba.FipsBusinessArea)
            .Include(p => p.Channels).ThenInclude(c => c.FipsChannel)
            .Include(p => p.UserGroups).ThenInclude(ug => ug.FipsUserGroup)
            .Include(p => p.Types).ThenInclude(t => t.FipsType)
            .Include(p => p.CategorisationItems).ThenInclude(ci => ci.FipsCategorisationItem).ThenInclude(i => i.Group)
            .Include(p => p.Contacts).ThenInclude(c => c.FipsContactRole)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (product == null)
            return NotFound();

        var email = CurrentUserEmail;
        var canManage = product.Contacts.Any(c =>
            c.CanManage &&
            string.Equals(c.UserEmail, email, StringComparison.OrdinalIgnoreCase));

        var productIdStr = product.Id.ToString();
        var auditHistory = await _context.AuditLogs
            .Where(a => a.Entity == "CMDBProduct" && a.EntityId == productIdStr)
            .OrderByDescending(a => a.ChangedUtc)
            .Select(a => new FipsAuditRow
            {
                ChangedAt = a.ChangedUtc,
                ChangedBy = a.ChangedBy ?? a.ChangedByEmail,
                ChangeType = a.Action,
                FieldName = a.EntityReference,
                PreviousValue = a.BeforeJson,
                NewValue = a.AfterJson
            })
            .ToListAsync(ct);

        var detailTab = NormalizeFipsDetailTab(tab);
        var editMode = canManage && edit;

        var vm = new FipsProductDetailViewModel
        {
            Product = product,
            CanManage = canManage,
            CurrentUserEmail = email,
            AuditHistory = auditHistory,
            NavContext = null,
            EditMode = editMode,
            ActiveDetailTab = detailTab,
        };

        if (canManage && editMode)
        {
            await _fipsBusinessAreaLookupSync.SyncFromBusinessAreaLookupsAsync(ct);
            vm.PhaseOptions = await _context.PhaseLookups
                .Where(x => x.IsActive).OrderBy(x => x.SortOrder).ToListAsync(ct);
            vm.BusinessAreaLookupOptions =
                await FipsBusinessAreaLookupUiHelper.LoadBusinessAreaLookupOptionsForEditAsync(_context, product, ct);
            vm.SelectedBusinessAreaLookupIds =
                FipsBusinessAreaLookupUiHelper.GetSelectedBusinessAreaLookupIds(product, vm.BusinessAreaLookupOptions);
            vm.ChannelOptions = await _context.FipsChannels
                .Where(x => x.Active).OrderBy(x => x.DisplayOrder).ToListAsync(ct);
            vm.UserGroupOptions = await _context.FipsUserGroups
                .Where(x => x.Active && x.ParentId == null).OrderBy(x => x.DisplayOrder).ToListAsync(ct);
            vm.TypeOptions = await _context.FipsTypes
                .Where(x => x.Active).OrderBy(x => x.DisplayOrder).ToListAsync(ct);
        }

        await FipsProductCategorisationPresentation.PopulateAsync(
            _context,
            vm,
            canManage && editMode && detailTab == "information",
            ct);

        await FipsProductRaidQuery.PopulateRaidListsAsync(_context, vm, product, ct);

        return View("Detail", vm);
    }

    // ── Update product (managers only) ──────────────────────────────────────

    [HttpPost("fips/{id:guid}/update")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FipsProductUpdate(Guid id, string? nc, string? userDescription,
        int? phaseId, string? productURL,
        int[]? businessAreaLookupIds, int[]? channelIds, int[]? userGroupIds, int[]? typeIds,
        int[]? categorisationItemIds,
        bool isEnterpriseService,
        CancellationToken ct)
    {
        var disabled = await RequireFipsDatabaseAsync();
        if (disabled != null)
            return disabled;

        var requireMgr = true;
        IActionResult? opsRedirect = null;
        if (IsOperationsNav(nc))
        {
            if (!await IsCentralOperationsAdminAsync(ct))
                return Forbid();
            requireMgr = false;
            opsRedirect = RedirectToAction(
                nameof(ModernOperationsController.ServiceRegisterProduct),
                "ModernOperations",
                new { id, tab = "information" });
        }

        var resolvedBusinessAreaIds =
            await _fipsBusinessAreaLookupSync.ResolveToFipsBusinessAreaIdsAsync(businessAreaLookupIds ?? Array.Empty<int>(), ct);

        var email = CurrentUserEmail;
        var auditName = User.Identity?.Name ?? email;
        var outcome = await _fipsProductWrite.TryUpdateAsync(
            id,
            email,
            auditName,
            requireMgr,
            userDescription,
            phaseId,
            productURL,
            resolvedBusinessAreaIds,
            channelIds,
            userGroupIds,
            typeIds,
            categorisationItemIds,
            null,
            isEnterpriseService,
            ct);

        if (outcome.NotFound)
            return NotFound();
        if (outcome.Forbidden)
            return Forbid();

        if (outcome.Changes.Count > 0)
            TempData["Success"] = $"Product updated: {string.Join(", ", outcome.Changes)}.";

        if (opsRedirect != null)
            return opsRedirect;

        return RedirectToAction(nameof(FipsProduct), new { id, tab = "information", edit = false });
    }

    // ── Change status ───────────────────────────────────────────────────────

    [HttpPost("fips/{id:guid}/change-status")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FipsChangeStatus(Guid id, CMDBProductStatus newStatus, string? nc, CancellationToken ct)
    {
        var disabled = await RequireFipsDatabaseAsync();
        if (disabled != null)
            return disabled;

        var requireMgr = true;
        IActionResult? opsRedirect = null;
        if (IsOperationsNav(nc))
        {
            if (!await IsCentralOperationsAdminAsync(ct))
                return Forbid();
            requireMgr = false;
            opsRedirect = RedirectToAction(
                nameof(ModernOperationsController.ServiceRegisterProduct),
                "ModernOperations",
                new { id });
        }

        var email = CurrentUserEmail;
        var auditName = User.Identity?.Name ?? email;
        var outcome = await _fipsProductWrite.TryChangeStatusAsync(
            id, email, auditName, requireMgr, newStatus, ct);

        if (outcome.NotFound)
            return NotFound();
        if (outcome.Forbidden)
            return Forbid();

        if (outcome.Changes.Count > 0)
            TempData["Success"] = "Status updated.";

        if (opsRedirect != null)
            return opsRedirect;

        return RedirectToAction(nameof(FipsProduct), new { id });
    }

    /// <summary>Legacy URL; standards live under <c>/modern/standards</c>.</summary>
    [HttpGet("standards")]
    public IActionResult Standards() =>
        RedirectToAction(nameof(ModernStandardsController.Dashboard), "ModernStandards");
}
