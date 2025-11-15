using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

public class ScoreSnapshot
{
	[Key]
	public Guid ScoreSnapshotId { get; set; } = Guid.NewGuid();

	public DateOnly SnapshotDate { get; set; }

	public int ServiceId { get; set; }
	public FipsService? Service { get; set; }

	public int ResponsesCount { get; set; }

	[Column(TypeName = "decimal(18,2)")]
	public decimal AvgUss { get; set; }

	[Column(TypeName = "decimal(18,2)")]
	public decimal MedianUss { get; set; }
}


