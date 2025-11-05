using Compass.Models;

namespace Compass.Services;

public interface IProductsApiService
{
    Task<List<ProductDto>> GetProductsAsync(string? userEmail = null);
    Task<ProductDto?> GetProductByFipsIdAsync(string fipsId);
    Task<List<string>> GetPhasesAsync();
    Task<List<string>> GetBusinessAreasAsync();
    Task<bool> UpdateProductUrlAsync(string fipsId, string productUrl);
    Task<List<CategoryValueDto>> GetPhaseCategoryValuesAsync();
    Task<List<CategoryValueDto>> GetBusinessAreaCategoryValuesAsync();
    Task<bool> UpdateProductPhaseAsync(string fipsId, int phaseCategoryValueId);
    Task<bool> UpdateProductBusinessAreaAsync(string fipsId, int businessAreaCategoryValueId);
    Task<bool> UpdateProductStateAsync(string fipsId, string state);
}

