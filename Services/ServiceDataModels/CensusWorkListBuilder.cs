using Compass.Models.Fips;
using Compass.Models.ServiceDataModels;

namespace Compass.Services.ServiceDataModels;

/// <summary>
/// Pure helpers for the census work list: Service Register product populations enriched with
/// standing Service Census completion (no user-facing "assignment" concept).
/// </summary>
public static class CensusWorkListBuilder
{
    public const string TabYour = "your";
    public const string TabAll = "all";
    public const string TabBusinessArea = "business-area";

    public const string NoBusinessAreaLabel = "No business area";

    /// <summary>
    /// Summary of the preferred standing census record for a product (Service Census preferred, else latest).
    /// </summary>
    public sealed record CensusProgressSummary(
        Guid StandingRecordId,
        ServiceDataModelAssignmentStatus Status,
        decimal? FieldCompletionPercent,
        int AnsweredCountingFields,
        int ApplicableCountingFields,
        string? ModelStableKey,
        DateTime UpdatedUtc);

    /// <summary>
    /// Published Service Census baseline used when a service has not started yet (0/N, 0%).
    /// </summary>
    public sealed record PublishedCensusBaseline(
        Guid VersionId,
        int ApplicableCountingFields);

    public sealed record BusinessAreaGroup(
        string Name,
        IReadOnlyList<FipsProductRow> Products);

    /// <summary>
    /// Normalises work-list tab keys. Legacy <c>my-work</c> / <c>managed</c> map to <see cref="TabYour"/>.
    /// </summary>
    public static string NormalizeTab(string? tab)
    {
        if (string.Equals(tab, TabAll, StringComparison.OrdinalIgnoreCase))
            return TabAll;
        if (string.Equals(tab, TabBusinessArea, StringComparison.OrdinalIgnoreCase)
            || string.Equals(tab, "businessarea", StringComparison.OrdinalIgnoreCase)
            || string.Equals(tab, "by-business-area", StringComparison.OrdinalIgnoreCase))
            return TabBusinessArea;

        // Default and legacy my-work / managed → Your products
        return TabYour;
    }

    /// <summary>
    /// Maps census work-list tab to the Service Register listing helper tab key.
    /// <see cref="TabAll"/> and <see cref="TabBusinessArea"/> use active catalogue products
    /// (same population / auth as register Active / AllProductsCount).
    /// </summary>
    public static string ToListingHelperTab(string normalizedTab) =>
        normalizedTab == TabYour ? "my" : "active";

    /// <summary>
    /// Enriches named/catalogue products with standing census progress.
    /// Products without a standing record yet show not-started completion from the published baseline.
    /// </summary>
    public static List<FipsProductRow> BuildRows(
        IEnumerable<FipsProductRow> products,
        IReadOnlyDictionary<Guid, CensusProgressSummary> progressByProduct,
        PublishedCensusBaseline? publishedBaseline,
        string? statusFilter)
    {
        var rows = products
            .Select(p => Enrich(
                CloneRow(p),
                progressByProduct.GetValueOrDefault(p.Id),
                publishedBaseline))
            .ToList();

        if (string.IsNullOrWhiteSpace(statusFilter))
            return rows;

        if (!Enum.TryParse<ServiceDataModelAssignmentStatus>(statusFilter, true, out var status))
            return rows;

        return rows.Where(r => r.CensusStatus == status).ToList();
    }

    /// <summary>
    /// Groups products by each linked business area name. Products with no area go under
    /// <see cref="NoBusinessAreaLabel"/>. A product with multiple areas appears in each group.
    /// </summary>
    public static IReadOnlyList<BusinessAreaGroup> GroupByBusinessArea(
        IEnumerable<FipsProductRow> products,
        IReadOnlyDictionary<Guid, IReadOnlyList<string>> businessAreasByProductId)
    {
        var buckets = new Dictionary<string, List<FipsProductRow>>(StringComparer.OrdinalIgnoreCase);
        var noArea = new List<FipsProductRow>();

        foreach (var product in products)
        {
            if (!businessAreasByProductId.TryGetValue(product.Id, out var areas) || areas.Count == 0)
            {
                noArea.Add(product);
                continue;
            }

            foreach (var area in areas.Where(a => !string.IsNullOrWhiteSpace(a)).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (!buckets.TryGetValue(area, out var list))
                {
                    list = [];
                    buckets[area] = list;
                }

                list.Add(product);
            }
        }

        var groups = buckets
            .OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
            .Select(kv => new BusinessAreaGroup(kv.Key, kv.Value))
            .ToList();

        if (noArea.Count > 0)
            groups.Add(new BusinessAreaGroup(NoBusinessAreaLabel, noArea));

        return groups;
    }

    public static CensusProgressSummary? SelectPrimaryProgress(
        IEnumerable<CensusProgressSummary> candidates)
    {
        var list = candidates.ToList();
        if (list.Count == 0)
            return null;

        var serviceCensus = list
            .Where(a => string.Equals(
                a.ModelStableKey,
                ServiceCensusDefaults.StableKey,
                StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(a => a.UpdatedUtc)
            .FirstOrDefault();

        return serviceCensus ?? list.OrderByDescending(a => a.UpdatedUtc).First();
    }

    public static string FormatStatusLabel(ServiceDataModelAssignmentStatus? status) =>
        status switch
        {
            null => "Not started",
            ServiceDataModelAssignmentStatus.NotStarted => "Not started",
            ServiceDataModelAssignmentStatus.InProgress => "In progress",
            ServiceDataModelAssignmentStatus.Submitted => "Submitted",
            ServiceDataModelAssignmentStatus.ChangesRequested => "Changes requested",
            ServiceDataModelAssignmentStatus.Reviewed => "Reviewed",
            ServiceDataModelAssignmentStatus.Withdrawn => "Withdrawn",
            ServiceDataModelAssignmentStatus.NotApplicable => "Not applicable",
            ServiceDataModelAssignmentStatus.ReviewDue => "Review due",
            _ => status.Value.ToString()
        };

    public static FipsProductRow Enrich(
        FipsProductRow row,
        CensusProgressSummary? progress,
        PublishedCensusBaseline? publishedBaseline)
    {
        row.CensusPublishedAvailable = publishedBaseline != null;

        if (progress != null)
        {
            row.HasCensusAssignment = true;
            row.CensusAssignmentId = progress.StandingRecordId;
            row.CensusStatus = progress.Status;
            row.CensusStatusLabel = FormatStatusLabel(progress.Status);
            row.CensusFieldCompletionPercent = progress.FieldCompletionPercent
                ?? (progress.ApplicableCountingFields > 0 ? 0m : null);
            row.CensusAnsweredCountingFields = progress.AnsweredCountingFields;
            row.CensusApplicableCountingFields = progress.ApplicableCountingFields;
            return row;
        }

        // No standing record yet — still completable when a census is published (not started).
        row.HasCensusAssignment = false;
        row.CensusAssignmentId = null;
        row.CensusStatus = publishedBaseline == null
            ? null
            : ServiceDataModelAssignmentStatus.NotStarted;
        row.CensusStatusLabel = FormatStatusLabel(row.CensusStatus);

        if (publishedBaseline == null)
        {
            row.CensusFieldCompletionPercent = null;
            row.CensusAnsweredCountingFields = null;
            row.CensusApplicableCountingFields = null;
            return row;
        }

        var total = publishedBaseline.ApplicableCountingFields;
        row.CensusAnsweredCountingFields = 0;
        row.CensusApplicableCountingFields = total;
        row.CensusFieldCompletionPercent = total > 0 ? 0m : null;
        return row;
    }

    private static FipsProductRow CloneRow(FipsProductRow source) => new()
    {
        Id = source.Id,
        UniqueID = source.UniqueID,
        Title = source.Title,
        CMDBDescription = source.CMDBDescription,
        UserDescription = source.UserDescription,
        PhaseName = source.PhaseName,
        BusinessAreaDisplay = source.BusinessAreaDisplay,
        TypesDisplay = source.TypesDisplay,
        ChannelsDisplay = source.ChannelsDisplay,
        UserGroupCount = source.UserGroupCount,
        ContactCount = source.ContactCount,
        ServiceOwner = source.ServiceOwner,
        ReportingContact = source.ReportingContact,
        Status = source.Status,
        QualityScore = source.QualityScore,
        QualityScoreMax = source.QualityScoreMax
    };
}
