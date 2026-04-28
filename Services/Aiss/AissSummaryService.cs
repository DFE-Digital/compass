using System.Net.Http.Headers;
using System.Text.Json;
using Compass.Models.Fips;
using Microsoft.Extensions.Options;

namespace Compass.Services.Aiss;

public sealed class AissSummaryService : IAissSummaryService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly HttpClient _http;
    private readonly AissConfiguration _aiss;
    private readonly ILogger<AissSummaryService> _logger;

    public AissSummaryService(
        HttpClient http,
        IOptions<FipsSyncConfiguration> fipsSync,
        ILogger<AissSummaryService> logger)
    {
        _http = http;
        _aiss = fipsSync.Value.Aiss ?? new AissConfiguration();
        _logger = logger;
    }

    public async Task<(AissPlatformSummary? Summary, string? ErrorMessage)> GetSummaryAsync(
        CancellationToken cancellationToken = default)
    {
        var url = BuildSummaryRequestUri();
        if (string.IsNullOrEmpty(url))
            return (null, "AISS is not configured. Set FipsSync:Aiss:Endpoint and FipsSync:Aiss:ApiKey in configuration.");

        if (string.IsNullOrWhiteSpace(_aiss.ApiKey))
            return (null, "AISS API key is not configured (FipsSync:Aiss:ApiKey).");

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _aiss.ApiKey.Trim());
        try
        {
            var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "AISS summary request failed: {StatusCode} {Body}", response.StatusCode,
                    body.Length > 200 ? body[..200] : body);
                return (null,
                    response.StatusCode == System.Net.HttpStatusCode.Unauthorized
                        ? "The AISS API rejected the token. Check that FipsSync:Aiss:ApiKey matches a valid service token in AISS."
                        : $"Could not load accessibility summary (HTTP {(int)response.StatusCode}).");
            }

            var summary = JsonSerializer.Deserialize<AissPlatformSummary>(body, JsonOptions);
            return (summary, null);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "AISS summary request timed out.");
            return (null, "The accessibility service did not respond in time. Check that the AISS app is running and the endpoint URL is correct.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "AISS summary request failed.");
            return (null, "Could not reach the accessibility (AISS) service. Check FipsSync:Aiss:Endpoint and network access.");
        }
    }

    private string? BuildSummaryRequestUri()
    {
        var baseUrl = _aiss.Endpoint?.Trim();
        if (string.IsNullOrEmpty(baseUrl))
            return null;
        baseUrl = baseUrl.TrimEnd('/');
        return $"{baseUrl}/v1/summary";
    }

    public async Task<(AissCriterionTrends? Trends, string? ErrorMessage)> GetCriterionTrendsAsync(
        int months = 12,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = _aiss.Endpoint?.Trim();
        if (string.IsNullOrEmpty(baseUrl))
            return (null, "AISS is not configured. Set FipsSync:Aiss:Endpoint and FipsSync:Aiss:ApiKey in configuration.");

        if (string.IsNullOrWhiteSpace(_aiss.ApiKey))
            return (null, "AISS API key is not configured (FipsSync:Aiss:ApiKey).");

        months = Math.Clamp(months, 3, 24);
        var url = $"{baseUrl.TrimEnd('/')}/v1/summary/trends?months={months}";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _aiss.ApiKey.Trim());
        try
        {
            var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("AISS trends request failed: {StatusCode} {Body}", response.StatusCode,
                    body.Length > 200 ? body[..200] : body);
                return (null,
                    response.StatusCode == System.Net.HttpStatusCode.Unauthorized
                        ? "The AISS API rejected the token. Check that FipsSync:Aiss:ApiKey matches a valid service token in AISS."
                        : $"Could not load accessibility trends (HTTP {(int)response.StatusCode}).");
            }

            var trends = JsonSerializer.Deserialize<AissCriterionTrends>(body, JsonOptions);
            return (trends, null);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "AISS trends request timed out.");
            return (null, "The accessibility service did not respond in time. Check that the AISS app is running and the endpoint URL is correct.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "AISS trends request failed.");
            return (null, "Could not reach the accessibility (AISS) service. Check FipsSync:Aiss:Endpoint and network access.");
        }
    }
}
