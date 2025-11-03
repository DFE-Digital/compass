using Compass.Attributes;
using Compass.Data;
using Compass.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Compass.Controllers.Api.V1;

[ApiController]
[Route("api/v1/[controller]")]
public class StatementTemplatesController : ControllerBase
{
    private readonly CompassDbContext _context;
    private readonly ILogger<StatementTemplatesController> _logger;

    public StatementTemplatesController(CompassDbContext context, ILogger<StatementTemplatesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get all active statement templates
    /// </summary>
    [HttpGet]
    [RequireApiPermission("StatementTemplates", "read")]
    public async Task<IActionResult> GetTemplates()
    {
        var templates = await _context.StatementTemplates
            .Where(st => !st.IsDeleted && st.IsActive)
            .OrderBy(st => st.Name)
            .ThenByDescending(st => st.Version)
            .GroupBy(st => st.Name)
            .Select(g => new
            {
                name = g.Key,
                version = g.First().Version,
                content = g.First().Content,
                description = g.First().Description,
                createdAt = g.First().CreatedAt,
                updatedAt = g.First().UpdatedAt
            })
            .ToListAsync();

        return Ok(new { data = templates });
    }

    /// <summary>
    /// Get a specific statement template by name
    /// </summary>
    [HttpGet("{name}")]
    [RequireApiPermission("StatementTemplates", "read")]
    public async Task<IActionResult> GetTemplate(string name)
    {
        var template = await _context.StatementTemplates
            .Where(st => st.Name == name && !st.IsDeleted && st.IsActive)
            .OrderByDescending(st => st.Version)
            .FirstOrDefaultAsync();

        if (template == null)
        {
            return NotFound(new
            {
                error = new
                {
                    code = "NOT_FOUND",
                    message = $"Statement template '{name}' not found"
                }
            });
        }

        return Ok(new
        {
            data = new
            {
                name = template.Name,
                version = template.Version,
                content = template.Content,
                description = template.Description,
                createdAt = template.CreatedAt,
                updatedAt = template.UpdatedAt
            }
        });
    }

    /// <summary>
    /// Get statement template parameters (extracted from template content)
    /// </summary>
    [HttpGet("{name}/parameters")]
    [RequireApiPermission("StatementTemplates", "read")]
    public async Task<IActionResult> GetTemplateParameters(string name)
    {
        var template = await _context.StatementTemplates
            .Where(st => st.Name == name && !st.IsDeleted && st.IsActive)
            .OrderByDescending(st => st.Version)
            .FirstOrDefaultAsync();

        if (template == null)
        {
            return NotFound(new
            {
                error = new
                {
                    code = "NOT_FOUND",
                    message = $"Statement template '{name}' not found"
                }
            });
        }

        // Extract parameters from template content (format: {{ parameter_name }})
        var parameterPattern = @"\{\{\s*([a-zA-Z_][a-zA-Z0-9_]*)\s*\}\}";
        var matches = System.Text.RegularExpressions.Regex.Matches(template.Content, parameterPattern);
        var parameters = matches
            .Cast<System.Text.RegularExpressions.Match>()
            .Select(m => m.Groups[1].Value)
            .Distinct()
            .OrderBy(p => p)
            .ToList();

        return Ok(new
        {
            data = new
            {
                templateName = template.Name,
                parameters = parameters
            }
        });
    }
}

