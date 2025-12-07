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

    [StringLength(255)]
    public string? CustomCourseProvider { get; set; } // Provider for custom course

    [Column(TypeName = "decimal(18,2)")]
    public decimal? CustomCourseCost { get; set; } // Cost for custom course

    [StringLength(500)]
    public string? CustomCourseUrl { get; set; } // Link to custom course information

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

    /// <summary>
    /// Actual cost incurred (may differ from course cost due to discounts/bulk pricing)
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal? ActualCost { get; set; }

    /// <summary>
    /// Planned date for the training to take place
    /// </summary>
    public DateTime? PlannedDate { get; set; }

    /// <summary>
    /// Whether the training actually took place
    /// </summary>
    public bool? TrainingCompleted { get; set; }

    /// <summary>
    /// Actual date the training was completed (if different from planned date)
    /// </summary>
    public DateTime? CompletedDate { get; set; }

    /// <summary>
    /// Financial year this training is allocated to (calculated from PlannedDate if not set)
    /// UK financial year starts 1st April
    /// </summary>
    public int? FinancialYear { get; set; }

    /// <summary>
    /// Date the payment was made
    /// </summary>
    public DateTime? PaymentDate { get; set; }

    /// <summary>
    /// Payment method used (PO, Card, Other)
    /// </summary>
    [StringLength(50)]
    public string? PaymentMethod { get; set; }

    /// <summary>
    /// Payment reference (e.g., PO Number, transaction reference)
    /// </summary>
    [StringLength(255)]
    public string? PaymentReference { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether a withdrawal has been requested for this training request
    /// </summary>
    public bool WithdrawalRequested { get; set; }

    /// <summary>
    /// Reason for withdrawal request
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? WithdrawalReason { get; set; }

    /// <summary>
    /// Date withdrawal was requested
    /// </summary>
    public DateTime? WithdrawalRequestedAt { get; set; }

    /// <summary>
    /// User ID to transfer this request to (if transfer is requested)
    /// </summary>
    public int? TransferToUserId { get; set; }

    // Navigation properties
    [ForeignKey("UserId")]
    public User? User { get; set; }

    [ForeignKey("CourseId")]
    public TrainingCourse? Course { get; set; }

    [ForeignKey("DecisionId")]
    public Decision? Decision { get; set; }

    [ForeignKey("TransferToUserId")]
    public User? TransferToUser { get; set; }
}

