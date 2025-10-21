using System.ComponentModel.DataAnnotations;
using Compass.Attributes;
using Compass.Data;
using Compass.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Compass.Controllers.Api.V1;

[ApiController]
[Route("api/v1/[controller]")]
public class MilestonesController : ControllerBase
{
    private readonly CompassDbContext _context;
    private readonly ILogger<MilestonesController> _logger;

    public MilestonesController(CompassDbContext context, ILogger<MilestonesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    [RequireApiPermission("Milestones", "read")]
    public async Task<IActionResult> GetMilestones(
        [FromQuery] string? fipsId = null,
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        if (pageSize > 100) pageSize = 100;
        if (page < 1) page = 1;

        var query = _context.Milestones
            .Where(m => !m.IsDeleted);

        if (!string.IsNullOrEmpty(fipsId))
        {
            query = query.Where(m => m.FipsId == fipsId);
        }

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(m => m.Status == status);
        }

        var totalRecords = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

        var milestones = await query
            .OrderBy(m => m.DueDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new
            {
                m.Id,
                m.Name,
                m.Description,
                m.FipsId,
                m.ObjectiveId,
                m.BaselineDueDate,
                m.DueDate,
                m.ActualDate,
                m.Status,
                m.ProgressPercent,
                m.BusinessArea,
                m.ExternalRef,
                m.CreatedAt,
                m.UpdatedAt
            })
            .ToListAsync();

        return Ok(new
        {
            data = milestones,
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
    [RequireApiPermission("Milestones", "read")]
    public async Task<IActionResult> GetMilestone(int id)
    {
        var milestone = await _context.Milestones
            .Include(m => m.MilestoneActions)
                .ThenInclude(ma => ma.Action)
            .Include(m => m.MilestoneRisks)
                .ThenInclude(mr => mr.Risk)
            .Include(m => m.MilestoneIssues)
                .ThenInclude(mi => mi.Issue)
            .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);

        if (milestone == null)
        {
            return NotFound(new
            {
                error = new
                {
                    code = "NOT_FOUND",
                    message = $"Milestone with ID {id} not found"
                }
            });
        }

        return Ok(new
        {
            milestone.Id,
            milestone.Name,
            milestone.Description,
            milestone.FipsId,
            milestone.ObjectiveId,
            milestone.BaselineDueDate,
            milestone.DueDate,
            milestone.ActualDate,
            milestone.Status,
            milestone.ProgressPercent,
            milestone.BusinessArea,
            milestone.ExternalRef,
            milestone.Notes,
            Actions = milestone.MilestoneActions.Select(ma => new
            {
                ma.Action.Id,
                ma.Action.Title,
                ma.Action.Status
            }).ToList(),
            Risks = milestone.MilestoneRisks.Select(mr => new
            {
                mr.Risk.Id,
                mr.Risk.Title,
                mr.Risk.RiskScore
            }).ToList(),
            Issues = milestone.MilestoneIssues.Select(mi => new
            {
                mi.Issue.Id,
                mi.Issue.Title,
                mi.Issue.Severity
            }).ToList(),
            milestone.CreatedAt,
            milestone.UpdatedAt
        });
    }

    [HttpPost]
    [RequireApiPermission("Milestones", "create")]
    public async Task<IActionResult> CreateMilestone([FromBody] MilestoneCreateDto dto)
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

        var milestone = new Milestone
        {
            Name = dto.Name,
            Description = dto.Description,
            FipsId = dto.FipsId,
            ObjectiveId = dto.ObjectiveId,
            BaselineDueDate = dto.BaselineDueDate,
            DueDate = dto.DueDate,
            Status = dto.Status ?? "not_started",
            ProgressPercent = dto.ProgressPercent ?? 0,
            BusinessArea = dto.BusinessArea,
            ExternalRef = dto.ExternalRef,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Milestones.Add(milestone);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetMilestone), new { id = milestone.Id }, new
        {
            milestone.Id,
            milestone.Name,
            milestone.Status,
            milestone.DueDate,
            milestone.CreatedAt
        });
    }

    [HttpPut("{id}")]
    [RequireApiPermission("Milestones", "update")]
    public async Task<IActionResult> UpdateMilestone(int id, [FromBody] MilestoneUpdateDto dto)
    {
        var milestone = await _context.Milestones.FindAsync(id);
        if (milestone == null || milestone.IsDeleted)
        {
            return NotFound(new
            {
                error = new
                {
                    code = "NOT_FOUND",
                    message = $"Milestone with ID {id} not found"
                }
            });
        }

        if (dto.Name != null) milestone.Name = dto.Name;
        if (dto.Description != null) milestone.Description = dto.Description;
        if (dto.Status != null) milestone.Status = dto.Status;
        if (dto.DueDate.HasValue) milestone.DueDate = dto.DueDate.Value;
        if (dto.ActualDate.HasValue) milestone.ActualDate = dto.ActualDate.Value;
        if (dto.ProgressPercent.HasValue) milestone.ProgressPercent = dto.ProgressPercent.Value;
        if (dto.Notes != null) milestone.Notes = dto.Notes;

        milestone.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new { message = "Milestone updated successfully" });
    }

    [HttpDelete("{id}")]
    [RequireApiPermission("Milestones", "delete")]
    public async Task<IActionResult> DeleteMilestone(int id)
    {
        var milestone = await _context.Milestones.FindAsync(id);
        if (milestone == null || milestone.IsDeleted)
        {
            return NotFound(new
            {
                error = new
                {
                    code = "NOT_FOUND",
                    message = $"Milestone with ID {id} not found"
                }
            });
        }

        milestone.IsDeleted = true;
        milestone.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

public class MilestoneCreateDto
{
    [Required]
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? FipsId { get; set; }
    public int? ObjectiveId { get; set; }
    public DateTime? BaselineDueDate { get; set; }
    [Required]
    public DateTime DueDate { get; set; }
    public string? Status { get; set; }
    [System.ComponentModel.DataAnnotations.Range(0, 100)]
    public int? ProgressPercent { get; set; }
    public string? BusinessArea { get; set; }
    public string? ExternalRef { get; set; }
    public string? Notes { get; set; }
}

public class MilestoneUpdateDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Status { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? ActualDate { get; set; }
    [System.ComponentModel.DataAnnotations.Range(0, 100)]
    public int? ProgressPercent { get; set; }
    public string? Notes { get; set; }
}

