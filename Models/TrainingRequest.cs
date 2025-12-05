using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

/// <summary>
/// Represents a training request submitted by a user
/// </summary>
public class TrainingRequest
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    public int? CourseId { get; set; } // Selected course OR null for custom

    [StringLength(255)]
    public string? CustomCourseTitle { get; set; } // Free-text course name

    [Column(TypeName = "nvarchar(max)")]
    public string? Justification { get; set; } // Why training is needed

    [Column(TypeName = "nvarchar(max)")]
    public string? ProfessionAlignment { get; set; } // Link to capability gap

    [Required]
    [StringLength(50)]
    public string Status { get; set; } = "Draft"; // Draft / submitted / approved / rejected / on-hold

    public int? DecisionId { get; set; } // Optional Compass decision link

    [Column(TypeName = "nvarchar(max)")]
    public string? ApproverComments { get; set; }

    [StringLength(255)]
    public string? ApprovedBy { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("UserId")]
    public User? User { get; set; }

    [ForeignKey("CourseId")]
    public TrainingCourse? Course { get; set; }

    [ForeignKey("DecisionId")]
    public Decision? Decision { get; set; }
}

