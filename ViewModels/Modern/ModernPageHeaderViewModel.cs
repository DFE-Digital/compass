using Microsoft.AspNetCore.Html;

namespace Compass.ViewModels.Modern;

/// <summary>Standard Modern layout page header: caption + title (+ optional lede) on the left, optional actions on the right (govuk-grid).</summary>
public class ModernPageHeaderViewModel
{
    /// <summary>Optional <c>govuk-back-link</c> rendered above the caption.</summary>
    public string? BackLinkHref { get; init; }
    public string? BackLinkText { get; init; }

    public string? Caption { get; init; }
    public string Title { get; init; } = "";
    /// <summary>e.g. govuk-heading-l (default for page titles).</summary>
    public string HeadingClass { get; init; } = "govuk-heading-l";
    /// <summary>Use "h2" when the page already has an h1 (e.g. work item chrome).</summary>
    public string HeadingTag { get; init; } = "h1";
    /// <summary>Optional row directly under the title (e.g. badges), before the lede.</summary>
    public IHtmlContent? TitleBelow { get; init; }
    public IHtmlContent? Lede { get; init; }
    public IHtmlContent? Actions { get; init; }
    /// <summary>When there are no actions, use <c>govuk-grid-column-two-thirds</c> instead of full width for title and lede.</summary>
    public bool ConstrainToTwoThirds { get; init; }
}
