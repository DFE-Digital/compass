using System.ComponentModel.DataAnnotations;

namespace Compass.Models;

public class ResponseScale
{
	[Key]
	public Guid ResponseScaleId { get; set; } = Guid.NewGuid();

	[Required]
	[StringLength(100)]
	public string Name { get; set; } = string.Empty; // e.g., "Yes/No", "Likert (Difficulty)", "Likert (Capacity)"

	[StringLength(500)]
	public string? Description { get; set; }

	[Required]
	public SurveyInputType InputType { get; set; } // Likert_1_5, YesNo, Select, Text

	public bool IsDefault { get; set; }

	public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

	public DateTime? UpdatedUtc { get; set; }

	public ICollection<ResponseScaleOption> Options { get; set; } = new List<ResponseScaleOption>();
}

