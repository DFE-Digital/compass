using System.ComponentModel.DataAnnotations;
using Compass.Attributes;
using Compass.Data;
using Compass.Models;
using Compass.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Compass.Controllers.Api.V1.Admin;

[ApiController]
[Route("api/v1/admin/services")] 
public class ServicesController : ControllerBase
{
	private readonly CompassDbContext _db;
	private readonly IAuditLogger _audit;

	public ServicesController(CompassDbContext db, IAuditLogger audit)
	{
		_db = db;
		_audit = audit;
	}

	[HttpPost("upsert")]
	[RequireApiPermission("SurveysAdmin", "write")]
	public async Task<IActionResult> Upsert([FromBody] UpsertServiceDto dto)
	{
		if (!ModelState.IsValid) return UnprocessableEntity(new { error = "validation_error" });
		var entity = await _db.Services.FirstOrDefaultAsync(s => s.FipsId == dto.FipsId);
		if (entity == null)
		{
			entity = new FipsService { FipsId = dto.FipsId, DisplayName = dto.DisplayName, IsActive = true };
			_db.Services.Add(entity);
		}
		else
		{
			entity.DisplayName = dto.DisplayName;
			entity.IsActive = dto.IsActive ?? entity.IsActive;
			entity.UpdatedUtc = DateTime.UtcNow;
		}
		await _db.SaveChangesAsync();
		await _audit.LogAsync("Service", entity.ServiceId.ToString(), "upsert", User?.Identity?.Name, System.Text.Json.JsonSerializer.Serialize(dto));
		return Ok(new { id = entity.ServiceId });
	}
}

public class UpsertServiceDto
{
	[Required]
	public string FipsId { get; set; } = string.Empty;
	[Required]
	public string DisplayName { get; set; } = string.Empty;
	public bool? IsActive { get; set; }
}


