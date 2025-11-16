using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Compass.ViewModels;

public class ActionDetailsUpdateInputModel
{
    [Required]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string? Title { get; set; }

    [MaxLength(50)]
    public string? Status { get; set; }

    [MaxLength(10)]
    public string? Priority { get; set; }

    [MaxLength(255)]
    public string? AssignedToEmail { get; set; }

    [MaxLength(200)]
    public string? BusinessArea { get; set; }

    public int? ObjectiveId { get; set; }

    public int? ActionSourceId { get; set; }

    public int? ParentActionId { get; set; }

    public string? FipsId { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? DueDate { get; set; }

    public DateTime? CompletedDate { get; set; }

    public string? Description { get; set; }

    public string? Notes { get; set; }

    [MaxLength(500)]
    public string? EvidenceUrl { get; set; }

    public string? SourceType { get; set; }

    public string? SourceReference { get; set; }

    public string? SourceRecordUrl { get; set; }

    public int? DecisionId { get; set; }

    public IList<int> SelectedRiskIds { get; set; } = new List<int>();

    public IList<int> SelectedIssueIds { get; set; } = new List<int>();

    public IList<int> SelectedMilestoneIds { get; set; } = new List<int>();
}
