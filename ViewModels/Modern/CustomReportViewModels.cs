using Compass.Models;

namespace Compass.ViewModels.Modern;

/// <summary>Dashboard listing the current user's reports and public reports.</summary>
public sealed class CustomReportsDashboardViewModel
{
    public List<CustomReportListItem> MyReports { get; set; } = new();
    public List<CustomReportListItem> PublicReports { get; set; } = new();
}

public sealed class CustomReportListItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public CustomReportVisibility Visibility { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsOwner { get; set; }
}

/// <summary>View model for the create/edit report builder form.</summary>
public sealed class CustomReportBuilderViewModel
{
    public int? ReportId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public CustomReportVisibility Visibility { get; set; } = CustomReportVisibility.Private;
    public string ReportingPeriod { get; set; } = "calendar-month";

    public List<int> SelectedWorkItemIds { get; set; } = new();
    public List<int> SelectedServiceRegisterIds { get; set; } = new();

    public List<CustomReportSectionItem> Sections { get; set; } = new();

    public List<ScopePickerItem> AvailableWorkItems { get; set; } = new();
    public List<ScopePickerItem> AvailableServiceRegisterItems { get; set; } = new();

    public List<AvailableSectionItem> AvailableSections { get; set; } = new();
}

public sealed class ScopePickerItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Status { get; set; }
    public string? BusinessArea { get; set; }
}

public sealed class CustomReportSectionItem
{
    public string SectionType { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsVisible { get; set; } = true;
}

public sealed class AvailableSectionItem
{
    public string SectionType { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
}

/// <summary>View model for the rendered custom report view page.</summary>
public sealed class CustomReportViewViewModel
{
    public int ReportId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public bool IsOwner { get; set; }
    public CustomReportVisibility Visibility { get; set; }
    public DateTime CreatedAt { get; set; }

    public string ReportingPeriod { get; set; } = "calendar-month";
    public int ReportYear { get; set; }
    public int ReportMonth { get; set; }
    public string MonthName { get; set; } = string.Empty;

    public List<CustomReportSectionItem> Sections { get; set; } = new();

    public List<ScopeWorkItem> WorkItems { get; set; } = new();
    public List<ScopeServiceRegisterItem> ServiceRegisterItems { get; set; } = new();

    public int RiskCount { get; set; }
    public int IssueCount { get; set; }
    public int AccessibilityIssueCount { get; set; }

    public List<RagPriorityRow> RagPriorityRows { get; set; } = new();
    public List<MilestoneRow> MilestoneRows { get; set; } = new();
    public List<PathToGreenRow> PathToGreenRows { get; set; } = new();
    public List<MonthlyUpdateRow> MonthlyUpdateRows { get; set; } = new();
    public List<RagSummaryRow> RagSummaryRows { get; set; } = new();
    public List<ResourcingRow> ResourcingRows { get; set; } = new();

    public string? IntelligenceSummary { get; set; }
}

public sealed class ScopeWorkItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Status { get; set; }
    public string? Rag { get; set; }
    public string? Priority { get; set; }
    public string? BusinessArea { get; set; }
}

public sealed class ScopeServiceRegisterItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Status { get; set; }
    public string? Phase { get; set; }
    public string? BusinessArea { get; set; }
}

public sealed class RagPriorityRow
{
    public int ProjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Rag { get; set; }
    public string? Priority { get; set; }
    public string? PreviousRag { get; set; }
    public string? BusinessArea { get; set; }
}

public sealed class MilestoneRow
{
    public int ProjectId { get; set; }
    public string ProjectTitle { get; set; } = string.Empty;
    public string MilestoneTitle { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public string? Status { get; set; }
}

public sealed class PathToGreenRow
{
    public int ProjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Rag { get; set; }
    public string? PathToGreen { get; set; }
}

public sealed class MonthlyUpdateRow
{
    public int ProjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Rag { get; set; }
    public string? UpdateSummary { get; set; }
    public DateTime? SubmittedAt { get; set; }
}

public sealed class RagSummaryRow
{
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
    public string CssClass { get; set; } = string.Empty;
}

public sealed class ResourcingRow
{
    public int ProjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal? PermFte { get; set; }
    public decimal? MspFte { get; set; }
    public string? BusinessArea { get; set; }
}
