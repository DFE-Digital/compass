namespace Compass.Services.DemandPipeline;

/// <summary>
/// Demand prioritisation scorecard: raw max 89 points (15+10+22+42), displayed on a 0–100 scale.
/// Bands on 100-point scale: Must do 57–100, Could do 21–56, Do not do 0–20.
/// </summary>
public static class DemandScoringHelper
{
    public const int MaxStrategic = 15;
    public const int MaxUrgency = 10;
    public const int MaxFunding = 22;
    public const int MaxRice = 42;
    public const int MaxRawTotal = MaxStrategic + MaxUrgency + MaxFunding + MaxRice;

    public static int ClampSection(int value, int max) => Math.Clamp(value, 0, max);

    public static int RawTotal(int strategic, int urgency, int funding, int rice) =>
        ClampSection(strategic, MaxStrategic)
        + ClampSection(urgency, MaxUrgency)
        + ClampSection(funding, MaxFunding)
        + ClampSection(rice, MaxRice);

    /// <summary>Maps raw total to 0–100 using the configured raw maximum (defaults to legacy 89).</summary>
    public static int ScaleRawTo100(int rawTotal, int? rawMax = null)
    {
        var max = rawMax ?? MaxRawTotal;
        if (max <= 0) max = 1;
        var clamped = Math.Clamp(rawTotal, 0, max);
        return (int)Math.Round(clamped * 100.0 / max);
    }

    /// <summary>Band from 100-point scaled score (aligned with product rules).</summary>
    public static string BandFromScaled100(int scaled100) => scaled100 switch
    {
        >= 57 => "MustDo",
        >= 21 => "CouldDo",
        _ => "DoNotDo"
    };

    public static string BandLabel(string? band) => band switch
    {
        "MustDo" => "Must do",
        "CouldDo" => "Could do",
        "DoNotDo" => "Do not do",
        _ => band ?? "—"
    };
}
