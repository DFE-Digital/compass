using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Compass.Data;
using Compass.Models;
using Compass.Services;
using Compass.ViewModels;
using System.Security.Claims;

namespace Compass.Controllers;

[Authorize]
public class MilestonesUpdatesSuccessesController : Controller
{
    private readonly CompassDbContext _context;
    private readonly ILogger<MilestonesUpdatesSuccessesController> _logger;
    private readonly IMonthlyUpdateService _monthlyUpdateService;

    public MilestonesUpdatesSuccessesController(
        CompassDbContext context,
        ILogger<MilestonesUpdatesSuccessesController> logger,
        IMonthlyUpdateService monthlyUpdateService)
    {
        _context = context;
        _logger = logger;
        _monthlyUpdateService = monthlyUpdateService;
    }

    public async Task<IActionResult> Overview(int? projectId)
    {
        if (!projectId.HasValue)
        {
            return NotFound();
        }

        var project = await _context.Projects
            .Include(p => p.Milestones)
            .Include(p => p.MonthlyUpdates)
            .Include(p => p.WeeklySuccessUpdates)
            .Include(p => p.Outcomes)
            .Include(p => p.Successes)
            .Include(p => p.ProjectContacts)
            .Include(p => p.StatusUpdates)
            .Include(p => p.Directorates)
                .ThenInclude(d => d.DirectorateLookup)
            .Include(p => p.Risks)
            .Include(p => p.Actions)
            .Include(p => p.Issues)
            .Include(p => p.Decisions)
            .Include(p => p.ProjectProducts)
            .FirstOrDefaultAsync(p => p.Id == projectId.Value && !p.IsDeleted);

        if (project == null)
        {
            return NotFound();
        }

        // Manually load dependencies since the relationship is polymorphic
        project.DependenciesAsSource = await _context.Dependencies
            .Where(d => d.SourceEntityType == "Project" && d.SourceEntityId == project.Id)
            .ToListAsync();

        project.DependenciesAsTarget = await _context.Dependencies
            .Where(d => d.TargetEntityType == "Project" && d.TargetEntityId == project.Id)
            .ToListAsync();

        ViewBag.Project = project;

        var deliveryCode = $"DFE-DDT-{project.Id}";
        
        // Calculate milestone counts
        var milestones = project.Milestones.Where(m => !m.IsDeleted).ToList();
        var totalMilestones = milestones.Count;
        var completedMilestones = milestones.Count(m => string.Equals(m.Status, "complete", StringComparison.OrdinalIgnoreCase));
        var activeMilestones = totalMilestones - completedMilestones;
        var overdueMilestones = milestones.Count(m =>
            !string.Equals(m.Status, "complete", StringComparison.OrdinalIgnoreCase) &&
            m.DueDate.Date < DateTime.Today);

        // Calculate monthly update counts and periods
        var monthlyUpdates = project.MonthlyUpdates.ToList();
        var totalMonthlyUpdates = monthlyUpdates.Count;
        
        // Generate periods starting from November 2025
        var startDate = new DateTime(2025, 11, 1);
        var currentDate = DateTime.UtcNow;
        var periods = new List<MonthlyUpdatePeriodStatus>();
        
        var periodDate = startDate;
        while (periodDate <= currentDate.AddMonths(1))
        {
            var year = periodDate.Year;
            var month = periodDate.Month;
            var dueDate = _monthlyUpdateService.GetMonthlyUpdateDueDate(year, month);
            var update = monthlyUpdates.FirstOrDefault(u => u.Year == year && u.Month == month);
            var status = _monthlyUpdateService.CalculateUpdateStatus(year, month, update?.SubmittedAt);
            
            periods.Add(new MonthlyUpdatePeriodStatus
            {
                Year = year,
                Month = month,
                PeriodName = periodDate.ToString("MMMM yyyy"),
                DueDate = dueDate,
                Status = status.ToString().ToLowerInvariant(),
                SubmittedDate = update?.SubmittedAt,
                HasUpdate = update != null
            });
            
            periodDate = periodDate.AddMonths(1);
        }
        
        var dueMonthlyUpdates = periods.Count(p => p.Status == "due");
        var lateMonthlyUpdates = periods.Count(p => p.Status == "late");
        var submittedMonthlyUpdates = periods.Count(p => p.Status == "submitted");

        // Calculate weekly success counts
        var weeklySuccesses = project.WeeklySuccessUpdates.ToList();
        var totalWeeklySuccesses = weeklySuccesses.Count;
        
        // Get current week
        var currentWeek = GetWeekNumber(DateTime.UtcNow);
        var currentYear = DateTime.UtcNow.Year;
        var thisWeekSuccesses = weeklySuccesses.Count(s => s.Year == currentYear && s.WeekNumber == currentWeek);

        var viewModel = new MilestonesUpdatesSuccessesOverviewViewModel
        {
            ProjectId = project.Id,
            ProjectTitle = project.Title,
            ProjectCode = deliveryCode,
            TotalMilestones = totalMilestones,
            ActiveMilestones = activeMilestones,
            CompletedMilestones = completedMilestones,
            OverdueMilestones = overdueMilestones,
            TotalMonthlyUpdates = totalMonthlyUpdates,
            DueMonthlyUpdates = dueMonthlyUpdates,
            LateMonthlyUpdates = lateMonthlyUpdates,
            SubmittedMonthlyUpdates = submittedMonthlyUpdates,
            TotalWeeklySuccesses = totalWeeklySuccesses,
            ThisWeekSuccesses = thisWeekSuccesses,
            MonthlyUpdatePeriods = periods.OrderByDescending(p => p.Year).ThenByDescending(p => p.Month).ToList()
        };

        return View(viewModel);
    }

    public async Task<IActionResult> Milestones(int? projectId)
    {
        if (!projectId.HasValue)
        {
            return NotFound();
        }

        var project = await _context.Projects
            .Include(p => p.Milestones.Where(m => !m.IsDeleted))
            .Include(p => p.Outcomes)
            .Include(p => p.Successes)
            .Include(p => p.ProjectContacts)
            .Include(p => p.StatusUpdates)
            .Include(p => p.Directorates)
                .ThenInclude(d => d.DirectorateLookup)
            .Include(p => p.Risks)
            .Include(p => p.Actions)
            .Include(p => p.Issues)
            .Include(p => p.Decisions)
            .Include(p => p.ProjectProducts)
            .FirstOrDefaultAsync(p => p.Id == projectId.Value && !p.IsDeleted);

        if (project == null)
        {
            return NotFound();
        }

        // Manually load dependencies since the relationship is polymorphic
        project.DependenciesAsSource = await _context.Dependencies
            .Where(d => d.SourceEntityType == "Project" && d.SourceEntityId == project.Id)
            .ToListAsync();

        project.DependenciesAsTarget = await _context.Dependencies
            .Where(d => d.TargetEntityType == "Project" && d.TargetEntityId == project.Id)
            .ToListAsync();

        ViewBag.ProjectId = project.Id;
        ViewBag.ProjectTitle = project.Title;
        ViewBag.ProjectCode = $"DFE-DDT-{project.Id}";
        
        return View(project);
    }

    public async Task<IActionResult> Updates(int? projectId)
    {
        if (!projectId.HasValue)
        {
            return NotFound();
        }

        var project = await _context.Projects
            .Include(p => p.MonthlyUpdates)
            .Include(p => p.Outcomes)
            .Include(p => p.Successes)
            .Include(p => p.ProjectContacts)
            .Include(p => p.StatusUpdates)
            .Include(p => p.Directorates)
                .ThenInclude(d => d.DirectorateLookup)
            .Include(p => p.Risks)
            .Include(p => p.Actions)
            .Include(p => p.Issues)
            .Include(p => p.Decisions)
            .Include(p => p.ProjectProducts)
            .FirstOrDefaultAsync(p => p.Id == projectId.Value && !p.IsDeleted);

        if (project == null)
        {
            return NotFound();
        }

        // Manually load dependencies since the relationship is polymorphic
        project.DependenciesAsSource = await _context.Dependencies
            .Where(d => d.SourceEntityType == "Project" && d.SourceEntityId == project.Id)
            .ToListAsync();

        project.DependenciesAsTarget = await _context.Dependencies
            .Where(d => d.TargetEntityType == "Project" && d.TargetEntityId == project.Id)
            .ToListAsync();

        ViewBag.ProjectId = project.Id;
        ViewBag.ProjectTitle = project.Title;
        ViewBag.ProjectCode = $"DFE-DDT-{project.Id}";
        ViewBag.MonthlyUpdateService = _monthlyUpdateService;
        
        return View(project);
    }

    public async Task<IActionResult> Successes(int? projectId)
    {
        if (!projectId.HasValue)
        {
            return NotFound();
        }

        var project = await _context.Projects
            .Include(p => p.WeeklySuccessUpdates)
            .Include(p => p.Successes)
            .Include(p => p.Outcomes)
            .Include(p => p.ProjectContacts)
            .Include(p => p.StatusUpdates)
            .Include(p => p.Directorates)
                .ThenInclude(d => d.DirectorateLookup)
            .Include(p => p.Risks)
            .Include(p => p.Actions)
            .Include(p => p.Issues)
            .Include(p => p.Decisions)
            .Include(p => p.ProjectProducts)
            .FirstOrDefaultAsync(p => p.Id == projectId.Value && !p.IsDeleted);

        if (project == null)
        {
            return NotFound();
        }

        // Manually load dependencies since the relationship is polymorphic
        project.DependenciesAsSource = await _context.Dependencies
            .Where(d => d.SourceEntityType == "Project" && d.SourceEntityId == project.Id)
            .ToListAsync();

        project.DependenciesAsTarget = await _context.Dependencies
            .Where(d => d.TargetEntityType == "Project" && d.TargetEntityId == project.Id)
            .ToListAsync();

        ViewBag.ProjectId = project.Id;
        ViewBag.ProjectTitle = project.Title;
        ViewBag.ProjectCode = $"DFE-DDT-{project.Id}";
        
        return View(project);
    }

    public async Task<IActionResult> CreateUpdate(int? projectId, int? year, int? month)
    {
        if (!projectId.HasValue || !year.HasValue || !month.HasValue)
        {
            return NotFound();
        }

        var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == projectId.Value && !p.IsDeleted);
        if (project == null)
        {
            return NotFound();
        }

        ViewBag.ProjectId = project.Id;
        ViewBag.ProjectTitle = project.Title;
        ViewBag.ProjectCode = $"DFE-DDT-{project.Id}";
        ViewBag.Year = year.Value;
        ViewBag.Month = month.Value;
        ViewBag.PeriodName = new DateTime(year.Value, month.Value, 1).ToString("MMMM yyyy");
        
        // TODO: Implement create update form
        return View();
    }

    public async Task<IActionResult> EditUpdate(int? projectId, int? year, int? month)
    {
        if (!projectId.HasValue || !year.HasValue || !month.HasValue)
        {
            return NotFound();
        }

        var project = await _context.Projects
            .Include(p => p.MonthlyUpdates)
            .FirstOrDefaultAsync(p => p.Id == projectId.Value && !p.IsDeleted);
        
        if (project == null)
        {
            return NotFound();
        }

        var update = project.MonthlyUpdates.FirstOrDefault(u => u.Year == year.Value && u.Month == month.Value);
        if (update == null)
        {
            return RedirectToAction("CreateUpdate", new { projectId = projectId.Value, year = year.Value, month = month.Value });
        }

        ViewBag.ProjectId = project.Id;
        ViewBag.ProjectTitle = project.Title;
        ViewBag.ProjectCode = $"DFE-DDT-{project.Id}";
        ViewBag.Year = year.Value;
        ViewBag.Month = month.Value;
        ViewBag.PeriodName = new DateTime(year.Value, month.Value, 1).ToString("MMMM yyyy");
        
        // TODO: Implement edit update form
        return View(update);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateUpdate(int? projectId, int? year, int? month, ProjectMonthlyUpdate model)
    {
        if (!projectId.HasValue || !year.HasValue || !month.HasValue)
        {
            return NotFound();
        }

        var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == projectId.Value && !p.IsDeleted);
        if (project == null)
        {
            return NotFound();
        }

        // Check if update already exists
        var existingUpdate = await _context.ProjectMonthlyUpdates
            .FirstOrDefaultAsync(u => u.ProjectId == projectId.Value && u.Year == year.Value && u.Month == month.Value);
        
        if (existingUpdate != null)
        {
            return RedirectToAction("EditUpdate", new { projectId = projectId.Value, year = year.Value, month = month.Value });
        }

        if (!ModelState.IsValid)
        {
            ViewBag.ProjectId = project.Id;
            ViewBag.ProjectTitle = project.Title;
            ViewBag.ProjectCode = $"DFE-DDT-{project.Id}";
            ViewBag.Year = year.Value;
            ViewBag.Month = month.Value;
            ViewBag.PeriodName = new DateTime(year.Value, month.Value, 1).ToString("MMMM yyyy");
            return View(model);
        }

        // Get user information from claims
        var userObjectIdClaim = User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;
        var userEmailClaim = User.FindFirst(ClaimTypes.Email)?.Value 
            ?? User.FindFirst("preferred_username")?.Value
            ?? User.FindFirst("email")?.Value;
        var userNameClaim = User.FindFirst(ClaimTypes.Name)?.Value 
            ?? User.FindFirst("name")?.Value;

        var update = new ProjectMonthlyUpdate
        {
            ProjectId = projectId.Value,
            Year = year.Value,
            Month = month.Value,
            Narrative = model.Narrative ?? string.Empty,
            CreatedByEntraId = userObjectIdClaim,
            CreatedByName = userNameClaim,
            CreatedByEmail = userEmailClaim,
            CreatedAt = DateTime.UtcNow,
            SubmittedAt = DateTime.UtcNow
        };

        _context.ProjectMonthlyUpdates.Add(update);
        await _context.SaveChangesAsync();

        return RedirectToAction("Updates", new { projectId = projectId.Value });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditUpdate(int? projectId, int? year, int? month, ProjectMonthlyUpdate model)
    {
        if (!projectId.HasValue || !year.HasValue || !month.HasValue)
        {
            return NotFound();
        }

        var update = await _context.ProjectMonthlyUpdates
            .FirstOrDefaultAsync(u => u.Id == model.Id && u.ProjectId == projectId.Value && u.Year == year.Value && u.Month == month.Value);
        
        if (update == null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            ViewBag.ProjectId = projectId.Value;
            var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == projectId.Value && !p.IsDeleted);
            ViewBag.ProjectTitle = project?.Title ?? "Project";
            ViewBag.ProjectCode = $"DFE-DDT-{projectId.Value}";
            ViewBag.Year = year.Value;
            ViewBag.Month = month.Value;
            ViewBag.PeriodName = new DateTime(year.Value, month.Value, 1).ToString("MMMM yyyy");
            return View(update);
        }

        // Get user information from claims
        var userObjectIdClaim = User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;
        var userEmailClaim = User.FindFirst(ClaimTypes.Email)?.Value 
            ?? User.FindFirst("preferred_username")?.Value
            ?? User.FindFirst("email")?.Value;
        var userNameClaim = User.FindFirst(ClaimTypes.Name)?.Value 
            ?? User.FindFirst("name")?.Value;

        update.Narrative = model.Narrative ?? string.Empty;
        update.UpdatedAt = DateTime.UtcNow;
        
        // If not already submitted, mark as submitted now
        if (!update.SubmittedAt.HasValue)
        {
            update.SubmittedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return RedirectToAction("Updates", new { projectId = projectId.Value });
    }

    public async Task<IActionResult> CreateWeeklySuccess(int? projectId)
    {
        if (!projectId.HasValue)
        {
            return NotFound();
        }

        var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == projectId.Value && !p.IsDeleted);
        if (project == null)
        {
            return NotFound();
        }

        ViewBag.ProjectId = project.Id;
        ViewBag.ProjectTitle = project.Title;
        ViewBag.ProjectCode = $"DFE-DDT-{project.Id}";
        
        // TODO: Implement create weekly success form
        return View();
    }

    public async Task<IActionResult> EditWeeklySuccess(int? projectId, int? id)
    {
        if (!projectId.HasValue || !id.HasValue)
        {
            return NotFound();
        }

        var update = await _context.ProjectWeeklySuccessUpdates
            .Include(u => u.Project)
            .FirstOrDefaultAsync(u => u.Id == id.Value && u.ProjectId == projectId.Value);
        
        if (update == null)
        {
            return NotFound();
        }

        ViewBag.ProjectId = update.ProjectId;
        ViewBag.ProjectTitle = update.Project.Title;
        ViewBag.ProjectCode = $"DFE-DDT-{update.ProjectId}";
        
        // TODO: Implement edit weekly success form
        return View(update);
    }

    private int GetWeekNumber(DateTime date)
    {
        var culture = System.Globalization.CultureInfo.CurrentCulture;
        var calendar = culture.Calendar;
        return calendar.GetWeekOfYear(date, culture.DateTimeFormat.CalendarWeekRule, culture.DateTimeFormat.FirstDayOfWeek);
    }
}

