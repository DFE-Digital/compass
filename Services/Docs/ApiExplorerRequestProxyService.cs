using System.Net.Http.Headers;
using Compass.Configuration;
using Microsoft.Extensions.Options;

namespace Compass.Services.Docs;

public sealed class ApiExplorerProxyRequest
{
    public string? BaseUrl { get; set; }
    public string Method { get; set; } = "GET";
    public string Path { get; set; } = "";
    public string? Body { get; set; }
    public string? Authorization { get; set; }
}

public sealed class ApiExplorerProxyResponse
{
    public int Status { get; init; }
    public string StatusText { get; init; } = "";
    public Dictionary<string, string> Headers { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public string Body { get; init; } = "";
}

public interface IApiExplorerRequestProxyService
{
    Task<ApiExplorerProxyResponse> ForwardAsync(
        ApiExplorerProxyRequest request,
        string currentRequestAuthority,
        CancellationToken cancellationToken);
}

public sealed class ApiExplorerRequestProxyService : IApiExplorerRequestProxyService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly DocsApiExplorerOptions _options;

    public ApiExplorerRequestProxyService(
        IHttpClientFactory httpClientFactory,
        IOptions<DocsApiExplorerOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
    }

    public async Task<ApiExplorerProxyResponse> ForwardAsync(
        ApiExplorerProxyRequest request,
        string currentRequestAuthority,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Path) || !request.Path.StartsWith('/'))
            throw new InvalidOperationException("Path must start with /.");

        if (!request.Path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Only /api/* paths are allowed.");

        var method = new HttpMethod(request.Method.ToUpperInvariant());
        if (method != HttpMethod.Get && method != HttpMethod.Head && method != HttpMethod.Post
            && method != HttpMethod.Put && method != HttpMethod.Patch && method != HttpMethod.Delete)
            throw new InvalidOperationException("HTTP method not allowed.");

        var targetBase = ResolveTargetBase(request.BaseUrl, currentRequestAuthority);
        var targetUri = new Uri(new Uri(targetBase.TrimEnd('/') + "/"), request.Path.TrimStart('/'));

        if (!IsAllowedTarget(targetUri, currentRequestAuthority))
            throw new InvalidOperationException("Target environment is not allowed.");

        using var httpRequest = new HttpRequestMessage(method, targetUri);
        if (!string.IsNullOrWhiteSpace(request.Authorization))
        {
            if (!AuthenticationHeaderValue.TryParse(request.Authorization, out var auth))
                throw new InvalidOperationException("Invalid Authorization header.");
            httpRequest.Headers.Authorization = auth;
        }

        if (request.Body != null
            && method != HttpMethod.Get
            && method != HttpMethod.Head
            && method != HttpMethod.Delete)
        {
            httpRequest.Content = new StringContent(request.Body, System.Text.Encoding.UTF8, "application/json");
        }

        var client = _httpClientFactory.CreateClient(nameof(ApiExplorerRequestProxyService));
        using var response = await client.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var header in response.Headers)
            headers[header.Key] = string.Join(", ", header.Value);
        foreach (var header in response.Content.Headers)
            headers[header.Key] = string.Join(", ", header.Value);

        return new ApiExplorerProxyResponse
        {
            Status = (int)response.StatusCode,
            StatusText = response.ReasonPhrase ?? "",
            Headers = headers,
            Body = body
        };
    }

    private string ResolveTargetBase(string? baseUrl, string currentRequestAuthority)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
            return "https://" + currentRequestAuthority.TrimStart('/');

        return baseUrl.Trim();
    }

    private bool IsAllowedTarget(Uri targetUri, string currentRequestAuthority)
    {
        var targetAuthority = targetUri.GetLeftPart(UriPartial.Authority);
        var current = "https://" + currentRequestAuthority.TrimStart('/');

        if (string.Equals(targetAuthority, current, StringComparison.OrdinalIgnoreCase))
            return true;

        return _options.AllConnectHosts().Any(h =>
            string.Equals(h.TrimEnd('/'), targetAuthority.TrimEnd('/'), StringComparison.OrdinalIgnoreCase));
    }
}
