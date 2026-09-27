namespace Compass.Services.ServiceDataModels;

/// <summary>
/// Pure theme task-list status for census summary rows.
/// Complete is only set when the user presses Complete this section (persisted flag).
/// </summary>
public static class CensusThemeStatusHelper
{
    public const string NotStarted = "Not started";
    public const string InProgress = "In progress";
    public const string Complete = "Complete";
    public const string NotApplicable = "Not applicable";

    public const string TagGrey = "govuk-tag--grey";
    public const string TagBlue = "govuk-tag--blue";
    public const string TagGreen = "govuk-tag--green";

    /// <summary>
    /// Resolves status label, action hint, and GOV.UK tag modifier from the complete flag
    /// and counting-field answer progress.
    /// </summary>
    public static (string StatusLabel, string ActionLabel, string TagClass) Resolve(
        bool isMarkedComplete,
        int answeredCountingFields,
        int applicableCountingFields)
    {
        if (applicableCountingFields <= 0)
            return (NotApplicable, "Open", TagGrey);

        if (isMarkedComplete)
            return (Complete, "Review", TagGreen);

        if (answeredCountingFields <= 0)
            return (NotStarted, "Start", TagGrey);

        return (InProgress, "Continue", TagBlue);
    }
}
