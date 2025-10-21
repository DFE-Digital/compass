using System.ComponentModel.DataAnnotations;
using Compass.Attributes;
using Compass.Data;
using Compass.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Compass.Controllers.Api.V1;

[ApiController]
[Route("api/v1/[controller]")]
public class IssuesController : ControllerBase
{
    private readonly CompassDbContext _context;
    private readonly ILogger<IssuesController> _logger;

    public IssuesController(CompassDbContext context, ILogger<IssuesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    [RequireApiPermission("Issues", "read")]
    public async Task<IActionResult> GetIssues(
        [FromQuery] string? fipsId = null,
        [FromQuery] string? status = null,
        [FromQuery] string? severity = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        if (pageSize > 100) pageSize = 100;
        if (page < 1) page = 1;

        var query = _context.Issues
            .Where(i => !i.IsDeleted);

        if (!string.IsNullOrEmpty(fipsId))
        {
            query = query.Where(i => i.FipsId == fipsId);
        }

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(i => i.Status == status);
        }

        if (!string.IsNullOrEmpty(severity))
        {
            query = query.Where(i => i.Severity == severity);
        }

        var totalRecords = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

        var issues = await query
            .OrderByDescending(i => i.DetectedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(i => new
            {
                i.Id,
                i.Title,
                i.Description,
                i.FipsId,
                i.ObjectiveId,
                i.Severity,
                i.Priority,
                i.Status,
                i.DetectedDate,
                i.TargetResolutionDate,
                i.BlockedFlag,
                i.Category,
                i.BusinessArea,
                i.CreatedAt,
                i.UpdatedAt
            })
            .ToListAsync();

        return Ok(new
        {
            data = issues,
            pagination = new
            {
                currentPage = page,
                pageSize,
                totalPages,
                totalRecords
            }
        });
    }

    [HttpGet("{id}")]
    [RequireApiPermission("Issues", "read")]
    public async Task<IActionResult> GetIssue(int id)
    {
        var issue = await _context.Issues
            .Include(i => i.IssueActions)
                .ThenInclude(ia => ia.Action)
            .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted);

        if (issue == null)
        {
            return NotFound(new
            {
                error = new
                {
                    code = "NOT_FOUND",
                    message = $"Issue with ID {id} not found"
                }
            });
        }

        return Ok(new
        {
            issue.Id,
            issue.Title,
            issue.Description,
            issue.FipsId,
            issue.ObjectiveId,
            issue.Severity,
            issue.Priority,
            issue.Status,
            issue.DetectedDate,
            issue.TargetResolutionDate,
            issue.ResolutionSummary,
            issue.Workaround,
            issue.BlockedFlag,
            issue.Category,
            issue.BusinessArea,
            issue.ClosedDate,
            Actions = issue.IssueActions.Select(ia => new
            {
                ia.Action.Id,
                ia.Action.Title,
                ia.Action.Status,
                ia.Action.DueDate
            }).ToList(),
            issue.CreatedAt,
            issue.UpdatedAt
        });
    }

    [HttpPost]
    [RequireApiPermission("Issues", "create")]
    public async Task<IActionResult> CreateIssue([FromBody] IssueCreateDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new
            {
                error = new
                {
                    code = "VALIDATION_ERROR",
                    message = "Invalid request data",
                    details = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                }
            });
        }

        var issue = new Issue
        {
            Title = dto.Title,
            Description = dto.Description,
            FipsId = dto.FipsId,
            ObjectiveId = dto.ObjectiveId,
            Severity = dto.Severity,
            Priority = dto.Priority,
            Status = dto.Status ?? "open",
            DetectedDate = dto.DetectedDate ?? DateTime.UtcNow,
            TargetResolutionDate = dto.TargetResolutionDate,
            Category = dto.Category,
            BusinessArea = dto.BusinessArea,
            Workaround = dto.Workaround,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Issues.Add(issue);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetIssue), new { id = issue.Id }, new
        {
            issue.Id,
            issue.Title,
            issue.Status,
            issue.Severity,
            issue.CreatedAt
        });
    }

    [HttpPut("{id}")]
    [RequireApiPermission("Issues", "update")]
    public async Task<IActionResult> UpdateIssue(int id, [FromBody] IssueUpdateDto dto)
    {
        var issue = await _context.Issues.FindAsync(id);
        if (issue == null || issue.IsDeleted)
        {
            return NotFound(new
            {
                error = new
                {
                    code = "NOT_FOUND",
                    message = $"Issue with ID {id} not found"
                }
            });
        }

        if (dto.Title != null) issue.Title = dto.Title;
        if (dto.Description != null) issue.Description = dto.Description;
        if (dto.Severity != null) issue.Severity = dto.Severity;
        if (dto.Priority != null) issue.Priority = dto.Priority;
        if (dto.Status != null) issue.Status = dto.Status;
        if (dto.ResolutionSummary != null) issue.ResolutionSummary = dto.ResolutionSummary;
        if (dto.Workaround != null) issue.Workaround = dto.Workaround;

        issue.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new { message = "Issue updated successfully" });
    }

    [HttpDelete("{id}")]
    [RequireApiPermission("Issues", "delete")]
    public async Task<IActionResult> DeleteIssue(int id)
    {
        var issue = await _context.Issues.FindAsync(id);
        if (issue == null || issue.IsDeleted)
        {
            return NotFound(new
            {
                error = new
                {
                    code = "NOT_FOUND",
                    message = $"Issue with ID {id} not found"
                }
            });
        }

        issue.IsDeleted = true;
        issue.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

public class IssueCreateDto
{
    [Required]
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? FipsId { get; set; }
    public int? ObjectiveId { get; set; }
    [Required]
    public string Severity { get; set; } = "medium";
    public string? Priority { get; set; }
    public string? Status { get; set; }
    public DateTime? DetectedDate { get; set; }
    public DateTime? TargetResolutionDate { get; set; }
    public string? Category { get; set; }
    public string? BusinessArea { get; set; }
    public string? Workaround { get; set; }
}

public class IssueUpdateDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Severity { get; set; }
    public string? Priority { get; set; }
    public string? Status { get; set; }
    public string? ResolutionSummary { get; set; }
    public string? Workaround { get; set; }
}

