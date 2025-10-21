using System.ComponentModel.DataAnnotations;
using Compass.Attributes;
using Compass.Data;
using Compass.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Compass.Controllers.Api.V1;

[ApiController]
[Route("api/v1/[controller]")]
public class EnterpriseMetricsController : ControllerBase
{
    private readonly CompassDbContext _context;
    private readonly ILogger<EnterpriseMetricsController> _logger;

    public EnterpriseMetricsController(CompassDbContext context, ILogger<EnterpriseMetricsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    [RequireApiPermission("EnterpriseMetrics", "read")]
    public async Task<IActionResult> GetEnterpriseMetrics()
    {
        var metrics = await _context.EnterpriseMetrics
            .OrderBy(em => em.Identifier)
            .Select(em => new
            {
                em.Id,
                em.Identifier,
                em.Title,
                em.Description,
                em.HintText,
                em.ValueType,
                em.ValidationRules,
                em.ValidFromYear,
                em.ValidFromMonth,
                em.CreatedAt,
                em.UpdatedAt
            })
            .ToListAsync();

        return Ok(new { data = metrics });
    }

    [HttpGet("{id}")]
    [RequireApiPermission("EnterpriseMetrics", "read")]
    public async Task<IActionResult> GetEnterpriseMetric(int id)
    {
        var metric = await _context.EnterpriseMetrics.FindAsync(id);

        if (metric == null)
        {
            return NotFound(new
            {
                error = new
                {
                    code = "NOT_FOUND",
                    message = $"Enterprise metric with ID {id} not found"
                }
            });
        }

        return Ok(new
        {
            metric.Id,
            metric.Identifier,
            metric.Title,
            metric.Description,
            metric.HintText,
            metric.ValueType,
            metric.ValidationRules,
            metric.ValidFromYear,
            metric.ValidFromMonth,
            metric.CreatedAt,
            metric.UpdatedAt
        });
    }

    [HttpPost("submit")]
    [RequireApiPermission("EnterpriseMetrics", "create")]
    public async Task<IActionResult> SubmitMetricValue([FromBody] EnterpriseMetricSubmitDto dto)
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

        // Get or create enterprise return
        var enterpriseReturn = await _context.EnterpriseReturns
            .FirstOrDefaultAsync(er => er.Year == dto.Year && er.Month == dto.Month);

        if (enterpriseReturn == null)
        {
            enterpriseReturn = new EnterpriseReturn
            {
                Year = dto.Year,
                Month = dto.Month,
                Status = ReturnStatus.Upcoming,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.EnterpriseReturns.Add(enterpriseReturn);
            await _context.SaveChangesAsync();
        }

        // Find or create metric value
        var metricValue = await _context.EnterpriseMetricValues
            .FirstOrDefaultAsync(emv => emv.EnterpriseReturnId == enterpriseReturn.Id && emv.EnterpriseMetricId == dto.MetricId);

        if (metricValue == null)
        {
            metricValue = new EnterpriseMetricValue
            {
                EnterpriseReturnId = enterpriseReturn.Id,
                EnterpriseMetricId = dto.MetricId,
                Value = dto.Value,
                IsComplete = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.EnterpriseMetricValues.Add(metricValue);
        }
        else
        {
            metricValue.Value = dto.Value;
            metricValue.IsComplete = true;
            metricValue.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetEnterpriseMetric), new { id = dto.MetricId }, new
        {
            message = "Enterprise metric value submitted successfully",
            metricValueId = metricValue.Id
        });
    }

    [HttpGet("values")]
    [RequireApiPermission("EnterpriseMetrics", "read")]
    public async Task<IActionResult> GetMetricValues(
        [FromQuery] int? year = null,
        [FromQuery] int? month = null)
    {
        var query = _context.EnterpriseMetricValues
            .Include(emv => emv.EnterpriseMetric)
            .Include(emv => emv.EnterpriseReturn)
            .AsQueryable();

        if (year.HasValue)
        {
            query = query.Where(emv => emv.EnterpriseReturn!.Year == year.Value);
        }

        if (month.HasValue)
        {
            query = query.Where(emv => emv.EnterpriseReturn!.Month == month.Value);
        }

        var values = await query
            .Select(emv => new
            {
                emv.Id,
                emv.EnterpriseReturnId,
                Year = emv.EnterpriseReturn!.Year,
                Month = emv.EnterpriseReturn!.Month,
                Metric = new
                {
                    emv.EnterpriseMetric!.Id,
                    emv.EnterpriseMetric.Identifier,
                    emv.EnterpriseMetric.Title
                },
                emv.Value,
                emv.IsComplete,
                emv.UpdatedAt
            })
            .ToListAsync();

        return Ok(new { data = values });
    }
}

public class EnterpriseMetricSubmitDto
{
    [Required]
    public int MetricId { get; set; }
    [Required]
    public int Year { get; set; }
    [Required]
    public int Month { get; set; }
    public string? Value { get; set; }
}

