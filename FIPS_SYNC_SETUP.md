# FIPS Sync Management Setup Guide

## Overview

The FIPS Sync Management system in COMPASS allows administrators to run and monitor data synchronization operations between different FIPS environments directly from the admin interface.

## Features

- Run CMDB to Strapi sync operations
- Run Strapi-to-Strapi sync operations (e.g., Production → Development)
- View sync history with detailed statistics
- Monitor running syncs with auto-refresh
- View detailed logs and error information for each sync run
- Manage sync history records

## Database Setup

### 1. Run Migration

The `FipsSyncHistory` table needs to be created in your COMPASS database:

```bash
cd /path/to/compass
dotnet ef migrations add AddFipsSyncHistory
dotnet ef database update
```

This creates the `FipsSyncHistories` table to store sync operation history.

## Configuration

### 1. Update appsettings.json

Add the following configuration to your `appsettings.json` (or environment-specific appsettings):

```json
{
  "FipsSync": {
    "SyncAppPath": "/path/to/sync-app",
    "NodeCommand": "node",
    "Strapi": {
      "Development": {
        "Endpoint": "http://localhost:1337/api",
        "ApiKey": "your-dev-api-key"
      },
      "Test": {
        "Endpoint": "https://fips-cms-test.azurewebsites.net/api",
        "ApiKey": "your-test-api-key"
      },
      "Production": {
        "Endpoint": "https://fips-cms.azurewebsites.net/api",
        "ApiKey": "your-production-api-key"
      }
    },
    "CMDB": {
      "Endpoint": "https://dfe.service-now.com/api/now/table/service_offering",
      "Username": "your-cmdb-username",
      "Password": "your-cmdb-password"
    }
  }
}
```

### 2. Environment-Specific Configuration

For different environments (Development, Test, Production), use environment-specific appsettings files:

- `appsettings.Development.json` - Local development settings
- `appsettings.Test.json` - Test environment settings  
- `appsettings.Production.json` - Production environment settings

### 3. Configuration Options

| Setting | Description | Example |
|---------|-------------|---------|
| `FipsSync:SyncAppPath` | Path to the sync-app directory | `/var/www/sync-app` |
| `FipsSync:NodeCommand` | Node.js command to use | `node` or `/usr/bin/node` |
| `FipsSync:Strapi:*:Endpoint` | Strapi API endpoint for environment | `https://fips-cms.azurewebsites.net/api` |
| `FipsSync:Strapi:*:ApiKey` | Strapi API key for environment | `your-api-key-here` |

## Setup Sync App

### 1. Install Dependencies

```bash
cd /path/to/sync-app
npm install
```

### 2. Configure Environment Variables

Create `.env` file in sync-app directory:

```bash
# CMDB Configuration
CMDB_ENDPOINT=https://dfe.service-now.com/api/now/table/service_offering
CMDB_USERNAME=your_username
CMDB_PASSWORD=your_password

# Default Strapi Configuration (used if not overridden by COMPASS)
STRAPI_ENDPOINT=https://fips-cms.azurewebsites.net/api
STRAPI_API_KEY=your_api_key

# SAS Configuration
SAS_ENDPOINT=https://service-assessments.education.gov.uk/api/product/
SAS_SECRET=your_sas_secret
```

### 3. Test Sync App

```bash
# Test CMDB sync
node app.js --count-only

# Test Strapi-to-Strapi sync
node strapi-to-strapi-sync.js --test
```

## Usage

### Access FIPS Sync Page

1. Log into COMPASS
2. Navigate to **Administration** → **System** → **FIPS Sync**

### Run a Sync Operation

1. Select **Sync Type**:
   - **CMDB to Strapi**: Sync products from ServiceNow CMDB
   - **Strapi to Strapi**: Copy data between Strapi environments

2. Select **Source Environment** (for Strapi-to-Strapi):
   - Production
   - Test
   - (N/A for CMDB syncs)

3. Select **Target Environment**:
   - Development
   - Test
   - Production (use with caution!)

4. Click **Run Sync**

### Monitor Sync Progress

- The sync runs in the background
- Page auto-refreshes every 30 seconds while syncs are running
- Click on a sync record to view detailed logs and statistics

### View Sync Details

Click the **eye icon** on any sync record to view:
- General information (start time, duration, status)
- Statistics (products created/updated/skipped, errors)
- Full sync log output
- Error details (if sync failed)

## Sync Types

### CMDB to Strapi

Synchronizes product data from ServiceNow CMDB to the specified Strapi environment:

- Creates new products from CMDB entries
- Updates existing products with latest CMDB data
- Preserves FIPS-specific fields (FIPS IDs, etc.)
- Syncs user/contact relationships

**Use Cases:**
- Initial data population
- Regular updates from CMDB
- Adding new products from CMDB

### Strapi to Strapi

Copies data from one Strapi instance to another:

- Syncs products (all fields)
- Syncs product assurances
- Syncs accessibility data
- Intelligent create/update handling

**Use Cases:**
- Refreshing Development with Production data
- Copying Test data to Development
- Environment synchronization

## Security Considerations

### API Keys

- Store API keys securely in configuration
- Use different API keys for each environment
- Production API keys should have minimal required permissions
- Development API keys can have full access

### Environment Restrictions

- Limit production syncs to authorized administrators only
- Consider implementing approval workflows for production syncs
- Monitor sync operations regularly

### Data Protection

- Syncing to Production can overwrite data - use with extreme caution
- Always test syncs in Development/Test first
- Consider backing up target environment before sync

## Troubleshooting

### Sync Fails to Start

**Problem**: "Error starting sync operation"

**Solutions**:
1. Check `FipsSync:SyncAppPath` is correct in appsettings.json
2. Verify sync-app directory exists and has required files
3. Check Node.js is installed and accessible
4. Review COMPASS logs for detailed error messages

### Sync Runs But Fails

**Problem**: Sync status shows "Failed"

**Solutions**:
1. Click on sync record to view error details
2. Check API keys are valid and have correct permissions
3. Verify network connectivity to Strapi endpoints
4. Check sync-app `.env` file configuration
5. Review full sync log for specific errors

### No Statistics Shown

**Problem**: Sync completes but shows 0 for all statistics

**Solutions**:
1. Check sync output log for actual statistics
2. Verify sync app is producing correctly formatted output
3. May need to update output parsing logic in `FipsSyncController.ParseSyncOutput()`

### Permission Errors

**Problem**: "Access denied" or "403 Forbidden"

**Solutions**:
1. Verify API keys have required permissions
2. For CMDB sync, check CMDB credentials
3. Ensure Strapi endpoints are accessible from COMPASS server

## Maintenance

### Clean Up Old Sync History

Delete old sync records to keep database clean:

1. Navigate to FIPS Sync page
2. Click **trash icon** on old records
3. Confirm deletion

Or run SQL directly:

```sql
-- Delete sync history older than 90 days
DELETE FROM FipsSyncHistories 
WHERE StartedAt < DATEADD(day, -90, GETUTCDATE());
```

### Monitor Sync Performance

Review sync duration to identify performance issues:

```sql
-- Average sync duration by type
SELECT 
    SyncType,
    AVG(DurationSeconds) as AvgDuration,
    MAX(DurationSeconds) as MaxDuration,
    COUNT(*) as TotalRuns
FROM FipsSyncHistories
WHERE Status = 'Completed'
GROUP BY SyncType;
```

## Support

For issues or questions:

1. Check sync logs and error details in COMPASS
2. Review sync-app documentation
3. Check configuration settings
4. Contact development team

## Related Documentation

- [Sync App README](../sync-app/README.md)
- [Strapi-to-Strapi Sync Guide](../sync-app/STRAPI_TO_STRAPI_SYNC.md)
- COMPASS Admin Documentation
