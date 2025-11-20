using Compass.Models;

namespace Compass.Services;

public interface IProductsApiService
{
    Task<List<ProductDto>> GetProductsAsync(string? userEmail = null);
    Task<List<ProductDto>> GetProductsByServiceOwnerAsync(string? userEmail);
    Task<List<ProductDto>> GetAllProductsAsync(string? userEmail = null);
    Task<ProductDto?> GetProductByFipsIdAsync(string fipsId);
    Task<List<string>> GetPhasesAsync();
    Task<List<string>> GetTypesAsync();
    Task<List<string>> GetBusinessAreasAsync();
    Task<bool> UpdateProductUrlAsync(string fipsId, string productUrl);
    Task<List<CategoryValueDto>> GetPhaseCategoryValuesAsync();
    Task<List<CategoryValueDto>> GetBusinessAreaCategoryValuesAsync();
    Task<List<CategoryValueDto>> GetUserGroupCategoryValuesAsync();
    Task<bool> UpdateProductPhaseAsync(string fipsId, int phaseCategoryValueId);
    Task<bool> RemoveDuplicatePhasesAsync(string fipsId, int phaseCategoryValueIdToKeep);
    Task<bool> UpdateProductBusinessAreaAsync(string fipsId, int businessAreaCategoryValueId);
    Task<bool> UpdateProductUserGroupsAsync(string fipsId, IEnumerable<int> userGroupCategoryValueIds);
    Task<bool> UpdateProductStateAsync(string fipsId, string state);
    Task<ProductDto?> CreateProductAsync(string title, string? shortDescription, string? longDescription, List<int> categoryValueIds, string state = "Active");
    Task<List<ProductDto>> SearchProductsByTitleAsync(string searchTerm);
    Task<List<EntraUserDto>> GetEntraUsersAsync();
    Task<EntraUserDto?> GetOrCreateEntraUserAsync(string emailAddress, string? entraId = null, string? displayName = null, string? firstName = null, string? lastName = null);
    Task<bool> UpdateProductServiceOwnerAsync(string fipsId, int entraUserId);
    Task<bool> UpdateProductRoleAsync(string fipsId, string roleFieldName, int entraUserId);
    Task<bool> UpdateProductBasicInfoAsync(string fipsId, string? title, string? shortDescription, string? longDescription, string? cmdbSysId);
}

