using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using ClosedXML.Excel;
using Compass.Data;
using Compass.Models;
using Compass.Models.Modern.Work;
using Compass.Services;
using Compass.Services.Modern;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Compass.Controllers.Modern;

/// <summary>Modern work UI at <c>/modern/work/*</c>, backed by Compass <see cref="Project"/> data.</summary>
[Authorize]
[Route("modern/work")]
public class ModernWorkController : Controller
{
    private readonly CompassDbContext _context;
    private readonly IModernWorkService _modernWork;
    private readonly INotificationRuleService _notificationRuleService;
    private readonly IMonthlyUpdateService _monthlyUpdateService;
    private readonly ILogger<ModernWorkController> _logger;

    public ModernWorkController(
        CompassDbContext context,
        IModernWorkService modernWork,
        INotificationRuleService notificationRuleService,
        IMonthlyUpdateService monthlyUpdateService,
        ILogger<ModernWorkController> logger)
    {
        _context = context;
        _modernWork = modernWork;
        _notificationRuleService = notificationRuleService;
        _monthlyUpdateService = monthlyUpdateService;
        _logger = logger;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard(string? tab)
    {
        var userEmail = User.Identity?.Name;
        if (string.IsNullOrEmpty(userEmail))
        {
            TempData["ErrorMessage"] = "Unable to identify the current user.";
            return View("~/Views/Modern/Work/Dashboard.cshtml");
        }

        var currentUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == userEmail.ToLower());
        if (currentUser == null)
        {
            TempData["ErrorMessage"] = "User account not found.";
            return View("~/Views/Modern/Work/Dashboard.cshtml");
        }

        await _modernWork.PopulateWorkDashboardAsync(this, currentUser, userEmail, tab);
        return View("~/Views/Modern/Work/Dashboard.cshtml");
    }

    [HttpGet("")]
    [HttpGet("index")]
    public async Task<IActionResult> Index(
        string? search,
        int? portfolioId,
        int? directorateId,
        int? phaseId,
        int? ragId,
        int? priorityId)
    {
        ViewBag.MainNavSection = "work";
        ViewBag.SubNavItem = "work-all";

        var userEmail = User.Identity?.Name;
        if (string.IsNullOrEmpty(userEmail))
            return Unauthorized();

        var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == userEmail.ToLower());
        if (currentUser == null)
            return Unauthorized();

        var vm = await _modernWork.BuildWorkRegisterAsync(
            isMyWork: false,
            search,
            portfolioId,
            directorateId,
            phaseId,
            ragId,
            priorityId,
            monthlyUpdate: null,
            currentUser,
            userEmail,
            Url);

        return View("~/Views/Modern/Work/Index.cshtml", vm);
    }

    [HttpGet("all")]
    public async Task<IActionResult> AllWork(
        string? tab,
        string? search,
        int? portfolioId,
        int? directorateId,
        int? phaseId,
        int? ragId,
        int? priorityId,
        string? monthlyUpdate)
    {
        ViewBag.MainNavSection = "work";
        ViewBag.SubNavItem = "work-allwork";

        var userEmail = User.Identity?.Name;
        if (string.IsNullOrEmpty(userEmail))
            return Unauthorized();

        var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == userEmail.ToLower());
        if (currentUser == null)
            return Unauthorized();

        var activeTab = (tab?.ToLowerInvariant()) switch
        {
            "completed" => "completed",
            "cancelled" => "cancelled",
            "all" => "all",
            _ => "active"
        };

        var vm = await _modernWork.BuildWorkRegisterAsync(
            isMyWork: false,
            search,
            portfolioId,
            directorateId,
            phaseId,
            ragId,
            priorityId,
            monthlyUpdate,
            currentUser,
            userEmail,
            Url);

        var allWorkUrl = Url.Action(nameof(AllWork), "ModernWork") ?? "/modern/work/all";
        ViewBag.SearchAndFilter = new Compass.Models.SearchAndFilterViewModel
        {
            IdPrefix = "work",
            SearchPlaceholder = "Search work items…",
            SearchValue = search,
            FormActionUrl = Url.Action(nameof(AllWork), "ModernWork", new { tab = activeTab }) ?? allWorkUrl,
            FormMethod = "get",
            ClearUrl = Url.Action(nameof(AllWork), "ModernWork", new { tab = activeTab }) ?? allWorkUrl,
            SecondaryActionUrl = Url.Action(nameof(ExportRegister), "ModernWork", new
            {
                scope = "allwork",
                tab = activeTab,
                search,
                portfolioId,
                directorateId,
                phaseId,
                ragId,
                priorityId,
                monthlyUpdate
            }),
            SecondaryActionLabel = "Export this view",
            Fields = new List<Compass.Models.SearchAndFilterFieldViewModel>()
        };
        ViewBag.ActiveTab = activeTab;
        ViewBag.AllWorkActiveTab = activeTab;

        return View("~/Views/Modern/Work/AllWork.cshtml", vm);
    }

    [HttpGet("export-register")]
    public async Task<IActionResult> ExportRegister(string? scope, string? tab, string? search, int? portfolioId, int? directorateId, int? phaseId, int? ragId, int? priorityId, string? monthlyUpdate)
    {
        var scopeLabel = string.IsNullOrWhiteSpace(scope) ? "allwork" : scope.Trim().ToLowerInvariant();
        var userEmail = User.Identity?.Name;
        if (string.IsNullOrEmpty(userEmail))
            return Unauthorized();

        var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == userEmail.ToLower());
        if (currentUser == null)
            return Unauthorized();

        var normalizedTab = (tab ?? "active").Trim().ToLowerInvariant();
        var exportTab = normalizedTab is "completed" or "cancelled" or "all" ? normalizedTab : "active";

        var vm = await _modernWork.BuildWorkRegisterAsync(
            isMyWork: false,
            search,
            portfolioId,
            directorateId,
            phaseId,
            ragId,
            priorityId,
            monthlyUpdate,
            currentUser,
            userEmail,
            Url);

        IEnumerable<WorkRegisterRow> rows = exportTab switch
        {
            "completed" => vm.Completed,
            "cancelled" => vm.Cancelled,
            "all" => vm.ActivePaused.Concat(vm.Completed).Concat(vm.Cancelled),
            _ => vm.ActivePaused
        };

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Work");
        WriteWorkRegisterWorksheet(worksheet, rows);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var fileName = $"work-{scopeLabel}-{exportTab}-{DateTime.UtcNow:yyyyMMdd-HHmm}.xlsx";
        return File(
            stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }

    private static void WriteWorkRegisterWorksheet(IXLWorksheet worksheet, IEnumerable<WorkRegisterRow> rows)
    {
        worksheet.Cell(1, 1).Value = "Work item";
        worksheet.Cell(1, 2).Value = "Reference";
        worksheet.Cell(1, 3).Value = "Status";
        worksheet.Cell(1, 4).Value = "Directorate";
        worksheet.Cell(1, 5).Value = "SRO";
        worksheet.Cell(1, 6).Value = "Primary contact";
        worksheet.Cell(1, 7).Value = "Portfolio";
        worksheet.Cell(1, 8).Value = "Phase";
        worksheet.Cell(1, 9).Value = "Priority";
        worksheet.Cell(1, 10).Value = "RAG";
        worksheet.Cell(1, 11).Value = "Milestones";
        worksheet.Cell(1, 12).Value = "Monthly update";
        worksheet.Cell(1, 13).Value = "Risk ref";
        worksheet.Cell(1, 14).Value = "Completed";
        worksheet.Cell(1, 15).Value = "Cancelled reason";

        var rowNumber = 2;
        foreach (var row in rows)
        {
            worksheet.Cell(rowNumber, 1).Value = row.Title ?? "";
            worksheet.Cell(rowNumber, 2).Value = "WI-" + row.Id.ToString("D8", CultureInfo.InvariantCulture);
            worksheet.Cell(rowNumber, 3).Value = row.Status ?? "";
            worksheet.Cell(rowNumber, 4).Value = row.DirectorateSummary ?? "";
            worksheet.Cell(rowNumber, 5).Value = row.SroDisplayName ?? "";
            worksheet.Cell(rowNumber, 6).Value = row.PrimaryContactName ?? "";
            worksheet.Cell(rowNumber, 7).Value = row.PortfolioName ?? "";
            worksheet.Cell(rowNumber, 8).Value = row.PhaseName ?? "";
            worksheet.Cell(rowNumber, 9).Value = row.PriorityName ?? "";
            worksheet.Cell(rowNumber, 10).Value = row.RagName ?? "";
            worksheet.Cell(rowNumber, 11).Value = row.MilestoneCount;
            worksheet.Cell(rowNumber, 12).Value = row.MonthlyUpdateStatus ?? "";
            worksheet.Cell(rowNumber, 13).Value = row.FirstRiskReference ?? "";
            worksheet.Cell(rowNumber, 14).Value = row.CompletedAt ?? "";
            worksheet.Cell(rowNumber, 15).Value = row.CancelledReason ?? "";
            rowNumber++;
        }

        var headerRange = worksheet.Range(1, 1, 1, 15);
        headerRange.Style.Font.Bold = true;
        worksheet.SheetView.FreezeRows(1);
        worksheet.Columns(1, 15).AdjustToContents();
    }

    [HttpGet("create")]
    [HttpGet("/ModernWork/Create")]
    public async Task<IActionResult> Create(int? businessCaseId, CancellationToken cancellationToken = default)
    {
        ViewBag.MainNavSection = "work";
        ViewBag.SubNavItem = "work-all";

        var userEmail = User.Identity?.Name;
        if (string.IsNullOrEmpty(userEmail))
            return Unauthorized();

        var currentUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == userEmail.ToLower(), cancellationToken);
        if (currentUser == null)
            return Unauthorized();

        await PopulateWorkCreateViewBagAsync(businessCaseId, cancellationToken);

        var today = DateTime.UtcNow.Date;
        var model = new WorkItem
        {
            Status = "Active",
            StartDate = today,
            SubjectToSpendControl = false,
            FlagshipProject = false
        };

        return View("~/Views/Modern/Work/Create.cshtml", model);
    }

    [HttpPost("create")]
    [HttpPost("/ModernWork/Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        WorkItem model,
        int[]? directorateIds,
        int[]? priorityOutcomeIds,
        int[]? missionPillarIds,
        int[]? governmentDepartmentIds,
        string? initialRagJustification,
        string? multiDept,
        int? businessCaseId,
        CancellationToken cancellationToken = default)
    {
        ViewBag.MainNavSection = "work";
        ViewBag.SubNavItem = "work-all";

        var userEmail = User.Identity?.Name;
        if (string.IsNullOrEmpty(userEmail))
            return Unauthorized();

        var currentUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == userEmail.ToLower(), cancellationToken);
        if (currentUser == null)
            return Unauthorized();

        directorateIds ??= Array.Empty<int>();
        priorityOutcomeIds ??= Array.Empty<int>();
        missionPillarIds ??= Array.Empty<int>();
        governmentDepartmentIds ??= Array.Empty<int>();

        if (string.IsNullOrWhiteSpace(model.Title))
            ModelState.AddModelError(nameof(model.Title), "Enter a title.");
        if (string.IsNullOrWhiteSpace(model.ProblemStatement))
            ModelState.AddModelError(nameof(model.ProblemStatement), "Enter a problem statement.");
        if (string.IsNullOrWhiteSpace(model.Aim))
            ModelState.AddModelError(nameof(model.Aim), "Enter an aim.");
        if (!model.PortfolioId.HasValue)
            ModelState.AddModelError(nameof(model.PortfolioId), "Select a portfolio.");
        if (!model.PriorityId.HasValue)
            ModelState.AddModelError(nameof(model.PriorityId), "Select a priority.");
        if (!model.ActivityTypeId.HasValue)
            ModelState.AddModelError(nameof(model.ActivityTypeId), "Select an activity type.");
        if (!model.StartDate.HasValue)
            ModelState.AddModelError(nameof(model.StartDate), "Enter a start date.");

        if (!ModelState.IsValid)
        {
            await PopulateWorkCreateViewBagAsync(businessCaseId, cancellationToken);
            ViewBag.SelectedDirectorateIds = directorateIds;
            ViewBag.SelectedPriorityOutcomeIds = priorityOutcomeIds;
            ViewBag.SelectedMissionPillarIds = missionPillarIds;
            ViewBag.InitialRagJustification = initialRagJustification;
            return View("~/Views/Modern/Work/Create.cshtml", model);
        }

        var lastProject = await _context.Projects.OrderByDescending(p => p.ProjectCode).FirstOrDefaultAsync(cancellationToken);
        var nextNumber = 1;
        if (lastProject != null && !string.IsNullOrEmpty(lastProject.ProjectCode))
        {
            var parts = lastProject.ProjectCode.Split('-');
            if (parts.Length >= 2 && int.TryParse(parts[^1], out var lastNumber))
                nextNumber = lastNumber + 1;
        }

        var isMultiDept = string.Equals(multiDept, "yes", StringComparison.OrdinalIgnoreCase);
        string? otherDepartmentsJson = null;
        if (isMultiDept && governmentDepartmentIds.Length > 0)
            otherDepartmentsJson = JsonSerializer.Serialize(governmentDepartmentIds);

        var now = DateTime.UtcNow;
        var project = new Project
        {
            ProjectCode = $"DDTDEL-{nextNumber:D4}",
            Title = model.Title.Trim(),
            Aim = model.Aim?.Trim(),
            Status = model.Status?.Trim() ?? "Active",
            StartDate = model.StartDate,
            TargetDeliveryDate = model.TargetEndDate,
            PrimaryOrganizationalGroupId = model.PortfolioId,
            PhaseId = model.DeliveryPhaseId,
            DeliveryPriorityId = model.PriorityId,
            RagStatusLookupId = model.RagStatusId,
            ActivityTypeLookupId = model.ActivityTypeId,
            RiskAppetiteLookupId = model.RiskAppetiteId,
            IsFlagship = model.FlagshipProject,
            IsAiInitiative = false,
            IsSubjectToSpendControl = model.SubjectToSpendControl,
            RagJustification = string.IsNullOrWhiteSpace(initialRagJustification) ? null : initialRagJustification.Trim(),
            IsMultiDepartmentProject = isMultiDept,
            OtherDepartments = otherDepartmentsJson,
            CreatedAt = now,
            UpdatedAt = now,
            CreationMethod = "Manual"
        };

        _context.Projects.Add(project);
        await _context.SaveChangesAsync(cancellationToken);

        _context.ProjectProblemStatements.Add(new ProjectProblemStatement
        {
            ProjectId = project.Id,
            ProblemStatement = model.ProblemStatement!.Trim(),
            CreatedByEmail = userEmail,
            CreatedByName = currentUser.Name,
            CreatedAt = now,
            UpdatedAt = now
        });

        foreach (var divId in directorateIds.Distinct())
        {
            _context.ProjectDirectorates.Add(new ProjectDirectorate
            {
                ProjectId = project.Id,
                DivisionId = divId,
                CreatedAt = now
            });
        }

        foreach (var objectiveId in priorityOutcomeIds.Distinct())
        {
            _context.ProjectObjectives.Add(new ProjectObjective
            {
                ProjectId = project.Id,
                ObjectiveId = objectiveId,
                CreatedAt = now
            });
        }

        foreach (var missionId in missionPillarIds.Distinct())
        {
            _context.ProjectMissions.Add(new ProjectMission
            {
                ProjectId = project.Id,
                MissionId = missionId,
                CreatedAt = now
            });
        }

        if (model.RagStatusId.HasValue)
        {
            var ragName = await _context.RagStatusLookups.AsNoTracking()
                .Where(r => r.Id == model.RagStatusId.Value)
                .Select(r => r.Name)
                .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;
            _context.ProjectRagHistories.Add(new ProjectRagHistory
            {
                ProjectId = project.Id,
                RagStatusLookupId = model.RagStatusId,
                RagStatus = ragName,
                Justification = string.IsNullOrWhiteSpace(initialRagJustification) ? null : initialRagJustification.Trim(),
                ChangedByEmail = userEmail,
                ChangedByName = currentUser.Name,
                ChangedAt = now
            });
        }

        if (businessCaseId.HasValue)
        {
            var bcExists = await _context.BusinessCases.AsNoTracking().AnyAsync(b => b.Id == businessCaseId.Value, cancellationToken);
            if (bcExists)
            {
                _context.BusinessCaseProjects.Add(new BusinessCaseProject
                {
                    BusinessCaseId = businessCaseId.Value,
                    ProjectId = project.Id,
                    CreatedAt = now
                });
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        return RedirectToAction(nameof(Detail), new { id = project.Id });
    }

    private async Task PopulateWorkCreateViewBagAsync(int? businessCaseId, CancellationToken cancellationToken)
    {
        var orgGroups = await _context.OrganizationalGroups.AsNoTracking().Where(g => g.IsActive).OrderBy(g => g.Name).ToListAsync(cancellationToken);
        ViewBag.Portfolios = orgGroups.Select(g => new Portfolio { Id = g.Id, Name = g.Name, IsActive = true }).ToList();

        var directorates = await _context.Divisions.AsNoTracking().Where(d => d.IsActive).OrderBy(d => d.Name)
            .Select(d => new Directorate { Id = d.Id, Name = d.Name, IsActive = true }).ToListAsync(cancellationToken);
        ViewBag.Directorates = directorates;

        ViewBag.WorkStatusOptions = new List<LookupOption>
        {
            new() { Name = "Active", Value = "Active" },
            new() { Name = "Paused", Value = "Paused" },
            new() { Name = "Completed", Value = "Completed" },
            new() { Name = "Cancelled", Value = "Cancelled" }
        };

        ViewBag.WorkPriorityOptions = await _context.DeliveryPriorities.AsNoTracking().OrderBy(p => p.SortOrder)
            .Select(p => new LookupOption { Id = p.Id, Name = p.Name ?? "", Value = p.Name ?? "" }).ToListAsync(cancellationToken);

        ViewBag.DeliveryPhaseOptions = await _context.PhaseLookups.AsNoTracking().Where(p => p.IsActive).OrderBy(p => p.SortOrder)
            .Select(p => new LookupOption { Id = p.Id, Name = p.Name ?? "", Value = p.Name ?? "" }).ToListAsync(cancellationToken);

        ViewBag.ActivityTypeOptions = await _context.ActivityTypeLookups.AsNoTracking().Where(a => a.IsActive).OrderBy(a => a.SortOrder)
            .Select(a => new LookupOption { Id = a.Id, Name = a.Name ?? "", Value = a.Name ?? "" }).ToListAsync(cancellationToken);

        ViewBag.RiskAppetiteOptions = await _context.RiskAppetiteLookups.AsNoTracking().Where(r => r.IsActive).OrderBy(r => r.SortOrder)
            .Select(r => new LookupOption { Id = r.Id, Name = r.Name ?? "", Value = r.Name ?? "" }).ToListAsync(cancellationToken);

        ViewBag.RagStatuses = await _context.RagStatusLookups.AsNoTracking().Where(r => r.IsActive).OrderBy(r => r.SortOrder)
            .Select(r => new RagStatus { Id = r.Id, Name = r.Name }).ToListAsync(cancellationToken);

        ViewBag.PriorityOutcomes = await _context.Objectives.AsNoTracking()
            .Where(o => !o.IsDeleted && o.Status == "active")
            .OrderBy(o => o.Title)
            .Select(o => new WorkLookupOption { Id = o.Id, Name = o.Title, Value = o.Title }).ToListAsync(cancellationToken);

        ViewBag.MissionPillars = await _context.Missions.AsNoTracking()
            .Where(m => !m.IsDeleted)
            .OrderBy(m => m.Title)
            .Select(m => new WorkLookupOption { Id = m.Id, Name = m.Title, Value = m.Title }).ToListAsync(cancellationToken);

        ViewBag.GovernmentDepartments = await _context.GovernmentDepartments.AsNoTracking().OrderBy(g => g.Title).ToListAsync(cancellationToken);

        ViewBag.SelectedDirectorateIds = Array.Empty<int>();
        ViewBag.SelectedPriorityOutcomeIds = Array.Empty<int>();
        ViewBag.SelectedMissionPillarIds = Array.Empty<int>();
        ViewBag.InitialRagJustification = null;
        ViewBag.FromDemand = null;
        ViewBag.FromBusinessCase = null;

        if (businessCaseId.HasValue)
        {
            var bc = await _context.BusinessCases.AsNoTracking().FirstOrDefaultAsync(b => b.Id == businessCaseId.Value, cancellationToken);
            if (bc != null)
                ViewBag.FromBusinessCase = bc;
        }
    }

    /// <summary>Bridge to legacy monthly update entry for the current reporting month.</summary>
    [HttpGet("{id:int}/monthly-update/add")]
    public IActionResult AddMonthlyUpdate(int id)
    {
        var now = DateTime.UtcNow;
        return RedirectToAction("CreateUpdate", "MilestonesUpdatesSuccesses", new { projectId = id, year = now.Year, month = now.Month });
    }

    /// <summary>View a submitted monthly update (modern UI). Matches <c>/ModernWork/ViewMonthlyUpdate/{id}?updateId=</c> from default MVC routes.</summary>
    [HttpGet("{id:int}/monthly-update/view")]
    [HttpGet("ViewMonthlyUpdate/{id:int}")]
    [HttpGet("/ModernWork/ViewMonthlyUpdate/{id:int}")]
    public async Task<IActionResult> ViewMonthlyUpdate(int id, [FromQuery] int updateId, CancellationToken cancellationToken = default)
    {
        var userEmail = User.Identity?.Name;
        if (string.IsNullOrEmpty(userEmail))
            return Unauthorized();

        var currentUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == userEmail.ToLower(), cancellationToken);
        if (currentUser == null)
            return Unauthorized();

        var mu = await _context.ProjectMonthlyUpdates.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == updateId && m.ProjectId == id, cancellationToken);
        if (mu == null)
            return NotFound();

        var work = await _modernWork.PopulateWorkDetailAsync(
            this,
            id,
            currentUser,
            userEmail,
            tab: "updates",
            milestonestab: null,
            cancellationToken);
        if (work == null)
            return NotFound();

        ViewBag.WorkItem = work;

        var vm = new MonthlyUpdate
        {
            Id = mu.Id,
            WorkItemId = mu.ProjectId,
            ReportMonth = new DateTime(mu.Year, mu.Month, 1, 0, 0, 0, DateTimeKind.Utc),
            Narrative = mu.Narrative,
            SubmittedAt = mu.SubmittedAt,
            SubmittedByUserId = mu.CreatedByUserId,
            SubmittedBy = mu.CreatedByName ?? (mu.SubmittedAt.HasValue ? mu.CreatedByEmail : null)
        };

        if (mu.CreatedByUserId.HasValue)
        {
            var sub = await _context.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == mu.CreatedByUserId.Value, cancellationToken);
            ViewBag.SubmittedByName = sub?.Name ?? sub?.Email ?? vm.SubmittedBy ?? "—";
        }
        else
        {
            ViewBag.SubmittedByName = vm.SubmittedBy ?? "—";
        }

        var submittedAt = mu.SubmittedAt ?? DateTime.MinValue;
        var nearestRag = await _context.ProjectRagHistories.AsNoTracking()
            .Include(r => r.RagStatusLookup)
            .Where(r => r.ProjectId == id && r.ChangedAt <= submittedAt)
            .OrderByDescending(r => r.ChangedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (nearestRag == null)
        {
            nearestRag = await _context.ProjectRagHistories.AsNoTracking()
                .Include(r => r.RagStatusLookup)
                .Where(r => r.ProjectId == id)
                .OrderByDescending(r => r.ChangedAt)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (nearestRag != null)
        {
            ViewBag.ViewUpdateRagStatusId = nearestRag.RagStatusLookupId;
            ViewBag.ViewUpdateRagJustification = nearestRag.Justification;
            ViewBag.ViewUpdatePathToGreen = nearestRag.PathToGreen;
        }

        var periodDue = _monthlyUpdateService.GetMonthlyUpdateDueDate(mu.Year, mu.Month);
        ViewBag.PeriodDueDate = periodDue;
        var canUnsubmit = mu.SubmittedAt.HasValue && DateTime.UtcNow.Date <= periodDue.Date;
        ViewBag.CanUnsubmit = canUnsubmit;

        return View("~/Views/Modern/Work/ViewMonthlyUpdate.cshtml", vm);
    }

    /// <summary>Edit draft monthly update — delegates to legacy Milestones/Updates flow.</summary>
    [HttpGet("{id:int}/monthly-update/edit")]
    [HttpGet("EditMonthlyUpdate/{id:int}")]
    [HttpGet("/ModernWork/EditMonthlyUpdate/{id:int}")]
    public async Task<IActionResult> EditMonthlyUpdate(int id, [FromQuery] int updateId, CancellationToken cancellationToken = default)
    {
        var mu = await _context.ProjectMonthlyUpdates.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == updateId && m.ProjectId == id, cancellationToken);
        if (mu == null)
            return NotFound();
        if (mu.SubmittedAt.HasValue)
            return RedirectToAction(nameof(ViewMonthlyUpdate), new { id, updateId });
        return RedirectToAction("EditUpdate", "MilestonesUpdatesSuccesses", new { projectId = id, year = mu.Year, month = mu.Month });
    }

    [HttpPost("{id:int}/monthly-update/unsubmit")]
    [HttpPost("/ModernWork/UnsubmitMonthlyUpdate/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnsubmitMonthlyUpdate(int id, [FromForm] int updateId, CancellationToken cancellationToken = default)
    {
        var userEmail = User.Identity?.Name;
        if (string.IsNullOrEmpty(userEmail))
            return Unauthorized();

        var mu = await _context.ProjectMonthlyUpdates
            .FirstOrDefaultAsync(m => m.Id == updateId && m.ProjectId == id, cancellationToken);
        if (mu == null)
            return NotFound();
        if (!mu.SubmittedAt.HasValue)
        {
            TempData["Message"] = "This monthly update is not submitted.";
            return RedirectToAction(nameof(ViewMonthlyUpdate), new { id, updateId });
        }

        var periodDue = _monthlyUpdateService.GetMonthlyUpdateDueDate(mu.Year, mu.Month);
        if (DateTime.UtcNow.Date > periodDue.Date)
        {
            TempData["Error"] = "Unsubmit is only allowed before the period due date has passed.";
            return RedirectToAction(nameof(ViewMonthlyUpdate), new { id, updateId });
        }

        mu.SubmittedAt = null;
        var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, cancellationToken);
        if (project != null)
            project.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        TempData["Message"] = "Monthly update unsubmitted. You can edit and resubmit.";
        return RedirectToAction(nameof(EditMonthlyUpdate), new { id, updateId });
    }

    [HttpGet("detail/{id:int}")]
    public async Task<IActionResult> Detail(
        int id,
        string? tab = null,
        string? milestonestab = null,
        CancellationToken cancellationToken = default)
    {
        var userEmail = User.Identity?.Name;
        if (string.IsNullOrEmpty(userEmail))
            return Unauthorized();

        var currentUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == userEmail.ToLower(), cancellationToken);
        if (currentUser == null)
            return Unauthorized();

        var work = await _modernWork.PopulateWorkDetailAsync(
            this,
            id,
            currentUser,
            userEmail,
            tab,
            milestonestab,
            cancellationToken);

        if (work == null)
            return NotFound();

        return View("~/Views/Modern/Work/Detail.cshtml", work);
    }

    [HttpPost("{id:int}/watch")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Watch(int id, string? tab = null, string? milestonestab = null, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser == null)
            return Unauthorized();

        var projectExists = await _context.Projects.AsNoTracking()
            .AnyAsync(p => p.Id == id && !p.IsDeleted, cancellationToken);
        if (!projectExists)
            return NotFound();

        var existing = await _context.ProjectWatchlists
            .FirstOrDefaultAsync(w => w.UserId == currentUser.Id && w.ProjectId == id, cancellationToken);
        if (existing == null)
        {
            _context.ProjectWatchlists.Add(new ProjectWatchlist
            {
                UserId = currentUser.Id,
                ProjectId = id,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync(cancellationToken);
        }

        return RedirectToAction(nameof(Detail), new { id, tab, milestonestab });
    }

    [HttpPost("{id:int}/unwatch")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unwatch(int id, string? tab = null, string? milestonestab = null, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser == null)
            return Unauthorized();

        var watchlist = await _context.ProjectWatchlists
            .FirstOrDefaultAsync(w => w.UserId == currentUser.Id && w.ProjectId == id, cancellationToken);
        if (watchlist != null)
        {
            _context.ProjectWatchlists.Remove(watchlist);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return RedirectToAction(nameof(Detail), new { id, tab, milestonestab });
    }

    private async Task<User?> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        var userEmail = User.Identity?.Name;
        if (string.IsNullOrEmpty(userEmail))
            return null;
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == userEmail.ToLower(), cancellationToken);
    }

    [HttpGet("{id:int}/milestone/add")]
    public async Task<IActionResult> AddMilestone(int id)
    {
        ViewBag.MainNavSection = "work";

        var project = await _context.Projects.AsNoTracking()
            .Include(p => p.PrimaryOrganizationalGroup)
            .Include(p => p.PhaseLookup)
            .Include(p => p.DeliveryPriority)
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
        if (project == null)
            return NotFound();

        var work = await _modernWork.GetWorkItemAsync(id);
        if (work == null)
            return NotFound();

        ViewBag.WorkItem = work;
        ViewBag.WorkChromeSection = "milestones";
        ViewBag.WorkChromeSubPage = true;
        ViewBag.PortfolioName = project.PrimaryOrganizationalGroup?.Name;
        ViewBag.DeliveryPhaseName = project.PhaseLookup?.Name;
        ViewBag.PriorityName = project.DeliveryPriority?.Name;

        var milestone = new Milestone
        {
            ProjectId = id,
            DueDate = DateTime.UtcNow.Date,
            Status = "not_started"
        };

        return View("~/Views/Modern/Work/AddMilestone.cshtml", milestone);
    }

    [HttpGet("{id:int}/add-team-member")]
    [HttpGet("/ModernWork/AddTeamMember/{id:int}")]
    public async Task<IActionResult> AddTeamMember(int id, CancellationToken cancellationToken = default)
    {
        var prep = await PrepareAddTeamMemberPageAsync(id, cancellationToken);
        if (prep != null)
            return prep;

        var model = new WorkItemTeamMember { WorkItemId = id, TeamStatus = "active" };
        return View("~/Views/Modern/Work/AddTeamMember.cshtml", model);
    }

    [HttpPost("{id:int}/add-team-member")]
    [HttpPost("/ModernWork/AddTeamMember/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddTeamMember(int id, [FromForm] WorkItemTeamMember model, CancellationToken cancellationToken = default)
    {
        model.WorkItemId = id;

        var prep = await PrepareAddTeamMemberPageAsync(id, cancellationToken);
        if (prep != null)
            return prep;

        if (model.AppUserId <= 0)
            ModelState.AddModelError(nameof(WorkItemTeamMember.AppUserId), "Select a team member.");
        if (string.IsNullOrWhiteSpace(model.Role))
            ModelState.AddModelError(nameof(WorkItemTeamMember.Role), "Select or enter a role.");

        if (!ModelState.IsValid)
        {
            await PopulateUserPickerForTeamMemberAsync(model.AppUserId, cancellationToken);
            return View("~/Views/Modern/Work/AddTeamMember.cshtml", model);
        }

        var project = await _context.Projects
            .Include(p => p.ProjectContacts)
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, cancellationToken);
        if (project == null)
            return NotFound();

        var user = await _context.Users.FindAsync(model.AppUserId);
        if (user == null)
        {
            ModelState.AddModelError(nameof(WorkItemTeamMember.AppUserId), "Selected person could not be found.");
            await PopulateUserPickerForTeamMemberAsync(model.AppUserId, cancellationToken);
            return View("~/Views/Modern/Work/AddTeamMember.cshtml", model);
        }

        if (string.IsNullOrWhiteSpace(user.Email))
        {
            ModelState.AddModelError(nameof(WorkItemTeamMember.AppUserId), "Selected person does not have an email address.");
            await PopulateUserPickerForTeamMemberAsync(model.AppUserId, cancellationToken);
            return View("~/Views/Modern/Work/AddTeamMember.cshtml", model);
        }

        var already = await _context.ProjectContacts
            .AnyAsync(pc => pc.ProjectId == id && pc.UserId == model.AppUserId, cancellationToken);
        if (already)
        {
            ModelState.AddModelError(nameof(WorkItemTeamMember.AppUserId), "This person is already on the team.");
            await PopulateUserPickerForTeamMemberAsync(model.AppUserId, cancellationToken);
            return View("~/Views/Modern/Work/AddTeamMember.cshtml", model);
        }

        var fundingArrangement = model.FundingOptionId == 2 ? "Programme" : "Admin";
        var employmentType = model.TypeOptionId == 2 ? "MSP" : "Permanent";
        var timeAllocation = model.TimeAllocationPct.HasValue ? $"{model.TimeAllocationPct.Value}%" : null;
        var teamStatusLegacy = string.Equals(model.TeamStatus, "inactive", StringComparison.OrdinalIgnoreCase) ? "previous" : "current";
        var leaveReason = teamStatusLegacy == "previous" ? "Inactive" : null;
        var leftAt = teamStatusLegacy == "previous" ? DateTime.UtcNow : (DateTime?)null;

        var normalizedName = !string.IsNullOrWhiteSpace(user.Name) ? user.Name.Trim() : user.Email.Trim();
        var contactEmail = user.Email.Trim();

        var teamMember = new ProjectContact
        {
            ProjectId = id,
            UserId = user.Id,
            Name = normalizedName,
            Email = contactEmail,
            Role = model.Role!.Trim(),
            FundingArrangement = fundingArrangement,
            TimeAllocation = timeAllocation,
            EmploymentType = employmentType,
            TeamStatus = teamStatusLegacy,
            LeaveReason = leaveReason,
            LeftAt = leftAt,
            SortOrder = project.ProjectContacts.Count + 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.ProjectContacts.Add(teamMember);
        await _context.SaveChangesAsync(cancellationToken);

        try
        {
            var templateVariables = new Dictionary<string, object>
            {
                { "project_title", project.Title },
                { "project_code", project.ProjectCode ?? string.Empty },
                { "user_name", normalizedName },
                { "user_email", contactEmail },
                { "role", model.Role.Trim() }
            };
            await _notificationRuleService.ProcessNotificationTriggerAsync(
                "team_member_added",
                projectId: id,
                userId: user.Id,
                templateVariables: templateVariables);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send team member added notification");
        }

        TempData["SuccessMessage"] = "Team member added successfully.";
        return Redirect((Url.Action(nameof(Detail), new { id, tab = "team" }) ?? "") + "#wd-team");
    }

    private async Task PopulateUserPickerForTeamMemberAsync(int appUserId, CancellationToken cancellationToken)
    {
        if (appUserId <= 0)
            return;
        var u = await _context.Users.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == appUserId, cancellationToken);
        if (u != null)
        {
            ViewBag.OwnerName = u.Name;
            ViewBag.OwnerEmail = u.Email;
        }
    }

    private async Task<IActionResult?> PrepareAddTeamMemberPageAsync(int id, CancellationToken cancellationToken)
    {
        ViewBag.MainNavSection = "work";

        var userEmail = User.Identity?.Name;
        if (string.IsNullOrEmpty(userEmail))
            return Unauthorized();

        var currentUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == userEmail.ToLower(), cancellationToken);
        if (currentUser == null)
            return Unauthorized();

        var projectExists = await _context.Projects.AsNoTracking()
            .AnyAsync(p => p.Id == id && !p.IsDeleted, cancellationToken);
        if (!projectExists)
            return NotFound();

        var work = await _modernWork.GetWorkItemAsync(id);
        if (work == null)
            return NotFound();

        var project = await _context.Projects.AsNoTracking()
            .Include(p => p.PrimaryOrganizationalGroup)
            .Include(p => p.PhaseLookup)
            .Include(p => p.DeliveryPriority)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        ViewBag.WorkItem = work;
        ViewBag.WorkChromeSection = "team";
        ViewBag.WorkChromeSubPage = true;
        ViewBag.WorkChromeTabsAsLinks = true;
        ViewBag.WorkIdShort = work.Id.ToString("X8").ToUpperInvariant();
        if (project != null)
        {
            ViewBag.PortfolioName = project.PrimaryOrganizationalGroup?.Name;
            ViewBag.DeliveryPhaseName = project.PhaseLookup?.Name;
            ViewBag.PriorityName = project.DeliveryPriority?.Name;
        }

        ViewBag.TeamMemberFundingOptions = new List<LookupOption>
        {
            new() { Id = 1, Name = "Admin", Value = "Admin" },
            new() { Id = 2, Name = "Programme", Value = "Programme" }
        };
        ViewBag.TeamMemberTypeOptions = new List<LookupOption>
        {
            new() { Id = 1, Name = "Permanent", Value = "Permanent" },
            new() { Id = 2, Name = "MSP", Value = "MSP" }
        };

        var roleNames = await _context.ProjectContacts.AsNoTracking()
            .Where(pc => !string.IsNullOrWhiteSpace(pc.Role))
            .Select(pc => pc.Role!.Trim())
            .Distinct()
            .OrderBy(r => r)
            .Take(50)
            .ToListAsync(cancellationToken);
        foreach (var d in new[] { "Delivery manager", "Product manager", "Technical lead" }.Reverse())
        {
            if (!roleNames.Any(x => string.Equals(x, d, StringComparison.OrdinalIgnoreCase)))
                roleNames.Insert(0, d);
        }

        ViewBag.TeamMemberRoleOptions = roleNames
            .Select((name, i) => new LookupOption { Id = i + 1, Name = name, Value = name })
            .ToList();

        return null;
    }

    [HttpGet("{id:int}/edit")]
    public IActionResult Edit(int id) =>
        RedirectToAction("Edit", "Project", new { id });

    [HttpGet("{id:int}/log-issue")]
    [HttpGet("/ModernWork/LogIssue/{id:int}")]
    public async Task<IActionResult> LogIssue(int id, CancellationToken cancellationToken = default)
    {
        var prep = await PrepareModernLogRaidPageAsync(id, cancellationToken);
        if (prep.ErrorResult != null)
            return prep.ErrorResult;

        var model = new WorkItemRiskOrIssue
        {
            WorkItemId = id,
            Type = "Issue"
        };
        return View("~/Views/Modern/Work/LogIssue.cshtml", model);
    }

    [HttpPost("{id:int}/log-issue")]
    [HttpPost("/ModernWork/LogIssue/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LogIssue(int id, [FromForm] ModernWorkLogRaidForm form, CancellationToken cancellationToken = default)
    {
        var prep = await PrepareModernLogRaidPageAsync(id, cancellationToken);
        if (prep.ErrorResult != null)
            return prep.ErrorResult;

        var title = (form.Title ?? "").Trim();
        var description = (form.Description ?? "").Trim();
        var impact = (form.ImpactOnDelivery ?? "").Trim();
        var mitigation = (form.MitigationOrAction ?? "").Trim();

        if (string.IsNullOrEmpty(title))
            ModelState.AddModelError(nameof(form.Title), "Enter a title.");
        if (string.IsNullOrEmpty(description))
            ModelState.AddModelError(nameof(form.Description), "Enter a description.");
        if (string.IsNullOrEmpty(impact))
            ModelState.AddModelError(nameof(form.ImpactOnDelivery), "Enter the impact on delivery.");
        if (!form.DirectorateId.HasValue || form.DirectorateId.Value <= 0)
            ModelState.AddModelError(nameof(form.DirectorateId), "Select a directorate.");
        if (string.IsNullOrEmpty(mitigation))
            ModelState.AddModelError(nameof(form.MitigationOrAction), "Enter an action plan.");

        if (!ModelState.IsValid)
        {
            var invalidModel = new WorkItemRiskOrIssue
            {
                WorkItemId = id,
                Type = "Issue",
                Title = form.Title ?? "",
                Description = form.Description,
                ImpactOnDelivery = form.ImpactOnDelivery,
                Priority = form.Priority,
                Tier = form.Tier,
                DirectorateId = form.DirectorateId,
                OwnerUserId = form.OwnerUserId,
                TargetResolutionDate = form.TargetResolutionDate,
                MitigationOrAction = form.MitigationOrAction,
                LinkedMilestoneId = form.LinkedMilestoneId
            };
            await PopulateOwnerDisplayForLogRaidAsync(form.OwnerUserId, cancellationToken);
            return View("~/Views/Modern/Work/LogIssue.cshtml", invalidModel);
        }

        var duplicate = await _context.Issues
            .AnyAsync(i => i.ProjectId == id && !i.IsDeleted && i.Title.ToLower() == title.ToLower(), cancellationToken);
        if (duplicate)
        {
            ModelState.AddModelError(nameof(form.Title), "An issue with this title already exists for this work item.");
            var invalidModel = new WorkItemRiskOrIssue
            {
                WorkItemId = id,
                Type = "Issue",
                Title = form.Title ?? "",
                Description = form.Description,
                ImpactOnDelivery = form.ImpactOnDelivery,
                Priority = form.Priority,
                Tier = form.Tier,
                DirectorateId = form.DirectorateId,
                OwnerUserId = form.OwnerUserId,
                TargetResolutionDate = form.TargetResolutionDate,
                MitigationOrAction = form.MitigationOrAction,
                LinkedMilestoneId = form.LinkedMilestoneId
            };
            await PopulateOwnerDisplayForLogRaidAsync(form.OwnerUserId, cancellationToken);
            return View("~/Views/Modern/Work/LogIssue.cshtml", invalidModel);
        }

        var directorateName = await _context.Divisions.AsNoTracking()
            .Where(d => d.Id == form.DirectorateId!.Value)
            .Select(d => d.Name)
            .FirstOrDefaultAsync(cancellationToken);

        var severity = MapPriorityToIssueSeverity(form.Priority);
        var fullDescription = description + "\n\nImpact on delivery:\n" + impact;
        if (!string.IsNullOrWhiteSpace(form.Tier))
            fullDescription += "\n\nTier: " + form.Tier.Trim();

        var issue = new Issue
        {
            ProjectId = id,
            Title = title,
            Description = fullDescription,
            Severity = severity,
            Status = "open",
            DetectedDate = DateTime.UtcNow.Date,
            TargetResolutionDate = form.TargetResolutionDate,
            BusinessArea = directorateName,
            OwnerUserId = form.OwnerUserId > 0 ? form.OwnerUserId : null,
            Priority = form.Priority?.Length <= 10 ? form.Priority : form.Priority![..10],
            MilestoneId = form.LinkedMilestoneId,
            Workaround = mitigation,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Issues.Add(issue);
        await _context.SaveChangesAsync(cancellationToken);

        TempData["SuccessMessage"] = "Issue logged.";
        return Redirect((Url.Action(nameof(Detail), new { id, tab = "risks" }) ?? "") + "#wd-risks");
    }

    [HttpGet("{id:int}/log-risk")]
    [HttpGet("/ModernWork/LogRisk/{id:int}")]
    public async Task<IActionResult> LogRisk(int id, CancellationToken cancellationToken = default)
    {
        var prep = await PrepareModernLogRaidPageAsync(id, cancellationToken);
        if (prep.ErrorResult != null)
            return prep.ErrorResult;

        var model = new WorkItemRiskOrIssue
        {
            WorkItemId = id,
            Type = "Risk"
        };
        return View("~/Views/Modern/Work/LogRisk.cshtml", model);
    }

    [HttpPost("{id:int}/log-risk")]
    [HttpPost("/ModernWork/LogRisk/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LogRisk(int id, [FromForm] ModernWorkLogRaidForm form, CancellationToken cancellationToken = default)
    {
        var prep = await PrepareModernLogRaidPageAsync(id, cancellationToken);
        if (prep.ErrorResult != null)
            return prep.ErrorResult;

        var title = (form.Title ?? "").Trim();
        var description = (form.Description ?? "").Trim();
        var impact = (form.ImpactOnDelivery ?? "").Trim();
        var mitigation = (form.MitigationOrAction ?? "").Trim();

        if (string.IsNullOrEmpty(title))
            ModelState.AddModelError(nameof(form.Title), "Enter a title.");
        if (string.IsNullOrEmpty(description))
            ModelState.AddModelError(nameof(form.Description), "Enter a description.");
        if (string.IsNullOrEmpty(impact))
            ModelState.AddModelError(nameof(form.ImpactOnDelivery), "Enter the impact on delivery.");
        if (!form.DirectorateId.HasValue || form.DirectorateId.Value <= 0)
            ModelState.AddModelError(nameof(form.DirectorateId), "Select a directorate.");
        if (string.IsNullOrEmpty(mitigation))
            ModelState.AddModelError(nameof(form.MitigationOrAction), "Enter a mitigation plan.");

        if (!ModelState.IsValid)
        {
            var invalidModel = new WorkItemRiskOrIssue
            {
                WorkItemId = id,
                Type = "Risk",
                Title = form.Title ?? "",
                Description = form.Description,
                ImpactOnDelivery = form.ImpactOnDelivery,
                Priority = form.Priority,
                Tier = form.Tier,
                DirectorateId = form.DirectorateId,
                OwnerUserId = form.OwnerUserId,
                TargetResolutionDate = form.TargetResolutionDate,
                MitigationOrAction = form.MitigationOrAction,
                LinkedMilestoneId = form.LinkedMilestoneId
            };
            await PopulateOwnerDisplayForLogRaidAsync(form.OwnerUserId, cancellationToken);
            return View("~/Views/Modern/Work/LogRisk.cshtml", invalidModel);
        }

        var duplicate = await _context.Risks
            .AnyAsync(r => r.ProjectId == id && !r.IsDeleted && r.Title.ToLower() == title.ToLower(), cancellationToken);
        if (duplicate)
        {
            ModelState.AddModelError(nameof(form.Title), "A risk with this title already exists for this work item.");
            var invalidModel = new WorkItemRiskOrIssue
            {
                WorkItemId = id,
                Type = "Risk",
                Title = form.Title ?? "",
                Description = form.Description,
                ImpactOnDelivery = form.ImpactOnDelivery,
                Priority = form.Priority,
                Tier = form.Tier,
                DirectorateId = form.DirectorateId,
                OwnerUserId = form.OwnerUserId,
                TargetResolutionDate = form.TargetResolutionDate,
                MitigationOrAction = form.MitigationOrAction,
                LinkedMilestoneId = form.LinkedMilestoneId
            };
            await PopulateOwnerDisplayForLogRaidAsync(form.OwnerUserId, cancellationToken);
            return View("~/Views/Modern/Work/LogRisk.cshtml", invalidModel);
        }

        var directorateName = await _context.Divisions.AsNoTracking()
            .Where(d => d.Id == form.DirectorateId!.Value)
            .Select(d => d.Name)
            .FirstOrDefaultAsync(cancellationToken);

        var riskPriorityId = await ResolveRiskPriorityIdAsync(form.Priority, cancellationToken);
        var riskTierId = await ResolveRiskTierIdAsync(form.Tier, cancellationToken);

        var notes = mitigation;
        if (!string.IsNullOrWhiteSpace(impact))
            notes = "Impact on delivery:\n" + impact + "\n\nMitigation:\n" + mitigation;

        var fullDescription = description;
        if (!string.IsNullOrWhiteSpace(form.Tier))
            fullDescription += "\n\nTier: " + form.Tier.Trim();

        var risk = new Risk
        {
            ProjectId = id,
            Title = title,
            Description = fullDescription,
            BusinessArea = directorateName,
            HowIdentified = "Logged from modern work UI",
            OwnerUserId = form.OwnerUserId > 0 ? form.OwnerUserId : null,
            ImpactRating = 3,
            LikelihoodRating = 3,
            RiskScore = 9,
            Status = "new",
            Notes = notes,
            RiskPriorityId = riskPriorityId,
            RiskTierId = riskTierId,
            IdentifiedDate = DateTime.UtcNow.Date,
            NextReviewDate = form.TargetResolutionDate,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Risks.Add(risk);
        await _context.SaveChangesAsync(cancellationToken);

        if (form.LinkedMilestoneId.HasValue)
        {
            var milestoneOk = await _context.Milestones.AsNoTracking()
                .AnyAsync(m => m.Id == form.LinkedMilestoneId.Value && m.ProjectId == id, cancellationToken);
            if (milestoneOk)
            {
                _context.MilestoneRisks.Add(new MilestoneRisk
                {
                    MilestoneId = form.LinkedMilestoneId.Value,
                    RiskId = risk.Id
                });
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        TempData["SuccessMessage"] = "Risk logged.";
        return Redirect((Url.Action(nameof(Detail), new { id, tab = "risks" }) ?? "") + "#wd-risks");
    }

    private static string MapPriorityToIssueSeverity(string? priority) =>
        priority?.Trim().ToLowerInvariant() switch
        {
            "high" => "high",
            "low" => "low",
            "medium" => "medium",
            _ => "medium"
        };

    private async Task<int?> ResolveRiskPriorityIdAsync(string? priority, CancellationToken cancellationToken)
    {
        var p = priority?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(p))
            return null;
        return await _context.RiskPriorities.AsNoTracking()
            .Where(x => x.IsActive && (x.Code.ToLower() == p || x.Label.ToLower().Contains(p)))
            .OrderBy(x => x.SortOrder)
            .Select(x => (int?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<int?> ResolveRiskTierIdAsync(string? tier, CancellationToken cancellationToken)
    {
        var t = tier?.Trim();
        if (string.IsNullOrEmpty(t))
            return null;
        return await _context.RiskTiers.AsNoTracking()
            .Where(x => x.IsActive && (x.Name == t || x.Code.Replace(" ", "") == t.Replace(" ", "") || x.Name.Contains(t)))
            .OrderBy(x => x.SortOrder)
            .Select(x => (int?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task PopulateOwnerDisplayForLogRaidAsync(int? ownerUserId, CancellationToken cancellationToken)
    {
        if (!ownerUserId.HasValue || ownerUserId.Value <= 0)
            return;
        var u = await _context.Users.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == ownerUserId.Value, cancellationToken);
        if (u != null)
        {
            ViewBag.OwnerName = u.Name;
            ViewBag.OwnerEmail = u.Email;
        }
    }

    private async Task<(IActionResult? ErrorResult, WorkItem? Work)> PrepareModernLogRaidPageAsync(int id, CancellationToken cancellationToken)
    {
        ViewBag.MainNavSection = "work";

        var userEmail = User.Identity?.Name;
        if (string.IsNullOrEmpty(userEmail))
            return (Unauthorized(), null);

        var currentUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == userEmail.ToLower(), cancellationToken);
        if (currentUser == null)
            return (Unauthorized(), null);

        var projectExists = await _context.Projects.AsNoTracking()
            .AnyAsync(p => p.Id == id && !p.IsDeleted, cancellationToken);
        if (!projectExists)
            return (NotFound(), null);

        var work = await _modernWork.GetWorkItemAsync(id);
        if (work == null)
            return (NotFound(), null);

        var milestones = await _context.Milestones.AsNoTracking()
            .Where(m => m.ProjectId == id)
            .OrderBy(m => m.DueDate)
            .ToListAsync(cancellationToken);

        var directorates = await _context.Divisions.AsNoTracking()
            .Where(d => d.IsActive)
            .OrderBy(d => d.Name)
            .Select(d => new RiskIssueNamedIntOption { Id = d.Id, Name = d.Name ?? "" })
            .ToListAsync(cancellationToken);

        var project = await _context.Projects.AsNoTracking()
            .Include(p => p.PrimaryOrganizationalGroup)
            .Include(p => p.PhaseLookup)
            .Include(p => p.DeliveryPriority)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        ViewBag.WorkItem = work;
        ViewBag.Milestones = milestones;
        ViewBag.DirectorateOptions = directorates;
        ViewBag.WorkChromeSection = "risks";
        ViewBag.WorkChromeSubPage = true;
        ViewBag.WorkChromeTabsAsLinks = true;
        ViewBag.WorkIdShort = work.Id.ToString("X8").ToUpperInvariant();
        if (project != null)
        {
            ViewBag.PortfolioName = project.PrimaryOrganizationalGroup?.Name;
            ViewBag.DeliveryPhaseName = project.PhaseLookup?.Name;
            ViewBag.PriorityName = project.DeliveryPriority?.Name;
        }

        return (null, work);
    }

    /// <summary>Bridge from modern work UI to legacy risk detail (project-scoped risk entity).</summary>
    [HttpGet("{workId:int}/risk/{id:int}")]
    public async Task<IActionResult> RiskDetail(int workId, int id, CancellationToken cancellationToken = default)
    {
        var exists = await _context.Risks.AsNoTracking()
            .AnyAsync(r => r.Id == id && r.ProjectId == workId && !r.IsDeleted, cancellationToken);
        if (!exists)
            return NotFound();

        return RedirectToAction("Details", "Risk", new { id });
    }

    /// <summary>Bridge from modern work UI to legacy issue edit.</summary>
    [HttpGet("{workId:int}/issue/{id:int}")]
    public async Task<IActionResult> IssueDetail(int workId, int id, CancellationToken cancellationToken = default)
    {
        var exists = await _context.Issues.AsNoTracking()
            .AnyAsync(i => i.Id == id && i.ProjectId == workId && !i.IsDeleted, cancellationToken);
        if (!exists)
            return NotFound();

        return RedirectToAction("EditIssue", "Project", new { projectId = workId, issueId = id });
    }

    [HttpGet("watching")]
    public async Task<IActionResult> Watching(
        string? search,
        int? portfolioId,
        int? directorateId,
        string? status,
        CancellationToken cancellationToken = default)
    {
        ViewBag.MainNavSection = "work";
        ViewBag.SubNavItem = "work-watching";

        var userEmail = User.Identity?.Name;
        if (string.IsNullOrEmpty(userEmail))
            return Unauthorized();

        var currentUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == userEmail.ToLower(), cancellationToken);
        if (currentUser == null)
            return Unauthorized();

        var items = await _modernWork.GetWatchingWorkItemsAsync(
            currentUser, search, portfolioId, directorateId, status, cancellationToken);

        var phaseNames = await _context.PhaseLookups.AsNoTracking()
            .Where(p => p.IsActive)
            .ToDictionaryAsync(p => p.Id, p => p.Name ?? "", cancellationToken);
        var priorityNames = await _context.DeliveryPriorities.AsNoTracking()
            .ToDictionaryAsync(p => p.Id, p => p.Name ?? "", cancellationToken);

        var portfolioIds = items.Select(w => w.PortfolioId).Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
        var portfolioNames = portfolioIds.Count > 0
            ? await _context.OrganizationalGroups.AsNoTracking()
                .Where(g => portfolioIds.Contains(g.Id))
                .ToDictionaryAsync(g => g.Id, g => g.Name, cancellationToken)
            : new Dictionary<int, string>();

        var orgGroups = await _context.OrganizationalGroups.AsNoTracking().Where(g => g.IsActive).OrderBy(g => g.Name).ToListAsync(cancellationToken);
        ViewBag.Portfolios = orgGroups.Select(g => new Portfolio { Id = g.Id, Name = g.Name, IsActive = true }).ToList();
        ViewBag.Directorates = await _context.Divisions.AsNoTracking().Where(d => d.IsActive).OrderBy(d => d.Name)
            .Select(d => new Directorate { Id = d.Id, Name = d.Name, IsActive = true }).ToListAsync(cancellationToken);
        ViewBag.WorkStatusOptions = new List<LookupOption>
        {
            new() { Name = "Active", Value = "Active" },
            new() { Name = "Paused", Value = "Paused" },
            new() { Name = "Completed", Value = "Completed" },
            new() { Name = "Cancelled", Value = "Cancelled" }
        };
        ViewBag.StatusFilter = status;
        ViewBag.Search = search;
        ViewBag.FilterPortfolioId = portfolioId;
        ViewBag.FilterDirectorateId = directorateId;
        ViewBag.PortfolioNames = portfolioNames;
        ViewBag.PhaseNames = phaseNames;
        ViewBag.PriorityNames = priorityNames;

        return View("~/Views/Modern/Work/Watching.cshtml", items);
    }

    [HttpGet("bypriority")]
    public async Task<IActionResult> ByPriority(
        string? search,
        int? portfolioId,
        int? directorateId,
        int? priorityId,
        string? status,
        string? tab = null,
        CancellationToken cancellationToken = default)
    {
        ViewBag.MainNavSection = "work";
        ViewBag.SubNavItem = "work-bypriority";

        var tabKey = (tab ?? "mission").Trim().ToLowerInvariant();
        if (tabKey != "mission" && tabKey != "outcomes")
            tabKey = "mission";
        ViewBag.ByPriorityTab = tabKey;

        var items = await _modernWork.GetByPriorityWorkItemsAsync(
            search, portfolioId, directorateId, priorityId, status, cancellationToken);

        var portfolioIds = items.Select(w => w.PortfolioId).Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
        var portfolioNames = portfolioIds.Count > 0
            ? await _context.OrganizationalGroups.AsNoTracking()
                .Where(g => portfolioIds.Contains(g.Id))
                .ToDictionaryAsync(g => g.Id, g => g.Name, cancellationToken)
            : new Dictionary<int, string>();

        var orgGroups = await _context.OrganizationalGroups.AsNoTracking().Where(g => g.IsActive).OrderBy(g => g.Name).ToListAsync(cancellationToken);
        ViewBag.Portfolios = orgGroups.Select(g => new Portfolio { Id = g.Id, Name = g.Name, IsActive = true }).ToList();
        ViewBag.Directorates = await _context.Divisions.AsNoTracking().Where(d => d.IsActive).OrderBy(d => d.Name)
            .Select(d => new Directorate { Id = d.Id, Name = d.Name, IsActive = true }).ToListAsync(cancellationToken);
        ViewBag.PriorityOptions = await _context.DeliveryPriorities.AsNoTracking().OrderBy(p => p.SortOrder)
            .Select(p => new LookupOption { Id = p.Id, Name = p.Name ?? "", Value = p.Name ?? "" }).ToListAsync(cancellationToken);
        ViewBag.WorkStatusOptions = new List<LookupOption>
        {
            new() { Name = "Active", Value = "Active" },
            new() { Name = "Paused", Value = "Paused" },
            new() { Name = "Completed", Value = "Completed" },
            new() { Name = "Cancelled", Value = "Cancelled" }
        };
        ViewBag.Search = search;
        ViewBag.FilterPortfolioId = portfolioId;
        ViewBag.FilterDirectorateId = directorateId;
        ViewBag.PriorityFilter = priorityId;
        ViewBag.StatusFilter = status;
        ViewBag.PortfolioNames = portfolioNames;

        var missionPillarList = await _context.Missions.AsNoTracking()
            .Where(m => !m.IsDeleted)
            .OrderBy(m => m.Title)
            .Select(m => new WorkLookupOption { Id = m.Id, Name = m.Title, Value = m.Title })
            .ToListAsync(cancellationToken);
        var priorityOutcomeList = await _context.Objectives.AsNoTracking()
            .Where(o => !o.IsDeleted && o.Status == "active")
            .OrderBy(o => o.Title)
            .Select(o => new WorkLookupOption { Id = o.Id, Name = o.Title, Value = o.Title })
            .ToListAsync(cancellationToken);

        ViewBag.MissionPillars = missionPillarList;
        ViewBag.PriorityOutcomes = priorityOutcomeList;
        ViewBag.WorkCountByMissionPillarId = missionPillarList.ToDictionary(
            x => x.Id,
            x => items.Count(w => w.MissionPillars.Any(mp => mp.MissionPillarId == x.Id)));
        ViewBag.WorkCountByPriorityOutcomeId = priorityOutcomeList.ToDictionary(
            x => x.Id,
            x => items.Count(w => w.PriorityOutcomes.Any(po => po.PriorityOutcomeId == x.Id)));

        return View("~/Views/Modern/Work/ByPriority.cshtml", items);
    }

    [HttpGet("flagship")]
    public async Task<IActionResult> Flagship(
        string? search,
        int? portfolioId,
        int? directorateId,
        string? status,
        CancellationToken cancellationToken = default)
    {
        ViewBag.MainNavSection = "work";
        ViewBag.SubNavItem = "work-flagship";

        var items = await _modernWork.GetFlagshipWorkItemsAsync(
            search, portfolioId, directorateId, status, cancellationToken);

        var portfolioIds = items.Select(w => w.PortfolioId).Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
        var portfolioNames = portfolioIds.Count > 0
            ? await _context.OrganizationalGroups.AsNoTracking()
                .Where(g => portfolioIds.Contains(g.Id))
                .ToDictionaryAsync(g => g.Id, g => g.Name, cancellationToken)
            : new Dictionary<int, string>();

        var orgGroups = await _context.OrganizationalGroups.AsNoTracking().Where(g => g.IsActive).OrderBy(g => g.Name).ToListAsync(cancellationToken);
        ViewBag.Portfolios = orgGroups.Select(g => new Portfolio { Id = g.Id, Name = g.Name, IsActive = true }).ToList();
        ViewBag.Directorates = await _context.Divisions.AsNoTracking().Where(d => d.IsActive).OrderBy(d => d.Name)
            .Select(d => new Directorate { Id = d.Id, Name = d.Name, IsActive = true }).ToListAsync(cancellationToken);
        ViewBag.WorkStatusOptions = new List<LookupOption>
        {
            new() { Name = "Active", Value = "Active" },
            new() { Name = "Paused", Value = "Paused" },
            new() { Name = "Completed", Value = "Completed" },
            new() { Name = "Cancelled", Value = "Cancelled" }
        };
        ViewBag.Search = search;
        ViewBag.FilterPortfolioId = portfolioId;
        ViewBag.FilterDirectorateId = directorateId;
        ViewBag.StatusFilter = status;
        ViewBag.PortfolioNames = portfolioNames;

        return View("~/Views/Modern/Work/Flagship.cshtml", items);
    }

    [HttpGet("flagship/export")]
    public async Task<IActionResult> ExportFlagship(
        string? search,
        int? portfolioId,
        int? directorateId,
        string? status,
        CancellationToken cancellationToken = default)
    {
        var items = await _modernWork.GetFlagshipWorkItemsAsync(
            search, portfolioId, directorateId, status, cancellationToken);

        var portfolioIds = items.Select(w => w.PortfolioId).Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
        var portfolioNames = portfolioIds.Count > 0
            ? await _context.OrganizationalGroups.AsNoTracking()
                .Where(g => portfolioIds.Contains(g.Id))
                .ToDictionaryAsync(g => g.Id, g => g.Name, cancellationToken)
            : new Dictionary<int, string>();

        var csv = BuildFlagshipCsv(items, portfolioNames);
        var fileName = $"flagship-work-{DateTime.UtcNow:yyyyMMdd-HHmm}.csv";
        return File(csv, "text/csv; charset=utf-8", fileName);
    }

    private static byte[] BuildFlagshipCsv(IReadOnlyList<WorkItem> items, IReadOnlyDictionary<int, string> portfolioNames)
    {
        static string CsvField(string? value)
        {
            var s = value ?? "";
            if (s.Contains('"', StringComparison.Ordinal))
                s = s.Replace("\"", "\"\"", StringComparison.Ordinal);
            if (s.Contains(',') || s.Contains('\n') || s.Contains('\r'))
                return "\"" + s + "\"";
            return s;
        }

        var sb = new StringBuilder();
        sb.AppendLine("Work item,Reference,Portfolio,RAG,Status,Start date,Target end,Updated");
        foreach (var w in items)
        {
            var latestRag = w.RagHistory?.OrderByDescending(r => r.UpdatedAt).FirstOrDefault();
            var ragName = latestRag?.RagStatus?.Name ?? "";
            var portfolio = w.PortfolioId.HasValue && portfolioNames.TryGetValue(w.PortfolioId.Value, out var pn) ? pn : "";
            sb.Append(CsvField(w.Title)).Append(',')
                .Append(CsvField("WI-" + w.Id.ToString("D8", CultureInfo.InvariantCulture))).Append(',')
                .Append(CsvField(portfolio)).Append(',')
                .Append(CsvField(ragName)).Append(',')
                .Append(CsvField(w.Status)).Append(',')
                .Append(CsvField(w.StartDate?.ToString("d MMM yyyy", CultureInfo.InvariantCulture))).Append(',')
                .Append(CsvField(w.TargetEndDate?.ToString("d MMM yyyy", CultureInfo.InvariantCulture))).Append(',')
                .Append(CsvField(w.UpdatedAt.ToString("d MMM yyyy", CultureInfo.InvariantCulture)))
                .AppendLine();
        }

        var preamble = Encoding.UTF8.GetPreamble();
        return preamble.Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }

    [HttpGet("directorates")]
    public async Task<IActionResult> Directorates(string? search, CancellationToken cancellationToken = default)
    {
        ViewBag.MainNavSection = "work";
        ViewBag.SubNavItem = "work-directorates";
        ViewBag.Search = search;

        var rows = await _context.Divisions.AsNoTracking()
            .Where(d => d.IsActive)
            .OrderBy(d => d.Name)
            .ToListAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            rows = rows.Where(d => d.Name.Contains(s, StringComparison.OrdinalIgnoreCase)
                || (d.Description != null && d.Description.Contains(s, StringComparison.OrdinalIgnoreCase))).ToList();
        }

        var directorates = rows.Select(d => new Directorate
        {
            Id = d.Id,
            Name = d.Name,
            Description = d.Description,
            IsActive = d.IsActive
        }).ToList();

        var activeProjectIds = await _context.Projects.AsNoTracking()
            .Where(p => !p.IsDeleted)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);
        var counts = activeProjectIds.Count == 0
            ? new Dictionary<int, int>()
            : await _context.ProjectDirectorates.AsNoTracking()
                .Where(pd => activeProjectIds.Contains(pd.ProjectId))
                .GroupBy(pd => pd.DivisionId)
                .Select(g => new { DivisionId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.DivisionId, x => x.Count, cancellationToken);

        ViewBag.Directorates = directorates;
        ViewBag.WorkCountByDirectorateId = counts;
        return View("~/Views/Modern/Work/Directorates.cshtml");
    }
}
