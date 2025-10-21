using System.Text.Json;
using Compass.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Compass.Services;

public class ProductsApiService : IProductsApiService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ProductsApiService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public ProductsApiService(HttpClient httpClient, IMemoryCache cache, ILogger<ProductsApiService> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
        
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
            var queryParams = new[]
            {
                "filters[fips_id][$eq]=" + fipsId,
                "fields[0]=id",
                "fields[1]=title",
                "fields[2]=fips_id",
                "populate[category_values][fields][0]=name",
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
}