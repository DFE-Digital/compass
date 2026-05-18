namespace Compass.Models;

/// <summary>JSON payload for a report that can combine several COMPASS data sets with per-field (dimension / measure) choice.</summary>
public sealed class CustomReportDefinition
{
    public List<CustomReportDataBlockDefinition> DataBlocks { get; set; } = new();
}

public sealed class CustomReportDataBlockDefinition
{
    public CustomReportDataSource DataSource { get; set; }

    /// <summary>Order of this block in the report builder and run view (lower first).</summary>
    public int SortOrder { get; set; }

    /// <summary>How this section is rendered at run time.</summary>
    public CustomReportSectionDisplayMode DisplayMode { get; set; } = CustomReportSectionDisplayMode.Table;

    /// <summary>Width in a 12-column layout grid (1–12). 12 = full width.</summary>
    public int LayoutSpan { get; set; } = 12;

    /// <summary>When <see cref="DisplayMode"/> is <see cref="CustomReportSectionDisplayMode.Chart"/>, column key for bar category / X labels.</summary>
    public string? ChartLabelColumnKey { get; set; }

    /// <summary>When <see cref="DisplayMode"/> is <see cref="CustomReportSectionDisplayMode.Chart"/>, column key for numeric values.</summary>
    public string? ChartValueColumnKey { get; set; }

    /// <summary>Column header keys to include as dimensions/attributes. Empty = all dimension columns in that data set.</summary>
    public List<string> DimensionFieldKeys { get; set; } = new();

    /// <summary>Column header keys to include as metrics/measures. Empty = all measure columns. If both this and <see cref="DimensionFieldKeys"/> are empty, all columns for the data set are returned.</summary>
    public List<string> MetricFieldKeys { get; set; } = new();
}
