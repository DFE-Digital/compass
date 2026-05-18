namespace Compass.Helpers;

/// <summary>Progress bar fill tone for reporting completion (actual %).</summary>
public static class ReportingProgressBarHelper
{
    /// <returns>CSS modifier class for <c>mr-progress-bar__fill</c> based on actual completion.</returns>
    public static string FillToneModifier(decimal actualPercent) => actualPercent switch
    {
        < 40 => "mr-progress-bar__fill--black",
        < 75 => "mr-progress-bar__fill--red",
        < 90 => "mr-progress-bar__fill--orange",
        _ => "mr-progress-bar__fill--green"
    };
}
