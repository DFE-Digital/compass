using System.Collections.Generic;

namespace Compass.ViewModels;

public class StandardLandingSectionViewModel
{
    public string Key { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string Description { get; set; } = string.Empty;
    public string IconClass { get; set; } = "fas fa-file-alt";
    public string ActionText { get; set; } = string.Empty;
    public string ActionUrl { get; set; } = string.Empty;
    public string ActionAriaLabel { get; set; } = string.Empty;
    public string? SecondaryLinkText { get; set; }
    public string? SecondaryLinkUrl { get; set; }
    public IEnumerable<string> Highlights { get; set; } = new List<string>();
}
