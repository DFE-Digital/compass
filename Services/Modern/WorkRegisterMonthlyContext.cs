using System.Globalization;
using Compass.Services;

namespace Compass.Services.Modern;

/// <summary>Monthly reporting dates resolved once per work-register build (avoids per-row DB lookups).</summary>
public sealed class WorkRegisterMonthlyContext
{
    public int ReportYear { get; init; }
    public int ReportMonth { get; init; }
    public DateTime NowDate { get; init; }
    public MonthlyReportingPeriodInfo? ExplicitPeriod { get; init; }
    public DateTime SubmissionWindowOpens { get; init; }
    public DateTime SubmissionWindowCloses { get; init; }
    public bool SubmissionWindowOpen { get; init; }
    public DateTime CurrentDueDate { get; init; }
    public string CurrentPeriodLabel { get; init; } = "";
    public string RegisterMonthlyColumnHeader { get; init; } = "";
    public DateTime PrevMonthDate { get; init; }
    public string PrevMonthLabel { get; init; } = "";

    public static WorkRegisterMonthlyContext Create(IMonthlyUpdateService monthlyUpdateService, DateTime utcNow)
    {
        var nowDate = utcNow.Date;
        var (reportY, reportM) = monthlyUpdateService.ResolveDashboardReportingPeriod(utcNow);
        var explicitPeriod = monthlyUpdateService.TryGetActiveExplicitReportingPeriod(reportY, reportM);

        DateTime opens;
        DateTime closes;
        if (explicitPeriod != null)
        {
            opens = explicitPeriod.SubmissionOpens.Date;
            closes = explicitPeriod.SubmissionCloses.Date;
        }
        else
        {
            var dim = DateTime.DaysInMonth(reportY, reportM);
            var openDay = Math.Min(20, dim);
            opens = new DateTime(reportY, reportM, openDay, 0, 0, 0, DateTimeKind.Utc).Date;
            closes = new DateTime(reportY, reportM, dim, 0, 0, 0, DateTimeKind.Utc).Date;
        }

        var currentDueDate = explicitPeriod != null
            ? explicitPeriod.SubmissionCloses.Date
            : monthlyUpdateService.GetMonthlyUpdateDueDate(reportY, reportM);

        var currentPeriodLabel = !string.IsNullOrWhiteSpace(explicitPeriod?.PeriodLabel)
            ? explicitPeriod!.PeriodLabel.Trim()
            : new DateTime(reportY, reportM, 1).ToString("MMMM", CultureInfo.GetCultureInfo("en-GB"));

        var prevMonthDate = nowDate.AddMonths(-1);
        var monthHeader = explicitPeriod != null
            ? explicitPeriod.PeriodStart.ToString("MMM", CultureInfo.GetCultureInfo("en-GB")) + " Update"
            : new DateTime(reportY, reportM, 1).ToString("MMM", CultureInfo.GetCultureInfo("en-GB")) + " Update";

        return new WorkRegisterMonthlyContext
        {
            ReportYear = reportY,
            ReportMonth = reportM,
            NowDate = nowDate,
            ExplicitPeriod = explicitPeriod,
            SubmissionWindowOpens = opens,
            SubmissionWindowCloses = closes,
            SubmissionWindowOpen = monthlyUpdateService.IsMonthlyReportEditingAllowed(reportY, reportM),
            CurrentDueDate = currentDueDate,
            CurrentPeriodLabel = currentPeriodLabel,
            RegisterMonthlyColumnHeader = monthHeader,
            PrevMonthDate = prevMonthDate,
            PrevMonthLabel = prevMonthDate.ToString("MMM", CultureInfo.InvariantCulture),
        };
    }
}
