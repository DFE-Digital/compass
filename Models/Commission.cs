using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

/// <summary>
/// Represents a commission reporting period with a custom timeframe.
/// Supports both date ranges and quarter-based periods (e.g., Q1 April to July 2025).
/// </summary>
public class Commission
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty; // e.g., "Q1 2025", "October-December 2025"

    [StringLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// The start date of the commission period.
    /// </summary>
    [Required]
    public DateTime StartDate { get; set; }

    /// <summary>
    /// The end date of the commission period.
    /// </summary>
    [Required]
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Optional quarter identifier (e.g., "Q1", "Q2", "Q3", "Q4").
    /// If set, represents a quarter-based commission period.
    /// </summary>
    [StringLength(10)]
    public string? Quarter { get; set; }

    /// <summary>
    /// The date when the commission becomes available for completion.
    /// </summary>
    [Required]
    public DateTime OpenDate { get; set; }

    /// <summary>
    /// The due date by which the commission must be submitted.
    /// </summary>
    [Required]
    public DateTime DueDate { get; set; }

    /// <summary>
    /// Whether this commission is currently active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Comma-separated product phases that are in scope for this commission (matches <see cref="ProductDto.Phase"/>).
    /// When null or empty, any phase is allowed (subject to global catalogue rules such as excluding decommissioned products).
    /// </summary>
    [StringLength(2000)]
    public string? InScopePhases { get; set; }

    /// <summary>
    /// Comma-separated product &quot;Type&quot; category values that are in scope.
    /// When null or empty, any type is allowed (subject to global rules such as data-only exclusions).
    /// </summary>
    [StringLength(2000)]
    public string? InScopeTypes { get; set; }

    /// <summary>
    /// Comma-separated <see cref="PerformanceMetric"/> ids that may appear on returns for this commission.
    /// When null or empty, all enabled metrics valid for the commission period are candidates; each product still only sees metrics that match its phase/type and conditions.
    /// </summary>
    [StringLength(4000)]
    public string? IncludedPerformanceMetricIds { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [StringLength(255)]
    public string CreatedBy { get; set; } = string.Empty;

    [StringLength(255)]
    public string UpdatedBy { get; set; } = string.Empty;

    // Navigation properties
    public ICollection<CommissionSubmission> Submissions { get; set; } = new List<CommissionSubmission>();
}

/// <summary>
/// Represents a commission submission for a specific product.
/// Similar to ProductReturn but for commission reporting periods.
/// </summary>
public class CommissionSubmission
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int CommissionId { get; set; }

    [Required]
    [StringLength(100)]
    public string ProductDocumentId { get; set; } = string.Empty; // Product DocumentID from CMS

    [StringLength(50)]
    public string? FipsId { get; set; } // Product FIPS ID (legacy, kept for backwards compatibility)

    [Required]
    [StringLength(200)]
    public string ProductTitle { get; set; } = string.Empty; // Denormalized for performance

    [Required]
    public CommissionSubmissionStatus Status { get; set; } = CommissionSubmissionStatus.NotStarted;

    public DateTime? SubmittedDate { get; set; }

    [StringLength(255)]
    public string? SubmittedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Optional comments or notes regarding the metrics, such as how they were calculated,
    /// or whether timescales are in hours, days, weeks, etc.
    /// </summary>
    [StringLength(2000)]
    public string? Comments { get; set; }

    // Navigation properties
    [ForeignKey("CommissionId")]
    public Commission? Commission { get; set; }

    public ICollection<CommissionMetricValue> MetricValues { get; set; } = new List<CommissionMetricValue>();
}

/// <summary>
/// Status of a commission submission.
/// </summary>
public enum CommissionSubmissionStatus
{
    NotStarted = 0,
    InProgress = 1,
    Submitted = 2,
    Late = 3
}

/// <summary>
/// Stores metric values for a commission submission.
/// Similar to ProductMetricValue but for commission reporting.
/// </summary>
public class CommissionMetricValue
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int CommissionSubmissionId { get; set; }

    [Required]
    public int PerformanceMetricId { get; set; }

    public string? Value { get; set; }

    public bool IsComplete { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public bool IsNotCaptured { get; set; } = false;
    
    public string? NotCapturedReason { get; set; }
    
    public string? ReasonForDifference { get; set; }

    // Navigation properties
    [ForeignKey("CommissionSubmissionId")]
    public CommissionSubmission? CommissionSubmission { get; set; }

    [ForeignKey("PerformanceMetricId")]
    public PerformanceMetric? PerformanceMetric { get; set; }
}
