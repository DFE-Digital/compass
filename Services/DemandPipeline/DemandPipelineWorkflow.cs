namespace Compass.Services.DemandPipeline;

/// <summary>
/// Demand request lifecycle: submitted work is enriched in Explore, scored, then triaged.
/// Stages can move forward or backward while the demand is still active in the pipeline.
/// </summary>
public static class DemandPipelineWorkflow
{
    public const string TransitionStartExplore = "start-explore";
    public const string TransitionBackToSubmitted = "back-to-submitted";
    public const string TransitionBackToExploreFromScoring = "back-to-explore";
    public const string TransitionCompleteScoring = "complete-scoring";
    public const string TransitionBackToScoring = "back-to-scoring";
    public const string TransitionForwardToTriage = "to-triage";
    public const string TransitionBackFromTriage = "back-from-triage";

    /// <summary>Ordered labels for the main pipeline strip (subset of statuses).</summary>
    public static readonly IReadOnlyList<(string Key, string Label)> MainSteps =
    [
        ("submitted", "Submitted"),
        ("explore", "Explore"),
        ("scoring", "Scoring"),
        ("scored", "Scored"),
        ("triage", "Triage")
    ];

    /// <summary>Which main step (0–4) best represents this status for UI highlighting.</summary>
    public static int MainStepIndex(string? status)
    {
        return status switch
        {
            "Submitted" or "Returned" => 0,
            "ExploratoryReview" => 1,
            "Scoring" => 2,
            "Scored" => 3,
            "TriagePending" or "Triage Pending" or "Backlog" => 4,
            "Triaged" or "Rejected" or "Paused" or "Closed" or "Progressed to delivery" or "Closed - Progressed to delivery" => 4,
            _ => 0
        };
    }

    /// <summary>Returns new status if transition is allowed; otherwise null.</summary>
    public static string? TryApplyTransition(string? currentStatus, string transition)
    {
        var t = transition.Trim();
        return (currentStatus, t) switch
        {
            ("Submitted", var x) when x == TransitionStartExplore => "ExploratoryReview",
            ("ExploratoryReview", var x) when x == TransitionBackToSubmitted => "Submitted",
            ("Scoring", var x) when x == TransitionBackToExploreFromScoring => "ExploratoryReview",
            ("Scored", var x) when x == TransitionBackToScoring => "Scoring",
            ("Scored", var x) when x == TransitionForwardToTriage => "TriagePending",
            ("TriagePending", var x) when x == TransitionBackFromTriage => "Scored",
            ("Triage Pending", var x) when x == TransitionBackFromTriage => "Scored",
            _ => null
        };
    }

    public static bool AllowsTransition(string? currentStatus, string transition)
        => TryApplyTransition(currentStatus, transition) != null;
}
