using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Compass.Models;

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
        
        // Set base address and authorization like reporting project
        _httpClient.BaseAddress = new Uri(_baseUrl);
        var readApiKey = _configuration["StandardsCmsApi:ReadApiKey"];
        if (!string.IsNullOrEmpty(readApiKey))
        {
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {readApiKey}");
        }
        
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
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Standards CMS API error: Status {StatusCode}, Response: {ErrorContent}", response.StatusCode, errorContent);
            }
            
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

    public async Task<List<StandardDto>> GetStandardsAsync(bool? published = null, string? search = null, string? category = null, string? stage = null, TimeSpan? cacheDuration = null)
    {
        try
        {
            var queryParams = new List<string>
            {
                "sort=title:asc",
                "pagination[page]=1",
                "pagination[pageSize]=1000",
                // Simple populate - match Node.js getStandardsForList approach
                "populate=*"
            };

            // Filter by publication status - use the same syntax as reporting project
            if (published.HasValue)
            {
                if (published.Value)
                {
                    // Published standards - only get published ones
                    queryParams.Add("filters[publishedAt][$notNull]=true");
                }
                else
                {
                    // Draft standards - publishedAt is null
                    queryParams.Add("filters[publishedAt][$null]=true");
                }
            }

            // Add search query if provided (same as reporting)
            if (!string.IsNullOrEmpty(search))
            {
                queryParams.Add($"filters[$or][0][title][$containsi]={Uri.EscapeDataString(search)}");
                queryParams.Add($"filters[$or][1][summary][$containsi]={Uri.EscapeDataString(search)}");
            }

            // Category filter - use title instead of slug for the filter dropdown
            // The filter dropdown shows titles, so match on title
            if (!string.IsNullOrEmpty(category))
            {
                queryParams.Add($"filters[categories][title][$eq]={Uri.EscapeDataString(category)}");
            }

            // Stage filter
            if (!string.IsNullOrEmpty(stage))
            {
                queryParams.Add($"filters[stage][title][$eq]={Uri.EscapeDataString(stage)}");
            }

            var queryString = string.Join("&", queryParams);
            var url = $"standards?{queryString}";

            _logger.LogInformation("Fetching standards from URL: {Url}", url);
            
            // Use direct HTTP call instead of GetAsync to get better error handling
            var fullUrl = $"{_baseUrl.TrimEnd('/')}/{url.TrimStart('/')}";
            var cacheKey = $"standards_cms_api_{url}";

            // Check cache first if duration specified
            if (cacheDuration.HasValue && _cache.TryGetValue<List<StandardDto>>(cacheKey, out var cachedResult))
            {
                _logger.LogInformation("Cache hit for Standards CMS endpoint: {Endpoint}", url);
                return cachedResult ?? new List<StandardDto>();
            }

            var response = await _httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Standards CMS API error: Status {StatusCode} for URL: {Url}. Response: {ErrorContent}", 
                    response.StatusCode, url, errorContent);
                return new List<StandardDto>();
            }
            
            var jsonContent = await response.Content.ReadAsStringAsync();
            
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            
            var result = JsonSerializer.Deserialize<ApiCollectionResponse<StandardDto>>(jsonContent, options);
            var standards = result?.Data ?? new List<StandardDto>();
            
            _logger.LogInformation("Successfully retrieved {Count} standards", standards.Count);
            
            // Cache the result if cache duration is specified
            if (cacheDuration.HasValue && standards.Count > 0)
            {
                _cache.Set(cacheKey, standards, cacheDuration.Value);
                _logger.LogInformation("Cached Standards CMS result for endpoint: {Endpoint} for {Duration}", url, cacheDuration.Value);
            }
            
            return standards;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching standards from Standards CMS API");
            return new List<StandardDto>();
        }
    }

    public async Task<StandardDto?> GetStandardByIdAsync(int id, TimeSpan? cacheDuration = null)
    {
        try
        {
            // Use specific field populate syntax based on actual Strapi schemas to avoid deep nesting errors
            var queryParams = new List<string>
            {
                // Stage - schema shows 'title' field
                "populate[stage][fields][0]=title",
                "populate[stage][fields][1]=number",
                "populate[stage][fields][2]=active",
                // Categories - schema shows 'title' field, don't populate nested sub_categories relation
                "populate[categories][fields][0]=title",
                "populate[categories][fields][1]=slug",
                "populate[categories][fields][2]=description",
                "populate[categories][fields][3]=active",
                // Sub_categories - can't populate nested category via query string
                // Will fetch sub-categories separately with category populated if needed
                "populate[sub_categories][fields][0]=title",
                "populate[sub_categories][fields][1]=slug",
                "populate[sub_categories][fields][2]=active",
                // Phases - schema shows 'Title' (capital T) and 'Enabled' (capital E)
                "populate[phases][fields][0]=Title",
                "populate[phases][fields][1]=Enabled",
                // Owners - user fields (firstName, lastName, email, JobRole - matching Node.js)
                "populate[owners][fields][0]=firstName",
                "populate[owners][fields][1]=lastName",
                "populate[owners][fields][2]=email",
                "populate[owners][fields][3]=JobRole",
                // Contacts - user fields (firstName, lastName, email, JobRole - matching Node.js)
                "populate[contacts][fields][0]=firstName",
                "populate[contacts][fields][1]=lastName",
                "populate[contacts][fields][2]=email",
                "populate[contacts][fields][3]=JobRole"
            };

            var queryString = string.Join("&", queryParams);
            var url = $"standards/{id}?{queryString}";

            _logger.LogInformation("Fetching standard by ID {Id} from URL: {Url}", id, url);
            
            var cacheKey = $"standards_cms_api_standard_{id}";
            
            // Check cache first if duration specified
            if (cacheDuration.HasValue && _cache.TryGetValue<StandardDto>(cacheKey, out var cachedResult))
            {
                _logger.LogInformation("Cache hit for standard {Id}", id);
                return cachedResult;
            }

            var response = await _httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Standards CMS API error: Status {StatusCode} for URL: {Url}. Response: {ErrorContent}", 
                    response.StatusCode, url, errorContent);
                return null;
            }
            
            var jsonContent = await response.Content.ReadAsStringAsync();
            
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            
            var result = JsonSerializer.Deserialize<ApiResponse<StandardDto>>(jsonContent, options);
            var standard = result?.Data;
            
            if (standard != null)
            {
                _logger.LogInformation("Successfully retrieved standard {Id}: {Title}", id, standard.Title);
                
                // Cache the result if cache duration is specified
                if (cacheDuration.HasValue)
                {
                    _cache.Set(cacheKey, standard, cacheDuration.Value);
                    _logger.LogInformation("Cached standard {Id} for {Duration}", id, cacheDuration.Value);
                }
            }
            else
            {
                _logger.LogWarning("No standard data found for ID {Id}", id);
            }
            
            return standard;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching standard {Id} from Standards CMS API", id);
            return null;
        }
    }

    public async Task<StandardDto?> GetStandardByDocumentIdAsync(string documentId, TimeSpan? cacheDuration = null)
    {
        try
        {
            // Use specific field populate syntax based on actual Strapi schemas to avoid deep nesting errors
            var queryParams = new List<string>
            {
                $"filters[documentId][$eq]={Uri.EscapeDataString(documentId)}",
                // Stage - schema shows 'title' field (not name)
                "populate[stage][fields][0]=title",
                "populate[stage][fields][1]=number",
                "populate[stage][fields][2]=active",
                // Categories - schema shows 'title' field, don't populate nested sub_categories relation
                "populate[categories][fields][0]=id",
                "populate[categories][fields][1]=title",
                "populate[categories][fields][2]=slug",
                "populate[categories][fields][3]=description",
                "populate[categories][fields][4]=active",
                // Sub_categories - populate with category relation like Node.js version
                "populate[sub_categories][fields][0]=id",
                "populate[sub_categories][fields][1]=title",
                "populate[sub_categories][fields][2]=slug",
                "populate[sub_categories][fields][3]=active",
                // Populate category relation on sub_categories (nested populate like Node.js)
                "populate[sub_categories][populate][category][fields][0]=id",
                "populate[sub_categories][populate][category][fields][1]=title",
                "populate[sub_categories][populate][category][fields][2]=slug",
                // Phases - schema shows 'Title' (capital T) and 'Enabled' (capital E)
                "populate[phases][fields][0]=Title",
                "populate[phases][fields][1]=Enabled",
                // Owners - user fields (firstName, lastName, email, JobRole - matching Node.js)
                "populate[owners][fields][0]=firstName",
                "populate[owners][fields][1]=lastName",
                "populate[owners][fields][2]=email",
                "populate[owners][fields][3]=JobRole",
                // Contacts - user fields (firstName, lastName, email, JobRole - matching Node.js)
                "populate[contacts][fields][0]=firstName",
                "populate[contacts][fields][1]=lastName",
                "populate[contacts][fields][2]=email",
                "populate[contacts][fields][3]=JobRole",
                // Products - schema shows title, vendor, version, useCase, externalLink
                "populate[approvedProducts][fields][0]=title",
                "populate[approvedProducts][fields][1]=vendor",
                "populate[approvedProducts][fields][2]=version",
                "populate[approvedProducts][fields][3]=useCase",
                "populate[approvedProducts][fields][4]=externalLink",
                "populate[toleratedProducts][fields][0]=title",
                "populate[toleratedProducts][fields][1]=vendor",
                "populate[toleratedProducts][fields][2]=version",
                "populate[toleratedProducts][fields][3]=useCase",
                "populate[toleratedProducts][fields][4]=externalLink",
                // Exceptions - schema shows title, details, active (no reason field)
                "populate[exceptions][fields][0]=title",
                "populate[exceptions][fields][1]=details",
                "populate[exceptions][fields][2]=active",
                // Standard comments - schema shows 'comments' and 'title' fields
                "populate[standard_comments][fields][0]=title",
                "populate[standard_comments][fields][1]=comments",
                "populate[creator][fields][0]=firstName",
                "populate[creator][fields][1]=lastName",
                "populate[creator][fields][2]=email"
            };

            var queryString = string.Join("&", queryParams);
            // Match Node.js: uses ?status=draft for draft standards
            var url = $"standards?status=draft&{queryString}";

            _logger.LogInformation("Fetching standard by documentId {DocumentId} from URL: {Url}", documentId, url);
            
            var cacheKey = $"standards_cms_api_standard_docid_{documentId}";
            
            // Check cache first if duration specified
            if (cacheDuration.HasValue && _cache.TryGetValue<StandardDto>(cacheKey, out var cachedResult))
            {
                _logger.LogInformation("Cache hit for standard documentId {DocumentId}", documentId);
                return cachedResult;
            }

            var response = await _httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Standards CMS API error: Status {StatusCode} for URL: {Url}. Response: {ErrorContent}", 
                    response.StatusCode, url, errorContent);
                return null;
            }
            
            var jsonContent = await response.Content.ReadAsStringAsync();
            
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            
            var result = JsonSerializer.Deserialize<ApiCollectionResponse<StandardDto>>(jsonContent, options);
            var standard = result?.Data?.FirstOrDefault();
            
            if (standard != null)
            {
                _logger.LogInformation("Successfully retrieved standard with documentId {DocumentId}: {Title}", documentId, standard.Title);
                
                // Cache the result if cache duration is specified
                if (cacheDuration.HasValue)
                {
                    _cache.Set(cacheKey, standard, cacheDuration.Value);
                    _logger.LogInformation("Cached standard documentId {DocumentId} for {Duration}", documentId, cacheDuration.Value);
                }
                
                return standard;
            }
            else
            {
                _logger.LogWarning("No standard data found for documentId {DocumentId}", documentId);
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching standard with documentId {DocumentId} from Standards CMS API", documentId);
            return null;
        }
    }

    public async Task<List<StandardCategoryDto>> GetCategoriesAsync(TimeSpan? cacheDuration = null)
    {
        try
        {
            var queryParams = new List<string>
            {
                "filters[publishedAt][$notNull]=true"
            };

            var queryString = string.Join("&", queryParams);
            var url = $"categories?{queryString}";

            _logger.LogInformation("Fetching categories from URL: {Url}", url);
            
            var cacheKey = $"standards_cms_api_categories_{url}";
            
            // Check cache first if duration specified
            if (cacheDuration.HasValue && _cache.TryGetValue<List<StandardCategoryDto>>(cacheKey, out var cachedResult))
            {
                _logger.LogInformation("Cache hit for categories endpoint");
                return cachedResult ?? new List<StandardCategoryDto>();
            }

            var response = await _httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("API Error: {StatusCode} {ReasonPhrase} for URL: {Url}. Response: {ErrorContent}", 
                    response.StatusCode, response.ReasonPhrase, url, errorContent);
                return new List<StandardCategoryDto>();
            }
            
            var jsonContent = await response.Content.ReadAsStringAsync();
            
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            
            var result = JsonSerializer.Deserialize<ApiCollectionResponse<StandardCategoryDto>>(jsonContent, options);
            var categories = result?.Data ?? new List<StandardCategoryDto>();
            
            // Cache the result if cache duration is specified
            if (cacheDuration.HasValue && categories.Count > 0)
            {
                _cache.Set(cacheKey, categories, cacheDuration.Value);
            }
            
            return categories;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API Error fetching categories");
            return new List<StandardCategoryDto>();
        }
    }

    public async Task<List<StandardStageDto>> GetStagesAsync(TimeSpan? cacheDuration = null)
    {
        try
        {
            var queryParams = new List<string>
            {
                "filters[publishedAt][$notNull]=true"
            };

            var queryString = string.Join("&", queryParams);
            var url = $"stages?{queryString}";

            _logger.LogInformation("Fetching stages from URL: {Url}", url);
            
            var cacheKey = $"standards_cms_api_stages_{url}";
            
            // Check cache first if duration specified
            if (cacheDuration.HasValue && _cache.TryGetValue<List<StandardStageDto>>(cacheKey, out var cachedResult))
            {
                _logger.LogInformation("Cache hit for stages endpoint");
                return cachedResult ?? new List<StandardStageDto>();
            }

            var response = await _httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("API Error: {StatusCode} {ReasonPhrase} for URL: {Url}. Response: {ErrorContent}", 
                    response.StatusCode, response.ReasonPhrase, url, errorContent);
                return new List<StandardStageDto>();
            }
            
            var jsonContent = await response.Content.ReadAsStringAsync();
            
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            
            var result = JsonSerializer.Deserialize<ApiCollectionResponse<StandardStageDto>>(jsonContent, options);
            var stages = result?.Data ?? new List<StandardStageDto>();
            
            // Cache the result if cache duration is specified
            if (cacheDuration.HasValue && stages.Count > 0)
            {
                _cache.Set(cacheKey, stages, cacheDuration.Value);
            }
            
            return stages;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API Error fetching stages");
            return new List<StandardStageDto>();
        }
    }

    public async Task<List<StandardSubCategoryDto>> GetSubCategoriesByIdsAsync(List<int> subCategoryIds, TimeSpan? cacheDuration = null)
    {
        if (subCategoryIds == null || !subCategoryIds.Any())
        {
            return new List<StandardSubCategoryDto>();
        }

        try
        {
            // Create cache key
            var cacheKey = $"sub_categories_{string.Join("_", subCategoryIds.OrderBy(id => id))}";
            
            // TEMPORARILY DISABLE CACHE FOR DEBUGGING - Remove this once we fix the issue
            // if (cacheDuration.HasValue && _cache.TryGetValue<List<StandardSubCategoryDto>>(cacheKey, out var cachedResult))
            // {
            //     _logger.LogInformation("Cache hit for sub-categories: {Ids}", string.Join(", ", subCategoryIds));
            //     return cachedResult ?? new List<StandardSubCategoryDto>();
            // }

            // Build query with filters for all sub-category IDs using $in operator
            // Strapi v5 requires indexed array syntax: filters[id][$in][0]=1&filters[id][$in][1]=2
            var queryParams = new List<string>();
            for (int i = 0; i < subCategoryIds.Count; i++)
            {
                queryParams.Add($"filters[id][$in][{i}]={subCategoryIds[i]}");
            }
            
            queryParams.AddRange(new[]
            {
                // Populate category relation with specific fields (can't use * as it tries to populate inverse relations)
                "populate[category][fields][0]=id",
                "populate[category][fields][1]=title",
                "populate[category][fields][2]=slug",
                "populate[category][fields][3]=description",
                "populate[category][fields][4]=active"
            });

            var queryString = string.Join("&", queryParams);
            
            // Try without status first (published items), then try with status=draft if needed
            var url = $"sub-categories?{queryString}";
            
            _logger.LogWarning("=== DEBUG: Fetching sub-categories with categories ===");
            _logger.LogWarning("Sub-category IDs being queried: {Ids}", string.Join(", ", subCategoryIds));
            _logger.LogWarning("Query string: {QueryString}", queryString);
            
            var fullUrl = $"{_baseUrl.TrimEnd('/')}/{url.TrimStart('/')}";
            _logger.LogWarning("Full URL (trying published first): {FullUrl}", fullUrl);
            
            var readApiKey = _configuration["StandardsCmsApi:ReadApiKey"];
            if (!string.IsNullOrEmpty(readApiKey))
            {
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", readApiKey);
            }
            
            var httpResponse = await _httpClient.GetAsync(fullUrl);
            
            if (!httpResponse.IsSuccessStatusCode)
            {
                var errorContent = await httpResponse.Content.ReadAsStringAsync();
                _logger.LogError("API error fetching sub-categories: Status {StatusCode}, Response: {ErrorContent}", 
                    httpResponse.StatusCode, errorContent);
                return new List<StandardSubCategoryDto>();
            }
            
            var jsonContent = await httpResponse.Content.ReadAsStringAsync();
            
            _logger.LogWarning("=== DEBUG: Raw API response for sub-categories (first 2000 chars) ===");
            _logger.LogWarning("{Response}", jsonContent.Length > 2000 ? jsonContent.Substring(0, 2000) + "..." : jsonContent);

            var response = JsonSerializer.Deserialize<ApiCollectionResponse<StandardSubCategoryDto>>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            var subCategories = response?.Data ?? new List<StandardSubCategoryDto>();
            
            // If no results and we haven't tried draft, try with status=draft
            if (!subCategories.Any())
            {
                _logger.LogWarning("No sub-categories found with published status, trying draft status...");
                var draftUrl = $"{url}&status=draft";
                var draftFullUrl = $"{_baseUrl.TrimEnd('/')}/{draftUrl.TrimStart('/')}";
                _logger.LogWarning("Trying draft URL: {DraftUrl}", draftFullUrl);
                
                var draftResponse = await _httpClient.GetAsync(draftFullUrl);
                if (draftResponse.IsSuccessStatusCode)
                {
                    var draftJsonContent = await draftResponse.Content.ReadAsStringAsync();
                    _logger.LogWarning("Draft response (first 1000 chars): {Response}", 
                        draftJsonContent.Length > 1000 ? draftJsonContent.Substring(0, 1000) + "..." : draftJsonContent);
                    
                    var draftApiResponse = JsonSerializer.Deserialize<ApiCollectionResponse<StandardSubCategoryDto>>(draftJsonContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    subCategories = draftApiResponse?.Data ?? new List<StandardSubCategoryDto>();
                }
            }

            _logger.LogInformation("Fetched {Count} sub-categories. Checking category relations...", subCategories.Count);
            foreach (var sc in subCategories)
            {
                if (sc.Category != null)
                {
                    _logger.LogInformation("Sub-category {Id} ({Title}) belongs to category {CategoryId} ({CategoryTitle})", 
                        sc.Id, sc.Title, sc.Category.Id, sc.Category.Title);
                }
                else
                {
                    _logger.LogWarning("Sub-category {Id} ({Title}) has no category relation", sc.Id, sc.Title);
                }
            }

            if (cacheDuration.HasValue && subCategories.Any())
            {
                _cache.Set(cacheKey, subCategories, cacheDuration.Value);
            }

            return subCategories;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API Error fetching sub-categories with categories");
            return new List<StandardSubCategoryDto>();
        }
    }
}

