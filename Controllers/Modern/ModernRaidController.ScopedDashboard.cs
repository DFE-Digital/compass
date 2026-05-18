using Compass.Models;
using Compass.ViewModels.Modern;
using Microsoft.EntityFrameworkCore;

namespace Compass.Controllers.Modern;

/// <summary>RAID dashboard scoped to a directorate or business area.</summary>
public partial class ModernRaidController
{
    private sealed record RaidDashboardScopeContext(
        RaidDashboardScopeKind Kind,
        string Title,
        string Summary,
        string ListUrl,
        int? DirectorateId,
        int? BusinessAreaId,
        string? Subtitle,
        IQueryable<Risk> ScopedRisks,
        IQueryable<Issue> ScopedIssues);

    private async Task<RaidDashboardScopeContext> BuildDirectorateDashboardScopeAsync(
        Division division,
        CancellationToken cancellationToken)
    {
        var directorateId = division.Id;
        var projectIds = await _db.ProjectDirectorates.AsNoTracking()
            .Where(pd => pd.DivisionId == directorateId)
            .Select(pd => pd.ProjectId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var risksQ = _db.Risks.AsNoTracking().Where(r => !r.IsDeleted &&
            ((r.ProjectId.HasValue && projectIds.Contains(r.ProjectId.Value)) ||
             r.RiskDivisions.Any(rd => rd.DivisionId == directorateId)));

        var issuesQ = _db.Issues.AsNoTracking().Where(i => !i.IsDeleted &&
            ((i.ProjectId.HasValue && projectIds.Contains(i.ProjectId.Value)) ||
             i.IssueDivisions.Any(idv => idv.DivisionId == directorateId)));

        var listUrl = "/modern/raid/directorate";
        var summary =
            $"Risks and issues linked to work items in this directorate, or tagged to it on the RAID record. {projectIds.Count} linked work item(s).";

        return new RaidDashboardScopeContext(
            RaidDashboardScopeKind.Directorate,
            division.Name ?? $"Directorate #{directorateId}",
            summary,
            listUrl,
            directorateId,
            null,
            string.IsNullOrWhiteSpace(division.Description) ? null : division.Description.Trim(),
            risksQ,
            issuesQ);
    }

    private async Task<RaidDashboardScopeContext> BuildBusinessAreaDashboardScopeAsync(
        BusinessAreaLookup ba,
        CancellationToken cancellationToken)
    {
        var areaId = ba.Id;
        var projectIds = await _db.Projects.AsNoTracking()
            .Where(p => !p.IsDeleted && p.BusinessAreaId == areaId)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        var risksQ = _db.Risks.AsNoTracking().Where(r => !r.IsDeleted &&
            ((r.ProjectId != null && projectIds.Contains(r.ProjectId.Value)) ||
             r.RiskBusinessAreas.Any(rba => rba.BusinessAreaLookupId == areaId)));

        var issuesQ = _db.Issues.AsNoTracking().Where(i => !i.IsDeleted &&
            ((i.ProjectId != null && projectIds.Contains(i.ProjectId.Value)) ||
             i.IssueBusinessAreas.Any(iba => iba.BusinessAreaLookupId == areaId)));

        var directorateNames = await _db.DivisionBusinessAreas.AsNoTracking()
            .Where(x => x.BusinessAreaLookupId == areaId)
            .Select(x => x.Division.Name)
            .Distinct()
            .OrderBy(n => n)
            .ToListAsync(cancellationToken);

        var leadershipNames = await _db.BusinessAreaLeadershipMembers.AsNoTracking()
            .Where(m => m.BusinessAreaLookupId == areaId)
            .Select(m => m.User.Name ?? m.User.Email ?? "")
            .Where(s => s != "")
            .Distinct()
            .ToListAsync(cancellationToken);

        var subtitleParts = new List<string>();
        if (directorateNames.Count > 0)
            subtitleParts.Add("Directorates: " + string.Join(", ", directorateNames));
        if (leadershipNames.Count > 0)
            subtitleParts.Add("Leadership: " + string.Join(", ", leadershipNames));

        var listUrl = "/modern/raid/business-areas";
        var summary =
            $"Risks and issues linked to this business area on work items or RAID records. {projectIds.Count} linked work item(s).";

        return new RaidDashboardScopeContext(
            RaidDashboardScopeKind.BusinessArea,
            ba.Name ?? $"Business area #{areaId}",
            summary,
            listUrl,
            null,
            areaId,
            subtitleParts.Count == 0 ? null : string.Join(" · ", subtitleParts),
            risksQ,
            issuesQ);
    }

    private async Task<ModernRaidDashboardViewModel> BuildScopedRaidDashboardViewModelAsync(
        RaidDashboardScopeContext scope,
        string displayName,
        CancellationToken cancellationToken)
    {
        var risksBase = "/modern/raid/risks";
        var issuesBase = "/modern/raid/issues";
        var risksRegisterUrl = scope.DirectorateId is int did
            ? $"{risksBase}?divisionId={did}"
            : scope.BusinessAreaId is int baid
                ? $"{risksBase}?businessAreaId={baid}"
                : risksBase;

        var issuesRegisterUrl = scope.DirectorateId is int did2
            ? $"{issuesBase}?divisionId={did2}"
            : scope.BusinessAreaId is int baid2
                ? $"{issuesBase}?businessAreaId={baid2}"
                : issuesBase;

        var dirBase = "/modern/raid/directorate";
        var baBase = "/modern/raid/business-areas";
        var tierUrl = scope.DirectorateId is int did3
            ? $"{dirBase}?divisionId={did3}"
            : scope.BusinessAreaId is int baid3
                ? $"{baBase}?filterAreaId={baid3}"
                : "/modern/raid/tier";

        return await BuildRaidDashboardViewModelAsync(
            userId: null,
            emailLower: "",
            displayName: displayName,
            projectIds: new List<int>(),
            productServiceIds: new List<int>(),
            adminBusinessAreaIds: new List<int>(),
            scope: scope,
            risksRegisterUrlOverride: risksRegisterUrl,
            issuesRegisterUrlOverride: issuesRegisterUrl,
            tierUrlOverride: tierUrl,
            cancellationToken: cancellationToken);
    }
}
