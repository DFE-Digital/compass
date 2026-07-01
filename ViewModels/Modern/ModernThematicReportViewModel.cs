using Compass.Models.Modern.Work;

namespace Compass.ViewModels.Modern;

/// <summary>Delivery work by theme — tag summary and optional filtered work register.</summary>
public sealed class ModernThematicReportViewModel
{
    public IReadOnlyList<ThematicReportTagSummaryRow> SummaryRows { get; init; } = Array.Empty<ThematicReportTagSummaryRow>();

    public IReadOnlyList<ModernBusinessAreaDashboardRow> DashboardRows { get; init; } = Array.Empty<ModernBusinessAreaDashboardRow>();

    public IReadOnlyList<BusinessAreaProjectItem> ScopeProjectItems { get; init; } = Array.Empty<BusinessAreaProjectItem>();

    public int ReportYear { get; init; }

    public int ReportMonth { get; init; }

    public string MonthName { get; init; } = "";

    public int? SelectedThemeId { get; init; }

    public string? SelectedThemeName { get; init; }

    public string? SelectedThemeDescription { get; init; }

    /// <summary>Work register for the selected theme; null when no theme is selected.</summary>
    public WorkRegisterViewModel? WorkRegister { get; init; }

    public string ActiveTab { get; init; } = "active";
}

public sealed class ThematicReportTagSummaryRow
{
    public int TagId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    public int ActiveCount { get; init; }

    public int CompletedCount { get; init; }

    public int TotalCount => ActiveCount + CompletedCount;
}

public sealed class ModernThematicReportDashboard
{
    public int ReportYear { get; init; }
    public int ReportMonth { get; init; }
    public string MonthName { get; init; } = "";
    public List<ModernBusinessAreaDashboardRow> Rows { get; init; } = new();
    public List<BusinessAreaProjectItem> ScopeProjectItems { get; init; } = new();
}
