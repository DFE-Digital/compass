using FipsReporting.Data;
using Microsoft.EntityFrameworkCore;

namespace FipsReporting.Services
{
    public interface IMetricsService
    {
        Task<List<ReportingMetric>> GetActiveMetricsAsync();
        Task<ReportingMetric?> GetMetricByIdAsync(int id);
        Task<ReportingMetric> CreateMetricAsync(ReportingMetric metric, string createdBy);
        Task<ReportingMetric> UpdateMetricAsync(ReportingMetric metric, string updatedBy);
        Task DeleteMetricAsync(int id);
        Task<List<ReportingMetric>> GetApplicableMetricsAsync(string productId);
        Task<bool> IsMetricApplicableToProductAsync(int metricId, string productId);
        Task SubmitReportingDataAsync(ReportingData reportingData);
        Task<List<ReportingData>> GetUserReportsAsync(string userId);
    }

    public class MetricsService : IMetricsService
    {
        private readonly ReportingDbContext _context;
        private readonly CmsApiService _cmsApiService;
        private readonly ILogger<MetricsService> _logger;

        public MetricsService(ReportingDbContext context, CmsApiService cmsApiService, ILogger<MetricsService> logger)
        {
            _context = context;
            _cmsApiService = cmsApiService;
            _logger = logger;
        }

        public async Task<List<ReportingMetric>> GetActiveMetricsAsync()
        {
            try
            {
                return await _context.ReportingMetrics
                    .Where(m => m.IsActive)
                    .Include(m => m.Conditions)
                    .OrderBy(m => m.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active metrics");
                throw;
            }
        }

        public async Task<ReportingMetric?> GetMetricByIdAsync(int id)
        {
            try
            {
                return await _context.ReportingMetrics
                    .Include(m => m.Conditions)
                    .FirstOrDefaultAsync(m => m.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting metric {MetricId}", id);
                throw;
            }
        }

        public async Task<ReportingMetric> CreateMetricAsync(ReportingMetric metric, string createdBy)
        {
            try
            {
                metric.CreatedBy = createdBy;
                metric.UpdatedBy = createdBy;
                metric.CreatedAt = DateTime.UtcNow;
                metric.UpdatedAt = DateTime.UtcNow;

                _context.ReportingMetrics.Add(metric);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Metric {MetricName} created by {CreatedBy}", metric.Name, createdBy);
                return metric;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating metric {MetricName}", metric.Name);
                throw;
            }
        }

        public async Task<ReportingMetric> UpdateMetricAsync(ReportingMetric metric, string updatedBy)
        {
            try
            {
                metric.UpdatedBy = updatedBy;
                metric.UpdatedAt = DateTime.UtcNow;

                _context.ReportingMetrics.Update(metric);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Metric {MetricId} updated by {UpdatedBy}", metric.Id, updatedBy);
                return metric;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating metric {MetricId}", metric.Id);
                throw;
            }
        }

        public async Task DeleteMetricAsync(int id)
        {
            try
            {
                var metric = await _context.ReportingMetrics.FindAsync(id);
                if (metric != null)
                {
                    _context.ReportingMetrics.Remove(metric);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("Metric {MetricId} deleted", id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting metric {MetricId}", id);
                throw;
            }
        }

        public async Task<List<ReportingMetric>> GetApplicableMetricsAsync(string productId)
        {
            try
            {
                var product = await _cmsApiService.GetProductByIdAsync(int.Parse(productId));
                if (product == null)
                {
                    return new List<ReportingMetric>();
                }

                var applicableMetrics = new List<ReportingMetric>();
                var allMetrics = await GetActiveMetricsAsync();

                foreach (var metric in allMetrics)
                {
                    if (await IsMetricApplicableToProductAsync(metric.Id, productId))
                    {
                        applicableMetrics.Add(metric);
                    }
                }

                return applicableMetrics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting applicable metrics for product {ProductId}", productId);
                throw;
            }
        }

        public async Task<bool> IsMetricApplicableToProductAsync(int metricId, string productId)
        {
            try
            {
                var metric = await GetMetricByIdAsync(metricId);
                if (metric == null || !metric.Conditions.Any())
                {
                    return true; // If no conditions, metric applies to all products
                }

                var product = await _cmsApiService.GetProductByIdAsync(int.Parse(productId));
                if (product == null)
                {
                    return false;
                }

                var productCategoryValues = product.CategoryValues?.Select(cv => cv.Name).ToList() ?? new List<string>();
                var productCategoryTypes = product.CategoryValues?.Select(cv => cv.CategoryType?.Name).Where(ct => !string.IsNullOrEmpty(ct)).ToList() ?? new List<string>();

                foreach (var condition in metric.Conditions)
                {
                    bool conditionMet = false;

                    switch (condition.Operator.ToLower())
                    {
                        case "contains":
                            if (condition.CategoryType == "Category Type")
                            {
                                conditionMet = productCategoryTypes.Contains(condition.CategoryValue);
                            }
                            else
                            {
                                conditionMet = productCategoryValues.Contains(condition.CategoryValue);
                            }
                            break;

                        case "equals":
                            if (condition.CategoryType == "Category Type")
                            {
                                conditionMet = productCategoryTypes.Contains(condition.CategoryValue);
                            }
                            else
                            {
                                conditionMet = productCategoryValues.Contains(condition.CategoryValue);
                            }
                            break;

                        case "not_contains":
                            if (condition.CategoryType == "Category Type")
                            {
                                conditionMet = !productCategoryTypes.Contains(condition.CategoryValue);
                            }
                            else
                            {
                                conditionMet = !productCategoryValues.Contains(condition.CategoryValue);
                            }
                            break;
                    }

                    if (!conditionMet)
                    {
                        return false; // If any condition is not met, metric doesn't apply
                    }
                }

                return true; // All conditions met
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking metric applicability for metric {MetricId} and product {ProductId}", metricId, productId);
                throw;
            }
        }

        public async Task SubmitReportingDataAsync(ReportingData reportingData)
        {
            try
            {
                reportingData.CreatedAt = DateTime.UtcNow;
                reportingData.UpdatedAt = DateTime.UtcNow;

                _context.ReportingData.Add(reportingData);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Reporting data submitted for metric {MetricId} by {SubmittedBy}", 
                    reportingData.MetricId, reportingData.SubmittedBy);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting reporting data for metric {MetricId}", reportingData.MetricId);
                throw;
            }
        }

        public async Task<List<ReportingData>> GetUserReportsAsync(string userId)
        {
            try
            {
                return await _context.ReportingData
                    .Where(rd => rd.SubmittedBy == userId)
                    .Include(rd => rd.Metric)
                    .OrderByDescending(rd => rd.SubmittedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user reports for {UserId}", userId);
                throw;
            }
        }
    }
}
