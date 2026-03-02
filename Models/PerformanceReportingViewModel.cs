namespace Compass.Models;

/// <summary>
/// View model for the Performance reporting dashboard (commission reporting completion).
/// </summary>
public class PerformanceReportingViewModel
{
    public Commission? SelectedCommission { get; set; }
    public List<Commission> ActiveCommissions { get; set; } = new();

    /// <summary>Total products in portfolio scope (required to report).</summary>
    public int TotalProductsInScope { get; set; }

    public int SubmittedCount { get; set; }
    public int InProgressCount { get; set; }
    public int NotStartedCount { get; set; }
    public int LateCount { get; set; }

    public decimal SubmissionCompletionPercentage { get; set; }

    /// <summary>Completion by business area (products in scope per BA and submission status).</summary>
    public List<BusinessAreaCompletionItem> BusinessAreaCompletions { get; set; } = new();

    /// <summary>Per-metric completion plus data summary and any comments from submissions.</summary>
    public List<MetricCompletionSummary> MetricCompletions { get; set; } = new();

    /// <summary>Comments from commission submissions (overall submission comments).</summary>
    public List<SubmissionCommentItem> SubmissionComments { get; set; } = new();
}

public class BusinessAreaCompletionItem
{
    public string BusinessAreaName { get; set; } = string.Empty;
    public int TotalProducts { get; set; }
    public int SubmittedCount { get; set; }
    public int InProgressCount { get; set; }
    public int NotStartedCount { get; set; }
    public int LateCount { get; set; }
    public decimal CompletionPercentage { get; set; }

    /// <summary>Product titles reportable in this business area (for expandable row).</summary>
    public List<string> ProductTitles { get; set; } = new();

    /// <summary>Products in this business area with their per-metric completion for the expand panel.</summary>
    public List<ProductInBusinessAreaMetrics> Products { get; set; } = new();
}

/// <summary>Product in a business area with its metric values for the performance reporting expand panel.</summary>
public class ProductInBusinessAreaMetrics
{
    public string ProductTitle { get; set; } = string.Empty;
    public string ProductDocumentId { get; set; } = string.Empty;
    /// <summary>Submission status for display: Submitted, In progress, Not started, Late.</summary>
    public string? SubmissionStatus { get; set; }
    /// <summary>Metric values in the same order as the metrics list (one per performance metric).</summary>
    public List<MetricValueForProduct> MetricRows { get; set; } = new();
}

/// <summary>Single metric value for a product in the business area expand panel.</summary>
public class MetricValueForProduct
{
    public int PerformanceMetricId { get; set; }
    public string MetricTitle { get; set; } = string.Empty;
    public string DisplayValue { get; set; } = "–";
    public bool IsComplete { get; set; }
    public string ApplicablePhases { get; set; } = string.Empty;
    public string ApplicableTypes { get; set; } = string.Empty;
}

public class MetricCompletionSummary
{
    public int PerformanceMetricId { get; set; }
    public string Identifier { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;

    /// <summary>Comma-separated phases this metric applies to (e.g. "Discovery, Alpha, Beta"); empty = all.</summary>
    public string ApplicablePhases { get; set; } = string.Empty;

    /// <summary>Comma-separated types this metric applies to (e.g. "Website, App"); empty = all.</summary>
    public string ApplicableTypes { get; set; } = string.Empty;

    public int CompletedCount { get; set; }
    public int TotalSubmissions { get; set; }
    public decimal CompletionPercentage { get; set; }

    /// <summary>Data summary for numeric metrics (fallback text when no structured min/max/avg).</summary>
    public string? DataSummary { get; set; }

    /// <summary>Sample or aggregate of values for display (e.g. count of distinct values).</summary>
    public int ValueCount { get; set; }

    /// <summary>When metric has numeric values: minimum for display.</summary>
    public decimal? NumericMin { get; set; }

    /// <summary>When metric has numeric values: maximum for display.</summary>
    public decimal? NumericMax { get; set; }

    /// <summary>When metric has numeric values: average for display.</summary>
    public decimal? NumericAvg { get; set; }

    /// <summary>Comments/reasons from metric submissions (NotCapturedReason, ReasonForDifference).</summary>
    public List<MetricCommentItem> Comments { get; set; } = new();
}

public class MetricCommentItem
{
    public string ProductTitle { get; set; } = string.Empty;
    public string? Value { get; set; }
    public string? NotCapturedReason { get; set; }
    public string? ReasonForDifference { get; set; }
}

public class SubmissionCommentItem
{
    public string ProductTitle { get; set; } = string.Empty;
    public string? Comments { get; set; }
    public DateTime? SubmittedDate { get; set; }
    public string? SubmittedBy { get; set; }
}
