using System.ComponentModel.DataAnnotations;

namespace Compass.Models;

/// <summary>
/// Which calendar month the due date falls in, relative to the reporting period (Year/Month).
/// </summary>
public enum MonthlyUpdateDueMonthOffset
{
    /// <summary>Due date is in the same month as the reporting period.</summary>
    [Display(Name = "Reporting month (same calendar month as the period)")]
    ReportingMonth = 0,

    /// <summary>Due date is in the month after the reporting period.</summary>
    [Display(Name = "Month after the reporting period")]
    FollowingMonth = 1,
}
