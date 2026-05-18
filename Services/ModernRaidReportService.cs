using Compass.Controllers.Modern;
using Compass.Data;
using Compass.Models;
using Compass.ViewModels;
using Compass.ViewModels.Modern;
using Microsoft.EntityFrameworkCore;

namespace Compass.Services;

/// <summary>Builds the tabbed RAID register report at <c>/modern/reporting/raid</c>.</summary>
public sealed class ModernRaidReportService
{
  private readonly CompassDbContext _db;

  public ModernRaidReportService(CompassDbContext db) => _db = db;

  public async Task<ModernRaidReportViewModel> BuildAsync(
    string? tab,
    int? businessAreaId,
    int? directorateId,
    CancellationToken cancellationToken = default)
  {
    var activeTab = NormalizeTab(tab);
    var (filterScopeSummary, baOptions, dirOptions) =
      await LoadFilterOptionsAsync(businessAreaId, directorateId, cancellationToken);

    var risks = await BuildRisksTabAsync(businessAreaId, directorateId, cancellationToken);
    var issues = await BuildIssuesTabAsync(businessAreaId, directorateId, cancellationToken);
    var nearMisses = await BuildNearMissesTabAsync(businessAreaId, directorateId, cancellationToken);
    var dependencies = await BuildDependenciesTabAsync(businessAreaId, directorateId, cancellationToken);
    var assumptions = await BuildAssumptionsTabAsync(businessAreaId, directorateId, cancellationToken);

    return new ModernRaidReportViewModel
    {
      ActiveTab = activeTab,
      FilterScopeSummary = filterScopeSummary,
      FilterBusinessAreaId = businessAreaId,
      FilterDirectorateId = directorateId,
      BusinessAreaFilterOptions = baOptions,
      DirectorateFilterOptions = dirOptions,
      Risks = risks,
      Issues = issues,
      NearMisses = nearMisses,
      Dependencies = dependencies,
      Assumptions = assumptions
    };
  }

  private async Task<RaidReportTabPanel> BuildRisksTabAsync(
    int? businessAreaId,
    int? directorateId,
    CancellationToken cancellationToken)
  {
    var today = DateTime.UtcNow.Date;
    var q = _db.Risks.AsNoTracking().Where(r => !r.IsDeleted && r.ClosedDate == null);
    q = ApplyRiskScope(q, businessAreaId, directorateId);

    var openCount = await q.CountAsync(cancellationToken);
    var highCount = await q.CountAsync(r => r.RiskScore >= 15, cancellationToken);
    var reviewOverdue = await q.CountAsync(
      r => r.NextReviewDate.HasValue && r.NextReviewDate.Value < today,
      cancellationToken);
    var avgScore = openCount == 0
      ? 0
      : await q.AverageAsync(r => (double)r.RiskScore, cancellationToken);

    var list = await q
      .AsSplitQuery()
      .Include(r => r.RiskTier)
      .Include(r => r.RiskStatus)
      .Include(r => r.RiskBusinessAreas).ThenInclude(x => x.BusinessAreaLookup)
      .Include(r => r.Project).ThenInclude(p => p!.BusinessAreaLookup)
      .OrderByDescending(r => r.RiskScore)
      .ThenBy(r => r.Title)
      .ToListAsync(cancellationToken);

    var rows = list.Select(r => new RaidReportRegisterRow(
      r.Id,
      $"R-{r.Id}",
      r.Title,
      [
        r.RiskScore.ToString(),
        r.RiskTier?.Name ?? "—",
        r.RiskStatus?.Label ?? r.Status ?? "—",
        RaidRegisterTableFormatting.FormatRiskBusinessAreaLabels(r),
        r.NextReviewDate.HasValue
          ? (r.NextReviewDate.Value < today ? $"Overdue ({r.NextReviewDate.Value:d MMM yyyy})" : r.NextReviewDate.Value.ToString("d MMM yyyy"))
          : "—"
      ],
      nameof(ModernRaidController.RiskDetail))).ToList();

    return new RaidReportTabPanel
    {
      Stats =
      [
        new("Open risks", openCount.ToString(), openCount > 0 ? "dfe-f-stat-card--tint-grey" : "dfe-f-stat-card--tint-grey"),
        new("Score ≥ 15", highCount.ToString(), highCount > 0 ? "dfe-f-stat-card--tint-red" : "dfe-f-stat-card--tint-grey"),
        new("Review overdue", reviewOverdue.ToString(), reviewOverdue > 0 ? "dfe-f-stat-card--tint-red" : "dfe-f-stat-card--tint-grey"),
        new("Average score", openCount == 0 ? "—" : avgScore.ToString("0.#"), "dfe-f-stat-card--tint-grey")
      ],
      TableHeaders = ["Score", "Tier", "Status", "Business area", "Next review"],
      Rows = rows
    };
  }

  private async Task<RaidReportTabPanel> BuildIssuesTabAsync(
    int? businessAreaId,
    int? directorateId,
    CancellationToken cancellationToken)
  {
    var today = DateTime.UtcNow.Date;
    var thirtyDaysAgo = today.AddDays(-30);
    var q = _db.Issues.AsNoTracking().Where(i => !i.IsDeleted && i.ClosedDate == null);
    q = ApplyIssueScope(q, businessAreaId, directorateId);

    var criticalIds = await _db.IssueSeverities.AsNoTracking()
      .Where(s => s.IsActive && s.Label == "Critical")
      .Select(s => s.Id)
      .ToListAsync(cancellationToken);

    var openCount = await q.CountAsync(cancellationToken);
    var blocked = await q.CountAsync(i => i.BlockedFlag, cancellationToken);
    var critical = criticalIds.Count == 0
      ? 0
      : await q.CountAsync(i => i.SeverityId.HasValue && criticalIds.Contains(i.SeverityId.Value), cancellationToken);
    var raisedRecent = await q.CountAsync(
      i => i.DetectedDate.Date >= thirtyDaysAgo,
      cancellationToken);

    var list = await q
      .AsSplitQuery()
      .Include(i => i.SeverityLookup)
      .Include(i => i.PriorityLookup)
      .Include(i => i.StatusLookup)
      .Include(i => i.IssueBusinessAreas).ThenInclude(x => x.BusinessAreaLookup)
      .Include(i => i.Project).ThenInclude(p => p!.BusinessAreaLookup)
      .OrderByDescending(i => i.BlockedFlag)
      .ThenByDescending(i => i.SeverityLookup != null ? i.SeverityLookup.SortOrder : 0)
      .ThenBy(i => i.Title)
      .ToListAsync(cancellationToken);

    var rows = list.Select(i => new RaidReportRegisterRow(
      i.Id,
      $"I-{i.Id}",
      i.Title,
      [
        i.SeverityLookup?.Label ?? i.Severity ?? "—",
        i.PriorityLookup?.Label ?? i.Priority ?? "—",
        i.StatusLookup?.Label ?? i.Status ?? "—",
        i.BlockedFlag ? "Yes" : "No",
        RaidRegisterTableFormatting.FormatIssueBusinessAreaLabels(i)
      ],
      nameof(ModernRaidController.IssueDetail))).ToList();

    return new RaidReportTabPanel
    {
      Stats =
      [
        new("Open issues", openCount.ToString(), "dfe-f-stat-card--tint-grey"),
        new("Blocked", blocked.ToString(), blocked > 0 ? "dfe-f-stat-card--tint-red" : "dfe-f-stat-card--tint-grey"),
        new("Critical severity", critical.ToString(), critical > 0 ? "dfe-f-stat-card--tint-red" : "dfe-f-stat-card--tint-grey"),
        new("Raised (30 days)", raisedRecent.ToString(), "dfe-f-stat-card--tint-blue")
      ],
      TableHeaders = ["Severity", "Priority", "Status", "Blocked", "Business area"],
      Rows = rows
    };
  }

  private async Task<RaidReportTabPanel> BuildNearMissesTabAsync(
    int? businessAreaId,
    int? directorateId,
    CancellationToken cancellationToken)
  {
    var today = DateTime.UtcNow.Date;
    var thirtyDaysAgo = today.AddDays(-30);
    var q = OpenNearMissQuery(businessAreaId, directorateId);

    var highSeriousnessIds = await _db.NearMissSeriousnesses.AsNoTracking()
      .Where(s => s.IsActive && (s.Code == "4" || s.SortOrder >= 30))
      .Select(s => s.Id)
      .ToListAsync(cancellationToken);

    var openCount = await q.CountAsync(cancellationToken);
    var highSeriousness = highSeriousnessIds.Count == 0
      ? 0
      : await q.CountAsync(n => n.NearMissSeriousnessId.HasValue && highSeriousnessIds.Contains(n.NearMissSeriousnessId.Value), cancellationToken);
    var loggedRecent = await q.CountAsync(n => n.DateLogged.Date >= thirtyDaysAgo, cancellationToken);
    var withTier = await q.CountAsync(n => n.RiskTierId != null, cancellationToken);

    var list = await q
      .Include(n => n.TypeLookup)
      .Include(n => n.SeriousnessLookup)
      .Include(n => n.StatusLookup)
      .Include(n => n.RiskTier)
      .Include(n => n.BusinessAreaLookup)
      .Include(n => n.DirectorateLookup)
      .OrderByDescending(n => n.SeriousnessLookup != null ? n.SeriousnessLookup.SortOrder : 0)
      .ThenByDescending(n => n.DateLogged)
      .ToListAsync(cancellationToken);

    var rows = list.Select(n => new RaidReportRegisterRow(
      n.Id,
      n.Reference,
      Truncate(n.Impact ?? n.Reference, 120),
      [
        n.TypeLookup?.Label ?? "—",
        n.SeriousnessLookup?.Label ?? "—",
        n.RiskTier?.Name ?? "—",
        n.DateLogged.ToString("d MMM yyyy"),
        n.StatusLookup?.Label ?? "—"
      ],
      nameof(ModernRaidController.NearMissDetail))).ToList();

    return new RaidReportTabPanel
    {
      Stats =
      [
        new("Open near misses", openCount.ToString(), "dfe-f-stat-card--tint-blue"),
        new("High seriousness (3–4)", highSeriousness.ToString(), highSeriousness > 0 ? "dfe-f-stat-card--tint-red" : "dfe-f-stat-card--tint-grey"),
        new("Logged (30 days)", loggedRecent.ToString(), "dfe-f-stat-card--tint-grey"),
        new("With risk tier", withTier.ToString(), "dfe-f-stat-card--tint-grey")
      ],
      TableHeaders = ["Type", "Seriousness", "Tier", "Date logged", "Status"],
      Rows = rows
    };
  }

  private async Task<RaidReportTabPanel> BuildDependenciesTabAsync(
    int? businessAreaId,
    int? directorateId,
    CancellationToken cancellationToken)
  {
    var today = DateTime.UtcNow.Date;
    var dueSoonEnd = today.AddDays(30);
    var q = ActiveDependencyQuery(businessAreaId, directorateId);

    var activeCount = await q.CountAsync(cancellationToken);
    var overdue = await q.CountAsync(d => d.DueDate.HasValue && d.DueDate.Value < today, cancellationToken);
    var dueSoon = await q.CountAsync(
      d => d.DueDate.HasValue && d.DueDate.Value >= today && d.DueDate.Value <= dueSoonEnd,
      cancellationToken);
    var noDueDate = await q.CountAsync(d => !d.DueDate.HasValue, cancellationToken);

    var list = await q
      .Include(d => d.CriticalityLookup)
      .Include(d => d.LinkTypeLookup)
      .OrderBy(d => d.DueDate ?? DateTime.MaxValue)
      .ThenBy(d => d.Id)
      .ToListAsync(cancellationToken);

    var rows = list.Select(d =>
    {
      var summary = Truncate(
        d.Description ?? $"{d.SourceEntityType} {d.SourceEntityId} → {d.TargetEntityType} {d.TargetEntityId}",
        100);
      var due = d.DueDate.HasValue
        ? d.DueDate.Value < today
          ? $"Overdue ({d.DueDate.Value:d MMM yyyy})"
          : d.DueDate.Value.ToString("d MMM yyyy")
        : "—";
      return new RaidReportRegisterRow(
        d.Id,
        $"DEP-{d.Id}",
        summary,
        [
          d.LinkTypeLookup?.Label ?? d.DependencyType ?? "—",
          d.CriticalityLookup?.Label ?? "—",
          due,
          d.Organisation ?? "—"
        ],
        nameof(ModernRaidController.DependencyDetail));
    }).ToList();

    return new RaidReportTabPanel
    {
      Stats =
      [
        new("Active dependencies", activeCount.ToString(), "dfe-f-stat-card--tint-grey"),
        new("Overdue", overdue.ToString(), overdue > 0 ? "dfe-f-stat-card--tint-red" : "dfe-f-stat-card--tint-grey"),
        new("Due within 30 days", dueSoon.ToString(), dueSoon > 0 ? "dfe-f-stat-card--tint-yellow" : "dfe-f-stat-card--tint-grey"),
        new("No due date", noDueDate.ToString(), "dfe-f-stat-card--tint-grey")
      ],
      TableHeaders = ["Link type", "Criticality", "Due date", "Organisation"],
      Rows = rows
    };
  }

  private async Task<RaidReportTabPanel> BuildAssumptionsTabAsync(
    int? businessAreaId,
    int? directorateId,
    CancellationToken cancellationToken)
  {
    var today = DateTime.UtcNow.Date;
    var closedStatusIds = await _db.AssumptionStatuses.AsNoTracking()
      .Where(s => s.IsActive && s.Code == "CLOSED")
      .Select(s => s.Id)
      .ToListAsync(cancellationToken);

    var q = _db.Assumptions.AsNoTracking()
      .Where(a => !a.IsDeleted)
      .Where(a => !a.AssumptionStatusId.HasValue || !closedStatusIds.Contains(a.AssumptionStatusId.Value));
    q = ApplyAssumptionScope(q, businessAreaId, directorateId);

    var openCount = await q.CountAsync(cancellationToken);
    var reviewOverdue = await q.CountAsync(
      a => a.ReviewDate.HasValue && a.ReviewDate.Value < today,
      cancellationToken);
    var noReviewDate = await q.CountAsync(a => !a.ReviewDate.HasValue, cancellationToken);
    var highCritIds = await _db.AssumptionCriticalities.AsNoTracking()
      .Where(c => c.IsActive && c.SortOrder >= 30)
      .Select(c => c.Id)
      .ToListAsync(cancellationToken);
    var highCriticality = highCritIds.Count == 0
      ? 0
      : await q.CountAsync(
        a => a.AssumptionCriticalityId.HasValue && highCritIds.Contains(a.AssumptionCriticalityId.Value),
        cancellationToken);

    var list = await q
      .Include(a => a.CriticalityLookup)
      .Include(a => a.StatusLookup)
      .OrderByDescending(a => a.CriticalityLookup != null ? a.CriticalityLookup.SortOrder : 0)
      .ThenBy(a => a.ReviewDate ?? DateTime.MaxValue)
      .ToListAsync(cancellationToken);

    var rows = list.Select(a =>
    {
      var review = a.ReviewDate.HasValue
        ? a.ReviewDate.Value < today
          ? $"Overdue ({a.ReviewDate.Value:d MMM yyyy})"
          : a.ReviewDate.Value.ToString("d MMM yyyy")
        : "—";
      return new RaidReportRegisterRow(
        a.Id,
        $"ASM-{a.Id}",
        Truncate(a.Description, 120),
        [
          a.CriticalityLookup?.Label ?? "—",
          a.StatusLookup?.Label ?? "—",
          review
        ],
        nameof(ModernRaidController.AssumptionDetail));
    }).ToList();

    return new RaidReportTabPanel
    {
      Stats =
      [
        new("Open assumptions", openCount.ToString(), "dfe-f-stat-card--tint-grey"),
        new("Review overdue", reviewOverdue.ToString(), reviewOverdue > 0 ? "dfe-f-stat-card--tint-red" : "dfe-f-stat-card--tint-grey"),
        new("No review date", noReviewDate.ToString(), "dfe-f-stat-card--tint-grey"),
        new("High criticality", highCriticality.ToString(), highCriticality > 0 ? "dfe-f-stat-card--tint-red" : "dfe-f-stat-card--tint-grey")
      ],
      TableHeaders = ["Criticality", "Status", "Review date"],
      Rows = rows
    };
  }

  private IQueryable<NearMiss> OpenNearMissQuery(int? businessAreaId, int? directorateId)
  {
    var query = _db.NearMisses.AsNoTracking().Where(n => !n.IsDeleted);
    if (businessAreaId is { } baid)
      query = query.Where(n => n.BusinessAreaLookupId == baid);
    if (directorateId is { } did)
      query = query.Where(n => n.DirectorateLookupId == did);

    var closedIds = _db.NearMissStatuses.AsNoTracking()
      .Where(s => s.IsActive && s.Code == "CLOSED")
      .Select(s => s.Id);
    return query.Where(n => !n.NearMissStatusId.HasValue || !closedIds.Contains(n.NearMissStatusId.Value));
  }

  private IQueryable<Dependency> ActiveDependencyQuery(int? businessAreaId, int? directorateId)
  {
    var query = _db.Dependencies.AsNoTracking()
      .Where(d => d.Status == "Active" || (d.Status != "Resolved" && d.Status != "Cancelled" && d.ResolvedDate == null));

    if (businessAreaId == null && directorateId == null)
      return query;

    var scopedRiskIds = _db.Risks.AsNoTracking()
      .Where(r => !r.IsDeleted && r.ClosedDate == null);
    scopedRiskIds = ApplyRiskScope(scopedRiskIds, businessAreaId, directorateId);
    var riskIds = scopedRiskIds.Select(r => r.Id);

    var scopedIssueIds = _db.Issues.AsNoTracking()
      .Where(i => !i.IsDeleted && i.ClosedDate == null);
    scopedIssueIds = ApplyIssueScope(scopedIssueIds, businessAreaId, directorateId);
    var issueIds = scopedIssueIds.Select(i => i.Id);

    var scopedProjectIds = _db.Projects.AsNoTracking()
      .Where(p => !p.IsDeleted && p.Status != "Cancelled" && p.Status != "Completed");
    if (businessAreaId is { } baid)
      scopedProjectIds = scopedProjectIds.Where(p => p.BusinessAreaId == baid);
    if (directorateId is { } did)
      scopedProjectIds = scopedProjectIds.Where(p => p.Directorates.Any(d => d.DivisionId == did));
    var projectIds = scopedProjectIds.Select(p => p.Id);

    return query.Where(d =>
      (d.SourceEntityType == "Risk" && riskIds.Contains(d.SourceEntityId))
      || (d.TargetEntityType == "Risk" && riskIds.Contains(d.TargetEntityId))
      || (d.SourceEntityType == "Issue" && issueIds.Contains(d.SourceEntityId))
      || (d.TargetEntityType == "Issue" && issueIds.Contains(d.TargetEntityId))
      || (d.SourceEntityType == "Project" && projectIds.Contains(d.SourceEntityId))
      || (d.TargetEntityType == "Project" && projectIds.Contains(d.TargetEntityId)));
  }

  private static string NormalizeTab(string? tab) =>
    tab?.Trim().ToLowerInvariant() switch
    {
      "issues" => "issues",
      "near-misses" or "nearmisses" or "near_misses" => "near-misses",
      "dependencies" => "dependencies",
      "assumptions" => "assumptions",
      _ => "risks"
    };

  private static IQueryable<Risk> ApplyRiskScope(IQueryable<Risk> query, int? businessAreaId, int? directorateId)
  {
    if (businessAreaId is { } baid)
    {
      query = query.Where(r =>
        r.RiskBusinessAreas.Any(b => b.BusinessAreaLookupId == baid)
        || (r.Project != null && r.Project.BusinessAreaId == baid));
    }

    if (directorateId is { } did)
      query = query.Where(r => r.RiskDivisions.Any(d => d.DivisionId == did));

    return query;
  }

  private static IQueryable<Issue> ApplyIssueScope(IQueryable<Issue> query, int? businessAreaId, int? directorateId)
  {
    if (businessAreaId is { } baid)
    {
      query = query.Where(i =>
        i.IssueBusinessAreas.Any(b => b.BusinessAreaLookupId == baid)
        || (i.Project != null && i.Project.BusinessAreaId == baid));
    }

    if (directorateId is { } did)
      query = query.Where(i => i.IssueDivisions.Any(d => d.DivisionId == did));

    return query;
  }

  private static IQueryable<Assumption> ApplyAssumptionScope(
    IQueryable<Assumption> query,
    int? businessAreaId,
    int? directorateId)
  {
    if (businessAreaId is { } baid)
    {
      query = query.Where(a =>
        a.AssumptionBusinessAreas.Any(b => b.BusinessAreaLookupId == baid)
        || (a.Project != null && a.Project.BusinessAreaId == baid));
    }

    if (directorateId is { } did)
      query = query.Where(a => a.AssumptionDivisions.Any(d => d.DivisionId == did));

    return query;
  }

  private static string Truncate(string? text, int max)
  {
    if (string.IsNullOrWhiteSpace(text)) return "—";
    var t = text.Trim();
    return t.Length <= max ? t : t[..(max - 1)] + "…";
  }

  private async Task<(string Summary, IReadOnlyList<RaidReportFilterSelectOption> Ba, IReadOnlyList<RaidReportFilterSelectOption> Dir)>
    LoadFilterOptionsAsync(int? businessAreaId, int? directorateId, CancellationToken cancellationToken)
  {
    var baOptions = await _db.BusinessAreaLookups.AsNoTracking()
      .Where(ba => ba.IsActive)
      .OrderBy(ba => ba.SortOrder).ThenBy(ba => ba.Name)
      .Select(ba => new RaidReportFilterSelectOption(ba.Id, ba.Name))
      .ToListAsync(cancellationToken);

    var dirOptions = await _db.Divisions.AsNoTracking()
      .Where(d => d.IsActive)
      .OrderBy(d => d.SortOrder).ThenBy(d => d.Name)
      .Select(d => new RaidReportFilterSelectOption(d.Id, d.Name))
      .ToListAsync(cancellationToken);

    var selBaName = businessAreaId is { } baid
      ? baOptions.FirstOrDefault(x => x.Id == baid)?.Name
      : null;
    var selDirName = directorateId is { } did
      ? dirOptions.FirstOrDefault(x => x.Id == did)?.Name
      : null;

    var summary = (selBaName, selDirName) switch
    {
      (null, null) => "All business areas · All directorates",
      (not null, null) => $"{selBaName} · All directorates",
      (null, not null) => $"All business areas · {selDirName}",
      (not null, not null) => $"{selBaName} · {selDirName}"
    };

    return (summary, baOptions, dirOptions);
  }
}
