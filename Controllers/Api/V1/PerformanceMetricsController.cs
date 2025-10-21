using System.ComponentModel.DataAnnotations;
using Compass.Attributes;
using Compass.Data;
using Compass.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Compass.Controllers.Api.V1;

[ApiController]
[Route("api/v1/[controller]")]
public class PerformanceMetricsController : ControllerBase
{
    private readonly CompassDbContext _context;
    private readonly ILogger<PerformanceMetricsController> _logger;

    public PerformanceMetricsController(CompassDbContext context, ILogger<PerformanceMetricsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    [RequireApiPermission("PerformanceMetrics", "read")]
    public async Task<IActionResult> GetPerformanceMetrics()
    {
        var metrics = await _context.PerformanceMetrics
            .OrderBy(pm => pm.Identifier)
            .Select(pm => new
            {
                pm.Id,
                pm.Identifier,
                pm.Title,
                pm.Description,
                pm.HintText,
                pm.ValueType,
                pm.ValidationRules,
                pm.ValidFromYear,
                pm.ValidFromMonth,
                pm.ApplicablePhases,
                pm.CreatedAt,
                pm.UpdatedAt
            })
            .ToListAsync();

        return Ok(new { data = metrics });
    }

    [HttpGet("{id}")]
    [RequireApiPermission("PerformanceMetrics", "read")]
    public async Task<IActionResult> GetPerformanceMetric(int id)
    {
        var metric = await _context.PerformanceMetrics.FindAsync(id);

        if (metric == null)
        {
            return NotFound(new
            {
                error = new
                {
                    code = "NOT_FOUND",
                    message = $"Performance metric with ID {id} not found"
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
            metric.ApplicablePhases,
            metric.CreatedAt,
            metric.UpdatedAt
        });
    }

    [HttpPost("submit")]
    [RequireApiPermission("PerformanceMetrics", "create")]
    public async Task<IActionResult> SubmitMetricValue([FromBody] PerformanceMetricSubmitDto dto)
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

        // Get or create product return
        var productReturn = await _context.ProductReturns
            .FirstOrDefaultAsync(pr => pr.FipsId == dto.FipsId && pr.Year == dto.Year && pr.Month == dto.Month);

        if (productReturn == null)
        {
            productReturn = new ProductReturn
            {
                FipsId = dto.FipsId,
                Year = dto.Year,
                Month = dto.Month,
                Status = ReturnStatus.Upcoming,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.ProductReturns.Add(productReturn);
            await _context.SaveChangesAsync();
        }

        // Find or create metric value
        var metricValue = await _context.ProductMetricValues
            .FirstOrDefaultAsync(mv => mv.ProductReturnId == productReturn.Id && mv.PerformanceMetricId == dto.MetricId);

        if (metricValue == null)
        {
            metricValue = new ProductMetricValue
            {
                ProductReturnId = productReturn.Id,
                PerformanceMetricId = dto.MetricId,
                Value = dto.Value,
                IsComplete = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.ProductMetricValues.Add(metricValue);
        }
        else
        {
            metricValue.Value = dto.Value;
            metricValue.IsComplete = true;
            metricValue.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetPerformanceMetric), new { id = dto.MetricId }, new
        {
            message = "Metric value submitted successfully",
            metricValueId = metricValue.Id
        });
    }

    [HttpGet("values")]
    [RequireApiPermission("PerformanceMetrics", "read")]
    public async Task<IActionResult> GetMetricValues(
        [FromQuery] string? fipsId = null,
        [FromQuery] int? year = null,
        [FromQuery] int? month = null)
    {
        var query = _context.ProductMetricValues
            .Include(mv => mv.PerformanceMetric)
            .Include(mv => mv.ProductReturn)
            .AsQueryable();

        if (!string.IsNullOrEmpty(fipsId))
        {
            query = query.Where(mv => mv.ProductReturn!.FipsId == fipsId);
        }

        if (year.HasValue)
        {
            query = query.Where(mv => mv.ProductReturn!.Year == year.Value);
        }

        if (month.HasValue)
        {
            query = query.Where(mv => mv.ProductReturn!.Month == month.Value);
        }

        var values = await query
            .Select(mv => new
            {
                mv.Id,
                mv.ProductReturnId,
                FipsId = mv.ProductReturn!.FipsId,
                Year = mv.ProductReturn!.Year,
                Month = mv.ProductReturn!.Month,
                Metric = new
                {
                    mv.PerformanceMetric!.Id,
                    mv.PerformanceMetric.Identifier,
                    mv.PerformanceMetric.Title
                },
                mv.Value,
                mv.IsComplete,
                mv.UpdatedAt
            })
            .ToListAsync();

        return Ok(new { data = values });
    }
}

public class PerformanceMetricSubmitDto
{
    [Required]
    public int MetricId { get; set; }
    [Required]
    public string FipsId { get; set; } = string.Empty;
    [Required]
    public int Year { get; set; }
    [Required]
    public int Month { get; set; }
    public string? Value { get; set; }
}

