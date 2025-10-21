using Compass.Models;

namespace Compass.Services;

public interface IApiTokenService
{
    Task<ApiToken?> GetByTokenAsync(string token);
    Task<List<ApiToken>> GetAllTokensAsync();
    Task<ApiToken?> GetByIdAsync(int id);
    Task<ApiToken> CreateTokenAsync(string name, string description, string createdByEmail, DateTime? expiresAt = null);
    Task<string> RecycleTokenAsync(int id);
    Task<bool> DeleteTokenAsync(int id);
    Task<bool> UpdateTokenStatusAsync(int id, bool isActive);
    Task<bool> UpdateLastUsedAsync(int tokenId);
    Task<bool> SetPermissionsAsync(int tokenId, Dictionary<string, (bool read, bool create, bool update, bool delete)> permissions);
    Task<Dictionary<string, (bool read, bool create, bool update, bool delete)>> GetPermissionsAsync(int tokenId);
    bool ValidatePermission(ApiToken token, string resource, string operation);
}

