using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

public class ResponseScaleOption
{
	[Key]
	public Guid ResponseScaleOptionId { get; set; } = Guid.NewGuid();

	[Required]
	public Guid ResponseScaleId { get; set; }

	[ForeignKey(nameof(ResponseScaleId))]
	public ResponseScale? Scale { get; set; }

	[Required]
	[StringLength(100)]
	public string Value { get; set; } = string.Empty; // e.g., "1", "2", "yes", "no"

	[Required]
	[StringLength(200)]
	public string Label { get; set; } = string.Empty; // e.g., "Very easy", "Very difficult", "Yes", "No"

	public int Ordinal { get; set; }

	public bool Active { get; set; } = true;
}

