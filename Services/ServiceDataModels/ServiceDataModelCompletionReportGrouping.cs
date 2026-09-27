using Compass.Models.ServiceDataModels;
using Compass.ViewModels.Modern.ServiceDataModels;

namespace Compass.Services.ServiceDataModels;

/// <summary>
/// Pure grouping / summary helpers for the Service Census completion report cuts.
/// </summary>
public static class ServiceDataModelCompletionReportGrouping
{
    public const int DefaultPageSize = 25;

    public static ServiceDataModelCompletionReportGroupBy ParseGroupBy(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return ServiceDataModelCompletionReportGroupBy.BusinessArea;

        var trimmed = value.Trim();
        // UI label is "Type"; enum name is ServiceType. Accept both.
        if (string.Equals(trimmed, "Type", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(trimmed, "ServiceType", StringComparison.OrdinalIgnoreCase))
            return ServiceDataModelCompletionReportGroupBy.ServiceType;

        return Enum.TryParse<ServiceDataModelCompletionReportGroupBy>(trimmed, ignoreCase: true, out var parsed)
            ? parsed
            : ServiceDataModelCompletionReportGroupBy.BusinessArea;
    }

    public static string GroupByDisplayName(ServiceDataModelCompletionReportGroupBy groupBy) => groupBy switch
    {
        ServiceDataModelCompletionReportGroupBy.BusinessArea => "Business area",
        ServiceDataModelCompletionReportGroupBy.Directorate => "Directorate",
        ServiceDataModelCompletionReportGroupBy.Product => "Product",
        ServiceDataModelCompletionReportGroupBy.Owner => "Owner",
        ServiceDataModelCompletionReportGroupBy.Phase => "Phase",
        ServiceDataModelCompletionReportGroupBy.ServiceType => "Type",
        _ => "Business area"
    };

    public static ServiceDataModelCompletionSummaryCounts Summarise(
        IReadOnlyList<ServiceDataModelCompletionAssignmentFact> facts,
        DateTime calculatedUtc)
    {
        _ = calculatedUtc;
        var assigned = facts.Count;
        var notApplicable = facts.Count(f => f.Status == ServiceDataModelAssignmentStatus.NotApplicable);
        var withdrawn = facts.Count(f => f.Status == ServiceDataModelAssignmentStatus.Withdrawn);
        var eligible = facts.Count(f =>
            f.Status is not (ServiceDataModelAssignmentStatus.NotApplicable
                or ServiceDataModelAssignmentStatus.Withdrawn));
        var notStarted = facts.Count(f => f.Status == ServiceDataModelAssignmentStatus.NotStarted);
        var inProgress = facts.Count(f => f.Status == ServiceDataModelAssignmentStatus.InProgress);
        var submitted = facts.Count(f => f.Status == ServiceDataModelAssignmentStatus.Submitted);
        var reviewed = facts.Count(f => f.Status == ServiceDataModelAssignmentStatus.Reviewed);
        var changesRequested = facts.Count(f => f.Status == ServiceDataModelAssignmentStatus.ChangesRequested);
        var overdue = facts.Count(f => f.IsOverdue);

        decimal? portfolioPct = eligible <= 0
            ? null
            : Math.Round((decimal)reviewed / eligible * 100m, 1, MidpointRounding.AwayFromZero);

        var fieldPercents = facts
            .Where(f =>
                f.Status is not (ServiceDataModelAssignmentStatus.NotApplicable
                    or ServiceDataModelAssignmentStatus.Withdrawn) &&
                f.FieldCompletionPercent.HasValue)
            .Select(f => f.FieldCompletionPercent!.Value)
            .ToList();

        decimal? averageFieldPct = fieldPercents.Count == 0
            ? null
            : Math.Round(fieldPercents.Average(), 1, MidpointRounding.AwayFromZero);

        return new ServiceDataModelCompletionSummaryCounts
        {
            AssignedCount = assigned,
            EligibleCount = eligible,
            NotStartedCount = notStarted,
            InProgressCount = inProgress,
            SubmittedCount = submitted,
            ReviewedCount = reviewed,
            ChangesRequestedCount = changesRequested,
            OverdueCount = overdue,
            NotApplicableCount = notApplicable,
            WithdrawnCount = withdrawn,
            PortfolioCompletionPercent = portfolioPct,
            AverageFieldCompletionPercent = averageFieldPct
        };
    }

    /// <summary>
    /// Builds aggregate cut rows. Multi-valued dimensions (business area, directorate, type, owner)
    /// expand so an assignment can contribute to more than one row.
    /// </summary>
    public static IReadOnlyList<ServiceDataModelCompletionBreakdownRowViewModel> BuildBreakdown(
        IReadOnlyList<ServiceDataModelCompletionAssignmentFact> facts,
        ServiceDataModelCompletionReportGroupBy groupBy,
        DateTime calculatedUtc)
    {
        if (groupBy == ServiceDataModelCompletionReportGroupBy.Product)
            return Array.Empty<ServiceDataModelCompletionBreakdownRowViewModel>();

        var buckets = new Dictionary<string, List<ServiceDataModelCompletionAssignmentFact>>(StringComparer.OrdinalIgnoreCase);

        foreach (var fact in facts)
        {
            foreach (var key in DimensionKeys(fact, groupBy))
            {
                if (!buckets.TryGetValue(key, out var list))
                {
                    list = new List<ServiceDataModelCompletionAssignmentFact>();
                    buckets[key] = list;
                }

                list.Add(fact);
            }
        }

        return buckets
            .OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
            .Select(kv =>
            {
                var summary = Summarise(kv.Value, calculatedUtc);
                return new ServiceDataModelCompletionBreakdownRowViewModel
                {
                    GroupKey = kv.Key,
                    GroupLabel = kv.Key,
                    AssignedCount = summary.AssignedCount,
                    EligibleCount = summary.EligibleCount,
                    NotStartedCount = summary.NotStartedCount,
                    InProgressCount = summary.InProgressCount,
                    SubmittedCount = summary.SubmittedCount,
                    ReviewedCount = summary.ReviewedCount,
                    ChangesRequestedCount = summary.ChangesRequestedCount,
                    OverdueCount = summary.OverdueCount,
                    NotApplicableCount = summary.NotApplicableCount,
                    WithdrawnCount = summary.WithdrawnCount,
                    PortfolioCompletionPercent = summary.PortfolioCompletionPercent,
                    AverageFieldCompletionPercent = summary.AverageFieldCompletionPercent
                };
            })
            .ToList();
    }

    public static (IReadOnlyList<ServiceDataModelCompletionAssignmentFact> Items, int TotalCount, int PageNumber, int PageSize, int TotalPages)
        Paginate(IReadOnlyList<ServiceDataModelCompletionAssignmentFact> ordered, int page, int pageSize)
    {
        if (pageSize < 1)
            pageSize = DefaultPageSize;
        if (page < 1)
            page = 1;

        var total = ordered.Count;
        var totalPages = total == 0 ? 1 : (int)Math.Ceiling(total / (double)pageSize);
        if (page > totalPages)
            page = totalPages;

        var slice = ordered.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return (slice, total, page, pageSize, totalPages);
    }

    public static IReadOnlyList<string> DimensionKeys(
        ServiceDataModelCompletionAssignmentFact fact,
        ServiceDataModelCompletionReportGroupBy groupBy) => groupBy switch
    {
        ServiceDataModelCompletionReportGroupBy.BusinessArea =>
            NamesOrFallback(fact.BusinessAreaNames, "No business area"),
        ServiceDataModelCompletionReportGroupBy.Directorate =>
            NamesOrFallback(fact.DirectorateNames, "No directorate"),
        ServiceDataModelCompletionReportGroupBy.Owner =>
            NamesOrFallback(fact.OwnerEmails, "No owner"),
        ServiceDataModelCompletionReportGroupBy.Phase =>
            new[] { string.IsNullOrWhiteSpace(fact.PhaseName) ? "Phase not set" : fact.PhaseName.Trim() },
        ServiceDataModelCompletionReportGroupBy.ServiceType =>
            NamesOrFallback(fact.ServiceTypeNames, "No type"),
        ServiceDataModelCompletionReportGroupBy.Product =>
            NamesOrFallback(
                string.IsNullOrWhiteSpace(fact.ProductTitle) ? null : new[] { fact.ProductTitle },
                "Untitled product"),
        _ => NamesOrFallback(fact.BusinessAreaNames, "No business area")
    };

    private static IReadOnlyList<string> NamesOrFallback(IReadOnlyList<string?>? names, string fallback)
    {
        if (names is null || names.Count == 0)
            return new[] { fallback };

        var cleaned = names
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => n!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToList();
        return cleaned.Count == 0 ? new[] { fallback } : cleaned;
    }
}

/// <summary>Flattened assignment row used for filtering, grouping and summary math.</summary>
public sealed class ServiceDataModelCompletionAssignmentFact
{
    public Guid AssignmentId { get; init; }
    public Guid ProductId { get; init; }
    public string ProductTitle { get; init; } = string.Empty;
    public string ModelName { get; init; } = string.Empty;
    public string ModelStableKey { get; init; } = string.Empty;
    public int? VersionNumber { get; init; }
    public string? PeriodLabel { get; init; }
    public ServiceDataModelAssignmentStatus Status { get; init; }
    public decimal? FieldCompletionPercent { get; init; }
    public decimal? MandatoryCompletionPercent { get; init; }
    public DateTime? DueUtc { get; init; }
    public bool IsOverdue { get; init; }
    public DateTime UpdatedUtc { get; init; }
    public DateTime? LastAnsweredUtc { get; init; }
    public DateTime? SubmittedUtc { get; init; }
    public DateTime? ReviewedUtc { get; init; }
    public string? PhaseName { get; init; }
    public IReadOnlyList<string> BusinessAreaNames { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> DirectorateNames { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> ServiceTypeNames { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> OwnerEmails { get; init; } = Array.Empty<string>();
}

public sealed class ServiceDataModelCompletionSummaryCounts
{
    public int AssignedCount { get; init; }
    public int EligibleCount { get; init; }
    public int NotStartedCount { get; init; }
    public int InProgressCount { get; init; }
    public int SubmittedCount { get; init; }
    public int ReviewedCount { get; init; }
    public int ChangesRequestedCount { get; init; }
    public int OverdueCount { get; init; }
    public int NotApplicableCount { get; init; }
    public int WithdrawnCount { get; init; }
    public decimal? PortfolioCompletionPercent { get; init; }
    public decimal? AverageFieldCompletionPercent { get; init; }
}
