using Compass.Models;
using Compass.Data;
using Microsoft.EntityFrameworkCore;

namespace Compass.Services;

public class MonthlyUpdateService : IMonthlyUpdateService
{
    private readonly CompassDbContext _context;

    public MonthlyUpdateService(CompassDbContext context)
    {
        _context = context;
    }

    public DateTime GetMonthlyUpdateDueDate(int year, int month)
    {
        var config = GetConfigForReportingPeriod(year, month);
        var (dueYear, dueMonth) = GetDueCalendarMonth(year, month, config);
        return ComputeCalendarDueDate(dueYear, dueMonth, config);
    }

    public string GetMonthlyUpdateDueRuleDescription(int reportingYear, int reportingMonth)
    {
        var config = GetConfigForReportingPeriod(reportingYear, reportingMonth);
        return DescribeDueRule(config);
    }

    public DateTime GetMonthlyUpdateCloseDate(int year, int month)
    {
        var nextMonth = month == 12 ? 1 : month + 1;
        var nextYear = month == 12 ? year + 1 : year;
        var nextPeriodDueDate = GetMonthlyUpdateDueDate(nextYear, nextMonth);
        return nextPeriodDueDate.AddDays(-10);
    }

    public UpdateSubmissionStatus CalculateUpdateStatus(int year, int month, DateTime? submittedDate)
    {
        if (submittedDate.HasValue)
        {
            return UpdateSubmissionStatus.Submitted;
        }

        var now = DateTime.UtcNow;
        var updateDueDate = GetMonthlyUpdateDueDate(year, month);
        var periodEndDate = new DateTime(year, month, DateTime.DaysInMonth(year, month));

        var config = GetConfigForReportingPeriod(year, month);
        var commissionDays = config?.CommissionDaysBeforeMonthEnd ?? 6;
        var commissionDate = periodEndDate.AddDays(-commissionDays);

        if (now.Date < commissionDate.Date)
        {
            return UpdateSubmissionStatus.Upcoming;
        }

        if (now <= updateDueDate.AddDays(1).AddTicks(-1))
        {
            return UpdateSubmissionStatus.Due;
        }

        return UpdateSubmissionStatus.Late;
    }

    private MonthlyUpdateDeadlineConfig? GetConfigForReportingPeriod(int year, int month)
    {
        var periodStart = new DateTime(year, month, 1);
        return _context.MonthlyUpdateDeadlineConfigs
            .AsNoTracking()
            .Where(c => c.IsActive &&
                        c.EffectiveFrom <= periodStart &&
                        (c.EffectiveUntil == null || c.EffectiveUntil >= periodStart))
            .OrderByDescending(c => c.EffectiveFrom)
            .FirstOrDefault();
    }

    private static (int Year, int Month) GetDueCalendarMonth(int reportingYear, int reportingMonth, MonthlyUpdateDeadlineConfig? config)
    {
        var offset = config?.DueMonthOffset ?? MonthlyUpdateDueMonthOffset.FollowingMonth;
        if (offset == MonthlyUpdateDueMonthOffset.ReportingMonth)
        {
            return (reportingYear, reportingMonth);
        }

        var followingMonth = reportingMonth == 12 ? 1 : reportingMonth + 1;
        var followingYear = reportingMonth == 12 ? reportingYear + 1 : reportingYear;
        return (followingYear, followingMonth);
    }

    /// <summary>
    /// Calendar date in the due month. Day is capped to the number of days in that month (e.g. day 30 or 31 in February becomes the last day of February).
    /// </summary>
    private static DateTime ComputeCalendarDueDate(int dueYear, int dueMonth, MonthlyUpdateDeadlineConfig? config)
    {
        var dayOfMonth = config?.DueCalendarDay ?? 5;
        var maxDay = DateTime.DaysInMonth(dueYear, dueMonth);
        var day = Math.Min(dayOfMonth, maxDay);
        return new DateTime(dueYear, dueMonth, day);
    }

    private static string DescribeDueRule(MonthlyUpdateDeadlineConfig? config)
    {
        var monthOffset = config?.DueMonthOffset ?? MonthlyUpdateDueMonthOffset.FollowingMonth;
        var day = config?.DueCalendarDay ?? 5;

        var monthPhrase = monthOffset == MonthlyUpdateDueMonthOffset.ReportingMonth
            ? "the reporting month"
            : "the month after the reporting month";

        return $"calendar day {day} of {monthPhrase} (if that day does not exist in the month, the last day of that month is used)";
    }
}
