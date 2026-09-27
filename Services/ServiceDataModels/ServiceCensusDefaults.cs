namespace Compass.Services.ServiceDataModels;

/// <summary>
/// Seed defaults for the standing Service Census model.
/// Not period-based: one current assignment per applicable service, no campaign due date.
/// </summary>
public static class ServiceCensusDefaults
{
    public const string StableKey = "service-census";
    public const string Name = "Service Census";

    public const string Description =
        "Standing service census to keep core service information and register confirmation current. Available any time — not an annual or period-based return.";

    public const string OwnerDisplayName = "Central Operations";
    public const string Classification = "Official";

    /// <summary>Standing record: one current assignment per service, not a new one each period.</summary>
    public const bool IsRepeatable = false;

    /// <summary>No campaign deadline; missing due date means no deadline (not overdue).</summary>
    public static readonly int? DefaultDueDaysAfterPublish = null;

    /// <summary>No annual/periodic cadence label on the standing census.</summary>
    public static readonly string? ReviewCadenceLabel = null;

    public const bool RequiresReviewerAttestation = true;
    public const bool IsReportable = true;
}
