using Compass.ViewModels;

namespace Compass.ViewModels.Modern;

/// <summary>Named bucket for RAID dashboard breakdown tables.</summary>
public sealed record ModernRaidLabelCountVm(string Label, int Count, string? DrillUrl = null);

/// <summary>Executive intelligence row — risks/issues grouped by business area, product, or work item.</summary>
public sealed record ModernRaidIntelligenceRowVm(
    string Label,
    int OpenRiskCount,
    int OpenIssueCount,
    int ElevatedRiskCount,
    string? DrillUrl);

/// <summary>Attention / health metric with link into register or detail.</summary>
public sealed record ModernRaidHealthMetricVm(
    string Label,
    int Count,
    string Description,
    string DrillUrl);

/// <summary>Aggregate counts across the whole RAID registers (organisation-wide reference).</summary>
public sealed record ModernRaidOrganisationTotalsVm(
    int OpenRiskCount,
    int OpenIssueCount,
    int RiskCount,
    int IssueCount,
    int AssumptionCount,
    int DependencyCount);

/// <summary>Portfolio (work item) filter option on the RAID reporting dashboard.</summary>
public sealed record ModernRaidDashboardProjectOption(int Id, string Name);

/// <summary>Single risk listed in a matrix cell drill-down (same display fields as the main risks register).</summary>
public sealed record ModernRaidMatrixCellRiskVm(
    int Id,
    string Reference,
    string Title,
    string? BusinessAreaLabel,
    string? Tier,
    string RelationKind,
    int? RelationProjectId,
    string? RelationTarget,
    string? RelationWorkItemUrl,
    string? Status,
    string? Owner,
    string? LikelihoodLabel,
    string? ImpactLabel,
    int RiskScore);

/// <summary>Risk matrix cell — impact × likelihood buckets (1–5).</summary>
public sealed record ModernRaidMatrixCellVm(
    int ImpactRating,
    int LikelihoodRating,
    int Count,
    int CellScore,
    string CellTone,
    IReadOnlyList<ModernRaidMatrixCellRiskVm> Risks);

/// <summary>Risk highlight row under the matrix.</summary>
public sealed record ModernRaidTopRiskVm(
    int Id,
    string Reference,
    string Title,
    string TeamLabel,
    string TierName,
    string ImpactLabel,
    string LikelihoodLabel,
    string RatingTagClass,
    string RatingLabel,
    string AppetiteLabel,
    string ProximityLabel,
    string TreatmentLabel,
    string StatusTagClass,
    string StatusLabel,
    string TrendLabel,
    bool HighlightRow);

/// <summary>Risk register row on the dashboard tab.</summary>
public sealed record ModernRaidDashboardRiskRegisterVm(
    int Id,
    string Reference,
    string Title,
    string? CategoryLine,
    string? SroDisplay,
    string? OwnerDisplay,
    string TierName,
    string ImpactLabel,
    string LikelihoodLabel,
    string RatingTagClass,
    string RatingLabel,
    string? ResidualLabel,
    string? AppetiteLabel,
    string TreatmentLabel,
    string ProximityLabel,
    string StatusTagClass,
    string StatusLabel,
    bool StaleNoUpdate,
    bool StagnantRating,
    int DaysSinceUpdate,
    int? DaysSinceRatingChange,
    bool HighlightRow);

/// <summary>Issue row on the dashboard tab.</summary>
public sealed record ModernRaidDashboardIssueRegisterVm(
    int Id,
    string Reference,
    string Title,
    string? CategoryLine,
    string? SroDisplay,
    string? OwnerDisplay,
    string SeverityTagClass,
    string SeverityLabel,
    string PriorityTagClass,
    string PriorityLabel,
    string RaisedLabel,
    int DaysOpen,
    string DaysOpenTone,
    string StatusTagClass,
    string StatusLabel,
    bool StaleNoUpdate,
    bool StagnantRating,
    int DaysSinceUpdate,
    int? DaysSinceRatingChange,
    bool HighlightRow);

/// <summary>Impact type summary row.</summary>
public sealed record ModernRaidImpactTypeSummaryVm(
    string ImpactType,
    int TotalRisks,
    int CrisisCriticalOpen,
    int ModerateOpen,
    int MarginalNegligibleOpen,
    string RatingSummaryLabel,
    int OpenIssuesInScope);

/// <summary>Rating movement snapshot (recent updates — audit-based differentiation not yet wired).</summary>
public sealed record ModernRaidMovementRowVm(
    string Reference,
    string DetailUrl,
    string Title,
    string PreviousLabel,
    string CurrentLabel,
    string DeltaHint,
    string Tone);

/// <summary>Cross-cutting “needs attention” row (stale update or long-unchanged rating from audit).</summary>
public sealed record ModernRaidAttentionRowVm(
    string Kind,
    int Id,
    string Reference,
    string Title,
    int DaysSinceUpdate,
    int? DaysSinceLastRatingChange,
    bool StaleNoUpdate,
    bool StagnantRating);

/// <summary>How the RAID dashboard filters and labels its metrics.</summary>
public enum RaidDashboardScopeKind
{
    /// <summary>Signed-in viewer — personal + organisation sections.</summary>
    Viewer = 0,
    /// <summary>Single directorate (division) at <c>/modern/raid/directorate/{id}</c>.</summary>
    Directorate = 1,
    /// <summary>Single business area at <c>/modern/raid/business-areas/{id}</c>.</summary>
    BusinessArea = 2
}

public sealed class ModernRaidDashboardViewModel
{
    /// <summary>When not <see cref="RaidDashboardScopeKind.Viewer"/>, UI shows a scoped summary only.</summary>
    public RaidDashboardScopeKind ScopeKind { get; init; } = RaidDashboardScopeKind.Viewer;

    public bool IsScoped => ScopeKind != RaidDashboardScopeKind.Viewer;

    /// <summary>Directorate or business area display name when scoped.</summary>
    public string? ScopeTitle { get; init; }

    /// <summary>Back link to the matrix list (directorate or business areas).</summary>
    public string? ScopeListUrl { get; init; }

    /// <summary>Matrix view filtered to this directorate (when <see cref="ScopeKind"/> is Directorate).</summary>
    public int? ScopeDirectorateId { get; init; }

    /// <summary>Matrix view filtered to this business area (when <see cref="ScopeKind"/> is BusinessArea).</summary>
    public int? ScopeBusinessAreaId { get; init; }

    /// <summary>Optional subtitle (leadership, linked directorates, etc.).</summary>
    public string? ScopeSubtitle { get; init; }

    /// <summary>Display name for greeting (falls back to email).</summary>
    public string? ViewerDisplayName { get; init; }

    /// <summary>Single line describing linked work items and products (may be empty).</summary>
    public string ScopeSummary { get; init; } = string.Empty;

    public int LinkedWorkItemCount { get; init; }

    public int LinkedProductCount { get; init; }

    /// <summary>Scoped to the viewer — total risks (including closed).</summary>
    public int RiskCount { get; init; }

    /// <summary>Scoped — total issues.</summary>
    public int IssueCount { get; init; }

    /// <summary>Org-wide dependency count (not fully attributable to scope).</summary>
    public int DependencyCount { get; init; }

    /// <summary>Scoped — assumptions.</summary>
    public int AssumptionCount { get; init; }

    /// <summary>Open risks in scope (<see cref="Risk.ClosedDate"/> null).</summary>
    public int OpenRiskCount { get; init; }

    /// <summary>Open issues in scope.</summary>
    public int OpenIssueCount { get; init; }

    /// <summary>Closed risks in scope.</summary>
    public int ClosedRiskCount { get; init; }

    /// <summary>Closed issues in scope.</summary>
    public int ClosedIssueCount { get; init; }

    /// <summary>Open scoped risks with inherent score ≥ 15.</summary>
    public int ElevatedOpenRiskCount { get; init; }

    /// <summary>Mean inherent risk score over open scoped risks.</summary>
    public double? MeanOpenRiskScore { get; init; }

    /// <summary>Open scoped risks grouped by normalized status label.</summary>
    public IReadOnlyList<ModernRaidLabelCountVm> OpenRiskStatusBreakdown { get; init; } = Array.Empty<ModernRaidLabelCountVm>();

    /// <summary>Open scoped issues grouped by normalized status label.</summary>
    public IReadOnlyList<ModernRaidLabelCountVm> OpenIssueStatusBreakdown { get; init; } = Array.Empty<ModernRaidLabelCountVm>();

    /// <summary>Open scoped risks by escalation tier.</summary>
    public IReadOnlyList<ModernRaidLabelCountVm> OpenRiskTierBreakdown { get; init; } = Array.Empty<ModernRaidLabelCountVm>();

    /// <summary>Open scoped risks by inherent score band.</summary>
    public IReadOnlyList<ModernRaidLabelCountVm> OpenRiskScoreBands { get; init; } = Array.Empty<ModernRaidLabelCountVm>();

    /// <summary>Whole-register counts for comparison.</summary>
    public ModernRaidOrganisationTotalsVm OrganisationTotals { get; init; } =
        new(0, 0, 0, 0, 0, 0);

    /// <summary>Displayed as reporting “data as at” timestamp.</summary>
    public DateTime DataAsAtUtc { get; init; } = DateTime.UtcNow;

    /// <summary>Risks scoring ≥16 (open).</summary>
    public int OpenCrisisCriticalRiskCount { get; init; }

    /// <summary>Open risks scoring 6–15.</summary>
    public int OpenModerateRiskCount { get; init; }

    /// <summary>Open risks scoring ≤5.</summary>
    public int OpenMarginalNegligibleRiskCount { get; init; }

    /// <summary>Open risks or issues whose status reads as escalated.</summary>
    public int EscalatedOpenRecordCount { get; init; }

    /// <summary>Open scoped issues grouped by severity label.</summary>
    public IReadOnlyList<ModernRaidLabelCountVm> OpenIssueSeverityBreakdown { get; init; } = Array.Empty<ModernRaidLabelCountVm>();

    /// <summary>Open scoped issues grouped by priority label.</summary>
    public IReadOnlyList<ModernRaidLabelCountVm> OpenIssuePriorityBreakdown { get; init; } = Array.Empty<ModernRaidLabelCountVm>();

    /// <summary>Active issue severities (columns), same ordering as RAID reporting.</summary>
    public IReadOnlyList<RaidReportAxisCell> IssueMatrixSeverityColumns { get; init; } = Array.Empty<RaidReportAxisCell>();

    /// <summary>Active issue priorities (rows), same ordering as RAID reporting.</summary>
    public IReadOnlyList<RaidReportAxisCell> IssueMatrixPriorityRows { get; init; } = Array.Empty<RaidReportAxisCell>();

    /// <summary>Cross-tab counts [priority row][severity column] for open scoped issues.</summary>
    public IReadOnlyList<IReadOnlyList<int>> IssuePrioritySeverityGrid { get; init; } =
        Array.Empty<IReadOnlyList<int>>();

    /// <summary>Open scoped issues missing severity or priority (excluded from <see cref="IssuePrioritySeverityGrid"/>).</summary>
    public int IssueMatrixExcludedCount { get; init; }

    /// <summary>Open issues grouped as high severity tier (approximate, from severity name).</summary>
    public int OpenIssueTier1ApproxCount { get; init; }

    public int OpenIssueTier2ApproxCount { get; init; }

    public int OpenIssueTier3ApproxCount { get; init; }

    /// <summary>Inherent likelihood labels for matrix columns (left → right).</summary>
    public IReadOnlyList<string> MatrixLikelihoodAxisLabels { get; init; } =
        Array.AsReadOnly(new[]
        {
            "Very unlikely",
            "Unlikely",
            "Possible",
            "Likely",
            "Very likely"
        });

    /// <summary>Impact axis labels bottom → top row order in UI (Negligible … Crisis).</summary>
    public IReadOnlyList<string> MatrixImpactAxisLabels { get; init; } =
        Array.AsReadOnly(new[]
        {
            "Negligible",
            "Marginal",
            "Moderate",
            "Critical",
            "Crisis"
        });

    public IReadOnlyList<ModernRaidMatrixCellVm> MatrixCells { get; init; } = Array.Empty<ModernRaidMatrixCellVm>();

    public IReadOnlyList<ModernRaidTopRiskVm> TopRisks { get; init; } = Array.Empty<ModernRaidTopRiskVm>();

    public IReadOnlyList<ModernRaidDashboardRiskRegisterVm> DashboardRiskRows { get; init; } =
        Array.Empty<ModernRaidDashboardRiskRegisterVm>();

    public IReadOnlyList<ModernRaidDashboardIssueRegisterVm> DashboardIssueRows { get; init; } =
        Array.Empty<ModernRaidDashboardIssueRegisterVm>();

    public IReadOnlyList<ModernRaidImpactTypeSummaryVm> ImpactTypeSummaries { get; init; } =
        Array.Empty<ModernRaidImpactTypeSummaryVm>();

    public IReadOnlyList<ModernRaidMovementRowVm> RecentMovementRows { get; init; } =
        Array.Empty<ModernRaidMovementRowVm>();

    public IReadOnlyList<ModernRaidDashboardProjectOption> PortfolioOptions { get; init; } =
        Array.Empty<ModernRaidDashboardProjectOption>();

    /// <summary>Selected portfolio filter (work item id).</summary>
    public int? SelectedPortfolioProjectId { get; init; }

    /// <summary>Sub-tab under reporting: matrix, register, issues, movement, type.</summary>
    public string ActiveReportTab { get; init; } = "matrix";

    /// <summary>When true, dashboard metrics and tables use all RAID data in Compass (Across Compass tab), not the signed-in user scope.</summary>
    public bool OrganisationWideDashboard { get; init; }

    /// <summary>Open scoped risks with no record update in 30+ days.</summary>
    public int StaleOpenRiskCount { get; init; }

    /// <summary>Open scoped issues with no record update in 30+ days.</summary>
    public int StaleOpenIssueCount { get; init; }

    /// <summary>Open scoped risks whose inherent rating fields have not changed in the audit log for 90+ days (falls back to created date if no log).</summary>
    public int StagnantOpenRiskCount { get; init; }

    /// <summary>Open scoped issues whose severity / priority (admin fields) have not changed in the audit log for 90+ days.</summary>
    public int StagnantOpenIssueCount { get; init; }

    /// <summary>
    /// Open risks in the viewer’s scope where inherent rating (audit) has not changed in ~3 months.
    /// </summary>
    public IReadOnlyList<ModernRaidAttentionRowVm> RisksInherentRatingUnchangedThreePlusMonths { get; init; } =
        Array.Empty<ModernRaidAttentionRowVm>();

    /// <summary>True when the signed-in user is a business area admin for at least one active business area (used in scope copy).</summary>
    public bool ViewerIsBusinessAreaAdmin { get; init; }

    /// <summary>Open risks in the viewer’s linked scope (work, product, ownership).</summary>
    public int YourOpenRiskCount { get; init; }

    /// <summary>Open issues in the viewer’s linked scope.</summary>
    public int YourOpenIssueCount { get; init; }

    /// <summary>Your-scope risks by operational tier.</summary>
    public IReadOnlyList<ModernRaidLabelCountVm> YourOpenRiskTierBreakdown { get; init; } =
        Array.Empty<ModernRaidLabelCountVm>();

    /// <summary>Your-scope open risks by inherent score band (Highest / Elevated / Medium / Lower).</summary>
    public IReadOnlyList<ModernRaidLabelCountVm> YourInherentRiskBands { get; init; } =
        Array.Empty<ModernRaidLabelCountVm>();

    /// <summary>Organisation-wide open risks by tier.</summary>
    public IReadOnlyList<ModernRaidLabelCountVm> OrganisationOpenRiskTierBreakdown { get; init; } =
        Array.Empty<ModernRaidLabelCountVm>();

    /// <summary>Organisation-wide open risks by inherent score band.</summary>
    public IReadOnlyList<ModernRaidLabelCountVm> OrganisationInherentRiskBands { get; init; } =
        Array.Empty<ModernRaidLabelCountVm>();

    /// <summary>Organisation-wide open issues by severity.</summary>
    public IReadOnlyList<ModernRaidLabelCountVm> OrganisationOpenIssueSeverityBreakdown { get; init; } =
        Array.Empty<ModernRaidLabelCountVm>();

    /// <summary>Organisation-wide open issues by priority.</summary>
    public IReadOnlyList<ModernRaidLabelCountVm> OrganisationOpenIssuePriorityBreakdown { get; init; } =
        Array.Empty<ModernRaidLabelCountVm>();

    /// <summary>Top business areas by open risk count (organisation).</summary>
    public IReadOnlyList<ModernRaidIntelligenceRowVm> IntelligenceByBusinessArea { get; init; } =
        Array.Empty<ModernRaidIntelligenceRowVm>();

    /// <summary>Top products by open risk count (organisation).</summary>
    public IReadOnlyList<ModernRaidIntelligenceRowVm> IntelligenceByProduct { get; init; } =
        Array.Empty<ModernRaidIntelligenceRowVm>();

    /// <summary>Top work items by open risk count (organisation).</summary>
    public IReadOnlyList<ModernRaidIntelligenceRowVm> IntelligenceByWorkItem { get; init; } =
        Array.Empty<ModernRaidIntelligenceRowVm>();

    /// <summary>Organisation risk health metrics (stale, proximity, mitigations, KRIs).</summary>
    public IReadOnlyList<ModernRaidHealthMetricVm> OrganisationRiskHealthMetrics { get; init; } =
        Array.Empty<ModernRaidHealthMetricVm>();

    /// <summary>Full workbook export (all risks and issues).</summary>
    public string? ExportExcelUrl { get; init; }

    /// <summary>Matrix and personal sections use viewer-linked scope when true; otherwise direct create/own/assign only.</summary>
    public bool UsesViewerLinkedScope { get; init; } = true;

    /// <summary>When false, hides organisation-wide oversight (scoped dashboards).</summary>
    public bool ShowOrganisationOversight { get; init; } = true;

    /// <summary>Open risks and issues in scope for the simplified dashboard table.</summary>
    public IReadOnlyList<ModernRaidDashboardSummaryRowVm> TableRows { get; init; } =
        Array.Empty<ModernRaidDashboardSummaryRowVm>();

    /// <summary>Risks and issues flagged as needing attention (overdue, high score, high severity).</summary>
    public IReadOnlyList<ModernRaidDashboardAttentionRowVm> AttentionRows { get; init; } =
        Array.Empty<ModernRaidDashboardAttentionRowVm>();

    public int OpenNearMissCount { get; init; }
}

/// <summary>Row in the dashboard “requires attention” table.</summary>
public sealed record ModernRaidDashboardAttentionRowVm(
    string Kind,
    int Id,
    string Reference,
    string Title,
    string Reason);

/// <summary>Combined risk/issue row on the RAID dashboard register table.</summary>
public sealed record ModernRaidDashboardSummaryRowVm(
    string Kind,
    int Id,
    string Reference,
    string Title,
    string RatingBadgeClass,
    string RatingBadgeText,
    string? RatingAriaLabel,
    string? OwnerName,
    DateTime UpdatedAt);
