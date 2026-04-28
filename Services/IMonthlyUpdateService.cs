using Compass.Models;

namespace Compass.Services;

public interface IMonthlyUpdateService
{
    DateTime GetMonthlyUpdateDueDate(int year, int month);
    /// <summary>Human-readable due rule for the reporting period, from active config or defaults.</summary>
    string GetMonthlyUpdateDueRuleDescription(int reportingYear, int reportingMonth);
    DateTime GetMonthlyUpdateCloseDate(int year, int month);
    UpdateSubmissionStatus CalculateUpdateStatus(int year, int month, DateTime? submittedDate);

    /// <summary>
    /// Active explicit reporting period from Admin (submission opens / closes).
    /// When null, legacy <see cref="MonthlyUpdateDeadlineConfig"/> rules apply.
    /// </summary>
    MonthlyReportingPeriodInfo? TryGetActiveExplicitReportingPeriod(int reportingYear, int reportingMonth);

    /// <summary>
    /// Whether the monthly report form may be edited for this reporting month (submission window).
    /// When no explicit period exists, returns true so legacy deadline rules continue to apply.
    /// </summary>
    bool IsMonthlyReportEditingAllowed(int reportingYear, int reportingMonth);
}

public enum UpdateSubmissionStatus
{
    Upcoming,
    Due,
    Late,
    Submitted
}
