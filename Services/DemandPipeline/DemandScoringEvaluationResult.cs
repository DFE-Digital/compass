namespace Compass.Services.DemandPipeline;

/// <summary>Output of evaluating answers against the configured demand scoring framework.</summary>
public sealed class DemandScoringEvaluationResult
{
    public int RawTotal { get; init; }
    public int RawMax { get; init; }
    public int Scaled100 { get; init; }
    public string? BandCode { get; init; }
    public string? BandLabel { get; init; }

    public int ScoreStrategic { get; init; }
    public int ScoreUrgency { get; init; }
    public int ScoreFunding { get; init; }
    public int ScoreRice { get; init; }
}
