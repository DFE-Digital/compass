using System.Text;
using System.Text.Json;
using Compass.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Compass.Services;

public class ProductsApiService : IProductsApiService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ProductsApiService> _logger;
    private readonly IConfiguration _configuration;
    private readonly JsonSerializerOptions _jsonOptions;

    public ProductsApiService(
        HttpClient httpClient, 
        IMemoryCache cache, 
        ILogger<ProductsApiService> logger,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
        _configuration = configuration;
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<List<ProductDto>> GetProductsAsync(string? userEmail = null)
    {
        var cacheKey = string.IsNullOrEmpty(userEmail) 
            ? "products_list_all" 
            : $"products_list_{userEmail}";
        
        if (_cache.TryGetValue(cacheKey, out List<ProductDto>? cachedProducts))
        {
            return cachedProducts ?? new List<ProductDto>();
        }

        try
        {
            var queryParams = new List<string>
            {
                "sort=title:asc",
                "filters[state][$eq]=Active",
                "pagination[pageSize]=1000",
                "fields[0]=id",
                "fields[1]=title",
                "fields[2]=fips_id",
                "fields[3]=product_url",
                "populate[category_values][fields][0]=name",
                "populate[category_values][populate][category_type][fields][0]=name",
                "populate[product_contacts][populate][users_permissions_user][fields][0]=email",
                "populate[product_contacts][populate][users_permissions_user][fields][1]=username"
            };

            var queryString = string.Join("&", queryParams);
            var url = $"products?{queryString}";

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError($"Failed to fetch products from CMS. Status: {response.StatusCode}, Error: {errorContent}");
                return new List<ProductDto>();
            }

            var content = await response.Content.ReadAsStringAsync();

            var apiResponse = JsonSerializer.Deserialize<ApiCollectionResponse<ProductDto>>(content, _jsonOptions);
            var products = apiResponse?.Data ?? new List<ProductDto>();
            
            _logger.LogInformation("Fetched {Count} products from CMS", products.Count);

            // Extract phase from category_values
            foreach (var product in products)
            {
                if (product.CategoryValues != null)
                {
                    var phaseCategory = product.CategoryValues
                        .FirstOrDefault(cv => cv.CategoryType?.Name?.Equals("Phase", StringComparison.OrdinalIgnoreCase) == true);
                    
                    if (phaseCategory != null)
                    {
                        product.Phase = phaseCategory.Name;
                    }
                }
            }

            // Filter by user email if provided (case-insensitive)
            if (!string.IsNullOrEmpty(userEmail))
            {
                var filteredProducts = products.Where(p => 
                    p.ProductContacts != null && 
                    p.ProductContacts.Any(pc => 
                        pc.UsersPermissionsUser != null &&
                        !string.IsNullOrEmpty(pc.UsersPermissionsUser.Email) && 
                        pc.UsersPermissionsUser.Email.Equals(userEmail, StringComparison.OrdinalIgnoreCase)
                    )
                ).ToList();
                
                _logger.LogInformation("Filtered to {Count} products for user {Email}", filteredProducts.Count, userEmail);
                products = filteredProducts;
            }

            // Cache for 5 minutes
            _cache.Set(cacheKey, products, TimeSpan.FromMinutes(5));

            return products;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching products from CMS");
            return new List<ProductDto>();
        }
    }

    public async Task<ProductDto?> GetProductByFipsIdAsync(string fipsId)
    {
        if (string.IsNullOrEmpty(fipsId))
        {
            return null;
        }

        var cacheKey = $"product_{fipsId}";
        
        if (_cache.TryGetValue(cacheKey, out ProductDto? cachedProduct))
        {
            return cachedProduct;
        }

        try
        {
            // Explicitly request fields we need, including product_url which may not be in default response
            // Populate category_values with category_type to get Business area
            // fips_id will be included automatically since we're filtering by it
            var queryParams = new[]
            {
                "filters[fips_id][$eq]=" + fipsId,
                "fields[0]=id",
                "fields[1]=documentId",
                "fields[2]=title",
                "fields[3]=fips_id",
                "fields[4]=product_url",
                "populate[category_values][fields][0]=id",
                "populate[category_values][fields][1]=name",
                "populate[category_values][populate][category_type][fields][0]=name"
            };

            var queryString = string.Join("&", queryParams);
            var url = $"products?{queryString}";

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Failed to fetch product {fipsId} from CMS. Status: {response.StatusCode}");
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("CMS API Response for {FipsId}: {Content}", fipsId, content);

            var apiResponse = JsonSerializer.Deserialize<ApiCollectionResponse<ProductDto>>(content, _jsonOptions);
            var product = apiResponse?.Data?.FirstOrDefault();

            if (product != null)
            {
                // Extract phase from category_values
                if (product.CategoryValues != null)
                {
                    var phaseCategory = product.CategoryValues
                        .FirstOrDefault(cv => cv.CategoryType?.Name?.Equals("Phase", StringComparison.OrdinalIgnoreCase) == true);
                    
                    if (phaseCategory != null)
                    {
                        product.Phase = phaseCategory.Name;
                    }
                }

                // Cache for 5 minutes
                _cache.Set(cacheKey, product, TimeSpan.FromMinutes(5));
            }

            return product;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching product {FipsId} from CMS", fipsId);
            return null;
        }
    }

    public async Task<List<string>> GetPhasesAsync()
    {
        var cacheKey = "AllPhases";
        if (_cache.TryGetValue(cacheKey, out List<string>? phases))
        {
            if (phases != null) return phases;
        }

        try
        {
            // Fetch all category values where category_type.name = "Phase"
            var queryParams = new List<string>
            {
                "filters[category_type][name][$eq]=Phase",
                "fields[0]=name",
                "fields[1]=sort_order",
                "sort=sort_order:asc"
            };

            var queryString = string.Join("&", queryParams);
            var url = $"category-values?{queryString}";

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Failed to fetch phases from CMS. Status: {response.StatusCode}");
                return new List<string>();
            }

            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiCollectionResponse<CategoryValueDto>>(content, _jsonOptions);

            phases = apiResponse?.Data?
                .Where(cv => !string.IsNullOrEmpty(cv.Name))
                .OrderBy(cv => cv.SortOrder ?? int.MaxValue)  // Sort by sort_order, nulls last
                .ThenBy(cv => cv.Name)  // Then by name as fallback
                .Select(cv => cv.Name)
                .Distinct()
                .ToList() ?? new List<string>();

            _cache.Set(cacheKey, phases, TimeSpan.FromHours(1)); // Cache for 1 hour
            return phases;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching phases from CMS");
            return new List<string>();
        }
    }

    public async Task<List<string>> GetBusinessAreasAsync()
    {
        const string cacheKey = "BusinessAreas";

        // Try to get from cache first
        if (_cache.TryGetValue(cacheKey, out List<string>? businessAreas))
        {
            if (businessAreas != null) return businessAreas;
        }

        try
        {
            // Fetch all category values where category_type.name = "Business area"
            var queryParams = new List<string>
            {
                "filters[category_type][name][$eq]=Business area",
                "fields[0]=name",
                "fields[1]=sort_order",
                "sort=sort_order:asc"
            };

            var queryString = string.Join("&", queryParams);
            var url = $"category-values?{queryString}";

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Failed to fetch business areas from CMS. Status: {response.StatusCode}");
                return new List<string>();
            }

            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiCollectionResponse<CategoryValueDto>>(content, _jsonOptions);

            businessAreas = apiResponse?.Data?
                .Where(cv => !string.IsNullOrEmpty(cv.Name))
                .OrderBy(cv => cv.SortOrder ?? int.MaxValue)
                .ThenBy(cv => cv.Name)
                .Select(cv => cv.Name)
                .Distinct()
                .ToList() ?? new List<string>();

            _cache.Set(cacheKey, businessAreas, TimeSpan.FromHours(1)); // Cache for 1 hour
            return businessAreas;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching business areas from CMS");
            return new List<string>();
        }
    }

    public async Task<bool> UpdateProductUrlAsync(string fipsId, string productUrl)
    {
        try
        {
            // First, get the product to find its documentId
            var product = await GetProductByFipsIdAsync(fipsId);
            if (product == null || string.IsNullOrEmpty(product.DocumentId))
            {
                _logger.LogError("Product {FipsId} not found or missing documentId", fipsId);
                return false;
            }

            if (string.IsNullOrEmpty(product.FipsId))
            {
                _logger.LogError("Product {FipsId} is missing fips_id value", fipsId);
                return false;
            }

            // Prepare update data - include fips_id to prevent it from changing
            var updateData = new
            {
                data = new
                {
                    fips_id = product.FipsId, // Include existing fips_id to prevent it from changing
                    product_url = productUrl
                    
                }
            };

            var json = JsonSerializer.Serialize(updateData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Use write API key for PUT requests - create a new HttpClient since the main one has read-only headers
            var writeApiKey = _configuration["CmsApi:WriteApiKey"];
            var baseUrl = _configuration["CmsApi:BaseUrl"] ?? "http://localhost:1337/api";
            
            using var httpClient = new HttpClient();
            var baseUri = baseUrl.TrimEnd('/');
            if (!baseUri.EndsWith("/api", StringComparison.OrdinalIgnoreCase))
            {
                baseUri += "/api";
            }
            httpClient.BaseAddress = new Uri(baseUri + "/");
            
            if (!string.IsNullOrEmpty(writeApiKey))
            {
                httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", writeApiKey);
            }

            var response = await httpClient.PutAsync($"products/{product.DocumentId}", content);
            
            if (response.IsSuccessStatusCode)
            {
                // Clear cache for this product
                _cache.Remove($"product_{fipsId}");
                _cache.Remove("products_list_all");
                
                _logger.LogInformation("Successfully updated product URL for {FipsId}", fipsId);
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to update product URL for {FipsId}. Status: {StatusCode}, Error: {Error}", 
                    fipsId, response.StatusCode, errorContent);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product URL for {FipsId}", fipsId);
            return false;
        }
    }

    public async Task<List<CategoryValueDto>> GetPhaseCategoryValuesAsync()
    {
        var cacheKey = "PhaseCategoryValues";
        if (_cache.TryGetValue(cacheKey, out List<CategoryValueDto>? cachedValues))
        {
            if (cachedValues != null) return cachedValues;
        }

        try
        {
            var queryParams = new List<string>
            {
                "filters[category_type][name][$eq]=Phase",
                "fields[0]=id",
                "fields[1]=name",
                "fields[2]=sort_order",
                "sort=sort_order:asc"
            };

            var queryString = string.Join("&", queryParams);
            var url = $"category-values?{queryString}";

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Failed to fetch phase category values from CMS. Status: {response.StatusCode}");
                return new List<CategoryValueDto>();
            }

            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiCollectionResponse<CategoryValueDto>>(content, _jsonOptions);

            var values = apiResponse?.Data ?? new List<CategoryValueDto>();
            _cache.Set(cacheKey, values, TimeSpan.FromHours(1));
            return values;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching phase category values from CMS");
            return new List<CategoryValueDto>();
        }
    }

    public async Task<List<CategoryValueDto>> GetBusinessAreaCategoryValuesAsync()
    {
        var cacheKey = "BusinessAreaCategoryValues";
        if (_cache.TryGetValue(cacheKey, out List<CategoryValueDto>? cachedValues))
        {
            if (cachedValues != null) return cachedValues;
        }

        try
        {
            var queryParams = new List<string>
            {
                "filters[category_type][name][$eq]=Business area",
                "fields[0]=id",
                "fields[1]=name",
                "fields[2]=sort_order",
                "sort=sort_order:asc"
            };

            var queryString = string.Join("&", queryParams);
            var url = $"category-values?{queryString}";

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Failed to fetch business area category values from CMS. Status: {response.StatusCode}");
                return new List<CategoryValueDto>();
            }

            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiCollectionResponse<CategoryValueDto>>(content, _jsonOptions);

            var values = apiResponse?.Data ?? new List<CategoryValueDto>();
            _cache.Set(cacheKey, values, TimeSpan.FromHours(1));
            return values;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching business area category values from CMS");
            return new List<CategoryValueDto>();
        }
    }

    public async Task<bool> UpdateProductPhaseAsync(string fipsId, int phaseCategoryValueId)
    {
        try
        {
            var product = await GetProductByFipsIdAsync(fipsId);
            if (product == null || string.IsNullOrEmpty(product.DocumentId))
            {
                _logger.LogError("Product {FipsId} not found or missing documentId", fipsId);
                return false;
            }

            // Get all current category values
            var currentCategoryValues = new List<int>();
            if (product.CategoryValues != null)
            {
                foreach (var cv in product.CategoryValues)
                {
                    // Skip the current Phase value (if any) and add all others
                    var categoryType = cv.CategoryType?.Name;
                    if (categoryType != null && !categoryType.Equals("Phase", StringComparison.OrdinalIgnoreCase))
                    {
                        currentCategoryValues.Add(cv.Id);
                    }
                }
            }

            // Add the new Phase value
            currentCategoryValues.Add(phaseCategoryValueId);

            var updateData = new
            {
                data = new
                {
                    fips_id = product.FipsId,
                    category_values = currentCategoryValues
                }
            };

            var json = JsonSerializer.Serialize(updateData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var writeApiKey = _configuration["CmsApi:WriteApiKey"];
            var baseUrl = _configuration["CmsApi:BaseUrl"] ?? "http://localhost:1337/api";
            
            using var httpClient = new HttpClient();
            var baseUri = baseUrl.TrimEnd('/');
            if (!baseUri.EndsWith("/api", StringComparison.OrdinalIgnoreCase))
            {
                baseUri += "/api";
            }
            httpClient.BaseAddress = new Uri(baseUri + "/");
            
            if (!string.IsNullOrEmpty(writeApiKey))
            {
                httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", writeApiKey);
            }

            var response = await httpClient.PutAsync($"products/{product.DocumentId}", content);
            
            if (response.IsSuccessStatusCode)
            {
                _cache.Remove($"product_{fipsId}");
                _cache.Remove("products_list_all");
                _logger.LogInformation("Successfully updated product Phase for {FipsId}", fipsId);
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to update product Phase for {FipsId}. Status: {StatusCode}, Error: {Error}", 
                    fipsId, response.StatusCode, errorContent);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product Phase for {FipsId}", fipsId);
            return false;
        }
    }

    public async Task<bool> UpdateProductBusinessAreaAsync(string fipsId, int businessAreaCategoryValueId)
    {
        try
        {
            var product = await GetProductByFipsIdAsync(fipsId);
            if (product == null || string.IsNullOrEmpty(product.DocumentId))
            {
                _logger.LogError("Product {FipsId} not found or missing documentId", fipsId);
                return false;
            }

            // Get all current category values
            var currentCategoryValues = new List<int>();
            if (product.CategoryValues != null)
            {
                foreach (var cv in product.CategoryValues)
                {
                    // Skip the current Business Area value (if any) and add all others
                    var categoryType = cv.CategoryType?.Name;
                    if (categoryType != null && !categoryType.Equals("Business area", StringComparison.OrdinalIgnoreCase))
                    {
                        currentCategoryValues.Add(cv.Id);
                    }
                }
            }

            // Add the new Business Area value
            currentCategoryValues.Add(businessAreaCategoryValueId);

            var updateData = new
            {
                data = new
                {
                    fips_id = product.FipsId,
                    category_values = currentCategoryValues
                }
            };

            var json = JsonSerializer.Serialize(updateData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var writeApiKey = _configuration["CmsApi:WriteApiKey"];
            var baseUrl = _configuration["CmsApi:BaseUrl"] ?? "http://localhost:1337/api";
            
            using var httpClient = new HttpClient();
            var baseUri = baseUrl.TrimEnd('/');
            if (!baseUri.EndsWith("/api", StringComparison.OrdinalIgnoreCase))
            {
                baseUri += "/api";
            }
            httpClient.BaseAddress = new Uri(baseUri + "/");
            
            if (!string.IsNullOrEmpty(writeApiKey))
            {
                httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", writeApiKey);
            }

            var response = await httpClient.PutAsync($"products/{product.DocumentId}", content);
            
            if (response.IsSuccessStatusCode)
            {
                _cache.Remove($"product_{fipsId}");
                _cache.Remove("products_list_all");
                _logger.LogInformation("Successfully updated product Business Area for {FipsId}", fipsId);
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to update product Business Area for {FipsId}. Status: {StatusCode}, Error: {Error}", 
                    fipsId, response.StatusCode, errorContent);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product Business Area for {FipsId}", fipsId);
            return false;
        }
    }
}