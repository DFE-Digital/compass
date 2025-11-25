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

    public IActionResult Updates(int? projectId)
    {
        if (!projectId.HasValue)
        {
            return NotFound();
        }

        // Redirect to Project/Details with monthlyupdates tab
        return RedirectToAction("Details", "Project", new { id = projectId.Value, tab = "monthlyupdates" });
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

        var project = await _context.Projects
            .Include(p => p.Directorates)
                .ThenInclude(d => d.DirectorateLookup)
            .Include(p => p.Milestones)
            .Include(p => p.ProjectContacts)
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

        var hasDigitalDirectorate = project.Directorates?.Any(d => d.DirectorateLookup != null && 
            d.DirectorateLookup.Name.Contains("Digital", StringComparison.OrdinalIgnoreCase)) == true;

        // Check if update already exists
        var existingUpdate = await _context.ProjectMonthlyUpdates
            .FirstOrDefaultAsync(u => u.ProjectId == projectId.Value && u.Year == year.Value && u.Month == month.Value);

        ViewBag.ProjectId = project.Id;
        ViewBag.ProjectTitle = project.Title;
        ViewBag.ProjectCode = $"DFE-DDT-{project.Id}";
        ViewBag.Year = year.Value;
        ViewBag.Month = month.Value;
        ViewBag.PeriodName = new DateTime(year.Value, month.Value, 1).ToString("MMMM yyyy");
        ViewBag.HasDigitalDirectorate = hasDigitalDirectorate;
        ViewBag.IsFlagship = project.IsFlagship;
        ViewBag.Project = project; // Pass full project for navigation badges
        ViewBag.ExistingUpdate = existingUpdate;
        ViewBag.Narratives = new List<Compass.Models.MonthlyUpdateNarrative>(); // Will be populated when model is added to DbContext
        
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
        ViewBag.IsFlagship = project.IsFlagship;
        
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

        // Check if update already exists, if not create it
        var existingUpdate = await _context.ProjectMonthlyUpdates
            .FirstOrDefaultAsync(u => u.ProjectId == projectId.Value && u.Year == year.Value && u.Month == month.Value);

        var narrativeText = Request.Form["Narrative"].ToString().Trim();
        
        if (string.IsNullOrWhiteSpace(narrativeText))
        {
            ModelState.AddModelError("Narrative", "Narrative is required.");
        }

        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(narrativeText))
        {
            var projectForView = await _context.Projects
                .Include(p => p.Directorates)
                    .ThenInclude(d => d.DirectorateLookup)
                .Include(p => p.Milestones)
                .Include(p => p.ProjectContacts)
                .FirstOrDefaultAsync(p => p.Id == projectId.Value && !p.IsDeleted);
            
            if (projectForView != null)
            {
                // Manually load dependencies since the relationship is polymorphic
                projectForView.DependenciesAsSource = await _context.Dependencies
                    .Where(d => d.SourceEntityType == "Project" && d.SourceEntityId == projectForView.Id)
                    .ToListAsync();

                projectForView.DependenciesAsTarget = await _context.Dependencies
                    .Where(d => d.TargetEntityType == "Project" && d.TargetEntityId == projectForView.Id)
                    .ToListAsync();
            }
            
            if (projectForView == null)
            {
                return NotFound();
            }
            
            var hasDigitalDirectorate = projectForView.Directorates?.Any(d => d.DirectorateLookup != null && 
                d.DirectorateLookup.Name.Contains("Digital", StringComparison.OrdinalIgnoreCase)) == true;

            ViewBag.ProjectId = projectForView.Id;
            ViewBag.ProjectTitle = projectForView.Title;
            ViewBag.ProjectCode = $"DFE-DDT-{projectForView.Id}";
            ViewBag.Year = year.Value;
            ViewBag.Month = month.Value;
            ViewBag.PeriodName = new DateTime(year.Value, month.Value, 1).ToString("MMMM yyyy");
            ViewBag.HasDigitalDirectorate = hasDigitalDirectorate;
            ViewBag.IsFlagship = projectForView.IsFlagship;
            ViewBag.Project = projectForView;
            ViewBag.ExistingUpdate = existingUpdate;
            ViewBag.Narratives = new List<MonthlyUpdateNarrative>();
            return View(model);
        }

        // Get user information from claims
        var userObjectIdClaim = User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;
        var userEmailClaim = User.FindFirst(ClaimTypes.Email)?.Value 
            ?? User.FindFirst("preferred_username")?.Value
            ?? User.FindFirst("email")?.Value;
        var userNameClaim = User.FindFirst(ClaimTypes.Name)?.Value 
            ?? User.FindFirst("name")?.Value;

        ProjectMonthlyUpdate update;
        
        if (existingUpdate == null)
        {
            // Create new monthly update
            update = new ProjectMonthlyUpdate
            {
                ProjectId = projectId.Value,
                Year = year.Value,
                Month = month.Value,
                Narrative = narrativeText, // Keep for backward compatibility
                CreatedByEntraId = userObjectIdClaim,
                CreatedByName = userNameClaim,
                CreatedByEmail = userEmailClaim,
                CreatedAt = DateTime.UtcNow,
                SubmittedAt = DateTime.UtcNow
            };
            _context.ProjectMonthlyUpdates.Add(update);
            await _context.SaveChangesAsync();
        }
        else
        {
            update = existingUpdate;
            // Update submitted date if not already set
            if (!update.SubmittedAt.HasValue)
            {
                update.SubmittedAt = DateTime.UtcNow;
            }
        }

        // For now, we'll store the narrative in the main Narrative field
        // When MonthlyUpdateNarrative is added to DbContext, we'll create entries there
        // For backward compatibility, append to existing narrative
        if (string.IsNullOrEmpty(update.Narrative))
        {
            update.Narrative = narrativeText;
        }
        else
        {
            update.Narrative += "\n\n" + narrativeText;
        }
        
        update.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Redirect back to the same page to show the new narrative
        return RedirectToAction("CreateUpdate", new { projectId = projectId.Value, year = year.Value, month = month.Value });
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

        return RedirectToAction("Details", "Project", new { id = projectId.Value, tab = "monthlyupdates" });
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

