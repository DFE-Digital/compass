using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models.DemandPipeline;

/// <summary>Risk or issue logged against a pipeline demand (editable in any stage).</summary>
[Table("DemandPipelineRiskIssues")]
public class DemandPipelineRiskIssue
{
    public Guid Id { get; set; }

    public Guid DemandPipelineRequestId { get; set; }

    /// <summary><c>Risk</c> or <c>Issue</c>.</summary>
    [Required]
    [MaxLength(20)]
    public string EntryType { get; set; } = "Risk";

    [Required]
    [MaxLength(500)]
    public string Title { get; set; } = string.Empty;

    public string? Details { get; set; }

    public string? Description { get; set; }
    public string? ImpactOnDelivery { get; set; }

    [MaxLength(20)]
    public string? Priority { get; set; }

    [MaxLength(20)]
    public string? Tier { get; set; }

    public int? DirectorateId { get; set; }

    public int? OwnerUserId { get; set; }

    public DateTime? TargetResolutionDate { get; set; }

    public string? MitigationOrAction { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    [MaxLength(450)]
    public string? CreatedBy { get; set; }

    [MaxLength(450)]
    public string? UpdatedBy { get; set; }

    [ForeignKey(nameof(DemandPipelineRequestId))]
    public DemandPipelineRequest? DemandPipelineRequest { get; set; }
}
