using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Compass.Models.Fips;
using Microsoft.Extensions.Options;

namespace Compass.Services.Aiss;

public sealed class AissProductAccessibilityService : IAissProductAccessibilityService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly HttpClient _http;
    private readonly AissConfiguration _aiss;
    private readonly ILogger<AissProductAccessibilityService> _logger;

    public AissProductAccessibilityService(
        HttpClient http,
        IOptions<FipsSyncConfiguration> fipsSync,
        ILogger<AissProductAccessibilityService> logger)
    {
        _http = http;
        _aiss = fipsSync.Value.Aiss ?? new AissConfiguration();
        _logger = logger;
    }

    public async Task<FipsProductAissAccessibility> LoadForProductAsync(
        int registerNumericUniqueId,
        Guid registerProductId,
        CancellationToken cancellationToken = default)
    {
        var webBase = _aiss.ResolveWebBaseUrl();
        if (string.IsNullOrWhiteSpace(_aiss.Endpoint) || string.IsNullOrWhiteSpace(_aiss.ApiKey))
        {
            return new FipsProductAissAccessibility
            {
                AissWebBaseUrl = webBase,
                ErrorMessage =
                    "AISS is not configured. Set FipsSync:Aiss:Endpoint and FipsSync:Aiss:ApiKey in configuration."
            };
        }

        AissServiceAccessibilityApiResponse? payload = null;
        string? error = null;

        if (registerProductId != Guid.Empty)
        {
            (payload, error) = await GetAccessibilityAsync(
                $"v1/services/by-register/{registerProductId:N}/accessibility?openIssuesLimit=40",
                cancellationToken);
        }

        if (payload?.Service == null && registerNumericUniqueId > 0)
        {
            (payload, error) = await GetAccessibilityAsync(
                $"v1/services/by-register-unique-id/{registerNumericUniqueId}/accessibility?openIssuesLimit=40",
                cancellationToken);
        }

        if (payload?.Service == null)
        {
            if (error != null && error.Contains("502", StringComparison.Ordinal))
            {
                return new FipsProductAissAccessibility
                {
                    AissWebBaseUrl = webBase,
                    ErrorMessage = error
                };
            }

            return new FipsProductAissAccessibility
            {
                AissWebBaseUrl = webBase,
                NotFound = true,
                ErrorMessage = error
            };
        }

        return new FipsProductAissAccessibility
        {
            AissWebBaseUrl = webBase,
            Service = payload.Service,
            Summary = payload.Summary,
            OpenIssues = payload.OpenIssues?.Items ?? new List<AissServiceIssueDto>(),
            ErrorMessage = error
        };
    }

    private async Task<(AissServiceAccessibilityApiResponse? Data, string? ErrorMessage)> GetAccessibilityAsync(
        string relativePath,
        CancellationToken cancellationToken)
    {
        var (data, error) = await GetJsonAsync<AissServiceAccessibilityApiResponse>(relativePath, cancellationToken);
        return (data, error);
    }

    private async Task<(T? Data, string? ErrorMessage)> GetJsonAsync<T>(
        string relativePath,
        CancellationToken cancellationToken)
    {
        var baseUrl = _aiss.Endpoint?.Trim();
        if (string.IsNullOrEmpty(baseUrl))
            return (default, "AISS endpoint is not configured.");

        var url = $"{baseUrl.TrimEnd('/')}/{relativePath.TrimStart('/')}";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _aiss.ApiKey.Trim());

        try
        {
            var response = await _http.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return (default, null);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "AISS request failed for {Url}: {StatusCode}",
                    url,
                    response.StatusCode);
                return (default,
                    response.StatusCode == HttpStatusCode.Unauthorized
                        ? "The AISS API rejected the token."
                        : response.StatusCode == HttpStatusCode.BadGateway
                            ? "Could not load the COMPASS service register from AISS."
                            : $"Could not load data from AISS (HTTP {(int)response.StatusCode}).");
            }

            var data = JsonSerializer.Deserialize<T>(body, JsonOptions);
            return (data, null);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "AISS request timed out for {Url}", url);
            return (default, "The accessibility service did not respond in time.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "AISS request failed for {Url}", url);
            return (default, "Could not reach the accessibility (AISS) service.");
        }
    }
}
