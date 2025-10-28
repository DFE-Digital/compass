using Compass.Models;

namespace Compass.Services;

public interface IProductsApiService
{
    Task<List<ProductDto>> GetProductsAsync(string? userEmail = null);
    Task<ProductDto?> GetProductByFipsIdAsync(string fipsId);
    Task<List<string>> GetPhasesAsync();
    Task<List<string>> GetBusinessAreasAsync();
    Task<bool> UpdateProductUrlAsync(string fipsId, string productUrl);
}

