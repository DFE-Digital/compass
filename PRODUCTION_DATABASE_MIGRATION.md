# Production database migration

This guide explains how to migrate data from your local SQLite database to the Azure SQL Production database.

## Prerequisites

Before running the migration, ensure:

1. You have the production Azure SQL database credentials configured in `appsettings.Production.json`
2. Your local `compass.db` SQLite database contains the data you want to migrate
3. Your IP address is allowed through the Azure SQL firewall
4. You have .NET 8.0 SDK installed

## Azure SQL database configuration

The production database connection has been configured in `appsettings.Production.json`:

- **Server**: s186d01-dops-compass.database.windows.net
- **Database**: compass
- **Authentication**: SQL Server authentication (credentials in connection string)

## Running the migration

### Option 1: Using the migration script (recommended)

Run the provided shell script:

```bash
./migrate-to-production.sh
```

This script will:
- Set the environment to Production
- Run the data migration utility
- Display progress and any errors

### Option 2: Manual execution

Set the environment and run directly:

```bash
export ASPNETCORE_ENVIRONMENT=Production
dotnet run --migrate-data
```

## What the migration does

The migration utility will:

1. **Apply migrations**: Ensure the Azure SQL database schema is up to date
2. **Migrate data**: Copy all data from SQLite to Azure SQL in the correct order to maintain referential integrity

The migration handles the following tables in order:

### Lookup tables
- Risk tiers
- Risk types  
- Action sources

### User data
- Users
- User preferences

### API management
- API tokens
- API token permissions
- API request logs

### Metrics
- Performance metrics
- Enterprise metrics

### Functional standards
- Functional standards
- Functional standard themes
- Practice areas
- Criteria

### Reporting
- Product returns
- Product metric values
- Functional standard assessments
- Assessment criteria responses
- Enterprise returns
- Enterprise metric values

### RAID items
- Objectives
- Risks
- Issues
- Milestones
- Actions
- Comments

### Junction tables
- Risk actions
- Risk risk types
- Issue actions
- Milestone actions
- Milestone risks
- Milestone issues

## Verifying the migration

After the migration completes, you should:

1. Check the console output for any errors
2. Verify data in the Azure SQL database using Azure Data Studio or SQL Server Management Studio
3. Test the application in production mode to ensure data is accessible

## Firewall configuration

If you encounter connection errors, you may need to add your IP address to the Azure SQL firewall:

1. Go to the Azure Portal
2. Navigate to the SQL Server: `s186d01-dops-compass`
3. Under Security, select "Networking"
4. Add your client IP address
5. Save the changes

## Troubleshooting

### Connection timeout
- Verify your IP is allowed in the Azure SQL firewall
- Check that the connection string is correct in `appsettings.Production.json`

### Foreign key constraint errors
- The migration utility handles dependencies automatically
- If errors occur, they will be displayed in the console

### Duplicate data
- The migration assumes the target database is empty or being replaced
- If you need to merge data, you may need to modify the migration utility

## Rolling back

If you need to start over:

1. Delete all data from the Azure SQL database tables
2. Or drop and recreate the database
3. Run the migration again

## Notes

- The migration uses `AsNoTracking()` for better performance
- Each table is migrated in a single batch
- Progress is displayed for each table being migrated
- The source SQLite database is not modified

