using Compass.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;
using Microsoft.Kiota.Abstractions;
using System.Net.Mime;
using System.Net;
using Microsoft.Extensions.Caching.Memory;
using System.Net.Http.Headers;
using System.Text;

namespace Compass.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private static readonly byte[] TransparentPixel =
        Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMBCfZ0T/8AAAAASUVORK5CYII=");

    private readonly GraphServiceClient _graph;
    private readonly IUserDirectoryService _userDirectoryService;
    private readonly ILogger<UsersController> _logger;
    private readonly IMemoryCache _cache;
    private static readonly MemoryCacheEntryOptions CacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(45),
        SlidingExpiration = TimeSpan.FromSeconds(30),
        Size = 1
    };
    private static readonly MemoryCacheEntryOptions PhotoCacheOptions = new()
    {
        SlidingExpiration = TimeSpan.FromMinutes(30),
        Size = 1
    };
    private static readonly MemoryCacheEntryOptions PhotoThrottleCacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
        Size = 1
    };
    private static readonly CachedPhoto TransparentPlaceholder = new(TransparentPixel, "image/png");

    public UsersController(
        GraphServiceClient graph,
        IUserDirectoryService userDirectoryService,
        ILogger<UsersController> logger,
        IMemoryCache cache)
    {
        _graph = graph;
        _userDirectoryService = userDirectoryService;
        _logger = logger;
        _cache = cache;
    }

    // GET /api/users/search?q=term
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string q = "", [FromQuery] int? top = null)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return Ok(Array.Empty<object>());
        }

        var trimmedQuery = q.Trim();
        var tokenisedTerms = Tokenise(trimmedQuery);

        var searchExpression = BuildSearchExpression(tokenisedTerms);

        int? boundedTop = null;
        if (top.HasValue)
        {
            boundedTop = Math.Clamp(top.Value, 1, 100);
        }

        var domainFilter = BuildDomainFilter();
        var cacheKey = $"people-search::{searchExpression ?? trimmedQuery.ToLowerInvariant()}::{boundedTop ?? 0}";
        if (_cache.TryGetValue(cacheKey, out IReadOnlyList<object>? cachedResults) && cachedResults is not null)
        {
            return Ok(cachedResults);
        }

        try
        {
            var results = await ExecuteGraphSearchAsync(
                searchExpression,
                domainFilter,
                boundedTop);

            if (IsEmpty(results) && tokenisedTerms.Length > 0)
            {
                var fallbackFilter = BuildStartsWithFilter(tokenisedTerms[0], domainFilter);

                results = await ExecuteGraphSearchAsync(
                    searchExpression: null,
                    additionalFilter: fallbackFilter,
                    topLimit: boundedTop);
            }

            var payload = results?.Value?
                .Where(u => !string.IsNullOrWhiteSpace(u?.Id))
                .Select(u => new
                {
                    id = u!.Id,
                    name = u.DisplayName,
                    email = string.IsNullOrWhiteSpace(u.Mail) ? u.UserPrincipalName : u.Mail,
                    jobTitle = u.JobTitle
                })
                .ToArray() ?? Array.Empty<object>();

            if (payload.Length > 0)
            {
                _cache.Set(cacheKey, payload, CacheOptions);
            }

            return Ok(payload);
        }
        catch (ServiceException serviceException) when (serviceException.ResponseStatusCode == (int)HttpStatusCode.TooManyRequests)
        {
            var retryAfter = ExtractRetryAfterSeconds(serviceException.ResponseHeaders);
            _logger.LogWarning("Graph user search rate limited. Retry after {RetryAfter}s", retryAfter?.TotalSeconds ?? 0);
            return StatusCode(StatusCodes.Status429TooManyRequests, new
            {
                error = "graph_rate_limited",
                retryAfter = retryAfter?.TotalSeconds
            });
        }
        catch (ApiException apiException)
        {
            if (apiException is ODataError odataException && odataException.Error != null)
            {
                var code = odataException.Error.Code;
                var message = odataException.Error.Message;

                if (string.Equals(code, "Authorization_RequestDenied", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(code, "AccessDenied", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Graph search failed due to insufficient privileges: {Message}", message);
                    return StatusCode(StatusCodes.Status403Forbidden, new { error = "insufficient_privileges" });
                }

                _logger.LogError("Graph search failed: {Code} - {Message}", code, message);
                return StatusCode(StatusCodes.Status502BadGateway, new { error = "graph_error", details = message });
            }

            _logger.LogError(apiException, "Graph search failed with API exception");
            return StatusCode(StatusCodes.Status502BadGateway, new { error = "graph_api_exception" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error performing Graph user search");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "unexpected_error" });
        }
    }

    [HttpPost("select")]
    public async Task<IActionResult> Select([FromBody] UserSelectionRequest? request, CancellationToken cancellationToken)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.ObjectId))
        {
            return BadRequest(new { error = "invalid_request" });
        }

        if (!Guid.TryParse(request.ObjectId, out var objectId))
        {
            return BadRequest(new { error = "invalid_object_id" });
        }

        try
        {
            var user = await _userDirectoryService.EnsureUserAsync(objectId, cancellationToken);
            var response = new UserSelectionResponse(
                user.Id,
                user.Name,
                user.Email,
                user.JobTitle,
                user.PhotoUpdatedAt,
                user.AzureObjectId);

            return Ok(response);
        }
        catch (ServiceException serviceException) when (serviceException.ResponseStatusCode == (int)HttpStatusCode.TooManyRequests)
        {
            _logger.LogWarning("Graph throttled while selecting user {ObjectId}", objectId);
            return StatusCode(StatusCodes.Status429TooManyRequests, new { error = "graph_rate_limited" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ensure directory user {ObjectId}", objectId);
            return StatusCode(StatusCodes.Status502BadGateway, new { error = "user_directory_failure" });
        }
    }

    // GET /api/users/photo/{id}?size=48
    [HttpGet("photo/{id}")]
    public async Task<IActionResult> Photo(string id, [FromQuery] int size = 48)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return NotFound();
        }

        var supportedSizes = new HashSet<int> { 48, 64, 96, 120, 240, 360 };
        var requestedSize = supportedSizes.Contains(size) ? size : 48;
        var sizeKey = $"{requestedSize}x{requestedSize}";
        var cacheKey = BuildPhotoCacheKey(id, requestedSize);

        if (_cache.TryGetValue(cacheKey, out CachedPhoto? cachedPhoto) && cachedPhoto is not null)
        {
            return File(cachedPhoto.Bytes, cachedPhoto.ContentType);
        }

        try
        {
            var sizedStream = await _graph.Users[id].Photos[sizeKey].Content.GetAsync();
            var cachedSized = await CreateCachedPhotoAsync(sizedStream, MediaTypeNames.Image.Jpeg);
            if (cachedSized != null)
            {
                _cache.Set(cacheKey, cachedSized, PhotoCacheOptions);
                return File(cachedSized.Bytes, cachedSized.ContentType);
            }
        }
        catch (ODataError e) when (e.ResponseStatusCode == 404)
        {
            // Ignore and fall back
        }
        catch (ServiceException serviceException) when (serviceException.ResponseStatusCode == (int)HttpStatusCode.TooManyRequests)
        {
            var retryAfter = ExtractRetryAfterSeconds(serviceException.ResponseHeaders)?.TotalSeconds ?? 60;
            _logger.LogWarning("Graph user photo rate limited for {UserId}. Caching placeholder for {Seconds}s", id, retryAfter);
            _cache.Set(cacheKey, TransparentPlaceholder, PhotoThrottleCacheOptions);
            return File(TransparentPlaceholder.Bytes, TransparentPlaceholder.ContentType);
        }
        catch
        {
            // Ignore and fall back
        }

        try
        {
            var originalStream = await _graph.Users[id].Photo.Content.GetAsync();
            var cachedOriginal = await CreateCachedPhotoAsync(originalStream, MediaTypeNames.Image.Jpeg);
            if (cachedOriginal != null)
            {
                _cache.Set(cacheKey, cachedOriginal, PhotoCacheOptions);
                return File(cachedOriginal.Bytes, cachedOriginal.ContentType);
            }
        }
        catch (ServiceException serviceException) when (serviceException.ResponseStatusCode == (int)HttpStatusCode.TooManyRequests)
        {
            var retryAfter = ExtractRetryAfterSeconds(serviceException.ResponseHeaders)?.TotalSeconds ?? 60;
            _logger.LogWarning("Graph user photo (fallback) rate limited for {UserId}. Caching placeholder for {Seconds}s", id, retryAfter);
            _cache.Set(cacheKey, TransparentPlaceholder, PhotoThrottleCacheOptions);
            return File(TransparentPlaceholder.Bytes, TransparentPlaceholder.ContentType);
        }
        catch
        {
            // Ignore and return placeholder
        }

        _cache.Set(cacheKey, TransparentPlaceholder, PhotoCacheOptions);
        return File(TransparentPlaceholder.Bytes, TransparentPlaceholder.ContentType);
    }

    private async Task<UserCollectionResponse?> ExecuteGraphSearchAsync(
        string? searchExpression,
        string additionalFilter,
        int? topLimit)
    {
        return await _graph.Users.GetAsync(requestConfiguration =>
        {
            if (topLimit.HasValue)
            {
                requestConfiguration.QueryParameters.Top = topLimit.Value;
            }

            if (!string.IsNullOrEmpty(searchExpression))
            {
                requestConfiguration.Headers.Add("ConsistencyLevel", "eventual");
                requestConfiguration.QueryParameters.Search = searchExpression;
                requestConfiguration.QueryParameters.Count = true;
            }

            if (!string.IsNullOrWhiteSpace(additionalFilter))
            {
                requestConfiguration.QueryParameters.Filter = additionalFilter;
            }

            requestConfiguration.QueryParameters.Select = new[]
            {
                "id", "displayName", "userPrincipalName", "mail", "jobTitle"
            };
            requestConfiguration.QueryParameters.Orderby = new[] { "displayName" };
        });
    }

    private static bool IsEmpty(UserCollectionResponse? response)
    {
        return response?.Value == null || response.Value.Count == 0;
    }

    private static string[] Tokenise(string input) =>
        input
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(SanitiseSearchToken)
            .Where(token => !string.IsNullOrEmpty(token))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private static string? BuildSearchExpression(IReadOnlyCollection<string> tokens)
    {
        if (tokens.Count == 0)
        {
            return null;
        }

        var fields = new[] { "displayName", "givenName", "surname", "mail", "userPrincipalName" };
        var tokenClauses = tokens.Select(token =>
        {
            var fieldClauses = fields.Select(field => $"\"{field}:{token}\"");
            return $"({string.Join(" OR ", fieldClauses)})";
        });
        return string.Join(" AND ", tokenClauses);
    }

    private static string BuildDomainFilter()
        => "(endswith(mail,'@education.gov.uk') or endswith(userPrincipalName,'@education.gov.uk'))";

    private static string BuildStartsWithFilter(string token, string domainFilter)
    {
        var escapedToken = token.Replace("'", "''");
        var startsWithConditions = new[]
        {
            $"startsWith(displayName,'{escapedToken}')",
            $"startsWith(givenName,'{escapedToken}')",
            $"startsWith(surname,'{escapedToken}')",
            $"startsWith(mail,'{escapedToken}')",
            $"startsWith(userPrincipalName,'{escapedToken}')"
        };

        return $"{domainFilter} and ({string.Join(" or ", startsWithConditions)})";
    }

    private static TimeSpan? ExtractRetryAfterSeconds(HttpResponseHeaders? headers)
    {
        if (headers == null)
        {
            return null;
        }

        if (headers.RetryAfter?.Delta is TimeSpan delta)
        {
            return delta;
        }

        if (headers.TryGetValues("Retry-After", out var values))
        {
            var raw = values?.FirstOrDefault();
            if (raw != null && double.TryParse(raw, out var seconds))
            {
                return TimeSpan.FromSeconds(seconds);
            }
        }

        return null;
    }

    private static string BuildPhotoCacheKey(string userId, int size) => $"user-photo::{userId}::{size}";

    private async Task<CachedPhoto?> CreateCachedPhotoAsync(Stream? stream, string contentType)
    {
        if (stream == null)
        {
            return null;
        }

        await using (stream)
        {
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            var buffer = memoryStream.ToArray();
            if (buffer.Length == 0)
            {
                return null;
            }
            return new CachedPhoto(buffer, contentType);
        }
    }

    private static string SanitiseSearchToken(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var trimmed = value.Trim();
        var builder = new StringBuilder(trimmed.Length);
        foreach (var character in trimmed)
        {
            if (character is '"' or '\\')
            {
                continue;
            }

            builder.Append(character);
        }

        return builder.ToString();
    }

    private sealed record CachedPhoto(byte[] Bytes, string ContentType);
}

public record UserSelectionRequest(string ObjectId);

public record UserSelectionResponse(int Id, string Name, string Email, string? JobTitle, DateTime? PhotoUpdatedAt, string? ObjectId);

