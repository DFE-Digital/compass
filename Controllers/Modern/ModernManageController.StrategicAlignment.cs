using System.Text.Json;
using Compass.Models;
using Compass.Models.Fips;
using Compass.Models.Modern.Work;
using Compass.Services.Fips;
using Compass.ViewModels.Modern;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Compass.Controllers.Modern;

public partial class ModernManageController
{
    private async Task<(CMDBProduct? Product, bool CanEdit, IActionResult? Error)> LoadFipsProductForStrategicEditAsync(
        Guid id,
        CancellationToken ct)
    {
        var disabled = await RequireFipsDatabaseAsync();
        if (disabled != null)
            return (null, false, disabled);

        var product = await _context.CMDBProducts
            .Include(p => p.Phase)
            .Include(p => p.BusinessAreas).ThenInclude(ba => ba.FipsBusinessArea)
            .Include(p => p.Contacts)
            .Include(p => p.Objectives)
            .Include(p => p.Missions)
            .Include(p => p.WorkItemTags)
            .Include(p => p.RiskAppetiteLookup)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (product == null)
            return (null, false, NotFound());

        var email = CurrentUserEmail;
        var isNamedContact = product.Contacts.Any(c =>
            !string.IsNullOrWhiteSpace(c.UserEmail) &&
            string.Equals(c.UserEmail.Trim(), email, StringComparison.OrdinalIgnoreCase));
        var canEdit = isNamedContact || await CanEditFipsProductInformationAsync(ct);
        if (!canEdit)
            return (product, false, Forbid());

        return (product, true, null);
    }

    [HttpGet("fips/{id:guid}/strategic-alignment/edit")]
    public async Task<IActionResult> FipsProductEditStrategicAlignment(Guid id, CancellationToken ct)
    {
        var (product, canEdit, error) = await LoadFipsProductForStrategicEditAsync(id, ct);
        if (error != null)
            return error;
        if (product == null)
            return NotFound();

        SetNav("manage-fips-products");

        var vm = await BuildFipsProductDetailShellAsync(product, "strategic-alignment", ct);
        ViewBag.PriorityOutcomes = await _context.Objectives.AsNoTracking()
            .Where(o => !o.IsDeleted && o.Status == "active")
            .OrderBy(o => o.Title)
            .Select(o => new WorkLookupOption { Id = o.Id, Name = o.Title, Value = o.Title })
            .ToListAsync(ct);
        ViewBag.MissionPillars = await _context.Missions.AsNoTracking()
            .Where(m => !m.IsDeleted)
            .OrderBy(m => m.Title)
            .Select(m => new WorkLookupOption { Id = m.Id, Name = m.Title, Value = m.Title })
            .ToListAsync(ct);
        ViewBag.RiskAppetiteOptions = await _context.RiskAppetiteLookups.AsNoTracking()
            .Where(r => r.IsActive)
            .OrderBy(r => r.SortOrder)
            .Select(r => new LookupOption { Id = r.Id, Name = r.Name ?? "", Value = r.Description ?? "" })
            .ToListAsync(ct);
        ViewBag.WorkTagOptions = await _context.WorkItemTagLookups.AsNoTracking()
            .Where(t => t.IsActive)
            .OrderBy(t => t.SortOrder).ThenBy(t => t.Name)
            .Select(t => new LookupOption { Id = t.Id, Name = t.Name ?? "", Value = t.Description ?? "" })
            .ToListAsync(ct);
        ViewBag.SelectedWorkTagIds = product.WorkItemTags.Select(t => t.WorkItemTagLookupId).ToArray();
        ViewBag.SelectedPriorityOutcomeIds = product.Objectives.Select(o => o.ObjectiveId).ToArray();
        ViewBag.SelectedMissionPillarIds = product.Missions.Select(m => m.MissionId).ToArray();

        return View("~/Views/Modern/Manage/EditStrategicAlignment.cshtml", vm);
    }

    [HttpPost("fips/{id:guid}/strategic-alignment/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FipsProductEditStrategicAlignment(
        Guid id,
        bool? subjectToSpendControl,
        int? riskAppetiteId,
        int[]? priorityOutcomeIds,
        int[]? missionPillarIds,
        int[]? workTagIds,
        CancellationToken ct)
    {
        var (product, canEdit, error) = await LoadFipsProductForStrategicEditAsync(id, ct);
        if (error != null)
            return error;
        if (product == null)
            return NotFound();

        priorityOutcomeIds ??= Array.Empty<int>();
        missionPillarIds ??= Array.Empty<int>();
        workTagIds ??= Array.Empty<int>();

        var now = DateTime.UtcNow;
        product.IsSubjectToSpendControl = subjectToSpendControl == true;
        product.RiskAppetiteLookupId = riskAppetiteId > 0 ? riskAppetiteId : null;
        product.UpdatedAt = now;

        _context.CMDBProductObjectives.RemoveRange(product.Objectives);
        foreach (var objectiveId in priorityOutcomeIds.Distinct())
        {
            _context.CMDBProductObjectives.Add(new CMDBProductObjective
            {
                CMDBProductId = id,
                ObjectiveId = objectiveId,
                CreatedAt = now,
            });
        }

        _context.CMDBProductMissions.RemoveRange(product.Missions);
        foreach (var missionId in missionPillarIds.Distinct())
        {
            _context.CMDBProductMissions.Add(new CMDBProductMission
            {
                CMDBProductId = id,
                MissionId = missionId,
                CreatedAt = now,
            });
        }

        var validTagIds = await _context.WorkItemTagLookups.AsNoTracking()
            .Where(t => t.IsActive && workTagIds.Contains(t.Id))
            .Select(t => t.Id)
            .ToListAsync(ct);

        _context.CMDBProductWorkItemTags.RemoveRange(product.WorkItemTags);
        foreach (var tagId in validTagIds.Distinct())
        {
            _context.CMDBProductWorkItemTags.Add(new CMDBProductWorkItemTag
            {
                CMDBProductId = id,
                WorkItemTagLookupId = tagId,
            });
        }

        await _context.SaveChangesAsync(ct);
        TempData["Success"] = "Strategic alignment updated.";
        return RedirectToAction(nameof(FipsProduct), new { id, tab = "strategic-alignment" });
    }

    [HttpGet("fips/{id:guid}/multi-dept/edit")]
    public async Task<IActionResult> FipsProductEditMultiDeptCooperation(Guid id, CancellationToken ct)
    {
        var (product, canEdit, error) = await LoadFipsProductForStrategicEditAsync(id, ct);
        if (error != null)
            return error;
        if (product == null)
            return NotFound();

        SetNav("manage-fips-products");

        var vm = await BuildFipsProductDetailShellAsync(product, "strategic-alignment", ct);
        ViewBag.GovernmentDepartments = await FipsProductStrategicAlignmentPresentation
            .LoadGovernmentDepartmentsAsync(_context, product.OtherDepartments, ct);

        return View("~/Views/Modern/Manage/EditMultiDeptCooperation.cshtml", vm);
    }

    [HttpPost("fips/{id:guid}/multi-dept/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FipsProductEditMultiDeptCooperation(
        Guid id,
        [FromForm] int[]? governmentDepartmentIds,
        CancellationToken ct)
    {
        var (product, canEdit, error) = await LoadFipsProductForStrategicEditAsync(id, ct);
        if (error != null)
            return error;
        if (product == null)
            return NotFound();

        if (governmentDepartmentIds != null && governmentDepartmentIds.Length > 0)
        {
            var validIds = await _context.GovernmentDepartments.AsNoTracking()
                .Where(g => governmentDepartmentIds.Contains(g.Id))
                .Select(g => g.Id)
                .ToArrayAsync(ct);
            product.OtherDepartments = JsonSerializer.Serialize(validIds);
            product.IsMultiDepartmentProduct = validIds.Length > 0;
        }
        else
        {
            product.OtherDepartments = null;
            product.IsMultiDepartmentProduct = false;
        }

        product.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);
        TempData["Success"] = "Multi-department cooperation updated.";
        return RedirectToAction(nameof(FipsProduct), new { id, tab = "strategic-alignment" });
    }

    private async Task<FipsProductDetailViewModel> BuildFipsProductDetailShellAsync(
        CMDBProduct product,
        string tab,
        CancellationToken ct)
    {
        var email = CurrentUserEmail;
        var isNamedContact = product.Contacts.Any(c =>
            !string.IsNullOrWhiteSpace(c.UserEmail) &&
            string.Equals(c.UserEmail.Trim(), email, StringComparison.OrdinalIgnoreCase));
        var canEditInformation = isNamedContact || await CanEditFipsProductInformationAsync(ct);

        var vm = new FipsProductDetailViewModel
        {
            Product = product,
            CanManage = product.Contacts.Any(c =>
                c.CanManage &&
                string.Equals(c.UserEmail, email, StringComparison.OrdinalIgnoreCase)),
            CanEditInformation = canEditInformation,
            CurrentUserEmail = email,
            ActiveDetailTab = tab,
        };

        await FipsProductStrategicAlignmentPresentation.PopulateAsync(_context, vm, product, ct);
        return vm;
    }
}
