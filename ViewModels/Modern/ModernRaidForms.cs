using System.ComponentModel.DataAnnotations;

namespace Compass.ViewModels.Modern;

public sealed class ModernRaidCreateAssumptionForm
{
    /// <summary>UI: work, product, organisation.</summary>
    public string AssociationKind { get; set; } = "work";

    public int? ProjectId { get; set; }

    public int? PrimaryProductId { get; set; }

    public int? OwnerUserId { get; set; }

    public int? SroUserId { get; set; }

    [Required]
    [Display(Name = "Assumption")]
    public string Description { get; set; } = "";

    public int? AssumptionCriticalityId { get; set; }

    public int? AssumptionStatusId { get; set; }

    public int? ReviewDay { get; set; }
    public int? ReviewMonth { get; set; }
    public int? ReviewYear { get; set; }

    /// <summary>Edit only.</summary>
    public int? Id { get; set; }

    /// <summary>Multiple divisions (checkboxes).</summary>
    public List<int> DivisionIds { get; set; } = new();

    /// <summary>Multiple business areas from admin lookup (checkboxes).</summary>
    public List<int> BusinessAreaLookupIds { get; set; } = new();

    /// <summary>When set, successful create redirects back to this work item’s Assumptions tab.</summary>
    public int? ReturnToWorkItemId { get; set; }
}

public sealed class ModernRaidRiskEditorForm
{
    public int? Id { get; set; }

    /// <summary>UI: work, product, organisation. Empty on create until the user chooses.</summary>
    public string AssociationKind { get; set; } = "";

    public int? ProjectId { get; set; }

    public int? PrimaryProductId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = "";

    public string? Description { get; set; }

    /// <summary>Cause / drivers (distinct from description).</summary>
    public string? Cause { get; set; }

    /// <summary>Impact if the risk materialises.</summary>
    public string? ImpactIfRealised { get; set; }

    public string? Contingency { get; set; }

    public string? Assurance { get; set; }

    public string? FinancialImpact { get; set; }

    /// <summary>Key risk indicators (metric and threshold pairs).</summary>
    public List<RiskKriItemForm> KriItems { get; set; } = new();

    public int? RiskTierId { get; set; }
    public int? RiskStatusId { get; set; }
    public int? RiskPriorityId { get; set; }
    public int? RiskLikelihoodId { get; set; }
    public int? RiskImpactLevelId { get; set; }

    // Current risk rating
    public int? CurrentLikelihoodId { get; set; }
    public int? CurrentImpactLevelId { get; set; }

    public int? RiskProximityId { get; set; }
    public int? RiskTreatmentId { get; set; }

    // Residual risk rating
    public int? ResidualLikelihoodId { get; set; }
    public int? ResidualImpactLevelId { get; set; }

    // Tolerance risk rating
    public int? ToleranceLikelihoodId { get; set; }
    public int? ToleranceImpactLevelId { get; set; }

    /// <summary>Primary category (single selection).</summary>
    public int? PrimaryRiskCategoryId { get; set; }

    /// <summary>Optional second category, must differ from <see cref="PrimaryRiskCategoryId"/>.</summary>
    public int? SecondaryRiskCategoryId { get; set; }

    /// <summary>Legacy: populated when loading; prefer primary/secondary on save.</summary>
    public List<int> RiskCategoryIds { get; set; } = new();

    /// <summary>Multiple divisions (checkboxes).</summary>
    public List<int> DivisionIds { get; set; } = new();

    /// <summary>Multiple business areas from admin lookup (checkboxes).</summary>
    public List<int> BusinessAreaLookupIds { get; set; } = new();

    public int? OwnerUserId { get; set; }

    public int? SroUserId { get; set; }

    public int? IdentifiedDay { get; set; }
    public int? IdentifiedMonth { get; set; }
    public int? IdentifiedYear { get; set; }

    public int? NextReviewDay { get; set; }
    public int? NextReviewMonth { get; set; }
    public int? NextReviewYear { get; set; }

    public string? ResponseStrategy { get; set; }
}

public sealed class RiskKriItemForm
{
    public string? Metric { get; set; }

    public string? Threshold { get; set; }
}

public sealed class IssueAssuranceItemForm
{
    /// <summary>board, review, event (or short label).</summary>
    public string? EventKind { get; set; }

    public string? Title { get; set; }

    public int? EventDay { get; set; }
    public int? EventMonth { get; set; }
    public int? EventYear { get; set; }

    /// <summary>Decision taken or expected.</summary>
    public string? DecisionSummary { get; set; }
}

/// <summary>Create an issue from an existing risk (materialisation).</summary>
public sealed class ModernRaidMakeIssueFromRiskForm
{
    public int RiskId { get; set; }

    /// <summary><c>close</c> marks the risk materialised/closed; <c>keep_open</c> leaves the risk open.</summary>
    public string RiskAfterIssue { get; set; } = "close";

    public string? MaterialisationNote { get; set; }
}

public sealed class ModernRaidMakeIssueFromRiskPageVm
{
    public int RiskId { get; set; }
    public string RiskReference { get; init; } = "";
    public string Title { get; init; } = "";
    public string? Description { get; init; }
    public string? Cause { get; init; }
    public string? ImpactIfRealised { get; init; }
    public bool RiskIsClosed { get; init; }
    public bool HasExistingMaterialisedIssue { get; init; }
    public int? ExistingIssueId { get; init; }
    public string? ExistingIssueReference { get; init; }
}

public sealed class ModernRaidIssueEditorForm
{
    public int? Id { get; set; }

    /// <summary>UI: work, product, organisation.</summary>
    public string AssociationKind { get; set; } = "work";

    public int? ProjectId { get; set; }

    public int? PrimaryProductId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = "";

    public string? Description { get; set; }

    public int? StatusId { get; set; }
    public int? SeverityId { get; set; }
    public int? PriorityId { get; set; }

    /// <summary>Multiple categories (checkboxes).</summary>
    public List<int> IssueCategoryIds { get; set; } = new();

    /// <summary>Multiple divisions (checkboxes).</summary>
    public List<int> DivisionIds { get; set; } = new();

    /// <summary>Multiple business areas from admin lookup (checkboxes).</summary>
    public List<int> BusinessAreaLookupIds { get; set; } = new();

    public int? OwnerUserId { get; set; }

    public int? SroUserId { get; set; }

    public int? TargetResolutionDay { get; set; }
    public int? TargetResolutionMonth { get; set; }
    public int? TargetResolutionYear { get; set; }

    /// <summary>Mitigation / workaround text.</summary>
    public string? Workaround { get; set; }

    public string? DetailedCause { get; set; }

    /// <summary>Narrative listing significant assurance arrangements.</summary>
    public string? AssuranceArrangements { get; set; }

    /// <summary>Boards, reviews, events with dates and decisions.</summary>
    public List<IssueAssuranceItemForm> AssuranceItems { get; set; } = new();
}

public sealed class ModernRaidDependencyEditorForm
{
    public int? Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string SourceEntityType { get; set; } = "";

    [Required]
    public int SourceEntityId { get; set; }

    [Required]
    [MaxLength(50)]
    public string TargetEntityType { get; set; } = "";

    [Required]
    public int TargetEntityId { get; set; }

    public int? DependencyLinkTypeId { get; set; }
    public int? DependencyCriticalityId { get; set; }

    [MaxLength(200)]
    public string? DependencyType { get; set; }

    public string? Description { get; set; }

    [MaxLength(20)]
    public string? Status { get; set; }

    public int? DueDay { get; set; }
    public int? DueMonth { get; set; }
    public int? DueYear { get; set; }

    [MaxLength(200)]
    public string? Organisation { get; set; }

    public int? OwnerUserId { get; set; }
}

public sealed class ModernRaidRiskTierEditorForm
{
    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = "";

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = "";

    public string? Description { get; set; }

    [MaxLength(200)]
    public string? Summary { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;
}

/// <summary>Linked issue row on risk detail → Linked records tab.</summary>
public sealed record RiskLinkedIssueCardVm(
    int Id,
    string Title,
    string? Status,
    string? Severity,
    string? Priority,
    DateTime UpdatedAt,
    string LinkRelationship);

/// <summary>Read-only risk context for Operations edit (<c>ViewBag.OperationsRiskSummary</c>).</summary>
public sealed class OperationsRiskEditorSummaryVm
{
    public string Reference { get; init; } = "";
    public string Title { get; init; } = "";
    public string? TierName { get; init; }
    public string? StatusLabel { get; init; }
    public int RiskScore { get; init; }
    public string InherentLabel { get; init; } = "—";
}

/// <summary>When set on <see cref="Microsoft.AspNetCore.Mvc.Controller.ViewBag"/>, <c>RiskEditor.cshtml</c> posts to the work log-risk action and hides association controls.</summary>
public sealed class RaidRiskEditorWorkItemContext
{
    public required int ProjectId { get; init; }
    public required string WorkTitle { get; init; }
    public required string FormPostAction { get; init; }
    public required string CancelUrl { get; init; }
    public required string WorkDetailUrl { get; init; }
}

/// <summary>When set on <see cref="Microsoft.AspNetCore.Mvc.Controller.ViewBag"/>, <c>IssueEditor.cshtml</c> posts to the work log-issue action and hides association controls.</summary>
public sealed class RaidIssueEditorWorkItemContext
{
    public required int ProjectId { get; init; }
    public required string WorkTitle { get; init; }
    public required string FormPostAction { get; init; }
    public required string CancelUrl { get; init; }
    public required string WorkDetailUrl { get; init; }
}

public static class ModernRaidRiskCategoryFormHelper
{
    public static bool TryBuildCategoryIdList(
        int? primaryRiskCategoryId,
        int? secondaryRiskCategoryId,
        out List<int> categoryIds,
        out (string key, string message)? error)
    {
        var p = primaryRiskCategoryId is > 0 ? primaryRiskCategoryId : null;
        var s = secondaryRiskCategoryId is > 0 ? secondaryRiskCategoryId : null;
        if (s.HasValue && !p.HasValue)
        {
            categoryIds = new List<int>();
            error = (nameof(ModernRaidRiskEditorForm.SecondaryRiskCategoryId), "Select a primary category before choosing a secondary category.");
            return false;
        }
        if (p.HasValue && s.HasValue && p.Value == s.Value)
        {
            categoryIds = new List<int>();
            error = (nameof(ModernRaidRiskEditorForm.SecondaryRiskCategoryId), "Primary and secondary category must be different.");
            return false;
        }
        categoryIds = new List<int>();
        if (p.HasValue) categoryIds.Add(p.Value);
        if (s.HasValue) categoryIds.Add(s.Value);
        error = null;
        return true;
    }
}

public sealed class ModernRaidNearMissForm
{
    public int? Id { get; set; }

    [Required]
    [MaxLength(50)]
    [Display(Name = "Near miss / unexpected issue ID")]
    public string? Reference { get; set; }

    public int? LoggedDay { get; set; }
    public int? LoggedMonth { get; set; }
    public int? LoggedYear { get; set; }

    public int? NearMissTypeId { get; set; }
    public int? DirectorateLookupId { get; set; }
    public int? BusinessAreaLookupId { get; set; }

    public List<int> OwnerUserIds { get; set; } = new();

    [MaxLength(4000)]
    public string? Impact { get; set; }

    public int? NearMissSeriousnessId { get; set; }
    public int? PostMitigationRagStatusLookupId { get; set; }
    public int? RiskTierId { get; set; }
    public int? NearMissStatusId { get; set; }

    public List<ModernRaidNearMissActionFormRow> Actions { get; set; } = new();
    public List<ModernRaidNearMissMitigationFormRow> Mitigations { get; set; } = new();
}

public sealed class ModernRaidNearMissActionFormRow
{
    public int? Id { get; set; }
    public int? ActionDay { get; set; }
    public int? ActionMonth { get; set; }
    public int? ActionYear { get; set; }

    [MaxLength(4000)]
    public string? ActionText { get; set; }

    public int? RecordedByUserId { get; set; }

    /// <summary>Display only — repopulates the actions table on validation errors.</summary>
    public string? RecordedByDisplayName { get; set; }
}

public sealed class ModernRaidNearMissMitigationFormRow
{
    public int? Id { get; set; }
    public int? MitigationDay { get; set; }
    public int? MitigationMonth { get; set; }
    public int? MitigationYear { get; set; }

    [MaxLength(4000)]
    public string? AssuranceTakenPlace { get; set; }

    public int? RecordedByUserId { get; set; }

    /// <summary>Display only — repopulates the mitigations table on validation errors.</summary>
    public string? RecordedByDisplayName { get; set; }
}
