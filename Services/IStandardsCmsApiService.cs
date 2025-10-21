namespace Compass.Services;

public interface IStandardsCmsApiService
{
    Task<T?> GetAsync<T>(string endpoint, TimeSpan? cacheDuration = null);
}

