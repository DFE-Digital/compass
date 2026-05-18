using Compass.Services.Aiss;

namespace Compass.ViewModels.Modern;

public class ModernOperationsAccessibilityViewModel
{
    public AissPlatformSummary? Summary { get; init; }
    public AissCriterionTrends? Trends { get; init; }
    public string? ErrorMessage { get; init; }
    public string? TrendsError { get; init; }
}
