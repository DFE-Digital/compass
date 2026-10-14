using Compass.Data;
using Compass.Models;
using Compass.Models.Fips;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Compass.Services.Modern;

public interface IGdsReturnExportService
{
    Task<IReadOnlyList<GdsReturnCommissionOption>> ListCommissionsAsync(CancellationToken cancellationToken = default);

    Task<byte[]> BuildExcelAsync(
        IReadOnlyCollection<int> commissionIds,
        CancellationToken cancellationToken = default);
}

public sealed class GdsReturnCommissionOption
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public string? Quarter { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public DateTime DueDate { get; init; }
    public bool IsActive { get; init; }
    public int SubmissionCount { get; init; }
    public string GdsQuarterLabel { get; init; } = "";
}

/// <summary>Service register fields used to enrich GDS Context Data rows.</summary>
internal sealed class GdsReturnFipsProduct
{
    public Guid Id { get; init; }
    public string Title { get; init; } = "";
    public string? CmdbId { get; init; }
    public string? UserDescription { get; init; }
    public string? CmdbDescription { get; init; }
    public string? ServiceOwner { get; init; }
    public List<string> Channels { get; init; } = [];
}

public sealed class GdsReturnExportService : IGdsReturnExportService
{
    private readonly CompassDbContext _db;
    private readonly IProductsApiService _productsApi;
    private readonly ILogger<GdsReturnExportService> _logger;

    public GdsReturnExportService(
        CompassDbContext db,
        IProductsApiService productsApi,
        ILogger<GdsReturnExportService> logger)
    {
        _db = db;
        _productsApi = productsApi;
        _logger = logger;
    }

    public async Task<IReadOnlyList<GdsReturnCommissionOption>> ListCommissionsAsync(
        CancellationToken cancellationToken = default)
    {
        var commissions = await _db.Commissions.AsNoTracking()
            .OrderByDescending(c => c.StartDate)
            .ThenByDescending(c => c.Id)
            .ToListAsync(cancellationToken);

        var counts = await _db.CommissionSubmissions.AsNoTracking()
            .GroupBy(s => s.CommissionId)
            .Select(g => new { CommissionId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CommissionId, x => x.Count, cancellationToken);

        return commissions.Select(c => new GdsReturnCommissionOption
        {
            Id = c.Id,
            Name = c.Name,
            Quarter = c.Quarter,
            StartDate = c.StartDate,
            EndDate = c.EndDate,
            DueDate = c.DueDate,
            IsActive = c.IsActive,
            SubmissionCount = counts.GetValueOrDefault(c.Id),
            GdsQuarterLabel = GdsReturnMapping.FormatGdsQuarter(c)
        }).ToList();
    }

    public async Task<byte[]> BuildExcelAsync(
        IReadOnlyCollection<int> commissionIds,
        CancellationToken cancellationToken = default)
    {
        var ids = commissionIds.Distinct().ToList();
        if (ids.Count == 0)
            throw new ArgumentException("Select at least one commission.", nameof(commissionIds));

        var commissions = await _db.Commissions.AsNoTracking()
            .Where(c => ids.Contains(c.Id))
            .ToListAsync(cancellationToken);

        if (commissions.Count == 0)
            throw new InvalidOperationException("No matching commissions found.");

        var commissionById = commissions.ToDictionary(c => c.Id);

        var submissions = await _db.CommissionSubmissions.AsNoTracking()
            .Include(s => s.MetricValues)
            .Where(s => ids.Contains(s.CommissionId))
            .OrderBy(s => s.ProductTitle)
            .ThenBy(s => s.CommissionId)
            .ToListAsync(cancellationToken);

        var metrics = await _db.PerformanceMetrics.AsNoTracking()
            .Where(m => !m.IsDisabled)
            .OrderBy(m => m.Identifier)
            .ToListAsync(cancellationToken);
        var metricMap = GdsReturnMapping.BuildServiceMetricColumnMap(metrics);

        Dictionary<string, ProductDto> productsByDocId;
        try
        {
            var allProducts = await _productsApi.GetAllProductsAsync(null);
            productsByDocId = allProducts
                .Where(p => !string.IsNullOrEmpty(p.DocumentId))
                .GroupBy(p => p.DocumentId!, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not load products from CMS for GDS return export; context fields will be limited.");
            productsByDocId = new Dictionary<string, ProductDto>(StringComparer.OrdinalIgnoreCase);
        }

        var fipsProducts = await LoadFipsProductsAsync(cancellationToken);

        var contextRows = BuildContextRows(submissions, productsByDocId, fipsProducts);
        var serviceRows = BuildServiceRows(submissions, commissionById, productsByDocId, metricMap, fipsProducts);

        return GdsReturnExcelExport.BuildWorkbook(contextRows, serviceRows);
    }

    private async Task<IReadOnlyList<GdsReturnFipsProduct>> LoadFipsProductsAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            var rows = await _db.CMDBProducts.AsNoTracking()
                .Where(p => p.Status == CMDBProductStatus.Active)
                .Select(p => new
                {
                    p.Id,
                    p.Title,
                    p.CMDBID,
                    p.UserDescription,
                    p.CMDBDescription,
                    ServiceOwner = p.Contacts
                        .Where(c => c.FipsContactRole != null && c.FipsContactRole.Name == "Service Owner")
                        .Select(c => c.UserName ?? c.UserEmail ?? "")
                        .FirstOrDefault(),
                    Channels = p.Channels.Select(c => c.FipsChannel.Name).ToList()
                })
                .ToListAsync(cancellationToken);

            return rows.Select(r => new GdsReturnFipsProduct
            {
                Id = r.Id,
                Title = r.Title ?? "",
                CmdbId = string.IsNullOrWhiteSpace(r.CMDBID) ? null : r.CMDBID.Trim(),
                UserDescription = r.UserDescription,
                CmdbDescription = r.CMDBDescription,
                ServiceOwner = string.IsNullOrWhiteSpace(r.ServiceOwner) ? null : r.ServiceOwner.Trim(),
                Channels = r.Channels
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList()
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not load FIPS service register data for GDS return export.");
            return Array.Empty<GdsReturnFipsProduct>();
        }
    }

    private static GdsReturnFipsProduct? ResolveFipsProduct(
        CommissionSubmission submission,
        ProductDto? product,
        IReadOnlyList<GdsReturnFipsProduct> fipsProducts)
    {
        if (fipsProducts.Count == 0)
            return null;

        var byCmdb = fipsProducts
            .Where(p => p.CmdbId != null)
            .GroupBy(p => p.CmdbId!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var cmdbKeys = new[]
        {
            product?.CmdbSysId?.Trim(),
            submission.FipsId?.Trim()
        };

        foreach (var key in cmdbKeys)
        {
            if (!string.IsNullOrEmpty(key) && byCmdb.TryGetValue(key, out var byId))
                return byId;
        }

        var docCandidates = new[]
        {
            product?.DocumentId?.Trim(),
            submission.ProductDocumentId?.Trim()
        };

        foreach (var doc in docCandidates)
        {
            if (string.IsNullOrEmpty(doc))
                continue;
            if (Guid.TryParse(doc, out var guid))
            {
                var byGuid = fipsProducts.FirstOrDefault(p => p.Id == guid);
                if (byGuid != null)
                    return byGuid;
            }
        }

        var title = !string.IsNullOrWhiteSpace(product?.Title)
            ? product!.Title.Trim()
            : (submission.ProductTitle ?? "").Trim();
        if (string.IsNullOrEmpty(title))
            return null;

        return fipsProducts.FirstOrDefault(p =>
            p.Title.Equals(title, StringComparison.OrdinalIgnoreCase));
    }

    private static List<IReadOnlyList<string>> BuildContextRows(
        IReadOnlyList<CommissionSubmission> submissions,
        IReadOnlyDictionary<string, ProductDto> productsByDocId,
        IReadOnlyList<GdsReturnFipsProduct> fipsProducts)
    {
        var byService = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        foreach (var submission in submissions)
        {
            productsByDocId.TryGetValue(submission.ProductDocumentId, out var product);
            var fips = ResolveFipsProduct(submission, product, fipsProducts);

            var serviceName = !string.IsNullOrWhiteSpace(fips?.Title)
                ? fips!.Title.Trim()
                : !string.IsNullOrWhiteSpace(product?.Title)
                    ? product!.Title.Trim()
                    : (submission.ProductTitle ?? "").Trim();
            if (string.IsNullOrEmpty(serviceName))
                continue;

            var key = serviceName;
            if (byService.ContainsKey(key))
            {
                var existing = byService[key];
                existing[^1] = MergeComments(existing[^1], submission.Comments);
                continue;
            }

            var channelNames = new List<string>(GdsReturnMapping.GetChannelNames(product));
            if (fips != null)
            {
                foreach (var ch in fips.Channels)
                {
                    if (!channelNames.Contains(ch, StringComparer.OrdinalIgnoreCase))
                        channelNames.Add(ch);
                }
            }

            var hasChannels = channelNames.Count > 0;

            byService[key] =
            [
                serviceName,
                GdsReturnMapping.MapTransactionalOrNot(product),
                GdsReturnMapping.MapServiceStatus(product),
                GdsReturnMapping.MapInternalOrExternal(product),
                GdsReturnMapping.MapServicePurpose(fips?.UserDescription, fips?.CmdbDescription, product),
                GdsReturnMapping.MapServiceUsers(product),
                GdsReturnMapping.MapServiceOwner(fips?.ServiceOwner, product),
                "", // service_manager — GDS enum; not on register
                "", // service_usage
                "", // payment_required
                "", // onelogin_available
                hasChannels ? GdsReturnMapping.MapChannelAvailable(channelNames, "online", "digital", "web", "internet") : "",
                hasChannels ? GdsReturnMapping.MapChannelAvailable(channelNames, "telephone", "phone", "call") : "",
                hasChannels ? GdsReturnMapping.MapChannelAvailable(channelNames, "post", "postal", "paper", "mail") : "",
                hasChannels ? GdsReturnMapping.MapChannelAvailable(channelNames, "in person", "in-person", "face to face", "face-to-face", "counter") : "",
                "", // user_satisfaction_method
                "", // digital_adoption_method
                "", // digital_completion_method
                "", // cost_per_transaction_method
                "", // service_kpi
                "", // kpi_value
                submission.Comments?.Trim() ?? ""
            ];
        }

        return byService.Values
            .OrderBy(r => r[0], StringComparer.OrdinalIgnoreCase)
            .Select(r => (IReadOnlyList<string>)r)
            .ToList();
    }

    private static List<IReadOnlyList<string>> BuildServiceRows(
        IReadOnlyList<CommissionSubmission> submissions,
        IReadOnlyDictionary<int, Commission> commissionById,
        IReadOnlyDictionary<string, ProductDto> productsByDocId,
        IReadOnlyDictionary<string, PerformanceMetric?> metricMap,
        IReadOnlyList<GdsReturnFipsProduct> fipsProducts)
    {
        var rows = new List<IReadOnlyList<string>>();

        foreach (var submission in submissions)
        {
            if (!commissionById.TryGetValue(submission.CommissionId, out var commission))
                continue;

            productsByDocId.TryGetValue(submission.ProductDocumentId, out var product);
            var fips = ResolveFipsProduct(submission, product, fipsProducts);

            var serviceName = !string.IsNullOrWhiteSpace(fips?.Title)
                ? fips!.Title.Trim()
                : !string.IsNullOrWhiteSpace(product?.Title)
                    ? product!.Title.Trim()
                    : (submission.ProductTitle ?? "").Trim();
            if (string.IsNullOrEmpty(serviceName))
                continue;

            string Metric(string column)
            {
                metricMap.TryGetValue(column, out var metric);
                var raw = GdsReturnMapping.GetMetricValue(submission, metric);
                if (column == "accessibility_compliance")
                    return GdsReturnMapping.NormalizeAccessibilityCompliance(raw);
                return raw;
            }

            rows.Add(
            [
                GdsReturnMapping.FormatGdsQuarter(commission),
                serviceName,
                Metric("started_transactions"),
                Metric("completed_transactions"),
                Metric("started_digital_transactions"),
                Metric("completed_digital_transactions"),
                Metric("usat_total_number_of_responses"),
                Metric("usat_number_of_satisfied_responses"),
                Metric("usat_number_of_very_satisfied_responses"),
                Metric("accessibility_compliance"),
                Metric("cost_per_transaction_amount"),
                Metric("categories_included_in_cpt"),
                Metric("fte_count"),
                product?.ProductUrl?.Trim() ?? "",
                submission.Comments?.Trim() ?? ""
            ]);
        }

        return rows
            .OrderBy(r => r[1], StringComparer.OrdinalIgnoreCase)
            .ThenBy(r => r[0], StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string MergeComments(string existing, string? incoming)
    {
        var next = incoming?.Trim() ?? "";
        if (string.IsNullOrEmpty(next))
            return existing;
        if (string.IsNullOrEmpty(existing))
            return next;
        if (existing.Contains(next, StringComparison.OrdinalIgnoreCase))
            return existing;
        return $"{existing}; {next}";
    }
}
