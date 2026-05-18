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

    public MonthlyReportingPeriodInfo? TryGetActiveExplicitReportingPeriod(int reportingYear, int reportingMonth)
    {
        var row = TryGetActiveExplicitPeriodRow(reportingYear, reportingMonth);
        return row == null
            ? null
            : new MonthlyReportingPeriodInfo(row.PeriodStart, row.PeriodEnd, row.SubmissionOpens,
                row.SubmissionCloses, row.PeriodLabel);
    }

    public bool IsMonthlyReportEditingAllowed(int reportingYear, int reportingMonth)
    {
        var d = DateTime.UtcNow.Date;
        var opens = GetSubmissionWindowOpens(reportingYear, reportingMonth);
        var closes = GetSubmissionWindowCloses(reportingYear, reportingMonth);
        if (TryGetActiveExplicitPeriodRow(reportingYear, reportingMonth) == null)
            return true;

        return d >= opens && d <= closes;
    }

    public DateTime GetSubmissionWindowOpens(int reportingYear, int reportingMonth)
    {
        var explicitRow = TryGetActiveExplicitPeriodRow(reportingYear, reportingMonth);
        if (explicitRow != null)
            return explicitRow.SubmissionOpens.Date;

        var dim = DateTime.DaysInMonth(reportingYear, reportingMonth);
        var day = Math.Min(20, dim);
        return new DateTime(reportingYear, reportingMonth, day);
    }

    public DateTime GetSubmissionWindowCloses(int reportingYear, int reportingMonth)
    {
        var explicitRow = TryGetActiveExplicitPeriodRow(reportingYear, reportingMonth);
        if (explicitRow != null)
            return explicitRow.SubmissionCloses.Date;

        return new DateTime(reportingYear, reportingMonth, DateTime.DaysInMonth(reportingYear, reportingMonth));
    }

    public DateTime GetMonthlyUpdateDueDate(int year, int month)
    {
        var explicitRow = TryGetActiveExplicitPeriodRow(year, month);
        if (explicitRow != null)
            return explicitRow.SubmissionCloses.Date;

        var config = GetConfigForReportingPeriod(year, month);
        var (dueYear, dueMonth) = GetDueCalendarMonth(year, month, config);
        return ComputeCalendarDueDate(dueYear, dueMonth, config);
    }

    public string GetMonthlyUpdateDueRuleDescription(int reportingYear, int reportingMonth)
    {
        var explicitRow = TryGetActiveExplicitPeriodRow(reportingYear, reportingMonth);
        if (explicitRow != null)
        {
            static string F(DateTime dt) =>
                dt.ToString("d MMMM yyyy", System.Globalization.CultureInfo.GetCultureInfo("en-GB"));

            return $"Submission opens {F(explicitRow.SubmissionOpens)}, closes {F(explicitRow.SubmissionCloses)}";
        }

        var config = GetConfigForReportingPeriod(reportingYear, reportingMonth);
        return DescribeDueRule(config);
    }

    public DateTime GetMonthlyUpdateCloseDate(int year, int month)
    {
        var explicitRow = TryGetActiveExplicitPeriodRow(year, month);
        if (explicitRow != null)
            return explicitRow.SubmissionCloses.Date;

        var nextMonth = month == 12 ? 1 : month + 1;
        var nextYear = month == 12 ? year + 1 : year;
        var nextPeriodDueDate = GetMonthlyUpdateDueDate(nextYear, nextMonth);
        return nextPeriodDueDate.AddDays(-10);
    }

    public (int Year, int Month) ResolveDashboardReportingPeriod(DateTime utcNow)
    {
        var reportYear = utcNow.Year;
        var reportMonth = utcNow.Month;

        if (IsMonthlyReportEditingAllowed(reportYear, reportMonth))
            return (reportYear, reportMonth);

        var prevYear = reportMonth == 1 ? reportYear - 1 : reportYear;
        var prevMonth = reportMonth == 1 ? 12 : reportMonth - 1;
        if (IsMonthlyReportEditingAllowed(prevYear, prevMonth))
            return (prevYear, prevMonth);

        var currentPeriodDueDate = GetMonthlyUpdateDueDate(reportYear, reportMonth);
        var daysUntilCurrentPeriodDueDate = (currentPeriodDueDate - utcNow).Days;
        var applicableYear = daysUntilCurrentPeriodDueDate <= 10
            ? reportYear
            : (reportMonth == 1 ? reportYear - 1 : reportYear);
        var applicableMonth = daysUntilCurrentPeriodDueDate <= 10
            ? reportMonth
            : (reportMonth == 1 ? 12 : reportMonth - 1);
        return (applicableYear, applicableMonth);
    }

    public UpdateSubmissionStatus CalculateUpdateStatus(int year, int month, DateTime? submittedDate)
    {
        if (submittedDate.HasValue)
            return UpdateSubmissionStatus.Submitted;

        var explicitRow = TryGetActiveExplicitPeriodRow(year, month);
        if (explicitRow != null)
        {
            var nowDate = DateTime.UtcNow.Date;
            var opens = explicitRow.SubmissionOpens.Date;
            var closes = explicitRow.SubmissionCloses.Date;

            if (nowDate < opens)
                return UpdateSubmissionStatus.Upcoming;
            if (nowDate > closes)
                return UpdateSubmissionStatus.Late;
            return UpdateSubmissionStatus.Due;
        }

        var updateDueDate = GetMonthlyUpdateDueDate(year, month);
        var periodEndDate = new DateTime(year, month, DateTime.DaysInMonth(year, month));

        var config = GetConfigForReportingPeriod(year, month);
        var commissionDays = config?.CommissionDaysBeforeMonthEnd ?? 6;
        var commissionDate = periodEndDate.AddDays(-commissionDays);

        var now = DateTime.UtcNow;

        if (now.Date < commissionDate.Date)
            return UpdateSubmissionStatus.Upcoming;

        if (now <= updateDueDate.AddDays(1).AddTicks(-1))
            return UpdateSubmissionStatus.Due;

        return UpdateSubmissionStatus.Late;
    }

    private WorkReportingCyclePeriod? TryGetActiveExplicitPeriodRow(int reportingYear, int reportingMonth)
    {
        var cycleId = _context.WorkReportingCycles.AsNoTracking()
            .Where(c => c.Code == WorkReportingMonthlyCycleCodes.MonthlyWorkUpdates)
            .Select(c => (int?)c.Id)
            .FirstOrDefault();
        if (!cycleId.HasValue)
            return null;

        var kUnpadded = $"{reportingYear}-{reportingMonth}";
        var kPaddedMonth = $"{reportingYear}-{reportingMonth:D2}";

        return _context.WorkReportingCyclePeriods.AsNoTracking()
            .Where(p =>
                p.ReportingCycleId == cycleId.Value &&
                p.IsActive &&
                (p.PeriodKey == kUnpadded || p.PeriodKey == kPaddedMonth))
            .FirstOrDefault();
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

    private static (int Year, int Month) GetDueCalendarMonth(int reportingYear, int reportingMonth,
        MonthlyUpdateDeadlineConfig? config)
    {
        var offset = config?.DueMonthOffset ?? MonthlyUpdateDueMonthOffset.FollowingMonth;
        if (offset == MonthlyUpdateDueMonthOffset.ReportingMonth)
            return (reportingYear, reportingMonth);

        var followingMonth = reportingMonth == 12 ? 1 : reportingMonth + 1;
        var followingYear = reportingMonth == 12 ? reportingYear + 1 : reportingYear;
        return (followingYear, followingMonth);
    }

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
