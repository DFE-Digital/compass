using FipsReporting.Models;
using Newtonsoft.Json;

namespace FipsReporting.Services
{
    public class CmsApiService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<CmsApiService> _logger;
        private readonly IConfiguration _configuration;

        public CmsApiService(HttpClient httpClient, ILogger<CmsApiService> logger, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<CmsApiResponse<CmsProduct>> GetProductsAsync(ProductFilter filter)
        {
            try
            {
                var queryParams = new List<string>();

                // Add pagination
                queryParams.Add($"pagination[page]={filter.Page}");
                queryParams.Add($"pagination[pageSize]={filter.PageSize}");

                // Add sorting
                queryParams.Add($"sort={filter.SortBy}:{filter.SortOrder}");

                // Add filters
                if (!string.IsNullOrEmpty(filter.Search))
                {
                    queryParams.Add($"filters[title][$containsi]={Uri.EscapeDataString(filter.Search)}");
                }

                if (!string.IsNullOrEmpty(filter.State))
                {
                    queryParams.Add($"filters[state][$eq]={Uri.EscapeDataString(filter.State)}");
                }

                if (!string.IsNullOrEmpty(filter.CategoryType) && !string.IsNullOrEmpty(filter.CategoryValue))
                {
                    queryParams.Add($"filters[category_values][category_type][name][$eq]={Uri.EscapeDataString(filter.CategoryType)}");
                    queryParams.Add($"filters[category_values][name][$eq]={Uri.EscapeDataString(filter.CategoryValue)}");
                }

                // Add population for related data (excluding description fields to reduce API response size)
                queryParams.Add("populate[0]=category_values");
                queryParams.Add("populate[1]=category_values.category_type");
                queryParams.Add("populate[2]=product_contacts");
                queryParams.Add("populate[3]=product_contacts.users_permissions_user");
                
                // Exclude description fields to reduce response size
                // Note: Commenting out fields restriction as it may be causing 400 errors
                // queryParams.Add("fields[0]=id");
                // queryParams.Add("fields[1]=title");
                // queryParams.Add("fields[2]=fips_id");
                // queryParams.Add("fields[3]=state");
                // queryParams.Add("fields[4]=product_url");
                // queryParams.Add("fields[5]=is_published");
                // queryParams.Add("fields[6]=created_at");
                // queryParams.Add("fields[7]=updated_at");

                var queryString = string.Join("&", queryParams);
                var baseUrl = _configuration["CmsApi:BaseUrl"] ?? "https://fips-cms.azurewebsites.net/api/";
                if (!baseUrl.EndsWith("/"))
                {
                    baseUrl += "/";
                }
                var url = $"{baseUrl}products?{queryString}";

                _logger.LogInformation("Fetching products from CMS API: {Url}", url);

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                var apiKey = _configuration["CmsApi:ReadApiKey"];
                if (!string.IsNullOrEmpty(apiKey))
                {
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
                }

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<CmsApiResponse<CmsProduct>>(content);

                if (result == null)
                {
                    throw new InvalidOperationException("Failed to deserialize CMS API response");
                }

                _logger.LogInformation("Successfully fetched {Count} products from CMS", result.Data.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching products from CMS API");
                throw;
            }
        }

        public async Task<CmsProduct?> GetProductByIdAsync(int id)
        {
            try
            {
                var queryParams = new List<string>
                {
                    "populate[0]=category_values",
                    "populate[1]=category_values.category_type",
                    "populate[2]=product_contacts",
                    "populate[3]=product_contacts.users_permissions_user"
                };

                var queryString = string.Join("&", queryParams);
                var baseUrl = _configuration["CmsApi:BaseUrl"] ?? "https://fips-cms.azurewebsites.net/api/";
                if (!baseUrl.EndsWith("/"))
                {
                    baseUrl += "/";
                }
                var url = $"{baseUrl}products/{id}?{queryString}";

                _logger.LogInformation("Fetching product {Id} from CMS API", id);

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                var apiKey = _configuration["CmsApi:ReadApiKey"];
                if (!string.IsNullOrEmpty(apiKey))
                {
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
                }

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<CmsApiResponse<CmsProduct>>(content);

                if (result == null || !result.Data.Any())
                {
                    return null;
                }

                return result.Data.First();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching product {Id} from CMS API", id);
                throw;
            }
        }

        public async Task<List<CmsCategoryType>> GetCategoryTypesAsync()
        {
            try
            {
                var baseUrl = _configuration["CmsApi:BaseUrl"] ?? "https://fips-cms.azurewebsites.net/api/";
                if (!baseUrl.EndsWith("/"))
                {
                    baseUrl += "/";
                }
                var url = $"{baseUrl}category-types?populate=values&sort=sort_order:asc";

                _logger.LogInformation("Fetching category types from CMS API");

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                var apiKey = _configuration["CmsApi:ReadApiKey"];
                if (!string.IsNullOrEmpty(apiKey))
                {
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
                }

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<CmsApiResponse<CmsCategoryType>>(content);

                if (result == null)
                {
                    throw new InvalidOperationException("Failed to deserialize CMS API response");
                }

                _logger.LogInformation("Successfully fetched {Count} category types from CMS", result.Data.Count);
                return result.Data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching category types from CMS API");
                throw;
            }
        }

        public async Task<List<string>> GetCategoryValuesByTypeAsync(string categoryTypeName)
        {
            try
            {
                var baseUrl = _configuration["CmsApi:BaseUrl"] ?? "https://fips-cms.azurewebsites.net/api/";
                if (!baseUrl.EndsWith("/"))
                {
                    baseUrl += "/";
                }
                
                // Filter category values by category_type name and enabled = true
                var url = $"{baseUrl}category-values?filters[category_type][name][$eq]={Uri.EscapeDataString(categoryTypeName)}&filters[enabled][$eq]=true&populate=category_type&sort=sort_order";

                _logger.LogInformation("Fetching category values for type '{CategoryType}' from CMS API", categoryTypeName);

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                var apiKey = _configuration["CmsApi:ReadApiKey"];
                if (!string.IsNullOrEmpty(apiKey))
                {
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
                }

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<CmsApiResponse<CmsCategoryValue>>(content);

                if (result == null)
                {
                    throw new InvalidOperationException("Failed to deserialize CMS API response");
                }

                var categoryValues = result.Data
                    .Where(cv => !string.IsNullOrEmpty(cv.Name))
                    .Select(cv => cv.Name!)
                    .Distinct()
                    .OrderBy(name => name)
                    .ToList();

                _logger.LogInformation("Successfully fetched {Count} category values for type '{CategoryType}' from CMS", categoryValues.Count, categoryTypeName);
                return categoryValues;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching category values for type '{CategoryType}' from CMS API", categoryTypeName);
                throw;
            }
        }

        public async Task<List<CmsCategoryValue>> GetCategoryValuesAsync()
        {
            try
            {
                var baseUrl = _configuration["CmsApi:BaseUrl"] ?? "https://fips-cms.azurewebsites.net/api/";
                if (!baseUrl.EndsWith("/"))
                {
                    baseUrl += "/";
                }
                var url = $"{baseUrl}category-values?populate=category_type&sort=sort_order:asc";

                _logger.LogInformation("Fetching category values from CMS API");

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                var apiKey = _configuration["CmsApi:ReadApiKey"];
                if (!string.IsNullOrEmpty(apiKey))
                {
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
                }

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<CmsApiResponse<CmsCategoryValue>>(content);

                if (result == null)
                {
                    throw new InvalidOperationException("Failed to deserialize CMS API response");
                }

                _logger.LogInformation("Successfully fetched {Count} category values from CMS", result.Data.Count);
                return result.Data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching category values from CMS API");
                throw;
            }
        }

        public async Task<List<CmsProduct>> GetProductsByUserEmailAsync(string userEmail)
        {
            try
            {
                var baseUrl = _configuration["CmsApi:BaseUrl"] ?? "https://fips-cms.azurewebsites.net/api/";
                if (!baseUrl.EndsWith("/"))
                {
                    baseUrl += "/";
                }
                
                // Query product_contacts filtered by user email (case-insensitive) and "reporting" role, then populate the related product data
                var url = $"{baseUrl}product-contacts?filters[users_permissions_user][email][$eqi]={Uri.EscapeDataString(userEmail)}&filters[role][$eq]={Uri.EscapeDataString("reporting")}&populate[0]=users_permissions_user&populate[1]=product&populate[2]=product.category_values&populate[3]=product.category_values.category_type&populate[4]=product.product_contacts&populate[5]=product.product_contacts.users_permissions_user";

                _logger.LogInformation("Fetching products with 'reporting' role for user {UserEmail} from CMS API", userEmail);

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                var apiKey = _configuration["CmsApi:ReadApiKey"];
                if (!string.IsNullOrEmpty(apiKey))
                {
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
                }

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<CmsApiResponse<CmsProductContact>>(content);

                if (result == null)
                {
                    throw new InvalidOperationException("Failed to deserialize CMS API response");
                }

                // Extract unique products from the product contacts
                var products = result.Data
                    .Where(pc => pc.Product != null)
                    .Select(pc => pc.Product!)
                    .GroupBy(p => p.Id)
                    .Select(g => g.First())
                    .ToList();

                _logger.LogInformation("Successfully fetched {Count} products with 'reporting' role for user {UserEmail} from CMS", products.Count, userEmail);
                return products;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching products with 'reporting' role for user {UserEmail} from CMS API", userEmail);
                throw;
            }
        }

        public ProductViewModel MapToViewModel(CmsProduct product, bool isAllocatedToUser = false)
        {
            return new ProductViewModel
            {
                Id = product.Id,
                FipsId = product.FipsId,
                Title = product.Title,
                ShortDescription = product.ShortDescription,
                LongDescription = product.LongDescription,
                ProductUrl = product.ProductUrl,
                State = product.State,
                CategoryValues = product.CategoryValues?.Select(cv => cv.Name).ToList() ?? new List<string>(),
                CategoryTypes = product.CategoryValues?.Select(cv => cv.CategoryType?.Name).Where(ct => !string.IsNullOrEmpty(ct)).ToList() ?? new List<string>(),
                ProductContacts = product.ProductContacts?.Select(pc => new ProductContactViewModel
                {
                    Id = pc.Id,
                    Role = pc.Role,
                    UserEmail = pc.User?.Email,
                    UserName = pc.User?.Username,
                    DisplayName = pc.User?.DisplayName ?? $"{pc.User?.FirstName} {pc.User?.LastName}".Trim()
                }).ToList() ?? new List<ProductContactViewModel>(),
                IsPublished = product.PublishedAt.HasValue,
                CreatedAt = product.CreatedAt,
                UpdatedAt = product.UpdatedAt,
                IsAllocatedToUser = isAllocatedToUser
            };
        }
    }
}
