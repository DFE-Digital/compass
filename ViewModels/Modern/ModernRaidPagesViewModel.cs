namespace Compass.ViewModels.Modern;

/// <summary>Dropdown line for RAID register filters.</summary>
public sealed record RaidLookupOptionVm(int Id, string Name);

/// <summary>Risks register with filters (GET query round-trips).</summary>
public sealed class ModernRaidRisksPageViewModel
{
    public IReadOnlyList<ModernRaidRiskRow> Rows { get; init; } = Array.Empty<ModernRaidRiskRow>();
    public int TotalFiltered { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 25;
    public int TotalPages => TotalFiltered <= 0 ? 1 : (int)Math.Ceiling(TotalFiltered / (double)Math.Max(PageSize, 1));

    public string? Search { get; init; }
    public int? ProjectId { get; init; }
    public int? RiskStatusId { get; init; }
    public int? RiskTierId { get; init; }
    public bool OpenOnly { get; init; }
    public string ActiveTab { get; init; } = "my";
    public int MyCount { get; init; }
    public int OpenCount { get; init; }
    public int ClosedCount { get; init; }
    public int AllCount { get; init; }

    /// <summary>Filter to projects linked to this division (<see cref="ProjectDirectorate"/>).</summary>
    public int? DivisionId { get; init; }

    /// <summary>Filter to projects with this business area (<see cref="BusinessAreaLookup"/>).</summary>
    public int? BusinessAreaId { get; init; }

    /// <summary>True when the request included <c>businessAreaId=0</c> (all areas; do not re-apply saved preference when building URLs).</summary>
    public bool RaidBusinessAreaExplicitNone { get; init; }

    /// <summary>Query fragment to preserve business area scope (empty when unset).</summary>
    public string RaidBusinessAreaQueryFragment
    {
        get
        {
            if (BusinessAreaId is int ba && ba > 0)
                return $"businessAreaId={ba}";
            if (RaidBusinessAreaExplicitNone)
                return "businessAreaId=0";
            return "";
        }
    }

    /// <summary>When true, the effective business area filter came from the user&apos;s saved RAID preference.</summary>
    public bool RaidBusinessAreaFromSavedPreference { get; init; }

    /// <summary>When true, the risks register shows the business area selector (main register only).</summary>
    public bool ShowRaidRegisterBusinessAreaBar { get; init; }

    /// <summary>When true, show &quot;Save as my default&quot; for the business area control.</summary>
    public bool CanSaveRaidBusinessAreaPreference { get; init; }

    /// <summary>When true, the visible business area filter matches the user&apos;s saved RAID default.</summary>
    public bool RaidBusinessAreaMatchesSavedDefault { get; init; }

    /// <summary>When true, the table inserts sub-head rows when <see cref="ModernRaidRiskRow.Tier"/> changes.</summary>
    public bool GroupRowsByRiskTier { get; init; }

    /// <summary>Open risks in the current filter scope (dashboard posture strip).</summary>
    public int RegisterStripOpenRiskCount { get; init; }

    /// <summary>Open risks with inherent score band crisis/critical (≥16), scoped to current filters.</summary>
    public int RegisterStripOpenCrisisCriticalRiskCount { get; init; }

    /// <summary>Open risks with score 6–15 (moderate band).</summary>
    public int RegisterStripOpenModerateRiskCount { get; init; }

    /// <summary>Open risks with score ≤5 (marginal/negligible band).</summary>
    public int RegisterStripOpenMarginalNegligibleRiskCount { get; init; }

    /// <summary>Open issues aligned to the same portfolio scope as the risk filters (excluding risk-only filters).</summary>
    public int RegisterStripOpenIssueCount { get; init; }

    /// <summary>Open risks or issues whose status looks escalated, within the aligned scope.</summary>
    public int RegisterStripEscalatedOpenRecordCount { get; init; }

    public IReadOnlyList<RaidLookupOptionVm> ProjectOptions { get; init; } = Array.Empty<RaidLookupOptionVm>();
    public IReadOnlyList<RaidLookupOptionVm> RiskStatusOptions { get; init; } = Array.Empty<RaidLookupOptionVm>();
    public IReadOnlyList<RaidLookupOptionVm> TierOptions { get; init; } = Array.Empty<RaidLookupOptionVm>();
    public IReadOnlyList<RaidLookupOptionVm> DivisionOptions { get; init; } = Array.Empty<RaidLookupOptionVm>();
    public IReadOnlyList<RaidLookupOptionVm> BusinessAreaOptions { get; init; } = Array.Empty<RaidLookupOptionVm>();
}

public sealed class ModernRaidIssuesPageViewModel
{
    public IReadOnlyList<ModernRaidIssueRow> Rows { get; init; } = Array.Empty<ModernRaidIssueRow>();
    public int TotalFiltered { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 25;
    public int TotalPages => TotalFiltered <= 0 ? 1 : (int)Math.Ceiling(TotalFiltered / (double)Math.Max(PageSize, 1));

    public string? Search { get; init; }
    public int? ProjectId { get; init; }
    public int? IssueStatusId { get; init; }
    public int? SeverityId { get; init; }
    public bool OpenOnly { get; init; }
    public string ActiveTab { get; init; } = "my";
    public int MyCount { get; init; }
    public int OpenCount { get; init; }
    public int ClosedCount { get; init; }
    public int AllCount { get; init; }

    /// <summary>Filter by work item business area or issue RAID business area tags.</summary>
    public int? BusinessAreaId { get; init; }

    public bool RaidBusinessAreaExplicitNone { get; init; }

    public string RaidBusinessAreaQueryFragment
    {
        get
        {
            if (BusinessAreaId is int ba && ba > 0)
                return $"businessAreaId={ba}";
            if (RaidBusinessAreaExplicitNone)
                return "businessAreaId=0";
            return "";
        }
    }

    public bool RaidBusinessAreaFromSavedPreference { get; init; }

    public bool ShowRaidRegisterBusinessAreaBar { get; init; }

    public bool CanSaveRaidBusinessAreaPreference { get; init; }

    public bool RaidBusinessAreaMatchesSavedDefault { get; init; }

    public IReadOnlyList<RaidLookupOptionVm> ProjectOptions { get; init; } = Array.Empty<RaidLookupOptionVm>();
    public IReadOnlyList<RaidLookupOptionVm> IssueStatusOptions { get; init; } = Array.Empty<RaidLookupOptionVm>();
    public IReadOnlyList<RaidLookupOptionVm> SeverityOptions { get; init; } = Array.Empty<RaidLookupOptionVm>();
    public IReadOnlyList<RaidLookupOptionVm> BusinessAreaOptions { get; init; } = Array.Empty<RaidLookupOptionVm>();
}

public sealed class ModernRaidDependenciesPageViewModel
{
    public IReadOnlyList<ModernRaidDependencyRow> Rows { get; init; } = Array.Empty<ModernRaidDependencyRow>();
    public int TotalFiltered { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 25;
    public int TotalPages => TotalFiltered <= 0 ? 1 : (int)Math.Ceiling(TotalFiltered / (double)Math.Max(PageSize, 1));

    public string? Search { get; init; }
    public int? LinkTypeId { get; init; }
    public int? CriticalityId { get; init; }
    public string? Status { get; init; }
    public string ActiveTab { get; init; } = "open";
    public int OpenCount { get; init; }
    public int ClosedCount { get; init; }
    public int AllCount { get; init; }

    public IReadOnlyList<RaidLookupOptionVm> LinkTypeOptions { get; init; } = Array.Empty<RaidLookupOptionVm>();
    public IReadOnlyList<RaidLookupOptionVm> CriticalityOptions { get; init; } = Array.Empty<RaidLookupOptionVm>();
    /// <summary>Distinct dependency status strings present in the database.</summary>
    public IReadOnlyList<string> StatusChoices { get; init; } = Array.Empty<string>();
}

/// <summary>Directorate matrix at <c>/modern/raid/directorate</c> (same slices as tier view, first column = directorate).</summary>
public sealed class ModernRaidDirectoratePageViewModel
{
    public string? Search { get; init; }
    public int? ProjectId { get; init; }
    public int? BusinessAreaId { get; init; }
    public bool RaidBusinessAreaExplicitNone { get; init; }
    public int? DirectorateId { get; init; }

    public IReadOnlyList<RaidLookupOptionVm> ProjectOptions { get; init; } = Array.Empty<RaidLookupOptionVm>();
    public IReadOnlyList<RaidLookupOptionVm> BusinessAreaOptions { get; init; } = Array.Empty<RaidLookupOptionVm>();
    public IReadOnlyList<RaidLookupOptionVm> DirectorateOptions { get; init; } = Array.Empty<RaidLookupOptionVm>();

    public IReadOnlyList<RaidDirectorateSummaryRowVm> SummaryRows { get; init; } = Array.Empty<RaidDirectorateSummaryRowVm>();

    public string? DrillEndpoint { get; init; }
}

public sealed class RaidDirectorateSummaryRowVm
{
    public int DirectorateId { get; init; }
    public string Name { get; init; } = "";

    /// <summary>Names from Admin → Directorate leadership (<see cref="Models.DivisionUser"/>), comma-separated.</summary>
    public string? LeadershipNames { get; init; }

    /// <summary>Distinct work items linked via <see cref="Models.ProjectDirectorate"/>.</summary>
    public int LinkedWorkItemCount { get; init; }

    public int RisksOpen { get; init; }
    public int RisksOverdue { get; init; }
    public int RisksClosed { get; init; }

    public int RiskScoreHighest { get; init; }
    public int RiskScoreElevated { get; init; }
    public int RiskScoreMedium { get; init; }
    public int RiskScoreLower { get; init; }

    public int IssuesOpen { get; init; }
    public int IssuesOverdue { get; init; }
    public int IssuesClosed { get; init; }

    public int IssuesLow { get; init; }
    public int IssuesMedium { get; init; }
    public int IssuesHigh { get; init; }
    public int IssuesCritical { get; init; }
}

/// <summary>Business area matrix at <c>/modern/raid/business-areas</c> (same slices as tier / directorate; first column = business area).</summary>
public sealed class ModernRaidBusinessAreasPageViewModel
{
    public string? Search { get; init; }
    public int? ProjectId { get; init; }
    public int? BusinessAreaId { get; init; }
    public bool RaidBusinessAreaExplicitNone { get; init; }
    public int? DirectorateId { get; init; }
    /// <summary>When set, the matrix only shows this business area row.</summary>
    public int? FilterAreaId { get; init; }

    public IReadOnlyList<RaidLookupOptionVm> ProjectOptions { get; init; } = Array.Empty<RaidLookupOptionVm>();
    public IReadOnlyList<RaidLookupOptionVm> BusinessAreaOptions { get; init; } = Array.Empty<RaidLookupOptionVm>();
    public IReadOnlyList<RaidLookupOptionVm> DirectorateOptions { get; init; } = Array.Empty<RaidLookupOptionVm>();

    public IReadOnlyList<RaidBusinessAreaSummaryRowVm> SummaryRows { get; init; } = Array.Empty<RaidBusinessAreaSummaryRowVm>();

    public string? DrillEndpoint { get; init; }
}

public sealed class RaidBusinessAreaSummaryRowVm
{
    public int BusinessAreaId { get; init; }
    public string Name { get; init; } = "";

    /// <summary>Deputy Director (DD) names from Admin → Business area leadership.</summary>
    public string? DeputyDirectorNames { get; init; }

    /// <summary>Work items with this business area on the work item record.</summary>
    public int LinkedWorkItemCount { get; init; }

    public int RisksOpen { get; init; }
    public int RisksOverdue { get; init; }
    public int RisksClosed { get; init; }

    public int RiskScoreHighest { get; init; }
    public int RiskScoreElevated { get; init; }
    public int RiskScoreMedium { get; init; }
    public int RiskScoreLower { get; init; }

    public int IssuesOpen { get; init; }
    public int IssuesOverdue { get; init; }
    public int IssuesClosed { get; init; }

    public int IssuesLow { get; init; }
    public int IssuesMedium { get; init; }
    public int IssuesHigh { get; init; }
    public int IssuesCritical { get; init; }
}

/// <summary>Tier governance matrix at <c>/modern/raid/tier</c> (score bands, issue severity, drill-down).</summary>
public sealed class ModernRaidTierReportingViewModel
{
    public string? Search { get; init; }
    public int? ProjectId { get; init; }
    public int? BusinessAreaId { get; init; }
    public bool RaidBusinessAreaExplicitNone { get; init; }
    public int? DirectorateId { get; init; }

    public string RaidBusinessAreaQueryFragment
    {
        get
        {
            if (BusinessAreaId is int ba && ba > 0)
                return $"businessAreaId={ba}";
            if (RaidBusinessAreaExplicitNone)
                return "businessAreaId=0";
            return "";
        }
    }

    public IReadOnlyList<RaidLookupOptionVm> ProjectOptions { get; init; } = Array.Empty<RaidLookupOptionVm>();
    public IReadOnlyList<RaidLookupOptionVm> BusinessAreaOptions { get; init; } = Array.Empty<RaidLookupOptionVm>();
    public IReadOnlyList<RaidLookupOptionVm> DirectorateOptions { get; init; } = Array.Empty<RaidLookupOptionVm>();

    public IReadOnlyList<RaidTierReportingMatrixRowVm> MatrixRows { get; init; } = Array.Empty<RaidTierReportingMatrixRowVm>();

    public string? DrillEndpoint { get; init; }
    public string? ExportExcelUrl { get; init; }
}

public sealed class RaidTierReportingMatrixRowVm
{
    public int TierId { get; init; }
    public string TierName { get; init; } = "";
    public bool IsProposedTier { get; init; }

    public int RisksOpen { get; init; }
    public int RisksOverdue { get; init; }
    public int RisksClosed { get; init; }

    public int RiskScoreHighest { get; init; }
    public int RiskScoreElevated { get; init; }
    public int RiskScoreMedium { get; init; }
    public int RiskScoreLower { get; init; }

    public int IssuesOpen { get; init; }
    public int IssuesOverdue { get; init; }
    public int IssuesClosed { get; init; }

    public int IssuesLow { get; init; }
    public int IssuesMedium { get; init; }
    public int IssuesHigh { get; init; }
    public int IssuesCritical { get; init; }
}

public sealed class RaidTierReportingDrillResponseVm
{
    public string Title { get; init; } = "";
    public IReadOnlyList<RaidTierReportingDrillItemVm> Items { get; init; } = Array.Empty<RaidTierReportingDrillItemVm>();
}

public sealed class RaidTierReportingDrillItemVm
{
    public string Kind { get; init; } = "";
    public int Id { get; init; }
    public string Title { get; init; } = "";
    /// <summary>Legacy one-line summary; prefer structured fields for UI.</summary>
    public string? Meta { get; init; }
    public string Url { get; init; } = "";
    public string? Reference { get; init; }
    public string? BusinessAreaLabel { get; init; }
    public string? RelationKind { get; init; }
    public int? RelationProjectId { get; init; }
    public string? RelationTarget { get; init; }
    public string? Status { get; init; }
    public string? Owner { get; init; }
    public string? LikelihoodLabel { get; init; }
    public string? ImpactLabel { get; init; }
    public int? RiskScore { get; init; }
    public string? IssueSeverityLabel { get; init; }
}

public sealed class ModernRaidAssumptionsPageViewModel
{
    public IReadOnlyList<ModernRaidAssumptionRow> Rows { get; init; } = Array.Empty<ModernRaidAssumptionRow>();
    public int TotalFiltered { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 25;
    public int TotalPages => TotalFiltered <= 0 ? 1 : (int)Math.Ceiling(TotalFiltered / (double)Math.Max(PageSize, 1));

    public string? Search { get; init; }
    public int? ProjectId { get; init; }
    public int? CriticalityId { get; init; }
    public int? StatusId { get; init; }
    public string ActiveTab { get; init; } = "open";
    public int OpenCount { get; init; }
    public int ClosedCount { get; init; }
    public int AllCount { get; init; }

    public IReadOnlyList<RaidLookupOptionVm> ProjectOptions { get; init; } = Array.Empty<RaidLookupOptionVm>();
    public IReadOnlyList<RaidLookupOptionVm> CriticalityOptions { get; init; } = Array.Empty<RaidLookupOptionVm>();
    public IReadOnlyList<RaidLookupOptionVm> StatusOptions { get; init; } = Array.Empty<RaidLookupOptionVm>();
}

