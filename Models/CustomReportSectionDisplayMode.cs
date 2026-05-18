namespace Compass.Models;

/// <summary>How a data set section is presented when running a custom report from the library.</summary>
public enum CustomReportSectionDisplayMode
{
    /// <summary>Tabular grid (GOV.UK table).</summary>
    Table = 0,

    /// <summary>One card per result row (first column or first dimension as a title when possible).</summary>
    Card = 1,

    /// <summary>Bar chart: category from <c>ChartLabelColumnKey</c>, numeric series from <c>ChartValueColumnKey</c>.</summary>
    Chart = 2,
}
