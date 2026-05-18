using Compass.Models;
using Compass.Models.Modern.Work;
using Compass.ViewModels.Modern;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Compass.Controllers.Modern;

public partial class ModernRaidController
{
    private static readonly string[] NearMissClosedStatusCodes = { "CLOSED" };

    [HttpGet("near-misses")]
    public async Task<IActionResult> NearMisses(
        string? search,
        int? directorateLookupId,
        int? businessAreaLookupId,
        int? riskTierId,
        int? nearMissStatusId,
        string? tab,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        SetRaidChrome("raid-near-misses");
        page = page < 1 ? 1 : page;

        var vm = await BuildNearMissesPageViewModelAsync(
            search,
            directorateLookupId,
            businessAreaLookupId,
            riskTierId,
            nearMissStatusId,
            tab,
            page,
            includeBreakdowns: false,
            cancellationToken);
        return View("~/Views/Modern/Raid/NearMisses.cshtml", vm);
    }

    [HttpGet("near-misses/create")]
    public async Task<IActionResult> NearMissCreate(CancellationToken cancellationToken = default)
    {
        SetRaidChrome("raid-near-misses");
        await PrepareNearMissFormLookupsAsync(cancellationToken);
        RaidDateFormHelper.SplitDateParts(DateTime.UtcNow.Date, out var d, out var m, out var y);
        var form = new ModernRaidNearMissForm
        {
            LoggedDay = d,
            LoggedMonth = m,
            LoggedYear = y
        };
        return View("~/Views/Modern/Raid/NearMissCreate.cshtml", form);
    }

    [HttpPost("near-misses/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> NearMissCreatePost(
        [FromForm] ModernRaidNearMissForm form,
        CancellationToken cancellationToken = default)
    {
        SetRaidChrome("raid-near-misses");
        await PrepareNearMissFormLookupsAsync(cancellationToken, form.OwnerUserIds);

        if (!await ValidateNearMissFormAsync(form, null, cancellationToken))
            return View("~/Views/Modern/Raid/NearMissCreate.cshtml", form);

        var userId = await ResolveCurrentUserIdAsync(cancellationToken);
        var now = DateTime.UtcNow;
        if (!RaidDateFormHelper.TryRequiredDate(
                form.LoggedDay, form.LoggedMonth, form.LoggedYear, "DateIncidentLogged", ModelState, out var logged))
            return View("~/Views/Modern/Raid/NearMissCreate.cshtml", form);

        var entity = new NearMiss
        {
            Reference = form.Reference!.Trim(),
            DateLogged = logged!.Value,
            NearMissTypeId = form.NearMissTypeId,
            DirectorateLookupId = form.DirectorateLookupId,
            BusinessAreaLookupId = form.BusinessAreaLookupId,
            Impact = string.IsNullOrWhiteSpace(form.Impact) ? null : form.Impact.Trim(),
            NearMissSeriousnessId = form.NearMissSeriousnessId,
            PostMitigationRagStatusLookupId = form.PostMitigationRagStatusLookupId,
            RiskTierId = form.RiskTierId,
            NearMissStatusId = form.NearMissStatusId,
            IsDeleted = false,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedByUserId = userId,
            UpdatedByUserId = userId
        };

        _db.NearMisses.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        await PersistNearMissOwnersAsync(entity.Id, form.OwnerUserIds, cancellationToken);
        await PersistNearMissActionsAsync(entity.Id, form.Actions, userId, cancellationToken);
        await PersistNearMissMitigationsAsync(entity.Id, form.Mitigations, userId, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        TempData["Message"] = "Near miss added to the register.";
        return RedirectToAction(nameof(NearMissDetail), new { id = entity.Id });
    }

    [HttpGet("near-misses/{id:int}")]
    public async Task<IActionResult> NearMissDetail(int id, CancellationToken cancellationToken = default)
    {
        SetRaidChrome("raid-near-misses");
        var nm = await LoadNearMissDetailQuery().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
        if (nm == null)
            return NotFound();

        ViewBag.NearMissAuditLogs = await _db.AuditLogs.AsNoTracking()
            .Where(a => a.Entity == nameof(NearMiss) && a.EntityId == id.ToString())
            .OrderByDescending(a => a.ChangedUtc)
            .Take(100)
            .ToListAsync(cancellationToken);

        return View("~/Views/Modern/Raid/NearMissDetail.cshtml", nm);
    }

    [HttpGet("near-misses/{id:int}/edit")]
    public async Task<IActionResult> NearMissEdit(int id, CancellationToken cancellationToken = default)
    {
        SetRaidChrome("raid-near-misses");
        var nm = await LoadNearMissDetailQuery().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
        if (nm == null)
            return NotFound();

        await PrepareNearMissFormLookupsAsync(
            cancellationToken,
            nm.NearMissOwners.Select(o => o.UserId).ToList());

        var form = MapNearMissToForm(nm);
        return View("~/Views/Modern/Raid/NearMissEdit.cshtml", form);
    }

    [HttpPost("near-misses/{id:int}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> NearMissEditPost(
        int id,
        [FromForm] ModernRaidNearMissForm form,
        CancellationToken cancellationToken = default)
    {
        SetRaidChrome("raid-near-misses");
        form.Id = id;
        await PrepareNearMissFormLookupsAsync(cancellationToken, form.OwnerUserIds);

        var entity = await _db.NearMisses
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
        if (entity == null)
            return NotFound();

        if (!await ValidateNearMissFormAsync(form, id, cancellationToken))
            return View("~/Views/Modern/Raid/NearMissEdit.cshtml", form);

        var userId = await ResolveCurrentUserIdAsync(cancellationToken);
        if (!RaidDateFormHelper.TryRequiredDate(
                form.LoggedDay, form.LoggedMonth, form.LoggedYear, "DateIncidentLogged", ModelState, out var logged))
            return View("~/Views/Modern/Raid/NearMissEdit.cshtml", form);

        entity.Reference = form.Reference!.Trim();
        entity.DateLogged = logged!.Value;
        entity.NearMissTypeId = form.NearMissTypeId;
        entity.DirectorateLookupId = form.DirectorateLookupId;
        entity.BusinessAreaLookupId = form.BusinessAreaLookupId;
        entity.Impact = string.IsNullOrWhiteSpace(form.Impact) ? null : form.Impact.Trim();
        entity.NearMissSeriousnessId = form.NearMissSeriousnessId;
        entity.PostMitigationRagStatusLookupId = form.PostMitigationRagStatusLookupId;
        entity.RiskTierId = form.RiskTierId;
        entity.NearMissStatusId = form.NearMissStatusId;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedByUserId = userId;

        await PersistNearMissOwnersAsync(entity.Id, form.OwnerUserIds, cancellationToken);
        await PersistNearMissActionsAsync(entity.Id, form.Actions, userId, cancellationToken);
        await PersistNearMissMitigationsAsync(entity.Id, form.Mitigations, userId, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        TempData["Message"] = "Near miss updated.";
        return RedirectToAction(nameof(NearMissDetail), new { id });
    }

    private async Task<ModernRaidNearMissesPageViewModel> BuildNearMissesPageViewModelAsync(
        string? search,
        int? directorateLookupId,
        int? businessAreaLookupId,
        int? riskTierId,
        int? nearMissStatusId,
        string? tab,
        int page,
        bool includeBreakdowns,
        CancellationToken cancellationToken)
    {
        IQueryable<NearMiss> q = _db.NearMisses.AsNoTracking().Where(n => !n.IsDeleted);

        if (directorateLookupId is > 0)
            q = q.Where(n => n.DirectorateLookupId == directorateLookupId);
        if (businessAreaLookupId is > 0)
            q = q.Where(n => n.BusinessAreaLookupId == businessAreaLookupId);
        if (riskTierId is > 0)
            q = q.Where(n => n.RiskTierId == riskTierId);
        if (nearMissStatusId is > 0)
            q = q.Where(n => n.NearMissStatusId == nearMissStatusId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var t = search.Trim();
            q = q.Where(n =>
                n.Reference.Contains(t) ||
                (n.Impact != null && n.Impact.Contains(t)));
        }

        var closedStatusIds = await GetNearMissClosedStatusIdsAsync(cancellationToken);
        var activeTab = (tab ?? "").Trim().ToLowerInvariant() switch
        {
            "closed" => "closed",
            "all" => "all",
            _ => "open"
        };

        var allCount = await q.CountAsync(cancellationToken);
        var closedCount = closedStatusIds.Count == 0
            ? 0
            : await q.Where(n => n.NearMissStatusId.HasValue && closedStatusIds.Contains(n.NearMissStatusId.Value))
                .CountAsync(cancellationToken);
        var openCount = allCount - closedCount;

        q = activeTab switch
        {
            "closed" => closedStatusIds.Count == 0
                ? q.Where(_ => false)
                : q.Where(n => n.NearMissStatusId.HasValue && closedStatusIds.Contains(n.NearMissStatusId.Value)),
            "open" => closedStatusIds.Count == 0
                ? q
                : q.Where(n => !n.NearMissStatusId.HasValue || !closedStatusIds.Contains(n.NearMissStatusId.Value)),
            _ => q
        };

        var total = await q.CountAsync(cancellationToken);
        var list = await q
            .OrderByDescending(n => n.DateLogged)
            .ThenByDescending(n => n.UpdatedAt)
            .Skip((page - 1) * RaidDefaultPageSize)
            .Take(RaidDefaultPageSize)
            .ToListAsync(cancellationToken);

        var rows = await MapNearMissRowsAsync(list, cancellationToken);

        IReadOnlyList<ModernRaidLabelCountVm> byDir = Array.Empty<ModernRaidLabelCountVm>();
        IReadOnlyList<ModernRaidLabelCountVm> byBa = Array.Empty<ModernRaidLabelCountVm>();
        IReadOnlyList<ModernRaidLabelCountVm> byTier = Array.Empty<ModernRaidLabelCountVm>();

        if (includeBreakdowns)
        {
            var openQ = _db.NearMisses.AsNoTracking().Where(n => !n.IsDeleted);
            if (closedStatusIds.Count > 0)
                openQ = openQ.Where(n =>
                    !n.NearMissStatusId.HasValue || !closedStatusIds.Contains(n.NearMissStatusId.Value));

            byDir = await BuildNearMissBreakdownByDirectorateAsync(openQ, cancellationToken);
            byBa = await BuildNearMissBreakdownByBusinessAreaAsync(openQ, cancellationToken);
            byTier = await BuildNearMissBreakdownByTierAsync(openQ, cancellationToken);
        }

        var dirOpts = await _db.DirectorateLookups.AsNoTracking()
            .Where(x => x.IsActive).OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
            .Select(x => new RaidLookupOptionVm(x.Id, x.Name))
            .ToListAsync(cancellationToken);
        var baOpts = await _db.BusinessAreaLookups.AsNoTracking()
            .Where(x => x.IsActive).OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
            .Select(x => new RaidLookupOptionVm(x.Id, x.Name))
            .ToListAsync(cancellationToken);
        var tierOpts = await _db.RiskTiers.AsNoTracking()
            .Where(x => x.IsActive && !x.IsProposedTier)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
            .Select(x => new RaidLookupOptionVm(x.Id, x.Name))
            .ToListAsync(cancellationToken);
        var stOpts = await _db.NearMissStatuses.AsNoTracking()
            .Where(x => x.IsActive).OrderBy(x => x.SortOrder)
            .Select(x => new RaidLookupOptionVm(x.Id, x.Label))
            .ToListAsync(cancellationToken);

        return new ModernRaidNearMissesPageViewModel
        {
            Rows = rows,
            TotalFiltered = total,
            Page = page,
            PageSize = RaidDefaultPageSize,
            Search = search,
            DirectorateLookupId = directorateLookupId,
            BusinessAreaLookupId = businessAreaLookupId,
            RiskTierId = riskTierId,
            NearMissStatusId = nearMissStatusId,
            ActiveTab = activeTab,
            OpenCount = openCount,
            ClosedCount = closedCount,
            AllCount = allCount,
            ByDirectorate = byDir,
            ByBusinessArea = byBa,
            ByTier = byTier,
            DirectorateOptions = dirOpts,
            BusinessAreaOptions = baOpts,
            TierOptions = tierOpts,
            StatusOptions = stOpts
        };
    }

    internal async Task<int> GetOpenNearMissCountAsync(CancellationToken cancellationToken)
    {
        var closedStatusIds = await GetNearMissClosedStatusIdsAsync(cancellationToken);
        var openQ = _db.NearMisses.AsNoTracking().Where(n => !n.IsDeleted);
        if (closedStatusIds.Count > 0)
            openQ = openQ.Where(n =>
                !n.NearMissStatusId.HasValue || !closedStatusIds.Contains(n.NearMissStatusId.Value));

        return await openQ.CountAsync(cancellationToken);
    }

    private async Task<List<ModernRaidLabelCountVm>> BuildNearMissBreakdownByDirectorateAsync(
        IQueryable<NearMiss> openQ,
        CancellationToken cancellationToken,
        int take = 15)
    {
        var grouped = await openQ
            .GroupBy(n => n.DirectorateLookupId)
            .Select(g => new { DirectorateLookupId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(take)
            .ToListAsync(cancellationToken);
        var ids = grouped.Where(x => x.DirectorateLookupId.HasValue).Select(x => x.DirectorateLookupId!.Value).ToList();
        var names = ids.Count == 0
            ? new Dictionary<int, string>()
            : await _db.DirectorateLookups.AsNoTracking()
                .Where(d => ids.Contains(d.Id))
                .ToDictionaryAsync(d => d.Id, d => d.Name, cancellationToken);
        return grouped.Select(g => new ModernRaidLabelCountVm(
            g.DirectorateLookupId is int id && names.TryGetValue(id, out var n) ? n : "Unassigned",
            g.Count)).ToList();
    }

    private async Task<List<ModernRaidLabelCountVm>> BuildNearMissBreakdownByBusinessAreaAsync(
        IQueryable<NearMiss> openQ,
        CancellationToken cancellationToken,
        int take = 15)
    {
        var grouped = await openQ
            .GroupBy(n => n.BusinessAreaLookupId)
            .Select(g => new { BusinessAreaLookupId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(take)
            .ToListAsync(cancellationToken);
        var ids = grouped.Where(x => x.BusinessAreaLookupId.HasValue).Select(x => x.BusinessAreaLookupId!.Value).ToList();
        var names = ids.Count == 0
            ? new Dictionary<int, string>()
            : await _db.BusinessAreaLookups.AsNoTracking()
                .Where(b => ids.Contains(b.Id))
                .ToDictionaryAsync(b => b.Id, b => b.Name, cancellationToken);
        return grouped.Select(g => new ModernRaidLabelCountVm(
            g.BusinessAreaLookupId is int id && names.TryGetValue(id, out var n) ? n : "Unassigned",
            g.Count)).ToList();
    }

    private async Task<List<ModernRaidLabelCountVm>> BuildNearMissBreakdownByTierAsync(
        IQueryable<NearMiss> openQ,
        CancellationToken cancellationToken,
        int take = 15)
    {
        var grouped = await openQ
            .GroupBy(n => n.RiskTierId)
            .Select(g => new { RiskTierId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(take)
            .ToListAsync(cancellationToken);
        var ids = grouped.Where(x => x.RiskTierId.HasValue).Select(x => x.RiskTierId!.Value).ToList();
        var names = ids.Count == 0
            ? new Dictionary<int, string>()
            : await _db.RiskTiers.AsNoTracking()
                .Where(t => ids.Contains(t.Id))
                .ToDictionaryAsync(t => t.Id, t => t.Name, cancellationToken);
        return grouped.Select(g => new ModernRaidLabelCountVm(
            g.RiskTierId is int id && names.TryGetValue(id, out var n) ? n : "Unassigned",
            g.Count)).ToList();
    }

    private IQueryable<NearMiss> LoadNearMissDetailQuery() =>
        _db.NearMisses.AsNoTracking()
            .Include(n => n.TypeLookup)
            .Include(n => n.DirectorateLookup)
            .Include(n => n.BusinessAreaLookup)
            .Include(n => n.SeriousnessLookup)
            .Include(n => n.PostMitigationRagStatusLookup)
            .Include(n => n.RiskTier)
            .Include(n => n.StatusLookup)
            .Include(n => n.CreatedByUser)
            .Include(n => n.UpdatedByUser)
            .Include(n => n.NearMissOwners).ThenInclude(o => o.User)
            .Include(n => n.NearMissActions).ThenInclude(a => a.RecordedByUser)
            .Include(n => n.NearMissMitigations).ThenInclude(m => m.RecordedByUser);

    private async Task<List<int>> GetNearMissClosedStatusIdsAsync(CancellationToken cancellationToken) =>
        await _db.NearMissStatuses.AsNoTracking()
            .Where(x => x.IsActive && NearMissClosedStatusCodes.Contains(x.Code))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

    private async Task<List<ModernRaidNearMissRow>> MapNearMissRowsAsync(
        List<NearMiss> list,
        CancellationToken cancellationToken)
    {
        if (list.Count == 0)
            return new List<ModernRaidNearMissRow>();

        var ids = list.Select(n => n.Id).ToList();
        var owners = await _db.NearMissOwners.AsNoTracking()
            .Where(o => ids.Contains(o.NearMissId))
            .Include(o => o.User)
            .ToListAsync(cancellationToken);
        var ownersByNm = owners.GroupBy(o => o.NearMissId)
            .ToDictionary(g => g.Key, g => g.Select(o => FormatNearMissUser(o.User)).Where(s => s != null).ToList());

        var typeIds = list.Where(n => n.NearMissTypeId.HasValue).Select(n => n.NearMissTypeId!.Value).Distinct().ToList();
        var dirIds = list.Where(n => n.DirectorateLookupId.HasValue).Select(n => n.DirectorateLookupId!.Value).Distinct().ToList();
        var baIds = list.Where(n => n.BusinessAreaLookupId.HasValue).Select(n => n.BusinessAreaLookupId!.Value).Distinct().ToList();
        var sevIds = list.Where(n => n.NearMissSeriousnessId.HasValue).Select(n => n.NearMissSeriousnessId!.Value).Distinct().ToList();
        var tierIds = list.Where(n => n.RiskTierId.HasValue).Select(n => n.RiskTierId!.Value).Distinct().ToList();
        var stIds = list.Where(n => n.NearMissStatusId.HasValue).Select(n => n.NearMissStatusId!.Value).Distinct().ToList();
        var ragIds = list.Where(n => n.PostMitigationRagStatusLookupId.HasValue)
            .Select(n => n.PostMitigationRagStatusLookupId!.Value).Distinct().ToList();

        var types = typeIds.Count == 0
            ? new Dictionary<int, string>()
            : await _db.NearMissTypes.AsNoTracking().Where(x => typeIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.Label, cancellationToken);
        var dirs = dirIds.Count == 0
            ? new Dictionary<int, string>()
            : await _db.DirectorateLookups.AsNoTracking().Where(x => dirIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);
        var bas = baIds.Count == 0
            ? new Dictionary<int, string>()
            : await _db.BusinessAreaLookups.AsNoTracking().Where(x => baIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);
        var sevs = sevIds.Count == 0
            ? new Dictionary<int, string>()
            : await _db.NearMissSeriousnesses.AsNoTracking().Where(x => sevIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.Label, cancellationToken);
        var tiers = tierIds.Count == 0
            ? new Dictionary<int, string>()
            : await _db.RiskTiers.AsNoTracking().Where(x => tierIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);
        var sts = stIds.Count == 0
            ? new Dictionary<int, string>()
            : await _db.NearMissStatuses.AsNoTracking().Where(x => stIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.Label, cancellationToken);
        var rags = ragIds.Count == 0
            ? new Dictionary<int, (string Name, string? CssClass)>()
            : await _db.RagStatusLookups.AsNoTracking().Where(x => ragIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => (Name: x.Name, CssClass: x.CssClass), cancellationToken);

        return list.Select(n =>
        {
            ownersByNm.TryGetValue(n.Id, out var ownerNames);
            var ownersSummary = ownerNames is { Count: > 0 } ? string.Join(", ", ownerNames) : null;
            string? ragName = null;
            string? ragCss = null;
            if (n.PostMitigationRagStatusLookupId is int ragId && rags.TryGetValue(ragId, out var rag))
            {
                ragName = rag.Name;
                ragCss = rag.CssClass != null ? $"dfe-f-badge dfe-f-badge--small dfe-c-tag--{rag.CssClass}" : null;
            }

            return new ModernRaidNearMissRow(
                n.Id,
                n.Reference,
                n.DateLogged,
                n.NearMissTypeId is int tid && types.TryGetValue(tid, out var tl) ? tl : null,
                n.DirectorateLookupId is int did && dirs.TryGetValue(did, out var dl) ? dl : null,
                n.BusinessAreaLookupId is int bid && bas.TryGetValue(bid, out var bl) ? bl : null,
                ownersSummary,
                n.NearMissSeriousnessId is int sid && sevs.TryGetValue(sid, out var sl) ? sl : null,
                n.RiskTierId is int trid && tiers.TryGetValue(trid, out var tn) ? tn : null,
                n.NearMissStatusId is int stid && sts.TryGetValue(stid, out var stl) ? stl : null,
                ragName,
                ragCss,
                n.UpdatedAt);
        }).ToList();
    }

    private static string? FormatNearMissUser(User? user)
    {
        if (user == null)
            return null;
        if (!string.IsNullOrWhiteSpace(user.Name))
            return user.Name.Trim();
        var fl = $"{user.FirstName} {user.LastName}".Trim();
        return string.IsNullOrWhiteSpace(fl) ? user.Email : fl;
    }

    private async Task PrepareNearMissFormLookupsAsync(
        CancellationToken cancellationToken,
        List<int>? ownerUserIds = null)
    {
        ViewBag.NearMissTypeOptions = await _db.NearMissTypes.AsNoTracking()
            .Where(x => x.IsActive).OrderBy(x => x.SortOrder).ToListAsync(cancellationToken);
        ViewBag.NearMissSeriousnessOptions = await _db.NearMissSeriousnesses.AsNoTracking()
            .Where(x => x.IsActive).OrderBy(x => x.SortOrder).ToListAsync(cancellationToken);
        ViewBag.NearMissStatusOptions = await _db.NearMissStatuses.AsNoTracking()
            .Where(x => x.IsActive).OrderBy(x => x.SortOrder).ToListAsync(cancellationToken);
        ViewBag.DirectorateOptions = await _db.DirectorateLookups.AsNoTracking()
            .Where(x => x.IsActive).OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
            .Select(x => new RiskIssueNamedIntOption { Id = x.Id, Name = x.Name })
            .ToListAsync(cancellationToken);
        ViewBag.BusinessAreaOptions = await _db.BusinessAreaLookups.AsNoTracking()
            .Where(x => x.IsActive).OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
            .Select(x => new RiskIssueNamedIntOption { Id = x.Id, Name = x.Name })
            .ToListAsync(cancellationToken);
        ViewBag.RiskTierOptions = await _db.RiskTiers.AsNoTracking()
            .Where(x => x.IsActive && !x.IsProposedTier)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);
        ViewBag.RagStatusOptions = await _db.RagStatusLookups.AsNoTracking()
            .Where(x => x.IsActive).OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        ownerUserIds ??= new List<int>();
        var users = ownerUserIds.Count == 0
            ? new List<User>()
            : await _db.Users.AsNoTracking().Where(u => ownerUserIds.Contains(u.Id)).ToListAsync(cancellationToken);
        ViewBag.NearMissOwnerUsers = users;

        var currentUserId = await ResolveCurrentUserIdAsync(cancellationToken);
        ViewBag.CurrentUserId = currentUserId;
        if (currentUserId is int uid)
        {
            var cu = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == uid, cancellationToken);
            ViewBag.CurrentUserDisplayName = cu != null ? FormatNearMissUser(cu) : "";
        }
        else
            ViewBag.CurrentUserDisplayName = "";
    }

    private async Task<bool> ValidateNearMissFormAsync(
        ModernRaidNearMissForm form,
        int? excludeNearMissId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(form.Reference))
            ModelState.AddModelError(nameof(form.Reference), "Enter the near miss or unexpected issue ID.");
        else
        {
            var reference = form.Reference.Trim();
            var duplicate = await _db.NearMisses.AsNoTracking()
                .AnyAsync(n => !n.IsDeleted && n.Reference == reference && n.Id != excludeNearMissId, cancellationToken);
            if (duplicate)
                ModelState.AddModelError(nameof(form.Reference), "This ID is already used on another near miss.");
        }

        if (form.NearMissTypeId is null or <= 0)
            ModelState.AddModelError(nameof(form.NearMissTypeId), "Select a type.");
        if (form.DirectorateLookupId is null or <= 0)
            ModelState.AddModelError(nameof(form.DirectorateLookupId), "Select a directorate.");
        if (form.BusinessAreaLookupId is null or <= 0)
            ModelState.AddModelError(nameof(form.BusinessAreaLookupId), "Select a business area.");
        if (form.OwnerUserIds == null || form.OwnerUserIds.Count == 0)
            ModelState.AddModelError(nameof(form.OwnerUserIds), "Select at least one owner.");
        if (form.NearMissSeriousnessId is null or <= 0)
            ModelState.AddModelError(nameof(form.NearMissSeriousnessId), "Select a seriousness level.");
        if (form.NearMissStatusId is null or <= 0)
            ModelState.AddModelError(nameof(form.NearMissStatusId), "Select a status.");
        if (form.RiskTierId is null or <= 0)
            ModelState.AddModelError(nameof(form.RiskTierId), "Select a tier.");

        form.OwnerUserIds ??= new List<int>();
        form.OwnerUserIds = form.OwnerUserIds.Where(id => id > 0).Distinct().ToList();
        form.Actions ??= new List<ModernRaidNearMissActionFormRow>();
        form.Mitigations ??= new List<ModernRaidNearMissMitigationFormRow>();

        return ModelState.IsValid;
    }

    private static ModernRaidNearMissForm MapNearMissToForm(NearMiss nm)
    {
        RaidDateFormHelper.SplitDateParts(nm.DateLogged, out var ld, out var lm, out var ly);
        return new ModernRaidNearMissForm
        {
            Id = nm.Id,
            Reference = nm.Reference,
            LoggedDay = ld,
            LoggedMonth = lm,
            LoggedYear = ly,
            NearMissTypeId = nm.NearMissTypeId,
            DirectorateLookupId = nm.DirectorateLookupId,
            BusinessAreaLookupId = nm.BusinessAreaLookupId,
            OwnerUserIds = nm.NearMissOwners.Select(o => o.UserId).ToList(),
            Impact = nm.Impact,
            NearMissSeriousnessId = nm.NearMissSeriousnessId,
            PostMitigationRagStatusLookupId = nm.PostMitigationRagStatusLookupId,
            RiskTierId = nm.RiskTierId,
            NearMissStatusId = nm.NearMissStatusId,
            Actions = nm.NearMissActions.OrderBy(a => a.ActionDate).Select(a =>
            {
                RaidDateFormHelper.SplitDateParts(a.ActionDate, out var d, out var m, out var y);
                return new ModernRaidNearMissActionFormRow
                {
                    Id = a.Id,
                    ActionDay = d,
                    ActionMonth = m,
                    ActionYear = y,
                    ActionText = a.ActionText,
                    RecordedByUserId = a.RecordedByUserId,
                    RecordedByDisplayName = a.RecordedByUser != null ? FormatNearMissUser(a.RecordedByUser) : null
                };
            }).ToList(),
            Mitigations = nm.NearMissMitigations.OrderBy(m => m.MitigationDate).Select(m =>
            {
                RaidDateFormHelper.SplitDateParts(m.MitigationDate, out var d, out var mo, out var y);
                return new ModernRaidNearMissMitigationFormRow
                {
                    Id = m.Id,
                    MitigationDay = d,
                    MitigationMonth = mo,
                    MitigationYear = y,
                    AssuranceTakenPlace = m.AssuranceTakenPlace,
                    RecordedByUserId = m.RecordedByUserId,
                    RecordedByDisplayName = m.RecordedByUser != null ? FormatNearMissUser(m.RecordedByUser) : null
                };
            }).ToList()
        };
    }

    private async Task PersistNearMissOwnersAsync(int nearMissId, List<int>? userIds, CancellationToken cancellationToken)
    {
        var existing = await _db.NearMissOwners.Where(x => x.NearMissId == nearMissId).ToListAsync(cancellationToken);
        _db.NearMissOwners.RemoveRange(existing);
        foreach (var uid in (userIds ?? new List<int>()).Where(id => id > 0).Distinct())
            _db.NearMissOwners.Add(new NearMissOwner { NearMissId = nearMissId, UserId = uid });
    }

    private async Task PersistNearMissActionsAsync(
        int nearMissId,
        List<ModernRaidNearMissActionFormRow>? rows,
        int? defaultRecordedByUserId,
        CancellationToken cancellationToken)
    {
        var existing = await _db.NearMissActions.Where(x => x.NearMissId == nearMissId).ToListAsync(cancellationToken);
        _db.NearMissActions.RemoveRange(existing);

        foreach (var row in rows ?? new List<ModernRaidNearMissActionFormRow>())
        {
            if (string.IsNullOrWhiteSpace(row.ActionText))
                continue;
            if (!RaidDateFormHelper.TryOptionalDate(
                    row.ActionDay, row.ActionMonth, row.ActionYear, "ActionDate", ModelState, out var dt)
                || !dt.HasValue)
                continue;

            _db.NearMissActions.Add(new NearMissAction
            {
                NearMissId = nearMissId,
                ActionDate = dt.Value,
                ActionText = row.ActionText.Trim(),
                RecordedByUserId = row.RecordedByUserId > 0 ? row.RecordedByUserId : defaultRecordedByUserId,
                CreatedAt = DateTime.UtcNow
            });
        }
    }

    private async Task PersistNearMissMitigationsAsync(
        int nearMissId,
        List<ModernRaidNearMissMitigationFormRow>? rows,
        int? defaultRecordedByUserId,
        CancellationToken cancellationToken)
    {
        var existing = await _db.NearMissMitigations.Where(x => x.NearMissId == nearMissId).ToListAsync(cancellationToken);
        _db.NearMissMitigations.RemoveRange(existing);

        foreach (var row in rows ?? new List<ModernRaidNearMissMitigationFormRow>())
        {
            if (string.IsNullOrWhiteSpace(row.AssuranceTakenPlace))
                continue;
            if (!RaidDateFormHelper.TryOptionalDate(
                    row.MitigationDay, row.MitigationMonth, row.MitigationYear, "MitigationDate", ModelState, out var dt)
                || !dt.HasValue)
                continue;

            _db.NearMissMitigations.Add(new NearMissMitigation
            {
                NearMissId = nearMissId,
                MitigationDate = dt.Value,
                AssuranceTakenPlace = row.AssuranceTakenPlace.Trim(),
                RecordedByUserId = row.RecordedByUserId > 0 ? row.RecordedByUserId : defaultRecordedByUserId,
                CreatedAt = DateTime.UtcNow
            });
        }
    }
}
