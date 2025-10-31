using System.ComponentModel.DataAnnotations;

namespace Compass.Models;

public class ScoreSnapshot
{
	[Key]
	public Guid ScoreSnapshotId { get; set; } = Guid.NewGuid();

	public DateOnly SnapshotDate { get; set; }

	public int ServiceId { get; set; }
	public FipsService? Service { get; set; }

	public int ResponsesCount { get; set; }
	public decimal AvgUss { get; set; }
	public decimal MedianUss { get; set; }
}


