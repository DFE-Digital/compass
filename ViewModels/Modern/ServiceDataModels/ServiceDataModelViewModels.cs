using System.ComponentModel.DataAnnotations;
using Compass.Models.Fips;
using Compass.Models.ServiceDataModels;

namespace Compass.ViewModels.Modern.ServiceDataModels;

// ——— Admin list / create / detail ———

public class ServiceDataModelListViewModel
{
    public IReadOnlyList<ServiceDataModelListRowViewModel> Rows { get; set; } =
        Array.Empty<ServiceDataModelListRowViewModel>();

    public bool CanManage { get; set; }
}

public class ServiceDataModelListRowViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string StableKey { get; set; } = string.Empty;
    public string? OwnerDisplayName { get; set; }
    public string? OwnerEmail { get; set; }
    public ServiceDataModelLifecycleStatus LifecycleStatus { get; set; }
    public int? CurrentVersionNumber { get; set; }
    public ServiceDataModelLifecycleStatus? CurrentVersionStatus { get; set; }
    public int AssignedCount { get; set; }
    public DateTime UpdatedUtc { get; set; }
}

public class ServiceDataModelCreateInput
{
    [Required, MaxLength(100)]
    public string StableKey { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    [MaxLength(200)]
    public string? OwnerDisplayName { get; set; }

    [MaxLength(320)]
    public string? OwnerEmail { get; set; }

    [MaxLength(100)]
    public string? Classification { get; set; }

    public bool IsRepeatable { get; set; }
}

public class ServiceDataModelSettingsInput
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    [MaxLength(200)]
    public string? OwnerDisplayName { get; set; }

    [MaxLength(320)]
    public string? OwnerEmail { get; set; }

    [MaxLength(100)]
    public string? Classification { get; set; }

    public bool IsRepeatable { get; set; }
    public bool IsReportable { get; set; } = true;
    public bool RequiresReviewerAttestation { get; set; } = true;
    public int? ProgressLabelThresholdPercent { get; set; }
    public int? DefaultDueDaysAfterPublish { get; set; }

    [MaxLength(100)]
    public string? ReviewCadenceLabel { get; set; }
}

public class ServiceDataModelDetailViewModel
{
    public Guid Id { get; set; }
    public string StableKey { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? OwnerDisplayName { get; set; }
    public string? OwnerEmail { get; set; }
    public string? Classification { get; set; }
    public bool IsRepeatable { get; set; }
    public bool IsReportable { get; set; }
    public ServiceDataModelLifecycleStatus LifecycleStatus { get; set; }
    public bool RequiresReviewerAttestation { get; set; }
    public int? ProgressLabelThresholdPercent { get; set; }
    public int? DefaultDueDaysAfterPublish { get; set; }
    public string? ReviewCadenceLabel { get; set; }
    public ServiceDataModelApplicabilityMode ApplicabilityMode { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }
    public string? UpdatedByEmail { get; set; }

    public Guid? DraftVersionId { get; set; }
    public int? DraftVersionNumber { get; set; }
    public Guid? PublishedVersionId { get; set; }
    public int? PublishedVersionNumber { get; set; }
    /// <summary>Version whose themes/questions the overview and theme editor show (published when live).</summary>
    public Guid? StructureVersionId { get; set; }
    public int? StructureVersionNumber { get; set; }
    public ServiceDataModelLifecycleStatus? StructureVersionStatus { get; set; }
    /// <summary>True when an admin may add/edit themes and questions on the structure version.</summary>
    public bool CanEditStructure { get; set; }

    public int AssignedCount { get; set; }
    public int GroupCount { get; set; }
    public int FieldCount { get; set; }

    public IReadOnlyList<ServiceDataModelGroupViewModel> DraftGroups { get; set; } =
        Array.Empty<ServiceDataModelGroupViewModel>();

    public IReadOnlyList<ServiceDataModelApplicabilityRuleViewModel> ApplicabilityRules { get; set; } =
        Array.Empty<ServiceDataModelApplicabilityRuleViewModel>();

    public IReadOnlyList<ServiceDataModelExplicitServiceViewModel> ExplicitServices { get; set; } =
        Array.Empty<ServiceDataModelExplicitServiceViewModel>();

    public IReadOnlyList<ServiceDataModelVersionSummaryViewModel> Versions { get; set; } =
        Array.Empty<ServiceDataModelVersionSummaryViewModel>();

    public bool CanManage { get; set; }

    /// <summary>Shared base themes every census inherits (read-only on the model).</summary>
    public IReadOnlyList<CoreCensusThemeListItemViewModel> SharedCensusThemes { get; set; } =
        Array.Empty<CoreCensusThemeListItemViewModel>();
}

public class CoreCensusThemeListItemViewModel
{
    public Guid Id { get; set; }
    public string StableKey { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Guidance { get; set; }
    public int FieldCount { get; set; }
    public bool IsActive { get; set; } = true;
    public bool AlreadyOnModel { get; set; }
}

public class ServiceDataModelVersionSummaryViewModel
{
    public Guid Id { get; set; }
    public int VersionNumber { get; set; }
    public ServiceDataModelLifecycleStatus Status { get; set; }
    public string? PeriodLabel { get; set; }
    public DateTime? PublishedUtc { get; set; }
    public string? ChangeSummary { get; set; }
}

public class ServiceDataModelGroupViewModel
{
    public Guid Id { get; set; }
    public string StableKey { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Guidance { get; set; }
    public int SortOrder { get; set; }
    public bool IsDisabled { get; set; }
    public IReadOnlyList<ServiceDataModelFieldViewModel> Fields { get; set; } =
        Array.Empty<ServiceDataModelFieldViewModel>();
}

public class ServiceDataModelFieldViewModel
{
    public Guid Id { get; set; }
    public string StableKey { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string? Guidance { get; set; }
    public ServiceDataModelFieldType FieldType { get; set; }
    public bool IsMandatory { get; set; }
    public bool CountsTowardsCompletion { get; set; } = true;
    public bool IsReportable { get; set; } = true;
    public int SortOrder { get; set; }
    public bool IsDisabled { get; set; }
    public string? VisibilityRuleJson { get; set; }
    public string? CanonicalAttributeKey { get; set; }
    public string? ValidationPattern { get; set; }
    public decimal? MinNumber { get; set; }
    public decimal? MaxNumber { get; set; }

    /// <summary>Admin lookup panel key supplying choice options (e.g. fips-user-groups).</summary>
    public string? OptionsLookupKey { get; set; }

    /// <summary>
    /// Text/Multiline: multi-value list. Lookup: multiple select.
    /// </summary>
    public bool AllowMultiple { get; set; }

    public IReadOnlyList<ServiceDataModelFieldOptionViewModel> Options { get; set; } =
        Array.Empty<ServiceDataModelFieldOptionViewModel>();
}

public class ServiceDataModelFieldOptionViewModel
{
    public Guid Id { get; set; }
    public string ValueKey { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

public class ServiceDataModelGroupInput
{
    /// <summary>Server-generated on create; ignored on update. Not editable in the UI.</summary>
    [MaxLength(100)]
    public string StableKey { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Guidance { get; set; }

    public int SortOrder { get; set; }
}

public class ServiceDataModelReorderInput
{
    public IReadOnlyList<Guid> OrderedIds { get; set; } = Array.Empty<Guid>();
}

public class ServiceDataModelThemeEditorViewModel
{
    public ServiceDataModelDetailViewModel Model { get; set; } = new();
    public ServiceDataModelGroupViewModel? SelectedTheme { get; set; }
    public bool IsCreateMode { get; set; }
    public ServiceDataModelGroupInput CreateInput { get; set; } = new();
}

public class ServiceDataModelThemeEditorSideNavViewModel
{
    public Guid ModelId { get; set; }
    public Guid? SelectedThemeId { get; set; }
    public bool IsCreateMode { get; set; }
    public bool IsOverviewCurrent { get; set; }
    public bool CanManage { get; set; }
    public IReadOnlyList<ServiceDataModelGroupViewModel> Themes { get; set; } =
        Array.Empty<ServiceDataModelGroupViewModel>();
}

public class ServiceDataModelFieldInput
{
    /// <summary>Server-generated on create from the label; ignored on update. Not editable in the UI.</summary>
    [MaxLength(100)]
    public string StableKey { get; set; } = string.Empty;

    [Required, MaxLength(300)]
    public string Label { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Guidance { get; set; }

    public ServiceDataModelFieldType FieldType { get; set; } = ServiceDataModelFieldType.Text;
    public bool IsMandatory { get; set; }
    public bool CountsTowardsCompletion { get; set; } = true;
    public bool IsReportable { get; set; } = true;
    public int SortOrder { get; set; }

    [MaxLength(2000)]
    public string? VisibilityRuleJson { get; set; }

    [MaxLength(100)]
    public string? CanonicalAttributeKey { get; set; }

    [MaxLength(500)]
    public string? ValidationPattern { get; set; }

    public decimal? MinNumber { get; set; }
    public decimal? MaxNumber { get; set; }

    /// <summary>Admin lookup panel key supplying choice options (e.g. fips-user-groups).</summary>
    [MaxLength(100)]
    public string? OptionsLookupKey { get; set; }

    /// <summary>
    /// Text/Multiline: multi-value list. Lookup: multiple select.
    /// </summary>
    public bool AllowMultiple { get; set; }
}

public class ServiceDataModelFieldOptionInput
{
    [Required, MaxLength(100)]
    public string ValueKey { get; set; } = string.Empty;

    [Required, MaxLength(300)]
    public string Label { get; set; } = string.Empty;

    public int SortOrder { get; set; }
}

public class ServiceDataModelApplicabilityRuleViewModel
{
    public Guid Id { get; set; }
    public CMDBProductStatus? ProductStatus { get; set; }
    public int? PhaseId { get; set; }
    public int? FipsTypeId { get; set; }
    public int? FipsBusinessAreaId { get; set; }
    public int? FipsDirectorateId { get; set; }
}

public class ServiceDataModelExplicitServiceViewModel
{
    public Guid Id { get; set; }
    public Guid CMDBProductId { get; set; }
    public string? ProductTitle { get; set; }
    public ServiceDataModelExplicitServiceMode Mode { get; set; }
}

public class ServiceDataModelApplicabilityInput
{
    public ServiceDataModelApplicabilityMode Mode { get; set; }
    public IReadOnlyList<ServiceDataModelApplicabilityRuleInput> Rules { get; set; } =
        Array.Empty<ServiceDataModelApplicabilityRuleInput>();
    public IReadOnlyList<Guid> ExplicitIncludeProductIds { get; set; } = Array.Empty<Guid>();
    public IReadOnlyList<Guid> ExplicitExcludeProductIds { get; set; } = Array.Empty<Guid>();
}

public class ServiceDataModelApplicabilityRuleInput
{
    public CMDBProductStatus? ProductStatus { get; set; }
    public int? PhaseId { get; set; }
    public int? FipsTypeId { get; set; }
    public int? FipsBusinessAreaId { get; set; }
    public int? FipsDirectorateId { get; set; }
}

public class ServiceDataModelApplicabilityPreviewViewModel
{
    public Guid ModelId { get; set; }
    public int Count { get; set; }
    public IReadOnlyList<string> SampleProductTitles { get; set; } = Array.Empty<string>();
}

public class ServiceDataModelPublishInput
{
    [MaxLength(1000)]
    public string? ChangeSummary { get; set; }

    [MaxLength(100)]
    public string? PeriodLabel { get; set; }

    public DateTime? PeriodStartUtc { get; set; }
    public DateTime? PeriodEndUtc { get; set; }
    public int? DueDays { get; set; }
}

// ——— Census tab / work list / assignment form ———

public class CensusProductAssignmentsViewModel
{
    public Guid ProductId { get; set; }
    public string ProductTitle { get; set; } = string.Empty;
    public IReadOnlyList<CensusAssignmentListItemViewModel> Assignments { get; set; } =
        Array.Empty<CensusAssignmentListItemViewModel>();
    public bool CanEdit { get; set; }
    public bool CanReview { get; set; }
}

public class CensusAssignmentListItemViewModel
{
    public Guid AssignmentId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductTitle { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public string ModelStableKey { get; set; } = string.Empty;
    public string? PeriodLabel { get; set; }
    public ServiceDataModelAssignmentStatus Status { get; set; }
    public decimal? FieldCompletionPercent { get; set; }
    public decimal? MandatoryCompletionPercent { get; set; }
    public DateTime? DueUtc { get; set; }
    public bool IsOverdue { get; set; }
    public DateTime UpdatedUtc { get; set; }
    public DateTime? LastAnsweredUtc { get; set; }
    public DateTime? ReviewedUtc { get; set; }

    /// <summary>Service Register display fields (work list / product-style rows).</summary>
    public string? PhaseName { get; set; }
    public string? BusinessAreaDisplay { get; set; }
    public string? TypesDisplay { get; set; }
    public string? ServiceOwner { get; set; }
}

public class CensusWorkListFilterOption
{
    public string Value { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}

public class CensusWorkListViewModel
{
    /// <summary>Work-list tab: <c>your</c>, <c>all</c>, or <c>business-area</c>.</summary>
    public string Tab { get; set; } = "your";

    /// <summary>Legacy alias for <see cref="Tab"/> (older links).</summary>
    public string Scope
    {
        get => Tab;
        set => Tab = value;
    }

    public string? StatusFilter { get; set; }
    public string? Search { get; set; }
    public int? BusinessAreaId { get; set; }
    public int? ChannelId { get; set; }
    public int? UserGroupId { get; set; }
    public int? PhaseId { get; set; }
    public int? TypeId { get; set; }
    public int YourProductsCount { get; set; }
    public int AllProductsCount { get; set; }

    /// <summary>True when a published Service Census version exists.</summary>
    public bool CensusPublishedAvailable { get; set; }

    /// <summary>
    /// Same product listing shape as Service Register tabs, with standing census progress on each row.
    /// </summary>
    public Compass.Models.Fips.FipsProductsViewModel Products { get; set; } = new() { ActiveTab = "my" };

    /// <summary>
    /// When <see cref="Tab"/> is business-area, products grouped by Fips business area.
    /// </summary>
    public IReadOnlyList<CensusWorkListBusinessAreaGroupViewModel> BusinessAreaGroups { get; set; } =
        Array.Empty<CensusWorkListBusinessAreaGroupViewModel>();

    /// <summary>Legacy assignment rows — kept empty; work list is product-based.</summary>
    public IReadOnlyList<CensusAssignmentListItemViewModel> Items { get; set; } =
        Array.Empty<CensusAssignmentListItemViewModel>();

    public IReadOnlyList<CensusWorkListFilterOption> BusinessAreaOptions { get; set; } =
        Array.Empty<CensusWorkListFilterOption>();
    public IReadOnlyList<CensusWorkListFilterOption> ChannelOptions { get; set; } =
        Array.Empty<CensusWorkListFilterOption>();
    public IReadOnlyList<CensusWorkListFilterOption> UserGroupOptions { get; set; } =
        Array.Empty<CensusWorkListFilterOption>();
    public IReadOnlyList<CensusWorkListFilterOption> PhaseOptions { get; set; } =
        Array.Empty<CensusWorkListFilterOption>();
    public IReadOnlyList<CensusWorkListFilterOption> TypeOptions { get; set; } =
        Array.Empty<CensusWorkListFilterOption>();
}

public class CensusWorkListBusinessAreaGroupViewModel
{
    public string Name { get; set; } = string.Empty;
    public IReadOnlyList<Compass.Models.Fips.FipsProductRow> Products { get; set; } =
        Array.Empty<Compass.Models.Fips.FipsProductRow>();
}

public class CensusAssignmentFormViewModel
{
    public Guid AssignmentId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductTitle { get; set; } = string.Empty;
    public Guid ModelId { get; set; }
    public string ModelName { get; set; } = string.Empty;
    public string ModelStableKey { get; set; } = string.Empty;
    public Guid VersionId { get; set; }
    public int VersionNumber { get; set; }
    public string? PeriodLabel { get; set; }
    public ServiceDataModelAssignmentStatus Status { get; set; }
    public DateTime? DueUtc { get; set; }
    public bool IsOverdue { get; set; }
    public decimal? FieldCompletionPercent { get; set; }
    public decimal? MandatoryCompletionPercent { get; set; }
    public DateTime UpdatedUtc { get; set; }
    public int? CurrentRevisionNumber { get; set; }
    public bool RequiresReviewerAttestation { get; set; }
    public string? ChangesRequestedNote { get; set; }
    public string? ReviewerAttestationNote { get; set; }
    public bool CanEdit { get; set; }
    public bool CanReview { get; set; }
    public bool IsReadOnly { get; set; }
    /// <summary>When set, the theme page is showing this group; null means summary.</summary>
    public Guid? SelectedGroupId { get; set; }
    public bool IsSummaryView => SelectedGroupId == null;
    /// <summary>First incomplete theme for Continue / next-theme navigation.</summary>
    public Guid? NextIncompleteGroupId { get; set; }
    public IReadOnlyList<CensusFormGroupViewModel> Groups { get; set; } =
        Array.Empty<CensusFormGroupViewModel>();
    public IReadOnlyList<CensusProposedChangeViewModel> ProposedChanges { get; set; } =
        Array.Empty<CensusProposedChangeViewModel>();

    public CensusFormGroupViewModel? SelectedGroup =>
        SelectedGroupId == null
            ? null
            : Groups.FirstOrDefault(g => g.Id == SelectedGroupId);
}

public class CensusFormGroupViewModel
{
    public Guid Id { get; set; }
    public string StableKey { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Guidance { get; set; }
    public int SortOrder { get; set; }
    public bool IsDisabled { get; set; }
    public decimal? FieldCompletionPercent { get; set; }
    /// <summary>True when the user pressed Complete this section for this theme.</summary>
    public bool IsMarkedComplete { get; set; }
    public string ThemeStatusLabel { get; set; } = "Not started";
    public string ThemeActionLabel { get; set; } = "Start";
    /// <summary>GOV.UK tag modifier, e.g. <c>govuk-tag--green</c>.</summary>
    public string ThemeStatusTagClass { get; set; } = "govuk-tag--grey";
    public IReadOnlyList<CensusFormFieldViewModel> Fields { get; set; } =
        Array.Empty<CensusFormFieldViewModel>();
}

public class CensusFormFieldViewModel
{
    public Guid Id { get; set; }
    public string StableKey { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string? Guidance { get; set; }
    public ServiceDataModelFieldType FieldType { get; set; }
    public bool IsMandatory { get; set; }
    public bool CountsTowardsCompletion { get; set; }
    public int SortOrder { get; set; }
    public bool IsDisabled { get; set; }
    public string? VisibilityRuleJson { get; set; }
    public string? CanonicalAttributeKey { get; set; }
    public string? CurrentRegisterValue { get; set; }
    public string? ValueJson { get; set; }
    public bool IsValid { get; set; } = true;
    public string? ValidationMessage { get; set; }
    public bool IsVisible { get; set; } = true;
    /// <summary>Hidden because the Service Register already answers the question (e.g. has-public-url when URL exists).</summary>
    public bool IsSuppressedFromForm { get; set; }
    /// <summary>Canonical title — shown as read-only register text; cannot be changed in census.</summary>
    public bool IsReadOnlyRegisterValue { get; set; }
    /// <summary>Text/Multiline multi-value list, or Lookup multiple select.</summary>
    public bool AllowMultiple { get; set; }
    public IReadOnlyList<ServiceDataModelFieldOptionViewModel> Options { get; set; } =
        Array.Empty<ServiceDataModelFieldOptionViewModel>();
    /// <summary>Resolved register items for Services / ServiceLines list answers.</summary>
    public IReadOnlyList<CensusListItemViewModel> ListItems { get; set; } =
        Array.Empty<CensusListItemViewModel>();
    /// <summary>Search endpoint for list pickers (Services / ServiceLines).</summary>
    public string? ListSearchUrl { get; set; }
}

public class CensusListItemViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
}

public class CensusProposedChangeViewModel
{
    public Guid Id { get; set; }
    public Guid FieldId { get; set; }
    public string CanonicalAttributeKey { get; set; } = string.Empty;
    public string? CurrentRegisterValue { get; set; }
    public string? ProposedValue { get; set; }
    public ServiceDataModelProposedChangeStatus Status { get; set; }
}

public class CensusSaveAnswersResult
{
    public bool Success { get; set; }
    public bool NotFound { get; set; }
    public bool Forbidden { get; set; }
    public bool Conflict { get; set; }
    public string? ErrorMessage { get; set; }
    /// <summary>Non-fatal notice (e.g. census saved but register URL/description could not be updated).</summary>
    public string? WarningMessage { get; set; }
    public CensusAssignmentFormViewModel? Form { get; set; }
}

public class CensusActionResult
{
    public bool Success { get; set; }
    public bool NotFound { get; set; }
    public bool Forbidden { get; set; }
    public string? ErrorMessage { get; set; }
    public CensusAssignmentFormViewModel? Form { get; set; }
}

// ——— Reporting ———

/// <summary>Cut / group-by dimension for the Service Census completion report.</summary>
public enum ServiceDataModelCompletionReportGroupBy
{
    BusinessArea = 0,
    Directorate = 1,
    Product = 2,
    Owner = 3,
    /// <summary>Service Register phase (lifecycle).</summary>
    Phase = 4,
    /// <summary>Service Register type (service type).</summary>
    ServiceType = 5
}

public class ServiceDataModelCompletionReportFilters
{
    public Guid? ModelId { get; set; }
    public Guid? VersionId { get; set; }
    public string? PeriodLabel { get; set; }
    public int? DirectorateId { get; set; }
    public int? BusinessAreaId { get; set; }
    public CMDBProductStatus? ProductStatus { get; set; }
    public int? PhaseId { get; set; }
    /// <summary>FipsType id — Service Register "Type".</summary>
    public int? TypeId { get; set; }
    public string? OwnerEmail { get; set; }
    public string? Search { get; set; }
    public ServiceDataModelCompletionReportGroupBy GroupBy { get; set; } =
        ServiceDataModelCompletionReportGroupBy.BusinessArea;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}

public class ServiceDataModelCompletionReportFilterOption
{
    public string Value { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}

public class ServiceDataModelCompletionReportViewModel
{
    public DateTime CalculatedUtc { get; set; }
    public Guid? ModelId { get; set; }
    public string? ModelName { get; set; }
    public Guid? VersionId { get; set; }
    public int? VersionNumber { get; set; }
    public string? PeriodLabel { get; set; }

    public int EligibleCount { get; set; }
    public int AssignedCount { get; set; }
    public int NotStartedCount { get; set; }
    public int InProgressCount { get; set; }
    public int SubmittedCount { get; set; }
    public int ReviewedCount { get; set; }
    public int OverdueCount { get; set; }
    public int NotApplicableCount { get; set; }
    public int WithdrawnCount { get; set; }
    public int ChangesRequestedCount { get; set; }

    /// <summary>Reviewed ÷ eligible assigned (excludes Withdrawn/NotApplicable from denominator).</summary>
    public decimal? PortfolioCompletionPercent { get; set; }

    /// <summary>Average of assignment field completion percents (separate from portfolio %). </summary>
    public decimal? AverageFieldCompletionPercent { get; set; }

    public ServiceDataModelCompletionReportFilters Filters { get; set; } = new();

    public string GroupByDisplayName { get; set; } = "Business area";
    public bool IsProductCut => Filters.GroupBy == ServiceDataModelCompletionReportGroupBy.Product;

    public IReadOnlyList<ServiceDataModelCompletionBreakdownRowViewModel> Breakdown { get; set; } =
        Array.Empty<ServiceDataModelCompletionBreakdownRowViewModel>();

    /// <summary>Product-level cut rows (paginated). Empty when group-by is an aggregate cut.</summary>
    public IReadOnlyList<ServiceDataModelCompletionDrilldownRowViewModel> Drilldown { get; set; } =
        Array.Empty<ServiceDataModelCompletionDrilldownRowViewModel>();

    public int DrilldownTotalCount { get; set; }
    public int DrilldownPage { get; set; } = 1;
    public int DrilldownPageSize { get; set; } = 25;
    public int DrilldownTotalPages { get; set; } = 1;

    public IReadOnlyList<ServiceDataModelCompletionExportRowViewModel> ExportRows { get; set; } =
        Array.Empty<ServiceDataModelCompletionExportRowViewModel>();

    public IReadOnlyList<ServiceDataModelCompletionBreakdownExportRowViewModel> BreakdownExportRows { get; set; } =
        Array.Empty<ServiceDataModelCompletionBreakdownExportRowViewModel>();

    public IReadOnlyList<ServiceDataModelCompletionReportFilterOption> DirectorateOptions { get; set; } =
        Array.Empty<ServiceDataModelCompletionReportFilterOption>();
    public IReadOnlyList<ServiceDataModelCompletionReportFilterOption> BusinessAreaOptions { get; set; } =
        Array.Empty<ServiceDataModelCompletionReportFilterOption>();
    public IReadOnlyList<ServiceDataModelCompletionReportFilterOption> PhaseOptions { get; set; } =
        Array.Empty<ServiceDataModelCompletionReportFilterOption>();
    public IReadOnlyList<ServiceDataModelCompletionReportFilterOption> TypeOptions { get; set; } =
        Array.Empty<ServiceDataModelCompletionReportFilterOption>();
    public IReadOnlyList<ServiceDataModelCompletionReportFilterOption> VersionOptions { get; set; } =
        Array.Empty<ServiceDataModelCompletionReportFilterOption>();
    public IReadOnlyList<ServiceDataModelCompletionReportFilterOption> PeriodOptions { get; set; } =
        Array.Empty<ServiceDataModelCompletionReportFilterOption>();
}

public class ServiceDataModelCompletionBreakdownRowViewModel
{
    public string GroupKey { get; set; } = string.Empty;
    public string GroupLabel { get; set; } = string.Empty;
    public int AssignedCount { get; set; }
    public int EligibleCount { get; set; }
    public int NotStartedCount { get; set; }
    public int InProgressCount { get; set; }
    public int SubmittedCount { get; set; }
    public int ReviewedCount { get; set; }
    public int ChangesRequestedCount { get; set; }
    public int OverdueCount { get; set; }
    public int NotApplicableCount { get; set; }
    public int WithdrawnCount { get; set; }
    public decimal? PortfolioCompletionPercent { get; set; }
    public decimal? AverageFieldCompletionPercent { get; set; }
}

public class ServiceDataModelCompletionDrilldownRowViewModel
{
    public Guid AssignmentId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductTitle { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public string? PeriodLabel { get; set; }
    public ServiceDataModelAssignmentStatus Status { get; set; }
    public decimal? FieldCompletionPercent { get; set; }
    public decimal? MandatoryCompletionPercent { get; set; }
    public DateTime? DueUtc { get; set; }
    public bool IsOverdue { get; set; }
    public DateTime UpdatedUtc { get; set; }
    public string? PhaseName { get; set; }
    public string BusinessAreasDisplay { get; set; } = string.Empty;
    public string DirectoratesDisplay { get; set; } = string.Empty;
    public string TypesDisplay { get; set; } = string.Empty;
    public string OwnersDisplay { get; set; } = string.Empty;
}

public class ServiceDataModelCompletionExportRowViewModel
{
    public string ModelName { get; set; } = string.Empty;
    public string ModelStableKey { get; set; } = string.Empty;
    public int? VersionNumber { get; set; }
    public string? PeriodLabel { get; set; }
    public string ProductTitle { get; set; } = string.Empty;
    public string ProductId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal? FieldCompletionPercent { get; set; }
    public decimal? MandatoryCompletionPercent { get; set; }
    public string? DueUtc { get; set; }
    public bool IsOverdue { get; set; }
    public string? LastAnsweredUtc { get; set; }
    public string? SubmittedUtc { get; set; }
    public string? ReviewedUtc { get; set; }
    public string? PhaseName { get; set; }
    public string BusinessAreas { get; set; } = string.Empty;
    public string Directorates { get; set; } = string.Empty;
    public string Types { get; set; } = string.Empty;
    public string Owners { get; set; } = string.Empty;
}

public class ServiceDataModelCompletionBreakdownExportRowViewModel
{
    public string CutDimension { get; set; } = string.Empty;
    public string GroupLabel { get; set; } = string.Empty;
    public int AssignedCount { get; set; }
    public int EligibleCount { get; set; }
    public int NotStartedCount { get; set; }
    public int InProgressCount { get; set; }
    public int SubmittedCount { get; set; }
    public int ReviewedCount { get; set; }
    public int ChangesRequestedCount { get; set; }
    public int OverdueCount { get; set; }
    public int NotApplicableCount { get; set; }
    public int WithdrawnCount { get; set; }
    public decimal? PortfolioCompletionPercent { get; set; }
    public decimal? AverageFieldCompletionPercent { get; set; }
}

// ——— Default (shared) census themes ———

public class DefaultCensusThemesOverviewViewModel
{
    public bool CanManage { get; set; }
    public IReadOnlyList<DefaultCensusThemeRowViewModel> Themes { get; set; } =
        Array.Empty<DefaultCensusThemeRowViewModel>();
}

public class DefaultCensusThemeRowViewModel
{
    public Guid Id { get; set; }
    public string StableKey { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Guidance { get; set; }
    public int SortOrder { get; set; }
    public int FieldCount { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDisabled { get; set; }
}

public class DefaultCensusThemeEditorViewModel
{
    public DefaultCensusThemesOverviewViewModel Overview { get; set; } = new();
    public ServiceDataModelGroupViewModel? SelectedTheme { get; set; }
    public bool IsCreateMode { get; set; }
    public ServiceDataModelGroupInput CreateInput { get; set; } = new();
}

public class DefaultCensusThemeSideNavViewModel
{
    public Guid? SelectedThemeId { get; set; }
    public bool IsCreateMode { get; set; }
    public bool IsOverviewCurrent { get; set; }
    public bool CanManage { get; set; }
    public IReadOnlyList<DefaultCensusThemeRowViewModel> Themes { get; set; } =
        Array.Empty<DefaultCensusThemeRowViewModel>();
}

