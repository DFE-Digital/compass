using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Compass.ViewModels;

public class ProjectDecisionInputModel
{
    [Required]
    public int ProjectId { get; set; }

    public int? DecisionId { get; set; }
    
    public int? SourceMilestoneId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Status { get; set; } = "pending";

    public string? Summary { get; set; }

    [MaxLength(50)]
    public string? DecisionType { get; set; }

    public DateTime? DecisionDate { get; set; }

    [MaxLength(100)]
    public string? BusinessArea { get; set; }

    public string? Outcome { get; set; }

    public string? Notes { get; set; }

    [EmailAddress]
    public string? OwnerEmail { get; set; }

    [MaxLength(50)]
    public string? FipsId { get; set; }

    [MaxLength(100)]
    public string? SourceType { get; set; }

    [MaxLength(100)]
    public string? SourceReference { get; set; }

    [Url]
    [MaxLength(500)]
    public string? SourceRecordUrl { get; set; }

    public List<int> LinkedRiskIds { get; set; } = new();

    public List<int> LinkedIssueIds { get; set; } = new();

    public List<int> LinkedActionIds { get; set; } = new();
}
