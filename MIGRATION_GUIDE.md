# Database Migration Guide

This guide explains how to manage database migrations for the FIPS Reporting Platform, including local development and Azure SQL Database deployment.

## Overview

The application uses Entity Framework Core migrations to manage database schema changes. This ensures:
- **Version control** of database schema changes
- **Consistent deployments** across environments
- **Rollback capability** for problematic changes
- **Azure SQL Database compatibility** for production deployments

## Current Migration Status

- **Initial Migration**: `20250919191932_InitialCreate`
- **Database Provider**: SQLite (Development) / SQL Server (Production)
- **Migration History**: Tracked in `__EFMigrationsHistory` table

## Local Development

### Prerequisites

1. Install EF Core tools globally:
   ```bash
   dotnet tool install --global dotnet-ef
   ```

2. Add tools to PATH (if needed):
   ```bash
   export PATH="$PATH:/Users/andyjones/.dotnet/tools"
   ```

### Creating New Migrations

When you modify the `ReportingDbContext` or entity models:

1. **Create a new migration**:
   ```bash
   dotnet ef migrations add MigrationName
   ```

2. **Review the generated migration** in `Migrations/` folder

3. **Apply the migration**:
   ```bash
   dotnet ef database update
   ```

### Example: Adding a New Entity

```csharp
// 1. Add new entity to ReportingDbContext.cs
public DbSet<NewEntity> NewEntities { get; set; }

// 2. Configure in OnModelCreating
modelBuilder.Entity<NewEntity>(entity =>
{
    entity.HasKey(e => e.Id);
    entity.Property(e => e.Name).HasMaxLength(100);
});

// 3. Create migration
dotnet ef migrations add AddNewEntity

// 4. Apply migration
dotnet ef database update
```

## Production Deployment (Azure SQL Database)

### Pre-deployment Checklist

1. **Review migrations**:
   ```bash
   dotnet ef migrations list
   ```

2. **Generate SQL script** (optional, for review):
   ```bash
   dotnet ef migrations script --output migration.sql
   ```

3. **Test migration on staging** environment first

### Azure Deployment Options

#### Option 1: Automatic Migration (Recommended)

The application automatically applies migrations on startup:

```csharp
// In Program.cs
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ReportingDbContext>();
    context.Database.Migrate(); // Automatically applies pending migrations
}
```

**Pros**: Simple, automatic
**Cons**: Requires application restart, potential downtime

#### Option 2: Manual Migration (Production)

For zero-downtime deployments:

1. **Generate migration script**:
   ```bash
   dotnet ef migrations script --output production-migration.sql
   ```

2. **Apply via Azure CLI**:
   ```bash
   az sql db execute-query \
     --resource-group your-resource-group \
     --server your-sql-server \
     --database your-database \
     --query-file production-migration.sql
   ```

3. **Deploy application** (migrations already applied)

#### Option 3: Azure DevOps Pipeline

```yaml
# azure-pipelines.yml
- task: DotNetCoreCLI@2
  displayName: 'Apply Database Migrations'
  inputs:
    command: 'custom'
    custom: 'ef'
    arguments: 'database update --connection-string "$(ConnectionString)"'
```

## Migration Commands Reference

### Core Commands

```bash
# Create new migration
dotnet ef migrations add MigrationName

# Apply migrations to database
dotnet ef database update

# Remove last migration (if not applied)
dotnet ef migrations remove

# List all migrations
dotnet ef migrations list

# Generate SQL script
dotnet ef migrations script

# Generate SQL script for specific migration
dotnet ef migrations script --from InitialCreate --to LatestMigration
```

### Advanced Commands

```bash
# Apply specific migration
dotnet ef database update MigrationName

# Rollback to specific migration
dotnet ef database update PreviousMigrationName

# Generate script with data seeding
dotnet ef migrations script --idempotent

# Check migration status
dotnet ef database update --dry-run
```

## Database Schema Overview

### Current Tables

1. **Milestones** - Project milestone tracking
2. **MilestoneUpdates** - Milestone status updates
3. **ReportingMetrics** - Configurable reporting metrics
4. **MetricConditions** - Metric applicability rules
5. **ReportingData** - Submitted reporting data
6. **ReportingUsers** - User management
7. **ProductAllocations** - Product-to-user assignments

### Key Relationships

- `MilestoneUpdates` → `Milestones` (Cascade delete)
- `MetricConditions` → `ReportingMetrics` (Cascade delete)
- `ReportingData` → `ReportingMetrics` (Cascade delete)

## Troubleshooting

### Common Issues

1. **Migration conflicts**:
   ```bash
   # Reset migrations (development only)
   rm -rf Migrations/
   dotnet ef migrations add InitialCreate
   ```

2. **Database locked**:
   ```bash
   # Stop application first
   pkill -f "dotnet run"
   # Then run migration
   dotnet ef database update
   ```

3. **Connection string issues**:
   - Check `appsettings.json` and `appsettings.Production.json`
   - Verify Azure SQL firewall rules
   - Test connection with Azure Data Studio

### Migration Rollback

```bash
# Rollback to previous migration
dotnet ef database update PreviousMigrationName

# Or rollback all migrations
dotnet ef database update 0
```

## Best Practices

1. **Always test migrations** on staging environment first
2. **Backup production database** before applying migrations
3. **Use descriptive migration names** (e.g., `AddUserRolesTable`)
4. **Review generated SQL** before applying to production
5. **Keep migrations small** and focused on single changes
6. **Never modify applied migrations** - create new ones instead

## Environment-Specific Configuration

### Development (SQLite)
```json
{
  "ConnectionStrings": {
    "ReportingDb": "Data Source=reporting.db"
  }
}
```

### Production (Azure SQL)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=tcp:your-server.database.windows.net,1433;Initial Catalog=your-database;Persist Security Info=False;User ID=your-user;Password=your-password;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
  }
}
```

## Monitoring and Maintenance

### Check Migration Status
```sql
-- View applied migrations
SELECT * FROM __EFMigrationsHistory ORDER BY MigrationId;
```

### Monitor Database Health
- Use Azure SQL Analytics
- Set up alerts for connection failures
- Monitor migration execution times

---

**Last Updated**: September 19, 2025
**Migration Version**: 20250919191932_InitialCreate
