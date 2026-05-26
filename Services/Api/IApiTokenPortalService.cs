using Compass.Models;

namespace Compass.Services.Api;

public interface IApiTokenPortalService
{
    static bool IsInternalUser(string? email) =>
        !string.IsNullOrWhiteSpace(email) &&
        email.Trim().EndsWith("@education.gov.uk", StringComparison.OrdinalIgnoreCase);

    Task<List<ApiToken>> GetAccessibleTokensAsync(string userEmail, CancellationToken cancellationToken = default);

    Task<bool> UserCanManageTokenAsync(string userEmail, int tokenId, CancellationToken cancellationToken = default);

    Task<List<int>> GetAccessibleTokenIdsAsync(string userEmail, CancellationToken cancellationToken = default);

    Task<ApiTokenRequestResult> SubmitRequestAsync(
        string requestorEmail,
        string environment,
        string projectSlug,
        string? justification,
        Dictionary<string, (bool read, bool create, bool update, bool delete)> permissions,
        CancellationToken cancellationToken = default);

    Task<ApiTokenRequestResult> ApproveRequestAsync(
        string reviewerEmail,
        int requestId,
        string? reviewNotes,
        CancellationToken cancellationToken = default);

    Task<ApiTokenRequestResult> RejectRequestAsync(
        string reviewerEmail,
        int requestId,
        string reviewNotes,
        CancellationToken cancellationToken = default);

    Task<RecycleTokenResult?> RecycleTokenAsync(
        string actorEmail,
        int tokenId,
        CancellationToken cancellationToken = default);

    Task<bool> AddMemberAsync(string actorEmail, int tokenId, string memberEmail, CancellationToken cancellationToken = default);

    Task<bool> RemoveMemberAsync(string actorEmail, int tokenId, string memberEmail, CancellationToken cancellationToken = default);

    Task<List<ApiTokenRequest>> GetPendingRequestsAsync(CancellationToken cancellationToken = default);

    Task<ApiTokenRequest?> GetRequestAsync(int requestId, CancellationToken cancellationToken = default);

    Task<List<ApiRequestLog>> GetLogsForUserAsync(string userEmail, int? tokenId, int take = 500, CancellationToken cancellationToken = default);

    Task<List<ExplorerTokenSummary>> GetExplorerTokenSummariesAsync(string userEmail, CancellationToken cancellationToken = default);

    Task<string?> GetExplorerBearerTokenAsync(string userEmail, int tokenId, CancellationToken cancellationToken = default);

    Task<ApiRequestLog?> GetLogForUserAsync(string userEmail, int logId, CancellationToken cancellationToken = default);

    Task NotifyTokenRecycledAsync(int tokenId, string newTokenValue, CancellationToken cancellationToken = default);
}

public sealed class ApiTokenRequestResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public ApiTokenRequest? Request { get; init; }
    public ApiToken? IssuedToken { get; init; }
    public string? IssuedTokenValue { get; init; }
    public bool RequiresAdminReview { get; init; }
}

public sealed class RecycleTokenResult
{
    public required ApiToken Token { get; init; }
    public required string NewTokenValue { get; init; }
}

public sealed class ExplorerTokenSummary
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public required string AccessTier { get; init; }
}
