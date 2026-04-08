using System.Globalization;
using Compass.Data;
using Compass.Models;
using Compass.Services;
using Compass.Models.Modern.Work;
using Compass.ViewModels.Dashboard;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Compass.Services.Modern;

public partial class ModernWorkService : IModernWorkService
{
    private readonly CompassDbContext _db;
    private readonly IMonthlyUpdateService _monthlyUpdateService;

    public ModernWorkService(CompassDbContext db, IMonthlyUpdateService monthlyUpdateService)
    {
        _db = db;
        _monthlyUpdateService = monthlyUpdateService;
    }

    private static string FormatSroShortName(User? u)
    {
        if (u == null) return "";
        var fn = (u.FirstName ?? "").Trim();
        var ln = (u.LastName ?? "").Trim();
        if (fn.Length > 0 && ln.Length > 0)
            return fn[0] + ". " + ln;
        if (!string.IsNullOrWhiteSpace(u.Name))
            return u.Name.Trim();
        return u.Email ?? "";
    }

    private static string? ComputeMonthlyUpdateFilterKey(string? status, DateTime nowDate, DateTime? periodDueDate)
    {
        if (string.IsNullOrWhiteSpace(status) || status == "—") return "";
        if (status.Contains("late", StringComparison.OrdinalIgnoreCase)) return "overdue";
        // "Complete return due by …" is only shown while the return window is open (see BuildWorkRegisterAsync).
        if (status.Contains("Complete return due", StringComparison.OrdinalIgnoreCase)) return "due-today";
        if (status.Contains("Submitted", StringComparison.OrdinalIgnoreCase) && !status.Contains("not submitted", StringComparison.OrdinalIgnoreCase)) return "submitted";
        if (status.Contains("not submitted", StringComparison.OrdinalIgnoreCase)) return "not-required";
        return "";
    }

    private static string EmailLower(string email) => email.Trim().ToLowerInvariant();

    // IQueryable predicate so EF translates; do not replace with a static bool(Project, ...) inside Where().
    private static IQueryable<Project> WhereAssignedToUser(IQueryable<Project> query, string emailLower) =>
        query.Where(p =>
            p.ProjectContacts.Any(pc => pc.Email.ToLower() == emailLower) ||
            (p.PrimaryContactUser != null && p.PrimaryContactUser.Email.ToLower() == emailLower) ||
            p.SeniorResponsibleOfficers.Any(sro => sro.User != null && sro.User.Email.ToLower() == emailLower) ||
            p.ServiceOwners.Any(so => so.User != null && so.User.Email.ToLower() == emailLower) ||
            p.PmoContacts.Any(pmo => pmo.User != null && pmo.User.Email.ToLower() == emailLower));

    private static WorkItem MapProjectToWorkItem(Project p)
    {
        var w = new WorkItem
        {
            Id = p.Id,
            LegacyProjectId = p.Id,
            Title = p.Title,
            Status = p.Status ?? "Active",
            FlagshipProject = p.IsFlagship,
            PortfolioId = p.PrimaryOrganizationalGroupId,
            DeliveryPhaseId = p.PhaseId,
            PriorityId = p.DeliveryPriorityId,
            RagStatusId = p.RagStatusLookupId,
            PrimaryContactUserId = p.PrimaryContactUserId,
            SubjectToSpendControl = p.IsSubjectToSpendControl == true,
            RiskAppetiteId = p.RiskAppetiteLookupId,
            StartDate = p.StartDate,
            TargetEndDate = p.TargetDeliveryDate,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt,
            CreatedBy = null,
            UpdatedBy = null,
            ProblemStatement = null,
            Aim = p.Aim,
            Description = null
        };

        foreach (var rh in p.RagHistory.OrderByDescending(x => x.ChangedAt))
        {
            w.RagHistory.Add(new WorkItemRagHistory
            {
                Id = rh.Id,
                WorkItemId = p.Id,
                RagStatusId = rh.RagStatusLookupId ?? 0,
                Justification = rh.Justification,
                PathToGreen = rh.PathToGreen,
                UpdatedAt = rh.ChangedAt,
                RagStatus = rh.RagStatusLookup == null
                    ? null
                    : new RagStatus
                    {
                        Id = rh.RagStatusLookup.Id,
                        Name = rh.RagStatusLookup.Name ?? "",
                        BackgroundColourKey = null,
                        TextColourKey = null
                    }
            });
        }

        foreach (var mu in p.MonthlyUpdates)
        {
            w.MonthlyUpdates.Add(new MonthlyUpdate
            {
                Id = mu.Id,
                WorkItemId = p.Id,
                ReportMonth = new DateTime(mu.Year, mu.Month, 1, 0, 0, 0, DateTimeKind.Utc),
                Narrative = mu.Narrative,
                SubmittedAt = mu.SubmittedAt,
                SubmittedByUserId = mu.CreatedByUserId,
                SubmittedBy = mu.CreatedByName ?? mu.CreatedByUser?.Name
            });
        }

        foreach (var m in p.Milestones.Where(x => !x.IsDeleted))
            w.Milestones.Add(m);

        if (p.Directorates != null)
        {
            foreach (var pd in p.Directorates)
            {
                w.Directorates.Add(new WorkItemDirectorate
                {
                    Id = pd.Id,
                    DirectorateId = pd.DivisionId,
                    Division = pd.Division
                });
            }
        }

        return w;
    }

    /// <summary>Populate mission pillar and priority outcome tags from loaded <see cref="Project"/> navigation collections.</summary>
    private static void ApplyStrategicAlignmentFromProject(Project p, WorkItem w)
    {
        w.PriorityOutcomes.Clear();
        foreach (var po in p.ProjectObjectives)
        {
            if (po.Objective == null) continue;
            w.PriorityOutcomes.Add(new WorkItemPriorityOutcome
            {
                Id = po.Id,
                PriorityOutcomeId = po.ObjectiveId,
                PriorityOutcome = new LookupOption
                {
                    Id = po.Objective.Id,
                    Name = po.Objective.Title,
                    Value = po.Objective.Title
                }
            });
        }

        w.MissionPillars.Clear();
        foreach (var pm in p.ProjectMissions)
        {
            if (pm.Mission == null) continue;
            w.MissionPillars.Add(new WorkItemMissionPillar
            {
                Id = pm.Id,
                MissionPillarId = pm.MissionId,
                MissionPillar = new LookupOption
                {
                    Id = pm.Mission.Id,
                    Name = pm.Mission.Title,
                    Value = pm.Mission.Title
                }
            });
        }
    }

    public async Task<WorkItem?> GetWorkItemAsync(int projectId, CancellationToken cancellationToken = default)
    {
        var p = await _db.Projects
            .AsNoTracking()
            .Where(x => x.Id == projectId && !x.IsDeleted)
            .Include(x => x.RagHistory).ThenInclude(r => r.RagStatusLookup)
            .Include(x => x.MonthlyUpdates).ThenInclude(mu => mu.CreatedByUser)
            .Include(x => x.Milestones)
            .Include(x => x.RagStatusLookup)
            .Include(x => x.PhaseLookup)
            .Include(x => x.DeliveryPriority)
            .Include(x => x.PrimaryOrganizationalGroup)
            .FirstOrDefaultAsync(cancellationToken);

        return p == null ? null : MapProjectToWorkItem(p);
    }

    public async Task PopulateWorkDashboardAsync(Controller controller, User currentUser, string userEmail, string? tab, CancellationToken cancellationToken = default)
    {
        var emailLower = EmailLower(userEmail);
        var now = DateTime.UtcNow;
        var startOfThisMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var nextMonth = startOfThisMonth.AddMonths(1);
        var in14Days = now.AddDays(14);

        var assignedProjects = await WhereAssignedToUser(
                _db.Projects.AsNoTracking().Where(p => !p.IsDeleted),
                emailLower)
            .Include(p => p.RagHistory).ThenInclude(r => r.RagStatusLookup)
            .Include(p => p.MonthlyUpdates)
            .Include(p => p.Milestones)
            .Include(p => p.RagStatusLookup)
            .Include(p => p.PhaseLookup)
            .Include(p => p.DeliveryPriority)
            .Include(p => p.PrimaryOrganizationalGroup)
            .OrderByDescending(p => p.UpdatedAt)
            .ToListAsync(cancellationToken);

        var assignedWorkAll = assignedProjects.Select(MapProjectToWorkItem).ToList();

        var assignedActivePaused = assignedWorkAll
            .Where(w => w.Status == "Active" || w.Status == "Paused")
            .OrderByDescending(w => w.UpdatedAt)
            .ToList();
        var assignedCompleted = assignedWorkAll.Where(w => w.Status == "Completed").OrderByDescending(w => w.UpdatedAt).ToList();
        var assignedCancelled = assignedWorkAll.Where(w => w.Status == "Cancelled").OrderByDescending(w => w.UpdatedAt).ToList();

        var assignedActivePausedIds = assignedActivePaused.Select(w => w.Id).ToHashSet();

        var portfolioNames = assignedProjects
            .Where(p => p.PrimaryOrganizationalGroupId.HasValue && p.PrimaryOrganizationalGroup != null)
            .GroupBy(p => p.PrimaryOrganizationalGroupId!.Value)
            .ToDictionary(g => g.Key, g => g.First().PrimaryOrganizationalGroup!.Name);

        var phaseLookups = await _db.PhaseLookups.AsNoTracking().Where(p => p.IsActive).ToDictionaryAsync(p => p.Id, p => p.Name, cancellationToken);
        var deliveryPhaseNames = assignedProjects
            .Where(p => p.PhaseId.HasValue)
            .Select(p => p.PhaseId!.Value)
            .Distinct()
            .ToDictionary(id => id, id => phaseLookups.GetValueOrDefault(id) ?? "—");

        var priorities = await _db.DeliveryPriorities.AsNoTracking().ToDictionaryAsync(p => p.Id, p => p.Name, cancellationToken);
        var priorityNameById = priorities.ToDictionary(kv => kv.Key, kv => kv.Value);

        int ragRed = 0, ragAmber = 0, ragGreen = 0;
        foreach (var w in assignedActivePaused)
        {
            var latestRag = w.RagHistory.OrderByDescending(r => r.UpdatedAt).FirstOrDefault()?.RagStatus?.Name;
            if (string.IsNullOrEmpty(latestRag)) continue;
            if (latestRag.Contains("Red", StringComparison.OrdinalIgnoreCase)) ragRed++;
            else if (latestRag.Contains("Amber", StringComparison.OrdinalIgnoreCase)) ragAmber++;
            else if (latestRag.Contains("Green", StringComparison.OrdinalIgnoreCase)) ragGreen++;
        }

        var highPriorityIds = await _db.DeliveryPriorities.AsNoTracking()
            .Where(p => p.Name != null && (p.Name.ToLower().Contains("high") || p.Name.ToLower().Contains("critical")))
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);
        var highPriorityCount = assignedActivePaused.Count(w => w.PriorityId.HasValue && highPriorityIds.Contains(w.PriorityId.Value));
        var flagshipCount = assignedActivePaused.Count(w => w.FlagshipProject);

        var watchedIds = await _db.ProjectWatchlists.AsNoTracking()
            .Where(w => w.UserId == currentUser.Id)
            .Select(w => w.ProjectId)
            .ToListAsync(cancellationToken);

        var watchedProjects = watchedIds.Count == 0
            ? new List<Project>()
            : await _db.Projects.AsNoTracking()
                .Where(p => watchedIds.Contains(p.Id))
                .Include(p => p.RagHistory).ThenInclude(r => r.RagStatusLookup)
                .OrderByDescending(p => p.UpdatedAt)
                .ToListAsync(cancellationToken);
        var watchedItems = watchedProjects.Select(MapProjectToWorkItem).ToList();

        var workIdsWithCurrentMonthUpdate = await _db.ProjectMonthlyUpdates.AsNoTracking()
            .Where(m => m.Year == startOfThisMonth.Year && m.Month == startOfThisMonth.Month && m.SubmittedAt != null)
            .Select(m => m.ProjectId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var reportsDueProjects = await _db.Projects
            .AsNoTracking()
            .Where(p => !p.IsDeleted && p.Status == "Active" && !workIdsWithCurrentMonthUpdate.Contains(p.Id))
            .Include(p => p.RagHistory).ThenInclude(r => r.RagStatusLookup)
            .Include(p => p.MonthlyUpdates)
            .Include(p => p.Milestones)
            .Include(p => p.RagStatusLookup)
            .Include(p => p.PhaseLookup)
            .Include(p => p.DeliveryPriority)
            .Include(p => p.PrimaryOrganizationalGroup)
            .OrderBy(p => p.Title)
            .ToListAsync(cancellationToken);
        var reportsDue = reportsDueProjects.Select(MapProjectToWorkItem).ToList();

        var ragIssues = assignedActivePaused
            .Where(w =>
            {
                var n = w.RagHistory.OrderByDescending(r => r.UpdatedAt).FirstOrDefault()?.RagStatus?.Name;
                return !string.IsNullOrEmpty(n) && !n.Contains("Green", StringComparison.OrdinalIgnoreCase);
            })
            .ToList();

        var milestonesUpcomingAll = assignedProjects
            .SelectMany(p => p.Milestones.Where(m => !m.IsDeleted && m.DueDate >= now && m.DueDate <= in14Days)
                .Select(m => (p, m)))
            .OrderBy(x => x.m.DueDate)
            .ToList();
        var milestonesLateAll = assignedProjects
            .SelectMany(p => p.Milestones.Where(m => !m.IsDeleted && m.DueDate < now && m.Status != "complete")
                .Select(m => (p, m)))
            .OrderBy(x => x.m.DueDate)
            .ToList();

        var milestonesUpcoming = milestonesUpcomingAll
            .Where(x => assignedActivePausedIds.Contains(x.p.Id))
            .Select(x => new WorkMilestoneRow
            {
                WorkItemId = x.p.Id,
                Title = x.m.Name,
                DueDate = x.m.DueDate,
                Status = x.m.Status,
                WorkItem = MapProjectToWorkItem(x.p)
            })
            .ToList();

        var milestonesLate = milestonesLateAll
            .Where(x => assignedActivePausedIds.Contains(x.p.Id))
            .Select(x => new WorkMilestoneRow
            {
                WorkItemId = x.p.Id,
                Title = x.m.Name,
                DueDate = x.m.DueDate,
                Status = x.m.Status,
                WorkItem = MapProjectToWorkItem(x.p)
            })
            .ToList();

        var mrPeriodLabel = now.ToString("MMMM yyyy", CultureInfo.GetCultureInfo("en-GB"));
        DateTime? mrDueDate = null;
        var monthlyUpdateRows = new List<HomeMonthlyUpdateRow>();
        var mrSubmitted = 0;
        var mrDraft = 0;
        var mrMissing = 0;

        foreach (var w in assignedActivePaused.Where(x => x.Status == "Active").OrderBy(x => x.Title))
        {
            var p = assignedProjects.First(ap => ap.Id == w.Id);
            var periodUpdate = p.MonthlyUpdates.FirstOrDefault(m => m.Year == startOfThisMonth.Year && m.Month == startOfThisMonth.Month);
            var submitted = periodUpdate?.SubmittedAt.HasValue == true;
            var draft = periodUpdate != null && periodUpdate.SubmittedAt == null;
            if (submitted) mrSubmitted++;
            else if (draft) mrDraft++;
            else mrMissing++;

            var portName = p.PrimaryOrganizationalGroup?.Name ?? "—";
            var ragNm = w.RagHistory.OrderByDescending(r => r.UpdatedAt).FirstOrDefault()?.RagStatus?.Name;

            if (submitted)
            {
                monthlyUpdateRows.Add(new HomeMonthlyUpdateRow
                {
                    WorkItemId = w.Id,
                    WorkTitle = w.Title,
                    PeriodLabel = mrPeriodLabel,
                    DueDate = mrDueDate,
                    StatusLabel = "Submitted",
                    ActionLabel = "View update",
                    ActionUrl = $"/modern/work/detail/{w.Id}#wd-updates",
                    PortfolioName = portName,
                    RagStatusName = ragNm
                });
            }
            else
            {
                monthlyUpdateRows.Add(new HomeMonthlyUpdateRow
                {
                    WorkItemId = w.Id,
                    WorkTitle = w.Title,
                    PeriodLabel = mrPeriodLabel,
                    DueDate = mrDueDate,
                    StatusLabel = draft ? "Draft" : "Not started",
                    ActionLabel = draft ? "Continue update" : "Add update",
                    ActionUrl = $"/modern/work/detail/{w.Id}#wd-updates",
                    PortfolioName = portName,
                    RagStatusName = ragNm
                });
            }
        }

        var reportingGapCount = mrDraft + mrMissing;
        var workDashCalloutHeading = $"Monthly reporting — {mrPeriodLabel}";
        string? workDashCalloutBody = null;
        if (reportingGapCount > 0)
        {
            var parts = new List<string>();
            if (mrMissing > 0) parts.Add($"{mrMissing} not started");
            if (mrDraft > 0) parts.Add($"{mrDraft} in progress");
            workDashCalloutBody = string.Join(", ", parts) + ".";
        }

        var ragOverviewRows = new List<WorkDashboardRagOverviewRow>();
        foreach (var w in assignedActivePaused.OrderBy(w => w.Title))
        {
            var p = assignedProjects.First(ap => ap.Id == w.Id);
            var latestRagH = w.RagHistory.OrderByDescending(r => r.UpdatedAt).FirstOrDefault();
            var phaseNm = p.PhaseLookup?.Name ?? "—";
            var latestSub = p.MonthlyUpdates.Where(m => m.SubmittedAt.HasValue).OrderByDescending(m => m.Year).ThenByDescending(m => m.Month).FirstOrDefault();
            var raw = latestSub?.Narrative;
            var commentary = string.IsNullOrWhiteSpace(raw) ? "—" : raw.Length > 220 ? raw[..217] + "…" : raw;
            var pn = p.PrimaryOrganizationalGroup?.Name ?? "—";
            ragOverviewRows.Add(new WorkDashboardRagOverviewRow
            {
                WorkItemId = w.Id,
                Title = w.Title,
                PortfolioName = pn,
                PhaseDisplayName = string.IsNullOrWhiteSpace(phaseNm) ? "—" : phaseNm,
                PhaseCssSuffix = PhaseCssSuffix(phaseNm),
                LatestRag = latestRagH,
                Commentary = commentary ?? "—"
            });
        }

        var workTab = (tab?.ToLowerInvariant()) switch
        {
            "my" => "assigned",
            "reporting" => "reporting",
            "assigned" => "assigned",
            "completed" => "completed",
            "cancelled" => "cancelled",
            "rag" => "rag",
            "rag-issues" => "rag-issues",
            "watching" => "watching",
            "milestones" => "milestones",
            _ => "assigned"
        };

        controller.ViewBag.MainNavSection = "work";
        controller.ViewBag.SubNavItem = "work-dashboard";
        controller.ViewBag.WorkDashboardTab = workTab;
        controller.ViewBag.AssignedCount = assignedActivePaused.Count;
        controller.ViewBag.CompletedAssignedCount = assignedCompleted.Count;
        controller.ViewBag.CancelledAssignedCount = assignedCancelled.Count;
        controller.ViewBag.RagRed = ragRed;
        controller.ViewBag.RagAmber = ragAmber;
        controller.ViewBag.RagGreen = ragGreen;
        controller.ViewBag.HighPriorityCount = highPriorityCount;
        controller.ViewBag.FlagshipCount = flagshipCount;
        controller.ViewBag.AssignedWork = assignedActivePaused;
        controller.ViewBag.AssignedCompletedWork = assignedCompleted;
        controller.ViewBag.AssignedCancelledWork = assignedCancelled;
        controller.ViewBag.WatchedItems = watchedItems;
        controller.ViewBag.WatchingTabCount = watchedItems.Count;
        controller.ViewBag.ReportsDue = reportsDue;
        controller.ViewBag.RagIssues = ragIssues;
        controller.ViewBag.RagIssuesAssigned = ragIssues;
        controller.ViewBag.RagNotGreenTabCount = ragIssues.Count;
        controller.ViewBag.MilestonesUpcoming = milestonesUpcoming;
        controller.ViewBag.MilestonesLate = milestonesLate;
        controller.ViewBag.MilestonesTabCount = milestonesUpcoming.Count + milestonesLate.Count;
        controller.ViewBag.PortfolioNames = portfolioNames;
        controller.ViewBag.WorkDashDeliveryPhaseNames = deliveryPhaseNames;
        controller.ViewBag.WorkDashPriorityNameById = priorityNameById;
        controller.ViewBag.WdMrRows = monthlyUpdateRows;
        controller.ViewBag.WdMrPeriodLabel = mrPeriodLabel;
        controller.ViewBag.WdMrDueDate = mrDueDate;
        controller.ViewBag.WdMrDaysRemaining = null;
        controller.ViewBag.WdMrWindowOpen = false;
        controller.ViewBag.WdMrSubmitted = mrSubmitted;
        controller.ViewBag.WdMrDraft = mrDraft;
        controller.ViewBag.WdMrMissing = mrMissing;
        controller.ViewBag.WdMrActiveAssignedCount = assignedActivePaused.Count(w => w.Status == "Active");
        controller.ViewBag.WdMrExtendedReportingTable = true;
        controller.ViewBag.ReportingTabCount = reportingGapCount;
        controller.ViewBag.WorkDashCalloutHeading = workDashCalloutHeading;
        controller.ViewBag.WorkDashCalloutBody = workDashCalloutBody;
        controller.ViewBag.WorkDashCalloutClass = "dfe-c-callout dfe-c-callout--warning dfe-c-mb-3";
        controller.ViewBag.WorkDashShowCallout = true;
        controller.ViewBag.WorkDashRagOverviewRows = ragOverviewRows;
        controller.ViewBag.CurrentPeriodLabel = mrPeriodLabel;
        controller.ViewBag.CurrentPeriodDueDate = mrDueDate;
    }

    private static string PhaseCssSuffix(string? phaseName)
    {
        if (string.IsNullOrWhiteSpace(phaseName)) return "";
        var s = phaseName.Trim().ToLowerInvariant();
        if (s.Contains("public") && s.Contains("beta")) return "public-beta";
        if (s.Contains("private") && s.Contains("beta")) return "private-beta";
        if (s.Contains("live")) return "live";
        if (s.Contains("discovery")) return "discovery";
        if (s.Contains("alpha")) return "alpha";
        if (s.Contains("retired")) return "retired";
        if (s.Contains("explore")) return "explore";
        return "";
    }

    public async Task<WorkRegisterViewModel> BuildWorkRegisterAsync(
        bool isMyWork,
        string? search,
        int? portfolioId,
        int? directorateId,
        int? phaseId,
        int? ragId,
        int? priorityId,
        string? monthlyUpdate,
        User currentUser,
        string userEmail,
        IUrlHelper url,
        CancellationToken cancellationToken = default)
    {
        var emailLower = EmailLower(userEmail);
        var q = _db.Projects.AsNoTracking().Where(p => !p.IsDeleted);

        if (isMyWork)
            q = WhereAssignedToUser(q, emailLower);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(p => p.Title.Contains(s) || (p.Aim != null && p.Aim.Contains(s)));
        }

        if (portfolioId.HasValue)
            q = q.Where(p => p.PrimaryOrganizationalGroupId == portfolioId.Value);

        if (directorateId.HasValue)
            q = q.Where(p => p.Directorates.Any(d => d.DivisionId == directorateId.Value));

        if (phaseId.HasValue)
            q = q.Where(p => p.PhaseId == phaseId.Value);

        if (ragId.HasValue)
        {
            var ragName = await _db.RagStatusLookups.AsNoTracking()
                .Where(r => r.Id == ragId.Value)
                .Select(r => r.Name)
                .FirstOrDefaultAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(ragName))
            {
                var ragNorm = ragName.Trim().ToLowerInvariant();
                q = q.Where(p =>
                    p.RagStatusLookupId == ragId.Value
                    || (p.RagStatusLookupId == null && p.RagStatus != null && p.RagStatus.ToLower() == ragNorm));
            }
            else
                q = q.Where(p => p.RagStatusLookupId == ragId.Value);
        }

        if (priorityId.HasValue)
            q = q.Where(p => p.DeliveryPriorityId == priorityId.Value);

        var items = await q
            .AsSplitQuery()
            .Include(p => p.RagStatusLookup)
            .Include(p => p.PhaseLookup)
            .Include(p => p.DeliveryPriority)
            .Include(p => p.PrimaryOrganizationalGroup)
            .Include(p => p.PrimaryContactUser)
            .Include(p => p.Directorates).ThenInclude(d => d.Division)
            .Include(p => p.Milestones)
            .Include(p => p.MonthlyUpdates)
            .Include(p => p.SeniorResponsibleOfficers).ThenInclude(s => s.User)
            .OrderByDescending(p => p.UpdatedAt)
            .ToListAsync(cancellationToken);

        var projectIds = items.Select(p => p.Id).ToList();
        var firstRiskByProject = new Dictionary<int, (int Id, string Reference)>();
        if (projectIds.Count > 0)
        {
            var riskRows = await _db.Risks.AsNoTracking()
                .Where(r => r.ProjectId.HasValue && projectIds.Contains(r.ProjectId.Value) && !r.IsDeleted && r.Status != "closed")
                .OrderBy(r => r.Id)
                .Select(r => new { ProjectId = r.ProjectId!.Value, r.Id, r.FipsId })
                .ToListAsync(cancellationToken);
            foreach (var x in riskRows)
            {
                if (firstRiskByProject.ContainsKey(x.ProjectId)) continue;
                var reference = !string.IsNullOrWhiteSpace(x.FipsId) ? x.FipsId : "RISK-" + x.Id.ToString("D5", CultureInfo.InvariantCulture);
                firstRiskByProject[x.ProjectId] = (x.Id, reference);
            }
        }

        var nowDate = DateTime.UtcNow.Date;
        var reportY = nowDate.Year;
        var reportM = nowDate.Month;
        var currentDueDate = _monthlyUpdateService.GetMonthlyUpdateDueDate(reportY, reportM);
        var currentPeriodLabel = new DateTime(reportY, reportM, 1).ToString("MMMM", CultureInfo.GetCultureInfo("en-GB"));
        var prevMonthDate = nowDate.AddMonths(-1);
        var prevMonthLabel = prevMonthDate.ToString("MMM", CultureInfo.InvariantCulture);

        var orgGroups = await _db.OrganizationalGroups.AsNoTracking().Where(g => g.IsActive).OrderBy(g => g.Name).ToListAsync(cancellationToken);
        var portfolios = orgGroups.Select(g => new Portfolio { Id = g.Id, Name = g.Name, IsActive = true }).ToList();

        var directorates = await _db.Divisions.AsNoTracking().Where(d => d.IsActive).OrderBy(d => d.Name)
            .Select(d => new Directorate { Id = d.Id, Name = d.Name, IsActive = true }).ToListAsync(cancellationToken);

        var phaseOpts = await _db.PhaseLookups.AsNoTracking().Where(p => p.IsActive)
            .OrderBy(p => p.SortOrder)
            .Select(p => new WorkLookupOption { Id = p.Id, Name = p.Name, Value = p.Name })
            .ToListAsync(cancellationToken);

        var ragOpts = await _db.RagStatusLookups.AsNoTracking().Where(r => r.IsActive).OrderBy(r => r.SortOrder)
            .Select(r => new RagStatusLookupOption { Id = r.Id, Name = r.Name })
            .ToListAsync(cancellationToken);

        var priOpts = await _db.DeliveryPriorities.AsNoTracking()
            .OrderBy(p => p.SortOrder)
            .Select(p => new WorkLookupOption { Id = p.Id, Name = p.Name, Value = p.Name })
            .ToListAsync(cancellationToken);

        var redRag = await _db.RagStatusLookups.AsNoTracking()
            .Where(r => r.Name != null && r.Name.ToLower() == "red")
            .Select(r => r.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var activePaused = new List<WorkRegisterRow>();
        var completed = new List<WorkRegisterRow>();
        var cancelled = new List<WorkRegisterRow>();

        foreach (var p in items)
        {
            var row = new WorkRegisterRow
            {
                Id = p.Id,
                Title = p.Title.Trim(),
                Status = p.Status ?? "—",
                PrimaryContactName = p.PrimaryContactUser?.Name ?? p.PrimaryContactUser?.Email ?? "—",
                PortfolioId = p.PrimaryOrganizationalGroupId,
                PortfolioName = p.PrimaryOrganizationalGroup?.Name ?? "—",
                PhaseName = p.PhaseLookup?.Name ?? "—",
                PriorityName = p.DeliveryPriority?.Name ?? "—",
                RagName = p.RagStatusLookup?.Name ?? "—",
                RagStatusId = p.RagStatusLookupId,
                MilestoneCount = p.Milestones.Count(m => !m.IsDeleted && !string.Equals(m.Status, "complete", StringComparison.OrdinalIgnoreCase)),
                MonthlyUpdateStatus = "—",
                DirectorateSummary = p.Directorates.Count == 0
                    ? null
                    : string.Join(", ", p.Directorates.Select(d => d.Division.Name).Where(n => !string.IsNullOrEmpty(n)).Distinct().OrderBy(n => n))
            };

            if (p.RagStatusLookup != null)
            {
                row.RagBackgroundColourKey = null;
                row.RagTextColourKey = null;
            }

            var sro = p.SeniorResponsibleOfficers.OrderBy(s => s.Id).FirstOrDefault();
            if (sro?.User != null)
            {
                var sroName = FormatSroShortName(sro.User);
                if (!string.IsNullOrWhiteSpace(sroName))
                    row.SroDisplayName = sroName;
            }

            if (firstRiskByProject.TryGetValue(p.Id, out var fr))
            {
                row.FirstRiskOrIssueId = fr.Id;
                row.FirstRiskReference = fr.Reference;
            }

            if (p.Status == "Active" || p.Status == "Paused")
            {
                var submittedMonths = new HashSet<(int Y, int M)>();
                foreach (var mu in p.MonthlyUpdates)
                {
                    if (mu.SubmittedAt.HasValue)
                        submittedMonths.Add((mu.Year, mu.Month));
                }

                var hasCurrent = submittedMonths.Contains((reportY, reportM));
                var hasPrev = submittedMonths.Contains((prevMonthDate.Year, prevMonthDate.Month));

                if (currentDueDate.Date < nowDate && !hasCurrent)
                {
                    row.MonthlyUpdateStatus = currentPeriodLabel + " return late";
                    row.MonthlyUpdateStatusLink = "wd-updates";
                }
                else if (!hasCurrent)
                {
                    var periodOpenDate = new DateTime(nowDate.Year, nowDate.Month, 20, 0, 0, 0, DateTimeKind.Utc).Date;
                    if (nowDate >= periodOpenDate && nowDate <= currentDueDate.Date)
                    {
                        row.MonthlyUpdateStatus = "Complete return due by " + currentDueDate.ToString("d MMMM", CultureInfo.GetCultureInfo("en-GB"));
                        row.MonthlyUpdateStatusLink = "add";
                    }
                    else
                    {
                        row.MonthlyUpdateStatus = prevMonthLabel + (hasPrev ? " Submitted" : " not submitted");
                    }
                }
                else
                {
                    row.MonthlyUpdateStatus = prevMonthLabel + (hasPrev ? " Submitted" : " not submitted");
                }

                row.MonthlyUpdateFilterKey = ComputeMonthlyUpdateFilterKey(row.MonthlyUpdateStatus, nowDate, currentDueDate);
                activePaused.Add(row);
            }
            else if (p.Status == "Completed")
            {
                row.CompletedAt = p.UpdatedAt.ToString("MMM yyyy", CultureInfo.InvariantCulture);
                completed.Add(row);
            }
            else if (p.Status == "Cancelled")
            {
                row.CompletedAt = p.UpdatedAt.ToString("MMM yyyy", CultureInfo.InvariantCulture);
                row.CancelledReason = p.StatusChangeReason;
                cancelled.Add(row);
            }
        }

        activePaused = activePaused.OrderBy(r => r.Title, StringComparer.OrdinalIgnoreCase).ToList();
        completed = completed.OrderBy(r => r.Title, StringComparer.OrdinalIgnoreCase).ToList();
        cancelled = cancelled.OrderBy(r => r.Title, StringComparer.OrdinalIgnoreCase).ToList();

        var activePausedCountBeforeMonthly = activePaused.Count;
        if (!string.IsNullOrWhiteSpace(monthlyUpdate))
        {
            var mu = monthlyUpdate.Trim().ToLowerInvariant();
            activePaused = activePaused.Where(r => string.Equals(r.MonthlyUpdateFilterKey, mu, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        var ragRedCount = redRag != 0 ? activePaused.Count(r => r.RagStatusId == redRag) : 0;

        return new WorkRegisterViewModel
        {
            PageTitle = isMyWork ? "My work" : "Work register",
            PageDescription = isMyWork ? "Work assigned to you" : "Active, paused and recently completed work items",
            IsMyWork = isMyWork,
            ActiveCount = activePaused.Count(r => r.Status == "Active"),
            PausedCount = activePaused.Count(r => r.Status == "Paused"),
            CompletedCount = completed.Count,
            CancelledCount = cancelled.Count,
            RagRedCount = ragRedCount,
            AwaitingMonthlyCount = 0,
            AwaitingMonthlyOverdue = false,
            ActivePausedCountBeforeMonthlyFilter = activePausedCountBeforeMonthly,
            ActivePaused = activePaused,
            Completed = completed,
            Cancelled = cancelled,
            Portfolios = portfolios,
            Directorates = directorates,
            DeliveryPhaseOptions = phaseOpts,
            RagOptions = ragOpts,
            PriorityOptions = priOpts,
            FilterSearch = search,
            FilterPortfolioId = portfolioId,
            FilterDirectorateId = directorateId,
            FilterPhaseId = phaseId,
            FilterRagId = ragId,
            FilterPriorityId = priorityId,
            FilterMonthlyUpdate = monthlyUpdate
        };
    }

    public async Task<List<WorkItem>> GetWatchingWorkItemsAsync(
        User currentUser,
        string? search,
        int? portfolioId,
        int? directorateId,
        string? status,
        CancellationToken cancellationToken = default)
    {
        var watchedIds = await _db.ProjectWatchlists.AsNoTracking()
            .Where(w => w.UserId == currentUser.Id)
            .Select(w => w.ProjectId)
            .ToListAsync(cancellationToken);
        if (watchedIds.Count == 0)
            return new List<WorkItem>();

        var q = _db.Projects.AsNoTracking().Where(p => !p.IsDeleted && watchedIds.Contains(p.Id));

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(p => p.Title.Contains(s) || (p.Aim != null && p.Aim.Contains(s)));
        }

        if (portfolioId.HasValue)
            q = q.Where(p => p.PrimaryOrganizationalGroupId == portfolioId.Value);
        if (directorateId.HasValue)
            q = q.Where(p => p.Directorates.Any(d => d.DivisionId == directorateId.Value));
        if (!string.IsNullOrWhiteSpace(status))
            q = q.Where(p => p.Status == status);

        var projects = await q
            .Include(p => p.RagHistory).ThenInclude(r => r.RagStatusLookup)
            .Include(p => p.Directorates).ThenInclude(d => d.Division)
            .OrderByDescending(p => p.UpdatedAt)
            .ToListAsync(cancellationToken);

        return projects.Select(MapProjectToWorkItem).ToList();
    }

    public async Task<List<WorkItem>> GetByPriorityWorkItemsAsync(
        string? search,
        int? portfolioId,
        int? directorateId,
        int? priorityId,
        string? status,
        CancellationToken cancellationToken = default)
    {
        var q = _db.Projects.AsNoTracking().Where(p => !p.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(p => p.Title.Contains(s) || (p.Aim != null && p.Aim.Contains(s)));
        }

        if (portfolioId.HasValue)
            q = q.Where(p => p.PrimaryOrganizationalGroupId == portfolioId.Value);
        if (directorateId.HasValue)
            q = q.Where(p => p.Directorates.Any(d => d.DivisionId == directorateId.Value));
        if (priorityId.HasValue)
            q = q.Where(p => p.DeliveryPriorityId == priorityId.Value);
        if (!string.IsNullOrWhiteSpace(status))
            q = q.Where(p => p.Status == status);

        var projects = await q
            .Include(p => p.RagHistory).ThenInclude(r => r.RagStatusLookup)
            .Include(p => p.Directorates).ThenInclude(d => d.Division)
            .Include(p => p.ProjectMissions).ThenInclude(pm => pm.Mission)
            .Include(p => p.ProjectObjectives).ThenInclude(po => po.Objective)
            .OrderByDescending(p => p.UpdatedAt)
            .ToListAsync(cancellationToken);

        return projects.Select(p =>
        {
            var w = MapProjectToWorkItem(p);
            ApplyStrategicAlignmentFromProject(p, w);
            return w;
        }).ToList();
    }

    public async Task<List<WorkItem>> GetFlagshipWorkItemsAsync(
        string? search,
        int? portfolioId,
        int? directorateId,
        string? status,
        CancellationToken cancellationToken = default)
    {
        var q = _db.Projects.AsNoTracking().Where(p => !p.IsDeleted && p.IsFlagship);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(p => p.Title.Contains(s) || (p.Aim != null && p.Aim.Contains(s)));
        }

        if (portfolioId.HasValue)
            q = q.Where(p => p.PrimaryOrganizationalGroupId == portfolioId.Value);
        if (directorateId.HasValue)
            q = q.Where(p => p.Directorates.Any(d => d.DivisionId == directorateId.Value));
        if (!string.IsNullOrWhiteSpace(status))
            q = q.Where(p => p.Status == status);

        var projects = await q
            .Include(p => p.RagHistory).ThenInclude(r => r.RagStatusLookup)
            .Include(p => p.Directorates).ThenInclude(d => d.Division)
            .OrderByDescending(p => p.UpdatedAt)
            .ToListAsync(cancellationToken);

        return projects.Select(MapProjectToWorkItem).ToList();
    }
}
