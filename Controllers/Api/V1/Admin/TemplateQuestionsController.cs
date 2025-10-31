using System.ComponentModel.DataAnnotations;
using Compass.Attributes;
using Compass.Data;
using Compass.Models;
using Compass.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Compass.Controllers.Api.V1.Admin;

[ApiController]
[Route("api/v1/admin")] // multiple sub-routes
public class TemplateQuestionsController : ControllerBase
{
	private readonly CompassDbContext _db;
	private readonly IAuditLogger _audit;

	public TemplateQuestionsController(CompassDbContext db, IAuditLogger audit)
	{
		_db = db;
		_audit = audit;
	}

	[HttpGet("templates/{id}/questions")]
	[RequireApiPermission("SurveysAdmin", "read")]
	public async Task<IActionResult> GetQuestions([FromRoute] Guid id)
	{
		var q = await _db.SurveyQuestions.Where(x => x.SurveyTemplateId == id)
			.OrderBy(x => x.Ordinal)
			.Select(x => new { x.SurveyQuestionId, x.Code, x.Title, x.Description, x.Mandatory, x.Weight, x.Ordinal, x.InputType, x.Active })
			.ToListAsync();
		return Ok(new { data = q });
	}

	[HttpPost("templates/{id}/questions")]
	[RequireApiPermission("SurveysAdmin", "write")]
	public async Task<IActionResult> AddQuestion([FromRoute] Guid id, [FromBody] CreateQuestionDto dto)
	{
		if (!ModelState.IsValid) return UnprocessableEntity(new { error = "validation_error" });
		var q = new SurveyQuestion
		{
			SurveyTemplateId = id,
			Code = dto.Code,
			Title = dto.Title,
			Description = dto.Description,
			Mandatory = dto.Mandatory,
			Weight = dto.Weight,
			Ordinal = dto.Ordinal,
			InputType = dto.InputType,
			Active = true
		};
		_db.SurveyQuestions.Add(q);
		await _db.SaveChangesAsync();
		await _audit.LogAsync("SurveyQuestion", q.SurveyQuestionId.ToString(), "create", User?.Identity?.Name, System.Text.Json.JsonSerializer.Serialize(dto));
		return Created(string.Empty, new { id = q.SurveyQuestionId });
	}

	[HttpPut("questions/{questionId}")]
	[RequireApiPermission("SurveysAdmin", "write")]
	public async Task<IActionResult> UpdateQuestion([FromRoute] Guid questionId, [FromBody] UpdateQuestionDto dto)
	{
		var q = await _db.SurveyQuestions.FindAsync(questionId);
		if (q == null) return NotFound();
		if (dto.Title != null) q.Title = dto.Title;
		if (dto.Description != null) q.Description = dto.Description;
		if (dto.Mandatory.HasValue) q.Mandatory = dto.Mandatory.Value;
		if (dto.Weight.HasValue) q.Weight = dto.Weight.Value;
		if (dto.Ordinal.HasValue) q.Ordinal = dto.Ordinal.Value;
		if (dto.InputType.HasValue) q.InputType = dto.InputType.Value;
		if (dto.Active.HasValue) q.Active = dto.Active.Value;
		await _db.SaveChangesAsync();
		await _audit.LogAsync("SurveyQuestion", questionId.ToString(), "update", User?.Identity?.Name, System.Text.Json.JsonSerializer.Serialize(dto));
		return Ok(new { id = questionId });
	}

	[HttpPost("questions/{questionId}/options")]
	[RequireApiPermission("SurveysAdmin", "write")]
	public async Task<IActionResult> AddOption([FromRoute] Guid questionId, [FromBody] CreateOptionDto dto)
	{
		if (!ModelState.IsValid) return UnprocessableEntity(new { error = "validation_error" });
		var opt = new SurveyOption { SurveyQuestionId = questionId, Value = dto.Value, Label = dto.Label, Ordinal = dto.Ordinal, Active = true };
		_db.SurveyOptions.Add(opt);
		await _db.SaveChangesAsync();
		await _audit.LogAsync("SurveyOption", opt.SurveyOptionId.ToString(), "create", User?.Identity?.Name, System.Text.Json.JsonSerializer.Serialize(dto));
		return Created(string.Empty, new { id = opt.SurveyOptionId });
	}

	[HttpPut("options/{optionId}")]
	[RequireApiPermission("SurveysAdmin", "write")]
	public async Task<IActionResult> UpdateOption([FromRoute] Guid optionId, [FromBody] UpdateOptionDto dto)
	{
		var opt = await _db.SurveyOptions.FindAsync(optionId);
		if (opt == null) return NotFound();
		if (dto.Value != null) opt.Value = dto.Value;
		if (dto.Label != null) opt.Label = dto.Label;
		if (dto.Ordinal.HasValue) opt.Ordinal = dto.Ordinal.Value;
		if (dto.Active.HasValue) opt.Active = dto.Active.Value;
		await _db.SaveChangesAsync();
		await _audit.LogAsync("SurveyOption", optionId.ToString(), "update", User?.Identity?.Name, System.Text.Json.JsonSerializer.Serialize(dto));
		return Ok(new { id = optionId });
	}
}

public class CreateQuestionDto
{
	[Required]
	[RegularExpression("^Q[0-9]+$")]
	public string Code { get; set; } = string.Empty;
	[Required]
	public string Title { get; set; } = string.Empty;
	public string? Description { get; set; }
	public bool Mandatory { get; set; }
	[Range(0, 1000)]
	public int Weight { get; set; } = 0;
	public int Ordinal { get; set; } = 0;
	public SurveyInputType InputType { get; set; } = SurveyInputType.Likert_1_5;
}

public class UpdateQuestionDto
{
	public string? Title { get; set; }
	public string? Description { get; set; }
	public bool? Mandatory { get; set; }
	public int? Weight { get; set; }
	public int? Ordinal { get; set; }
	public SurveyInputType? InputType { get; set; }
	public bool? Active { get; set; }
}

public class CreateOptionDto
{
	[Required]
	public string Value { get; set; } = string.Empty;
	[Required]
	public string Label { get; set; } = string.Empty;
	public int Ordinal { get; set; }
}

public class UpdateOptionDto
{
	public string? Value { get; set; }
	public string? Label { get; set; }
	public int? Ordinal { get; set; }
	public bool? Active { get; set; }
}


