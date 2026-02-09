# Complete FIPS Sync Integration - Implementation Guide

## What's Been Created

### ✅ Already Implemented

1. **Database Model**
   - `/Models/FipsSyncHistory.cs` - Sync history tracking
   - Updated `CompassDbContext.cs` with FipsSyncHistories DbSet

2. **Views**
   - `/Views/FipsSync/Index.cshtml` - Main sync management page
   - `/Views/FipsSync/Details.cshtml` - Detailed sync view
   - Updated `/Views/Admin/Index.cshtml` with FIPS Sync link

3. **Services - Interfaces**
   - `/Services/Fips/ICmdbService.cs`
   - `/Services/Fips/IStrapiService.cs`

4. **Services - Implementations**
   - `/Services/Fips/CmdbService.cs` - Complete ServiceNow CMDB integration

5. **Models**
   - `/Models/Fips/CmdbModels.cs` - CMDB data models
   - `/Models/Fips/StrapiModels.cs` - Strapi data models
   - `/Models/Fips/FipsSyncConfiguration.cs` - Configuration models

6. **Documentation**
   - `FIPS_SYNC_SETUP.md` - Setup guide
   - `COMPASS_FIPS_SYNC_INTEGRATION.md` - Architecture guide
   - This file - Implementation guide

## What Needs To Be Completed

### 1. Complete StrapiService Implementation

Create `/Services/Fips/StrapiService.cs` with full implementation (1000+ lines).
This service handles all Strapi API operations.

**Key Methods:**
- `GetAllProductsAsync()` - Fetch all products with pagination
- `GetProductCountAsync()` - Get total product count
- `FindProductByCmdbSysIdAsync()` - Find product by CMDB ID
- `CreateProductAsync()` - Create new product
- `UpdateProductAsync()` - Update existing product
- `DeleteProductAsync()` - Delete product

### 2. Create FipsSyncOrchestrator Service

Create `/Services/Fips/IFipsSyncOrchestrator.cs`:

```csharp
using Compass.Models.Fips;

namespace Compass.Services.Fips;

public interface IFipsSyncOrchestrator
{
    Task ExecuteSyncAsync(int syncHistoryId, string syncType, string targetEnvironment);
    Task<SyncStatistics> GetSyncStatisticsAsync(int syncHistoryId);
}

public class SyncStatistics
{
    public int ProductsCreated { get; set; }
    public int ProductsUpdated { get; set; }
    public int ProductsSkipped { get; set; }
    public int ErrorsEncountered { get; set; }
    public List<string> ActionLog { get; set; } = new();
}
```

Create `/Services/Fips/FipsSyncOrchestrator.cs` - Main orchestration logic.

### 3. Update FipsSyncController

Replace the Node.js execution code in `RunSync` action:

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> RunSync(string syncType, string targetEnvironment)
{
    try
    {
        var userEmail = User.Identity?.Name 
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
            ?? "Unknown";

        var syncHistory = new FipsSyncHistory
        {
            SyncType = syncType,
            TargetEnvironment = targetEnvironment,
            Status = "Running",
            StartedAt = DateTime.UtcNow,
            InitiatedBy = userEmail
        };

        _context.FipsSyncHistories.Add(syncHistory);
        await _context.SaveChangesAsync();

        // Execute sync in background using the orchestrator
        _ = Task.Run(async () =>
        {
            await _orchestrator.ExecuteSyncAsync(
                syncHistory.Id, 
                syncType, 
                targetEnvironment);
        });

        TempData["SuccessMessage"] = $"Sync to {targetEnvironment} has been started.";
        return RedirectToAction(nameof(Index));
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error starting sync");
        TempData["ErrorMessage"] = $"Error: {ex.Message}";
        return RedirectToAction(nameof(Index));
    }
}
```

Update constructor:

```csharp
private readonly IFipsSyncOrchestrator _orchestrator;

public FipsSyncController(
    CompassDbContext context,
    ILogger<FipsSyncController> logger,
    IFipsSyncOrchestrator orchestrator)
{
    _context = context;
    _logger = logger;
    _orchestrator = orchestrator;
}
```

### 4. Register Services in Program.cs

Add before `builder.Build()`:

```csharp
// FIPS Sync Configuration
builder.Services.Configure<FipsSyncConfiguration>(
    builder.Configuration.GetSection("FipsSync"));

// FIPS Sync Services
builder.Services.AddHttpClient<ICmdbService, CmdbService>()
    .SetHandlerLifetime(TimeSpan.FromMinutes(5));

builder.Services.AddHttpClient<IStrapiService, StrapiService>()
    .SetHandlerLifetime(TimeSpan.FromMinutes(5));

builder.Services.AddScoped<IFipsSyncOrchestrator, FipsSyncOrchestrator>();
```

### 5. Add Configuration

In `appsettings.json` (or environment-specific files):

```json
{
  "FipsSync": {
    "Cmdb": {
      "Endpoint": "https://dfe.service-now.com/api/now/table/service_offering",
      "Username": "POWERBI-Arch",
      "Password": ">x2@@qnB7idQR=$t*o&p1}BqDouaWx()kGBSxm57"
    },
    "Strapi": {
      "Development": {
        "Endpoint": "http://localhost:1337/api",
        "ApiKey": "your-dev-api-key"
      },
      "Test": {
        "Endpoint": "https://fips-cms-test.azurewebsites.net/api",
        "ApiKey": "33c4930716a8232dfbf905946133cf7cce8a393d6fb0f7e198d32d7badf46b05087a17bbbc22c29ce1db6ba3b3e05a81d22ef4451c0550e574f51c5ad868bd24f37abc166dbd5aac9d7d31b7dd43eb29cf194b0563c0158fe7bd03ceac7cde6f88eb232489bfa4f56aa66da2369989404d9c4b8f4d92c8a8d11d67b436d2834b"
      },
      "Production": {
        "Endpoint": "https://fips-cms.azurewebsites.net/api",
        "ApiKey": "8f2924c3614086a524dc0a2f9591644c7d40c5772a2e3f860f0f09e0eef2f762219e1a0d3c5e6c919701f6f905893a2b3b95b0074adb81295a81514db84e821decb16cf1a7284a8379b70df596e64c3350bb85cb958cebecb2f523242ed883cdcb5f128857a79004f125571fdc186505553e884a6dc6d21c7546d7a0895def71"
      }
    },
    "Sas": {
      "Endpoint": "https://service-assessments.education.gov.uk/api/product/",
      "SecretKey": "97324fubudfgh2eu7gf982eguf92egfegw2q97fgt8eq2"
    },
    "Aiss": {
      "Endpoint": "https://your-aiss-endpoint.com/api",
      "ApiKey": "your-aiss-api-key"
    }
  }
}
```

### 6. Run Database Migration

```bash
cd compass
dotnet ef migrations add AddFipsSyncHistory
dotnet ef database update
```

## Simplified Approach - Minimal Implementation

If you want to get started quickly without implementing everything, here's the minimal approach:

### Option A: Keep Using Node.js Scripts (Current Approach)

The current FipsSyncController already does this - it calls the Node.js scripts.
Just need to:
1. Run the database migration
2. Add configuration to appsettings.json
3. Ensure Node.js and sync-app are set up

### Option B: Implement Services (Recommended)

Benefits:
- No external dependencies
- Better integration  
- Easier to maintain
- Type-safe
- Better error handling

Requires:
1. Complete StrapiService implementation (~500 lines)
2. Create FipsSyncOrchestrator (~300 lines)
3. Update controller (~50 lines)
4. Register services (~10 lines)

Total: ~860 lines of C# code to replace Node.js dependency

## Quick Start Steps

### For Immediate Use (Node.js approach):

1. **Run migration:**
   ```bash
   dotnet ef migrations add AddFipsSyncHistory
   dotnet ef database update
   ```

2. **Update appsettings.json** with FipsSync configuration

3. **Update FipsSyncController** with correct path to sync-app:
   ```json
   {
     "FipsSync": {
       "SyncAppPath": "/Users/andyjones/Source/code-digital-ops/FIPS/sync-app",
       "NodeCommand": "node"
     }
   }
   ```

4. **Test:**
   - Navigate to Admin → FIPS Sync
   - Run a test sync
   - View results

### For Integrated Approach (C# services):

1. I can create the remaining service implementations (StrapiService and FipsSyncOrchestrator)
2. Update the controller to use services
3. Register services in Program.cs
4. Test the integrated approach

## Decision Point

**Which approach do you prefer?**

A. **Keep Node.js approach** (works now, minimal changes)
   - ✅ Quick to implement
   - ✅ Uses existing sync-app code
   - ❌ External dependency on Node.js
   - ❌ More complex deployment

B. **Full C# integration** (recommended long-term)
   - ✅ No external dependencies
   - ✅ Better integration
   - ✅ Easier maintenance
   - ❌ More code to write initially
   - ❌ Need to test thoroughly

## Next Steps

Let me know which approach you'd like and I can:

1. **Node.js Approach**: Finalize the current controller and configuration
2. **C# Integration**: Create the remaining services (StrapiService + Orchestrator)

Both approaches work and can be migrated between later if needed!

## Files Summary

### Already Created (✅):
- Models/FipsSyncHistory.cs
- Models/Fips/CmdbModels.cs
- Models/Fips/StrapiModels.cs  
- Models/Fips/FipsSyncConfiguration.cs
- Services/Fips/ICmdbService.cs
- Services/Fips/CmdbService.cs
- Services/Fips/IStrapiService.cs
- Controllers/FipsSyncController.cs (with Node.js execution)
- Views/FipsSync/Index.cshtml
- Views/FipsSync/Details.cshtml
- Data/CompassDbContext.cs (updated)
- Views/Admin/Index.cshtml (updated)

### To Create for Full Integration (🔄):
- Services/Fips/StrapiService.cs
- Services/Fips/IFipsSyncOrchestrator.cs
- Services/Fips/FipsSyncOrchestrator.cs
- Update Controllers/FipsSyncController.cs
- Update Program.cs

### Configuration Files:
- appsettings.json (add FipsSync section)
- appsettings.Development.json (add FipsSync section)
- appsettings.Test.json (add FipsSync section)
- appsettings.Production.json (add FipsSync section)
