using Compass.Models.Modern.Work;

namespace Compass.ViewModels.Modern;

/// <summary>Delivery work by theme — tag summary and optional filtered work register.</summary>
public sealed class ModernThematicReportViewModel
{
    public IReadOnlyList<ThematicReportTagSummaryRow> SummaryRows { get; init; } = Array.Empty<ThematicReportTagSummaryRow>();

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
