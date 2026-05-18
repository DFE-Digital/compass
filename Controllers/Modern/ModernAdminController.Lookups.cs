using Compass.Models;
using Compass.ViewModels.Modern;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Compass.Controllers.Modern;

public partial class ModernAdminController
{
    private static readonly HashSet<string> LookupEditorPanels = new(StringComparer.OrdinalIgnoreCase)
    {
        "business-areas", "phases", "directorates", "universal-barriers", "priorities", "rag-defns",
        "activity-types", "work-tagging", "mission-pillars", "priority-outcomes", "portfolios",
        "risk-tiers", "risk-categories", "issue-categories", "departments",
        "std-categories", "std-subcategories",
        "risk-statuses", "risk-priorities", "risk-likelihoods", "risk-impact-levels", "risk-proximities", "risk-treatments", "risk-appetites",
        "action-statuses", "action-priorities", "action-types", "action-categories",
        "action-impact-levels", "action-reminder-frequencies", "action-escalation-thresholds",
        "issue-statuses", "issue-priorities", "issue-severities",
        "decision-statuses", "decision-priorities", "decision-outcomes", "decision-implementation-statuses",
        "raid-evidence-types", "governance-boards",         "demand-request-statuses", "triage-outcome-stages",
        "assumption-statuses", "assumption-criticalities", "dependency-criticalities", "dependency-link-types",
        "near-miss-types", "near-miss-seriousness", "near-miss-statuses"
    };

    [HttpGet("lookup/{panel}/create")]
    public async Task<IActionResult> LookupCreate(string panel)
    {
        if (!LookupEditorPanels.Contains(panel))
            return NotFound();
        SetAdminChrome("admin-index");
        var vm = await BuildLookupEditorAsync(panel, null);
        if (vm == null) return NotFound();
        vm.IsCreate = true;
        vm.PageHeading = $"Add {GetLookupSingularTitle(panel)}";
        if (TempData["AdminMessage"] is string msg)
            ViewBag.AdminMessage = msg;
        return View("~/Views/Modern/Admin/LookupEditor.cshtml", vm);
    }

    [HttpGet("lookup/{panel}/edit/{id:int}")]
    public async Task<IActionResult> LookupEdit(string panel, int id)
    {
        if (!LookupEditorPanels.Contains(panel))
            return NotFound();
        SetAdminChrome("admin-index");
        var vm = await BuildLookupEditorAsync(panel, id);
        if (vm == null) return NotFound();
        vm.IsCreate = false;
        vm.PageHeading = $"Edit {GetLookupSingularTitle(panel)}";
        if (TempData["AdminMessage"] is string msg)
            ViewBag.AdminMessage = msg;
        return View("~/Views/Modern/Admin/LookupEditor.cshtml", vm);
    }

    private static string GetLookupSingularTitle(string panel) => panel switch
    {
        "business-areas" => "business area",
        "phases" => "delivery phase",
        "directorates" => "directorate",
        "universal-barriers" => "universal barrier",
        "priorities" => "priority level",
        "rag-defns" => "RAG status",
        "activity-types" => "activity type",
        "work-tagging" => "work tag",
        "mission-pillars" => "mission pillar",
        "priority-outcomes" => "priority outcome",
        "portfolios" => "portfolio",
        "risk-tiers" => "risk tier",
        "risk-categories" => "risk category",
        "issue-categories" => "issue category",
        "departments" => "government department",
        "std-categories" => "standard category",
        "std-subcategories" => "standard sub-category",
        "risk-statuses" => "risk status",
        "risk-priorities" => "risk priority",
        "risk-likelihoods" => "risk likelihood",
        "risk-impact-levels" => "risk impact level",
        "risk-proximities" => "risk proximity",
        "risk-treatments" => "risk treatment",
        "risk-appetites" => "risk appetite",
        "action-statuses" => "action status",
        "action-priorities" => "action priority",
        "action-types" => "action type",
        "action-categories" => "action category",
        "action-impact-levels" => "action impact level",
        "action-reminder-frequencies" => "action reminder frequency",
        "action-escalation-thresholds" => "action escalation threshold",
        "issue-statuses" => "issue status",
        "issue-priorities" => "issue priority",
        "issue-severities" => "issue severity",
        "decision-statuses" => "decision status",
        "decision-priorities" => "decision priority",
        "decision-outcomes" => "decision outcome",
        "decision-implementation-statuses" => "decision implementation status",
        "raid-evidence-types" => "evidence type",
        "governance-boards" => "governance board",
        "demand-request-statuses" => "demand request status",
        "triage-outcome-stages" => "triage outcome stage",
        "assumption-statuses" => "assumption status",
        "assumption-criticalities" => "assumption criticality",
        "dependency-criticalities" => "dependency criticality",
        "dependency-link-types" => "dependency link type",
        "near-miss-types" => "near miss type",
        "near-miss-seriousness" => "near miss seriousness level",
        "near-miss-statuses" => "near miss status",
        _ => "item"
    };

    private async Task<AdminLookupEditorViewModel?> BuildLookupEditorAsync(string panel, int? id)
    {
        var p = panel.Trim().ToLowerInvariant();
        switch (p)
        {
            case "business-areas":
                if (id is null)
                    return new AdminLookupEditorViewModel { Panel = p, EditorKind = AdminLookupEditorKind.Simple, SortOrder = 0, IsActive = true };
                var ba = await _context.BusinessAreaLookups.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
                if (ba == null) return null;
                return new AdminLookupEditorViewModel
                {
                    Panel = p, EditorKind = AdminLookupEditorKind.Simple, Id = ba.Id, Name = ba.Name, Description = ba.Description,
                    SortOrder = ba.SortOrder, IsActive = ba.IsActive
                };

            case "phases":
                if (id is null)
                    return new AdminLookupEditorViewModel { Panel = p, EditorKind = AdminLookupEditorKind.Simple, SortOrder = 0, IsActive = true };
                var ph = await _context.PhaseLookups.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
                if (ph == null) return null;
                return new AdminLookupEditorViewModel
                {
                    Panel = p, EditorKind = AdminLookupEditorKind.Simple, Id = ph.Id, Name = ph.Name, Description = ph.Description,
                    SortOrder = ph.SortOrder, IsActive = ph.IsActive
                };

            case "directorates":
                if (id is null)
                    return new AdminLookupEditorViewModel { Panel = p, EditorKind = AdminLookupEditorKind.Simple, SortOrder = 0, IsActive = true };
                var dir = await _context.DirectorateLookups.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
                if (dir == null) return null;
                return new AdminLookupEditorViewModel
                {
                    Panel = p, EditorKind = AdminLookupEditorKind.Simple, Id = dir.Id, Name = dir.Name, Description = dir.Description,
                    SortOrder = dir.SortOrder, IsActive = dir.IsActive
                };

            case "universal-barriers":
                if (id is null)
                    return new AdminLookupEditorViewModel { Panel = p, EditorKind = AdminLookupEditorKind.UniversalBarrier, SortOrder = 0, IsActive = true };
                var ub = await _context.UniversalBarrierLookups.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
                if (ub == null) return null;
                return new AdminLookupEditorViewModel
                {
                    Panel = p, EditorKind = AdminLookupEditorKind.UniversalBarrier, Id = ub.Id, Name = ub.Name, Description = ub.Description,
                    GuidanceUrl = ub.GuidanceUrl, SortOrder = ub.SortOrder, IsActive = ub.IsActive
                };

            case "priorities":
                if (id is null)
                    return new AdminLookupEditorViewModel { Panel = p, EditorKind = AdminLookupEditorKind.Priority, SortOrder = 0, IsActive = true };
                var pr = await _context.DeliveryPriorities.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
                if (pr == null) return null;
                return new AdminLookupEditorViewModel
                {
                    Panel = p, EditorKind = AdminLookupEditorKind.Priority, Id = pr.Id, Name = pr.Name, Summary = pr.Summary,
                    Description = pr.Description, CssClass = pr.CssClass, SortOrder = pr.SortOrder, IsActive = pr.IsActive
                };

            case "rag-defns":
                if (id is null)
                    return new AdminLookupEditorViewModel { Panel = p, EditorKind = AdminLookupEditorKind.Rag, SortOrder = 0, IsActive = true };
                var rg = await _context.RagStatusLookups.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
                if (rg == null) return null;
                return new AdminLookupEditorViewModel
                {
                    Panel = p, EditorKind = AdminLookupEditorKind.Rag, Id = rg.Id, Name = rg.Name, Description = rg.Description,
                    CssClass = rg.CssClass, SortOrder = rg.SortOrder, IsActive = rg.IsActive
                };

            case "activity-types":
                if (id is null)
                    return new AdminLookupEditorViewModel { Panel = p, EditorKind = AdminLookupEditorKind.ActivityType, SortOrder = 0, IsActive = true };
                var at = await _context.ActivityTypeLookups.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
                if (at == null) return null;
                return new AdminLookupEditorViewModel
                {
                    Panel = p, EditorKind = AdminLookupEditorKind.ActivityType, Id = at.Id, Name = at.Name, Description = at.Description,
                    SortOrder = at.SortOrder, IsActive = at.IsActive
                };

            case "work-tagging":
                if (id is null)
                    return new AdminLookupEditorViewModel { Panel = p, EditorKind = AdminLookupEditorKind.WorkItemTag, SortOrder = 0, IsActive = true };
                var wt = await _context.WorkItemTagLookups.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
                if (wt == null) return null;
                return new AdminLookupEditorViewModel
                {
                    Panel = p, EditorKind = AdminLookupEditorKind.WorkItemTag, Id = wt.Id, Name = wt.Name, Description = wt.Description,
                    SortOrder = wt.SortOrder, IsActive = wt.IsActive
                };

            case "mission-pillars":
                if (id is null)
                    return new AdminLookupEditorViewModel { Panel = p, EditorKind = AdminLookupEditorKind.MissionPillar, SortOrder = 0, IsActive = true };
                var ms = await _context.Missions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
                if (ms == null) return null;
                return new AdminLookupEditorViewModel
                {
                    Panel = p, EditorKind = AdminLookupEditorKind.MissionPillar, Id = ms.Id, Name = ms.Title, Description = ms.Description,
                    SortOrder = 0, IsActive = !ms.IsDeleted
                };

            case "priority-outcomes":
                if (id is null)
                {
                    var missions = await _context.Missions.AsNoTracking().Where(m => !m.IsDeleted).OrderBy(m => m.Title)
                        .Select(m => new SelectListItem { Value = m.Id.ToString(), Text = m.Title }).ToListAsync();
                    return new AdminLookupEditorViewModel
                    {
                        Panel = p, EditorKind = AdminLookupEditorKind.PriorityOutcome, SortOrder = 0, IsActive = true,
                        MissionOptions = missions, Status = "active"
                    };
                }
                var ob = await _context.Objectives.AsNoTracking()
                    .Include(o => o.OwnerUser)
                    .Include(o => o.ThemeSroUser)
                    .Include(o => o.OutcomeSroUser)
                    .FirstOrDefaultAsync(x => x.Id == id);
                if (ob == null) return null;
                var missionOpts = await _context.Missions.AsNoTracking().Where(m => !m.IsDeleted).OrderBy(m => m.Title)
                    .Select(m => new SelectListItem { Value = m.Id.ToString(), Text = m.Title, Selected = ob.MissionId == m.Id }).ToListAsync();
                return new AdminLookupEditorViewModel
                {
                    Panel = p, EditorKind = AdminLookupEditorKind.PriorityOutcome, Id = ob.Id, Name = ob.Title, Description = ob.Description,
                    Theme = ob.Theme, Status = ob.Status,
                    MissionId = ob.MissionId, SortOrder = 0, IsActive = !ob.IsDeleted, MissionOptions = missionOpts,
                    OwnerUserId = ob.OwnerUserId, OwnerName = ob.OwnerUser?.Name, OwnerEmail = ob.OwnerUser?.Email,
                    ThemeSroUserId = ob.ThemeSroUserId, ThemeSroName = ob.ThemeSroUser?.Name, ThemeSroEmail = ob.ThemeSroUser?.Email,
                    OutcomeSroUserId = ob.OutcomeSroUserId, OutcomeSroName = ob.OutcomeSroUser?.Name, OutcomeSroEmail = ob.OutcomeSroUser?.Email
                };

            case "departments":
                var parentDepts = await _context.GovernmentDepartments.AsNoTracking()
                    .Where(d => !d.IsDeleted && d.ClosedAt == null)
                    .OrderBy(d => d.Title)
                    .Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.Title })
                    .ToListAsync();
                if (id is null)
                    return new AdminLookupEditorViewModel
                    {
                        Panel = p, EditorKind = AdminLookupEditorKind.Department,
                        SortOrder = 0, IsActive = true, ParentDepartmentOptions = parentDepts
                    };
                var dept = await _context.GovernmentDepartments.AsNoTracking()
                    .Include(d => d.ParentDepartment)
                    .FirstOrDefaultAsync(x => x.Id == id);
                if (dept == null) return null;
                foreach (var o in parentDepts.Where(o => o.Value != dept.Id.ToString()))
                    o.Selected = dept.ParentDepartmentId.HasValue && o.Value == dept.ParentDepartmentId.Value.ToString();
                parentDepts.RemoveAll(o => o.Value == dept.Id.ToString());
                return new AdminLookupEditorViewModel
                {
                    Panel = p, EditorKind = AdminLookupEditorKind.Department,
                    Id = dept.Id, Name = dept.Title, Description = dept.Description,
                    Summary = dept.Abbreviation, Format = dept.Format,
                    WebUrl = dept.WebUrl, GovukStatus = dept.GovukStatus,
                    LastSyncedAt = dept.LastSyncedAt,
                    ParentDepartmentId = dept.ParentDepartmentId,
                    ParentDepartmentOptions = parentDepts,
                    SortOrder = 0, IsActive = dept.ClosedAt == null
                };

            case "portfolios":
                var parents = await _context.OrganizationalGroups.AsNoTracking().Where(g => g.IsActive).OrderBy(g => g.Name)
                    .Select(g => new SelectListItem { Value = g.Id.ToString(), Text = g.Name }).ToListAsync();
                if (id is null)
                    return new AdminLookupEditorViewModel { Panel = p, EditorKind = AdminLookupEditorKind.Portfolio, SortOrder = 0, IsActive = true, ParentGroupOptions = parents };
                var og = await _context.OrganizationalGroups.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
                if (og == null) return null;
                foreach (var o in parents)
                    o.Selected = og.ParentGroupId.HasValue && o.Value == og.ParentGroupId.Value.ToString();
                return new AdminLookupEditorViewModel
                {
                    Panel = p, EditorKind = AdminLookupEditorKind.Portfolio, Id = og.Id, Name = og.Name, Description = og.Description,
                    SortOrder = og.SortOrder, IsActive = og.IsActive, ParentGroupId = og.ParentGroupId, ParentGroupOptions = parents
                };

            case "risk-tiers":
                if (id is null)
                    return new AdminLookupEditorViewModel { Panel = p, EditorKind = AdminLookupEditorKind.RiskTierLike, SortOrder = 0, IsActive = true };
                var rt = await _context.RiskTiers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
                if (rt == null) return null;
                return new AdminLookupEditorViewModel
                {
                    Panel = p, EditorKind = AdminLookupEditorKind.RiskTierLike, Id = rt.Id, Code = rt.Code, Name = rt.Name, Description = rt.Description,
                    DetailSummary = rt.Summary, SortOrder = rt.SortOrder, IsActive = rt.IsActive,
                    RiskTierGovernanceLevel = rt.GovernanceLevel,
                    RiskTierIsProposedTier = rt.IsProposedTier
                };

            case "std-categories":
                if (id is null)
                    return new AdminLookupEditorViewModel { Panel = p, EditorKind = AdminLookupEditorKind.Simple, SortOrder = 0, IsActive = true };
                var sc = await _context.StandardCategories.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
                if (sc == null) return null;
                return new AdminLookupEditorViewModel
                {
                    Panel = p, EditorKind = AdminLookupEditorKind.Simple, Id = sc.Id, Name = sc.Name, Description = sc.Description,
                    SortOrder = sc.SortOrder, IsActive = sc.IsActive
                };

            case "risk-appetites":
                if (id is null)
                    return new AdminLookupEditorViewModel { Panel = p, EditorKind = AdminLookupEditorKind.Simple, SortOrder = 0, IsActive = true };
                var ra = await _context.RiskAppetiteLookups.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
                if (ra == null) return null;
                return new AdminLookupEditorViewModel
                {
                    Panel = p, EditorKind = AdminLookupEditorKind.Simple, Id = ra.Id, Name = ra.Name, Description = ra.Description,
                    SortOrder = ra.SortOrder, IsActive = ra.IsActive
                };

            case "std-subcategories":
                var catOpts = await _context.StandardCategories.AsNoTracking()
                    .Where(c => c.IsActive).OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
                    .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name })
                    .ToListAsync();
                if (id is null)
                    return new AdminLookupEditorViewModel
                    {
                        Panel = p, EditorKind = AdminLookupEditorKind.StandardSubCategory,
                        SortOrder = 0, IsActive = true, CategoryOptions = catOpts
                    };
                var ssc = await _context.StandardSubCategories.AsNoTracking()
                    .Include(x => x.Category)
                    .FirstOrDefaultAsync(x => x.Id == id);
                if (ssc == null) return null;
                foreach (var o in catOpts)
                    o.Selected = o.Value == ssc.CategoryId.ToString();
                return new AdminLookupEditorViewModel
                {
                    Panel = p, EditorKind = AdminLookupEditorKind.StandardSubCategory,
                    Id = ssc.Id, Name = ssc.Name, Description = ssc.Description,
                    CategoryId = ssc.CategoryId, SortOrder = ssc.SortOrder, IsActive = ssc.IsActive,
                    CategoryOptions = catOpts
                };

            case "risk-statuses": case "risk-categories": case "risk-priorities": case "risk-likelihoods": case "risk-impact-levels": case "risk-proximities": case "risk-treatments":
            case "action-statuses": case "action-priorities": case "action-types": case "action-categories":
            case "action-impact-levels": case "action-reminder-frequencies": case "action-escalation-thresholds":
            case "issue-statuses": case "issue-categories": case "issue-priorities": case "issue-severities":
            case "decision-statuses": case "decision-priorities": case "decision-outcomes": case "decision-implementation-statuses":
            case "raid-evidence-types": case "governance-boards": case "demand-request-statuses": case "triage-outcome-stages":
            case "assumption-statuses": case "assumption-criticalities": case "dependency-criticalities": case "dependency-link-types":
            case "near-miss-types": case "near-miss-seriousness": case "near-miss-statuses":
                var raidDef = FindRaidDef(p);
                if (raidDef == null) return null;
                if (id is null)
                    return new AdminLookupEditorViewModel { Panel = p, EditorKind = AdminLookupEditorKind.Raid, SortOrder = 0, IsActive = true };
                var raidEntity = await raidDef.Query(_context).AsNoTracking().FirstOrDefaultAsync(x => x.Id == id.Value);
                if (raidEntity == null) return null;
                int? raidMatrix = raidEntity switch
                {
                    RiskLikelihood rl => rl.MatrixScore,
                    RiskImpactLevel im => im.MatrixScore,
                    _ => null
                };
                return new AdminLookupEditorViewModel
                {
                    Panel = p, EditorKind = AdminLookupEditorKind.Raid, Id = raidEntity.Id, Code = raidEntity.Code,
                    Name = raidEntity.Label, Description = raidEntity.Description,
                    SortOrder = raidEntity.SortOrder, IsActive = raidEntity.IsActive,
                    RaidMatrixScore = raidMatrix
                };

            default:
                return null;
        }
    }

    // ── Activity types ──

    [HttpPost("activity-type/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ActivityTypeCreate(string name, string? description, int sortOrder)
    {
        name = (name ?? "").Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["AdminMessage"] = "Name is required.";
            return RedirectToAction(nameof(LookupCreate), new { panel = "activity-types" });
        }
        var entity = new ActivityTypeLookup { Name = name, Description = description?.Trim(), SortOrder = sortOrder };
        _context.ActivityTypeLookups.Add(entity);
        await _context.SaveChangesAsync();
        TempData["AdminMessage"] = $"Activity type \"{name}\" added.";
        return RedirectToAction(nameof(LookupEdit), new { panel = "activity-types", id = entity.Id });
    }

    [HttpPost("activity-type/{id:int}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ActivityTypeEdit(int id, string name, string? description, int sortOrder)
    {
        var entity = await _context.ActivityTypeLookups.FindAsync(id);
        if (entity == null) return NotFound();
        name = (name ?? "").Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["AdminMessage"] = "Name is required.";
            return RedirectToAction(nameof(LookupEdit), new { panel = "activity-types", id });
        }
        entity.Name = name;
        entity.Description = description?.Trim();
        entity.SortOrder = sortOrder;
        entity.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        TempData["AdminMessage"] = $"Activity type \"{name}\" updated.";
        return RedirectToAction(nameof(Index), new { panel = "activity-types" });
    }

    [HttpPost("activity-type/{id:int}/toggle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ActivityTypeToggle(int id)
    {
        var entity = await _context.ActivityTypeLookups.FindAsync(id);
        if (entity == null) return NotFound();
        entity.IsActive = !entity.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        TempData["AdminMessage"] = $"\"{entity.Name}\" is now {(entity.IsActive ? "active" : "inactive")}.";
        return RedirectToAction(nameof(LookupEdit), new { panel = "activity-types", id });
    }

    // ── Risk appetites ──

    [HttpPost("risk-appetite/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RiskAppetiteCreate(string name, string? description, int sortOrder)
    {
        name = (name ?? "").Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["AdminMessage"] = "Name is required.";
            return RedirectToAction(nameof(LookupCreate), new { panel = "risk-appetites" });
        }
        var entity = new RiskAppetiteLookup { Name = name, Description = description?.Trim(), SortOrder = sortOrder };
        _context.RiskAppetiteLookups.Add(entity);
        await _context.SaveChangesAsync();
        TempData["AdminMessage"] = $"Risk appetite \"{name}\" added.";
        return RedirectToAction(nameof(LookupEdit), new { panel = "risk-appetites", id = entity.Id });
    }

    [HttpPost("risk-appetite/{id:int}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RiskAppetiteEdit(int id, string name, string? description, int sortOrder)
    {
        var entity = await _context.RiskAppetiteLookups.FindAsync(id);
        if (entity == null) return NotFound();
        name = (name ?? "").Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["AdminMessage"] = "Name is required.";
            return RedirectToAction(nameof(LookupEdit), new { panel = "risk-appetites", id });
        }
        entity.Name = name;
        entity.Description = description?.Trim();
        entity.SortOrder = sortOrder;
        entity.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        TempData["AdminMessage"] = $"Risk appetite \"{name}\" updated.";
        return RedirectToAction(nameof(Index), new { panel = "risk-appetites" });
    }

    [HttpPost("risk-appetite/{id:int}/toggle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RiskAppetiteToggle(int id)
    {
        var entity = await _context.RiskAppetiteLookups.FindAsync(id);
        if (entity == null) return NotFound();
        entity.IsActive = !entity.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        TempData["AdminMessage"] = $"\"{entity.Name}\" is now {(entity.IsActive ? "active" : "inactive")}.";
        return RedirectToAction(nameof(LookupEdit), new { panel = "risk-appetites", id });
    }

    // ── Work tags (custom tagging for work items) ──

    [HttpPost("work-item-tag/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> WorkItemTagCreate(string name, string? description, int sortOrder)
    {
        name = (name ?? "").Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["AdminMessage"] = "Name is required.";
            return RedirectToAction(nameof(LookupCreate), new { panel = "work-tagging" });
        }
        var entity = new WorkItemTagLookup { Name = name, Description = description?.Trim(), SortOrder = sortOrder };
        _context.WorkItemTagLookups.Add(entity);
        await _context.SaveChangesAsync();
        TempData["AdminMessage"] = $"Work tag \"{name}\" added.";
        return RedirectToAction(nameof(LookupEdit), new { panel = "work-tagging", id = entity.Id });
    }

    [HttpPost("work-item-tag/{id:int}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> WorkItemTagEdit(int id, string name, string? description, int sortOrder)
    {
        var entity = await _context.WorkItemTagLookups.FindAsync(id);
        if (entity == null) return NotFound();
        name = (name ?? "").Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["AdminMessage"] = "Name is required.";
            return RedirectToAction(nameof(LookupEdit), new { panel = "work-tagging", id });
        }
        entity.Name = name;
        entity.Description = description?.Trim();
        entity.SortOrder = sortOrder;
        entity.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        TempData["AdminMessage"] = $"Work tag \"{name}\" updated.";
        return RedirectToAction(nameof(Index), new { panel = "work-tagging" });
    }

    [HttpPost("work-item-tag/{id:int}/toggle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> WorkItemTagToggle(int id)
    {
        var entity = await _context.WorkItemTagLookups.FindAsync(id);
        if (entity == null) return NotFound();
        entity.IsActive = !entity.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        TempData["AdminMessage"] = $"\"{entity.Name}\" is now {(entity.IsActive ? "active" : "inactive")}.";
        return RedirectToAction(nameof(LookupEdit), new { panel = "work-tagging", id });
    }

    // ── Mission pillars ──

    [HttpPost("mission-pillar/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MissionPillarCreate(string name, string? description)
    {
        name = (name ?? "").Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["AdminMessage"] = "Title is required.";
            return RedirectToAction(nameof(LookupCreate), new { panel = "mission-pillars" });
        }
        var entity = new Mission
        {
            Title = name,
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            Status = "Active",
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Missions.Add(entity);
        await _context.SaveChangesAsync();
        TempData["AdminMessage"] = $"Mission pillar \"{name}\" added.";
        return RedirectToAction(nameof(LookupEdit), new { panel = "mission-pillars", id = entity.Id });
    }

    [HttpPost("mission-pillar/{id:int}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MissionPillarEdit(int id, string name, string? description)
    {
        var entity = await _context.Missions.FindAsync(id);
        if (entity == null) return NotFound();
        name = (name ?? "").Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["AdminMessage"] = "Title is required.";
            return RedirectToAction(nameof(LookupEdit), new { panel = "mission-pillars", id });
        }
        entity.Title = name;
        entity.Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        entity.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        TempData["AdminMessage"] = "Mission pillar updated.";
        return RedirectToAction(nameof(Index), new { panel = "mission-pillars" });
    }

    [HttpPost("mission-pillar/{id:int}/toggle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MissionPillarToggle(int id)
    {
        var entity = await _context.Missions.FindAsync(id);
        if (entity == null) return NotFound();
        entity.IsDeleted = !entity.IsDeleted;
        entity.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        TempData["AdminMessage"] = entity.IsDeleted ? "Mission pillar deactivated." : "Mission pillar activated.";
        return RedirectToAction(nameof(LookupEdit), new { panel = "mission-pillars", id });
    }

    // ── Priority outcomes (objectives) ──

    [HttpPost("priority-outcome/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PriorityOutcomeCreate(string name, string? theme, string? description, int? missionId,
        string? status, int? ownerUserId, int? themeSroUserId, int? outcomeSroUserId)
    {
        name = (name ?? "").Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["AdminMessage"] = "Title is required.";
            return RedirectToAction(nameof(LookupCreate), new { panel = "priority-outcomes" });
        }
        var entity = new Objective
        {
            Title = name,
            Theme = string.IsNullOrWhiteSpace(theme) ? null : theme.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            MissionId = missionId,
            Status = string.IsNullOrWhiteSpace(status) ? "active" : status.Trim(),
            OwnerUserId = ownerUserId,
            ThemeSroUserId = themeSroUserId,
            OutcomeSroUserId = outcomeSroUserId,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Objectives.Add(entity);
        await _context.SaveChangesAsync();
        TempData["AdminMessage"] = $"Priority outcome \"{name}\" added.";
        return RedirectToAction(nameof(LookupEdit), new { panel = "priority-outcomes", id = entity.Id });
    }

    [HttpPost("priority-outcome/{id:int}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PriorityOutcomeEdit(int id, string name, string? theme, string? description, int? missionId,
        string? status, int? ownerUserId, int? themeSroUserId, int? outcomeSroUserId)
    {
        var entity = await _context.Objectives.FindAsync(id);
        if (entity == null) return NotFound();
        name = (name ?? "").Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["AdminMessage"] = "Title is required.";
            return RedirectToAction(nameof(LookupEdit), new { panel = "priority-outcomes", id });
        }
        entity.Title = name;
        entity.Theme = string.IsNullOrWhiteSpace(theme) ? null : theme.Trim();
        entity.Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        entity.MissionId = missionId;
        entity.Status = string.IsNullOrWhiteSpace(status) ? null : status.Trim();
        entity.OwnerUserId = ownerUserId;
        entity.ThemeSroUserId = themeSroUserId;
        entity.OutcomeSroUserId = outcomeSroUserId;
        entity.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        TempData["AdminMessage"] = "Priority outcome updated.";
        return RedirectToAction(nameof(Index), new { panel = "priority-outcomes" });
    }

    [HttpPost("priority-outcome/{id:int}/toggle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PriorityOutcomeToggle(int id)
    {
        var entity = await _context.Objectives.FindAsync(id);
        if (entity == null) return NotFound();
        entity.IsDeleted = !entity.IsDeleted;
        entity.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        TempData["AdminMessage"] = entity.IsDeleted ? "Priority outcome deactivated." : "Priority outcome activated.";
        return RedirectToAction(nameof(LookupEdit), new { panel = "priority-outcomes", id });
    }

    // ── Portfolios (organizational groups) ──

    [HttpPost("portfolio/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PortfolioCreate(string name, string? description, int sortOrder, int? parentGroupId)
    {
        name = (name ?? "").Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["AdminMessage"] = "Name is required.";
            return RedirectToAction(nameof(LookupCreate), new { panel = "portfolios" });
        }
        var entity = new OrganizationalGroup
        {
            Name = name,
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            SortOrder = sortOrder,
            ParentGroupId = parentGroupId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.OrganizationalGroups.Add(entity);
        await _context.SaveChangesAsync();
        TempData["AdminMessage"] = $"Portfolio \"{name}\" added.";
        return RedirectToAction(nameof(LookupEdit), new { panel = "portfolios", id = entity.Id });
    }

    [HttpPost("portfolio/{id:int}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PortfolioEdit(int id, string name, string? description, int sortOrder, int? parentGroupId)
    {
        var entity = await _context.OrganizationalGroups.FindAsync(id);
        if (entity == null) return NotFound();
        name = (name ?? "").Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["AdminMessage"] = "Name is required.";
            return RedirectToAction(nameof(LookupEdit), new { panel = "portfolios", id });
        }
        entity.Name = name;
        entity.Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        entity.SortOrder = sortOrder;
        entity.ParentGroupId = parentGroupId;
        entity.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        TempData["AdminMessage"] = "Portfolio updated.";
        return RedirectToAction(nameof(Index), new { panel = "portfolios" });
    }

    [HttpPost("portfolio/{id:int}/toggle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PortfolioToggle(int id)
    {
        var entity = await _context.OrganizationalGroups.FindAsync(id);
        if (entity == null) return NotFound();
        entity.IsActive = !entity.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        TempData["AdminMessage"] = $"\"{entity.Name}\" is now {(entity.IsActive ? "active" : "inactive")}.";
        return RedirectToAction(nameof(LookupEdit), new { panel = "portfolios", id });
    }

    // ── Government departments ──

    [HttpPost("department/{id:int}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DepartmentEdit(int id, string name, string? summary, string? description, int? parentDepartmentId)
    {
        var entity = await _context.GovernmentDepartments.FindAsync(id);
        if (entity == null) return NotFound();
        name = (name ?? "").Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["AdminMessage"] = "Title is required.";
            return RedirectToAction(nameof(LookupEdit), new { panel = "departments", id });
        }
        entity.Title = name;
        entity.Abbreviation = string.IsNullOrWhiteSpace(summary) ? null : summary.Trim();
        entity.Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        entity.ParentDepartmentId = parentDepartmentId;
        entity.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        TempData["AdminMessage"] = $"Department \"{name}\" updated.";
        return RedirectToAction(nameof(Index), new { panel = "departments" });
    }

    [HttpPost("department/{id:int}/toggle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DepartmentToggle(int id)
    {
        var entity = await _context.GovernmentDepartments.FindAsync(id);
        if (entity == null) return NotFound();
        entity.IsDeleted = !entity.IsDeleted;
        entity.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        TempData["AdminMessage"] = entity.IsDeleted ? "Department deactivated." : "Department activated.";
        return RedirectToAction(nameof(LookupEdit), new { panel = "departments", id });
    }

    // ── Risk tiers ──

    [HttpPost("risk-tier/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RiskTierCreate(string code, string name, string? description, string? summary, int sortOrder, int governanceLevel, bool isProposedTier)
    {
        code = (code ?? "").Trim();
        name = (name ?? "").Trim();
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
        {
            TempData["AdminMessage"] = "Code and name are required.";
            return RedirectToAction(nameof(LookupCreate), new { panel = "risk-tiers" });
        }
        var entity = new RiskTier
        {
            Code = code,
            Name = name,
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            Summary = string.IsNullOrWhiteSpace(summary) ? null : summary.Trim(),
            SortOrder = sortOrder,
            GovernanceLevel = Math.Clamp(governanceLevel, 0, 99),
            IsProposedTier = isProposedTier,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.RiskTiers.Add(entity);
        await _context.SaveChangesAsync();
        TempData["AdminMessage"] = "Risk tier added.";
        return RedirectToAction(nameof(LookupEdit), new { panel = "risk-tiers", id = entity.Id });
    }

    [HttpPost("risk-tier/{id:int}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RiskTierEdit(int id, string code, string name, string? description, string? summary, int sortOrder, int governanceLevel, bool isProposedTier)
    {
        var entity = await _context.RiskTiers.FindAsync(id);
        if (entity == null) return NotFound();
        code = (code ?? "").Trim();
        name = (name ?? "").Trim();
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
        {
            TempData["AdminMessage"] = "Code and name are required.";
            return RedirectToAction(nameof(LookupEdit), new { panel = "risk-tiers", id });
        }
        entity.Code = code;
        entity.Name = name;
        entity.Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        entity.Summary = string.IsNullOrWhiteSpace(summary) ? null : summary.Trim();
        entity.SortOrder = sortOrder;
        entity.GovernanceLevel = Math.Clamp(governanceLevel, 0, 99);
        entity.IsProposedTier = isProposedTier;
        entity.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        TempData["AdminMessage"] = "Risk tier updated.";
        return RedirectToAction(nameof(Index), new { panel = "risk-tiers" });
    }

    [HttpPost("risk-tier/{id:int}/toggle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RiskTierToggle(int id)
    {
        var entity = await _context.RiskTiers.FindAsync(id);
        if (entity == null) return NotFound();
        entity.IsActive = !entity.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        TempData["AdminMessage"] = $"Risk tier is now {(entity.IsActive ? "active" : "inactive")}.";
        return RedirectToAction(nameof(LookupEdit), new { panel = "risk-tiers", id });
    }

}
