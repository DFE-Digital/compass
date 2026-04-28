using System.Globalization;
using System.Security.Claims;
using System.Linq.Expressions;
using System.Text;
using Compass.Data;
using Compass.Models;
using Compass.Models.Modern.Work;
using Compass.Services;
using Compass.Services.Fips;
using Compass.Services.Raid;
using Compass.ViewModels.Modern;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Compass.Controllers.Modern;

/// <summary>RAID cross-service area at <c>/modern/raid</c> (Risks, Issues, Dependencies, Assumptions, etc.).</summary>
[Authorize]
[Route("modern/raid")]
public partial class ModernRaidController : Controller
{
    private readonly CompassDbContext _db;
    private readonly IBusinessAreaAdminService _businessAreaAdmins;
    private readonly IBusinessAreaLeadershipService _businessAreaLeadership;
    private readonly IDirectorateLeadershipService _directorateLeadership;
    private readonly IPermissionService _permissions;

    public ModernRaidController(
        CompassDbContext db,
        IBusinessAreaAdminService businessAreaAdmins,
        IBusinessAreaLeadershipService businessAreaLeadership,
        IDirectorateLeadershipService directorateLeadership,
        IPermissionService permissions)
    {
        _db = db;
        _businessAreaAdmins = businessAreaAdmins;
        _businessAreaLeadership = businessAreaLeadership;
        _directorateLeadership = directorateLeadership;
        _permissions = permissions;
    }

    private void SetRaidChrome(string subItem)
    {
        ViewBag.MainNavSection = "raid";
        ViewBag.SubNavItem = subItem;
    }

    /// <summary>Maps stored <see cref="RaidAssociationKinds"/> / legacy rows to UI radio values (work, product, organisation).</summary>
    private static string ToRaidAssociationUiKind(string? storedKind, int? projectId, int? primaryProductId)
    {
        if (storedKind == RaidAssociationKinds.Product) return "product";
        if (storedKind == RaidAssociationKinds.Organisation) return "organisation";
        if (storedKind == RaidAssociationKinds.WorkItem) return "work";
        if (primaryProductId.HasValue) return "product";
        if (projectId.HasValue) return "work";
        return "organisation";
    }

    private Task<List<RiskIssueNamedIntOption>> RaidFipsProductSelectOptionsAsync(CancellationToken cancellationToken) =>
        FipsProductRaidQuery.BuildActiveCmdbProductServiceSelectOptionsForRaidAsync(_db, cancellationToken);

    /// <summary>All non-deleted work items for RAID register editors (no artificial cap).</summary>
    private async Task<List<RiskIssueNamedIntOption>> RaidEditorProjectOptionsFullAsync(CancellationToken cancellationToken)
    {
        return await _db.Projects.AsNoTracking()
            .Where(p => !p.IsDeleted)
            .OrderBy(p => p.Title)
            .Select(p => new RiskIssueNamedIntOption { Id = p.Id, Name = p.Title ?? "" })
            .ToListAsync(cancellationToken);
    }

    private async Task PopulateRaidUserPickersAsync(int? ownerUserId, int? sroUserId, CancellationToken cancellationToken)
    {
        var ids = new List<int>();
        if (ownerUserId is > 0) ids.Add(ownerUserId.Value);
        if (sroUserId is > 0) ids.Add(sroUserId.Value);
        ids = ids.Distinct().ToList();

        Dictionary<int, User> map = new();
        if (ids.Count > 0)
            map = await _db.Users.AsNoTracking().Where(u => ids.Contains(u.Id)).ToDictionaryAsync(u => u.Id, cancellationToken);

        static string? DisplayName(User u) => string.IsNullOrWhiteSpace(u.Name) ? u.Email : u.Name;

        User? OwnerRow() => ownerUserId is > 0 && map.TryGetValue(ownerUserId.Value, out var u) ? u : null;
        User? SroRow() => sroUserId is > 0 && map.TryGetValue(sroUserId.Value, out var u) ? u : null;

        ViewBag.OwnerUserPicker = new UserPickerViewModel
        {
            FieldName = "OwnerUserId",
            Label = "Owner",
            DefaultUserId = ownerUserId,
            DefaultName = OwnerRow() is { } o ? DisplayName(o) : null,
            DefaultEmail = OwnerRow()?.Email,
            InputIdSuffix = "owner"
        };

        ViewBag.SroUserPicker = new UserPickerViewModel
        {
            FieldName = "SroUserId",
            Label = "Senior responsible officer (SRO)",
            DefaultUserId = sroUserId,
            DefaultName = SroRow() is { } s ? DisplayName(s) : null,
            DefaultEmail = SroRow()?.Email,
            InputIdSuffix = "sro"
        };
    }

    [HttpGet("")]
    [HttpGet("index")]
    [HttpGet("dashboard")]
    [HttpGet("/ModernRaid")]
    [HttpGet("/ModernRaid/Index")]
    [HttpGet("/ModernRaid/Dashboard")]
    public async Task<IActionResult> Dashboard(CancellationToken cancellationToken = default)
    {
        SetRaidChrome("raid-dashboard");
        var userEmail = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(userEmail))
            return Challenge();

        var emailLower = userEmail.Trim().ToLowerInvariant();
        var viewer = await _db.Users.AsNoTracking()
            .Where(u => u.Email.ToLower() == emailLower)
            .Select(u => new { u.Id, u.Name, u.Email })
            .FirstOrDefaultAsync(cancellationToken);

        int? userId = viewer?.Id;
        var displayName = !string.IsNullOrWhiteSpace(viewer?.Name)
            ? viewer!.Name!.Trim()
            : viewer?.Email ?? userEmail;

        var projectIds = await RaidProjectsForUserEmail(emailLower)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);
        var productServiceIds = await RaidProductServiceIdsForProjectsAsync(projectIds, cancellationToken);

        var adminBusinessAreaIds = new List<int>();
        if (userId is int uid2)
        {
            var adminIds = await _businessAreaAdmins.GetAdministeredBusinessAreaLookupIdsAsync(uid2, cancellationToken);
            var leadershipIds = await _businessAreaLeadership.GetLeadershipBusinessAreaLookupIdsAsync(
                uid2, cancellationToken);
            adminBusinessAreaIds = adminIds.Union(leadershipIds).Distinct().ToList();
        }

        // Personal dashboard: only records the signed-in user created, owns (including email-only on risks), or is assigned to via a linked action.
        var riskScope = RaidDashboardRiskDirectToUser(userId, emailLower);
        var issueScope = RaidDashboardIssueDirectToUser(userId);

        var filRisksQ = _db.Risks.AsNoTracking().Where(r => !r.IsDeleted).Where(riskScope);
        var filIssuesQ = _db.Issues.AsNoTracking().Where(i => !i.IsDeleted).Where(issueScope);

        var openRiskCount = await filRisksQ.Where(r => r.ClosedDate == null).CountAsync(cancellationToken);
        var openIssueCount = await filIssuesQ.Where(i => i.ClosedDate == null).CountAsync(cancellationToken);

        var issueSeverityLabelList = await (
                from i in filIssuesQ
                where i.ClosedDate == null
                join sv in _db.IssueSeverities.AsNoTracking() on i.SeverityId equals sv.Id into sj
                from sv in sj.DefaultIfEmpty()
                select sv != null ? sv.Label : (i.Severity ?? "Unassigned"))
            .ToListAsync(cancellationToken);
        var openIssueSeverityBreakdown = issueSeverityLabelList
            .GroupBy(l => l)
            .Select(g => new ModernRaidLabelCountVm(g.Key, g.Count()))
            .OrderByDescending(x => x.Count)
            .ToList();

        var issuePriorityLabelList = await (
                from i in filIssuesQ
                where i.ClosedDate == null
                join pv in _db.IssuePriorities.AsNoTracking() on i.PriorityId equals pv.Id into pj
                from pv in pj.DefaultIfEmpty()
                select pv != null ? pv.Label : (string.IsNullOrWhiteSpace(i.Priority) ? "Unassigned" : i.Priority!))
            .ToListAsync(cancellationToken);
        var openIssuePriorityBreakdown = issuePriorityLabelList
            .GroupBy(l => l)
            .Select(g => new ModernRaidLabelCountVm(g.Key, g.Count()))
            .OrderByDescending(x => x.Count)
            .ToList();

        var openRiskScores = await filRisksQ
            .Where(r => r.ClosedDate == null)
            .Select(r => r.RiskScore)
            .ToListAsync(cancellationToken);
        var it1 = 0;
        var it2 = 0;
        var it3 = 0;
        foreach (var lab in issueSeverityLabelList)
            BucketIssueSeverityTier(lab, ref it1, ref it2, ref it3);

        var openRiskBandCrisisCritical = openRiskScores.Count(s => s >= 16);
        var openRiskBandModerate = openRiskScores.Count(s => s is >= 6 and <= 15);
        var openRiskBandMarginal = openRiskScores.Count(s => s <= 5);

        var matrixCells = new List<ModernRaidMatrixCellVm>();
        var cellMap = new Dictionary<(int Impact, int Likelihood), List<int>>();
        var openCells = await filRisksQ
            .Where(r => r.ClosedDate == null)
            .Select(r => new { r.Id, r.ImpactRating, r.LikelihoodRating })
            .ToListAsync(cancellationToken);
        foreach (var x in openCells)
        {
            var ir = Math.Clamp(x.ImpactRating, 1, 5);
            var lr = Math.Clamp(x.LikelihoodRating, 1, 5);
            var key = (ir, lr);
            if (!cellMap.TryGetValue(key, out var list))
            {
                list = new List<int>();
                cellMap[key] = list;
            }

            list.Add(x.Id);
        }

        var allCellRiskIds = cellMap.Values.SelectMany(ids => ids).Distinct().ToList();
        var riskMetaById = new Dictionary<int, ModernRaidMatrixCellRiskVm>();
        if (allCellRiskIds.Count > 0)
        {
            var fullRisks = await filRisksQ
                .Where(r => allCellRiskIds.Contains(r.Id))
                .AsSplitQuery()
                .Include(r => r.RiskTier)
                .Include(r => r.RiskStatus)
                .Include(r => r.Likelihood)
                .Include(r => r.ImpactLevel)
                .Include(r => r.OwnerUser)
                .Include(r => r.Project).ThenInclude(p => p!.BusinessAreaLookup)
                .Include(r => r.PrimaryProduct)
                .Include(r => r.RiskBusinessAreas).ThenInclude(x => x.BusinessAreaLookup)
                .ToListAsync(cancellationToken);
            foreach (var r in fullRisks)
            {
                var rel = RaidRegisterTableFormatting.BuildRiskRelation(r);
                string? workItemUrl = rel.Kind == RaidRegisterRelationKinds.Work && r.ProjectId is int wpid && wpid > 0
                    ? Url.Action("Detail", "ModernWork", new { id = wpid, tab = "risks" })
                    : null;
                riskMetaById[r.Id] = new ModernRaidMatrixCellRiskVm(
                    r.Id,
                    $"R-{r.Id:D4}",
                    r.Title ?? "—",
                    RaidRegisterTableFormatting.FormatRiskBusinessAreaLabels(r),
                    r.RiskTier?.Name,
                    rel.Kind,
                    rel.ProjectId,
                    rel.Target,
                    workItemUrl,
                    r.RiskStatus?.Label ?? r.Status,
                    r.OwnerUser != null ? (r.OwnerUser.Name ?? r.OwnerUser.Email) : r.OwnerEmail,
                    r.Likelihood?.Label ?? r.LikelihoodRating.ToString(),
                    r.ImpactLevel?.Label ?? r.ImpactRating.ToString(),
                    r.RiskScore);
            }
        }

        for (var impact = 5; impact >= 1; impact--)
        {
            for (var lik = 1; lik <= 5; lik++)
            {
                cellMap.TryGetValue((impact, lik), out var ids);
                var cnt = ids?.Count ?? 0;
                var cellScore = impact * lik;
                IReadOnlyList<ModernRaidMatrixCellRiskVm> cellRisks = Array.Empty<ModernRaidMatrixCellRiskVm>();
                if (cnt > 0 && ids != null)
                {
                    cellRisks = ids
                        .Select(id => riskMetaById.TryGetValue(id, out var m)
                            ? m
                            : new ModernRaidMatrixCellRiskVm(
                                id,
                                $"R-{id:D4}",
                                "—",
                                null,
                                null,
                                RaidRegisterRelationKinds.Unknown,
                                null,
                                null,
                                null,
                                null,
                                "—",
                                "—",
                                "—",
                                0))
                        .OrderBy(r => r.Reference, StringComparer.Ordinal)
                        .ToList();
                }

                matrixCells.Add(new ModernRaidMatrixCellVm(
                    impact,
                    lik,
                    cnt,
                    cellScore,
                    RaidMatrixCellTone(cellScore),
                    cellRisks));
            }
        }

        var utcNow = DateTime.UtcNow;
        const int registerStaleAfterDays = 30;
        const int ratingUnchangedAfterDays = 90;

        int DaysSinceUtc(DateTime t) => (int)Math.Floor((utcNow - t).TotalDays);

        var openRiskHealth = await filRisksQ
            .Where(r => r.ClosedDate == null)
            .Select(r => new { r.Id, r.Title, r.CreatedAt, r.UpdatedAt })
            .ToListAsync(cancellationToken);
        var openIssueHealth = await filIssuesQ
            .Where(i => i.ClosedDate == null)
            .Select(i => new { i.Id, i.Title, i.CreatedAt, i.UpdatedAt })
            .ToListAsync(cancellationToken);

        var lastRiskRatingUtc = await RaidDashboardAuditHealth.GetLastRiskRatingChangeUtcByRiskIdAsync(
            _db, openRiskHealth.Select(x => x.Id).ToList(), cancellationToken);
        var lastIssueRatingUtc = await RaidDashboardAuditHealth.GetLastIssueRatingChangeUtcByIssueIdAsync(
            _db, openIssueHealth.Select(x => x.Id).ToList(), cancellationToken);

        var risksInherentUnchanged3Months = new List<ModernRaidAttentionRowVm>(openRiskHealth.Count);
        foreach (var x in openRiskHealth)
        {
            var lastR = lastRiskRatingUtc.GetValueOrDefault(x.Id, x.CreatedAt);
            var dRt = DaysSinceUtc(lastR);
            if (dRt > ratingUnchangedAfterDays)
            {
                var dUp = DaysSinceUtc(x.UpdatedAt);
                risksInherentUnchanged3Months.Add(
                    new ModernRaidAttentionRowVm("Risk", x.Id, $"R-{x.Id:D4}", x.Title, dUp, dRt, dUp > registerStaleAfterDays, true));
            }
        }

        risksInherentUnchanged3Months = risksInherentUnchanged3Months
            .OrderByDescending(a => a.DaysSinceLastRatingChange)
            .ToList();

        var staleOpenRiskCount = openRiskHealth.Count(x => DaysSinceUtc(x.UpdatedAt) > registerStaleAfterDays);
        var staleOpenIssueCount = openIssueHealth.Count(x => DaysSinceUtc(x.UpdatedAt) > registerStaleAfterDays);

        var stagnantOpenIssueCount = 0;
        foreach (var x in openIssueHealth)
        {
            var last = lastIssueRatingUtc.GetValueOrDefault(x.Id, x.CreatedAt);
            if (DaysSinceUtc(last) > ratingUnchangedAfterDays)
                stagnantOpenIssueCount++;
        }

        var scopeSummary = userId is not null
            ? "Open risks, open issues, the matrix, and the tables below count only records you created, own (including by email on risks), or are assigned to through a linked action."
            : "Open risks, open issues, the matrix, and the tables below count only risks you own by email until your sign-in is linked in Compass users.";

        var meanScore = openRiskScores.Count == 0
            ? (double?)null
            : Math.Round(openRiskScores.Average(s => (double)s), 1);
        var elevated = openRiskScores.Count(s => s >= 15);

        var escRiskLabels = await (
                from r in filRisksQ
                where r.ClosedDate == null
                join s in _db.RiskStatuses.AsNoTracking() on r.RiskStatusId equals s.Id into gj
                from s in gj.DefaultIfEmpty()
                select s != null ? s.Label : (r.Status ?? ""))
            .ToListAsync(cancellationToken);
        var escIssueLabels = await (
                from i in filIssuesQ
                where i.ClosedDate == null
                join s in _db.IssueStatuses.AsNoTracking() on i.StatusId equals s.Id into gj
                from s in gj.DefaultIfEmpty()
                select s != null ? s.Label : (i.Status ?? ""))
            .ToListAsync(cancellationToken);
        var escalatedCount = escRiskLabels.Count(RaidLooksEscalated) + escIssueLabels.Count(RaidLooksEscalated);

        var riskCount = await filRisksQ.CountAsync(cancellationToken);
        var issueCount = await filIssuesQ.CountAsync(cancellationToken);

        var vm = new ModernRaidDashboardViewModel
        {
            ViewerDisplayName = displayName,
            ScopeSummary = scopeSummary,
            LinkedWorkItemCount = projectIds.Count,
            LinkedProductCount = productServiceIds.Count,
            RiskCount = riskCount,
            IssueCount = issueCount,
            DependencyCount = 0,
            AssumptionCount = 0,
            OpenRiskCount = openRiskCount,
            OpenIssueCount = openIssueCount,
            ClosedRiskCount = 0,
            ClosedIssueCount = 0,
            ElevatedOpenRiskCount = elevated,
            MeanOpenRiskScore = meanScore,
            OpenRiskStatusBreakdown = Array.Empty<ModernRaidLabelCountVm>(),
            OpenIssueStatusBreakdown = Array.Empty<ModernRaidLabelCountVm>(),
            OpenRiskTierBreakdown = Array.Empty<ModernRaidLabelCountVm>(),
            OpenRiskScoreBands = Array.Empty<ModernRaidLabelCountVm>(),
            OrganisationTotals = new ModernRaidOrganisationTotalsVm(0, 0, 0, 0, 0, 0),
            DataAsAtUtc = DateTime.UtcNow,
            OpenCrisisCriticalRiskCount = openRiskBandCrisisCritical,
            OpenModerateRiskCount = openRiskBandModerate,
            OpenMarginalNegligibleRiskCount = openRiskBandMarginal,
            EscalatedOpenRecordCount = escalatedCount,
            OpenIssueTier1ApproxCount = it1,
            OpenIssueTier2ApproxCount = it2,
            OpenIssueTier3ApproxCount = it3,
            OpenIssueSeverityBreakdown = openIssueSeverityBreakdown,
            OpenIssuePriorityBreakdown = openIssuePriorityBreakdown,
            MatrixCells = matrixCells,
            TopRisks = Array.Empty<ModernRaidTopRiskVm>(),
            DashboardRiskRows = Array.Empty<ModernRaidDashboardRiskRegisterVm>(),
            DashboardIssueRows = Array.Empty<ModernRaidDashboardIssueRegisterVm>(),
            ImpactTypeSummaries = Array.Empty<ModernRaidImpactTypeSummaryVm>(),
            RecentMovementRows = Array.Empty<ModernRaidMovementRowVm>(),
            PortfolioOptions = Array.Empty<ModernRaidDashboardProjectOption>(),
            SelectedPortfolioProjectId = null,
            ActiveReportTab = "summary",
            OrganisationWideDashboard = false,
            StaleOpenRiskCount = staleOpenRiskCount,
            StaleOpenIssueCount = staleOpenIssueCount,
            StagnantOpenRiskCount = risksInherentUnchanged3Months.Count,
            StagnantOpenIssueCount = stagnantOpenIssueCount,
            AttentionRows = Array.Empty<ModernRaidAttentionRowVm>(),
            RisksInherentRatingUnchangedThreePlusMonths = risksInherentUnchanged3Months,
            ViewerIsBusinessAreaAdmin = adminBusinessAreaIds.Count > 0
        };

        return View("~/Views/Modern/Raid/Dashboard.cshtml", vm);
    }
    /// <summary>Same association rules as <see cref="Services.Modern.ModernWorkService"/> work dashboard.</summary>
    private IQueryable<Project> RaidProjectsForUserEmail(string emailLower) =>
        _db.Projects.AsNoTracking()
            .Where(p => !p.IsDeleted)
            .Where(p =>
                p.ProjectContacts.Any(pc => pc.Email.ToLower() == emailLower) ||
                (p.PrimaryContactUser != null && p.PrimaryContactUser.Email.ToLower() == emailLower) ||
                p.SeniorResponsibleOfficers.Any(sro => sro.User != null && sro.User.Email.ToLower() == emailLower) ||
                p.ServiceOwners.Any(so => so.User != null && so.User.Email.ToLower() == emailLower) ||
                p.PmoContacts.Any(pmo => pmo.User != null && pmo.User.Email.ToLower() == emailLower));

    private async Task<List<int>> RaidProductServiceIdsForProjectsAsync(
        IReadOnlyCollection<int> projectIds,
        CancellationToken cancellationToken)
    {
        if (projectIds.Count == 0)
            return new List<int>();

        var fipsIds = await _db.ProjectProducts.AsNoTracking()
            .Where(pp => projectIds.Contains(pp.ProjectId))
            .Where(pp => pp.ProductFipsId != null && pp.ProductFipsId != "")
            .Select(pp => pp.ProductFipsId!)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (fipsIds.Count == 0)
            return new List<int>();

        return await _db.Services.AsNoTracking()
            .Where(s => fipsIds.Contains(s.FipsId))
            .Select(s => s.ServiceId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    private static Expression<Func<Risk, bool>> RaidRiskMatchesViewerScope(
        int? userId,
        string emailLower,
        List<int> projectIds,
        List<int> productServiceIds,
        List<int> adminBusinessAreaIds)
    {
        return r =>
            (userId.HasValue && (r.CreatedByUserId == userId.Value || r.OwnerUserId == userId.Value || r.SroUserId == userId.Value)) ||
            (r.OwnerUserId == null && r.OwnerEmail != null && r.OwnerEmail.ToLower() == emailLower) ||
            (projectIds.Count > 0 && r.ProjectId != null && projectIds.Contains(r.ProjectId.Value)) ||
            (productServiceIds.Count > 0 && r.PrimaryProductId != null &&
             productServiceIds.Contains(r.PrimaryProductId.Value)) ||
            (adminBusinessAreaIds.Count > 0 &&
             r.RiskBusinessAreas.Any(ra => adminBusinessAreaIds.Contains(ra.BusinessAreaLookupId))) ||
            (adminBusinessAreaIds.Count > 0 && r.ProjectId != null && r.Project != null && r.Project.BusinessAreaId != null &&
             adminBusinessAreaIds.Contains(r.Project.BusinessAreaId.Value));
    }

    private static Expression<Func<Issue, bool>> RaidIssueMatchesViewerScope(
        int? userId,
        List<int> projectIds,
        List<int> productServiceIds,
        List<int> adminBusinessAreaIds)
    {
        return i =>
            (userId.HasValue && (i.CreatedByUserId == userId.Value || i.OwnerUserId == userId.Value || i.SroUserId == userId.Value)) ||
            (projectIds.Count > 0 && i.ProjectId != null && projectIds.Contains(i.ProjectId.Value)) ||
            (productServiceIds.Count > 0 && i.PrimaryProductId != null &&
             productServiceIds.Contains(i.PrimaryProductId.Value)) ||
            (adminBusinessAreaIds.Count > 0 &&
             i.IssueBusinessAreas.Any(ia => adminBusinessAreaIds.Contains(ia.BusinessAreaLookupId))) ||
            (adminBusinessAreaIds.Count > 0 && i.ProjectId != null && i.Project != null && i.Project.BusinessAreaId != null &&
             adminBusinessAreaIds.Contains(i.Project.BusinessAreaId.Value));
    }

    /// <summary>Personal RAID dashboard: risks the viewer created, owns (including by email), or is assigned to via a linked action.</summary>
    private static Expression<Func<Risk, bool>> RaidDashboardRiskDirectToUser(int? userId, string emailLower)
    {
        var el = (emailLower ?? string.Empty).Trim().ToLowerInvariant();
        if (userId is int uid)
        {
            return r =>
                r.CreatedByUserId == uid ||
                r.OwnerUserId == uid ||
                (r.OwnerEmail != null && r.OwnerEmail.ToLower() == el) ||
                r.RiskActions.Any(ra => ra.Action.AssignedToUserId == uid);
        }

        return r =>
            r.OwnerUserId == null &&
            r.OwnerEmail != null &&
            r.OwnerEmail.ToLower() == el;
    }

    /// <summary>Personal RAID dashboard: issues the viewer created, owns, or is assigned to via a linked action.</summary>
    private static Expression<Func<Issue, bool>> RaidDashboardIssueDirectToUser(int? userId)
    {
        if (userId is not int uid)
            return i => false;

        return i =>
            i.CreatedByUserId == uid ||
            i.OwnerUserId == uid ||
            i.IssueActions.Any(ia => ia.Action.AssignedToUserId == uid);
    }

    private static Expression<Func<Assumption, bool>> RaidAssumptionMatchesViewerScope(
        int? userId,
        List<int> projectIds,
        List<int> productServiceIds)
    {
        return a =>
            (userId.HasValue && a.OwnerUserId == userId.Value) ||
            (projectIds.Count > 0 && a.ProjectId != null && projectIds.Contains(a.ProjectId.Value)) ||
            (productServiceIds.Count > 0 && a.PrimaryProductId != null &&
             productServiceIds.Contains(a.PrimaryProductId.Value));
    }

    private static string RaidRiskRatingLabel(Risk risk) => RaidRiskRatingLabelFromScore(risk.RiskScore);

    /// <summary>Inherent rating phrase from product score (impact × likelihood).</summary>
    private static string RaidRiskRatingLabelFromScore(int score)
    {
        if (score >= 20) return "Crisis / Likely";
        if (score >= 16) return "Critical / Possible";
        if (score >= 11) return "High / Possible";
        if (score >= 6) return "Moderate / Possible";
        return "Low / Unlikely";
    }

    private static string RaidRiskRatingTagClass(int score)
    {
        if (score >= 20) return "dfe-c-tag dfe-c-tag--red-strong";
        if (score >= 16) return "dfe-c-tag dfe-c-tag--red";
        if (score >= 11) return "dfe-c-tag dfe-c-tag--amber";
        if (score >= 6) return "dfe-c-tag dfe-c-tag--amber";
        return "dfe-c-tag dfe-c-tag--green";
    }

    private static string RaidRiskStatusTagClass(string? statusLabel)
    {
        var s = (statusLabel ?? "").ToLowerInvariant();
        if (s.Contains("escalat")) return "dfe-c-tag dfe-c-tag--red";
        if (s.Contains("closed")) return "dfe-c-tag dfe-c-tag--green";
        return "dfe-c-tag dfe-c-tag--amber";
    }

    private static string RaidMatrixCellTone(int cellScore)
    {
        if (cellScore >= 20) return "r";
        if (cellScore >= 15) return "ar";
        if (cellScore >= 8) return "ag";
        return "g";
    }

    private static bool RaidLooksEscalated(string? label)
    {
        var l = (label ?? "").ToLowerInvariant();
        return l.Contains("escalat");
    }

    private static string RaidIssueSeverityTierLabel(string? severityLabel)
    {
        var l = (severityLabel ?? "").Trim();
        if (string.IsNullOrEmpty(l)) return "Tier 3 — Team";
        var ll = l.ToLowerInvariant();
        if (ll.Contains("major") || ll.Contains("critical") || ll.Contains("tier 1"))
            return "Tier 1 — governance";
        if (ll.Contains("medium") || ll.Contains("tier 2"))
            return "Tier 2 — Director";
        return "Tier 3 — Team";
    }

    private static string RaidIssueSeverityTagClass(string? severityLabel)
    {
        var ll = (severityLabel ?? "").ToLowerInvariant();
        if (ll.Contains("major") || ll.Contains("critical")) return "dfe-c-tag dfe-c-tag--red-strong";
        if (ll.Contains("medium")) return "dfe-c-tag dfe-c-tag--amber";
        return "dfe-c-tag dfe-c-tag--green";
    }

    private static string RaidIssuePriorityTagClass(string? priorityLabel)
    {
        var l = (priorityLabel ?? "—").ToLowerInvariant();
        if (l is "—" or "")
            return "dfe-c-tag dfe-c-tag--light-grey";
        if (l.Contains("p1", StringComparison.Ordinal) || l.Contains("high", StringComparison.Ordinal) ||
            l.Contains("urgent", StringComparison.Ordinal) || l.Contains("critical", StringComparison.Ordinal))
            return "dfe-c-tag dfe-c-tag--red";
        if (l.Contains("p2", StringComparison.Ordinal) || l.Contains("medium", StringComparison.Ordinal))
            return "dfe-c-tag dfe-c-tag--amber";
        if (l.Contains("p3", StringComparison.Ordinal) || l.Contains("p4", StringComparison.Ordinal) ||
            l.Contains("low", StringComparison.Ordinal))
            return "dfe-c-tag dfe-c-tag--green";
        return "dfe-c-tag dfe-c-tag--light-grey";
    }

    private static void BucketIssueSeverityTier(string? severityLabel, ref int tier1, ref int tier2, ref int tier3)
    {
        var ll = (severityLabel ?? "").ToLowerInvariant();
        if (ll.Contains("major") || ll.Contains("critical") || ll.Contains("tier 1"))
        {
            tier1++;
            return;
        }

        if (ll.Contains("medium") || ll.Contains("tier 2"))
        {
            tier2++;
            return;
        }

        tier3++;
    }

    private static string RaidResidualSummary(Risk r)
    {
        if (r.ResidualImpact is > 0 && r.ResidualLikelihood is > 0)
        {
            var rs = Math.Clamp(r.ResidualImpact.Value * r.ResidualLikelihood.Value, 1, 25);
            return RaidRiskRatingLabelFromScore(rs);
        }

        if (r.ResidualScore.HasValue)
        {
            var v = (int)Math.Clamp(Math.Round(r.ResidualScore.Value), 1, 25);
            return RaidRiskRatingLabelFromScore(v);
        }

        return "—";
    }

    private static string PrimaryRiskImpactTypeLabel(Risk r)
    {
        var junction = r.RiskRiskCategories?.FirstOrDefault()?.RiskCategory?.Label;
        if (!string.IsNullOrWhiteSpace(junction))
            return junction.Trim();
        if (!string.IsNullOrWhiteSpace(r.RiskCategory?.Label))
            return r.RiskCategory.Label.Trim();
        if (!string.IsNullOrWhiteSpace(r.Category))
            return r.Category.Trim();
        return "Unassigned";
    }

    private static string PrimaryIssueCategoryLabel(Issue i)
    {
        if (!string.IsNullOrWhiteSpace(i.CategoryLookup?.Label))
            return i.CategoryLookup.Label.Trim();
        if (!string.IsNullOrWhiteSpace(i.Category))
            return i.Category.Trim();
        return "Unassigned";
    }

    private static string RaidIssueTierLabel(Issue issue)
    {
        var sev = issue.SeverityId ?? 0;
        if (sev >= 5) return "Tier 1 — PRC";
        if (sev >= 3) return "Tier 2 — Director";
        return "Tier 3 — Team";
    }

    /// <summary>
    /// <para><strong>Escalate</strong> (higher DfE intensity, lower level number 1/2/3): every <strong>proposed</strong>
    /// band for targets below the current level down to the lowest configured operational level.</para>
    /// <para><strong>De-escalate</strong> (less intensity, higher number): every proposed band above the current
    /// level up to the highest configured operational level (e.g. from Tier 1: Tier 2 — Proposed and Tier 3 — Proposed,
    /// not a single “adjacent” rung).</para>
    /// Targets are always <see cref="RiskTier.IsProposedTier" /> (Admin → RAID → Risk tiers).
    /// </summary>
    private static IReadOnlyList<ModernRaidEscalationTierChoice> BuildRiskEscalationTierChoices(
        RiskTier? currentTier,
        IReadOnlyList<RiskTier> orderedActiveTiers)
    {
        if (currentTier == null || orderedActiveTiers.Count == 0)
            return Array.Empty<ModernRaidEscalationTierChoice>();

        var all = orderedActiveTiers.ToList();
        var current = all.FirstOrDefault(t => t.Id == currentTier.Id);
        if (current == null || current.IsProposedTier)
            return Array.Empty<ModernRaidEscalationTierChoice>();

        var proposed = all.Where(t => t.IsActive && t.IsProposedTier).ToList();
        if (proposed.Count == 0)
            return Array.Empty<ModernRaidEscalationTierChoice>();

        var currentLevel = RiskTierGovernance.ResolveLevel(current, all);

        // Use the full 1/2/3 (or more) product band range — not only "distinct levels among operational" rows,
        // or maxOp can be 1 and de-escalation from Tier 1 is empty; and proposed bands must be found with fallbacks.
        const int minBand = 1;
        var maxBand = Math.Max(
            3,
            all
                .Where(t => t.IsActive)
                .Select(t => RiskTierGovernance.ResolveLevel(t, all))
                .DefaultIfEmpty(0)
                .Max());

        var list = new List<ModernRaidEscalationTierChoice>();

        // Escalation (higher DfE intensity, lower number): e.g. Tier 3 → proposed for bands 1 and 2.
        for (var targetLevel = minBand; targetLevel < currentLevel; targetLevel++)
        {
            var esc = RiskTierGovernance.FindProposedForGovernanceBand(proposed, all, targetLevel);
            if (esc is null) continue;
            var hintE = !string.IsNullOrWhiteSpace(esc.Summary) ? esc.Summary.Trim() : esc.Description?.Trim();
            list.Add(new ModernRaidEscalationTierChoice(
                esc.Id,
                $"raid-escalate-tier-{esc.Id}",
                $"Escalate to {esc.Name}",
                string.IsNullOrWhiteSpace(hintE) ? null : hintE));
        }

        // De-escalation: e.g. Tier 1 → proposed for 2 and 3 (all bands above current up to product max).
        for (var targetLevel = currentLevel + 1; targetLevel <= maxBand; targetLevel++)
        {
            var de = RiskTierGovernance.FindProposedForGovernanceBand(proposed, all, targetLevel);
            if (de is null) continue;
            var hintD = !string.IsNullOrWhiteSpace(de.Summary) ? de.Summary.Trim() : de.Description?.Trim();
            list.Add(new ModernRaidEscalationTierChoice(
                de.Id,
                $"raid-deescalate-tier-{de.Id}",
                $"De-escalate to {de.Name}",
                string.IsNullOrWhiteSpace(hintD) ? null : hintD));
        }

        return list;
    }

    [HttpGet("risks/{id:int}/escalation-request")]
    public async Task<IActionResult> RiskEscalationRequest(int id, CancellationToken cancellationToken = default)
    {
        SetRaidChrome("raid-risks");
        var risk = await _db.Risks.AsNoTracking()
            .Include(r => r.RiskTier)
            .Include(r => r.RiskStatus)
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, cancellationToken);
        if (risk == null)
            return NotFound();

        var tiers = await _db.RiskTiers.AsNoTracking()
            .Where(t => t.IsActive)
            .OrderBy(t => t.SortOrder)
            .ThenBy(t => t.Id)
            .ToListAsync(cancellationToken);

        var tierChoices = BuildRiskEscalationTierChoices(risk.RiskTier, tiers);
        string? blockedReason = null;
        if (risk.RiskTierId == null || risk.RiskTier == null)
            blockedReason = "This risk does not have a tier assigned. Add a tier on the risk record before requesting a governance change.";
        else if (risk.RiskTier!.IsProposedTier)
            blockedReason = "This risk is on a proposed-tier row. Set an operational tier on the risk before requesting a change.";
        else if (tierChoices.Count == 0)
            blockedReason =
                "No valid proposed target tier is available. In Admin → RAID → Risk tiers, add “Tier 1/2/3 — Proposed” (one per band) with explicit governance levels 1–3, or use names that include “tier 1”, “tier 2”, and “tier 3”.";

        var pendingReq = await _db.RaidEscalationTierChangeRequests.AsNoTracking()
            .Include(x => x.FromRiskTier)
            .Include(x => x.ToRiskTier)
            .Include(x => x.SubmittedByUser)
            .Where(x => x.RiskId == id && x.RecordType == "risk" && x.Status == "pending")
            .OrderByDescending(x => x.SubmittedAt)
            .FirstOrDefaultAsync(cancellationToken);

        string? pendingRequestedBy = null;
        if (pendingReq?.SubmittedByUser != null)
        {
            pendingRequestedBy = string.IsNullOrWhiteSpace(pendingReq.SubmittedByUser.Name)
                ? pendingReq.SubmittedByUser.Email
                : pendingReq.SubmittedByUser.Name;
        }

        var currentUid = await ResolveCurrentUserIdAsync(cancellationToken);
        var canCancel = pendingReq != null
            && currentUid.HasValue
            && pendingReq.SubmittedByUserId == currentUid.Value;

        var vm = new ModernRaidEscalationRequestViewModel
        {
            RecordType = "risk",
            RecordId = risk.Id,
            Reference = $"R-{risk.Id:D4}",
            Title = risk.Title,
            CurrentTierLabel = risk.RiskTier?.Name ?? "Unassigned",
            CurrentStatusLabel = risk.RiskStatus?.Label ?? risk.Status ?? "Open",
            CurrentRatingLabel = RaidRiskRatingLabel(risk),
            CurrentDetail = $"Score {risk.RiskScore} · Last updated {risk.UpdatedAt:d MMM yyyy}",
            UpdatedAt = risk.UpdatedAt,
            CurrentTierId = risk.RiskTierId,
            TierChoices = tierChoices,
            TierChoicesBlockedReason = blockedReason,
            HasPendingTierChangeRequest = pendingReq != null,
            PendingRequestId = pendingReq?.Id,
            PendingFromTierLabel = pendingReq?.FromRiskTier?.Name,
            PendingToTierLabel = pendingReq?.ToRiskTier?.Name,
            PendingRationale = pendingReq?.Rationale,
            PendingRequestedBy = pendingRequestedBy ?? "Unknown",
            PendingSubmittedAt = pendingReq?.SubmittedAt,
            CanCancelPendingRequest = canCancel
        };
        return View("~/Views/Modern/Raid/RiskEscalationRequest.cshtml", vm);
    }

    [HttpPost("risks/{id:int}/escalation-request")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RiskEscalationRequestPost(
        int id,
        int? requestedTargetTierId,
        string? rationale,
        CancellationToken cancellationToken = default)
    {
        var risk = await _db.Risks
            .Include(r => r.RiskTier)
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, cancellationToken);
        if (risk == null)
            return NotFound();

        var hasPending = await _db.RaidEscalationTierChangeRequests.AsNoTracking()
            .AnyAsync(
                x => x.RiskId == id && x.RecordType == "risk" && x.Status == "pending",
                cancellationToken);
        if (hasPending)
        {
            TempData["Error"] =
                "A tier change request is already waiting for Operations. Cancel that request first if you need to make a different change.";
            return RedirectToAction(nameof(RiskEscalationRequest), new { id });
        }

        var tiers = await _db.RiskTiers.AsNoTracking()
            .Where(t => t.IsActive)
            .OrderBy(t => t.SortOrder)
            .ThenBy(t => t.Id)
            .ToListAsync(cancellationToken);

        var allowed = BuildRiskEscalationTierChoices(risk.RiskTier, tiers);
        var choice = allowed.FirstOrDefault(c => c.TargetTierId == requestedTargetTierId);
        if (choice == null)
        {
            TempData["Error"] = "Choose a valid proposed tier from the list, or update the risk tier and try again.";
            return RedirectToAction(nameof(RiskEscalationRequest), new { id });
        }

        var rationaleNorm = NormalizeEscalationRationale(rationale);
        var utcNow = DateTime.UtcNow;

        var submittedByUserId = await ResolveCurrentUserIdAsync(cancellationToken);

        var fromTierId = risk.RiskTierId;
        var req = new RaidEscalationTierChangeRequest
        {
            RecordType = "risk",
            RiskId = id,
            FromRiskTierId = fromTierId,
            ToRiskTierId = choice.TargetTierId,
            Rationale = rationaleNorm,
            Status = "pending",
            SubmittedAt = utcNow,
            SubmittedByUserId = submittedByUserId
        };

        // Move the risk onto the proposed tier so Operations RAID queues and approve/reject flows align.
        risk.RiskTierId = choice.TargetTierId;
        risk.UpdatedAt = utcNow;
        risk.UpdatedByUserId = submittedByUserId;

        _db.RaidEscalationTierChangeRequests.Add(req);
        await _db.SaveChangesAsync(cancellationToken);

        TempData["Message"] =
            $"Risk escalation request submitted: {choice.PrimaryLabel}. Operations RAID will review.";
        return RedirectToAction(nameof(RiskDetail), new { id });
    }

    /// <summary>Withdraw a pending risk tier request (requester only). Restores the risk to the pre-request operational tier.</summary>
    [HttpPost("risks/{id:int}/escalation-request/cancel")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RiskEscalationRequestCancel(
        int id,
        int requestId,
        CancellationToken cancellationToken = default)
    {
        SetRaidChrome("raid-risks");
        var currentUid = await ResolveCurrentUserIdAsync(cancellationToken);
        if (currentUid is null)
        {
            TempData["Error"] = "We could not match your sign-in to a user. Try again or contact support.";
            return RedirectToAction(nameof(RiskEscalationRequest), new { id });
        }

        var req = await _db.RaidEscalationTierChangeRequests
            .Include(x => x.Risk)
            .FirstOrDefaultAsync(
                x => x.Id == requestId && x.RiskId == id,
                cancellationToken);
        if (req == null || !string.Equals(req.RecordType, "risk", StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "That request was not found.";
            return RedirectToAction(nameof(RiskEscalationRequest), new { id });
        }

        if (!string.Equals(req.Status, "pending", StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "This request is no longer pending.";
            return RedirectToAction(nameof(RiskEscalationRequest), new { id });
        }

        if (req.SubmittedByUserId != currentUid.Value)
        {
            TempData["Error"] = "Only the person who submitted the request can cancel it.";
            return RedirectToAction(nameof(RiskEscalationRequest), new { id });
        }

        if (req.Risk is null)
        {
            TempData["Error"] = "The risk for this request could not be found.";
            return RedirectToAction(nameof(RiskEscalationRequest), new { id });
        }

        var utcNow = DateTime.UtcNow;
        req.Status = "cancelled";
        req.DecidedAt = utcNow;
        req.DecidedByUserId = currentUid;
        req.DecisionNote = "Cancelled by the requester before Operations reviewed it.";

        if (req.FromRiskTierId.HasValue)
        {
            req.Risk.RiskTierId = req.FromRiskTierId;
            req.Risk.UpdatedAt = utcNow;
            req.Risk.UpdatedByUserId = currentUid;
        }

        await _db.SaveChangesAsync(cancellationToken);
        TempData["SuccessMessage"] =
            "The tier change request was cancelled. The risk’s tier has been restored to how it was when the request was raised.";
        return RedirectToAction(nameof(RiskEscalationRequest), new { id });
    }

    private static string? NormalizeEscalationRationale(string? rationale)
    {
        if (string.IsNullOrWhiteSpace(rationale))
            return null;
        var t = rationale.Trim();
        return t.Length <= 2000 ? t : t[..2000];
    }

    private async Task<int?> ResolveCurrentUserIdAsync(CancellationToken cancellationToken)
    {
        var email = User.Identity?.Name
            ?? User.FindFirst(ClaimTypes.Email)?.Value
            ?? User.FindFirst("preferred_username")?.Value;
        if (string.IsNullOrWhiteSpace(email))
            return null;
        email = email.Trim();
        return await _db.Users.AsNoTracking()
            .Where(u => u.Email.ToLower() == email.ToLower())
            .Select(u => (int?)u.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    [HttpGet("issues/{id:int}/escalation-request")]
    public async Task<IActionResult> IssueEscalationRequest(int id, CancellationToken cancellationToken = default)
    {
        SetRaidChrome("raid-issues");
        var issue = await _db.Issues.AsNoTracking()
            .Include(i => i.StatusLookup)
            .Include(i => i.SeverityLookup)
            .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted, cancellationToken);
        if (issue == null)
            return NotFound();

        var severityLabel = issue.SeverityLookup?.Label ?? issue.Severity ?? "Open";
        var vm = new ModernRaidEscalationRequestViewModel
        {
            RecordType = "issue",
            RecordId = issue.Id,
            Reference = $"I-{issue.Id:D4}",
            Title = issue.Title,
            CurrentTierLabel = RaidIssueTierLabel(issue),
            CurrentStatusLabel = issue.StatusLookup?.Label ?? issue.Status ?? "Open",
            CurrentRatingLabel = severityLabel,
            CurrentDetail = $"Detected {issue.DetectedDate:d MMM yyyy} · Updated {issue.UpdatedAt:d MMM yyyy}",
            UpdatedAt = issue.UpdatedAt
        };
        return View("~/Views/Modern/Raid/IssueEscalationRequest.cshtml", vm);
    }

    [HttpPost("issues/{id:int}/escalation-request")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> IssueEscalationRequestPost(
        int id,
        string? requestType,
        string? rationale,
        CancellationToken cancellationToken = default)
    {
        var exists = await _db.Issues.AsNoTracking().AnyAsync(i => i.Id == id && !i.IsDeleted, cancellationToken);
        if (!exists)
            return NotFound();
        TempData["Message"] = $"Issue escalation request ({requestType ?? "change"}) submitted.";
        return RedirectToAction(nameof(IssueDetail), new { id });
    }

    [HttpGet("export/risks.csv")]
    public async Task<IActionResult> ExportRisksCsv(CancellationToken cancellationToken = default)
    {
        var list = await _db.Risks.AsNoTracking()
            .Where(r => !r.IsDeleted)
            .Include(r => r.RiskTier)
            .Include(r => r.RiskStatus)
            .Include(r => r.RiskPriority)
            .Include(r => r.OwnerUser)
            .Include(r => r.Project)
            .OrderByDescending(r => r.UpdatedAt)
            .Take(5000)
            .ToListAsync(cancellationToken);

        await using var ms = new MemoryStream();
        await using var writer = new StreamWriter(ms, Encoding.UTF8, leaveOpen: true);
        var cfg = new CsvConfiguration(CultureInfo.InvariantCulture);
        await using var csv = new CsvWriter(writer, cfg);
        csv.WriteField("Id");
        csv.WriteField("ProjectId");
        csv.WriteField("WorkTitle");
        csv.WriteField("Title");
        csv.WriteField("Tier");
        csv.WriteField("Status");
        csv.WriteField("Priority");
        csv.WriteField("Owner");
        csv.WriteField("IdentifiedDate");
        csv.WriteField("UpdatedAt");
        csv.NextRecord();
        foreach (var r in list)
        {
            csv.WriteField(r.Id);
            csv.WriteField(r.ProjectId);
            csv.WriteField(r.Project?.Title);
            csv.WriteField(r.Title);
            csv.WriteField(r.RiskTier?.Name);
            csv.WriteField(r.RiskStatus?.Label ?? r.Status);
            csv.WriteField(r.RiskPriority?.Label);
            csv.WriteField(r.OwnerUser != null ? (r.OwnerUser.Name ?? r.OwnerUser.Email) : r.OwnerEmail);
            csv.WriteField(r.IdentifiedDate?.ToString("u", CultureInfo.InvariantCulture));
            csv.WriteField(r.UpdatedAt.ToString("u", CultureInfo.InvariantCulture));
            csv.NextRecord();
        }

        await writer.FlushAsync(cancellationToken);
        var bytes = ms.ToArray();
        return File(bytes, "text/csv; charset=utf-8", $"raid-risks-{DateTime.UtcNow:yyyyMMddHHmm}.csv");
    }

    [HttpGet("export/issues.csv")]
    public async Task<IActionResult> ExportIssuesCsv(CancellationToken cancellationToken = default)
    {
        var list = await _db.Issues.AsNoTracking()
            .Where(i => !i.IsDeleted)
            .Include(i => i.StatusLookup)
            .Include(i => i.PriorityLookup)
            .Include(i => i.SeverityLookup)
            .Include(i => i.OwnerUser)
            .Include(i => i.Project)
            .OrderByDescending(i => i.UpdatedAt)
            .Take(5000)
            .ToListAsync(cancellationToken);

        await using var ms = new MemoryStream();
        await using var writer = new StreamWriter(ms, Encoding.UTF8, leaveOpen: true);
        var cfg = new CsvConfiguration(CultureInfo.InvariantCulture);
        await using var csv = new CsvWriter(writer, cfg);
        csv.WriteField("Id");
        csv.WriteField("ProjectId");
        csv.WriteField("WorkTitle");
        csv.WriteField("Title");
        csv.WriteField("Status");
        csv.WriteField("Priority");
        csv.WriteField("Severity");
        csv.WriteField("Owner");
        csv.WriteField("DetectedDate");
        csv.WriteField("UpdatedAt");
        csv.NextRecord();
        foreach (var i in list)
        {
            csv.WriteField(i.Id);
            csv.WriteField(i.ProjectId);
            csv.WriteField(i.Project?.Title);
            csv.WriteField(i.Title);
            csv.WriteField(i.StatusLookup?.Label ?? i.Status);
            csv.WriteField(i.PriorityLookup?.Label ?? i.Priority);
            csv.WriteField(i.SeverityLookup?.Label ?? i.Severity);
            csv.WriteField(i.OwnerUser != null ? (i.OwnerUser.Name ?? i.OwnerUser.Email) : null);
            csv.WriteField(i.DetectedDate.ToString("u", CultureInfo.InvariantCulture));
            csv.WriteField(i.UpdatedAt.ToString("u", CultureInfo.InvariantCulture));
            csv.NextRecord();
        }

        await writer.FlushAsync(cancellationToken);
        return File(ms.ToArray(), "text/csv; charset=utf-8", $"raid-issues-{DateTime.UtcNow:yyyyMMddHHmm}.csv");
    }

    private static string NormalizeRaidEntityType(string entityType) => entityType.Trim().ToLowerInvariant();

    private static string Snippet(string? text, int maxChars)
    {
        if (string.IsNullOrEmpty(text))
            return "";
        var t = text.Trim().Replace('\r', ' ').Replace('\n', ' ');
        return t.Length <= maxChars ? t : t[..maxChars] + "…";
    }

    private async Task PrepareAssumptionCreateLookupsAsync(CancellationToken cancellationToken, int? ownerUserId = null, int? sroUserId = null)
    {
        ViewBag.AssumptionCriticalityOptions = await _db.AssumptionCriticalities.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Label)
            .ToListAsync(cancellationToken);
        ViewBag.AssumptionStatusOptions = await _db.AssumptionStatuses.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Label)
            .ToListAsync(cancellationToken);
        ViewBag.ProjectOptions = await RaidEditorProjectOptionsFullAsync(cancellationToken);
        ViewBag.FipsProductOptions = await RaidFipsProductSelectOptionsAsync(cancellationToken);
        await PopulateRaidUserPickersAsync(ownerUserId, sroUserId, cancellationToken);
        await LoadRaidDivisionBusinessAreaOptionsAsync(cancellationToken);
    }

    private async Task<Dictionary<(string NormalizedType, int Id), string>> BuildDependencyEndpointTitleMapAsync(
        IReadOnlyCollection<Dependency> dependencies,
        CancellationToken cancellationToken)
    {
        var pairs = new HashSet<(string NormalizedType, int Id)>();
        foreach (var d in dependencies)
        {
            pairs.Add((NormalizeRaidEntityType(d.SourceEntityType), d.SourceEntityId));
            pairs.Add((NormalizeRaidEntityType(d.TargetEntityType), d.TargetEntityId));
        }

        var map = new Dictionary<(string, int), string>();

        foreach (var grp in pairs.GroupBy(p => p.NormalizedType))
        {
            var ids = grp.Select(x => x.Id).Distinct().ToList();
            switch (grp.Key)
            {
                case "project":
                    {
                        var rows = await _db.Projects.AsNoTracking()
                            .Where(p => ids.Contains(p.Id))
                            .Select(p => new { p.Id, p.Title })
                            .ToListAsync(cancellationToken);
                        foreach (var x in rows)
                            map[("project", x.Id)] = x.Title ?? $"Project {x.Id}";
                        break;
                    }
                case "risk":
                    {
                        var rows = await _db.Risks.AsNoTracking()
                            .Where(r => ids.Contains(r.Id) && !r.IsDeleted)
                            .Select(r => new { r.Id, r.Title })
                            .ToListAsync(cancellationToken);
                        foreach (var x in rows)
                            map[("risk", x.Id)] = x.Title;
                        break;
                    }
                case "issue":
                    {
                        var rows = await _db.Issues.AsNoTracking()
                            .Where(i => ids.Contains(i.Id) && !i.IsDeleted)
                            .Select(i => new { i.Id, i.Title })
                            .ToListAsync(cancellationToken);
                        foreach (var x in rows)
                            map[("issue", x.Id)] = x.Title;
                        break;
                    }
                case "milestone":
                    {
                        var rows = await _db.Milestones.AsNoTracking()
                            .Where(m => ids.Contains(m.Id))
                            .Select(m => new { m.Id, m.Name })
                            .ToListAsync(cancellationToken);
                        foreach (var x in rows)
                            map[("milestone", x.Id)] = x.Name ?? $"Milestone {x.Id}";
                        break;
                    }
                case "action":
                    {
                        var rows = await _db.Actions.AsNoTracking()
                            .Where(a => ids.Contains(a.Id) && !a.IsDeleted)
                            .Select(a => new { a.Id, a.Title })
                            .ToListAsync(cancellationToken);
                        foreach (var x in rows)
                            map[("action", x.Id)] = x.Title ?? $"Action {x.Id}";
                        break;
                    }
                default:
                    foreach (var id in ids)
                        map[(grp.Key, id)] = $"{grp.Key} #{id}";
                    break;
            }
        }

        return map;
    }
}
