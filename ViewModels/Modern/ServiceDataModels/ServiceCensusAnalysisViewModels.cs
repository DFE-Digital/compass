using Compass.Models.Fips;
using Compass.Models.ServiceDataModels;

namespace Compass.ViewModels.Modern.ServiceDataModels;

/// <summary>Which drill level the Service Census analysis report is showing.</summary>
public enum ServiceCensusAnalysisViewMode
{
    Themes = 0,
    Theme = 1,
    Metric = 2,
    OptionProducts = 3,
    ProductDetail = 4
}

public class ServiceCensusAnalysisReportFilters
{
    public Guid? ModelId { get; set; }
    public int? DirectorateId { get; set; }
    public int? BusinessAreaId { get; set; }
    public CMDBProductStatus? ProductStatus { get; set; }
    public int? PhaseId { get; set; }
    public int? TypeId { get; set; }
    public string? OwnerEmail { get; set; }
    public string? Search { get; set; }
    /// <summary>Optional theme stable key filter (limits the theme list / aggregations).</summary>
    public string? ThemeStableKey { get; set; }
    public string? FieldStableKey { get; set; }
    /// <summary>Option / linked-item / text-value key for product drill-down.</summary>
    public string? OptionKey { get; set; }
    public Guid? ProductId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}

public class ServiceCensusAnalysisReportViewModel
{
    public DateTime CalculatedUtc { get; set; }
    public ServiceCensusAnalysisReportFilters Filters { get; set; } = new();
    public ServiceCensusAnalysisViewMode ViewMode { get; set; }

    /// <summary>Services in the authorised filter that have at least one saved answer.</summary>
    public int AnsweredServiceCount { get; set; }

    /// <summary>Services in the authorised filter (assigned census), including those with no answers yet.</summary>
    public int ScopedServiceCount { get; set; }

    public string PopulationLabel { get; set; } = "Answered services in the filter";

    public IReadOnlyList<ServiceCensusAnalysisThemeSummaryViewModel> Themes { get; set; } =
        Array.Empty<ServiceCensusAnalysisThemeSummaryViewModel>();

    public ServiceCensusAnalysisThemeDetailViewModel? SelectedTheme { get; set; }
    public ServiceCensusAnalysisMetricDetailViewModel? SelectedMetric { get; set; }
    public ServiceCensusAnalysisOptionDrillViewModel? OptionDrill { get; set; }
    public ServiceCensusAnalysisProductDetailViewModel? ProductDetail { get; set; }

    public IReadOnlyList<ServiceDataModelCompletionReportFilterOption> DirectorateOptions { get; set; } =
        Array.Empty<ServiceDataModelCompletionReportFilterOption>();
    public IReadOnlyList<ServiceDataModelCompletionReportFilterOption> BusinessAreaOptions { get; set; } =
        Array.Empty<ServiceDataModelCompletionReportFilterOption>();
    public IReadOnlyList<ServiceDataModelCompletionReportFilterOption> PhaseOptions { get; set; } =
        Array.Empty<ServiceDataModelCompletionReportFilterOption>();
    public IReadOnlyList<ServiceDataModelCompletionReportFilterOption> TypeOptions { get; set; } =
        Array.Empty<ServiceDataModelCompletionReportFilterOption>();
    public IReadOnlyList<ServiceDataModelCompletionReportFilterOption> ThemeOptions { get; set; } =
        Array.Empty<ServiceDataModelCompletionReportFilterOption>();
}

public class ServiceCensusAnalysisThemeSummaryViewModel
{
    public string StableKey { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public int MetricCount { get; set; }
    public int AnsweredServiceCount { get; set; }
}

public class ServiceCensusAnalysisThemeDetailViewModel
{
    public string StableKey { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int AnsweredServiceCount { get; set; }
    public IReadOnlyList<ServiceCensusAnalysisMetricSummaryViewModel> Metrics { get; set; } =
        Array.Empty<ServiceCensusAnalysisMetricSummaryViewModel>();
}

public class ServiceCensusAnalysisMetricSummaryViewModel
{
    public string StableKey { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public ServiceDataModelFieldType FieldType { get; set; }
    public bool AllowMultiple { get; set; }
    public string AggregationKind { get; set; } = "choice";
    public int AnsweredServiceCount { get; set; }
    public string SummaryText { get; set; } = string.Empty;
}

public class ServiceCensusAnalysisMetricDetailViewModel
{
    public string ThemeStableKey { get; set; } = string.Empty;
    public string ThemeName { get; set; } = string.Empty;
    public string StableKey { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public ServiceDataModelFieldType FieldType { get; set; }
    public bool AllowMultiple { get; set; }
    public string AggregationKind { get; set; } = "choice";
    public int AnsweredServiceCount { get; set; }

    public IReadOnlyList<ServiceCensusAnalysisOptionCountViewModel> Options { get; set; } =
        Array.Empty<ServiceCensusAnalysisOptionCountViewModel>();

    public int? NumberCount { get; set; }
    public decimal? NumberAverage { get; set; }
    public decimal? NumberMin { get; set; }
    public decimal? NumberMax { get; set; }

    public IReadOnlyList<ServiceCensusAnalysisTextValueViewModel> TextValues { get; set; } =
        Array.Empty<ServiceCensusAnalysisTextValueViewModel>();

    public IReadOnlyList<ServiceCensusAnalysisProductAnswerRowViewModel> NumberProducts { get; set; } =
        Array.Empty<ServiceCensusAnalysisProductAnswerRowViewModel>();

    public int ProductTotalCount { get; set; }
    public int ProductPage { get; set; } = 1;
    public int ProductPageSize { get; set; } = 25;
    public int ProductTotalPages { get; set; } = 1;
}

public class ServiceCensusAnalysisOptionCountViewModel
{
    public string OptionKey { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal? Percent { get; set; }
}

public class ServiceCensusAnalysisTextValueViewModel
{
    public string ValueKey { get; set; } = string.Empty;
    public string DisplayValue { get; set; } = string.Empty;
    public int ServiceCount { get; set; }
}

public class ServiceCensusAnalysisOptionDrillViewModel
{
    public string ThemeStableKey { get; set; } = string.Empty;
    public string ThemeName { get; set; } = string.Empty;
    public string FieldStableKey { get; set; } = string.Empty;
    public string FieldLabel { get; set; } = string.Empty;
    public string OptionKey { get; set; } = string.Empty;
    public string OptionLabel { get; set; } = string.Empty;
    public IReadOnlyList<ServiceCensusAnalysisProductAnswerRowViewModel> Products { get; set; } =
        Array.Empty<ServiceCensusAnalysisProductAnswerRowViewModel>();
    public int ProductTotalCount { get; set; }
    public int ProductPage { get; set; } = 1;
    public int ProductPageSize { get; set; } = 25;
    public int ProductTotalPages { get; set; } = 1;
}

public class ServiceCensusAnalysisProductAnswerRowViewModel
{
    public Guid ProductId { get; set; }
    public Guid AssignmentId { get; set; }
    public string ProductTitle { get; set; } = string.Empty;
    public string? AnswerDisplay { get; set; }
    public string? PhaseName { get; set; }
    public string? BusinessAreasDisplay { get; set; }
}

public class ServiceCensusAnalysisProductDetailViewModel
{
    public Guid ProductId { get; set; }
    public Guid AssignmentId { get; set; }
    public string ProductTitle { get; set; } = string.Empty;
    public string? PhaseName { get; set; }
    public string? BusinessAreasDisplay { get; set; }
    public string? DirectoratesDisplay { get; set; }
    public IReadOnlyList<ServiceCensusAnalysisProductThemeAnswersViewModel> Themes { get; set; } =
        Array.Empty<ServiceCensusAnalysisProductThemeAnswersViewModel>();
}

public class ServiceCensusAnalysisProductThemeAnswersViewModel
{
    public string StableKey { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public IReadOnlyList<ServiceCensusAnalysisProductFieldAnswerViewModel> Answers { get; set; } =
        Array.Empty<ServiceCensusAnalysisProductFieldAnswerViewModel>();
}

public class ServiceCensusAnalysisProductFieldAnswerViewModel
{
    public string StableKey { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public ServiceDataModelFieldType FieldType { get; set; }
    public string? DisplayValue { get; set; }
    public bool HasAnswer { get; set; }
}

/// <summary>Helper model for the shared product table partial in analysis drill-downs.</summary>
public class ServiceCensusAnalysisProductTableModel
{
    public IReadOnlyList<ServiceCensusAnalysisProductAnswerRowViewModel> Products { get; set; } =
        Array.Empty<ServiceCensusAnalysisProductAnswerRowViewModel>();
    public int TotalCount { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    public int TotalPages { get; set; } = 1;
    public bool ShowAnswer { get; set; } = true;
    public Func<int, string>? PageUrl { get; set; }
}
