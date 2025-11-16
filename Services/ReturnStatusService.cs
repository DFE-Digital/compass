using Compass.Models;
using Compass.Data;
using Microsoft.EntityFrameworkCore;

namespace Compass.Services;

public class ReturnStatusService : IReturnStatusService
{
    private readonly CompassDbContext _context;

    public ReturnStatusService(CompassDbContext context)
    {
        _context = context;
    }

    public DateTime GetReturnDueDate(int year, int month)
    {
        // Check if there's an override for this reporting period
        var dueDateOverride = _context.PerformanceReportingDueDateOverrides
            .FirstOrDefault(o => o.ReportingYear == year && 
                                 o.ReportingMonth == month && 
                                 o.IsActive);
        
        if (dueDateOverride != null)
        {
            return dueDateOverride.DueDate;
        }
        
        // Default: Returns are due by the 3rd working day of the following month
        var followingMonth = month == 12 ? 1 : month + 1;
        var followingYear = month == 12 ? year + 1 : year;
        
        return GetThirdWorkingDayOfMonth(followingYear, followingMonth);
    }

    public ReturnStatus CalculateReturnStatus(int year, int month, DateTime? submittedDate)
    {
        if (submittedDate.HasValue)
        {
            return ReturnStatus.Submitted;
        }

        var now = DateTime.UtcNow;
        var returnDueDate = GetReturnDueDate(year, month);
        var periodEndDate = new DateTime(year, month, DateTime.DaysInMonth(year, month));

        // If we haven't reached the end of the reporting period yet
        if (now < periodEndDate)
        {
            return ReturnStatus.Upcoming;
        }

        // If we're past the end of the period but before the due date
        if (now >= periodEndDate && now <= returnDueDate.AddDays(1).AddTicks(-1))
        {
            return ReturnStatus.Due;
        }

        // If we're past the due date
        return ReturnStatus.Late;
    }

    public bool IsWorkingDay(DateTime date)
    {
        // Weekend check
        if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
        {
            return false;
        }

        // UK Bank Holidays (simplified - you may want to use a proper holiday API)
        var ukBankHolidays = GetUkBankHolidays(date.Year);
        if (ukBankHolidays.Contains(date.Date))
        {
            return false;
        }

        return true;
    }

    public DateTime GetThirdWorkingDayOfMonth(int year, int month)
    {
        var date = new DateTime(year, month, 1);
        var workingDaysCount = 0;

        while (workingDaysCount < 3)
        {
            if (IsWorkingDay(date))
            {
                workingDaysCount++;
                if (workingDaysCount == 3)
                {
                    return date;
                }
            }
            date = date.AddDays(1);
        }

        return date;
    }

    private List<DateTime> GetUkBankHolidays(int year)
    {
        // Comprehensive list of UK bank holidays
        var holidays = new List<DateTime>();
        
        // Fixed holidays
        holidays.Add(new DateTime(year, 1, 1));  // New Year's Day
        holidays.Add(new DateTime(year, 12, 25)); // Christmas Day
        holidays.Add(new DateTime(year, 12, 26)); // Boxing Day
        
        // Calculate Easter Sunday and related holidays
        var easter = CalculateEaster(year);
        holidays.Add(easter.AddDays(-2)); // Good Friday
        holidays.Add(easter.AddDays(1));  // Easter Monday
        
        // Early May Bank Holiday (first Monday in May)
        var earlyMay = new DateTime(year, 5, 1);
        while (earlyMay.DayOfWeek != DayOfWeek.Monday)
            earlyMay = earlyMay.AddDays(1);
        holidays.Add(earlyMay);
        
        // Spring Bank Holiday (last Monday in May)
        var springBank = new DateTime(year, 5, 31);
        while (springBank.DayOfWeek != DayOfWeek.Monday)
            springBank = springBank.AddDays(-1);
        holidays.Add(springBank);
        
        // Summer Bank Holiday (last Monday in August)
        var summerBank = new DateTime(year, 8, 31);
        while (summerBank.DayOfWeek != DayOfWeek.Monday)
            summerBank = summerBank.AddDays(-1);
        holidays.Add(summerBank);

        return holidays;
    }
    
    private DateTime CalculateEaster(int year)
    {
        // Easter calculation using the algorithm from the Council of Nicaea
        var a = year % 19;
        var b = year / 100;
        var c = year % 100;
        var d = b / 4;
        var e = b % 4;
        var f = (b + 8) / 25;
        var g = (b - f + 1) / 3;
        var h = (19 * a + b - d - g + 15) % 30;
        var i = c / 4;
        var k = c % 4;
        var l = (32 + 2 * e + 2 * i - h - k) % 7;
        var m = (a + 11 * h + 22 * l) / 451;
        var month = (h + l - 7 * m + 114) / 31;
        var day = ((h + l - 7 * m + 114) % 31) + 1;
        
        return new DateTime(year, month, day);
    }
}

