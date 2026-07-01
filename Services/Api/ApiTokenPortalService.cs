using System.Text.Json;
using Compass.Data;
using Compass.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

namespace Compass.Services.Api;

public class ApiTokenPortalService : IApiTokenPortalService
{
    private const string DesignOpsEmail = "design.ops@education.gov.uk";

    private readonly CompassDbContext _context;
    private readonly IApiTokenService _apiTokenService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<ApiTokenPortalService> _logger;
    private readonly IConfiguration _configuration;

    public ApiTokenPortalService(
        CompassDbContext context,
        IApiTokenService apiTokenService,
        INotificationService notificationService,
        ILogger<ApiTokenPortalService> logger,
        IConfiguration configuration)
    {
        _context = context;
        _apiTokenService = apiTokenService;
        _notificationService = notificationService;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<List<ApiToken>> GetAccessibleTokensAsync(string userEmail, CancellationToken cancellationToken = default)
    {
        var email = NormalizeEmail(userEmail);
        var memberTokenIds = await _context.ApiTokenMembers
            .AsNoTracking()
            .Where(m => m.UserEmail == email)
            .Select(m => m.ApiTokenId)
            .ToListAsync(cancellationToken);

        return await _context.ApiTokens
            .AsNoTracking()
            .Include(t => t.Permissions)
            .Include(t => t.Members)
            .Where(t => t.OwnerEmail == email || memberTokenIds.Contains(t.Id))
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> UserCanManageTokenAsync(string userEmail, int tokenId, CancellationToken cancellationToken = default)
    {
        var email = NormalizeEmail(userEmail);
        var token = await _context.ApiTokens.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tokenId, cancellationToken);
        if (token == null)
            return false;
        if (string.Equals(token.OwnerEmail, email, StringComparison.OrdinalIgnoreCase))
            return true;
        return await _context.ApiTokenMembers.AnyAsync(
            m => m.ApiTokenId == tokenId && m.UserEmail == email,
            cancellationToken);
    }

    public async Task<List<int>> GetAccessibleTokenIdsAsync(string userEmail, CancellationToken cancellationToken = default)
    {
        var tokens = await GetAccessibleTokensAsync(userEmail, cancellationToken);
        return tokens.Select(t => t.Id).ToList();
    }

    public async Task<ApiTokenRequestResult> SubmitRequestAsync(
        string requestorEmail,
        string environment,
        string projectSlug,
        string? justification,
        Dictionary<string, (bool read, bool create, bool update, bool delete)> permissions,
        CancellationToken cancellationToken = default)
    {
        var email = NormalizeEmail(requestorEmail);
        if (!ApiTokenNaming.IsValidEnvironment(environment))
            return Fail("Environment must be DEV, TEST or PROD.");
        if (!ApiTokenNaming.TryNormalizeProjectSlug(projectSlug, out var slug, out var slugError))
            return Fail(slugError!);
        if (ApiTokenResourceCatalog.HasDelete(permissions))
            return Fail($"Delete access is not available through self-service. Contact {DesignOpsEmail} with details if you need delete permissions.");

        var normalizedEnv = environment.Trim().ToUpperInvariant();
        var tier = ApiTokenNaming.ResolveAccessTier(permissions);
        var tokenName = ApiTokenNaming.BuildTokenName(normalizedEnv, tier, slug);
        if (await _context.ApiTokens.AnyAsync(t => t.Name == tokenName, cancellationToken))
            return Fail($"A token named \"{tokenName}\" already exists. Choose a different project name or environment.");

        var isReadOnlyAll = ApiTokenResourceCatalog.IsReadOnlyAllData(permissions);
        var request = new ApiTokenRequest
        {
            RequestorEmail = email,
            Environment = normalizedEnv,
            ProjectSlug = slug,
            Justification = justification?.Trim(),
            PermissionsJson = SerializePermissions(permissions),
            IsReadOnlyAllData = isReadOnlyAll,
            Status = ApiTokenRequestStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        var autoApprove = IApiTokenPortalService.IsInternalUser(email) && isReadOnlyAll;
        if (autoApprove)
        {
            var issued = await IssueTokenFromRequestAsync(request, permissions, email, autoApproved: true, cancellationToken);
            if (issued.IssuedToken == null)
                return issued;
            request.Status = ApiTokenRequestStatus.AutoApproved;
            request.ReviewedByEmail = "system";
            request.ReviewedAt = DateTime.UtcNow;
            request.IssuedApiTokenId = issued.IssuedToken.Id;
            _context.ApiTokenRequests.Add(request);
            await _context.SaveChangesAsync(cancellationToken);
            await SendTokenIssuedEmailAsync(email, issued.IssuedToken!, issued.IssuedTokenValue!, cancellationToken);
            return new ApiTokenRequestResult
            {
                Success = true,
                Request = request,
                IssuedToken = issued.IssuedToken,
                IssuedTokenValue = issued.IssuedTokenValue,
                RequiresAdminReview = false
            };
        }

        _context.ApiTokenRequests.Add(request);
        await _context.SaveChangesAsync(cancellationToken);
        await SendRequestQueuedEmailAsync(email, request, cancellationToken);
        await NotifyAdminsOfPendingRequestAsync(request, cancellationToken);

        return new ApiTokenRequestResult
        {
            Success = true,
            Request = request,
            RequiresAdminReview = true
        };
    }

    public async Task<ApiTokenRequestResult> ApproveRequestAsync(
        string reviewerEmail,
        int requestId,
        string? reviewNotes,
        CancellationToken cancellationToken = default)
    {
        var request = await _context.ApiTokenRequests.FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken);
        if (request == null)
            return Fail("Request not found.");
        if (request.Status is ApiTokenRequestStatus.Approved or ApiTokenRequestStatus.AutoApproved)
            return Fail("This request has already been approved.");
        if (request.Status == ApiTokenRequestStatus.Rejected)
            return Fail("This request was rejected.");

        var permissions = DeserializePermissions(request.PermissionsJson);
        if (ApiTokenResourceCatalog.HasDelete(permissions) && !IApiTokenPortalService.IsInternalUser(request.RequestorEmail))
            return Fail($"Delete permissions can only be issued by admins. Contact {DesignOpsEmail} if the requestor needs delete access.");

        var tier = ApiTokenNaming.ResolveAccessTier(permissions);
        var tokenName = ApiTokenNaming.BuildTokenName(request.Environment, tier, request.ProjectSlug);
        if (await _context.ApiTokens.AnyAsync(t => t.Name == tokenName, cancellationToken))
            return Fail($"A token named \"{tokenName}\" already exists.");

        try
        {
            var issued = await IssueTokenFromRequestAsync(request, permissions, reviewerEmail, autoApproved: false, cancellationToken);
            if (!issued.Success || issued.IssuedToken == null)
                return issued;

            request.Status = ApiTokenRequestStatus.Approved;
            request.ReviewedByEmail = NormalizeEmail(reviewerEmail);
            request.ReviewedAt = DateTime.UtcNow;
            request.ReviewNotes = reviewNotes?.Trim();
            request.IssuedApiTokenId = issued.IssuedToken.Id;
            await _context.SaveChangesAsync(cancellationToken);

            await SendTokenIssuedEmailAsync(request.RequestorEmail, issued.IssuedToken, issued.IssuedTokenValue!, cancellationToken);
            await SendRequestApprovedEmailAsync(request, cancellationToken);

            return issued;
        }
        catch (DbUpdateException ex) when (IsDuplicateTokenName(ex))
        {
            return Fail($"A token named \"{tokenName}\" already exists. Remove or rename the existing token before approving this request.");
        }
    }

    public async Task<ApiTokenRequestResult> RejectRequestAsync(
        string reviewerEmail,
        int requestId,
        string reviewNotes,
        CancellationToken cancellationToken = default)
    {
        var request = await _context.ApiTokenRequests.FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken);
        if (request == null)
            return Fail("Request not found.");
        if (request.Status is ApiTokenRequestStatus.Approved or ApiTokenRequestStatus.AutoApproved)
            return Fail("This request has already been approved.");
        if (request.Status == ApiTokenRequestStatus.Rejected)
            return Fail("This request was already rejected.");

        request.Status = ApiTokenRequestStatus.Rejected;
        request.ReviewedByEmail = NormalizeEmail(reviewerEmail);
        request.ReviewedAt = DateTime.UtcNow;
        request.ReviewNotes = reviewNotes.Trim();
        await _context.SaveChangesAsync(cancellationToken);
        await SendRequestRejectedEmailAsync(request, cancellationToken);
        return new ApiTokenRequestResult { Success = true, Request = request };
    }

    public async Task<RecycleTokenResult?> RecycleTokenAsync(
        string actorEmail,
        int tokenId,
        CancellationToken cancellationToken = default)
    {
        if (!await UserCanManageTokenAsync(actorEmail, tokenId, cancellationToken))
            return null;

        var token = await _apiTokenService.GetByIdAsync(tokenId);
        if (token == null)
            return null;

        var newValue = await _apiTokenService.RecycleTokenAsync(tokenId);
        token = await _apiTokenService.GetByIdAsync(tokenId);
        if (token == null)
            return null;

        var ownerEmail = token.OwnerEmail ?? token.CreatedByEmail;
        await SendTokenRecycledEmailAsync(ownerEmail, token, newValue, cancellationToken);

        return new RecycleTokenResult { Token = token, NewTokenValue = newValue };
    }

    public async Task<bool> AddMemberAsync(string actorEmail, int tokenId, string memberEmail, CancellationToken cancellationToken = default)
    {
        if (!await UserCanManageTokenAsync(actorEmail, tokenId, cancellationToken))
            return false;

        var member = NormalizeEmail(memberEmail);
        if (string.IsNullOrEmpty(member) || !member.Contains('@'))
            return false;

        var exists = await _context.ApiTokenMembers.AnyAsync(
            m => m.ApiTokenId == tokenId && m.UserEmail == member,
            cancellationToken);
        if (exists)
            return true;

        _context.ApiTokenMembers.Add(new ApiTokenMember
        {
            ApiTokenId = tokenId,
            UserEmail = member,
            AddedByEmail = NormalizeEmail(actorEmail),
            AddedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> RemoveMemberAsync(string actorEmail, int tokenId, string memberEmail, CancellationToken cancellationToken = default)
    {
        var token = await _context.ApiTokens.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tokenId, cancellationToken);
        if (token == null)
            return false;
        if (!await UserCanManageTokenAsync(actorEmail, tokenId, cancellationToken))
            return false;

        var member = NormalizeEmail(memberEmail);
        if (string.Equals(token.OwnerEmail, member, StringComparison.OrdinalIgnoreCase))
            return false;

        var row = await _context.ApiTokenMembers.FirstOrDefaultAsync(
            m => m.ApiTokenId == tokenId && m.UserEmail == member,
            cancellationToken);
        if (row == null)
            return false;

        _context.ApiTokenMembers.Remove(row);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public Task<List<ApiTokenRequest>> GetPendingRequestsAsync(CancellationToken cancellationToken = default) =>
        _context.ApiTokenRequests
            .AsNoTracking()
            .Where(r => r.Status == ApiTokenRequestStatus.Pending)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<ApiTokenRequest?> GetRequestAsync(int requestId, CancellationToken cancellationToken = default) =>
        await _context.ApiTokenRequests
            .AsNoTracking()
            .Include(r => r.IssuedApiToken)
            .FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken);

    public async Task<List<ApiRequestLog>> GetLogsForUserAsync(
        string userEmail,
        int? tokenId,
        int take = 500,
        CancellationToken cancellationToken = default)
    {
        var ids = await GetAccessibleTokenIdsAsync(userEmail, cancellationToken);
        if (ids.Count == 0)
            return new List<ApiRequestLog>();

        if (tokenId.HasValue)
        {
            if (!ids.Contains(tokenId.Value))
                return new List<ApiRequestLog>();
            ids = new List<int> { tokenId.Value };
        }

        return await _context.ApiRequestLogs
            .AsNoTracking()
            .Include(l => l.ApiToken)
            .Where(l => ids.Contains(l.ApiTokenId))
            .OrderByDescending(l => l.RequestTimestamp)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task NotifyTokenRecycledAsync(int tokenId, string newTokenValue, CancellationToken cancellationToken = default)
    {
        var token = await _apiTokenService.GetByIdAsync(tokenId);
        if (token == null)
            return;
        var ownerEmail = token.OwnerEmail ?? token.CreatedByEmail;
        if (!string.IsNullOrEmpty(ownerEmail))
            await SendTokenRecycledEmailAsync(ownerEmail, token, newTokenValue, cancellationToken);
    }

    public async Task<List<ExplorerTokenSummary>> GetExplorerTokenSummariesAsync(
        string userEmail,
        CancellationToken cancellationToken = default)
    {
        var tokens = await GetAccessibleTokensAsync(userEmail, cancellationToken);
        return tokens
            .Where(t => t.IsActive && (!t.ExpiresAt.HasValue || t.ExpiresAt > DateTime.UtcNow))
            .Select(t => new ExplorerTokenSummary
            {
                Id = t.Id,
                Name = t.Name,
                AccessTier = t.AccessTier ?? ApiTokenNaming.ResolveAccessTier(
                    t.Permissions.ToDictionary(p => p.Resource, p => (p.CanRead, p.CanCreate, p.CanUpdate, p.CanDelete)))
            })
            .ToList();
    }

    public async Task<string?> GetExplorerBearerTokenAsync(
        string userEmail,
        int tokenId,
        CancellationToken cancellationToken = default)
    {
        if (!await UserCanManageTokenAsync(userEmail, tokenId, cancellationToken))
            return null;

        var token = await _context.ApiTokens.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tokenId && t.IsActive, cancellationToken);
        if (token == null)
            return null;
        if (token.ExpiresAt.HasValue && token.ExpiresAt.Value < DateTime.UtcNow)
            return null;

        return token.Token;
    }

    public async Task<ApiRequestLog?> GetLogForUserAsync(
        string userEmail,
        int logId,
        CancellationToken cancellationToken = default)
    {
        var ids = await GetAccessibleTokenIdsAsync(userEmail, cancellationToken);
        if (ids.Count == 0)
            return null;

        return await _context.ApiRequestLogs
            .AsNoTracking()
            .Include(l => l.ApiToken)
            .FirstOrDefaultAsync(l => l.Id == logId && ids.Contains(l.ApiTokenId), cancellationToken);
    }

    private async Task<ApiTokenRequestResult> IssueTokenFromRequestAsync(
        ApiTokenRequest request,
        Dictionary<string, (bool read, bool create, bool update, bool delete)> permissions,
        string actorEmail,
        bool autoApproved,
        CancellationToken cancellationToken)
    {
        var tier = ApiTokenNaming.ResolveAccessTier(permissions);
        var name = ApiTokenNaming.BuildTokenName(request.Environment, tier, request.ProjectSlug);
        var description = BuildDescription(request, autoApproved);

        var created = await _apiTokenService.CreateTokenAsync(
            name,
            description,
            actorEmail,
            expiresAt: null);

        var entity = await _context.ApiTokens.FirstAsync(t => t.Id == created.Id, cancellationToken);
        entity.OwnerEmail = request.RequestorEmail;
        entity.Environment = request.Environment;
        entity.ProjectSlug = request.ProjectSlug;
        entity.AccessTier = tier;
        entity.IsSelfService = true;
        await _context.SaveChangesAsync(cancellationToken);

        await _apiTokenService.SetPermissionsAsync(created.Id, permissions);

        var withPerms = await _apiTokenService.GetByIdAsync(created.Id);
        return new ApiTokenRequestResult
        {
            Success = true,
            IssuedToken = withPerms,
            IssuedTokenValue = created.Token
        };
    }

    private static string BuildDescription(ApiTokenRequest request, bool autoApproved)
    {
        var via = autoApproved ? "auto-approved read-only request" : "approved API key request";
        var justification = string.IsNullOrWhiteSpace(request.Justification) ? "" : $" — {request.Justification}";
        var description = $"{request.Environment} self-service token ({via}){justification}";
        return description.Length <= 1000 ? description : description[..1000];
    }

    private static bool IsDuplicateTokenName(DbUpdateException ex) =>
        ex.InnerException is SqlException { Number: 2601 or 2627 };

    private async Task SendTokenIssuedEmailAsync(
        string recipientEmail,
        ApiToken token,
        string tokenValue,
        CancellationToken cancellationToken)
    {
        var baseUrl = _configuration["Compass:BaseUrl"]?.TrimEnd('/') ?? "https://compass.education.gov.uk";
        var body = $"""
            Your COMPASS API key has been issued.

            Token name: {token.Name}
            Environment: {token.Environment ?? "—"}
            Access level: {token.AccessTier ?? "—"}

            Bearer token (copy now — you will not be able to view it again in COMPASS):
            {tokenValue}

            Use it as: Authorization: Bearer {tokenValue}

            Developer portal: {baseUrl}/docs/developer/api
            API reference: {baseUrl}/docs/api
            API explorer: {baseUrl}/docs/api-explorer

            To recycle this key, use the developer portal. If you need delete access on any resource, contact {DesignOpsEmail}.
            """;

        await TrySendEmailAsync(recipientEmail, "Your COMPASS API key", body, cancellationToken);
    }

    private async Task SendTokenRecycledEmailAsync(
        string recipientEmail,
        ApiToken token,
        string newTokenValue,
        CancellationToken cancellationToken)
    {
        var baseUrl = _configuration["Compass:BaseUrl"]?.TrimEnd('/') ?? "https://compass.education.gov.uk";
        var body = $"""
            Your COMPASS API key "{token.Name}" has been recycled.

            New bearer token (copy now — you will not be able to view it again):
            {newTokenValue}

            Developer portal: {baseUrl}/docs/developer/api
            """;

        await TrySendEmailAsync(recipientEmail, "Your COMPASS API key was recycled", body, cancellationToken);
    }

    private async Task SendRequestQueuedEmailAsync(
        string recipientEmail,
        ApiTokenRequest request,
        CancellationToken cancellationToken)
    {
        var body = $"""
            We received your COMPASS API key request for {request.Environment}-{request.ProjectSlug}.

            Status: pending admin review
            Requested: {DateTime.UtcNow:u}

            You will receive another email when your key is approved or rejected.
            Read-only keys for @education.gov.uk accounts are approved automatically.
            """;
        await TrySendEmailAsync(recipientEmail, "COMPASS API key request received", body, cancellationToken);
    }

    private async Task SendRequestApprovedEmailAsync(ApiTokenRequest request, CancellationToken cancellationToken)
    {
        var body = $"Your COMPASS API key request ({request.Environment}-{request.ProjectSlug}) was approved. Check your inbox for the bearer token.";
        await TrySendEmailAsync(request.RequestorEmail, "COMPASS API key request approved", body, cancellationToken);
    }

    private async Task SendRequestRejectedEmailAsync(ApiTokenRequest request, CancellationToken cancellationToken)
    {
        var notes = string.IsNullOrWhiteSpace(request.ReviewNotes) ? "No additional notes." : request.ReviewNotes;
        var body = $"""
            Your COMPASS API key request ({request.Environment}-{request.ProjectSlug}) was rejected.

            Notes from reviewer:
            {notes}

            Contact {DesignOpsEmail} if you need to discuss this request.
            """;
        await TrySendEmailAsync(request.RequestorEmail, "COMPASS API key request rejected", body, cancellationToken);
    }

    private async Task NotifyAdminsOfPendingRequestAsync(ApiTokenRequest request, CancellationToken cancellationToken)
    {
        var baseUrl = _configuration["Compass:BaseUrl"]?.TrimEnd('/') ?? "https://compass.education.gov.uk";
        var body = $"""
            A new COMPASS API key request needs review.

            Requestor: {request.RequestorEmail}
            Environment: {request.Environment}
            Project: {request.ProjectSlug}
            Read-only all data: {request.IsReadOnlyAllData}

            Review: {baseUrl}/modern/admin?panel=api-token-requests
            """;
        await TrySendEmailAsync(DesignOpsEmail, "COMPASS API key request pending review", body, cancellationToken);
    }

    private async Task TrySendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _notificationService.SendEmailAsync(to, subject, body, cancellationToken: cancellationToken);
            if (!result.Success)
                _logger.LogWarning("API token email to {Email} failed: {Error}", to, result.ErrorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "API token email to {Email} threw", to);
        }
    }

    private static ApiTokenRequestResult Fail(string message) =>
        new() { Success = false, ErrorMessage = message };

    private static string NormalizeEmail(string email) =>
        email.Trim().ToLowerInvariant();

    private static string SerializePermissions(Dictionary<string, (bool read, bool create, bool update, bool delete)> permissions)
    {
        var dto = permissions.ToDictionary(
            kv => kv.Key,
            kv => new PermissionDto
            {
                Read = kv.Value.read,
                Create = kv.Value.create,
                Update = kv.Value.update,
                Delete = kv.Value.delete
            });
        return JsonSerializer.Serialize(dto);
    }

    public static Dictionary<string, (bool read, bool create, bool update, bool delete)> DeserializePermissions(string json)
    {
        try
        {
            var raw = JsonSerializer.Deserialize<Dictionary<string, PermissionDto>>(json);
            if (raw == null)
                return new Dictionary<string, (bool, bool, bool, bool)>();
            return raw.ToDictionary(
                kv => kv.Key,
                kv => (kv.Value.Read, kv.Value.Create, kv.Value.Update, kv.Value.Delete));
        }
        catch
        {
            return new Dictionary<string, (bool, bool, bool, bool)>();
        }
    }

    private sealed class PermissionDto
    {
        public bool Read { get; set; }
        public bool Create { get; set; }
        public bool Update { get; set; }
        public bool Delete { get; set; }
    }
}
