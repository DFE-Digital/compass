namespace Compass.ViewModels.Modern;

/// <summary>One row on the risk detail “Update history” timeline.</summary>
public sealed class RiskAuditTimelineItemVm
{
    public required DateTime WhenUtc { get; init; }

    /// <summary>Short display name (audit user name or email).</summary>
    public required string ActorDisplay { get; init; }

    /// <summary>Primary summary line (what changed).</summary>
    public required string What { get; init; }

    /// <summary>Optional supporting detail (field-level summary).</summary>
    public string? Detail { get; init; }

    public bool IsAlert { get; init; }
}
