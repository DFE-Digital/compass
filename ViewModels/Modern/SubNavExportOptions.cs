namespace Compass.ViewModels.Modern;

/// <summary>Export links shown in service sub-navigation (current filters vs full dataset).</summary>
public sealed class SubNavExportOptions
{
    public bool Show { get; init; }

    public string? CurrentViewUrl { get; init; }

    public string? AllDataUrl { get; init; }

    public bool HasAny =>
        Show && (!string.IsNullOrEmpty(CurrentViewUrl) || !string.IsNullOrEmpty(AllDataUrl));
}
