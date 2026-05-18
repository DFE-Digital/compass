using Compass.Models;

namespace Compass.Services;

public interface IReturnStatusService
{
    DateTime GetReturnDueDate(int year, int month);
    ReturnStatus CalculateReturnStatus(int year, int month, DateTime? submittedDate);
    bool IsWorkingDay(DateTime date);
    DateTime GetThirdWorkingDayOfMonth(int year, int month);
    DateTime GetLastWorkingDayOfMonth(int year, int month);
}

