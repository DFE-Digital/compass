using System.ComponentModel.DataAnnotations;
using Compass.Attributes;
using Compass.Data;
using Compass.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Compass.Controllers.Api.V1;

[ApiController]
[Route("api/v1/[controller]")]
public class FunctionalStandardsController : ControllerBase
{
    private readonly CompassDbContext _context;
    private readonly ILogger<FunctionalStandardsController> _logger;

    public FunctionalStandardsController(CompassDbContext context, ILogger<FunctionalStandardsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    [RequireApiPermission("FunctionalStandards", "read")]
    public async Task<IActionResult> GetFunctionalStandards()
    {
        var standards = await _context.FunctionalStandards
            .Include(fs => fs.Themes)
                .ThenInclude(t => t.PracticeAreas)
            .OrderBy(fs => fs.Id)
            .Select(fs => new
            {
                fs.Id,
                fs.Title,
                fs.Description,
                fs.PublishedDate,
                Themes = fs.Themes.Select(t => new
                {
                    t.ThemeId,
                    t.Title,
                    t.Description,
                    PracticeAreaCount = t.PracticeAreas.Count
                }).ToList()
            })
            .ToListAsync();

        return Ok(new { data = standards });
    }

    [HttpGet("{id}")]
    [RequireApiPermission("FunctionalStandards", "read")]
    public async Task<IActionResult> GetFunctionalStandard(int id)
    {
        var standard = await _context.FunctionalStandards
            .Include(fs => fs.Themes)
                .ThenInclude(t => t.PracticeAreas)
                    .ThenInclude(pa => pa.Criteria)
            .FirstOrDefaultAsync(fs => fs.Id == id);

        if (standard == null)
        {
            return NotFound(new
            {
                error = new
                {
                    code = "NOT_FOUND",
                    message = $"Functional standard with ID {id} not found"
                }
            });
        }

        return Ok(new
        {
            standard.Id,
            standard.Title,
            standard.Description,
            standard.PublishedDate,
            Themes = standard.Themes.Select(t => new
            {
                t.ThemeId,
                t.Title,
                t.Description,
                PracticeAreas = t.PracticeAreas.Select(pa => new
                {
                    pa.PracticeAreaId,
                    pa.Title,
                    pa.Description,
                    Criteria = pa.Criteria.Select(c => new
                    {
                        c.CriteriaCode,
                        c.Criteria,
                        c.Rating
                    }).ToList()
                }).ToList()
            }).ToList()
        });
    }

    [HttpGet("assessments")]
    [RequireApiPermission("FunctionalStandards", "read")]
    public async Task<IActionResult> GetAssessments(
        [FromQuery] int? functionalStandardId = null)
    {
        var query = _context.FunctionalStandardAssessments
            .Include(fsa => fsa.FunctionalStandard)
            .AsQueryable();

        if (functionalStandardId.HasValue)
        {
            query = query.Where(fsa => fsa.FunctionalStandardId == functionalStandardId.Value);
        }

        var assessments = await query
            .Select(fsa => new
            {
                fsa.Id,
                fsa.AssessmentName,
                FunctionalStandard = new
                {
                    fsa.FunctionalStandard!.Id,
                    fsa.FunctionalStandard.Title
                },
                fsa.AssessedBy,
                fsa.AssessmentDate,
                fsa.SubmittedAt,
                fsa.CreatedAt,
                fsa.UpdatedAt
            })
            .ToListAsync();

        return Ok(new { data = assessments });
    }

    [HttpGet("assessments/{id}")]
    [RequireApiPermission("FunctionalStandards", "read")]
    public async Task<IActionResult> GetAssessment(int id)
    {
        var assessment = await _context.FunctionalStandardAssessments
            .Include(fsa => fsa.FunctionalStandard)
            .Include(fsa => fsa.CriteriaResponses)
                .ThenInclude(cr => cr.Criterion)
            .FirstOrDefaultAsync(fsa => fsa.Id == id);

        if (assessment == null)
        {
            return NotFound(new
            {
                error = new
                {
                    code = "NOT_FOUND",
                    message = $"Assessment with ID {id} not found"
                }
            });
        }

        return Ok(new
        {
            assessment.Id,
            assessment.AssessmentName,
            FunctionalStandard = new
            {
                assessment.FunctionalStandard!.Id,
                assessment.FunctionalStandard.Title
            },
            assessment.AssessedBy,
            assessment.AssessmentDate,
            assessment.SubmittedAt,
            assessment.Notes,
            Responses = assessment.CriteriaResponses.Select(cr => new
            {
                cr.Id,
                Criterion = new
                {
                    cr.CriteriaCode,
                    cr.Criterion!.Criteria
                },
                cr.Attainment,
                cr.Notes
            }).ToList(),
            assessment.CreatedAt,
            assessment.UpdatedAt
        });
    }

    [HttpPost("assessments")]
    [RequireApiPermission("FunctionalStandards", "create")]
    public async Task<IActionResult> CreateAssessment([FromBody] AssessmentCreateDto dto)
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

        var assessment = new FunctionalStandardAssessment
        {
            AssessmentName = dto.AssessmentName,
            FunctionalStandardId = dto.FunctionalStandardId,
            AssessedBy = dto.AssessedBy,
            AssessmentDate = dto.AssessmentDate ?? DateTime.UtcNow,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.FunctionalStandardAssessments.Add(assessment);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAssessment), new { id = assessment.Id }, new
        {
            assessment.Id,
            assessment.AssessmentName,
            assessment.AssessedBy,
            assessment.CreatedAt
        });
    }
}

public class AssessmentCreateDto
{
    [Required]
    public string AssessmentName { get; set; } = string.Empty;
    [Required]
    public int FunctionalStandardId { get; set; }
    [Required]
    public string AssessedBy { get; set; } = string.Empty;
    public DateTime? AssessmentDate { get; set; }
    public string? Notes { get; set; }
}

