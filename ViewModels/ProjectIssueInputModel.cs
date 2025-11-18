using System.ComponentModel.DataAnnotations;

namespace Compass.ViewModels;

public class ProjectIssueInputModel
{
    [Required]
    public int ProjectId { get; set; }

    public int? IssueId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    [MaxLength(10)]
    public string Severity { get; set; } = "medium";

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "open";

    [DataType(DataType.Date)]
    public DateTime DetectedDate { get; set; } = DateTime.Today;

    [DataType(DataType.Date)]
    public DateTime? TargetResolutionDate { get; set; }

    [MaxLength(100)]
    public string? Category { get; set; }

    public string? Workaround { get; set; }

    public string? ResolutionSummary { get; set; }

    [MaxLength(50)]
    public string? FipsId { get; set; }

    [MaxLength(100)]
    public string? BusinessArea { get; set; }

    [MaxLength(100)]
    public string? SourceType { get; set; }

    [MaxLength(100)]
    public string? SourceReference { get; set; }

    [MaxLength(500)]
    [Url]
    public string? SourceRecordUrl { get; set; }

    [EmailAddress]
    [MaxLength(255)]
    public string? OwnerEmail { get; set; }

    public int? OwnerUserId { get; set; }

    [MaxLength(255)]
    public string? OwnerName { get; set; }

    public int? SourceRiskId { get; set; }

    public List<int> LinkedRiskIds { get; set; } = new();

    public List<int> LinkedActionIds { get; set; } = new();

    public List<int> LinkedDecisionIds { get; set; } = new();

    public string? InitiatingRiskContext { get; set; }
}
