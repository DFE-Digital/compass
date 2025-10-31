using System.ComponentModel.DataAnnotations;

namespace Compass.Models;

public class SurveyResponse
{
	[Key]
	public Guid SurveyResponseId { get; set; } = Guid.NewGuid();

	public Guid SurveyInstanceId { get; set; }
	public SurveyInstance? SurveyInstance { get; set; }

	public DateTime SubmittedUtc { get; set; } = DateTime.UtcNow;

	[StringLength(50)]
	public string? Channel { get; set; }

	[StringLength(200)]
	public string? UserAgentHash { get; set; }

	[StringLength(100)]
	public string? GeoRegion { get; set; }

	[StringLength(2000)]
	public string? FreeText { get; set; }

	public decimal UssComputed { get; set; }

	[StringLength(10)]
	public string? Band { get; set; } // Red/Amber/Green

	public ICollection<ResponseAnswer> Answers { get; set; } = new List<ResponseAnswer>();
}


