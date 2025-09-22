using FipsReporting.Data;
using FipsReporting.Models;
using Microsoft.EntityFrameworkCore;

namespace FipsReporting.Services
{
    public interface IReportingStatusService
    {
        Task<string> GetPerformanceStatusAsync(string fipsId, string reportingPeriod);
        Task<string> GetSubmissionStatusAsync(string fipsId, string reportingPeriod);
        Task<(int completed, int total)> GetServiceCompletionCountAsync(string userEmail, string reportingPeriod);
        Task<string> GetDueDateStatusAsync(DateTime dueDate);
        Task<bool> IsReportSubmittedAsync(string userEmail, string reportingPeriod);
    }

    public class ReportingStatusService : IReportingStatusService
    {
        private readonly ReportingDbContext _context;
        private readonly ILogger<ReportingStatusService> _logger;
        private readonly CmsApiService _cmsApiService;

        public ReportingStatusService(ReportingDbContext context, ILogger<ReportingStatusService> logger, CmsApiService cmsApiService)
        {
            _context = context;
            _logger = logger;
            _cmsApiService = cmsApiService;
        }

        public async Task<string> GetPerformanceStatusAsync(string fipsId, string reportingPeriod)
        {
            try
            {
                // Get all enabled metrics for this product
                var totalMetrics = await _context.PerformanceMetrics
                    .Where(m => m.Enabled)
                    .CountAsync();

                if (totalMetrics == 0)
                {
                    return "Not started";
                }

                // Get completed metrics for this product and period
                // A metric is considered complete if it has a value OR is marked as not applicable
                var completedMetrics = await _context.PerformanceMetricData
                    .Where(d => d.ProductId == fipsId && 
                               d.ReportingPeriod == reportingPeriod &&
                               (!string.IsNullOrEmpty(d.Value) || d.IsNullReturn))
                    .CountAsync();

                if (completedMetrics == 0)
                {
                    return "Not started";
                }
                else if (completedMetrics == totalMetrics)
                {
                    return "Complete";
                }
                else
                {
                    return "In progress";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating performance status for {FipsId} in {ReportingPeriod}", fipsId, reportingPeriod);
                return "Not started";
            }
        }

        public async Task<string> GetSubmissionStatusAsync(string fipsId, string reportingPeriod)
        {
            try
            {
                // Check if this specific product's report has been submitted
                var isSubmitted = await _context.PerformanceMetricData
                    .Where(d => d.ProductId == fipsId && d.ReportingPeriod == reportingPeriod)
                    .AnyAsync(d => d.IsSubmitted); // We'll need to add this field to PerformanceMetricData

                if (isSubmitted)
                {
                    return "Submitted";
                }

                // Check if all metrics are complete
                var status = await GetPerformanceStatusAsync(fipsId, reportingPeriod);
                if (status == "Complete")
                {
                    return "Ready to submit";
                }
                else
                {
                    return "Cannot submit";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating submission status for {FipsId} in {ReportingPeriod}", fipsId, reportingPeriod);
                return "Cannot submit";
            }
        }

        public async Task<(int completed, int total)> GetServiceCompletionCountAsync(string userEmail, string reportingPeriod)
        {
            try
            {
                // Get all products assigned to this user from CMS
                var assignedProducts = await _cmsApiService.GetProductsByUserEmailAsync(userEmail);
                var totalProducts = assignedProducts.Count;

                if (totalProducts == 0)
                {
                    return (0, 0);
                }

                // Get total number of enabled metrics
                var totalMetrics = await _context.PerformanceMetrics
                    .Where(m => m.Enabled)
                    .CountAsync();

                if (totalMetrics == 0)
                {
                    return (0, totalProducts);
                }

                // Count how many products are complete (all their metrics are done)
                var completedProducts = 0;
                foreach (var product in assignedProducts)
                {
                    var productStatus = await GetPerformanceStatusAsync(product.FipsId, reportingPeriod);
                    if (productStatus == "Complete")
                    {
                        completedProducts++;
                    }
                }

                return (completedProducts, totalProducts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating service completion count for {UserEmail} in {ReportingPeriod}", userEmail, reportingPeriod);
                return (0, 1);
            }
        }

        public async Task<string> GetDueDateStatusAsync(DateTime dueDate)
        {
            var now = DateTime.UtcNow.Date;
            var dueDateOnly = dueDate.Date;

            if (now > dueDateOnly)
            {
                return "Overdue";
            }
            else if (now.AddDays(7) >= dueDateOnly)
            {
                return "Due soon";
            }
            else
            {
                return "Upcoming";
            }
        }

        public async Task<bool> IsReportSubmittedAsync(string userEmail, string reportingPeriod)
        {
            try
            {
                // Check if any of the user's reports have been submitted
                return await _context.PerformanceMetricData
                    .Where(d => d.ReportingPeriod == reportingPeriod && d.SubmittedBy == userEmail)
                    .AnyAsync(d => d.IsSubmitted); // We'll need to add this field
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if report is submitted for {UserEmail} in {ReportingPeriod}", userEmail, reportingPeriod);
                return false;
            }
        }
    }
}
