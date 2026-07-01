namespace Compass.ViewModels.Modern;

public class StandardsDashboardViewModel
{
    public int PublishedDdtCount { get; set; }
    public int FunctionalStandardsCount { get; set; }
    public int InProgressAssessmentsCount { get; set; }
    public int CompleteAssessmentsYtdCount { get; set; }
    public string? ReviewCalloutHeading { get; set; }
    public IReadOnlyList<StandardsDashboardDdtRow> DdtRows { get; set; } = Array.Empty<StandardsDashboardDdtRow>();
    public IReadOnlyList<StandardsDashboardAssessmentRow> AssessmentRows { get; set; } = Array.Empty<StandardsDashboardAssessmentRow>();
}

public class StandardsDashboardDdtRow
{
    public int StandardId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string VersionDisplay { get; set; } = string.Empty;
    /// <summary>Most recent publication date for recently-published dashboard list.</summary>
    public string PublishedDisplay { get; set; } = string.Empty;
}

public class StandardsDashboardAssessmentRow
{
    /// <summary><see cref="FunctionalStandardAssessment.Id"/> — use for conduct/summary URLs, not functional standard dashboard.</summary>
    public int Id { get; set; }
    public int FunctionalStandardId { get; set; }
    public bool IsSubmitted { get; set; }
    public string AssessmentTitle { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string StandardRef { get; set; } = string.Empty;
    public string StatusTagClass { get; set; } = "dfe-c-tag--grey";
    public string StatusLabel { get; set; } = string.Empty;
    public string? AttainmentTagClass { get; set; }
    public string? AttainmentLabel { get; set; }
}

public class DdtStandardsRegisterViewModel
{
    public string Tab { get; set; } = "published";
    public int TotalCount { get; set; }
    public int YourStandardsCount { get; set; }
    public bool ShowYourStandardsTab { get; set; }
    public int PublishedCount { get; set; }
    public int DraftCount { get; set; }
    public int ReviewCount { get; set; }
    public int AwaitingPublishCount { get; set; }
    public int WithdrawnCount { get; set; }
    public List<DdtStandardsRegisterRow> Items { get; set; } = new();
    public List<string> CategoryOptions { get; set; } = new();
    public string CategoryFilter { get; set; } = "all";
    public string? Search { get; set; }
    public int? OwnerFilter { get; set; }
    public List<DdtStandardsOwnerOption> OwnerOptions { get; set; } = new();
}

public class DdtStandardsRegisterRow
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Stage { get; set; } = string.Empty;
    public string CategoryDisplay { get; set; } = string.Empty;
    public string OwnerDisplay { get; set; } = string.Empty;
    public DateTime? PublishedAt { get; set; }
    public DateTime DraftCreated { get; set; }
    public bool IsPublished { get; set; }
    public int CriteriaCount { get; set; }
    public string? WithdrawalNote { get; set; }
}

public class DdtStandardsOwnerOption
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class DdtStandardLookupOption
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class DdtStandardEditViewModel
{
    public int? Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string? Purpose { get; set; }
    public string? Criteria { get; set; }
    public string? HowToMeet { get; set; }
    public string? Governance { get; set; }
    public string? LegalBasis { get; set; }
    public bool LegalStandard { get; set; }
    public int? ValidityPeriod { get; set; }
    public string? RelatedGuidance { get; set; }
    public string Stage { get; set; } = "Draft";
    public string Version { get; set; } = "0.1.0";
    /// <summary>Published version this draft replaces, when editing an existing standard.</summary>
    public string? PublishedVersion { get; set; }
    public List<int> SelectedCategoryIds { get; set; } = new();
    public List<int> SelectedPhaseIds { get; set; } = new();
    public string? OwnerObjectIds { get; set; }
    public string? ContactObjectIds { get; set; }
    public List<DdtStandardLookupOption> Categories { get; set; } = new();
    public List<DdtStandardLookupOption> Phases { get; set; } = new();
    public bool CanEdit { get; set; }
    public bool CanSubmit { get; set; }
    public bool CanDelete { get; set; }
    public bool CanUnpublish { get; set; }
    public int WorkflowStepIndex { get; set; }
}

public class DdtStandardWorkflowContextViewModel
{
    public int StandardId { get; set; }
    public string Stage { get; set; } = string.Empty;
    public int WorkflowStepIndex { get; set; }
    public bool CanEdit { get; set; }
    public bool CanSubmit { get; set; }
    public bool CanDelete { get; set; }
    public bool CanApprove { get; set; }
    public bool CanReject { get; set; }
    public bool CanPublish { get; set; }
    public bool CanUnpublish { get; set; }
    public List<DdtStandardForumCommentRow> ForumComments { get; set; } = new();
}

/// <summary>Interruption page before creating a draft from a published standard.</summary>
public class DdtStartEditConfirmViewModel
{
    public int StandardId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
}

/// <summary>Detail page toolbar: edit access and version history link.</summary>
public class DdtStandardDetailToolbarViewModel
{
    public int StandardId { get; set; }
    public bool CanOpenEdit { get; set; }
    /// <summary>When set, navigate directly to edit this draft id.</summary>
    public int? DraftEditId { get; set; }
    /// <summary>Published standard: user can edit but must create a draft first (POST ensure).</summary>
    public bool NeedsDraftFromPublished { get; set; }
    public bool CanUnpublish { get; set; }
    public string OwnerContactDisplay { get; set; } = "the standard owner";
    public bool HasVersionHistory { get; set; }
}

public class DdtStandardHistoryViewModel
{
    public int StandardId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string CurrentVersion { get; set; } = string.Empty;
    public List<DdtStandardHistoryRow> Entries { get; set; } = new();
}

public class DdtStandardHistoryRow
{
    public string VersionNumber { get; set; } = string.Empty;
    public string? PreviousVersion { get; set; }
    public DateTime? ChangedAt { get; set; }
    public string? ChangeSummary { get; set; }
    public string? ChangeDetails { get; set; }
    public string? PublishedByName { get; set; }
    public int? ViewStandardId { get; set; }
    public bool IsCurrent { get; set; }
}

public class DdtStandardForumCommentRow
{
    public string Title { get; set; } = string.Empty;
    public string? Body { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string CommentType { get; set; } = string.Empty;
}

public class FunctionalStandardsLandingViewModel
{
    /// <summary>Tab key: <c>in-progress</c>, <c>completed</c>, or <c>all</c>.</summary>
    public string Tab { get; set; } = "in-progress";

    public List<FunctionalStandardLandingRow> Standards { get; set; } = new();
    public List<FunctionalStandardInProgressRow> InProgressAssessments { get; set; } = new();
    public List<FunctionalStandardCompletedAssessmentRow> CompletedAssessments { get; set; } = new();
}

public class FunctionalStandardLandingRow
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int ThemeCount { get; set; }
    public int CriteriaCount { get; set; }
    public int AssessmentCount { get; set; }
    public int? ContinueAssessmentId { get; set; }
    public FunctionalStandardLastCycleVm? LastCycle { get; set; }
    public List<FunctionalStandardAssessmentHistoryVm> History { get; set; } = new();
}

public class FunctionalStandardLastCycleVm
{
    public int Answered { get; set; }
    public int Total { get; set; }
    public int FullyMet { get; set; }
    public int PartiallyMet { get; set; }
    public int NotMet { get; set; }
    public string Headline { get; set; } = string.Empty;
    public string HeadlineColorVar { get; set; } = "var(--grey-4)";
    public string LastCycleTagClass { get; set; } = "dfe-c-tag--grey";
}

public class FunctionalStandardAssessmentHistoryVm
{
    public int AssessmentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;
    public bool IsComplete { get; set; }
    public string StatusLabel { get; set; } = string.Empty;
    public string StatusTagClass { get; set; } = "dfe-c-tag--grey";
    public string AttainmentHtml { get; set; } = "—";
    public string CriteriaMet { get; set; } = "—";
    public string RowUrl { get; set; } = "#";
}

public class FunctionalStandardInProgressRow
{
    public int AssessmentId { get; set; }
    public string StandardTitle { get; set; } = string.Empty;
    public string AssessmentName { get; set; } = string.Empty;
    public DateTime AssessmentDate { get; set; }
    public int CriteriaAnswered { get; set; }
    public int CriteriaTotal { get; set; }
}

public class FunctionalStandardCompletedAssessmentRow
{
    public int AssessmentId { get; set; }
    public string StandardTitle { get; set; } = string.Empty;
    public string AssessmentName { get; set; } = string.Empty;
    public DateTime AssessmentDate { get; set; }
    public DateTime SubmittedAt { get; set; }
    public int CriteriaAnswered { get; set; }
    public int CriteriaTotal { get; set; }
}

public class StandardsManagementViewModel
{
    /// <summary>Sub-area: <c>ddt</c>, <c>functional</c>, <c>categories</c>, or <c>products</c>.</summary>
    public string ActiveSection { get; set; } = "ddt";

    public int YourStandardsCount { get; set; }
    public int DraftStandardsCount { get; set; }
    public int StandardsToReviewCount { get; set; }
    public int StandardsAwaitingPublishCount { get; set; }
    public int FunctionalStandardsCount { get; set; }
    public int StandardCategoriesCount { get; set; }
    public int StandardProductsCount { get; set; }

    public string? ProductFilterStatus { get; set; }

    public List<DdtStandardsRegisterRow> PendingReviewStandards { get; set; } = new();
    public List<FunctionalStandardAdminRow> FunctionalStandards { get; set; } = new();
    public List<StandardsManagementCategoryRow> Categories { get; set; } = new();
    public List<StandardsManagementProductRow> Products { get; set; } = new();
}

public class StandardsManagementCategoryRow
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public string? Description { get; init; }
    public int SortOrder { get; init; }
    public bool IsActive { get; init; }
    public IReadOnlyList<StandardsManagementSubCategoryRow> SubCategories { get; init; } =
        Array.Empty<StandardsManagementSubCategoryRow>();
}

public class StandardsManagementSubCategoryRow
{
    public int Id { get; init; }
    public int CategoryId { get; init; }
    public string Name { get; init; } = "";
    public string? Description { get; init; }
    public int SortOrder { get; init; }
    public bool IsActive { get; init; }
}

public class StandardsManagementProductRow
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public string? Description { get; init; }
    public string? Provider { get; init; }
    public string? Version { get; init; }
    public string ApprovalStatus { get; init; } = "Pending";
    public string? DfeProductName { get; init; }
    public int LinkedStandardsCount { get; init; }
    public string? CreatedByDisplay { get; init; }
    public DateTime CreatedAt { get; init; }
}

public class StandardsManagementCategoryFormViewModel
{
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public int SortOrder { get; set; }
}

public class StandardsManagementProductFormViewModel
{
    public int? Id { get; init; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string? Provider { get; set; }
    public string? Version { get; set; }
    public string? DfeFipsProductId { get; set; }
    public string? DfeProductName { get; set; }
    public string ApprovalStatus { get; set; } = "Pending";
    public bool IsEdit => Id is > 0;
}

public class FunctionalStandardAdminRow
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int ThemeCount { get; set; }
    public int CriteriaCount { get; set; }
    public int AssessmentCount { get; set; }
}

public class FunctionalStandardDashboardViewModel
{
    public int StandardId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int ThemeCount { get; set; }
    public int CriteriaCount { get; set; }
    public int AssessmentCount { get; set; }
    public int CompletedAssessmentCount { get; set; }
    public List<FunctionalStandardThemeRow> Themes { get; set; } = new();
    public List<FunctionalStandardAssessmentHistoryVm> Assessments { get; set; } = new();
}

public class FunctionalStandardThemeRow
{
    public int ThemeId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int PracticeAreaCount { get; set; }
    public int CriteriaCount { get; set; }
}

public class StandardsAdminConfigViewModel
{
    public string ActiveTab { get; set; } = "ddt";
    public List<AdminLookupRow> DdtCategories { get; set; } = new();
    public List<AdminLookupRow> DdtSubCategories { get; set; } = new();
    public List<FunctionalStandardAdminRow> FunctionalStandards { get; set; } = new();
}
