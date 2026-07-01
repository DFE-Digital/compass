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
    /// When explicit periods are configured, only those configured windows are editable.
    /// When none are configured, legacy deadline rules apply and this returns true.
    /// </summary>
    bool IsMonthlyReportEditingAllowed(int reportingYear, int reportingMonth);

    /// <summary>First day of the submission window (inclusive). Explicit Admin dates, else day 20 of the reporting month.</summary>
    DateTime GetSubmissionWindowOpens(int reportingYear, int reportingMonth);

    /// <summary>Last day of the submission window (inclusive). Explicit Admin closes, else last day of the reporting month.</summary>
    DateTime GetSubmissionWindowCloses(int reportingYear, int reportingMonth);

    /// <summary>
    /// Reporting period for home / work dashboards: prefers the explicit period whose
    /// submission window contains today, otherwise falls back to the legacy applicable-period heuristic.
    /// </summary>
    (int Year, int Month) ResolveDashboardReportingPeriod(DateTime utcNow);
}

public enum UpdateSubmissionStatus
{
    Upcoming,
    Due,
    Late,
    Submitted
}
