namespace Compass.Services;

public interface ICmsApiService
{
    Task<T?> GetAsync<T>(string endpoint, TimeSpan? cacheDuration = null);
    Task<T?> PostAsync<T>(string endpoint, object data);
    Task<T?> PutAsync<T>(string endpoint, object data);
    Task<bool> DeleteAsync(string endpoint);
}

