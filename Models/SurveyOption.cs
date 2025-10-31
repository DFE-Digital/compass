using System.ComponentModel.DataAnnotations;

namespace Compass.Models;

public class SurveyOption
{
	[Key]
	public Guid SurveyOptionId { get; set; } = Guid.NewGuid();

	[Required]
	public Guid SurveyQuestionId { get; set; }

	public SurveyQuestion? Question { get; set; }

	[Required]
	[StringLength(100)]
	public string Value { get; set; } = string.Empty;

	[Required]
	[StringLength(200)]
	public string Label { get; set; } = string.Empty;

	public int Ordinal { get; set; }

	public int? Score { get; set; } // Optional score for this option

	public bool Active { get; set; } = true;
}


