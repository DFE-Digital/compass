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
    public string? Mode { get; set; } // Live / async / cohort (kept for backward compatibility)

    [StringLength(100)]
    public string? Duration { get; set; } // Hours or days

    [Column(TypeName = "nvarchar(max)")]
    public string? Prerequisites { get; set; } // Prerequisites for the course

    [Column(TypeName = "nvarchar(max)")]
    public string? Location { get; set; } // List of locations for in-person courses (comma-separated or newline-separated)

    [Column(TypeName = "decimal(18,2)")]
    public decimal? Cost { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? ProfessionTags { get; set; } // Legacy field - kept for backward compatibility

    /// <summary>
    /// Primary professions - where this skill is specifically required
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? PrimaryProfessionTags { get; set; } // Comma-separated profession names

    /// <summary>
    /// Secondary professions - useful for additional awareness training or career progression
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? SecondaryProfessionTags { get; set; } // Comma-separated profession names

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

