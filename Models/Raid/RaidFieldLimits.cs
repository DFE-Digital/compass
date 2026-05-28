namespace Compass.Models.Raid;

/// <summary>Shared length limits for modern RAID risk and issue narrative fields.</summary>
public static class RaidFieldLimits
{
    public const int NarrativeMaxLength = 4000;

    public const int TitleMaxLength = 200;

    public static string? NormalizeNarrative(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        return trimmed.Length <= NarrativeMaxLength ? trimmed : trimmed[..NarrativeMaxLength];
    }
}
