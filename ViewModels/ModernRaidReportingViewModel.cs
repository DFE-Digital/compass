using System.Linq;

namespace Compass.ViewModels;

/// <summary>Reporting dashboard for modern RAID registers (heatmap matrices, KPIs, trends).</summary>
public sealed class ModernRaidReportingViewModel
{
    /// <summary>5×5 zero grid for likelihood (rows) × impact (cols) — safe default when the full builder is not run.</summary>
    public static int[][] EmptyRiskLikelihoodImpactGrid() =>
        Enumerable.Range(0, 5).Select(_ => Enumerable.Range(0, 5).Select(__ => 0).ToArray()).ToArray();

    /// <summary>risks | issues | intelligence</summary>
    public string ActiveTab { get; init; } = "risks";

    /// <summary>Cross-cutting portfolio intelligence (when <see cref="ActiveTab"/> is intelligence).</summary>
    public RaidIntelPack Intel { get; init; } = RaidIntelPack.Empty;

    /// <summary>Risk intelligence sub-panel when <see cref="ActiveTab"/> is risks: patterns | emerging | trends | materialised.</summary>
    public string RiskIntelTab { get; init; } = "patterns";

    /// <summary>Scope aggregates to a single business area (RAID tags and/or work item project BA), or <c>null</c> for all areas.</summary>
    public int? FilterBusinessAreaId { get; init; }

    /// <summary>Scope to a single directorate (RAID division tags on risks/issues), or <c>null</c> for all.</summary>
    public int? FilterDirectorateId { get; init; }

    /// <summary>One-line copy for the RAID toolbar, e.g. all areas vs a named BA + directorate.</summary>
    public string FilterScopeSummary { get; init; } = "All business areas · All directorates";

    public IReadOnlyList<RaidReportFilterSelectOption> BusinessAreaFilterOptions { get; init; } = [];

    public IReadOnlyList<RaidReportFilterSelectOption> DirectorateFilterOptions { get; init; } = [];

    /// <summary>Cross-portfolio posture strip (counts are live; some trend labels use score-band proxies — see UI note).</summary>
    public RaidRiskPostureStrip Posture { get; init; } = new(0, 0, 0, 0, 0);

    public RaidPatternHighlightCards PatternHighlights { get; init; } =
        new(null, 0, 0, null, 0, 0, 0, null, null);

    public IReadOnlyList<RaidThemeAnalyticsRow> ThemeRows { get; init; } = [];

    public IReadOnlyList<RaidEmergingRiskReportRow> EmergingRisks { get; init; } = [];

    public IReadOnlyList<RaidRiskScoreTrendReportRow> TrendRising { get; init; } = [];

    public IReadOnlyList<RaidRiskScoreTrendReportRow> TrendFalling { get; init; } = [];

    public IReadOnlyList<RaidMaterialisedRiskReportRow> MaterialisedRows { get; init; } = [];

    /* ─── Risk matrix: rows = likelihood 5→1, cols = impact 1→5 (standard 5×5) ─── */

    /// <summary>Counts where [likelihoodIndex][impactIndex], each index 0..4 maps to ratings 5..1 for rows and 1..5 for cols.</summary>
    public int[][] RiskLikelihoodImpactGrid { get; init; } = EmptyRiskLikelihoodImpactGrid();

    /// <summary>Open risks in the matrix / register.</summary>
    public int RiskMatrixTotal { get; init; }

    /* ─── Issue matrix ─── */

    public IReadOnlyList<RaidReportAxisCell> IssueSeverityColumns { get; init; } = [];

    public IReadOnlyList<RaidReportAxisCell> IssuePriorityRows { get; init; } = [];

    /// <summary>[priorityRow][severityCol]</summary>
    public int[][] IssuePrioritySeverityGrid { get; init; } = [];

    public int IssueMatrixTotal { get; init; }

    public int IssuesMissingSeverityOrPriority { get; init; }

    /* ─── KPIs ─── */

    public RaidReportingRiskKpis RiskKpis { get; init; } = new(0, 0, 0, 0, 0);

    public RaidReportingIssueKpis IssueKpis { get; init; } = new(0, 0, 0, 0);

    /* ─── Charts (serialized in view) ─── */

    public IReadOnlyList<string> TrendMonthLabels { get; init; } = [];

    public IReadOnlyList<int> TrendNewRisksPerMonth { get; init; } = [];

    public IReadOnlyList<double?> TrendAvgRiskScoreNewPerMonth { get; init; } = [];

    public IReadOnlyList<int> TrendNewIssuesPerMonth { get; init; } = [];

    /// <summary>Each key is a severity label; each array aligns with <see cref="TrendMonthLabels"/>.</summary>
    public IReadOnlyDictionary<string, int[]> TrendNewIssuesBySeveritySeries { get; init; }
        = new Dictionary<string, int[]>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, int> RiskScoreBucketCounts { get; init; }
        = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, int> OpenIssueSeverityCounts { get; init; }
        = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<RaidReportingTopRiskRow> TopRisks { get; init; } = [];

    public IReadOnlyDictionary<string, int> RiskTierCounts { get; init; }
        = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Risk counts by proximity label (portfolio timing).</summary>
    public IReadOnlyDictionary<string, int> RiskProximityCounts { get; init; }
        = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Open risks by resolved business area (junction tags; falls back to project / legacy).</summary>
    public IReadOnlyDictionary<string, int> RiskBusinessAreaCounts { get; init; }
        = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Open risks by inherent impact rating (1–5).</summary>
    public IReadOnlyDictionary<string, int> RiskImpactRatingCounts { get; init; }
        = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Open risks by category labels (junction + primary; multi-tag sums &gt; open count).</summary>
    public IReadOnlyDictionary<string, int> RiskCategoryCounts { get; init; }
        = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Open risks by workflow status.</summary>
    public IReadOnlyDictionary<string, int> RiskStatusCounts { get; init; }
        = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Open risks bucketed by age since identification (or creation).</summary>
    public IReadOnlyDictionary<string, int> RiskOpenAgeBucketCounts { get; init; }
        = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, int> IssueBusinessAreaCounts { get; init; }
        = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, int> IssueCategoryCounts { get; init; }
        = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, int> IssueStatusCounts { get; init; }
        = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, int> IssuePriorityCounts { get; init; }
        = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, int> IssueSeverityLabelCounts { get; init; }
        = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Open issues bucketed by age since detection (or creation).</summary>
    public IReadOnlyDictionary<string, int> IssueOpenAgeBucketCounts { get; init; }
        = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Audit-derived risk status transitions in the last 30 days.</summary>
    public int RiskStatusChangesLast30Days { get; init; }

    /// <summary>Audit-derived risk status transitions in the last 90 days.</summary>
    public int RiskStatusChangesLast90Days { get; init; }

    public int IssueStatusChangesLast30Days { get; init; }

    public int IssueStatusChangesLast90Days { get; init; }

    /// <summary>Monthly risk status changes (aligned with <see cref="TrendMonthLabels"/>).</summary>
    public IReadOnlyList<int> RiskStatusChangesPerMonth { get; init; } = [];

    /// <summary>Monthly issue status changes.</summary>
    public IReadOnlyList<int> IssueStatusChangesPerMonth { get; init; } = [];
}

/// <summary>Prioritised RAID signals for risk-team triage (business area / division heat, focus lists, throughput).</summary>
public sealed record RaidIntelPack(
    IReadOnlyList<string> NarrativeBullets,
    IReadOnlyList<RaidIntelHotspotRow> BusinessAreaHotspots,
    IReadOnlyList<RaidIntelHotspotRow> DivisionHotspots,
    IReadOnlyList<RaidIntelPriorityRiskRow> FocusRisks,
    IReadOnlyList<RaidIntelPriorityIssueRow> FocusIssues,
    IReadOnlyList<RaidIntelThroughputRow> QuickClosedRisks,
    IReadOnlyList<RaidIntelThroughputRow> QuickClosedIssues,
    IReadOnlyList<RaidIntelStaleRiskRow> StaleRisks,
    IReadOnlyList<RaidIntelStaleIssueRow> StaleIssues,
    IReadOnlyList<RaidIntelPriorityRiskRow> LongestOpenRisks,
    IReadOnlyList<RaidIntelPriorityIssueRow> LongestOpenIssues,
    IReadOnlyList<RaidIntelProximityNearingRow> ProximityNearingRisks)
{
    public static RaidIntelPack Empty { get; } = new(
        [],
        [],
        [],
        [],
        [],
        [],
        [],
        [],
        [],
        [],
        [],
        []);
}

public sealed record RaidIntelHotspotRow(
    string Label,
    int OpenRisks,
    int OpenIssues,
    int HighScoreRisks,
    int MaxImpactRisks,
    int BlockedIssues,
    int HeatScore,
    string Rationale);

public sealed record RaidIntelPriorityRiskRow(
    int Id,
    string Title,
    string ReferenceLabel,
    int RiskScore,
    int ImpactRating,
    int DaysOpen,
    int DaysSinceUpdate,
    bool ReviewOverdue,
    string Insight);

public sealed record RaidIntelPriorityIssueRow(
    int Id,
    string Title,
    string ReferenceLabel,
    string SeverityLabel,
    bool Blocked,
    int DaysOpen,
    string Insight);

public sealed record RaidIntelThroughputRow(
    int Id,
    string Title,
    string ReferenceLabel,
    bool IsRisk,
    int DaysOpen,
    DateTime ClosedDate,
    string Insight);

public sealed record RaidIntelStaleRiskRow(
    int Id,
    string Title,
    string ReferenceLabel,
    int RiskScore,
    int DaysSinceUpdate,
    int DaysOpen,
    string Insight);

public sealed record RaidIntelStaleIssueRow(
    int Id,
    string Title,
    string ReferenceLabel,
    string SeverityLabel,
    int DaysSinceUpdate,
    int DaysOpen,
    string Insight);

/// <summary>Open risk: raised some time ago, proximity within 6 months of raise, and proximity date in the next window (portfolio catch-up).</summary>
public sealed record RaidIntelProximityNearingRow(
    int Id,
    string Title,
    string ReferenceLabel,
    DateTime RaisedDate,
    DateTime ProximityDate,
    int DaysToProximity,
    int RiskScore,
    string Insight);

public sealed record RaidReportFilterSelectOption(int Id, string Name);

public sealed record RaidReportAxisCell(int Id, string Label);

public sealed record RaidReportingRiskKpis(
    int OpenRisks,
    double AvgRiskScore,
    int HighRiskScoreCount,
    int ReviewOverdueCount,
    int RegisteredLast30Days);

public sealed record RaidReportingIssueKpis(
    int OpenIssues,
    int BlockedIssues,
    int SevereOpenCount,
    int RaisedLast30Days);

public sealed record RaidReportingTopRiskRow(int Id, string Title, int RiskScore, string? TierName);

public sealed record RaidRiskPostureStrip(
    int TotalOpen,
    int MaterialisedLast30Days,
    int ScoreElevatedActive90d,
    int ScoreReducedActive90d,
    int NearTermProximityOrElevatedBand30d);

public sealed record RaidPatternHighlightCards(
    string? TopCategoryLabel,
    int TopCategoryOpenCount,
    int TopCategoryMaterialised6Months,
    string? TopPortfolioLabel,
    int TopPortfolioRiskCount,
    int TopPortfolioCriticalApprox,
    int TopPortfolioMaterialised6Months,
    string? ImprovingThemeLabel,
    string? ImprovingTrendSummary);

public sealed record RaidThemeAnalyticsRow(
    string Theme,
    int RiskCount,
    double AvgScore,
    string TrendSymbol,
    string TrendLabel,
    string TrendCss,
    string PortfoliosSummary);

public sealed record RaidEmergingRiskReportRow(
    int Id,
    string Title,
    string ReferenceLabel,
    int RiskScore,
    string? TierName,
    string? Subtitle,
    string WhyFlagged,
    string TrajectorySummary,
    string RecommendedAction,
    string SeverityCss);

public sealed record RaidRiskScoreTrendReportRow(
    int Id,
    string Title,
    string ReferenceLabel,
    string? PortfolioName,
    int CurrentScore,
    string? RiskStatusLabel,
    string ChangeHint);

public sealed record RaidMaterialisedRiskReportRow(
    int RiskId,
    string RiskTitle,
    string RiskRef,
    DateTime MaterialisedDate,
    int IssueId,
    string IssueTitle,
    string IssueRef,
    int RiskScoreSnapshot,
    string MitigationTag,
    string MitigationCss);
