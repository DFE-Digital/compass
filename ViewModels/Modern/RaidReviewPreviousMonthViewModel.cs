namespace Compass.ViewModels.Modern;

/// <summary>Previous calendar month's review comment and plain-English changes since then.</summary>
public sealed class RaidReviewPreviousMonthViewModel
{
    public string PreviousMonthLabel { get; init; } = "";

    public bool HadReview { get; init; }

    public DateTime? ReviewedAtUtc { get; init; }

    public string? ReviewerDisplay { get; init; }

    public string? MonthlyComment { get; init; }

    /// <summary>Plain-English summary of record changes since the previous month's review (if any).</summary>
    public string? ChangesSincePreviousReview { get; init; }
}
