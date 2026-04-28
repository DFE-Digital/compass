using Compass.Models.DemandPipeline;

namespace Compass.Services.DemandPipeline;

public interface IDemandScoringFrameworkService
{
    /// <summary>Loads active sections with questions and options, plus bands. Ensures default seed exists.</summary>
    Task<DemandScoringFrameworkSnapshot> LoadActiveFrameworkAsync(CancellationToken cancellationToken = default);

    Task EnsureDefaultFrameworkSeededAsync(CancellationToken cancellationToken = default);

    DemandScoringEvaluationResult Evaluate(
        DemandScoringFrameworkSnapshot framework,
        IReadOnlyDictionary<string, string> answers);

    /// <summary>When no JSON answers exist, use posted legacy section totals (clamped to configured section caps).</summary>
    DemandScoringEvaluationResult EvaluateFromLegacyInts(
        DemandScoringFrameworkSnapshot framework,
        int? scoreStrategic,
        int? scoreUrgency,
        int? scoreFunding,
        int? scoreRice);

    /// <summary>Returns a user-facing error message if any scored question is unanswered; otherwise null.</summary>
    string? ValidateScoringAnswersComplete(
        DemandScoringFrameworkSnapshot framework,
        IReadOnlyDictionary<string, string>? answers);
}

/// <summary>Cached framework graph for evaluation and rendering.</summary>
public sealed class DemandScoringFrameworkSnapshot
{
    public IReadOnlyList<DemandScoringFrameworkSection> Sections { get; init; } = Array.Empty<DemandScoringFrameworkSection>();
    public IReadOnlyList<DemandScoringBandDefinition> Bands { get; init; } = Array.Empty<DemandScoringBandDefinition>();
}
