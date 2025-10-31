using System.ComponentModel.DataAnnotations;

namespace Compass.Models;

public class JourneyStep
{
	[Key]
	public Guid JourneyStepId { get; set; } = Guid.NewGuid();

	[Required]
	public Guid SurveyTemplateId { get; set; }

	public SurveyTemplate? Template { get; set; }

	[Required]
	[StringLength(10)]
	public string QuestionCode { get; set; } = string.Empty; // references SurveyQuestion.Code

	public int Ordinal { get; set; }

	[StringLength(1000)]
	public string? HelpText { get; set; }

	// JSON expression for basic conditional display logic
	public string? ConditionalOnJson { get; set; }

	public bool Active { get; set; } = true;
}


