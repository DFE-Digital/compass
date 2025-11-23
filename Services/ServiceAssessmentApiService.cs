using System.Net.Http.Headers;
using System.Text.Json;
using System.Linq;

namespace Compass.Services;

public class ServiceAssessmentApiService : IServiceAssessmentApiService
{
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
        
        // Get base URL from configuration (stored for logging purposes)
        _apiBaseUrl = _configuration["ServiceAssessments:ApiUrl"] ?? "https://service-assessments.education.gov.uk/api/";
        
        // Ensure base URL ends with /
        if (!_apiBaseUrl.EndsWith("/"))
        {
            _apiBaseUrl += "/";
        }
        
        // Get token from ServiceAssessments:ApiToken only (not FIPSPK_TOKEN)
        // Trim any whitespace that might cause authentication issues
        _apiToken = (_configuration["ServiceAssessments:ApiToken"] ?? string.Empty).Trim();
        
        // Note: BaseAddress is set in Program.cs via HttpClient configuration
        // User-Agent and Timeout are also set there
        
        if (string.IsNullOrEmpty(_apiToken))
        {
            _logger.LogWarning("Service assessment API token is not configured. Check ServiceAssessments:ApiToken in appsettings.");
        }
        else
        {
            _logger.LogInformation("Service assessment API token loaded (length: {TokenLength})", _apiToken.Length);
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
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            // The API returns a response with assessments array
            var result = JsonSerializer.Deserialize<ServiceAssessmentResponse>(jsonContent, options);

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
}

