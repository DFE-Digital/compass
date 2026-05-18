namespace Compass.ViewModels.Modern;

public sealed record ModernRaidRiskRow(
    int Id,
    string Title,
    string? BusinessAreaLabel,
    RaidRegisterRelationParts Relation,
    string? Status,
    string? Owner,
    string? LikelihoodLabel,
    string? ImpactLabel,
    int RiskScore,
    /// <summary>Risk tier display name from <c>RiskTier.Name</c> when set; also used as the sub-heading key when the register groups by tier.</summary>
    string? Tier,
    /// <summary>Plain-text description snippet for quick views (e.g. business area dashboard modal).</summary>
    string? DescriptionSummary = null);

public sealed record ModernRaidIssueRow(
    int Id,
    string Title,
    string? BusinessAreaLabel,
    RaidRegisterRelationParts Relation,
    string? Status,
    string? Owner,
    string? Priority,
    string? Severity,
    string? DescriptionSummary = null);

public sealed record ModernRaidDependencyRow(
    int Id,
    string SourceType,
    int SourceId,
    string? SourceTitle,
    string? SourceDetailUrl,
    string TargetType,
    int TargetId,
    string? TargetTitle,
    string? TargetDetailUrl,
    string? DependencyType,
    string? LinkTypeLabel,
    string? CriticalityLabel,
    string? Organisation,
    DateTime? DueDate,
    string? Description,
    string? Status,
    DateTime Updated);

public sealed record ModernRaidAssumptionRow(
    int Id,
    /// <summary>work, product, or organisation.</summary>
    string AssociationUi,
    int? ProjectId,
    string? ProjectTitle,
    string? ProductLabel,
    string DescriptionSnippet,
    string? Criticality,
    string? Status,
    DateTime? ReviewDate,
    DateTime Updated);

public sealed record ModernRaidNearMissRow(
    int Id,
    string Reference,
    DateTime DateLogged,
    string? TypeLabel,
    string? DirectorateLabel,
    string? BusinessAreaLabel,
    string? OwnersSummary,
    string? SeriousnessLabel,
    string? TierLabel,
    string? StatusLabel,
    string? PostMitigationRagLabel,
    string? PostMitigationRagCssClass,
    DateTime Updated);
