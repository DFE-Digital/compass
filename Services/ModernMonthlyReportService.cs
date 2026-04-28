using Compass.Controllers;
using Compass.Data;
using Compass.Models;
using Compass.Services.Aiss;
using Compass.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Compass.Services;

/// <summary>Builds data for the modern monthly reporting dashboard (aligned with Central Ops Monthly Summary V2 logic).</summary>
public class ModernMonthlyReportService
{
    private readonly CompassDbContext _db;
    private readonly IMonthlyUpdateService _monthlyUpdateService;
    private readonly IAissSummaryService _aissSummary;

    public ModernMonthlyReportService(
        CompassDbContext db,
        IMonthlyUpdateService monthlyUpdateService,
        IAissSummaryService aissSummary)
    {
        _db = db;
        _monthlyUpdateService = monthlyUpdateService;
        _aissSummary = aissSummary;
    }

    public async Task<ModernMonthlyReportDashboardViewModel> BuildDashboardAsync(
        int? year,
        int? month,
        int? businessAreaId,
        int? directorateId,
        CancellationToken cancellationToken = default)
    {
        var currentDate = DateTime.UtcNow;
        var calendarYearUtc = currentDate.Year;
        var currentMonth = currentDate.Month;

        const int minReportYear = 2026;
        var maxSelectableYear = calendarYearUtc >= minReportYear ? calendarYearUtc : minReportYear;

        var currentPeriodDueDate = _monthlyUpdateService.GetMonthlyUpdateDueDate(calendarYearUtc, currentMonth);
        var daysUntilCurrentPeriodDueDate = (currentPeriodDueDate - currentDate).Days;

        var defaultReportYear = daysUntilCurrentPeriodDueDate <= 10 ? calendarYearUtc : (currentMonth == 1 ? calendarYearUtc - 1 : calendarYearUtc);
        var defaultReportMonth = daysUntilCurrentPeriodDueDate <= 10 ? currentMonth : (currentMonth == 1 ? 12 : currentMonth - 1);

        defaultReportYear = Math.Max(minReportYear, defaultReportYear);

        var reportYear = year ?? defaultReportYear;
        var reportMonth = month ?? defaultReportMonth;
        if (reportMonth < 1 || reportMonth > 12)
            reportMonth = defaultReportMonth;

        reportYear = Math.Clamp(reportYear, minReportYear, maxSelectableYear);

        var monthStart = new DateTime(reportYear, reportMonth, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59);
        var todayUtc = DateTime.UtcNow.Date;

        var upcomingWindowEnd = monthStart.AddDays(30);

        var query = _db.Projects
            .AsNoTracking()
            .Include(p => p.PrimaryContactUser)
            .Include(p => p.DeliveryPriority)
            .Include(p => p.BusinessAreaLookup)
            .Include(p => p.Milestones)
            .Include(p => p.MonthlyUpdates)
                .ThenInclude(mu => mu.MonthlyUpdateNarratives)
            .Include(p => p.RagStatusLookup)
            .Include(p => p.Directorates)
            .Where(p => !p.IsDeleted && p.Status != "Cancelled" && p.Status != "Completed");

        if (businessAreaId.HasValue)
            query = query.Where(p => p.BusinessAreaId == businessAreaId.Value);
        if (directorateId.HasValue)
            query = query.Where(p => p.Directorates.Any(d => d.DivisionId == directorateId.Value));

        var allProjects = await query.ToListAsync(cancellationToken);

        var businessAreas = await _db.BusinessAreaLookups
            .AsNoTracking()
            .Where(ba => ba.IsActive)
            .OrderBy(ba => ba.SortOrder)
            .ThenBy(ba => ba.Name)
            .ToListAsync(cancellationToken);

        var directorates = await _db.Divisions
            .AsNoTracking()
            .Where(d => d.IsActive)
            .OrderBy(d => d.SortOrder)
            .ThenBy(d => d.Name)
            .ToListAsync(cancellationToken);

        var totalActiveProjects = allProjects.Count;
        var newProjectsThisMonth = allProjects
            .Where(p => p.CreatedAt >= monthStart && p.CreatedAt <= monthEnd)
            .OrderByDescending(p => p.CreatedAt)
            .ToList();

        var milestonesAchieved = allProjects
            .SelectMany(p => p.Milestones
                .Where(m => !m.IsDeleted &&
                            m.Status == "complete" &&
                            m.ActualDate.HasValue &&
                            m.ActualDate.Value >= monthStart &&
                            m.ActualDate.Value <= monthEnd)
                .Select(m => new MilestoneWithProject { Project = p, Milestone = m }))
            .OrderBy(x => x.Milestone.ActualDate)
            .ToList();

        var upcomingMilestones30 = allProjects
            .SelectMany(p => p.Milestones
                .Where(m => !m.IsDeleted &&
                            m.Status != "complete" &&
                            m.Status != "cancelled" &&
                            m.DueDate >= monthStart &&
                            m.DueDate < upcomingWindowEnd)
                .Select(m => new MilestoneWithProject { Project = p, Milestone = m }))
            .OrderBy(x => x.Milestone.DueDate)
            .ToList();

        var lateMilestones = allProjects
            .SelectMany(p => p.Milestones
                .Where(m => !m.IsDeleted &&
                            m.Status != "complete" &&
                            m.Status != "cancelled" &&
                            m.DueDate.Date < todayUtc)
                .Select(m => new MilestoneWithProject { Project = p, Milestone = m }))
            .OrderBy(x => x.Milestone.DueDate)
            .ToList();

        var monthlyUpdateStats = CalculateMonthlyUpdateStats(allProjects, reportYear, reportMonth, _monthlyUpdateService);
        var dueDateForSubmission = _monthlyUpdateService.GetMonthlyUpdateDueDate(reportYear, reportMonth);
        var nowUtcForSubmission = DateTime.UtcNow;

        var ragDistribution = BuildRagDistribution(allProjects);
        var priorityDistribution = BuildPriorityDistribution(allProjects);

        var businessAreaRows = allProjects
            .GroupBy(p => p.BusinessAreaId)
            .Select(g =>
            {
                var submitted = 0;
                var inProgress = 0;
                var late = 0;
                var notStarted = 0;
                foreach (var p in g)
                {
                    var update = p.MonthlyUpdates?.FirstOrDefault(u => u.Year == reportYear && u.Month == reportMonth);
                    if (update != null && update.SubmittedAt.HasValue)
                        submitted++;
                    else if (nowUtcForSubmission > dueDateForSubmission)
                        late++;
                    else if (update != null && !update.SubmittedAt.HasValue)
                        inProgress++;
                    else
                        notStarted++;
                }

                var total = g.Count();
                var completion = total == 0 ? 0 : Math.Round(100m * submitted / total, 1, MidpointRounding.AwayFromZero);

                return new ModernBusinessAreaDashboardRow
            {
                BusinessArea = g.First().BusinessAreaLookup?.Name ?? "Not set",
                BusinessAreaId = g.Key,
                TotalProjects = total,
                SubmittedCount = submitted,
                InProgressCount = inProgress,
                LateCount = late,
                NotStartedCount = notStarted,
                CompletionRatePercent = completion,
                NewThisMonth = g.Count(p => p.CreatedAt >= monthStart && p.CreatedAt <= monthEnd),
                MilestonesCompleted = g.SelectMany(p => p.Milestones
                    .Where(m => !m.IsDeleted &&
                                m.Status == "complete" &&
                                m.ActualDate.HasValue &&
                                m.ActualDate.Value >= monthStart &&
                                m.ActualDate.Value <= monthEnd)).Count(),
                MilestonesUpcoming30Days = g.SelectMany(p => p.Milestones
                    .Where(m => !m.IsDeleted &&
                                m.Status != "complete" &&
                                m.Status != "cancelled" &&
                                m.DueDate >= monthStart &&
                                m.DueDate < upcomingWindowEnd)).Count(),
                MilestonesLate = g.SelectMany(p => p.Milestones
                    .Where(m => !m.IsDeleted &&
                                m.Status != "complete" &&
                                m.Status != "cancelled" &&
                                m.DueDate.Date < todayUtc)).Count(),
                RagRed = g.Count(p => RagBucket(p) == "Red"),
                RagAmberRed = g.Count(p => RagBucket(p) == "Amber-Red"),
                RagAmberGreen = g.Count(p => RagBucket(p) == "Amber-Green"),
                RagGreen = g.Count(p => RagBucket(p) == "Green"),
                RagNotSet = g.Count(p => RagBucket(p) == "Not Set"),
                PriCritical = g.Count(p => PriorityBucket(p) == "Critical"),
                PriHigh = g.Count(p => PriorityBucket(p) == "High"),
                PriMedium = g.Count(p => PriorityBucket(p) == "Medium"),
                PriLow = g.Count(p => PriorityBucket(p) == "Low"),
                PriNotSet = g.Count(p => PriorityBucket(p) == "Not Set"),
                Projects = g.Select(p => new BusinessAreaProjectItem
                {
                    Id = p.Id,
                    Title = p.Title,
                    Rag = RagBucket(p),
                    Priority = PriorityBucket(p),
                    SubmittedUpdate = p.MonthlyUpdates.Any(u => u.Year == reportYear && u.Month == reportMonth && u.SubmittedAt.HasValue),
                    IsNew = p.CreatedAt >= monthStart && p.CreatedAt <= monthEnd,
                    HasMilestoneCompletedInPeriod = p.Milestones.Any(m =>
                        !m.IsDeleted &&
                        m.Status == "complete" &&
                        m.ActualDate.HasValue &&
                        m.ActualDate.Value >= monthStart &&
                        m.ActualDate.Value <= monthEnd),
                    HasMilestoneUpcomingInWindow = p.Milestones.Any(m =>
                        !m.IsDeleted &&
                        m.Status != "complete" &&
                        m.Status != "cancelled" &&
                        m.DueDate >= monthStart &&
                        m.DueDate < upcomingWindowEnd),
                    HasLateMilestone = p.Milestones.Any(m =>
                        !m.IsDeleted &&
                        m.Status != "complete" &&
                        m.Status != "cancelled" &&
                        m.DueDate.Date < todayUtc)
                }).OrderBy(x => x.Title).ToList()
                };
            })
            .OrderByDescending(r => r.CompletionRatePercent)
            .ThenBy(r => r.BusinessArea == "Not set" ? "zzzzzz" : r.BusinessArea)
            .ToList();

        var businessAreaSubmissionProgress = businessAreaRows
            .Select(r => new BusinessAreaSubmissionProgressRow
            {
                BusinessArea = r.BusinessArea,
                BusinessAreaId = r.BusinessAreaId,
                TotalToReport = r.TotalProjects,
                Submitted = r.SubmittedCount,
                InProgress = r.InProgressCount,
                Late = r.LateCount,
                NotStarted = r.NotStartedCount,
                CompletionRatePercent = r.CompletionRatePercent
            })
            .ToList();

        var projectsWithPathToGreen = allProjects
            .Where(p => !string.IsNullOrWhiteSpace(p.PathToGreen) &&
                        RagBucket(p) != "Green" &&
                        !string.IsNullOrWhiteSpace(NormalizeRagStatus(p.RagStatusLookup?.Name ?? p.RagStatus)))
            .OrderBy(p =>
            {
                var rag = RagBucket(p);
                return rag switch
                {
                    "Red" => 1,
                    "Amber-Red" => 2,
                    "Not Set" => 3,
                    "Amber-Green" => 4,
                    _ => 99
                };
            })
            .ThenBy(p => p.BusinessAreaLookup?.Name ?? "ZZZ")
            .ThenBy(p => p.Title)
            .ToList();

        var ragOrder = new[] { "Red", "Amber-Red", "Amber-Green", "Green", "Not Set" };
        var priOrder = new[] { "Critical", "High", "Medium", "Low", "Not Set" };
        var matrix = new List<RagPriorityMatrixCell>();
        foreach (var r in ragOrder)
        {
            foreach (var pr in priOrder)
            {
                var c = allProjects.Count(p => RagBucket(p) == r && PriorityBucket(p) == pr);
                matrix.Add(new RagPriorityMatrixCell { Rag = r, Priority = pr, Count = c });
            }
        }

        var prevMonth = reportMonth == 1 ? 12 : reportMonth - 1;
        var prevYear = reportMonth == 1 ? reportYear - 1 : reportYear;
        var prevMonthStart = new DateTime(prevYear, prevMonth, 1);
        var prevMonthName = prevMonthStart.ToString("MMMM yyyy");

        var prevQuery = _db.Projects
            .AsNoTracking()
            .Include(p => p.MonthlyUpdates)
                .ThenInclude(mu => mu.MonthlyUpdateNarratives)
            .Include(p => p.RagStatusLookup)
            .Include(p => p.DeliveryPriority)
            .Include(p => p.Directorates)
            .Where(p => !p.IsDeleted && p.Status != "Cancelled" && p.Status != "Completed");
        if (businessAreaId.HasValue)
            prevQuery = prevQuery.Where(p => p.BusinessAreaId == businessAreaId.Value);
        if (directorateId.HasValue)
            prevQuery = prevQuery.Where(p => p.Directorates.Any(d => d.DivisionId == directorateId.Value));

        var prevMonthProjects = await prevQuery.ToListAsync(cancellationToken);

        var prevMonthRagDistribution = await BuildPrevMonthRagDistributionAsync(prevMonthProjects, monthStart, cancellationToken);
        var prevMonthPriorityDistribution = BuildPriorityDistribution(prevMonthProjects);

        var allowedProjectIds = allProjects.Select(p => p.Id).ToHashSet();

        var ragHistoryDuringMonth = await _db.ProjectRagHistories
            .AsNoTracking()
            .Include(rh => rh.RagStatusLookup)
            .Where(rh => rh.ChangedAt >= monthStart && rh.ChangedAt <= monthEnd && allowedProjectIds.Contains(rh.ProjectId))
            .ToListAsync(cancellationToken);

        var projectsWithRagChange = ragHistoryDuringMonth.Select(rh => rh.ProjectId).Distinct().Count();

        var projectsWithPriorityChange = allProjects
            .Where(p => p.UpdatedAt >= monthStart &&
                        p.UpdatedAt <= monthEnd &&
                        prevMonthProjects.Any(pp => pp.Id == p.Id &&
                            ((pp.DeliveryPriorityId == null && p.DeliveryPriorityId != null) ||
                             (pp.DeliveryPriorityId != null && p.DeliveryPriorityId == null) ||
                             (pp.DeliveryPriorityId != p.DeliveryPriorityId))))
            .Count();

        var projectIds = allProjects.Select(p => p.Id).ToList();
        var ragHistoriesForProjects = await _db.ProjectRagHistories
            .AsNoTracking()
            .Include(rh => rh.RagStatusLookup)
            .Where(rh => projectIds.Contains(rh.ProjectId))
            .OrderByDescending(rh => rh.ChangedAt)
            .ToListAsync(cancellationToken);

        var historyByProject = ragHistoriesForProjects
            .GroupBy(rh => rh.ProjectId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.ChangedAt).ToList());

        var ragTrend = BuildRagTrend(allProjects, historyByProject, reportYear, reportMonth);
        var priorityTrend = BuildPriorityTrend(allProjects, reportYear, reportMonth);

        var ragChanges = BuildRagChangeDetails(allProjects, ragHistoryDuringMonth, historyByProject, monthStart, reportYear, reportMonth);
        var priorityChanges = BuildPriorityChangeDetails(allProjects, prevMonthProjects, monthStart, monthEnd, reportYear, reportMonth);

        var nextMonthDate = monthStart.AddMonths(1);
        var nextMonthAllowed =
            (nextMonthDate.Year < defaultReportYear ||
             (nextMonthDate.Year == defaultReportYear && nextMonthDate.Month <= defaultReportMonth)) &&
            nextMonthDate.Year <= calendarYearUtc;
        var prevMonthDate = monthStart.AddMonths(-1);
        var earliestReportPeriod = new DateTime(minReportYear, 1, 1);
        var hasPreviousMonthNav = prevMonthDate >= earliestReportPeriod;

        AissPlatformSummary? accessibilitySummary = null;
        string? accessibilityError = null;
        IReadOnlyList<AissByBusinessAreaRow> accessibilityAreaRows = Array.Empty<AissByBusinessAreaRow>();
        try
        {
            var (acc, accErr) = await _aissSummary.GetSummaryAsync(cancellationToken);
            accessibilitySummary = acc;
            accessibilityError = accErr;
            if (acc?.ByBusinessArea is { Count: > 0 } baList)
            {
                if (businessAreaId is int fbaid)
                {
                    var compBaName = businessAreas.FirstOrDefault(b => b.Id == fbaid)?.Name;
                    if (!string.IsNullOrWhiteSpace(compBaName))
                    {
                        var n = compBaName.Trim();
                        var match = baList.FirstOrDefault(r =>
                            string.Equals((r.BusinessArea ?? "").Trim(), n, StringComparison.OrdinalIgnoreCase));
                        accessibilityAreaRows = match is null
                            ? Array.Empty<AissByBusinessAreaRow>()
                            : new[] { match };
                    }
                }
                else
                {
                    accessibilityAreaRows = baList;
                }
            }
        }
        catch
        {
            // Optional reporting enhancement — do not fail the monthly report page
            if (string.IsNullOrEmpty(accessibilityError))
                accessibilityError = "Accessibility data could not be loaded.";
        }

        AissIssueCriteriaBlock? accessibilityIssueCriteria = null;
        if (accessibilitySummary is { } forCriteria)
        {
            if (businessAreaId is null)
                accessibilityIssueCriteria = forCriteria.IssueCriteria;
            else if (accessibilityAreaRows.Count == 1)
                accessibilityIssueCriteria = accessibilityAreaRows[0].IssueCriteria;
        }

        string? businessAreaNarrative = null;
        if (businessAreaId.HasValue)
        {
            var baName = businessAreas.FirstOrDefault(x => x.Id == businessAreaId.Value)?.Name ?? "This business area";
            businessAreaNarrative = BuildBusinessAreaSummaryNarrative(
                baName,
                monthStart.ToString("MMMM yyyy"),
                monthStart,
                reportYear,
                reportMonth,
                allProjects,
                newProjectsThisMonth.Count,
                milestonesAchieved.Count,
                upcomingMilestones30,
                lateMilestones,
                monthlyUpdateStats,
                ragDistribution,
                priorityDistribution,
                prevMonthRagDistribution,
                prevMonthName,
                ragTrend,
                projectsWithPathToGreen.Count,
                projectsWithRagChange,
                projectsWithPriorityChange,
                ragChanges,
                priorityChanges);
        }

        return new ModernMonthlyReportDashboardViewModel
        {
            ReportYear = reportYear,
            ReportMonth = reportMonth,
            MonthName = monthStart.ToString("MMMM yyyy"),
            MonthStart = monthStart,
            MonthEnd = monthEnd,
            DefaultReportYear = defaultReportYear,
            DefaultReportMonth = defaultReportMonth,
            FilterBusinessAreaId = businessAreaId,
            FilterDirectorateId = directorateId,
            BusinessAreas = businessAreas,
            Directorates = directorates,
            TotalActiveProjects = totalActiveProjects,
            NewProjectsCount = newProjectsThisMonth.Count,
            MilestonesAchievedCount = milestonesAchieved.Count,
            NewProjectsThisMonth = newProjectsThisMonth,
            MilestonesAchieved = milestonesAchieved,
            UpcomingMilestonesNext30Days = upcomingMilestones30,
            LateMilestones = lateMilestones,
            MonthlyUpdateStats = monthlyUpdateStats,
            BusinessAreaSubmissionProgress = businessAreaSubmissionProgress,
            RagDistribution = ragDistribution,
            PriorityDistribution = priorityDistribution,
            PrevMonthRagDistribution = prevMonthRagDistribution,
            PrevMonthPriorityDistribution = prevMonthPriorityDistribution,
            PrevMonthName = prevMonthName,
            BusinessAreaRows = businessAreaRows,
            ProjectsWithPathToGreen = projectsWithPathToGreen,
            RagPriorityMatrix = matrix,
            ProjectsWithRagChangeInPeriod = projectsWithRagChange,
            ProjectsWithPriorityChangeInPeriod = projectsWithPriorityChange,
            RagTrend = ragTrend,
            PriorityTrend = priorityTrend,
            RagChanges = ragChanges,
            PriorityChanges = priorityChanges,
            BusinessAreaSummaryNarrative = businessAreaNarrative,
            MinReportYear = minReportYear,
            MaxReportYear = maxSelectableYear,
            HasPreviousMonthNav = hasPreviousMonthNav,
            HasNextMonthNav = nextMonthAllowed,
            PreviousNavYear = hasPreviousMonthNav ? prevMonthDate.Year : null,
            PreviousNavMonth = hasPreviousMonthNav ? prevMonthDate.Month : null,
            NextNavYear = nextMonthAllowed ? nextMonthDate.Year : null,
            NextNavMonth = nextMonthAllowed ? nextMonthDate.Month : null,
            AccessibilitySummary = accessibilitySummary,
            AccessibilitySummaryError = accessibilityError,
            AccessibilityAreaRows = accessibilityAreaRows,
            AccessibilityIssueCriteria = accessibilityIssueCriteria
        };
    }

    private static List<RagTrendMonthPoint> BuildRagTrend(
        List<Project> projects,
        Dictionary<int, List<ProjectRagHistory>> historyByProject,
        int reportYear,
        int reportMonth)
    {
        var list = new List<RagTrendMonthPoint>();
        var startMonth = new DateTime(2026, 1, 1);
        var reportMonthStart = new DateTime(reportYear, reportMonth, 1);
        for (var period = startMonth; period <= reportMonthStart; period = period.AddMonths(1))
        {
            var y = period.Year;
            var m = period.Month;
            var cutoff = new DateTime(y, m, 1).AddMonths(1);
            var label = new DateTime(y, m, 1).ToString("MMM yyyy");

            var dist = new Dictionary<string, int>
            {
                ["Red"] = 0,
                ["Amber-Red"] = 0,
                ["Amber-Green"] = 0,
                ["Green"] = 0,
                ["Not Set"] = 0
            };

            foreach (var p in projects)
            {
                var rag = ResolveRagAtCutoff(p, cutoff, historyByProject);
                if (string.IsNullOrWhiteSpace(rag) || string.Equals(rag, "Amber", StringComparison.OrdinalIgnoreCase))
                    dist["Not Set"]++;
                else if (dist.ContainsKey(rag))
                    dist[rag]++;
                else
                    dist["Not Set"]++;
            }

            list.Add(new RagTrendMonthPoint
            {
                Label = label,
                Year = y,
                Month = m,
                Red = dist["Red"],
                AmberRed = dist["Amber-Red"],
                AmberGreen = dist["Amber-Green"],
                Green = dist["Green"],
                NotSet = dist["Not Set"]
            });
        }

        for (var i = 1; i <= 3; i++)
        {
            var future = reportMonthStart.AddMonths(i);
            list.Add(new RagTrendMonthPoint
            {
                Label = future.ToString("MMM yyyy"),
                Year = future.Year,
                Month = future.Month,
                Red = 0,
                AmberRed = 0,
                AmberGreen = 0,
                Green = 0,
                NotSet = 0
            });
        }

        return list;
    }

    private static string ResolveRagAtCutoff(
        Project project,
        DateTime cutoff,
        Dictionary<int, List<ProjectRagHistory>> historyByProject)
    {
        if (!historyByProject.TryGetValue(project.Id, out var list))
            return NormalizeRagStatus(project.RagStatusLookup?.Name ?? project.RagStatus);

        var last = list.FirstOrDefault(h => h.ChangedAt < cutoff);
        if (last != null)
            return NormalizeRagStatus(last.RagStatusLookup?.Name ?? last.RagStatus);

        return NormalizeRagStatus(project.RagStatusLookup?.Name ?? project.RagStatus);
    }

    private async Task<Dictionary<string, int>> BuildPrevMonthRagDistributionAsync(
        List<Project> prevMonthProjects,
        DateTime currentMonthStart,
        CancellationToken cancellationToken)
    {
        var prevMonthProjectIds = prevMonthProjects.Select(p => p.Id).ToList();
        var ragHistoryUpToPrevMonth = await _db.ProjectRagHistories
            .AsNoTracking()
            .Include(rh => rh.RagStatusLookup)
            .Where(rh => rh.ChangedAt < currentMonthStart && prevMonthProjectIds.Contains(rh.ProjectId))
            .OrderByDescending(rh => rh.ChangedAt)
            .ToListAsync(cancellationToken);

        var projectRagStatusAtPrevMonthEnd = ragHistoryUpToPrevMonth
            .GroupBy(rh => rh.ProjectId)
            .ToDictionary(g => g.Key, g => NormalizeRagStatus(g.First().RagStatusLookup?.Name ?? g.First().RagStatus));

        var prevMonthRagDistribution = new Dictionary<string, int>
        {
            { "Red", 0 },
            { "Amber-Red", 0 },
            { "Amber-Green", 0 },
            { "Green", 0 },
            { "Not Set", 0 }
        };

        foreach (var project in prevMonthProjects)
        {
            string ragStatus;
            if (projectRagStatusAtPrevMonthEnd.TryGetValue(project.Id, out var fromHist))
                ragStatus = fromHist;
            else
                ragStatus = NormalizeRagStatus(project.RagStatusLookup?.Name ?? project.RagStatus);

            if (string.IsNullOrWhiteSpace(ragStatus))
                prevMonthRagDistribution["Not Set"]++;
            else if (string.Equals(ragStatus, "Amber", StringComparison.OrdinalIgnoreCase))
                prevMonthRagDistribution["Not Set"]++;
            else if (prevMonthRagDistribution.ContainsKey(ragStatus))
                prevMonthRagDistribution[ragStatus]++;
            else
                prevMonthRagDistribution["Not Set"]++;
        }

        return prevMonthRagDistribution;
    }

    private static Dictionary<string, int> BuildRagDistribution(List<Project> projects)
    {
        var ragDistribution = new Dictionary<string, int>
        {
            { "Red", 0 },
            { "Amber-Red", 0 },
            { "Amber-Green", 0 },
            { "Green", 0 },
            { "Not Set", 0 }
        };

        foreach (var project in projects)
        {
            var ragStatus = NormalizeRagStatus(project.RagStatusLookup?.Name ?? project.RagStatus);
            if (string.IsNullOrWhiteSpace(ragStatus))
                ragDistribution["Not Set"]++;
            else if (string.Equals(ragStatus, "Amber", StringComparison.OrdinalIgnoreCase))
                ragDistribution["Not Set"]++;
            else if (ragDistribution.ContainsKey(ragStatus))
                ragDistribution[ragStatus]++;
            else
                ragDistribution["Not Set"]++;
        }

        return ragDistribution;
    }

    private static Dictionary<string, int> BuildPriorityDistribution(List<Project> projects)
    {
        var priorityDistribution = new Dictionary<string, int>
        {
            { "Critical", 0 },
            { "High", 0 },
            { "Medium", 0 },
            { "Low", 0 },
            { "Not Set", 0 }
        };

        foreach (var project in projects)
        {
            var bucket = PriorityBucket(project);
            priorityDistribution[bucket]++;
        }

        return priorityDistribution;
    }

    private static string PriorityBucket(Project project)
    {
        if (project.DeliveryPriority == null)
            return "Not Set";
        var priorityName = project.DeliveryPriority.Name.ToLowerInvariant();
        if (priorityName.Contains("critical"))
            return "Critical";
        if (priorityName.Contains("high"))
            return "High";
        if (priorityName.Contains("medium"))
            return "Medium";
        if (priorityName.Contains("low"))
            return "Low";
        return "Not Set";
    }

    private static string RagBucket(Project project)
    {
        var ragStatus = NormalizeRagStatus(project.RagStatusLookup?.Name ?? project.RagStatus);
        if (string.IsNullOrWhiteSpace(ragStatus))
            return "Not Set";
        if (string.Equals(ragStatus, "Amber", StringComparison.OrdinalIgnoreCase))
            return "Not Set";
        return ragStatus is "Red" or "Amber-Red" or "Amber-Green" or "Green" ? ragStatus : "Not Set";
    }

    private static string NormalizeRagStatus(string? ragStatus)
    {
        if (string.IsNullOrWhiteSpace(ragStatus))
            return string.Empty;

        var normalized = ragStatus.Trim()
            .Replace(" / ", "-")
            .Replace("/", "-")
            .Replace(" /", "-")
            .Replace("/ ", "-");

        var parts = normalized.Split('-');
        for (var i = 0; i < parts.Length; i++)
        {
            if (parts[i].Length > 0)
            {
                parts[i] = char.ToUpperInvariant(parts[i][0]) +
                           (parts[i].Length > 1 ? parts[i][1..].ToLowerInvariant() : "");
            }
        }

        return string.Join("-", parts);
    }

    private static List<PriorityTrendMonthPoint> BuildPriorityTrend(
        List<Project> projects,
        int reportYear,
        int reportMonth)
    {
        var list = new List<PriorityTrendMonthPoint>();
        var startMonth = new DateTime(2026, 1, 1);
        var reportMonthStart = new DateTime(reportYear, reportMonth, 1);

        for (var period = startMonth; period <= reportMonthStart; period = period.AddMonths(1))
        {
            var label = period.ToString("MMM yyyy");
            var dist = new Dictionary<string, int>
            {
                ["Critical"] = 0,
                ["High"] = 0,
                ["Medium"] = 0,
                ["Low"] = 0,
                ["Not Set"] = 0
            };
            foreach (var p in projects)
            {
                var bucket = PriorityBucket(p);
                dist[bucket]++;
            }
            list.Add(new PriorityTrendMonthPoint
            {
                Label = label,
                Year = period.Year,
                Month = period.Month,
                Critical = dist["Critical"],
                High = dist["High"],
                Medium = dist["Medium"],
                Low = dist["Low"],
                NotSet = dist["Not Set"]
            });
        }

        for (var i = 1; i <= 3; i++)
        {
            var future = reportMonthStart.AddMonths(i);
            list.Add(new PriorityTrendMonthPoint
            {
                Label = future.ToString("MMM yyyy"),
                Year = future.Year,
                Month = future.Month,
                Critical = 0,
                High = 0,
                Medium = 0,
                Low = 0,
                NotSet = 0
            });
        }

        return list;
    }

    private static List<ProjectChangeRow> BuildRagChangeDetails(
        List<Project> projects,
        List<ProjectRagHistory> ragHistoryDuringMonth,
        Dictionary<int, List<ProjectRagHistory>> historyByProject,
        DateTime monthStart,
        int reportYear, int reportMonth)
    {
        var projectMap = projects.ToDictionary(p => p.Id);
        var changedProjectIds = ragHistoryDuringMonth
            .Where(rh => projectMap.ContainsKey(rh.ProjectId))
            .Select(rh => rh.ProjectId)
            .Distinct();

        var rows = new List<ProjectChangeRow>();
        foreach (var pid in changedProjectIds)
        {
            if (!projectMap.TryGetValue(pid, out var project)) continue;

            var fromRag = ResolveRagAtCutoff(project, monthStart, historyByProject);
            if (string.IsNullOrWhiteSpace(fromRag)) fromRag = "Not Set";
            else if (string.Equals(fromRag, "Amber", StringComparison.OrdinalIgnoreCase)) fromRag = "Not Set";

            var toRag = NormalizeRagStatus(project.RagStatusLookup?.Name ?? project.RagStatus);
            if (string.IsNullOrWhiteSpace(toRag)) toRag = "Not Set";
            else if (string.Equals(toRag, "Amber", StringComparison.OrdinalIgnoreCase)) toRag = "Not Set";

            if (fromRag == toRag) continue;

            var lastChange = ragHistoryDuringMonth
                .Where(rh => rh.ProjectId == pid)
                .OrderByDescending(rh => rh.ChangedAt)
                .First();

            var latestUpdate = project.MonthlyUpdates?
                .Where(u => u.Year == reportYear && u.Month == reportMonth)
                .OrderByDescending(u => u.SubmittedAt ?? u.CreatedAt)
                .FirstOrDefault()
                ?? project.MonthlyUpdates?
                    .OrderByDescending(u => u.Year * 100 + u.Month)
                    .FirstOrDefault();

            rows.Add(new ProjectChangeRow
            {
                ProjectId = project.Id,
                Title = project.Title,
                BusinessArea = project.BusinessAreaLookup?.Name,
                From = fromRag,
                To = toRag,
                ChangedAt = lastChange.ChangedAt,
                Justification = !string.IsNullOrWhiteSpace(lastChange.Justification)
                    ? lastChange.Justification
                    : project.RagJustification,
                RagJustification = project.RagJustification,
                LatestNarrative = latestUpdate != null ? ProjectMonthlyUpdateNarrative.Compose(latestUpdate) : null
            });
        }
        return rows.OrderBy(r => RagSortOrder(r.To)).ThenBy(r => r.Title).ToList();
    }

    private static List<ProjectChangeRow> BuildPriorityChangeDetails(
        List<Project> currentProjects,
        List<Project> prevMonthProjects,
        DateTime monthStart, DateTime monthEnd,
        int reportYear, int reportMonth)
    {
        var prevMap = prevMonthProjects.ToDictionary(p => p.Id);
        var rows = new List<ProjectChangeRow>();
        foreach (var p in currentProjects)
        {
            if (p.UpdatedAt < monthStart || p.UpdatedAt > monthEnd) continue;
            if (!prevMap.TryGetValue(p.Id, out var prev)) continue;

            var fromPri = PriorityBucket(prev);
            var toPri = PriorityBucket(p);
            if (fromPri == toPri) continue;

            var latestUpdate = p.MonthlyUpdates?
                .Where(u => u.Year == reportYear && u.Month == reportMonth)
                .OrderByDescending(u => u.SubmittedAt ?? u.CreatedAt)
                .FirstOrDefault()
                ?? p.MonthlyUpdates?
                    .OrderByDescending(u => u.Year * 100 + u.Month)
                    .FirstOrDefault();

            rows.Add(new ProjectChangeRow
            {
                ProjectId = p.Id,
                Title = p.Title,
                BusinessArea = p.BusinessAreaLookup?.Name,
                From = fromPri,
                To = toPri,
                ChangedAt = p.UpdatedAt,
                Justification = p.DeliveryPriorityChangeReason,
                RagJustification = p.RagJustification,
                LatestNarrative = latestUpdate != null ? ProjectMonthlyUpdateNarrative.Compose(latestUpdate) : null
            });
        }
        return rows.OrderBy(r => r.Title).ToList();
    }

    private static int RagSortOrder(string rag) => rag switch
    {
        "Red" => 1,
        "Amber-Red" => 2,
        "Amber-Green" => 3,
        "Green" => 4,
        _ => 5
    };

    private static MonthlyUpdateStats CalculateMonthlyUpdateStats(
        List<Project> projects,
        int year,
        int month,
        IMonthlyUpdateService monthlyUpdateService)
    {
        var totalProjects = projects.Count;
        var dueDate = monthlyUpdateService.GetMonthlyUpdateDueDate(year, month);
        var currentDate = DateTime.UtcNow;

        var submitted = 0;
        var notStarted = 0;
        var inProgress = 0;
        var late = 0;

        foreach (var project in projects)
        {
            var update = project.MonthlyUpdates?.FirstOrDefault(u => u.Year == year && u.Month == month);

            if (update != null && update.SubmittedAt.HasValue)
                submitted++;
            else if (currentDate > dueDate)
                late++;
            else if (update != null && !update.SubmittedAt.HasValue)
                inProgress++;
            else
                notStarted++;
        }

        return new MonthlyUpdateStats
        {
            Year = year,
            Month = month,
            TotalProjects = totalProjects,
            Submitted = submitted,
            NotStarted = notStarted,
            InProgress = inProgress,
            Late = late,
            DueDate = dueDate
        };
    }

    /// <summary>
    /// Plain-language narrative for the single–business-area monthly report. Tone is suggestive, not prescriptive; data-driven prompts only.
    /// </summary>
    private static string BuildBusinessAreaSummaryNarrative(
        string businessAreaName,
        string monthDisplay,
        DateTime monthStart,
        int reportYear,
        int reportMonth,
        IReadOnlyList<Project> projects,
        int newThisMonthCount,
        int milestonesAchievedCount,
        IReadOnlyList<MilestoneWithProject> upcomingMilestones,
        IReadOnlyList<MilestoneWithProject> lateMilestones,
        MonthlyUpdateStats? submissionStats,
        Dictionary<string, int> ragDistribution,
        Dictionary<string, int> priorityDistribution,
        Dictionary<string, int> prevMonthRagDistribution,
        string prevMonthName,
        IReadOnlyList<RagTrendMonthPoint> ragTrend,
        int pathToGreenCount,
        int ragChangeProjects,
        int priorityChangeProjects,
        List<ProjectChangeRow> ragChanges,
        List<ProjectChangeRow> priorityChanges)
    {
        static int RagRiskOrder(string rag) => rag switch
        {
            "Green" => 5,
            "Amber-Green" => 4,
            "Amber-Red" => 2,
            "Red" => 1,
            _ => 3
        };

        static bool RagWorsened(string from, string to) =>
            RagRiskOrder(to) < RagRiskOrder(from);

        static bool RagImproved(string from, string to) =>
            RagRiskOrder(to) > RagRiskOrder(from);

        var totalActive = projects.Count;
        var upcomingMilestonesCount = upcomingMilestones.Count;
        var lateMilestonesCount = lateMilestones.Count;
        var paragraphs = new List<string>();

        if (totalActive == 0)
        {
            paragraphs.Add(
                $"There are no active work items attributed to {businessAreaName} in Compass for {monthDisplay} with the current filters.");
            return string.Join("\n\n", paragraphs);
        }

        paragraphs.Add(
            $"For {monthDisplay}, {businessAreaName} has {totalActive} active work item{(totalActive == 1 ? "" : "s")}. " +
            $"{(newThisMonthCount > 0 ? $"{newThisMonthCount} new item{(newThisMonthCount == 1 ? "" : "s")} started in this month. " : "")}" +
            "The figures on this page summarise returns, RAG, priority, and milestones for this view.");

        if (submissionStats != null && submissionStats.TotalProjects > 0)
        {
            var due = submissionStats.DueDate.ToString("d MMMM yyyy");
            var pct = Math.Round(100m * submissionStats.Submitted / submissionStats.TotalProjects, 1, MidpointRounding.AwayFromZero);
            var submissionSentence =
                $"Monthly returns for {monthDisplay} were due {due}. " +
                $"Of {submissionStats.TotalProjects} work item{(submissionStats.TotalProjects == 1 ? "" : "s")} in scope, " +
                $"{submissionStats.Submitted} ({pct}%) {(submissionStats.Submitted == 1 ? "was" : "were")} submitted.";
            if (submissionStats.InProgress > 0)
                submissionSentence += $" {submissionStats.InProgress} {(submissionStats.InProgress == 1 ? "has" : "have")} a return in progress.";
            if (submissionStats.NotStarted > 0)
                submissionSentence +=
                    $" {submissionStats.NotStarted} {(submissionStats.NotStarted == 1 ? "has" : "have")} not yet started a return for this period (within the window reflected here).";
            if (submissionStats.Late > 0)
                submissionSentence +=
                    $" {submissionStats.Late} {(submissionStats.Late == 1 ? "was" : "were")} still unreturned after the due date—if that still applies, a light touch with the relevant delivery lead may help clear the line.";
            else if (submissionStats.Submitted == submissionStats.TotalProjects)
                submissionSentence += " Returns are in for all items in scope for this period.";
            paragraphs.Add(submissionSentence);
        }
        else if (submissionStats != null && submissionStats.TotalProjects == 0)
            paragraphs.Add("No work items in this view were in scope for monthly returns for the selected period.");

        var stableRag = TryDescribeRagMixStability(ragTrend, reportYear, reportMonth);
        if (!string.IsNullOrEmpty(stableRag))
            paragraphs.Add(stableRag);

        var milestoneParts = new List<string>();
        if (milestonesAchievedCount > 0)
            milestoneParts.Add(
                $"{milestonesAchievedCount} milestone{(milestonesAchievedCount == 1 ? "" : "s")} were recorded complete in {monthDisplay}");
        if (upcomingMilestonesCount > 0)
        {
            var uPlural = upcomingMilestonesCount == 1 ? "" : "s";
            var uVerb = upcomingMilestonesCount == 1 ? "is" : "are";
            milestoneParts.Add(
                $"{upcomingMilestonesCount} open milestone{uPlural} {uVerb} due in the 30 days from the start of the month. {DescribeUpcomingMilestoneHint(upcomingMilestones, monthStart)}");
        }
        if (lateMilestonesCount > 0)
        {
            var lPlural = lateMilestonesCount == 1 ? "" : "s";
            var lVerb = lateMilestonesCount == 1 ? "is" : "are";
            milestoneParts.Add(
                $"{lateMilestonesCount} open milestone{lPlural} {lVerb} past due. {DescribeLateMilestonesHint(lateMilestones, monthStart)}");
        }
        if (milestoneParts.Count > 0)
            paragraphs.Add("Milestones: " + string.Join("; ", milestoneParts) + ".");

        var staleMilestoneCount = CountStaleOpenMilestones(projects, monthStart, staleBefore: monthStart.AddDays(-56));
        if (staleMilestoneCount > 0)
        {
            paragraphs.Add(
                $"{staleMilestoneCount} open milestone{(staleMilestoneCount == 1 ? "" : "s")} {(staleMilestoneCount == 1 ? "has" : "have")} not been updated in Compass for at least eight weeks before the start of {monthDisplay}. A quick refresh of dates or status in the plan may be worthwhile.");
        }

        var ragRed = ragDistribution.GetValueOrDefault("Red");
        var ragAmbr = ragDistribution.GetValueOrDefault("Amber-Red");
        var ragAmg = ragDistribution.GetValueOrDefault("Amber-Green");
        var ragGreen = ragDistribution.GetValueOrDefault("Green");
        var prevRed = prevMonthRagDistribution.GetValueOrDefault("Red");
        if (ragRed + ragAmbr + ragAmg + ragGreen + ragDistribution.GetValueOrDefault("Not Set") > 0)
        {
            var mixParts = new List<string>();
            if (ragRed + ragAmbr > 0)
                mixParts.Add(
                    $"{ragRed + ragAmbr} at Red or Amber–Red ({ragRed} Red, {ragAmbr} Amber–Red)");
            mixParts.Add($"{ragAmg} Amber–Green, {ragGreen} Green");
            var ragSentence = "RAG: " + string.Join("; ", mixParts) + ".";
            if (ragRed != prevRed)
            {
                ragSentence +=
                    $" Compared with {prevMonthName}, the count in Red moved from {prevRed} to {ragRed}.";
            }
            paragraphs.Add(ragSentence);

            var lowPriElevatedRag = CountElevatedRagWithLowerPriority(projects);
            if (lowPriElevatedRag > 0)
            {
                paragraphs.Add(
                    $"{lowPriElevatedRag} work item{(lowPriElevatedRag == 1 ? "" : "s")} {(lowPriElevatedRag == 1 ? "shows" : "show")} Red or Amber–Red while delivery priority is Medium, Low, not set, or marked optional. That pairing can be fine, but you may want to confirm that priority still reflects the assurance signal—or adjust one or the other for clarity.");
            }
        }

        var crit = priorityDistribution.GetValueOrDefault("Critical");
        var high = priorityDistribution.GetValueOrDefault("High");
        if (crit + high > 0)
            paragraphs.Add(
                $"Delivery priority: {crit} Critical, {high} High (among others in the table). These counts are a prompt for where attention may already be formalised.");

        var worsening = ragChanges.Count(r => RagWorsened(r.From, r.To));
        var improving = ragChanges.Count(r => RagImproved(r.From, r.To));
        var priChanged = priorityChanges.Count;
        var movementParts = new List<string>();
        if (ragChangeProjects > 0)
            movementParts.Add(
                $"{ragChangeProjects} work item{(ragChangeProjects == 1 ? "" : "s")} had a RAG change recorded in the month" +
                (worsening > 0 || improving > 0
                    ? $" ({worsening} where the signal moved toward red, {improving} toward green, where the history allows a clean read)"
                    : ""));
        if (priChanged > 0)
            movementParts.Add($"{priChanged} delivery priority change{(priChanged == 1 ? "" : "s")} in the period");
        if (pathToGreenCount > 0)
            movementParts.Add(
                $"{pathToGreenCount} non-green work item{(pathToGreenCount == 1 ? "" : "s")} {(pathToGreenCount == 1 ? "has" : "have")} path-to-green text—useful as a thread for follow-up, not a test of pass or fail");
        if (movementParts.Count > 0)
            paragraphs.Add("Movement: " + string.Join("; ", movementParts) + ".");

        return string.Join("\n\n", paragraphs);
    }

    /// <summary>
    /// Red or Amber–Red with medium/low/unset/optional-style priority (not Critical/High).
    /// </summary>
    private static int CountElevatedRagWithLowerPriority(IEnumerable<Project> projects) =>
        projects.Count(p =>
        {
            var r = RagBucket(p);
            if (r is not ("Red" or "Amber-Red"))
                return false;
            return IsLowerOrUnsetDeliveryPriority(p);
        });

    private static bool IsLowerOrUnsetDeliveryPriority(Project project)
    {
        if (IsOptionalPriorityName(project))
            return true;
        return PriorityBucket(project) is "Low" or "Medium" or "Not Set";
    }

    private static bool IsOptionalPriorityName(Project project)
    {
        var name = project.DeliveryPriority?.Name;
        if (string.IsNullOrWhiteSpace(name))
            return false;
        return name.Contains("optional", StringComparison.OrdinalIgnoreCase);
    }

    private static int CountStaleOpenMilestones(
        IReadOnlyList<Project> projects,
        DateTime monthStart,
        DateTime staleBefore)
    {
        var n = 0;
        foreach (var p in projects)
        {
            if (p.Milestones == null) continue;
            foreach (var m in p.Milestones)
            {
                if (m.IsDeleted) continue;
                if (m.Status is "complete" or "cancelled")
                    continue;
                if (m.UpdatedAt >= staleBefore)
                    continue;
                n++;
            }
        }
        return n;
    }

    private static string? TryDescribeRagMixStability(
        IReadOnlyList<RagTrendMonthPoint> trend,
        int reportYear,
        int reportMonth)
    {
        var historical = trend
            .Where(p => p.Year < reportYear || (p.Year == reportYear && p.Month <= reportMonth))
            .OrderBy(p => p.Year).ThenBy(p => p.Month)
            .ToList();
        if (historical.Count < 2)
            return null;

        const int wantMonths = 3;
        var take = Math.Min(wantMonths, historical.Count);
        var slice = historical.TakeLast(take).ToList();
        if (slice.Count < 2)
            return null;

        var first = slice[0];
        bool flat = slice.All(p =>
            p.Red == first.Red && p.AmberRed == first.AmberRed && p.AmberGreen == first.AmberGreen
            && p.Green == first.Green && p.NotSet == first.NotSet);
        if (!flat)
            return null;

        var n = slice.Count;
        return
            $"The RAG mix in this business area has been the same for the last {n} month{(n == 1 ? "" : "s")} in Compass (from {first.Label} to {slice[^1].Label}). " +
            "If that still matches how teams feel on the ground, no change is implied—it's simply a prompt to sense-check if needed.";
    }

    private static string DescribeUpcomingMilestoneHint(
        IReadOnlyList<MilestoneWithProject> upcoming,
        DateTime monthStart)
    {
        if (upcoming.Count == 0)
            return "You may want to line these up with team plans when convenient.";
        var soonest = upcoming.Min(x => x.Milestone.DueDate);
        if (soonest <= monthStart.AddDays(7))
            return "At least one is very soon, so a short check-in with owners may help.";
        return "Worth a quick glance at upcoming commitments when you next speak with teams.";
    }

    private static string DescribeLateMilestonesHint(
        IReadOnlyList<MilestoneWithProject> late,
        DateTime monthStart)
    {
        if (late.Count == 0)
            return "Confirming next steps with owners can help, if still relevant.";
        var oldest = late.Min(x => x.Milestone.DueDate);
        if (oldest < monthStart.AddMonths(-2))
            return "Some have been open a long time; a light refresh of dates or ownership in the plan may be useful.";
        return "A quick look at whether dates or owners still feel right may be enough.";
    }

}
