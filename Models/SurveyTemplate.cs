using System.ComponentModel.DataAnnotations;

namespace Compass.Models;

public class SurveyTemplate
{
	[Key]
	public Guid SurveyTemplateId { get; set; } = Guid.NewGuid();

	[Required]
	[StringLength(200)]
	public string Name { get; set; } = string.Empty;

	public int Version { get; set; } = 1;

	public bool IsDefault { get; set; }

	public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

	public ICollection<SurveyQuestion> Questions { get; set; } = new List<SurveyQuestion>();
	public ICollection<JourneyStep> JourneySteps { get; set; } = new List<JourneyStep>();
}


