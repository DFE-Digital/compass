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
    public string UniqueId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string VersionDisplay { get; set; } = string.Empty;
    public string StatusTagClass { get; set; } = "dfe-c-tag--grey";
    public string StatusLabel { get; set; } = string.Empty;
    public bool UseDraftAction { get; set; }
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
    public int PublishedCount { get; set; }
    public int DraftCount { get; set; }
    public int ReviewCount { get; set; }
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
    public List<DdtStandardsRegisterRow> PendingReviewStandards { get; set; } = new();
    public List<FunctionalStandardAdminRow> FunctionalStandards { get; set; } = new();
    public List<AdminLookupRow> StandardCategories { get; set; } = new();
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
