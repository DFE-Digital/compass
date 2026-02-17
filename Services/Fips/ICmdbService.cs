using Compass.Models.Fips;

namespace Compass.Services.Fips;

/// <summary>
/// Service for interacting with ServiceNow CMDB
/// </summary>
public interface ICmdbService
{
    /// <summary>
    /// Get all active CMDB entries
    /// </summary>
    Task<List<CmdbEntry>> GetAllCmdbEntriesAsync();
    
    /// <summary>
    /// Get count of active CMDB entries
    /// </summary>
    Task<int> GetCmdbEntryCountAsync();
    
    /// <summary>
    /// Get CMDB entries with pagination
    /// </summary>
    Task<CmdbPagedResult> GetCmdbEntriesPaginatedAsync(int limit = 100, int offset = 0);
    
    /// <summary>
    /// Get user details from ServiceNow
    /// </summary>
    Task<CmdbUser?> GetUserDetailsAsync(string userId);
    
    /// <summary>
    /// Get users associated with a service offering
    /// </summary>
    Task<CmdbServiceUsers> GetServiceOfferingUsersAsync(CmdbEntry entry);
    
    /// <summary>
    /// Get a service offering by sys_id
    /// </summary>
    Task<CmdbEntry?> GetServiceOfferingBySysIdAsync(string sysId);
}
