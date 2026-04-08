using System.Globalization;
using System.Text.Json;
using Compass.Models;
using Compass.Models.Modern.Work;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Compass.Services.Modern;

public partial class ModernWorkService
{
    private static WorkAppUser? ToWorkApp(User? u) =>
        u == null
            ? null
            : new WorkAppUser { Name = u.Name, Email = u.Email };

    public async Task<WorkItem?> PopulateWorkDetailAsync(
        Controller controller,
        int projectId,
        User currentUser,
        string userEmail,
        string? tab,
        string? milestonestab,
        CancellationToken cancellationToken = default)
    {
        var emailLower = EmailLower(userEmail);

        var p = await _db.Projects
            .AsNoTracking()
            .AsSplitQuery()
            .Where(x => x.Id == projectId && !x.IsDeleted)
            .Include(x => x.RagHistory).ThenInclude(r => r.RagStatusLookup)
            .Include(x => x.MonthlyUpdates).ThenInclude(mu => mu.CreatedByUser)
            .Include(x => x.Milestones)
            .Include(x => x.Risks).ThenInclude(r => r.OwnerUser)
            .Include(x => x.Risks).ThenInclude(r => r.RiskTier)
            .Include(x => x.Risks).ThenInclude(r => r.RiskStatus)
            .Include(x => x.Risks).ThenInclude(r => r.RiskPriority)
            .Include(x => x.Issues).ThenInclude(i => i.OwnerUser)
            .Include(x => x.Issues).ThenInclude(i => i.StatusLookup)
            .Include(x => x.RagStatusLookup)
            .Include(x => x.PhaseLookup)
            .Include(x => x.DeliveryPriority)
            .Include(x => x.PrimaryOrganizationalGroup)
            .Include(x => x.PrimaryContactUser)
            .Include(x => x.ActivityTypeLookup)
            .Include(x => x.RiskAppetiteLookup)
            .Include(x => x.Directorates).ThenInclude(d => d.Division)
            .Include(x => x.ProblemStatements)
            .Include(x => x.ProjectMissions).ThenInclude(pm => pm.Mission)
            .Include(x => x.ProjectObjectives).ThenInclude(po => po.Objective)
            .Include(x => x.ProjectContacts).ThenInclude(pc => pc.User)
            .Include(x => x.BudgetOwners).ThenInclude(bo => bo.BusinessAreaLookup)
            .FirstOrDefaultAsync(cancellationToken);

        if (p == null)
            return null;

        var work = MapProjectToWorkItem(p);

        var ps = p.ProblemStatements?.OrderByDescending(x => x.UpdatedAt).FirstOrDefault()?.ProblemStatement;
        work.ProblemStatement = string.IsNullOrWhiteSpace(ps) ? null : ps;

        work.PriorityChangeReason = p.DeliveryPriorityChangeReason;

        work.PriorityOutcomes.Clear();
        foreach (var po in p.ProjectObjectives)
        {
            if (po.Objective == null) continue;
            work.PriorityOutcomes.Add(new WorkItemPriorityOutcome
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

        work.MissionPillars.Clear();
        foreach (var pm in p.ProjectMissions)
        {
            if (pm.Mission == null) continue;
            work.MissionPillars.Add(new WorkItemMissionPillar
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

        work.Directorates.Clear();
        foreach (var d in p.Directorates)
        {
            work.Directorates.Add(new WorkItemDirectorate
            {
                Id = d.Id,
                DirectorateId = d.DivisionId,
                Division = d.Division
            });
        }

        work.GovernmentDepartments.Clear();
        if (!string.IsNullOrWhiteSpace(p.OtherDepartments))
        {
            try
            {
                var ids = JsonSerializer.Deserialize<int[]>(p.OtherDepartments);
                if (ids is { Length: > 0 })
                {
                    var gds = await _db.GovernmentDepartments.AsNoTracking()
                        .Where(g => ids.Contains(g.Id))
                        .ToListAsync(cancellationToken);
                    foreach (var gd in gds)
                    {
                        work.GovernmentDepartments.Add(new WorkItemGovernmentDepartment
                        {
                            Id = gd.Id,
                            GovernmentDepartmentId = gd.Id,
                            GovernmentDepartment = gd
                        });
                    }
                }
            }
            catch
            {
                // ignore malformed JSON
            }
        }

        work.Contacts.Clear();
        foreach (var pc in p.ProjectContacts)
        {
            work.Contacts.Add(new WorkItemContact
            {
                Id = pc.Id,
                WorkItemId = p.Id,
                ContactRoleTypeId = null,
                AppUser = pc.User
            });
        }

        work.RiskOrIssues.Clear();
        foreach (var r in p.Risks.Where(x => !x.IsDeleted))
        {
            var statusName = r.RiskStatus?.Label ?? r.Status;
            work.RiskOrIssues.Add(new WorkItemRiskOrIssue
            {
                Id = r.Id,
                WorkItemId = p.Id,
                ReferenceNumber = r.Id,
                Type = "Risk",
                Title = r.Title,
                Description = r.Description,
                Tier = r.RiskTier?.Name,
                Priority = r.RiskPriority?.Label,
                Status = statusName,
                MitigationOrAction = r.ResponseStrategy ?? r.Notes,
                OwnerUser = ToWorkApp(r.OwnerUser),
                RaisedAt = r.IdentifiedDate ?? r.CreatedAt,
                ClosedAt = string.Equals(statusName, "closed", StringComparison.OrdinalIgnoreCase) ? r.ClosedDate : null,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt
            });
        }

        foreach (var i in p.Issues.Where(x => !x.IsDeleted))
        {
            var statusName = i.StatusLookup?.Label ?? i.Status;
            work.RiskOrIssues.Add(new WorkItemRiskOrIssue
            {
                Id = i.Id,
                WorkItemId = p.Id,
                ReferenceNumber = i.Id,
                Type = "Issue",
                Title = i.Title,
                Description = i.Description,
                Priority = i.Priority ?? i.Severity,
                Status = statusName,
                MitigationOrAction = i.Description ?? i.ResolutionSummary ?? string.Empty,
                OwnerUser = ToWorkApp(i.OwnerUser),
                RaisedAt = i.DetectedDate,
                ClosedAt = i.ClosedDate,
                ClosureOutcome = i.ResolutionSummary,
                CreatedAt = i.CreatedAt,
                UpdatedAt = i.UpdatedAt
            });
        }

        var canEdit = await WhereAssignedToUser(
                _db.Projects.Where(proj => proj.Id == projectId && !proj.IsDeleted),
                emailLower)
            .AnyAsync(cancellationToken);

        var isWatching = await _db.ProjectWatchlists.AsNoTracking()
            .AnyAsync(w => w.UserId == currentUser.Id && w.ProjectId == projectId, cancellationToken);

        var ragRows = await _db.RagStatusLookups.AsNoTracking()
            .Where(r => r.IsActive)
            .OrderBy(r => r.SortOrder)
            .ToListAsync(cancellationToken);
        var ragStatusesDict = ragRows.ToDictionary(r => r.Id, r => r.Name);
        var ragBgByStatusId = new Dictionary<int, string?>();
        var ragTextByStatusId = new Dictionary<int, string?>();

        var riskAppetiteOpts = await _db.RiskAppetiteLookups.AsNoTracking()
            .Where(r => r.IsActive)
            .OrderBy(r => r.SortOrder)
            .Select(r => new LookupOption
            {
                Id = r.Id,
                Name = r.Name,
                Value = r.Name
            })
            .ToListAsync(cancellationToken);

        var nowDate = DateTime.UtcNow.Date;
        var reportY = nowDate.Year;
        var reportM = nowDate.Month;
        var currentDueDate = _monthlyUpdateService.GetMonthlyUpdateDueDate(reportY, reportM);
        var currentPeriodLabel = new DateTime(reportY, reportM, 1).ToString("MMMM yyyy", CultureInfo.GetCultureInfo("en-GB"));

        var submittedMonths = new HashSet<(int Y, int M)>();
        foreach (var mu in p.MonthlyUpdates)
        {
            if (mu.SubmittedAt.HasValue)
                submittedMonths.Add((mu.Year, mu.Month));
        }

        var hasCurrent = submittedMonths.Contains((reportY, reportM));
        var monthlyReportDue = (work.Status == "Active" || work.Status == "Paused")
            && currentDueDate.Date < nowDate
            && !hasCurrent;

        int? daysRemaining = null;
        if (currentDueDate.Date >= nowDate)
            daysRemaining = (currentDueDate.Date - nowDate).Days;

        ProjectMonthlyUpdate? lastSubmitted = null;
        foreach (var mu in p.MonthlyUpdates.Where(m => m.SubmittedAt.HasValue)
                     .OrderByDescending(m => m.Year).ThenByDescending(m => m.Month))
        {
            lastSubmitted = mu;
            break;
        }

        string? lastMonthLabel = null;
        string? lastBy = null;
        if (lastSubmitted != null)
        {
            lastMonthLabel = new DateTime(lastSubmitted.Year, lastSubmitted.Month, 1)
                .ToString("MMMM yyyy", CultureInfo.GetCultureInfo("en-GB"));
            lastBy = lastSubmitted.CreatedByName ?? lastSubmitted.CreatedByUser?.Name;
        }

        var latestRag = work.RagHistory.OrderByDescending(r => r.UpdatedAt).FirstOrDefault();
        var currentRagName = p.RagStatusLookup?.Name ?? latestRag?.RagStatus?.Name;

        var milestoneProgressNames = work.Milestones
            .Where(m => !m.IsDeleted)
            .ToDictionary(
                m => m.Id,
                m => string.IsNullOrEmpty(m.Status) ? "—" : m.Status.Replace("_", " ", StringComparison.OrdinalIgnoreCase));

        var submittedUserIds = p.MonthlyUpdates
            .Where(m => m.SubmittedAt.HasValue && m.CreatedByUserId.HasValue)
            .Select(m => m.CreatedByUserId!.Value)
            .Distinct()
            .ToList();
        var monthlyUpdateSubmittedByNames = new Dictionary<int, string>();
        if (submittedUserIds.Count > 0)
        {
            var users = await _db.Users.AsNoTracking()
                .Where(u => submittedUserIds.Contains(u.Id))
                .Select(u => new { u.Id, u.Name, u.Email })
                .ToListAsync(cancellationToken);
            foreach (var u in users)
                monthlyUpdateSubmittedByNames[u.Id] = u.Name ?? u.Email ?? "—";
        }

        var reportingPeriods = new List<ReportingCyclePeriod>();
        var periodDueByKey = new Dictionary<string, (DateTime DueDate, string Label)>();
        for (var i = -18; i <= 3; i++)
        {
            var dt = new DateTime(reportY, reportM, 1).AddMonths(i);
            var y = dt.Year;
            var m = dt.Month;
            var key = y + "-" + m;
            var due = _monthlyUpdateService.GetMonthlyUpdateDueDate(y, m);
            var label = dt.ToString("MMMM yyyy", CultureInfo.GetCultureInfo("en-GB"));
            reportingPeriods.Add(new ReportingCyclePeriod
            {
                PeriodKey = key,
                PeriodLabel = label,
                DueDate = due
            });
            periodDueByKey[key] = (due, label);
        }

        var periodDraft = p.MonthlyUpdates.FirstOrDefault(mu =>
            mu.Year == reportY && mu.Month == reportM && mu.SubmittedAt == null);

        var section = (tab ?? "overview").Trim().ToLowerInvariant() switch
        {
            "updates" => "updates",
            "milestones" => "milestones",
            "risks" => "risks",
            "team" => "team",
            "dependencies" => "dependencies",
            "links" => "links",
            "audit" => "audit",
            _ => "overview"
        };

        var milestoneTab = (milestonestab ?? "inprogress").Trim().ToLowerInvariant() switch
        {
            "complete" => "complete",
            _ => "inprogress"
        };

        var budgetOwnerName = p.BudgetOwners?.FirstOrDefault()?.BusinessAreaLookup?.Name ?? "—";

        controller.ViewBag.MainNavSection = "work";
        controller.ViewBag.SubNavItem = "work-allwork";
        controller.ViewBag.PortfolioName = p.PrimaryOrganizationalGroup?.Name ?? "—";
        controller.ViewBag.PriorityName = p.DeliveryPriority?.Name ?? "—";
        controller.ViewBag.DeliveryPhaseName = p.PhaseLookup?.Name ?? "—";
        controller.ViewBag.ActivityTypeName = p.ActivityTypeLookup?.Name ?? "—";
        controller.ViewBag.RiskAppetiteName = p.RiskAppetiteLookup?.Name ?? "—";
        controller.ViewBag.RiskAppetiteOptions = riskAppetiteOpts;
        controller.ViewBag.PrimaryContactName = p.PrimaryContactUser?.Name ?? p.PrimaryContactUser?.Email ?? "—";
        controller.ViewBag.BudgetOwnerName = budgetOwnerName;
        controller.ViewBag.LinkedDemand = null;
        controller.ViewBag.LinkedBusinessCase = null;
        controller.ViewBag.LatestRag = latestRag;
        controller.ViewBag.IsWatching = isWatching;
        controller.ViewBag.MonthlyReportDue = monthlyReportDue;
        controller.ViewBag.CurrentPeriodLabel = currentPeriodLabel;
        controller.ViewBag.CurrentPeriodDueDate = currentDueDate;
        controller.ViewBag.MonthlyReportDaysRemaining = daysRemaining;
        controller.ViewBag.LastMonthlyUpdateMonth = lastMonthLabel;
        controller.ViewBag.LastMonthlyUpdateBy = lastBy;
        controller.ViewBag.RagUpdatedByNames = new Dictionary<int, string>();
        controller.ViewBag.RagStatusesDict = ragStatusesDict;
        controller.ViewBag.CurrentRagName = currentRagName;
        controller.ViewBag.MilestoneProgressNames = milestoneProgressNames;
        controller.ViewBag.PrimaryContactUser = p.PrimaryContactUser;
        controller.ViewBag.BudgetOwnerUser = (User?)null;
        controller.ViewBag.KeyPeoplePanelItems = new List<(string Key, string Label)>
        {
            ("PrimaryContact", "Primary contact"),
            ("BudgetOwner", "Budget owner")
        };
        controller.ViewBag.ContactRoleTypes = new List<ContactRoleType>();
        controller.ViewBag.CanEditWorkItem = canEdit;
        controller.ViewBag.CanChangeStatus = canEdit;
        controller.ViewBag.WorkStatusDisplayName = work.Status;
        controller.ViewBag.TeamMemberFundingOptions = new List<LookupOption>();
        controller.ViewBag.TeamMemberTypeOptions = new List<LookupOption>();
        controller.ViewBag.ReportingPeriodsList = reportingPeriods;
        controller.ViewBag.ReportingPeriodDueByKey = periodDueByKey;
        controller.ViewBag.MonthlyUpdateSubmittedByNames = monthlyUpdateSubmittedByNames;
        controller.ViewBag.RagBgByStatusId = ragBgByStatusId;
        controller.ViewBag.RagTextByStatusId = ragTextByStatusId;
        controller.ViewBag.FallbackRagByUpdateId = new Dictionary<int, int>();
        controller.ViewBag.DependencyTargetPortfolioNames = new Dictionary<int, string>();
        controller.ViewBag.WorkItemAuditEntries = new List<Compass.Models.AuditLog>();
        controller.ViewBag.WorkChromeSection = section;
        controller.ViewBag.WorkChromeTabsAsLinks = true;
        controller.ViewBag.MilestoneCount = work.Milestones.Count(m => !m.IsDeleted);
        controller.ViewBag.MilestoneTab = milestoneTab;
        controller.ViewBag.CurrentPeriodMonthLabel = currentPeriodLabel;
        controller.ViewBag.CurrentPeriodDraftUpdateId = periodDraft?.Id;
        controller.ViewBag.CurrentPeriodKey = reportY + "-" + reportM;
        controller.ViewBag.CurrentRagBackgroundColourKey = null;
        controller.ViewBag.CurrentRagTextColourKey = null;
        controller.ViewBag.WorkIdShort = work.Id.ToString("D8", CultureInfo.InvariantCulture);

        return work;
    }
}
