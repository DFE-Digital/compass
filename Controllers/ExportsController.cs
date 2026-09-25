using System.Text;
using System.Text.Json;
using ClosedXML.Excel;
using Compass.Data;
using Compass.Helpers;
using Compass.Models;
using Compass.Models.DemandTriage;
using Compass.Services;
using Compass.Services.Modern;
using Compass.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Compass.Controllers;

/// <summary>Data exports hub — <c>/Exports</c>. Used by Work and Demand sub-navigation.</summary>
[Authorize]
public class ExportsController : Controller
{
    private readonly CompassDbContext _db;
    private readonly IGlobalFeatureToggleService _features;
    private readonly IMonthlyUpdateService _monthlyUpdateService;

    public ExportsController(
        CompassDbContext db,
        IGlobalFeatureToggleService features,
        IMonthlyUpdateService monthlyUpdateService)
    {
        _db = db;
        _features = features;
        _monthlyUpdateService = monthlyUpdateService;
    }

    private bool IsDemandGloballyActive()
    {
        var row = _db.Features.AsNoTracking().FirstOrDefault(f => f.Code == FeatureCodes.Demand);
        return row == null || row.IsActive;
    }

    /// <summary>Exports landing. <paramref name="section"/> drives which area nav is active (reporting, work, performance, demand).</summary>
    [HttpGet]
    public async Task<IActionResult> Index(string? section = "reporting")
    {
        var s = (section ?? "reporting").Trim().ToLowerInvariant();
        if (s is not ("work" or "demand" or "performance" or "reporting"))
            s = "reporting";

        if (s == "demand")
        {
            if (!await _features.IsFeatureEnabledForPrincipalAsync(FeatureCodes.Demand, User))
                return RedirectToAction(nameof(Index), new { section = "reporting" });

            ViewBag.MainNavSection = "demand";
            ViewBag.SubNavItem = "demand-dashboard";
        }
        else if (s == "work")
        {
            ViewBag.MainNavSection = "work";
            ViewBag.SubNavItem = "work-allwork";
        }
        else if (s == "performance")
        {
            ViewBag.MainNavSection = "performance";
            ViewBag.SubNavItem = "perf-dashboard";
        }
        else
        {
            ViewBag.MainNavSection = "reporting";
            ViewBag.SubNavItem = "reporting-exports";
        }

        ViewBag.ExportsSection = s;
        var demandEnabledForUser = await _features.IsFeatureEnabledForPrincipalAsync(FeatureCodes.Demand, User);
        ViewBag.ShowDemandExportRows = IsDemandGloballyActive() && demandEnabledForUser;

        return View("~/Views/Modern/Exports/Index.cshtml");
    }

    [HttpGet]
    public Task<IActionResult> DownloadDemandExcel(CancellationToken cancellationToken = default)
        => DownloadExcel(cancellationToken);

    [HttpGet]
    public async Task<IActionResult> DownloadWorkExcel(CancellationToken cancellationToken = default)
    {
        using var wb = new XLWorkbook();

        var projects = await _db.Projects.AsNoTracking()
            .Where(p => !p.IsDeleted)
            .Include(p => p.PhaseLookup)
            .Include(p => p.BusinessAreaLookup)
            .Include(p => p.RagStatusLookup)
            .Include(p => p.DeliveryPriority)
            .Include(p => p.ActivityTypeLookup)
            .Include(p => p.RiskAppetiteLookup)
            .Include(p => p.PrimaryOrganizationalGroup)
            .Include(p => p.PrimaryContactUser)
            .OrderBy(p => p.Title)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        var projectIds = projects.Select(p => p.Id).ToList();

        var monthlyAll = await _db.ProjectMonthlyUpdates.AsNoTracking()
            .Where(m => projectIds.Contains(m.ProjectId))
            .OrderByDescending(m => m.Year).ThenByDescending(m => m.Month)
            .ToListAsync(cancellationToken);
        var monthlyByProjectId = monthlyAll
            .GroupBy(m => m.ProjectId)
            .ToDictionary(g => g.Key, g => g.ToList());
        foreach (var project in projects)
        {
            project.MonthlyUpdates = monthlyByProjectId.TryGetValue(project.Id, out var updates)
                ? updates
                : new List<ProjectMonthlyUpdate>();
        }

        var (reportYear, reportMonth) = _monthlyUpdateService.ResolveDashboardReportingPeriod(DateTime.UtcNow);
        var periodColumns = await WorkRegisterMonthlySubmissionExportHelper.LoadPeriodColumnsAsync(
            _db,
            reportYear,
            reportMonth,
            WorkRegisterMonthlySubmissionExportHelper.DefaultMinReportYear,
            cancellationToken);
        var periodStatusesByProject = WorkRegisterMonthlySubmissionExportHelper.BuildPeriodStatusesByProject(
            projects,
            periodColumns);

        var latestMuByProject = monthlyAll
            .GroupBy(m => m.ProjectId)
            .ToDictionary(g => g.Key, g => g.First());

        var latestMuIds = latestMuByProject.Values.Select(m => m.Id).ToList();
        var latestDraftMeta = latestMuIds.Count == 0
            ? new Dictionary<int, ProjectMonthlyUpdate>()
            : await _db.ProjectMonthlyUpdates.AsNoTracking()
                .Where(m => latestMuIds.Contains(m.Id))
                .Include(m => m.DraftRagStatusLookup)
                .ToDictionaryAsync(m => m.Id, cancellationToken);

        var narrativeRows = latestMuIds.Count == 0
            ? new List<MonthlyUpdateNarrative>()
            : await _db.MonthlyUpdateNarratives.AsNoTracking()
                .Where(n => latestMuIds.Contains(n.ProjectMonthlyUpdateId))
                .OrderBy(n => n.ProjectMonthlyUpdateId).ThenBy(n => n.Id)
                .ToListAsync(cancellationToken);
        var extraNarrativeByMuId = narrativeRows
            .GroupBy(n => n.ProjectMonthlyUpdateId)
            .ToDictionary(g => g.Key, g => string.Join("\n---\n", g.Select(x => x.Narrative)));

        var contacts = await _db.ProjectContacts.AsNoTracking()
            .Where(c => projectIds.Contains(c.ProjectId))
            .OrderBy(c => c.ProjectId).ThenBy(c => c.SortOrder)
            .ToListAsync(cancellationToken);
        var contactsByProject = contacts
            .GroupBy(c => c.ProjectId)
            .ToDictionary(
                g => g.Key,
                g => string.Join("; ", g.Select(x => $"{x.Role}: {x.Name} <{x.Email}>")));

        var sros = await _db.ProjectSeniorResponsibleOfficers.AsNoTracking()
            .Where(s => projectIds.Contains(s.ProjectId))
            .Include(s => s.User)
            .ToListAsync(cancellationToken);
        var sroByProject = sros
            .GroupBy(s => s.ProjectId)
            .ToDictionary(
                g => g.Key,
                g => string.Join("; ", g.Select(x => UserDisplay(x.User))));

        var svcOwners = await _db.ProjectServiceOwners.AsNoTracking()
            .Where(s => projectIds.Contains(s.ProjectId))
            .Include(s => s.User)
            .ToListAsync(cancellationToken);
        var svcByProject = svcOwners
            .GroupBy(s => s.ProjectId)
            .ToDictionary(
                g => g.Key,
                g => string.Join("; ", g.Select(x => UserDisplay(x.User))));

        var pmo = await _db.ProjectPmoContacts.AsNoTracking()
            .Where(s => projectIds.Contains(s.ProjectId))
            .Include(s => s.User)
            .ToListAsync(cancellationToken);
        var pmoByProject = pmo
            .GroupBy(s => s.ProjectId)
            .ToDictionary(
                g => g.Key,
                g => string.Join("; ", g.Select(x => UserDisplay(x.User))));

        var directorates = await _db.ProjectDirectorates.AsNoTracking()
            .Where(d => projectIds.Contains(d.ProjectId))
            .Include(d => d.Division)
            .ToListAsync(cancellationToken);
        var directoratesByProject = directorates
            .GroupBy(d => d.ProjectId)
            .ToDictionary(g => g.Key, g => (ICollection<ProjectDirectorate>)g.ToList());

        var missions = await _db.ProjectMissions.AsNoTracking()
            .Where(pm => projectIds.Contains(pm.ProjectId))
            .Include(pm => pm.Mission)
            .ToListAsync(cancellationToken);
        var missionsByProject = missions
            .Where(pm => pm.Mission != null && !pm.Mission.IsDeleted)
            .GroupBy(pm => pm.ProjectId)
            .ToDictionary(g => g.Key, g => (ICollection<ProjectMission>)g.ToList());

        var objectives = await _db.ProjectObjectives.AsNoTracking()
            .Where(po => projectIds.Contains(po.ProjectId))
            .Include(po => po.Objective)
            .ToListAsync(cancellationToken);
        var objectivesByProject = objectives
            .Where(po => po.Objective != null && !po.Objective.IsDeleted)
            .GroupBy(po => po.ProjectId)
            .ToDictionary(g => g.Key, g => (ICollection<ProjectObjective>)g.ToList());

        var problemStatements = await _db.ProjectProblemStatements.AsNoTracking()
            .Where(ps => projectIds.Contains(ps.ProjectId))
            .ToListAsync(cancellationToken);
        var problemByProject = problemStatements
            .GroupBy(ps => ps.ProjectId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(ps => ps.UpdatedAt).First().ProblemStatement);

        var budgetOwners = await _db.ProjectBudgetOwners.AsNoTracking()
            .Where(b => projectIds.Contains(b.ProjectId))
            .Include(b => b.BusinessAreaLookup)
            .ToListAsync(cancellationToken);
        var budgetByProject = budgetOwners
            .GroupBy(b => b.ProjectId)
            .ToDictionary(
                g => g.Key,
                g => string.Join("; ", g
                    .Select(x => x.BusinessAreaLookup?.Name?.Trim() ?? "")
                    .Where(n => n.Length > 0)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(n => n)));

        var departmentIds = projects
            .SelectMany(p => ParseDepartmentIds(p.OtherDepartments))
            .Distinct()
            .ToList();
        var departmentNames = departmentIds.Count == 0
            ? new Dictionary<int, string>()
            : await _db.GovernmentDepartments.AsNoTracking()
                .Where(g => departmentIds.Contains(g.Id))
                .ToDictionaryAsync(g => g.Id, g => g.Title, cancellationToken);
        var multiDeptByProject = projects.ToDictionary(
            p => p.Id,
            p => string.Join("; ", ParseDepartmentIds(p.OtherDepartments)
                .Select(id => departmentNames.TryGetValue(id, out var name) ? name : "")
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct(StringComparer.OrdinalIgnoreCase)));

        var weeklyAll = await _db.ProjectWeeklyWorkUpdates.AsNoTracking()
            .Where(w => projectIds.Contains(w.ProjectId))
            .Include(w => w.DraftRagStatusLookup)
            .OrderByDescending(w => w.IsoYear).ThenByDescending(w => w.IsoWeek)
            .ThenByDescending(w => w.SubmittedAt ?? w.UpdatedAt ?? w.CreatedAt)
            .ToListAsync(cancellationToken);
        var latestWeeklyByProject = weeklyAll
            .GroupBy(w => w.ProjectId)
            .ToDictionary(g => g.Key, g => g.First());
        var currentFteByProject = new Dictionary<int, ProjectMonthlyUpdate>();
        foreach (var group in monthlyAll.GroupBy(m => m.ProjectId))
        {
            var picked = PickCurrentFteUpdate(group);
            if (picked != null)
                currentFteByProject[group.Key] = picked;
        }

        foreach (var project in projects)
        {
            project.Directorates = directoratesByProject.TryGetValue(project.Id, out var dirs)
                ? dirs
                : new List<ProjectDirectorate>();
            project.ProjectMissions = missionsByProject.TryGetValue(project.Id, out var pms)
                ? pms
                : new List<ProjectMission>();
            project.ProjectObjectives = objectivesByProject.TryGetValue(project.Id, out var pos)
                ? pos
                : new List<ProjectObjective>();
        }

        var tags = await _db.ProjectWorkItemTags.AsNoTracking()
            .Where(t => projectIds.Contains(t.ProjectId))
            .Include(t => t.WorkItemTagLookup)
            .ToListAsync(cancellationToken);
        var tagsByProject = tags
            .GroupBy(t => t.ProjectId)
            .ToDictionary(
                g => g.Key,
                g => string.Join("; ", g
                    .Where(x => x.WorkItemTagLookup != null && x.WorkItemTagLookup.IsActive)
                    .OrderBy(x => x.WorkItemTagLookup!.SortOrder)
                    .ThenBy(x => x.WorkItemTagLookup!.Name)
                    .Select(x => x.WorkItemTagLookup!.Name)));

        var products = await _db.ProjectProducts.AsNoTracking()
            .Where(pp => projectIds.Contains(pp.ProjectId))
            .ToListAsync(cancellationToken);
        var productsByProject = products
            .GroupBy(pp => pp.ProjectId)
            .ToDictionary(
                g => g.Key,
                g => string.Join("; ", g.Select(x =>
                    string.IsNullOrWhiteSpace(x.ProductFipsId)
                        ? x.ProductTitle
                        : $"{x.ProductTitle} ({x.ProductFipsId})")));

        var openRiskCounts = await _db.Risks.AsNoTracking()
            .Where(r => !r.IsDeleted && r.ClosedDate == null && r.ProjectId != null && projectIds.Contains(r.ProjectId.Value))
            .GroupBy(r => r.ProjectId!.Value)
            .Select(g => new { ProjectId = g.Key, C = g.Count() })
            .ToListAsync(cancellationToken);
        var openRiskDict = openRiskCounts.ToDictionary(x => x.ProjectId, x => x.C);

        var openIssueCounts = await _db.Issues.AsNoTracking()
            .Where(r => !r.IsDeleted && r.ClosedDate == null && r.ProjectId != null && projectIds.Contains(r.ProjectId.Value))
            .GroupBy(r => r.ProjectId!.Value)
            .Select(g => new { ProjectId = g.Key, C = g.Count() })
            .ToListAsync(cancellationToken);
        var openIssueDict = openIssueCounts.ToDictionary(x => x.ProjectId, x => x.C);

        var asmCounts = await _db.Assumptions.AsNoTracking()
            .Where(a => !a.IsDeleted && a.ProjectId != null && projectIds.Contains(a.ProjectId.Value))
            .GroupBy(a => a.ProjectId!.Value)
            .Select(g => new { ProjectId = g.Key, C = g.Count() })
            .ToListAsync(cancellationToken);
        var asmDict = asmCounts.ToDictionary(x => x.ProjectId, x => x.C);

        var msCounts = await _db.Milestones.AsNoTracking()
            .Where(m => m.ProjectId != null && !m.IsDeleted && projectIds.Contains(m.ProjectId.Value))
            .GroupBy(m => m.ProjectId!.Value)
            .Select(g => new { ProjectId = g.Key, C = g.Count() })
            .ToListAsync(cancellationToken);
        var msDict = msCounts.ToDictionary(x => x.ProjectId, x => x.C);

        var prodCounts = products
            .GroupBy(p => p.ProjectId)
            .ToDictionary(g => g.Key, g => g.Count());

        var wsMaster = wb.AddWorksheet("Work items (master)");
        WriteWorkItemsMasterSheet(
            wsMaster,
            projects,
            latestMuByProject,
            latestDraftMeta,
            extraNarrativeByMuId,
            contactsByProject,
            sroByProject,
            svcByProject,
            pmoByProject,
            tagsByProject,
            productsByProject,
            prodCounts,
            openRiskDict,
            openIssueDict,
            asmDict,
            msDict,
            periodColumns,
            periodStatusesByProject,
            problemByProject,
            multiDeptByProject,
            budgetByProject,
            latestWeeklyByProject,
            currentFteByProject);

        var milestones = await _db.Milestones.AsNoTracking()
            .Include(m => m.RagStatusLookup)
            .Where(m => m.ProjectId != null && !m.IsDeleted && projectIds.Contains(m.ProjectId.Value))
            .OrderBy(m => m.ProjectId).ThenBy(m => m.DueDate)
            .ToListAsync(cancellationToken);
        var workItemTitles = projects.ToDictionary(p => p.Id, p => p.Title);
        var wsMilestones = wb.AddWorksheet("Milestones");
        WorkRegisterExcelExport.WriteMilestonesSheet(wsMilestones, milestones, workItemTitles);

        var monthlyDetailed = await _db.ProjectMonthlyUpdates.AsNoTracking()
            .Include(m => m.DraftRagStatusLookup)
            .Where(m => projectIds.Contains(m.ProjectId))
            .OrderBy(m => m.ProjectId).ThenByDescending(m => m.Year).ThenByDescending(m => m.Month)
            .ToListAsync(cancellationToken);
        var muIdsAll = monthlyDetailed.Select(m => m.Id).ToList();
        var narrAll = muIdsAll.Count == 0
            ? new List<MonthlyUpdateNarrative>()
            : await _db.MonthlyUpdateNarratives.AsNoTracking()
                .Where(n => muIdsAll.Contains(n.ProjectMonthlyUpdateId))
                .OrderBy(n => n.ProjectMonthlyUpdateId).ThenBy(n => n.Id)
                .ToListAsync(cancellationToken);
        var narrBlocksAll = narrAll
            .GroupBy(n => n.ProjectMonthlyUpdateId)
            .ToDictionary(g => g.Key, g => string.Join("\n---\n", g.Select(x => x.Narrative)));

        var codeTitle = projects.ToDictionary(p => p.Id, p => (Code: p.ProjectCode, Title: p.Title));
        var wsMonthly = wb.AddWorksheet("Monthly updates (history)");
        WriteProjectMonthlyUpdatesDetailed(wsMonthly, monthlyDetailed, narrBlocksAll, codeTitle);

        var wsWeekly = wb.AddWorksheet("Weekly updates (history)");
        WriteProjectWeeklyUpdatesDetailed(wsWeekly, weeklyAll, codeTitle);

        await WriteWorkRisksSheetAsync(wb.AddWorksheet("Risks"), projectIds, codeTitle, cancellationToken);
        await WriteWorkIssuesSheetAsync(wb.AddWorksheet("Issues"), projectIds, codeTitle, cancellationToken);
        await WriteWorkAssumptionsSheetAsync(wb.AddWorksheet("Assumptions"), projectIds, codeTitle, cancellationToken);
        await WriteWorkDependenciesSheetAsync(wb.AddWorksheet("Dependencies"), projectIds, codeTitle, cancellationToken);

        using var stream = new MemoryStream();
        wb.SaveAs(stream);
        stream.Position = 0;
        var fileName = $"Compass-work-export-{DateTime.UtcNow:yyyyMMdd-HHmm}.xlsx";
        return File(
            stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }

    private static string UserDisplay(User? u)
    {
        if (u == null) return "";
        return string.IsNullOrWhiteSpace(u.Name) ? (u.Email ?? "") : $"{u.Name} ({u.Email})";
    }

    /// <summary>
    /// Every linked directorate name. Primary first when one is designated, otherwise the full list
    /// (the values previously exported as Divisions).
    /// </summary>
    private static string FormatDirectorateColumn(Project project)
    {
        var primary = WorkStrategicAlignmentExport.GetPrimaryDirectorateName(project);
        var additional = WorkStrategicAlignmentExport.GetAdditionalDirectorateNames(project);
        if (string.IsNullOrEmpty(primary))
            return additional;
        if (string.IsNullOrEmpty(additional))
            return primary;
        return string.Join(WorkStrategicAlignmentExport.MultiValueDelimiter, primary, additional);
    }

    /// <summary>Non-primary directorates only. Empty when no primary is designated, because those names are already in Directorate.</summary>
    private static string FormatAdditionalDirectoratesColumn(Project project)
    {
        if (string.IsNullOrEmpty(WorkStrategicAlignmentExport.GetPrimaryDirectorateName(project)))
            return "";
        return WorkStrategicAlignmentExport.GetAdditionalDirectorateNames(project);
    }

    /// <summary>Linked strategic-alignment names, falling back to legacy free text when nothing is linked.</summary>
    private static string AlignmentOrLegacy(string joined, string? legacy)
    {
        if (!string.IsNullOrWhiteSpace(joined))
            return joined;
        return legacy?.Trim() ?? "";
    }

    private static List<int> ParseDepartmentIds(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new List<int>();
        try
        {
            return JsonSerializer.Deserialize<int[]>(json)?
                .Where(id => id > 0)
                .Distinct()
                .ToList() ?? new List<int>();
        }
        catch (JsonException)
        {
            return new List<int>();
        }
    }

    /// <summary>
    /// Latest submitted monthly return that recorded FTE, otherwise the latest return that recorded FTE.
    /// </summary>
    private static ProjectMonthlyUpdate? PickCurrentFteUpdate(IEnumerable<ProjectMonthlyUpdate> updates)
    {
        var ordered = updates
            .OrderByDescending(u => u.Year)
            .ThenByDescending(u => u.Month)
            .ToList();
        return ordered.FirstOrDefault(u =>
                   u.SubmittedAt != null && (u.MonthlyPermFte != null || u.MonthlyMspFte != null))
               ?? ordered.FirstOrDefault(u => u.MonthlyPermFte != null || u.MonthlyMspFte != null);
    }

    [HttpGet]
    public async Task<IActionResult> DownloadPerformanceExcel(CancellationToken cancellationToken = default)
    {
        using var wb = new XLWorkbook();

        var commissions = await _db.Commissions.AsNoTracking()
            .OrderByDescending(c => c.StartDate)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);
        var wsCommissions = wb.AddWorksheet("Commissions");
        WriteCommissionsSheet(wsCommissions, commissions);

        var submissions = await _db.CommissionSubmissions.AsNoTracking()
            .OrderByDescending(s => s.UpdatedAt)
            .ToListAsync(cancellationToken);
        var wsSubmissions = wb.AddWorksheet("Commission submissions");
        WriteCommissionSubmissionsSheet(wsSubmissions, submissions);

        var metrics = await _db.PerformanceMetrics.AsNoTracking()
            .OrderBy(m => m.Identifier)
            .ToListAsync(cancellationToken);
        var wsMetrics = wb.AddWorksheet("Performance metrics");
        WritePerformanceMetricsSheet(wsMetrics, metrics);

        var metricNameById = metrics.ToDictionary(m => m.Id, m => m.Identifier);
        var metricValues = await _db.CommissionMetricValues.AsNoTracking()
            .OrderByDescending(v => v.UpdatedAt)
            .ToListAsync(cancellationToken);
        var wsValues = wb.AddWorksheet("Metric values");
        WriteCommissionMetricValuesSheet(wsValues, metricValues, metricNameById);

        using var stream = new MemoryStream();
        wb.SaveAs(stream);
        stream.Position = 0;
        var fileName = $"Compass-performance-export-{DateTime.UtcNow:yyyyMMdd-HHmm}.xlsx";
        return File(
            stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }

    [HttpGet]
    public async Task<IActionResult> DownloadExcel(CancellationToken cancellationToken = default)
    {
        if (!IsDemandGloballyActive())
            return NotFound();

        using var wb = new XLWorkbook();

        var businessCases = await _db.BusinessCases.AsNoTracking().OrderBy(b => b.BusinessCaseId).ToListAsync(cancellationToken);
        WriteBusinessCasesSheet(wb.AddWorksheet("Business cases"), businessCases);

        var demands = await _db.DemandTriageRequests.AsNoTracking().OrderBy(d => d.RequestReference).ToListAsync(cancellationToken);
        WriteDemandsSheet(wb.AddWorksheet("Demands"), demands);

        var outcomes = await _db.DemandTriageOutcomes.AsNoTracking()
            .OrderByDescending(t => t.DecidedAt ?? t.CreatedAt)
            .ToListAsync(cancellationToken);
        WriteTriageOutcomesSheet(wb.AddWorksheet("Triage outcomes"), outcomes);

        var scorecards = await _db.DemandScorecards.AsNoTracking()
            .OrderByDescending(s => s.UpdatedAt)
            .ToListAsync(cancellationToken);
        WriteScorecardsSheet(wb.AddWorksheet("Scoring"), scorecards);

        var reviews = await _db.DemandExploratoryReviews.AsNoTracking()
            .OrderByDescending(r => r.CompletedAt ?? r.CreatedAt)
            .ToListAsync(cancellationToken);
        WriteExploratoryReviewsSheet(wb.AddWorksheet("Exploratory reviews"), reviews);

        using var stream = new MemoryStream();
        wb.SaveAs(stream);
        stream.Position = 0;
        var fileName = $"Compass-demand-export-{DateTime.UtcNow:yyyyMMdd-HHmm}.xlsx";
        return File(
            stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }

    private static void WriteWorkItemsMasterSheet(
        IXLWorksheet ws,
        List<Project> projects,
        IReadOnlyDictionary<int, ProjectMonthlyUpdate> latestMuByProject,
        IReadOnlyDictionary<int, ProjectMonthlyUpdate> latestDraftMeta,
        IReadOnlyDictionary<int, string> extraNarrativeByMuId,
        IReadOnlyDictionary<int, string> contactsByProject,
        IReadOnlyDictionary<int, string> sroByProject,
        IReadOnlyDictionary<int, string> svcByProject,
        IReadOnlyDictionary<int, string> pmoByProject,
        IReadOnlyDictionary<int, string> tagsByProject,
        IReadOnlyDictionary<int, string> productsByProject,
        IReadOnlyDictionary<int, int> prodCounts,
        IReadOnlyDictionary<int, int> openRiskDict,
        IReadOnlyDictionary<int, int> openIssueDict,
        IReadOnlyDictionary<int, int> asmDict,
        IReadOnlyDictionary<int, int> msDict,
        IReadOnlyList<SubmissionTrendMonthColumn> periodColumns,
        IReadOnlyDictionary<int, List<string>> periodStatusesByProject,
        IReadOnlyDictionary<int, string> problemByProject,
        IReadOnlyDictionary<int, string> multiDeptByProject,
        IReadOnlyDictionary<int, string> budgetByProject,
        IReadOnlyDictionary<int, ProjectWeeklyWorkUpdate> latestWeeklyByProject,
        IReadOnlyDictionary<int, ProjectMonthlyUpdate> currentFteByProject)
    {
        var headers = new List<string>
        {
            "WorkItemId", "ProjectCode", "Title", "Aim", "PriorityOutcomes", "MissionPillars", "Thematic tags",
            "StartDate", "TargetDeliveryDate", "ActualDeliveryDate",
            "Phase", "BusinessArea", "RagStatus", "RagJustification", "PathToGreen",
            "DeliveryPriority", "DeliveryPriorityChangeReason",
            "ActivityType", "RiskAppetite",
            "Portfolio_OrganizationalGroup", "PrimaryContact",
            "Status", "StatusChangeReason",
            "IsFlagship", "IsAiInitiative", "ShowInFips", "IsMultiDepartmentProject", "OtherDepartmentsJson",
            "BusinessCaseApproval", "TotalPermFte", "TotalMspFte",
            "PipelineDemandRequestId", "ServiceUsers", "IsInternal", "IsExternal",
            "IsSubjectToSpendControl", "CreationMethod", "CreatedAt", "UpdatedAt",
            "HistoricBuRTId",
            "Directorate", "Additional Directorates", "SeniorResponsibleOfficers", "ServiceOwners", "PmoContacts",
            "TeamContacts",
            "LinkedProductsSummary", "LinkedProductCount",
            "OpenRisksCount", "OpenIssuesCount", "AssumptionsLinkedCount", "MilestoneCount",
            "DiscoveryStartDatePlanned", "DiscoveryStartDateActual", "DiscoveryEndDatePlanned", "DiscoveryEndDateActual",
            "AlphaStartDatePlanned", "AlphaStartDateActual", "AlphaEndDatePlanned", "AlphaEndDateActual",
            "PrivateBetaStartDatePlanned", "PrivateBetaStartDateActual", "PrivateBetaEndDatePlanned", "PrivateBetaEndDateActual",
            "PublicBetaStartDatePlanned", "PublicBetaStartDateActual", "PublicBetaEndDatePlanned", "PublicBetaEndDateActual",
            "LatestMonthly_Period",
            "LatestMonthly_SubmittedAt",
            "LatestMonthly_MainNarrative",
            "LatestMonthly_AdditionalNarrativeBlocks",
            "LatestMonthly_PermFte", "LatestMonthly_MspFte",
            "LatestMonthly_DraftRag", "LatestMonthly_DraftRagJustification", "LatestMonthly_DraftPathToGreen",
            "LatestMonthly_CreatedAt", "LatestMonthly_UpdatedAt",
            "LatestMonthly_CreatedByName", "LatestMonthly_CreatedByEmail",
            "ProblemStatement",
            "MultiDeptCooperation",
            "BudgetOwners",
            "LatestMonthly_PeopleNarrative",
            "CurrentPermFte", "CurrentMspFte",
            "LatestWeekly_Period", "LatestWeekly_SubmittedAt",
            "LatestWeekly_Narrative", "LatestWeekly_PeopleNarrative",
            "LatestWeekly_PermFte", "LatestWeekly_MspFte",
            "LatestWeekly_DraftRag", "LatestWeekly_DraftRagJustification", "LatestWeekly_DraftPathToGreen"
        };
        headers.AddRange(periodColumns.Select(c => c.Label));

        for (var c = 0; c < headers.Count; c++)
            ws.Cell(1, c + 1).Value = headers[c];
        ws.Row(1).Style.Font.Bold = true;

        var row = 2;
        foreach (var p in projects)
        {
            latestMuByProject.TryGetValue(p.Id, out var mu);
            ProjectMonthlyUpdate? muFull = null;
            if (mu != null)
                latestDraftMeta.TryGetValue(mu.Id, out muFull);

            string? extraNar = null;
            if (mu != null)
                extraNarrativeByMuId.TryGetValue(mu.Id, out extraNar);
            contactsByProject.TryGetValue(p.Id, out var contactStr);
            sroByProject.TryGetValue(p.Id, out var sroStr);
            svcByProject.TryGetValue(p.Id, out var svcStr);
            pmoByProject.TryGetValue(p.Id, out var pmoStr);
            tagsByProject.TryGetValue(p.Id, out var tagStr);
            productsByProject.TryGetValue(p.Id, out var prodStr);
            prodCounts.TryGetValue(p.Id, out var pc);
            openRiskDict.TryGetValue(p.Id, out var rc);
            openIssueDict.TryGetValue(p.Id, out var ic);
            asmDict.TryGetValue(p.Id, out var ac);
            msDict.TryGetValue(p.Id, out var mc);

            var period = mu == null
                ? ""
                : $"{mu.Year:D4}-{mu.Month:D2}";

            var draftRagName = muFull?.DraftRagStatusLookup?.Name ?? "";

            var col = 1;
            SetCell(ws, row, ref col, p.Id);
            SetCell(ws, row, ref col, p.ProjectCode);
            SetCell(ws, row, ref col, p.Title);
            SetCell(ws, row, ref col, p.Aim);
            SetCell(ws, row, ref col, AlignmentOrLegacy(WorkStrategicAlignmentExport.GetPriorityOutcomeNames(p), p.StrategicObjectives));
            SetCell(ws, row, ref col, AlignmentOrLegacy(WorkStrategicAlignmentExport.GetMissionPillarNames(p), p.MissionPillars));
            SetCell(ws, row, ref col, tagStr ?? "");
            SetCell(ws, row, ref col, p.StartDate);
            SetCell(ws, row, ref col, p.TargetDeliveryDate);
            SetCell(ws, row, ref col, p.ActualDeliveryDate);
            SetCell(ws, row, ref col, p.PhaseLookup?.Name);
            SetCell(ws, row, ref col, p.BusinessAreaLookup?.Name);
            SetCell(ws, row, ref col, p.RagStatusLookup?.Name ?? p.RagStatus);
            SetCell(ws, row, ref col, p.RagJustification);
            SetCell(ws, row, ref col, p.PathToGreen);
            SetCell(ws, row, ref col, p.DeliveryPriority?.Name);
            SetCell(ws, row, ref col, p.DeliveryPriorityChangeReason);
            SetCell(ws, row, ref col, p.ActivityTypeLookup?.Name);
            SetCell(ws, row, ref col, p.RiskAppetiteLookup?.Name);
            SetCell(ws, row, ref col, p.PrimaryOrganizationalGroup?.Name);
            SetCell(ws, row, ref col, p.PrimaryContactUser != null ? UserDisplay(p.PrimaryContactUser) : "");
            SetCell(ws, row, ref col, p.Status);
            SetCell(ws, row, ref col, p.StatusChangeReason);
            SetCell(ws, row, ref col, p.IsFlagship);
            SetCell(ws, row, ref col, p.IsAiInitiative);
            SetCell(ws, row, ref col, p.ShowInFips);
            SetCell(ws, row, ref col, p.IsMultiDepartmentProject);
            SetCell(ws, row, ref col, p.OtherDepartments);
            SetCell(ws, row, ref col, p.BusinessCaseApproval);
            SetCell(ws, row, ref col, p.TotalPermFte);
            SetCell(ws, row, ref col, p.TotalMspFte);
            SetCell(ws, row, ref col, p.PipelineDemandRequestId?.ToString());
            SetCell(ws, row, ref col, p.ServiceUsers);
            SetCell(ws, row, ref col, p.IsInternal);
            SetCell(ws, row, ref col, p.IsExternal);
            SetCell(ws, row, ref col, p.IsSubjectToSpendControl);
            SetCell(ws, row, ref col, p.CreationMethod);
            SetCell(ws, row, ref col, p.CreatedAt);
            SetCell(ws, row, ref col, p.UpdatedAt);
            SetCell(ws, row, ref col, p.HistoricBuRTId);
            SetCell(ws, row, ref col, FormatDirectorateColumn(p));
            SetCell(ws, row, ref col, FormatAdditionalDirectoratesColumn(p));
            SetCell(ws, row, ref col, sroStr ?? "");
            SetCell(ws, row, ref col, svcStr ?? "");
            SetCell(ws, row, ref col, pmoStr ?? "");
            SetCell(ws, row, ref col, contactStr ?? "");
            SetCell(ws, row, ref col, prodStr ?? "");
            SetCell(ws, row, ref col, pc);
            SetCell(ws, row, ref col, rc);
            SetCell(ws, row, ref col, ic);
            SetCell(ws, row, ref col, ac);
            SetCell(ws, row, ref col, mc);
            SetCell(ws, row, ref col, p.DiscoveryStartDatePlanned);
            SetCell(ws, row, ref col, p.DiscoveryStartDateActual);
            SetCell(ws, row, ref col, p.DiscoveryEndDatePlanned);
            SetCell(ws, row, ref col, p.DiscoveryEndDateActual);
            SetCell(ws, row, ref col, p.AlphaStartDatePlanned);
            SetCell(ws, row, ref col, p.AlphaStartDateActual);
            SetCell(ws, row, ref col, p.AlphaEndDatePlanned);
            SetCell(ws, row, ref col, p.AlphaEndDateActual);
            SetCell(ws, row, ref col, p.PrivateBetaStartDatePlanned);
            SetCell(ws, row, ref col, p.PrivateBetaStartDateActual);
            SetCell(ws, row, ref col, p.PrivateBetaEndDatePlanned);
            SetCell(ws, row, ref col, p.PrivateBetaEndDateActual);
            SetCell(ws, row, ref col, p.PublicBetaStartDatePlanned);
            SetCell(ws, row, ref col, p.PublicBetaStartDateActual);
            SetCell(ws, row, ref col, p.PublicBetaEndDatePlanned);
            SetCell(ws, row, ref col, p.PublicBetaEndDateActual);
            SetCell(ws, row, ref col, period);
            SetCell(ws, row, ref col, mu?.SubmittedAt);
            SetCell(ws, row, ref col, mu?.Narrative);
            SetCell(ws, row, ref col, extraNar ?? "");
            SetCell(ws, row, ref col, mu?.MonthlyPermFte);
            SetCell(ws, row, ref col, mu?.MonthlyMspFte);
            SetCell(ws, row, ref col, draftRagName);
            SetCell(ws, row, ref col, mu?.DraftRagJustification);
            SetCell(ws, row, ref col, mu?.DraftPathToGreen);
            SetCell(ws, row, ref col, mu?.CreatedAt);
            SetCell(ws, row, ref col, mu?.UpdatedAt);
            SetCell(ws, row, ref col, mu?.CreatedByName);
            SetCell(ws, row, ref col, mu?.CreatedByEmail);

            problemByProject.TryGetValue(p.Id, out var problem);
            multiDeptByProject.TryGetValue(p.Id, out var multiDept);
            budgetByProject.TryGetValue(p.Id, out var budgetOwners);
            latestWeeklyByProject.TryGetValue(p.Id, out var weekly);
            currentFteByProject.TryGetValue(p.Id, out var currentFte);
            var currentPerm = currentFte?.MonthlyPermFte ?? weekly?.WeeklyPermFte;
            var currentMsp = currentFte?.MonthlyMspFte ?? weekly?.WeeklyMspFte;
            var weeklyPeriod = weekly == null
                ? ""
                : WeeklyUpdateService.FormatPeriodLabel(weekly.WeekStartDate, weekly.WeekEndDate);

            SetCell(ws, row, ref col, problem ?? "");
            SetCell(ws, row, ref col, multiDept ?? "");
            SetCell(ws, row, ref col, budgetOwners ?? "");
            SetCell(ws, row, ref col, mu?.PeopleNarrative);
            SetCell(ws, row, ref col, currentPerm);
            SetCell(ws, row, ref col, currentMsp);
            SetCell(ws, row, ref col, weeklyPeriod);
            SetCell(ws, row, ref col, weekly?.SubmittedAt);
            SetCell(ws, row, ref col, weekly?.Narrative);
            SetCell(ws, row, ref col, weekly?.PeopleNarrative);
            SetCell(ws, row, ref col, weekly?.WeeklyPermFte);
            SetCell(ws, row, ref col, weekly?.WeeklyMspFte);
            SetCell(ws, row, ref col, weekly?.DraftRagStatusLookup?.Name ?? "");
            SetCell(ws, row, ref col, weekly?.DraftRagJustification);
            SetCell(ws, row, ref col, weekly?.DraftPathToGreen);

            if (periodColumns.Count > 0
                && periodStatusesByProject.TryGetValue(p.Id, out var periodStatuses))
            {
                for (var i = 0; i < periodColumns.Count; i++)
                {
                    var status = i < periodStatuses.Count ? periodStatuses[i] : "—";
                    SetCell(ws, row, ref col, status);
                }
            }
            else if (periodColumns.Count > 0)
            {
                for (var i = 0; i < periodColumns.Count; i++)
                    SetCell(ws, row, ref col, "—");
            }

            row++;
        }

        ws.SheetView.FreezeRows(1);
        ws.Columns(1, headers.Count).AdjustToContents();
    }

    private static void SetCell(IXLWorksheet ws, int row, ref int col, object? v)
    {
        var cell = ws.Cell(row, col++);
        switch (v)
        {
            case null:
                return;
            case DateTime dt:
                cell.Value = dt;
                return;
            case bool b:
                cell.Value = b;
                return;
            case int i:
                cell.Value = i;
                return;
            case long l:
                cell.Value = l;
                return;
            case decimal d:
                cell.Value = d;
                return;
            case double f:
                cell.Value = f;
                return;
            default:
                cell.Value = v.ToString() ?? "";
                return;
        }
    }

    private static void WriteProjectMonthlyUpdatesDetailed(
        IXLWorksheet ws,
        List<ProjectMonthlyUpdate> list,
        IReadOnlyDictionary<int, string> narrativeBlocksByMuId,
        IReadOnlyDictionary<int, (string Code, string Title)> codeTitle)
    {
        var headers = new[]
        {
            "MonthlyUpdateId", "ProjectId", "ProjectTitle", "ProjectCode", "Year", "Month",
            "MainNarrative", "AdditionalNarrativeBlocks",
            "SubmittedAt", "MonthlyPermFte", "MonthlyMspFte", "PeopleNarrative",
            "DraftRagStatus", "DraftRagJustification", "DraftPathToGreen",
            "CreatedAt", "UpdatedAt", "CreatedByName", "CreatedByEmail"
        };
        for (var c = 0; c < headers.Length; c++)
            ws.Cell(1, c + 1).Value = headers[c];
        ws.Row(1).Style.Font.Bold = true;
        var row = 2;
        foreach (var m in list)
        {
            codeTitle.TryGetValue(m.ProjectId, out var ct);
            narrativeBlocksByMuId.TryGetValue(m.Id, out var blocks);
            var col = 1;
            SetCell(ws, row, ref col, m.Id);
            SetCell(ws, row, ref col, m.ProjectId);
            SetCell(ws, row, ref col, ct.Title ?? "");
            SetCell(ws, row, ref col, ct.Code ?? "");
            SetCell(ws, row, ref col, m.Year);
            SetCell(ws, row, ref col, m.Month);
            SetCell(ws, row, ref col, m.Narrative);
            SetCell(ws, row, ref col, blocks ?? "");
            SetCell(ws, row, ref col, m.SubmittedAt);
            SetCell(ws, row, ref col, m.MonthlyPermFte);
            SetCell(ws, row, ref col, m.MonthlyMspFte);
            SetCell(ws, row, ref col, m.PeopleNarrative);
            SetCell(ws, row, ref col, m.DraftRagStatusLookup?.Name ?? "");
            SetCell(ws, row, ref col, m.DraftRagJustification);
            SetCell(ws, row, ref col, m.DraftPathToGreen);
            SetCell(ws, row, ref col, m.CreatedAt);
            SetCell(ws, row, ref col, m.UpdatedAt);
            SetCell(ws, row, ref col, m.CreatedByName);
            SetCell(ws, row, ref col, m.CreatedByEmail);
            row++;
        }

        ws.SheetView.FreezeRows(1);
        ws.Columns(1, headers.Length).AdjustToContents();
    }

    private static void WriteProjectWeeklyUpdatesDetailed(
        IXLWorksheet ws,
        List<ProjectWeeklyWorkUpdate> list,
        IReadOnlyDictionary<int, (string Code, string Title)> codeTitle)
    {
        var headers = new[]
        {
            "WeeklyUpdateId", "ProjectId", "ProjectTitle", "ProjectCode",
            "IsoYear", "IsoWeek", "WeekStart", "WeekEnd", "Period",
            "Narrative", "PeopleNarrative",
            "SubmittedAt", "WeeklyPermFte", "WeeklyMspFte",
            "DraftRagStatus", "DraftRagJustification", "DraftPathToGreen",
            "CreatedAt", "UpdatedAt", "CreatedByName", "CreatedByEmail"
        };
        for (var c = 0; c < headers.Length; c++)
            ws.Cell(1, c + 1).Value = headers[c];
        ws.Row(1).Style.Font.Bold = true;
        var row = 2;
        foreach (var w in list)
        {
            codeTitle.TryGetValue(w.ProjectId, out var ct);
            var col = 1;
            SetCell(ws, row, ref col, w.Id);
            SetCell(ws, row, ref col, w.ProjectId);
            SetCell(ws, row, ref col, ct.Title ?? "");
            SetCell(ws, row, ref col, ct.Code ?? "");
            SetCell(ws, row, ref col, w.IsoYear);
            SetCell(ws, row, ref col, w.IsoWeek);
            SetCell(ws, row, ref col, w.WeekStartDate);
            SetCell(ws, row, ref col, w.WeekEndDate);
            SetCell(ws, row, ref col, WeeklyUpdateService.FormatPeriodLabel(w.WeekStartDate, w.WeekEndDate));
            SetCell(ws, row, ref col, w.Narrative);
            SetCell(ws, row, ref col, w.PeopleNarrative);
            SetCell(ws, row, ref col, w.SubmittedAt);
            SetCell(ws, row, ref col, w.WeeklyPermFte);
            SetCell(ws, row, ref col, w.WeeklyMspFte);
            SetCell(ws, row, ref col, w.DraftRagStatusLookup?.Name ?? "");
            SetCell(ws, row, ref col, w.DraftRagJustification);
            SetCell(ws, row, ref col, w.DraftPathToGreen);
            SetCell(ws, row, ref col, w.CreatedAt);
            SetCell(ws, row, ref col, w.UpdatedAt);
            SetCell(ws, row, ref col, w.CreatedByName);
            SetCell(ws, row, ref col, w.CreatedByEmail);
            row++;
        }

        ws.SheetView.FreezeRows(1);
        ws.Columns(1, headers.Length).AdjustToContents();
    }

    private async Task WriteWorkRisksSheetAsync(
        IXLWorksheet ws,
        List<int> projectIds,
        IReadOnlyDictionary<int, (string Code, string Title)> codeTitle,
        CancellationToken cancellationToken)
    {
        var headers = new[]
        {
            "Reference", "RiskId", "WorkItemId", "WorkItemTitle", "ProjectCode",
            "Title", "Description", "Status", "Tier", "Priority",
            "Likelihood", "Impact", "Score", "Owner",
            "IdentifiedDate", "TargetDate", "ClosedDate",
            "ResponseStrategy", "Cause", "ImpactIfRealised", "Contingency", "Notes"
        };
        WriteHeaderRow(ws, headers);

        var risks = await _db.Risks.AsNoTracking()
            .Where(r => !r.IsDeleted && r.ProjectId != null && projectIds.Contains(r.ProjectId.Value))
            .Include(r => r.RiskStatus)
            .Include(r => r.RiskTier)
            .Include(r => r.RiskPriority)
            .Include(r => r.Likelihood)
            .Include(r => r.ImpactLevel)
            .Include(r => r.OwnerUser)
            .OrderBy(r => r.ProjectId).ThenBy(r => r.Id)
            .ToListAsync(cancellationToken);

        var row = 2;
        foreach (var r in risks)
        {
            var projectId = r.ProjectId!.Value;
            codeTitle.TryGetValue(projectId, out var ct);
            var col = 1;
            SetCell(ws, row, ref col, $"R-{r.Id:D4}");
            SetCell(ws, row, ref col, r.Id);
            SetCell(ws, row, ref col, projectId);
            SetCell(ws, row, ref col, ct.Title ?? "");
            SetCell(ws, row, ref col, ct.Code ?? "");
            SetCell(ws, row, ref col, r.Title);
            SetCell(ws, row, ref col, r.Description);
            SetCell(ws, row, ref col, r.RiskStatus?.Label ?? r.Status);
            SetCell(ws, row, ref col, r.RiskTier?.Name);
            SetCell(ws, row, ref col, r.RiskPriority?.Label);
            SetCell(ws, row, ref col, r.Likelihood?.Label);
            SetCell(ws, row, ref col, r.ImpactLevel?.Label);
            SetCell(ws, row, ref col, r.RiskScore);
            SetCell(ws, row, ref col, UserDisplay(r.OwnerUser));
            SetCell(ws, row, ref col, r.IdentifiedDate);
            SetCell(ws, row, ref col, r.TargetDate);
            SetCell(ws, row, ref col, r.ClosedDate);
            SetCell(ws, row, ref col, r.ResponseStrategy);
            SetCell(ws, row, ref col, r.Cause);
            SetCell(ws, row, ref col, r.ImpactIfRealised);
            SetCell(ws, row, ref col, r.Contingency);
            SetCell(ws, row, ref col, r.Notes);
            row++;
        }

        FinishSheet(ws, headers.Length);
    }

    private async Task WriteWorkIssuesSheetAsync(
        IXLWorksheet ws,
        List<int> projectIds,
        IReadOnlyDictionary<int, (string Code, string Title)> codeTitle,
        CancellationToken cancellationToken)
    {
        var headers = new[]
        {
            "Reference", "IssueId", "WorkItemId", "WorkItemTitle", "ProjectCode",
            "Title", "Description", "Status", "Priority", "Severity", "Category", "Owner",
            "DetectedDate", "TargetResolutionDate", "ClosedDate",
            "Workaround", "ResolutionSummary", "UserImpact", "ServiceImpact", "Blocked"
        };
        WriteHeaderRow(ws, headers);

        var issues = await _db.Issues.AsNoTracking()
            .Where(i => !i.IsDeleted && i.ProjectId != null && projectIds.Contains(i.ProjectId.Value))
            .Include(i => i.StatusLookup)
            .Include(i => i.PriorityLookup)
            .Include(i => i.SeverityLookup)
            .Include(i => i.CategoryLookup)
            .Include(i => i.OwnerUser)
            .OrderBy(i => i.ProjectId).ThenBy(i => i.Id)
            .ToListAsync(cancellationToken);

        var row = 2;
        foreach (var issue in issues)
        {
            var projectId = issue.ProjectId!.Value;
            codeTitle.TryGetValue(projectId, out var ct);
            var col = 1;
            SetCell(ws, row, ref col, $"I-{issue.Id:D4}");
            SetCell(ws, row, ref col, issue.Id);
            SetCell(ws, row, ref col, projectId);
            SetCell(ws, row, ref col, ct.Title ?? "");
            SetCell(ws, row, ref col, ct.Code ?? "");
            SetCell(ws, row, ref col, issue.Title);
            SetCell(ws, row, ref col, issue.Description);
            SetCell(ws, row, ref col, issue.StatusLookup?.Label ?? issue.Status);
            SetCell(ws, row, ref col, issue.PriorityLookup?.Label ?? issue.Priority);
            SetCell(ws, row, ref col, issue.SeverityLookup?.Label ?? issue.Severity);
            SetCell(ws, row, ref col, issue.CategoryLookup?.Label ?? issue.Category);
            SetCell(ws, row, ref col, UserDisplay(issue.OwnerUser));
            SetCell(ws, row, ref col, issue.DetectedDate);
            SetCell(ws, row, ref col, issue.TargetResolutionDate);
            SetCell(ws, row, ref col, issue.ClosedDate);
            SetCell(ws, row, ref col, issue.Workaround);
            SetCell(ws, row, ref col, issue.ResolutionSummary);
            SetCell(ws, row, ref col, issue.UserImpactSummary);
            SetCell(ws, row, ref col, issue.ServiceImpactSummary);
            SetCell(ws, row, ref col, issue.BlockedFlag);
            row++;
        }

        FinishSheet(ws, headers.Length);
    }

    private async Task WriteWorkAssumptionsSheetAsync(
        IXLWorksheet ws,
        List<int> projectIds,
        IReadOnlyDictionary<int, (string Code, string Title)> codeTitle,
        CancellationToken cancellationToken)
    {
        var headers = new[]
        {
            "Reference", "AssumptionId", "WorkItemId", "WorkItemTitle", "ProjectCode",
            "Description", "Status", "Criticality", "Owner",
            "ReviewDate", "ValidationOutcome", "CreatedAt", "UpdatedAt"
        };
        WriteHeaderRow(ws, headers);

        var assumptions = await _db.Assumptions.AsNoTracking()
            .Where(a => !a.IsDeleted && a.ProjectId != null && projectIds.Contains(a.ProjectId.Value))
            .Include(a => a.StatusLookup)
            .Include(a => a.CriticalityLookup)
            .Include(a => a.OwnerUser)
            .OrderBy(a => a.ProjectId).ThenBy(a => a.Id)
            .ToListAsync(cancellationToken);

        var row = 2;
        foreach (var a in assumptions)
        {
            var projectId = a.ProjectId!.Value;
            codeTitle.TryGetValue(projectId, out var ct);
            var col = 1;
            SetCell(ws, row, ref col, $"A-{a.Id:D4}");
            SetCell(ws, row, ref col, a.Id);
            SetCell(ws, row, ref col, projectId);
            SetCell(ws, row, ref col, ct.Title ?? "");
            SetCell(ws, row, ref col, ct.Code ?? "");
            SetCell(ws, row, ref col, a.Description);
            SetCell(ws, row, ref col, a.StatusLookup?.Label);
            SetCell(ws, row, ref col, a.CriticalityLookup?.Label);
            SetCell(ws, row, ref col, UserDisplay(a.OwnerUser));
            SetCell(ws, row, ref col, a.ReviewDate);
            SetCell(ws, row, ref col, a.ValidationOutcome);
            SetCell(ws, row, ref col, a.CreatedAt);
            SetCell(ws, row, ref col, a.UpdatedAt);
            row++;
        }

        FinishSheet(ws, headers.Length);
    }

    private async Task WriteWorkDependenciesSheetAsync(
        IXLWorksheet ws,
        List<int> projectIds,
        IReadOnlyDictionary<int, (string Code, string Title)> codeTitle,
        CancellationToken cancellationToken)
    {
        var headers = new[]
        {
            "DependencyId", "WorkItemId", "WorkItemTitle", "ProjectCode",
            "Direction", "Relationship",
            "RelatedEntityType", "RelatedEntityId", "RelatedTitle", "BusinessArea",
            "Description", "Status", "LinkType", "Criticality", "Owner", "Organisation",
            "DueDate", "CreatedAt", "UpdatedAt"
        };
        WriteHeaderRow(ws, headers);

        var projectIdSet = projectIds.ToHashSet();
        var dependencies = await _db.Dependencies.AsNoTracking()
            .Include(d => d.LinkTypeLookup)
            .Include(d => d.CriticalityLookup)
            .Include(d => d.OwnerUser)
            .Where(d =>
                (d.SourceEntityType == "Project" && projectIds.Contains(d.SourceEntityId)) ||
                (d.TargetEntityType == "Project" && projectIds.Contains(d.TargetEntityId)))
            .OrderBy(d => d.Id)
            .ToListAsync(cancellationToken);

        var relatedProjectIds = dependencies
            .SelectMany(d => new[]
            {
                string.Equals(d.SourceEntityType, "Project", StringComparison.OrdinalIgnoreCase) ? d.SourceEntityId : 0,
                string.Equals(d.TargetEntityType, "Project", StringComparison.OrdinalIgnoreCase) ? d.TargetEntityId : 0
            })
            .Where(id => id > 0)
            .Distinct()
            .ToList();
        var relatedProjects = relatedProjectIds.Count == 0
            ? new Dictionary<int, (string Title, string BusinessArea)>()
            : await _db.Projects.AsNoTracking()
                .Where(p => relatedProjectIds.Contains(p.Id))
                .Include(p => p.BusinessAreaLookup)
                .Include(p => p.PrimaryOrganizationalGroup)
                .ToDictionaryAsync(
                    p => p.Id,
                    p => (
                        Title: p.Title ?? "",
                        BusinessArea: p.BusinessAreaLookup?.Name
                            ?? p.PrimaryOrganizationalGroup?.Name
                            ?? ""),
                    cancellationToken);

        var row = 2;
        foreach (var d in dependencies)
        {
            var sourceIsProject = string.Equals(d.SourceEntityType, "Project", StringComparison.OrdinalIgnoreCase);
            var targetIsProject = string.Equals(d.TargetEntityType, "Project", StringComparison.OrdinalIgnoreCase);
            if (sourceIsProject && projectIdSet.Contains(d.SourceEntityId))
            {
                WriteDependencyRow(ws, ref row, d, d.SourceEntityId, "In", d.TargetEntityType, d.TargetEntityId, codeTitle, relatedProjects);
            }

            if (targetIsProject && projectIdSet.Contains(d.TargetEntityId)
                && !(sourceIsProject && d.SourceEntityId == d.TargetEntityId))
            {
                WriteDependencyRow(ws, ref row, d, d.TargetEntityId, "Out", d.SourceEntityType, d.SourceEntityId, codeTitle, relatedProjects);
            }
        }

        FinishSheet(ws, headers.Length);
    }

    private static void WriteDependencyRow(
        IXLWorksheet ws,
        ref int row,
        Dependency d,
        int workItemId,
        string direction,
        string relatedType,
        int relatedId,
        IReadOnlyDictionary<int, (string Code, string Title)> codeTitle,
        IReadOnlyDictionary<int, (string Title, string BusinessArea)> relatedProjects)
    {
        codeTitle.TryGetValue(workItemId, out var ct);
        var relatedIsProject = string.Equals(relatedType, "Project", StringComparison.OrdinalIgnoreCase);
        var relatedIsExternal = string.Equals(relatedType, "External", StringComparison.OrdinalIgnoreCase);
        relatedProjects.TryGetValue(relatedId, out var related);
        var relatedTitle = relatedIsProject
            ? related.Title
            : relatedIsExternal
                ? (d.Description ?? "")
                : "";
        var businessArea = relatedIsProject ? related.BusinessArea : "";
        var relationship = relatedIsExternal ? "External" : relatedIsProject ? "Internal" : relatedType;

        var col = 1;
        SetCell(ws, row, ref col, d.Id);
        SetCell(ws, row, ref col, workItemId);
        SetCell(ws, row, ref col, ct.Title ?? "");
        SetCell(ws, row, ref col, ct.Code ?? "");
        SetCell(ws, row, ref col, direction);
        SetCell(ws, row, ref col, relationship);
        SetCell(ws, row, ref col, relatedType);
        SetCell(ws, row, ref col, relatedIsExternal ? null : relatedId);
        SetCell(ws, row, ref col, relatedTitle);
        SetCell(ws, row, ref col, businessArea);
        SetCell(ws, row, ref col, d.Description);
        SetCell(ws, row, ref col, d.Status);
        SetCell(ws, row, ref col, d.LinkTypeLookup?.Label ?? d.DependencyType);
        SetCell(ws, row, ref col, d.CriticalityLookup?.Label);
        SetCell(ws, row, ref col, UserDisplay(d.OwnerUser));
        SetCell(ws, row, ref col, d.Organisation);
        SetCell(ws, row, ref col, d.DueDate);
        SetCell(ws, row, ref col, d.CreatedAt);
        SetCell(ws, row, ref col, d.UpdatedAt);
        row++;
    }

    private static void WriteHeaderRow(IXLWorksheet ws, string[] headers)
    {
        for (var c = 0; c < headers.Length; c++)
            ws.Cell(1, c + 1).Value = headers[c];
        ws.Row(1).Style.Font.Bold = true;
    }

    private static void FinishSheet(IXLWorksheet ws, int columnCount)
    {
        ws.SheetView.FreezeRows(1);
        if (columnCount > 0)
            ws.Columns(1, columnCount).AdjustToContents();
    }

    private static void WriteBusinessCasesSheet(IXLWorksheet ws, List<BusinessCase> list)
    {
        var headers = new[] { "Id", "BusinessCaseId", "Title", "Status", "BusinessArea", "CreatedAt", "UpdatedAt" };
        for (var c = 0; c < headers.Length; c++)
            ws.Cell(1, c + 1).Value = headers[c];
        ws.Row(1).Style.Font.Bold = true;
        var row = 2;
        foreach (var b in list)
        {
            ws.Cell(row, 1).Value = b.Id;
            ws.Cell(row, 2).Value = b.BusinessCaseId;
            ws.Cell(row, 3).Value = b.Title;
            ws.Cell(row, 4).Value = b.Status;
            ws.Cell(row, 5).Value = b.BusinessArea;
            ws.Cell(row, 6).Value = b.CreatedAt;
            ws.Cell(row, 7).Value = b.UpdatedAt;
            row++;
        }
        ws.Columns().AdjustToContents();
    }

    private static void WriteDemandsSheet(IXLWorksheet ws, List<DemandTriageRequest> list)
    {
        var headers = new[] { "Id", "RequestReference", "Status", "RequestName", "RequesterFullName", "ProposedRequestTitle", "TargetDeliveryDate", "CreatedAt", "UpdatedAt" };
        for (var c = 0; c < headers.Length; c++)
            ws.Cell(1, c + 1).Value = headers[c];
        ws.Row(1).Style.Font.Bold = true;
        var row = 2;
        foreach (var d in list)
        {
            ws.Cell(row, 1).Value = d.Id;
            ws.Cell(row, 2).Value = d.RequestReference;
            ws.Cell(row, 3).Value = d.Status;
            ws.Cell(row, 4).Value = d.RequestName;
            ws.Cell(row, 5).Value = d.RequesterFullName;
            ws.Cell(row, 6).Value = d.ProposedRequestTitle;
            ws.Cell(row, 7).Value = d.TargetDeliveryDate;
            ws.Cell(row, 8).Value = d.CreatedAt;
            ws.Cell(row, 9).Value = d.UpdatedAt;
            row++;
        }
        ws.Columns().AdjustToContents();
    }

    private static void WriteTriageOutcomesSheet(IXLWorksheet ws, List<DemandTriageOutcome> list)
    {
        var headers = new[] { "Id", "DemandTriageRequestId", "OutcomeSelection", "OutcomeSummary", "RoutedToArea", "DecidedAt", "DecidedBy", "CreatedAt" };
        for (var c = 0; c < headers.Length; c++)
            ws.Cell(1, c + 1).Value = headers[c];
        ws.Row(1).Style.Font.Bold = true;
        var row = 2;
        foreach (var t in list)
        {
            ws.Cell(row, 1).Value = t.Id;
            ws.Cell(row, 2).Value = t.DemandTriageRequestId;
            ws.Cell(row, 3).Value = t.OutcomeSelection;
            ws.Cell(row, 4).Value = t.OutcomeSummary;
            ws.Cell(row, 5).Value = t.RoutedToArea;
            ws.Cell(row, 6).Value = t.DecidedAt;
            ws.Cell(row, 7).Value = t.DecidedBy;
            ws.Cell(row, 8).Value = t.CreatedAt;
            row++;
        }
        ws.Columns().AdjustToContents();
    }

    private static void WriteScorecardsSheet(IXLWorksheet ws, List<DemandScorecard> list)
    {
        var headers = new[] { "Id", "DemandTriageRequestId", "ScorecardStatus", "TotalScore", "SuggestionBand", "FinalisedAt", "CreatedAt", "UpdatedAt" };
        for (var c = 0; c < headers.Length; c++)
            ws.Cell(1, c + 1).Value = headers[c];
        ws.Row(1).Style.Font.Bold = true;
        var row = 2;
        foreach (var s in list)
        {
            ws.Cell(row, 1).Value = s.Id;
            ws.Cell(row, 2).Value = s.DemandTriageRequestId;
            ws.Cell(row, 3).Value = s.ScorecardStatus;
            ws.Cell(row, 4).Value = s.TotalScore;
            ws.Cell(row, 5).Value = s.SuggestionBand;
            ws.Cell(row, 6).Value = s.FinalisedAt;
            ws.Cell(row, 7).Value = s.CreatedAt;
            ws.Cell(row, 8).Value = s.UpdatedAt;
            row++;
        }
        ws.Columns().AdjustToContents();
    }

    private static void WriteExploratoryReviewsSheet(IXLWorksheet ws, List<DemandExploratoryReview> list)
    {
        var headers = new[] { "Id", "DemandTriageRequestId", "RecommendationToProceed", "CompletedAt", "CompletedBy", "CreatedAt", "UpdatedAt" };
        for (var c = 0; c < headers.Length; c++)
            ws.Cell(1, c + 1).Value = headers[c];
        ws.Row(1).Style.Font.Bold = true;
        var row = 2;
        foreach (var r in list)
        {
            ws.Cell(row, 1).Value = r.Id;
            ws.Cell(row, 2).Value = r.DemandTriageRequestId;
            ws.Cell(row, 3).Value = r.RecommendationToProceed.HasValue ? (r.RecommendationToProceed.Value ? "Yes" : "No") : "";
            ws.Cell(row, 4).Value = r.CompletedAt;
            ws.Cell(row, 5).Value = r.CompletedBy;
            ws.Cell(row, 6).Value = r.CreatedAt;
            ws.Cell(row, 7).Value = r.UpdatedAt;
            row++;
        }
        ws.Columns().AdjustToContents();
    }

    private static void WriteCommissionsSheet(IXLWorksheet ws, List<Commission> list)
    {
        var headers = new[] { "Id", "Name", "Quarter", "StartDate", "EndDate", "OpenDate", "DueDate", "IsActive", "InScopePhases", "InScopeTypes", "IncludedPerformanceMetricIds", "CreatedAt", "UpdatedAt" };
        for (var c = 0; c < headers.Length; c++)
            ws.Cell(1, c + 1).Value = headers[c];
        ws.Row(1).Style.Font.Bold = true;
        var row = 2;
        foreach (var item in list)
        {
            ws.Cell(row, 1).Value = item.Id;
            ws.Cell(row, 2).Value = item.Name;
            ws.Cell(row, 3).Value = item.Quarter;
            ws.Cell(row, 4).Value = item.StartDate;
            ws.Cell(row, 5).Value = item.EndDate;
            ws.Cell(row, 6).Value = item.OpenDate;
            ws.Cell(row, 7).Value = item.DueDate;
            ws.Cell(row, 8).Value = item.IsActive;
            ws.Cell(row, 9).Value = item.InScopePhases;
            ws.Cell(row, 10).Value = item.InScopeTypes;
            ws.Cell(row, 11).Value = item.IncludedPerformanceMetricIds;
            ws.Cell(row, 12).Value = item.CreatedAt;
            ws.Cell(row, 13).Value = item.UpdatedAt;
            row++;
        }
        ws.SheetView.FreezeRows(1);
        ws.Columns(1, 13).AdjustToContents();
    }

    private static void WriteCommissionSubmissionsSheet(IXLWorksheet ws, List<CommissionSubmission> list)
    {
        var headers = new[] { "Id", "CommissionId", "ProductDocumentId", "FipsId", "ProductTitle", "Status", "SubmittedDate", "SubmittedBy", "Comments", "CreatedAt", "UpdatedAt" };
        for (var c = 0; c < headers.Length; c++)
            ws.Cell(1, c + 1).Value = headers[c];
        ws.Row(1).Style.Font.Bold = true;
        var row = 2;
        foreach (var item in list)
        {
            ws.Cell(row, 1).Value = item.Id;
            ws.Cell(row, 2).Value = item.CommissionId;
            ws.Cell(row, 3).Value = item.ProductDocumentId;
            ws.Cell(row, 4).Value = item.FipsId;
            ws.Cell(row, 5).Value = item.ProductTitle;
            ws.Cell(row, 6).Value = item.Status.ToString();
            ws.Cell(row, 7).Value = item.SubmittedDate;
            ws.Cell(row, 8).Value = item.SubmittedBy;
            ws.Cell(row, 9).Value = item.Comments;
            ws.Cell(row, 10).Value = item.CreatedAt;
            ws.Cell(row, 11).Value = item.UpdatedAt;
            row++;
        }
        ws.SheetView.FreezeRows(1);
        ws.Columns(1, 11).AdjustToContents();
    }

    private static void WritePerformanceMetricsSheet(IXLWorksheet ws, List<PerformanceMetric> list)
    {
        var headers = new[] { "Id", "Identifier", "Title", "ValueType", "ValidFromYear", "ValidFromMonth", "ApplicablePhases", "ApplicableTypes", "IsDisabled", "ConditionalOnMetricId", "CreatedAt", "UpdatedAt" };
        for (var c = 0; c < headers.Length; c++)
            ws.Cell(1, c + 1).Value = headers[c];
        ws.Row(1).Style.Font.Bold = true;
        var row = 2;
        foreach (var item in list)
        {
            ws.Cell(row, 1).Value = item.Id;
            ws.Cell(row, 2).Value = item.Identifier;
            ws.Cell(row, 3).Value = item.Title;
            ws.Cell(row, 4).Value = item.ValueType.ToString();
            ws.Cell(row, 5).Value = item.ValidFromYear;
            ws.Cell(row, 6).Value = item.ValidFromMonth;
            ws.Cell(row, 7).Value = item.ApplicablePhases;
            ws.Cell(row, 8).Value = item.ApplicableTypes;
            ws.Cell(row, 9).Value = item.IsDisabled;
            ws.Cell(row, 10).Value = item.ConditionalOnMetricId;
            ws.Cell(row, 11).Value = item.CreatedAt;
            ws.Cell(row, 12).Value = item.UpdatedAt;
            row++;
        }
        ws.SheetView.FreezeRows(1);
        ws.Columns(1, 12).AdjustToContents();
    }

    private static void WriteCommissionMetricValuesSheet(IXLWorksheet ws, List<CommissionMetricValue> list, Dictionary<int, string> metricNameById)
    {
        var headers = new[] { "Id", "CommissionSubmissionId", "PerformanceMetricId", "PerformanceMetricIdentifier", "Value", "IsComplete", "IsNotCaptured", "NotCapturedReason", "ReasonForDifference", "CreatedAt", "UpdatedAt" };
        for (var c = 0; c < headers.Length; c++)
            ws.Cell(1, c + 1).Value = headers[c];
        ws.Row(1).Style.Font.Bold = true;
        var row = 2;
        foreach (var item in list)
        {
            ws.Cell(row, 1).Value = item.Id;
            ws.Cell(row, 2).Value = item.CommissionSubmissionId;
            ws.Cell(row, 3).Value = item.PerformanceMetricId;
            ws.Cell(row, 4).Value = metricNameById.TryGetValue(item.PerformanceMetricId, out var metricName) ? metricName : "";
            ws.Cell(row, 5).Value = item.Value;
            ws.Cell(row, 6).Value = item.IsComplete;
            ws.Cell(row, 7).Value = item.IsNotCaptured;
            ws.Cell(row, 8).Value = item.NotCapturedReason;
            ws.Cell(row, 9).Value = item.ReasonForDifference;
            ws.Cell(row, 10).Value = item.CreatedAt;
            ws.Cell(row, 11).Value = item.UpdatedAt;
            row++;
        }
        ws.SheetView.FreezeRows(1);
        ws.Columns(1, 11).AdjustToContents();
    }

    /// <summary>CSV export of pipeline demand requests (modern demand register).</summary>
    [HttpGet]
    public async Task<IActionResult> DownloadDemandRegisterCsv(CancellationToken cancellationToken = default)
    {
        if (!IsDemandGloballyActive())
            return NotFound();

        var rows = await _db.DemandPipelineRequests.AsNoTracking()
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(cancellationToken);
        var sb = new StringBuilder();
        sb.AppendLine("Reference,Title,Department,SRO,Status,Score,Band,TriageOutcome,Submitted");
        foreach (var d in rows)
        {
            sb.AppendLine(string.Join(",", new[]
            {
                CsvEscape(d.Reference),
                CsvEscape(d.Title),
                CsvEscape(d.DepartmentGroup),
                CsvEscape(d.Sro),
                CsvEscape(d.Status),
                d.TotalScore?.ToString() ?? "",
                CsvEscape(d.SuggestedBand),
                CsvEscape(d.TriageOutcome),
                d.SubmittedDate?.ToString("yyyy-MM-dd") ?? ""
            }));
        }
        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        var fileName = $"Compass-demand-register-{DateTime.UtcNow:yyyyMMdd-HHmm}.csv";
        return File(bytes, "text/csv;charset=utf-8", fileName);
    }

    private static string CsvEscape(string? s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        if (s.Contains(',') || s.Contains('"') || s.Contains('\n') || s.Contains('\r'))
            return "\"" + s.Replace("\"", "\"\"") + "\"";
        return s;
    }
}
