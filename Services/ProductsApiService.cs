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
    private static readonly string[] UserGroupCategoryTypeNames = new[]
    {
        "User group", "User Group", "User groups", "User Groups",
        "User Type", "User Types", "Audience", "Target Audience"
    };

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

    private static bool IsUserGroupCategory(string? categoryTypeName) =>
        !string.IsNullOrWhiteSpace(categoryTypeName) &&
        UserGroupCategoryTypeNames.Any(name => 
            name.Equals(categoryTypeName, StringComparison.OrdinalIgnoreCase));

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
            var allProducts = new List<ProductDto>();
            var currentPage = 1;
            var pageSize = 100; // Use 100 per page for better performance
            var hasMorePages = true;
            int? totalCount = null;

            while (hasMorePages)
            {
                var queryParams = new List<string>
                {
                    "sort=title:asc",
                    "filters[state][$eq]=Active",
                    $"pagination[page]={currentPage}",
                    $"pagination[pageSize]={pageSize}",
                    "fields[0]=id",
                    "fields[1]=title",
                    "fields[2]=fips_id",
                    "fields[3]=product_url",
                    "populate[category_values][fields][0]=name",
                    "populate[category_values][populate][category_type][fields][0]=name",
                    "populate[product_contacts]=*",
                    "populate[product_contacts][populate][users_permissions_user][fields][0]=email",
                    "populate[product_contacts][populate][users_permissions_user][fields][1]=username"
                };

                var queryString = string.Join("&", queryParams);
                var url = $"products?{queryString}";

                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Failed to fetch products from CMS (page {currentPage}). Status: {response.StatusCode}, Error: {errorContent}");
                    break;
                }

                var content = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<ApiCollectionResponse<ProductDto>>(content, _jsonOptions);
                
                if (apiResponse?.Data != null && apiResponse.Data.Any())
                {
                    allProducts.AddRange(apiResponse.Data);
                    
                    // Store total count from first page
                    if (!totalCount.HasValue && apiResponse.Meta?.Pagination != null)
                    {
                        totalCount = apiResponse.Meta.Pagination.Total;
                        _logger.LogInformation("Fetching {Total} Active products from CMS across {PageCount} pages", 
                            totalCount, apiResponse.Meta.Pagination.PageCount);
                    }
                    
                    // Check if there are more pages
                    if (apiResponse.Meta?.Pagination != null)
                    {
                        hasMorePages = currentPage < apiResponse.Meta.Pagination.PageCount;
                        currentPage++;
                    }
                    else
                    {
                        hasMorePages = false;
                    }
                }
                else
                {
                    hasMorePages = false;
                }
            }
            
            _logger.LogInformation("Successfully fetched {Count} Active products from CMS (Total available: {Total})", 
                allProducts.Count, totalCount ?? allProducts.Count);

            // Extract phase from category_values
            foreach (var product in allProducts)
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
                var filteredProducts = allProducts.Where(p => 
                    p.ProductContacts != null && 
                    p.ProductContacts.Any(pc => 
                        pc.UsersPermissionsUser != null &&
                        !string.IsNullOrEmpty(pc.UsersPermissionsUser.Email) && 
                        pc.UsersPermissionsUser.Email.Equals(userEmail, StringComparison.OrdinalIgnoreCase)
                    )
                ).ToList();
                
                _logger.LogInformation("Filtered to {Count} products for user {Email}", filteredProducts.Count, userEmail);
                allProducts = filteredProducts;
            }

            // Cache for 5 minutes
            _cache.Set(cacheKey, allProducts, TimeSpan.FromMinutes(5));

            return allProducts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching products from CMS");
            return new List<ProductDto>();
        }
    }

    public async Task<List<ProductDto>> GetAllProductsAsync(string? userEmail = null)
    {
        var cacheKey = string.IsNullOrEmpty(userEmail) 
            ? "products_list_all_states" 
            : $"products_list_all_states_{userEmail}";
        
        if (_cache.TryGetValue(cacheKey, out List<ProductDto>? cachedProducts))
        {
            return cachedProducts ?? new List<ProductDto>();
        }

        try
        {
            var allProducts = new List<ProductDto>();
            var currentPage = 1;
            var pageSize = 100; // Use 100 per page for better performance
            var hasMorePages = true;
            int? totalCount = null;

            while (hasMorePages)
            {
                var queryParams = new List<string>
                {
                    "sort=title:asc",
                    // No state filter - get all products regardless of state
                    $"pagination[page]={currentPage}",
                    $"pagination[pageSize]={pageSize}",
                    "fields[0]=id",
                    "fields[1]=title",
                    "fields[2]=fips_id",
                    "fields[3]=product_url",
                    "fields[4]=state",
                    "populate[category_values][fields][0]=name",
                    "populate[category_values][populate][category_type][fields][0]=name",
                    "populate[product_contacts]=*",
                    "populate[product_contacts][populate][users_permissions_user][fields][0]=email",
                    "populate[product_contacts][populate][users_permissions_user][fields][1]=username"
                };

                var queryString = string.Join("&", queryParams);
                var url = $"products?{queryString}";

                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Failed to fetch all products from CMS (page {currentPage}). Status: {response.StatusCode}, Error: {errorContent}");
                    break;
                }

                var content = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<ApiCollectionResponse<ProductDto>>(content, _jsonOptions);
                
                if (apiResponse?.Data != null && apiResponse.Data.Any())
                {
                    allProducts.AddRange(apiResponse.Data);
                    
                    // Store total count from first page
                    if (!totalCount.HasValue && apiResponse.Meta?.Pagination != null)
                    {
                        totalCount = apiResponse.Meta.Pagination.Total;
                        _logger.LogInformation("Fetching {Total} products (all states) from CMS across {PageCount} pages", 
                            totalCount, apiResponse.Meta.Pagination.PageCount);
                    }
                    
                    // Check if there are more pages
                    if (apiResponse.Meta?.Pagination != null)
                    {
                        hasMorePages = currentPage < apiResponse.Meta.Pagination.PageCount;
                        currentPage++;
                    }
                    else
                    {
                        hasMorePages = false;
                    }
                }
                else
                {
                    hasMorePages = false;
                }
            }
            
            _logger.LogInformation("Successfully fetched {Count} products (all states) from CMS (Total available: {Total})", 
                allProducts.Count, totalCount ?? allProducts.Count);

            // Extract phase from category_values
            foreach (var product in allProducts)
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
                var filteredProducts = allProducts.Where(p => 
                    p.ProductContacts != null && 
                    p.ProductContacts.Any(pc => 
                        pc.UsersPermissionsUser != null &&
                        !string.IsNullOrEmpty(pc.UsersPermissionsUser.Email) && 
                        pc.UsersPermissionsUser.Email.Equals(userEmail, StringComparison.OrdinalIgnoreCase)
                    )
                ).ToList();
                
                _logger.LogInformation("Filtered to {Count} products (all states) for user {Email}", filteredProducts.Count, userEmail);
                allProducts = filteredProducts;
            }

            // Cache for 5 minutes
            _cache.Set(cacheKey, allProducts, TimeSpan.FromMinutes(5));

            return allProducts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching all products from CMS");
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

    public async Task<List<string>> GetTypesAsync()
    {
        var cacheKey = "AllTypes";
        if (_cache.TryGetValue(cacheKey, out List<string>? types))
        {
            if (types != null) return types;
        }

        try
        {
            // Fetch all category values where category_type.name = "Type"
            var queryParams = new List<string>
            {
                "filters[category_type][name][$eq]=Type",
                "fields[0]=name",
                "fields[1]=sort_order",
                "sort[0]=sort_order:asc",
                "pagination[pageSize]=100"
            };

            var queryString = string.Join("&", queryParams);
            var url = $"category-values?{queryString}";

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Failed to fetch Type category values from CMS. Status: {response.StatusCode}");
                return new List<string>();
            }

            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiCollectionResponse<CategoryValueDto>>(content, _jsonOptions);

            var typeList = apiResponse?.Data?
                .Where(cv => !string.IsNullOrEmpty(cv.Name))
                .Select(cv => cv.Name!)
                .ToList() ?? new List<string>();

            _cache.Set(cacheKey, typeList, TimeSpan.FromHours(1));
            return typeList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Type category values from CMS");
            return new List<string>();
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

    public async Task<List<CategoryValueDto>> GetUserGroupCategoryValuesAsync()
    {
        const string cacheKey = "UserGroupCategoryValues";
        if (_cache.TryGetValue(cacheKey, out List<CategoryValueDto>? cachedValues))
        {
            if (cachedValues != null) return cachedValues;
        }

        try
        {
            var queryParams = new List<string>
            {
                "fields[0]=id",
                "fields[1]=name",
                "fields[2]=sort_order",
                "sort=name:asc",
                "populate[category_type][fields][0]=name",
                "pagination[pageSize]=500"
            };

            for (var i = 0; i < UserGroupCategoryTypeNames.Length; i++)
            {
                queryParams.Add($"filters[category_type][name][$in][{i}]={Uri.EscapeDataString(UserGroupCategoryTypeNames[i])}");
            }

            var queryString = string.Join("&", queryParams);
            var url = $"category-values?{queryString}";

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to fetch user group category values from CMS. Status: {StatusCode}", response.StatusCode);
                return new List<CategoryValueDto>();
            }

            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiCollectionResponse<CategoryValueDto>>(content, _jsonOptions);

            var values = apiResponse?.Data?
                .Where(cv => IsUserGroupCategory(cv.CategoryType?.Name))
                .OrderBy(cv => cv.SortOrder ?? int.MaxValue)
                .ThenBy(cv => cv.Name)
                .ToList() ?? new List<CategoryValueDto>();

            _cache.Set(cacheKey, values, TimeSpan.FromHours(1));
            return values;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching user group category values from CMS");
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

            // Count existing business areas for logging
            var existingBusinessAreas = product.CategoryValues?
                .Where(cv => cv.CategoryType?.Name?.Equals("Business area", StringComparison.OrdinalIgnoreCase) == true)
                .Select(cv => cv.Name)
                .ToList() ?? new List<string>();
            
            if (existingBusinessAreas.Any())
            {
                _logger.LogInformation("Removing {Count} existing business area(s) from product {FipsId}: {BusinessAreas}", 
                    existingBusinessAreas.Count, fipsId, string.Join(", ", existingBusinessAreas));
            }

            // Get all current category values EXCEPT Business Area (remove all existing business areas)
            var currentCategoryValues = new List<int>();
            if (product.CategoryValues != null)
            {
                foreach (var cv in product.CategoryValues)
                {
                    // Only keep non-Business Area category values (Phase, User Groups, etc.)
                    var categoryType = cv.CategoryType?.Name;
                    if (categoryType != null && !categoryType.Equals("Business area", StringComparison.OrdinalIgnoreCase))
                    {
                        currentCategoryValues.Add(cv.Id);
                    }
                }
            }

            // Add the new Business Area value (ensuring only ONE business area is assigned)
            currentCategoryValues.Add(businessAreaCategoryValueId);
            
            _logger.LogInformation("Assigning business area ID {BusinessAreaId} to product {FipsId}", 
                businessAreaCategoryValueId, fipsId);

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
                // Clear all relevant caches
                _cache.Remove($"product_{fipsId}");
                _cache.Remove("products_list_all");
                _cache.Remove("products_list_all_states");
                
                _logger.LogInformation("Successfully updated Business Area for product {FipsId}. Removed {RemovedCount} old business area(s), assigned 1 new business area.", 
                    fipsId, existingBusinessAreas.Count);
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

    public async Task<bool> UpdateProductUserGroupsAsync(string fipsId, IEnumerable<int> userGroupCategoryValueIds)
    {
        try
        {
            var product = await GetProductByFipsIdAsync(fipsId);
            if (product == null || string.IsNullOrEmpty(product.DocumentId))
            {
                _logger.LogError("Product {FipsId} not found or missing documentId", fipsId);
                return false;
            }

            var newUserGroupIds = userGroupCategoryValueIds?
                .Where(id => id > 0)
                .Distinct()
                .ToList() ?? new List<int>();

            _logger.LogInformation("Updating {Count} user group(s) for product {FipsId}", newUserGroupIds.Count, fipsId);

            var currentCategoryValues = new List<int>();
            if (product.CategoryValues != null)
            {
                foreach (var cv in product.CategoryValues)
                {
                    var categoryType = cv.CategoryType?.Name;
                    if (!IsUserGroupCategory(categoryType))
                    {
                        currentCategoryValues.Add(cv.Id);
                    }
                }
            }

            currentCategoryValues.AddRange(newUserGroupIds);

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
                _cache.Remove("products_list_all_states");
                _logger.LogInformation("Successfully updated user groups for product {FipsId}", fipsId);
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to update user groups for {FipsId}. Status: {StatusCode}, Error: {Error}",
                    fipsId, response.StatusCode, errorContent);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user groups for {FipsId}", fipsId);
            return false;
        }
    }

    public async Task<bool> UpdateProductStateAsync(string fipsId, string state)
    {
        try
        {
            var product = await GetProductByFipsIdAsync(fipsId);
            if (product == null || string.IsNullOrEmpty(product.DocumentId))
            {
                _logger.LogError("Product {FipsId} not found or missing documentId", fipsId);
                return false;
            }

            var updateData = new
            {
                data = new
                {
                    fips_id = product.FipsId,
                    state = state
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
                _logger.LogInformation("Successfully updated product state to {State} for {FipsId}", state, fipsId);
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to update product state for {FipsId}. Status: {StatusCode}, Error: {Error}", 
                    fipsId, response.StatusCode, errorContent);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product state for {FipsId}", fipsId);
            return false;
        }
    }

    public async Task<ProductDto?> CreateProductAsync(string title, string? shortDescription, string? longDescription, List<int> categoryValueIds, string state = "Active")
    {
        try
        {
            var createData = new
            {
                data = new
                {
                    title = title,
                    short_description = shortDescription,
                    long_description = longDescription,
                    category_values = categoryValueIds,
                    state = state
                    // Note: publishedAt is intentionally not set, keeping it as draft
                }
            };

            var json = JsonSerializer.Serialize(createData);
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

            var response = await httpClient.PostAsync("products", content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<ApiResponse<ProductDto>>(responseContent, _jsonOptions);
                
                // Clear caches
                _cache.Remove("products_list_all");
                _cache.Remove("products_list_all_states");
                
                _logger.LogInformation("Successfully created product: {Title}", title);
                return apiResponse?.Data;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to create product. Status: {StatusCode}, Error: {Error}", 
                    response.StatusCode, errorContent);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product: {Title}", title);
            return null;
        }
    }

    public async Task<List<ProductDto>> SearchProductsByTitleAsync(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return new List<ProductDto>();
        }

        try
        {
            var queryParams = new List<string>
            {
                $"filters[title][$containsi]={Uri.EscapeDataString(searchTerm)}",
                "fields[0]=id",
                "fields[1]=title",
                "fields[2]=fips_id",
                "fields[3]=state",
                "populate[category_values][fields][0]=name",
                "populate[category_values][populate][category_type][fields][0]=name",
                "pagination[pageSize]=25"
            };

            var queryString = string.Join("&", queryParams);
            var url = $"products?{queryString}";

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Failed to search products. Status: {response.StatusCode}");
                return new List<ProductDto>();
            }

            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiCollectionResponse<ProductDto>>(content, _jsonOptions);

            var products = apiResponse?.Data ?? new List<ProductDto>();
            
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

            return products;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching products by title: {SearchTerm}", searchTerm);
            return new List<ProductDto>();
        }
    }
}