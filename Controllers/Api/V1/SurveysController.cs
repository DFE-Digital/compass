using System.ComponentModel.DataAnnotations;
using Compass.Data;
using Compass.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using Compass.Attributes;

namespace Compass.Controllers.Api.V1;

[ApiController]
[Route("api/v1")] // explicit routes below
public class SurveysController : ControllerBase
{
	private readonly CompassDbContext _db;
	private readonly ILogger<SurveysController> _logger;

	public SurveysController(CompassDbContext db, ILogger<SurveysController> logger)
	{
		_db = db;
		_logger = logger;
	}

    [HttpGet("surveys/{fipsId}")]
    [EnableRateLimiting("SurveysGetPolicy")]
    [RequireApiPermission("UserSatisfactionQuestions", "read")]
	[ProducesResponseType(typeof(GetSurveyResponseDto), 200)]
	[ProducesResponseType(404)]
	public async Task<IActionResult> GetSurvey([FromRoute] string fipsId)
	{
		var service = await _db.Services.AsNoTracking().FirstOrDefaultAsync(s => s.FipsId == fipsId && s.IsActive);
		if (service == null)
		{
			return NotFound(new { error = "not_found", message = "Unknown fips_id" });
		}

		var now = DateTime.UtcNow;
		var instance = await _db.SurveyInstances.AsNoTracking()
			.Include(si => si.Template)
			.ThenInclude(t => t!.Questions)
			.ThenInclude(q => q.Options)
			.Include(si => si.Template) // include steps separately to avoid nullable issues
			.FirstOrDefaultAsync(si => si.ServiceId == service.ServiceId && si.IsActive && si.StartUtc <= now && (si.EndUtc == null || si.EndUtc >= now));

		if (instance == null || instance.Template == null)
		{
			return NotFound(new { error = "not_found", message = "No active survey instance for fips_id" });
		}

		var steps = await _db.JourneySteps.AsNoTracking()
			.Where(js => js.SurveyTemplateId == instance.SurveyTemplateId && js.Active)
			.OrderBy(js => js.Ordinal)
			.ToListAsync();

		var questionLookup = instance.Template.Questions
			.Where(q => q.Active)
			.ToDictionary(q => q.Code, q => q);

		var dto = new GetSurveyResponseDto
		{
			FipsId = service.FipsId,
			DisplayName = service.DisplayName,
			Template = new TemplateDto { Id = instance.SurveyTemplateId, Version = instance.Template.Version },
			Steps = new List<StepDto>()
		};

		foreach (var step in steps)
		{
			if (!questionLookup.TryGetValue(step.QuestionCode, out var q)) continue;
			var options = new List<string>();
			if (q.Options != null && q.Options.Any(o => o.Active))
			{
				options = q.Options.Where(o => o.Active).OrderBy(o => o.Ordinal).Select(o => o.Value).ToList();
			}
			dto.Steps.Add(new StepDto
			{
				Code = q.Code,
				Title = q.Title,
				Mandatory = q.Mandatory,
				InputType = MapInputType(q.InputType),
				Options = options
			});
		}

		return Ok(dto);
	}

    [HttpPost("responses")]
    [EnableRateLimiting("ResponsesPolicy")]
    [RequireApiPermission("UserSatisfactionResponses", "create")]
	[ProducesResponseType(typeof(PostResponseResultDto), 201)]
	[ProducesResponseType(400)]
	[ProducesResponseType(422)]
	public async Task<IActionResult> SubmitResponse([FromBody] PostResponseDto payload)
	{
		if (payload == null || string.IsNullOrWhiteSpace(payload.FipsId))
		{
			return BadRequest(new { error = "validation_error", message = "fips_id is required" });
		}

		var service = await _db.Services.FirstOrDefaultAsync(s => s.FipsId == payload.FipsId && s.IsActive);
		if (service == null)
		{
			return BadRequest(new { error = "validation_error", message = "Unknown fips_id" });
		}

		var now = DateTime.UtcNow;
		var instance = await _db.SurveyInstances
			.Include(si => si.Template)
			.ThenInclude(t => t!.Questions)
			.ThenInclude(q => q.Options)
			.FirstOrDefaultAsync(si => si.ServiceId == service.ServiceId && si.IsActive && si.StartUtc <= now && (si.EndUtc == null || si.EndUtc >= now));

		if (instance == null || instance.Template == null)
		{
			return BadRequest(new { error = "validation_error", message = "No active survey for fips_id" });
		}

		// Validate Q1 1-5
		if (!payload.Answers.TryGetValue("Q1", out var q1Obj) || q1Obj == null)
		{
			return BadRequest(new { error = "validation_error", message = "Q1 is mandatory and must be 1–5", fields = new { Q1 = "required" } });
		}
		if (q1Obj is not int q1Val || q1Val < 1 || q1Val > 5)
		{
			return BadRequest(new { error = "validation_error", message = "Q1 is mandatory and must be 1–5", fields = new { Q1 = "invalid" } });
		}

		// Build weights (default) possibly overridden in instance
		var weights = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		foreach (var q in instance.Template.Questions.Where(q => q.Active))
		{
			weights[q.Code] = q.Weight;
		}
		// Parse overrides if present
		if (!string.IsNullOrWhiteSpace(instance.WeightsJson))
		{
			try
			{
				var overrides = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, int>>(instance.WeightsJson!);
				if (overrides != null)
				{
					foreach (var kv in overrides)
					{
						weights[kv.Key] = kv.Value;
					}
				}
			}
			catch { /* ignore bad override */ }
		}

		// Collect ratings
		var ratings = new Dictionary<string, int?>();
		foreach (var q in instance.Template.Questions)
		{
			if (payload.Answers.TryGetValue(q.Code, out var val) && val is int r)
			{
				ratings[q.Code] = r;
			}
			else
			{
				ratings[q.Code] = null;
			}
		}

		var uss = UssScoring.ComputeUss(ratings, weights);
		var band = UssScoring.BandFromScore(uss);

		var response = new Models.SurveyResponse
		{
			SurveyInstanceId = instance.SurveyInstanceId,
			Channel = payload.Channel,
			GeoRegion = payload.GeoRegion,
			FreeText = payload.Answers.TryGetValue("COMMENT", out var v) ? v as string : null,
			UssComputed = uss,
			Band = band
		};

		_db.SurveyResponses.Add(response);
		await _db.SaveChangesAsync();

		// Answers
		var answers = new List<Models.ResponseAnswer>();
		foreach (var q in instance.Template.Questions)
		{
			payload.Answers.TryGetValue(q.Code, out var raw);
			int? rating = raw is int ri ? ri : null;
			string? text = raw is string s ? s : null;
			string? option = raw is string so && q.InputType != Models.SurveyInputType.Text ? so : null;

			answers.Add(new Models.ResponseAnswer
			{
				SurveyResponseId = response.SurveyResponseId,
				SurveyQuestionId = q.SurveyQuestionId,
				Rating = rating,
				TextValue = text,
				OptionValue = option
			});
		}

		_db.ResponseAnswers.AddRange(answers);
		await _db.SaveChangesAsync();

		var result = new PostResponseResultDto
		{
			ResponseId = response.SurveyResponseId,
			Uss = response.UssComputed,
			Band = response.Band!
		};

		return Created(string.Empty, result);
	}

	private static string MapInputType(Models.SurveyInputType t) => t switch
	{
		Models.SurveyInputType.Likert_1_5 => "likert_1_5",
		Models.SurveyInputType.Select => "select",
		Models.SurveyInputType.Text => "text",
		Models.SurveyInputType.YesNo => "yesno",
		_ => "text"
	};
}

public class GetSurveyResponseDto
{
	public string FipsId { get; set; } = string.Empty;
	public string? DisplayName { get; set; }
	public TemplateDto Template { get; set; } = new();
	public List<StepDto> Steps { get; set; } = new();
}

public class TemplateDto
{
	public Guid Id { get; set; }
	public int Version { get; set; }
}

public class StepDto
{
	public string Code { get; set; } = string.Empty;
	public string Title { get; set; } = string.Empty;
	public bool Mandatory { get; set; }
	public string InputType { get; set; } = "text";
	public List<string> Options { get; set; } = new();
}

public class PostResponseDto
{
	[Required]
	public string FipsId { get; set; } = string.Empty;
	[Required]
	public Dictionary<string, object?> Answers { get; set; } = new();
	public string? Channel { get; set; }
	public string? GeoRegion { get; set; }
}

public class PostResponseResultDto
{
	public Guid ResponseId { get; set; }
	public decimal Uss { get; set; }
	public string Band { get; set; } = string.Empty;
}


