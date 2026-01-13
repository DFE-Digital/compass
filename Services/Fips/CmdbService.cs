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

    public CmdbService(
        HttpClient httpClient,
        IOptions<FipsSyncConfiguration> config,
        ILogger<CmdbService> logger)
    {
        _httpClient = httpClient;
        _config = config.Value;
        _logger = logger;

        // Configure HTTP client with basic auth
        var credentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{_config.Cmdb.Username}:{_config.Cmdb.Password}"));
        _httpClient.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Basic", credentials);
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.BaseAddress = new Uri(_config.Cmdb.Endpoint);
    }

    public async Task<List<CmdbEntry>> GetAllCmdbEntriesAsync()
    {
        try
        {
            _logger.LogInformation("Fetching CMDB entries from ServiceNow...");
            _logger.LogInformation("CMDB Endpoint: {Endpoint}", _config.Cmdb.Endpoint);
            _logger.LogInformation("CMDB Username: {Username}", _config.Cmdb.Username);

            var queryParams = new Dictionary<string, string>
            {
                ["sysparm_query"] = "active=true",
                ["sysparm_fields"] = "name,sys_id,parent.name,description,u_delivery_manager,u_information_asset_owner,u_senior_responsible_owner",
                ["sysparm_limit"] = "5000"
            };

            var query = string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
            var requestUri = $"?{query}";
            
            _logger.LogInformation("Request URI: {BaseAddress}{RequestUri}", _httpClient.BaseAddress, requestUri);
            
            var response = await _httpClient.GetAsync(requestUri);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("CMDB API Error: Status {StatusCode}, Content: {Content}", 
                    response.StatusCode, errorContent);
                throw new HttpRequestException($"CMDB API returned {response.StatusCode}: {errorContent}");
            }
            
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ServiceNowResponse<List<CmdbEntry>>>(content);

            var entries = result?.Result ?? new List<CmdbEntry>();
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
            var response = await _httpClient.GetAsync($"?{query}");
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
                ["sysparm_fields"] = "name,sys_id,parent.name,description,u_delivery_manager,u_information_asset_owner,u_senior_responsible_owner",
                ["sysparm_limit"] = limit.ToString(),
                ["sysparm_offset"] = offset.ToString()
            };

            var query = string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
            var response = await _httpClient.GetAsync($"?{query}");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ServiceNowResponse<List<CmdbEntry>>>(content);

            var entries = result?.Result ?? new List<CmdbEntry>();

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
            // Change endpoint to sys_user table
            var userEndpoint = _config.Cmdb.Endpoint.Replace("/service_offering", "/sys_user");
            
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
            // Fetch all user details in parallel
            var tasks = new List<Task<(string role, CmdbUser? user)>>();

            if (!string.IsNullOrEmpty(entry.DeliveryManagerId))
            {
                tasks.Add(FetchUserWithRole("DeliveryManager", entry.DeliveryManagerId));
            }

            if (!string.IsNullOrEmpty(entry.InformationAssetOwnerId))
            {
                tasks.Add(FetchUserWithRole("InformationAssetOwner", entry.InformationAssetOwnerId));
            }

            if (!string.IsNullOrEmpty(entry.SeniorResponsibleOwnerId))
            {
                tasks.Add(FetchUserWithRole("SeniorResponsibleOwner", entry.SeniorResponsibleOwnerId));
            }

            var results = await Task.WhenAll(tasks);

            foreach (var (role, user) in results)
            {
                if (user != null)
                {
                    switch (role)
                    {
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

    private async Task<(string role, CmdbUser? user)> FetchUserWithRole(string role, string userId)
    {
        var user = await GetUserDetailsAsync(userId);
        return (role, user);
    }
}
