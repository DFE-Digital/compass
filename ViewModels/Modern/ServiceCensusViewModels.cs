using Compass.Models.Fips;
using Compass.Services.Census;

namespace Compass.ViewModels.Modern;

public sealed class ServiceCensusProductListViewModel
{
    public bool ShowAll { get; set; }
    public string? Search { get; set; }
    public int YourProductCount { get; set; }
    public int AllProductCount { get; set; }
    public List<ServiceCensusProductRowViewModel> Products { get; set; } = [];
}

public sealed class ServiceCensusProductRowViewModel
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string Status { get; set; } = "";
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
    public List<ServiceCensusReportWeekPoint> Weeks { get; set; } = [];
    public List<ServiceCensusReportAnswerRow> SubmissionAnswers { get; set; } = [];
    public List<ServiceCensusReportContactRow> SubmissionContacts { get; set; } = [];

    /// <summary>Admin or Central Operations group users can download the spreadsheet.</summary>
    public bool CanExport { get; set; }
}

public sealed class ServiceCensusReportAnswerRow
{
    public string Product { get; set; } = "";
    public string Areas { get; set; } = "";
    public string CensusStatus { get; set; } = "";
    public DateTime? SubmittedAt { get; set; }
    public string? SubmittedBy { get; set; }
    public string Section { get; set; } = "";
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
    public string Product { get; set; } = "";
    public string Areas { get; set; } = "";
    public string CensusStatus { get; set; } = "";
    public string Role { get; set; } = "";
    public string? Name { get; set; }
    public string? Email { get; set; }
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
}

public sealed class ServiceCensusPromptViewModel
{
    public int OutstandingCount { get; set; }
    public Guid? SingleProductId { get; set; }
    public string? SingleProductTitle { get; set; }
}

public sealed class ServiceCensusReportWeekPoint
{
    public string Label { get; set; } = "";
    public string IsoWeek { get; set; } = "";
    public int TasksCompleted { get; set; }
    public int ProductsSubmitted { get; set; }
    public int CumulativeSubmitted { get; set; }
    public int CumulativePercent { get; set; }
}
