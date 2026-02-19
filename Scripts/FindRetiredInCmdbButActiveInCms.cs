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
/// Script to find products that have Operational Status = "Retired" in CMDB 
/// but State = "Active" in CMS.
/// 
/// Run this script from the Compass project root:
/// dotnet run --project Compass.csproj --script Scripts/FindRetiredInCmdbButActiveInCms.cs
/// 
/// Or use as a console app entry point.
/// </summary>
public class FindRetiredInCmdbButActiveInCms
{
    private readonly HttpClient _httpClient;
    private readonly string _cmdbEndpoint;
    private readonly string _cmdbUsername;
    private readonly string _cmdbPassword;
    private readonly string _cmsBaseUrl;
    private readonly string _cmsReadApiKey;

    public FindRetiredInCmdbButActiveInCms(
        string cmdbEndpoint,
        string cmdbUsername,
        string cmdbPassword,
        string cmsBaseUrl,
        string cmsReadApiKey)
    {
        _cmdbEndpoint = cmdbEndpoint;
        _cmdbUsername = cmdbUsername;
        _cmdbPassword = cmdbPassword;
        _cmsBaseUrl = cmsBaseUrl;
        _cmsReadApiKey = cmsReadApiKey;

        _httpClient = new HttpClient();
        
        // Configure CMDB auth
        var credentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{_cmdbUsername}:{_cmdbPassword}"));
        _httpClient.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Basic", credentials);
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<List<MismatchEntry>> FindMismatchesAsync()
    {
        Console.WriteLine("=== Finding products with Operational Status = 'Retired' (6) in CMDB but State = 'Active' in CMS ===\n");

        // Step 1: Fetch CMDB entries with operational_status = 6 (Retired)
        Console.WriteLine("Step 1: Fetching CMDB entries with operational_status = 6 (Retired)...");
        var retiredCmdbEntries = await GetRetiredCmdbEntriesAsync();
        Console.WriteLine($"Found {retiredCmdbEntries.Count} retired CMDB entries\n");

        // Step 2: Fetch CMS products with state = "Active"
        Console.WriteLine("Step 2: Fetching CMS products with state = 'Active'...");
        var activeCmsProducts = await GetActiveCmsProductsAsync();
        Console.WriteLine($"Found {activeCmsProducts.Count} active CMS products\n");

        // Step 3: Match by cmdb_sys_id (CMS) = sys_id (CMDB)
        Console.WriteLine("Step 3: Matching products...");
        var mismatches = new List<MismatchEntry>();

        foreach (var cmdbEntry in retiredCmdbEntries)
        {
            var matchingProduct = activeCmsProducts.FirstOrDefault(
                p => !string.IsNullOrEmpty(p.CmdbSysId) && 
                     p.CmdbSysId.Equals(cmdbEntry.SysId, StringComparison.OrdinalIgnoreCase));

            if (matchingProduct != null)
            {
                mismatches.Add(new MismatchEntry
                {
                    CmdbSysId = cmdbEntry.SysId,
                    CmdbName = cmdbEntry.Name,
                    CmdbOperationalStatus = cmdbEntry.OperationalStatus,
                    CmsProductId = matchingProduct.Id,
                    CmsDocumentId = matchingProduct.DocumentId,
                    CmsTitle = matchingProduct.Title,
                    CmsFipsId = matchingProduct.FipsId,
                    CmsState = matchingProduct.State
                });
            }
        }

        Console.WriteLine($"Found {mismatches.Count} mismatches\n");
        return mismatches;
    }

    private async Task<List<CmdbEntryWithStatus>> GetRetiredCmdbEntriesAsync()
    {
        var allEntries = new List<CmdbEntryWithStatus>();
        var offset = 0;
        const int limit = 1000;
        var hasMore = true;

        while (hasMore)
        {
            var queryParams = new Dictionary<string, string>
            {
                ["sysparm_query"] = "operational_status=6",
                ["sysparm_fields"] = "name,sys_id,operational_status,description",
                ["sysparm_limit"] = limit.ToString(),
                ["sysparm_offset"] = offset.ToString()
            };

            var query = string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
            var requestUri = $"{_cmdbEndpoint}?{query}";

            try
            {
                var response = await _httpClient.GetAsync(requestUri);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ServiceNowResponse<List<CmdbEntryWithStatus>>>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                var entries = result?.Result ?? new List<CmdbEntryWithStatus>();
                allEntries.AddRange(entries);

                hasMore = entries.Count == limit;
                offset += limit;

                Console.WriteLine($"  Fetched {allEntries.Count} retired entries so far...");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching CMDB entries: {ex.Message}");
                throw;
            }
        }

        return allEntries;
    }

    private async Task<List<CmsProduct>> GetActiveCmsProductsAsync()
    {
        var allProducts = new List<CmsProduct>();
        var currentPage = 1;
        const int pageSize = 100;
        var hasMorePages = true;

        _httpClient.DefaultRequestHeaders.Remove("Authorization");
        _httpClient.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", _cmsReadApiKey);

        while (hasMorePages)
        {
            var queryParams = new List<string>
            {
                "sort=title:asc",
                "filters[state][$eq]=Active",
                $"pagination[page]={currentPage}",
                $"pagination[pageSize]={pageSize}",
                "fields[0]=id",
                "fields[1]=documentId",
                "fields[2]=title",
                "fields[3]=fips_id",
                "fields[4]=state",
                "fields[5]=cmdb_sys_id"
            };

            var queryString = string.Join("&", queryParams);
            var url = $"{_cmsBaseUrl.TrimEnd('/')}/products?{queryString}";

            try
            {
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<CmsApiResponse>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                var products = result?.Data ?? new List<CmsProduct>();
                allProducts.AddRange(products);

                hasMorePages = products.Count == pageSize;
                currentPage++;

                Console.WriteLine($"  Fetched {allProducts.Count} active products so far...");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching CMS products: {ex.Message}");
                throw;
            }
        }

        return allProducts;
    }

    public void PrintResults(List<MismatchEntry> mismatches)
    {
        Console.WriteLine("=== RESULTS ===\n");
        Console.WriteLine($"Total mismatches found: {mismatches.Count}\n");

        if (mismatches.Count == 0)
        {
            Console.WriteLine("No mismatches found. All products are correctly aligned between CMDB and CMS.");
            return;
        }

        Console.WriteLine("Products with Operational Status = 'Retired' in CMDB but State = 'Active' in CMS:\n");
        Console.WriteLine(new string('-', 120));
        Console.WriteLine($"{"CMDB Name",-40} | {"CMDB Sys ID",-20} | {"CMS Title",-40} | {"CMS FIPS ID",-15}");
        Console.WriteLine(new string('-', 120));

        foreach (var mismatch in mismatches.OrderBy(m => m.CmdbName))
        {
            Console.WriteLine($"{Truncate(mismatch.CmdbName, 40),-40} | {mismatch.CmdbSysId,-20} | {Truncate(mismatch.CmsTitle, 40),-40} | {mismatch.CmsFipsId ?? "N/A",-15}");
        }

        Console.WriteLine(new string('-', 120));
        Console.WriteLine($"\nTotal: {mismatches.Count} products\n");

        // Also output as JSON for programmatic use
        Console.WriteLine("=== JSON Output ===");
        var json = JsonSerializer.Serialize(mismatches, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        Console.WriteLine(json);
    }

    private static string Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return "";
        return value.Length <= maxLength ? value : value.Substring(0, maxLength - 3) + "...";
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}

// Data models
public class CmdbEntryWithStatus
{
    [JsonPropertyName("sys_id")]
    public string SysId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("operational_status")]
    public string? OperationalStatus { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

public class CmsProduct
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("documentId")]
    public string? DocumentId { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("fips_id")]
    public string? FipsId { get; set; }

    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;

    [JsonPropertyName("cmdb_sys_id")]
    public string? CmdbSysId { get; set; }
}

public class MismatchEntry
{
    public string CmdbSysId { get; set; } = string.Empty;
    public string CmdbName { get; set; } = string.Empty;
    public string? CmdbOperationalStatus { get; set; }
    public int CmsProductId { get; set; }
    public string? CmsDocumentId { get; set; }
    public string CmsTitle { get; set; } = string.Empty;
    public string? CmsFipsId { get; set; }
    public string CmsState { get; set; } = string.Empty;
}

public class ServiceNowResponse<T>
{
    [JsonPropertyName("result")]
    public T? Result { get; set; }
}

public class CmsApiResponse
{
    [JsonPropertyName("data")]
    public List<CmsProduct>? Data { get; set; }

    [JsonPropertyName("meta")]
    public CmsMeta? Meta { get; set; }
}

public class CmsMeta
{
    [JsonPropertyName("pagination")]
    public CmsPagination? Pagination { get; set; }
}

public class CmsPagination
{
    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; }

    [JsonPropertyName("pageCount")]
    public int PageCount { get; set; }

    [JsonPropertyName("total")]
    public int Total { get; set; }
}
