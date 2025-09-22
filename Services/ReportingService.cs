using FipsReporting.Data;
using FipsReporting.Models;
using Microsoft.EntityFrameworkCore;

namespace FipsReporting.Services
{
    public interface IReportingService
    {
        Task<List<ProductViewModel>> GetProductsForUserAsync(string userEmail, ProductFilter filter);
        Task<bool> IsProductAllocatedToUserAsync(string productId, string userEmail);
        Task AllocateProductToUserAsync(string productId, string userEmail, string allocatedBy);
        Task DeallocateProductFromUserAsync(string productId, string userEmail);
    }

    public class ReportingService : IReportingService
    {
        private readonly ReportingDbContext _context;
        private readonly CmsApiService _cmsApiService;
        private readonly ILogger<ReportingService> _logger;

        public ReportingService(ReportingDbContext context, CmsApiService cmsApiService, ILogger<ReportingService> logger)
        {
            _context = context;
            _cmsApiService = cmsApiService;
            _logger = logger;
        }

        public async Task<List<ProductViewModel>> GetProductsForUserAsync(string userEmail, ProductFilter filter)
        {
            try
            {
                // Set filter to only get active products
                filter.State = "Active";
                
                var cmsResponse = await _cmsApiService.GetProductsAsync(filter);
                
                var products = new List<ProductViewModel>();
                
                foreach (var product in cmsResponse.Data)
                {
                    var isAllocated = await IsProductAllocatedToUserAsync(product.Id.ToString(), userEmail);
                    
                    // Only include products where user is allocated
                    if (isAllocated)
                    {
                        var viewModel = _cmsApiService.MapToViewModel(product, isAllocated);
                        products.Add(viewModel);
                    }
                }

                return products;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting products for user {UserEmail}", userEmail);
                throw;
            }
        }

        public async Task<bool> IsProductAllocatedToUserAsync(string productId, string userEmail)
        {
            try
            {
                return await _context.ProductAllocations
                    .AnyAsync(pa => pa.ProductId == productId && pa.UserEmail == userEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking product allocation for product {ProductId} and user {UserEmail}", productId, userEmail);
                throw;
            }
        }

        public async Task AllocateProductToUserAsync(string productId, string userEmail, string allocatedBy)
        {
            try
            {
                var existingAllocation = await _context.ProductAllocations
                    .FirstOrDefaultAsync(pa => pa.ProductId == productId && pa.UserEmail == userEmail);

                if (existingAllocation == null)
                {
                    var allocation = new ProductAllocation
                    {
                        ProductId = productId,
                        UserEmail = userEmail,
                        AllocatedAt = DateTime.UtcNow,
                        AllocatedBy = allocatedBy
                    };

                    _context.ProductAllocations.Add(allocation);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("Product {ProductId} allocated to user {UserEmail} by {AllocatedBy}", 
                        productId, userEmail, allocatedBy);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error allocating product {ProductId} to user {UserEmail}", productId, userEmail);
                throw;
            }
        }

        public async Task DeallocateProductFromUserAsync(string productId, string userEmail)
        {
            try
            {
                var allocation = await _context.ProductAllocations
                    .FirstOrDefaultAsync(pa => pa.ProductId == productId && pa.UserEmail == userEmail);

                if (allocation != null)
                {
                    _context.ProductAllocations.Remove(allocation);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("Product {ProductId} deallocated from user {UserEmail}", productId, userEmail);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deallocating product {ProductId} from user {UserEmail}", productId, userEmail);
                throw;
            }
        }
    }
}
