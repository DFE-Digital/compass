using System.Security.Claims;
using Compass.Data;
using Compass.Helpers;
using Compass.Models;
using Compass.Models.Fips;
using Compass.Services;
using Compass.Services.Aiss;
using Compass.Services.Fips;
using Compass.Services.Modern;
using Compass.ViewModels.Modern;
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
    private readonly IFipsDirectorateLookupSyncService _fipsDirectorateLookupSync;
    private readonly IGlobalFeatureToggleService _globalFeatureToggle;
    private readonly IProductsApiService _productsApi;
    private readonly IServiceAssessmentApiService _serviceAssessmentApi;
    private readonly IAissProductAccessibilityService _aissProductAccessibility;
    private readonly IConfiguration _configuration;
    private readonly IWorkServiceRegisterLinkService _workServiceRegisterLinks;

    public ModernManageController(
        CompassDbContext context,
        IFipsProductWriteService fipsProductWrite,
        IPermissionService permission,
        IFipsBusinessAreaLookupSyncService fipsBusinessAreaLookupSync,
        IFipsDirectorateLookupSyncService fipsDirectorateLookupSync,
        IGlobalFeatureToggleService globalFeatureToggle,
        IProductsApiService productsApi,
        IServiceAssessmentApiService serviceAssessmentApi,
        IAissProductAccessibilityService aissProductAccessibility,
        IConfiguration configuration,
        IWorkServiceRegisterLinkService workServiceRegisterLinks)
    {
        _context = context;
        _fipsProductWrite = fipsProductWrite;
        _permission = permission;
        _fipsBusinessAreaLookupSync = fipsBusinessAreaLookupSync;
        _fipsDirectorateLookupSync = fipsDirectorateLookupSync;
        _globalFeatureToggle = globalFeatureToggle;
        _productsApi = productsApi;
        _serviceAssessmentApi = serviceAssessmentApi;
        _aissProductAccessibility = aissProductAccessibility;
        _configuration = configuration;
        _workServiceRegisterLinks = workServiceRegisterLinks;
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

    private async Task<bool> CanEditFipsProductInformationAsync(CancellationToken ct)
    {
        var email = CurrentUserEmail;
        if (string.IsNullOrWhiteSpace(email))
            return false;

        return await _permission.IsOperationConsoleUserAsync(email);
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
            _ when string.Equals(tab, "risk", StringComparison.OrdinalIgnoreCase) => "risks",
            _ when string.Equals(tab, "risks", StringComparison.OrdinalIgnoreCase) => "risks",
            _ when string.Equals(tab, "issues", StringComparison.OrdinalIgnoreCase) => "issues",
            _ when string.Equals(tab, "assumptions", StringComparison.OrdinalIgnoreCase) => "assumptions",
            _ when string.Equals(tab, "dependencies", StringComparison.OrdinalIgnoreCase) => "dependencies",
            _ when string.Equals(tab, "accessibility", StringComparison.OrdinalIgnoreCase) => "accessibility",
            _ when string.Equals(tab, "performance", StringComparison.OrdinalIgnoreCase) => "performance",
            _ when string.Equals(tab, "assurance", StringComparison.OrdinalIgnoreCase) => "assurance",
            _ when string.Equals(tab, "work", StringComparison.OrdinalIgnoreCase) => "work",
            _ when string.Equals(tab, "workitems", StringComparison.OrdinalIgnoreCase) => "work",
            _ when string.Equals(tab, "details", StringComparison.OrdinalIgnoreCase) => "information",
            _ => "information"
        };

    // ── FIPS service register dashboard ─────────────────────────────────────

    [HttpGet("")]
    [HttpGet("fips")]
    [HttpGet("fips/dashboard")]
    public async Task<IActionResult> FipsDashboard(
        string? tab,
        string? search,
        int? businessAreaId,
        int? channelId,
        int? userGroupId,
        int? typeId,
        int? phaseId,
        int? categorisationItemId,
        int? categorisationGroupId,
        CancellationToken ct)
    {
        var disabled = await RequireFipsDatabaseAsync();
        if (disabled != null)
            return disabled;

        if (!string.IsNullOrWhiteSpace(tab))
        {
            var normalizedTab = tab.Trim().ToLowerInvariant();
            if (!string.Equals(normalizedTab, "active", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction(nameof(Fips), new
                {
                    tab,
                    search,
                    businessAreaId,
                    channelId,
                    userGroupId,
                    typeId,
                    phaseId,
                    categorisationItemId,
                    categorisationGroupId
                });
            }
        }

        SetNav("manage-fips-dashboard");

        var fipsBaseUrl = _configuration["fipsProduct:baseUrl"] ?? "https://fips.education.gov.uk";
        var email = CurrentUserEmail;
        var dashboardVm = await FipsManageDashboardBuilder.BuildAsync(
            _context, email, fipsBaseUrl, ct);

        var activeProducts = await FipsProductListingHelper.BuildProductsViewModelAsync(
            _context, "active", email, search, businessAreaId, channelId, userGroupId, typeId, phaseId,
            categorisationItemId, categorisationGroupId, ct);

        var baseUrl = Url.Action(nameof(FipsDashboard), "ModernManage")
                      ?? "/modern/manage/fips/dashboard";
        var sf = FipsProductListingHelper.BuildSearchAndFilter(activeProducts, "active", baseUrl);
        sf.ActiveChips = SearchAndFilterActiveChipsBuilder.FromViewModel(
            sf, Url, nameof(FipsDashboard), "ModernManage",
            new { tab = "active", categorisationItemId, categorisationGroupId });
        ViewBag.SearchAndFilter = sf;

        var vm = new FipsManageDashboardViewModel
        {
            MyProductsCount = dashboardVm.MyProductsCount,
            ActiveProductsCount = dashboardVm.ActiveProductsCount,
            EnterpriseProductsCount = dashboardVm.EnterpriseProductsCount,
            ServiceLinesCount = dashboardVm.ServiceLinesCount,
            FipsPublicBaseUrl = dashboardVm.FipsPublicBaseUrl,
            ActiveProducts = activeProducts
        };

        return View("Dashboard", vm);
    }

    // ── FIPS product listing ────────────────────────────────────────────────

    [HttpGet("fips/products")]
    public async Task<IActionResult> Fips(
        string? tab,
        string? search,
        int? businessAreaId,
        int? channelId,
        int? userGroupId,
        int? typeId,
        int? phaseId,
        int? categorisationItemId,
        int? categorisationGroupId,
        CancellationToken ct)
    {
        var disabled = await RequireFipsDatabaseAsync();
        if (disabled != null)
            return disabled;

        SetNav("manage-fips-products");

        // Default to your products.
        var activeTab = string.IsNullOrWhiteSpace(tab) ? "my" : tab.Trim().ToLowerInvariant();
        if (string.Equals(activeTab, "all", StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(search))
            activeTab = "active";
        var email = CurrentUserEmail;

        var vm = await FipsProductListingHelper.BuildProductsViewModelAsync(
            _context, activeTab, email, search, businessAreaId, channelId, userGroupId, typeId, phaseId,
            categorisationItemId, categorisationGroupId, ct);
        vm.CanSyncFromCmdb = false;

        var baseUrl = Url.Action(nameof(Fips), "ModernManage", new { tab = activeTab })
                      ?? "/modern/manage/fips/products";
        var sf = FipsProductListingHelper.BuildSearchAndFilter(vm, activeTab, baseUrl);
        sf.ActiveChips = SearchAndFilterActiveChipsBuilder.FromViewModel(
            sf, Url, nameof(Fips), "ModernManage", new { tab = activeTab, categorisationItemId, categorisationGroupId });
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

        SetNav("manage-fips-products");

        var product = await _context.CMDBProducts
            .Include(p => p.Phase)
            .Include(p => p.BusinessAreas).ThenInclude(ba => ba.FipsBusinessArea)
            .Include(p => p.Directorates).ThenInclude(d => d.FipsDirectorate).ThenInclude(fd => fd.DirectorateLookup)
            .Include(p => p.Channels).ThenInclude(c => c.FipsChannel)
            .Include(p => p.UserGroups).ThenInclude(ug => ug.FipsUserGroup)
            .Include(p => p.Types).ThenInclude(t => t.FipsType)
            .Include(p => p.CategorisationItems).ThenInclude(ci => ci.FipsCategorisationItem).ThenInclude(i => i.Group)
            .Include(p => p.Contacts).ThenInclude(c => c.FipsContactRole)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (product == null)
            return NotFound();

        var email = CurrentUserEmail;
        var isNamedContact = product.Contacts.Any(c =>
            !string.IsNullOrWhiteSpace(c.UserEmail) &&
            string.Equals(c.UserEmail.Trim(), email, StringComparison.OrdinalIgnoreCase));
        var canManage = product.Contacts.Any(c =>
            c.CanManage &&
            string.Equals(c.UserEmail, email, StringComparison.OrdinalIgnoreCase));
        var canEditInformation = isNamedContact || await CanEditFipsProductInformationAsync(ct);

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
        var editMode = canEditInformation && detailTab == "information" && edit;

        var vm = new FipsProductDetailViewModel
        {
            Product = product,
            CanManage = canManage,
            CanEditInformation = canEditInformation,
            CurrentUserEmail = email,
            AuditHistory = auditHistory,
            NavContext = null,
            EditMode = editMode,
            ActiveDetailTab = detailTab,
        };

        if (editMode)
        {
            await _fipsBusinessAreaLookupSync.SyncFromBusinessAreaLookupsAsync(ct);
            await _fipsDirectorateLookupSync.SyncFromDirectorateLookupsAsync(ct);
            vm.PhaseOptions = await _context.PhaseLookups
                .Where(x => x.IsActive).OrderBy(x => x.SortOrder).ToListAsync(ct);
            vm.DirectorateLookupOptions =
                await FipsDirectorateLookupUiHelper.LoadDirectorateLookupOptionsForEditAsync(_context, product, ct);
            vm.SelectedDirectorateLookupIds =
                FipsDirectorateLookupUiHelper.GetSelectedDirectorateLookupIds(product, vm.DirectorateLookupOptions);
            vm.BusinessAreaLookupOptions =
                await FipsBusinessAreaLookupUiHelper.LoadBusinessAreaLookupOptionsForEditAsync(_context, product, ct);
            vm.SelectedBusinessAreaLookupIds =
                FipsBusinessAreaLookupUiHelper.GetSelectedBusinessAreaLookupIds(product, vm.BusinessAreaLookupOptions);
            vm.ChannelOptions = await _context.FipsChannels
                .Where(x => x.Active).OrderBy(x => x.DisplayOrder).ToListAsync(ct);
            vm.UserGroupTreeOptions = await FipsUserGroupUiHelper.LoadActiveTreeAsync(_context, ct);
            vm.TypeOptions = await _context.FipsTypes
                .Where(x => x.Active).OrderBy(x => x.DisplayOrder).ToListAsync(ct);
        }

        await FipsProductCategorisationPresentation.PopulateAsync(
            _context,
            vm,
            editMode,
            ct);

        await FipsProductRaidQuery.PopulateRaidListsAsync(_context, vm, product, ct);

        vm.DataCompletion = FipsProductListingHelper.GetDataCompletionSummary(product);

        await PopulateFipsProductExtendedContextAsync(vm, product, ct);

        vm.AissAccessibility = await _aissProductAccessibility.LoadForProductAsync(
            product.UniqueID,
            product.Id,
            ct);

        var workLinks = await _workServiceRegisterLinks.GetLinksForServiceRegisterProductAsync(
            id,
            workId => Url.Action("Detail", "ModernWork", new { id = workId }) ?? "#",
            ct);
        vm.WorkItemsPanel = new FipsProductWorkItemsPanelViewModel
        {
            ProductId = id,
            CanLink = await _workServiceRegisterLinks.CanLinkFromServiceRegisterProductAsync(id, email, ct),
            Links = workLinks,
            PickWorkUrl = Url.Action(nameof(FipsProductPickWork), new { id }) ?? "",
            LinkUrl = Url.Action(nameof(FipsProductLinkWork), new { id }) ?? "",
        };

        return View("Detail", vm);
    }

    private async Task PopulateFipsProductExtendedContextAsync(
        FipsProductDetailViewModel vm,
        CMDBProduct product,
        CancellationToken ct)
    {
        string? cmsDocId = null;
        var fipsKey = product.CMDBID?.Trim();
        try
        {
            if (!string.IsNullOrEmpty(fipsKey))
            {
                var dto = await _productsApi.GetProductByFipsIdAsync(fipsKey);
                cmsDocId = dto?.DocumentId;
            }
        }
        catch
        {
            // CMS unavailable — fall back to identifiers we already have
        }

        vm.ResolvedCmsDocumentId = cmsDocId ?? product.Id.ToString();

        var docCandidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            product.Id.ToString(),
            vm.ResolvedCmsDocumentId
        };

        var subs = await _context.CommissionSubmissions.AsNoTracking()
            .Include(s => s.Commission)
            .Where(s =>
                s.Commission != null &&
                (docCandidates.Contains(s.ProductDocumentId) ||
                 (!string.IsNullOrEmpty(fipsKey) && s.FipsId == fipsKey)))
            .OrderByDescending(s => s.Commission!.EndDate)
            .Take(40)
            .ToListAsync(ct);

        vm.PerformanceCommissionRows = subs.Select(s => new FipsProductPerformanceRow(
            s.CommissionId,
            s.Commission!.Name,
            s.Commission.EndDate,
            s.Commission.DueDate,
            s.Status)).ToList();

        vm.LinkedProductAccessibility = await _context.ProductAccessibilities.AsNoTracking()
            .Include(a => a.Issues)
            .Where(a => !a.IsDeleted && a.IsActive &&
                        (docCandidates.Contains(a.ProductDocumentId ?? "") ||
                         (!string.IsNullOrEmpty(fipsKey) && a.FipsId == fipsKey)))
            .OrderByDescending(a => a.UpdatedAt)
            .FirstOrDefaultAsync(ct);

        vm.SasReportBaseUrl = (_configuration["FipsSync:Sas:ReportBaseUrl"]
            ?? "https://service-assessments.education.gov.uk/reports/report").TrimEnd('/');

        var productFipsId = !string.IsNullOrWhiteSpace(product.CMDBID)
            ? product.CMDBID.Trim()
            : vm.ResolvedCmsDocumentId ?? product.Id.ToString();

        var sasResponse = await _serviceAssessmentApi.GetAssessmentsByProductIdAsync(productFipsId, ct);
        var sasRows = sasResponse?.Assessments ?? new List<SasProductAssessmentRow>();
        vm.ServiceAssessments = sasRows
            .OrderByDescending(a => a.AssessmentDateTime)
            .Select(a => new FipsProductServiceAssessmentRow(
                a.AssessmentID,
                a.Type,
                a.Phase,
                a.Outcome,
                a.AssessmentDateTime,
                $"{vm.SasReportBaseUrl}/{a.AssessmentID}"))
            .ToList();
    }

    // ── Update product (named contacts or operations admin) ─────────────────

    [HttpPost("fips/{id:guid}/update")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FipsProductUpdate(Guid id, string? nc, string? userDescription,
        int? phaseId, string? productURL,
        int[]? directorateLookupIds, int[]? businessAreaLookupIds, int[]? channelIds, int[]? userGroupIds, int[]? typeIds,
        int[]? categorisationItemIds,
        bool isEnterpriseService,
        CancellationToken ct)
    {
        var disabled = await RequireFipsDatabaseAsync();
        if (disabled != null)
            return disabled;

        var requireMgr = true;
        IActionResult? opsRedirect = null;
        var email = CurrentUserEmail;
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
        else
        {
            var isNamedContact = await _context.CMDBProductContacts.AsNoTracking()
                .AnyAsync(c =>
                    c.CMDBProductId == id &&
                    c.UserEmail != null &&
                    c.UserEmail.Trim().ToLower() == email.Trim().ToLower(), ct);
            var canEditInformation = isNamedContact || await CanEditFipsProductInformationAsync(ct);
            if (!canEditInformation)
                return Forbid();
            requireMgr = false;
        }

        var resolvedDirectorateIds =
            await _fipsDirectorateLookupSync.ResolveToFipsDirectorateIdsAsync(directorateLookupIds ?? Array.Empty<int>(), ct);
        var resolvedBusinessAreaIds =
            await _fipsBusinessAreaLookupSync.ResolveToFipsBusinessAreaIdsAsync(businessAreaLookupIds ?? Array.Empty<int>(), ct);

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
            resolvedDirectorateIds,
            categorisationItemIds,
            null,
            isEnterpriseService: isEnterpriseService,
            ct);

        if (outcome.NotFound)
            return NotFound();
        if (outcome.Forbidden)
            return Forbid();

        if (outcome.Changes.Count > 0)
            TempData["Success"] = $"Product updated: {string.Join(", ", outcome.Changes)}.";

        if (opsRedirect != null)
            return opsRedirect;

        return RedirectToAction(nameof(FipsProduct), new { id, tab = "information" });
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
