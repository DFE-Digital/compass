using Microsoft.AspNetCore.Html;

namespace Compass.ViewModels.Modern;

public enum DoDontVariant
{
    Do,
    Dont
}

/// <summary>One bullet in a Do or Don't list. Use <see cref="Text"/> for plain copy, or <see cref="Content"/> for HTML (links, mixed markup). Content wins when both are set.</summary>
public sealed class DoDontItemViewModel
{
    public string? Text { get; init; }
    public IHtmlContent? Content { get; init; }
}

/// <summary>Single GOV.UK card: either Do (green tick) or Don't (red cross), NHS-style layout.</summary>
public sealed class DoDontListViewModel
{
    public DoDontVariant Variant { get; init; }
    /// <summary>Defaults to "Do" or "Don't" from <see cref="Variant"/>.</summary>
    public string? Title { get; init; }
    public IReadOnlyList<DoDontItemViewModel> Items { get; init; } = Array.Empty<DoDontItemViewModel>();
    /// <summary>Semantic heading for the card title: h2, h3, or h4.</summary>
    public string HeadingLevel { get; init; } = "h2";
}

/// <summary>Side-by-side Do and Don't cards (stacks on narrow viewports).</summary>
public sealed class DoDontPairViewModel
{
    public DoDontListViewModel Dos { get; init; } = new() { Variant = DoDontVariant.Do };
    public DoDontListViewModel Donts { get; init; } = new() { Variant = DoDontVariant.Dont };
}
