using System.ComponentModel.DataAnnotations;

namespace Compass.ViewModels;

public class ProjectActionInputModel
{
    [Required]
    public int ProjectId { get; set; }

    public int? ActionId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "not_started";

    [EmailAddress]
    [MaxLength(255)]
    public string? AssignedToEmail { get; set; }

    [MaxLength(10)]
    public string? Priority { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? DueDate { get; set; }

    public DateTime? CompletedDate { get; set; }

    [MaxLength(50)]
    public string? FipsId { get; set; }

    [MaxLength(100)]
    public string? BusinessArea { get; set; }

    public int? ActionSourceId { get; set; }

    [MaxLength(100)]
    public string? SourceType { get; set; }

    [MaxLength(100)]
    public string? SourceReference { get; set; }

    [MaxLength(500)]
    [Url]
    public string? SourceRecordUrl { get; set; }

    public string? Notes { get; set; }

    [MaxLength(500)]
    [Url]
    public string? EvidenceUrl { get; set; }

    public int? DecisionId { get; set; }

    public int? ParentActionId { get; set; }

    public List<int> LinkedRiskIds { get; set; } = new();

    public List<int> LinkedIssueIds { get; set; } = new();

    public string? InitiatingEntityType { get; set; }

    public int? InitiatingEntityId { get; set; }
}
