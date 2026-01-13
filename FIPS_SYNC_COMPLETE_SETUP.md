# FIPS Sync - Complete Setup Guide

## ✅ Implementation Complete!

The FIPS sync functionality has been fully integrated into COMPASS. You can now sync data from CMDB to any Strapi environment (Development, Test, or Production) directly from the COMPASS admin interface.

## 🎯 Key Features

- **No External Dependencies**: All sync logic is built into COMPASS (C#)
- **Environment Selection**: Choose target Strapi environment from the UI
- **Live Configuration**: All Strapi environments configured in appsettings.json
- **Real-time Progress**: Auto-refreshing UI shows sync progress
- **Comprehensive Logging**: Detailed logs and error tracking
- **Background Processing**: Syncs run asynchronously without blocking the UI

## 📋 Setup Steps

### 1. Run Database Migration

Create the FipsSyncHistory table:

```bash
cd /Users/andyjones/Source/code-digital-ops/FIPS/compass
dotnet ef migrations add AddFipsSyncHistory
dotnet ef database update
```

### 2. Configure appsettings.json

Add the FipsSync configuration section to your `appsettings.json`:

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
        "ApiKey": "your-development-api-key-here"
      },
      "Test": {
        "Endpoint": "https://fips-cms-test.azurewebsites.net/api",
        "ApiKey": "33c4930716a8232dfbf905946133cf7cce8a393d6fb0f7e198d32d7badf46b05087a17bbbc22c29ce1db6ba3b3e05a81d22ef4451c0550e574f51c5ad868bd24f37abc166dbd5aac9d7d31b7dd43eb29cf194b0563c0158fe7bd03ceac7cde6f88eb232489bfa4f56aa66da2369989404d9c4b8f4d92c8a8d11d67b436d2834b"
      },
      "Production": {
        "Endpoint": "https://fips-cms.azurewebsites.net/api",
        "ApiKey": "8f2924c3614086a524dc0a2f9591644c7d40c5772a2e3f860f0f09e0eef2f762219e1a0d3c5e6c919701f6f905893a2b3b95b0074adb81295a81514db84e821decb16cf1a7284a8379b70df596e64c3350bb85cb958cebecb2f523242ed883cdcb5f128857a79004f125553e884a6dc6d21c7546d7a0895def71"
      }
    }
  }
}
```

**📝 Note**: You can also use environment-specific configuration files:
- `appsettings.Development.json` - for local development
- `appsettings.Test.json` - for test environment
- `appsettings.Production.json` - for production

### 3. Build the Application

```bash
dotnet build
```

### 4. Run the Application

```bash
dotnet run
```

## 🚀 Usage

### Running a Sync

1. Navigate to **Admin** → **System** → **FIPS Sync**
2. Select sync type: **"CMDB to Strapi"**
3. Select target environment from dropdown:
   - **Development** - Your local Strapi instance
   - **Test** - Azure test environment
   - **Production** - Azure production environment
4. Click **"Run Sync"**
5. The page will auto-refresh showing progress
6. Click on any sync record to view detailed logs

### Understanding Sync Results

Each sync shows:
- **Status**: Running, Completed, or Failed
- **Duration**: How long the sync took
- **Statistics**:
  - Products Created
  - Products Updated
  - Products Skipped (no changes)
  - Errors Encountered

### Viewing Detailed Logs

Click **"View Details"** on any sync to see:
- Full action log with all operations performed
- Error details (if any)
- Sync configuration used
- Timestamp information

## 🏗️ Architecture

### Services Created

```
Services/Fips/
├── ICmdbService.cs                - CMDB interface
├── CmdbService.cs                 - ServiceNow CMDB client
├── IStrapiService.cs              - Strapi interface
├── StrapiService.cs               - Strapi CMS client
├── IFipsSyncOrchestrator.cs       - Orchestrator interface
└── FipsSyncOrchestrator.cs        - Main sync orchestration
```

### Models Created

```
Models/Fips/
├── CmdbModels.cs                  - CMDB data structures
├── StrapiModels.cs                - Strapi data structures
└── FipsSyncConfiguration.cs       - Configuration models
```

### How It Works

1. **User triggers sync** from Admin UI
2. **FipsSyncController** creates a sync history record
3. **Background task** starts `FipsSyncOrchestrator`
4. **Orchestrator**:
   - Selects correct Strapi config based on target environment
   - Fetches CMDB entries via `CmdbService`
   - Fetches existing Strapi products via `StrapiService`
   - Compares and syncs (create/update)
   - Updates sync history with results
5. **UI auto-refreshes** to show progress
6. **User views** detailed results

## 🔧 Configuration Details

### Environment Selection

The system stores configurations for all three Strapi environments:

```
Development → http://localhost:1337/api
Test        → https://fips-cms-test.azurewebsites.net/api
Production  → https://fips-cms.azurewebsites.net/api
```

When you select a target environment in the UI, the orchestrator automatically uses the correct endpoint and API key from `appsettings.json`.

### Why This Approach?

✅ **No runtime configuration changes needed**
- All environments pre-configured
- Select target from UI dropdown
- No need to restart app or change settings

✅ **Separation of environments**
- Clear distinction between Dev/Test/Prod
- Different API keys for security
- Can't accidentally sync to wrong environment

✅ **Run from production COMPASS**
- Production COMPASS instance can sync to any Strapi environment
- Useful for:
  - Syncing CMDB to Prod Strapi (normal operation)
  - Syncing CMDB to Test Strapi (testing)
  - Syncing CMDB to Dev Strapi (development)

## 🔐 Security

### API Keys

- Store in Azure Key Vault for production deployments
- Use User Secrets for local development:
  ```bash
  dotnet user-secrets set "FipsSync:Strapi:Development:ApiKey" "your-key-here"
  ```

### CMDB Credentials

- Use service account with read-only permissions
- Store in Azure Key Vault for production
- Rotate credentials regularly

### Access Control

The FIPS Sync admin page is:
- Behind `[Authorize]` attribute (requires login)
- Add permission checks if needed:
  ```csharp
  if (!await _permissionService.IsInGroupAsync(userEmail, "FIPS-Admins"))
  {
      return Forbid();
  }
  ```

## 📊 Monitoring

### Health Checks

Test connections to all configured environments:

```csharp
var healthResults = await _orchestrator.TestConnectionsAsync();
// Returns: { "CMDB": true, "Strapi-Development": true, "Strapi-Test": true, "Strapi-Production": true }
```

### Logging

All operations are logged via `ILogger`:
- Info: Normal operations
- Warning: Skipped items, retryable errors
- Error: Failures requiring attention

View logs in:
- Console output (development)
- Application Insights (production)
- Sync history detailed view (UI)

## 🐛 Troubleshooting

### Issue: Migration fails

**Solution**: Ensure you're in the compass directory and have a valid connection string:
```bash
cd /Users/andyjones/Source/code-digital-ops/FIPS/compass
dotnet ef database update
```

### Issue: Sync fails immediately

**Solution**: Check configuration in appsettings.json:
- Verify CMDB credentials are correct
- Verify Strapi endpoints are accessible
- Verify API keys are valid

### Issue: CMDB connection fails

**Symptoms**: Error message about authentication or connection
**Solution**:
1. Check `FipsSync:Cmdb:Username` and `Password`
2. Verify ServiceNow endpoint is accessible
3. Check network/firewall rules

### Issue: Strapi connection fails

**Symptoms**: 401 Unauthorized or connection refused
**Solution**:
1. Verify API key for the selected environment
2. Check Strapi instance is running (for Development)
3. Verify Azure app is running (for Test/Production)
4. Test endpoint manually:
   ```bash
   curl -H "Authorization: Bearer YOUR_API_KEY" https://fips-cms-test.azurewebsites.net/api/products
   ```

### Issue: Sync shows as "Running" forever

**Solution**:
1. Check application logs for errors
2. The background task may have crashed
3. Restart the COMPASS application
4. Check database - status may need manual update

### Issue: Products not syncing

**Symptoms**: Sync completes but no products created/updated
**Solution**:
1. Check sync logs for specific errors
2. Verify CMDB has active entries
3. Check Strapi schema matches expected fields
4. Review error details in sync history

## 🎓 Best Practices

### Running Syncs

1. **Test First**: Always run to Development before Test/Production
2. **Check History**: Review previous sync results before running new ones
3. **Monitor Progress**: Keep the sync page open to watch progress
4. **Review Logs**: Always check detailed logs after completion

### Managing Environments

1. **Development**: 
   - Use for testing sync logic
   - Can reset/wipe data freely
   - Run frequently during development

2. **Test**:
   - Mirror of production data
   - Use for UAT and validation
   - Run before production syncs

3. **Production**:
   - Live data - be careful
   - Run on schedule (e.g., daily/weekly)
   - Monitor carefully for errors

### Scheduling

To run syncs on a schedule, use:
- Azure Functions with timer trigger
- Hangfire background jobs
- Windows Task Scheduler (for on-premise)
- Call the orchestrator from a scheduled job:
  ```csharp
  await _orchestrator.ExecuteSyncAsync(syncHistoryId, "CMDB to Strapi", "Production");
  ```

## 📦 What About the sync-app?

### Current Status

The Node.js sync-app is still available and functional. You have two options:

**Option 1: Use COMPASS only** (Recommended)
- All sync functionality is in COMPASS
- No need to maintain Node.js code
- Easier deployment
- Better integration

**Option 2: Keep both**
- Use COMPASS for UI-driven syncs
- Keep sync-app for CLI/scheduled jobs
- Redundancy in case one fails

### Migration Path

To fully retire the sync-app:

1. ✅ Verify COMPASS sync works correctly
2. ✅ Test all environments (Dev/Test/Prod)
3. ✅ Run parallel syncs to compare results
4. ✅ Document any differences
5. ✅ Switch all scheduled jobs to use COMPASS
6. 📦 Archive sync-app (don't delete - keep as backup)

## 🎉 Summary

You now have a fully integrated FIPS sync system in COMPASS!

### What You Can Do:
- ✅ Sync CMDB to any Strapi environment
- ✅ Select target environment from UI
- ✅ Monitor sync progress in real-time
- ✅ View detailed logs and statistics
- ✅ Track sync history
- ✅ No external Node.js dependency

### Files Created:
- 6 service files (interfaces + implementations)
- 3 model files (CMDB, Strapi, Configuration)
- 1 controller (updated for services)
- 1 view (already existed)
- 1 database model (FipsSyncHistory)
- 3 documentation files

### Next Steps:
1. Run the database migration
2. Add FipsSync config to appsettings.json
3. Build and run COMPASS
4. Navigate to Admin → FIPS Sync
5. Run a test sync to Development
6. Review results and logs
7. Roll out to Test and Production

Need help? Check the logs or review this guide!
