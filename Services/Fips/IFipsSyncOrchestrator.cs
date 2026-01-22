namespace Compass.Services.Fips;

public interface IFipsSyncOrchestrator
{
    /// <summary>
    /// Execute a FIPS sync operation
    /// </summary>
    /// <param name="syncHistoryId">ID of the sync history record to update</param>
    /// <param name="syncType">Type of sync (e.g., "CMDB to Strapi")</param>
    /// <param name="targetEnvironment">Target Strapi environment (Development, Test, Production)</param>
    Task ExecuteSyncAsync(int syncHistoryId, string syncType, string targetEnvironment);
    
    /// <summary>
    /// Test connections to CMDB and Strapi environments
    /// </summary>
    Task<Dictionary<string, bool>> TestConnectionsAsync();
}
