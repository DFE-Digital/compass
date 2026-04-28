using System.Net.Http.Headers;
using System.Text.Json;
using System.Linq;

namespace Compass.Services;

public class ServiceAssessmentApiService : IServiceAssessmentApiService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ServiceAssessmentApiService> _logger;
    private readonly string _apiBaseUrl;
    private readonly string _apiToken;

    public ServiceAssessmentApiService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<ServiceAssessmentApiService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;

        _apiBaseUrl = _configuration["FipsSync:Sas:Endpoint"]
            ?? _configuration["ServiceAssessments:ApiUrl"]
            ?? "https://service-assessments.education.gov.uk/api/";

        if (!_apiBaseUrl.EndsWith("/", StringComparison.Ordinal))
        {
            _apiBaseUrl += "/";
        }

        _apiToken = (
            _configuration["FipsSync:Sas:SecretKey"]
            ?? _configuration["ServiceAssessments:ApiToken"]
            ?? string.Empty).Trim();

        if (string.IsNullOrEmpty(_apiToken))
        {
            _logger.LogWarning(
                "Service assessment API token is not configured. Set FipsSync:Sas:SecretKey or ServiceAssessments:ApiToken.");
        }
    }

    public async Task<ServiceAssessmentResponse?> GetActionsByStandardAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(_apiToken))
            {
                _logger.LogError("Service assessment API token is not configured. Cannot make request.");
                return null;
            }
            
            // Build full URL - use BaseAddress if set, otherwise use _apiBaseUrl
            var endpointPath = "assessments/actions/all";
            Uri baseUri;
            
            if (_httpClient.BaseAddress != null)
            {
                baseUri = _httpClient.BaseAddress;
            }
            else
            {
                baseUri = new Uri(_apiBaseUrl);
            }
            
            // Build the request URI
            var requestUri = new Uri(baseUri, endpointPath);
            
            // Create request
            var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            
            // Use Bearer token authentication (standard approach)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiToken);
            
            _logger.LogWarning("Calling service assessment API: {Url} with Bearer token (token length: {TokenLength})", 
                requestUri, _apiToken.Length);
            
            var response = await _httpClient.SendAsync(request);
            
            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Service assessment API returned status {StatusCode}: {ReasonPhrase}. Response: {ResponseBody}", 
                    response.StatusCode, response.ReasonPhrase, responseBody);
                return null;
            }

            var jsonContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ServiceAssessmentResponse>(jsonContent, JsonOptions);

            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error calling service assessment API: {BaseUrl}{Endpoint}", _apiBaseUrl, "assessments/actions/all");
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error deserializing service assessment API response");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error calling service assessment API");
            return null;
        }
    }

    public async Task<SasPublishedSummaryResponse?> GetPublishedSummaryAsync(
        CancellationToken cancellationToken = default) =>
        await GetSasJsonAsync<SasPublishedSummaryResponse>(
            "assessments/published/summary", cancellationToken);

    public async Task<SasActionsByStandardResponse?> GetPublishedActionsByStandardAsync(
        CancellationToken cancellationToken = default) =>
        await GetSasJsonAsync<SasActionsByStandardResponse>(
            "assessments/published/actions-by-standard", cancellationToken);

    private async Task<T?> GetSasJsonAsync<T>(string relativePath, CancellationToken cancellationToken) where T : class
    {
        try
        {
            if (string.IsNullOrEmpty(_apiToken))
            {
                _logger.LogError("Service assessment API token is not configured. Cannot make request.");
                return null;
            }

            var path = relativePath.TrimStart('/');
            var baseUri = _httpClient.BaseAddress ?? new Uri(_apiBaseUrl, UriKind.Absolute);
            var requestUri = new Uri(baseUri, path);
            var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiToken);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "Service assessment API {Path} returned {Status}: {Body}",
                    path, response.StatusCode, body);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling service assessment API path {Path}", relativePath);
            return null;
        }
    }
}

