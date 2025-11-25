using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Compass.Data;
using Compass.Models;
using Compass.ViewModels;
using System.Security.Claims;

namespace Compass.Controllers;

[Authorize]
public class TasksController : Controller
{
    private readonly CompassDbContext _context;
    private readonly ILogger<TasksController> _logger;

    public TasksController(CompassDbContext context, ILogger<TasksController> logger)
    {
        _context = context;
        _logger = logger;
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
                .AsNoTracking()
                .ToListAsync();

            var tasks = new List<ProjectTask>();

            foreach (var project in userProjects)
            {
                var deliveryCode = $"DEL-DDT-{project.Id}";
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

