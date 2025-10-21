using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace Compass.Services;

public class CmsApiService : ICmsApiService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CmsApiService> _logger;
    private readonly IMemoryCache _cache;
    private readonly string _baseUrl;

    public CmsApiService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<CmsApiService> logger,
        IMemoryCache cache)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _cache = cache;
        _baseUrl = _configuration["CmsApi:BaseUrl"] ?? "http://localhost:1337/api";
        
        // Configure HttpClient timeout
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<T?> GetAsync<T>(string endpoint, TimeSpan? cacheDuration = null)
    {
        try
        {
            var fullUrl = $"{_baseUrl.TrimEnd('/')}/{endpoint.TrimStart('/')}";
            var cacheKey = $"cms_api_{endpoint}";

            // Check cache first if duration specified
            if (cacheDuration.HasValue && _cache.TryGetValue<T>(cacheKey, out var cachedResult))
            {
                _logger.LogInformation("Cache hit for endpoint: {Endpoint}", endpoint);
                return cachedResult;
            }

            // Use read API key for GET requests
            var readApiKey = _configuration["CmsApi:ReadApiKey"];
            if (!string.IsNullOrEmpty(readApiKey))
            {
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", readApiKey);
            }

            var response = await _httpClient.GetAsync(fullUrl);
            response.EnsureSuccessStatusCode();

            var jsonContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<T>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            // Cache the result if cache duration is specified
            if (cacheDuration.HasValue && result != null)
            {
                _cache.Set(cacheKey, result, cacheDuration.Value);
                _logger.LogInformation("Cached result for endpoint: {Endpoint} for {Duration}", endpoint, cacheDuration.Value);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling CMS API endpoint: {Endpoint}", endpoint);
            return default;
        }
    }

    public async Task<T?> PostAsync<T>(string endpoint, object data)
    {
        try
        {
            var fullUrl = $"{_baseUrl.TrimEnd('/')}/{endpoint.TrimStart('/')}";
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Use write API key for POST requests
            var writeApiKey = _configuration["CmsApi:WriteApiKey"];
            if (!string.IsNullOrEmpty(writeApiKey))
            {
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", writeApiKey);
            }

            var response = await _httpClient.PostAsync(fullUrl, content);
            response.EnsureSuccessStatusCode();

            var jsonContent = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling CMS API endpoint: {Endpoint}", endpoint);
            return default;
        }
    }

    public async Task<T?> PutAsync<T>(string endpoint, object data)
    {
        try
        {
            var fullUrl = $"{_baseUrl.TrimEnd('/')}/{endpoint.TrimStart('/')}";
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Use write API key for PUT requests
            var writeApiKey = _configuration["CmsApi:WriteApiKey"];
            if (!string.IsNullOrEmpty(writeApiKey))
            {
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", writeApiKey);
            }

            var response = await _httpClient.PutAsync(fullUrl, content);
            response.EnsureSuccessStatusCode();

            var jsonContent = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling CMS API endpoint: {Endpoint}", endpoint);
            return default;
        }
    }

    public async Task<bool> DeleteAsync(string endpoint)
    {
        try
        {
            var fullUrl = $"{_baseUrl.TrimEnd('/')}/{endpoint.TrimStart('/')}";

            // Use write API key for DELETE requests
            var writeApiKey = _configuration["CmsApi:WriteApiKey"];
            if (!string.IsNullOrEmpty(writeApiKey))
            {
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", writeApiKey);
            }

            var response = await _httpClient.DeleteAsync(fullUrl);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling CMS API endpoint: {Endpoint}", endpoint);
            return false;
        }
    }
}

