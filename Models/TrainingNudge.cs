using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

/// <summary>
/// Represents a training recommendation/nudge shown to a user based on capability gaps
/// </summary>
public class TrainingNudge
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    public int CourseId { get; set; }

    [Required]
    [StringLength(100)]
    public string Reason { get; set; } = string.Empty; // e.g., "Capability gap", "Profession alignment", "Recommended"

    [Column(TypeName = "nvarchar(max)")]
    public string? CapabilityGap { get; set; } // The specific gap this addresses

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? DismissedAt { get; set; }

    public DateTime? AcceptedAt { get; set; }

    public bool IsActive { get; set; } = true; // False if dismissed or accepted

    // Navigation properties
    [ForeignKey("UserId")]
    public User? User { get; set; }

    [ForeignKey("CourseId")]
    public TrainingCourse? Course { get; set; }
}

