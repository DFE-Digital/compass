using System.Text.Json;
using Compass.Services;

namespace Compass.Services;

public class AccessibilityIssuesService : IAccessibilityIssuesService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AccessibilityIssuesService> _logger;
    private readonly string _baseUrl;
    private readonly string _apiKey;

    public AccessibilityIssuesService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<AccessibilityIssuesService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        
        _baseUrl = _configuration["FipsSync:Aiss:Endpoint"] ?? "http://localhost:5418/api/";
        _apiKey = _configuration["FipsSync:Aiss:ApiKey"] ?? string.Empty;

        // Ensure base URL ends with /
        if (!_baseUrl.EndsWith("/"))
        {
            _baseUrl += "/";
        }

        // Configure HttpClient
        _httpClient.BaseAddress = new Uri(_baseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
        
        if (!string.IsNullOrEmpty(_apiKey))
        {
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
        }
    }

    public async Task<IssuesResponse?> GetIssuesByDocumentIdAsync(string documentId)
    {
        try
        {
            var endpoint = $"v1/issues/by-document/{Uri.EscapeDataString(documentId)}";
            var response = await _httpClient.GetAsync(endpoint);

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("No issues found for document ID: {DocumentId}", documentId);
                    return null;
                }

                response.EnsureSuccessStatusCode();
            }

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<IssuesResponseDto>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result?.Issues == null)
            {
                return new IssuesResponse { Count = 0, Issues = new List<IssueDto>() };
            }

            return new IssuesResponse
            {
                Count = result.Count,
                Issues = result.Issues
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching issues for document ID: {DocumentId}", documentId);
            throw;
        }
    }

    public async Task<IssuesResponse?> GetIssuesAsync(
        int? serviceId = null,
        string? documentId = null,
        string? status = null,
        string? wcagCriteria = null)
    {
        try
        {
            var queryParams = new List<string>();
            if (serviceId.HasValue)
                queryParams.Add($"serviceId={serviceId.Value}");
            if (!string.IsNullOrEmpty(documentId))
                queryParams.Add($"documentId={Uri.EscapeDataString(documentId)}");
            if (!string.IsNullOrEmpty(status))
                queryParams.Add($"status={Uri.EscapeDataString(status)}");
            if (!string.IsNullOrEmpty(wcagCriteria))
                queryParams.Add($"wcagCriteria={Uri.EscapeDataString(wcagCriteria)}");

            var queryString = queryParams.Any() ? "?" + string.Join("&", queryParams) : "";
            var endpoint = $"v1/issues{queryString}";
            var response = await _httpClient.GetAsync(endpoint);

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return new IssuesResponse { Count = 0, Issues = new List<IssueDto>() };
                }

                response.EnsureSuccessStatusCode();
            }

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<IssuesResponseDto>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result?.Issues == null)
            {
                return new IssuesResponse { Count = 0, Issues = new List<IssueDto>() };
            }

            return new IssuesResponse
            {
                Count = result.Count,
                Issues = result.Issues
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching issues");
            throw;
        }
    }

    public async Task<IssueDto?> GetIssueAsync(int id)
    {
        try
        {
            var endpoint = $"v1/issues/{id}";
            var response = await _httpClient.GetAsync(endpoint);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<IssueDto>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching issue {IssueId}", id);
            throw;
        }
    }

    public async Task<IssueSummaryDto?> GetIssueSummaryAsync(int serviceId)
    {
        try
        {
            var endpoint = $"v1/issues/summary/{serviceId}";
            var response = await _httpClient.GetAsync(endpoint);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<IssueSummaryDto>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching issue summary for service {ServiceId}", serviceId);
            throw;
        }
    }

    public async Task<ServicesResponse?> GetServicesAsync(bool? isActive = null, string? documentId = null)
    {
        try
        {
            var queryParams = new List<string>();
            if (isActive.HasValue)
                queryParams.Add($"isActive={isActive.Value.ToString().ToLowerInvariant()}");
            if (!string.IsNullOrEmpty(documentId))
                queryParams.Add($"documentId={Uri.EscapeDataString(documentId)}");

            var queryString = queryParams.Any() ? "?" + string.Join("&", queryParams) : "";
            var endpoint = $"v1/services{queryString}";
            var response = await _httpClient.GetAsync(endpoint);

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return new ServicesResponse { Count = 0, Services = new List<ServiceDto>() };
                }

                response.EnsureSuccessStatusCode();
            }

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ServicesResponseDto>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result?.Services == null)
            {
                return new ServicesResponse { Count = 0, Services = new List<ServiceDto>() };
            }

            return new ServicesResponse
            {
                Count = result.Count,
                Services = result.Services
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching services");
            throw;
        }
    }

    public async Task<ServiceDto?> GetServiceAsync(int id)
    {
        try
        {
            var endpoint = $"v1/services/{id}";
            var response = await _httpClient.GetAsync(endpoint);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ServiceDto>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching service {ServiceId}", id);
            throw;
        }
    }

    public async Task<ServiceDto?> GetServiceByDocumentIdAsync(string documentId)
    {
        try
        {
            var endpoint = $"v1/services/by-document/{Uri.EscapeDataString(documentId)}";
            var response = await _httpClient.GetAsync(endpoint);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ServiceDto>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching service by document ID: {DocumentId}", documentId);
            throw;
        }
    }

    public async Task<ServiceWithIssuesDto?> GetServiceWithIssuesAsync(
        int id,
        string? status = null,
        string? wcagCriteria = null)
    {
        try
        {
            var queryParams = new List<string>();
            if (!string.IsNullOrEmpty(status))
                queryParams.Add($"status={Uri.EscapeDataString(status)}");
            if (!string.IsNullOrEmpty(wcagCriteria))
                queryParams.Add($"wcagCriteria={Uri.EscapeDataString(wcagCriteria)}");

            var queryString = queryParams.Any() ? "?" + string.Join("&", queryParams) : "";
            var endpoint = $"v1/services/{id}/issues{queryString}";
            var response = await _httpClient.GetAsync(endpoint);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ServiceWithIssuesResponseDto>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result == null)
            {
                return null;
            }

            return new ServiceWithIssuesDto
            {
                Service = result.Service ?? new ServiceDto(),
                Issues = new IssuesResponse
                {
                    Count = result.Issues?.Count ?? 0,
                    Issues = result.Issues?.Items ?? new List<IssueDto>()
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching service with issues for service {ServiceId}", id);
            throw;
        }
    }

    public async Task<ServiceSummaryDto?> GetServiceSummaryAsync(int id)
    {
        try
        {
            var endpoint = $"v1/services/{id}/summary";
            var response = await _httpClient.GetAsync(endpoint);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ServiceSummaryDto>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching service summary for service {ServiceId}", id);
            throw;
        }
    }

    // Internal DTOs for deserialization
    private class IssuesResponseDto
    {
        public int Count { get; set; }
        public List<IssueDto> Issues { get; set; } = new();
    }

    private class ServicesResponseDto
    {
        public int Count { get; set; }
        public List<ServiceDto> Services { get; set; } = new();
    }

    private class ServiceWithIssuesResponseDto
    {
        public ServiceDto? Service { get; set; }
        public ServiceIssuesResponseDto? Issues { get; set; }
    }

    private class ServiceIssuesResponseDto
    {
        public int Count { get; set; }
        public List<IssueDto> Items { get; set; } = new();
    }
}
