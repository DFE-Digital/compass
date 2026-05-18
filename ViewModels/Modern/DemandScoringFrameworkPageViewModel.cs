using Compass.Models.DemandPipeline;
using Compass.Services.DemandPipeline;

namespace Compass.ViewModels.Modern;

public class DemandScoringFrameworkPageViewModel
{
    public DemandPipelineRequest Demand { get; set; } = null!;
    public DemandScoringFrameworkSnapshot Framework { get; set; } = null!;
    public Dictionary<string, string> Answers { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Submission context for Context-type questions (priority outcome, pillar, portfolio).</summary>
    public string? PriorityOutcomeName { get; set; }
    public string? MissionPillarName { get; set; }
    public string? PortfolioName { get; set; }

    public int MaxForLegacy(string legacy) =>
        Framework.Sections
            .Where(s => string.Equals(s.LegacyColumn, legacy, StringComparison.OrdinalIgnoreCase))
            .Sum(s => s.MaxPoints);

    public int RawMax => Framework.Sections.Sum(s => s.MaxPoints);
}
