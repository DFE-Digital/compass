# Database migration status

## ✅ Completed

1. **Production configuration updated** - `appsettings.Production.json` now contains the correct Azure SQL connection string
2. **Migration utility created** - `DataMigrationUtility.cs` handles data transfer from SQLite to Azure SQL
3. **Build errors fixed** - All Razor syntax errors have been resolved
4. **Migration script created** - `migrate-to-production.sh` for easy execution
5. **Documentation created** - `PRODUCTION_DATABASE_MIGRATION.md` with full instructions
6. **Network connectivity verified** - Port 1433 is accessible on the Azure SQL server

## ⚠️ Current issue

The migration is experiencing intermittent connection issues when trying to save data to Azure SQL. The error indicates:
- Initial connection test passes ✓
- Schema preparation succeeds ✓
- Data migration starts ✓
- But then fails with DNS resolution errors when saving

Error: `nodename nor servname provided, or not known`

## 🔍 Troubleshooting steps

### 1. Verify database exists

First, check if the `compass` database exists on the Azure SQL server:

```bash
# Using Azure CLI
az sql db show --resource-group <your-rg> --server s186d01-dops-compass --name compass
```

If it doesn't exist, create it:

```bash
az sql db create --resource-group <your-rg> --server s186d01-dops-compass --name compass --service-objective Basic
```

Or create it via Azure Portal:
- Go to SQL Server `s186d01-dops-compass`
- Click "+ New database"
- Database name: `compass`
- Pricing tier: Basic or Standard

### 2. Check firewall rules

Ensure your IP (195.89.173.204) is allowed:

```bash
az sql server firewall-rule list --resource-group <your-rg> --server s186d01-dops-compass
```

### 3. Test connection directly

You can test the connection using a SQL client:

```bash
# Using sqlcmd (if installed)
sqlcmd -S tcp:s186d01-dops-compass.database.windows.net,1433 -d compass -U RwEvbrYeyA0M7jXo4b6z3vTHH0jqXV14MkJDRCL8ms7oD9X5kiGiAe -P CaF2QrPTgoBxF751b8WaEwbSoo4QR2X7ftY2VOKTlykbSbLVpk4125
```

### 4. Alternative: Run migration from Azure

If local connection issues persist, you can run the migration from:

- **Azure Cloud Shell** - Has reliable connectivity to Azure SQL
- **Azure VM** - Create a temporary VM in the same region
- **GitHub Actions / Azure DevOps** - Run as part of deployment pipeline

## 📝 Connection string

Current production connection string (in `appsettings.Production.json`):

```
Server=tcp:s186d01-dops-compass.database.windows.net,1433;
Initial Catalog=compass;
Persist Security Info=False;
User ID=RwEvbrYeyA0M7jXo4b6z3vTHH0jqXV14MkJDRCL8ms7oD9X5kiGiAe;
Password=CaF2QrPTgoBxF751b8WaEwbSoo4QR2X7ftY2VOKTlykbSbLVpk4125;
MultipleActiveResultSets=False;
Encrypt=True;
TrustServerCertificate=False;
Connection Timeout=30;
```

## 🚀 Next steps

Once the database is confirmed to exist and firewall is configured:

```bash
cd /Users/andyjones/Source/code-digital-ops/FIPS/compass
./migrate-to-production.sh
```

Or run directly:

```bash
export ASPNETCORE_ENVIRONMENT=Production
dotnet run --migrate-data
```

## 📊 What the migration will do

When successful, it will migrate:
- 5 Risk tiers
- Risk types
- Action sources
- Users and preferences
- API tokens and permissions
- Performance metrics
- Functional standards hierarchy
- Product reporting data
- Enterprise metrics
- RAID items (objectives, risks, issues, milestones, actions)
- All junction tables

Total of 26+ tables with full referential integrity maintained.

