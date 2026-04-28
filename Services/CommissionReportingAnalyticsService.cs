using System;
using System.Collections.Generic;
using System.Linq;
using Compass.Data;
using Compass.Models;
using Compass.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Compass.Services;

/// <summary>Org-wide commission completion analytics for the reporting hub (catalogue scope).</summary>
public sealed class CommissionReportingAnalyticsService
{
    private readonly CompassDbContext _context;
    private readonly IProductsApiService _productsApi;
    private readonly ILogger<CommissionReportingAnalyticsService> _logger;

    public CommissionReportingAnalyticsService(
        CompassDbContext context,
        IProductsApiService productsApi,
        ILogger<CommissionReportingAnalyticsService> logger)
    {
        _context = context;
        _productsApi = productsApi;
        _logger = logger;
    }

    private static bool IsOpenForSubmissionWindow(Commission c, DateTime now) =>
        c.IsActive && now >= c.OpenDate && now <= c.DueDate.AddDays(1);

    /// <summary>
    /// Closed for the performance "completed" style list: not in the active submission window,
    /// excluding active commissions that are not yet open.
    /// </summary>
    public static bool IsEligibleForCompletedPerformanceReporting(Commission c, DateTime now)
    {
        if (IsOpenForSubmissionWindow(c, now))
            return false;
        if (c.IsActive && now < c.OpenDate)
            return false;
        return true;
    }

    public async Task<ModernReportingPerformanceIndexViewModel> BuildIndexAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var commissions = await _context.Commissions.AsNoTracking()
            .OrderByDescending(c => c.DueDate)
            .ToListAsync(cancellationToken);

        var eligible = commissions.Where(c => IsEligibleForCompletedPerformanceReporting(c, now)).ToList();
        if (eligible.Count == 0)
            return new ModernReportingPerformanceIndexViewModel();

        List<ProductDto> catalogue;
        try
        {
            catalogue = await _productsApi.GetAllProductsAsync(null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Commission reporting analytics: failed to load product catalogue");
            return new ModernReportingPerformanceIndexViewModel
            {
                LoadError = "Could not load the product catalogue. Try again shortly."
            };
        }

        catalogue = CommissionReportingProductScope.GetAllActivePublishedEligible(catalogue);

        var commissionIds = eligible.Select(c => c.Id).ToList();
        var submissions = await _context.CommissionSubmissions.AsNoTracking()
            .Include(cs => cs.MetricValues)
            .Where(cs => commissionIds.Contains(cs.CommissionId))
            .ToListAsync(cancellationToken);

        var subsByCommission = submissions.GroupBy(s => s.CommissionId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var rows = new List<ModernReportingPerformanceCommissionSummary>();
        foreach (var commission in eligible)
        {
            subsByCommission.TryGetValue(commission.Id, out var subs);
            var summary = await BuildSummaryAsync(commission, catalogue, subs ?? new List<CommissionSubmission>(), cancellationToken);
            rows.Add(summary);
        }

        return new ModernReportingPerformanceIndexViewModel { Commissions = rows };
    }

    public async Task<ModernReportingPerformanceCommissionDetailViewModel?> BuildDetailAsync(
        int commissionId,
        string? filterBusinessAreaName,
        string? filterDirectorateName,
        CancellationToken cancellationToken = default)
    {
        var commission = await _context.Commissions.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == commissionId, cancellationToken);
        if (commission == null)
            return null;

        var now = DateTime.UtcNow;
        if (!IsEligibleForCompletedPerformanceReporting(commission, now))
            return null;

        List<ProductDto> catalogue;
        try
        {
            catalogue = await _productsApi.GetAllProductsAsync(null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Commission reporting analytics: failed to load product catalogue for commission {Id}", commissionId);
            return new ModernReportingPerformanceCommissionDetailViewModel
            {
                Commission = commission,
                LoadError = "Could not load the product catalogue. Try again shortly."
            };
        }

        catalogue = CommissionReportingProductScope.GetAllActivePublishedEligible(catalogue);
        catalogue = ApplyPerformanceCatalogueFilters(catalogue, filterBusinessAreaName, filterDirectorateName);

        var submissions = await _context.CommissionSubmissions.AsNoTracking()
            .Include(cs => cs.MetricValues)
            .Where(cs => cs.CommissionId == commissionId)
            .ToListAsync(cancellationToken);

        var summary = await BuildSummaryAsync(commission, catalogue, submissions, cancellationToken);
        var perProduct = await BuildPerProductStatsAsync(commission, catalogue, submissions, cancellationToken);

        var baGroups = perProduct
            .GroupBy(p => p.BusinessArea ?? "Not assigned", StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var businessAreas = new List<ModernReportingPerformanceBusinessAreaRow>();
        foreach (var g in baGroups)
        {
            var total = g.Count();
            var ns = g.Count(x => x.Status == CommissionSubmissionStatus.NotStarted);
            var ip = g.Count(x => x.Status == CommissionSubmissionStatus.InProgress);
            var sub = g.Count(x => x.Status == CommissionSubmissionStatus.Submitted);
            var late = g.Count(x => x.Status == CommissionSubmissionStatus.Late);
            var returned = sub + late;
            var rate = total == 0 ? 0 : Math.Round(100m * returned / total, 1);

            var metNum = 0;
            var metDen = 0;
            foreach (var x in g)
            {
                if (x.TotalMetrics > 0)
                {
                    metDen += x.TotalMetrics;
                    metNum += x.CompletedMetrics;
                }
            }

            var metPct = metDen == 0 ? 0 : Math.Round(100m * metNum / metDen, 1);
            businessAreas.Add(new ModernReportingPerformanceBusinessAreaRow
            {
                BusinessArea = g.Key,
                Total = total,
                NotStarted = ns,
                InProgress = ip,
                Submitted = sub,
                Late = late,
                ReturnRatePercent = rate,
                MetricCompletionPercent = metPct
            });
        }

        return new ModernReportingPerformanceCommissionDetailViewModel
        {
            Commission = commission,
            ProductsInScope = summary.ProductsInScope,
            NotStarted = summary.NotStarted,
            InProgress = summary.InProgress,
            Submitted = summary.Submitted,
            Late = summary.Late,
            ReturnRatePercent = summary.ReturnRatePercent,
            MetricCompletionPercent = summary.MetricCompletionPercent,
            CompletedMetricCells = summary.CompletedMetricCells,
            ApplicableMetricCells = summary.ApplicableMetricCells,
            BusinessAreas = businessAreas
        };
    }

    /// <summary>
    /// Single reporting page: commission picker, optional business area / directorate filters (same lookups as monthly report), summary + breakdown.
    /// </summary>
    public async Task<ModernReportingPerformancePageViewModel> BuildPerformancePageAsync(
        int? commissionId,
        int? businessAreaId,
        int? directorateId,
        CancellationToken cancellationToken = default)
    {
        var index = await BuildIndexAsync(cancellationToken);
        var businessAreaOptions = await _context.BusinessAreaLookups.AsNoTracking()
            .Where(b => b.IsActive)
            .OrderBy(b => b.SortOrder)
            .ThenBy(b => b.Name)
            .ToListAsync(cancellationToken);
        var directorateOptions = await _context.Divisions.AsNoTracking()
            .Where(d => d.IsActive)
            .OrderBy(d => d.SortOrder)
            .ThenBy(d => d.Name)
            .ToListAsync(cancellationToken);

        var page = new ModernReportingPerformancePageViewModel
        {
            LoadError = index.LoadError,
            Commissions = index.Commissions,
            BusinessAreaOptions = businessAreaOptions,
            DirectorateOptions = directorateOptions
        };

        if (!string.IsNullOrEmpty(index.LoadError))
            return page;

        if (index.Commissions.Count == 0)
            return page;

        var selectedId = commissionId;
        if (selectedId == null || index.Commissions.All(c => c.CommissionId != selectedId))
            selectedId = index.Commissions[0].CommissionId;

        page.SelectedCommissionId = selectedId;

        string? baName = null;
        if (businessAreaId is > 0)
        {
            baName = businessAreaOptions.FirstOrDefault(x => x.Id == businessAreaId)?.Name;
            page.FilterBusinessAreaId = baName != null ? businessAreaId : null;
        }

        string? dirName = null;
        if (directorateId is > 0)
        {
            dirName = directorateOptions.FirstOrDefault(x => x.Id == directorateId)?.Name;
            page.FilterDirectorateId = dirName != null ? directorateId : null;
        }

        var detail = await BuildDetailAsync(selectedId.Value, baName, dirName, cancellationToken);
        page.Detail = detail;

        return page;
    }

    private static List<ProductDto> ApplyPerformanceCatalogueFilters(
        List<ProductDto> catalogue,
        string? businessAreaName,
        string? directorateName)
    {
        IEnumerable<ProductDto> q = catalogue;
        if (!string.IsNullOrWhiteSpace(businessAreaName))
        {
            var ba = businessAreaName.Trim();
            q = q.Where(p => string.Equals(
                CommissionReportingProductScope.GetBusinessArea(p) ?? "",
                ba,
                StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(directorateName))
        {
            var dn = directorateName.Trim();
            q = q.Where(p => string.Equals(
                CommissionReportingProductScope.GetDirectorate(p) ?? "",
                dn,
                StringComparison.OrdinalIgnoreCase));
        }

        return q.ToList();
    }

    private sealed class ProductCommissionStats
    {
        public string? BusinessArea { get; init; }
        public CommissionSubmissionStatus Status { get; init; }
        public int CompletedMetrics { get; init; }
        public int TotalMetrics { get; init; }
    }

    private async Task<List<ProductCommissionStats>> BuildPerProductStatsAsync(
        Commission commission,
        List<ProductDto> catalogue,
        List<CommissionSubmission> submissions,
        CancellationToken cancellationToken)
    {
        var scopeProducts = catalogue
            .Where(p => CommissionReportingProductScope.ProductMatchesCommissionInScopeRules(commission, p))
            .ToList();

        var submissionsDict = submissions.ToDictionary(cs => cs.ProductDocumentId, cs => cs, StringComparer.OrdinalIgnoreCase);

        var metricsForPeriod =
            await CommissionReportingMetricsHelper.LoadEnabledMetricsForCommissionPeriodAsync(_context, commission,
                cancellationToken);

        var rows = new List<ProductCommissionStats>();
        foreach (var p in scopeProducts)
        {
            var docId = p.DocumentId ?? "";
            submissionsDict.TryGetValue(docId, out var sub);
            var existing = sub?.MetricValues?.ToList() ?? new List<CommissionMetricValue>();
            var applicable = CommissionReportingMetricsHelper.FilterApplicableMetricsForProduct(commission, p,
                metricsForPeriod, existing);
            var total = applicable.Count;
            var completed = applicable.Count(m =>
                existing.Any(mv => mv.PerformanceMetricId == m.Id && mv.IsComplete));

            rows.Add(new ProductCommissionStats
            {
                BusinessArea = CommissionReportingProductScope.GetBusinessArea(p),
                Status = sub?.Status ?? CommissionSubmissionStatus.NotStarted,
                CompletedMetrics = completed,
                TotalMetrics = total
            });
        }

        return rows;
    }

    private async Task<ModernReportingPerformanceCommissionSummary> BuildSummaryAsync(
        Commission commission,
        List<ProductDto> catalogue,
        List<CommissionSubmission> submissions,
        CancellationToken cancellationToken)
    {
        var perProduct = await BuildPerProductStatsAsync(commission, catalogue, submissions, cancellationToken);
        var total = perProduct.Count;
        var ns = perProduct.Count(x => x.Status == CommissionSubmissionStatus.NotStarted);
        var ip = perProduct.Count(x => x.Status == CommissionSubmissionStatus.InProgress);
        var sub = perProduct.Count(x => x.Status == CommissionSubmissionStatus.Submitted);
        var late = perProduct.Count(x => x.Status == CommissionSubmissionStatus.Late);
        var returned = sub + late;
        var rate = total == 0 ? 0 : Math.Round(100m * returned / total, 1);

        var metNum = 0;
        var metDen = 0;
        foreach (var x in perProduct)
        {
            if (x.TotalMetrics > 0)
            {
                metDen += x.TotalMetrics;
                metNum += x.CompletedMetrics;
            }
        }

        var metPct = metDen == 0 ? 0 : Math.Round(100m * metNum / metDen, 1);

        return new ModernReportingPerformanceCommissionSummary
        {
            CommissionId = commission.Id,
            Name = commission.Name,
            StartDate = commission.StartDate,
            EndDate = commission.EndDate,
            DueDate = commission.DueDate,
            IsActive = commission.IsActive,
            ProductsInScope = total,
            NotStarted = ns,
            InProgress = ip,
            Submitted = sub,
            Late = late,
            ReturnRatePercent = rate,
            MetricCompletionPercent = metPct,
            CompletedMetricCells = metNum,
            ApplicableMetricCells = metDen
        };
    }
}
