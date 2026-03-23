using Compass.Models;

namespace Compass.Services;

public interface IMonthlyUpdateService
{
    DateTime GetMonthlyUpdateDueDate(int year, int month);
    /// <summary>Human-readable due rule for the reporting period, from active config or defaults.</summary>
    string GetMonthlyUpdateDueRuleDescription(int reportingYear, int reportingMonth);
    DateTime GetMonthlyUpdateCloseDate(int year, int month);
    UpdateSubmissionStatus CalculateUpdateStatus(int year, int month, DateTime? submittedDate);
}

public enum UpdateSubmissionStatus
{
    Upcoming,
    Due,
    Late,
    Submitted
}
