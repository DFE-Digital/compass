using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Compass.Scripts;

/// <summary>
/// Script to update products that are Retired in CMDB but Active in CMS
/// to have Phase category value set to "Decommissioned"
/// </summary>
public class UpdateRetiredProductsToDecommissioned
{
    private readonly HttpClient _httpClient;
    private readonly string _cmsBaseUrl;
    private readonly string _cmsWriteApiKey;
    private readonly List<UpdateResult> _results = new();
    private readonly List<RollbackEntry> _rollbackLog = new();

    public UpdateRetiredProductsToDecommissioned(
        string cmsBaseUrl,
        string cmsWriteApiKey)
    {
        _cmsBaseUrl = cmsBaseUrl.TrimEnd('/');
        _cmsWriteApiKey = cmsWriteApiKey;

        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", _cmsWriteApiKey);
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.BaseAddress = new Uri(_cmsBaseUrl + "/");
    }

    public async Task<UpdateSummary> UpdateProductsAsync(List<MismatchEntry> productsToUpdate)
    {
        Console.WriteLine("=== Updating Retired Products to Decommissioned Phase ===\n");

        // Step 1: Find the "Decommissioned" category value ID
        Console.WriteLine("Step 1: Finding 'Decommissioned' Phase category value...");
        var decommissionedCategoryValueId = await FindDecommissionedCategoryValueIdAsync();
        
        if (decommissionedCategoryValueId == null)
        {
            Console.WriteLine("ERROR: Could not find 'Decommissioned' Phase category value. Aborting.");
            return new UpdateSummary
            {
                TotalProducts = productsToUpdate.Count,
                Successful = 0,
                Failed = productsToUpdate.Count,
                Errors = new List<string> { "Could not find 'Decommissioned' Phase category value" }
            };
        }

        Console.WriteLine($"Found 'Decommissioned' category value ID: {decommissionedCategoryValueId}\n");

        // Step 2: Update each product
        Console.WriteLine($"Step 2: Updating {productsToUpdate.Count} products...\n");
        
        var processed = 0;
        foreach (var product in productsToUpdate)
        {
            processed++;
            Console.WriteLine($"[{processed}/{productsToUpdate.Count}] Processing: {product.CmsTitle} ({product.CmsFipsId})");
            
            try
            {
                var result = await UpdateProductAsync(
                    product.CmsFipsId!,
                    product.CmsDocumentId!,
                    decommissionedCategoryValueId.Value);
                
                _results.Add(result);
                
                if (result.Success)
                {
                    Console.WriteLine($"  ✓ Successfully updated");
                }
                else
                {
                    Console.WriteLine($"  ✗ Failed: {result.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                var errorResult = new UpdateResult
                {
                    FipsId = product.CmsFipsId!,
                    Success = false,
                    ErrorMessage = $"Exception: {ex.Message}"
                };
                _results.Add(errorResult);
                Console.WriteLine($"  ✗ Exception: {ex.Message}");
            }

            // Small delay to avoid overwhelming the API
            await Task.Delay(500);
        }

        Console.WriteLine("\n=== Update Complete ===\n");
        
        return new UpdateSummary
        {
            TotalProducts = productsToUpdate.Count,
            Successful = _results.Count(r => r.Success),
            Failed = _results.Count(r => !r.Success),
            Errors = _results.Where(r => !r.Success).Select(r => $"{r.FipsId}: {r.ErrorMessage}").ToList()
        };
    }

    private async Task<int?> FindDecommissionedCategoryValueIdAsync()
    {
        try
        {
            var queryParams = new List<string>
            {
                "filters[category_type][name][$eq]=Phase",
                "filters[name][$eq]=Decommissioned",
                "filters[publishedAt][$notNull]=true",
                "filters[enabled]=true",
                "fields[0]=id",
                "fields[1]=name",
                "populate[category_type][fields][0]=name"
            };

            var queryString = string.Join("&", queryParams);
            var url = $"category-values?{queryString}";

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<CmsApiResponse<CmsCategoryValue>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var categoryValue = result?.Data?.FirstOrDefault();
            return categoryValue?.Id;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error finding Decommissioned category value: {ex.Message}");
            return null;
        }
    }

    private async Task<UpdateResult> UpdateProductAsync(string fipsId, string documentId, int decommissionedCategoryValueId)
    {
        var result = new UpdateResult { FipsId = fipsId };

        try
        {
            // Step 1: Get current product with all category values
            var product = await GetProductWithCategoryValuesAsync(fipsId);
            if (product == null)
            {
                result.ErrorMessage = "Product not found";
                return result;
            }

            // Step 2: Build rollback log entry
            var rollbackEntry = new RollbackEntry
            {
                FipsId = fipsId,
                DocumentId = documentId,
                Title = product.Title,
                OriginalCategoryValueIds = product.CategoryValues?.Select(cv => cv.Id).ToList() ?? new List<int>(),
                OriginalCategoryValues = product.CategoryValues?.Select(cv => new CategoryValueInfo
                {
                    Id = cv.Id,
                    Name = cv.Name,
                    CategoryType = cv.CategoryType?.Name ?? "Unknown"
                }).ToList() ?? new List<CategoryValueInfo>()
            };
            _rollbackLog.Add(rollbackEntry);

            // Step 3: Process category values
            var currentCategoryValueIds = product.CategoryValues?.Select(cv => cv.Id).ToList() ?? new List<int>();
            
            // Find and remove existing Phase category value
            var phaseCategoryValue = product.CategoryValues?
                .FirstOrDefault(cv => cv.CategoryType?.Name?.Equals("Phase", StringComparison.OrdinalIgnoreCase) == true);
            
            if (phaseCategoryValue != null)
            {
                currentCategoryValueIds.Remove(phaseCategoryValue.Id);
                result.RemovedPhaseCategoryValueId = phaseCategoryValue.Id;
                result.RemovedPhaseCategoryValueName = phaseCategoryValue.Name;
            }

            // Add Decommissioned Phase category value
            if (!currentCategoryValueIds.Contains(decommissionedCategoryValueId))
            {
                currentCategoryValueIds.Add(decommissionedCategoryValueId);
            }

            result.UpdatedCategoryValueIds = currentCategoryValueIds.ToList();

            // Step 4: Update the product
            var updateData = new
            {
                data = new
                {
                    fips_id = fipsId,
                    category_values = currentCategoryValueIds
                }
            };

            var json = JsonSerializer.Serialize(updateData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync($"products/{documentId}", content);
            
            if (response.IsSuccessStatusCode)
            {
                result.Success = true;
                result.Message = "Product updated successfully";
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                result.ErrorMessage = $"HTTP {response.StatusCode}: {errorContent}";
            }
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"Exception: {ex.Message}";
        }

        return result;
    }

    private async Task<CmsProductWithCategories?> GetProductWithCategoryValuesAsync(string fipsId)
    {
        try
        {
            var queryParams = new List<string>
            {
                $"filters[fips_id][$eq]={Uri.EscapeDataString(fipsId)}",
                "fields[0]=id",
                "fields[1]=documentId",
                "fields[2]=title",
                "fields[3]=fips_id",
                "populate[category_values][fields][0]=id",
                "populate[category_values][fields][1]=name",
                "populate[category_values][populate][category_type][fields][0]=name"
            };

            var queryString = string.Join("&", queryParams);
            var url = $"products?{queryString}";

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<CmsApiResponse<CmsProductWithCategories>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result?.Data?.FirstOrDefault();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching product {fipsId}: {ex.Message}");
            return null;
        }
    }

    public void PrintResults(UpdateSummary summary)
    {
        Console.WriteLine("=== UPDATE RESULTS ===\n");
        Console.WriteLine($"Total products: {summary.TotalProducts}");
        Console.WriteLine($"Successful: {summary.Successful}");
        Console.WriteLine($"Failed: {summary.Failed}\n");

        if (summary.Errors.Any())
        {
            Console.WriteLine("Errors:");
            foreach (var error in summary.Errors)
            {
                Console.WriteLine($"  - {error}");
            }
            Console.WriteLine();
        }
    }

    public List<RollbackEntry> GetRollbackLog()
    {
        return _rollbackLog;
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}

// Data models
public class CmsCategoryValue
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("category_type")]
    public CategoryTypeInfo? CategoryType { get; set; }
}

public class CategoryTypeInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

public class CmsProductWithCategories
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("documentId")]
    public string? DocumentId { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("fips_id")]
    public string? FipsId { get; set; }

    [JsonPropertyName("category_values")]
    public List<CmsCategoryValue>? CategoryValues { get; set; }
}

public class UpdateResult
{
    public string FipsId { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? ErrorMessage { get; set; }
    public int? RemovedPhaseCategoryValueId { get; set; }
    public string? RemovedPhaseCategoryValueName { get; set; }
    public List<int>? UpdatedCategoryValueIds { get; set; }
}

public class UpdateSummary
{
    public int TotalProducts { get; set; }
    public int Successful { get; set; }
    public int Failed { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class RollbackEntry
{
    public string FipsId { get; set; } = string.Empty;
    public string DocumentId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public List<int> OriginalCategoryValueIds { get; set; } = new();
    public List<CategoryValueInfo> OriginalCategoryValues { get; set; } = new();
}

public class CategoryValueInfo
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CategoryType { get; set; } = string.Empty;
}

public class CmsApiResponse<T>
{
    [JsonPropertyName("data")]
    public List<T>? Data { get; set; }

    [JsonPropertyName("meta")]
    public CmsMeta? Meta { get; set; }
}
