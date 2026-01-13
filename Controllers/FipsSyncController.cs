using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Compass.Data;
using Compass.Models;
using Compass.Services.Fips;

namespace Compass.Controllers;

[Authorize]
public class FipsSyncController : Controller
{
    private readonly CompassDbContext _context;
    private readonly ILogger<FipsSyncController> _logger;
    private readonly IFipsSyncOrchestrator _orchestrator;
    private readonly IConfiguration _configuration;

    public FipsSyncController(
        CompassDbContext context,
        ILogger<FipsSyncController> logger,
        IFipsSyncOrchestrator orchestrator,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _orchestrator = orchestrator;
        _configuration = configuration;
    }

    // GET: /FipsSync/Index
    public async Task<IActionResult> Index()
    {
        var syncHistory = await _context.FipsSyncHistories
            .OrderByDescending(sh => sh.StartedAt)
            .Take(50)
            .ToListAsync();

        ViewBag.Environments = new[] { "Development", "Test", "Production" };

        return View(syncHistory);
    }

    // GET: /FipsSync/GetSyncConfiguration
    [HttpGet]
    public IActionResult GetSyncConfiguration(string syncType, string sourceEnvironment, string targetEnvironment)
    {
        try
        {
            var config = new
            {
                syncType = syncType,
                source = GetEnvironmentConfig(sourceEnvironment),
                target = GetEnvironmentConfig(targetEnvironment),
                cmdb = new
                {
                    endpoint = _configuration["FipsSync:Cmdb:Endpoint"] ?? "Not configured",
                    username = _configuration["FipsSync:Cmdb:Username"] ?? "Not configured"
                }
            };

            return Json(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sync configuration");
            return Json(new { error = ex.Message });
        }
    }

    private object GetEnvironmentConfig(string environment)
    {
        if (string.IsNullOrEmpty(environment))
        {
            return new
            {
                environment = "N/A",
                endpoint = "N/A",
                apiKeyPreview = "N/A"
            };
        }

        // Get the endpoint and API key from configuration
        var endpointValue = _configuration[$"FipsSync:Strapi:{environment}:Endpoint"] 
            ?? "Not configured";
        var apiKeyValue = _configuration[$"FipsSync:Strapi:{environment}:ApiKey"] 
            ?? "Not configured";

        // Mask the API key for security
        var apiKeyPreview = apiKeyValue != "Not configured" && apiKeyValue.Length > 20
            ? apiKeyValue.Substring(0, 10) + "..." + apiKeyValue.Substring(apiKeyValue.Length - 10)
            : "***";

        return new
        {
            environment = environment,
            endpoint = endpointValue,
            apiKeyPreview = apiKeyPreview
        };
    }

    // GET: /FipsSync/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var syncHistory = await _context.FipsSyncHistories
            .FirstOrDefaultAsync(sh => sh.Id == id);

        if (syncHistory == null)
        {
            TempData["ErrorMessage"] = "Sync history record not found.";
            return RedirectToAction(nameof(Index));
        }

        return View(syncHistory);
    }

    // POST: /FipsSync/RunSync
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RunSync(string syncType, string sourceEnvironment, string targetEnvironment)
    {
        try
        {
            var userEmail = User.Identity?.Name 
                ?? User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                ?? "Unknown";

            // For CMDB to Strapi, sourceEnvironment is "CMDB" and we only care about targetEnvironment
            var displaySource = syncType == "CMDB to Strapi" ? "CMDB" : sourceEnvironment;

            // Create sync history record
            var syncHistory = new FipsSyncHistory
            {
                SyncType = syncType,
                SourceEnvironment = displaySource,
                TargetEnvironment = targetEnvironment,
                Status = "Running",
                StartedAt = DateTime.UtcNow,
                InitiatedBy = userEmail
            };

            _context.FipsSyncHistories.Add(syncHistory);
            await _context.SaveChangesAsync();

            // Start sync process in background using orchestrator
            _ = Task.Run(async () => 
            {
                try
                {
                    await _orchestrator.ExecuteSyncAsync(syncHistory.Id, syncType, targetEnvironment);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Background sync failed for history ID {SyncHistoryId}", syncHistory.Id);
                }
            });

            TempData["SuccessMessage"] = $"Sync to {targetEnvironment} has been started. Check the history below for progress.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting sync operation");
            TempData["ErrorMessage"] = $"Error starting sync: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }


    // GET: /FipsSync/Delete/5
    public async Task<IActionResult> Delete(int id)
    {
        var syncHistory = await _context.FipsSyncHistories
            .FirstOrDefaultAsync(sh => sh.Id == id);

        if (syncHistory == null)
        {
            TempData["ErrorMessage"] = "Sync history record not found.";
            return RedirectToAction(nameof(Index));
        }

        return View(syncHistory);
    }

    // POST: /FipsSync/DeleteConfirmed/5
    [HttpPost, ActionName("DeleteConfirmed")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        try
        {
            var syncHistory = await _context.FipsSyncHistories.FindAsync(id);
            
            if (syncHistory != null)
            {
                _context.FipsSyncHistories.Remove(syncHistory);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Sync history record deleted successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Sync history record not found.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting sync history record");
            TempData["ErrorMessage"] = $"Error deleting record: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }
}
