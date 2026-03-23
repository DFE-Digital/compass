namespace Compass.Models;

/// <summary>
/// Display formatting for commission period and due dates (operational reporting UI).
/// </summary>
public static class CommissionDisplay
{
    /// <summary>
    /// e.g. "1 Feb to 28 Feb 2026" (year on end date when same calendar year).
    /// </summary>
    public static string FormatPeriod(DateTime start, DateTime end)
    {
        if (start.Year == end.Year)
            return $"{start:d MMM} to {end:d MMM yyyy}";
        return $"{start:d MMM yyyy} to {end:d MMM yyyy}";
    }

    /// <summary>e.g. "21 Apr 2026"</summary>
    public static string FormatDueDate(DateTime due) => due.ToString("d MMM yyyy");
}
