using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace Compass.Services;

public class StandardsCmsApiService : IStandardsCmsApiService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<StandardsCmsApiService> _logger;
    private readonly IMemoryCache _cache;
    private readonly string _baseUrl;

    public StandardsCmsApiService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<StandardsCmsApiService> logger,
        IMemoryCache cache)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _cache = cache;
        _baseUrl = _configuration["StandardsCmsApi:BaseUrl"] ?? "https://dfe-standards-cms-217ce4e280a0.herokuapp.com/api/";
        
        // Configure HttpClient timeout
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<T?> GetAsync<T>(string endpoint, TimeSpan? cacheDuration = null)
    {
        try
        {
            var fullUrl = $"{_baseUrl.TrimEnd('/')}/{endpoint.TrimStart('/')}";
            var cacheKey = $"standards_cms_api_{endpoint}";

            // Check cache first if duration specified
            if (cacheDuration.HasValue && _cache.TryGetValue<T>(cacheKey, out var cachedResult))
            {
                _logger.LogInformation("Cache hit for Standards CMS endpoint: {Endpoint}", endpoint);
                return cachedResult;
            }

            // Use read API key
            var readApiKey = _configuration["StandardsCmsApi:ReadApiKey"];
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
                _logger.LogInformation("Cached Standards CMS result for endpoint: {Endpoint} for {Duration}", endpoint, cacheDuration.Value);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Standards CMS API endpoint: {Endpoint}", endpoint);
            return default;
        }
    }
}

