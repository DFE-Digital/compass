using System.Globalization;
using System.Text.Json;
using Compass.Data;
using Compass.Models;
using Microsoft.EntityFrameworkCore;

namespace Compass.Services;

/// <summary>Shared metric applicability and validation for commission (product) performance reporting.</summary>
public static class CommissionReportingMetricsHelper
{
    public static (bool IsValid, string? ErrorMessage) ValidateMetricValue(string? value, PerformanceMetric metric)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            try
            {
                var rules = JsonSerializer.Deserialize<ValidationRules>(metric.ValidationRules);
                if (rules?.Required == true && rules?.AllowNull != true)
                    return (false, "This field is required");
                return (true, null);
            }
            catch
            {
                return (true, null);
            }
        }

        try
        {
            var rules = JsonSerializer.Deserialize<ValidationRules>(metric.ValidationRules);

            switch (metric.ValueType)
            {
                case Models.ValueType.Number:
                    if (!int.TryParse(value, out var intValue))
                        return (false, "Value must be a whole number");

                    if (rules?.MinimumValue.HasValue == true && intValue < rules.MinimumValue.Value)
                        return (false, $"Value must be at least {rules.MinimumValue.Value}");
                    if (rules?.MaximumValue.HasValue == true && intValue > rules.MaximumValue.Value)
                        return (false, $"Value must be at most {rules.MaximumValue.Value}");

                    if (rules?.Range != null)
                    {
                        if (rules.Range.Min.HasValue && intValue < rules.Range.Min.Value)
                            return (false, $"Value must be at least {rules.Range.Min.Value}");
                        if (rules.Range.Max.HasValue && intValue > rules.Range.Max.Value)
                            return (false, $"Value must be at most {rules.Range.Max.Value}");
                    }

                    break;

                case Models.ValueType.Decimal:
                    if (!decimal.TryParse(value, out var decimalValue))
                        return (false, "Value must be a decimal number");

                    if (rules?.MinimumValue.HasValue == true && decimalValue < rules.MinimumValue.Value)
                        return (false, $"Value must be at least {rules.MinimumValue.Value}");
                    if (rules?.MaximumValue.HasValue == true && decimalValue > rules.MaximumValue.Value)
                        return (false, $"Value must be at most {rules.MaximumValue.Value}");

                    if (rules?.Range != null)
                    {
                        if (rules.Range.Min.HasValue && decimalValue < rules.Range.Min.Value)
                            return (false, $"Value must be at least {rules.Range.Min.Value}");
                        if (rules.Range.Max.HasValue && decimalValue > rules.Range.Max.Value)
                            return (false, $"Value must be at most {rules.Range.Max.Value}");
                    }

                    if (rules?.DecimalPlaces.HasValue == true)
                    {
                        var decimalPart = value.Split('.').Skip(1).FirstOrDefault();
                        if (!string.IsNullOrEmpty(decimalPart) && decimalPart.Length > rules.DecimalPlaces.Value)
                            return (false, $"Value must have at most {rules.DecimalPlaces.Value} decimal places");
                    }

                    break;

                case Models.ValueType.Text:
                    break;
            }

            return (true, null);
        }
        catch
        {
            return (true, null);
        }
    }

    /// <summary>
    /// When non-null, only these metric ids are candidates for the commission (intersected with per-product applicability).
    /// </summary>
    public static HashSet<int>? ParseIncludedMetricIds(string? includedPerformanceMetricIds)
    {
        if (string.IsNullOrWhiteSpace(includedPerformanceMetricIds))
            return null;
        var set = new HashSet<int>();
        foreach (var part in includedPerformanceMetricIds.Split(',',
                     StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (int.TryParse(part, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
                set.Add(id);
        }

        return set.Count > 0 ? set : null;
    }

    public static async Task<List<PerformanceMetric>> LoadEnabledMetricsForCommissionPeriodAsync(
        CompassDbContext context,
        Commission commission,
        CancellationToken cancellationToken = default)
    {
        var year = commission.EndDate.Year;
        var month = commission.EndDate.Month;
        return await context.PerformanceMetrics.AsNoTracking()
            .Where(m => !m.IsDisabled &&
                        (m.ValidFromYear < year ||
                         (m.ValidFromYear == year && m.ValidFromMonth <= month)))
            .OrderBy(m => m.Identifier)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Metrics the product must complete: commission metric allow-list (if any), valid-for-period metrics,
    /// metric phase/type rules, then conditional dependencies.
    /// </summary>
    public static List<PerformanceMetric> FilterApplicableMetricsForProduct(
        Commission commission,
        ProductDto product,
        IReadOnlyList<PerformanceMetric> metricsValidForPeriod,
        IReadOnlyList<CommissionMetricValue> existingMetricValues)
    {
        var includedIds = ParseIncludedMetricIds(commission.IncludedPerformanceMetricIds);
        var baseList = includedIds == null
            ? metricsValidForPeriod.ToList()
            : metricsValidForPeriod.Where(m => includedIds.Contains(m.Id)).ToList();

        var phaseFilteredMetrics = baseList.Where(m =>
        {
            if (string.IsNullOrEmpty(m.ApplicablePhases))
                return true;
            if (string.IsNullOrEmpty(product.Phase))
                return true;
            var applicablePhases = m.ApplicablePhases.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .ToList();
            return applicablePhases.Contains(product.Phase, StringComparer.OrdinalIgnoreCase);
        }).ToList();

        var productTypes = new List<string>();
        if (product.CategoryValues != null)
        {
            productTypes = product.CategoryValues
                .Where(cv => cv.CategoryType?.Name?.Equals("Type", StringComparison.OrdinalIgnoreCase) == true)
                .Select(cv => cv.Name)
                .ToList();
        }

        var typeFilteredMetrics = phaseFilteredMetrics.Where(m =>
        {
            if (string.IsNullOrEmpty(m.ApplicableTypes))
                return true;
            if (!productTypes.Any())
                return true;
            var applicableTypes = m.ApplicableTypes.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .ToList();
            return productTypes.Any(pt => applicableTypes.Contains(pt, StringComparer.OrdinalIgnoreCase));
        }).ToList();

        return typeFilteredMetrics.Where(m =>
        {
            if (!m.ConditionalOnMetricId.HasValue)
                return true;
            var parentMetricValue = existingMetricValues
                .FirstOrDefault(mv => mv.PerformanceMetricId == m.ConditionalOnMetricId.Value);
            return parentMetricValue != null && !string.IsNullOrWhiteSpace(parentMetricValue.Value);
        }).ToList();
    }

    /// <summary>
    /// Gets applicable performance metrics for a product for this commission (commission metric set × product phase/type × conditions).
    /// </summary>
    public static async Task<List<PerformanceMetric>> GetApplicableMetricsForProductAsync(
        CompassDbContext context,
        ProductDto product,
        int commissionId,
        CancellationToken cancellationToken = default)
    {
        var commission = await context.Commissions.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == commissionId, cancellationToken);
        if (commission == null)
            return new List<PerformanceMetric>();

        var allMetrics = await LoadEnabledMetricsForCommissionPeriodAsync(context, commission, cancellationToken);

        var docKey = product.DocumentId ?? product.FipsId ?? "";
        var submission = await context.CommissionSubmissions
            .Include(cs => cs.MetricValues)
            .FirstOrDefaultAsync(cs => cs.CommissionId == commissionId &&
                                       (cs.ProductDocumentId == docKey || cs.FipsId == product.FipsId),
                cancellationToken);

        var existingMetricValues = submission?.MetricValues?.ToList() ?? new List<CommissionMetricValue>();

        return FilterApplicableMetricsForProduct(commission, product, allMetrics, existingMetricValues);
    }
}
