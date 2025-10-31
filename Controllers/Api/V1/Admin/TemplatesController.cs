using System.ComponentModel.DataAnnotations;
using Compass.Attributes;
using Compass.Data;
using Compass.Models;
using Compass.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Compass.Controllers.Api.V1.Admin;

[ApiController]
[Route("api/v1/admin/templates")]
public class TemplatesController : ControllerBase
{
	private readonly CompassDbContext _db;
	private readonly IAuditLogger _audit;

	public TemplatesController(CompassDbContext db, IAuditLogger audit)
	{
		_db = db;
		_audit = audit;
	}

	[HttpGet]
	[RequireApiPermission("SurveysAdmin", "read")]
	public async Task<IActionResult> List()
	{
		var items = await _db.SurveyTemplates
			.OrderByDescending(t => t.CreatedUtc)
			.Select(t => new { t.SurveyTemplateId, t.Name, t.Version, t.IsDefault, t.CreatedUtc })
			.ToListAsync();
		return Ok(new { data = items });
	}

	[HttpPost]
	[RequireApiPermission("SurveysAdmin", "write")]
	public async Task<IActionResult> Create([FromBody] CreateTemplateDto dto)
	{
		if (!ModelState.IsValid) return UnprocessableEntity(new { error = "validation_error" });
		var entity = new SurveyTemplate { Name = dto.Name, Version = dto.Version, IsDefault = dto.IsDefault };
		_db.SurveyTemplates.Add(entity);
		await _db.SaveChangesAsync();
		await _audit.LogAsync("SurveyTemplate", entity.SurveyTemplateId.ToString(), "create", User?.Identity?.Name, System.Text.Json.JsonSerializer.Serialize(dto));
		return Created(string.Empty, new { id = entity.SurveyTemplateId });
	}

	[HttpPut("{id}")]
	[RequireApiPermission("SurveysAdmin", "write")]
	public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateTemplateDto dto)
	{
		var entity = await _db.SurveyTemplates.FindAsync(id);
		if (entity == null) return NotFound();
		if (dto.Name != null) entity.Name = dto.Name;
		if (dto.IsDefault.HasValue) entity.IsDefault = dto.IsDefault.Value;
		await _db.SaveChangesAsync();
		await _audit.LogAsync("SurveyTemplate", id.ToString(), "update", User?.Identity?.Name, System.Text.Json.JsonSerializer.Serialize(dto));
		return Ok(new { id });
	}
}

public class CreateTemplateDto
{
	[Required]
	public string Name { get; set; } = string.Empty;
	[Range(1, 1000)]
	public int Version { get; set; } = 1;
	public bool IsDefault { get; set; }
}

public class UpdateTemplateDto
{
	public string? Name { get; set; }
	public bool? IsDefault { get; set; }
}


