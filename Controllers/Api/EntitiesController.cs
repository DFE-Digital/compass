using System.Text.RegularExpressions;
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
                "Risk" => await SearchRisks(searchTerm),
                "RiskOrIssue" => await SearchRisksAndIssues(searchTerm),
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
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return new List<object>();
        }

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

    private async Task<List<object>> SearchRisks(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return new List<object>();

        var likeTerm = searchTerm.Trim();
        if (TryParseEntityReference(likeTerm, out var refType, out var refId) && refType == "Risk")
        {
            var byRef = await FindRiskSearchResultAsync(refId);
            return byRef != null ? [byRef] : [];
        }

        var risks = await _context.Risks
            .AsNoTracking()
            .Include(r => r.Project)
            .Where(r => !r.IsDeleted &&
                   ((r.Title != null && r.Title.Contains(likeTerm)) ||
                    (r.FipsId != null && r.FipsId.Contains(likeTerm))))
            .OrderBy(r => r.Title)
            .Take(20)
            .Select(r => new
            {
                id = r.Id,
                title = r.Title,
                metadata = r.Project != null ? r.Project.Title + " (" + r.Project.ProjectCode + ")" : "No project",
                reference = "R-" + r.Id.ToString("D4"),
                entityType = "Risk"
            })
            .ToListAsync();

        return risks.Cast<object>().ToList();
    }

    private async Task<List<object>> SearchRisksAndIssues(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return new List<object>();

        var likeTerm = searchTerm.Trim();

        if (TryParseEntityReference(likeTerm, out var refType, out var refId))
        {
            if (refType == "Risk")
            {
                var risk = await FindRiskSearchResultAsync(refId);
                return risk != null ? [risk] : [];
            }

            var issue = await FindIssueSearchResultAsync(refId);
            return issue != null ? [issue] : [];
        }

        if (int.TryParse(likeTerm, out var numericId) && numericId > 0)
        {
            var byId = new List<object>();
            var risk = await FindRiskSearchResultAsync(numericId);
            if (risk != null) byId.Add(risk);
            var issue = await FindIssueSearchResultAsync(numericId);
            if (issue != null) byId.Add(issue);
            if (byId.Count > 0)
                return byId;
        }

        var risks = await _context.Risks
            .AsNoTracking()
            .Include(r => r.Project)
            .Where(r => !r.IsDeleted &&
                   ((r.Title != null && r.Title.Contains(likeTerm)) ||
                    (r.FipsId != null && r.FipsId.Contains(likeTerm))))
            .OrderBy(r => r.Title)
            .Take(10)
            .Select(r => new
            {
                id = r.Id,
                title = r.Title,
                metadata = r.Project != null ? r.Project.Title + " (" + r.Project.ProjectCode + ")" : "No project",
                reference = "R-" + r.Id.ToString("D4"),
                entityType = "Risk"
            })
            .ToListAsync();

        var issues = await _context.Issues
            .AsNoTracking()
            .Include(i => i.Project)
            .Where(i => !i.IsDeleted &&
                   ((i.Title != null && i.Title.Contains(likeTerm)) ||
                    (i.FipsId != null && i.FipsId.Contains(likeTerm))))
            .OrderBy(i => i.Title)
            .Take(10)
            .Select(i => new
            {
                id = i.Id,
                title = i.Title,
                metadata = i.Project != null ? i.Project.Title + " (" + i.Project.ProjectCode + ")" : "No project",
                reference = "I-" + i.Id.ToString("D4"),
                entityType = "Issue"
            })
            .ToListAsync();

        var combined = new List<object>();
        combined.AddRange(risks.Cast<object>());
        combined.AddRange(issues.Cast<object>());
        return combined;
    }

  private static bool TryParseEntityReference(string term, out string entityType, out int id)
    {
        entityType = "";
        id = 0;
        var match = Regex.Match(term.Trim(), @"^(?i)(R|I)-?(\d+)$");
        if (!match.Success)
            return false;

        entityType = match.Groups[1].Value.Equals("R", StringComparison.OrdinalIgnoreCase) ? "Risk" : "Issue";
        id = int.Parse(match.Groups[2].Value);
        return true;
    }

    private async Task<object?> FindRiskSearchResultAsync(int id)
    {
        return await _context.Risks
            .AsNoTracking()
            .Include(r => r.Project)
            .Where(r => !r.IsDeleted && r.Id == id)
            .Select(r => new
            {
                id = r.Id,
                title = r.Title,
                metadata = r.Project != null ? r.Project.Title + " (" + r.Project.ProjectCode + ")" : "No project",
                reference = "R-" + r.Id.ToString("D4"),
                entityType = "Risk"
            })
            .FirstOrDefaultAsync();
    }

    private async Task<object?> FindIssueSearchResultAsync(int id)
    {
        return await _context.Issues
            .AsNoTracking()
            .Include(i => i.Project)
            .Where(i => !i.IsDeleted && i.Id == id)
            .Select(i => new
            {
                id = i.Id,
                title = i.Title,
                metadata = i.Project != null ? i.Project.Title + " (" + i.Project.ProjectCode + ")" : "No project",
                reference = "I-" + i.Id.ToString("D4"),
                entityType = "Issue"
            })
            .FirstOrDefaultAsync();
    }
}

