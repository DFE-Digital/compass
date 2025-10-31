using System.ComponentModel.DataAnnotations;

namespace Compass.Models;

public class SurveyInstance
{
	[Key]
	public Guid SurveyInstanceId { get; set; } = Guid.NewGuid();

	public int ServiceId { get; set; }
	public FipsService? Service { get; set; }

	public Guid SurveyTemplateId { get; set; }
	public SurveyTemplate? Template { get; set; }

	public DateTime StartUtc { get; set; }
	public DateTime? EndUtc { get; set; }

	public bool IsActive { get; set; }

	// optional weights override: { "Q1":60, ... }
	public string? WeightsJson { get; set; }

	public ICollection<SurveyResponse> Responses { get; set; } = new List<SurveyResponse>();
}


