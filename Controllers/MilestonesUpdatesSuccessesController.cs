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
    private readonly IPermissionService _permissionService;

    public MilestonesUpdatesSuccessesController(
        CompassDbContext context,
        ILogger<MilestonesUpdatesSuccessesController> logger,
        IMonthlyUpdateService monthlyUpdateService,
        IPermissionService permissionService)
    {
        _context = context;
        _logger = logger;
        _monthlyUpdateService = monthlyUpdateService;
        _permissionService = permissionService;
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
            .Include(p => p.SeniorResponsibleOfficers)
                .ThenInclude(sro => sro.User)
            .Include(p => p.ServiceOwners)
                .ThenInclude(so => so.User)
            .Include(p => p.PrimaryContactUser)
            .Include(p => p.RagHistory)
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
            .Include(u => u.MonthlyUpdateNarratives)
            .FirstOrDefaultAsync(u => u.ProjectId == projectId.Value && u.Year == year.Value && u.Month == month.Value);

        // Check if reporting window has closed
        var dueDate = _monthlyUpdateService.GetMonthlyUpdateDueDate(year.Value, month.Value);
        var canEdit = DateTime.UtcNow <= dueDate;

        // Check if current user can submit (must be SRO, Service Owner, or Primary Contact)
        var canSubmit = await CanUserSubmitMonthlyUpdate(project);

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
        ViewBag.CanEdit = canEdit;
        ViewBag.DueDate = dueDate;
        ViewBag.CanSubmit = canSubmit;
        
        return View();
    }

    public async Task<IActionResult> EditUpdate(int? projectId, int? year, int? month)
    {
        if (!projectId.HasValue || !year.HasValue || !month.HasValue)
        {
            return NotFound();
        }

        var project = await _context.Projects
            .Include(p => p.SeniorResponsibleOfficers)
                .ThenInclude(sro => sro.User)
            .Include(p => p.ServiceOwners)
                .ThenInclude(so => so.User)
            .Include(p => p.PrimaryContactUser)
            .Include(p => p.RagHistory)
            .Include(p => p.MonthlyUpdates)
            .Include(p => p.Directorates)
                .ThenInclude(d => d.DirectorateLookup)
            .Include(p => p.Milestones)
            .Include(p => p.ProjectContacts)
            .Include(p => p.WeeklySuccessUpdates)
            .Include(p => p.Successes)
            .Include(p => p.ProjectProducts)
            .Include(p => p.Risks)
            .Include(p => p.Actions)
            .Include(p => p.Issues)
            .Include(p => p.Decisions)
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

        var update = await _context.ProjectMonthlyUpdates
            .Include(u => u.MonthlyUpdateNarratives)
            .FirstOrDefaultAsync(u => u.ProjectId == projectId.Value && u.Year == year.Value && u.Month == month.Value);
        if (update == null)
        {
            return Redirect($"/MilestonesUpdatesSuccesses/CreateUpdate?projectId={projectId.Value}&year={year.Value}&month={month.Value}");
        }

        // Check if reporting window has closed
        var dueDate = _monthlyUpdateService.GetMonthlyUpdateDueDate(year.Value, month.Value);
        var canEdit = DateTime.UtcNow <= dueDate;

        // Check if current user can submit (must be SRO, Service Owner, or Primary Contact)
        var canSubmit = await CanUserSubmitMonthlyUpdate(project);

        ViewBag.ProjectId = project.Id;
        ViewBag.ProjectTitle = project.Title;
        ViewBag.ProjectCode = $"DFE-DDT-{project.Id}";
        ViewBag.Year = year.Value;
        ViewBag.Month = month.Value;
        ViewBag.PeriodName = new DateTime(year.Value, month.Value, 1).ToString("MMMM yyyy");
        ViewBag.IsFlagship = project.IsFlagship;
        ViewBag.HasDigitalDirectorate = hasDigitalDirectorate;
        ViewBag.Project = project; // Pass full project for navigation badges
        ViewBag.CanSubmit = canSubmit;
        ViewBag.CanEdit = canEdit;
        ViewBag.DueDate = dueDate;
        
        return View(update);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateUpdate(int? projectId, int? year, int? month, ProjectMonthlyUpdate model)
    {
        _logger.LogInformation("CreateUpdate POST received - projectId: {ProjectId}, year: {Year}, month: {Month}", 
            projectId, year, month);
        
        // Try to get values from form if not in route
        if (!projectId.HasValue && Request.Form.ContainsKey("projectId"))
        {
            if (int.TryParse(Request.Form["projectId"].ToString(), out var pid))
            {
                projectId = pid;
            }
        }
        if (!year.HasValue && Request.Form.ContainsKey("year"))
        {
            if (int.TryParse(Request.Form["year"].ToString(), out var y))
            {
                year = y;
            }
        }
        if (!month.HasValue && Request.Form.ContainsKey("month"))
        {
            if (int.TryParse(Request.Form["month"].ToString(), out var m))
            {
                month = m;
            }
        }
        
        if (!projectId.HasValue || !year.HasValue || !month.HasValue)
        {
            _logger.LogWarning("CreateUpdate POST - Missing required parameters: projectId={ProjectId}, year={Year}, month={Month}", 
                projectId, year, month);
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

        // Get narrative from form
        var narrativeText = string.Empty;
        if (Request.Form.ContainsKey("Narrative"))
        {
            narrativeText = Request.Form["Narrative"].ToString() ?? string.Empty;
        }
        narrativeText = narrativeText?.Trim() ?? string.Empty;
        
        _logger.LogInformation("CreateUpdate POST - projectId: {ProjectId}, year: {Year}, month: {Month}, narrative: '{Narrative}' (length: {Length})", 
            projectId, year, month, narrativeText, narrativeText.Length);
        
        // Only validate narrative - ignore ModelState.IsValid because ProjectMonthlyUpdate 
        // model binding might fail (we're reading values directly from Request.Form)
        if (string.IsNullOrWhiteSpace(narrativeText))
        {
            ModelState.AddModelError("Narrative", "Narrative is required.");
            
            // Return view with error
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

            var dueDateForView = _monthlyUpdateService.GetMonthlyUpdateDueDate(year.Value, month.Value);
            var canEditForView = DateTime.UtcNow <= dueDateForView;

            ViewBag.Project = projectForView;
            ViewBag.ExistingUpdate = existingUpdate;
            ViewBag.CanEdit = canEditForView;
            ViewBag.DueDate = dueDateForView;
            return View(model);
        }

        // Check if reporting window has closed
        var dueDate = _monthlyUpdateService.GetMonthlyUpdateDueDate(year.Value, month.Value);
        if (DateTime.UtcNow > dueDate)
        {
            TempData["ErrorMessage"] = "The reporting window for this period has closed. You can no longer add or edit entries.";
            return Redirect($"/MilestonesUpdatesSuccesses/CreateUpdate?projectId={projectId.Value}&year={year.Value}&month={month.Value}");
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
                Narrative = narrativeText,
                CreatedByEntraId = userObjectIdClaim,
                CreatedByName = userNameClaim,
                CreatedByEmail = userEmailClaim,
                CreatedAt = DateTime.UtcNow,
                SubmittedAt = null // Only set when explicitly submitted
            };
            _context.ProjectMonthlyUpdates.Add(update);
        }
        else
        {
            update = existingUpdate;
            update.Narrative = narrativeText;
            update.UpdatedAt = DateTime.UtcNow;
            // Don't set SubmittedAt here - it should only be set when explicitly submitted
        }

        try
        {
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Monthly update saved successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving ProjectMonthlyUpdate for ProjectId {ProjectId}", projectId.Value);
            TempData["ErrorMessage"] = "An error occurred while saving the monthly update. Please try again.";
        }
        
        // Redirect back to the same page to show the new narrative
        // Use explicit route to avoid /api/ prefix
        return Redirect($"/MilestonesUpdatesSuccesses/CreateUpdate?projectId={projectId.Value}&year={year.Value}&month={month.Value}");
    }

    public async Task<IActionResult> EditNarrative(int? projectId, int? year, int? month, int? narrativeId)
    {
        if (!projectId.HasValue || !year.HasValue || !month.HasValue || !narrativeId.HasValue)
        {
            return NotFound();
        }

        // Check if reporting window has closed
        var dueDate = _monthlyUpdateService.GetMonthlyUpdateDueDate(year.Value, month.Value);
        if (DateTime.UtcNow > dueDate)
        {
            TempData["ErrorMessage"] = "The reporting window for this period has closed. You can no longer edit entries.";
            return Redirect($"/MilestonesUpdatesSuccesses/CreateUpdate?projectId={projectId.Value}&year={year.Value}&month={month.Value}");
        }

        var narrative = await _context.MonthlyUpdateNarratives
            .Include(n => n.ProjectMonthlyUpdate)
            .FirstOrDefaultAsync(n => n.Id == narrativeId.Value && 
                n.ProjectMonthlyUpdate.ProjectId == projectId.Value &&
                n.ProjectMonthlyUpdate.Year == year.Value &&
                n.ProjectMonthlyUpdate.Month == month.Value);

        if (narrative == null)
        {
            return NotFound();
        }

        var project = await _context.Projects
            .Include(p => p.PmoContacts)
                .ThenInclude(pc => pc.User)
            .Include(p => p.PrimaryContactUser)
            .FirstOrDefaultAsync(p => p.Id == projectId.Value && !p.IsDeleted);
        if (project == null)
        {
            return NotFound();
        }

        // Create project summary for sidebar
        var projectSummary = new Compass.ViewModels.ProjectSummaryViewModel
        {
            Id = project.Id,
            Title = project.Title,
            ProjectCode = project.ProjectCode,
            Status = project.Status ?? string.Empty,
            RagStatus = project.RagStatus,
            Phase = project.Phase,
            BusinessArea = project.BusinessArea,
            StartDate = project.StartDate,
            TargetDeliveryDate = project.TargetDeliveryDate,
            PrimaryContactName = project.PrimaryContactUser?.Name,
            PrimaryContactEmail = project.PrimaryContactUser?.Email,
            PmoContacts = project.PmoContacts?
                .Where(pc => pc.User != null)
                .Select(pc => new Compass.ViewModels.PmoContactInfo
                {
                    Name = pc.User!.Name ?? string.Empty,
                    Email = pc.User.Email
                })
                .ToList() ?? new List<Compass.ViewModels.PmoContactInfo>()
        };

        ViewBag.ProjectId = project.Id;
        ViewBag.ProjectTitle = project.Title;
        ViewBag.ProjectCode = $"DFE-DDT-{project.Id}";
        ViewBag.Year = year.Value;
        ViewBag.Month = month.Value;
        ViewBag.PeriodName = new DateTime(year.Value, month.Value, 1).ToString("MMMM yyyy");
        ViewBag.NarrativeId = narrativeId.Value;
        ViewBag.ProjectSummary = projectSummary;

        return View(narrative);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditNarrative(int? projectId, int? year, int? month, int? narrativeId, string narrative)
    {
        if (!projectId.HasValue || !year.HasValue || !month.HasValue || !narrativeId.HasValue)
        {
            return NotFound();
        }

        // Check if reporting window has closed
        var dueDate = _monthlyUpdateService.GetMonthlyUpdateDueDate(year.Value, month.Value);
        if (DateTime.UtcNow > dueDate)
        {
            TempData["ErrorMessage"] = "The reporting window for this period has closed. You can no longer edit entries.";
            return Redirect($"/MilestonesUpdatesSuccesses/CreateUpdate?projectId={projectId.Value}&year={year.Value}&month={month.Value}");
        }

        if (string.IsNullOrWhiteSpace(narrative))
        {
            ModelState.AddModelError("Narrative", "Narrative is required.");
            var narrativeForView = await _context.MonthlyUpdateNarratives
                .Include(n => n.ProjectMonthlyUpdate)
                .FirstOrDefaultAsync(n => n.Id == narrativeId.Value);
            
            if (narrativeForView == null)
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
            ViewBag.NarrativeId = narrativeId.Value;
            return View(narrativeForView);
        }

        var existingNarrative = await _context.MonthlyUpdateNarratives
            .Include(n => n.ProjectMonthlyUpdate)
            .FirstOrDefaultAsync(n => n.Id == narrativeId.Value && 
                n.ProjectMonthlyUpdate.ProjectId == projectId.Value &&
                n.ProjectMonthlyUpdate.Year == year.Value &&
                n.ProjectMonthlyUpdate.Month == month.Value);

        if (existingNarrative == null)
        {
            return NotFound();
        }

        existingNarrative.Narrative = narrative.Trim();
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Narrative entry updated successfully.";
        return RedirectToAction("CreateUpdate", new { projectId = projectId.Value, year = year.Value, month = month.Value });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteNarrative(int? projectId, int? year, int? month, int? narrativeId)
    {
        if (!projectId.HasValue || !year.HasValue || !month.HasValue || !narrativeId.HasValue)
        {
            return NotFound();
        }

        // Check if reporting window has closed
        var dueDate = _monthlyUpdateService.GetMonthlyUpdateDueDate(year.Value, month.Value);
        if (DateTime.UtcNow > dueDate)
        {
            TempData["ErrorMessage"] = "The reporting window for this period has closed. You can no longer delete entries.";
            return Redirect($"/MilestonesUpdatesSuccesses/CreateUpdate?projectId={projectId.Value}&year={year.Value}&month={month.Value}");
        }

        var narrative = await _context.MonthlyUpdateNarratives
            .Include(n => n.ProjectMonthlyUpdate)
            .FirstOrDefaultAsync(n => n.Id == narrativeId.Value && 
                n.ProjectMonthlyUpdate.ProjectId == projectId.Value &&
                n.ProjectMonthlyUpdate.Year == year.Value &&
                n.ProjectMonthlyUpdate.Month == month.Value);

        if (narrative == null)
        {
            return NotFound();
        }

        _context.MonthlyUpdateNarratives.Remove(narrative);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Narrative entry deleted successfully.";
        return RedirectToAction("CreateUpdate", new { projectId = projectId.Value, year = year.Value, month = month.Value });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateNarrative(int? projectId, int? year, int? month, string narrative)
    {
        if (!projectId.HasValue || !year.HasValue || !month.HasValue)
        {
            return NotFound();
        }

        // Check if reporting window has closed
        var dueDate = _monthlyUpdateService.GetMonthlyUpdateDueDate(year.Value, month.Value);
        if (DateTime.UtcNow > dueDate)
        {
            TempData["ErrorMessage"] = "The reporting window for this period has closed. You can no longer add entries.";
            return Redirect($"/MilestonesUpdatesSuccesses/CreateUpdate?projectId={projectId.Value}&year={year.Value}&month={month.Value}");
        }

        if (string.IsNullOrWhiteSpace(narrative))
        {
            TempData["ErrorMessage"] = "Narrative is required.";
            return RedirectToAction("CreateUpdate", new { projectId = projectId.Value, year = year.Value, month = month.Value });
        }

        // Get or create the monthly update
        var monthlyUpdate = await _context.ProjectMonthlyUpdates
            .FirstOrDefaultAsync(u => u.ProjectId == projectId.Value && u.Year == year.Value && u.Month == month.Value);

        if (monthlyUpdate == null)
        {
            // Create the monthly update first
            var userObjectIdClaim = User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;
            var userEmailClaim = User.FindFirst(ClaimTypes.Email)?.Value 
                ?? User.FindFirst("preferred_username")?.Value
                ?? User.FindFirst("email")?.Value;
            var userNameClaim = User.FindFirst(ClaimTypes.Name)?.Value 
                ?? User.FindFirst("name")?.Value;

            monthlyUpdate = new ProjectMonthlyUpdate
            {
                ProjectId = projectId.Value,
                Year = year.Value,
                Month = month.Value,
                Narrative = string.Empty, // Legacy field, kept for compatibility
                CreatedByEntraId = userObjectIdClaim,
                CreatedByName = userNameClaim,
                CreatedByEmail = userEmailClaim,
                CreatedAt = DateTime.UtcNow,
                SubmittedAt = null // Only set when explicitly submitted
            };
            _context.ProjectMonthlyUpdates.Add(monthlyUpdate);
            await _context.SaveChangesAsync();
        }

        // Get user information from claims
        var narrativeUserObjectIdClaim = User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;
        var narrativeUserEmailClaim = User.FindFirst(ClaimTypes.Email)?.Value 
            ?? User.FindFirst("preferred_username")?.Value
            ?? User.FindFirst("email")?.Value;
        var narrativeUserNameClaim = User.FindFirst(ClaimTypes.Name)?.Value 
            ?? User.FindFirst("name")?.Value;

        // Create the narrative entry
        var narrativeEntry = new MonthlyUpdateNarrative
        {
            ProjectMonthlyUpdateId = monthlyUpdate.Id,
            Narrative = narrative.Trim(),
            CreatedByEntraId = narrativeUserObjectIdClaim,
            CreatedByName = narrativeUserNameClaim,
            CreatedByEmail = narrativeUserEmailClaim,
            CreatedAt = DateTime.UtcNow
        };

        _context.MonthlyUpdateNarratives.Add(narrativeEntry);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Narrative entry added successfully.";
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

    private async Task<bool> CanUserSubmitMonthlyUpdate(Project project)
    {
        // Get current user email from claims
        var userEmail = User.FindFirst("preferred_username")?.Value
            ?? User.FindFirst(ClaimTypes.Email)?.Value
            ?? User.Identity?.Name;

        if (string.IsNullOrEmpty(userEmail))
        {
            return false;
        }

        // Check if user is Central Ops Admin or Super Admin - they can submit any return
        try
        {
            var isSuperAdmin = await _permissionService.IsSuperAdminAsync(userEmail);
            var isCentralOpsAdmin = await _permissionService.IsInGroupAsync(userEmail, "Central Operations Admin");
            if (isSuperAdmin || isCentralOpsAdmin)
            {
                return true;
            }
        }
        catch
        {
            // If permission check fails, continue with normal checks
        }

        // Get the current user from database
        var currentUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == userEmail.ToLower());

        // Check if user is Primary Contact
        if (currentUser != null && project.PrimaryContactUserId == currentUser.Id)
        {
            return true;
        }

        // Check by email for Primary Contact
        if (project.PrimaryContactUser != null && 
            project.PrimaryContactUser.Email?.ToLower() == userEmail.ToLower())
        {
            return true;
        }

        // Check if user is SRO
        if (currentUser != null && project.SeniorResponsibleOfficers?.Any(sro => sro.UserId == currentUser.Id) == true)
        {
            return true;
        }

        // Check SROs by email
        if (project.SeniorResponsibleOfficers?.Any(sro => 
            sro.User != null && sro.User.Email?.ToLower() == userEmail.ToLower()) == true)
        {
            return true;
        }

        // Check if user is Service Owner
        if (currentUser != null && project.ServiceOwners?.Any(so => so.UserId == currentUser.Id) == true)
        {
            return true;
        }

        // Check Service Owners by email
        if (project.ServiceOwners?.Any(so => 
            so.User != null && so.User.Email?.ToLower() == userEmail.ToLower()) == true)
        {
            return true;
        }

        return false;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitMonthlyUpdate(int? projectId, int? year, int? month, string? ragStatus, string? ragJustification, string? pathToGreen, bool isNilReturn = false)
    {
        if (!projectId.HasValue || !year.HasValue || !month.HasValue)
        {
            return NotFound();
        }

        var project = await _context.Projects
            .Include(p => p.SeniorResponsibleOfficers)
                .ThenInclude(sro => sro.User)
            .Include(p => p.ServiceOwners)
                .ThenInclude(so => so.User)
            .Include(p => p.PrimaryContactUser)
            .Include(p => p.RagHistory)
            .FirstOrDefaultAsync(p => p.Id == projectId.Value && !p.IsDeleted);

        if (project == null)
        {
            return NotFound();
        }

        // Check if user can submit
        if (!await CanUserSubmitMonthlyUpdate(project))
        {
            TempData["ErrorMessage"] = "You do not have permission to submit monthly updates. Only SROs, Service Owners, Primary Contacts, and Central Operations admins can submit.";
            // Determine redirect action based on whether update exists
            var existingUpdateForRedirect = await _context.ProjectMonthlyUpdates
                .FirstOrDefaultAsync(u => u.ProjectId == projectId.Value && u.Year == year.Value && u.Month == month.Value);
            var redirectActionForError = existingUpdateForRedirect != null ? "EditUpdate" : "CreateUpdate";
            return RedirectToAction(redirectActionForError, new { projectId = projectId.Value, year = year.Value, month = month.Value });
        }

        // Get or create the monthly update first to determine redirect action
        var monthlyUpdate = await _context.ProjectMonthlyUpdates
            .FirstOrDefaultAsync(u => u.ProjectId == projectId.Value && u.Year == year.Value && u.Month == month.Value);

        // Determine redirect action based on whether update exists
        var redirectAction = monthlyUpdate != null ? "EditUpdate" : "CreateUpdate";

        // Validate RAG status
        if (string.IsNullOrWhiteSpace(ragStatus))
        {
            TempData["ErrorMessage"] = "RAG status is required before submitting the monthly update.";
            return RedirectToAction(redirectAction, new { projectId = projectId.Value, year = year.Value, month = month.Value });
        }

        var oldRagStatus = project.RagStatus;
        var ragChanged = oldRagStatus != ragStatus;
        var isNotGreen = ragStatus != "Green";

        // Validate justification if RAG changed or not green
        if ((ragChanged || isNotGreen) && string.IsNullOrWhiteSpace(ragJustification))
        {
            TempData["ErrorMessage"] = "RAG justification is required when RAG status has changed or is not Green.";
            return RedirectToAction(redirectAction, new { projectId = projectId.Value, year = year.Value, month = month.Value });
        }

        // Validate path to green if not green
        if (isNotGreen && string.IsNullOrWhiteSpace(pathToGreen))
        {
            TempData["ErrorMessage"] = "Path to Green is required when RAG status is not Green.";
            return RedirectToAction(redirectAction, new { projectId = projectId.Value, year = year.Value, month = month.Value });
        }

        // Get or create monthly update
        if (monthlyUpdate == null)
        {
            monthlyUpdate = new ProjectMonthlyUpdate
            {
                ProjectId = projectId.Value,
                Year = year.Value,
                Month = month.Value,
                CreatedAt = DateTime.UtcNow
            };
            _context.ProjectMonthlyUpdates.Add(monthlyUpdate);
            await _context.SaveChangesAsync();
        }

        // Check if there are any narratives
        var narrativeCount = await _context.MonthlyUpdateNarratives
            .CountAsync(n => n.ProjectMonthlyUpdateId == monthlyUpdate.Id);

        // Handle nil return
        if (isNilReturn && narrativeCount == 0)
        {
            // Create a nil return narrative
            var nilReturnNarrative = new MonthlyUpdateNarrative
            {
                ProjectMonthlyUpdateId = monthlyUpdate.Id,
                Narrative = "No update provided this month",
                CreatedAt = DateTime.UtcNow,
                CreatedByName = User.Identity?.Name ?? "Unknown",
                CreatedByEmail = User.Identity?.Name ?? "Unknown"
            };
            _context.MonthlyUpdateNarratives.Add(nilReturnNarrative);
            await _context.SaveChangesAsync();
            narrativeCount = 1;
        }
        else if (!isNilReturn && narrativeCount == 0)
        {
            TempData["ErrorMessage"] = "Cannot submit monthly update with no entries. Please add at least one update entry before submitting, or submit a nil return.";
            return RedirectToAction(redirectAction, new { projectId = projectId.Value, year = year.Value, month = month.Value });
        }

        // Check if reporting window has closed
        var dueDate = _monthlyUpdateService.GetMonthlyUpdateDueDate(year.Value, month.Value);
        if (DateTime.UtcNow > dueDate)
        {
            TempData["ErrorMessage"] = "The reporting window for this period has closed. You can no longer submit monthly updates.";
            return RedirectToAction(redirectAction, new { projectId = projectId.Value, year = year.Value, month = month.Value });
        }

        // Update RAG status if changed
        if (ragChanged)
        {
            // Create RAG history entry
            var ragHistory = new ProjectRagHistory
            {
                ProjectId = projectId.Value,
                RagStatus = ragStatus,
                Justification = ragJustification,
                PathToGreen = pathToGreen,
                ChangedAt = DateTime.UtcNow,
                ChangedByEmail = User.Identity?.Name ?? "Unknown",
                ChangedByName = User.Identity?.Name ?? "Unknown"
            };
            _context.ProjectRagHistories.Add(ragHistory);
        }

        // Update project RAG status
        project.RagStatus = ragStatus;
        project.RagJustification = ragJustification;
        if (isNotGreen)
        {
            project.PathToGreen = pathToGreen;
        }
        else
        {
            project.PathToGreen = null; // Clear path to green if status is Green
        }
        project.UpdatedAt = DateTime.UtcNow;

        // Set submitted date if not already set
        if (!monthlyUpdate.SubmittedAt.HasValue)
        {
            monthlyUpdate.SubmittedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            
            // Note: RAG status change notifications are handled by the ProjectController.UpdateRagStatus method
            // when the RAG status is updated through the project details page. Since we're updating RAG here
            // as part of monthly update submission, we'll skip the notification to avoid duplicate notifications.
            
            TempData["SuccessMessage"] = "Monthly update submitted successfully.";
        }
        else
        {
            await _context.SaveChangesAsync();
            TempData["InfoMessage"] = "This monthly update has already been submitted.";
        }

        return RedirectToAction(redirectAction, new { projectId = projectId.Value, year = year.Value, month = month.Value });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnsubmitMonthlyUpdate(int? projectId, int? year, int? month)
    {
        if (!projectId.HasValue || !year.HasValue || !month.HasValue)
        {
            return NotFound();
        }

        var project = await _context.Projects
            .Include(p => p.SeniorResponsibleOfficers)
                .ThenInclude(sro => sro.User)
            .Include(p => p.ServiceOwners)
                .ThenInclude(so => so.User)
            .Include(p => p.PrimaryContactUser)
            .FirstOrDefaultAsync(p => p.Id == projectId.Value && !p.IsDeleted);

        if (project == null)
        {
            return NotFound();
        }

        // Check if user can submit/unsubmit
        if (!await CanUserSubmitMonthlyUpdate(project))
        {
            TempData["ErrorMessage"] = "You do not have permission to unsubmit monthly updates. Only SROs, Service Owners, and Primary Contacts can unsubmit.";
            return RedirectToAction("CreateUpdate", new { projectId = projectId.Value, year = year.Value, month = month.Value });
        }

        // Check if reporting window has closed
        var dueDate = _monthlyUpdateService.GetMonthlyUpdateDueDate(year.Value, month.Value);
        if (DateTime.UtcNow > dueDate)
        {
            TempData["ErrorMessage"] = "The reporting window for this period has closed. You can no longer unsubmit monthly updates.";
            return RedirectToAction("CreateUpdate", new { projectId = projectId.Value, year = year.Value, month = month.Value });
        }

        // Get the monthly update
        var monthlyUpdate = await _context.ProjectMonthlyUpdates
            .FirstOrDefaultAsync(u => u.ProjectId == projectId.Value && u.Year == year.Value && u.Month == month.Value);

        if (monthlyUpdate == null)
        {
            TempData["ErrorMessage"] = "No monthly update found.";
            return RedirectToAction("CreateUpdate", new { projectId = projectId.Value, year = year.Value, month = month.Value });
        }

        // Only allow unsubmit if it's currently submitted
        if (!monthlyUpdate.SubmittedAt.HasValue)
        {
            TempData["ErrorMessage"] = "This monthly update is not currently submitted.";
            return RedirectToAction("CreateUpdate", new { projectId = projectId.Value, year = year.Value, month = month.Value });
        }

        // Unsubmit by clearing the submitted date
        monthlyUpdate.SubmittedAt = null;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Monthly update unsubmitted successfully. You can now make changes and resubmit.";
        return RedirectToAction("CreateUpdate", new { projectId = projectId.Value, year = year.Value, month = month.Value });
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

        // Redirect to the existing CreateSuccess action in ProjectController
        // which uses ProjectSuccess model with IsReportedToSlt flag
        return RedirectToAction("CreateSuccess", "Project", new { projectId = projectId.Value });
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

