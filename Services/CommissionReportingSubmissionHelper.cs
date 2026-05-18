using Compass.Data;
using Compass.Models;
using Microsoft.EntityFrameworkCore;

namespace Compass.Services;

/// <summary>Creates or updates commission submissions and metric value rows (same rules as ProductReportingController.SubmitCommission).</summary>
public static class CommissionReportingSubmissionHelper
{
    /// <summary>Loads or creates the submission and ensures <see cref="CommissionMetricValue"/> rows exist for each metric in scope (phase, type, conditional).</summary>
    public static async Task<(CommissionSubmission Submission, List<CommissionMetricValue> MetricValues)> EnsureSubmissionAndMetricRowsAsync(
        CompassDbContext context,
        ProductDto product,
        int commissionId,
        CancellationToken cancellationToken = default)
    {
        var productDocumentId = product.DocumentId ?? product.FipsId ?? "";

        var submission = await context.CommissionSubmissions
            .Include(cs => cs.MetricValues)
            .ThenInclude(cmv => cmv.PerformanceMetric)
            .FirstOrDefaultAsync(cs => cs.CommissionId == commissionId && cs.ProductDocumentId == productDocumentId,
                cancellationToken);

        if (submission == null)
        {
            submission = new CommissionSubmission
            {
                CommissionId = commissionId,
                ProductDocumentId = productDocumentId,
                FipsId = product.FipsId,
                ProductTitle = product.Title,
                Status = CommissionSubmissionStatus.NotStarted,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.CommissionSubmissions.Add(submission);
            await context.SaveChangesAsync(cancellationToken);
        }

        var commission = await context.Commissions.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == commissionId, cancellationToken);
        if (commission == null)
            return (submission, new List<CommissionMetricValue>());

        var allMetrics =
            await CommissionReportingMetricsHelper.LoadEnabledMetricsForCommissionPeriodAsync(context, commission,
                cancellationToken);

        var existingMetricValues = await context.CommissionMetricValues
            .Include(cmv => cmv.PerformanceMetric)
            .Where(cmv => cmv.CommissionSubmissionId == submission.Id)
            .ToListAsync(cancellationToken);

        var metrics = CommissionReportingMetricsHelper.FilterApplicableMetricsForProduct(commission, product, allMetrics,
            existingMetricValues);

        foreach (var metric in metrics)
        {
            if (!existingMetricValues.Any(mv => mv.PerformanceMetricId == metric.Id))
            {
                var newValue = new CommissionMetricValue
                {
                    CommissionSubmissionId = submission.Id,
                    PerformanceMetricId = metric.Id,
                    IsComplete = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                context.CommissionMetricValues.Add(newValue);
                existingMetricValues.Add(newValue);
            }
        }

        await context.SaveChangesAsync(cancellationToken);

        var allMetricValues = await context.CommissionMetricValues
            .Include(cmv => cmv.PerformanceMetric)
            .Where(cmv => cmv.CommissionSubmissionId == submission.Id)
            .ToListAsync(cancellationToken);

        var applicableMetricIds = metrics.Select(m => m.Id).ToHashSet();
        var metricValues = allMetricValues
            .Where(mv => mv.PerformanceMetric != null && applicableMetricIds.Contains(mv.PerformanceMetric.Id))
            .OrderBy(mv => mv.PerformanceMetric!.Identifier)
            .ToList();

        return (submission, metricValues);
    }

    public static void RecalculateSubmissionStatus(CommissionSubmission submission, IEnumerable<CommissionMetricValue> metricValues)
    {
        var list = metricValues.ToList();
        var completedCount = list.Count(mv => mv.IsComplete);
        var totalCount = list.Count;

        if (completedCount == totalCount && totalCount > 0)
            submission.Status = CommissionSubmissionStatus.InProgress;
        else if (completedCount > 0)
            submission.Status = CommissionSubmissionStatus.InProgress;
        else
            submission.Status = CommissionSubmissionStatus.NotStarted;

        submission.UpdatedAt = DateTime.UtcNow;
    }
}
