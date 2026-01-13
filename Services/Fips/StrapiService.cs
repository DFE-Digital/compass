using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Compass.Models.Fips;
using Microsoft.Extensions.Options;

namespace Compass.Services.Fips;

public class StrapiService : IStrapiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<StrapiService> _logger;
    private readonly string _apiKey;
    private readonly string _endpoint;

    // Constructor for dependency injection with IOptions (gets default from config)
    public StrapiService(
        HttpClient httpClient,
        IOptions<FipsSyncConfiguration> config,
        ILogger<StrapiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        
        // Default to production if not specified
        _endpoint = config.Value.Strapi.Production.Endpoint;
        _apiKey = config.Value.Strapi.Production.ApiKey;
        
        ConfigureHttpClient();
    }

    // Constructor for specifying environment dynamically
    public StrapiService(
        HttpClient httpClient,
        string endpoint,
        string apiKey,
        ILogger<StrapiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _endpoint = endpoint;
        _apiKey = apiKey;
        
        ConfigureHttpClient();
    }

    private void ConfigureHttpClient()
    {
        _httpClient.BaseAddress = new Uri(_endpoint);
        _httpClient.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", _apiKey);
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<List<StrapiProduct>> GetAllProductsAsync()
    {
        try
        {
            _logger.LogInformation("Fetching all products from Strapi ({Endpoint})", _endpoint);

            var allProducts = new List<StrapiProduct>();
            var page = 1;
            var pageSize = 100;
            var hasMore = true;

            while (hasMore)
            {
                var queryParams = new Dictionary<string, string>
                {
                    ["pagination[page]"] = page.ToString(),
                    ["pagination[pageSize]"] = pageSize.ToString(),
                    ["populate"] = "*"
                };

                var query = string.Join("&", queryParams.Select(kvp => 
                    $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
                
                var response = await _httpClient.GetAsync($"/products?{query}");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<StrapiResponse<List<StrapiProduct>>>(content, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (result?.Data != null && result.Data.Any())
                {
                    allProducts.AddRange(result.Data);
                    _logger.LogInformation("Fetched page {Page}, got {Count} products", 
                        page, result.Data.Count);
                    
                    // Check if there are more pages
                    hasMore = result.Meta?.Pagination != null && 
                              page < result.Meta.Pagination.PageCount;
                    page++;
                }
                else
                {
                    hasMore = false;
                }
            }

            _logger.LogInformation("Successfully fetched {Count} total products from Strapi", 
                allProducts.Count);
            return allProducts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching products from Strapi");
            throw;
        }
    }

    public async Task<int> GetProductCountAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/products?pagination[pageSize]=1");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<StrapiResponse<List<StrapiProduct>>>(content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var count = result?.Meta?.Pagination?.Total ?? 0;
            _logger.LogInformation("Strapi has {Count} total products", count);
            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting product count from Strapi");
            throw;
        }
    }

    public async Task<StrapiProduct?> FindProductByCmdbSysIdAsync(string cmdbSysId)
    {
        try
        {
            if (string.IsNullOrEmpty(cmdbSysId))
                return null;

            // Search for product by cmdb_sys_id
            var filter = $"filters[cmdb_sys_id][$eq]={Uri.EscapeDataString(cmdbSysId)}";
            var response = await _httpClient.GetAsync($"/products?{filter}&populate=*");
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to find product with CMDB SysId {CmdbSysId}", cmdbSysId);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<StrapiResponse<List<StrapiProduct>>>(content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return result?.Data?.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding product by CMDB SysId {CmdbSysId}", cmdbSysId);
            return null;
        }
    }

    public async Task<StrapiProduct> CreateProductAsync(CmdbEntry cmdbEntry)
    {
        try
        {
            _logger.LogInformation("Creating product in Strapi: {Name}", cmdbEntry.Name);

            var productData = new StrapiCreateProductRequest
            {
                Data = new StrapiProductData
                {
                    CmdbSysId = cmdbEntry.SysId,
                    Title = cmdbEntry.Name,
                    ShortDescription = TruncateString(cmdbEntry.Description, 200),
                    LongDescription = cmdbEntry.Description ?? string.Empty,
                    ParentCategory = cmdbEntry.ParentName,
                    State = "New",
                    CmdbLastSync = DateTime.UtcNow,
                    PublishedAt = DateTime.UtcNow
                }
            };

            var json = JsonSerializer.Serialize(productData, new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
            });
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/products", httpContent);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<StrapiResponse<StrapiProduct>>(responseContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            _logger.LogInformation("Successfully created product: {Name}", cmdbEntry.Name);
            return result?.Data ?? throw new Exception("Failed to deserialize created product");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product {Name} in Strapi", cmdbEntry.Name);
            throw;
        }
    }

    public async Task<StrapiProduct> UpdateProductAsync(
        string documentId, 
        CmdbEntry cmdbEntry, 
        StrapiProduct existingProduct)
    {
        try
        {
            _logger.LogInformation("Updating product in Strapi: {Name}", cmdbEntry.Name);

            // Only update if there are actual changes
            var hasChanges = false;
            var existingAttrs = existingProduct.Attributes;

            if (existingAttrs == null)
            {
                _logger.LogWarning("Existing product has no attributes, forcing update");
                hasChanges = true;
            }
            else
            {
                hasChanges = 
                    existingAttrs.Title != cmdbEntry.Name ||
                    existingAttrs.ShortDescription != TruncateString(cmdbEntry.Description, 200) ||
                    existingAttrs.LongDescription != cmdbEntry.Description ||
                    existingAttrs.ParentCategory != cmdbEntry.ParentName;
            }

            if (!hasChanges)
            {
                _logger.LogInformation("No changes detected for {Name}, skipping update", cmdbEntry.Name);
                
                // Update the sync timestamp only
                var syncUpdateData = new StrapiCreateProductRequest
                {
                    Data = new StrapiProductData
                    {
                        CmdbSysId = cmdbEntry.SysId,
                        Title = cmdbEntry.Name,
                        ShortDescription = existingAttrs?.ShortDescription ?? string.Empty,
                        LongDescription = existingAttrs?.LongDescription ?? string.Empty,
                        ParentCategory = existingAttrs?.ParentCategory,
                        State = existingAttrs?.State ?? "New",
                        CmdbLastSync = DateTime.UtcNow,
                        PublishedAt = existingAttrs?.PublishedAt
                    }
                };

                var syncJson = JsonSerializer.Serialize(syncUpdateData, new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
                });
                var syncContent = new StringContent(syncJson, Encoding.UTF8, "application/json");

                var syncResponse = await _httpClient.PutAsync($"/products/{documentId}", syncContent);
                syncResponse.EnsureSuccessStatusCode();

                return existingProduct;
            }

            // Perform full update
            var productData = new StrapiCreateProductRequest
            {
                Data = new StrapiProductData
                {
                    CmdbSysId = cmdbEntry.SysId,
                    Title = cmdbEntry.Name,
                    ShortDescription = TruncateString(cmdbEntry.Description, 200),
                    LongDescription = cmdbEntry.Description ?? string.Empty,
                    ParentCategory = cmdbEntry.ParentName,
                    State = existingAttrs?.State ?? "New",
                    CmdbLastSync = DateTime.UtcNow,
                    PublishedAt = existingAttrs?.PublishedAt ?? DateTime.UtcNow
                }
            };

            var json = JsonSerializer.Serialize(productData, new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
            });
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync($"/products/{documentId}", httpContent);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<StrapiResponse<StrapiProduct>>(responseContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            _logger.LogInformation("Successfully updated product: {Name}", cmdbEntry.Name);
            return result?.Data ?? throw new Exception("Failed to deserialize updated product");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product {Name} in Strapi", cmdbEntry.Name);
            throw;
        }
    }

    public async Task DeleteProductAsync(string documentId)
    {
        try
        {
            _logger.LogInformation("Deleting product from Strapi: {DocumentId}", documentId);

            var response = await _httpClient.DeleteAsync($"/products/{documentId}");
            response.EnsureSuccessStatusCode();

            _logger.LogInformation("Successfully deleted product: {DocumentId}", documentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting product {DocumentId} from Strapi", documentId);
            throw;
        }
    }

    private static string TruncateString(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        if (value.Length <= maxLength)
            return value;

        return value.Substring(0, maxLength - 3) + "...";
    }
}
