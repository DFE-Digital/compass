using FipsReporting.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FipsReporting.Services
{
    public interface IPerformanceMetricService
    {
        Task<List<PerformanceMetric>> GetAllMetricsAsync();
        Task<List<PerformanceMetric>> GetActiveMetricsAsync();
        Task<PerformanceMetric?> GetMetricByIdAsync(int id);
        Task<PerformanceMetric?> GetMetricByUniqueIdAsync(string uniqueId);
        Task<PerformanceMetric> CreateMetricAsync(PerformanceMetric metric, string createdBy);
        Task<PerformanceMetric> UpdateMetricAsync(PerformanceMetric metric, string updatedBy);
        Task<bool> DeleteMetricAsync(int id);
        Task<List<PerformanceMetricData>> GetMetricDataForProductAsync(string productId, string reportingPeriod);
        Task<PerformanceMetricData?> GetMetricDataAsync(int metricId, string productId, string reportingPeriod);
        Task<PerformanceMetricData> SaveMetricDataAsync(PerformanceMetricData data);
        Task<List<string>> GetPhasesFromCmsAsync();
        Task<List<PerformanceMetricData>> GetMetricDataForUserAsync(string userEmail, string reportingPeriod);
        Task UpdateMetricDataAsync(List<PerformanceMetricData> dataList);
    }

    public class PerformanceMetricService : IPerformanceMetricService
    {
        private readonly ReportingDbContext _context;
        private readonly ILogger<PerformanceMetricService> _logger;
        private readonly CmsApiService _cmsApiService;

        public PerformanceMetricService(ReportingDbContext context, ILogger<PerformanceMetricService> logger, CmsApiService cmsApiService)
        {
            _context = context;
            _logger = logger;
            _cmsApiService = cmsApiService;
        }

        public async Task<List<PerformanceMetric>> GetAllMetricsAsync()
        {
            return await _context.PerformanceMetrics
                .OrderBy(m => m.Name)
                .ToListAsync();
        }

        public async Task<List<PerformanceMetric>> GetActiveMetricsAsync()
        {
            return await _context.PerformanceMetrics
                .Where(m => m.Enabled)
                .OrderBy(m => m.Name)
                .ToListAsync();
        }

        public async Task<PerformanceMetric?> GetMetricByIdAsync(int id)
        {
            return await _context.PerformanceMetrics
                .Include(m => m.PerformanceData)
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<PerformanceMetric?> GetMetricByUniqueIdAsync(string uniqueId)
        {
            return await _context.PerformanceMetrics
                .Include(m => m.PerformanceData)
                .FirstOrDefaultAsync(m => m.UniqueId == uniqueId);
        }

        public async Task<PerformanceMetric> CreateMetricAsync(PerformanceMetric metric, string createdBy)
        {
            metric.CreatedBy = createdBy;
            metric.UpdatedBy = createdBy;
            metric.CreatedAt = DateTime.UtcNow;
            metric.UpdatedAt = DateTime.UtcNow;

            _context.PerformanceMetrics.Add(metric);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created performance metric {MetricName} with ID {MetricId}", metric.Name, metric.Id);
            return metric;
        }

        public async Task<PerformanceMetric> UpdateMetricAsync(PerformanceMetric metric, string updatedBy)
        {
            metric.UpdatedBy = updatedBy;
            metric.UpdatedAt = DateTime.UtcNow;

            _context.PerformanceMetrics.Update(metric);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated performance metric {MetricName} with ID {MetricId}", metric.Name, metric.Id);
            return metric;
        }

        public async Task<bool> DeleteMetricAsync(int id)
        {
            var metric = await _context.PerformanceMetrics.FindAsync(id);
            if (metric == null)
                return false;

            _context.PerformanceMetrics.Remove(metric);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted performance metric {MetricName} with ID {MetricId}", metric.Name, metric.Id);
            return true;
        }

        public async Task<List<PerformanceMetricData>> GetMetricDataForProductAsync(string productId, string reportingPeriod)
        {
            return await _context.PerformanceMetricData
                .Include(d => d.PerformanceMetric)
                .Where(d => d.ProductId == productId && d.ReportingPeriod == reportingPeriod)
                .ToListAsync();
        }

        public async Task<PerformanceMetricData?> GetMetricDataAsync(int metricId, string productId, string reportingPeriod)
        {
            return await _context.PerformanceMetricData
                .Include(d => d.PerformanceMetric)
                .FirstOrDefaultAsync(d => d.PerformanceMetricId == metricId && 
                                        d.ProductId == productId && 
                                        d.ReportingPeriod == reportingPeriod);
        }

        public async Task<PerformanceMetricData> SaveMetricDataAsync(PerformanceMetricData data)
        {
            var existing = await _context.PerformanceMetricData
                .FirstOrDefaultAsync(d => d.PerformanceMetricId == data.PerformanceMetricId &&
                                        d.ProductId == data.ProductId &&
                                        d.ReportingPeriod == data.ReportingPeriod);

            if (existing != null)
            {
                existing.Value = data.Value;
                existing.Comment = data.Comment;
                existing.IsNullReturn = data.IsNullReturn;
                existing.UpdatedAt = DateTime.UtcNow;
                _context.PerformanceMetricData.Update(existing);
                await _context.SaveChangesAsync();
                return existing;
            }
            else
            {
                data.CreatedAt = DateTime.UtcNow;
                data.UpdatedAt = DateTime.UtcNow;
                _context.PerformanceMetricData.Add(data);
                await _context.SaveChangesAsync();
                return data;
            }
        }

        public async Task<List<string>> GetPhasesFromCmsAsync()
        {
            try
            {
                // Get category values filtered by category_type = 'Phase' and enabled = true
                var phases = await _cmsApiService.GetCategoryValuesByTypeAsync("Phase");
                
                // If we only have one phase from CMS, use fallback phases instead
                if (phases.Count <= 1)
                {
                    _logger.LogInformation("Only {Count} phase(s) found in CMS, using fallback phases", phases.Count);
                    return new List<string>
                    {
                        "Alpha",
                        "Beta", 
                        "Live",
                        "Retired"
                    };
                }

                _logger.LogInformation("Fetched {Count} phases from CMS API", phases.Count);
                return phases;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching phases from CMS API");
                
                // Fallback to hardcoded phases if CMS API fails
                return new List<string>
                {
                    "Alpha",
                    "Beta", 
                    "Live",
                    "Retired"
                };
            }
        }

        public async Task<List<PerformanceMetricData>> GetMetricDataForUserAsync(string userEmail, string reportingPeriod)
        {
            return await _context.PerformanceMetricData
                .Where(d => d.SubmittedBy == userEmail && d.ReportingPeriod == reportingPeriod)
                .ToListAsync();
        }

        public async Task UpdateMetricDataAsync(List<PerformanceMetricData> dataList)
        {
            foreach (var data in dataList)
            {
                _context.PerformanceMetricData.Update(data);
            }
            await _context.SaveChangesAsync();
        }
    }
}
