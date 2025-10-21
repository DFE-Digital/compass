using System.ComponentModel.DataAnnotations;
using Compass.Attributes;
using Compass.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Action = Compass.Models.Action;

namespace Compass.Controllers.Api.V1;

[ApiController]
[Route("api/v1/[controller]")]
public class ActionsController : ControllerBase
{
    private readonly CompassDbContext _context;
    private readonly ILogger<ActionsController> _logger;

    public ActionsController(CompassDbContext context, ILogger<ActionsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    [RequireApiPermission("Actions", "read")]
    public async Task<IActionResult> GetActions(
        [FromQuery] string? fipsId = null,
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        if (pageSize > 100) pageSize = 100;
        if (page < 1) page = 1;

        var query = _context.Actions
            .Include(a => a.ActionSource)
            .Where(a => !a.IsDeleted);

        if (!string.IsNullOrEmpty(fipsId))
        {
            query = query.Where(a => a.FipsId == fipsId);
        }

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(a => a.Status == status);
        }

        var totalRecords = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

        var actions = await query
            .OrderBy(a => a.DueDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new
            {
                a.Id,
                a.Title,
                a.Description,
                a.FipsId,
                a.ObjectiveId,
                ActionSource = a.ActionSource != null ? new { a.ActionSource.Id, a.ActionSource.Name } : null,
                a.Priority,
                a.Status,
                a.StartDate,
                a.DueDate,
                a.CompletedDate,
                a.BusinessArea,
                a.CreatedAt,
                a.UpdatedAt
            })
            .ToListAsync();

        return Ok(new
        {
            data = actions,
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
    [RequireApiPermission("Actions", "read")]
    public async Task<IActionResult> GetAction(int id)
    {
        var action = await _context.Actions
            .Include(a => a.ActionSource)
            .Include(a => a.ParentAction)
            .Include(a => a.SubActions)
            .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);

        if (action == null)
        {
            return NotFound(new
            {
                error = new
                {
                    code = "NOT_FOUND",
                    message = $"Action with ID {id} not found"
                }
            });
        }

        return Ok(new
        {
            action.Id,
            action.Title,
            action.Description,
            action.FipsId,
            action.ObjectiveId,
            ActionSource = action.ActionSource != null ? new { action.ActionSource.Id, action.ActionSource.Name } : null,
            action.Priority,
            action.Status,
            action.StartDate,
            action.DueDate,
            action.CompletedDate,
            action.ParentActionId,
            ParentAction = action.ParentAction != null ? new { action.ParentAction.Id, action.ParentAction.Title } : null,
            SubActions = action.SubActions.Select(sa => new { sa.Id, sa.Title, sa.Status }).ToList(),
            action.EvidenceUrl,
            action.Notes,
            action.BusinessArea,
            action.CreatedAt,
            action.UpdatedAt
        });
    }

    [HttpPost]
    [RequireApiPermission("Actions", "create")]
    public async Task<IActionResult> CreateAction([FromBody] ActionCreateDto dto)
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

        var action = new Action
        {
            Title = dto.Title,
            Description = dto.Description,
            FipsId = dto.FipsId,
            ObjectiveId = dto.ObjectiveId,
            ActionSourceId = dto.ActionSourceId,
            Priority = dto.Priority,
            Status = dto.Status ?? "not_started",
            StartDate = dto.StartDate,
            DueDate = dto.DueDate,
            BusinessArea = dto.BusinessArea,
            ParentActionId = dto.ParentActionId,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Actions.Add(action);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAction), new { id = action.Id }, new
        {
            action.Id,
            action.Title,
            action.Status,
            action.CreatedAt
        });
    }

    [HttpPut("{id}")]
    [RequireApiPermission("Actions", "update")]
    public async Task<IActionResult> UpdateAction(int id, [FromBody] ActionUpdateDto dto)
    {
        var action = await _context.Actions.FindAsync(id);
        if (action == null || action.IsDeleted)
        {
            return NotFound(new
            {
                error = new
                {
                    code = "NOT_FOUND",
                    message = $"Action with ID {id} not found"
                }
            });
        }

        if (dto.Title != null) action.Title = dto.Title;
        if (dto.Description != null) action.Description = dto.Description;
        if (dto.Priority != null) action.Priority = dto.Priority;
        if (dto.Status != null)
        {
            action.Status = dto.Status;
            if (dto.Status == "done" && !action.CompletedDate.HasValue)
            {
                action.CompletedDate = DateTime.UtcNow;
            }
        }
        if (dto.DueDate.HasValue) action.DueDate = dto.DueDate;
        if (dto.CompletedDate.HasValue) action.CompletedDate = dto.CompletedDate;
        if (dto.Notes != null) action.Notes = dto.Notes;
        if (dto.EvidenceUrl != null) action.EvidenceUrl = dto.EvidenceUrl;

        action.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new { message = "Action updated successfully" });
    }

    [HttpDelete("{id}")]
    [RequireApiPermission("Actions", "delete")]
    public async Task<IActionResult> DeleteAction(int id)
    {
        var action = await _context.Actions.FindAsync(id);
        if (action == null || action.IsDeleted)
        {
            return NotFound(new
            {
                error = new
                {
                    code = "NOT_FOUND",
                    message = $"Action with ID {id} not found"
                }
            });
        }

        action.IsDeleted = true;
        action.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

public class ActionCreateDto
{
    [Required]
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? FipsId { get; set; }
    public int? ObjectiveId { get; set; }
    public int? ActionSourceId { get; set; }
    public string? Priority { get; set; }
    public string? Status { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? DueDate { get; set; }
    public int? ParentActionId { get; set; }
    public string? BusinessArea { get; set; }
    public string? Notes { get; set; }
}

public class ActionUpdateDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Priority { get; set; }
    public string? Status { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public string? Notes { get; set; }
    public string? EvidenceUrl { get; set; }
}

