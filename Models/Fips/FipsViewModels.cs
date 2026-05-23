using Compass.Models;
using Compass.Models.Modern.Work;
using Compass.Services.Aiss;
using Compass.ViewModels.Modern;

namespace Compass.Models.Fips;

public class FipsProductsViewModel
{
    public string ActiveTab { get; set; } = "all";

    public List<FipsProductRow> Products { get; set; } = new();

    /// <summary>Count for the Active tab: status Active, excluding enterprise services.</summary>
    public int AllProductsCount { get; set; }

    /// <summary>Count for the All tab: every product regardless of status.</summary>
    public int CatalogueProductsCount { get; set; }

    /// <summary>Count for the Enterprise tab: status Active and enterprise flag.</summary>
    public int EnterpriseProductsCount { get; set; }

    /// <summary>True when the list is driven by a cross-catalogue search (all statuses).</summary>
    public bool IsSearchResults { get; set; }

    /// <summary>Total rows in <c>CMDBProducts</c> (cheap COUNT); helps diagnose empty lists vs filters.</summary>
    public int TotalProductsInDatabase { get; set; }
    public int MyProductsCount { get; set; }
    public int NewProductsCount { get; set; }
    /// <summary>Count for Inactive tab: <see cref="CMDBProductStatus.Rejected"/> or <see cref="CMDBProductStatus.Inactive"/>.</summary>
    public int InactiveProductsCount { get; set; }
    /// <summary>Count for Retired tab: <see cref="CMDBProductStatus.Inactive"/> only (subset of the Inactive tab).</summary>
    public int RetiredCount { get; set; }

    /// <summary>CMDB sync is available on this list (Operations → Service register only).</summary>
    public bool CanSyncFromCmdb { get; set; }

    // Filter options
    public List<FipsBusinessArea> BusinessAreaOptions { get; set; } = new();
    public List<FipsChannel> ChannelOptions { get; set; } = new();
    public List<FipsUserGroup> UserGroupOptions { get; set; } = new();
    public List<FipsType> TypeOptions { get; set; } = new();
    public List<PhaseLookup> PhaseOptions { get; set; } = new();

    /// <summary>Active <see cref="BusinessAreaLookup"/> rows (for bulk checkboxes on the new products tab).</summary>
    public List<BusinessAreaLookup> BusinessAreaLookups { get; set; } = new();

    // Current filter values
    public string? Search { get; set; }
    public int? BusinessAreaId { get; set; }
    public int? ChannelId { get; set; }
    public int? UserGroupId { get; set; }
    public int? TypeId { get; set; }
    public int? PhaseId { get; set; }
    public int? CategorisationItemId { get; set; }
    public string? CategorisationItemName { get; set; }
    public int? CategorisationGroupId { get; set; }
    public string? CategorisationGroupName { get; set; }
}

public class FipsProductRow
{
    public Guid Id { get; set; }
    public int UniqueID { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? CMDBDescription { get; set; }
    public string? UserDescription { get; set; }
    public string? PhaseName { get; set; }
    public string? BusinessAreaDisplay { get; set; }
    /// <summary>Comma-separated type names (e.g. All products tab).</summary>
    public string? TypesDisplay { get; set; }
    /// <summary>Comma-separated channel names (e.g. All products tab).</summary>
    public string? ChannelsDisplay { get; set; }
    public int UserGroupCount { get; set; }
    public int ContactCount { get; set; }
    public string? ServiceOwner { get; set; }
    public string? ReportingContact { get; set; }
    public CMDBProductStatus Status { get; set; }
    public int QualityScore { get; set; }
    public int QualityScoreMax { get; set; } = 7;
}

/// <summary>Matches <see cref="Compass.Services.Fips.FipsProductListingHelper.CalculateQualityScore"/> — score out of 7 with human labels for gaps.</summary>
public sealed record FipsDataCompletionSummary(int Score, int Max, int PercentRounded, IReadOnlyList<string> OutstandingLabels);

/// <summary>Commission returns linked to this catalogue product (performance reporting).</summary>
public sealed record FipsProductPerformanceRow(
    int CommissionId,
    string CommissionName,
    DateTime PeriodEnd,
    DateTime DueDate,
    Compass.Models.CommissionSubmissionStatus Status);

/// <summary>Service assessment from SAS for a FIPS product.</summary>
public sealed record FipsProductServiceAssessmentRow(
    int AssessmentId,
    string? Type,
    string? Phase,
    string? Outcome,
    DateTime? AssessmentDate,
    string ReportUrl);

/// <summary>AISS accessibility snapshot for a single service register product.</summary>
public sealed class FipsProductAissAccessibility
{
    public string AissWebBaseUrl { get; init; } = "";
    public string? ErrorMessage { get; init; }
    public bool NotFound { get; init; }
    public AissServiceDto? Service { get; init; }
    public AissServiceSummaryDto? Summary { get; init; }
    public List<AissServiceIssueDto> OpenIssues { get; init; } = new();

    public int OpenIssueCount
    {
        get
        {
            var fromSummary = (Summary?.OpenCount ?? 0) + (Summary?.InProgressCount ?? 0);
            return fromSummary > 0 ? fromSummary : OpenIssues.Count;
        }
    }

    public bool IsOnboarded => Service != null && !NotFound;

    public string AccessibilityBadgeLabel =>
        !IsOnboarded ? "No data"
        : OpenIssueCount > 0 ? "Non-Compliant"
        : "No issues";

    public string AccessibilityBadgeColor =>
        !IsOnboarded ? "grey"
        : OpenIssueCount > 0 ? "red"
        : "green";

    public string ServicePageUrl =>
        Service is { Id: > 0 } ? $"{AissWebBaseUrl}/services/{Service.Id}" : "";

    public string OpenIssuesPageUrl =>
        Service is { Id: > 0 } ? $"{AissWebBaseUrl}/services/{Service.Id}/issues?view=open" : "";

    public string IssuePageUrl(int issueId) =>
        Service is { Id: > 0 } && issueId > 0
            ? $"{AissWebBaseUrl}/services/{Service.Id}/issues/{issueId}"
            : "";

    public string? PublicStatementUrl =>
        Service is { NumericId: > 0, StatementInstalled: true }
            ? $"{AissWebBaseUrl}/statement/{Service.NumericId}"
            : null;
}

public class FipsProductDetailViewModel
{
    public CMDBProduct Product { get; set; } = null!;
    public bool CanManage { get; set; }

    /// <summary>User is listed as a contact on this service register product (any role), or is an operations console editor.</summary>
    public bool CanEditInformation { get; set; }
    public string? CurrentUserEmail { get; set; }

    /// <summary>Same completion logic as the FIPS product list &quot;Data completion&quot; column.</summary>
    public FipsDataCompletionSummary DataCompletion { get; set; } =
        new(0, 7, 0, Array.Empty<string>());

    /// <summary>When <c>operations</c>, main nav highlights Operations → Service register (legacy; prefer <see cref="IsOperationsServiceRegisterProduct"/>).</summary>
    public string? NavContext { get; set; }

    /// <summary>Operations → Service register product page: full edit without contact checks, CMDB tools, separate from Manage FIPS.</summary>
    public bool IsOperationsServiceRegisterProduct { get; set; }

    /// <summary>Pretty-printed <see cref="CMDBProduct.LastCmdbSnapshotJson"/> for display; raw text if parsing fails.</summary>
    public string? CmdbSnapshotJsonFormatted { get; set; }

    /// <summary>True when the information tab shows the editable form (named contacts or operations console users on manage pages).</summary>
    public bool EditMode { get; set; }

    /// <summary><c>information</c>, <c>history</c>, <c>risks</c>, <c>issues</c>, <c>assumptions</c>, <c>dependencies</c>, <c>accessibility</c>, <c>assurance</c>, <c>work</c>, or <c>performance</c>.</summary>
    public string ActiveDetailTab { get; set; } = "information";

    /// <summary>Linked delivery work items (from <see cref="ProjectProduct"/>).</summary>
    public FipsProductWorkItemsPanelViewModel? WorkItemsPanel { get; set; }

    /// <summary>CMS product document id when resolved from the catalogue API (fallback: CMDB product GUID string).</summary>
    public string? ResolvedCmsDocumentId { get; set; }

    /// <summary>Commission submissions for this product (when CMS document id or FIPS id matches).</summary>
    public List<FipsProductPerformanceRow> PerformanceCommissionRows { get; set; } = new();

    /// <summary>Legacy Compass DB accessibility enrolment (superseded by <see cref="AissAccessibility"/> when AISS is configured).</summary>
    public Compass.Models.ProductAccessibility? LinkedProductAccessibility { get; set; }

    /// <summary>Live accessibility data from AISS for this product.</summary>
    public FipsProductAissAccessibility? AissAccessibility { get; set; }

    /// <summary>Service assessments from SAS for this product (<c>GET /product/{fipsId}</c>).</summary>
    public List<FipsProductServiceAssessmentRow> ServiceAssessments { get; set; } = new();

    /// <summary>Base URL for published SAS reports (no trailing slash).</summary>
    public string SasReportBaseUrl { get; set; } = "https://service-assessments.education.gov.uk/reports/report";

    /// <summary>Resolved <see cref="Compass.Models.FipsService.ServiceId"/> when the CMDB product maps to the Service table.</summary>
    public int? ResolvedFipsServiceId { get; set; }

    public List<FipsProductRaidListItem> ProductRisks { get; set; } = new();

    public List<FipsProductRaidListItem> ProductIssues { get; set; } = new();

    public List<FipsProductAssumptionListItem> ProductAssumptions { get; set; } = new();

    public List<FipsProductDependencyListItem> ProductDependencies { get; set; } = new();

    public string BusinessAreasDisplay => string.Join(", ", Product.BusinessAreas.Select(ba => ba.FipsBusinessArea.Name));
    public string ChannelsDisplay => string.Join(", ", Product.Channels.Select(c => c.FipsChannel.Name));
    public string UserGroupsDisplay => string.Join(", ", Product.UserGroups.Select(ug => ug.FipsUserGroup.Name));
    public string TypesDisplay => string.Join(", ", Product.Types.Select(t => t.FipsType.Name));

    // Edit dropdowns (populated when CanManage = true)
    public List<PhaseLookup> PhaseOptions { get; set; } = new();
    /// <summary>Admin business-area lookups (same source as <c>/modern/admin</c> → Business areas).</summary>
    public List<BusinessAreaLookup> BusinessAreaLookupOptions { get; set; } = new();

    /// <summary>Set in edit mode: lookup IDs currently linked to the product (for checkbox state).</summary>
    public HashSet<int> SelectedBusinessAreaLookupIds { get; set; } = new();
    public List<FipsChannel> ChannelOptions { get; set; } = new();
    public List<FipsUserGroup> UserGroupOptions { get; set; } = new();
    /// <summary>Active user groups in hierarchy order for edit checkboxes.</summary>
    public List<AdminFipsUserGroupRow> UserGroupTreeOptions { get; set; } = new();
    public List<FipsType> TypeOptions { get; set; } = new();

    // Audit history
    public List<FipsAuditRow> AuditHistory { get; set; } = new();

    /// <summary>Read-only lines for custom categorisation groups (group label + selected item names).</summary>
    public List<FipsCategorisationSummaryLine> CategorisationSummaryLines { get; set; } = new();

    /// <summary>Edit checkboxes grouped by categorisation dimension (when <see cref="EditMode"/>).</summary>
    public List<FipsCategorisationGroupEditSection> CategorisationGroupSections { get; set; } = new();
}

public sealed record FipsCategorisationSummaryLine(string GroupName, string ItemsDisplay);

public sealed class FipsCategorisationGroupEditSection
{
    public int GroupId { get; init; }
    public string GroupName { get; init; } = "";
    public List<FipsCategorisationItemCheckboxOption> Items { get; init; } = new();
}

public sealed class FipsCategorisationItemCheckboxOption
{
    public int ItemId { get; init; }
    public string Name { get; init; } = "";
}

/// <summary>Risk or issue row on FIPS product RAID tabs (aligns with RAID register table columns).</summary>
public class FipsProductRaidListItem
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string? Status { get; set; }
    public int? Score { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsClosed { get; set; }

    public string? BusinessAreaLabel { get; set; }
    public string RelationKind { get; set; } = "Unknown";
    public int? RelationProjectId { get; set; }
    public string? RelationTarget { get; set; }
    public string? OwnerLabel { get; set; }
    public string? LikelihoodLabel { get; set; }
    public string? ImpactLabel { get; set; }
    public string? TierName { get; set; }
    public string? PriorityLabel { get; set; }
    public string? SeverityName { get; set; }
}

public class FipsProductAssumptionListItem
{
    public int Id { get; set; }
    public string Description { get; set; } = "";
    public string? StatusLabel { get; set; }
    public string? CriticalityLabel { get; set; }
    public DateTime? ReviewDate { get; set; }
    public string? OwnerLabel { get; set; }
}

public class FipsProductDependencyListItem
{
    public int Id { get; set; }
    public string SourceLabel { get; set; } = "";
    public string TargetLabel { get; set; } = "";
    public string? LinkTypeLabel { get; set; }
    public string? Status { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class FipsAuditRow
{
    public DateTime ChangedAt { get; set; }
    public string? ChangedBy { get; set; }
    /// <summary>Audit log action, e.g. <c>update</c>.</summary>
    public string? ChangeType { get; set; }
    /// <summary>Field or area changed (maps to audit <c>EntityReference</c>).</summary>
    public string? FieldName { get; set; }
    public string? PreviousValue { get; set; }
    public string? NewValue { get; set; }
}

public class FipsAdminViewModel
{
    public string ActiveTab { get; set; } = "channels";

    public List<FipsChannel> Channels { get; set; } = new();
    public List<FipsType> Types { get; set; } = new();
    public List<FipsUserGroup> UserGroups { get; set; } = new();
    public List<FipsContactRole> ContactRoles { get; set; } = new();
    public List<FipsBusinessArea> BusinessAreas { get; set; } = new();
}
