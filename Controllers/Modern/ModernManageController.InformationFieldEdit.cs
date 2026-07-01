using Compass.Models.Fips;
using Compass.Services.Fips;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Compass.Controllers.Modern;

public partial class ModernManageController
{
    private async Task<(CMDBProduct? Product, bool CanEdit, IActionResult? Error)> LoadFipsProductForInformationEditAsync(
        Guid id,
        string? nc,
        CancellationToken ct)
    {
        var disabled = await RequireFipsDatabaseAsync();
        if (disabled != null)
            return (null, false, disabled);

        var product = await _context.CMDBProducts
            .Include(p => p.Phase)
            .Include(p => p.BusinessAreas).ThenInclude(ba => ba.FipsBusinessArea)
            .Include(p => p.Directorates).ThenInclude(d => d.FipsDirectorate)
            .Include(p => p.Channels).ThenInclude(c => c.FipsChannel)
            .Include(p => p.UserGroups).ThenInclude(ug => ug.FipsUserGroup)
            .Include(p => p.Types).ThenInclude(t => t.FipsType)
            .Include(p => p.CategorisationItems).ThenInclude(ci => ci.FipsCategorisationItem).ThenInclude(i => i.Group)
            .Include(p => p.Contacts)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (product == null)
            return (null, false, NotFound());

        if (IsOperationsNav(nc))
        {
            if (!await IsCentralOperationsAdminAsync(ct))
                return (product, false, Forbid());
            return (product, true, null);
        }

        var email = CurrentUserEmail;
        var isNamedContact = product.Contacts.Any(c =>
            !string.IsNullOrWhiteSpace(c.UserEmail) &&
            string.Equals(c.UserEmail.Trim(), email, StringComparison.OrdinalIgnoreCase));
        var canEdit = isNamedContact || await CanEditFipsProductInformationAsync(ct);
        if (!canEdit)
            return (product, false, Forbid());

        return (product, true, null);
    }

    private async Task PopulateInformationEditOptionsAsync(FipsProductDetailViewModel vm, string? onlyField, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(onlyField))
        {
            vm.PhaseOptions = await _context.PhaseLookups
                .Where(x => x.IsActive).OrderBy(x => x.SortOrder).ToListAsync(ct);
            vm.DirectorateLookupOptions =
                await FipsDirectorateLookupUiHelper.LoadDirectorateLookupOptionsForEditAsync(_context, vm.Product, ct);
            vm.SelectedDirectorateLookupIds =
                FipsDirectorateLookupUiHelper.GetSelectedDirectorateLookupIds(vm.Product, vm.DirectorateLookupOptions);
            vm.BusinessAreaLookupOptions =
                await FipsBusinessAreaLookupUiHelper.LoadBusinessAreaLookupOptionsForEditAsync(_context, vm.Product, ct);
            vm.SelectedBusinessAreaLookupIds =
                FipsBusinessAreaLookupUiHelper.GetSelectedBusinessAreaLookupIds(vm.Product, vm.BusinessAreaLookupOptions);
            vm.ChannelOptions = await _context.FipsChannels
                .Where(x => x.Active).OrderBy(x => x.DisplayOrder).ToListAsync(ct);
            vm.UserGroupTreeOptions = await FipsUserGroupUiHelper.LoadActiveTreeAsync(_context, ct);
            vm.TypeOptions = await _context.FipsTypes
                .Where(x => x.Active).OrderBy(x => x.DisplayOrder).ToListAsync(ct);
            await FipsProductCategorisationPresentation.PopulateAsync(_context, vm, includeEditSections: true, ct);
            return;
        }

        var field = onlyField.Trim().ToLowerInvariant();
        switch (field)
        {
            case FipsProductInformationFieldKeys.Phase:
                vm.PhaseOptions = await _context.PhaseLookups
                    .Where(x => x.IsActive).OrderBy(x => x.SortOrder).ToListAsync(ct);
                break;
            case FipsProductInformationFieldKeys.Directorate:
                vm.DirectorateLookupOptions =
                    await FipsDirectorateLookupUiHelper.LoadDirectorateLookupOptionsForEditAsync(_context, vm.Product, ct);
                vm.SelectedDirectorateLookupIds =
                    FipsDirectorateLookupUiHelper.GetSelectedDirectorateLookupIds(vm.Product, vm.DirectorateLookupOptions);
                break;
            case FipsProductInformationFieldKeys.BusinessArea:
                vm.BusinessAreaLookupOptions =
                    await FipsBusinessAreaLookupUiHelper.LoadBusinessAreaLookupOptionsForEditAsync(_context, vm.Product, ct);
                vm.SelectedBusinessAreaLookupIds =
                    FipsBusinessAreaLookupUiHelper.GetSelectedBusinessAreaLookupIds(vm.Product, vm.BusinessAreaLookupOptions);
                break;
            case FipsProductInformationFieldKeys.Channel:
                vm.ChannelOptions = await _context.FipsChannels
                    .Where(x => x.Active).OrderBy(x => x.DisplayOrder).ToListAsync(ct);
                break;
            case FipsProductInformationFieldKeys.UserGroup:
                vm.UserGroupTreeOptions = await FipsUserGroupUiHelper.LoadActiveTreeAsync(_context, ct);
                break;
            case FipsProductInformationFieldKeys.Type:
                vm.TypeOptions = await _context.FipsTypes
                    .Where(x => x.Active).OrderBy(x => x.DisplayOrder).ToListAsync(ct);
                break;
            default:
                if (FipsProductInformationFieldKeys.TryParseCategorisationGroupId(field, out var groupId))
                    await FipsProductCategorisationPresentation.PopulateEditSectionForGroupAsync(_context, vm, groupId, ct);
                break;
        }
    }

    [HttpGet("fips/{id:guid}/information/edit")]
    public async Task<IActionResult> FipsProductEditInformation(Guid id, string? nc, CancellationToken ct)
    {
        var (product, canEdit, error) = await LoadFipsProductForInformationEditAsync(id, nc, ct);
        if (error != null)
            return error;
        if (product == null)
            return NotFound();

        SetNav(IsOperationsNav(nc) ? "operations-service-register" : "manage-fips-products");

        var vm = new FipsProductDetailViewModel
        {
            Product = product,
            CanManage = product.Contacts.Any(c =>
                c.CanManage &&
                string.Equals(c.UserEmail, CurrentUserEmail, StringComparison.OrdinalIgnoreCase)),
            CanEditInformation = canEdit,
            CurrentUserEmail = CurrentUserEmail,
            ActiveDetailTab = "information",
        };

        var embed = string.Equals(Request.Query["embed"].FirstOrDefault(), "1", StringComparison.Ordinal);
        var onlyField = embed ? Request.Query["field"].FirstOrDefault()?.Trim().ToLowerInvariant() : null;

        await PopulateInformationEditOptionsAsync(vm, onlyField, ct);

        ViewBag.FipsNavContext = nc;
        ViewBag.FipsInformationEditEmbed = embed;
        ViewBag.FipsInformationEditOnlyField = onlyField;

        return View("~/Views/Modern/Manage/EditInformation.cshtml", vm);
    }

    [HttpPost("fips/{id:guid}/information/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FipsProductEditInformation(
        Guid id,
        string? nc,
        string? field,
        string? userDescription,
        int? phaseId,
        string? productURL,
        int[]? directorateLookupIds,
        int[]? businessAreaLookupIds,
        int[]? channelIds,
        int[]? userGroupIds,
        int[]? typeIds,
        int[]? categorisationItemIds,
        bool isEnterpriseService,
        CancellationToken ct)
    {
        var (product, canEdit, error) = await LoadFipsProductForInformationEditAsync(id, nc, ct);
        if (error != null)
            return error;
        if (product == null)
            return NotFound();

        var email = CurrentUserEmail;
        var auditName = User.Identity?.Name ?? email;
        var requireMgr = false;

        var targetUserDescription = product.UserDescription;
        var targetPhaseId = product.PhaseId;
        var targetProductUrl = product.ProductURL;
        var targetBusinessAreaIds = product.BusinessAreas.Select(b => b.FipsBusinessAreaId).ToArray();
        var targetChannelIds = product.Channels.Select(c => c.FipsChannelId).ToArray();
        var targetUserGroupIds = product.UserGroups.Select(u => u.FipsUserGroupId).ToArray();
        var targetTypeIds = product.Types.Select(t => t.FipsTypeId).ToArray();
        var targetDirectorateIds = product.Directorates.Select(d => d.FipsDirectorateId).ToArray();
        var targetCategorisationIds = product.CategorisationItems.Select(c => c.FipsCategorisationItemId).ToArray();
        var targetEnterprise = product.IsEnterpriseService;

        var normalizedField = (field ?? "").Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalizedField))
        {
            targetUserDescription = userDescription;
            targetPhaseId = phaseId;
            targetProductUrl = productURL;
            targetEnterprise = isEnterpriseService;
            targetDirectorateIds = await _fipsDirectorateLookupSync.ResolveToFipsDirectorateIdsAsync(
                directorateLookupIds ?? [], ct);
            targetBusinessAreaIds = await _fipsBusinessAreaLookupSync.ResolveToFipsBusinessAreaIdsAsync(
                businessAreaLookupIds ?? [], ct);
            targetChannelIds = channelIds ?? [];
            targetUserGroupIds = userGroupIds ?? [];
            targetTypeIds = typeIds ?? [];
            targetCategorisationIds = categorisationItemIds ?? [];
        }
        else switch (normalizedField)
        {
            case FipsProductInformationFieldKeys.Description:
                targetUserDescription = userDescription;
                break;
            case FipsProductInformationFieldKeys.Phase:
                targetPhaseId = phaseId;
                break;
            case FipsProductInformationFieldKeys.ProductUrl:
                targetProductUrl = productURL;
                break;
            case FipsProductInformationFieldKeys.EnterpriseService:
                targetEnterprise = isEnterpriseService;
                break;
            case FipsProductInformationFieldKeys.Directorate:
                targetDirectorateIds = await _fipsDirectorateLookupSync.ResolveToFipsDirectorateIdsAsync(
                    directorateLookupIds ?? [], ct);
                break;
            case FipsProductInformationFieldKeys.BusinessArea:
                targetBusinessAreaIds = await _fipsBusinessAreaLookupSync.ResolveToFipsBusinessAreaIdsAsync(
                    businessAreaLookupIds ?? [], ct);
                break;
            case FipsProductInformationFieldKeys.Channel:
                targetChannelIds = channelIds ?? [];
                break;
            case FipsProductInformationFieldKeys.UserGroup:
                targetUserGroupIds = userGroupIds ?? [];
                break;
            case FipsProductInformationFieldKeys.Type:
                targetTypeIds = typeIds ?? [];
                break;
            default:
                if (FipsProductInformationFieldKeys.TryParseCategorisationGroupId(normalizedField, out var groupId))
                {
                    var groupItemIds = await _context.FipsCategorisationItems.AsNoTracking()
                        .Where(i => i.FipsCategorisationGroupId == groupId && i.Active)
                        .Select(i => i.Id)
                        .ToListAsync(ct);
                    var groupItemSet = groupItemIds.ToHashSet();
                    var preserved = targetCategorisationIds.Where(i => !groupItemSet.Contains(i)).ToList();
                    var selected = (categorisationItemIds ?? []).Where(groupItemSet.Contains).Distinct();
                    targetCategorisationIds = preserved.Concat(selected).Distinct().ToArray();
                }
                else
                {
                    TempData["Error"] = "Unknown field.";
                    return RedirectBackFromInformationEdit(id, nc);
                }

                break;
        }

        var outcome = await _fipsProductWrite.TryUpdateAsync(
            id,
            email,
            auditName,
            requireMgr,
            targetUserDescription,
            targetPhaseId,
            targetProductUrl,
            targetBusinessAreaIds,
            targetChannelIds,
            targetUserGroupIds,
            targetTypeIds,
            targetDirectorateIds,
            targetCategorisationIds,
            null,
            isEnterpriseService: targetEnterprise,
            ct);

        if (outcome.NotFound)
            return NotFound();
        if (outcome.Forbidden)
            return Forbid();

        if (outcome.Changes.Count > 0)
            TempData["Success"] = $"Updated: {string.Join(", ", outcome.Changes)}.";
        else
            TempData["Success"] = "No changes were needed.";

        return RedirectBackFromInformationEdit(id, nc);
    }

    private IActionResult RedirectBackFromInformationEdit(Guid id, string? nc)
    {
        if (IsOperationsNav(nc))
        {
            return RedirectToAction(
                nameof(ModernOperationsController.ServiceRegisterProduct),
                "ModernOperations",
                new { id, tab = "information" });
        }

        return RedirectToAction(nameof(FipsProduct), new { id, tab = "information" });
    }
}
