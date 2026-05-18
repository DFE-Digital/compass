using System.ComponentModel.DataAnnotations;
using Compass.Models;

namespace Compass.Models.Modern.Work;

/// <summary>Named lookup line for risk/issue tier and similar dropdowns in modern work views.</summary>
public class RiskIssueNamedIntOption
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional supporting text (e.g. <see cref="Compass.Models.RaidLookupBase.Description"/>).</summary>
    public string? Description { get; set; }

    /// <summary>For risk likelihood / impact lookups: admin matrix weight (1–5).</summary>
    public int? MatrixScore { get; set; }
}

/// <summary>Form POST body for modern work Log issue / Log risk pages.</summary>
public class ModernWorkLogRaidForm
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? ImpactOnDelivery { get; set; }
    public string? Priority { get; set; }
    public string? Tier { get; set; }
    public int? DirectorateId { get; set; }
    public int? OwnerUserId { get; set; }
    public DateTime? TargetResolutionDate { get; set; }
    public string? MitigationOrAction { get; set; }
    public int? LinkedMilestoneId { get; set; }

    /// <summary>RaidLookupBase likelihood row id.</summary>
    public int? RiskLikelihoodId { get; set; }
    /// <summary>RaidLookupBase impact level row id.</summary>
    public int? RiskImpactLevelId { get; set; }
    public int? RiskProximityId { get; set; }
    public int? RiskCategoryId { get; set; }

    /// <summary>Risk priority lookup (<see cref="Compass.Models.RiskPriority"/>).</summary>
    public int? RiskPriorityLookupId { get; set; }

    public int? IssueSeverityId { get; set; }
    /// <summary>Maps to <see cref="Compass.Models.IssuePriority"/>.</summary>
    public int? IssuePriorityLookupId { get; set; }
    public int? IssueStatusLookupId { get; set; }
    public int? IssueCategoryId { get; set; }
}

/// <summary>Risk or issue row for ported modern work views (maps to Compass <see cref="Risk"/> / <see cref="Issue"/> in a later iteration).</summary>
public class WorkItemRiskOrIssue
{
    public int Id { get; set; }
    public int WorkItemId { get; set; }
    public int ReferenceNumber { get; set; }

    [Required, MaxLength(20)]
    public string Type { get; set; } = "Risk";

    [Required, MaxLength(500)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }
    public string? HowIdentified { get; set; }
    public string? Cause { get; set; }
    [MaxLength(100)]
    public string? Tier { get; set; }
    [MaxLength(50)]
    public string? Priority { get; set; }
    public int? RiskLikelihoodLookupId { get; set; }
    public WorkLookupOption? RiskLikelihoodLookup { get; set; }
    public int? RiskImpactLookupId { get; set; }
    public WorkLookupOption? RiskImpactLookup { get; set; }
    public int? RiskProximityLookupId { get; set; }
    public int? RiskCategoryLookupId { get; set; }
    public int? RiskPriorityLookupId { get; set; }

    public int? IssueSeverityLookupId { get; set; }
    public int? IssuePriorityLookupId { get; set; }
    public string? IssueSeverityLabel { get; set; }
    public string? IssuePriorityLabel { get; set; }
    public int? IssueStatusLookupId { get; set; }
    public int? IssueCategoryLookupId { get; set; }

    [Required, MaxLength(50)]
    public string Status { get; set; } = "Open";

    public string? ImpactOnDelivery { get; set; }
    public string? MitigationOrAction { get; set; }
    /// <summary>Inherent/computed score from the core risk row when Type is Risk.</summary>
    public int? RiskScore { get; set; }
    public string? LikelihoodLabel { get; set; }
    public string? ImpactLabel { get; set; }
    [MaxLength(50)]
    public string? Likelihood { get; set; }
    public int? ImpactLevel { get; set; }
    public int? LikelihoodLevel { get; set; }
    public string? Proximity { get; set; }
    public DateTime? TargetResolutionDate { get; set; }
    public DateTime? NextBoardReviewAt { get; set; }
    [MaxLength(500)]
    public string? EscalatedTo { get; set; }

    public int? LinkedMilestoneId { get; set; }
    /// <summary>Linked milestone for display (maps from <see cref="LinkedMilestoneId"/>).</summary>
    public Milestone? LinkedMilestone { get; set; }
    public int? MaterialisedFromRiskOrIssueId { get; set; }
    /// <summary>When this issue was created from a risk, the source risk row.</summary>
    public WorkItemRiskOrIssue? MaterialisedFrom { get; set; }
    /// <summary>Directorate scope for this risk/issue (UI).</summary>
    public Compass.Models.Directorate? Directorate { get; set; }
    public int? OwnerUserId { get; set; }
    public int? RaisedByUserId { get; set; }
    public WorkAppUser? RaisedByUser { get; set; }

    [MaxLength(50)]
    public string? RelatesToType { get; set; }
    public Guid? RelatesToBusinessCaseId { get; set; }
    public Guid? RelatesToDemandRequestId { get; set; }
    public int? RelatesToWorkItemId { get; set; }
    [MaxLength(200)]
    public string? RelatesToFipsProductId { get; set; }
    public int? RelatesToGovernmentDepartmentId { get; set; }
    public int? RelatesToDirectorateId { get; set; }
    public bool RelatesToDdtAll { get; set; }
    public int? RelatesToPortfolioId { get; set; }
    public int? ManagingPortfolioId { get; set; }
    public int? DirectorateId { get; set; }
    /// <summary>From junction / work item or legacy string; RAID register “Business area” column.</summary>
    public string? BusinessAreaLabel { get; set; }
    public string? RelatesToOtherDetails { get; set; }
    public Guid? CreationIdempotencyKey { get; set; }

    public DateTime RaisedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public string? ClosureOutcome { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    [MaxLength(450)]
    public string? CreatedBy { get; set; }
    [MaxLength(450)]
    public string? UpdatedBy { get; set; }

    public WorkItem? WorkItem { get; set; }
    public WorkAppUser? OwnerUser { get; set; }
    public ICollection<WorkItemKri> Kris { get; set; } = new List<WorkItemKri>();
    public ICollection<WorkItemRiskOrIssue> MaterialisedAsIssues { get; set; } = new List<WorkItemRiskOrIssue>();
}

public class WorkItemKri
{
    public int Id { get; set; }
    public int WorkItemRiskOrIssueId { get; set; }
    public int ReferenceNumber { get; set; }
    [Required, MaxLength(500)]
    public string Name { get; set; } = string.Empty;
    public string? WhatIsMeasured { get; set; }
    [MaxLength(500)]
    public string? ThresholdDescription { get; set; }
    public DateTime? ThresholdDate { get; set; }
    [MaxLength(200)]
    public string? CurrentValue { get; set; }
    [Required, MaxLength(20)]
    public string Status { get; set; } = "OnTrack";
    [MaxLength(100)]
    public string? MeasurementFrequency { get; set; }
    public int? ResponsibleUserId { get; set; }
    public string? BreachAction { get; set; }
    public DateTime? NextReviewDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? SetByUserId { get; set; }
    public WorkItemRiskOrIssue WorkItemRiskOrIssue { get; set; } = null!;
    public WorkAppUser? ResponsibleUser { get; set; }
    public ICollection<WorkItemKriUpdate> Updates { get; set; } = new List<WorkItemKriUpdate>();
}

public class WorkItemKriUpdate
{
    public int Id { get; set; }
    public int WorkItemKriId { get; set; }
    [MaxLength(200)]
    public string? MeasuredValue { get; set; }
    [Required, MaxLength(20)]
    public string Status { get; set; } = "OnTrack";
    public string? ProximityUpdate { get; set; }
    public string? ActionsTaken { get; set; }
    public DateTime? NextReviewDate { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int? UpdatedByUserId { get; set; }
    public WorkItemKri WorkItemKri { get; set; } = null!;
}

public class WorkItemTeamMember
{
    public int Id { get; set; }
    public int WorkItemId { get; set; }
    public int AppUserId { get; set; }
    [MaxLength(200)]
    public string? Role { get; set; }
    public int? FundingOptionId { get; set; }
    public int? TimeAllocationPct { get; set; }
    public int? TypeOptionId { get; set; }
    [MaxLength(50)]
    public string TeamStatus { get; set; } = "active";
    public WorkItem WorkItem { get; set; } = null!;
    public WorkAppUser AppUser { get; set; } = null!;
}


public class SetPrimaryContactViewModel
{
    public int WorkItemId { get; set; }
    public int AppUserId { get; set; }
}

public class SetBudgetOwnerViewModel
{
    public int WorkItemId { get; set; }
    public bool SameAsSro { get; set; }
    public int AppUserId { get; set; }
}

public class WorkPortfoliosViewModel
{
    public int ActivePortfolioCount { get; set; }
    public int TotalWorkItems { get; set; }
    public int RedWorkCount { get; set; }
    public int MonthlyCompliancePercent { get; set; }
    public string LedePeriodLabel { get; set; } = "";
    public IReadOnlyList<WorkPortfolioRow> Rows { get; set; } = Array.Empty<WorkPortfolioRow>();
}

public class WorkPortfolioRow
{
    public int PortfolioId { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string DirectorateDisplay { get; set; } = "—";
    public string? HeadOfPortfolioDisplay { get; set; }
    public int WorkItemCount { get; set; }
    public int RagGreen { get; set; }
    public int RagAmber { get; set; }
    public int RagRed { get; set; }
    public string PortfolioRagClass { get; set; } = "dfe-c-rag--tbc";
    public string PortfolioRagLabel { get; set; } = "—";
    public int MonthlyCompliancePercent { get; set; }
    public string MonthlyComplianceCssVar { get; set; } = "var(--rag-g)";
    public int OpenRisksCount { get; set; }
}
