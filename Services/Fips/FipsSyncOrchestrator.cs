using System.Text;
using Compass.Data;
using Compass.Models;
using Compass.Models.Fips;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Compass.Services.Fips;

public class FipsSyncOrchestrator : IFipsSyncOrchestrator
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<FipsSyncOrchestrator> _logger;
    private readonly FipsSyncConfiguration _config;

    public FipsSyncOrchestrator(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<FipsSyncOrchestrator> logger,
        IOptions<FipsSyncConfiguration> config)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
        _config = config.Value;
    }

    public async Task ExecuteSyncAsync(int syncHistoryId, string syncType, string targetEnvironment)
    {
        var startTime = DateTime.UtcNow;
        var actionLog = new StringBuilder();
        var errorDetails = new StringBuilder();

        try
        {
            actionLog.AppendLine($"=== FIPS Sync Started at {startTime:yyyy-MM-dd HH:mm:ss} UTC ===");
            actionLog.AppendLine($"Sync Type: {syncType}");
            actionLog.AppendLine($"Target Environment: {targetEnvironment}");
            actionLog.AppendLine();

            // Get environment-specific configuration
            var strapiConfig = GetStrapiConfig(targetEnvironment);
            if (strapiConfig == null)
            {
                throw new Exception($"Invalid target environment: {targetEnvironment}");
            }

            actionLog.AppendLine($"Strapi Endpoint: {strapiConfig.Endpoint}");
            actionLog.AppendLine();

            // Create scoped services
            using var scope = _serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<CompassDbContext>();
            var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
            var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();

            // Execute the appropriate sync type
            SyncResult result;
            if (syncType == "CMDB to Strapi")
            {
                result = await ExecuteCmdbToStrapiSyncAsync(
                    strapiConfig, 
                    httpClientFactory, 
                    loggerFactory, 
                    actionLog, 
                    errorDetails);
            }
            else
            {
                throw new Exception($"Unknown sync type: {syncType}");
            }

            // Update sync history with results
            var syncHistory = await context.FipsSyncHistories.FindAsync(syncHistoryId);
            if (syncHistory != null)
            {
                var endTime = DateTime.UtcNow;
                syncHistory.Status = result.Success ? "Completed" : "Failed";
                syncHistory.CompletedAt = endTime;
                syncHistory.DurationSeconds = (int)(endTime - startTime).TotalSeconds;
                syncHistory.ProductsCreated = result.ProductsCreated;
                syncHistory.ProductsUpdated = result.ProductsUpdated;
                syncHistory.ProductsSkipped = result.ProductsSkipped;
                syncHistory.ErrorsEncountered = result.ErrorsEncountered;
                syncHistory.ActionsLog = actionLog.ToString();
                syncHistory.ErrorDetails = errorDetails.Length > 0 ? errorDetails.ToString() : null;
                syncHistory.Configuration = $"Target: {targetEnvironment}, Endpoint: {strapiConfig.Endpoint}";

                await context.SaveChangesAsync();

                _logger.LogInformation(
                    "Sync {SyncHistoryId} completed. Status: {Status}, Created: {Created}, Updated: {Updated}, Errors: {Errors}",
                    syncHistoryId, syncHistory.Status, result.ProductsCreated, result.ProductsUpdated, result.ErrorsEncountered);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing sync {SyncHistoryId}", syncHistoryId);
            
            errorDetails.AppendLine($"FATAL ERROR: {ex.Message}");
            errorDetails.AppendLine($"Stack Trace: {ex.StackTrace}");

            // Update sync history with error
            using var scope = _serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<CompassDbContext>();
            var syncHistory = await context.FipsSyncHistories.FindAsync(syncHistoryId);
            if (syncHistory != null)
            {
                syncHistory.Status = "Failed";
                syncHistory.CompletedAt = DateTime.UtcNow;
                syncHistory.DurationSeconds = (int)(DateTime.UtcNow - startTime).TotalSeconds;
                syncHistory.ErrorDetails = errorDetails.ToString();
                syncHistory.ActionsLog = actionLog.ToString();
                syncHistory.ErrorsEncountered = 1;

                await context.SaveChangesAsync();
            }
        }
    }

    private async Task<SyncResult> ExecuteCmdbToStrapiSyncAsync(
        StrapiEnvironmentConfig strapiConfig,
        IHttpClientFactory httpClientFactory,
        ILoggerFactory loggerFactory,
        StringBuilder actionLog,
        StringBuilder errorDetails)
    {
        var result = new SyncResult();
        
        try
        {
            // Create service instances with environment-specific config
            var cmdbHttpClient = httpClientFactory.CreateClient();
            var cmdbLogger = loggerFactory.CreateLogger<CmdbService>();
            var cmdbService = new CmdbService(
                cmdbHttpClient, 
                Options.Create(_config), 
                cmdbLogger);

            var strapiHttpClient = httpClientFactory.CreateClient();
            var strapiLogger = loggerFactory.CreateLogger<StrapiService>();
            var strapiService = new StrapiService(
                strapiHttpClient,
                strapiConfig.Endpoint,
                strapiConfig.ApiKey,
                strapiLogger);

            actionLog.AppendLine("=== Fetching CMDB Entries ===");
            var cmdbEntries = await cmdbService.GetAllCmdbEntriesAsync();
            actionLog.AppendLine($"Found {cmdbEntries.Count} CMDB entries");
            actionLog.AppendLine();

            actionLog.AppendLine("=== Fetching Existing Strapi Products ===");
            var strapiProducts = await strapiService.GetAllProductsAsync();
            actionLog.AppendLine($"Found {strapiProducts.Count} existing Strapi products");
            actionLog.AppendLine();

            // Create a lookup dictionary for faster matching
            var strapiProductsBySysId = strapiProducts
                .Where(p => !string.IsNullOrEmpty(p.CmdbSysId))
                .ToDictionary(p => p.CmdbSysId!, p => p);

            actionLog.AppendLine("=== Processing CMDB Entries ===");
            actionLog.AppendLine();

            var processedCount = 0;
            foreach (var cmdbEntry in cmdbEntries)
            {
                processedCount++;
                try
                {
                    if (strapiProductsBySysId.TryGetValue(cmdbEntry.SysId, out var existingProduct))
                    {
                        // Product exists - check if update is needed
                        var needsUpdate = 
                            existingProduct.Attributes?.Title != cmdbEntry.Name ||
                            existingProduct.Attributes?.LongDescription != cmdbEntry.Description ||
                            existingProduct.Attributes?.ParentCategory != cmdbEntry.ParentName;

                        if (needsUpdate)
                        {
                            await strapiService.UpdateProductAsync(
                                existingProduct.DocumentId, 
                                cmdbEntry, 
                                existingProduct);
                            
                            result.ProductsUpdated++;
                            actionLog.AppendLine($"✓ [{processedCount}/{cmdbEntries.Count}] Updated: {cmdbEntry.Name}");
                        }
                        else
                        {
                            result.ProductsSkipped++;
                            if (processedCount % 10 == 0)
                            {
                                actionLog.AppendLine($"  [{processedCount}/{cmdbEntries.Count}] Skipped {result.ProductsSkipped} products (no changes)");
                            }
                        }
                    }
                    else
                    {
                        // Product doesn't exist - create it
                        await strapiService.CreateProductAsync(cmdbEntry);
                        result.ProductsCreated++;
                        actionLog.AppendLine($"+ [{processedCount}/{cmdbEntries.Count}] Created: {cmdbEntry.Name}");
                    }
                }
                catch (Exception ex)
                {
                    result.ErrorsEncountered++;
                    var errorMsg = $"✗ [{processedCount}/{cmdbEntries.Count}] Error processing '{cmdbEntry.Name}': {ex.Message}";
                    actionLog.AppendLine(errorMsg);
                    errorDetails.AppendLine(errorMsg);
                    errorDetails.AppendLine($"  Stack: {ex.StackTrace}");
                    errorDetails.AppendLine();
                    
                    _logger.LogError(ex, "Error processing CMDB entry {Name}", cmdbEntry.Name);
                }

                // Progress update every 20 items
                if (processedCount % 20 == 0)
                {
                    actionLog.AppendLine($"  Progress: {processedCount}/{cmdbEntries.Count} processed");
                }
            }

            actionLog.AppendLine();
            actionLog.AppendLine("=== Sync Summary ===");
            actionLog.AppendLine($"Total CMDB Entries: {cmdbEntries.Count}");
            actionLog.AppendLine($"Products Created: {result.ProductsCreated}");
            actionLog.AppendLine($"Products Updated: {result.ProductsUpdated}");
            actionLog.AppendLine($"Products Skipped: {result.ProductsSkipped}");
            actionLog.AppendLine($"Errors Encountered: {result.ErrorsEncountered}");
            actionLog.AppendLine();

            result.Success = result.ErrorsEncountered == 0 || 
                           (result.ProductsCreated + result.ProductsUpdated) > 0;

            if (result.Success)
            {
                actionLog.AppendLine("✓ Sync completed successfully!");
            }
            else
            {
                actionLog.AppendLine("⚠ Sync completed with errors");
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorsEncountered++;
            errorDetails.AppendLine($"SYNC ERROR: {ex.Message}");
            errorDetails.AppendLine(ex.StackTrace);
            throw;
        }

        return result;
    }

    private StrapiEnvironmentConfig? GetStrapiConfig(string environment)
    {
        return environment switch
        {
            "Development" => _config.Strapi.Development,
            "Test" => _config.Strapi.Test,
            "Production" => _config.Strapi.Production,
            _ => null
        };
    }

    public async Task<Dictionary<string, bool>> TestConnectionsAsync()
    {
        var results = new Dictionary<string, bool>();

        try
        {
            // Test CMDB connection
            using var scope = _serviceScopeFactory.CreateScope();
            var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
            var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();

            // Test CMDB
            try
            {
                var cmdbHttpClient = httpClientFactory.CreateClient();
                var cmdbLogger = loggerFactory.CreateLogger<CmdbService>();
                var cmdbService = new CmdbService(cmdbHttpClient, Options.Create(_config), cmdbLogger);
                await cmdbService.GetCmdbEntryCountAsync();
                results["CMDB"] = true;
            }
            catch
            {
                results["CMDB"] = false;
            }

            // Test Strapi environments
            foreach (var env in new[] { "Development", "Test", "Production" })
            {
                try
                {
                    var strapiConfig = GetStrapiConfig(env);
                    if (strapiConfig != null && !string.IsNullOrEmpty(strapiConfig.Endpoint))
                    {
                        var strapiHttpClient = httpClientFactory.CreateClient();
                        var strapiLogger = loggerFactory.CreateLogger<StrapiService>();
                        var strapiService = new StrapiService(
                            strapiHttpClient,
                            strapiConfig.Endpoint,
                            strapiConfig.ApiKey,
                            strapiLogger);
                        await strapiService.GetProductCountAsync();
                        results[$"Strapi-{env}"] = true;
                    }
                    else
                    {
                        results[$"Strapi-{env}"] = false;
                    }
                }
                catch
                {
                    results[$"Strapi-{env}"] = false;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing connections");
        }

        return results;
    }
}

public class SyncResult
{
    public bool Success { get; set; }
    public int ProductsCreated { get; set; }
    public int ProductsUpdated { get; set; }
    public int ProductsSkipped { get; set; }
    public int ErrorsEncountered { get; set; }
}
