namespace Compass.Models.ViewModels;

/// <summary>ViewModel for the shared standard page header (caption, title, version | status).</summary>
public class StandardHeaderViewModel
{
    public string Title { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string StatusLabel { get; set; } = string.Empty;
    public string? StatusBadgeClass { get; set; }
}
