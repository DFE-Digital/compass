using Compass.Helpers;
using Compass.Models.Fips;

namespace Compass.Services.Fips;

/// <summary>Weekday window for the scheduled CMDB → service register sync.</summary>
public static class FipsCmdbSyncSchedule
{
    public const int DefaultIntervalMinutes = 10;
    public const int DefaultStartHourUk = 7;
    public const int DefaultEndHourUk = 18;

    public static int IntervalMinutes(FipsSyncConfiguration config) =>
        config.ScheduledSyncIntervalMinutes is >= 1 and <= 60
            ? config.ScheduledSyncIntervalMinutes
            : DefaultIntervalMinutes;

    public static int StartHour(FipsSyncConfiguration config) =>
        config.ScheduledSyncStartHourUk is >= 0 and <= 23
            ? config.ScheduledSyncStartHourUk
            : DefaultStartHourUk;

    public static int EndHour(FipsSyncConfiguration config)
    {
        var end = config.ScheduledSyncEndHourUk is >= 1 and <= 24
            ? config.ScheduledSyncEndHourUk
            : DefaultEndHourUk;
        return end > StartHour(config) ? end : DefaultEndHourUk;
    }

    public static bool IsWeekday(DateTime uk) =>
        uk.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday);

    public static bool IsInsideWindow(DateTime uk, FipsSyncConfiguration config)
    {
        if (!IsWeekday(uk))
            return false;

        var time = uk.TimeOfDay;
        return time >= TimeSpan.FromHours(StartHour(config))
               && time < TimeSpan.FromHours(EndHour(config));
    }

    /// <summary>Start of the clock slot that contains <paramref name="uk"/> (seconds ignored).</summary>
    public static DateTime CurrentSlotStartUk(DateTime uk, int intervalMinutes)
    {
        var minutes = uk.Hour * 60 + uk.Minute;
        var slot = minutes - (minutes % intervalMinutes);
        return uk.Date.AddMinutes(slot);
    }

    /// <summary>Next wake time strictly after <paramref name="ukNow"/>, on a weekday inside the window.</summary>
    public static DateTime NextWakeUk(DateTime ukNow, FipsSyncConfiguration config)
    {
        var interval = IntervalMinutes(config);
        var start = TimeSpan.FromHours(StartHour(config));
        var end = TimeSpan.FromHours(EndHour(config));

        if (IsWeekday(ukNow) && ukNow.TimeOfDay < end)
        {
            if (ukNow.TimeOfDay < start)
                return ukNow.Date + start;

            var minutes = ukNow.Hour * 60 + ukNow.Minute;
            var remainder = minutes % interval;
            var add = remainder == 0 ? interval : interval - remainder;
            var wake = ukNow.Date.AddMinutes(minutes + add);
            if (wake.Date == ukNow.Date && wake.TimeOfDay < end)
                return wake;
        }

        var day = ukNow.Date.AddDays(1);
        while (!IsWeekday(day))
            day = day.AddDays(1);
        return day + start;
    }

    public static string Describe(FipsSyncConfiguration config)
    {
        var interval = IntervalMinutes(config);
        var culture = System.Globalization.CultureInfo.GetCultureInfo("en-GB");
        var start = DateTime.Today.AddHours(StartHour(config)).ToString("h:mmtt", culture).ToLowerInvariant();
        var end = DateTime.Today.AddHours(EndHour(config)).ToString("h:mmtt", culture).ToLowerInvariant();
        return $"Every {interval} minutes, {start} to {end}, Monday to Friday (UK)";
    }
}
