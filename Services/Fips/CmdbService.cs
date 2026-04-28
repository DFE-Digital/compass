using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Compass.Models.Fips;
using Microsoft.Extensions.Options;

namespace Compass.Services.Fips;

public class CmdbService : ICmdbService
{
    private readonly HttpClient _httpClient;
    private readonly FipsSyncConfiguration _config;
    private readonly ILogger<CmdbService> _logger;
    /// <summary>Trimmed table API URL (no trailing slash), e.g. https://instance.service-now.com/api/now/table/service_offering</summary>
    private readonly string _serviceOfferingTableUrl;
    private readonly string _cmdbUsername;
    private readonly string _cmdbPassword;

    public CmdbService(
        HttpClient httpClient,
        IOptions<FipsSyncConfiguration> config,
        ILogger<CmdbService> logger)
    {
        _httpClient = httpClient;
        _config = config.Value;
        _logger = logger;

        _cmdbUsername = _config.Cmdb.Username?.Trim() ?? "";
        _cmdbPassword = _config.Cmdb.Password?.Trim() ?? "";
        _serviceOfferingTableUrl = (_config.Cmdb.Endpoint ?? "").Trim().TrimEnd('/');

        if (string.IsNullOrEmpty(_cmdbUsername) || string.IsNullOrEmpty(_cmdbPassword))
            _logger.LogWarning("CMDB Username or Password is missing; ServiceNow requests will return 401.");

        // Do not set BaseAddress: combining it with a query-only relative URI (?sysparm_...) is unreliable
        // when the configured endpoint has no trailing slash. Use absolute URLs per request instead.
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_cmdbUsername}:{_cmdbPassword}"));
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", credentials);
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
    }

    private string ServiceOfferingRequestUrl(string queryWithoutQuestionPrefix)
    {
        var q = queryWithoutQuestionPrefix.TrimStart('?');
        return string.IsNullOrEmpty(q) ? _serviceOfferingTableUrl : $"{_serviceOfferingTableUrl}?{q}";
    }

    private static List<CmdbEntry> DeserializeServiceOfferingResultRows(string content)
    {
        using var doc = JsonDocument.Parse(content);
        if (!doc.RootElement.TryGetProperty("result", out var resultEl))
            return new List<CmdbEntry>();

        var list = new List<CmdbEntry>();
        if (resultEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var row in resultEl.EnumerateArray())
            {
                var raw = row.GetRawText();
                var entry = JsonSerializer.Deserialize<CmdbEntry>(raw);
                if (entry != null)
                {
                    entry.RecordJson = raw;
                    list.Add(entry);
                }
            }
        }

        return list;
    }

    private static CmdbEntry? DeserializeServiceOfferingResultSingle(string content)
    {
        using var doc = JsonDocument.Parse(content);
        if (!doc.RootElement.TryGetProperty("result", out var resultEl))
            return null;
        if (resultEl.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            return null;

        var raw = resultEl.GetRawText();
        var entry = JsonSerializer.Deserialize<CmdbEntry>(raw);
        if (entry != null)
            entry.RecordJson = raw;
        return entry;
    }

    public async Task<List<CmdbEntry>> GetAllCmdbEntriesAsync()
    {
        try
        {
            _logger.LogInformation("Fetching CMDB entries from ServiceNow...");
            _logger.LogInformation("CMDB Endpoint: {Endpoint}", _serviceOfferingTableUrl);
            _logger.LogInformation("CMDB Username: {Username}", _cmdbUsername);

            var queryParams = new Dictionary<string, string>
            {
                ["sysparm_query"] = "active=true",
                ["sysparm_limit"] = "5000"
            };

            var query = string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
            var requestUrl = ServiceOfferingRequestUrl(query);
            _logger.LogInformation("CMDB request URL: {Url}", requestUrl);

            var response = await _httpClient.GetAsync(requestUrl);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("CMDB API Error: Status {StatusCode}, Content: {Content}", 
                    response.StatusCode, errorContent);
                throw new HttpRequestException($"CMDB API returned {response.StatusCode}: {errorContent}");
            }
            
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var entries = DeserializeServiceOfferingResultRows(content);
            _logger.LogInformation("Successfully fetched {Count} CMDB entries", entries.Count);

            return entries;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching CMDB entries");
            throw;
        }
    }

    public async Task<int> GetCmdbEntryCountAsync()
    {
        try
        {
            _logger.LogInformation("Fetching CMDB entry count...");

            var queryParams = new Dictionary<string, string>
            {
                ["sysparm_query"] = "active=true",
                ["sysparm_fields"] = "sys_id",
                ["sysparm_limit"] = "1"
            };

            var query = string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
            var response = await _httpClient.GetAsync(ServiceOfferingRequestUrl(query));
            response.EnsureSuccessStatusCode();

            // Try to get count from headers
            if (response.Headers.TryGetValues("X-Total-Count", out var totalCountValues))
            {
                var count = int.Parse(totalCountValues.First());
                _logger.LogInformation("Total CMDB entries: {Count}", count);
                return count;
            }

            // Fallback: parse from response
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ServiceNowResponse<List<CmdbEntry>>>(content);
            var count2 = result?.Result?.Count ?? 0;
            _logger.LogInformation("Total CMDB entries: {Count}", count2);
            return count2;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching CMDB entry count");
            throw;
        }
    }

    public async Task<CmdbPagedResult> GetCmdbEntriesPaginatedAsync(int limit = 100, int offset = 0)
    {
        try
        {
            var queryParams = new Dictionary<string, string>
            {
                ["sysparm_query"] = "active=true",
                ["sysparm_limit"] = limit.ToString(),
                ["sysparm_offset"] = offset.ToString()
            };

            var query = string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
            var response = await _httpClient.GetAsync(ServiceOfferingRequestUrl(query));
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var entries = DeserializeServiceOfferingResultRows(content);

            // Try to get total count from headers
            int total = 0;
            if (response.Headers.TryGetValues("X-Total-Count", out var totalCountValues))
            {
                total = int.Parse(totalCountValues.First());
            }

            return new CmdbPagedResult
            {
                Results = entries,
                Total = total,
                HasMore = entries.Count == limit
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching paginated CMDB entries");
            throw;
        }
    }

    public async Task<CmdbUser?> GetUserDetailsAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId))
            return null;

        try
        {
            // Change endpoint to sys_user table (same instance / API prefix as service_offering)
            var userEndpoint = _serviceOfferingTableUrl.Replace("/service_offering", "/sys_user", StringComparison.Ordinal);
            
            var queryParams = new Dictionary<string, string>
            {
                ["sysparm_fields"] = "sys_id,federated_id,name,last_name,first_name,email,active"
            };

            var query = string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
            var requestUri = $"{userEndpoint}/{userId}?{query}";
            
            var response = await _httpClient.GetAsync(requestUri);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ServiceNowResponse<CmdbUser>>(content);

            return result?.Result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error fetching user details for userId {UserId}", userId);
            return null;
        }
    }

    public async Task<CmdbServiceUsers> GetServiceOfferingUsersAsync(CmdbEntry entry)
    {
        var users = new CmdbServiceUsers();

        try
        {
            // Extract user IDs from the service offering
            // Note: CMDB returns these as objects with .value or as direct strings
            var serviceOwnerId = ExtractUserId(entry.OwnedBy);
            var productManagerId = ExtractUserId(entry.ProductManagerId);
            var deliveryManagerId = ExtractUserId(entry.DeliveryManagerId);
            var assetOwnerId = ExtractUserId(entry.InformationAssetOwnerId);
            var seniorOwnerId = ExtractUserId(entry.SeniorResponsibleOwnerId);

            // Fetch all user details in parallel
            var tasks = new List<Task<(string role, CmdbUser? user)>>();

            if (!string.IsNullOrEmpty(serviceOwnerId))
            {
                tasks.Add(FetchUserWithRole("ServiceOwner", serviceOwnerId));
            }

            if (!string.IsNullOrEmpty(productManagerId))
            {
                tasks.Add(FetchUserWithRole("ProductManager", productManagerId));
            }

            if (!string.IsNullOrEmpty(deliveryManagerId))
            {
                tasks.Add(FetchUserWithRole("DeliveryManager", deliveryManagerId));
            }

            if (!string.IsNullOrEmpty(assetOwnerId))
            {
                tasks.Add(FetchUserWithRole("InformationAssetOwner", assetOwnerId));
            }

            if (!string.IsNullOrEmpty(seniorOwnerId))
            {
                tasks.Add(FetchUserWithRole("SeniorResponsibleOwner", seniorOwnerId));
            }

            var results = await Task.WhenAll(tasks);

            foreach (var (role, user) in results)
            {
                if (user != null)
                {
                    switch (role)
                    {
                        case "ServiceOwner":
                            users.ServiceOwner = user;
                            break;
                        case "ProductManager":
                            users.ProductManager = user;
                            break;
                        case "DeliveryManager":
                            users.DeliveryManager = user;
                            break;
                        case "InformationAssetOwner":
                            users.InformationAssetOwner = user;
                            break;
                        case "SeniorResponsibleOwner":
                            users.SeniorResponsibleOwner = user;
                            break;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching service offering users for {EntryName}", entry.Name);
        }

        return users;
    }

    /// <summary>
    /// Extract user ID from CMDB field which can be a string or an object with .value property
    /// </summary>
    private string? ExtractUserId(object? field)
    {
        if (field == null) return null;
        
        if (field is string str)
        {
            return string.IsNullOrWhiteSpace(str) ? null : str;
        }
        
        // Try to extract from JsonElement if it's a JSON object
        if (field is System.Text.Json.JsonElement jsonElement)
        {
            if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.String)
            {
                var strValue = jsonElement.GetString();
                return string.IsNullOrWhiteSpace(strValue) ? null : strValue;
            }
            
            if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.Object)
            {
                if (jsonElement.TryGetProperty("value", out var valueElement))
                {
                    if (valueElement.ValueKind == System.Text.Json.JsonValueKind.String)
                    {
                        var strValue = valueElement.GetString();
                        return string.IsNullOrWhiteSpace(strValue) ? null : strValue;
                    }
                }
            }
        }
        
        // Try to serialize and deserialize as a dynamic object to extract value
        try
        {
            var json = JsonSerializer.Serialize(field);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            
            if (root.ValueKind == System.Text.Json.JsonValueKind.String)
            {
                var strValue = root.GetString();
                return string.IsNullOrWhiteSpace(strValue) ? null : strValue;
            }
            
            if (root.ValueKind == System.Text.Json.JsonValueKind.Object)
            {
                if (root.TryGetProperty("value", out var valueElement))
                {
                    var strValue = valueElement.GetString();
                    return string.IsNullOrWhiteSpace(strValue) ? null : strValue;
                }
            }
        }
        catch
        {
            // If serialization fails, return null
        }
        
        return null;
    }

    private async Task<(string role, CmdbUser? user)> FetchUserWithRole(string role, string userId)
    {
        var user = await GetUserDetailsAsync(userId);
        return (role, user);
    }

    public async Task<CmdbEntry?> GetServiceOfferingBySysIdAsync(string sysId)
    {
        if (string.IsNullOrEmpty(sysId))
            return null;

        try
        {
            // ServiceNow API format: GET /api/now/table/{table_name}/{sys_id}
            // The endpoint is already configured as service_offering (e.g., https://dfe.service-now.com/api/now/table/service_offering)
            // We just need to append /{sys_id} to get the specific record
            var requestUri = $"{_serviceOfferingTableUrl}/{sysId}";
            
            // Create a temporary HttpClient for this request since we're using a different base URI
            using var tempClient = new HttpClient();
            var credentials = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{_cmdbUsername}:{_cmdbPassword}"));
            tempClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Basic", credentials);
            tempClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            
            var response = await tempClient.GetAsync(requestUri);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("CMDB API Error for sys_id {SysId}: Status {StatusCode}, Content: {Content}", 
                    sysId, response.StatusCode, errorContent);
                return null;
            }
            
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return DeserializeServiceOfferingResultSingle(content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching CMDB entry by sys_id {SysId}", sysId);
            return null;
        }
    }
}
