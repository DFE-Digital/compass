using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

public enum SurveyInputType
{
	Likert_1_5,
	Select,
	Text,
	YesNo
}

public class SurveyQuestion
{
	[Key]
	public Guid SurveyQuestionId { get; set; } = Guid.NewGuid();

	[Required]
	public Guid SurveyTemplateId { get; set; }

	[ForeignKey(nameof(SurveyTemplateId))]
	public SurveyTemplate? Template { get; set; }

	[Required]
	[StringLength(10)]
	public string Code { get; set; } = string.Empty; // e.g., Q1, Q2

	[Required]
	[StringLength(300)]
	public string Title { get; set; } = string.Empty;

	[StringLength(1000)]
	public string? Description { get; set; }

	public bool Mandatory { get; set; }

	public int Weight { get; set; } = 0;

	public int Ordinal { get; set; } = 0;

	public SurveyInputType InputType { get; set; } = SurveyInputType.Likert_1_5;

	public Guid? ResponseScaleId { get; set; }

	[ForeignKey(nameof(ResponseScaleId))]
	public ResponseScale? ResponseScale { get; set; }

	public bool Active { get; set; } = true;

	public ICollection<SurveyOption> Options { get; set; } = new List<SurveyOption>();
}


