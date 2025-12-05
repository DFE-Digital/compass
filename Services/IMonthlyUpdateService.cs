using Compass.Models;

namespace Compass.Services;

public interface IMonthlyUpdateService
{
    DateTime GetMonthlyUpdateDueDate(int year, int month);
    DateTime GetMonthlyUpdateCloseDate(int year, int month);
    UpdateSubmissionStatus CalculateUpdateStatus(int year, int month, DateTime? submittedDate);
    bool IsWorkingDay(DateTime date);
    DateTime GetNthWorkingDayOfMonth(int year, int month, int workingDayNumber);
}

public enum UpdateSubmissionStatus
{
    Upcoming,
    Due,
    Late,
    Submitted
}

