using Compass.Models;

namespace Compass.Services;

public interface IStandardsCmsApiService
{
    Task<T?> GetAsync<T>(string endpoint, TimeSpan? cacheDuration = null);
    Task<List<StandardDto>> GetStandardsAsync(bool? published = null, string? search = null, string? category = null, string? stage = null, TimeSpan? cacheDuration = null);
        Task<StandardDto?> GetStandardByIdAsync(int id, TimeSpan? cacheDuration = null);
        Task<StandardDto?> GetStandardByDocumentIdAsync(string documentId, TimeSpan? cacheDuration = null);
        Task<List<StandardCategoryDto>> GetCategoriesAsync(TimeSpan? cacheDuration = null);
        Task<List<StandardStageDto>> GetStagesAsync(TimeSpan? cacheDuration = null);
        Task<List<StandardSubCategoryDto>> GetSubCategoriesByIdsAsync(List<int> subCategoryIds, TimeSpan? cacheDuration = null);
}

