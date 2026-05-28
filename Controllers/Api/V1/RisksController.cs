using System.ComponentModel.DataAnnotations;
using Compass.Attributes;
using Compass.Data;
using Compass.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Compass.Controllers.Api.V1;

[ApiController]
[Route("api/v1/[controller]")]
public class RisksController : ControllerBase
{
    private readonly CompassDbContext _context;
    private readonly ILogger<RisksController> _logger;

    public RisksController(CompassDbContext context, ILogger<RisksController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    [RequireApiPermission("Risks", "read")]
    public async Task<IActionResult> GetRisks(
        [FromQuery] string? fipsId = null,
        [FromQuery] string? status = null,
        [FromQuery] int? minScore = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        if (pageSize > 100) pageSize = 100;
        if (page < 1) page = 1;

        var query = _context.Risks
            .Include(r => r.RiskTier)
            .Include(r => r.RiskRiskTypes)
                .ThenInclude(rrt => rrt.RiskType)
            .Where(r => !r.IsDeleted);

        if (!string.IsNullOrEmpty(fipsId))
        {
            query = query.Where(r => r.FipsId == fipsId);
        }

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(r => r.Status == status);
        }

        if (minScore.HasValue)
        {
            query = query.Where(r => r.RiskScore >= minScore.Value);
        }

        var totalRecords = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

        var risks = await query
            .OrderByDescending(r => r.RiskScore)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new
            {
                r.Id,
                r.Title,
                r.Description,
                r.FipsId,
                r.ObjectiveId,
                RiskTier = r.RiskTier != null ? new { r.RiskTier.Id, r.RiskTier.Name } : null,
                r.ImpactRating,
                r.LikelihoodRating,
                r.RiskScore,
                r.Status,
                r.OwnerEmail,
                r.ProximityDate,
                r.Response,
                r.ResidualImpact,
                r.ResidualLikelihood,
                r.TargetDate,
                r.Category,
                r.BusinessArea,
                RiskTypes = r.RiskRiskTypes.Select(rrt => new
                {
                    rrt.RiskType.Id,
                    rrt.RiskType.Name,
                    rrt.RiskType.Code
                }).ToList(),
                r.CreatedAt,
                r.UpdatedAt
            })
            .ToListAsync();

        return Ok(new
        {
            data = risks,
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
    [RequireApiPermission("Risks", "read")]
    public async Task<IActionResult> GetRisk(int id)
    {
        var risk = await _context.Risks
            .Include(r => r.RiskTier)
            .Include(r => r.RiskRiskTypes)
                .ThenInclude(rrt => rrt.RiskType)
            .Include(r => r.RiskActions)
                .ThenInclude(ra => ra.Action)
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);

        if (risk == null)
        {
            return NotFound(new
            {
                error = new
                {
                    code = "NOT_FOUND",
                    message = $"Risk with ID {id} not found"
                }
            });
        }

        return Ok(new
        {
            risk.Id,
            risk.Title,
            risk.Description,
            risk.FipsId,
            risk.ObjectiveId,
            RiskTier = risk.RiskTier != null ? new { risk.RiskTier.Id, risk.RiskTier.Name } : null,
            risk.ImpactRating,
            risk.LikelihoodRating,
            risk.RiskScore,
            risk.Status,
            risk.OwnerEmail,
            risk.ProximityDate,
            risk.Response,
            risk.ResidualImpact,
            risk.ResidualLikelihood,
            risk.TargetDate,
            risk.Category,
            risk.BusinessArea,
            risk.Notes,
            RiskTypes = risk.RiskRiskTypes.Select(rrt => new
            {
                rrt.RiskType.Id,
                rrt.RiskType.Name,
                rrt.RiskType.Code
            }).ToList(),
            Actions = risk.RiskActions.Select(ra => new
            {
                ra.Action.Id,
                ra.Action.Title,
                ra.Action.Status,
                ra.Action.DueDate
            }).ToList(),
            risk.CreatedAt,
            risk.UpdatedAt
        });
    }

    [HttpPost]
    [RequireApiPermission("Risks", "create")]
    public async Task<IActionResult> CreateRisk([FromBody] RiskCreateDto dto)
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

        string? ownerEmail = dto.OwnerEmail;
        if (dto.OwnerUserId is > 0)
        {
            var ownerUser = await _context.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == dto.OwnerUserId.Value);
            if (ownerUser == null)
            {
                return BadRequest(new
                {
                    error = new
                    {
                        code = "VALIDATION_ERROR",
                        message = $"Owner user with ID {dto.OwnerUserId} not found"
                    }
                });
            }

            ownerEmail = ownerUser.Email ?? ownerEmail;
        }

        var risk = new Risk
        {
            Title = dto.Title,
            Description = dto.Description,
            FipsId = dto.FipsId,
            ObjectiveId = dto.ObjectiveId,
            RiskTierId = dto.RiskTierId,
            ImpactRating = dto.ImpactRating,
            LikelihoodRating = dto.LikelihoodRating,
            RiskScore = dto.ImpactRating * dto.LikelihoodRating,
            OwnerUserId = dto.OwnerUserId is > 0 ? dto.OwnerUserId : null,
            OwnerEmail = ownerEmail,
            Status = dto.Status ?? "new",
            Category = dto.Category,
            BusinessArea = dto.BusinessArea,
            ProximityDate = dto.ProximityDate,
            Response = dto.Response,
            TargetDate = dto.TargetDate,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Risks.Add(risk);
        await _context.SaveChangesAsync();

        // Add risk types if provided
        if (dto.RiskTypeIds != null && dto.RiskTypeIds.Any())
        {
            foreach (var typeId in dto.RiskTypeIds)
            {
                risk.RiskRiskTypes.Add(new RiskRiskType { RiskId = risk.Id, RiskTypeId = typeId });
            }
            await _context.SaveChangesAsync();
        }

        return CreatedAtAction(nameof(GetRisk), new { id = risk.Id }, new
        {
            risk.Id,
            risk.Title,
            risk.Status,
            risk.RiskScore,
            risk.CreatedAt
        });
    }

    [HttpPut("{id}")]
    [RequireApiPermission("Risks", "update")]
    public async Task<IActionResult> UpdateRisk(int id, [FromBody] RiskUpdateDto dto)
    {
        var risk = await _context.Risks.FindAsync(id);
        if (risk == null || risk.IsDeleted)
        {
            return NotFound(new
            {
                error = new
                {
                    code = "NOT_FOUND",
                    message = $"Risk with ID {id} not found"
                }
            });
        }

        if (dto.Title != null) risk.Title = dto.Title;
        if (dto.Description != null) risk.Description = dto.Description;
        if (dto.ImpactRating.HasValue) risk.ImpactRating = dto.ImpactRating.Value;
        if (dto.LikelihoodRating.HasValue) risk.LikelihoodRating = dto.LikelihoodRating.Value;
        if (dto.Status != null) risk.Status = dto.Status;
        if (dto.OwnerEmail != null) risk.OwnerEmail = dto.OwnerEmail;
        if (dto.Category != null) risk.Category = dto.Category;
        if (dto.BusinessArea != null) risk.BusinessArea = dto.BusinessArea;
        if (dto.Response != null) risk.Response = dto.Response;

        risk.RiskScore = risk.ImpactRating * risk.LikelihoodRating;
        risk.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new { message = "Risk updated successfully" });
    }

    [HttpDelete("{id}")]
    [RequireApiPermission("Risks", "delete")]
    public async Task<IActionResult> DeleteRisk(int id)
    {
        var risk = await _context.Risks.FindAsync(id);
        if (risk == null || risk.IsDeleted)
        {
            return NotFound(new
            {
                error = new
                {
                    code = "NOT_FOUND",
                    message = $"Risk with ID {id} not found"
                }
            });
        }

        risk.IsDeleted = true;
        risk.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

public class RiskCreateDto
{
    [Required]
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? FipsId { get; set; }
    public int? ObjectiveId { get; set; }
    public int? RiskTierId { get; set; }
    [Required]
    [System.ComponentModel.DataAnnotations.Range(1, 5)]
    public int ImpactRating { get; set; }
    [Required]
    [System.ComponentModel.DataAnnotations.Range(1, 5)]
    public int LikelihoodRating { get; set; }
    public string? OwnerEmail { get; set; }
    public int? OwnerUserId { get; set; }
    public string? Status { get; set; }
    public string? Category { get; set; }
    public string? BusinessArea { get; set; }
    public DateTime? ProximityDate { get; set; }
    public string? Response { get; set; }
    public DateTime? TargetDate { get; set; }
    public List<int>? RiskTypeIds { get; set; }
}

public class RiskUpdateDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    [System.ComponentModel.DataAnnotations.Range(1, 5)]
    public int? ImpactRating { get; set; }
    [System.ComponentModel.DataAnnotations.Range(1, 5)]
    public int? LikelihoodRating { get; set; }
    public string? Status { get; set; }
    public string? OwnerEmail { get; set; }
    public string? Category { get; set; }
    public string? BusinessArea { get; set; }
    public string? Response { get; set; }
}

