namespace Compass.ViewModels.Modern;

/// <summary>Footer row count + GOV.UK pagination for work register lists (All work, etc.).</summary>
public sealed class WorkRegisterPaginationFooterViewModel
{
    public bool IsPaginated { get; init; }
    public int DisplayRowStart { get; init; }
    public int DisplayRowEnd { get; init; }
    public int TotalCount { get; init; }
    public int CurrentPage { get; init; } = 1;
    public int TotalPages { get; init; } = 1;
    public required Func<int, string> PageUrl { get; init; }
    public string NavigationAriaLabel { get; init; } = "All work results";
    public string ItemNoun { get; init; } = "work items";
}
