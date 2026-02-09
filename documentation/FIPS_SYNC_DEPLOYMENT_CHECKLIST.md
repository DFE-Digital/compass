# FIPS Sync - Deployment Checklist

Use this checklist to deploy the FIPS Sync integration to each environment.

## Pre-Deployment Checklist

- [ ] All code files have been created (14 new files)
- [ ] Program.cs has been updated with service registrations
- [ ] Controller has been updated to use orchestrator
- [ ] Documentation has been reviewed
- [ ] Development environment is available for testing

## Development Environment Setup

### Database Migration

- [ ] Navigate to compass directory
  ```bash
  cd /Users/andyjones/Source/code-digital-ops/FIPS/compass
  ```

- [ ] Create migration
  ```bash
  dotnet ef migrations add AddFipsSyncHistory
  ```

- [ ] Review migration file
  - [ ] Check `Migrations/[timestamp]_AddFipsSyncHistory.cs`
  - [ ] Verify FipsSyncHistories table will be created
  - [ ] Verify indexes are included

- [ ] Apply migration
  ```bash
  dotnet ef database update
  ```

- [ ] Verify table exists
  ```sql
  -- Run in your database client
  SELECT * FROM FipsSyncHistories;
  ```

### Configuration

- [ ] Copy configuration from `appsettings.FipsSync.json`

- [ ] Add to `appsettings.Development.json`:
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
          "ApiKey": "[YOUR-DEV-API-KEY]"
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

- [ ] Get Development Strapi API key:
  ```bash
  # Start local Strapi
  cd /Users/andyjones/Source/code-digital-ops/FIPS/cms
  npm run develop
  # Go to http://localhost:1337/admin
  # Settings → API Tokens → Create new token
  # Copy the token and update appsettings.Development.json
  ```

### Build & Test

- [ ] Clean build
  ```bash
  dotnet clean
  dotnet build
  ```

- [ ] Check for errors
  - [ ] No compilation errors
  - [ ] No warnings about missing types
  - [ ] Services are registered correctly

- [ ] Run application
  ```bash
  dotnet run
  ```

- [ ] Test application starts
  - [ ] No startup errors
  - [ ] Application listens on port
  - [ ] Can navigate to home page

### Functional Testing

- [ ] Navigate to Admin page
  - [ ] URL: `https://localhost:[port]/Admin`
  - [ ] System section visible
  - [ ] FIPS Sync card present

- [ ] Navigate to FIPS Sync page
  - [ ] URL: `https://localhost:[port]/FipsSync`
  - [ ] Page loads without errors
  - [ ] Form is visible
  - [ ] Environment dropdown works

- [ ] Test CMDB to Development sync
  - [ ] Select "CMDB to Strapi" as sync type
  - [ ] Select "Development" as target
  - [ ] Click "Run Sync"
  - [ ] Redirected to index with success message
  - [ ] New row appears in history table
  - [ ] Status shows "Running"
  - [ ] Page auto-refreshes

- [ ] Monitor sync progress
  - [ ] Status changes from "Running" to "Completed" or "Failed"
  - [ ] Duration is recorded
  - [ ] Statistics are populated (created/updated/skipped)

- [ ] View sync details
  - [ ] Click "View Details" button
  - [ ] Details page loads
  - [ ] Action log is visible
  - [ ] Statistics are shown
  - [ ] Configuration is displayed

- [ ] Verify Strapi data
  - [ ] Open Strapi admin: `http://localhost:1337/admin`
  - [ ] Go to Content Manager → Products
  - [ ] Verify products were created/updated
  - [ ] Check a few products have correct data
  - [ ] Verify `cmdb_sys_id` is populated

## Test Environment Deployment

### Prerequisites

- [ ] Development testing complete and successful
- [ ] Test environment database is accessible
- [ ] Test Azure app service is running

### Database

- [ ] Connect to Test database

- [ ] Apply migration
  ```bash
  # Using connection string for test database
  dotnet ef database update --connection "[TEST-CONNECTION-STRING]"
  ```

- [ ] Verify table exists in Test database

### Configuration

- [ ] Update `appsettings.Test.json` with FipsSync configuration
  - [ ] Same CMDB credentials
  - [ ] All three Strapi endpoints
  - [ ] Verify Test and Production API keys

### Deployment

- [ ] Commit changes to git
  ```bash
  git add .
  git commit -m "Add FIPS Sync integration to COMPASS"
  git push
  ```

- [ ] Deploy to Test Azure App Service
  - [ ] Via Azure DevOps pipeline, or
  - [ ] Manual publish:
    ```bash
    dotnet publish -c Release
    # Upload to Azure
    ```

- [ ] Verify deployment
  - [ ] App service started successfully
  - [ ] No errors in Application Insights
  - [ ] Can access the site

### Testing in Test Environment

- [ ] Navigate to Admin → FIPS Sync

- [ ] Test CMDB to Test sync
  - [ ] Select "Test" as target
  - [ ] Run sync
  - [ ] Monitor progress
  - [ ] Verify success

- [ ] Verify Test Strapi
  - [ ] Go to `https://fips-cms-test.azurewebsites.net/admin`
  - [ ] Check products were synced correctly

- [ ] Test CMDB to Development sync (from Test COMPASS)
  - [ ] Should still work
  - [ ] Verify can sync to local development

## Production Environment Deployment

### Prerequisites

- [ ] Test environment fully tested and working
- [ ] UAT completed successfully
- [ ] Change request approved (if required)
- [ ] Rollback plan documented

### Pre-Deployment

- [ ] Create database backup
- [ ] Document current database state
- [ ] Notify users of deployment window (if downtime required)

### Database

- [ ] Connect to Production database

- [ ] Apply migration
  ```bash
  dotnet ef database update --connection "[PROD-CONNECTION-STRING]"
  ```
  **OR** run SQL script manually if required by security policy

- [ ] Verify table exists
- [ ] Verify indexes created

### Configuration

- [ ] Update `appsettings.Production.json`
  - [ ] Add FipsSync configuration
  - [ ] **Use Azure Key Vault references for sensitive data**
  ```json
  {
    "FipsSync": {
      "Cmdb": {
        "Endpoint": "https://dfe.service-now.com/api/now/table/service_offering",
        "Username": "@Microsoft.KeyVault(SecretUri=https://your-vault.vault.azure.net/secrets/CmdbUsername/)",
        "Password": "@Microsoft.KeyVault(SecretUri=https://your-vault.vault.azure.net/secrets/CmdbPassword/)"
      },
      "Strapi": {
        "Production": {
          "Endpoint": "https://fips-cms.azurewebsites.net/api",
          "ApiKey": "@Microsoft.KeyVault(SecretUri=https://your-vault.vault.azure.net/secrets/StrapiProductionApiKey/)"
        }
      }
    }
  }
  ```

- [ ] Add secrets to Azure Key Vault:
  ```bash
  az keyvault secret set --vault-name your-vault --name CmdbUsername --value "POWERBI-Arch"
  az keyvault secret set --vault-name your-vault --name CmdbPassword --value "..."
  az keyvault secret set --vault-name your-vault --name StrapiProductionApiKey --value "..."
  ```

### Deployment

- [ ] Create release branch
  ```bash
  git checkout -b release/fips-sync-integration
  git push origin release/fips-sync-integration
  ```

- [ ] Deploy to Production
  - [ ] Via Azure DevOps pipeline
  - [ ] Approve production deployment
  - [ ] Monitor deployment logs

- [ ] Verify deployment
  - [ ] App service restarted successfully
  - [ ] No startup errors
  - [ ] Application Insights shows no errors
  - [ ] Health check passes

### Post-Deployment Testing

- [ ] Smoke test
  - [ ] Navigate to homepage
  - [ ] Login works
  - [ ] Navigate to Admin
  - [ ] FIPS Sync link present

- [ ] Functional test
  - [ ] Go to FIPS Sync page
  - [ ] Page loads correctly
  - [ ] Form is present
  - [ ] Environment dropdown has all options

- [ ] Test sync to Production
  - [ ] Select "Production" as target
  - [ ] Run sync
  - [ ] **Monitor very carefully**
  - [ ] Verify success
  - [ ] Check detailed logs

- [ ] Verify Production Strapi
  - [ ] Check products were synced
  - [ ] Spot-check data accuracy
  - [ ] Verify no data loss

### Rollback Plan (if needed)

If issues occur:

- [ ] Revert database migration
  ```bash
  dotnet ef database update [PreviousMigrationName]
  ```

- [ ] Revert application deployment
  - [ ] Deploy previous version
  - [ ] Verify previous version works

- [ ] Document issue for investigation

## Post-Deployment

### Documentation

- [ ] Update team wiki with:
  - [ ] How to use FIPS Sync
  - [ ] When to run syncs
  - [ ] Troubleshooting guide
  - [ ] Contact for issues

### Monitoring

- [ ] Set up Application Insights alerts:
  - [ ] Failed sync operations
  - [ ] High error rates
  - [ ] Long-running syncs

- [ ] Create dashboard
  - [ ] Sync success rate
  - [ ] Average sync duration
  - [ ] Products synced per day

### Training

- [ ] Train administrators on:
  - [ ] How to run syncs
  - [ ] How to read logs
  - [ ] When to escalate issues
  - [ ] Best practices

### Scheduling

- [ ] Decide on sync schedule
  - [ ] Daily? Weekly? On-demand?
  - [ ] What time of day?
  - [ ] Which environment?

- [ ] Implement scheduling (optional)
  - [ ] Azure Function with timer trigger
  - [ ] Hangfire background job
  - [ ] Manual runs only

## Verification Checklist

After deployment to all environments:

- [ ] Development: Sync works correctly
- [ ] Test: Sync works correctly
- [ ] Production: Sync works correctly
- [ ] All environments accessible from COMPASS
- [ ] Logs are comprehensive and helpful
- [ ] Error handling works correctly
- [ ] UI is responsive and user-friendly
- [ ] Documentation is up-to-date
- [ ] Team is trained

## Sign-Off

### Development
- [ ] Tested by: _________________ Date: _________
- [ ] Approved by: _________________ Date: _________

### Test
- [ ] Tested by: _________________ Date: _________
- [ ] Approved by: _________________ Date: _________

### Production
- [ ] Tested by: _________________ Date: _________
- [ ] Approved by: _________________ Date: _________

## Notes

Use this space to document any issues encountered or deviations from the plan:

```
Date: ___________
Issue: 
Resolution:

Date: ___________
Issue:
Resolution:
```

## Success Criteria

Deployment is considered successful when:

✅ All database migrations applied successfully
✅ Application builds and runs without errors
✅ FIPS Sync UI is accessible and functional
✅ Can sync CMDB to all three Strapi environments
✅ Sync history is recorded correctly
✅ Logs are detailed and helpful
✅ No data loss or corruption in any environment
✅ Team is trained and documentation is complete

---

**Completion Date**: __________________
**Deployed By**: __________________
**Sign-Off**: __________________
