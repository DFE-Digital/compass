using Compass.Models;
using Compass.ViewModels;

namespace Compass.ViewModels.Modern;

/// <summary>Shared period navigation and filters for modern monthly reporting pages.</summary>
public class MonthlyReportPeriodToolbarViewModel
{
    public string FormAction { get; set; } = "";
    public string IdPrefix { get; set; } = "mr";

    public int ReportYear { get; set; }
    public int ReportMonth { get; set; }
    public string MonthName { get; set; } = "";

    public int MinReportYear { get; set; } = 2026;
    public int MaxReportYear { get; set; } = 2026;

    public int? FilterBusinessAreaId { get; set; }
    public int? FilterDirectorateId { get; set; }

    public List<BusinessAreaLookup> BusinessAreas { get; set; } = new();
    public List<Division> Directorates { get; set; } = new();

    public bool HasPreviousMonthNav { get; set; }
    public bool HasNextMonthNav { get; set; }
    public int? PreviousNavYear { get; set; }
    public int? PreviousNavMonth { get; set; }
    public int? NextNavYear { get; set; }
    public int? NextNavMonth { get; set; }

    /// <summary>Muted text after the period name (e.g. due date, submission window).</summary>
    public string? PeriodMeta { get; set; }

    public string FilterYearId => $"{IdPrefix}-filter-year";
    public string FilterMonthId => $"{IdPrefix}-filter-month";
    public string FilterBusinessAreaIdElement => $"{IdPrefix}-filter-business-area";
    public string FilterDirectorateIdElement => $"{IdPrefix}-filter-directorate";
    public string FilterApplyId => $"{IdPrefix}-filter-apply";

    public string PreviousNavUrl { get; set; } = "#";
    public string NextNavUrl { get; set; } = "#";

    public string? ExportUrl { get; set; }

    public static MonthlyReportPeriodToolbarViewModel FromSubmissionProgress(
        ModernMonthlySubmissionProgressViewModel m,
        string formAction,
        Func<int?, int?, int?, int?, string> navUrlBuilder,
        string? exportUrl = null)
    {
        var periodMetaParts = new List<string>();
        if (!string.IsNullOrWhiteSpace(m.SubmissionWindowDescription))
            periodMetaParts.Add(m.SubmissionWindowDescription);
        periodMetaParts.Add($"Submission window {m.SubmissionWindowStart:d MMM} – {m.SubmissionWindowEnd:d MMM yyyy}");

        var toolbar = Create(
            m,
            formAction,
            navUrlBuilder,
            idPrefix: "msp",
            periodMeta: periodMetaParts.Count == 0 ? null : " · " + string.Join(" · ", periodMetaParts));
        toolbar.ExportUrl = exportUrl;
        return toolbar;
    }

    public static MonthlyReportPeriodToolbarViewModel FromMonthlyReport(
        ModernMonthlyReportDashboardViewModel m,
        string formAction,
        Func<int?, int?, int?, int?, string> navUrlBuilder)
    {
        var periodMeta = m.MonthlyUpdateStats != null
            ? $" · Due {m.MonthlyUpdateStats.DueDate:d MMM yyyy}"
            : null;

        return Create(m, formAction, navUrlBuilder, idPrefix: "mr", periodMeta: periodMeta);
    }

    private static MonthlyReportPeriodToolbarViewModel Create(
        int reportYear,
        int reportMonth,
        string monthName,
        int minReportYear,
        int maxReportYear,
        int? filterBusinessAreaId,
        int? filterDirectorateId,
        List<BusinessAreaLookup> businessAreas,
        List<Division> directorates,
        bool hasPreviousMonthNav,
        bool hasNextMonthNav,
        int? previousNavYear,
        int? previousNavMonth,
        int? nextNavYear,
        int? nextNavMonth,
        string formAction,
        Func<int?, int?, int?, int?, string> navUrlBuilder,
        string idPrefix,
        string? periodMeta)
    {
        return new MonthlyReportPeriodToolbarViewModel
        {
            FormAction = formAction,
            IdPrefix = idPrefix,
            ReportYear = reportYear,
            ReportMonth = reportMonth,
            MonthName = monthName,
            MinReportYear = minReportYear,
            MaxReportYear = maxReportYear,
            FilterBusinessAreaId = filterBusinessAreaId,
            FilterDirectorateId = filterDirectorateId,
            BusinessAreas = businessAreas,
            Directorates = directorates,
            HasPreviousMonthNav = hasPreviousMonthNav,
            HasNextMonthNav = hasNextMonthNav,
            PreviousNavYear = previousNavYear,
            PreviousNavMonth = previousNavMonth,
            NextNavYear = nextNavYear,
            NextNavMonth = nextNavMonth,
            PeriodMeta = periodMeta,
            PreviousNavUrl = hasPreviousMonthNav && previousNavYear.HasValue && previousNavMonth.HasValue
                ? navUrlBuilder(previousNavYear, previousNavMonth, filterBusinessAreaId, filterDirectorateId)
                : "#",
            NextNavUrl = hasNextMonthNav && nextNavYear.HasValue && nextNavMonth.HasValue
                ? navUrlBuilder(nextNavYear, nextNavMonth, filterBusinessAreaId, filterDirectorateId)
                : "#"
        };
    }

    private static MonthlyReportPeriodToolbarViewModel Create(
        ModernMonthlySubmissionProgressViewModel m,
        string formAction,
        Func<int?, int?, int?, int?, string> navUrlBuilder,
        string idPrefix,
        string? periodMeta) =>
        Create(
            m.ReportYear, m.ReportMonth, m.MonthName, m.MinReportYear, m.MaxReportYear,
            m.FilterBusinessAreaId, m.FilterDirectorateId, m.BusinessAreas, m.Directorates,
            m.HasPreviousMonthNav, m.HasNextMonthNav, m.PreviousNavYear, m.PreviousNavMonth, m.NextNavYear, m.NextNavMonth,
            formAction, navUrlBuilder, idPrefix, periodMeta);

    private static MonthlyReportPeriodToolbarViewModel Create(
        ModernMonthlyReportDashboardViewModel m,
        string formAction,
        Func<int?, int?, int?, int?, string> navUrlBuilder,
        string idPrefix,
        string? periodMeta) =>
        Create(
            m.ReportYear, m.ReportMonth, m.MonthName, m.MinReportYear, m.MaxReportYear,
            m.FilterBusinessAreaId, m.FilterDirectorateId, m.BusinessAreas, m.Directorates,
            m.HasPreviousMonthNav, m.HasNextMonthNav, m.PreviousNavYear, m.PreviousNavMonth, m.NextNavYear, m.NextNavMonth,
            formAction, navUrlBuilder, idPrefix, periodMeta);

    public static MonthlyReportPeriodToolbarViewModel FromRaidMeetingReport(
        ModernRaidReviewProgressViewModel m,
        string formAction,
        Func<int?, int?, int?, int?, string> navUrlBuilder,
        string? periodMeta = null) =>
        Create(
            m.ReportYear, m.ReportMonth, m.MonthName, m.MinReportYear, m.MaxReportYear,
            m.FilterBusinessAreaId, m.FilterDirectorateId, m.BusinessAreas, m.Directorates,
            m.HasPreviousMonthNav, m.HasNextMonthNav, m.PreviousNavYear, m.PreviousNavMonth, m.NextNavYear, m.NextNavMonth,
            formAction, navUrlBuilder, idPrefix: "rr", periodMeta: periodMeta);
}
