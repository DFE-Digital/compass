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

    private async Task<(string? DocumentId, string? FipsId)> GetProductDocumentRefByFipsIdAsync(string fipsId)
    {
        if (string.IsNullOrWhiteSpace(fipsId))
        {
            return (null, null);
        }

        try
        {
            var queryParams = new[]
            {
                "filters[fips_id][$eq]=" + Uri.EscapeDataString(fipsId),
                "fields[0]=id",
                "fields[1]=documentId",
                "fields[2]=fips_id",
                "pagination[page]=1",
                "pagination[pageSize]=1"
            };

            var url = "products?" + string.Join("&", queryParams);
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to fetch lightweight product reference for {FipsId}. Status: {StatusCode}, Error: {Error}",
                    fipsId, response.StatusCode, errorContent);
                return (null, null);
            }

            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiCollectionResponse<ProductDto>>(content, _jsonOptions);
            var product = apiResponse?.Data?.FirstOrDefault();

            return (product?.DocumentId, product?.FipsId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching lightweight product reference for {FipsId}", fipsId);
            return (null, null);
        }
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
                    "fields[4]=publishedAt",
                    "populate[category_values][fields][0]=name",
                    "populate[category_values][populate][category_type][fields][0]=name",
                    "populate[product_contacts][fields][0]=id",
                    "populate[product_contacts][fields][1]=role",
                    "populate[product_contacts][populate][users_permissions_user][fields][0]=id",
                    "populate[product_contacts][populate][users_permissions_user][fields][1]=display_name",
                    "populate[product_contacts][populate][users_permissions_user][fields][2]=username",
                    "populate[service_owner][fields][0]=emailAddress",
                    "populate[service_owner][fields][1]=displayName",
                    "populate[product_manager][fields][0]=id",
                    "populate[product_manager][fields][1]=emailAddress",
                    "populate[product_manager][fields][2]=displayName",
                    "populate[delivery_manager][fields][0]=id",
                    "populate[delivery_manager][fields][1]=emailAddress",
                    "populate[delivery_manager][fields][2]=displayName",
                    "populate[Information_asset_owner][fields][0]=id",
                    "populate[Information_asset_owner][fields][1]=emailAddress",
                    "populate[Information_asset_owner][fields][2]=displayName",
                    "populate[reporting_user][fields][0]=id",
                    "populate[reporting_user][fields][1]=emailAddress",
                    "populate[reporting_user][fields][2]=displayName",
                    "populate[senior_responsible_officer][fields][0]=id",
                    "populate[senior_responsible_officer][fields][1]=emailAddress",
                    "populate[senior_responsible_officer][fields][2]=displayName",
                    "populate[service_designs][fields][0]=id",
                    "populate[service_designs][fields][1]=emailAddress",
                    "populate[service_designs][fields][2]=displayName",
                    "populate[user_researchers][fields][0]=id",
                    "populate[user_researchers][fields][1]=emailAddress",
                    "populate[user_researchers][fields][2]=displayName"
                };

                // If userEmail is provided, filter products by product contact's user email at the CMS level
                if (!string.IsNullOrEmpty(userEmail))
                {
                    queryParams.Add($"filters[product_contacts][users_permissions_user][email][$eq]={Uri.EscapeDataString(userEmail)}");
                }

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
            
            _logger.LogInformation("Successfully fetched {Count} Active products from CMS (Total available: {Total}){UserFilter}", 
                allProducts.Count, totalCount ?? allProducts.Count, 
                !string.IsNullOrEmpty(userEmail) ? $" for user {userEmail}" : "");

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

            // Cache for 1 minute (reduced from 5 minutes to ensure contact/role updates appear quickly)
            _cache.Set(cacheKey, allProducts, TimeSpan.FromMinutes(1));

            return allProducts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching products from CMS");
            return new List<ProductDto>();
        }
    }

    public async Task<List<ProductDto>> GetProductsByServiceOwnerAsync(string? userEmail)
    {
        if (string.IsNullOrEmpty(userEmail))
        {
            return new List<ProductDto>();
        }

        var cacheKey = $"products_by_service_owner_{userEmail}";
        
        if (_cache.TryGetValue(cacheKey, out List<ProductDto>? cachedProducts))
        {
            return cachedProducts ?? new List<ProductDto>();
        }

        try
        {
            var allProducts = new List<ProductDto>();
            var currentPage = 1;
            var pageSize = 100;
            var hasMorePages = true;
            int? totalCount = null;

            while (hasMorePages)
            {
                var queryParams = new List<string>
                {
                    "sort=title:asc",
                    "filters[state][$eq]=Active",
                    $"filters[service_owner][emailAddress][$eqi]={Uri.EscapeDataString(userEmail)}",
                    $"pagination[page]={currentPage}",
                    $"pagination[pageSize]={pageSize}",
                    "fields[0]=id",
                    "fields[1]=title",
                    "fields[2]=fips_id",
                    "fields[3]=product_url",
                    "fields[4]=publishedAt",
                    "populate[category_values][fields][0]=name",
                    "populate[category_values][populate][category_type][fields][0]=name",
                    "populate[product_contacts][fields][0]=id",
                    "populate[product_contacts][fields][1]=role",
                    "populate[product_contacts][populate][users_permissions_user][fields][0]=id",
                    "populate[product_contacts][populate][users_permissions_user][fields][1]=display_name",
                    "populate[product_contacts][populate][users_permissions_user][fields][2]=username",
                    "populate[service_owner][fields][0]=emailAddress",
                    "populate[service_owner][fields][1]=displayName",
                    "populate[product_manager][fields][0]=id",
                    "populate[product_manager][fields][1]=emailAddress",
                    "populate[product_manager][fields][2]=displayName",
                    "populate[delivery_manager][fields][0]=id",
                    "populate[delivery_manager][fields][1]=emailAddress",
                    "populate[delivery_manager][fields][2]=displayName",
                    "populate[Information_asset_owner][fields][0]=id",
                    "populate[Information_asset_owner][fields][1]=emailAddress",
                    "populate[Information_asset_owner][fields][2]=displayName",
                    "populate[reporting_user][fields][0]=id",
                    "populate[reporting_user][fields][1]=emailAddress",
                    "populate[reporting_user][fields][2]=displayName",
                    "populate[senior_responsible_officer][fields][0]=id",
                    "populate[senior_responsible_officer][fields][1]=emailAddress",
                    "populate[senior_responsible_officer][fields][2]=displayName",
                    "populate[service_designs][fields][0]=id",
                    "populate[service_designs][fields][1]=emailAddress",
                    "populate[service_designs][fields][2]=displayName",
                    "populate[user_researchers][fields][0]=id",
                    "populate[user_researchers][fields][1]=emailAddress",
                    "populate[user_researchers][fields][2]=displayName"
                };

                var queryString = string.Join("&", queryParams);
                var url = $"products?{queryString}";

                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Failed to fetch products by service owner from CMS (page {currentPage}). Status: {response.StatusCode}, Error: {errorContent}");
                    break;
                }

                var content = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<ApiCollectionResponse<ProductDto>>(content, _jsonOptions);
                
                if (apiResponse?.Data != null && apiResponse.Data.Any())
                {
                    allProducts.AddRange(apiResponse.Data);
                    
                    if (!totalCount.HasValue && apiResponse.Meta?.Pagination != null)
                    {
                        totalCount = apiResponse.Meta.Pagination.Total;
                        _logger.LogInformation("Fetching {Total} Active products by service owner from CMS across {PageCount} pages", 
                            totalCount, apiResponse.Meta.Pagination.PageCount);
                    }
                    
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
            
            _logger.LogInformation("Successfully fetched {Count} Active products by service owner from CMS (Total available: {Total}) for user {UserEmail}", 
                allProducts.Count, totalCount ?? allProducts.Count, userEmail);

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

            // Cache for 1 minute (reduced from 5 minutes to ensure role updates appear quickly)
            _cache.Set(cacheKey, allProducts, TimeSpan.FromMinutes(1));

            return allProducts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching products by service owner from CMS for user {UserEmail}", userEmail);
            return new List<ProductDto>();
        }
    }

    public async Task<List<ProductDto>> GetProductsByProductManagerAsync(string? userEmail)
    {
        if (string.IsNullOrEmpty(userEmail))
        {
            return new List<ProductDto>();
        }

        var cacheKey = $"products_by_product_manager_{userEmail}";
        
        if (_cache.TryGetValue(cacheKey, out List<ProductDto>? cachedProducts))
        {
            return cachedProducts ?? new List<ProductDto>();
        }

        try
        {
            var allProducts = new List<ProductDto>();
            var currentPage = 1;
            var pageSize = 100;
            var hasMorePages = true;
            int? totalCount = null;

            while (hasMorePages)
            {
                var queryParams = new List<string>
                {
                    "sort=title:asc",
                    "filters[state][$eq]=Active",
                    $"filters[product_manager][emailAddress][$eqi]={Uri.EscapeDataString(userEmail)}",
                    $"pagination[page]={currentPage}",
                    $"pagination[pageSize]={pageSize}",
                    "fields[0]=id",
                    "fields[1]=title",
                    "fields[2]=fips_id",
                    "fields[3]=product_url",
                    "fields[4]=publishedAt",
                    "populate[category_values][fields][0]=name",
                    "populate[category_values][populate][category_type][fields][0]=name",
                    "populate[product_contacts][fields][0]=id",
                    "populate[product_contacts][fields][1]=role",
                    "populate[product_contacts][populate][users_permissions_user][fields][0]=id",
                    "populate[product_contacts][populate][users_permissions_user][fields][1]=display_name",
                    "populate[product_contacts][populate][users_permissions_user][fields][2]=username",
                    "populate[service_owner][fields][0]=emailAddress",
                    "populate[service_owner][fields][1]=displayName",
                    "populate[product_manager][fields][0]=id",
                    "populate[product_manager][fields][1]=emailAddress",
                    "populate[product_manager][fields][2]=displayName",
                    "populate[delivery_manager][fields][0]=id",
                    "populate[delivery_manager][fields][1]=emailAddress",
                    "populate[delivery_manager][fields][2]=displayName",
                    "populate[Information_asset_owner][fields][0]=id",
                    "populate[Information_asset_owner][fields][1]=emailAddress",
                    "populate[Information_asset_owner][fields][2]=displayName",
                    "populate[reporting_user][fields][0]=id",
                    "populate[reporting_user][fields][1]=emailAddress",
                    "populate[reporting_user][fields][2]=displayName",
                    "populate[senior_responsible_officer][fields][0]=id",
                    "populate[senior_responsible_officer][fields][1]=emailAddress",
                    "populate[senior_responsible_officer][fields][2]=displayName",
                    "populate[service_designs][fields][0]=id",
                    "populate[service_designs][fields][1]=emailAddress",
                    "populate[service_designs][fields][2]=displayName",
                    "populate[user_researchers][fields][0]=id",
                    "populate[user_researchers][fields][1]=emailAddress",
                    "populate[user_researchers][fields][2]=displayName"
                };

                var queryString = string.Join("&", queryParams);
                var url = $"products?{queryString}";

                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Failed to fetch products by product manager from CMS (page {currentPage}). Status: {response.StatusCode}, Error: {errorContent}");
                    break;
                }

                var content = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<ApiCollectionResponse<ProductDto>>(content, _jsonOptions);
                
                if (apiResponse?.Data != null && apiResponse.Data.Any())
                {
                    allProducts.AddRange(apiResponse.Data);
                    
                    if (!totalCount.HasValue && apiResponse.Meta?.Pagination != null)
                    {
                        totalCount = apiResponse.Meta.Pagination.Total;
                        _logger.LogInformation("Fetching {Total} Active products by product manager from CMS across {PageCount} pages", 
                            totalCount, apiResponse.Meta.Pagination.PageCount);
                    }
                    
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
            
            _logger.LogInformation("Successfully fetched {Count} Active products by product manager from CMS (Total available: {Total}) for user {UserEmail}", 
                allProducts.Count, totalCount ?? allProducts.Count, userEmail);

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

            // Cache for 1 minute (reduced from 5 minutes to ensure role updates appear quickly)
            _cache.Set(cacheKey, allProducts, TimeSpan.FromMinutes(1));

            return allProducts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching products by product manager from CMS for user {UserEmail}", userEmail);
            return new List<ProductDto>();
        }
    }

    public async Task<List<ProductDto>> GetProductsByReportingUserAsync(string? userEmail)
    {
        if (string.IsNullOrEmpty(userEmail))
        {
            return new List<ProductDto>();
        }

        var cacheKey = $"products_by_reporting_user_{userEmail}";
        
        if (_cache.TryGetValue(cacheKey, out List<ProductDto>? cachedProducts))
        {
            return cachedProducts ?? new List<ProductDto>();
        }

        try
        {
            var allProducts = new List<ProductDto>();
            var currentPage = 1;
            var pageSize = 100;
            var hasMorePages = true;
            int? totalCount = null;

            while (hasMorePages)
            {
                var queryParams = new List<string>
                {
                    "sort=title:asc",
                    "filters[state][$eq]=Active",
                    $"filters[reporting_user][emailAddress][$eqi]={Uri.EscapeDataString(userEmail)}",
                    $"pagination[page]={currentPage}",
                    $"pagination[pageSize]={pageSize}",
                    "fields[0]=id",
                    "fields[1]=title",
                    "fields[2]=fips_id",
                    "fields[3]=product_url",
                    "fields[4]=publishedAt",
                    "populate[category_values][fields][0]=name",
                    "populate[category_values][populate][category_type][fields][0]=name",
                    "populate[product_contacts][fields][0]=id",
                    "populate[product_contacts][fields][1]=role",
                    "populate[product_contacts][populate][users_permissions_user][fields][0]=id",
                    "populate[product_contacts][populate][users_permissions_user][fields][1]=display_name",
                    "populate[product_contacts][populate][users_permissions_user][fields][2]=username",
                    "populate[service_owner][fields][0]=emailAddress",
                    "populate[service_owner][fields][1]=displayName",
                    "populate[product_manager][fields][0]=id",
                    "populate[product_manager][fields][1]=emailAddress",
                    "populate[product_manager][fields][2]=displayName",
                    "populate[delivery_manager][fields][0]=id",
                    "populate[delivery_manager][fields][1]=emailAddress",
                    "populate[delivery_manager][fields][2]=displayName",
                    "populate[Information_asset_owner][fields][0]=id",
                    "populate[Information_asset_owner][fields][1]=emailAddress",
                    "populate[Information_asset_owner][fields][2]=displayName",
                    "populate[reporting_user][fields][0]=id",
                    "populate[reporting_user][fields][1]=emailAddress",
                    "populate[reporting_user][fields][2]=displayName",
                    "populate[senior_responsible_officer][fields][0]=id",
                    "populate[senior_responsible_officer][fields][1]=emailAddress",
                    "populate[senior_responsible_officer][fields][2]=displayName",
                    "populate[service_designs][fields][0]=id",
                    "populate[service_designs][fields][1]=emailAddress",
                    "populate[service_designs][fields][2]=displayName",
                    "populate[user_researchers][fields][0]=id",
                    "populate[user_researchers][fields][1]=emailAddress",
                    "populate[user_researchers][fields][2]=displayName"
                };

                var queryString = string.Join("&", queryParams);
                var url = $"products?{queryString}";

                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Failed to fetch products by reporting user from CMS (page {currentPage}). Status: {response.StatusCode}, Error: {errorContent}");
                    break;
                }

                var content = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<ApiCollectionResponse<ProductDto>>(content, _jsonOptions);
                
                if (apiResponse?.Data != null && apiResponse.Data.Any())
                {
                    allProducts.AddRange(apiResponse.Data);
                    
                    if (!totalCount.HasValue && apiResponse.Meta?.Pagination != null)
                    {
                        totalCount = apiResponse.Meta.Pagination.Total;
                        _logger.LogInformation("Fetching {Total} Active products by reporting user from CMS across {PageCount} pages", 
                            totalCount, apiResponse.Meta.Pagination.PageCount);
                    }
                    
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
            
            _logger.LogInformation("Successfully fetched {Count} Active products by reporting user from CMS (Total available: {Total}) for user {UserEmail}", 
                allProducts.Count, totalCount ?? allProducts.Count, userEmail);

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

            // Cache for 1 minute (reduced from 5 minutes to ensure role updates appear quickly)
            _cache.Set(cacheKey, allProducts, TimeSpan.FromMinutes(1));

            return allProducts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching products by reporting user from CMS for user {UserEmail}", userEmail);
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
                    "fields[1]=documentId",
                    "fields[2]=title",
                    "fields[3]=fips_id",
                    "fields[4]=product_url",
                    "fields[5]=state",
                    "fields[6]=publishedAt",
                    "populate[category_values][fields][0]=name",
                    "populate[category_values][populate][category_type][fields][0]=name",
                    "populate[product_contacts][fields][0]=id",
                    "populate[product_contacts][fields][1]=role",
                    "populate[product_contacts][populate][users_permissions_user][fields][0]=id",
                    "populate[product_contacts][populate][users_permissions_user][fields][1]=display_name",
                    "populate[product_contacts][populate][users_permissions_user][fields][2]=username",
                    "populate[service_owner][fields][0]=emailAddress",
                    "populate[service_owner][fields][1]=displayName",
                    "populate[product_manager][fields][0]=id",
                    "populate[product_manager][fields][1]=emailAddress",
                    "populate[product_manager][fields][2]=displayName",
                    "populate[delivery_manager][fields][0]=id",
                    "populate[delivery_manager][fields][1]=emailAddress",
                    "populate[delivery_manager][fields][2]=displayName",
                    "populate[Information_asset_owner][fields][0]=id",
                    "populate[Information_asset_owner][fields][1]=emailAddress",
                    "populate[Information_asset_owner][fields][2]=displayName",
                    "populate[reporting_user][fields][0]=id",
                    "populate[reporting_user][fields][1]=emailAddress",
                    "populate[reporting_user][fields][2]=displayName",
                    "populate[senior_responsible_officer][fields][0]=id",
                    "populate[senior_responsible_officer][fields][1]=emailAddress",
                    "populate[senior_responsible_officer][fields][2]=displayName",
                    "populate[service_designs][fields][0]=id",
                    "populate[service_designs][fields][1]=emailAddress",
                    "populate[service_designs][fields][2]=displayName",
                    "populate[user_researchers][fields][0]=id",
                    "populate[user_researchers][fields][1]=emailAddress",
                    "populate[user_researchers][fields][2]=displayName"
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

            // Cache for 1 minute (reduced from 5 minutes to ensure contact/role updates appear quickly)
            _cache.Set(cacheKey, allProducts, TimeSpan.FromMinutes(1));

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
        
        // Clear cache to ensure we get fresh data with all fields
        _cache.Remove(cacheKey);
        
        // Don't use cache for detailed product loads (to ensure we get all fields)
        // if (_cache.TryGetValue(cacheKey, out ProductDto? cachedProduct))
        // {
        //     return cachedProduct;
        // }

        try
        {
            // Explicitly request all fields we need for editing
            var queryParams = new[]
            {
                "filters[fips_id][$eq]=" + fipsId,
                "fields[0]=id",
                "fields[1]=documentId",
                "fields[2]=title",
                "fields[3]=publishedAt",
                "fields[3]=fips_id",
                "fields[4]=product_url",
                "fields[5]=short_description",
                "fields[6]=long_description",
                "fields[7]=cmdb_sys_id",
                "fields[8]=state",
                "populate[category_values][fields][0]=id",
                "populate[category_values][fields][1]=name",
                "populate[category_values][populate][category_type][fields][0]=name",
                "populate[service_owner][fields][0]=id",
                "populate[service_owner][fields][1]=emailAddress",
                "populate[service_owner][fields][2]=displayName",
                "populate[product_manager][fields][0]=id",
                "populate[product_manager][fields][1]=emailAddress",
                "populate[product_manager][fields][2]=displayName",
                "populate[delivery_manager][fields][0]=id",
                "populate[delivery_manager][fields][1]=emailAddress",
                "populate[delivery_manager][fields][2]=displayName",
                "populate[Information_asset_owner][fields][0]=id",
                "populate[Information_asset_owner][fields][1]=emailAddress",
                "populate[Information_asset_owner][fields][2]=displayName",
                "populate[senior_responsible_officer][fields][0]=id",
                "populate[senior_responsible_officer][fields][1]=emailAddress",
                "populate[senior_responsible_officer][fields][2]=displayName",
                "populate[service_designs][fields][0]=id",
                "populate[service_designs][fields][1]=emailAddress",
                "populate[service_designs][fields][2]=displayName",
                "populate[user_researchers][fields][0]=id",
                "populate[user_researchers][fields][1]=emailAddress",
                "populate[user_researchers][fields][2]=displayName"
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
                // Log the deserialized values to help debug missing fields
                _logger.LogInformation("Deserialized product fields for {FipsId}: ShortDescription={ShortDescription}, LongDescription={LongDescription}, CmdbSysId={CmdbSysId}",
                    fipsId, 
                    product.ShortDescription ?? "null", 
                    product.LongDescription ?? "null", 
                    product.CmdbSysId ?? "null");
                
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

    public async Task<bool> RemoveDuplicatePhasesAsync(string fipsId, int phaseCategoryValueIdToKeep)
    {
        try
        {
            var product = await GetProductByFipsIdAsync(fipsId);
            if (product == null || string.IsNullOrEmpty(product.DocumentId))
            {
                _logger.LogError("Product {FipsId} not found or missing documentId", fipsId);
                return false;
            }

            // Get all current phase category values
            var existingPhases = product.CategoryValues?
                .Where(cv => cv.CategoryType?.Name?.Equals("Phase", StringComparison.OrdinalIgnoreCase) == true)
                .ToList() ?? new List<CategoryValueDto>();
            
            if (existingPhases.Count <= 1)
            {
                _logger.LogInformation("Product {FipsId} does not have duplicate phases", fipsId);
                return true; // No duplicates to remove
            }

            // Log which phases are being removed
            var phasesToRemove = existingPhases
                .Where(p => p.Id != phaseCategoryValueIdToKeep)
                .Select(p => p.Name ?? $"ID {p.Id}")
                .ToList();
            
            if (phasesToRemove.Any())
            {
                _logger.LogInformation("Removing {Count} duplicate phase(s) from product {FipsId}: {Phases}", 
                    phasesToRemove.Count, fipsId, string.Join(", ", phasesToRemove));
            }

            // Get all current category values EXCEPT Phase (remove all existing phases)
            var currentCategoryValues = new List<int>();
            if (product.CategoryValues != null)
            {
                foreach (var cv in product.CategoryValues)
                {
                    // Only keep non-Phase category values (Business Area, User Groups, etc.)
                    var categoryType = cv.CategoryType?.Name;
                    if (categoryType != null && !categoryType.Equals("Phase", StringComparison.OrdinalIgnoreCase))
                    {
                        currentCategoryValues.Add(cv.Id);
                    }
                }
            }

            // Add the phase to keep (ensuring only ONE phase is assigned)
            currentCategoryValues.Add(phaseCategoryValueIdToKeep);
            
            _logger.LogInformation("Keeping phase ID {PhaseId} for product {FipsId}", 
                phaseCategoryValueIdToKeep, fipsId);

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
                _logger.LogInformation("Successfully removed duplicate phases for product {FipsId}", fipsId);
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to remove duplicate phases for product {FipsId}. Status: {StatusCode}, Error: {Error}", 
                    fipsId, response.StatusCode, errorContent);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing duplicate phases for product {FipsId}", fipsId);
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

    public async Task<List<EntraUserDto>> GetEntraUsersAsync()
    {
        const string cacheKey = "EntraUsers";
        if (_cache.TryGetValue(cacheKey, out List<EntraUserDto>? cachedUsers))
        {
            if (cachedUsers != null) return cachedUsers;
        }

        try
        {
            var allUsers = new List<EntraUserDto>();
            var currentPage = 1;
            var pageSize = 100;
            var hasMorePages = true;

            while (hasMorePages)
            {
                var queryParams = new List<string>
                {
                    $"pagination[page]={currentPage}",
                    $"pagination[pageSize]={pageSize}",
                    "sort=displayName:asc",
                    "fields[0]=id",
                    "fields[1]=documentId",
                    "fields[2]=emailAddress",
                    "fields[3]=entraId",
                    "fields[4]=displayName",
                    "fields[5]=firstName",
                    "fields[6]=lastName"
                };

                var queryString = string.Join("&", queryParams);
                var url = $"entra-users?{queryString}";

                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to fetch entra-users from CMS (page {Page}). Status: {StatusCode}", currentPage, response.StatusCode);
                    break;
                }

                var content = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<ApiCollectionResponse<EntraUserDto>>(content, _jsonOptions);

                if (apiResponse?.Data != null && apiResponse.Data.Any())
                {
                    allUsers.AddRange(apiResponse.Data);

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

            _cache.Set(cacheKey, allUsers, TimeSpan.FromMinutes(10));
            return allUsers;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching entra-users from CMS");
            return new List<EntraUserDto>();
        }
    }

    public async Task<EntraUserDto?> GetOrCreateEntraUserAsync(string emailAddress, string? entraId = null, string? displayName = null, string? firstName = null, string? lastName = null)
    {
        if (string.IsNullOrWhiteSpace(emailAddress))
        {
            _logger.LogError("Cannot get or create entra-user: emailAddress is required");
            return null;
        }

        try
        {
            // First, try to find existing user by email (case-insensitive)
            var queryParams = new List<string>
            {
                $"filters[emailAddress][$eqi]={Uri.EscapeDataString(emailAddress)}",
                "fields[0]=id",
                "fields[1]=documentId",
                "fields[2]=emailAddress",
                "fields[3]=entraId",
                "fields[4]=displayName",
                "fields[5]=firstName",
                "fields[6]=lastName"
            };

            var queryString = string.Join("&", queryParams);
            var url = $"entra-users?{queryString}";

            var response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<ApiCollectionResponse<EntraUserDto>>(content, _jsonOptions);
                var existingUser = apiResponse?.Data?.FirstOrDefault();

                if (existingUser != null)
                {
                    _logger.LogInformation("Found existing entra-user with email {Email}, updating with latest Entra info. Existing: EntraId={ExistingEntraId}, FirstName={ExistingFirstName}, LastName={ExistingLastName}",
                        emailAddress, existingUser.EntraId, existingUser.FirstName, existingUser.LastName);
                    
                    // Update existing user with latest Entra info
                    // Always update with provided values to keep Entra data in sync
                    var finalEntraId = !string.IsNullOrWhiteSpace(entraId) ? entraId : existingUser.EntraId;
                    var finalDisplayName = !string.IsNullOrWhiteSpace(displayName) ? displayName : (existingUser.DisplayName ?? emailAddress);
                    var finalFirstName = !string.IsNullOrWhiteSpace(firstName) ? firstName : existingUser.FirstName;
                    var finalLastName = !string.IsNullOrWhiteSpace(lastName) ? lastName : existingUser.LastName;
                    
                    _logger.LogInformation("Updating entra-user with: EntraId={EntraId}, DisplayName={DisplayName}, FirstName={FirstName}, LastName={LastName}",
                        finalEntraId, finalDisplayName, finalFirstName, finalLastName);
                    
                    var updateData = new
                    {
                        data = new
                        {
                            emailAddress = emailAddress,
                            entraId = finalEntraId,
                            displayName = finalDisplayName,
                            firstName = finalFirstName,
                            lastName = finalLastName
                        }
                    };

                    // Use JSON options with camelCase naming to match Strapi schema
                    var updateJsonOptions = new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        WriteIndented = false
                    };
                    var updateJson = JsonSerializer.Serialize(updateData, updateJsonOptions);
                    _logger.LogInformation("Update JSON payload: {Json}", updateJson);
                    var updateContent = new StringContent(updateJson, Encoding.UTF8, "application/json");

                    var updateWriteApiKey = _configuration["CmsApi:WriteApiKey"];
                    var updateBaseUrl = _configuration["CmsApi:BaseUrl"] ?? "http://localhost:1337/api";

                    using var updateHttpClient = new HttpClient();
                    var updateBaseUri = updateBaseUrl.TrimEnd('/');
                    if (!updateBaseUri.EndsWith("/api", StringComparison.OrdinalIgnoreCase))
                    {
                        updateBaseUri += "/api";
                    }
                    updateHttpClient.BaseAddress = new Uri(updateBaseUri + "/");

                    if (!string.IsNullOrEmpty(updateWriteApiKey))
                    {
                        updateHttpClient.DefaultRequestHeaders.Authorization =
                            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", updateWriteApiKey);
                    }

                    var updateResponse = await updateHttpClient.PutAsync($"entra-users/{existingUser.DocumentId}", updateContent);

                    if (updateResponse.IsSuccessStatusCode)
                    {
                        var updateResponseContent = await updateResponse.Content.ReadAsStringAsync();
                        var updateApiResponse = JsonSerializer.Deserialize<ApiResponse<EntraUserDto>>(updateResponseContent, _jsonOptions);
                        
                        // Clear cache
                        _cache.Remove("EntraUsers");
                        _cache.Remove($"product_{existingUser.Id}"); // In case we need to clear product cache

                        _logger.LogInformation("Successfully updated entra-user with email {Email}", emailAddress);
                        return updateApiResponse?.Data ?? existingUser;
                    }
                    else
                    {
                        var errorContent = await updateResponse.Content.ReadAsStringAsync();
                        _logger.LogWarning("Failed to update existing entra-user. Status: {StatusCode}, Error: {Error}. Returning existing user.",
                            updateResponse.StatusCode, errorContent);
                        return existingUser;
                    }
                }
            }

            // User doesn't exist, create it
            _logger.LogInformation("Creating new entra-user with email {Email}, EntraId={EntraId}, DisplayName={DisplayName}, FirstName={FirstName}, LastName={LastName}",
                emailAddress, entraId, displayName ?? emailAddress, firstName, lastName);

            var createData = new
            {
                data = new
                {
                    emailAddress = emailAddress,
                    entraId = entraId,
                    displayName = displayName ?? emailAddress,
                    firstName = firstName,
                    lastName = lastName
                    // Note: publishedAt is intentionally not set, keeping it as draft
                }
            };

            // Use JSON options with camelCase naming to match Strapi schema
            var createJsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
            var json = JsonSerializer.Serialize(createData, createJsonOptions);
            _logger.LogInformation("Create JSON payload: {Json}", json);
            var createContent = new StringContent(json, Encoding.UTF8, "application/json");

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

            var createResponse = await httpClient.PostAsync("entra-users", createContent);

            if (createResponse.IsSuccessStatusCode)
            {
                var responseContent = await createResponse.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<ApiResponse<EntraUserDto>>(responseContent, _jsonOptions);

                // Clear cache
                _cache.Remove("EntraUsers");

                _logger.LogInformation("Successfully created entra-user with email {Email}", emailAddress);
                return apiResponse?.Data;
            }
            else
            {
                var errorContent = await createResponse.Content.ReadAsStringAsync();
                _logger.LogError("Failed to create entra-user. Status: {StatusCode}, Error: {Error}",
                    createResponse.StatusCode, errorContent);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting or creating entra-user with email {Email}", emailAddress);
            return null;
        }
    }

    public async Task<bool> UpdateProductServiceOwnerAsync(string fipsId, int entraUserId)
    {
        try
        {
            var productRef = await GetProductDocumentRefByFipsIdAsync(fipsId);
            if (string.IsNullOrEmpty(productRef.DocumentId))
            {
                _logger.LogError("Product {FipsId} not found or missing documentId", fipsId);
                return false;
            }

            // Update product with service_owner relation
            // Since service_owner is oneToMany, we set it as an array with a single ID
            var updateData = new
            {
                data = new
                {
                    fips_id = productRef.FipsId ?? fipsId,
                    service_owner = new[] { entraUserId }
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

            var response = await httpClient.PutAsync($"products/{productRef.DocumentId}", content);

            if (response.IsSuccessStatusCode)
            {
                _cache.Remove($"product_{fipsId}");
                _cache.Remove("products_list_all");
                _cache.Remove("products_list_all_states");
                // Note: User-specific product caches (products_by_service_owner_{email}, etc.) 
                // cannot be cleared by pattern with IMemoryCache. Cache time reduced to 1 minute
                // to ensure updates appear quickly. Consider implementing cache key tracking for future.
                _logger.LogInformation("Successfully updated product service owner for {FipsId}. User-specific product caches will refresh within 1 minute.", fipsId);
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to update product service owner for {FipsId}. Status: {StatusCode}, Error: {Error}",
                    fipsId, response.StatusCode, errorContent);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product service owner for {FipsId}", fipsId);
            return false;
        }
    }

    public async Task<bool> UpdateProductRoleAsync(string fipsId, string roleFieldName, int entraUserId)
    {
        try
        {
            var productRef = await GetProductDocumentRefByFipsIdAsync(fipsId);
            if (string.IsNullOrEmpty(productRef.DocumentId))
            {
                _logger.LogError("Product {FipsId} not found or missing documentId", fipsId);
                return false;
            }

            // Update product with role relation
            // Since roles are oneToMany, we set them as an array with a single ID
            // Use reflection to create an anonymous object with the role field name
            var updateData = new Dictionary<string, object>
            {
                ["fips_id"] = productRef.FipsId ?? fipsId
            };
            updateData[roleFieldName] = new[] { entraUserId };

            // Create anonymous object with data property
            // Note: Dictionary will serialize to JSON object with keys as-is (snake_case for Strapi)
            var dataObject = new { data = updateData };
            var json = JsonSerializer.Serialize(dataObject);
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

            var response = await httpClient.PutAsync($"products/{productRef.DocumentId}", content);

            if (response.IsSuccessStatusCode)
            {
                _cache.Remove($"product_{fipsId}");
                _cache.Remove("products_list_all");
                _cache.Remove("products_list_all_states");
                // Note: User-specific product caches (products_by_service_owner_{email}, etc.) 
                // cannot be cleared by pattern with IMemoryCache. Cache time reduced to 1 minute
                // to ensure updates appear quickly. Consider implementing cache key tracking for future.
                _logger.LogInformation("Successfully updated product role {RoleFieldName} for {FipsId}. User-specific product caches will refresh within 1 minute.", roleFieldName, fipsId);
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to update product role {RoleFieldName} for {FipsId}. Status: {StatusCode}, Error: {Error}",
                    roleFieldName, fipsId, response.StatusCode, errorContent);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product role {RoleFieldName} for {FipsId}", roleFieldName, fipsId);
            return false;
        }
    }

    public async Task<bool> UpdateProductBasicInfoAsync(string fipsId, string? title, string? shortDescription, string? longDescription, string? cmdbSysId)
    {
        try
        {
            var product = await GetProductByFipsIdAsync(fipsId);
            if (product == null || string.IsNullOrEmpty(product.DocumentId))
            {
                _logger.LogError("Product {FipsId} not found or missing documentId", fipsId);
                return false;
            }

            var updateData = new Dictionary<string, object>
            {
                ["fips_id"] = product.FipsId!
            };

            if (title != null)
            {
                updateData["title"] = title.Trim();
            }

            if (shortDescription != null)
            {
                updateData["short_description"] = shortDescription.Trim();
            }

            if (longDescription != null)
            {
                updateData["long_description"] = longDescription.Trim();
            }

            if (cmdbSysId != null)
            {
                updateData["cmdb_sys_id"] = cmdbSysId.Trim();
            }

            var dataObject = new { data = updateData };
            var json = JsonSerializer.Serialize(dataObject);
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
                _logger.LogInformation("Successfully updated product basic info for {FipsId}", fipsId);
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to update product basic info for {FipsId}. Status: {StatusCode}, Error: {Error}", 
                    fipsId, response.StatusCode, errorContent);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product basic info for {FipsId}", fipsId);
            return false;
        }
    }
}