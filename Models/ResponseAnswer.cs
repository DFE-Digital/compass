using System.ComponentModel.DataAnnotations;

namespace Compass.Models;

public class ResponseAnswer
{
	[Key]
	public Guid ResponseAnswerId { get; set; } = Guid.NewGuid();

	public Guid SurveyResponseId { get; set; }
	public SurveyResponse? SurveyResponse { get; set; }

	public Guid SurveyQuestionId { get; set; }
	public SurveyQuestion? SurveyQuestion { get; set; }

	public int? Rating { get; set; } // 1-5 (Likert)

	[StringLength(2000)]
	public string? TextValue { get; set; }

	[StringLength(100)]
	public string? OptionValue { get; set; }
}


