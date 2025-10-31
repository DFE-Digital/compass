using System.ComponentModel.DataAnnotations;
using Compass.Attributes;
using Compass.Data;
using Compass.Models;
using Compass.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Compass.Controllers.Api.V1.Admin;

[ApiController]
[Route("api/v1/admin/templates/{id}/journey")]
public class JourneyController : ControllerBase
{
	private readonly CompassDbContext _db;
	private readonly IAuditLogger _audit;

	public JourneyController(CompassDbContext db, IAuditLogger audit)
	{
		_db = db;
		_audit = audit;
	}

	[HttpGet]
	[RequireApiPermission("SurveysAdmin", "read")]
	public async Task<IActionResult> Get([FromRoute] Guid id)
	{
		var steps = await _db.JourneySteps.Where(js => js.SurveyTemplateId == id)
			.OrderBy(js => js.Ordinal)
			.Select(js => new { js.JourneyStepId, js.QuestionCode, js.Ordinal, js.HelpText, js.ConditionalOnJson, js.Active })
			.ToListAsync();
		return Ok(new { data = steps });
	}

	[HttpPut]
	[RequireApiPermission("SurveysAdmin", "write")]
	public async Task<IActionResult> Replace([FromRoute] Guid id, [FromBody] ReplaceJourneyDto dto)
	{
		// Basic validation: referenced questions must exist and be active
		var codes = dto.Steps.Select(s => s.QuestionCode).Distinct().ToList();
		var questions = await _db.SurveyQuestions.Where(q => q.SurveyTemplateId == id && codes.Contains(q.Code) && q.Active).Select(q => q.Code).ToListAsync();
		var missing = codes.Except(questions, StringComparer.OrdinalIgnoreCase).ToList();
		if (missing.Any())
		{
			return BadRequest(new { error = "validation_error", message = "Journey references inactive or missing questions", missing });
		}

		var existing = await _db.JourneySteps.Where(js => js.SurveyTemplateId == id).ToListAsync();
		_db.JourneySteps.RemoveRange(existing);
		var toAdd = dto.Steps.Select((s, idx) => new JourneyStep
		{
			SurveyTemplateId = id,
			QuestionCode = s.QuestionCode,
			Ordinal = s.Ordinal ?? idx,
			HelpText = s.HelpText,
			ConditionalOnJson = s.ConditionalOn,
			Active = s.Active ?? true
		}).ToList();
		_db.JourneySteps.AddRange(toAdd);
		await _db.SaveChangesAsync();
		await _audit.LogAsync("Journey", id.ToString(), "replace", User?.Identity?.Name, System.Text.Json.JsonSerializer.Serialize(dto));
		return Ok(new { count = toAdd.Count });
	}
}

public class ReplaceJourneyDto
{
	[Required]
	public List<JourneyStepDto> Steps { get; set; } = new();
}

public class JourneyStepDto
{
	[Required]
	public string QuestionCode { get; set; } = string.Empty;
	public int? Ordinal { get; set; }
	public string? HelpText { get; set; }
	public string? ConditionalOn { get; set; }
	public bool? Active { get; set; }
}


