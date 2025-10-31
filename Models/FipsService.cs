using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

[Table("Service")]
public class FipsService
{
	[Key]
	public int ServiceId { get; set; }

	[Required]
	[StringLength(200)]
	public string FipsId { get; set; } = string.Empty; // unique

	[StringLength(300)]
	public string? DisplayName { get; set; }

	public bool IsActive { get; set; } = true;

	public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
	public DateTime? UpdatedUtc { get; set; }

	public ICollection<SurveyInstance> SurveyInstances { get; set; } = new List<SurveyInstance>();
}


