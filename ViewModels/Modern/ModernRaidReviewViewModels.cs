namespace Compass.ViewModels.Modern;

public sealed class ModernRaidReviewSetupViewModel
{
    public IReadOnlyList<RaidLookupOptionVm> BusinessAreaOptions { get; init; } = Array.Empty<RaidLookupOptionVm>();
    public IReadOnlyList<int> SuggestedBusinessAreaIds { get; init; } = Array.Empty<int>();
}

public sealed class ModernRaidReviewOverviewViewModel
{
    public string ReviewMonthLabel { get; init; } = "";
    public int ReviewYear { get; init; }
    public int ReviewMonth { get; init; }
    public string BusinessAreaQuery { get; init; } = "";
    public IReadOnlyList<string> BusinessAreaLabels { get; init; } = Array.Empty<string>();

    /// <summary>e.g. Friday 29 May</summary>
    public string ReviewDueByLabel { get; init; } = "";

    public int OpenRiskCount { get; init; }
    public int OpenIssueCount { get; init; }
    public int ReviewedThisMonthCount { get; init; }
    public bool HasStartedReview { get; init; }
    public int AttentionCount { get; init; }

    public IReadOnlyList<ModernRaidLabelCountVm> RisksByTier { get; init; } = Array.Empty<ModernRaidLabelCountVm>();
    public IReadOnlyList<ModernRaidReviewAttentionItemVm> AttentionItems { get; init; } = Array.Empty<ModernRaidReviewAttentionItemVm>();
}

public sealed class ModernRaidReviewAttentionItemVm
{
    public required string Kind { get; init; }
    public int Id { get; init; }
    public required string Reference { get; init; }
    public required string Title { get; init; }
    public required string Reason { get; init; }
    public string? DetailHref { get; init; }
}

public sealed class ModernRaidReviewWorkViewModel
{
    public string ReviewMonthLabel { get; init; } = "";
    public int ReviewYear { get; init; }
    public int ReviewMonth { get; init; }
    public string BusinessAreaQuery { get; init; } = "";
    public IReadOnlyList<string> BusinessAreaLabels { get; init; } = Array.Empty<string>();
    public IReadOnlyList<ModernRaidReviewWorkItemVm> PendingRisks { get; init; } = Array.Empty<ModernRaidReviewWorkItemVm>();
    public IReadOnlyList<ModernRaidReviewWorkItemVm> PendingIssues { get; init; } = Array.Empty<ModernRaidReviewWorkItemVm>();
    public IReadOnlyList<ModernRaidReviewWorkItemVm> ReviewedRisks { get; init; } = Array.Empty<ModernRaidReviewWorkItemVm>();
    public IReadOnlyList<ModernRaidReviewWorkItemVm> ReviewedIssues { get; init; } = Array.Empty<ModernRaidReviewWorkItemVm>();

    public int TotalCount =>
        PendingRisks.Count + PendingIssues.Count + ReviewedRisks.Count + ReviewedIssues.Count;

    public int ReviewedCount => ReviewedRisks.Count + ReviewedIssues.Count;

    public int RiskCount => PendingRisks.Count + ReviewedRisks.Count;

    public int IssueCount => PendingIssues.Count + ReviewedIssues.Count;

    /// <summary><c>risk</c> or <c>issue</c> — which record type the work list is filtered to.</summary>
    public string ActiveKind { get; init; } = "risk";

    public IReadOnlyList<RaidLookupOptionVm> RiskLikelihoodOptions { get; init; } = Array.Empty<RaidLookupOptionVm>();
    public IReadOnlyList<RaidLookupOptionVm> RiskImpactOptions { get; init; } = Array.Empty<RaidLookupOptionVm>();
    public IReadOnlyList<RaidLookupOptionVm> RiskPriorityOptions { get; init; } = Array.Empty<RaidLookupOptionVm>();
    public IReadOnlyList<RaidLookupOptionVm> IssueSeverityOptions { get; init; } = Array.Empty<RaidLookupOptionVm>();
    public IReadOnlyList<RaidLookupOptionVm> IssuePriorityOptions { get; init; } = Array.Empty<RaidLookupOptionVm>();
}

public sealed class ModernRaidReviewWorkItemVm
{
    public required string Kind { get; init; }
    public int Id { get; init; }
    public required string Reference { get; init; }
    public required string Title { get; init; }
    public string? SeverityOrPriorityLabel { get; init; }
    public string? LikelihoodLabel { get; init; }
    public string? ImpactLabel { get; init; }
    public int? RiskScore { get; init; }
    public string? Tier { get; init; }
    public DateTime OpenedDate { get; init; }
    public string? Owner { get; init; }
    public DateTime UpdatedAt { get; init; }
    public string? UpdatedBy { get; init; }
    public string? Description { get; init; }
    public string DetailHref { get; init; } = "#";
    public bool ReviewedThisMonth { get; init; }
    public DateTime? ReviewedAtUtc { get; init; }
    public string? ExistingMonthlyComment { get; init; }
    public int? RiskLikelihoodId { get; init; }
    public int? RiskImpactLevelId { get; init; }
    public int? RiskPriorityId { get; init; }
    public int? IssueSeverityId { get; init; }
    public int? IssuePriorityId { get; init; }
    public int? IssueSeveritySortOrder { get; init; }
    public int? IssuePrioritySortOrder { get; init; }
    public IReadOnlyList<RiskAuditTimelineItemVm> Timeline { get; init; } = Array.Empty<RiskAuditTimelineItemVm>();
}

public sealed class ModernRaidReviewWorkItemRowViewModel
{
    public required ModernRaidReviewWorkItemVm Item { get; init; }
    public required ModernRaidReviewWorkViewModel Page { get; init; }
}

public sealed class ModernRaidReviewSaveRiskForm
{
    public int RiskId { get; set; }
    public string? Ba { get; set; }
    public int? RiskLikelihoodId { get; set; }
    public int? RiskImpactLevelId { get; set; }
    public int? RiskPriorityId { get; set; }
    public string? MonthlyComment { get; set; }
}

public sealed class ModernRaidReviewSaveIssueForm
{
    public int IssueId { get; set; }
    public string? Ba { get; set; }
    public int? IssueSeverityId { get; set; }
    public int? IssuePriorityId { get; set; }
    public string? MonthlyComment { get; set; }
}

public sealed class ModernRaidReviewCloseForm
{
    public string? Ba { get; set; }
    public string? ClosureComment { get; set; }
}
