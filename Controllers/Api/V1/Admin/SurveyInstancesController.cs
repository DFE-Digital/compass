using System.ComponentModel.DataAnnotations;
using Compass.Attributes;
using Compass.Data;
using Compass.Models;
using Compass.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Compass.Controllers.Api.V1.Admin;

[ApiController]
[Route("api/v1/admin/survey-instances")]
public class SurveyInstancesController : ControllerBase
{
	private readonly CompassDbContext _db;
	private readonly IAuditLogger _audit;

	public SurveyInstancesController(CompassDbContext db, IAuditLogger audit)
	{
		_db = db;
		_audit = audit;
	}

	[HttpPost]
	[RequireApiPermission("SurveysAdmin", "write")]
	public async Task<IActionResult> Create([FromBody] CreateSurveyInstanceDto dto)
	{
		if (!ModelState.IsValid) return UnprocessableEntity(new { error = "validation_error" });
		var service = await _db.Services.FirstOrDefaultAsync(s => s.FipsId == dto.FipsId);
		if (service == null) return BadRequest(new { error = "validation_error", message = "Unknown fips_id" });
		var template = await _db.SurveyTemplates.FindAsync(dto.TemplateId);
		if (template == null) return BadRequest(new { error = "validation_error", message = "Unknown template" });

		// prevent overlapping active windows for same service
		var overlapping = await _db.SurveyInstances.AnyAsync(si => si.ServiceId == service.ServiceId && si.IsActive &&
			((si.EndUtc == null && dto.EndUtc == null) ||
			 (si.EndUtc == null && dto.StartUtc <= si.StartUtc) ||
			 (dto.EndUtc == null && si.StartUtc <= dto.StartUtc) ||
			 (si.StartUtc <= dto.EndUtc && dto.StartUtc <= si.EndUtc)));
		if (overlapping) return Conflict(new { error = "conflict", message = "Overlapping active survey instance for service" });

		var entity = new SurveyInstance
		{
			ServiceId = service.ServiceId,
			SurveyTemplateId = template.SurveyTemplateId,
			StartUtc = dto.StartUtc,
			EndUtc = dto.EndUtc,
			IsActive = true,
			WeightsJson = dto.WeightsOverrideJson
		};

		_db.SurveyInstances.Add(entity);
		await _db.SaveChangesAsync();
		await _audit.LogAsync("SurveyInstance", entity.SurveyInstanceId.ToString(), "create", User?.Identity?.Name, System.Text.Json.JsonSerializer.Serialize(dto));
		return Created(string.Empty, new { id = entity.SurveyInstanceId });
	}

	[HttpPut("{id}")]
	[RequireApiPermission("SurveysAdmin", "write")]
	public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateSurveyInstanceDto dto)
	{
		var si = await _db.SurveyInstances.FindAsync(id);
		if (si == null) return NotFound();
		if (dto.StartUtc.HasValue) si.StartUtc = dto.StartUtc.Value;
		if (dto.EndUtcHasValue) si.EndUtc = dto.EndUtc; // allow explicit null via flag
		if (dto.IsActive.HasValue) si.IsActive = dto.IsActive.Value;
		if (dto.WeightsOverrideJson != null) si.WeightsJson = dto.WeightsOverrideJson;

		await _db.SaveChangesAsync();
		await _audit.LogAsync("SurveyInstance", id.ToString(), "update", User?.Identity?.Name, System.Text.Json.JsonSerializer.Serialize(dto));
		return Ok(new { id });
	}
}

public class CreateSurveyInstanceDto
{
	[Required]
	public string FipsId { get; set; } = string.Empty;
	[Required]
	public Guid TemplateId { get; set; }
	[Required]
	public DateTime StartUtc { get; set; }
	public DateTime? EndUtc { get; set; }
	public string? WeightsOverrideJson { get; set; }
}

public class UpdateSurveyInstanceDto
{
	public DateTime? StartUtc { get; set; }
	public DateTime? EndUtc { get; set; }
	public bool EndUtcHasValue { get; set; }
	public bool? IsActive { get; set; }
	public string? WeightsOverrideJson { get; set; }
}


