using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

/// <summary>
/// Represents a training record for a user who has attended or booked training
/// </summary>
public class TrainingRecord
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    public int? CourseId { get; set; } // Nullable for custom courses

    [Required]
    [StringLength(50)]
    public string Status { get; set; } = string.Empty; // Completed / booked / cancelled

    public DateTime? DateRequested { get; set; }

    public DateTime? DateApproved { get; set; }

    public DateTime? DateAttended { get; set; }

    [Range(1, 5)]
    public int? OutcomeRating { get; set; } // 1–5 score

    [Column(TypeName = "nvarchar(max)")]
    public string? Feedback { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? CostActual { get; set; } // Actual cost spent

    [StringLength(500)]
    public string? EvidenceFileUrl { get; set; } // Certificate/evidence

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("UserId")]
    public User? User { get; set; }

    [ForeignKey("CourseId")]
    public TrainingCourse? Course { get; set; }
}

