using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Compass.Data;
using Compass.Models;
using Compass.ViewModels;
using Compass.Services;
using System.Security.Claims;

namespace Compass.Controllers;

[Authorize]
public class TasksController : Controller
{
    private readonly CompassDbContext _context;
    private readonly ILogger<TasksController> _logger;
    private readonly IMonthlyUpdateService _monthlyUpdateService;

    public TasksController(CompassDbContext context, ILogger<TasksController> logger, IMonthlyUpdateService monthlyUpdateService)
    {
        _context = context;
        _logger = logger;
        _monthlyUpdateService = monthlyUpdateService;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null)
            {
                TempData["ErrorMessage"] = "Unable to identify the current user.";
                return View(new TasksViewModel());
            }

            var userEmail = User.Identity?.Name;
            if (string.IsNullOrEmpty(userEmail))
            {
                TempData["ErrorMessage"] = "Unable to identify the current user.";
                return View(new TasksViewModel());
            }

            // Get projects where user is owner, SRO, or service owner
            var userProjects = await _context.Projects
                .Where(p => !p.IsDeleted && (
                    p.PrimaryContactUserId == currentUser.Id ||
                    p.SeniorResponsibleOfficers.Any(sro => sro.UserId == currentUser.Id) ||
                    p.ServiceOwners.Any(so => so.UserId == currentUser.Id)
                ))
                .Include(p => p.ProjectContacts)
                .Include(p => p.SeniorResponsibleOfficers)
                .Include(p => p.ServiceOwners)
                .Include(p => p.Directorates)
                .Include(p => p.BudgetOwners)
                .Include(p => p.PrimaryContactUser)
                .Include(p => p.MonthlyUpdates)
                .AsNoTracking()
                .ToListAsync();

            var tasks = new List<ProjectTask>();

            foreach (var project in userProjects)
            {
                var deliveryCode = $"DFE-DDT-{project.Id}";
                var projectOverviewUrl = Url.Action("Details", "Project", new { id = project.Id, tab = "overview" }) ?? "#";
                
                // Check: At least 1 team member added
                var currentTeamMembers = project.ProjectContacts
                    .Where(pc => string.IsNullOrEmpty(pc.TeamStatus) || 
                                 pc.TeamStatus.ToLower() == "current")
                    .ToList();
                if (!currentTeamMembers.Any())
                {
                    tasks.Add(new ProjectTask
                    {
                        ProjectId = project.Id,
                        ProjectTitle = project.Title,
                        ProjectCode = deliveryCode,
                        TaskDescription = "Add at least 1 team member",
                        ActionUrl = Url.Action("Details", "Project", new { id = project.Id, tab = "team" }) ?? "#",
                        ProjectOverviewUrl = projectOverviewUrl
                    });
                }

                // Check: The SRO is added
                if (!project.SeniorResponsibleOfficers.Any())
                {
                    tasks.Add(new ProjectTask
                    {
                        ProjectId = project.Id,
                        ProjectTitle = project.Title,
                        ProjectCode = deliveryCode,
                        TaskDescription = "Add the SRO",
                        ActionUrl = Url.Action("Details", "Project", new { id = project.Id, tab = "overview" }) ?? "#",
                        ProjectOverviewUrl = projectOverviewUrl
                    });
                }

                // Check: At least 1 directorate added
                if (!project.Directorates.Any())
                {
                    tasks.Add(new ProjectTask
                    {
                        ProjectId = project.Id,
                        ProjectTitle = project.Title,
                        ProjectCode = deliveryCode,
                        TaskDescription = "Add at least 1 directorate",
                        ActionUrl = Url.Action("Details", "Project", new { id = project.Id, tab = "overview" }) ?? "#",
                        ProjectOverviewUrl = projectOverviewUrl
                    });
                }

                // Check: Activity type is set
                if (project.ActivityTypeLookupId == null)
                {
                    tasks.Add(new ProjectTask
                    {
                        ProjectId = project.Id,
                        ProjectTitle = project.Title,
                        ProjectCode = deliveryCode,
                        TaskDescription = "Set the activity type",
                        ActionUrl = Url.Action("Details", "Project", new { id = project.Id, tab = "overview" }) ?? "#",
                        ProjectOverviewUrl = projectOverviewUrl
                    });
                }

                // Check: RAG Status is set
                if (string.IsNullOrWhiteSpace(project.RagStatus))
                {
                    tasks.Add(new ProjectTask
                    {
                        ProjectId = project.Id,
                        ProjectTitle = project.Title,
                        ProjectCode = deliveryCode,
                        TaskDescription = "Set the RAG status",
                        ActionUrl = Url.Action("Details", "Project", new { id = project.Id, tab = "rag" }) ?? "#",
                        ProjectOverviewUrl = projectOverviewUrl
                    });
                }

                // Check: Business area is set
                if (project.BusinessAreaId == null)
                {
                    tasks.Add(new ProjectTask
                    {
                        ProjectId = project.Id,
                        ProjectTitle = project.Title,
                        ProjectCode = deliveryCode,
                        TaskDescription = "Set the business area",
                        ActionUrl = Url.Action("Details", "Project", new { id = project.Id, tab = "overview" }) ?? "#",
                        ProjectOverviewUrl = projectOverviewUrl
                    });
                }

                // Check: Aim is set
                if (string.IsNullOrWhiteSpace(project.Aim))
                {
                    tasks.Add(new ProjectTask
                    {
                        ProjectId = project.Id,
                        ProjectTitle = project.Title,
                        ProjectCode = deliveryCode,
                        TaskDescription = "Set the aim",
                        ActionUrl = Url.Action("Details", "Project", new { id = project.Id, tab = "overview" }) ?? "#",
                        ProjectOverviewUrl = projectOverviewUrl
                    });
                }

                // Check: At least 1 primary contact
                if (project.PrimaryContactUserId == null)
                {
                    tasks.Add(new ProjectTask
                    {
                        ProjectId = project.Id,
                        ProjectTitle = project.Title,
                        ProjectCode = deliveryCode,
                        TaskDescription = "Add at least 1 primary contact",
                        ActionUrl = Url.Action("Details", "Project", new { id = project.Id, tab = "overview" }) ?? "#",
                        ProjectOverviewUrl = projectOverviewUrl
                    });
                }

                // Check: Budget owner is set
                if (!project.BudgetOwners.Any())
                {
                    tasks.Add(new ProjectTask
                    {
                        ProjectId = project.Id,
                        ProjectTitle = project.Title,
                        ProjectCode = deliveryCode,
                        TaskDescription = "Set the budget owner",
                        ActionUrl = Url.Action("Details", "Project", new { id = project.Id, tab = "overview" }) ?? "#",
                        ProjectOverviewUrl = projectOverviewUrl
                    });
                }
            }

            // Check for outstanding monthly updates (due or late) for current and previous months
            var now = DateTime.UtcNow;
            var currentYear = now.Year;
            var currentMonth = now.Month;
            
            // Calculate previous month
            var previousMonth = currentMonth == 1 ? 12 : currentMonth - 1;
            var previousYear = currentMonth == 1 ? currentYear - 1 : currentYear;
            
            // Check both current and previous months
            var monthsToCheck = new[] { 
                (Year: currentYear, Month: currentMonth),
                (Year: previousYear, Month: previousMonth)
            };

            foreach (var project in userProjects)
            {
                var deliveryCode = $"DFE-DDT-{project.Id}";
                var projectOverviewUrl = Url.Action("Details", "Project", new { id = project.Id, tab = "overview" }) ?? "#";

                foreach (var (year, month) in monthsToCheck)
                {
                    var update = project.MonthlyUpdates?.FirstOrDefault(u => u.Year == year && u.Month == month);
                    var status = _monthlyUpdateService.CalculateUpdateStatus(year, month, update?.SubmittedAt);
                    
                    // Add task if update is due or late
                    if (status == UpdateSubmissionStatus.Due || status == UpdateSubmissionStatus.Late)
                    {
                        var periodName = new DateTime(year, month, 1).ToString("MMMM yyyy");
                        var actionUrl = update != null
                            ? Url.Action("EditUpdate", "MilestonesUpdatesSuccesses", new { projectId = project.Id, year = year, month = month }) ?? "#"
                            : Url.Action("CreateUpdate", "MilestonesUpdatesSuccesses", new { projectId = project.Id, year = year, month = month }) ?? "#";
                        
                        var taskDescription = status == UpdateSubmissionStatus.Late
                            ? $"Submit monthly update for {periodName} (late)"
                            : $"Submit monthly update for {periodName} (due)";

                        tasks.Add(new ProjectTask
                        {
                            ProjectId = project.Id,
                            ProjectTitle = project.Title,
                            ProjectCode = deliveryCode,
                            TaskDescription = taskDescription,
                            ActionUrl = actionUrl,
                            ProjectOverviewUrl = projectOverviewUrl
                        });
                    }
                }
            }

            var viewModel = new TasksViewModel
            {
                Tasks = tasks.OrderBy(t => t.ProjectTitle).ThenBy(t => t.TaskDescription).ToList()
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading tasks");
            TempData["ErrorMessage"] = "An error occurred while loading tasks. Please try again.";
            return View(new TasksViewModel());
        }
    }

    private async Task<User?> GetCurrentUserAsync()
    {
        var userObjectIdClaim = User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;
        if (string.IsNullOrWhiteSpace(userObjectIdClaim) || !Guid.TryParse(userObjectIdClaim, out var objectId))
        {
            // Fallback to email lookup
            var userEmail = User.Identity?.Name;
            if (!string.IsNullOrWhiteSpace(userEmail))
            {
                return await _context.Users
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == userEmail.ToLower());
            }
            return null;
        }

        return await _context.Users.FirstOrDefaultAsync(u => u.AzureObjectId == objectId.ToString());
    }
}

