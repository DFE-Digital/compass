using Compass.Models.Fips;
using Compass.Services.Census;

namespace Compass.ViewModels.Modern;

public sealed class ServiceCensusProductListViewModel
{
    public bool ShowAll { get; set; }
    public string? Search { get; set; }
    public string? ContactFilter { get; set; }
    public List<string> ContactOptions { get; set; } = [];
    public List<string> BusinessAreaOptions { get; set; } = [];
    public List<string> ContactRoles { get; set; } = [];
    public int YourProductCount { get; set; }
    public int AllProductCount { get; set; }
    public List<ServiceCensusProductRowViewModel> Products { get; set; } = [];
}

public sealed class ServiceCensusProductRowViewModel
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string Status { get; set; } = "";
    public string Areas { get; set; } = "";
    public List<string> AreaNames { get; set; } = [];
    public List<string> ContactNames { get; set; } = [];
    public Dictionary<string, string> RoleContacts { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public int TasksComplete { get; set; }
    public int TaskCount { get; set; }
    public bool Submitted { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public bool UserIsNamedContact { get; set; }

    public int Percent => TaskCount <= 0 ? 0 : (int)Math.Round(100.0 * TasksComplete / TaskCount, MidpointRounding.AwayFromZero);
}

public sealed class ServiceCensusNamedPersonViewModel
{
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string RoleName { get; set; } = "";
}

public sealed class ServiceCensusTaskListViewModel
{
    public Guid ProductId { get; set; }
    public string ProductTitle { get; set; } = "";
    public bool ShowAll { get; set; }
    public bool CanSubmit { get; set; }
    public bool AllComplete { get; set; }
    public bool UserIsNamedSubmitter { get; set; }
    public bool UserIsNamedOrAdmin { get; set; }
    public bool Submitted { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public string? SubmittedBy { get; set; }
    public string SubmitBlockedReason { get; set; } = "";
    public int TasksComplete { get; set; }
    public int TaskCount { get; set; }
    public List<ServiceCensusNamedPersonViewModel> NamedSubmitters { get; set; } = [];
    public List<ServiceCensusNavItemViewModel> Nav { get; set; } = [];
    public List<ServiceCensusTaskGroupViewModel> Groups { get; set; } = [];
    public bool CanAdminReset { get; set; }
}

public sealed class ServiceCensusTaskGroupViewModel
{
    public string Key { get; set; } = "";
    public string Name { get; set; } = "";
    public List<ServiceCensusTaskSummaryViewModel> Tasks { get; set; } = [];
}

public sealed class ServiceCensusTaskSummaryViewModel
{
    public string Key { get; set; } = "";
    public string Heading { get; set; } = "";
    public string Help { get; set; } = "";
    public string Status { get; set; } = "";
    public string StatusLabel { get; set; } = "";
}

public sealed class ServiceCensusNavItemViewModel
{
    public string Key { get; set; } = "";
    public string GroupName { get; set; } = "";
    public string Heading { get; set; } = "";
    public string Status { get; set; } = "";
    public string StatusLabel { get; set; } = "";
    public bool Current { get; set; }
}

public sealed class ServiceCensusQuestionViewModel
{
    public Guid ProductId { get; set; }
    public string ProductTitle { get; set; } = "";
    public bool ShowAll { get; set; }
    public string TaskKey { get; set; } = "";
    public string GroupName { get; set; } = "";
    public string Heading { get; set; } = "";
    public string Help { get; set; } = "";
    public CensusTaskKind Kind { get; set; }
    public CensusLookupControl LookupControl { get; set; }
    public string TitleLabel { get; set; } = "";
    public string NarrativeLabel { get; set; } = "";
    public string ItemNoun { get; set; } = "";
    public string AddLabel { get; set; } = "";
    public string EmptyLabel { get; set; } = "";
    public bool CaptureUrl { get; set; }
    public string UrlLabel { get; set; } = "Link to reference materials";
    public string UrlHint { get; set; } = "Optional. Add a URL for guidance, a dashboard, or other material relating to this question.";
    public bool PickFromRegister { get; set; }
    public string? DependencySuggestUrl { get; set; }
    public string Status { get; set; } = "";
    public bool CannotAnswer { get; set; }
    public string? CannotAnswerReason { get; set; }
    public string? LongText { get; set; }
    public string? YesNo { get; set; }
    public string? ChoiceValue { get; set; }
    public List<string> ChoiceValues { get; set; } = [];
    public string? ChoiceNarrative { get; set; }
    public List<CensusChoice> ChoiceOptions { get; set; } = [];
    public string? LookupId { get; set; }
    public List<string> SelectedLookupIds { get; set; } = [];
    public List<CensusChoice> LookupOptions { get; set; } = [];
    public List<CensusFormField> FormFields { get; set; } = [];
    public Dictionary<string, string> FormValues { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string> FormNarratives { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public string? RegisterProductUrl { get; set; }
    public List<ServiceCensusAnswerRowViewModel> Items { get; set; } = [];
    public List<ServiceCensusContactRowViewModel> Contacts { get; set; } = [];
    public bool AccessibilityEnrolled { get; set; }
    public bool AccessibilityPending { get; set; }
    public string AccessibilityStatusLabel { get; set; } = "";
    public int AccessibilityOpenIssues { get; set; }
    public string? AccessibilityNotEnrolledReason { get; set; }
    public string AccessibilityEnrolUrl { get; set; } = CensusDefinition.AccessibilityEnrolUrl;
    public string? PreviousTaskKey { get; set; }
    public string? NextTaskKey { get; set; }
    public bool Submitted { get; set; }
    public bool AllComplete { get; set; }
    public bool CanSubmit { get; set; }
    public bool UserIsNamedSubmitter { get; set; }
    public List<ServiceCensusNamedPersonViewModel> NamedSubmitters { get; set; } = [];
    public List<ServiceCensusNavItemViewModel> Nav { get; set; } = [];
    public bool CanAdminReset { get; set; }
}

public sealed class ServiceCensusAnswerRowViewModel
{
    public string? Title { get; set; }
    public string? Narrative { get; set; }
    public string? ExternalUrl { get; set; }
    public string? LookupCode { get; set; }
}

public sealed class CensusDependencySuggestion
{
    public string Code { get; init; } = "";
    public string Title { get; init; } = "";
    public string Kind { get; init; } = "";
}

public sealed class ServiceCensusContactRowViewModel
{
    public string RoleKey { get; set; } = "";
    public string RoleName { get; set; } = "";
    public string RoleHint { get; set; } = "";
    public string? CmdbName { get; set; }
    public string? CmdbEmail { get; set; }
    public string? OriginalName { get; set; }
    public string? OriginalEmail { get; set; }
    public int? SelectedUserId { get; set; }
    public string? SelectedName { get; set; }
    public string? SelectedEmail { get; set; }
    public bool IsAdded { get; set; }
    public bool IsIncorrect { get; set; }
    public string? IncorrectReason { get; set; }

    public string DisplayName => string.IsNullOrWhiteSpace(SelectedName) ? "" : SelectedName;
    public string DisplayEmail => string.IsNullOrWhiteSpace(SelectedEmail) ? "" : SelectedEmail;
}

public sealed class ServiceCensusSubmitViewModel
{
    public Guid ProductId { get; set; }
    public string ProductTitle { get; set; } = "";
    public bool ShowAll { get; set; }
    public bool CanSubmit { get; set; }
    public bool UserIsNamedOrAdmin { get; set; }
    public bool AllComplete { get; set; }
    public bool Submitted { get; set; }
    public string BlockedReason { get; set; } = "";
    public List<ServiceCensusNamedPersonViewModel> NamedSubmitters { get; set; } = [];
    public List<ServiceCensusTaskSummaryViewModel> Incomplete { get; set; } = [];
    public List<ServiceCensusNavItemViewModel> Nav { get; set; } = [];
    public bool CanAdminReset { get; set; }
}

public sealed class ServiceCensusReopenFormModel
{
    public Guid ProductId { get; set; }
    public bool ShowAll { get; set; }
    public string? TaskKey { get; set; }
    public bool CanAdminReset { get; set; }
}

public sealed class ServiceCensusSaveRequest
{
    public string Intent { get; set; } = "save";
    public bool CannotAnswer { get; set; }
    public string? CannotAnswerReason { get; set; }
    public string? LongText { get; set; }
    public string? YesNo { get; set; }
    public string? ChoiceValue { get; set; }
    public List<string> ChoiceValues { get; set; } = [];
    public string? ChoiceNarrative { get; set; }
    public string? LookupId { get; set; }
    public List<string> LookupIds { get; set; } = [];
    public Dictionary<string, string> FormValues { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string> FormNarratives { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public string? AccessibilityNotEnrolledReason { get; set; }
    public bool ItemListPosted { get; set; }
    public bool ItemListCleared { get; set; }
    public string? ItemListJson { get; set; }
    public List<ServiceCensusAnswerRowViewModel> Items { get; set; } = [];
    public List<ServiceCensusContactRowViewModel> Contacts { get; set; } = [];
}

public sealed class ServiceCensusReportViewModel
{
    public string? BusinessAreaFilter { get; set; }
    public List<string> BusinessAreaOptions { get; set; } = [];
    public int ProductCount { get; set; }
    public int TaskCount { get; set; }
    public int OverallPercent { get; set; }
    public int NotStarted { get; set; }
    public int InProgress { get; set; }
    public int QuestionsComplete { get; set; }
    public int Submitted { get; set; }
    public int TasksCompleteThisWeek { get; set; }
    public int SubmittedThisWeek { get; set; }
    public decimal AverageWeeklySubmissions { get; set; }
    public decimal PreviousAverageWeeklySubmissions { get; set; }
    public string RunRateTrendLabel { get; set; } = "";
    public string WeeksToFinishLabel { get; set; } = "";
    public List<ServiceCensusReportAreaRow> BusinessAreas { get; set; } = [];
    public List<ServiceCensusReportSectionRow> Sections { get; set; } = [];
    public List<ServiceCensusReportProductRow> Products { get; set; } = [];
    public List<string> ContactRoles { get; set; } = [];
    public List<ServiceCensusReportWeekPoint> Weeks { get; set; } = [];
    public List<ServiceCensusReportQuestionRow> Questions { get; set; } = [];
    public List<ServiceCensusReportAnswerRow> SubmissionAnswers { get; set; } = [];
    public List<ServiceCensusReportContactRow> SubmissionContacts { get; set; } = [];

    /// <summary>Admin or Central Operations group users can download the spreadsheet.</summary>
    public bool CanExport { get; set; }
}

public sealed class ServiceCensusReportQuestionRow
{
    public string Key { get; set; } = "";
    public string Section { get; set; } = "";
    public string Question { get; set; } = "";
    public int CompleteProducts { get; set; }
    public int ProductCount { get; set; }
    public int AveragePercent { get; set; }
}

public sealed class ServiceCensusReportAnswerRow
{
    public Guid ProductId { get; set; }
    public string Product { get; set; } = "";
    public string Areas { get; set; } = "";
    public string CensusStatus { get; set; } = "";
    public DateTime? SubmittedAt { get; set; }
    public string? SubmittedBy { get; set; }
    public string Section { get; set; } = "";
    public string QuestionKey { get; set; } = "";
    public string Question { get; set; } = "";
    public string QuestionStatus { get; set; } = "";
    public bool CannotAnswer { get; set; }
    public string? CannotAnswerReason { get; set; }
    public string? Item { get; set; }
    public string? Details { get; set; }
    public string? Reference { get; set; }
}

public sealed class ServiceCensusReportContactRow
{
    public Guid ProductId { get; set; }
    public string Product { get; set; } = "";
    public string Areas { get; set; } = "";
    public string CensusStatus { get; set; } = "";
    public string? RoleKey { get; set; }
    public string Role { get; set; } = "";
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? OriginalName { get; set; }
    public string? OriginalEmail { get; set; }
    public bool IsAdded { get; set; }
    public string Change { get; set; } = "";
}

public sealed class ServiceCensusReportAreaRow
{
    public string Name { get; set; } = "";
    public int ProductCount { get; set; }
    public int AveragePercent { get; set; }
    public int NotStarted { get; set; }
    public int InProgress { get; set; }
    public int QuestionsComplete { get; set; }
    public int Submitted { get; set; }
    public List<ServiceCensusReportProductRow> Products { get; set; } = [];
}

public sealed class ServiceCensusReportSectionRow
{
    public string Name { get; set; } = "";
    public int AveragePercent { get; set; }
    public int CompleteProducts { get; set; }
    public int ProductCount { get; set; }
}

public sealed class ServiceCensusReportProductRow
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string Status { get; set; } = "";
    public string Areas { get; set; } = "";
    public int TasksComplete { get; set; }
    public int TaskCount { get; set; }
    public int Percent { get; set; }
    public string CensusStatus { get; set; } = "";
    public bool Submitted { get; set; }
    public Dictionary<string, string> RoleContacts { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class ServiceCensusPromptViewModel
{
    public int OutstandingCount { get; set; }
    public Guid? SingleProductId { get; set; }
    public string? SingleProductTitle { get; set; }
}

public sealed class ServiceCensusExploreViewModel
{
    public string Dimension { get; set; } = "question";
    public string? QuestionKey { get; set; }
    public Guid? ProductId { get; set; }
    public string? BusinessAreaFilter { get; set; }
    public bool SubmittedOnly { get; set; } = true;
    public List<string> BusinessAreaOptions { get; set; } = [];
    public List<ServiceCensusReportQuestionRow> Questions { get; set; } = [];
    public List<ServiceCensusReportProductRow> Products { get; set; } = [];
    public List<ServiceCensusReportAnswerRow> Rows { get; set; } = [];
    public List<ServiceCensusExploreValueCount> ValueCounts { get; set; } = [];
    public string? SelectedQuestion { get; set; }
    public string? SelectedProduct { get; set; }
}

public sealed class ServiceCensusExploreValueCount
{
    public string Value { get; set; } = "";
    public int ProductCount { get; set; }
}

public sealed class ServiceCensusAnalysisViewModel
{
    /// <summary>overview | themes | hierarchy | contacts | area | question | product</summary>
    public string View { get; set; } = "overview";
    public string? BusinessAreaFilter { get; set; }
    public string? QuestionKey { get; set; }
    public Guid? ProductId { get; set; }
    public bool SubmittedOnly { get; set; } = true;
    /// <summary>When the selected question has a fixed option set, group the product list by answer.</summary>
    public bool GroupByAnswer { get; set; }
    public List<string> BusinessAreaOptions { get; set; } = [];
    public List<ServiceCensusReportQuestionRow> Questions { get; set; } = [];
    public List<ServiceCensusReportProductRow> Products { get; set; } = [];

    public int ProductCount { get; set; }
    public int SubmittedCount { get; set; }
    public int AnswerRowCount { get; set; }
    public List<ServiceCensusAnalysisKpi> Kpis { get; set; } = [];
    public List<ServiceCensusAnalysisTheme> Themes { get; set; } = [];

    public List<ServiceCensusAnalysisAreaRow> Areas { get; set; } = [];
    public ServiceCensusAnalysisAreaDetail? SelectedArea { get; set; }

    public ServiceCensusAnalysisQuestionDetail? SelectedQuestion { get; set; }
    public ServiceCensusAnalysisProductDetail? SelectedProduct { get; set; }

    public List<ServiceCensusAnalysisTheme> ThemeBoard { get; set; } = [];
    public List<ServiceCensusAnalysisServiceLineNode> ServiceHierarchy { get; set; } = [];
    public ServiceCensusAnalysisContactsView? Contacts { get; set; }
}

public sealed class ServiceCensusAnalysisKpi
{
    public string Label { get; set; } = "";
    public string Value { get; set; } = "";
    public string? Hint { get; set; }
    public string? QuestionKey { get; set; }
}

public sealed class ServiceCensusAnalysisTheme
{
    public string Title { get; set; } = "";
    public string Section { get; set; } = "";
    public string QuestionKey { get; set; } = "";
    public string? Description { get; set; }
    public int ProductsAnswered { get; set; }
    public int ProductsCannotAnswer { get; set; }
    public List<ServiceCensusAnalysisValueCount> Values { get; set; } = [];
}

public sealed class ServiceCensusAnalysisValueCount
{
    public string Value { get; set; } = "";
    public int ProductCount { get; set; }
    public int Percent { get; set; }
}

public sealed class ServiceCensusAnalysisAreaRow
{
    public string Name { get; set; } = "";
    public int ProductCount { get; set; }
    public int SubmittedCount { get; set; }
    public int SubmittedPercent { get; set; }
    public string TopPhase { get; set; } = "—";
    public string AiYesPercent { get; set; } = "—";
    public string AzurePercent { get; set; } = "—";
    public string IncidentProcessYesPercent { get; set; } = "—";
    public string TopTechnology { get; set; } = "—";
}

public sealed class ServiceCensusAnalysisAreaDetail
{
    public string Name { get; set; } = "";
    public int ProductCount { get; set; }
    public int SubmittedCount { get; set; }
    public List<ServiceCensusAnalysisTheme> Themes { get; set; } = [];
    public List<ServiceCensusReportProductRow> Products { get; set; } = [];
}

public sealed class ServiceCensusAnalysisQuestionDetail
{
    public string Key { get; set; } = "";
    public string Section { get; set; } = "";
    public string Question { get; set; } = "";
    public string KindLabel { get; set; } = "";
    public bool CanGroupByAnswer { get; set; }
    public bool IsMultiSelect { get; set; }
    public int ProductsInScope { get; set; }
    public int ProductsAnswered { get; set; }
    public int ProductsCannotAnswer { get; set; }
    public int ProductsMissing { get; set; }
    public double AverageItemsPerProduct { get; set; }
    public List<ServiceCensusAnalysisValueCount> ValueCounts { get; set; } = [];
    public List<ServiceCensusAnalysisProductAnswer> ProductAnswers { get; set; } = [];
    public List<ServiceCensusAnalysisAnswerGroup> AnswerGroups { get; set; } = [];
}

public sealed class ServiceCensusAnalysisAnswerGroup
{
    public string Answer { get; set; } = "";
    public int ProductCount { get; set; }
    public int Percent { get; set; }
    public List<ServiceCensusAnalysisProductAnswer> Products { get; set; } = [];
}

public sealed class ServiceCensusAnalysisProductAnswer
{
    public Guid ProductId { get; set; }
    public string Product { get; set; } = "";
    public string Areas { get; set; } = "";
    public string Status { get; set; } = "";
    public string Summary { get; set; } = "";
    public string? Details { get; set; }
    public List<string> AnswerValues { get; set; } = [];
}

public sealed class ServiceCensusAnalysisProductDetail
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string Areas { get; set; } = "";
    public string CensusStatus { get; set; } = "";
    public DateTime? SubmittedAt { get; set; }
    public string? SubmittedBy { get; set; }
    public List<ServiceCensusAnalysisSectionAnswers> Sections { get; set; } = [];
}

public sealed class ServiceCensusAnalysisSectionAnswers
{
    public string Name { get; set; } = "";
    public List<ServiceCensusAnalysisQuestionAnswers> Questions { get; set; } = [];
}

public sealed class ServiceCensusAnalysisQuestionAnswers
{
    public string Key { get; set; } = "";
    public string Question { get; set; } = "";
    public string Status { get; set; } = "";
    public bool CannotAnswer { get; set; }
    public string? CannotAnswerReason { get; set; }
    public List<ServiceCensusAnalysisAnswerItem> Items { get; set; } = [];
}

public sealed class ServiceCensusAnalysisAnswerItem
{
    public string? Item { get; set; }
    public string? Details { get; set; }
    public string? Reference { get; set; }
}

public sealed class ServiceCensusAnalysisServiceLineNode
{
    public string ServiceLineName { get; set; } = "";
    public string? Description { get; set; }
    public int ProductCount { get; set; }
    public List<ServiceCensusAnalysisServiceNode> Services { get; set; } = [];
    public List<ServiceCensusAnalysisHierarchyProduct> UnassignedProducts { get; set; } = [];
}

public sealed class ServiceCensusAnalysisServiceNode
{
    public string ServiceName { get; set; } = "";
    public int ProductCount { get; set; }
    public List<ServiceCensusAnalysisHierarchyProduct> Products { get; set; } = [];
}

public sealed class ServiceCensusAnalysisHierarchyProduct
{
    public Guid ProductId { get; set; }
    public string Product { get; set; } = "";
    public string Areas { get; set; } = "";
    public string CensusStatus { get; set; } = "";
}

public sealed class ServiceCensusAnalysisContactsView
{
    public int ChangedCount { get; set; }
    public int AddedCount { get; set; }
    public int RemovedCount { get; set; }
    public int ConfirmedCount { get; set; }
    public List<ServiceCensusAnalysisContactChange> Changes { get; set; } = [];
    public List<ServiceCensusAnalysisContactChange> AddedRoles { get; set; } = [];
    public List<ServiceCensusAnalysisContactChange> RemovedRoles { get; set; } = [];
}

public sealed class ServiceCensusAnalysisContactChange
{
    public Guid ProductId { get; set; }
    public string Product { get; set; } = "";
    public string Areas { get; set; } = "";
    public string Role { get; set; } = "";
    public string Change { get; set; } = "";
    public string? OriginalName { get; set; }
    public string? OriginalEmail { get; set; }
    public string? NewName { get; set; }
    public string? NewEmail { get; set; }
}

public sealed class ServiceCensusReportWeekPoint
{
    public string Label { get; set; } = "";
    public string IsoWeek { get; set; } = "";
    public int TasksCompleted { get; set; }
    public int ProductsInProgress { get; set; }
    public int ProductsStarted { get; set; }
    public int ProductsSubmitted { get; set; }
    public int CumulativeSubmitted { get; set; }
    public int CumulativePercent { get; set; }
}
