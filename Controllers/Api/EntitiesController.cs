using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Compass.Data;

namespace Compass.Controllers.Api;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class EntitiesController : ControllerBase
{
    private readonly CompassDbContext _context;
    private readonly ILogger<EntitiesController> _logger;

    public EntitiesController(CompassDbContext context, ILogger<EntitiesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string entityType, [FromQuery] string q = "")
    {
        try
        {
            var searchTerm = q ?? "";
            _logger.LogInformation("Searching {EntityType} with term: {SearchTerm}", entityType, searchTerm);

            object results = entityType switch
            {
                "Project" => await SearchProjects(searchTerm),
                "Milestone" => await SearchMilestones(searchTerm),
                "Issue" => await SearchIssues(searchTerm),
                _ => new List<object>()
            };

            return Ok(new { results });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching entities");
            return StatusCode(500, new { error = "An error occurred while searching entities" });
        }
    }

    private async Task<List<object>> SearchProjects(string searchTerm)
    {
        var projects = await _context.Projects
            .Where(p => !p.IsDeleted && 
                   ((p.Title != null && p.Title.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)) ||
                    (p.ProjectCode != null && p.ProjectCode.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))))
            .OrderBy(p => p.Title)
            .Take(20)
            .Select(p => new
            {
                id = p.Id,
                title = p.Title,
                metadata = p.ProjectCode
            })
            .ToListAsync();

        return projects.Cast<object>().ToList();
    }

    private async Task<List<object>> SearchMilestones(string searchTerm)
    {
        var milestones = await _context.Milestones
            .Include(m => m.Project)
            .Where(m => !m.IsDeleted && 
                   m.Name != null && m.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
            .OrderBy(m => m.Name)
            .Take(20)
            .Select(m => new
            {
                id = m.Id,
                title = m.Name,
                metadata = m.Project != null ? m.Project.Title + " (" + m.Project.ProjectCode + ")" : "Unknown Project"
            })
            .ToListAsync();

        return milestones.Cast<object>().ToList();
    }

    private async Task<List<object>> SearchIssues(string searchTerm)
    {
        var issues = await _context.Issues
            .Include(i => i.Project)
            .Where(i => !i.IsDeleted && 
                   i.Title != null && i.Title.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
            .OrderBy(i => i.Title)
            .Take(20)
            .Select(i => new
            {
                id = i.Id,
                title = i.Title,
                metadata = i.Project != null ? i.Project.Title + " (" + i.Project.ProjectCode + ")" : "Unknown Project"
            })
            .ToListAsync();

        return issues.Cast<object>().ToList();
    }
}

