using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

/// <summary>
/// Represents a training course available in the course library
/// </summary>
public class TrainingCourse
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(255)]
    public string Title { get; set; } = string.Empty;

    [StringLength(255)]
    public string? Provider { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? Description { get; set; }

    [StringLength(50)]
    public string? Format { get; set; } // Online / blended / in-person

    [StringLength(50)]
    public string? Mode { get; set; } // Live / async / cohort

    [StringLength(100)]
    public string? Duration { get; set; } // Hours or days

    [Column(TypeName = "decimal(18,2)")]
    public decimal? Cost { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? ProfessionTags { get; set; } // JSON array or comma-separated

    [Column(TypeName = "nvarchar(max)")]
    public string? CapabilityTags { get; set; } // JSON array or comma-separated

    public bool Active { get; set; } = true; // Soft-delete flag

    [StringLength(500)]
    public string? Url { get; set; } // Link to course info

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [StringLength(255)]
    public string? CreatedBy { get; set; }

    [StringLength(255)]
    public string? UpdatedBy { get; set; }

    // Navigation properties
    public ICollection<TrainingRecord> TrainingRecords { get; set; } = new List<TrainingRecord>();
    public ICollection<TrainingRequest> TrainingRequests { get; set; } = new List<TrainingRequest>();
}

