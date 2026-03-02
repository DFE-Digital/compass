using System;
using System.Collections.Generic;
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

    [Required]
    public int? StatusId { get; set; }

    [EmailAddress]
    [MaxLength(255)]
    public string? AssignedToEmail { get; set; }

    public int? AssignedToUserId { get; set; }

    [MaxLength(255)]
    public string? AssignedToName { get; set; }

    [MaxLength(10)]
    public string? Priority { get; set; }

    [Required]
    public int? PriorityId { get; set; }

    public int? ActionTypeId { get; set; }

    public int? CategoryId { get; set; }

    public int? ImpactLevelId { get; set; }

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

    public int? EvidenceTypeId { get; set; }

    [MaxLength(250)]
    public string? EvidenceSummary { get; set; }

    public bool VerificationRequired { get; set; }

    [MaxLength(500)]
    public string? VerificationNotes { get; set; }

    [Range(0, 100, ErrorMessage = "Enter a value between 0 and 100.")]
    public int ProgressPercent { get; set; }

    public bool Blocked { get; set; }

    [MaxLength(500)]
    public string? BlockedReason { get; set; }

    [MaxLength(15)]
    public string? RagRating { get; set; }

    [MaxLength(1000)]
    public string? ClosureNotes { get; set; }

    [MaxLength(150)]
    public string? TeamName { get; set; }

    public int? ReminderFrequencyId { get; set; }

    public int? EscalationThresholdId { get; set; }

    public bool EscalationTriggered { get; set; }

    public string? Tags { get; set; }

    public int? DecisionId { get; set; }

    public List<int> LinkedDecisionIds { get; set; } = new();

    public int? ParentActionId { get; set; }

    public List<int> LinkedRiskIds { get; set; } = new();

    public List<int> LinkedIssueIds { get; set; } = new();

    public string? InitiatingEntityType { get; set; }

    public int? InitiatingEntityId { get; set; }
}
