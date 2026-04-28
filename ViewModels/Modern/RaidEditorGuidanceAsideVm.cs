namespace Compass.ViewModels.Modern;

/// <summary>Aside panel for RAID risk/issue editors: contextual field guidance and (for risks) matrix JSON.</summary>
public sealed class RaidEditorGuidanceAsideVm
{
    /// <summary>"risk" or "issue".</summary>
    public required string Mode { get; init; }

    /// <summary>Serialized object <c>{ likelihood: { id: score }, impact: { id: score } }</c> for live score; risk mode only.</summary>
    public string? MatrixScoresJson { get; init; }
}
