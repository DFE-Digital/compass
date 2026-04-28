using System.Globalization;
using System.Linq;
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
    private readonly IPermissionService _permissionService;
    private readonly IBusinessAreaAdminService _businessAreaAdmins;
    private readonly IBusinessAreaLeadershipService _businessAreaLeadership;
    private readonly IDirectorateLeadershipService _directorateLeadership;

    public ModernWorkService(
        CompassDbContext db,
        IMonthlyUpdateService monthlyUpdateService,
        IPermissionService permissionService,
        IBusinessAreaAdminService businessAreaAdmins,
        IBusinessAreaLeadershipService businessAreaLeadership,
        IDirectorateLeadershipService directorateLeadership)
    {
        _db = db;
        _monthlyUpdateService = monthlyUpdateService;
        _permissionService = permissionService;
        _businessAreaAdmins = businessAreaAdmins;
        _businessAreaLeadership = businessAreaLeadership;
        _directorateLeadership = directorateLeadership;
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

    /// <summary>Sets monthly reporting badge fields for the current calendar month (aligned with work dashboard).</summary>
    private void FillLatestMonthlyDueForRegisterRow(WorkRegisterRow row, Project p, DateTime nowDate, int reportY, int reportM, IUrlHelper url)
    {
        row.LatestMonthlyPeriodLabel = new DateTime(reportY, reportM, 1).ToString("MMMM yyyy", CultureInfo.GetCultureInfo("en-GB"));
        row.LatestMonthlyStatusLabel = null;
        row.LatestMonthlyActionLabel = null;

        if (p.Status != "Active" && p.Status != "Paused")
        {
            row.LatestMonthlyDueLabel = null;
            row.LatestMonthlyDueAction = "na";
            row.LatestMonthlyDueUrl = null;
            row.LatestMonthlyPeriodLabel = null;
            return;
        }

        row.LatestMonthlyDueLabel = new DateTime(reportY, reportM, 1).ToString("MMM", CultureInfo.GetCultureInfo("en-GB")) + " Update";

        var periodOpen = new DateTime(nowDate.Year, nowDate.Month, 20, 0, 0, 0, DateTimeKind.Utc).Date;
        var mu = p.MonthlyUpdates?.FirstOrDefault(m => m.Year == reportY && m.Month == reportM);
        var submitted = mu?.SubmittedAt != null;
        var draft = mu != null && mu.SubmittedAt == null;

        if (submitted)
        {
            row.LatestMonthlyDueAction = "view";
            row.LatestMonthlyDueUrl = url.Action("ViewMonthlyUpdate", "ModernWork", new { id = p.Id, updateId = mu!.Id });
            row.LatestMonthlyStatusLabel = "Submitted";
            row.LatestMonthlyActionLabel = "View";
            return;
        }

        if (nowDate < periodOpen)
        {
            row.LatestMonthlyDueAction = "not-due";
            row.LatestMonthlyDueUrl = null;
            row.LatestMonthlyStatusLabel = "Not due";
            row.LatestMonthlyActionLabel = "";
            return;
        }

        row.LatestMonthlyDueAction = "complete";
        row.LatestMonthlyDueUrl = url.Action("MonthlyReport", "ModernWork", new { id = p.Id, year = reportY, month = reportM });
        row.LatestMonthlyStatusLabel = draft ? "Draft" : "Not started";
        row.LatestMonthlyActionLabel = "Complete";
    }

    /// <summary>Maps a loaded <see cref="Project"/> to a register row, or <c>null</c> if status is not shown on the register.</summary>
    private WorkRegisterRow? BuildWorkRegisterRow(
        Project p,
        Dictionary<int, (int Id, string Reference)> firstRiskByProject,
        DateTime nowDate,
        int reportY,
        int reportM,
        DateTime prevMonthDate,
        string prevMonthLabel,
        string currentPeriodLabel,
        DateTime currentDueDate,
        IUrlHelper url)
    {
        var row = new WorkRegisterRow
        {
            Id = p.Id,
            UpdatedAtUtc = p.UpdatedAt,
            Title = p.Title.Trim(),
            Status = p.Status ?? "—",
            PrimaryContactName = p.PrimaryContactUser?.Name ?? p.PrimaryContactUser?.Email ?? "—",
            PortfolioId = p.PrimaryOrganizationalGroupId,
            PortfolioName = p.PrimaryOrganizationalGroup?.Name ?? "—",
            PhaseName = p.PhaseLookup?.Name ?? "—",
            PriorityName = p.DeliveryPriority?.Name ?? "—",
            RagName = p.RagStatusLookup?.Name ?? "—",
            RagCssClass = p.RagStatusLookup?.CssClass,
            RagStatusId = p.RagStatusLookupId,
            MilestoneCount = p.Milestones.Count(m => !m.IsDeleted && !string.Equals(m.Status, "complete", StringComparison.OrdinalIgnoreCase)),
            MonthlyUpdateStatus = "—",
            DirectorateSummary = p.Directorates.Count == 0
                ? null
                : string.Join(", ", p.Directorates.Select(d => d.Division.Name).Where(n => !string.IsNullOrEmpty(n)).Distinct().OrderBy(n => n)),
            BusinessAreaName = string.IsNullOrWhiteSpace(p.BusinessAreaLookup?.Name) ? null : p.BusinessAreaLookup.Name.Trim(),
            TagNamesSummary = p.ProjectWorkItemTags == null || p.ProjectWorkItemTags.Count == 0
                ? null
                : string.Join(", ", p.ProjectWorkItemTags
                    .Where(l => l.WorkItemTagLookup != null && l.WorkItemTagLookup.IsActive)
                    .OrderBy(l => l.WorkItemTagLookup!.SortOrder).ThenBy(l => l.WorkItemTagLookup!.Name)
                    .Select(l => l.WorkItemTagLookup!.Name))
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
        }
        else if (p.Status == "Completed")
        {
            row.CompletedAt = p.UpdatedAt.ToString("MMM yyyy", CultureInfo.InvariantCulture);
        }
        else if (p.Status == "Cancelled")
        {
            row.CompletedAt = p.UpdatedAt.ToString("MMM yyyy", CultureInfo.InvariantCulture);
            row.CancelledReason = p.StatusChangeReason;
        }
        else
        {
            return null;
        }

        FillLatestMonthlyDueForRegisterRow(row, p, nowDate, reportY, reportM, url);
        return row;
    }

    private async Task<List<Project>> LoadProjectsForWorkRegisterByIdsAsync(IReadOnlyList<int> ids, CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
            return new List<Project>();

        var set = await _db.Projects.AsNoTracking()
            .Where(p => ids.Contains(p.Id))
            .AsSplitQuery()
            .Include(p => p.RagStatusLookup)
            .Include(p => p.PhaseLookup)
            .Include(p => p.DeliveryPriority)
            .Include(p => p.PrimaryOrganizationalGroup)
            .Include(p => p.PrimaryContactUser)
            .Include(p => p.BusinessAreaLookup)
            .Include(p => p.Directorates).ThenInclude(d => d.Division)
            .Include(p => p.Milestones)
            .Include(p => p.MonthlyUpdates)
            .Include(p => p.SeniorResponsibleOfficers).ThenInclude(s => s.User)
            .Include(p => p.ProjectWorkItemTags).ThenInclude(l => l.WorkItemTagLookup)
            .ToListAsync(cancellationToken);

        var byId = set.ToDictionary(p => p.Id);
        var ordered = new List<Project>(ids.Count);
        foreach (var id in ids)
        {
            if (byId.TryGetValue(id, out var proj))
                ordered.Add(proj);
        }

        return ordered;
    }

    // IQueryable predicate so EF translates; do not replace with a static bool(Project, ...) inside Where().
    private static IQueryable<Project> WhereAssignedToUser(IQueryable<Project> query, string emailLower) =>
        query.Where(p =>
            p.ProjectContacts.Any(pc => pc.Email.ToLower() == emailLower) ||
            (p.PrimaryContactUser != null && p.PrimaryContactUser.Email.ToLower() == emailLower) ||
            p.SeniorResponsibleOfficers.Any(sro => sro.User != null && sro.User.Email.ToLower() == emailLower) ||
            p.ServiceOwners.Any(so => so.User != null && so.User.Email.ToLower() == emailLower) ||
            p.PmoContacts.Any(pmo => pmo.User != null && pmo.User.Email.ToLower() == emailLower));

    /// <inheritdoc />
    public async Task<bool> CanUserEditWorkItemAsync(int projectId, string userEmail, CancellationToken cancellationToken = default)
    {
        var proj = await _db.Projects.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == projectId && !p.IsDeleted, cancellationToken);
        if (proj == null)
            return false;

        if (await _permissionService.IsCentralOperationsAdminOrSuperAdminAsync(userEmail))
            return true;

        var emailLower = EmailLower(userEmail);
        if (await WhereAssignedToUser(
                _db.Projects.Where(p => p.Id == projectId && !p.IsDeleted),
                emailLower)
            .AnyAsync(cancellationToken))
            return true;

        var uid = await _db.Users.AsNoTracking()
            .Where(u => u.Email.ToLower() == emailLower)
            .Select(u => (int?)u.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (!uid.HasValue)
            return false;

        var baIds = BusinessAreaAdminHelper.GetBusinessAreaLookupIdsForProject(proj);
        if (baIds.Count > 0
            && (await _businessAreaAdmins.IsUserAdminForAnyBusinessAreaAsync(uid.Value, baIds, cancellationToken)
                || await _businessAreaLeadership.IsUserLeaderForAnyBusinessAreaAsync(
                    uid.Value, baIds, cancellationToken)))
            return true;

        var divIds = await _db.ProjectDirectorates.AsNoTracking()
            .Where(pd => pd.ProjectId == projectId)
            .Select(pd => pd.DivisionId)
            .ToListAsync(cancellationToken);
        return await _directorateLeadership.IsUserDirectorateLeaderForProjectContextAsync(
            uid.Value, divIds, baIds, cancellationToken);
    }

    /// <summary>Uses <see cref="ProjectMonthlyUpdate.Narrative"/> when set; otherwise joins <see cref="MonthlyUpdateNarrative"/> rows when loaded.</summary>
    private static string? ComposeProjectMonthlyNarrative(ProjectMonthlyUpdate mu)
    {
        if (!string.IsNullOrWhiteSpace(mu.Narrative))
            return mu.Narrative;
        if (mu.MonthlyUpdateNarratives is { Count: > 0 })
            return string.Join("\n\n", mu.MonthlyUpdateNarratives.OrderBy(n => n.CreatedAt).Select(n => n.Narrative));
        return mu.Narrative;
    }

    private static WorkItem MapProjectToWorkItem(Project p)
    {
        var w = new WorkItem
        {
            Id = p.Id,
            LegacyProjectId = p.Id,
            DemandRequestId = p.PipelineDemandRequestId,
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
                Narrative = ComposeProjectMonthlyNarrative(mu),
                RagStatusId = mu.DraftRagStatusLookupId,
                RagJustification = mu.DraftRagJustification,
                PathToGreen = mu.DraftPathToGreen,
                SubmittedAt = mu.SubmittedAt,
                SubmittedByUserId = mu.CreatedByUserId,
                SubmittedBy = mu.CreatedByName ?? mu.CreatedByUser?.Name,
                PermFte = mu.MonthlyPermFte,
                MspFte = mu.MonthlyMspFte
            });
        }

        foreach (var m in p.Milestones.Where(x => !x.IsDeleted))
            w.Milestones.Add(m);

        if (p.ProjectWorkItemTags != null)
        {
            foreach (var link in p.ProjectWorkItemTags
                         .OrderBy(x => x.WorkItemTagLookup?.SortOrder)
                         .ThenBy(x => x.WorkItemTagLookup?.Name))
            {
                if (link.WorkItemTagLookup == null || !link.WorkItemTagLookup.IsActive)
                    continue;
                w.Tags.Add(new WorkItemTagRef
                {
                    Id = link.WorkItemTagLookupId,
                    Name = link.WorkItemTagLookup.Name,
                    Description = link.WorkItemTagLookup.Description
                });
            }
        }

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
            .Include(x => x.MonthlyUpdates).ThenInclude(mu => mu.MonthlyUpdateNarratives)
            .Include(x => x.Milestones)
            .Include(x => x.RagStatusLookup)
            .Include(x => x.PhaseLookup)
            .Include(x => x.DeliveryPriority)
            .Include(x => x.PrimaryOrganizationalGroup)
            .Include(x => x.ProjectWorkItemTags).ThenInclude(t => t.WorkItemTagLookup)
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
            .Include(p => p.PrimaryContactUser)
            .Include(p => p.Directorates).ThenInclude(d => d.Division)
            .Include(p => p.BusinessAreaLookup)
            .Include(p => p.ProjectWorkItemTags).ThenInclude(t => t.WorkItemTagLookup)
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
        var mrPeriodTitle = startOfThisMonth.ToString("MMM", CultureInfo.GetCultureInfo("en-GB")) + " Update";
        DateTime? mrDueDate = null;
        var monthlyUpdateRows = new List<HomeMonthlyUpdateRow>();
        var mrSubmitted = 0;
        var mrDraft = 0;
        var mrMissing = 0;
        var nowDateDash = DateTime.UtcNow.Date;
        var periodOpenDash = new DateTime(nowDateDash.Year, nowDateDash.Month, 20, 0, 0, 0, DateTimeKind.Utc).Date;
        var monthlyReportingWindowOpen = nowDateDash >= periodOpenDash;

        foreach (var w in assignedActivePaused.Where(x => x.Status == "Active" || x.Status == "Paused").OrderBy(x => x.Title))
        {
            var p = assignedProjects.First(ap => ap.Id == w.Id);
            var periodUpdate = p.MonthlyUpdates.FirstOrDefault(m => m.Year == startOfThisMonth.Year && m.Month == startOfThisMonth.Month);
            var submitted = periodUpdate?.SubmittedAt.HasValue == true;
            var draft = periodUpdate != null && periodUpdate.SubmittedAt == null;
            if (submitted) mrSubmitted++;
            else if (draft)
            {
                if (monthlyReportingWindowOpen) mrDraft++;
            }
            else if (monthlyReportingWindowOpen) mrMissing++;

            var portName = p.PrimaryOrganizationalGroup?.Name ?? "—";
            var ragNm = w.RagHistory.OrderByDescending(r => r.UpdatedAt).FirstOrDefault()?.RagStatus?.Name;

            var detailUpdatesUrl = (controller.Url.Action("Detail", "ModernWork", new { id = w.Id }) ?? "") + "#wd-updates";
            var monthlyReportUrl = controller.Url.Action("MonthlyReport", "ModernWork", new { id = w.Id, year = startOfThisMonth.Year, month = startOfThisMonth.Month }) ?? detailUpdatesUrl;

            string rowKind;
            string actionUrl;
            string actionLabel;

            if (submitted)
            {
                rowKind = "view";
                actionUrl = controller.Url.Action("ViewMonthlyUpdate", "ModernWork", new { id = w.Id, updateId = periodUpdate!.Id }) ?? detailUpdatesUrl;
                actionLabel = "View";
            }
            else if (nowDateDash < periodOpenDash)
            {
                rowKind = "not-due";
                actionUrl = "";
                actionLabel = "";
            }
            else
            {
                rowKind = "complete";
                actionUrl = monthlyReportUrl;
                actionLabel = "Complete";
            }

            var latestRagSnapshot = w.RagHistory.OrderByDescending(r => r.UpdatedAt).FirstOrDefault();
            var bizArea = p.BusinessAreaLookup?.Name
                ?? p.PrimaryOrganizationalGroup?.Name
                ?? "—";
            var priNm = p.DeliveryPriority?.Name;
            var phNm = p.PhaseLookup?.Name;
            var pcName = p.PrimaryContactUser?.Name ?? p.PrimaryContactUser?.Email ?? "—";
            var tagSummary = p.ProjectWorkItemTags == null || p.ProjectWorkItemTags.Count == 0
                ? null
                : string.Join(", ", p.ProjectWorkItemTags
                    .Where(l => l.WorkItemTagLookup != null && l.WorkItemTagLookup.IsActive)
                    .OrderBy(l => l.WorkItemTagLookup!.SortOrder).ThenBy(l => l.WorkItemTagLookup!.Name)
                    .Select(l => l.WorkItemTagLookup!.Name));

            string statusLabel;
            if (submitted) statusLabel = "Submitted";
            else if (draft) statusLabel = "Draft";
            else if (!monthlyReportingWindowOpen) statusLabel = "Not due";
            else statusLabel = "Not started";

            monthlyUpdateRows.Add(new HomeMonthlyUpdateRow
            {
                WorkItemId = w.Id,
                WorkTitle = w.Title,
                PeriodLabel = mrPeriodLabel,
                PeriodTitle = mrPeriodTitle,
                DueDate = mrDueDate,
                StatusLabel = statusLabel,
                ActionLabel = actionLabel,
                ActionUrl = actionUrl,
                RowKind = rowKind,
                PortfolioName = portName,
                RagStatusName = ragNm,
                BusinessArea = bizArea,
                PriorityName = priNm,
                PhaseName = phNm,
                PrimaryContact = pcName,
                TagNamesSummary = tagSummary,
                WorkItemStatus = w.Status,
                LatestRag = latestRagSnapshot
            });
        }

        var reportingGapCount = monthlyReportingWindowOpen ? mrDraft + mrMissing : 0;
        var workDashCalloutHeading = $"Monthly reporting — {mrPeriodLabel}";
        string? workDashCalloutBody = null;
        if (monthlyReportingWindowOpen && reportingGapCount > 0)
        {
            var parts = new List<string>();
            if (mrMissing > 0) parts.Add($"{mrMissing} not started");
            if (mrDraft > 0) parts.Add($"{mrDraft} in progress");
            workDashCalloutBody = string.Join(", ", parts) + ".";
        }

        controller.ViewBag.MainNavSection = "work";
        controller.ViewBag.SubNavItem = "work-dashboard";
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
        controller.ViewBag.WorkDashPrimaryContactById = assignedProjects.ToDictionary(p => p.Id, p => p.PrimaryContactUser?.Name ?? p.PrimaryContactUser?.Email ?? "—");
        controller.ViewBag.WdMrRows = monthlyUpdateRows;
        controller.ViewBag.WdMrMonthlyUpdateColumnHeader = mrPeriodTitle;
        controller.ViewBag.WdMrPeriodLabel = mrPeriodLabel;
        controller.ViewBag.WdMrDueDate = mrDueDate;
        controller.ViewBag.WdMrDaysRemaining = null;
        controller.ViewBag.WdMrWindowOpen = monthlyReportingWindowOpen;
        controller.ViewBag.WdMrWindowOpensOn = periodOpenDash;
        controller.ViewBag.WdMrSubmitted = mrSubmitted;
        controller.ViewBag.WdMrDraft = mrDraft;
        controller.ViewBag.WdMrMissing = mrMissing;
        controller.ViewBag.WdMrActiveAssignedCount = assignedActivePaused.Count(w => w.Status == "Active" || w.Status == "Paused");
        controller.ViewBag.WdMrExtendedReportingTable = true;
        controller.ViewBag.ReportingTabCount = reportingGapCount;
        controller.ViewBag.WorkDashCalloutHeading = workDashCalloutHeading;
        controller.ViewBag.WorkDashCalloutBody = workDashCalloutBody;
        controller.ViewBag.WorkDashCalloutClass = "dfe-c-callout dfe-c-callout--warning dfe-c-mb-3";
        controller.ViewBag.WorkDashShowCallout = reportingGapCount > 0;
        controller.ViewBag.CurrentPeriodLabel = mrPeriodLabel;
        controller.ViewBag.CurrentPeriodDueDate = mrDueDate;
    }

    private static string NormalizeRegisterSortKey(string? sort) =>
        string.IsNullOrWhiteSpace(sort) ? "title" : sort.Trim().ToLowerInvariant();

    private static IQueryable<Project> ApplyRegisterSort(IQueryable<Project> query, string? sort, bool desc)
    {
        var key = NormalizeRegisterSortKey(sort);

        return key switch
        {
            "businessarea" => desc
                ? query.OrderByDescending(p => p.BusinessAreaLookup != null ? p.BusinessAreaLookup.Name ?? "" : "").ThenByDescending(p => p.Title)
                : query.OrderBy(p => p.BusinessAreaLookup != null ? p.BusinessAreaLookup.Name ?? "" : "").ThenBy(p => p.Title),
            "phase" => desc
                ? query.OrderByDescending(p => p.PhaseLookup != null ? p.PhaseLookup.Name ?? "" : "").ThenByDescending(p => p.Title)
                : query.OrderBy(p => p.PhaseLookup != null ? p.PhaseLookup.Name ?? "" : "").ThenBy(p => p.Title),
            "priority" => desc
                ? query.OrderByDescending(p => p.DeliveryPriority != null ? p.DeliveryPriority.Name ?? "" : "").ThenByDescending(p => p.Title)
                : query.OrderBy(p => p.DeliveryPriority != null ? p.DeliveryPriority.Name ?? "" : "").ThenBy(p => p.Title),
            "rag" => desc
                ? query.OrderByDescending(p => p.RagStatusLookup != null ? p.RagStatusLookup.Name ?? "" : "").ThenByDescending(p => p.Title)
                : query.OrderBy(p => p.RagStatusLookup != null ? p.RagStatusLookup.Name ?? "" : "").ThenBy(p => p.Title),
            "primarycontact" => desc
                ? query.OrderByDescending(p => p.PrimaryContactUser != null ? p.PrimaryContactUser.Name ?? p.PrimaryContactUser.Email ?? "" : "").ThenByDescending(p => p.Title)
                : query.OrderBy(p => p.PrimaryContactUser != null ? p.PrimaryContactUser.Name ?? p.PrimaryContactUser.Email ?? "" : "").ThenBy(p => p.Title),
            "status" => desc
                ? query.OrderByDescending(p => p.Status).ThenByDescending(p => p.Title)
                : query.OrderBy(p => p.Status).ThenBy(p => p.Title),
            "monthly" => desc
                ? query.OrderByDescending(p => p.UpdatedAt).ThenByDescending(p => p.Title)
                : query.OrderBy(p => p.UpdatedAt).ThenBy(p => p.Title),
            // Tag text is built client-side on rows; SQL uses updated-at as stable proxy when sorting by tags.
            "tags" => desc
                ? query.OrderByDescending(p => p.UpdatedAt).ThenByDescending(p => p.Title)
                : query.OrderBy(p => p.UpdatedAt).ThenBy(p => p.Title),
            _ => desc
                ? query.OrderByDescending(p => p.Title)
                : query.OrderBy(p => p.Title),
        };
    }

    private static List<WorkRegisterRow> OrderRegisterRows(IEnumerable<WorkRegisterRow> rows, string? sort, bool desc)
    {
        var key = NormalizeRegisterSortKey(sort);
        return key switch
        {
            "businessarea" => desc
                ? rows.OrderByDescending(r => r.BusinessAreaName ?? "").ThenByDescending(r => r.Title).ToList()
                : rows.OrderBy(r => r.BusinessAreaName ?? "").ThenBy(r => r.Title).ToList(),
            "phase" => desc
                ? rows.OrderByDescending(r => r.PhaseName ?? "").ThenByDescending(r => r.Title).ToList()
                : rows.OrderBy(r => r.PhaseName ?? "").ThenBy(r => r.Title).ToList(),
            "priority" => desc
                ? rows.OrderByDescending(r => r.PriorityName ?? "").ThenByDescending(r => r.Title).ToList()
                : rows.OrderBy(r => r.PriorityName ?? "").ThenBy(r => r.Title).ToList(),
            "rag" => desc
                ? rows.OrderByDescending(r => r.RagName ?? "").ThenByDescending(r => r.Title).ToList()
                : rows.OrderBy(r => r.RagName ?? "").ThenBy(r => r.Title).ToList(),
            "primarycontact" => desc
                ? rows.OrderByDescending(r => r.PrimaryContactName ?? "").ThenByDescending(r => r.Title).ToList()
                : rows.OrderBy(r => r.PrimaryContactName ?? "").ThenBy(r => r.Title).ToList(),
            "status" => desc
                ? rows.OrderByDescending(r => r.Status).ThenByDescending(r => r.Title).ToList()
                : rows.OrderBy(r => r.Status).ThenBy(r => r.Title).ToList(),
            "monthly" => desc
                ? rows.OrderByDescending(r => r.UpdatedAtUtc).ThenByDescending(r => r.Title).ToList()
                : rows.OrderBy(r => r.UpdatedAtUtc).ThenBy(r => r.Title).ToList(),
            "tags" => desc
                ? rows.OrderByDescending(r => r.TagNamesSummary ?? "").ThenByDescending(r => r.Title).ToList()
                : rows.OrderBy(r => r.TagNamesSummary ?? "").ThenBy(r => r.Title).ToList(),
            _ => desc
                ? rows.OrderByDescending(r => r.Title).ToList()
                : rows.OrderBy(r => r.Title).ToList(),
        };
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
        string? registerTab = null,
        int? registerPage = null,
        int registerPageSize = 20,
        int? businessAreaId = null,
        int? primaryContactUserId = null,
        int[]? tagIds = null,
        string? registerSort = null,
        bool registerSortDesc = false,
        CancellationToken cancellationToken = default)
    {
        var emailLower = EmailLower(userEmail);
        var neutralQ = _db.Projects.AsNoTracking().Where(p => !p.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            neutralQ = neutralQ.Where(p =>
                p.Title.Contains(s) || (p.Aim != null && p.Aim.Contains(s))
                || p.ProjectWorkItemTags.Any(t => t.WorkItemTagLookup != null
                    && t.WorkItemTagLookup.IsActive
                    && t.WorkItemTagLookup.Name.Contains(s)));
        }

        if (portfolioId.HasValue)
            neutralQ = neutralQ.Where(p => p.PrimaryOrganizationalGroupId == portfolioId.Value);

        if (businessAreaId.HasValue)
            neutralQ = neutralQ.Where(p => p.BusinessAreaId == businessAreaId.Value);

        if (directorateId.HasValue)
            neutralQ = neutralQ.Where(p => p.Directorates.Any(d => d.DivisionId == directorateId.Value));

        if (phaseId.HasValue)
            neutralQ = neutralQ.Where(p => p.PhaseId == phaseId.Value);

        if (ragId.HasValue)
        {
            var ragName = await _db.RagStatusLookups.AsNoTracking()
                .Where(r => r.Id == ragId.Value)
                .Select(r => r.Name)
                .FirstOrDefaultAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(ragName))
            {
                var ragNorm = ragName.Trim().ToLowerInvariant();
                neutralQ = neutralQ.Where(p =>
                    p.RagStatusLookupId == ragId.Value
                    || (p.RagStatusLookupId == null && p.RagStatus != null && p.RagStatus.ToLower() == ragNorm));
            }
            else
                neutralQ = neutralQ.Where(p => p.RagStatusLookupId == ragId.Value);
        }

        if (priorityId.HasValue)
            neutralQ = neutralQ.Where(p => p.DeliveryPriorityId == priorityId.Value);

        if (primaryContactUserId.HasValue)
            neutralQ = neutralQ.Where(p => p.PrimaryContactUserId == primaryContactUserId.Value);

        var tagIdList = (tagIds ?? Array.Empty<int>()).Where(id => id > 0).Distinct().ToList();
        if (tagIdList.Count > 0)
            neutralQ = neutralQ.Where(p => p.ProjectWorkItemTags.Any(t => tagIdList.Contains(t.WorkItemTagLookupId)));

        var mineScopedQ = WhereAssignedToUser(neutralQ, emailLower);
        var baseQ = isMyWork ? mineScopedQ : neutralQ;

        var registerActivePausedCountMine = await mineScopedQ.CountAsync(
            p => p.Status == "Active" || p.Status == "Paused", cancellationToken);
        var registerActivePausedCountOrg = await neutralQ.CountAsync(
            p => p.Status == "Active" || p.Status == "Paused", cancellationToken);

        var sortKeyNorm = NormalizeRegisterSortKey(registerSort);
        var filterTagIdUi = tagIdList.Count == 1 ? tagIdList[0] : (int?)null;

        var nowDate = DateTime.UtcNow.Date;
        var reportY = nowDate.Year;
        var reportM = nowDate.Month;
        var currentDueDate = _monthlyUpdateService.GetMonthlyUpdateDueDate(reportY, reportM);
        var currentPeriodLabel = new DateTime(reportY, reportM, 1).ToString("MMMM", CultureInfo.GetCultureInfo("en-GB"));
        var prevMonthDate = nowDate.AddMonths(-1);
        var prevMonthLabel = prevMonthDate.ToString("MMM", CultureInfo.InvariantCulture);
        var registerMonthlyColumnHeader = new DateTime(reportY, reportM, 1).ToString("MMM", CultureInfo.GetCultureInfo("en-GB")) + " Update";

        var orgGroups = await _db.OrganizationalGroups.AsNoTracking().Where(g => g.IsActive).OrderBy(g => g.Name).ToListAsync(cancellationToken);
        var portfolios = orgGroups.Select(g => new Portfolio { Id = g.Id, Name = g.Name, IsActive = true }).ToList();

        var businessAreas = await _db.BusinessAreaLookups.AsNoTracking()
            .Where(b => b.IsActive)
            .OrderBy(b => b.SortOrder)
            .ThenBy(b => b.Name)
            .ToListAsync(cancellationToken);

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

        var tagOpts = await _db.WorkItemTagLookups.AsNoTracking()
            .Where(t => t.IsActive)
            .OrderBy(t => t.SortOrder).ThenBy(t => t.Name)
            .Select(t => new WorkLookupOption { Id = t.Id, Name = t.Name, Value = t.Name })
            .ToListAsync(cancellationToken);

        var primaryContactIds = await _db.Projects.AsNoTracking()
            .Where(p => !p.IsDeleted && p.PrimaryContactUserId != null)
            .Select(p => p.PrimaryContactUserId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        var primaryContactOpts = await _db.Users.AsNoTracking()
            .Where(u => primaryContactIds.Contains(u.Id))
            .OrderBy(u => u.Name).ThenBy(u => u.Email)
            .Select(u => new WorkPrimaryContactOption
            {
                UserId = u.Id,
                DisplayName = u.Name != null ? u.Name : (u.Email ?? "")
            })
            .ToListAsync(cancellationToken);

        var redRag = await _db.RagStatusLookups.AsNoTracking()
            .Where(r => r.Name != null && r.Name.ToLower() == "red")
            .Select(r => r.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var tabKey = (registerTab ?? "active").Trim().ToLowerInvariant();
        if (tabKey is not ("active" or "completed" or "cancelled" or "all"))
            tabKey = "active";

        if (registerPage.HasValue && string.IsNullOrWhiteSpace(monthlyUpdate))
        {
            var pageSize = registerPageSize < 1 ? 20 : registerPageSize;

            var activeCount = await baseQ.CountAsync(p => p.Status == "Active", cancellationToken);
            var pausedCount = await baseQ.CountAsync(p => p.Status == "Paused", cancellationToken);
            var completedCount = await baseQ.CountAsync(p => p.Status == "Completed", cancellationToken);
            var cancelledCount = await baseQ.CountAsync(p => p.Status == "Cancelled", cancellationToken);
            var activePausedCountBeforeMonthlyFilter = activeCount + pausedCount;

            var ragRedCount = 0;
            if (redRag != 0)
            {
                ragRedCount = await baseQ.CountAsync(p =>
                    (p.Status == "Active" || p.Status == "Paused") && p.RagStatusLookupId == redRag, cancellationToken);
            }

            IQueryable<Project> tabQ = tabKey switch
            {
                "completed" => baseQ.Where(p => p.Status == "Completed"),
                "cancelled" => baseQ.Where(p => p.Status == "Cancelled"),
                "all" => baseQ.Where(p =>
                    p.Status == "Active" || p.Status == "Paused" || p.Status == "Completed" || p.Status == "Cancelled"),
                _ => baseQ.Where(p => p.Status == "Active" || p.Status == "Paused"),
            };

            var total = await tabQ.CountAsync(cancellationToken);
            var pageCount = total == 0 ? 1 : (total + pageSize - 1) / pageSize;
            var pageClamped = Math.Min(Math.Max(1, registerPage.Value), pageCount);
            var skip = (pageClamped - 1) * pageSize;

            var sortedTabQ = ApplyRegisterSort(tabQ, registerSort, registerSortDesc);
            var ids = await sortedTabQ
                .Skip(skip)
                .Take(pageSize)
                .Select(p => p.Id)
                .ToListAsync(cancellationToken);

            var firstRiskByProject = new Dictionary<int, (int Id, string Reference)>();
            var pageRows = new List<WorkRegisterRow>();
            if (ids.Count > 0)
            {
                var riskRows = await _db.Risks.AsNoTracking()
                    .Where(r => r.ProjectId.HasValue && ids.Contains(r.ProjectId.Value) && !r.IsDeleted && r.Status != "closed")
                    .OrderBy(r => r.Id)
                    .Select(r => new { ProjectId = r.ProjectId!.Value, r.Id, r.FipsId })
                    .ToListAsync(cancellationToken);
                foreach (var x in riskRows)
                {
                    if (firstRiskByProject.ContainsKey(x.ProjectId)) continue;
                    var reference = !string.IsNullOrWhiteSpace(x.FipsId) ? x.FipsId : "RISK-" + x.Id.ToString("D5", CultureInfo.InvariantCulture);
                    firstRiskByProject[x.ProjectId] = (x.Id, reference);
                }

                var pageProjects = await LoadProjectsForWorkRegisterByIdsAsync(ids, cancellationToken);
                foreach (var p in pageProjects)
                {
                    var row = BuildWorkRegisterRow(p, firstRiskByProject, nowDate, reportY, reportM, prevMonthDate, prevMonthLabel, currentPeriodLabel, currentDueDate, url);
                    if (row != null)
                        pageRows.Add(row);
                }
            }

            var rowStart = total == 0 ? 0 : skip + 1;
            var rowEnd = total == 0 ? 0 : skip + pageRows.Count;

            return new WorkRegisterViewModel
            {
                PageTitle = isMyWork ? "My work" : "Work register",
                PageDescription = isMyWork ? "Work assigned to you" : "Active, paused and recently completed work items",
                IsMyWork = isMyWork,
                ActiveCount = activeCount,
                PausedCount = pausedCount,
                CompletedCount = completedCount,
                CancelledCount = cancelledCount,
                RagRedCount = ragRedCount,
                AwaitingMonthlyCount = 0,
                AwaitingMonthlyOverdue = false,
                ActivePausedCountBeforeMonthlyFilter = activePausedCountBeforeMonthlyFilter,
                RegisterActivePausedCountMine = registerActivePausedCountMine,
                RegisterActivePausedCountOrg = registerActivePausedCountOrg,
                RegisterMonthlyColumnHeader = registerMonthlyColumnHeader,
                ActivePaused = new List<WorkRegisterRow>(),
                Completed = new List<WorkRegisterRow>(),
                Cancelled = new List<WorkRegisterRow>(),
                Portfolios = portfolios,
                BusinessAreas = businessAreas,
                Directorates = directorates,
                DeliveryPhaseOptions = phaseOpts,
                RagOptions = ragOpts,
                PriorityOptions = priOpts,
                FilterSearch = search,
                FilterPortfolioId = portfolioId,
                FilterBusinessAreaId = businessAreaId,
                FilterDirectorateId = directorateId,
                FilterPhaseId = phaseId,
                FilterRagId = ragId,
                FilterPriorityId = priorityId,
                FilterMonthlyUpdate = monthlyUpdate,
                FilterPrimaryContactUserId = primaryContactUserId,
                FilterTagIds = tagIdList,
                PrimaryContactFilterOptions = primaryContactOpts,
                TagFilterOptions = tagOpts,
                RegisterSortField = sortKeyNorm,
                RegisterSortDescending = registerSortDesc,
                FilterTagId = filterTagIdUi,
                RegisterIsPaginated = true,
                RegisterTab = tabKey,
                RegisterPage = pageClamped,
                RegisterPageSize = pageSize,
                RegisterTotalCount = total,
                RegisterPageCount = pageCount,
                RegisterDisplayRowStart = rowStart,
                RegisterDisplayRowEnd = rowEnd,
                RegisterPageRows = pageRows
            };
        }

        var allProjects = await baseQ
            .AsSplitQuery()
            .Include(p => p.RagStatusLookup)
            .Include(p => p.PhaseLookup)
            .Include(p => p.DeliveryPriority)
            .Include(p => p.PrimaryOrganizationalGroup)
            .Include(p => p.PrimaryContactUser)
            .Include(p => p.BusinessAreaLookup)
            .Include(p => p.Directorates).ThenInclude(d => d.Division)
            .Include(p => p.Milestones)
            .Include(p => p.MonthlyUpdates)
            .Include(p => p.SeniorResponsibleOfficers).ThenInclude(s => s.User)
            .Include(p => p.ProjectWorkItemTags).ThenInclude(l => l.WorkItemTagLookup)
            .OrderByDescending(p => p.UpdatedAt)
            .ToListAsync(cancellationToken);

        var projectIds = allProjects.Select(p => p.Id).ToList();
        var firstRiskByProjectSlow = new Dictionary<int, (int Id, string Reference)>();
        if (projectIds.Count > 0)
        {
            var riskRows = await _db.Risks.AsNoTracking()
                .Where(r => r.ProjectId.HasValue && projectIds.Contains(r.ProjectId.Value) && !r.IsDeleted && r.Status != "closed")
                .OrderBy(r => r.Id)
                .Select(r => new { ProjectId = r.ProjectId!.Value, r.Id, r.FipsId })
                .ToListAsync(cancellationToken);
            foreach (var x in riskRows)
            {
                if (firstRiskByProjectSlow.ContainsKey(x.ProjectId)) continue;
                var reference = !string.IsNullOrWhiteSpace(x.FipsId) ? x.FipsId : "RISK-" + x.Id.ToString("D5", CultureInfo.InvariantCulture);
                firstRiskByProjectSlow[x.ProjectId] = (x.Id, reference);
            }
        }

        var activePaused = new List<WorkRegisterRow>();
        var completed = new List<WorkRegisterRow>();
        var cancelled = new List<WorkRegisterRow>();

        foreach (var p in allProjects)
        {
            var row = BuildWorkRegisterRow(p, firstRiskByProjectSlow, nowDate, reportY, reportM, prevMonthDate, prevMonthLabel, currentPeriodLabel, currentDueDate, url);
            if (row == null)
                continue;

            if (p.Status == "Active" || p.Status == "Paused")
                activePaused.Add(row);
            else if (p.Status == "Completed")
                completed.Add(row);
            else if (p.Status == "Cancelled")
                cancelled.Add(row);
        }

        activePaused = OrderRegisterRows(activePaused, registerSort, registerSortDesc);
        completed = OrderRegisterRows(completed, registerSort, registerSortDesc);
        cancelled = OrderRegisterRows(cancelled, registerSort, registerSortDesc);

        var activePausedCountBeforeMonthly = activePaused.Count;
        if (!string.IsNullOrWhiteSpace(monthlyUpdate))
        {
            var mu = monthlyUpdate.Trim().ToLowerInvariant();
            activePaused = activePaused.Where(r => string.Equals(r.MonthlyUpdateFilterKey, mu, StringComparison.OrdinalIgnoreCase)).ToList();
            activePaused = OrderRegisterRows(activePaused, registerSort, registerSortDesc);
        }

        var ragRedCountSlow = redRag != 0 ? activePaused.Count(r => r.RagStatusId == redRag) : 0;

        if (registerPage.HasValue)
        {
            var pageSize = registerPageSize < 1 ? 20 : registerPageSize;
            IReadOnlyList<WorkRegisterRow> sourceFull = tabKey switch
            {
                "completed" => completed,
                "cancelled" => cancelled,
                "all" => OrderRegisterRows(activePaused.Concat(completed).Concat(cancelled), registerSort, registerSortDesc),
                _ => activePaused
            };

            var total = sourceFull.Count;
            var pageCount = total == 0 ? 1 : (total + pageSize - 1) / pageSize;
            var pageClamped = Math.Min(Math.Max(1, registerPage.Value), pageCount);
            var skip = (pageClamped - 1) * pageSize;
            var pageRows = sourceFull.Skip(skip).Take(pageSize).ToList();
            var rowStart = total == 0 ? 0 : skip + 1;
            var rowEnd = total == 0 ? 0 : skip + pageRows.Count;

            return new WorkRegisterViewModel
            {
                PageTitle = isMyWork ? "My work" : "Work register",
                PageDescription = isMyWork ? "Work assigned to you" : "Active, paused and recently completed work items",
                IsMyWork = isMyWork,
                ActiveCount = activePaused.Count(r => r.Status == "Active"),
                PausedCount = activePaused.Count(r => r.Status == "Paused"),
                CompletedCount = completed.Count,
                CancelledCount = cancelled.Count,
                RagRedCount = ragRedCountSlow,
                AwaitingMonthlyCount = 0,
                AwaitingMonthlyOverdue = false,
                ActivePausedCountBeforeMonthlyFilter = activePausedCountBeforeMonthly,
                RegisterActivePausedCountMine = registerActivePausedCountMine,
                RegisterActivePausedCountOrg = registerActivePausedCountOrg,
                RegisterMonthlyColumnHeader = registerMonthlyColumnHeader,
                ActivePaused = new List<WorkRegisterRow>(),
                Completed = new List<WorkRegisterRow>(),
                Cancelled = new List<WorkRegisterRow>(),
                Portfolios = portfolios,
                BusinessAreas = businessAreas,
                Directorates = directorates,
                DeliveryPhaseOptions = phaseOpts,
                RagOptions = ragOpts,
                PriorityOptions = priOpts,
                FilterSearch = search,
                FilterPortfolioId = portfolioId,
                FilterBusinessAreaId = businessAreaId,
                FilterDirectorateId = directorateId,
                FilterPhaseId = phaseId,
                FilterRagId = ragId,
                FilterPriorityId = priorityId,
                FilterMonthlyUpdate = monthlyUpdate,
                FilterPrimaryContactUserId = primaryContactUserId,
                FilterTagIds = tagIdList,
                PrimaryContactFilterOptions = primaryContactOpts,
                TagFilterOptions = tagOpts,
                RegisterSortField = sortKeyNorm,
                RegisterSortDescending = registerSortDesc,
                FilterTagId = filterTagIdUi,
                RegisterIsPaginated = true,
                RegisterTab = tabKey,
                RegisterPage = pageClamped,
                RegisterPageSize = pageSize,
                RegisterTotalCount = total,
                RegisterPageCount = pageCount,
                RegisterDisplayRowStart = rowStart,
                RegisterDisplayRowEnd = rowEnd,
                RegisterPageRows = pageRows
            };
        }

        return new WorkRegisterViewModel
        {
            PageTitle = isMyWork ? "My work" : "Work register",
            PageDescription = isMyWork ? "Work assigned to you" : "Active, paused and recently completed work items",
            IsMyWork = isMyWork,
            ActiveCount = activePaused.Count(r => r.Status == "Active"),
            PausedCount = activePaused.Count(r => r.Status == "Paused"),
            CompletedCount = completed.Count,
            CancelledCount = cancelled.Count,
            RagRedCount = ragRedCountSlow,
            AwaitingMonthlyCount = 0,
            AwaitingMonthlyOverdue = false,
            ActivePausedCountBeforeMonthlyFilter = activePausedCountBeforeMonthly,
            RegisterActivePausedCountMine = registerActivePausedCountMine,
            RegisterActivePausedCountOrg = registerActivePausedCountOrg,
            RegisterMonthlyColumnHeader = registerMonthlyColumnHeader,
            ActivePaused = activePaused,
            Completed = completed,
            Cancelled = cancelled,
            Portfolios = portfolios,
            BusinessAreas = businessAreas,
            Directorates = directorates,
            DeliveryPhaseOptions = phaseOpts,
            RagOptions = ragOpts,
            PriorityOptions = priOpts,
            FilterSearch = search,
            FilterPortfolioId = portfolioId,
            FilterBusinessAreaId = businessAreaId,
            FilterDirectorateId = directorateId,
            FilterPhaseId = phaseId,
            FilterRagId = ragId,
            FilterPriorityId = priorityId,
            FilterMonthlyUpdate = monthlyUpdate,
            FilterPrimaryContactUserId = primaryContactUserId,
            FilterTagIds = tagIdList,
            PrimaryContactFilterOptions = primaryContactOpts,
            TagFilterOptions = tagOpts,
            RegisterSortField = sortKeyNorm,
            RegisterSortDescending = registerSortDesc,
            FilterTagId = filterTagIdUi
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
            .Include(p => p.BusinessAreaLookup)
            .Include(p => p.ProjectWorkItemTags).ThenInclude(t => t.WorkItemTagLookup)
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
            .Include(p => p.BusinessAreaLookup)
            .Include(p => p.ProjectMissions).ThenInclude(pm => pm.Mission)
            .Include(p => p.ProjectObjectives).ThenInclude(po => po.Objective)
            .OrderBy(p => p.Title)
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
            .Include(p => p.BusinessAreaLookup)
            .OrderBy(p => p.Title)
            .ToListAsync(cancellationToken);

        return projects.Select(MapProjectToWorkItem).ToList();
    }
}
