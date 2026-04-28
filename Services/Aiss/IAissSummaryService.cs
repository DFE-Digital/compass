namespace Compass.Services.Aiss;

public interface IAissSummaryService
{
    /// <summary>Loads the AISS platform summary, or a user-facing error message on failure or misconfiguration.</summary>
    Task<(AissPlatformSummary? Summary, string? ErrorMessage)> GetSummaryAsync(
        CancellationToken cancellationToken = default);

    /// <summary>Monthly open (end of month) and closed (in month) counts by WCAG / BP / UX band — <c>GET /api/v1/summary/trends</c>.</summary>
    Task<(AissCriterionTrends? Trends, string? ErrorMessage)> GetCriterionTrendsAsync(
        int months = 12,
        CancellationToken cancellationToken = default);
}
