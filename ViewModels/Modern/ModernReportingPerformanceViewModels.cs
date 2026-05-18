using System;
using System.Collections.Generic;
using System.Linq;
using Compass.Models;

namespace Compass.ViewModels;

/// <summary>Reporting hub — completed commission performance rounds.</summary>
public class ModernReportingPerformanceIndexViewModel
{
    /// <summary>Set when the product catalogue could not be loaded; <see cref="Commissions"/> will be empty.</summary>
    public string? LoadError { get; set; }

    public List<ModernReportingPerformanceCommissionSummary> Commissions { get; set; } = new();
}

/// <summary>
/// Unified performance report at <c>/modern/reporting/performance</c> — commission selector, filters, summary metrics, business area breakdown.
/// </summary>
public class ModernReportingPerformancePageViewModel
{
    public string? LoadError { get; set; }

    public List<ModernReportingPerformanceCommissionSummary> Commissions { get; set; } = new();

    /// <summary>Selected commission for detail (defaults to the most recently due closed round).</summary>
    public int? SelectedCommissionId { get; set; }

    /// <summary>Filter catalogue by FIPS business area category (matches <see cref="BusinessAreaLookup"/> name).</summary>
    public int? FilterBusinessAreaId { get; set; }

    /// <summary>Filter catalogue by FIPS directorate category (matches <see cref="Division"/> name).</summary>
    public int? FilterDirectorateId { get; set; }

    /// <summary>Dropdown options (same source as monthly report).</summary>
    public List<BusinessAreaLookup> BusinessAreaOptions { get; set; } = new();

    public List<Division> DirectorateOptions { get; set; } = new();

    public ModernReportingPerformanceCommissionDetailViewModel? Detail { get; set; }

    /// <summary>Business area breakdown rows (detail is already scoped by catalogue filters).</summary>
    public IReadOnlyList<ModernReportingPerformanceBusinessAreaRow> FilteredBusinessAreas =>
        Detail == null
            ? Array.Empty<ModernReportingPerformanceBusinessAreaRow>()
            : Detail.BusinessAreas;
}

public class ModernReportingPerformanceCommissionSummary
{
    public int CommissionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime DueDate { get; set; }
    public bool IsActive { get; set; }

    public int ProductsInScope { get; set; }
    public int NotStarted { get; set; }
    public int InProgress { get; set; }
    public int Submitted { get; set; }
    public int Late { get; set; }

    /// <summary>Products that returned metrics (submitted on time or late).</summary>
    public int Returned => Submitted + Late;

    public decimal ReturnRatePercent { get; set; }

    /// <summary>Share of applicable metric cells completed, across in-scope products (0 if none).</summary>
    public decimal MetricCompletionPercent { get; set; }

    /// <summary>Completed metric values across applicable cells.</summary>
    public int CompletedMetricCells { get; set; }

    /// <summary>Total applicable metric cells (denominator for completion).</summary>
    public int ApplicableMetricCells { get; set; }
}

public class ModernReportingPerformanceCommissionDetailViewModel
{
    public Commission Commission { get; set; } = new();

    /// <summary>Set when the product catalogue could not be loaded; other fields may be empty.</summary>
    public string? LoadError { get; set; }

    public int ProductsInScope { get; set; }
    public int NotStarted { get; set; }
    public int InProgress { get; set; }
    public int Submitted { get; set; }
    public int Late { get; set; }
    public int Returned => Submitted + Late;
    public decimal ReturnRatePercent { get; set; }
    public decimal MetricCompletionPercent { get; set; }

    /// <summary>Completed metric values across applicable cells (commission-wide).</summary>
    public int CompletedMetricCells { get; set; }

    /// <summary>Total applicable metric cells (commission-wide).</summary>
    public int ApplicableMetricCells { get; set; }

    public List<ModernReportingPerformanceBusinessAreaRow> BusinessAreas { get; set; } = new();
}

public class ModernReportingPerformanceBusinessAreaRow
{
    public string BusinessArea { get; set; } = string.Empty;
    public int Total { get; set; }
    public int NotStarted { get; set; }
    public int InProgress { get; set; }
    public int Submitted { get; set; }
    public int Late { get; set; }
    public int Returned => Submitted + Late;
    public decimal ReturnRatePercent { get; set; }
    public decimal MetricCompletionPercent { get; set; }
}
