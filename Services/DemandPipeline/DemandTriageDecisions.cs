namespace Compass.Services.DemandPipeline;

/// <summary>Stored in <see cref="Models.DemandPipeline.DemandPipelineRequest.TriageOutcome"/> (short code).</summary>
public static class DemandTriageDecisions
{
    public const string Active = "Active";
    public const string ProgressedToDelivery = "ProgressedToDelivery";
    public const string Rejected = "Rejected";
    public const string Paused = "Paused";
    public const string Backlog = "Backlog";
    public const string Returned = "Returned";
    public const string Triaged = "Triaged";

    public static readonly IReadOnlyList<string> All =
    [
        Active,
        ProgressedToDelivery,
        Rejected,
        Paused,
        Backlog,
        Returned,
        Triaged
    ];

    public static string? MapToDemandStatus(string? decision) => decision switch
    {
        Active => null,
        ProgressedToDelivery => "Progressed to delivery",
        Rejected => "Rejected",
        Paused => "Paused",
        Backlog => "Backlog",
        Returned => "Returned",
        Triaged => "Triaged",
        _ => null
    };

    public static string Label(string? decision) => decision switch
    {
        Active => "Active (pipeline status unchanged)",
        ProgressedToDelivery => "Progressed to delivery",
        Rejected => "Rejected",
        Paused => "Paused",
        Backlog => "Backlog",
        Returned => "Returned to submitter",
        Triaged => "Triaged (no further action)",
        _ => decision ?? "—"
    };
}
